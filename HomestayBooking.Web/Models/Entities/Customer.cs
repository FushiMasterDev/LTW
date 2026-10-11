namespace HomestayBooking.Web.Models.Entities;

/// <summary>Hồ sơ nghiệp vụ khách hàng (v5 mục 4.4). Booking tham chiếu Customer, không tham chiếu Account.</summary>
public class Customer
{
    public int Id { get; set; }
    public int PersonId { get; set; }

    /// <summary>Ghi chú nội bộ của nhân viên về khách (FR-12).</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PersonProfile Person { get; set; } = null!;
    public CustomerProfile? Profile { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
