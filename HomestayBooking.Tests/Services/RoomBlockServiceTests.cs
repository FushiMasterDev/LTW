using System.Data.Common;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class RoomBlockServiceTests
{
    [Fact]
    public async Task ConflictingBookingReturnsCodeDatesAndLeavesNoBlock()
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.BookAsync();
        var ex = await Assert.ThrowsAsync<RoomBlockConflictException>(() => h.Blocks.CreateAsync(b.RoomId, b.CheckIn, b.CheckOut, RoomBlockReason.Maintenance, "Sửa", h.Owner));
        Assert.Contains(b.Code, ex.Message); Assert.Equal(2, ex.Conflicts.Count); Assert.Contains(ex.Conflicts, c => c.Date == b.CheckIn);
        Assert.Empty(h.Db.RoomBlocks); Assert.Empty(h.Db.AuditLogs);
    }
    [Theory]
    [InlineData(-1, 2, 0)] [InlineData(1, 0, 0)] [InlineData(1, 2, 256)]
    public async Task InvalidBlockIsRejected(int days, int length, int noteLength)
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync(); var start = h.Today.AddDays(days);
        await Assert.ThrowsAsync<BookingException>(() => h.Blocks.CreateAsync(room.Id, start, start.AddDays(length), RoomBlockReason.Maintenance, new string('X', noteLength), h.Owner));
        Assert.Empty(h.Db.RoomBlocks);
    }
    [Theory]
    [InlineData(0)] [InlineData(2)]
    public async Task UnlockStartingTodayOrFutureDeletesBlockAndCascadeNights(int startsIn)
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync();
        var block = await h.Blocks.CreateAsync(room.Id, h.Today.AddDays(startsIn), h.Today.AddDays(startsIn + 3), RoomBlockReason.OtherChannel, null, h.Owner);
        Assert.Equal(3, await h.Db.RoomNights.CountAsync(n => n.RoomBlockId == block.Id));
        await h.Blocks.UnlockAsync(block.Id, h.Owner); Assert.Empty(h.Db.RoomBlocks); Assert.Empty(h.Db.RoomNights);
        Assert.NotNull(await h.Db.AuditLogs.SingleAsync(a => a.TableName == "RoomBlocks" && a.RecordId == block.Id.ToString() && a.Action == AuditAction.Delete));
    }
    [Fact]
    public async Task UnlockOngoingKeepsPastAndCropsEndWithoutBreakingDateCheck()
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync(); var start = h.Today;
        var block = await h.Blocks.CreateAsync(room.Id, start, start.AddDays(4), RoomBlockReason.Maintenance, null, h.Owner);
        h.H.Clock.Advance(TimeSpan.FromDays(2)); await h.Blocks.UnlockAsync(block.Id, h.Manager);
        Assert.Equal(h.Today, block.EndDate); Assert.Equal(start, block.StartDate);
        var nights = await h.Db.RoomNights.Where(n => n.RoomBlockId == block.Id).ToListAsync();
        Assert.Equal(2, nights.Count); Assert.All(nights, n => Assert.True(n.Date < h.Today));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)]
    public async Task EndedBlockThrowsWithoutAddingAudit(int extraDays)
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync();
        var block = await h.Blocks.CreateAsync(room.Id, h.Today, h.Today.AddDays(1), RoomBlockReason.Maintenance, null, h.Owner);
        h.H.Clock.Advance(TimeSpan.FromDays(1 + extraDays)); var audits = await h.Db.AuditLogs.CountAsync();
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Blocks.UnlockAsync(block.Id, h.Owner));
        Assert.Equal("Khóa ngày đã kết thúc, không cần mở khóa.", ex.Message); Assert.Equal(audits, await h.Db.AuditLogs.CountAsync());
        Assert.Single(h.Db.RoomBlocks); Assert.Single(h.Db.RoomNights);
    }
    [Fact]
    public async Task ConflictsIncludeOtherBlockAndHeldChangeRequest()
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync();
        var first = await h.Blocks.CreateAsync(room.Id, h.Today.AddDays(1), h.Today.AddDays(2), RoomBlockReason.Maintenance, null, h.Manager);
        var blocked = await Assert.ThrowsAsync<RoomBlockConflictException>(() => h.Blocks.CreateAsync(room.Id, first.StartDate, first.EndDate, RoomBlockReason.OtherChannel, null, h.Owner));
        Assert.Contains("BL" + first.Id, blocked.Message);
        var b = await h.DepositedAsync(); var input = new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(7), b.CheckOut.AddDays(7));
        var r = await h.H.Changes().SubmitAsync(input, h.Customer); await h.H.Changes().ApproveAsync(r.Id, "Đồng ý", h.Management);
        Assert.Equal(ChangeRequestStatus.AwaitingPayment, r.Status);
        var held = await Assert.ThrowsAsync<RoomBlockConflictException>(() => h.Blocks.CreateAsync(r.NewRoomId!.Value, r.NewCheckIn!.Value, r.NewCheckOut!.Value, RoomBlockReason.Maintenance, null, h.Owner));
        Assert.Contains("CR" + r.Id, held.Message); Assert.Contains(b.Code, held.Message);
    }
    [Fact]
    public async Task CalendarReturnsFourStatesUsingOneRoomNightQuery()
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.DepositedAsync();
        await h.Blocks.CreateAsync(b.RoomId, h.Today.AddDays(1), h.Today.AddDays(2), RoomBlockReason.Maintenance, null, h.Owner);
        var r = await h.H.Changes().SubmitAsync(new(b.Id, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(7), b.CheckOut.AddDays(7)), h.Customer);
        await h.H.Changes().ApproveAsync(r.Id, "Đồng ý", h.Management);
        var counter = new NightQueryCounter(); await using var db = h.H.NewContext(counter);
        var service = new RoomBlockService(db, new HomestayAccessService(db, h.P.Registrations), h.H.Clock);
        var calendar = await service.CalendarAsync(h.HomeId, h.Today, h.Today.AddDays(15), h.Owner);
        Assert.Equal(30, calendar.Count); Assert.Equal(1, counter.Queries);
        Assert.Equal(4, calendar.Select(n => n.State).Distinct().Count());
        Assert.Contains(calendar, n => n.State == CalendarNightState.Booked && n.Code == b.Code && n.BookingStatus == BookingStatus.Deposited);
        Assert.Contains(calendar, n => n.State == CalendarNightState.Blocked && n.BlockReason == RoomBlockReason.Maintenance);
        Assert.Contains(calendar, n => n.State == CalendarNightState.ChangeRequestHeld && n.Code == "CR" + r.Id);
    }
    [Theory]
    [InlineData(62, true)] [InlineData(63, false)] [InlineData(0, false)]
    public async Task CalendarRangeBoundary(int days, bool allowed)
    {
        using var h = await HomestayTestHost.CreateAsync();
        if (allowed) Assert.Equal(124, (await h.Blocks.CalendarAsync(h.HomeId, h.Today, h.Today.AddDays(days), h.Owner)).Count);
        else await Assert.ThrowsAsync<BookingException>(() => h.Blocks.CalendarAsync(h.HomeId, h.Today, h.Today.AddDays(days), h.Owner));
    }
    [Theory]
    [InlineData("otherOwner")] [InlineData("consultant")] [InlineData("accountant")]
    public async Task CalendarAndBlockRequireOwnerOrManagement(string who)
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync();
        var actor = who == "consultant" ? h.P.Consultant : who == "accountant" ? h.P.Accountant : new Actor(ActorType.Owner, h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id);
        await Assert.ThrowsAsync<BookingException>(() => h.Blocks.CalendarAsync(h.HomeId, h.Today, h.Today.AddDays(1), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Blocks.CreateAsync(room.Id, h.Today, h.Today.AddDays(1), RoomBlockReason.Maintenance, null, actor));
    }
    private sealed class NightQueryCounter : DbCommandInterceptor
    {
        public int Queries { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"RoomNights\"")) Queries++;
            return ValueTask.FromResult(result);
        }
    }
}
