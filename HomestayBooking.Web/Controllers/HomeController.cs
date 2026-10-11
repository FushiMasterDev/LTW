using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
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
                .Select(a => new AreaSummary(a.Name, a.Homestays.Count(h => h.Status == HomestayStatus.Active)))
                .ToListAsync(),
        };
        return View(model);
    }

    /// <summary>Trang lỗi cho 403/404/... (UseStatusCodePagesWithReExecute).</summary>
    [HttpGet("Home/StatusCode/{code:int}")]
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
