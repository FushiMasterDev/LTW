using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Số việc đang chờ ở từng tab kế toán (hiện trên thanh tab và trang tổng quan).</summary>
public record AccountingCounts(int PendingReceipts, int OpenTransactions, int PendingRefunds, decimal PendingRefundAmount, int PendingAdditionalDeposits = 0);

/// <summary>Ảnh bill (ManualReceipt) trong danh sách duyệt.</summary>
public record ReceiptRow(
    int PaymentId,
    string BookingCode,
    BookingStatus BookingStatus,
    string ContactName,
    string ContactPhone,
    string HomestayName,
    decimal Amount,
    decimal AmountDue,
    string? TransferContent,
    PaymentStatus Status,
    DateTime SubmittedAt,
    DateTime HoldExpiresAt,
    int PreviousRejections,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string? RejectReason)
{
    /// <summary>Phút giờ hành chính đã chờ — quá BookingOptions.ReceiptReviewAlertMinutes là trễ.</summary>
    public double OfficeMinutesWaiting { get; init; }
}

public record BankTransactionRow(
    int Id,
    BankProvider Provider,
    string ProviderTransactionId,
    decimal Amount,
    string? TransferContent,
    DateTime TransactionTime,
    BankMatchStatus MatchStatus,
    string? BookingCode,
    BookingStatus? BookingStatus,
    decimal? BookingAmountDue,
    string? Note,
    string? ProcessedBy,
    DateTime? ProcessedAt,
    string? RefundBankRef);

/// <summary>Đơn có thể là đích của giao dịch: còn chờ thanh toán, số tiền cần chuyển không vượt số tiền đã nhận.</summary>
public record MatchSuggestion(string Code, string ContactName, string ContactPhone, string HomestayName, BookingStatus Status, decimal AmountDue, DateTime CreatedAt);

public class TransactionProcessViewModel
{
    public required BankTransactionRow Transaction { get; init; }
    public IReadOnlyList<MatchSuggestion> Suggestions { get; init; } = [];
}

public record RefundRow(
    int BookingId,
    string BookingCode,
    BookingStatus Status,
    string ContactName,
    string ContactPhone,
    string HomestayName,
    decimal AmountPaid,
    decimal RefundAmount,
    DateTime? CancelledAt,
    string? CancelReason,
    string? DepositTransferContent,
    string? RefundBankRef,
    string? RefundedBy,
    DateTime? RefundedAt);
