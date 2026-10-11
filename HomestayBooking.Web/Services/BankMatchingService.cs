using System.Text.RegularExpressions;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>Một giao dịch tiền vào do dịch vụ theo dõi tài khoản (SePay) hoặc trang giả lập gửi tới.</summary>
public record IncomingBankTransaction(
    BankProvider Provider,
    string ProviderTransactionId,
    string AccountNumber,
    decimal Amount,
    string? TransferContent,
    DateTime TransactionTimeUtc,
    string? BankReference = null,
    string? RawPayload = null);

public record BankMatchResult(bool Duplicate, BankMatchStatus Status, string? BookingCode);

/// <summary>
/// Ghi BankTransactions và tự khớp với đơn (D13, fuji_homestay_database_v1 mục 5.F):
/// đọc mã đơn trong nội dung chuyển khoản, đơn đang chờ thanh toán và đủ tiền → Payment Approved, đơn → Deposited.
/// </summary>
public partial class BankMatchingService
{
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;
    private readonly BankAccountOptions _account;
    private readonly TimeProvider _clock;

    public BankMatchingService(ApplicationDbContext db, BookingService bookings, IOptions<BankAccountOptions> account, TimeProvider clock)
    {
        _db = db;
        _bookings = bookings;
        _account = account.Value;
        _clock = clock;
    }

    /// <summary>
    /// Tìm mã đơn trong nội dung chuyển khoản. Ngân hàng hay bỏ dấu "-" và thêm chữ khác,
    /// nên chuẩn hóa (chữ hoa, chỉ giữ chữ và số) rồi tìm "FJ" + 6 số ngày + 3–4 số thứ tự.
    /// </summary>
    public static string? ExtractBookingCode(string? transferContent)
    {
        if (string.IsNullOrWhiteSpace(transferContent))
        {
            return null;
        }
        var normalized = NonAlphanumeric().Replace(transferContent.ToUpperInvariant(), string.Empty);
        foreach (Match match in BookingCodePattern().Matches(normalized))
        {
            if (DateOnly.TryParseExact(match.Groups[1].Value, "yyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return BookingCodeGenerator.Format(date, int.Parse(match.Groups[2].Value));
            }
        }
        return null;
    }

    public async Task<BankMatchResult> ProcessAsync(IncomingBankTransaction incoming)
    {
        // Webhook gửi lặp → trả kết quả cũ, không ghi lần hai
        var existing = await FindAsync(incoming);
        if (existing is not null)
        {
            return new BankMatchResult(true, existing.MatchStatus, existing.MatchedBooking?.Code);
        }

        var tx = new BankTransaction
        {
            Provider = incoming.Provider,
            ProviderTransactionId = incoming.ProviderTransactionId,
            BankReference = incoming.BankReference,
            AccountNumber = incoming.AccountNumber,
            Amount = incoming.Amount,
            TransferContent = incoming.TransferContent,
            TransactionTime = incoming.TransactionTimeUtc,
            ReceivedAt = _clock.GetUtcNow().UtcDateTime,
            RawPayload = incoming.RawPayload,
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await MatchAsync(tx);
        _db.BankTransactions.Add(tx);
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Đơn vừa đổi trạng thái (ví dụ job vừa cho hết hạn) → khớp lại từ đầu với trạng thái mới
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return await ProcessAsync(incoming);
        }
        catch (DbUpdateException)
        {
            // Hai webhook giống nhau đến cùng lúc: bản kia đã ghi trước
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var saved = await FindAsync(incoming);
            if (saved is null)
            {
                throw;
            }
            return new BankMatchResult(true, saved.MatchStatus, saved.MatchedBooking?.Code);
        }

        return new BankMatchResult(false, tx.MatchStatus, tx.MatchedBooking?.Code);
    }

    private Task<BankTransaction?> FindAsync(IncomingBankTransaction incoming) =>
        _db.BankTransactions.AsNoTracking()
            .Include(t => t.MatchedBooking)
            .SingleOrDefaultAsync(t => t.Provider == incoming.Provider && t.ProviderTransactionId == incoming.ProviderTransactionId);

    private async Task MatchAsync(BankTransaction tx)
    {
        if (tx.AccountNumber != _account.AccountNumber)
        {
            tx.MatchStatus = BankMatchStatus.Ignored;
            tx.Note = "Không phải tài khoản nhận cọc.";
            return;
        }

        var code = ExtractBookingCode(tx.TransferContent);
        var booking = code is null ? null : await _db.Bookings.Include(b => b.Payments).SingleOrDefaultAsync(b => b.Code == code);
        if (booking is null)
        {
            tx.MatchStatus = BankMatchStatus.Unmatched;
            return;
        }

        tx.MatchedBooking = booking;
        switch (booking.Status)
        {
            case BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted when tx.Amount >= booking.AmountDue:
                tx.MatchStatus = BankMatchStatus.Matched;
                if (tx.Amount > booking.AmountDue)
                {
                    tx.Note = $"Chuyển dư {tx.Amount - booking.AmountDue:N0}đ.";
                }
                ApprovePayment(booking, tx);
                await _bookings.ApplyStatusAsync(booking, BookingStatus.Deposited, Actor.System, $"Tự khớp giao dịch {tx.ProviderTransactionId}.");
                break;

            case BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted:
                tx.MatchStatus = BankMatchStatus.Underpaid;
                tx.Note = $"Thiếu {booking.AmountDue - tx.Amount:N0}đ so với số tiền cần chuyển.";
                break;

            case BookingStatus.Expired or BookingStatus.Cancelled or BookingStatus.Rejected:
                tx.MatchStatus = BankMatchStatus.LatePayment;
                break;

            default:
                // Đơn đã có cọc: khoản thừa, để kế toán xử lý
                tx.MatchStatus = BankMatchStatus.Unmatched;
                tx.Note = "Đơn đã có cọc trước đó.";
                break;
        }
    }

    /// <summary>
    /// Khách đã tải bill (ManualReceipt, Submitted) → duyệt luôn khoản đó và gắn giao dịch;
    /// chưa có → tạo khoản BankAutoMatch. Hệ thống duyệt nên ReviewedByEmployeeId = null.
    /// </summary>
    private void ApprovePayment(Booking booking, BankTransaction tx)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var payment = booking.Payments.SingleOrDefault(p => p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Submitted);
        if (payment is null)
        {
            payment = new Payment
            {
                Type = PaymentType.Deposit,
                Method = PaymentMethod.BankAutoMatch,
                SubmittedAt = now,
            };
            booking.Payments.Add(payment);
        }

        payment.BankTransaction = tx;
        payment.Amount = tx.Amount;
        payment.TransferContent = tx.TransferContent;
        payment.BankRef = tx.BankReference;
        payment.Status = PaymentStatus.Approved;
        payment.ReviewedAt = now;
    }

    [GeneratedRegex("[^A-Z0-9]")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex(@"FJ(\d{6})(\d{3,4})(?!\d)")]
    private static partial Regex BookingCodePattern();
}
