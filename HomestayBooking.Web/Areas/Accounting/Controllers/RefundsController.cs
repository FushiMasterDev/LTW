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


        ViewData["Done"] = done;
        ViewData["Query"] = q;
        await LoadCountsAsync(_accounting);
        return View(await _accounting.RefundsAsync(done, q, page, await CurrentActorAsync()));
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
