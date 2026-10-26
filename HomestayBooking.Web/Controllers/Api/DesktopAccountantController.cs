using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomestayBooking.Web.Extensions;

namespace HomestayBooking.Web.Controllers.Api;

[ApiController]
[Authorize(Policy = "DesktopAccountant")]
[Route("api/desktop/accountant")]
public sealed class DesktopAccountantController(ApplicationDbContext db, AccountingService accounting, BookingChangeRequestService changes,
    ReceiptReviewService receiptReviews, ReceiptFileStore receiptFiles) : ControllerBase
{
    public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int TotalPages, int TotalCount);
    public sealed record NoteInput(string Note);
    public sealed record RefundInput(string BankRef, string? Note);
    public sealed record RejectReceiptInput(string Reason);
    public sealed record ReceiptDto(int PaymentId, string BookingCode, string ContactName, string ContactPhone, string HomestayName,
        decimal AmountDue, string? TransferContent, DateTime HoldExpiresAt, int PreviousRejections, string Status);

    [HttpGet("receipts")]
    public async Task<IActionResult> Receipts([FromQuery] string? status, [FromQuery] int page = 1)
    {
        var selected = PaymentStatus.Submitted;
        if (!string.IsNullOrWhiteSpace(status) && !Enum.TryParse(status, true, out selected)) return Problem(detail: "Trạng thái bill không hợp lệ.", statusCode: 400);
        var result = await accounting.ReceiptsAsync(selected, page, Actor);
        var rows = result.Select(r => new ReceiptDto(r.PaymentId, r.BookingCode, r.ContactName, r.ContactPhone, r.HomestayName,
            r.AmountDue, r.TransferContent, r.HoldExpiresAt, r.PreviousRejections, r.Status.ToString())).ToList();
        return Ok(new PageResult<ReceiptDto>(rows, result.PageIndex, Math.Max(1, result.TotalPages), result.TotalCount));
    }

    [HttpGet("receipts/{id:int}/file")]
    public async Task<IActionResult> ReceiptFile(int id)
    {
        var name = await db.Payments.AsNoTracking().Where(p => p.Id == id && p.Type == PaymentType.Deposit
            && p.Method == PaymentMethod.ManualReceipt).Select(p => p.ReceiptImageUrl).SingleOrDefaultAsync();
        var path = receiptFiles.Resolve(name);
        return path is null ? NotFound() : PhysicalFile(path, ReceiptFileStore.ContentTypeOf(path));
    }

    [HttpPost("receipts/{id:int}/approve")]
    public async Task<IActionResult> ApproveReceipt(int id)
    {
        try { await receiptReviews.ApproveAsync(id, EmployeeId); return NoContent(); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
        catch (DbUpdateConcurrencyException) { return Problem(detail: "Đơn vừa thay đổi trạng thái, vui lòng tải lại danh sách.", statusCode: 409); }
    }

    [HttpPost("receipts/{id:int}/reject")]
    public async Task<IActionResult> RejectReceipt(int id, RejectReceiptInput input)
    {
        try { await receiptReviews.RejectAsync(id, EmployeeId, input.Reason); return NoContent(); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
        catch (DbUpdateConcurrencyException) { return Problem(detail: "Đơn vừa thay đổi trạng thái, vui lòng tải lại danh sách.", statusCode: 409); }
    }

    [HttpGet("holidays")]
    public async Task<IActionResult> Holidays() => Ok(await db.Holidays.AsNoTracking().OrderBy(h => h.Date).Select(h => new { Date = h.Date.ToString("yyyy-MM-dd"), h.Name }).ToListAsync());

    [HttpGet("areas")]
    public async Task<IActionResult> Areas() => Ok(await db.Areas.AsNoTracking().OrderBy(a => a.Name).Select(a => new { a.Id, a.Name, Province = a.Province, Group = a.AreaGroup.ToString() }).ToListAsync());

    [HttpGet("room-categories")]
    public async Task<IActionResult> RoomCategories() => Ok(await db.RoomCategories.AsNoTracking().OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync());

    [HttpGet("amenities")]
    public async Task<IActionResult> Amenities() => Ok(await db.Amenities.AsNoTracking().OrderBy(a => a.Name).Select(a => new { a.Id, a.Name }).ToListAsync());

    [HttpGet("bank-transactions")]
    public async Task<IActionResult> Transactions([FromQuery] string? status, [FromQuery] int page = 1)
    {
        BankMatchStatus? match = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BankMatchStatus>(status, true, out var parsed)) return Problem(detail: "Trạng thái giao dịch không hợp lệ.", statusCode: 400);
            match = parsed;
        }
        var result = await accounting.TransactionsAsync(match, all: match is not null, q: null, page, Actor);
        var rows = result.Select(t => new { t.Id, Provider = t.Provider.ToString(), t.ProviderTransactionId, t.Amount, t.TransferContent,
            t.TransactionTime, MatchStatus = t.MatchStatus.ToString(), t.BookingCode, t.RefundBankRef }).Cast<object>().ToList();
        return Ok(new PageResult<object>(rows, result.PageIndex, Math.Max(1, result.TotalPages), result.TotalCount));
    }

    [HttpPost("bank-transactions/{id:int}/refund")]
    public async Task<IActionResult> RefundTransaction(int id, RefundInput input)
    {
        try { await accounting.RefundTransactionAsync(id, EmployeeId, input.BankRef, input.Note); return NoContent(); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpGet("additional-deposits")]
    public async Task<IActionResult> AdditionalDeposits([FromQuery] int page = 1)
    {
        var result = await accounting.AdditionalDepositsAsync(page, Actor);
        var rows = result.Select(p => new { p.Id, ChangeRequestId = p.ChangeRequestId, BookingCode = p.Booking.Code, CustomerName = p.Booking.ContactName,
            p.Booking.ContactPhone, HomestayName = p.Booking.Homestay.Name, p.Amount, p.ChangeRequest!.AdditionalDeposit,
            SubmittedAt = p.SubmittedAt, p.TransferContent }).ToList();
        return Ok(new PageResult<object>(rows, result.PageIndex, Math.Max(1, result.TotalPages), result.TotalCount));
    }

    [HttpPost("additional-deposits/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmAdditionalDeposit(int id, NoteInput input)
    {
        try { await changes.ConfirmAsync(id, input.Note, User); return NoContent(); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpPost("additional-deposits/{id:int}/reject")]
    public async Task<IActionResult> RejectAdditionalDeposit(int id, NoteInput input)
    {
        try { await changes.RejectFinanceAsync(id, input.Note, User); return NoContent(); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    private Actor Actor => new(ActorType.Employee, User.FindFirstValue(ClaimTypes.NameIdentifier), EmployeeId);
    private int EmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var id) ? id : 0;
}
