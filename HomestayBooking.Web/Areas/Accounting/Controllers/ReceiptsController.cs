using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

/// <summary>Duyệt ảnh bill — chỉ các trường hợp giao dịch không tự khớp được (D11).</summary>
public class ReceiptsController : AccountingController
{
    private readonly ReceiptReviewService _reviews;
    private readonly AccountingService _accounting;
    private readonly BookingOptions _options;
    private readonly TimeProvider _clock;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReceiptsController(ApplicationDbContext db, ReceiptReviewService reviews, AccountingService accounting,
        IOptions<BookingOptions> options, TimeProvider clock, IStringLocalizer<SharedResource> localizer) : base(db)
    {
        _reviews = reviews;
        _accounting = accounting;
        _options = options.Value;
        _clock = clock;
        _localizer = localizer;
    }

    /// <summary>Mặc định: bill chờ duyệt, cũ nhất trước. status = Approved / Rejected để xem lịch sử.</summary>
    public async Task<IActionResult> Index(PaymentStatus? status, int? page)
    {
        var shown = status ?? PaymentStatus.Submitted;
        var query = Db.Payments.AsNoTracking()
            .Where(p => p.Method == PaymentMethod.ManualReceipt && p.Status == shown);
        query = shown == PaymentStatus.Submitted
            ? query.OrderBy(p => p.SubmittedAt)
            : query.OrderByDescending(p => p.ReviewedAt);
        var rows = query.Select(p => new ReceiptRow(
            p.Id, p.Booking.Code, p.Booking.Status, p.Booking.ContactName, p.Booking.ContactPhone, p.Booking.Homestay.Name,
            p.Amount, p.Booking.DepositAmount - p.Booking.DiscountAmount, p.TransferContent, p.Status, p.SubmittedAt,
            p.Booking.HoldExpiresAt,
            p.Booking.Payments.Count(o => o.Id != p.Id && o.Method == PaymentMethod.ManualReceipt && o.Status == PaymentStatus.Rejected),
            p.ReviewedByEmployee == null ? null : p.ReviewedByEmployee.Person.FullName ?? p.ReviewedByEmployee.EmployeeCode,
            p.ReviewedAt, p.RejectReason));

        var list = await PaginatedList<ReceiptRow>.CreateAsync(rows, page);
        var now = _clock.GetUtcNow().UtcDateTime;
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = list[i] with
            {
                OfficeMinutesWaiting = VietnamTime.OfficeMinutesBetween(list[i].SubmittedAt, now, _options.OfficeStartHour, _options.OfficeEndHour),
            };
        }
        ViewData["Status"] = shown;
        ViewData["AlertMinutes"] = _options.ReceiptReviewAlertMinutes;
        ViewData["MaxRejections"] = _options.MaxReceiptRejections;
        await LoadCountsAsync(_accounting);
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _reviews.ApproveAsync(id, await CurrentEmployeeIdAsync());
            TempData["Success"] = _localizer["Đã duyệt bill, đơn chuyển sang Đã đặt cọc."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = _localizer["Đơn vừa thay đổi trạng thái, vui lòng tải lại trang."].Value;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        try
        {
            await _reviews.RejectAsync(id, await CurrentEmployeeIdAsync(), reason ?? string.Empty);
            TempData["Success"] = _localizer["Đã từ chối bill và báo cho khách."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = _localizer["Đơn vừa thay đổi trạng thái, vui lòng tải lại trang."].Value;
        }
        return RedirectToAction(nameof(Index));
    }
}
