using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/receipts")]
[Authorize(Policy = AppPolicies.Accounting)]
public sealed class DesktopReceiptsController(ApplicationDbContext db, AccountingService accounting, ReceiptReviewService reviews,
    ReceiptFileStore files) : DesktopController(db)
{
    /// <summary>Bill cọc đầu qua AccountingService.ReceiptsAsync, mặc định Submitted; phân trang, thời điểm UTC.</summary>
    /// <response code="200">Trang bill.</response>
    /// <response code="400">Status hoặc page không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<ReceiptDto>>(200)]
    public async Task<ActionResult<DesktopPage<ReceiptDto>>> List([FromQuery] PaymentStatus status = PaymentStatus.Submitted, [FromQuery] int? page = null)
    {
        if (!Enum.IsDefined(status)) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        return Ok(DesktopPage<ReceiptDto>.From(await accounting.ReceiptsAsync(status, page, await CurrentActorAsync()), r =>
            new(r.PaymentId, r.BookingCode, r.BookingStatus.ToString(), r.ContactName, r.ContactPhone, r.HomestayName,
                r.Amount, r.AmountDue, r.TransferContent, r.Status.ToString(), Utc(r.SubmittedAt), Utc(r.HoldExpiresAt),
                r.PreviousRejections, r.ReviewedBy, Utc(r.ReviewedAt), r.RejectReason, r.SettlementId,
                $"{Request.PathBase}/api/desktop/receipts/{r.PaymentId}/file")));
    }

    /// <summary>File bill riêng tư qua ReceiptFileStore, cùng quyền đọc Accountant/Manager/Administrator của web.</summary>
    /// <response code="200">Nội dung file với MIME gốc.</response>
    /// <response code="404">Không có payment hoặc file không tồn tại.</response>
    [HttpGet("{paymentId:int}/file")]
    [ProducesResponseType(typeof(FileResult), 200, "image/png", "image/jpeg", "image/webp", "application/pdf")]
    public async Task<IActionResult> File(int paymentId)
    {
        // Recheck the active employee through the read service as well as the endpoint policy.
        await accounting.ReceiptsAsync(PaymentStatus.Submitted, 1, await CurrentActorAsync());
        var name = await Db.Payments.AsNoTracking().Where(p => p.Id == paymentId).Select(p => p.ReceiptImageUrl).SingleOrDefaultAsync();
        var path = files.Resolve(name) ?? throw new KeyNotFoundException("Không tìm thấy ảnh bill.");
        return PhysicalFile(path, ReceiptFileStore.ContentTypeOf(path));
    }

    /// <summary>Duyệt bill qua ReceiptReviewService.ApproveAsync, service vẫn kiểm người xử lý Accountant/Manager.</summary>
    /// <response code="204">Đã duyệt.</response>
    /// <response code="400">Bill/trạng thái/người duyệt không hợp lệ.</response>
    /// <response code="404">Không có payment.</response>
    /// <response code="409">Cập nhật đồng thời.</response>
    [HttpPost("{paymentId:int}/approve")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Approve(int paymentId)
    {
        await reviews.ApproveAsync(paymentId, await CurrentEmployeeIdAsync());
        return NoContent();
    }

    /// <summary>Từ chối bill có lý do qua ReceiptReviewService.RejectAsync; service giữ toàn bộ luật hạn và số lần từ chối.</summary>
    /// <response code="204">Đã từ chối.</response>
    /// <response code="400">Thiếu lý do, sai trạng thái hoặc không được duyệt.</response>
    /// <response code="404">Không có payment.</response>
    /// <response code="409">Cập nhật đồng thời.</response>
    [HttpPost("{paymentId:int}/reject")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Reject(int paymentId, ReasonRequest request)
    {
        await reviews.RejectAsync(paymentId, await CurrentEmployeeIdAsync(), request.Reason);
        return NoContent();
    }
}
