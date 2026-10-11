using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
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

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
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

        return View(new OwnerDashboardViewModel(
            owner.Registration?.Status, owner.Registration?.ReviewNote, owner.Homestays));
    }
}

public record OwnerHomestayRow(int Id, string Name, string AreaName, int RoomCount, HomestayStatus Status);

public record OwnerDashboardViewModel(
    OwnerRegistrationStatus? RegistrationStatus, string? ReviewNote, List<OwnerHomestayRow> Homestays);
