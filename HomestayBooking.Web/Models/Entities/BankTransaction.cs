namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Giao dịch tiền vào tài khoản MB của Fuji do dịch vụ theo dõi tài khoản báo về (D13).
/// Lưu cả giao dịch không khớp được đơn nào để kế toán không bỏ sót tiền của khách.
/// </summary>
public class BankTransaction
{
    public int Id { get; set; }
    public BankProvider Provider { get; set; }

    /// <summary>Unique theo Provider: webhook gửi lặp không bị ghi hai lần.</summary>
    public string ProviderTransactionId { get; set; } = string.Empty;

    public string? BankReference { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransferContent { get; set; }
    public DateTime TransactionTime { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON gốc để tra soát.</summary>
    public string? RawPayload { get; set; }

    public BankMatchStatus MatchStatus { get; set; } = BankMatchStatus.Unmatched;
    public int? MatchedBookingId { get; set; }
    public int? ProcessedByEmployeeId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Note { get; set; }

    public Booking? MatchedBooking { get; set; }
    public Employee? ProcessedByEmployee { get; set; }
    public Payment? Payment { get; set; }
}
