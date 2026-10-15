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

namespace HomestayBooking.Web.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Policy = AppPolicies.ChangeRequestApprove)]
public class ChangeApprovalsController(ApplicationDbContext db, BookingChangeRequestService changes,
    ChangeRequestUiService ui, IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index(int? page)
    {
        var requests = await PaginatedList<BookingChangeRequest>.CreateAsync(db.BookingChangeRequests.AsNoTracking()
            .Where(r => r.Status == ChangeRequestStatus.Pending).OrderBy(r => r.CreatedAt).ThenBy(r => r.Id), page);
        var rows = await ui.HistoryAsync(requests.Select(r => r.BookingId).ToArray(), requests.Select(r => r.Id).ToArray());
        return View(new PaginatedList<ChangeRequestHistoryRow>(rows.OrderBy(r => r.Request.CreatedAt).ThenBy(r => r.Request.Id),
            requests.TotalCount, requests.PageIndex, requests.PageSize));
    }

    [HttpPost]
    public Task<IActionResult> Approve(int id, ChangeRequestDecisionForm form) => DecideAsync(id, form, true);
    [HttpPost]
    public Task<IActionResult> Reject(int id, ChangeRequestDecisionForm form) => DecideAsync(id, form, false);

    private async Task<IActionResult> DecideAsync(int id, ChangeRequestDecisionForm form, bool approve)
    {
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Vui lòng nhập ghi chú/lý do có nội dung, tối đa 500 ký tự.");
            var request = approve ? await changes.ApproveAsync(id, form.Note, User) : await changes.RejectAsync(id, form.Note, User);
            TempData[request.Status == ChangeRequestStatus.Rejected && approve ? "Error" : "Success"] = approve && request.Status == ChangeRequestStatus.Rejected
                ? localizer[request.RejectionReason!].Value : localizer["Đã xử lý yêu cầu."].Value;
        }
        catch (BookingException ex) { TempData["Error"] = localizer[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index));
    }
}
