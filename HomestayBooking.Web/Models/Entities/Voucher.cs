namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Voucher xin lỗi khách khi Fuji làm hỏng đơn (trùng phòng, homestay từ chối sau khi đã cọc...). Q4, D14:
/// chỉ quản lý cấp; gắn với một khách; có số dư dùng nhiều lần đến hết hạn; mỗi đơn chỉ giảm tối đa 20% hoa hồng
/// Fuji của đơn đó (trừ vào tiền cọc). Fuji chịu toàn bộ, homestay vẫn nhận đủ.
/// </summary>
public class Voucher
{
    public int Id { get; set; }

    /// <summary>Mã hiển thị cho khách, ví dụ VC7K2M9QXA.</summary>
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    /// <summary>Giá trị lúc cấp: 5% tổng tiền đơn gốc, trong khoảng 50.000–200.000đ.</summary>
    public decimal Amount { get; set; }

    /// <summary>Số dư còn dùng được. Là concurrency token: hai đơn dùng cùng lúc thì một đơn phải thử lại.</summary>
    public decimal Balance { get; set; }

    /// <summary>Hết hạn sau ngày này (giờ Việt Nam).</summary>
    public DateOnly ExpiresOn { get; set; }

    public VoucherStatus Status { get; set; } = VoucherStatus.Active;
    public string Reason { get; set; } = string.Empty;

    /// <summary>Đơn bị làm hỏng dẫn tới việc cấp voucher; mỗi đơn chỉ được cấp một voucher.</summary>
    public int? SourceBookingId { get; set; }

    public int IssuedByEmployeeId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }

    public Customer Customer { get; set; } = null!;
    public Booking? SourceBooking { get; set; }
    public Employee IssuedByEmployee { get; set; } = null!;
    public ICollection<Booking> UsedByBookings { get; set; } = new List<Booking>();
}
