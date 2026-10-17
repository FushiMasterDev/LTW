using System.Data.Common;
using System.Text.Json;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class ReportServiceTests
{
    private static readonly ReportQuery October = new(new(2026, 10, 1), new(2026, 10, 31));
    private static DateTime At(int day, int hour = 3) => new(2026, 10, day, hour, 0, 0, DateTimeKind.Utc);
    private static async Task<Booking> Add(SettlementTestHost h, BookingStatus status = BookingStatus.Completed, decimal refund = 0, decimal fee = 0, decimal discount = 0)
    {
        var b = await h.BookingAsync(status, refund: refund, fee: fee, discount: discount); b.CreatedAt = At(10);
        b.DepositedAt = status is BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted or BookingStatus.Expired ? null : At(10);
        if (b.ConfirmedAt != null) b.ConfirmedAt = At(10).AddMinutes(20);
        b.CreatedByEmployeeId = h.P.Consultant.EmployeeId; b.AssignedEmployeeId = h.P.Manager.EmployeeId;
        await h.Db.SaveChangesAsync(); return b;
    }

    [Fact]
    public async Task FinancialTotalsUseCalculatorExcludeReplacedAndCountCompensationByPaidDate()
    {
        using var h = await SettlementTestHost.CreateAsync(); var completed = await Add(h, fee: 300000, discount: 20000);
        await Add(h, BookingStatus.NoShow); await Add(h, BookingStatus.Cancelled, refund: 300000);
        var replaced = await Add(h, BookingStatus.Cancelled); replaced.ReplacedByBookingId = completed.Id;
        await Add(h, BookingStatus.Deposited);
        h.Db.Payments.AddRange(new Payment { BookingId = completed.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
            Amount = 250000, Status = PaymentStatus.Approved, ReviewedAt = At(31, 16), BankRef = "FT" },
            new Payment { BookingId = completed.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
                Amount = 100000, Status = PaymentStatus.Approved, ReviewedAt = At(31, 17), BankRef = "FT-NOV" });
        await h.Db.SaveChangesAsync();
        var report = await new ReportService(h.Db).FinancialAsync(October, h.P.Accountant);
        var items = (await h.Db.Bookings.AsNoTracking().ToListAsync()).Select(SettlementCalculator.ItemFor).OfType<SettlementItem>().ToList();
        Assert.True(report.IsProvisional); Assert.Equal(2000000, report.CompletedRoomAmount);
        Assert.Equal(items.Sum(i => i.RetainedAmount), report.Retained); Assert.Equal(items.Sum(i => i.CommissionAmount), report.Commission);
        Assert.Equal(items.Sum(SettlementCalculator.FujiNetRevenue), report.FujiNet); Assert.Equal(250000, report.CompensationPaid);
        var filtered = await new ReportService(h.Db).FinancialAsync(October with { HomestayId = -1 }, h.P.Accountant);
        Assert.Equal(0, filtered.CompletedRoomAmount); Assert.Equal(0, filtered.CompensationPaid);
    }

    [Fact]
    public async Task CreationCohortUsesVietnamMidnightAndSeparatesCancellationsRejectionsExpiry()
    {
        using var h = await SettlementTestHost.CreateAsync(); var valid = await Add(h);
        var cancelled = await Add(h, BookingStatus.Cancelled); var rejected = await Add(h, BookingStatus.Rejected);
        var expired = await Add(h, BookingStatus.Expired); var unpaid = await Add(h, BookingStatus.Cancelled); unpaid.DepositedAt = null;
        var replaced = await Add(h, BookingStatus.Cancelled); replaced.ReplacedByBookingId = valid.Id;
        var before = await Add(h, BookingStatus.Deposited); before.CreatedAt = new(2026, 9, 30, 16, 59, 59, DateTimeKind.Utc);
        var midnight = await Add(h, BookingStatus.Deposited); midnight.CreatedAt = new(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc);
        var after = await Add(h, BookingStatus.Deposited); after.CreatedAt = At(31, 17);
        cancelled.SourceChannel = SourceChannel.Phone; await h.Db.SaveChangesAsync();
        var r = await new ReportService(h.Db).BookingsAsync(October, h.P.Manager);
        Assert.Equal(7, r.Total); Assert.Equal(4, r.DepositedCohort); Assert.Equal(1, r.CustomerCancelled); Assert.Equal(25, r.CancellationPercent);
        Assert.Equal(1, r.Rejected); Assert.Equal(100d / 7, r.RejectionPercent, 8); Assert.Equal(1, r.Expired);
        Assert.Contains(r.Breakdown, b => b.Channel == SourceChannel.Phone && b.Status == BookingStatus.Cancelled && b.Count == 1);
    }

    [Fact]
    public async Task EmployeeConversionUsesCreatedCohortAndRevenueKeepsCreatorAfterReassignment()
    {
        using var h = await SettlementTestHost.CreateAsync(); await Add(h); await Add(h, BookingStatus.Confirmed); await Add(h, BookingStatus.Deposited);
        var cancelled = await Add(h, BookingStatus.Cancelled); cancelled.ConfirmedAt = At(10).AddMinutes(15); await h.Db.SaveChangesAsync();
        var r = Assert.Single(await new ReportService(h.Db).EmployeesAsync(October, h.P.Consultant));
        Assert.Equal(h.P.Consultant.EmployeeId, r.EmployeeId); Assert.Equal(4, r.Created); Assert.Equal(3, r.Confirmed); Assert.Equal(75, r.ConversionPercent);
        Assert.Equal(2000000, r.CompletedRoomAmount); Assert.Equal(0, r.AssignedOpen);
        var manager = Assert.Single(await new ReportService(h.Db).EmployeesAsync(October, h.P.Admin, h.P.Manager.EmployeeId));
        Assert.Equal(0, manager.Created); Assert.Equal(0, manager.CompletedRoomAmount); Assert.Equal(2, manager.AssignedOpen); Assert.Equal(2, manager.AssignedFinished);
    }

    [Fact]
    public async Task HomestaySummaryIgnoresHiddenReviewsAndGroupsConfirmationAndIncidentsInDatabase()
    {
        using var h = await SettlementTestHost.CreateAsync(); var first = await Add(h); var second = await Add(h);
        second.ConfirmedAt = second.DepositedAt!.Value.AddMinutes(40);
        h.Db.Reviews.AddRange(new Review { BookingId = first.Id, HomestayId = first.HomestayId, Rating = 4, CreatedAt = At(11) },
            new Review { BookingId = second.Id, HomestayId = second.HomestayId, Rating = 1, IsHidden = true, CreatedAt = At(11) });
        var replaced = await Add(h, BookingStatus.Cancelled); replaced.ReplacedByBookingId = second.Id; replaced.CancelledAt = At(30);
        h.Db.AuditLogs.AddRange(new AuditLog { TableName = "Bookings", RecordId = first.Id.ToString(), CreatedAt = At(12),
            NewValues = JsonSerializer.Serialize(new { Values = new { Step = "OverbookingReported" } }) },
            new AuditLog { TableName = "Bookings", RecordId = first.Id.ToString(), CreatedAt = At(12), NewValues = JsonSerializer.Serialize(new { Note = "OverbookingReported" }) });
        await h.Db.SaveChangesAsync();
        var probe = new Probe(); await using var db = h.H.NewContext(probe);
        var r = Assert.Single(await new ReportService(db).HomestaysAsync(October, h.P.Manager));
        Assert.Equal(4, r.Rating); Assert.Equal(1, r.ReviewCount); Assert.Equal(30, r.AverageConfirmMinutes!.Value, 3); Assert.Equal(50, r.ConfirmWithinThirtyPercent);
        var month = Assert.Single(r.Incidents); Assert.Equal(2026, month.Year); Assert.Equal(10, month.Month); Assert.Equal(1, month.Reported); Assert.Equal(1, month.Replaced);
        Assert.True(probe.Commands.Count(c => c.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase)) >= 4);
        Assert.Empty(await new ReportService(db).HomestaysAsync(October with { AreaId = -1 }, h.P.Manager));
    }

    [Theory]
    [InlineData("consultant")] [InlineData("accountant")] [InlineData("owner")] [InlineData("customer")]
    public async Task OnlyManagersCanReadFullManagementOrHomestayStatistics(string type)
    {
        using var h = await SettlementTestHost.CreateAsync(); var actor = type == "consultant" ? h.P.Consultant : type == "accountant" ? h.P.Accountant : new Actor(type == "owner" ? ActorType.Owner : ActorType.Customer);
        var service = new ReportService(h.Db);
        await Assert.ThrowsAsync<BookingException>(() => service.ManagementAsync(October, actor));
        await Assert.ThrowsAsync<BookingException>(() => service.HomestaysAsync(October, actor));
        await Assert.ThrowsAsync<BookingException>(() => service.BookingsAsync(October, actor));
        if (type != "accountant") await Assert.ThrowsAsync<BookingException>(() => service.FinancialAsync(October, actor));
        else Assert.Equal(0, (await service.FinancialAsync(October, actor)).Retained);
    }

    [Fact]
    public async Task ConsultantCannotSelectAnotherEmployeeAndAccountantCannotReadEmployees()
    {
        using var h = await SettlementTestHost.CreateAsync(); var service = new ReportService(h.Db);
        await Assert.ThrowsAsync<BookingException>(() => service.EmployeesAsync(October, h.P.Consultant, h.P.Manager.EmployeeId));
        await Assert.ThrowsAsync<BookingException>(() => service.EmployeesAsync(October, h.P.Accountant));
        Assert.Single(await service.EmployeesAsync(October, h.P.Consultant, h.P.Consultant.EmployeeId));
    }

    [Theory]
    [InlineData(0, true)] [InlineData(365, true)] [InlineData(366, false)] [InlineData(-1, false)]
    public async Task RangeAcceptsUpTo366InclusiveDays(int offset, bool allowed)
    {
        using var h = await SettlementTestHost.CreateAsync(); var q = October with { To = October.From.AddDays(offset) };
        if (allowed) Assert.Equal(0, (await new ReportService(h.Db).ManagementAsync(q, h.P.Admin)).Bookings.Total);
        else await Assert.ThrowsAsync<BookingException>(() => new ReportService(h.Db).ManagementAsync(q, h.P.Manager));
    }
    private sealed class Probe : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData e, InterceptionResult<DbDataReader> r, CancellationToken ct = default)
        { Commands.Add(command.CommandText); return ValueTask.FromResult(r); }
    }
}
