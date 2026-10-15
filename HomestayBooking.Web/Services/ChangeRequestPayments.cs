using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Cùng một định nghĩa khoản nộp đúng hạn cho kế toán và job hết hạn.</summary>
internal static class ChangeRequestPayments
{
    internal static IQueryable<Payment> Valid(ApplicationDbContext db) => db.Payments.Where(p =>
        p.Type == PaymentType.AdditionalDeposit && p.ChangeRequestId != null
        && (p.Status == PaymentStatus.Submitted || p.Status == PaymentStatus.Approved)
        && p.BookingId == p.ChangeRequest!.BookingId && p.Amount == p.ChangeRequest.AdditionalDeposit
        && ((p.Method == PaymentMethod.BankAutoMatch && p.BankTransactionId != null
            && (p.BankTransaction!.MatchStatus == BankMatchStatus.Matched || p.BankTransaction.MatchStatus == BankMatchStatus.ManuallyMatched)
            && p.BankTransaction.MatchedBookingId == p.BookingId && p.BankTransaction.Amount == p.Amount
            && p.BankTransaction.TransactionTime >= p.ChangeRequest.DecidedAt
            && p.BankTransaction.TransactionTime <= p.ChangeRequest.PaymentDeadlineAt)
        || (p.Method == PaymentMethod.ManualReceipt && p.ReceiptImageUrl != null && p.ReceiptImageUrl != ""
            && p.SubmittedAt >= p.ChangeRequest.DecidedAt && p.SubmittedAt <= p.ChangeRequest.PaymentDeadlineAt)));

    /// <summary>UPDATE cùng hàng với job hết hạn, trong transaction của bên nộp tiền.</summary>
    internal static async Task<bool> GuardSubmissionAsync(ApplicationDbContext db, int id, DateTime submittedAt)
    {
        var query = db.BookingChangeRequests.Where(r => r.Id == id && r.Status == ChangeRequestStatus.AwaitingPayment
            && r.DecidedAt <= submittedAt && r.PaymentDeadlineAt >= submittedAt && r.AdditionalDeposit > 0);
        if (db.Database.IsRelational())
            return await query.ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, r => r.Status)) == 1;
        var request = await query.SingleOrDefaultAsync();
        if (request is null) return false;
        db.Entry(request).Property(r => r.Status).IsModified = true;
        return true;
    }
}
