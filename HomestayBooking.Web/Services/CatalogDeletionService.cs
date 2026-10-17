using System.Security.Claims;
using System.Globalization;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public enum CatalogKind { Area, RoomCategory, Amenity, Holiday }

/// <summary>FR-21: kiểm tra đang dùng và dịch cả lỗi FK do tranh chấp; không xóa dây chuyền tiện nghi đã dùng.</summary>
public class CatalogDeletionService(ApplicationDbContext db, TimeProvider clock)
{
    public async Task DeleteAsync(CatalogKind kind, object id, ClaimsPrincipal principal)
    {
        var account = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || account is null) throw new BookingException("Vui lòng đăng nhập.");
        var employee = await db.Employees.Where(e => e.Person.Account != null && e.Person.Account.Id == account).Select(e => (int?)e.Id).SingleOrDefaultAsync();
        await DeleteAsync(kind, id, new Actor(ActorType.Employee, account, employee));
    }

    public async Task DeleteAsync(CatalogKind kind, object id, Actor actor)
    {
        try
        {
            await HomestayWrite.RunAsync(db, async () =>
            {
                await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
                if (kind == CatalogKind.Holiday ? id is not DateOnly : id is not int) throw new BookingException("Mã danh mục không hợp lệ.");
                object entity; string table;
                switch (kind)
                {
                    case CatalogKind.Area:
                        if (db.Database.IsRelational()) await db.Areas.Where(a => a.Id == (int)id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, a => a.Name));
                        entity = await db.Areas.FindAsync(id) ?? throw new BookingException("Không tìm thấy khu vực."); table = "Areas";
                        if (await db.Homestays.AnyAsync(h => h.AreaId == (int)id)) throw InUse();
                        break;
                    case CatalogKind.RoomCategory:
                        if (db.Database.IsRelational()) await db.RoomCategories.Where(a => a.Id == (int)id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, a => a.Name));
                        entity = await db.RoomCategories.FindAsync(id) ?? throw new BookingException("Không tìm thấy danh mục loại phòng."); table = "RoomCategories";
                        if (await db.RoomTypes.AnyAsync(t => t.CategoryId == (int)id)) throw InUse();
                        break;
                    case CatalogKind.Amenity:
                        if (db.Database.IsRelational()) await db.Amenities.Where(a => a.Id == (int)id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, a => a.Name));
                        entity = await db.Amenities.FindAsync(id) ?? throw new BookingException("Không tìm thấy tiện nghi."); table = "Amenities";
                        // Quan hệ này cascade nên phải chủ động chặn thay vì chờ FK.
                        if (await db.HomestayAmenities.AnyAsync(h => h.AmenityId == (int)id)) throw InUse();
                        break;
                    case CatalogKind.Holiday:
                        if (db.Database.IsRelational()) await db.Holidays.Where(a => a.Date == (DateOnly)id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, a => a.Name));
                        entity = await db.Holidays.FindAsync(id) ?? throw new BookingException("Không tìm thấy ngày lễ."); table = "Holidays";
                        // Ngày lễ không có FK; BookingNights đã lưu snapshot giá/DayType của đơn cũ.
                        break;
                    default: throw new BookingException("Danh mục không hợp lệ.");
                }
                // Controller có thể đã đọc entity trước transaction; audit phải lấy giá trị thực dưới khóa.
                await db.Entry(entity).ReloadAsync();
                if (db.Entry(entity).State == EntityState.Detached) throw new BookingException("Danh mục vừa được người khác xóa, vui lòng tải lại.");
                var old = db.Entry(entity).Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                db.Remove(entity);
                var recordId = id is DateOnly date ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : id.ToString()!;
                PartnerServiceSupport.Audit(db, clock, actor, table, recordId, old, new { Deleted = true }, AuditAction.Delete);
                return true;
            });
        }
        catch (DbUpdateConcurrencyException) { throw new BookingException("Danh mục vừa được người khác xử lý, vui lòng tải lại."); }
        catch (DbUpdateException e) when (e.GetBaseException().Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 }) { throw InUse(); }
    }
    private static BookingException InUse() => new("Không thể xóa danh mục vì đang được sử dụng.");
}
