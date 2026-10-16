using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class OverbookingServiceTests
{
    [Fact]
    public async Task NewOwnerRejectionQueuesOnlyReplacementCashRefundAndRestoresExactlyTransferredDiscount()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var voucher = await h.VoucherAsync();
        var original = await h.BookingAsync(voucherId: voucher.Id); var originalId = original.Id;
        var target = await h.TypeAsync(800000, sameHome: false);
        var account = new ApplicationUser { Status = AccountStatus.Active,
            Person = new PersonProfile { PhoneNumber = "+84912000123", FullName = "Chủ mới" } };
        var owner = new Owner { Person = account.Person }; h.Db.AddRange(account, owner);
        target.Homestay.Owner = owner; await h.Db.SaveChangesAsync();
        var balanceBefore = voucher.Balance;
        var replacement = await h.Overbooking().ReplaceAsync(originalId, target.Id, "Khách chọn homestay mới", h.Manager);
        var replacementId = replacement.Id; var paid = replacement.AmountDue; var discount = replacement.DiscountAmount;
        Assert.True(discount > 0); Assert.Equal(balanceBefore, voucher.Balance);
        Assert.Empty(await h.H.Accounting().PendingRefunds().ToListAsync());
        await h.H.Bookings().ChangeStatusAsync(replacement, BookingStatus.Rejected, new(ActorType.Owner, account.Id), "Chủ mới không nhận được khách");
        h.Db.ChangeTracker.Clear();
        var queued = Assert.Single(await h.H.Accounting().PendingRefunds().ToListAsync());
        Assert.Equal(replacementId, queued.Id); Assert.Equal(BookingStatus.Rejected, queued.Status); Assert.Equal(paid, queued.RefundAmount);
        Assert.Equal(balanceBefore + discount, (await h.Db.Vouchers.SingleAsync()).Balance);
        var old = await h.Db.Bookings.SingleAsync(b => b.Id == originalId);
        Assert.Equal(BookingStatus.Cancelled, old.Status); Assert.Equal(replacementId, old.ReplacedByBookingId); Assert.Equal(0, old.RefundAmount);
        Assert.Null(SettlementCalculator.ItemFor(old));
        Assert.Empty(SettlementCalculator.Build(old.HomestayId, SettlementCalculator.PeriodOf(old.CheckOut), [old]).Items);
        Assert.Empty(await h.Db.RoomNights.Where(n => n.BookingId == replacementId || n.BookingId == originalId).ToListAsync());
        Assert.False(await h.Db.Payments.AnyAsync(p => p.Type == PaymentType.Refund));
    }

    [Theory]
    [InlineData("owner")] [InlineData("consultant")] [InlineData("manager")] [InlineData("admin")]
    public async Task ReportNotifiesAssignedAndManagersAndAuditsActor(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        var actor = who == "owner" ? h.Owner : who == "manager" ? h.Manager : who == "admin" ? h.P.Admin : h.Consultant;
        await h.Overbooking().ReportAsync(b.Id, "Chủ báo khách kênh khác đang ở", actor);
        var log = await h.Db.AuditLogs.SingleAsync(); Assert.Equal(actor.AccountId, log.AccountId); Assert.Equal(b.Id.ToString(), log.RecordId);
        Assert.Contains("OverbookingReported", log.NewValues);
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Manager.AccountId && n.Message.Contains(b.Code));
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Consultant.AccountId && n.Message.Contains(b.Code));
        Assert.All(h.Db.Notifications, n => Assert.True(n.Message.Length <= 500));
    }

    [Theory]
    [InlineData("deposited")] [InlineData("completed")] [InlineData("checkout")]
    [InlineData("other-owner")] [InlineData("accountant")] [InlineData("customer")]
    [InlineData("pending")] [InlineData("awaiting")]
    public async Task InvalidBookingActorOrOpenRequestBlocksIncident(string kind)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var b = await h.BookingAsync(kind == "deposited" ? BookingStatus.Deposited : kind == "completed" ? BookingStatus.Completed : BookingStatus.Confirmed);
        if (kind == "checkout") h.H.Clock.Advance(TimeSpan.FromDays(7));
        if (kind is "pending" or "awaiting")
        {
            var customer = HomestayTestHost.Principal(h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id, "Customer");
            var r = await h.H.Changes().SubmitAsync(new(b.Id, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(7), b.CheckOut.AddDays(7)), customer);
            if (kind == "awaiting")
                await h.H.Changes().ApproveAsync(r.Id, "Đồng ý đổi", HomestayTestHost.Principal(h.Manager.AccountId!, "Employee"));
            Assert.Equal(kind == "pending" ? ChangeRequestStatus.Pending : ChangeRequestStatus.AwaitingPayment, r.Status);
        }
        var actor = kind == "other-owner" ? new Actor(ActorType.Owner, h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id)
            : kind == "accountant" ? h.P.Accountant : kind == "customer" ? new Actor(ActorType.Customer) : h.Owner;
        var audits = await h.Db.AuditLogs.CountAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().ReportAsync(b.Id, "Không hợp lệ", actor));
        Assert.Equal(audits, await h.Db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task AlternativesPrioritizeSameHomeThenAreaThenOtherWithoutLeakingContact()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        var same = await h.TypeAsync(900000); var area = await h.TypeAsync(200000, sameHome: false, name: "Cùng khu");
        var elsewhere = await h.TypeAsync(100000, sameHome: false, sameArea: false, name: "Khác khu");
        var result = await h.Overbooking().AlternativesAsync(b.Id, h.Consultant);
        Assert.Equal(new[] { b.RoomTypeId, same.Id, area.Id, elsewhere.Id }, result.Select(r => r.Offer.RoomTypeId));
        Assert.All(result, r => { Assert.True(r.Offer.MaxGuests >= b.Guests); Assert.Equal(r.Offer.TotalPrice - b.TotalAmount, r.Difference); });
        Assert.DoesNotContain("Địa chỉ riêng", JsonSerializer.Serialize(result)); Assert.DoesNotContain("+84912000999", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task SameTypeRoomMovePreservesMoneyCodeAndWritesIncidentHistory()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var voucher = await h.VoucherAsync(); var b = await h.BookingAsync(voucherId: voucher.Id);
        var code = b.Code; var money = new[] { b.TotalAmount, b.DepositAmount, b.DiscountAmount, b.CommissionRate, b.DepositRate, voucher.Balance };
        var target = (await h.H.Bookings().FreeRoomIdsAsync(b.RoomTypeId, b.CheckIn, b.CheckOut)).Single();
        await h.Overbooking().MoveRoomAsync(b.Id, target, "Đã thống nhất với khách", h.Consultant);
        Assert.Single(h.Db.Bookings); Assert.Equal(code, b.Code); Assert.Equal(target, b.RoomId); Assert.Equal(BookingStatus.Confirmed, b.Status);
        Assert.Equal(money, new[] { b.TotalAmount, b.DepositAmount, b.DiscountAmount, b.CommissionRate, b.DepositRate, voucher.Balance });
        Assert.All(await h.Db.RoomNights.ToListAsync(), n => { Assert.Equal(b.Id, n.BookingId); Assert.Equal(target, n.RoomId); });
        var history = await h.Db.BookingStatusHistories.OrderBy(x => x.Id).LastAsync();
        Assert.Contains("Đổi phòng do trùng phòng", history.Note); Assert.Equal(h.Consultant.AccountId, history.ActorAccountId);
    }

    [Theory]
    [InlineData(true, true)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(false, false)]
    public async Task ReplacementTransfersSnapshotsVoucherAndCreatesOnlyPositiveCompensation(bool dearer, bool sameHome)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var voucher = await h.VoucherAsync(); var b = await h.BookingAsync(voucherId: voucher.Id);
        var oldId = b.Id; var balance = voucher.Balance; var deposit = b.DepositAmount; var discount = b.DiscountAmount;
        var creator = b.CreatedByEmployeeId; var type = await h.TypeAsync(dearer ? 800000 : 400000, sameHome);
        var price = await h.H.Pricing().QuoteAsync(type.Id, b.CheckIn, b.CheckOut); var previousTotal = b.TotalAmount;
        var n = await h.Overbooking().ReplaceAsync(b.Id, type.Id, "Khách đồng ý phương án mới", h.Manager);
        h.Db.ChangeTracker.Clear(); b = await h.Db.Bookings.SingleAsync(x => x.Id == oldId); n = await h.Db.Bookings.SingleAsync(x => x.Id == n.Id);
        Assert.Equal(BookingStatus.Cancelled, b.Status); Assert.Equal(n.Id, b.ReplacedByBookingId); Assert.Equal(0, b.RefundAmount);
        Assert.Equal(BookingStatus.Deposited, n.Status); Assert.Equal(h.H.Clock.Now.UtcDateTime, n.DepositedAt);
        Assert.Equal(h.H.Clock.Now.UtcDateTime.AddMinutes(h.H.Options.ConfirmMinutes), n.ConfirmDeadlineAt);
        Assert.Equal(b.CustomerId, n.CustomerId); Assert.Equal(b.ContactName, n.ContactName); Assert.Equal(b.ContactPhone, n.ContactPhone);
        Assert.Equal(b.CheckIn, n.CheckIn); Assert.Equal(b.CheckOut, n.CheckOut); Assert.Equal(b.Guests, n.Guests); Assert.Equal(b.SourceChannel, n.SourceChannel);
        Assert.Equal(b.DepositRate, n.DepositRate); Assert.Equal(deposit, n.DepositAmount); Assert.Equal(discount, n.DiscountAmount); Assert.Equal(voucher.Id, n.VoucherId);
        Assert.Equal(balance, (await h.Db.Vouchers.SingleAsync()).Balance); Assert.Equal(creator, n.CreatedByEmployeeId); Assert.Equal(h.Manager.EmployeeId, n.AssignedEmployeeId);
        Assert.Equal(price.Total, n.TotalAmount); Assert.Equal(sameHome ? .12m : .15m, n.CommissionRate);
        Assert.Equal(price.Total, (await h.Db.BookingNights.Where(x => x.BookingId == n.Id).ToListAsync()).Sum(x => x.Price));
        Assert.Empty(await h.Db.RoomNights.Where(x => x.BookingId == b.Id).ToListAsync());
        Assert.Equal(2, await h.Db.RoomNights.CountAsync(x => x.BookingId == n.Id)); Assert.Null(SettlementCalculator.ItemFor(b));
        if (dearer)
        {
            var payment = await h.Db.Payments.SingleAsync(); Assert.Equal(PaymentType.Compensation, payment.Type);
            Assert.Equal(n.Id, payment.BookingId); Assert.Equal(n.TotalAmount - previousTotal, payment.Amount);
            Assert.Equal(PaymentStatus.Submitted, payment.Status); Assert.Equal(PaymentMethod.ManualTransfer, payment.Method);
            Assert.Equal(previousTotal - deposit, n.TotalAmount - n.DepositAmount - payment.Amount);
            Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.TableName == "Payments" && a.RecordId == payment.Id.ToString());
        }
        else Assert.Empty(h.Db.Payments);
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.TableName == "Bookings" && a.RecordId == n.Id.ToString() && a.Action == AuditAction.Insert);
        var messages = await h.Db.Notifications.Where(x => x.UserId == h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id).Select(x => x.Message).ToListAsync();
        Assert.Contains(messages, m => m.Contains(n.Code)); Assert.DoesNotContain(messages, m => m.Contains("Địa chỉ riêng") || m.Contains("+84912000999"));
        await h.H.Bookings().ChangeStatusAsync(n, BookingStatus.Rejected, h.Owner, "Chủ mới từ chối");
        Assert.Equal(n.AmountDue, n.RefundAmount); Assert.Empty(await h.Db.RoomNights.Where(x => x.BookingId == n.Id).ToListAsync());
        Assert.Equal(100000, (await h.Db.Vouchers.SingleAsync()).Balance);
    }

    [Fact]
    public async Task ReplacementBelowDepositRollsBackEveryTableIncludingCodeCounter()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var type = await h.TypeAsync(100000);
        var before = await Snapshot(h.Db);
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().ReplaceAsync(b.Id, type.Id, "Quá thấp", h.Consultant));
        Assert.Contains("nhỏ hơn cọc", ex.Message); Assert.Equal(before, await Snapshot(h.Db));
    }

    [Fact]
    public async Task FujiRefundRestoresFullVoucherIssuesApologyAndQueuesCashRefund()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var v = await h.VoucherAsync(); var b = await h.BookingAsync(voucherId: v.Id);
        var due = b.AmountDue; var result = await h.Overbooking().RefundAsync(b.Id, "Không tìm được phòng khách chấp nhận", h.Manager);
        Assert.Equal(BookingStatus.Cancelled, result.Booking.Status); Assert.Equal(due, result.Booking.RefundAmount); Assert.Null(result.Booking.ReplacedByBookingId);
        Assert.Equal(v.Amount, v.Balance); Assert.Equal(b.Id, result.ApologyVoucher.SourceBookingId);
        Assert.Equal(h.H.Vouchers().IssueAmountFor(b.TotalAmount), result.ApologyVoucher.Amount);
        Assert.Equal(h.Today.AddMonths(6), result.ApologyVoucher.ExpiresOn); Assert.Equal(h.Manager.EmployeeId, result.ApologyVoucher.IssuedByEmployeeId);
        Assert.Empty(h.Db.RoomNights); Assert.Empty(h.Db.Payments); Assert.Single(await h.H.Accounting().PendingRefunds().ToListAsync());
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.TableName == "Vouchers" && a.RecordId == result.ApologyVoucher.Id.ToString());
    }

    [Theory]
    [InlineData("consultant")] [InlineData("owner")] [InlineData("accountant")]
    public async Task OnlyManagementMayChooseFullRefund(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        var actor = who == "consultant" ? h.Consultant : who == "owner" ? h.Owner : h.P.Accountant;
        await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().RefundAsync(b.Id, "Không tìm được", actor));
        Assert.Equal(BookingStatus.Confirmed, (await h.Db.Bookings.SingleAsync()).Status); Assert.Empty(h.Db.Vouchers);
    }

    [Theory]
    [InlineData("owner")] [InlineData("accountant")] [InlineData("customer")]
    public async Task OnlyStaffMayMoveReplaceOrListAlternatives(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var type = await h.TypeAsync(700000);
        var actor = who == "owner" ? h.Owner : who == "accountant" ? h.P.Accountant : new Actor(ActorType.Customer);
        await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().MoveRoomAsync(b.Id, 999, "Không được", actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().ReplaceAsync(b.Id, type.Id, "Không được", actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Overbooking().AlternativesAsync(b.Id, actor));
    }

    [Theory]
    [InlineData("replacement")] [InlineData("refund")] [InlineData("move")]
    public async Task FailureAfterBusinessWritesRollsBackCompleteIncident(string action)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var v = await h.VoucherAsync(); var b = await h.BookingAsync(voucherId: v.Id); var type = await h.TypeAsync(800000);
        var room = (await h.H.Bookings().FreeRoomIdsAsync(b.RoomTypeId, b.CheckIn, b.CheckOut)).Single(); var before = await Snapshot(h.Db);
        await using var db = h.H.NewContext(new FailAudit());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (action == "replacement") await h.Overbooking(db).ReplaceAsync(b.Id, type.Id, "Lỗi giữa chừng", h.Manager);
            else if (action == "refund") await h.Overbooking(db).RefundAsync(b.Id, "Lỗi giữa chừng", h.Manager);
            else await h.Overbooking(db).MoveRoomAsync(b.Id, room, "Lỗi giữa chừng", h.Manager);
        });
        Assert.Equal(before, await Snapshot(h.Db));
    }

    [Fact]
    public async Task ReplacementRoomNightCollisionRollsBackOldAndNewBookingPaymentAndVoucher()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var type = await h.TypeAsync(800000);
        var target = await h.Db.Rooms.SingleAsync(r => r.RoomTypeId == type.Id);
        var block = new RoomBlock { RoomId = target.Id, StartDate = b.CheckIn.AddDays(10), EndDate = b.CheckOut.AddDays(10),
            Reason = RoomBlockReason.OtherChannel, CreatedByAccountId = h.Owner.AccountId! };
        h.Db.Add(block); await h.Db.SaveChangesAsync(); var before = await Snapshot(h.Db);
        await using var db = h.H.NewContext(new CompetingNight(target.Id, b.CheckIn, block.Id));
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Overbooking(db).ReplaceAsync(b.Id, type.Id, "Phòng vừa bị giữ", h.Manager));
        Assert.Contains("vừa có người đặt", ex.Message); Assert.Equal(before, await Snapshot(h.Db));
    }

    [Fact]
    public void ReplacedBookingNeverEntersSettlementEvenWithRetainedDepositOrChangeFees()
    {
        var b = new Booking { Id = 1, HomestayId = 2, CheckOut = new(2026, 11, 15), Status = BookingStatus.Cancelled,
            DepositedAt = DateTime.UtcNow, TotalAmount = 1000000, DepositAmount = 300000, CommissionRate = .12m, RefundAmount = 0, ChangeFeeAmount = 100000, ReplacedByBookingId = 3 };
        Assert.Null(SettlementCalculator.ItemFor(b)); Assert.Empty(SettlementCalculator.Build(2, new(2026, 11, 1), [b]).Items);
    }

    private static async Task<string> Snapshot(ApplicationDbContext db) => JsonSerializer.Serialize(new {
        Bookings = await db.Bookings.AsNoTracking().OrderBy(b => b.Id).Select(b => new { b.Id, b.Status, b.RoomId, b.RefundAmount, b.ReplacedByBookingId }).ToListAsync(),
        Nights = await db.RoomNights.AsNoTracking().OrderBy(n => n.RoomId).ThenBy(n => n.Date).Select(n => new { n.RoomId, n.Date, n.BookingId, n.RoomBlockId }).ToListAsync(),
        Prices = await db.BookingNights.AsNoTracking().OrderBy(n => n.BookingId).ThenBy(n => n.Date).Select(n => new { n.BookingId, n.Date, n.Price }).ToListAsync(),
        Payments = await db.Payments.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.Amount, p.BookingId, p.Type }).ToListAsync(),
        Vouchers = await db.Vouchers.AsNoTracking().OrderBy(v => v.Id).Select(v => new { v.Id, v.Balance, v.Status }).ToListAsync(),
        Counters = await db.BookingCodeCounters.AsNoTracking().OrderBy(c => c.Date).Select(c => new { c.Date, c.LastSeq }).ToListAsync(),
        History = await db.BookingStatusHistories.AsNoTracking().OrderBy(h => h.Id).Select(h => new { h.Id, h.BookingId, h.ToStatus }).ToListAsync(),
        Audit = await db.AuditLogs.CountAsync(), Notifications = await db.Notifications.CountAsync() });
    private sealed class FailAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added)) throw new InvalidOperationException("Lỗi audit giả lập");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class CompetingNight(int room, DateOnly date, int block) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await eventData.Context!.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoomNights (RoomId, Date, RoomBlockId) VALUES ({room}, {date}, {block})", cancellationToken);
            return result;
        }
    }
}
