using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class CompensationServiceTests
{
    [Theory]
    [InlineData(BookingStatus.Completed, false, true)] [InlineData(BookingStatus.Completed, true, false)]
    [InlineData(BookingStatus.Deposited, false, false)] [InlineData(BookingStatus.Confirmed, false, false)]
    [InlineData(BookingStatus.CheckedIn, false, false)] [InlineData(BookingStatus.Rejected, false, false)]
    [InlineData(BookingStatus.NoShow, false, false)] [InlineData(BookingStatus.Cancelled, false, false)] [InlineData(BookingStatus.Expired, false, false)]
    public async Task QueueAndPaymentOnlyAllowCompletedReplacement(BookingStatus status, bool replacedAgain, bool allowed)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(status, replacedAgain); await h.BankAsync();
        Assert.Equal(allowed ? 1 : 0, (await h.Compensation().PendingAsync(h.P.Accountant)).Count);
        if (allowed)
        {
            await h.Compensation().RecordPaidAsync(p.Id, " FT-COMP ", h.P.Accountant);
            Assert.Equal(PaymentStatus.Approved, p.Status); Assert.Equal("FT-COMP", p.BankRef);
            Assert.Equal(h.P.Accountant.EmployeeId, p.ReviewedByEmployeeId); Assert.Equal(h.H.Clock.Now.UtcDateTime, p.ReviewedAt);
            var audit = await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Payments"); Assert.Equal(p.Id.ToString(), audit.RecordId); Assert.Equal(h.P.Accountant.AccountId, audit.AccountId);
            Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.B.Owner.AccountId && n.Message.Contains("chuyển riêng"));
            await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RecordPaidAsync(p.Id, "Lặp", h.P.Accountant));
        }
        else await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RecordPaidAsync(p.Id, "FT", h.P.Accountant));
    }

    [Theory]
    [InlineData(BookingStatus.Rejected, false, true)] [InlineData(BookingStatus.NoShow, false, true)]
    [InlineData(BookingStatus.Cancelled, false, true)] [InlineData(BookingStatus.Expired, false, true)]
    [InlineData(BookingStatus.Confirmed, true, true)] [InlineData(BookingStatus.Completed, false, false)] [InlineData(BookingStatus.Deposited, false, false)]
    public async Task ClosedOrReplacedAgainIsRejectedBySystemWithoutDeleting(BookingStatus status, bool replaced, bool rejected)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(status, replaced);
        Assert.Equal(rejected ? 1 : 0, await h.Compensation().RejectClosedAsync()); Assert.Equal(0, await h.Compensation().RejectClosedAsync());
        var saved = await h.Db.Payments.AsNoTracking().SingleAsync(); Assert.Equal(p.Id, saved.Id);
        Assert.Equal(rejected ? PaymentStatus.Rejected : PaymentStatus.Submitted, saved.Status);
        if (rejected)
        {
            Assert.False(string.IsNullOrWhiteSpace(saved.RejectReason)); Assert.Equal(h.H.Clock.Now.UtcDateTime, saved.ReviewedAt); Assert.Null(saved.ReviewedByEmployeeId);
            var log = await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Payments"); Assert.Null(log.AccountId); Assert.Contains("System", log.NewValues);
        }
    }

    [Theory]
    [InlineData("")] [InlineData(" \t")] [InlineData("long")]
    public async Task BankReferenceIsRequiredAndLimited(string value)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(); await h.BankAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RecordPaidAsync(p.Id, value == "long" ? new string('X', 101) : value, h.P.Accountant));
        Assert.Equal(PaymentStatus.Submitted, (await h.Db.Payments.AsNoTracking().SingleAsync()).Status);
    }

    [Theory]
    [InlineData("manager")] [InlineData("admin")] [InlineData("consultant")] [InlineData("owner")]
    public async Task OnlyAccountantMayRecordOrReject(string who)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync();
        var actor = who == "manager" ? h.P.Manager : who == "admin" ? h.P.Admin : who == "consultant" ? h.P.Consultant : h.B.Owner;
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RecordPaidAsync(p.Id, "FT", actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RejectAsync(p.Id, "Lý do", actor));
        if (who is "consultant" or "owner") await Assert.ThrowsAsync<BookingException>(() => h.Compensation().PendingAsync(actor));
        else Assert.Single(await h.Compensation().PendingAsync(actor));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, "Đã đối chiếu không cần bù", true)]
    [InlineData(BookingStatus.Completed, "Không cho", false)] [InlineData(BookingStatus.Confirmed, " ", false)]
    public async Task AccountantMayRejectBeforeCompletedWithReason(BookingStatus status, string reason, bool allowed)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(status);
        if (allowed)
        {
            await h.Compensation().RejectAsync(p.Id, reason, h.P.Accountant); Assert.Equal(PaymentStatus.Rejected, p.Status);
            Assert.Equal(reason, p.RejectReason); Assert.Equal(h.P.Accountant.EmployeeId, p.ReviewedByEmployeeId);
        }
        else await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RejectAsync(p.Id, reason, h.P.Accountant));
    }

    [Fact]
    public async Task CompensationIsDisplayedButNeverAddedToSettlementAmounts()
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(); await h.BankAsync();
        var row = (await h.Service().GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Accountant)).Settlement!;
        var old = (row.TotalRoomAmount, row.TotalRetained, row.TotalCommission, row.PayoutAmount);
        var details = await h.Service().DetailsAsync(row.Id, h.P.Manager); Assert.Single(details.Compensations); Assert.Single(details.Lines);
        await h.Compensation().RecordPaidAsync(p.Id, "FT-ONE", h.P.Accountant);
        row = (await h.Service().GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Accountant)).Settlement!;
        Assert.Equal(old, (row.TotalRoomAmount, row.TotalRetained, row.TotalCommission, row.PayoutAmount));
        Assert.Equal(row.PayoutAmount, await h.Db.SettlementItems.SumAsync(i => i.PayoutAmount));
        await h.Service().SendAsync(row.Id, h.P.Accountant); Assert.Single((await h.Service().DetailsAsync(row.Id, h.B.Owner)).Compensations);
    }

    [Fact]
    public async Task TransferAuditFailureRollsBackPaymentAndNotification()
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(); await h.BankAsync(); var before = await h.Db.Notifications.CountAsync();
        await using var broken = h.H.NewContext(new FailAudit());
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Compensation(broken).RecordPaidAsync(p.Id, "FT", h.P.Accountant));
        Assert.Equal(PaymentStatus.Submitted, (await h.Db.Payments.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await h.Db.AuditLogs.Where(a => a.TableName == "Payments").ToListAsync()); Assert.Equal(before, await h.Db.Notifications.CountAsync());
    }

    [Fact]
    public async Task RefundAndCompensationUseSameTransferAuditWithRealId()
    {
        using var h = await SettlementTestHost.CreateAsync(); var b = await h.BookingAsync(BookingStatus.Cancelled, refund: 300000);
        var p = await h.H.Accounting().RecordRefundAsync(b.Id, h.P.Accountant.EmployeeId!.Value, "FT-REFUND");
        var log = await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Payments"); Assert.Equal(p.Id.ToString(), log.RecordId);
        Assert.Equal(h.P.Accountant.AccountId, log.AccountId); Assert.Equal(AuditAction.Insert, log.Action);
        Assert.Equal((int)PaymentStatus.Approved, System.Text.Json.JsonDocument.Parse(log.NewValues!).RootElement.GetProperty("Values").GetProperty("Status").GetInt32());
    }

    [Theory]
    [InlineData("bank-name")] [InlineData("bank-no")] [InlineData("bank-owner")] [InlineData("complete")]
    public async Task CompensationRequiresFullBankDetailsOfReplacementHomestayOwner(string field)
    {
        using var h = await SettlementTestHost.CreateAsync(); var p = await h.CompensationAsync(); await h.BankAsync();
        var owner = await h.Db.Owners.SingleAsync();
        if (field == "bank-name") owner.BankName = " ";
        if (field == "bank-no") owner.BankAccountNo = null;
        if (field == "bank-owner") owner.BankAccountName = "";
        await h.Db.SaveChangesAsync();
        if (field == "complete")
        {
            await h.Compensation().RecordPaidAsync(p.Id, "FT-BANK", h.P.Accountant);
            Assert.Equal(PaymentStatus.Approved, p.Status);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<BookingException>(() => h.Compensation().RecordPaidAsync(p.Id, "FT-BANK", h.P.Accountant));
            Assert.Contains("thông tin tài khoản ngân hàng", ex.Message);
            var saved = await h.Db.Payments.AsNoTracking().SingleAsync(); Assert.Equal(PaymentStatus.Submitted, saved.Status);
            Assert.Null(saved.ReviewedAt); Assert.Null(saved.BankRef); Assert.Empty(h.Db.AuditLogs); Assert.Empty(h.Db.Notifications);
        }
    }

    private sealed class FailAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
        { if (e.Context!.ChangeTracker.Entries<AuditLog>().Any(a => a.State == EntityState.Added)) throw new InvalidOperationException("Lỗi audit giả lập"); return ValueTask.FromResult(r); }
    }
}
