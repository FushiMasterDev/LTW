using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record RoomTypeInput(string Name, int CategoryId, int MaxGuests, int? AreaM2, string? Description);
public record RoomAvailabilityChange(IReadOnlyList<string> UpcomingBookingCodes);

public class RoomService(ApplicationDbContext db, HomestayAccessService access, TimeProvider clock)
{
    private static object TypeState(RoomType t) => new { t.Id, t.HomestayId, t.Name, t.CategoryId, t.MaxGuests, t.AreaM2, t.Description, t.IsActive, t.UpdatedAt };
    private static object RoomState(Room r) => new { r.Id, r.RoomTypeId, r.RoomCode, r.IsActive };
    private static object PriceState(RoomTypePrice p) => new { p.Id, p.RoomTypeId, DayType = p.DayType.ToString(), p.Price, p.EffectiveFrom };
    private async Task ValidateTypeAsync(int homeId, RoomTypeInput input, int? exceptId = null)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100 || input.MaxGuests <= 0
            || input.AreaM2 is <= 0 || input.Description?.Length > 1000) throw new BookingException("Thông tin loại phòng không hợp lệ.");
        if (!await db.RoomCategories.AnyAsync(c => c.Id == input.CategoryId)) throw new BookingException("Danh mục loại phòng không tồn tại.");
        if (await db.RoomTypes.AnyAsync(t => t.HomestayId == homeId && t.Name == input.Name.Trim() && t.Id != exceptId))
            throw new BookingException("Tên loại phòng đã có trong homestay.");
    }
    public Task<RoomType> CreateTypeAsync(int homeId, RoomTypeInput input, IReadOnlyDictionary<DayType, decimal> prices, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await access.AuthorizeAsync(homeId, actor); await ValidateTypeAsync(homeId, input);
        if (prices.Count != 3 || Enum.GetValues<DayType>().Any(t => !prices.TryGetValue(t, out var price) || price <= 0))
            throw new BookingException("Cần đủ ba giá ngày thường, cuối tuần và ngày lễ lớn hơn 0.");
        var now = clock.GetUtcNow().UtcDateTime; var today = VietnamTime.Today(clock);
        var type = new RoomType { HomestayId = homeId, Name = input.Name.Trim(), CategoryId = input.CategoryId,
            MaxGuests = input.MaxGuests, AreaM2 = input.AreaM2, Description = input.Description?.Trim(), CreatedAt = now, UpdatedAt = now };
        foreach (var pair in prices) type.Prices.Add(new() { DayType = pair.Key, Price = pair.Value, EffectiveFrom = today, CreatedAt = now });
        db.RoomTypes.Add(type); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "RoomTypes", type.Id.ToString(), null, TypeState(type), AuditAction.Insert);
        foreach (var p in type.Prices) PartnerServiceSupport.Audit(db, clock, actor, "RoomTypePrices", p.Id.ToString(), null, PriceState(p), AuditAction.Insert);
        return type;
    });
    private async Task<RoomType> TypeAsync(int id, Actor actor)
    {
        var type = await db.RoomTypes.SingleOrDefaultAsync(t => t.Id == id) ?? throw new BookingException("Không tìm thấy loại phòng.");
        await access.AuthorizeAsync(type.HomestayId, actor); return type;
    }
    public Task<RoomType> UpdateTypeAsync(int id, RoomTypeInput input, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var type = await TypeAsync(id, actor); await ValidateTypeAsync(type.HomestayId, input, id); var old = TypeState(type);
        type.Name = input.Name.Trim(); type.CategoryId = input.CategoryId; type.MaxGuests = input.MaxGuests;
        type.AreaM2 = input.AreaM2; type.Description = input.Description?.Trim(); type.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "RoomTypes", id.ToString(), old, TypeState(type)); return type;
    });
    public Task<RoomAvailabilityChange> SetTypeActiveAsync(int id, bool active, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var type = await TypeAsync(id, actor);
        if (!active) await EnsureSalesRemainAsync(type.HomestayId, exceptTypeId: id);
        var old = TypeState(type); type.IsActive = active; type.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "RoomTypes", id.ToString(), old, TypeState(type));
        return new RoomAvailabilityChange(await UpcomingAsync(id, null));
    });
    public Task<Room> AddRoomAsync(int typeId, string code, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await TypeAsync(typeId, actor); code = await ValidateCodeAsync(typeId, code);
        var room = new Room { RoomTypeId = typeId, RoomCode = code }; db.Rooms.Add(room); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Rooms", room.Id.ToString(), null, RoomState(room), AuditAction.Insert); return room;
    });
    private async Task<string> ValidateCodeAsync(int typeId, string code, int? exceptId = null)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 20) throw new BookingException("Mã phòng phải có từ 1 đến 20 ký tự.");
        if (await db.Rooms.AnyAsync(r => r.RoomTypeId == typeId && r.RoomCode == code.Trim() && r.Id != exceptId)) throw new BookingException("Mã phòng đã có trong loại phòng.");
        return code.Trim();
    }
    private async Task<Room> RoomAsync(int id, Actor actor)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(r => r.Id == id) ?? throw new BookingException("Không tìm thấy phòng.");
        await TypeAsync(room.RoomTypeId, actor); return room;
    }
    public Task<Room> RenameRoomAsync(int id, string code, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var room = await RoomAsync(id, actor); code = await ValidateCodeAsync(room.RoomTypeId, code, id); var old = RoomState(room); room.RoomCode = code;
        PartnerServiceSupport.Audit(db, clock, actor, "Rooms", id.ToString(), old, RoomState(room)); return room;
    });
    public Task<RoomAvailabilityChange> SetRoomActiveAsync(int id, bool active, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var room = await RoomAsync(id, actor);
        if (!active) await EnsureSalesRemainAsync(room.RoomType.HomestayId, exceptRoomId: id);
        var old = RoomState(room); room.IsActive = active;
        PartnerServiceSupport.Audit(db, clock, actor, "Rooms", id.ToString(), old, RoomState(room));
        return new RoomAvailabilityChange(await UpcomingAsync(room.RoomTypeId, id));
    });
    private async Task EnsureSalesRemainAsync(int homeId, int? exceptTypeId = null, int? exceptRoomId = null)
    {
        if (await db.Homestays.AnyAsync(h => h.Id == homeId && h.Status == HomestayStatus.Active)
            && !await db.Rooms.AnyAsync(r => r.RoomType.HomestayId == homeId && r.IsActive && r.RoomType.IsActive
                && (exceptTypeId == null || r.RoomTypeId != exceptTypeId) && (exceptRoomId == null || r.Id != exceptRoomId)))
            throw new BookingException("Không thể tắt phòng hoặc loại phòng cuối cùng đang bán của homestay hoạt động. Vui lòng dùng khóa ngày hoặc đề nghị Quản lý tạm ngưng homestay.");
    }
    private Task<List<string>> UpcomingAsync(int typeId, int? roomId)
    {
        var today = VietnamTime.Today(clock); var holds = Enum.GetValues<BookingStatus>().Where(s => s.HoldsRoom() && s != BookingStatus.Completed).ToArray();
        return db.Bookings.Where(b => b.RoomTypeId == typeId && (roomId == null || b.RoomId == roomId) && b.CheckIn >= today && holds.Contains(b.Status))
            .OrderBy(b => b.Code).Select(b => b.Code).ToListAsync();
    }
    public Task<RoomTypePrice> SetPriceAsync(int typeId, DayType dayType, DateOnly effectiveFrom, decimal price, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await TypeAsync(typeId, actor); var today = VietnamTime.Today(clock);
        if (!Enum.IsDefined(dayType) || price <= 0 || effectiveFrom < today) throw new BookingException("Giá phải lớn hơn 0, ngày hiệu lực không ở quá khứ.");
        var row = await db.RoomTypePrices.SingleOrDefaultAsync(p => p.RoomTypeId == typeId && p.DayType == dayType && p.EffectiveFrom == effectiveFrom);
        if (row is not null && row.EffectiveFrom <= today) throw new BookingException("Không sửa giá đã có hiệu lực; hãy thêm giá cho ngày mới.");
        var old = row is null ? null : PriceState(row); var insert = row is null;
        row ??= new RoomTypePrice { RoomTypeId = typeId, DayType = dayType, EffectiveFrom = effectiveFrom, CreatedAt = clock.GetUtcNow().UtcDateTime };
        row.Price = price; if (insert) db.RoomTypePrices.Add(row); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "RoomTypePrices", row.Id.ToString(), old, PriceState(row), insert ? AuditAction.Insert : AuditAction.Update);
        return row;
    });
}
