namespace HomestayBooking.Web.Models.Entities;

/// <summary>Lịch sử đổi trạng thái đơn (V4, FR-09).</summary>
public class BookingStatusHistory
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    /// <summary>Null khi đơn vừa được tạo.</summary>
    public BookingStatus? FromStatus { get; set; }

    public BookingStatus ToStatus { get; set; }
    public ActorType ActorType { get; set; }

    /// <summary>Null khi do hệ thống hoặc khách chưa đăng nhập thực hiện.</summary>
    public string? ActorAccountId { get; set; }

    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public ApplicationUser? ActorAccount { get; set; }
}
