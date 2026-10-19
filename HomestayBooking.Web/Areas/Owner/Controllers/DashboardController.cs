using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Areas.Owner.Controllers;

/// <summary>
/// Owner chưa được duyệt vẫn vào được trang tổng quan để xem trạng thái hồ sơ (v5 mục 6.1);
/// các chức năng quản lý homestay dùng policy ApprovedOwner.
/// </summary>
[Area(AppRoles.Owner)]
[Authorize(Roles = AppRoles.Owner)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly OwnerScheduleService _schedule;
    private readonly TimeProvider _clock;

    public DashboardController(ApplicationDbContext db, OwnerScheduleService schedule, TimeProvider clock)
    {
        _db = db;
        _schedule = schedule;
        _clock = clock;
    }

    public async Task<IActionResult> Index()
    {
        // Chủ homestay chỉ thấy dữ liệu của mình (NFR-04)
        var personId = User.PersonId();
        var owner = await _db.Owners.AsNoTracking()
            .Where(o => o.PersonId == personId)
            .Select(o => new
            {
                Registration = o.Registrations
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new { r.Status, r.ReviewNote })
                    .FirstOrDefault(),
                Homestays = o.Homestays
                    .OrderBy(h => h.Name)
                    .Select(h => new OwnerHomestayRow(h.Id, h.Name, h.Area.Name, h.RoomTypes.SelectMany(t => t.Rooms).Count(), h.Status))
                    .ToList(),
            })
            .SingleOrDefaultAsync();
        if (owner is null)
        {
            return Forbid();
        }

        var today = VietnamTime.Today(_clock);
        var arrivalsToday = User.IsApprovedOwner()
            ? (int?)(await _schedule.ReadAsync(
                new Actor(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier)), today, today)).Arrivals.Count
            : null;
        return View(new OwnerDashboardViewModel(
            owner.Registration?.Status, owner.Registration?.ReviewNote, owner.Homestays)
        {
            ArrivalsToday = arrivalsToday
        });
    }
}

public record OwnerHomestayRow(int Id, string Name, string AreaName, int RoomCount, HomestayStatus Status);

public record OwnerDashboardViewModel(
    OwnerRegistrationStatus? RegistrationStatus, string? ReviewNote, List<OwnerHomestayRow> Homestays)
{
    public int? ArrivalsToday { get; init; }
}
