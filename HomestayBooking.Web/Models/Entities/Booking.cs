namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Đơn đặt phòng — một đơn đúng một phòng (D2). Phòng được gán và giữ ngay khi đặt (D4).
/// Các cột tiền là snapshot tại thời điểm đặt, không đổi theo giá/hợp đồng về sau.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    /// <summary>Mã đơn dạng FJyyMMdd-NNN (BR-07), unique; cũng là nội dung chuyển khoản.</summary>
    public string Code { get; set; } = string.Empty;

    // ---- Ai đặt ----

    /// <summary>Khách đặt (v5 mục 4.4): tham chiếu Customer, kể cả khi khách chưa có Account.</summary>
    public int CustomerId { get; set; }

    /// <summary>Người ở thật, có thể khác người đặt.</summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>E.164.</summary>
    public string ContactPhone { get; set; } = string.Empty;

    public SourceChannel SourceChannel { get; set; } = SourceChannel.Web;

    /// <summary>Null = khách tự đặt trên web.</summary>
    public int? CreatedByEmployeeId { get; set; }

    /// <summary>Nhân viên phụ trách, giao lại được (FR-09).</summary>
    public int? AssignedEmployeeId { get; set; }

    // ---- Đặt gì ----

    /// <summary>Lưu thừa có chủ đích để lọc theo chủ homestay và lập đối soát nhanh.</summary>
    public int HomestayId { get; set; }

    public int RoomTypeId { get; set; }
    public int RoomId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }
    public string? SpecialRequests { get; set; }

    // ---- Trạng thái và mốc thời gian (mục 6) ----

    public BookingStatus Status { get; set; } = BookingStatus.AwaitingPayment;

    /// <summary>Hết hạn giữ phòng chờ thanh toán (D5): CreatedAt + 15 phút.</summary>
    public DateTime HoldExpiresAt { get; set; }

    public DateTime? PaymentSubmittedAt { get; set; }
    public DateTime? DepositedAt { get; set; }

    /// <summary>Hạn homestay xác nhận (D12, BR-06): DepositedAt + 30 phút.</summary>
    public DateTime? ConfirmDeadlineAt { get; set; }

    /// <summary>Đã cảnh báo nhân viên vì quá hạn xác nhận, để job không gửi lặp.</summary>
    public DateTime? ConfirmAlertSentAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // ---- Tiền (snapshot) ----

    /// <summary>= tổng BookingNights.Price.</summary>
    public decimal TotalAmount { get; set; }

    public decimal DepositRate { get; set; } = 0.30m;
    public decimal DepositAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public int CancellationPolicyId { get; set; }

    /// <summary>Số tiền hoàn khi hủy/từ chối (BR-04).</summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>Đơn thay thế khi Fuji chuyển khách sang phòng khác vì trùng phòng (BR-11).</summary>
    public int? ReplacedByBookingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public Employee? CreatedByEmployee { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public Homestay Homestay { get; set; } = null!;
    public RoomType RoomType { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public CancellationPolicy CancellationPolicy { get; set; } = null!;
    public Booking? ReplacedByBooking { get; set; }
    public ICollection<BookingNight> Nights { get; set; } = new List<BookingNight>();
    public ICollection<RoomNight> RoomNights { get; set; } = new List<RoomNight>();
    public ICollection<BookingStatusHistory> Histories { get; set; } = new List<BookingStatusHistory>();
    public ICollection<BookingChangeRequest> ChangeRequests { get; set; } = new List<BookingChangeRequest>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
    public SettlementItem? SettlementItem { get; set; }
}
