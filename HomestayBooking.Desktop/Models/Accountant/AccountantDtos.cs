using System.Text.Json.Serialization;

namespace HomestayBooking.Desktop.Models.Accountant;

public sealed class ReceiptDto
{
    public int PaymentId { get; set; }
    public string BookingCode { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string HomestayName { get; set; } = "";
    public decimal AmountDue { get; set; }
    public string? TransferContent { get; set; }
    public DateTime HoldExpiresAt { get; set; }
    public int PreviousRejections { get; set; }
    public string Status { get; set; } = "";
    public string? FileUrl { get; set; }
}

public sealed class PagedDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public sealed class HolidayDto { public string Date { get; set; } = ""; public string Name { get; set; } = ""; }
public sealed class AreaDto { public int Id { get; set; } public string Name { get; set; } = ""; public string? Province { get; set; } public string? AreaGroup { get; set; } }
public sealed class CatalogItemDto { public int Id { get; set; } public string Name { get; set; } = ""; }
public sealed class RejectReceiptRequest { public string Reason { get; set; } = ""; }
public sealed class BankTransactionDto { public int Id { get; set; } public string Provider { get; set; } = ""; public string ProviderTransactionId { get; set; } = ""; public decimal Amount { get; set; } public string? TransferContent { get; set; } public DateTime TransactionTime { get; set; } public string MatchStatus { get; set; } = ""; public string? BookingCode { get; set; } public string? RefundBankRef { get; set; } }
public sealed class AdditionalDepositDto { public int PaymentId { get; set; } public int ChangeRequestId { get; set; } public string BookingCode { get; set; } = ""; public string ContactName { get; set; } = ""; public string ContactPhone { get; set; } = ""; public string HomestayName { get; set; } = ""; public decimal Amount { get; set; } public string Method { get; set; } = ""; public DateTime SubmittedAt { get; set; } public DateTime? PaymentDeadlineAt { get; set; } public string? DecisionNote { get; set; } public string? TransferContent { get; set; } }
