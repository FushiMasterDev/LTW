namespace HomestayBooking.Web.Models.Entities;

/// <summary>Bảng đối soát một homestay một kỳ (BR-03): kỳ 1 ngày 1–15, kỳ 2 ngày 16 – cuối tháng.</summary>
public class Settlement
{
    public int Id { get; set; }
    public int HomestayId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int PeriodNo { get; set; }
    public decimal TotalRoomAmount { get; set; }
    public decimal TotalRetained { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal PayoutAmount { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;
    /// <summary>Lúc kế toán gửi bảng cho chủ homestay (Draft → Sent).</summary>
    public DateTime? SentAt { get; set; }
    /// <summary>Lúc chủ homestay xác nhận số liệu (Sent → Confirmed).</summary>
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    /// <summary>Mã giao dịch Fuji chuyển tiền cho chủ homestay.</summary>
    public string? PaidBankRef { get; set; }
    public int? PaidByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Homestay Homestay { get; set; } = null!;
    public Employee? PaidByEmployee { get; set; }
    public ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();
}

/// <summary>Một đơn trong bảng đối soát; số tiền sao chép từ đơn lúc lập (công thức mục 7.3).</summary>
public class SettlementItem
{
    public int Id { get; set; }
    public int SettlementId { get; set; }

    /// <summary>Unique: một đơn chỉ vào một kỳ.</summary>
    public int BookingId { get; set; }

    public SettlementItemType ItemType { get; set; }
    public decimal RoomAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RetainedAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    /// <summary>Phần voucher Fuji chịu cho đơn này (đã trừ phần trả lại voucher khi hủy) — Fuji thực thu = CommissionAmount − DiscountAmount.</summary>
    public decimal DiscountAmount { get; set; }

    public Settlement Settlement { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
}
