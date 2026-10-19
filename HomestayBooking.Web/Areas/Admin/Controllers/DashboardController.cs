using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class DashboardController(ApplicationDbContext db, ReportService reports, ContractService contracts,
    HomestayReviewService reviews, OwnerRegistrationQueryService registrations, TimeProvider clock,
    IStringLocalizer<SharedResource> localizer) : AdminServiceController(db, localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = VietnamTime.Today(clock);
        var query = new ReportQuery(new(today.Year, today.Month, 1), today);
        var actor = await CurrentActorAsync();
        var report = await reports.ManagementAsync(query, actor);
        var expiring = await contracts.ListAsync(new(ExpiringWithinDays: 30), actor);
        var pending = await reviews.PendingAsync(actor);
        var submitted = await registrations.ListAsync(new(Status: OwnerRegistrationStatus.PendingReview), actor);
        var reviewing = await registrations.ListAsync(new(Status: OwnerRegistrationStatus.UnderReview), actor);
        return View(new AdminDashboardModel(query, report, expiring, pending.Count, submitted.TotalCount + reviewing.TotalCount));
    }
}
