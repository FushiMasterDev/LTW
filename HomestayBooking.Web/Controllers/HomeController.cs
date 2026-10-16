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
    private const int FeaturedCount = 6;

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public HomeController(ApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
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
            Featured = await FeaturedAsync(),
        };
        return View(model);
    }

    /// <summary>Homestay đang nhận đặt, điểm đánh giá cao trước; cùng điều kiện "đặt được" với trang tìm kiếm.</summary>
    private async Task<List<FeaturedHomestay>> FeaturedAsync()
    {
        var today = VietnamTime.Today(_clock);
        var rows = await _db.Homestays
            .Where(h => h.Status == HomestayStatus.Active
                        && _db.Contracts.Covering(today, today).Any(c => c.HomestayId == h.Id)
                        && h.RoomTypes.Any(t => t.IsActive))
            .Select(h => new
            {
                h.Id,
                h.Name,
                AreaName = h.Area.Name,
                Cover = h.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault(),
                Rating = _db.Reviews.Where(r => r.HomestayId == h.Id && !r.IsHidden).Average(r => (double?)r.Rating),
                ReviewCount = _db.Reviews.Count(r => r.HomestayId == h.Id && !r.IsHidden),
                RoomTypeIds = h.RoomTypes.Where(t => t.IsActive).Select(t => t.Id).ToList(),
            })
            .OrderByDescending(h => h.Rating ?? 0)
            .ThenByDescending(h => h.ReviewCount)
            .ThenBy(h => h.Name)
            .Take(FeaturedCount)
            .ToListAsync();

        var roomTypeIds = rows.SelectMany(r => r.RoomTypeIds).ToList();
        var prices = PricingService.CurrentPrices(
            await _db.RoomTypePrices.AsNoTracking().Where(p => roomTypeIds.Contains(p.RoomTypeId)).ToListAsync(), today);
        return rows
            .Select(r => new FeaturedHomestay(r.Id, r.Name, r.AreaName, r.Cover,
                r.RoomTypeIds.Select(id => prices.TryGetValue((id, DayType.Weekday), out var p) ? p : (decimal?)null).Min(),
                r.Rating, r.ReviewCount))
            .ToList();
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
