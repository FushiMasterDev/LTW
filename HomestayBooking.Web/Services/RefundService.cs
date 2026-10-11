using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record RefundQuote(int DaysBefore, int RefundPercent, decimal RefundAmount);

/// <summary>Chọn chính sách hủy (D10) và tính tiền hoàn (BR-04) — fuji_homestay_database_v1 mục 5.C, 7.2.</summary>
public class RefundService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public RefundService(ApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>
    /// Ưu tiên: chính sách lễ của homestay (nếu đơn có đêm lễ) → chính sách mọi ngày của homestay
    /// → chính sách lễ chung (nếu có đêm lễ) → chính sách mọi ngày chung.
    /// </summary>
    public async Task<CancellationPolicy> SelectPolicyAsync(int homestayId, bool hasHolidayNight)
    {
        var candidates = await _db.CancellationPolicies
            .Where(p => p.IsActive && (p.HomestayId == homestayId || p.HomestayId == null))
            .ToListAsync();

        var ordered = new (int? HomestayId, PolicyScope Scope)[]
        {
            (homestayId, PolicyScope.Holiday),
            (homestayId, PolicyScope.All),
            (null, PolicyScope.Holiday),
            (null, PolicyScope.All),
        };
        foreach (var (owner, scope) in ordered)
        {
            if (scope == PolicyScope.Holiday && !hasHolidayNight)
            {
                continue;
            }
            var policy = candidates.FirstOrDefault(p => p.HomestayId == owner && p.Scope == scope);
            if (policy is not null)
            {
                return policy;
            }
        }

        throw new InvalidOperationException("Chưa cấu hình chính sách hủy chung của Fuji.");
    }

    /// <summary>
    /// Hoàn = cọc × % của quy tắc có MinDaysBefore lớn nhất mà ≤ số ngày còn lại trước ngày nhận phòng.
    /// Chỉ tính khi cọc đã được duyệt (Deposited, Confirmed); chưa có cọc thì hoàn 0.
    /// </summary>
    public async Task<RefundQuote> QuoteCancellationAsync(Booking booking)
    {
        var daysBefore = booking.CheckIn.DayNumber - VietnamTime.Today(_clock).DayNumber;
        if (booking.Status is not (BookingStatus.Deposited or BookingStatus.Confirmed))
        {
            return new RefundQuote(daysBefore, 0, 0);
        }

        var rules = await _db.CancellationRules
            .Where(r => r.PolicyId == booking.CancellationPolicyId)
            .ToListAsync();
        var percent = rules
            .Where(r => r.MinDaysBefore <= daysBefore)
            .MaxBy(r => r.MinDaysBefore)?.RefundPercent ?? 0;

        return new RefundQuote(daysBefore, percent, PricingService.RoundToThousand(booking.DepositAmount * percent / 100m));
    }
}
