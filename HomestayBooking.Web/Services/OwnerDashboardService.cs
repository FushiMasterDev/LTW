using HomestayBooking.Web.Areas.Owner.Controllers;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Tổng quan dùng chung, giữ cả khả năng xem trạng thái hồ sơ chưa duyệt trên web.</summary>
public class OwnerDashboardService(ApplicationDbContext db, OwnerScheduleService schedule, TimeProvider clock)
{
    public async Task<OwnerDashboardViewModel?> ReadAsync(int? personId, bool approved, Actor actor)
    {
        // Chủ homestay chỉ thấy dữ liệu của mình (NFR-04)
        var owner = await db.Owners.AsNoTracking()
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
            return null;
        }

        var today = VietnamTime.Today(clock);
        var summary = approved
            ? await schedule.OwnerBookingSummaryAsync(actor) : null;
        return new OwnerDashboardViewModel(
            owner.Registration?.Status, owner.Registration?.ReviewNote, owner.Homestays)
        {
            ArrivalsToday = summary?.ArrivalsToday, PendingConfirmation = summary?.PendingConfirmation, OverdueConfirmation = summary?.OverdueConfirmation
        };
    }
}
