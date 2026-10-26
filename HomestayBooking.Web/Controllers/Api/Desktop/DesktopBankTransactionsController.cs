using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/bank-transactions")]
[Authorize(Policy = AppPolicies.Accounting)]
public sealed class DesktopBankTransactionsController(ApplicationDbContext db, AccountingService accounting) : DesktopController(db)
{
    /// <summary>Giao dịch ngân hàng qua AccountingService.TransactionsAsync, giống web: không status và all=false là các khoản cần xử lý tay.</summary>
    /// <response code="200">Trang giao dịch, mới nhất trước.</response>
    /// <response code="400">Status hoặc page không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<BankTransactionDto>>(200)]
    public async Task<ActionResult<DesktopPage<BankTransactionDto>>> List([FromQuery] BankMatchStatus? status = null,
        [FromQuery] bool all = false, [FromQuery] string? q = null, [FromQuery] int? page = null)
    {
        if (status is { } s && !Enum.IsDefined(s)) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        return Ok(DesktopPage<BankTransactionDto>.From(await accounting.TransactionsAsync(status, all, q, page, await CurrentActorAsync()), t =>
            new(t.Id, t.Provider.ToString(), t.ProviderTransactionId, t.Amount, t.TransferContent, Utc(t.TransactionTime),
                t.MatchStatus.ToString(), t.BookingCode, t.BookingStatus?.ToString(), t.BookingAmountDue, t.Note, t.ProcessedBy,
                Utc(t.ProcessedAt), t.RefundBankRef, t.SettlementId)));
    }

    /// <summary>Khớp tay giao dịch còn mở với đơn chờ thanh toán qua AccountingService.MatchAsync.</summary>
    /// <response code="200">Đơn sau khi khớp.</response>
    /// <response code="400">Giao dịch đã xử lý, đơn không còn chờ thanh toán hoặc số tiền không đủ.</response>
    /// <response code="404">Không có giao dịch hoặc mã đơn.</response>
    [HttpPost("{id:int}/match")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public async Task<ActionResult<BookingStatusDto>> Match(int id, MatchRequest request)
    {
        var booking = await accounting.MatchAsync(id, request.BookingCode, await CurrentEmployeeIdAsync(), request.Note);
        return Ok(new BookingStatusDto(booking.Id, booking.Code, booking.Status.ToString()));
    }

    /// <summary>Ghi nhận đã chuyển trả giao dịch còn mở, bắt buộc mã giao dịch chuyển hoàn.</summary>
    /// <response code="204">Đã ghi nhận.</response>
    /// <response code="400">Thiếu mã chuyển hoàn hoặc giao dịch đã xử lý.</response>
    /// <response code="404">Không có giao dịch.</response>
    [HttpPost("{id:int}/refund")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Refund(int id, RefundRequest request)
    {
        await accounting.RefundTransactionAsync(id, await CurrentEmployeeIdAsync(), request.BankRef, request.Note);
        return NoContent();
    }

    /// <summary>Bỏ qua tiền vào không liên quan đặt phòng, bắt buộc lý do.</summary>
    /// <response code="204">Đã bỏ qua.</response>
    /// <response code="400">Thiếu lý do hoặc giao dịch đã xử lý.</response>
    /// <response code="404">Không có giao dịch.</response>
    [HttpPost("{id:int}/ignore")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Ignore(int id, ReasonRequest request)
    {
        await accounting.IgnoreTransactionAsync(id, await CurrentEmployeeIdAsync(), request.Reason);
        return NoContent();
    }
}
