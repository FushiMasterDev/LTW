using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class AccountingServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();
    private readonly Employee _accountant;
    private readonly Employee _consultant;
    private int _txSeq;

    public AccountingServiceTests()
    {
        var person = new PersonProfile { FullName = "Kế toán", PhoneNumber = "+84901000021" };
        _host.Db.Users.Add(new ApplicationUser { Person = person, Status = AccountStatus.Active });
        _accountant = new Employee { Person = person, EmployeeCode = "KT001", Position = EmployeePosition.Accountant };
        _host.Db.Employees.Add(_accountant);
        _host.Db.SaveChanges();
        _consultant = _host.Db.Employees.Single(e => e.EmployeeCode == "NV001");
        _host.Db.ChangeTracker.Clear();
    }

    public void Dispose() => _host.Dispose();

    private Task<Booking> NewBookingAsync() =>
        _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)));

    /// <summary>Tiền vào qua webhook; trả về Id giao dịch đã lưu.</summary>
    private async Task<int> IncomingAsync(decimal amount, string content)
    {
        var id = $"TX{++_txSeq}";
        await _host.Matching().ProcessAsync(new IncomingBankTransaction(BankProvider.SePay, id, BookingTestHost.FujiAccount, amount, content,
            _host.Clock.GetUtcNow().UtcDateTime));
        _host.Db.ChangeTracker.Clear();
        return await _host.Db.BankTransactions.Where(t => t.ProviderTransactionId == id).Select(t => t.Id).SingleAsync();
    }

    private Task<BankTransaction> TxAsync(int id) => _host.Db.BankTransactions.AsNoTracking().SingleAsync(t => t.Id == id);

    private Task<Booking> LoadAsync(int id) =>
        _host.Db.Bookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == id);

    [Fact]
    public async Task Match_UnmatchedTransaction_DepositsBookingAndRecordsAccountant()
    {
        var booking = await NewBookingAsync();
        var txId = await IncomingAsync(booking.AmountDue, "chuyen tien coc homestay"); // khách quên ghi mã đơn

        await _host.Accounting().MatchAsync(txId, booking.Code.Replace("-", "").ToLowerInvariant(), _accountant.Id, null);

        var saved = await LoadAsync(booking.Id);
        Assert.Equal(BookingStatus.Deposited, saved.Status);
        var payment = Assert.Single(saved.Payments);
        Assert.Equal((PaymentStatus.Approved, txId, _accountant.Id), (payment.Status, payment.BankTransactionId, payment.ReviewedByEmployeeId));
        var tx = await TxAsync(txId);
        Assert.Equal(BankMatchStatus.ManuallyMatched, tx.MatchStatus);
        Assert.Equal((booking.Id, _accountant.Id), (tx.MatchedBookingId!.Value, tx.ProcessedByEmployeeId!.Value));
    }

    [Fact]
    public async Task Match_BookingWithPendingReceipt_ApprovesThatReceipt()
    {
        var booking = await NewBookingAsync();
        await _host.Receipts().SubmitAsync(booking.Id, "a.jpg", null, new Actor(ActorType.Customer));
        var txId = await IncomingAsync(booking.AmountDue + 20_000, "ck homestay");
        _host.Db.ChangeTracker.Clear();

        await _host.Accounting().MatchAsync(txId, booking.Code, _accountant.Id, null);

        var payment = Assert.Single((await LoadAsync(booking.Id)).Payments);
        Assert.Equal(PaymentMethod.ManualReceipt, payment.Method);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Contains("Chuyển dư 20.000", (await TxAsync(txId)).Note);
    }

    [Fact]
    public async Task Match_NotEnoughMoney_IsRefused()
    {
        var booking = await NewBookingAsync();
        var txId = await IncomingAsync(booking.AmountDue - 50_000, booking.Code); // tự đánh dấu Underpaid

        var ex = await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().MatchAsync(txId, booking.Code, _accountant.Id, null));

        Assert.Contains("chưa đủ cọc", ex.Message);
        Assert.Equal(BankMatchStatus.Underpaid, (await TxAsync(txId)).MatchStatus);
    }

    [Fact]
    public async Task Match_ExpiredBooking_IsRefused()
    {
        var booking = await NewBookingAsync();
        await _host.Bookings().ChangeStatusAsync(booking, BookingStatus.Expired, Actor.System);
        var txId = await IncomingAsync(booking.AmountDue, booking.Code); // LatePayment

        var ex = await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().MatchAsync(txId, booking.Code, _accountant.Id, null));

        Assert.Contains("hoàn tiền", ex.Message);
    }

    [Fact]
    public async Task Consultant_CannotProcessTransactions()
    {
        var txId = await IncomingAsync(300_000, "khong ro");

        await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().RefundTransactionAsync(txId, _consultant.Id, "FT1", null));
    }

    [Fact]
    public async Task RefundTransaction_RecordsRefundAndCannotBeProcessedAgain()
    {
        var txId = await IncomingAsync(300_000, "khong ro");

        await _host.Accounting().RefundTransactionAsync(txId, _accountant.Id, " FT2611100001 ", "Chuyển nhầm");

        var tx = await TxAsync(txId);
        Assert.Equal(BankMatchStatus.Refunded, tx.MatchStatus);
        Assert.Equal("FT2611100001", tx.RefundBankRef);
        Assert.NotNull(tx.RefundedAt);
        _host.Db.ChangeTracker.Clear();
        var again = await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().IgnoreTransactionAsync(txId, _accountant.Id, "x"));
        Assert.Contains("đã được xử lý", again.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RefundAndIgnore_RequireReferenceOrReason(string text)
    {
        var txId = await IncomingAsync(300_000, "khong ro");

        await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().RefundTransactionAsync(txId, _accountant.Id, text, null));
        await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().IgnoreTransactionAsync(txId, _accountant.Id, text));
    }

    [Fact]
    public async Task TwoAccountantsSameTransaction_OnlyFirstSucceeds()
    {
        var txId = await IncomingAsync(300_000, "khong ro");
        using var db1 = _host.NewContext();
        using var db2 = _host.NewContext();
        var first = _host.Accounting(db1);
        var second = _host.Accounting(db2);
        // Cả hai đã đọc giao dịch còn mở trước khi người đầu lưu
        await db1.BankTransactions.SingleAsync(t => t.Id == txId);
        await db2.BankTransactions.SingleAsync(t => t.Id == txId);

        await first.RefundTransactionAsync(txId, _accountant.Id, "FT1", null);
        var ex = await Assert.ThrowsAsync<BookingException>(() => second.IgnoreTransactionAsync(txId, _accountant.Id, "Không liên quan"));

        Assert.Contains("người khác", ex.Message);
        Assert.Equal(BankMatchStatus.Refunded, (await TxAsync(txId)).MatchStatus);
    }

    /// <summary>Đơn đã cọc qua QR rồi khách hủy sớm (≥ 7 ngày) → hoàn 100% tiền đã chuyển, chờ kế toán.</summary>
    private async Task<Booking> CancelledWithRefundAsync()
    {
        var booking = await NewBookingAsync();
        await IncomingAsync(booking.AmountDue, booking.Code);
        var tracked = await _host.Db.Bookings.SingleAsync(b => b.Id == booking.Id);
        await _host.Bookings().ChangeStatusAsync(tracked, BookingStatus.Cancelled, new Actor(ActorType.Customer), "Đổi lịch");
        _host.Db.ChangeTracker.Clear();
        return tracked;
    }

    [Fact]
    public async Task RecordRefund_AddsRefundPaymentAndLeavesQueue()
    {
        var booking = await CancelledWithRefundAsync();
        Assert.Equal(1, await _host.Accounting().PendingRefunds().CountAsync());

        var payment = await _host.Accounting().RecordRefundAsync(booking.Id, _accountant.Id, "FT2611100002");

        Assert.Equal((PaymentType.Refund, PaymentMethod.ManualTransfer, booking.RefundAmount!.Value), (payment.Type, payment.Method, payment.Amount));
        Assert.Equal("FT2611100002", payment.BankRef);
        Assert.Equal(0, await _host.Accounting().PendingRefunds().CountAsync());
        Assert.True(await _host.Db.Notifications.AnyAsync(n => n.Message.Contains("đã chuyển hoàn")));
        _host.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BookingException>(() => _host.Accounting().RecordRefundAsync(booking.Id, _accountant.Id, "FT3"));
    }

    [Fact]
    public async Task TwoAccountantsRecordSameRefund_OnlyOneRefundPayment()
    {
        var booking = await CancelledWithRefundAsync();
        using var db1 = _host.NewContext();
        using var db2 = _host.NewContext();
        var first = _host.Accounting(db1);
        var second = _host.Accounting(db2);
        // Người thứ hai đã mở trang khi đơn còn trong hàng chờ: chèn khoản hoàn trực tiếp để mô phỏng hai lần lưu cùng lúc
        await first.RecordRefundAsync(booking.Id, _accountant.Id, "FT1");
        db2.Payments.Add(new Payment
        {
            BookingId = booking.Id, Type = PaymentType.Refund, Method = PaymentMethod.ManualTransfer, Amount = 1, Status = PaymentStatus.Approved,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        Assert.Equal(1, await _host.Db.Payments.CountAsync(p => p.BookingId == booking.Id && p.Type == PaymentType.Refund));
    }
}
