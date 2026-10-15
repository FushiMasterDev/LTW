using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

[Authorize(Policy = AppPolicies.ChangeRequestFinance)]
public class AdditionalDepositsController(ApplicationDbContext db, AccountingService accounting,
    BookingChangeRequestService changes, IStringLocalizer<SharedResource> localizer) : AccountingController(db)
{
    public async Task<IActionResult> Index(int? page)
    {
        await LoadCountsAsync(accounting);
        var query = accounting.PendingAdditionalDeposits().AsNoTracking().Include(p => p.Booking).ThenInclude(b => b.Homestay)
            .Include(p => p.BankTransaction).Include(p => p.ChangeRequest);
        return View(await PaginatedList<Payment>.CreateAsync(query, page));
    }

    [HttpPost]
    public Task<IActionResult> Confirm(int id, ChangeRequestDecisionForm form) => HandleAsync(id, form, true);
    [HttpPost]
    public Task<IActionResult> Reject(int id, ChangeRequestDecisionForm form) => HandleAsync(id, form, false);

    private async Task<IActionResult> HandleAsync(int id, ChangeRequestDecisionForm form, bool confirm)
    {
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Vui lòng nhập ghi chú/lý do có nội dung, tối đa 500 ký tự.");
            var request = confirm ? await changes.ConfirmAsync(id, form.Note, User) : await changes.RejectFinanceAsync(id, form.Note, User);
            TempData[confirm && request.Status == ChangeRequestStatus.Rejected ? "Error" : "Success"] = confirm && request.Status == ChangeRequestStatus.Rejected
                ? localizer[request.RejectionReason!].Value : localizer["Đã xử lý yêu cầu."].Value;
        }
        catch (BookingException ex) { TempData["Error"] = localizer[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index));
    }
}
