using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public partial class AccountingService
{
    private Task<Employee> ReadAccountingAsync(Actor actor) => PartnerServiceSupport.EmployeeAsync(_db, actor,
        EmployeePosition.Accountant, EmployeePosition.Manager, EmployeePosition.Administrator);

    public async Task<PaginatedList<ReceiptRow>> ReceiptsAsync(PaymentStatus status, int? page, Actor actor)
    {
        await ReadAccountingAsync(actor);
        var query = InitialDepositReceipts(status).AsNoTracking();
        query = status == PaymentStatus.Submitted ? query.OrderBy(p => p.SubmittedAt) : query.OrderByDescending(p => p.ReviewedAt);
        var rows = query.Select(p => new ReceiptRow(
            p.Id, p.Booking.Code, p.Booking.Status, p.Booking.ContactName, p.Booking.ContactPhone, p.Booking.Homestay.Name,
            p.Amount, p.Booking.DepositAmount - p.Booking.DiscountAmount, p.TransferContent, p.Status, p.SubmittedAt,
            p.Booking.HoldExpiresAt,
            p.Booking.Payments.Count(o => o.Id != p.Id && o.Type == PaymentType.Deposit && o.Method == PaymentMethod.ManualReceipt && o.Status == PaymentStatus.Rejected),
            p.ReviewedByEmployee == null ? null : p.ReviewedByEmployee.Person.FullName ?? p.ReviewedByEmployee.EmployeeCode,
            p.ReviewedAt, p.RejectReason, p.Booking.SettlementItem == null ? null : (int?)p.Booking.SettlementItem.SettlementId));
        return await PaginatedList<ReceiptRow>.CreateAsync(rows, page);
    }

    public async Task<PaginatedList<RefundRow>> RefundsAsync(bool done, string? q, int? page, Actor actor)
    {
        await ReadAccountingAsync(actor);
        var query = done
            ? _db.Bookings.AsNoTracking().Where(b => b.Payments.Any(p => p.Type == PaymentType.Refund))
            : PendingRefunds().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(b => b.Code.Contains(term) || b.ContactName.Contains(term) || (phone != null && b.ContactPhone == phone));
        }
        query = done
            ? query.OrderByDescending(b => b.Payments.Where(p => p.Type == PaymentType.Refund).Max(p => p.ReviewedAt))
            : query.OrderBy(b => b.CancelledAt);

        var rows = query.Select(b => new RefundRow(
            b.Id, b.Code, b.Status, b.ContactName, b.ContactPhone, b.Homestay.Name,
            b.DepositAmount - b.DiscountAmount, b.RefundAmount ?? 0, b.CancelledAt, b.CancelReason,
            b.Payments.Where(p => p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Approved).Select(p => p.TransferContent).FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund).Select(p => p.BankRef).FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund)
                .Select(p => p.ReviewedByEmployee == null ? null : p.ReviewedByEmployee.Person.FullName ?? p.ReviewedByEmployee.EmployeeCode)
                .FirstOrDefault(),
            b.Payments.Where(p => p.Type == PaymentType.Refund).Select(p => p.ReviewedAt).FirstOrDefault(), b.SettlementItem == null ? null : (int?)b.SettlementItem.SettlementId));
        return await PaginatedList<RefundRow>.CreateAsync(rows, page);
    }

    public async Task<PaginatedList<BankTransactionRow>> TransactionsAsync(BankMatchStatus? status, bool all, string? q, int? page, Actor actor)
    {
        await ReadAccountingAsync(actor);
        var query = _db.BankTransactions.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(t => t.MatchStatus == status);
        }
        else if (!all)
        {
            query = query.Where(t => AccountingService.OpenStatuses.Contains(t.MatchStatus));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var code = BankMatchingService.ExtractBookingCode(term);
            query = query.Where(t => t.TransferContent!.Contains(term) || t.ProviderTransactionId.Contains(term)
                                     || (code != null && t.MatchedBooking!.Code == code));
        }
        return await PaginatedList<BankTransactionRow>.CreateAsync(TransactionRows(query.OrderByDescending(t => t.TransactionTime)), page);
    }

    public async Task<BankTransactionRow?> TransactionAsync(int id, Actor actor)
    {
        await ReadAccountingAsync(actor);
        return await TransactionRows(_db.BankTransactions.AsNoTracking().Where(t => t.Id == id)).SingleOrDefaultAsync();
    }

    public async Task<PaginatedList<Payment>> AdditionalDepositsAsync(int? page, Actor actor)
    {
        await ReadAccountingAsync(actor);
        var query = PendingAdditionalDeposits().AsNoTracking().Include(p => p.Booking).ThenInclude(b => b.Homestay)
            .Include(p => p.Booking).ThenInclude(b => b.SettlementItem).Include(p => p.BankTransaction).Include(p => p.ChangeRequest);
        return await PaginatedList<Payment>.CreateAsync(query, page);
    }

    private static IQueryable<BankTransactionRow> TransactionRows(IQueryable<BankTransaction> query) =>
        query.Select(t => new BankTransactionRow(
            t.Id, t.Provider, t.ProviderTransactionId, t.Amount, t.TransferContent, t.TransactionTime, t.MatchStatus,
            t.MatchedBooking == null ? null : t.MatchedBooking.Code,
            t.MatchedBooking == null ? null : t.MatchedBooking.Status,
            t.MatchedBooking == null ? null : t.MatchedBooking.DepositAmount - t.MatchedBooking.DiscountAmount,
            t.Note,
            t.ProcessedByEmployee == null ? null : t.ProcessedByEmployee.Person.FullName ?? t.ProcessedByEmployee.EmployeeCode,
            t.ProcessedAt, t.RefundBankRef, t.MatchedBooking == null || t.MatchedBooking.SettlementItem == null ? null : (int?)t.MatchedBooking.SettlementItem.SettlementId));
}
