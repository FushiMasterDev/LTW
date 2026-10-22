using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop.Owner;

[Route("api/desktop/owner")]
public sealed class DesktopOwnerFinanceController(HomestayContentService content, ReviewService reviews, SettlementService settlements,
    OwnerRegistrationQueryService registrations, OwnerResourceService access) : DesktopOwnerController
{
    /// <summary>Đánh giá hiện của homestay thuộc chủ, cùng điểm trung bình và bộ lọc như web.</summary>
    /// <response code="200">Trang đánh giá và tổng hợp điểm.</response>
    /// <response code="404">Homestay ngoài phạm vi.</response>
    [HttpGet("reviews")]
    [ProducesResponseType<OwnerReviewsDto>(200)]
    public async Task<ActionResult<OwnerReviewsDto>> Reviews([FromQuery] int? homestayId, [FromQuery] int? page)
    {
        homestayId ??= (await content.OwnerHomestaysAsync(Actor)).FirstOrDefault()?.Id;
        if (homestayId is not int id) return Ok(new OwnerReviewsDto(null, DesktopPage<OwnerReviewDto>.Slice([], page), null, 0));
        await access.HomeAsync(id, Actor);
        var d = await reviews.ListAsync(id, page, actor: Actor);
        return Ok(new OwnerReviewsDto(id, DesktopPage<OwnerReviewDto>.From(d.Reviews, DesktopOwnerMapping.Review), d.Average, d.Count));
    }

    /// <summary>Trả lời đánh giá của mình, văn bản thuần 1–1000 ký tự qua ReviewService.</summary>
    /// <response code="200">Id đánh giá đã trả lời.</response>
    /// <response code="400">Nội dung không hợp lệ.</response>
    /// <response code="404">Đánh giá ngoài phạm vi.</response>
    [HttpPost("reviews/{id:int}/reply")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Reply(int id, OwnerReplyRequest request)
    {
        await access.ReviewAsync(id, Actor);
        await reviews.ReplyAsync(id, request.Reply, Actor);
        return Ok(new OwnerWriteResult(id));
    }

    /// <summary>Đối soát đã gửi của chủ, không hiện bảng nháp, lọc status và phân trang như web.</summary>
    /// <response code="200">Trang đối soát.</response>
    /// <response code="400">Status không hợp lệ.</response>
    [HttpGet("settlements")]
    [ProducesResponseType<DesktopPage<OwnerSettlementDto>>(200)]
    public async Task<ActionResult<DesktopPage<OwnerSettlementDto>>> Settlements([FromQuery] SettlementStatus? status, [FromQuery] int? page)
    {
        if (status is { } s && !Enum.IsDefined(s)) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        return Ok(DesktopPage<OwnerSettlementDto>.From(await settlements.ListAsync(new(Status: status, Page: page), Actor), DesktopOwnerMapping.Settlement));
    }

    /// <summary>Snapshot đối soát, dòng đơn, khoản bù và lịch sử; chỉ bảng đã gửi thuộc chủ.</summary>
    /// <response code="200">Chi tiết với timestamp UTC, enum chữ.</response>
    /// <response code="404">Bảng nháp hoặc ngoài phạm vi.</response>
    [HttpGet("settlements/{id:int}")]
    [ProducesResponseType<OwnerSettlementDetailsDto>(200)]
    public async Task<ActionResult<OwnerSettlementDetailsDto>> Settlement(int id)
    {
        await access.SettlementAsync(id, Actor);
        var d = await settlements.DetailsAsync(id, Actor);
        return Ok(new OwnerSettlementDetailsDto(DesktopOwnerMapping.Settlement(d.Settlement), d.Lines.Select(l => new OwnerSettlementLineDto(
            l.Item.Id, l.Item.BookingId, l.BookingCode, l.Item.ItemType.ToString(), l.Item.RoomAmount, l.Item.DepositAmount, l.Item.RefundAmount,
            l.Item.RetainedAmount, l.Item.CommissionRate, l.Item.CommissionAmount, l.Item.PayoutAmount, l.Item.DiscountAmount, l.FujiNetRevenue)).ToList(),
            d.UnclosedBookingCodes, d.Compensations.Select(p => new OwnerCompensationDto(p.Id, p.Booking.Code, p.Amount, p.Status.ToString())).ToList(),
            d.History.Select(a => new AuditDto(a.Id, a.Action.ToString(), null, a.OldValues, a.NewValues, Utc(a.CreatedAt))).ToList()));
    }

    /// <summary>Chủ xác nhận đối soát đã gửi qua SettlementService.</summary>
    /// <response code="200">Trạng thái mới.</response>
    /// <response code="400">Bảng đã xử lý.</response>
    [HttpPost("settlements/{id:int}/confirm")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Confirm(int id)
    {
        await access.SettlementAsync(id, Actor);
        var s = await settlements.ConfirmAsync(id, Actor);
        return Ok(new OwnerWriteResult(s.Id, s.Status.ToString()));
    }

    /// <summary>Gửi phản hồi sai lệch 1–500 ký tự, ghi audit và thông báo kế toán như web.</summary>
    /// <response code="200">Id và trạng thái đối soát.</response>
    /// <response code="400">Ghi chú sai.</response>
    [HttpPost("settlements/{id:int}/discrepancy")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Discrepancy(int id, OwnerNoteRequest request)
    {
        await access.SettlementAsync(id, Actor);
        var s = await settlements.ReportDiscrepancyAsync(id, request.Note, Actor);
        return Ok(new OwnerWriteResult(s.Id, s.Status.ToString()));
    }

    /// <summary>Đọc giấy tờ riêng tư qua service kiểm quyền, hỗ trợ cả giấy tờ hồ sơ lịch sử của chính chủ.</summary>
    /// <response code="200">Nội dung file, giữ MIME và tên tải về.</response>
    /// <response code="404">File thiếu hoặc giấy tờ ngoài phạm vi.</response>
    [HttpGet("documents/{id:int}/file")]
    [ProducesResponseType(typeof(FileResult), 200, "image/png", "image/jpeg", "image/webp", "application/pdf")]
    public async Task<IActionResult> Document(int id)
    {
        await access.DocumentAsync(id, Actor);
        var f = await registrations.OpenDocumentAsync(id, Actor);
        return PhysicalFile(f.Path, f.ContentType, f.DownloadName);
    }
}
