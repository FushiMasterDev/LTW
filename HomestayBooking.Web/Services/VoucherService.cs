using System.Security.Cryptography;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Voucher (Q4, D14): quản lý cấp cho khách bị ảnh hưởng; khách dùng trừ vào tiền cọc của đơn sau.
/// Mỗi đơn giảm tối đa 20% hoa hồng Fuji (Fuji vẫn lãi ≥ 80% hoa hồng); số dư còn lại dùng tiếp đến khi hết hạn.
/// Đơn dùng voucher bị hủy / từ chối / hết hạn → cộng trả số tiền đã giảm vào voucher (nếu voucher còn hạn).
/// </summary>
public class VoucherService
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // bỏ 0/O, 1/I dễ nhầm

    private static readonly EmployeePosition[] Issuers = [EmployeePosition.Manager, EmployeePosition.Administrator];

    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly VoucherOptions _options;

    public VoucherService(ApplicationDbContext db, NotificationService notifications, TimeProvider clock, IOptions<VoucherOptions> options)
    {
        _db = db;
        _notifications = notifications;
        _clock = clock;
        _options = options.Value;
    }

    public VoucherOptions Options => _options;

    private DateOnly Today => VietnamTime.Today(_clock);

    /// <summary>Giảm tối đa cho một đơn = tỷ lệ × hoa hồng, làm tròn XUỐNG nghìn để không vượt quá tỷ lệ.</summary>
    public static decimal MaxDiscount(decimal total, decimal commissionRate, decimal share) =>
        Math.Floor(total * commissionRate * share / 1000m) * 1000m;

    /// <summary>Giá trị voucher khi cấp từ đơn gốc.</summary>
    public decimal IssueAmountFor(decimal sourceTotal) =>
        Math.Clamp(PricingService.RoundToThousand(sourceTotal * _options.IssueRate), _options.MinAmount, _options.MaxAmount);

    /// <summary>Voucher khách đang dùng được (còn hạn, còn số dư), hạn gần nhất lên trước.</summary>
    public Task<List<Voucher>> UsableAsync(int customerId)
    {
        var today = Today;
        return _db.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == customerId && v.Status == VoucherStatus.Active && v.Balance > 0 && v.ExpiresOn >= today)
            .OrderBy(v => v.ExpiresOn)
            .ToListAsync();
    }

    /// <summary>Số tiền sẽ giảm nếu dùng voucher này cho đơn có tổng tiền và hoa hồng như trên (để báo giá trước khi đặt).</summary>
    public decimal PreviewDiscount(Voucher voucher, decimal total, decimal commissionRate) =>
        Math.Min(voucher.Balance, MaxDiscount(total, commissionRate, _options.MaxDiscountShareOfCommission));

    /// <summary>
    /// Trừ số dư voucher cho đơn đang tạo (chưa lưu — BookingService lưu cùng đơn trong một transaction).
    /// Không giảm được (voucher không hợp lệ, hoặc đơn quá nhỏ nên giảm 0đ) thì ném BookingException.
    /// </summary>
    public async Task ApplyAsync(Booking booking, int voucherId, int customerId)
    {
        var voucher = await _db.Vouchers.SingleOrDefaultAsync(v => v.Id == voucherId);
        if (voucher is null || voucher.CustomerId != customerId || voucher.Status != VoucherStatus.Active
            || voucher.Balance <= 0 || voucher.ExpiresOn < Today)
        {
            throw new BookingException("Voucher không hợp lệ hoặc đã hết hạn.");
        }

        var discount = PreviewDiscount(voucher, booking.TotalAmount, booking.CommissionRate);
        if (discount <= 0)
        {
            throw new BookingException("Đơn quá nhỏ để dùng voucher.");
        }

        voucher.Balance -= discount;
        if (voucher.Balance == 0)
        {
            voucher.Status = VoucherStatus.UsedUp;
        }
        booking.Voucher = voucher;
        booking.DiscountAmount = discount;
    }

    /// <summary>
    /// Đơn dùng voucher không thành → cộng trả số tiền đã giảm vào voucher, theo cùng tỷ lệ hoàn tiền mặt
    /// (hủy được hoàn 50% thì voucher cũng chỉ được trả 50%), để khách dùng voucher không mất ít hơn khách không dùng.
    /// Homestay từ chối, hết hạn giữ phòng, hủy khi chưa có cọc → trả 100%. Chưa lưu.
    /// </summary>
    public async Task RestoreAsync(Booking booking, int percent = 100)
    {
        if (booking.VoucherId is null || booking.DiscountAmount <= 0)
        {
            return;
        }
        var voucher = booking.Voucher ?? await _db.Vouchers.SingleAsync(v => v.Id == booking.VoucherId);
        if (voucher.Status is VoucherStatus.Revoked or VoucherStatus.Expired || voucher.ExpiresOn < Today)
        {
            return;
        }
        var restored = PricingService.RoundToThousand(booking.DiscountAmount * percent / 100m);
        if (restored <= 0)
        {
            return;
        }
        voucher.Balance = Math.Min(voucher.Amount, voucher.Balance + restored);
        voucher.Status = VoucherStatus.Active;
    }

    /// <summary>Quản lý cấp voucher cho khách của một đơn bị ảnh hưởng.</summary>
    public async Task<Voucher> IssueAsync(string sourceBookingCode, int employeeId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BookingException("Vui lòng nhập lý do cấp voucher.");
        }
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId && e.EmploymentStatus == EmploymentStatus.Active && Issuers.Contains(e.Position)))
        {
            throw new BookingException("Chỉ quản lý được cấp voucher.");
        }

        var code = sourceBookingCode.Trim().ToUpperInvariant();
        var source = await _db.Bookings
            .Where(b => b.Code == code)
            .Select(b => new
            {
                b.Id,
                b.CustomerId,
                b.TotalAmount,
                Account = b.Customer.Person.Account == null ? null : b.Customer.Person.Account.Id,
            })
            .SingleOrDefaultAsync()
            ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        if (await _db.Vouchers.AnyAsync(v => v.SourceBookingId == source.Id))
        {
            throw new BookingException("Đơn này đã được cấp voucher.");
        }

        var amount = IssueAmountFor(source.TotalAmount);
        var voucher = new Voucher
        {
            Code = NewCode(),
            CustomerId = source.CustomerId,
            Amount = amount,
            Balance = amount,
            ExpiresOn = Today.AddMonths(_options.ValidMonths),
            Reason = reason.Trim(),
            SourceBookingId = source.Id,
            IssuedByEmployeeId = employeeId,
            IssuedAt = _clock.GetUtcNow().UtcDateTime,
        };
        _db.Vouchers.Add(voucher);
        _notifications.Add([source.Account],
            $"Fuji tặng bạn voucher {voucher.Code} trị giá {amount:N0}đ, dùng đến {voucher.ExpiresOn:dd/MM/yyyy}. Xin lỗi vì sự bất tiện.");
        await _db.SaveChangesAsync();
        return voucher;
    }

    public async Task RevokeAsync(int voucherId, int employeeId, string reason)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId && e.EmploymentStatus == EmploymentStatus.Active && Issuers.Contains(e.Position)))
        {
            throw new BookingException("Chỉ quản lý được thu hồi voucher.");
        }
        var voucher = await _db.Vouchers.SingleOrDefaultAsync(v => v.Id == voucherId)
            ?? throw new BookingException("Không tìm thấy voucher.");
        if (voucher.Status == VoucherStatus.Revoked)
        {
            return;
        }
        voucher.Status = VoucherStatus.Revoked;
        voucher.RevokedAt = _clock.GetUtcNow().UtcDateTime;
        voucher.RevokeReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _db.SaveChangesAsync();
    }

    /// <summary>Job hằng ngày/phút: voucher quá hạn → Expired.</summary>
    public Task<int> ExpireAsync()
    {
        var today = Today;
        return _db.Vouchers
            .Where(v => v.ExpiresOn < today && (v.Status == VoucherStatus.Active || v.Status == VoucherStatus.UsedUp))
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, VoucherStatus.Expired));
    }

    private static string NewCode() =>
        "VC" + new string(Enumerable.Range(0, 8).Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]).ToArray());
}
