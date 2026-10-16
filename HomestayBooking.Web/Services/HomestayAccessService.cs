using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Một chốt quyền dùng chung cho nội dung, phòng/giá, ảnh, giấy tờ và chính sách riêng.</summary>
public class HomestayAccessService(ApplicationDbContext db, OwnerRegistrationService registrations)
{
    public async Task<Homestay> AuthorizeAsync(int id, Actor actor, bool allowConsultant = true, bool readOnly = false)
    {
        var home = await db.Homestays.SingleOrDefaultAsync(h => h.Id == id)
            ?? throw new BookingException("Không tìm thấy homestay.");
        if (!readOnly && home.Status == HomestayStatus.Ended) throw new BookingException("Homestay đã kết thúc hợp tác.");
        var registration = await db.OwnerRegistrations.Include(r => r.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(r => r.OwnerId == home.OwnerId && OwnerRegistration.OpenStatuses.Contains(r.Status));
        if (!readOnly && registration is not null) OwnerRegistrationService.EnsureDraftEditable(registration, home);
        if (actor.Type == ActorType.Employee)
        {
            var employee = await PartnerServiceSupport.EmployeeAsync(db, actor,
                allowConsultant ? [EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator]
                    : [EmployeePosition.Manager, EmployeePosition.Administrator]);
            if (employee.Position == EmployeePosition.Consultant)
            {
                if (home.Status is not (HomestayStatus.Draft or HomestayStatus.PendingReview))
                    throw new BookingException("Tư vấn chỉ được sửa homestay nháp hoặc chờ duyệt trước khi chủ kích hoạt.");
                if (registration is null) throw new BookingException("Không có hồ sơ tạo thay còn mở để sửa.");
                await registrations.AuthorizeEditorAsync(registration, actor);
            }
        }
        else if (actor.Type != ActorType.Owner || actor.AccountId is null || !await db.Owners.AnyAsync(o => o.Id == home.OwnerId
            && o.Person.Account != null && o.Person.Account.Id == actor.AccountId && o.Person.Account.Status == AccountStatus.Active
            && o.Person.Employee == null))
            throw new BookingException("Bạn không có quyền quản lý homestay này.");
        return home;
    }
}

/// <summary>Transaction mặc định và thông báo lỗi trùng của các thao tác B2, không đổi schema.</summary>
internal static class HomestayWrite
{
    internal static async Task<T> RunAsync<T>(ApplicationDbContext db, Func<Task<T>> action)
    {
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        try
        {
            var result = await action(); await db.SaveChangesAsync();
            if (tx is not null) await tx.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            if (tx is not null) await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            if (ex is DbUpdateException)
            {
                var message = ex.GetBaseException().Message;
                foreach (var (key, vietnamese) in new[] {
                    ("RoomTypes.HomestayId, RoomTypes.Name", "Tên loại phòng đã có trong homestay."),
                    ("IX_RoomTypes_HomestayId_Name", "Tên loại phòng đã có trong homestay."),
                    ("Rooms.RoomTypeId, Rooms.RoomCode", "Mã phòng đã có trong loại phòng."),
                    ("IX_Rooms_RoomTypeId_RoomCode", "Mã phòng đã có trong loại phòng."),
                    ("IX_HomestayImages_Cover", "Ảnh bìa vừa được thay đổi, vui lòng tải lại."),
                    ("HomestayImages.HomestayId", "Ảnh bìa vừa được thay đổi, vui lòng tải lại."),
                    ("IX_CancellationPolicies_Active", "Chính sách hủy vừa được thay đổi, vui lòng tải lại."),
                    ("CancellationPolicies.HomestayId, CancellationPolicies.Scope", "Chính sách hủy vừa được thay đổi, vui lòng tải lại."),
                    ("IX_RoomTypePrices_RoomTypeId_DayType_EffectiveFrom", "Giá ngày hiệu lực này vừa được thêm, vui lòng tải lại."),
                    ("RoomTypePrices.RoomTypeId, RoomTypePrices.DayType, RoomTypePrices.EffectiveFrom", "Giá ngày hiệu lực này vừa được thêm, vui lòng tải lại.") })
                    if (message.Contains(key, StringComparison.Ordinal)) throw new BookingException(vietnamese);
            }
            throw;
        }
    }
}
