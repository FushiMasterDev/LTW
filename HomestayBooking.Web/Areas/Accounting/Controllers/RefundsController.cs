using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

/// <summary>Hoàn cọc (BR-04, D12): đơn hủy / bị từ chối đã chốt số tiền hoàn, kế toán chuyển rồi ghi nhận.</summary>
public class RefundsController : AccountingController
{
    private readonly AccountingService _accounting;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RefundsController(ApplicationDbContext db, AccountingService accounting, IStringLocalizer<SharedResource> localizer) : base(db)
    {
        _accounting = accounting;
        _localizer = localizer;
    }

    /// <summary>done = false: chờ hoàn, cũ nhất trước; done = true: đã hoàn, mới nhất trước.</summary>
    public async Task<IActionResult> Index(bool done, string? q, int? page)
    {
        var query = done
            ? Db.Bookings.AsNoTracking().Where(b => b.Payments.Any(p => p.Type == PaymentType.Refund))
            : _accounting.PendingRefunds().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(b => b.Code.Contains(term) || b.ContactName.Contains(term) || (phone != null && b.ContactPhone == phone));
        }
        query = done
            ? query.OrderByDescending(b => b.Payments.Where(p => p.Type == PaymentType.Refund).Max(p => p.ReviewedAt))
            : query.OrderBy(b => b.CancelledAt);

        var rows = query.Select(b => new RefundRow(
            b.Id, b.Code, b.Status, b.ContactName, b.ContactPhone, b.Homestay.Name,
            b.DepositAmount - b.DiscountAmount, b.RefundAmount ?? 0, b.CancelledAt, b.CancelReason,
            b.Payments.Where(p => p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Approved).Select(p => p.TransferContent).FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund).Select(p => p.BankRef).FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund)
                .Select(p => p.ReviewedByEmployee == null ? null : p.ReviewedByEmployee.Person.FullName ?? p.ReviewedByEmployee.EmployeeCode)
                .FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund).Select(p => p.ReviewedAt).FirstOrDefault()));

        ViewData["Done"] = done;
        ViewData["Query"] = q;
        await LoadCountsAsync(_accounting);
        return View(await PaginatedList<RefundRow>.CreateAsync(rows, page));
    }

    [HttpPost]
    public async Task<IActionResult> Record(int id, string? bankRef)
    {
        try
        {
            var payment = await _accounting.RecordRefundAsync(id, await CurrentEmployeeIdAsync(), bankRef ?? string.Empty);
            TempData["Success"] = _localizer["Đã ghi nhận hoàn {0:N0}đ và báo cho khách.", payment.Amount].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        return RedirectToAction(nameof(Index));
    }
}
