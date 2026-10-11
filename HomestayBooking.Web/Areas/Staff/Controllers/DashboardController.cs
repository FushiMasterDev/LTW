using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Policy = AppPolicies.Employee)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var counts = await _db.Bookings.GroupBy(b => b.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        ViewBag.ActiveHomestayCount = await _db.Homestays.CountAsync(h => h.Status == HomestayStatus.Active);
        return View(Enum.GetValues<BookingStatus>().ToDictionary(s => s, s => counts.GetValueOrDefault(s)));
    }
}
