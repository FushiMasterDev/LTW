using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed partial class BookingChangeRequestServiceTests
{
    private static async Task<(Booking Booking, BookingChangeRequest Request, Employee Accountant, ClaimsPrincipal Principal)> Awaiting(Scenario s, bool upgrade = false)
    {
        var b = await s.Book(upgrade ? 10 : 4);
        var accountant = s.Staff(EmployeePosition.Accountant);
        var principal = Principal(accountant.Person.Account!.Id, AppRoles.Employee, EmployeePosition.Accountant);
        var input = upgrade ? new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeRoomType, NewRoomTypeId: s.Type(1_500_000).Id) : s.Dates(b);
        var request = await s.Service().SubmitAsync(input, s.Customer);
        await s.Service().ApproveAsync(request.Id, "Đồng ý nghiệp vụ", s.Manager);
        return (b, request, accountant, principal);
    }

    private static IncomingBankTransaction AdditionalTx(Scenario s, Booking b, BookingChangeRequest r, string id = "CR-TX", decimal? amount = null,
        DateTime? time = null, string? content = null) => new(BankProvider.SePay, id, BookingTestHost.FujiAccount,
            amount ?? r.AdditionalDeposit!.Value, content ?? $"CR{r.Id} {b.Code}", time ?? s.H.Clock.Now.UtcDateTime);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AdditionalDepositCompletesMoneyExamples(bool upgrade)
    {
        using var s = new Scenario(); var (b, r, accountant, principal) = await Awaiting(s, upgrade);
        var history = await s.Db.BookingStatusHistories.CountAsync(); var notifications = await s.Db.Notifications.CountAsync();
        var code = b.Code; var oldDates = b.CheckIn;
        var result = await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        Assert.Equal(BankMatchStatus.Matched, result.Status);
        var payment = await s.Db.Payments.SingleAsync();
        Assert.Equal(PaymentType.AdditionalDeposit, payment.Type); Assert.Equal(PaymentStatus.Submitted, payment.Status);
        Assert.Equal(PaymentMethod.BankAutoMatch, payment.Method); Assert.Equal(r.Id, payment.ChangeRequestId);
        Assert.Null(payment.ReviewedAt); Assert.Equal(BookingStatus.Confirmed, b.Status); Assert.Equal(oldDates, b.CheckIn);
        await s.Service().ConfirmAsync(r.Id, "Đã đối chiếu tiền", principal);
        s.Db.ChangeTracker.Clear(); b = await s.Db.Bookings.SingleAsync(); r = await s.Db.BookingChangeRequests.SingleAsync();
        payment = await s.Db.Payments.SingleAsync();
        Assert.Equal(ChangeRequestStatus.Applied, r.Status); Assert.Equal(code, b.Code);
        Assert.Equal(upgrade ? 900_000 : 600_000, b.DepositAmount); Assert.Equal(upgrade ? 0 : 300_000, b.ChangeFeeAmount);
        Assert.Equal(upgrade ? 3_000_000 : 2_000_000, b.TotalAmount);
        Assert.Equal(BookingStatus.Deposited, b.Status); Assert.Null(b.ConfirmedAt);
        Assert.Equal(accountant.Person.Account!.Id, r.FinanceConfirmedByAccountId); Assert.Equal("Đã đối chiếu tiền", r.FinanceNote);
        Assert.Equal(PaymentStatus.Approved, payment.Status); Assert.Equal(accountant.Id, payment.ReviewedByEmployeeId);
        Assert.Equal(s.H.Clock.Now.UtcDateTime, payment.ReviewedAt);
        Assert.Equal(0, await s.Db.RoomNights.CountAsync(n => n.BookingChangeRequestId != null));
        Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        Assert.Equal(b.TotalAmount, (await s.Db.BookingNights.ToListAsync()).Sum(n => n.Price));
        Assert.Equal(history + 1, await s.Db.BookingStatusHistories.CountAsync());
        Assert.Equal(notifications + 2, await s.Db.Notifications.CountAsync());
        Assert.Contains(await s.Db.AuditLogs.ToListAsync(), a => a.NewValues!.Contains("FinanceConfirmed"));
        var before = await FinanceSnapshot(s.Db);
        var ex = await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(r.Id, "Lần hai", principal));
        Assert.Equal(BookingChangeRequestService.Processed, ex.Message); Assert.Equal(before, await FinanceSnapshot(s.Db));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TimelySubmissionCanBeConfirmedNextDay(bool receipt)
    {
        using var s = new Scenario(); var (b, r, _, principal) = await Awaiting(s);
        s.H.Clock.Now = new DateTimeOffset(r.PaymentDeadlineAt!.Value, TimeSpan.Zero);
        if (receipt) await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        else await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        s.H.Clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(0, await s.Service().ExpireUnpaidAsync());
        Assert.Single(await s.H.Accounting().PendingAdditionalDeposits().ToListAsync());
        Assert.Empty(await s.H.Accounting().InitialDepositReceipts().ToListAsync());
        await s.Service().ConfirmAsync(r.Id, "Xác nhận sáng hôm sau", principal);
        Assert.Equal(ChangeRequestStatus.Applied, r.Status); Assert.True(r.FinanceConfirmedAt > r.PaymentDeadlineAt);
    }

    [Fact]
    public async Task ExpiryReleasesOnlyRequestHoldsAndNotifiesOnce()
    {
        using var s = new Scenario(); var (b, r, _, _) = await Awaiting(s);
        var audits = await s.Db.AuditLogs.CountAsync(); var notices = await s.Db.Notifications.CountAsync();
        s.H.Clock.Now = new DateTimeOffset(r.PaymentDeadlineAt!.Value, TimeSpan.Zero);
        Assert.Equal(0, await s.Service().ExpireUnpaidAsync());
        s.H.Clock.Advance(TimeSpan.FromTicks(1));
        Assert.Equal(1, await s.Service().ExpireUnpaidAsync()); Assert.Equal(0, await s.Service().ExpireUnpaidAsync());
        Assert.Equal(ChangeRequestStatus.Expired, (await s.Db.BookingChangeRequests.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == r.Id).ToListAsync());
        Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        Assert.Equal(BookingStatus.Confirmed, b.Status); Assert.Equal(600_000, b.DepositAmount);
        Assert.Equal(audits + 1, await s.Db.AuditLogs.CountAsync()); Assert.Equal(notices + 1, await s.Db.Notifications.CountAsync());
        var audit = await s.Db.AuditLogs.OrderBy(a => a.Id).LastAsync(); Assert.Null(audit.AccountId); Assert.Contains("System", audit.NewValues);
    }

    [Theory]
    [InlineData("under")] [InlineData("over")] [InlineData("wrong-booking")] [InlineData("missing-request")]
    [InlineData("malformed")] [InlineData("late")] [InlineData("early")] [InlineData("closed")]
    public async Task InvalidAdditionalWebhookIsPreservedUnmatched(string kind)
    {
        using var s = new Scenario(); var (b, r, _, _) = await Awaiting(s);
        var amount = r.AdditionalDeposit!.Value + (kind == "under" ? -1 : kind == "over" ? 1 : 0);
        var content = kind switch { "wrong-booking" => $"CR{r.Id} FJ261110-999", "missing-request" => $"CR999999 {b.Code}",
            "malformed" => $"CR2147483648 {b.Code}", _ => $"CR{r.Id} {b.Code}" };
        var time = kind == "early" ? r.DecidedAt!.Value.AddTicks(-1) : kind == "late" ? r.PaymentDeadlineAt!.Value.AddTicks(1) : s.H.Clock.Now.UtcDateTime;
        if (kind == "closed") { s.H.Clock.Advance(TimeSpan.FromMinutes(16)); await s.Service().ExpireUnpaidAsync(); }
        var tx = AdditionalTx(s, b, r, amount: amount, time: time, content: content);
        var result = await s.H.Matching().ProcessAsync(tx);
        Assert.Equal(BankMatchStatus.Unmatched, result.Status); Assert.Empty(await s.Db.Payments.ToListAsync());
        var saved = await s.Db.BankTransactions.SingleAsync(); Assert.Contains("CR", saved.Note); Assert.Equal(amount, saved.Amount);
        Assert.Equal(BookingStatus.Confirmed, b.Status);
        Assert.True((await s.H.Matching().ProcessAsync(tx)).Duplicate); Assert.Equal(1, await s.Db.BankTransactions.CountAsync());
    }

    [Fact]
    public async Task WebhookAndBillReferToOneSubmittedPaymentAndNeverApproveAutomatically()
    {
        using var s = new Scenario(); var (b, r, _, _) = await Awaiting(s);
        var bill = await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/change.jpg", null, s.Customer);
        var tx = AdditionalTx(s, b, r);
        Assert.False((await s.H.Matching().ProcessAsync(tx)).Duplicate);
        Assert.True((await s.H.Matching().ProcessAsync(tx)).Duplicate);
        var payment = await s.Db.Payments.SingleAsync(); Assert.Equal(bill.Id, payment.Id); Assert.Equal(PaymentStatus.Submitted, payment.Status);
        Assert.NotNull(payment.BankTransactionId); Assert.Null(payment.ReviewedAt); Assert.Equal(PaymentMethod.ManualReceipt, payment.Method);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r, "EXTRA"));
        Assert.Equal(1, await s.Db.Payments.CountAsync()); Assert.Equal(1, await s.Db.BankTransactions.CountAsync(t => t.MatchStatus == BankMatchStatus.Unmatched));
        Assert.Equal(ChangeRequestStatus.AwaitingPayment, r.Status); Assert.Equal(BookingStatus.Confirmed, b.Status);
    }

    [Theory]
    [InlineData("other-customer")] [InlineData("manager")] [InlineData("no-image")] [InlineData("late")] [InlineData("duplicate")]
    public async Task InvalidAdditionalReceiptIsRejected(string kind)
    {
        using var s = new Scenario(); var (_, r, _, _) = await Awaiting(s);
        var principal = kind == "manager" ? s.Manager : kind == "other-customer"
            ? Principal(s.Manager.FindFirstValue(ClaimTypes.NameIdentifier)!, AppRoles.Customer) : s.Customer;
        if (kind == "late") s.H.Clock.Advance(TimeSpan.FromMinutes(16));
        if (kind == "duplicate") await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/first.jpg", null, s.Customer);
        var before = await FinanceSnapshot(s.Db);
        await Assert.ThrowsAsync<BookingException>(() => s.H.Receipts().SubmitChangeRequestAsync(r.Id, kind == "no-image" ? " " : "receipts/cr.jpg", null, principal));
        Assert.Equal(before, await FinanceSnapshot(s.Db));
    }

    [Theory]
    [InlineData(EmployeePosition.Manager)] [InlineData(EmployeePosition.Consultant)] [InlineData(EmployeePosition.Administrator)]
    public async Task OnlyAccountantCanConfirmOrRejectFinance(EmployeePosition position)
    {
        using var s = new Scenario(); var (b, r, _, _) = await Awaiting(s);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        var employee = s.Staff(position); var principal = Principal(employee.Person.Account!.Id, AppRoles.Employee, position);
        await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(r.Id, "Ghi chú", principal));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().RejectAsync(r.Id, "Lý do", principal));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task AccountantCannotBeSenderOrBusinessApprover(bool sender)
    {
        using var s = new Scenario(); var b = await s.Book(4); var employee = s.Staff(EmployeePosition.Accountant);
        var principal = Principal(employee.Person.Account!.Id, AppRoles.Employee, EmployeePosition.Accountant);
        var request = await s.Service().SubmitAsync(s.Dates(b), sender ? principal : s.Customer);
        if (!sender)
        {
            employee.Position = EmployeePosition.Manager; await s.Db.SaveChangesAsync();
            await s.Service().ApproveAsync(request.Id, "Duyệt", Principal(employee.Person.Account.Id, AppRoles.Employee, EmployeePosition.Manager));
            employee.Position = EmployeePosition.Accountant; await s.Db.SaveChangesAsync();
        }
        else await s.Service().ApproveAsync(request.Id, "Duyệt", s.Manager);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, request));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(request.Id, "Xác nhận", principal));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().RejectAsync(request.Id, "Từ chối", principal));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FinanceRejectionReturnsBankTransactionToExistingRefundQueue(bool changedPrice)
    {
        using var s = new Scenario(); var (b, r, accountant, principal) = await Awaiting(s);
        var managerNote = r.DecisionNote;
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        var originalRefund = b.RefundAmount;
        if (changedPrice)
        {
            foreach (var price in await s.Db.RoomTypePrices.ToListAsync()) price.Price += 100_000;
            await s.Db.SaveChangesAsync(); await s.Service().ConfirmAsync(r.Id, "Đối chiếu", principal);
        }
        else await s.Service().RejectAsync(r.Id, "Không thể áp dụng", principal);
        s.Db.ChangeTracker.Clear(); r = await s.Db.BookingChangeRequests.SingleAsync();
        var payment = await s.Db.Payments.Include(p => p.BankTransaction).SingleAsync(); var tx = payment.BankTransaction!;
        Assert.Equal(ChangeRequestStatus.Rejected, r.Status); Assert.Equal(PaymentStatus.Rejected, payment.Status);
        Assert.Equal(managerNote, r.DecisionNote); Assert.Equal(r.RejectionReason, payment.RejectReason);
        if (changedPrice) Assert.Equal(BookingChangeRequestService.PriceChanged, r.RejectionReason);
        Assert.Equal(accountant.Id, payment.ReviewedByEmployeeId); Assert.NotNull(payment.ReviewedAt);
        Assert.Equal(BankMatchStatus.Unmatched, tx.MatchStatus); Assert.Contains($"CR{r.Id}", tx.Note); Assert.Contains(r.RejectionReason!, tx.Note);
        Assert.Empty(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == r.Id).ToListAsync());
        Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        Assert.Null(r.FinanceConfirmedAt); Assert.Null(r.FinanceConfirmedByAccountId);
        Assert.Equal(originalRefund, (await s.Db.Bookings.SingleAsync()).RefundAmount);
        await s.H.Accounting().RefundTransactionAsync(tx.Id, accountant.Id, "HOAN-CR", "Hoàn cọc bổ sung");
        Assert.Equal(BankMatchStatus.Refunded, tx.MatchStatus); Assert.NotNull(tx.RefundedAt); Assert.Equal("HOAN-CR", tx.RefundBankRef);
        Assert.Single(await s.Db.Payments.ToListAsync()); Assert.Equal(tx.Id, payment.BankTransactionId);
        Assert.Empty(await s.Db.Payments.Where(p => p.Type == PaymentType.Refund).ToListAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FinanceRejectionPreservesManagersApprovalNote(bool bankTransfer)
    {
        using var s = new Scenario(); var (b, r, _, principal) = await Awaiting(s);
        var managerNote = r.DecisionNote;
        var managerId = r.DecidedByAccountId;
        var decidedAt = r.DecidedAt;
        Assert.Equal("Đồng ý nghiệp vụ", managerNote);
        if (bankTransfer) await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        else await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        await s.Service().RejectAsync(r.Id, "Không chấp nhận khoản nộp", principal);
        s.Db.ChangeTracker.Clear();
        var saved = await s.Db.BookingChangeRequests.SingleAsync();
        Assert.Equal(ChangeRequestStatus.Rejected, saved.Status);
        Assert.Equal(managerNote, saved.DecisionNote);
        Assert.Equal(managerId, saved.DecidedByAccountId); Assert.Equal(decidedAt, saved.DecidedAt);
        Assert.Equal("Không chấp nhận khoản nộp", saved.RejectionReason);
        Assert.Equal(saved.RejectionReason, (await s.Db.Payments.SingleAsync()).RejectReason);
    }

    [Fact]
    public async Task RejectedReceiptAloneCreatesNeitherTransactionNorRefundAndLaterWebhookIsUnmatched()
    {
        using var s = new Scenario(); var (b, r, _, principal) = await Awaiting(s);
        await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        await s.Service().RejectAsync(r.Id, "Bill không hợp lệ", principal);
        Assert.Equal(PaymentStatus.Rejected, (await s.Db.Payments.SingleAsync()).Status);
        Assert.Empty(await s.Db.BankTransactions.ToListAsync()); Assert.Single(await s.Db.Payments.ToListAsync()); Assert.Null(b.RefundAmount);
        var result = await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        Assert.Equal(BankMatchStatus.Unmatched, result.Status); Assert.Contains($"CR{r.Id}", (await s.Db.BankTransactions.SingleAsync()).Note);
        Assert.Single(await s.Db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData("confirm")] [InlineData("reject")] [InlineData("expire")] [InlineData("receipt")]
    public async Task FinanceFailureAfterDatabaseWritesRollsBackEverything(string action)
    {
        using var s = new Scenario(); var (b, r, _, principal) = await Awaiting(s);
        if (action is "confirm" or "reject") await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        if (action == "expire") s.H.Clock.Advance(TimeSpan.FromMinutes(16));
        var before = await FinanceSnapshot(s.Db); var failure = new FailAfterWrite();
        await using var db = s.H.NewContext(failure);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            if (action == "confirm") await s.Service(db).ConfirmAsync(r.Id, "Xác nhận", principal);
            else if (action == "reject") await s.Service(db).RejectAsync(r.Id, "Từ chối", principal);
            else if (action == "expire") await s.Service(db).ExpireUnpaidAsync();
            else await s.H.Receipts(db).SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        });
        Assert.True(failure.Reached); Assert.Equal(before, await FinanceSnapshot(s.Db));
    }

    [Fact]
    public async Task VoucherCancellationDoesNotDependOnAuditHistory()
    {
        using var s = new Scenario(); var b = await s.Book(4); await VoucherAsync(s, b);
        var request = await s.Service().SubmitAsync(new(b.Id, ChangeRequestType.Cancel), s.Consultant);
        // Giả lập audit không chứa tỷ lệ: nguồn tính tiền vẫn là báo giá chốt trên yêu cầu.
        foreach (var audit in await s.Db.AuditLogs.ToListAsync()) audit.NewValues = "{}";
        await s.Db.SaveChangesAsync(); s.H.Clock.Advance(TimeSpan.FromDays(3));
        await s.Service().ApproveAsync(request.Id, "Duyệt hủy", s.Manager);
        Assert.Equal(280_000, b.RefundAmount); Assert.Equal(80_000, (await s.Db.Vouchers.SingleAsync()).Balance);
    }

    [Fact]
    public async Task AdditionalQrUsesRequestIdBookingCodeAndExactAmount()
    {
        using var s = new Scenario(); var (b, r, _, _) = await Awaiting(s);
        var url = await s.Service().GetPaymentQrAsync(r.Id, s.Customer, new VietQrService(Options.Create(s.H.Bank)));
        Assert.Contains("amount=300000", url); Assert.Contains(Uri.EscapeDataString($"CR{r.Id} {b.Code}"), url);
        await Assert.ThrowsAsync<BookingException>(() => s.Service().GetPaymentQrAsync(r.Id, s.Manager, new VietQrService(Options.Create(s.H.Bank))));
    }


    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ConcurrentWebhookAndExpiryHaveOneWinner(bool startWebhookFirst)
    {
        var path = Path.Combine(Path.GetTempPath(), "booking-finance-" + Guid.NewGuid().ToString("N") + ".sqlite");
        using var s = new Scenario($"DataSource={path};Default Timeout=10");
        var (booking, request, _, _) = await Awaiting(s);
        var tx = AdditionalTx(s, booking, request, time: request.PaymentDeadlineAt!.Value);
        s.H.Clock.Now = new DateTimeOffset(request.PaymentDeadlineAt.Value.AddTicks(1), TimeSpan.Zero);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<BankMatchResult> Submit()
        {
            await gate.Task;
            await using var db = s.H.NewIndependentContext();
            return await s.H.Matching(db).ProcessAsync(tx);
        }
        async Task<int> Expire()
        {
            await gate.Task;
            await using var db = s.H.NewIndependentContext();
            return await s.Service(db).ExpireUnpaidAsync();
        }
        Task<BankMatchResult> submit; Task<int> expire;
        if (startWebhookFirst) { submit = Task.Run(Submit); expire = Task.Run(Expire); }
        else { expire = Task.Run(Expire); submit = Task.Run(Submit); }
        gate.SetResult(); await Task.WhenAll(submit, expire);
        var submitResult = await submit; var expiredCount = await expire;
        s.Db.ChangeTracker.Clear();
        var saved = await s.Db.BookingChangeRequests.SingleAsync();
        if (saved.Status == ChangeRequestStatus.Expired)
        {
            Assert.Equal(1, expiredCount); Assert.Equal(BankMatchStatus.Unmatched, submitResult.Status);
            Assert.Empty(await s.Db.Payments.ToListAsync());
            Assert.Empty(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == request.Id).ToListAsync());
        }
        else
        {
            Assert.Equal(ChangeRequestStatus.AwaitingPayment, saved.Status); Assert.Equal(0, expiredCount);
            Assert.Equal(BankMatchStatus.Matched, submitResult.Status);
            Assert.Equal(PaymentStatus.Submitted, (await s.Db.Payments.SingleAsync()).Status);
            Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingChangeRequestId == request.Id));
        }
        Assert.Single(await s.Db.BankTransactions.ToListAsync());
        Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingId == booking.Id));
        // Giữ file SQLite tạm theo quy tắc không xóa database.
    }

    [Theory]
    [InlineData("missing")] [InlineData("wrong-amount")] [InlineData("late")]
    public async Task FinanceConfirmationRequiresValidTimelyPayment(string kind)
    {
        using var s = new Scenario(); var (b, r, _, principal) = await Awaiting(s);
        if (kind != "missing")
        {
            await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
            var payment = await s.Db.Payments.Include(p => p.BankTransaction).SingleAsync();
            if (kind == "wrong-amount") payment.Amount += 1;
            else payment.BankTransaction!.TransactionTime = r.PaymentDeadlineAt!.Value.AddTicks(1);
            await s.Db.SaveChangesAsync();
        }
        var before = await FinanceSnapshot(s.Db);
        await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(r.Id, "Xác nhận", principal));
        Assert.Equal(before, await FinanceSnapshot(s.Db));
    }


    [Fact]
    public async Task TwoPaidDateChangesAccumulateFeesOnceOnSameBooking()
    {
        using var s = new Scenario(); var (b, first, _, principal) = await Awaiting(s);
        var code = b.Code;
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, first));
        await s.Service().ConfirmAsync(first.Id, "Tiền lần một", principal);
        s.H.Clock.Advance(TimeSpan.FromDays(7));
        var second = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        Assert.Equal(300_000, second.ChangeFee); Assert.Equal(300_000, second.AdditionalDeposit);
        await s.Service().ApproveAsync(second.Id, "Đồng ý lần hai", s.Manager);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, second, "CR-SECOND"));
        await s.Service().ConfirmAsync(second.Id, "Tiền lần hai", principal);
        Assert.Equal(code, b.Code); Assert.Equal(600_000, b.ChangeFeeAmount); Assert.Equal(600_000, b.DepositAmount);
        Assert.Single(await s.Db.Bookings.ToListAsync()); Assert.Equal(2, await s.Db.Payments.CountAsync());
        b.Status = BookingStatus.Completed;
        var item = SettlementCalculator.ItemFor(b)!;
        Assert.Equal(1_200_000, item.RetainedAmount); Assert.Equal(312_000, item.CommissionAmount); Assert.Equal(888_000, item.PayoutAmount);
    }

    [Fact]
    public async Task FinanceConfirmationReusesOverlappingBookingNight()
    {
        using var s = new Scenario(); var b = await s.Book(4); var employee = s.Staff(EmployeePosition.Accountant);
        var principal = Principal(employee.Person.Account!.Id, AppRoles.Employee, EmployeePosition.Accountant);
        var room = b.RoomId;
        var request = await s.Service().SubmitAsync(s.Dates(b, 1), s.Customer);
        await s.Service().ApproveAsync(request.Id, "Đổi một ngày", s.Manager);
        Assert.Equal(1, await s.Db.RoomNights.CountAsync(n => n.BookingChangeRequestId == request.Id));
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, request));
        await s.Service().ConfirmAsync(request.Id, "Đã nhận", principal);
        var nights = await s.Db.RoomNights.OrderBy(n => n.Date).ToListAsync();
        Assert.Equal(2, nights.Count); Assert.All(nights, n => { Assert.Equal(room, n.RoomId); Assert.Equal(b.Id, n.BookingId); Assert.Null(n.BookingChangeRequestId); });
        Assert.Equal(request.NewCheckIn, nights[0].Date);
    }

    [Fact]
    public async Task InitialReceiptReviewCannotApproveAnAdditionalDeposit()
    {
        using var s = new Scenario(); var (_, r, accountant, _) = await Awaiting(s);
        var payment = await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        await Assert.ThrowsAsync<BookingException>(() => s.H.Receipts().ApproveAsync(payment.Id, accountant.Id));
        await Assert.ThrowsAsync<BookingException>(() => s.H.Receipts().RejectAsync(payment.Id, accountant.Id, "Sai luồng"));
        Assert.Equal(PaymentStatus.Submitted, (await s.Db.Payments.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InactiveAccountantCannotConfirm(bool lockedAccount)
    {
        using var s = new Scenario(); var (b, r, accountant, principal) = await Awaiting(s);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        if (lockedAccount) accountant.Person.Account!.Status = AccountStatus.Locked;
        else accountant.EmploymentStatus = EmploymentStatus.Resigned;
        await s.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(r.Id, "Đã nhận", principal));
    }

    [Fact]
    public async Task AccountantReviewNextDayDoesNotReapplyCustomerNoticeLimit()
    {
        using var s = new Scenario(); var b = await s.Book(3); var employee = s.Staff(EmployeePosition.Accountant);
        var principal = Principal(employee.Person.Account!.Id, AppRoles.Employee, EmployeePosition.Accountant);
        var request = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        await s.Service().ApproveAsync(request.Id, "Duyệt trước ba ngày", s.Manager);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, request));
        s.H.Clock.Advance(TimeSpan.FromDays(1));
        await s.Service().ConfirmAsync(request.Id, "Duyệt sau hạn nộp", principal);
        Assert.Equal(ChangeRequestStatus.Applied, request.Status);
    }

    [Fact]
    public async Task InvalidPaymentDoesNotProtectExpiredHold()
    {
        using var s = new Scenario(); var (_, r, _, _) = await Awaiting(s);
        var payment = await s.H.Receipts().SubmitChangeRequestAsync(r.Id, "receipts/cr.jpg", null, s.Customer);
        payment.Amount += 1; await s.Db.SaveChangesAsync(); s.H.Clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(1, await s.Service().ExpireUnpaidAsync());
        Assert.Empty(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == r.Id).ToListAsync());
    }

    private static async Task<string> FinanceSnapshot(ApplicationDbContext db) => JsonSerializer.Serialize(new {
        BookingState = await Snapshot(db), Payments = await db.Payments.AsNoTracking().OrderBy(p => p.Id).Select(p => new {
            p.Id, p.BookingId, p.ChangeRequestId, p.Amount, p.Status, p.Method, p.BankTransactionId, p.ReviewedAt, p.ReviewedByEmployeeId, p.RejectReason }).ToListAsync(),
        Finance = await db.BookingChangeRequests.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.FinanceConfirmedAt, r.FinanceConfirmedByAccountId, r.FinanceNote }).ToListAsync(),
        Transactions = await db.BankTransactions.AsNoTracking().OrderBy(t => t.Id).Select(t => new { t.Id, t.Amount, t.MatchStatus, t.Note, t.RefundedAt, t.RefundBankRef }).ToListAsync() });
}
