using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class DashboardController : AdminController
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.HomestayCount = await _db.Homestays.CountAsync();
        ViewBag.RoomCount = await _db.Rooms.CountAsync();
        ViewBag.OwnerCount = await _db.Owners.CountAsync();
        ViewBag.StaffCount = await _db.Employees.CountAsync(e => e.EmploymentStatus == EmploymentStatus.Active);
        ViewBag.PendingOwnerCount = await _db.OwnerRegistrations
            .CountAsync(r => r.Status == OwnerRegistrationStatus.PendingReview || r.Status == OwnerRegistrationStatus.UnderReview);
        ViewBag.PendingBookingCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Deposited);
        return View();
    }
}
