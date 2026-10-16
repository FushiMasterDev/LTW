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

public class HomestayContentService(ApplicationDbContext db, HomestayAccessService access,
    NotificationService notifications, TimeProvider clock)
{
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
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old, HomestayReviewService.State(home));
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
