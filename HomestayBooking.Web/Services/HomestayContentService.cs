using Ganss.Xss;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record HomestayContentInput(string Name, string? DescriptionHtml, string? CheckinGuideHtml,
    string ContactPhone, TimeOnly CheckinTime, TimeOnly CheckoutTime, IReadOnlyCollection<int> AmenityIds);
public record HomestayLocationInput(string Address, int AreaId, decimal? Latitude, decimal? Longitude);
public record HomestayContentResult(Homestay Homestay, IReadOnlyList<string> UpcomingBookingCodes);

public record OwnerHomestaySummary(int Id, string Name, string AreaName, HomestayStatus Status, string? CoverUrl, int RoomCount);
public record EditorAmenity(int Id, string Name, string? Icon, bool Selected);
public record EditorCategory(int Id, string Name);
public record HomestayEditorPrice(int Id, DayType DayType, decimal Price, DateOnly EffectiveFrom);
public record HomestayEditorRoom(int Id, string RoomCode, bool IsActive);
/// <summary>CurrentPrices: giá đang áp dụng hôm nay (mỗi DayType một dòng EffectiveFrom gần nhất ≤ hôm nay); UpcomingPrices: EffectiveFrom > hôm nay.</summary>
public record HomestayEditorRoomType(int Id, string Name, int CategoryId, string CategoryName, int MaxGuests, int? AreaM2, string? Description,
    bool IsActive, IReadOnlyList<HomestayEditorRoom> Rooms, IReadOnlyList<HomestayEditorPrice> CurrentPrices, IReadOnlyList<HomestayEditorPrice> UpcomingPrices);
public record HomestayEditorImage(int Id, string Url, int? RoomTypeId, int SortOrder, bool IsCover);
public record HomestayEditorPolicy(int Id, PolicyScope Scope, string Name, IReadOnlyList<CancellationRuleInput> Rules);
/// <summary>Không có FileUrl: giấy tờ là file riêng tư, chỉ mở qua service có kiểm quyền.</summary>
public record HomestayEditorDocument(int Id, DocumentType Type, string? OriginalFileName, DateOnly? ExpiryDate, DateTime CreatedAt);
public record HomestayEditor(int Id, string Name, string Address, int AreaId, string AreaName, decimal? Latitude, decimal? Longitude,
    string? DescriptionHtml, string? CheckinGuideHtml, string? ContactPhone, TimeOnly CheckinTime, TimeOnly CheckoutTime, HomestayStatus Status,
    IReadOnlyList<int> AmenityIds, IReadOnlyList<HomestayEditorRoomType> RoomTypes, IReadOnlyList<HomestayEditorImage> Images,
    IReadOnlyList<HomestayEditorPolicy> Policies, IReadOnlyList<HomestayEditorDocument> Documents)
{
    public IReadOnlyList<EditorAmenity> Amenities { get; init; } = [];
    public IReadOnlyList<EditorCategory> Categories { get; init; } = [];
}

public class HomestayContentService(ApplicationDbContext db, HomestayAccessService access,
    NotificationService notifications, TimeProvider clock)
{
    /// <summary>Homestay của chủ đang đăng nhập (mọi trạng thái), cho trang "Homestay của tôi".</summary>
    public async Task<IReadOnlyList<OwnerHomestaySummary>> OwnerHomestaysAsync(Actor actor)
    {
        if (actor.Type != ActorType.Owner || actor.AccountId is null) throw new BookingException("Bạn không có quyền quản lý homestay này.");
        return await db.Homestays.AsNoTracking()
            .Where(h => h.Owner.Person.Account != null && h.Owner.Person.Account.Id == actor.AccountId
                && h.Owner.Person.Account.Status == AccountStatus.Active && h.Owner.Person.Employee == null)
            .OrderBy(h => h.Name)
            .Select(h => new OwnerHomestaySummary(h.Id, h.Name, h.Area.Name, h.Status,
                h.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault(), h.RoomTypes.SelectMany(t => t.Rooms).Count()))
            .ToListAsync();
    }

    /// <summary>Dữ liệu đổ vào form sửa homestay (và trang duyệt nội dung). Quyền đọc theo HomestayAccessService ở chế độ chỉ xem.</summary>
    public async Task<HomestayEditor> EditorAsync(int id, Actor actor, bool allowConsultant = true)
    {
        await access.AuthorizeAsync(id, actor, allowConsultant, readOnly: true);
        var today = VietnamTime.Today(clock);
        var home = await db.Homestays.AsNoTracking().AsSplitQuery()
            .Include(h => h.Area).Include(h => h.HomestayAmenities).Include(h => h.Images)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Category)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Rooms)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .Include(h => h.CancellationPolicies.Where(p => p.IsActive)).ThenInclude(p => p.Rules)
            .SingleAsync(h => h.Id == id);
        var documents = await db.VerificationDocuments.AsNoTracking().Where(d => d.HomestayId == id).OrderBy(d => d.DocumentType).ThenBy(d => d.Id)
            .Select(d => new HomestayEditorDocument(d.Id, d.DocumentType, d.OriginalFileName, d.ExpiryDate, d.CreatedAt)).ToListAsync();
        static HomestayEditorPrice Price(RoomTypePrice p) => new(p.Id, p.DayType, p.Price, p.EffectiveFrom);
        return new HomestayEditor(home.Id, home.Name, home.Address, home.AreaId, home.Area.Name, home.Latitude, home.Longitude,
            home.DescriptionHtml, home.CheckinGuideHtml, home.ContactPhone, home.CheckinTime, home.CheckoutTime, home.Status,
            home.HomestayAmenities.Select(a => a.AmenityId).Order().ToList(),
            home.RoomTypes.OrderBy(t => t.Name).Select(t => new HomestayEditorRoomType(t.Id, t.Name, t.CategoryId, t.Category.Name, t.MaxGuests, t.AreaM2,
                t.Description, t.IsActive, t.Rooms.OrderBy(r => r.RoomCode).Select(r => new HomestayEditorRoom(r.Id, r.RoomCode, r.IsActive)).ToList(),
                t.Prices.Where(p => p.EffectiveFrom <= today).GroupBy(p => p.DayType)
                    .Select(g => Price(g.MaxBy(p => p.EffectiveFrom)!)).OrderBy(p => p.DayType).ToList(),
                t.Prices.Where(p => p.EffectiveFrom > today).OrderBy(p => p.EffectiveFrom).ThenBy(p => p.DayType).Select(Price).ToList())).ToList(),
            home.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).ThenBy(i => i.Id)
                .Select(i => new HomestayEditorImage(i.Id, i.Url, i.RoomTypeId, i.SortOrder, i.IsCover)).ToList(),
            home.CancellationPolicies.OrderBy(p => p.Scope).Select(p => new HomestayEditorPolicy(p.Id, p.Scope, p.Name,
                p.Rules.OrderByDescending(r => r.MinDaysBefore).Select(r => new CancellationRuleInput(r.MinDaysBefore, r.RefundPercent)).ToList())).ToList(),
            documents)
        {
            Amenities = await db.Amenities.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new EditorAmenity(a.Id, a.Name, a.Icon, a.HomestayAmenities.Any(x => x.HomestayId == id))).ToListAsync(),
            Categories = await db.RoomCategories.AsNoTracking().OrderBy(c => c.Name).Select(c => new EditorCategory(c.Id, c.Name)).ToListAsync()
        };
    }

    public Task<Homestay> UpdateAsync(int id, HomestayContentInput input, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var home = await access.AuthorizeAsync(id, actor);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 200) throw new BookingException("Tên homestay phải có từ 1 đến 200 ký tự.");
        var phone = PhoneNumberNormalizer.ToE164(input.ContactPhone) ?? throw new BookingException("Số điện thoại liên hệ không hợp lệ.");
        var ids = input.AmenityIds.Distinct().ToArray();
        if (await db.Amenities.CountAsync(a => ids.Contains(a.Id)) != ids.Length) throw new BookingException("Tiện nghi không tồn tại.");
        var amenities = await db.HomestayAmenities.Where(a => a.HomestayId == id).ToListAsync();
        var old = new { Homestay = HomestayReviewService.State(home), AmenityIds = amenities.Select(a => a.AmenityId).ToArray() };
        var sanitizer = new HtmlSanitizer();
        home.Name = input.Name.Trim(); home.ContactPhone = phone;
        home.DescriptionHtml = sanitizer.Sanitize(input.DescriptionHtml ?? "");
        home.CheckinGuideHtml = sanitizer.Sanitize(input.CheckinGuideHtml ?? "");
        home.CheckinTime = input.CheckinTime; home.CheckoutTime = input.CheckoutTime; home.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        db.HomestayAmenities.RemoveRange(amenities.Where(a => !ids.Contains(a.AmenityId)));
        foreach (var amenity in ids.Except(amenities.Select(a => a.AmenityId))) db.HomestayAmenities.Add(new() { HomestayId = id, AmenityId = amenity });
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old,
            new { Homestay = HomestayReviewService.State(home), AmenityIds = ids });
        return home;
    });

    public Task<HomestayContentResult> UpdateLocationAsync(int id, HomestayLocationInput input, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var home = await access.AuthorizeAsync(id, actor);
        if (string.IsNullOrWhiteSpace(input.Address) || input.Address.Trim().Length > 300 || !await db.Areas.AnyAsync(a => a.Id == input.AreaId))
            throw new BookingException("Địa chỉ hoặc khu vực không hợp lệ.");
        if (input.Latitude is < -90 or > 90 || input.Longitude is < -180 or > 180) throw new BookingException("Tọa độ không hợp lệ.");
        var changed = home.Address != input.Address.Trim() || home.AreaId != input.AreaId || home.Latitude != input.Latitude || home.Longitude != input.Longitude;
        if (!changed) return new HomestayContentResult(home, []);
        var old = HomestayReviewService.State(home);
        home.Address = input.Address.Trim(); home.AreaId = input.AreaId; home.Latitude = input.Latitude; home.Longitude = input.Longitude;
        var recheck = home.Status is HomestayStatus.Active or HomestayStatus.Suspended;
        if (home.Status == HomestayStatus.Active) HomestayReviewService.Move(home, HomestayStatus.PendingReview, clock);
        home.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old, new { Homestay = HomestayReviewService.State(home), ChangeReason = "Thay đổi địa chỉ / vị trí" });
        var today = VietnamTime.Today(clock);
        var codes = await db.Bookings.Where(b => b.HomestayId == id && b.CheckIn >= today
            && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Deposited)).OrderBy(b => b.Code).Select(b => b.Code).ToListAsync();
        if (recheck) await HomestayManagerMessages.SendAsync(db, notifications, clock, id,
            "Địa chỉ/vị trí đã thay đổi, cần duyệt lại và liên hệ khách của các đơn sau", codes);
        return new HomestayContentResult(home, codes);
    });
}

internal static class HomestayManagerMessages
{
    internal static async Task SendAsync(ApplicationDbContext db, NotificationService notifications, TimeProvider clock,
        int id, string message, IReadOnlyList<string>? codes = null)
    {
        var managers = await PartnerServiceSupport.Managers(db).ToListAsync();
        foreach (var chunk in (codes is { Count: > 0 } ? codes.Chunk(12) : [Array.Empty<string>()]))
            PartnerServiceSupport.Notify(db, notifications, clock, managers,
                $"Homestay #{id}: {message}. {string.Join(", ", chunk)}", $"/Staff/Homestays/{id}");
    }
}
