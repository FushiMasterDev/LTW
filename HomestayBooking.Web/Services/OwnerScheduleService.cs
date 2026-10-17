using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record OwnerScheduleItem(int BookingId, string Code, int HomestayId, string HomestayName, DateOnly CheckIn, DateOnly CheckOut,
    string ContactName, string ContactPhone, int Guests, string RoomCode, string? SpecialRequests);
public record OwnerSchedule(IReadOnlyList<OwnerScheduleItem> Arrivals, IReadOnlyList<OwnerScheduleItem> Departures);

/// <summary>FR-16: khoảng ngày lịch bao gồm hai đầu, tối đa 31 ngày; khác khoảng đêm lưu trú.</summary>
public class OwnerScheduleService(ApplicationDbContext db, TimeProvider clock)
{
    public Task<OwnerSchedule> ReadAsync(int ownerId, Actor actor) =>
        ReadAsync(ownerId, VietnamTime.Today(clock), VietnamTime.Today(clock).AddDays(30), actor);

    public async Task<OwnerSchedule> ReadAsync(int ownerId, DateOnly from, DateOnly through, Actor actor, int? homestayId = null)
    {
        if (through < from || through.DayNumber - from.DayNumber + 1 > 31) throw new BookingException("Khoảng lịch nhận–trả phải từ 1 đến 31 ngày.");
        if (actor.Type == ActorType.Employee) await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        else if (actor.Type != ActorType.Owner || actor.AccountId is null || !await db.Owners.AnyAsync(o => o.Id == ownerId
            && o.Person.Account != null && o.Person.Account.Id == actor.AccountId && o.Person.Account.Status == AccountStatus.Active && o.Person.Employee == null))
            throw new BookingException("Bạn không có quyền xem lịch nhận–trả của chủ này.");
        if (homestayId is int home && !await db.Homestays.AnyAsync(h => h.Id == home && h.OwnerId == ownerId))
            throw new BookingException("Homestay không thuộc chủ đang xem lịch.");
        var bookings = await db.Bookings.AsNoTracking().Where(b => b.Homestay.OwnerId == ownerId && (homestayId == null || b.HomestayId == homestayId)
            && ((b.Status == BookingStatus.Confirmed && b.CheckIn >= from && b.CheckIn <= through)
                || (b.Status == BookingStatus.CheckedIn && b.CheckOut >= from && b.CheckOut <= through)))
            .OrderBy(b => b.CheckIn).ThenBy(b => b.Code)
            .Select(b => new { b.Status, Item = new OwnerScheduleItem(b.Id, b.Code, b.HomestayId, b.Homestay.Name, b.CheckIn, b.CheckOut,
                b.ContactName, b.ContactPhone, b.Guests, b.Room.RoomCode, b.SpecialRequests) }).ToListAsync();
        return new(bookings.Where(b => b.Status == BookingStatus.Confirmed).Select(b => b.Item).ToList(),
            bookings.Where(b => b.Status == BookingStatus.CheckedIn).Select(b => b.Item).OrderBy(b => b.CheckOut).ThenBy(b => b.Code).ToList());
    }
}
