using System.Security.Claims;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Owner.Controllers;

[Area(AppRoles.Owner), Authorize(Policy = AppPolicies.ApprovedOwner)]
public class ScheduleController(OwnerScheduleService schedules, TimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(string period = "today", int? homestayId = null, DateOnly? date = null)
    {
        if (!new[] { "today", "tomorrow", "week" }.Contains(period)) return BadRequest();
        if (!ModelState.IsValid || date is { Year: < 1900 or > 2100 }) return BadRequest();
        var today = VietnamTime.Today(clock);
        var from = date ?? (period == "tomorrow" ? today.AddDays(1) : today);
        var through = period == "week" ? from.AddDays(6) : from;
        try {
            var data = await schedules.ReadAsync(new Actor(ActorType.Owner,User.FindFirstValue(ClaimTypes.NameIdentifier)),from,through,homestayId);
            return View(new OwnerScheduleModel(data,period,from,through));
        } catch (BookingException) { return NotFound(); }
    }
}
