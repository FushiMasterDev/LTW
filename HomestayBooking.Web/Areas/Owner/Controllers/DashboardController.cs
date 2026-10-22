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
    private readonly OwnerDashboardService _dashboard;
    public DashboardController(OwnerDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index()
    {
        var model = await _dashboard.ReadAsync(User.PersonId(), User.IsApprovedOwner(),
            new Actor(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier)));
        return model is null ? Forbid() : View(model);
    }

}

public record OwnerHomestayRow(int Id, string Name, string AreaName, int RoomCount, HomestayStatus Status);

public record OwnerDashboardViewModel(
    OwnerRegistrationStatus? RegistrationStatus, string? ReviewNote, List<OwnerHomestayRow> Homestays)
{
    public int? ArrivalsToday { get; init; }
    public int? PendingConfirmation { get; init; }
    public int? OverdueConfirmation { get; init; }
}
