using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public class ReminderOptions : IValidatableObject
{
    public const string Section = "Reminders";
    [Range(0, 365)] public int CustomerDaysBefore { get; set; } = 1;
    [Range(0, 23)] public int SendHour { get; set; } = 9;
    [Range(1, 23)] public int QuietFromHour { get; set; } = 21;
    // Giới hạn URL để mã đơn, ngày/giờ và SĐT vẫn nằm trọn trong tin 160 ký tự.
    [Required, MaxLength(50)] public string PublicBaseUrl { get; set; } = "http://localhost:5011";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SendHour >= QuietFromHour) yield return new("Giờ bắt đầu gửi phải trước giờ yên lặng.", [nameof(SendHour), nameof(QuietFromHour)]);
        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            yield return new("URL công khai phải là địa chỉ HTTP/HTTPS tuyệt đối, không có thông tin đăng nhập, query hoặc fragment.", [nameof(PublicBaseUrl)]);
    }
}

/// <summary>FR-23: giữ Pending trong transaction ngắn, gửi ngoài transaction, lưu kết quả bằng transaction khác.</summary>
public class ReminderService(ApplicationDbContext db, MessageService messages, NotificationService notifications,
    TimeProvider clock, IOptions<ReminderOptions> options, ILogger<ReminderService> logger)
{
    public const string CustomerTemplate = "CheckinReminder";
    public const string OwnerTemplate = "OwnerArrivals";
    private const int MaxAttempts = 3;
    public static string OwnerUrl(int ownerId, DateOnly arrivalDate) => $"/Owner/Bookings?ownerId={ownerId}&arrivals={arrivalDate:yyyy-MM-dd}";
    public static string CustomerUrl(string code) => $"/Bookings/Details/{code}?reminder=checkin";
    private bool CanSendNow()
    {
        var hour = VietnamTime.ToLocal(clock.GetUtcNow().UtcDateTime).Hour;
        return hour >= options.Value.SendHour && hour < options.Value.QuietFromHour;
    }

    public static string CustomerText(Booking booking, string publicBaseUrl)
    {
        if (!BookingContactVisibility.CanView(booking.Status)) throw new BookingException("Chưa được gửi liên hệ homestay trước khi xác nhận.");
        var link = publicBaseUrl.TrimEnd('/') + "/Bookings/Details/" + Uri.EscapeDataString(booking.Code);
        var tail = $" | {booking.CheckIn:dd/MM/yyyy} {booking.Homestay.CheckinTime:HH\\:mm} | {booking.Homestay.ContactPhone} | {link}";
        var name = Regex.Replace(booking.Homestay.Name, @"\s+", " ").Trim();
        var budget = 160 - booking.Code.Length - 3 - tail.Length;
        if (budget < 1) throw new BookingException("URL công khai quá dài để tạo tin nhắc 160 ký tự.");
        if (name.Length > budget)
        {
            var length = budget - 1;
            if (length > 0 && char.IsHighSurrogate(name[length - 1])) length--;
            name = name[..length].TrimEnd() + "…";
        }
        return booking.Code + " | " + name + tail;
    }

    public async Task<int> SendCustomersAsync(CancellationToken cancellationToken = default)
    {
        if (!CanSendNow()) return 0;
        var today = VietnamTime.Today(clock); var last = today.AddDays(options.Value.CustomerDaysBefore);
        var ids = await db.Bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn >= today && b.CheckIn <= last)
            .Select(b => b.Id).ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var id in ids)
        {
            if (!CanSendNow()) break;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var delivery = await HomestayWrite.RunAsync(db, async () =>
                {
                    if (db.Database.IsRelational() && await db.Bookings.Where(b => b.Id == id && b.Status == BookingStatus.Confirmed)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, b => b.Status), cancellationToken) != 1) return null;
                    var booking = await db.Bookings.AsNoTracking().Include(b => b.Homestay)
                        .Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
                        .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
                    if (booking is null || booking.Status != BookingStatus.Confirmed || booking.ConfirmedAt is null
                        || booking.CheckIn < today || booking.CheckIn > last || !CanSendNow()) return null;
                    var url = CustomerUrl(booking.Code);
                    var account = booking.Customer.Person.Account?.Id;
                    // Thông báo app độc lập với kênh ngoài; xác nhận lại mở chu kỳ nhắc mới.
                    if (account is not null && !await db.Notifications.AnyAsync(n => n.UserId == account && n.Url == url
                        && n.CreatedAt >= booking.ConfirmedAt, cancellationToken))
                        PartnerServiceSupport.Notify(db, notifications, clock, [account],
                            $"Sắp đến lịch nhận phòng đơn {booking.Code} ngày {booking.CheckIn:dd/MM/yyyy}. Xem chi tiết đơn để biết thông tin nhận phòng.", url);
                    try
                    {
                        var text = CustomerText(booking, options.Value.PublicBaseUrl);
                        var pending = await messages.ReserveAsync(booking.ContactPhone, CustomerTemplate, id, booking.ConfirmedAt.Value, MaxAttempts, cancellationToken);
                        return pending is null ? null : new CustomerDelivery(pending, text);
                    }
                    catch (BookingException) { logger.LogWarning("Chưa có dữ liệu hợp lệ để gửi tin ngoài cho đơn {BookingId}.", id); return null; }
                });
                if (delivery is null) continue;
                var result = await messages.DeliverAsync(delivery.Pending, delivery.Text, canSend: CanSendNow, cancellationToken: cancellationToken);
                if (result.Succeeded) sent++;
            }
            catch (BookingException) { logger.LogWarning("Không gửi được lời nhắc cho đơn {BookingId}: kiểm tra dữ liệu liên hệ/cấu hình.", id); }
        }
        return sent;
    }

    private record CustomerDelivery(MessageLog Pending, string Text);
    private record OwnerDelivery(MessageLog Pending, string Text);

    public async Task<int> SendOwnersAsync(CancellationToken cancellationToken = default)
    {
        if (!CanSendNow()) return 0;
        var local = VietnamTime.ToLocal(clock.GetUtcNow().UtcDateTime); var tomorrow = DateOnly.FromDateTime(local).AddDays(1);
        var since = VietnamTime.ToUtc(local.Date);
        var candidates = await OwnerBookings(tomorrow).ToListAsync(cancellationToken);
        var sentOwners = new HashSet<int>();
        // Metadata nhật ký dùng số nhận/ngày: gộp cả cơ sở khác chủ nếu dùng chung số liên hệ.
        foreach (var group in candidates.GroupBy(b => PhoneNumberNormalizer.ToE164(b.Homestay.ContactPhone)))
        {
            if (!CanSendNow()) break;
            cancellationToken.ThrowIfCancellationRequested();
            var ownerIds = group.Select(b => b.Homestay.OwnerId).Distinct().Order().ToArray();
            async Task LockOwners()
            {
                foreach (var owner in ownerIds) await LockOwnerAsync(owner, cancellationToken);
            }
            var delivery = await HomestayWrite.RunAsync(db, async () =>
            {
                await LockOwners();
                var bookings = await OwnerBookings(tomorrow).Where(b => ownerIds.Contains(b.Homestay.OwnerId)).ToListAsync(cancellationToken);
                var forPhone = bookings.Where(b => PhoneNumberNormalizer.ToE164(b.Homestay.ContactPhone) == group.Key).ToList();
                if (forPhone.Count == 0 || !CanSendNow()) return null;
                foreach (var owner in bookings.GroupBy(b => b.Homestay.OwnerId))
                {
                    var url = OwnerUrl(owner.Key, tomorrow);
                    var account = owner.First().Homestay.Owner.Person.Account?.Id;
                    if (account is not null && !await db.Notifications.AnyAsync(n => n.UserId == account && n.Url == url, cancellationToken))
                        foreach (var chunk in OwnerSummary(owner, tomorrow).SelectMany(x => x.Chunks))
                            PartnerServiceSupport.Notify(db, notifications, clock, [account], chunk, url);
                }
                if (group.Key is null) { logger.LogWarning("Có cơ sở chưa có SĐT hợp lệ để nhắc lịch đón khách."); return null; }
                var pending = await messages.ReserveAsync(group.Key, OwnerTemplate, null, since, MaxAttempts, cancellationToken);
                if (pending is null) return null;
                return new OwnerDelivery(pending, string.Join(" ", OwnerSummary(forPhone, tomorrow).Select(x => x.Text)));
            });
            if (delivery is null) continue;
            var result = await messages.DeliverAsync(delivery.Pending, delivery.Text, canSend: CanSendNow,
                cancellationToken: cancellationToken, lockForCompletion: LockOwners);
            if (result.Succeeded) sentOwners.UnionWith(ownerIds);
        }
        return sentOwners.Count;
    }

    private IQueryable<Booking> OwnerBookings(DateOnly tomorrow) => db.Bookings.AsNoTracking()
        .Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
        .Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn == tomorrow).OrderBy(b => b.Code);

    private static IEnumerable<(string Text, IReadOnlyList<string> Chunks)> OwnerSummary(IEnumerable<Booking> bookings, DateOnly date)
    {
        foreach (var home in bookings.GroupBy(b => b.HomestayId))
        {
            var h = home.First().Homestay;
            var rows = home.Select(b => $"{b.Code}: {b.ContactName}, {b.Guests} khách, nhận {h.CheckinTime:HH\\:mm}.").ToList();
            var header = $"Lịch đón khách ngày {date:dd/MM/yyyy} tại {h.Name}: ";
            yield return (header + string.Join(" ", rows), Chunks(header, rows).ToList());
        }
    }

    private async Task LockOwnerAsync(int id, CancellationToken cancellationToken)
    {
        if (db.Database.IsRelational()) await db.Owners.Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UpdatedAt, o => o.UpdatedAt), cancellationToken);
    }

    private static IEnumerable<string> Chunks(string header, IEnumerable<string> rows)
    {
        var current = header;
        foreach (var row in rows)
        {
            if (current.Length + row.Length + 1 > 500) { yield return current.Trim(); current = header; }
            current += row + " ";
        }
        if (current.Length > header.Length) yield return current.Trim();
    }
}
