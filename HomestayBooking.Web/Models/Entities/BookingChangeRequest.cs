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
    public string? RequestedByAccountId { get; set; }
    public int? NewRoomId { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Các khoản tiền chốt lúc gửi; NULL chỉ dành cho lịch sử chưa có báo giá.</summary>
    public decimal? ChangeFee { get; set; }
    public decimal? DepositCarriedOver { get; set; }
    public decimal? AdditionalDeposit { get; set; }
    public decimal? NewTotalAmount { get; set; }

    /// <summary>Hạn khách nộp tiền, không phải hạn kế toán xác nhận.</summary>
    public DateTime? PaymentDeadlineAt { get; set; }
    public string? FinanceConfirmedByAccountId { get; set; }
    public DateTime? FinanceConfirmedAt { get; set; }
    public string? FinanceNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public RoomType? NewRoomType { get; set; }
    public ApplicationUser? DecidedByAccount { get; set; }
    public ApplicationUser? RequestedByAccount { get; set; }
    public ApplicationUser? FinanceConfirmedByAccount { get; set; }
    public Room? NewRoom { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<RoomNight> RoomNights { get; set; } = new List<RoomNight>();
}
