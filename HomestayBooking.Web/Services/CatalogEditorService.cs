using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Validation của danh mục, gồm cả tranh chấp unique; MVC giữ lỗi theo trường, desktop trả 409.</summary>
public sealed class CatalogConflictException(string field, string message) : BookingException(message)
{
    public string Field { get; } = field;
}

/// <summary>Logic thêm/sửa chuyển nguyên từ bốn controller Admin; không thay đổi quy tắc hay audit xóa.</summary>
public sealed class CatalogEditorService(ApplicationDbContext db)
{
    public Task<bool> AreaExistsAsync(int id, string? name, string? province) =>
        string.IsNullOrEmpty(name?.Trim()) || string.IsNullOrEmpty(province?.Trim()) ? Task.FromResult(false)
        : db.Areas.AnyAsync(a => a.Name == name.Trim() && a.Province == province.Trim() && a.Id != id);
    public Task<bool> AmenityExistsAsync(int id, string? name) => string.IsNullOrEmpty(name?.Trim()) ? Task.FromResult(false)
        : db.Amenities.AnyAsync(a => a.Name == name.Trim() && a.Id != id);
    public Task<bool> CategoryExistsAsync(int id, string? name) => string.IsNullOrEmpty(name?.Trim()) ? Task.FromResult(false)
        : db.RoomCategories.AnyAsync(a => a.Name == name.Trim() && a.Id != id);
    public Task<bool> HolidayExistsAsync(DateOnly date) => db.Holidays.AnyAsync(h => h.Date == date);

    public async Task<Area> SaveAreaAsync(int id, string name, string province, AreaGroup group)
    {
        var area = id == 0 ? new Area() : await db.Areas.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy khu vực.");
        if (await AreaExistsAsync(id, name, province)) throw new CatalogConflictException("Name", "Khu vực này đã có trong tỉnh/thành.");
        area.Name = name.Trim(); area.Province = province.Trim(); area.AreaGroup = group;
        if (id == 0) db.Areas.Add(area);
        await SaveAsync("Name", "Khu vực này đã có trong tỉnh/thành.");
        return area;
    }

    public async Task<Amenity> SaveAmenityAsync(int id, string name, string? icon)
    {
        var amenity = id == 0 ? new Amenity() : await db.Amenities.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy tiện nghi.");
        if (await AmenityExistsAsync(id, name)) throw new CatalogConflictException("Name", "Tên này đã tồn tại.");
        amenity.Name = name.Trim(); amenity.Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        if (id == 0) db.Amenities.Add(amenity);
        await SaveAsync("Name", "Tên này đã tồn tại.");
        return amenity;
    }

    public async Task<RoomCategory> SaveCategoryAsync(int id, string name)
    {
        var category = id == 0 ? new RoomCategory() : await db.RoomCategories.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy danh mục loại phòng.");
        if (await CategoryExistsAsync(id, name)) throw new CatalogConflictException("Name", "Tên này đã tồn tại.");
        category.Name = name.Trim();
        if (id == 0) db.RoomCategories.Add(category);
        await SaveAsync("Name", "Tên này đã tồn tại.");
        return category;
    }

    public async Task<Holiday> CreateHolidayAsync(DateOnly date, string name)
    {
        if (await HolidayExistsAsync(date)) throw new CatalogConflictException("Date", "Ngày này đã có trong danh sách.");
        var holiday = new Holiday { Date = date, Name = name.Trim() };
        db.Holidays.Add(holiday);
        await SaveAsync("Date", "Ngày này đã có trong danh sách.");
        return holiday;
    }

    public async Task<Holiday> UpdateHolidayAsync(DateOnly date, string name)
    {
        var holiday = await db.Holidays.FindAsync(date) ?? throw new KeyNotFoundException("Không tìm thấy ngày lễ.");
        holiday.Name = name.Trim();
        await db.SaveChangesAsync();
        return holiday;
    }

    private async Task SaveAsync(string field, string message)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        { throw new CatalogConflictException(field, message); }
    }
}
