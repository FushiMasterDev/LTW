using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers;

public class HomeController : Controller
{

    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;

    public HomeController(ApplicationDbContext db, TimeProvider clock, AvailabilityService? availability = null)
    {
        _db = db;
        _availability = availability ?? new AvailabilityService(db, clock, Microsoft.Extensions.Options.Options.Create(new BookingOptions()));
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            HomestayCount = await _db.Homestays.CountAsync(h => h.Status == HomestayStatus.Active),
            RoomCount = await _db.Rooms.CountAsync(r =>
                r.IsActive && r.RoomType.IsActive && r.RoomType.Homestay.Status == HomestayStatus.Active),
            Areas = await _db.Areas
                .OrderBy(a => a.Name)
                .Select(a => new AreaSummary(a.Id, a.Name, a.Homestays.Count(h => h.Status == HomestayStatus.Active)))
                .ToListAsync(),
            Featured = await _availability.FeaturedAsync(),
        };
        return View(model);
    }

    /// <summary>
    /// Trang lỗi cho 403/404/... (UseStatusCodePagesWithReExecute). Re-execute giữ nguyên method gốc nên phải nhận
    /// mọi method (POST lỗi sẽ thành 405 nếu chỉ nhận GET); chỉ hiển thị, không đổi dữ liệu nên bỏ qua antiforgery.
    /// </summary>
    [Route("Home/StatusCode/{code:int}")]
    [IgnoreAntiforgeryToken]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult StatusCodePage(int code)
    {
        return View("StatusCode", code);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
