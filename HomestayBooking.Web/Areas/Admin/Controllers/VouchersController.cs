using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

/// <summary>Quản lý cấp và thu hồi voucher xin lỗi khách (Q4, D14).</summary>
public class VouchersController : AdminController
{
    private readonly ApplicationDbContext _db;
    private readonly VoucherService _vouchers;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public VouchersController(ApplicationDbContext db, VoucherService vouchers, IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _vouchers = vouchers;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(VoucherStatus? status, string? q, int? page)
    {
        var query = _db.Vouchers.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(v => v.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(v => v.Code.Contains(term) || v.Customer.Person.FullName!.Contains(term)
                                     || (phone != null && v.Customer.Person.PhoneNumber == phone));
        }

        ViewData["Query"] = q;
        ViewData["Status"] = status;
        var rows = query
            .OrderByDescending(v => v.IssuedAt)
            .Select(v => new VoucherRow(v.Id, v.Code, v.Customer.Person.FullName ?? "", v.Customer.Person.PhoneNumber, v.Amount, v.Balance,
                v.ExpiresOn, v.Status, v.Reason, v.SourceBooking == null ? null : v.SourceBooking.Code,
                v.IssuedByEmployee.Person.FullName ?? v.IssuedByEmployee.EmployeeCode, v.IssuedAt,
                // Đơn hủy / bị từ chối / hết hạn đã được trả lại số tiền giảm nên không tính là đã dùng
                v.UsedByBookings.Count(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected
                                            && b.Status != BookingStatus.Expired)));
        return View(await PaginatedList<VoucherRow>.CreateAsync(rows, page));
    }

    public IActionResult Create(string? bookingCode) => View(new VoucherIssueViewModel { BookingCode = bookingCode ?? string.Empty });

    [HttpPost]
    public async Task<IActionResult> Create(VoucherIssueViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var employeeId = await CurrentEmployeeIdAsync();
        try
        {
            var voucher = await _vouchers.IssueAsync(model.BookingCode, employeeId, model.Reason);
            TempData["Success"] = _localizer["Đã cấp voucher {0} trị giá {1:N0}đ.", voucher.Code, voucher.Amount].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError(string.Empty, _localizer[ex.Message]);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Revoke(int id, string? reason)
    {
        try
        {
            await _vouchers.RevokeAsync(id, await CurrentEmployeeIdAsync(), reason ?? string.Empty);
            TempData["Success"] = _localizer["Đã thu hồi voucher."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<int> CurrentEmployeeIdAsync()
    {
        var personId = User.PersonId();
        return _db.Employees.Where(e => e.PersonId == personId).Select(e => e.Id).SingleAsync();
    }
}
