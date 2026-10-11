namespace HomestayBooking.Web.Models.Entities;

/// <summary>Yêu cầu đổi ngày / đổi loại phòng / hủy (FR-05, FR-10).</summary>
public class BookingChangeRequest
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public ChangeRequestType RequestType { get; set; }
    public ActorType RequestedByType { get; set; }
    public DateOnly? NewCheckIn { get; set; }
    public DateOnly? NewCheckOut { get; set; }
    public int? NewRoomTypeId { get; set; }

    /// <summary>Chênh lệch tiền khi đổi, có thể âm.</summary>
    public decimal? PriceDifference { get; set; }

    /// <summary>Số tiền hoàn dự kiến theo chính sách hủy (khách xem trước).</summary>
    public decimal? RefundAmount { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
    public string? DecidedByAccountId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public RoomType? NewRoomType { get; set; }
    public ApplicationUser? DecidedByAccount { get; set; }
}
