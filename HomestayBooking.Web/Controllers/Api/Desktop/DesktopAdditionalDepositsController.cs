using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

/// <summary>Chỉ Kế toán, như web; service vẫn chặn người gửi hoặc người duyệt nghiệp vụ tự xác nhận tiền.</summary>
[Route("api/desktop/additional-deposits")]
[Authorize(Policy = AppPolicies.ChangeRequestFinance)]
public sealed class DesktopAdditionalDepositsController(ApplicationDbContext db, AccountingService accounting,
    BookingChangeRequestService changes) : DesktopController(db)
{
    /// <summary>Cọc bổ sung đã nộp của yêu cầu đổi đơn đang chờ tiền, cũ nhất trước.</summary>
    /// <response code="200">Trang cọc bổ sung.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<AdditionalDepositDto>>(200)]
    public async Task<ActionResult<DesktopPage<AdditionalDepositDto>>> List([FromQuery] int? page = null) =>
        Ok(DesktopPage<AdditionalDepositDto>.From(await accounting.AdditionalDepositsAsync(page, await CurrentActorAsync()), p =>
            new(p.Id, p.ChangeRequestId!.Value, p.Booking.Code, p.Booking.ContactName, p.Booking.ContactPhone, p.Booking.Homestay.Name,
                p.Amount, p.Method.ToString(),
                Utc(p.Method == PaymentMethod.BankAutoMatch ? p.BankTransaction?.TransactionTime ?? p.SubmittedAt : p.SubmittedAt),
                Utc(p.ChangeRequest!.PaymentDeadlineAt), p.ChangeRequest.DecisionNote, p.BankTransactionId,
                p.BankTransaction?.TransferContent ?? p.TransferContent, p.Booking.SettlementItem?.SettlementId,
                p.ReceiptImageUrl is null ? null : $"{Request.PathBase}/api/desktop/receipts/{p.Id}/file")));

    /// <summary>Xác nhận tiền và áp dụng thay đổi qua BookingChangeRequestService.ConfirmAsync; id là ChangeRequestId.</summary>
    /// <response code="200">Trạng thái yêu cầu sau xử lý: Applied, hoặc Rejected khi giá đã đổi (kèm RejectionReason).</response>
    /// <response code="400">Thiếu ghi chú, đã xử lý hoặc không đủ điều kiện.</response>
    /// <response code="404">Không có yêu cầu.</response>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType<ChangeRequestDecisionDto>(200)]
    public async Task<ActionResult<ChangeRequestDecisionDto>> Confirm(int id, NoteRequest request)
    {
        var result = await changes.ConfirmAsync(id, request.Note, User);
        return Ok(new ChangeRequestDecisionDto(result.Id, result.Status.ToString(), result.RejectionReason));
    }

    /// <summary>Từ chối khoản cọc bổ sung, nhả phòng giữ cho yêu cầu; id là ChangeRequestId.</summary>
    /// <response code="204">Đã từ chối.</response>
    /// <response code="400">Thiếu lý do hoặc đã xử lý.</response>
    /// <response code="404">Không có yêu cầu.</response>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Reject(int id, ReasonRequest request)
    {
        await changes.RejectFinanceAsync(id, request.Reason, User);
        return NoContent();
    }
}
