using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

/// <summary>Giao dịch ngân hàng (D13): mặc định các khoản cần xử lý tay — chuyển thiếu, đến muộn, không rõ đơn.</summary>
public class TransactionsController : AccountingController
{
    private readonly AccountingService _accounting;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TransactionsController(ApplicationDbContext db, AccountingService accounting, IStringLocalizer<SharedResource> localizer) : base(db)
    {
        _accounting = accounting;
        _localizer = localizer;
    }

    /// <summary>status = null → các khoản cần xử lý; all = true → mọi giao dịch.</summary>
    public async Task<IActionResult> Index(BankMatchStatus? status, bool all, string? q, int? page)
    {


        ViewData["Status"] = status;
        ViewData["All"] = all;
        ViewData["Query"] = q;
        await LoadCountsAsync(_accounting);
        return View(await _accounting.TransactionsAsync(status, all, q, page, await CurrentActorAsync()));
    }

    public async Task<IActionResult> Process(int id)
    {
        var row = await _accounting.TransactionAsync(id, await CurrentActorAsync());
        if (row is null)
        {
            return NotFound();
        }
        if (!AccountingService.OpenStatuses.Contains(row.MatchStatus))
        {
            TempData["Error"] = _localizer["Giao dịch này đã được xử lý."].Value;
            return RedirectToAction(nameof(Index), new { all = true });
        }

        // Gợi ý: đơn còn chờ thanh toán, tạo trong 1 ngày trước giao dịch, cần chuyển không quá số tiền nhận được
        var from = row.TransactionTime.AddDays(-1);
        var to = row.TransactionTime.AddHours(1);
        var suggestions = await Db.Bookings.AsNoTracking()
            .Where(b => (b.Status == BookingStatus.AwaitingPayment || b.Status == BookingStatus.PaymentSubmitted)
                        && b.DepositAmount - b.DiscountAmount <= row.Amount && b.CreatedAt >= from && b.CreatedAt <= to)
            .Select(b => new MatchSuggestion(b.Code, b.ContactName, b.ContactPhone, b.Homestay.Name, b.Status,
                b.DepositAmount - b.DiscountAmount, b.CreatedAt))
            .ToListAsync();
        await LoadCountsAsync(_accounting);
        return View(new TransactionProcessViewModel
        {
            Transaction = row,
            Suggestions = suggestions.OrderBy(s => row.Amount - s.AmountDue).ThenByDescending(s => s.CreatedAt).Take(5).ToList(),
        });
    }

    [HttpPost]
    public Task<IActionResult> Match(int id, string? bookingCode, string? note) =>
        HandleAsync(id, async employeeId =>
        {
            var booking = await _accounting.MatchAsync(id, bookingCode ?? string.Empty, employeeId, note);
            return _localizer["Đã khớp giao dịch với đơn {0}, đơn chuyển sang Đã đặt cọc.", booking.Code].Value;
        });

    [HttpPost]
    public Task<IActionResult> Refund(int id, string? bankRef, string? note) =>
        HandleAsync(id, async employeeId =>
        {
            await _accounting.RefundTransactionAsync(id, employeeId, bankRef ?? string.Empty, note);
            return _localizer["Đã ghi nhận chuyển trả giao dịch."].Value;
        });

    [HttpPost]
    public Task<IActionResult> Ignore(int id, string? note) =>
        HandleAsync(id, async employeeId =>
        {
            await _accounting.IgnoreTransactionAsync(id, employeeId, note ?? string.Empty);
            return _localizer["Đã đánh dấu bỏ qua giao dịch."].Value;
        });

    private async Task<IActionResult> HandleAsync(int id, Func<int, Task<string>> action)
    {
        try
        {
            TempData["Success"] = await action(await CurrentEmployeeIdAsync());
            return RedirectToAction(nameof(Index));
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
            return RedirectToAction(nameof(Process), new { id });
        }
    }

}
