using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomestayBooking.Web.Services;

/// <summary>Kiểm tra trước mọi thay đổi hồ sơ, trong transaction đăng ký của bên gọi.</summary>
public sealed class BookingCodeVerificationService(ApplicationDbContext db, BookingCodeVerificationAttempts attempts)
{
    public const string VerificationRequired = "Số điện thoại này đã có đơn đặt phòng. Vui lòng nhập đúng mã một đơn đã đặt để xác minh, hoặc liên hệ Fuji.";
    public const string TooManyAttempts = "Thử quá nhiều lần, vui lòng thử lại sau 15 phút.";

    /// <returns>True khi hồ sơ có đơn và đã xác minh; false khi không cần xác minh.</returns>
    public async Task<bool> VerifyAsync(PersonProfile? person, string? bookingCode)
    {
        if (person is null || person.Account is not null
            || !await db.Bookings.AnyAsync(b => b.Customer.PersonId == person.Id))
            return false;

        attempts.ThrowIfBlocked(person.PhoneNumber);
        var code = bookingCode?.Trim().ToUpperInvariant();
        var matches = !string.IsNullOrEmpty(code) && await db.Bookings.AnyAsync(b =>
            b.Customer.PersonId == person.Id && b.Code.Trim().ToUpper() == code);
        attempts.RecordResult(person.PhoneNumber, matches);
        return true;
    }
}

/// <summary>Bộ đếm singleton dùng chung giữa đăng ký khách và chủ, khóa theo SĐT E.164.</summary>
public sealed class BookingCodeVerificationAttempts(IMemoryCache cache, TimeProvider clock)
{
    // shortcut: đếm trong bộ nhớ, mất khi khởi động lại; chuyển sang DB/Redis nếu chạy nhiều instance
    private readonly object _gate = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private sealed class Attempts
    {
        public Queue<DateTimeOffset> Failures { get; } = new();
        public DateTimeOffset? BlockedUntil { get; set; }
    }

    public void ThrowIfBlocked(string e164)
    {
        lock (_gate)
        {
            if (cache.TryGetValue<Attempts>((typeof(BookingCodeVerificationAttempts), e164), out var state)
                && state?.BlockedUntil > clock.GetUtcNow())
                throw new BookingException(BookingCodeVerificationService.TooManyAttempts);
        }
    }

    public void RecordResult(string e164, bool matches)
    {
        lock (_gate)
        {
            var key = (typeof(BookingCodeVerificationAttempts), e164);
            var now = clock.GetUtcNow();
            var state = cache.Get<Attempts>(key) ?? new Attempts();
            // Kiểm tra lại sau truy vấn để các request đồng thời không vượt qua khóa vừa được đặt.
            if (state.BlockedUntil > now)
                throw new BookingException(BookingCodeVerificationService.TooManyAttempts);
            if (matches)
            {
                cache.Remove(key);
                return;
            }
            while (state.Failures.TryPeek(out var failure) && failure <= now - Window)
                state.Failures.Dequeue();
            state.Failures.Enqueue(now);
            if (state.Failures.Count >= 5) state.BlockedUntil = now + Window;
            // Hạn cache chỉ dọn bộ nhớ; cửa sổ và thời hạn khóa luôn xét bằng TimeProvider.
            cache.Set(key, state, Window + Window);
            throw new BookingException(state.BlockedUntil > now
                ? BookingCodeVerificationService.TooManyAttempts : BookingCodeVerificationService.VerificationRequired);
        }
    }
}
