namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Khoản tiền gắn với một đơn. Cọc đi đường chính là tự khớp giao dịch QR MB (BankAutoMatch, D13);
/// dự phòng là ảnh bill kế toán duyệt (ManualReceipt, D11).
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int? ChangeRequestId { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Một giao dịch ngân hàng chỉ thành một khoản thanh toán.</summary>
    public int? BankTransactionId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Ảnh bill; bắt buộc khi Method = ManualReceipt. Lưu ngoài wwwroot (dữ liệu nhạy cảm).</summary>
    public string? ReceiptImageUrl { get; set; }

    /// <summary>Nội dung chuyển khoản, phải chứa mã đơn (BR-10).</summary>
    public string? TransferContent { get; set; }

    public string? BankRef { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Submitted;

    /// <summary>Null = khách tra cứu bằng mã đơn + SĐT, hoặc hệ thống.</summary>
    public string? SubmittedByAccountId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null khi hệ thống tự duyệt.</summary>
    public int? ReviewedByEmployeeId { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }

    /// <summary>Bill chờ duyệt quá lâu đã báo quản lý (chỉ báo một lần).</summary>
    public DateTime? ReviewAlertSentAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public BookingChangeRequest? ChangeRequest { get; set; }
    public BankTransaction? BankTransaction { get; set; }
    public ApplicationUser? SubmittedByAccount { get; set; }
    public Employee? ReviewedByEmployee { get; set; }
}
