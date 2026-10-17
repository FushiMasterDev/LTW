using System.Text.Json;
using System.Data.Common;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class SettlementServiceTests
{
    public static IEnumerable<object[]> Transitions() => from a in Enum.GetValues<SettlementStatus>() from b in Enum.GetValues<SettlementStatus>() select new object[] { a, b,
        a == SettlementStatus.Draft && b == SettlementStatus.Sent || a == SettlementStatus.Sent && b is SettlementStatus.Confirmed or SettlementStatus.Paid or SettlementStatus.Draft
        || a == SettlementStatus.Confirmed && b is SettlementStatus.Paid or SettlementStatus.Draft };
    [Theory, MemberData(nameof(Transitions))]
    public void CompleteTransitionTable(SettlementStatus from, SettlementStatus to, bool allowed) => Assert.Equal(allowed, SettlementService.CanTransition(from, to));

    [Theory]
    [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, true)]
    public async Task PeriodClosesStrictlyAfterGraceDaysInVietnamTime(int novemberDay, bool allowed)
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BookingAsync();
        h.H.Clock.Now = new DateTimeOffset(2026, 11, 1, 17, 0, 0, TimeSpan.Zero).AddDays(novemberDay - 2);
        if (allowed) Assert.NotNull((await h.Service().GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Accountant)).Settlement);
        else await Assert.ThrowsAsync<BookingException>(() => h.Service().GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Accountant));
    }

    [Theory]
    [InlineData(BookingStatus.Completed, 0, true)] [InlineData(BookingStatus.NoShow, 0, true)]
    [InlineData(BookingStatus.Cancelled, 0, true)] [InlineData(BookingStatus.Cancelled, 600000, false)]
    [InlineData(BookingStatus.Deposited, 0, false)] [InlineData(BookingStatus.Confirmed, 0, false)]
    [InlineData(BookingStatus.CheckedIn, 0, false)] [InlineData(BookingStatus.PaymentSubmitted, 0, false)] [InlineData(BookingStatus.AwaitingPayment, 0, false)]
    public async Task OnlyCalculatorEligibleRowsCreateNonEmptyDraft(BookingStatus status, int refund, bool eligible)
    {
        using var h = await SettlementTestHost.CreateAsync(); var b = await h.BookingAsync(status, refund: refund);
        var result = await h.Service().GenerateAsync(b.HomestayId, h.Period, h.P.Manager);
        Assert.Equal(eligible, result.Settlement is not null);
        if (eligible)
        {
            var expected = SettlementCalculator.ItemFor(b)!; var item = await h.Db.SettlementItems.SingleAsync();
            Assert.Equal(expected.PayoutAmount, item.PayoutAmount); Assert.Equal(h.Period, SettlementCalculator.PeriodOf(b.CheckOut));
        }
        else Assert.Empty(h.Db.Settlements);
    }

    [Fact]
    public async Task DraftRebuildsForLateCompletionJobIsIdempotentAndSentIsFrozen()
    {
        using var h = await SettlementTestHost.CreateAsync(); var a = await h.BookingAsync(); var late = await h.BookingAsync(BookingStatus.Confirmed);
        var result = await h.Service().GenerateAsync(a.HomestayId, h.Period, h.P.Accountant); var row = result.Settlement!;
        Assert.Equal(new[] { late.Code }, result.UnclosedBookingCodes);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().SendAsync(row.Id, h.P.Accountant));
        late = await h.Db.Bookings.SingleAsync(b => b.Id == late.Id); late.Status = BookingStatus.Completed; await h.Db.SaveChangesAsync();
        row = (await h.Service().GenerateAsync(a.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        Assert.Equal(2, await h.Db.SettlementItems.CountAsync());
        var expected = SettlementCalculator.Build(a.HomestayId, h.Period, [a, late]); Assert.Equal(expected.PayoutAmount, row.PayoutAmount);
        Assert.Equal(expected.TotalRoomAmount, row.TotalRoomAmount); Assert.Equal(expected.TotalRetained, row.TotalRetained); Assert.Equal(expected.TotalCommission, row.TotalCommission);
        await h.Service().GenerateRecentAsync(); var audits = await h.Db.AuditLogs.CountAsync();
        var items = await h.Db.SettlementItems.Select(i => i.Id).Order().ToArrayAsync(); await h.Service().GenerateRecentAsync();
        Assert.Single(h.Db.Settlements); Assert.Equal(items, await h.Db.SettlementItems.Select(i => i.Id).Order().ToArrayAsync()); Assert.Equal(audits, await h.Db.AuditLogs.CountAsync());
        await h.Service().SendAsync(row.Id, h.P.Accountant); var total = row.PayoutAmount;
        await h.BookingAsync(); await h.Service().GenerateAsync(a.HomestayId, h.Period, h.P.Manager);
        Assert.Equal(total, row.PayoutAmount); Assert.Equal(2, await h.Db.SettlementItems.CountAsync());
    }

    [Fact]
    public async Task CheckOutBoundariesReplacedAndAlreadySettledBookingsAreHandled()
    {
        using var h = await SettlementTestHost.CreateAsync(); var first = await h.BookingAsync(checkOut: new(2026, 10, 15));
        var second = await h.BookingAsync(checkOut: new(2026, 10, 16)); var replaced = await h.BookingAsync(BookingStatus.Cancelled); replaced.ReplacedByBookingId = second.Id; await h.Db.SaveChangesAsync();
        var elsewhere = await h.BookingAsync(); var preexisting = SettlementCalculator.Build(elsewhere.HomestayId, h.Period, [elsewhere]); h.Db.Add(preexisting); await h.Db.SaveChangesAsync();
        var p1 = (await h.Service().GenerateAsync(first.HomestayId, new(2026, 10, 1), h.P.Accountant)).Settlement!;
        Assert.Equal(first.Id, (await h.Db.SettlementItems.SingleAsync(i => i.SettlementId == p1.Id)).BookingId);
        // Một đơn đã có ở bảng khác được loại, kể cả dữ liệu cũ gắn vào kỳ khác.
        preexisting.Year = 2025; await h.Db.SaveChangesAsync();
        var p2 = (await h.Service().GenerateAsync(second.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        Assert.Equal(second.Id, (await h.Db.SettlementItems.SingleAsync(i => i.SettlementId == p2.Id)).BookingId);
        Assert.False(await h.Db.SettlementItems.AnyAsync(i => i.BookingId == replaced.Id));
    }

    [Theory]
    [InlineData(BookingStatus.AwaitingPayment)] [InlineData(BookingStatus.PaymentSubmitted)] [InlineData(BookingStatus.Deposited)]
    [InlineData(BookingStatus.Confirmed)] [InlineData(BookingStatus.CheckedIn)]
    public async Task EveryUnclosedStatusBlocksSendingAndAppearsOnDraft(BookingStatus status)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync(); var b = await h.BookingAsync(status);
        var details = await h.Service().DetailsAsync(row.Id, h.P.Accountant); Assert.Contains(b.Code, details.UnclosedBookingCodes);
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Service().SendAsync(row.Id, h.P.Accountant)); Assert.Contains(b.Code, ex.Message);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PaidFromSentOrConfirmedRecordsActorsAndRealAuditIds(bool ownerConfirms)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync(); await h.BankAsync();
        await h.Service().SendAsync(row.Id, h.P.Accountant);
        if (ownerConfirms) await h.Service().ConfirmAsync(row.Id, h.B.Owner);
        await h.Service().RecordPaidAsync(row.Id, " FT-PAID ", h.P.Accountant);
        Assert.Equal(SettlementStatus.Paid, row.Status); Assert.Equal("FT-PAID", row.PaidBankRef); Assert.Equal(h.P.Accountant.EmployeeId, row.PaidByEmployeeId);
        Assert.Equal(h.H.Clock.Now.UtcDateTime, row.PaidAt); Assert.Equal(ownerConfirms, row.ConfirmedAt.HasValue);
        var audits = await h.Db.AuditLogs.Where(a => a.TableName == "Settlements" && a.RecordId == row.Id.ToString()).ToListAsync();
        Assert.Contains(audits, a => a.Action == AuditAction.Insert && a.AccountId == h.P.Accountant.AccountId);
        Assert.Contains(audits, a => a.NewValues!.Contains("Sent") && a.AccountId == h.P.Accountant.AccountId);
        if (ownerConfirms) Assert.Contains(audits, a => a.NewValues!.Contains("OwnerConfirmed") && a.AccountId == h.B.Owner.AccountId);
        Assert.Contains(audits, a => a.NewValues!.Contains("Paid") && a.AccountId == h.P.Accountant.AccountId);
        Assert.All(audits, a => { Assert.NotNull(a.OldValues); Assert.NotNull(a.NewValues); });
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ReopenAsync(row.Id, "Không được", h.P.Accountant));
    }

    [Theory]
    [InlineData("ref")] [InlineData("long-ref")] [InlineData("bank-name")] [InlineData("bank-no")] [InlineData("bank-owner")]
    public async Task PayingRequiresReferenceAndFullBankDetails(string missing)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync(); await h.BankAsync(); await h.Service().SendAsync(row.Id, h.P.Accountant);
        var owner = await h.Db.Owners.SingleAsync();
        if (missing == "bank-name") owner.BankName = " "; if (missing == "bank-no") owner.BankAccountNo = null; if (missing == "bank-owner") owner.BankAccountName = null; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Service().RecordPaidAsync(row.Id, missing == "ref" ? " " : missing == "long-ref" ? new string('X', 101) : "FT", h.P.Accountant));
        Assert.Equal(SettlementStatus.Sent, (await h.Db.Settlements.AsNoTracking().SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReopenRecalculatesAndOwnerFeedbackDoesNotChangeState(bool confirmed)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync(); await h.Service().SendAsync(row.Id, h.P.Accountant);
        if (confirmed) await h.Service().ConfirmAsync(row.Id, h.B.Owner);
        var status = row.Status; await h.Service().ReportDiscrepancyAsync(row.Id, new string('X', 500), h.B.Owner); Assert.Equal(status, row.Status);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ReportDiscrepancyAsync(row.Id, " ", h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ReportDiscrepancyAsync(row.Id, new string('X', 501), h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ReopenAsync(row.Id, " ", h.P.Accountant));
        await h.BookingAsync(); row = await h.Service().ReopenAsync(row.Id, "Chủ phát hiện đơn chốt muộn", h.P.Accountant);
        Assert.Equal(SettlementStatus.Draft, row.Status); Assert.Null(row.SentAt); Assert.Null(row.ConfirmedAt); Assert.Equal(2, await h.Db.SettlementItems.CountAsync());
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.P.Accountant.AccountId && n.Message.Contains("phản hồi"));
        Assert.All(h.Db.Notifications, n => Assert.True(n.Message.Length <= 500));
    }

    [Theory]
    [InlineData("manager")] [InlineData("admin")] [InlineData("consultant")] [InlineData("owner")] [InlineData("customer")]
    public async Task OnlyAccountantSendsReopensOrPays(string who)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync();
        var actor = who == "manager" ? h.P.Manager : who == "admin" ? h.P.Admin : who == "consultant" ? h.P.Consultant : who == "owner" ? h.B.Owner : new Actor(ActorType.Customer);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().SendAsync(row.Id, actor));
        await h.Service().SendAsync(row.Id, h.P.Accountant);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ReopenAsync(row.Id, "Không đúng quyền", actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().RecordPaidAsync(row.Id, "FT", actor));
    }

    [Fact]
    public async Task OwnerCannotSeeDraftOrOtherHomesConsultantCannotReadAndFujiNetUsesSharedFormula()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BookingAsync(discount: 20000);
        var row = (await h.Service().GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Admin)).Settlement!;
        Assert.Empty(await h.Service().ListAsync(new(), h.B.Owner)); await Assert.ThrowsAsync<BookingException>(() => h.Service().DetailsAsync(row.Id, h.B.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ListAsync(new(), h.P.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().GenerateAsync(row.HomestayId, h.Period, h.P.Consultant));
        await h.Service().SendAsync(row.Id, h.P.Accountant);
        var own = await h.Service().DetailsAsync(row.Id, h.B.Owner); var line = Assert.Single(own.Lines);
        Assert.Equal(220000, line.FujiNetRevenue); Assert.Equal(SettlementCalculator.FujiNetRevenue(line.Item), line.FujiNetRevenue);
        Assert.Single(await h.Service().ListAsync(new(h.Period, SettlementStatus.Sent, row.HomestayId), h.P.Manager));
        var other = new Actor(ActorType.Owner, (await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Customer.PersonId)).Id);
        h.Db.Owners.Add(new Owner { PersonId = h.H.Customer.PersonId }); await h.Db.SaveChangesAsync();
        Assert.Empty(await h.Service().ListAsync(new(), other));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().DetailsAsync(row.Id, other));
        await Assert.ThrowsAsync<BookingException>(() => h.Service().ConfirmAsync(row.Id, other));
    }

    [Fact]
    public async Task EndedHomeIsPaidAndJobOnlyScansConfiguredPeriods()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BookingAsync(); await h.BookingAsync(checkOut: new(2026, 8, 15));
        (await h.Db.Homestays.SingleAsync()).Status = HomestayStatus.Ended; await h.Db.SaveChangesAsync(); h.Options.LookbackPeriods = 1;
        await h.Service().GenerateRecentAsync(); Assert.Single(h.Db.Settlements);
        var row = await h.Db.Settlements.SingleAsync(); Assert.Equal(h.Period, new SettlementPeriod(row.Year, row.Month, row.PeriodNo));
        await h.Service().GeneratePeriodAsync(new(2026, 8, 1), h.P.Manager); Assert.Equal(2, await h.Db.Settlements.CountAsync());
    }

    [Theory]
    [InlineData(2026, 0, 1)] [InlineData(2026, 13, 1)] [InlineData(2019, 10, 1)] [InlineData(2101, 10, 1)]
    [InlineData(2026, 10, 0)] [InlineData(2026, 10, 3)]
    public async Task InvalidPeriodIsRejectedClearly(int year, int month, int part)
    {
        using var h = await SettlementTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Service().GenerateAsync(h.H.Homestay.Id, new(year, month, part), h.P.Manager));
        Assert.Empty(h.Db.Settlements);
    }

    [Fact]
    public async Task ExistingDraftCanBecomeEmptyButCannotBeSent()
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync();
        var b = await h.Db.Bookings.SingleAsync(); b.Status = BookingStatus.Cancelled; b.RefundAmount = b.AmountDue; await h.Db.SaveChangesAsync();
        var result = (await h.Service().GenerateAsync(b.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        Assert.Equal(row.Id, result.Id); Assert.Equal(0, result.PayoutAmount); Assert.Empty(h.Db.SettlementItems); Assert.Single(h.Db.Settlements);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().SendAsync(row.Id, h.P.Accountant));
    }

    [Fact]
    public async Task ZeroPayoutMayBePaidWithoutReferenceAfterBankDetailsArePresent()
    {
        using var h = await SettlementTestHost.CreateAsync(); var b = await h.BookingAsync(); b.DepositAmount = 100000; await h.Db.SaveChangesAsync();
        var row = (await h.Service().GenerateAsync(b.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        Assert.Equal(0, row.PayoutAmount); await h.Service().SendAsync(row.Id, h.P.Accountant);
        await Assert.ThrowsAsync<BookingException>(() => h.Service().RecordPaidAsync(row.Id, null, h.P.Accountant));
        await h.BankAsync(); row = await h.Service().RecordPaidAsync(row.Id, null, h.P.Accountant);
        Assert.Equal(SettlementStatus.Paid, row.Status); Assert.Null(row.PaidBankRef); Assert.NotNull(row.PaidAt);
    }

    [Theory]
    [InlineData(nameof(SettlementOptions.GraceDays), -1, false)] [InlineData(nameof(SettlementOptions.GraceDays), 31, false)]
    [InlineData(nameof(SettlementOptions.GraceDays), 0, true)] [InlineData(nameof(SettlementOptions.GraceDays), 30, true)]
    [InlineData(nameof(SettlementOptions.LookbackPeriods), 0, false)] [InlineData(nameof(SettlementOptions.LookbackPeriods), 49, false)]
    [InlineData(nameof(SettlementOptions.LookbackPeriods), 1, true)] [InlineData(nameof(SettlementOptions.LookbackPeriods), 48, true)]
    [InlineData(nameof(SettlementOptions.GenerateEveryMinutes), 4, false)] [InlineData(nameof(SettlementOptions.GenerateEveryMinutes), 1441, false)]
    [InlineData(nameof(SettlementOptions.GenerateEveryMinutes), 5, true)] [InlineData(nameof(SettlementOptions.GenerateEveryMinutes), 1440, true)]
    public void OptionsValidate(string field, int value, bool valid)
    { var o = new SettlementOptions(); typeof(SettlementOptions).GetProperty(field)!.SetValue(o, value); Assert.Equal(valid, new DataAnnotationValidateOptions<SettlementOptions>(Options.DefaultName).Validate(Options.DefaultName, o).Succeeded); }

    [Theory]
    [InlineData(SettlementStatus.Draft)] [InlineData(SettlementStatus.Sent)]
    [InlineData(SettlementStatus.Confirmed)] [InlineData(SettlementStatus.Paid)]
    public async Task UnchangedDraftAndFrozenStatementsOnlyReadWithoutLockOrWrite(SettlementStatus status)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync();
        if (status != SettlementStatus.Draft) await h.Service().SendAsync(row.Id, h.P.Accountant);
        if (status == SettlementStatus.Confirmed) await h.Service().ConfirmAsync(row.Id, h.B.Owner);
        if (status == SettlementStatus.Paid) { await h.BankAsync(); await h.Service().RecordPaidAsync(row.Id, "FT", h.P.Accountant); }
        var before = await Snapshot(h.Db); var updated = row.UpdatedAt; var homeUpdated = (await h.Db.Homestays.SingleAsync()).UpdatedAt;
        h.H.Clock.Advance(TimeSpan.FromHours(1));
        var commands = new CommandProbe(); await using var db = h.H.NewContext(commands);
        var result = await h.Service(db).GenerateAsync(row.HomestayId, h.Period, h.P.Accountant);
        await h.Service(db).GenerateRecentAsync();
        Assert.Equal(status, result.Settlement!.Status); Assert.NotEmpty(commands.Commands);
        Assert.All(commands.Commands, sql => Assert.True(sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase), sql));
        Assert.Equal(before, await Snapshot(h.Db)); Assert.Equal(updated, result.Settlement.UpdatedAt);
        Assert.Equal(homeUpdated, (await h.Db.Homestays.AsNoTracking().SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task ReadOnlyDraftCheckStillRebuildsWhenBookingCompletesLater()
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = await h.DraftAsync(); var late = await h.BookingAsync(BookingStatus.Confirmed);
        var before = row.UpdatedAt; var auditCount = await h.Db.AuditLogs.CountAsync();
        var commands = new CommandProbe(); await using var db = h.H.NewContext(commands);
        var first = await h.Service(db).GenerateAsync(row.HomestayId, h.Period, h.P.Accountant);
        Assert.Equal(new[] { late.Code }, first.UnclosedBookingCodes);
        Assert.All(commands.Commands, sql => Assert.True(sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase), sql));
        late.Status = BookingStatus.Completed; await h.Db.SaveChangesAsync(); h.H.Clock.Advance(TimeSpan.FromHours(1)); commands.Commands.Clear();
        var changed = (await h.Service(db).GenerateAsync(row.HomestayId, h.Period, h.P.Accountant)).Settlement!;
        Assert.Contains(commands.Commands, sql => sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, await h.Db.SettlementItems.CountAsync()); Assert.True(changed.UpdatedAt > before);
        Assert.True(await h.Db.AuditLogs.CountAsync() > auditCount);
        var expected = SettlementCalculator.Build(row.HomestayId, h.Period, await h.Db.Bookings.AsNoTracking().ToListAsync());
        Assert.Equal(expected.PayoutAmount, changed.PayoutAmount);
    }

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData e, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        { Commands.Add(command.CommandText); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData e, InterceptionResult<int> result, CancellationToken ct = default)
        { Commands.Add(command.CommandText); return ValueTask.FromResult(result); }
    }

    [Theory]
    [InlineData("generate")] [InlineData("send")] [InlineData("paid")]
    public async Task FailureRollsBackRowsItemsAuditAndNotification(string step)
    {
        using var h = await SettlementTestHost.CreateAsync(); var row = step == "generate" ? null : await h.DraftAsync();
        if (row is null) await h.BookingAsync();
        if (step == "paid") { await h.BankAsync(); await h.Service().SendAsync(row!.Id, h.P.Accountant); }
        var before = await Snapshot(h.Db); await using var broken = h.H.NewContext(new FailAudit());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (step == "generate") await h.Service(broken).GenerateAsync(h.H.Homestay.Id, h.Period, h.P.Accountant);
            else if (step == "send") await h.Service(broken).SendAsync(row!.Id, h.P.Accountant);
            else await h.Service(broken).RecordPaidAsync(row!.Id, "FT", h.P.Accountant);
        });
        Assert.Equal(before, await Snapshot(h.Db));
    }
    private static async Task<string> Snapshot(ApplicationDbContext db) => JsonSerializer.Serialize(new { Rows = await db.Settlements.AsNoTracking().OrderBy(s => s.Id).Select(s => new { s.Id, s.Status, s.PayoutAmount, s.SentAt, s.PaidAt }).ToListAsync(), Items = await db.SettlementItems.AsNoTracking().OrderBy(i => i.Id).Select(i => new { i.Id, i.BookingId, i.PayoutAmount }).ToListAsync(), Audit = await db.AuditLogs.CountAsync(), Notice = await db.Notifications.CountAsync() });
    private sealed class FailAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
        { if (e.Context!.ChangeTracker.Entries<AuditLog>().Any(a => a.State == EntityState.Added)) throw new InvalidOperationException("Lỗi audit giả lập"); return ValueTask.FromResult(r); }
    }
}
