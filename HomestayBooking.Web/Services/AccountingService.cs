using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Việc của kế toán ngoài duyệt bill (ReceiptReviewService): xử lý giao dịch ngân hàng không tự khớp được
/// và ghi nhận Fuji đã chuyển hoàn cọc (fuji_homestay_database_v1 mục 5.F, 9).
/// </summary>
public class AccountingService
{
    /// <summary>Giao dịch còn chờ kế toán: chuyển thiếu, đến muộn, không đọc được mã đơn.</summary>
    public static readonly BankMatchStatus[] OpenStatuses = [BankMatchStatus.Underpaid, BankMatchStatus.LatePayment, BankMatchStatus.Unmatched];

    private static readonly EmployeePosition[] Handlers = [EmployeePosition.Accountant, EmployeePosition.Manager];

    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;
    private readonly NotificationService _notifications;
    private readonly TimeProvider _clock;

    public AccountingService(ApplicationDbContext db, BookingService bookings, NotificationService notifications, TimeProvider clock)
    {
        _db = db;
        _bookings = bookings;
        _notifications = notifications;
        _clock = clock;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    /// <summary>Cọc bổ sung chờ Accountant, kể cả khi đã qua hạn khách nộp.</summary>
    public IQueryable<Payment> PendingAdditionalDeposits() => _db.Payments.Where(p =>
        p.Type == PaymentType.AdditionalDeposit && p.Status == PaymentStatus.Submitted
        && p.ChangeRequest!.Status == ChangeRequestStatus.AwaitingPayment).OrderBy(p => p.SubmittedAt);

    /// <summary>Bill cọc đầu, tách khỏi các khoản đổi đơn.</summary>
    public IQueryable<Payment> InitialDepositReceipts(PaymentStatus status = PaymentStatus.Submitted) =>
        _db.Payments.Where(p => p.Type == PaymentType.Deposit && p.Method == PaymentMethod.ManualReceipt && p.Status == status);

    /// <summary>Đơn đã chốt tiền hoàn nhưng Fuji chưa chuyển (chưa có khoản Refund).</summary>
    public IQueryable<Booking> PendingRefunds() =>
        _db.Bookings.Where(b => b.RefundAmount > 0 && !b.Payments.Any(p => p.Type == PaymentType.Refund));

    /// <summary>
    /// Khớp tay giao dịch với một đơn còn chờ thanh toán và đủ tiền: duyệt cọc, đơn → Deposited.
    /// Đơn đã hết hạn / hủy, hoặc chuyển thiếu → không khớp được, kế toán hoàn tiền cho khách.
    /// </summary>
    public async Task<Booking> MatchAsync(int transactionId, string bookingCode, int employeeId, string? note)
    {
        var handler = await HandlerAsync(employeeId);
        var tx = await OpenTransactionAsync(transactionId);
        var code = BankMatchingService.ExtractBookingCode(bookingCode) ?? bookingCode.Trim().ToUpperInvariant();
        var booking = await _db.Bookings.Include(b => b.Payments).SingleOrDefaultAsync(b => b.Code == code)
            ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        if (booking.Status is not (BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted))
        {
            throw new BookingException("Đơn này không còn chờ thanh toán, hãy hoàn tiền cho khách.");
        }
        if (tx.Amount < booking.AmountDue)
        {
            throw new BookingException("Số tiền chưa đủ cọc, hãy hoàn tiền hoặc đợi khách chuyển đủ.");
        }

        if (await _db.Payments.AnyAsync(p => p.BankTransactionId == tx.Id))
            throw new BookingException("Giao dịch đã gắn khoản thanh toán, hãy đối chiếu và hoàn theo giao dịch.");
        var now = UtcNow;
        tx.MatchedBooking = booking;
        tx.MatchStatus = BankMatchStatus.ManuallyMatched;
        Process(tx, handler.EmployeeId!.Value, now, note ?? (tx.Amount > booking.AmountDue ? $"Chuyển dư {tx.Amount - booking.AmountDue:N0}đ." : null));
        BankMatchingService.AttachApprovedDeposit(booking, tx, now, handler.EmployeeId);
        await _bookings.ApplyStatusAsync(booking, BookingStatus.Deposited, handler,
            $"Kế toán khớp tay giao dịch {tx.ProviderTransactionId}.");
        await SaveAsync();
        return booking;
    }

    /// <summary>Fuji chuyển trả khoản tiền (chuyển thiếu, đến muộn, không rõ đơn) cho người gửi.</summary>
    public async Task RefundTransactionAsync(int transactionId, int employeeId, string bankRef, string? note)
    {
        var handler = await HandlerAsync(employeeId);
        var tx = await OpenTransactionAsync(transactionId);
        var now = UtcNow;
        tx.MatchStatus = BankMatchStatus.Refunded;
        tx.RefundedAt = now;
        tx.RefundBankRef = RequireBankRef(bankRef);
        Process(tx, handler.EmployeeId!.Value, now, note);
        await SaveAsync();
    }

    /// <summary>Tiền vào không liên quan đặt phòng (ví dụ khoản thu khác của Fuji) — bắt buộc ghi lý do.</summary>
    public async Task IgnoreTransactionAsync(int transactionId, int employeeId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new BookingException("Vui lòng ghi lý do bỏ qua giao dịch.");
        }
        var handler = await HandlerAsync(employeeId);
        var tx = await OpenTransactionAsync(transactionId);
        tx.MatchStatus = BankMatchStatus.Ignored;
        Process(tx, handler.EmployeeId!.Value, UtcNow, note);
        await SaveAsync();
    }

    /// <summary>Ghi nhận Fuji đã chuyển hoàn cọc cho khách (đơn hủy / bị từ chối) và báo khách.</summary>
    public Task<Payment> RecordRefundAsync(int bookingId, int employeeId, string bankRef) => HomestayWrite.RunAsync(_db, async () =>
    {
        var handler = await HandlerAsync(employeeId);
        await LockTransferBookingAsync(bookingId);
        var booking = await PendingRefunds().Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new BookingException("Đơn này không có khoản hoàn nào đang chờ.");
        var now = UtcNow;
        var payment = new Payment
        {
            Type = PaymentType.Refund,
            Method = PaymentMethod.ManualTransfer,
            Amount = booking.RefundAmount!.Value,
            TransferContent = $"Fuji hoan coc {booking.Code}",
            Status = PaymentStatus.Submitted,
            SubmittedByAccountId = handler.AccountId,
            SubmittedAt = now,
            ReviewedByEmployeeId = handler.EmployeeId,
            ReviewedAt = now,
        };
        booking.Payments.Add(payment);
        await RecordTransferInTransactionAsync(payment, handler, bankRef);
        PartnerServiceSupport.Notify(_db, _notifications, _clock, [booking.Customer.Person.Account?.Id],
            $"Fuji đã chuyển hoàn {payment.Amount:N0}đ cho đơn {booking.Code}. Tiền thường về tài khoản trong vòng 24 giờ.", $"/Bookings/Details/{booking.Code}");
        return payment;
    });

    // Dùng chung cho hoàn cọc và chuyển riêng khoản bù; caller đã kiểm quyền, điều kiện và mở transaction.
    internal async Task<Payment> RecordTransferInTransactionAsync(Payment payment, Actor handler, string bankRef)
    {
        await PartnerServiceSupport.EmployeeAsync(_db, handler, Handlers);
        var inserted = payment.Id == 0;
        var old = inserted ? null : TransferState(payment);
        payment.BankRef = RequireTransferReference(bankRef);
        payment.Status = PaymentStatus.Approved;
        payment.ReviewedByEmployeeId = handler.EmployeeId; payment.ReviewedAt = UtcNow;
        if (inserted) await _db.SaveChangesAsync();
        PartnerServiceSupport.Audit(_db, _clock, handler, "Payments", payment.Id.ToString(), old, TransferState(payment), inserted ? AuditAction.Insert : AuditAction.Update);
        return payment;
    }

    internal static object TransferState(Payment p) => new { p.BookingId, p.Type, p.Method, p.Amount, p.Status, p.BankRef,
        p.ReviewedByEmployeeId, p.ReviewedAt, p.RejectReason };

    internal static string RequireTransferReference(string? bankRef) => string.IsNullOrWhiteSpace(bankRef) || bankRef.Trim().Length > 100
        ? throw new BookingException("Vui lòng nhập mã giao dịch chuyển tiền từ 1 đến 100 ký tự.") : bankRef.Trim();

    internal async Task LockTransferBookingAsync(int id)
    {
        if (_db.Database.IsRelational())
        {
            var homeId = await _db.Bookings.Where(b => b.Id == id).Select(b => (int?)b.HomestayId).SingleOrDefaultAsync();
            // Cùng thứ tự với đối soát: homestay trước, đơn sau; không giữ khóa ngược khi báo chủ.
            if (homeId is int home) await _db.Homestays.Where(h => h.Id == home)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.UpdatedAt, h => h.UpdatedAt));
            await _db.Bookings.Where(b => b.Id == id).ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, b => b.Status));
        }
    }

    private static string RequireBankRef(string? bankRef) =>
        string.IsNullOrWhiteSpace(bankRef) ? throw new BookingException("Vui lòng nhập mã giao dịch chuyển hoàn.") : bankRef.Trim();

    private static void Process(BankTransaction tx, int employeeId, DateTime now, string? note)
    {
        tx.ProcessedByEmployeeId = employeeId;
        tx.ProcessedAt = now;
        if (!string.IsNullOrWhiteSpace(note))
        {
            tx.Note = string.IsNullOrEmpty(tx.Note) ? note.Trim() : $"{tx.Note} {note.Trim()}";
        }
    }

    private async Task<Actor> HandlerAsync(int employeeId)
    {
        var handler = await _db.Employees
            .Where(e => e.Id == employeeId && e.EmploymentStatus == EmploymentStatus.Active && Handlers.Contains(e.Position))
            .Select(e => new { e.Id, AccountId = e.Person.Account == null ? null : e.Person.Account.Id })
            .SingleOrDefaultAsync()
            ?? throw new BookingException("Chỉ kế toán hoặc quản lý được xử lý giao dịch.");
        return new Actor(ActorType.Employee, handler.AccountId, handler.Id);
    }

    private async Task<BankTransaction> OpenTransactionAsync(int transactionId)
    {
        var tx = await _db.BankTransactions.SingleOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new BookingException("Không tìm thấy giao dịch.");
        if (!OpenStatuses.Contains(tx.MatchStatus))
        {
            throw new BookingException("Giao dịch này đã được xử lý.");
        }
        return tx;
    }

    /// <summary>Người khác vừa xử lý cùng giao dịch / đơn (concurrency token, unique hoàn cọc) → báo tải lại.</summary>
    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            throw new BookingException("Giao dịch hoặc đơn vừa được người khác xử lý, vui lòng tải lại trang.");
        }
    }
}
