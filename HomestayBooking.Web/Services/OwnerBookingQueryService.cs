using HomestayBooking.Web.Areas.Owner.Controllers;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Truy vấn đơn của chủ dùng chung MVC và desktop; chỉ hiện đơn đã cọc.</summary>
public class OwnerBookingQueryService(ApplicationDbContext db, BookingService bookings, TimeProvider clock, ChangeRequestUiService changeUi)
{
    private IQueryable<Booking> OwnBookings(int? personId)
    {
        return db.Bookings.Where(b => b.Homestay.Owner.PersonId == personId && b.DepositedAt != null);
    }

    public Task<Booking?> FindAsync(int id, int? personId) => OwnBookings(personId).SingleOrDefaultAsync(b => b.Id == id);

    public async Task<PaginatedList<OwnerBookingRow>> SearchAsync(int? personId, BookingStatus? status, string? q, int? page)
    {
        var query = OwnBookings(personId).AsNoTracking();
        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(b => b.Code.Contains(term) || b.ContactName.Contains(term) || (phone != null && b.ContactPhone == phone));
        }

        var rows = query
            // Chờ xác nhận → sắp ở / đang ở (CheckIn gần nhất trước) → đã đóng (CheckIn gần đây trước)
            .OrderBy(b => b.Status == BookingStatus.Deposited ? 0
                : b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn ? 1 : 2)
            .ThenBy(b => b.Status == BookingStatus.Deposited || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn
                ? b.CheckIn : DateOnly.MaxValue)
            .ThenByDescending(b => b.CheckIn)
            .Select(b => new OwnerBookingRow(
                b.Id, b.Code, b.Status, b.ContactName, b.ContactPhone, b.Homestay.Name, b.RoomType.Name, b.Room.RoomCode,
                b.CheckIn, b.CheckOut, b.Guests, b.TotalAmount, b.ConfirmDeadlineAt));

        return await PaginatedList<OwnerBookingRow>.CreateAsync(rows, page);
    }

    public async Task<OwnerBookingDetailsViewModel?> DetailsAsync(int id, int? personId)
    {
        var booking = await OwnBookings(personId).AsNoTracking()
            .Include(b => b.Homestay)
            .Include(b => b.RoomType)
            .Include(b => b.Room)
            .Include(b => b.Nights)
            .Include(b => b.Histories)
            .SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null)
        {
            return null;
        }

        // Phòng để đổi khi xác nhận (Q8): cùng tiêu chí "trống" với lúc đặt
        var freeIds = booking.Status == BookingStatus.Deposited
            ? await bookings.FreeRoomIdsAsync(booking.RoomTypeId, booking.CheckIn, booking.CheckOut)
            : [];
        var freeRooms = await db.Rooms.AsNoTracking()
            .Where(r => freeIds.Contains(r.Id))
            .OrderBy(r => r.RoomCode)
            .Select(r => new OwnerRoomOption(r.Id, r.RoomCode))
            .ToListAsync();
        var history = await changeUi.HistoryAsync([booking.Id]);
        return new OwnerBookingDetailsViewModel(booking, freeRooms, VietnamTime.Today(clock)) { ChangeHistory = history };
    }

}
