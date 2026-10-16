using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record RoomNightConflict(DateOnly Date, string Code);
public class RoomBlockConflictException(IReadOnlyList<RoomNightConflict> conflicts) : BookingException(
    conflicts.Count == 0 ? "Lịch phòng vừa bị chiếm, vui lòng tải lại." : "Lịch phòng bị trùng: " + string.Join("; ", conflicts.Select(c => $"{c.Date:dd/MM/yyyy}: {c.Code}")))
{
    public IReadOnlyList<RoomNightConflict> Conflicts { get; } = conflicts;
}
public enum CalendarNightState { Free, Booked, Blocked, ChangeRequestHeld }
public record CalendarNight(int RoomId, string RoomCode, DateOnly Date, CalendarNightState State,
    string? Code = null, BookingStatus? BookingStatus = null, RoomBlockReason? BlockReason = null);

public class RoomBlockService(ApplicationDbContext db, HomestayAccessService access, TimeProvider clock)
{
    private static object State(RoomBlock b) => new { b.Id, b.RoomId, b.StartDate, b.EndDate, Reason = b.Reason.ToString(), b.Note, b.CreatedByAccountId };
    private async Task<Room> RoomAsync(int id, Actor actor)
    {
        var room = await db.Rooms.Include(r => r.RoomType).SingleOrDefaultAsync(r => r.Id == id) ?? throw new BookingException("Không tìm thấy phòng.");
        await access.AuthorizeAsync(room.RoomType.HomestayId, actor, allowConsultant: false); return room;
    }
    private Task<List<RoomNight>> OccupiedAsync(int roomId, DateOnly start, DateOnly end) => db.RoomNights.AsNoTracking()
        .Include(n => n.Booking).Include(n => n.RoomBlock).Include(n => n.BookingChangeRequest).ThenInclude(r => r!.Booking)
        .Where(n => n.RoomId == roomId && n.Date >= start && n.Date < end).OrderBy(n => n.Date).ToListAsync();
    private static List<RoomNightConflict> Conflicts(IEnumerable<RoomNight> nights) => nights.Select(n => new RoomNightConflict(n.Date,
        n.Booking?.Code ?? (n.RoomBlockId is int block ? $"BL{block}" : $"CR{n.BookingChangeRequestId} {n.BookingChangeRequest?.Booking.Code}"))).ToList();
    public async Task<RoomBlock> CreateAsync(int roomId, DateOnly start, DateOnly end, RoomBlockReason reason, string? note, Actor actor)
    {
        try
        {
            return await HomestayWrite.RunAsync(db, async () =>
            {
                await RoomAsync(roomId, actor);
                if (start < VietnamTime.Today(clock) || end <= start || !Enum.IsDefined(reason) || note?.Length > 255)
                    throw new BookingException("Khoảng khóa, lý do hoặc ghi chú không hợp lệ; không khóa ngày quá khứ.");
                var conflicts = Conflicts(await OccupiedAsync(roomId, start, end));
                if (conflicts.Count > 0) throw new RoomBlockConflictException(conflicts);
                var block = new RoomBlock { RoomId = roomId, StartDate = start, EndDate = end, Reason = reason,
                    Note = note?.Trim(), CreatedByAccountId = actor.AccountId!, CreatedAt = clock.GetUtcNow().UtcDateTime };
                for (var date = start; date < end; date = date.AddDays(1)) block.Nights.Add(new() { RoomId = roomId, Date = date });
                db.RoomBlocks.Add(block); await db.SaveChangesAsync();
                PartnerServiceSupport.Audit(db, clock, actor, "RoomBlocks", block.Id.ToString(), null, State(block), AuditAction.Insert);
                return block;
            });
        }
        catch (DbUpdateException ex) when (ex.GetBaseException().Message.Contains("PK_RoomNights")
            || ex.GetBaseException().Message.Contains("UNIQUE constraint failed: RoomNights.RoomId, RoomNights.Date"))
        {
            throw new RoomBlockConflictException(Conflicts(await OccupiedAsync(roomId, start, end)));
        }
    }
    public Task<int> UnlockAsync(int id, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var block = await db.RoomBlocks.SingleOrDefaultAsync(b => b.Id == id) ?? throw new BookingException("Không tìm thấy khóa ngày.");
        await RoomAsync(block.RoomId, actor); var today = VietnamTime.Today(clock);
        if (block.EndDate <= today) throw new BookingException("Khóa ngày đã kết thúc, không cần mở khóa.");
        var old = State(block);
        if (block.StartDate >= today)
        {
            db.RoomBlocks.Remove(block);
            PartnerServiceSupport.Audit(db, clock, actor, "RoomBlocks", id.ToString(), old, new { Deleted = true }, AuditAction.Delete);
        }
        else
        {
            block.EndDate = today;
            db.RoomNights.RemoveRange(await db.RoomNights.Where(n => n.RoomBlockId == id && n.Date >= today).ToListAsync());
            PartnerServiceSupport.Audit(db, clock, actor, "RoomBlocks", id.ToString(), old, State(block));
        }
        return id;
    });
    public async Task<List<CalendarNight>> CalendarAsync(int homeId, DateOnly start, DateOnly end, Actor actor)
    {
        await access.AuthorizeAsync(homeId, actor, allowConsultant: false, readOnly: true);
        if (end <= start || end.DayNumber - start.DayNumber > 62) throw new BookingException("Khoảng lịch phòng phải từ 1 đến 62 ngày.");
        var rooms = await db.Rooms.AsNoTracking().Where(r => r.RoomType.HomestayId == homeId).OrderBy(r => r.Id).Select(r => new { r.Id, r.RoomCode }).ToListAsync();
        var ids = rooms.Select(r => r.Id).ToArray();
        // Chỉ một truy vấn RoomNights cho toàn bộ phòng và khoảng ngày, không truy vấn theo từng ô.
        var nights = await db.RoomNights.AsNoTracking().Include(n => n.Booking).Include(n => n.RoomBlock).Include(n => n.BookingChangeRequest)
            .Where(n => ids.Contains(n.RoomId) && n.Date >= start && n.Date < end).ToListAsync();
        var occupied = nights.ToDictionary(n => (n.RoomId, n.Date)); var result = new List<CalendarNight>();
        foreach (var room in rooms)
        for (var date = start; date < end; date = date.AddDays(1))
        {
            occupied.TryGetValue((room.Id, date), out var n);
            result.Add(n?.Booking is { } b ? new(room.Id, room.RoomCode, date, CalendarNightState.Booked, b.Code, b.Status)
                : n?.RoomBlock is { } block ? new(room.Id, room.RoomCode, date, CalendarNightState.Blocked, $"BL{block.Id}", BlockReason: block.Reason)
                : n?.BookingChangeRequestId is int request ? new(room.Id, room.RoomCode, date, CalendarNightState.ChangeRequestHeld, $"CR{request}")
                : new(room.Id, room.RoomCode, date, CalendarNightState.Free));
        }
        return result;
    }
}
