using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class B9FinanceTests
{
    [Fact]
    public async Task AccountantConfirmsOnBehalfWithReasonAuditAndOwnerLinkThenPaysActualAmountAndTime()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BankAsync();
        var row = await h.DraftAsync(); await h.Service().SendAsync(row.Id, h.P.Accountant);
        await h.Service().ConfirmOnBehalfAsync(row.Id, "Đã đối chiếu qua điện thoại", h.P.Accountant);
        Assert.Equal(SettlementStatus.Confirmed, row.Status);
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.AccountId == h.P.Accountant.AccountId && a.NewValues!.Contains("AccountantConfirmedOnBehalf"));
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.B.Owner.AccountId && n.Url == $"/Owner/Settlements/{row.Id}" && n.Message.Contains("xác nhận thay chủ"));
        h.H.Clock.Advance(TimeSpan.FromHours(2)); var paidAt = h.H.Clock.Now.UtcDateTime.AddHours(-1);
        await h.Service().RecordPaidAsync(row.Id, "B9-TRANSFER", row.PayoutAmount, paidAt, h.P.Accountant);
        Assert.Equal(paidAt, row.PaidAt); Assert.Equal(SettlementStatus.Paid, row.Status);
        var log = await h.Db.AuditLogs.OrderByDescending(a => a.Id).FirstAsync();
        Assert.Contains(row.PayoutAmount.ToString(System.Globalization.CultureInfo.InvariantCulture), log.NewValues!);
        Assert.Contains(paidAt.ToString("O").Substring(0,19), log.NewValues!);
    }

    [Theory]
    [InlineData("empty-reason")]
    [InlineData("wrong-state")]
    [InlineData("owner")]
    [InlineData("consultant")]
    [InlineData("manager")]
    public async Task OnBehalfRejectsInvalidConfirmation(string invalid)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync();
        if (invalid != "wrong-state") await h.Service().SendAsync(row.Id, h.P.Accountant);
        var actor = invalid switch { "owner" => h.B.Owner, "consultant" => h.P.Consultant, "manager" => h.P.Manager, _ => h.P.Accountant };
        var before = row.Status;
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ConfirmOnBehalfAsync(row.Id, invalid == "empty-reason" ? " " : "Đã liên hệ", actor));
        Assert.Equal(before, (await h.Db.Settlements.AsNoTracking().SingleAsync()).Status);
    }

    [Theory]
    [InlineData("wrong-amount")]
    [InlineData("future")]
    [InlineData("before-sent")]
    [InlineData("before-confirmed")]
    [InlineData("wrong-state")]
    [InlineData("owner")]
    [InlineData("consultant")]
    public async Task PaymentRejectsBadAmountDateStateAndActors(string invalid)
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BankAsync(); var row = await h.DraftAsync();
        if (invalid != "wrong-state") await h.Service().SendAsync(row.Id, h.P.Accountant);
        var sent = h.H.Clock.Now.UtcDateTime; h.H.Clock.Advance(TimeSpan.FromHours(1));
        if (invalid == "before-confirmed") await h.Service().ConfirmOnBehalfAsync(row.Id, "Đã kiểm tra", h.P.Accountant);
        var paidAt = invalid switch { "future" => h.H.Clock.Now.UtcDateTime.AddMinutes(1), "before-sent" => sent.AddMinutes(-1), "before-confirmed" => sent, _ => h.H.Clock.Now.UtcDateTime };
        var actor = invalid switch { "owner" => h.B.Owner, "consultant" => h.P.Consultant, _ => h.P.Accountant };
        await Assert.ThrowsAsync<BookingException>(() => h.Service().RecordPaidAsync(row.Id, "B9", invalid == "wrong-amount" ? row.PayoutAmount - 1 : row.PayoutAmount, paidAt, actor));
        Assert.NotEqual(SettlementStatus.Paid, (await h.Db.Settlements.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task RejectableCompensationsUseExactlyRejectConditionsAndHideCompleted()
    {
        using var h = await SettlementTestHost.CreateAsync();
        var ready = await h.CompensationAsync(BookingStatus.Confirmed); var complete = await h.CompensationAsync();
        var rejected = await h.CompensationAsync(BookingStatus.Deposited); await h.Compensation().RejectAsync(rejected.Id, "Trùng yêu cầu", h.P.Accountant);
        var rows = await h.Compensation().RejectableAsync(h.P.Accountant);
        Assert.Equal(ready.Id, Assert.Single(rows).Id); Assert.NotEqual(BookingStatus.Completed, rows[0].Booking.Status);
        await h.Compensation().RejectAsync(ready.Id, "Khách không tiếp tục", h.P.Accountant);
        Assert.Empty(await h.Compensation().RejectableAsync(h.P.Accountant));
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RejectableAsync(h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RejectableAsync(h.P.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RejectAsync(complete.Id, "Không hợp lệ", h.P.Accountant));
    }

    [Fact]
    public async Task ReadRowsUseRealSettlementItemsAndLoadNamesAndBlockOwner()
    {
        using var h = await SettlementTestHost.CreateAsync();
        var b = await h.BookingAsync(BookingStatus.Cancelled, refund: 100000);
        var row = (await h.Service().GenerateAsync(b.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        var tx = new BankTransaction { ProviderTransactionId = "B9-READ", Amount = 600000, MatchedBookingId = b.Id, TransactionTime = h.H.Clock.Now.UtcDateTime };
        var receipt = new Payment { BookingId = b.Id, Type = PaymentType.Deposit, Method = PaymentMethod.ManualReceipt, ReceiptImageUrl = "receipts/b9.png", Amount = 600000, Status = PaymentStatus.Approved };
        var request = new BookingChangeRequest { BookingId = b.Id, RequestType = ChangeRequestType.ChangeDates, RequestedByType = ActorType.Employee, NewCheckIn = b.CheckIn.AddDays(5), NewCheckOut = b.CheckOut.AddDays(5), DecisionNote = "Đã duyệt thử", AdditionalDeposit = 100000, PaymentDeadlineAt = h.H.Clock.Now.UtcDateTime.AddHours(1), Status = ChangeRequestStatus.AwaitingPayment, DecidedByAccountId = h.P.Manager.AccountId, DecidedAt = h.H.Clock.Now.UtcDateTime };
        h.Db.AddRange(tx, receipt, request); await h.Db.SaveChangesAsync();
        h.Db.Payments.Add(new() { BookingId = b.Id, ChangeRequestId = request.Id, Type = PaymentType.AdditionalDeposit, Amount = 100000, Status = PaymentStatus.Submitted });
        await h.Db.SaveChangesAsync();
        var service = h.H.Accounting(); var actor = h.P.Accountant;
        Assert.Equal(row.Id, Assert.Single(await service.ReceiptsAsync(PaymentStatus.Approved, 1, actor)).SettlementId);
        Assert.Equal(row.Id, Assert.Single(await service.RefundsAsync(false, null, 1, actor)).SettlementId);
        Assert.Equal(row.Id, Assert.Single(await service.TransactionsAsync(null, true, null, 1, actor)).SettlementId);
        Assert.Equal(row.Id, (await service.TransactionAsync(tx.Id, actor))!.SettlementId);
        Assert.Equal(row.Id, Assert.Single(await service.AdditionalDepositsAsync(1, actor)).Booking.SettlementItem!.SettlementId);
        var noItem = await h.BookingAsync(BookingStatus.AwaitingPayment);
        tx.MatchedBookingId = noItem.Id; await h.Db.SaveChangesAsync();
        Assert.Null((await service.TransactionAsync(tx.Id, actor))!.SettlementId);
        var detail = await h.Service().DetailsAsync(row.Id, actor);
        Assert.NotEmpty(detail.Settlement.Homestay.Name); Assert.NotNull(detail.Settlement.Homestay.Owner.Person.FullName);
        Assert.NotEmpty(Assert.Single(await h.Service().ListAsync(new(), actor)).Homestay.Name);
        await Assert.ThrowsAsync<BookingException>(() => service.ReceiptsAsync(PaymentStatus.Approved, 1, h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => service.RefundsAsync(false, null, 1, h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => service.TransactionsAsync(null, true, null, 1, h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => service.TransactionAsync(tx.Id, h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => service.AdditionalDepositsAsync(1, h.B.Owner));
    }
}
