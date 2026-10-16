using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class RoomManagementTests
{
    [Fact]
    public async Task CreateAndEditTypeRoomWithCompletePricesAndTrueAuditIds()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var t = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(), h.Owner);
        Assert.Equal(3, t.Prices.Count); Assert.All(t.Prices, p => Assert.Equal(h.Today, p.EffectiveFrom));
        await h.Rooms.UpdateTypeAsync(t.Id, h.TypeInput("Tên đã sửa"), h.Manager);
        var room = await h.Rooms.AddRoomAsync(t.Id, "A1", h.Owner); await h.Rooms.RenameRoomAsync(room.Id, "A2", h.Owner);
        Assert.Equal("A2", room.RoomCode); Assert.Equal("Tên đã sửa", t.Name);
        Assert.NotNull(await h.Db.AuditLogs.SingleAsync(a => a.TableName == "RoomTypes" && a.RecordId == t.Id.ToString() && a.Action == AuditAction.Insert));
        Assert.NotNull(await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Rooms" && a.RecordId == room.Id.ToString() && a.Action == AuditAction.Insert));
    }
    [Theory]
    [InlineData("missing")] [InlineData("zero")] [InlineData("category")] [InlineData("capacity")] [InlineData("duplicate")]
    public async Task InvalidTypeCreationIsRejected(string bad)
    {
        using var h = await HomestayTestHost.CreateAsync(); var input = h.TypeInput(); var prices = HomestayTestHost.Prices();
        if (bad == "missing") prices.Remove(DayType.Holiday);
        if (bad == "zero") prices[DayType.Weekday] = 0;
        if (bad == "category") input = input with { CategoryId = 99999 };
        if (bad == "capacity") input = input with { MaxGuests = 0 };
        if (bad == "duplicate") input = input with { Name = h.H.RoomType.Name };
        await Assert.ThrowsAsync<BookingException>(() => h.Rooms.CreateTypeAsync(h.HomeId, input, prices, h.Owner));
        Assert.Single(h.Db.RoomTypes); Assert.Empty(h.Db.AuditLogs);
    }
    [Fact]
    public async Task DuplicateRoomCodeIsRejectedButDifferentTypeCanReuseCode()
    {
        using var h = await HomestayTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Rooms.AddRoomAsync(h.H.RoomType.Id, "101", h.Owner));
        var t = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(), h.Owner);
        var room = await h.Rooms.AddRoomAsync(t.Id, "101", h.Owner); Assert.Equal("101", room.RoomCode);
        var second = await h.Rooms.AddRoomAsync(t.Id, "102", h.Owner);
        await Assert.ThrowsAsync<BookingException>(() => h.Rooms.RenameRoomAsync(second.Id, "101", h.Owner));
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task DisableBlocksCreateFreeAndChangedRoomConfirmationButPreservesOldBooking(bool type)
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.DepositedAsync();
        // Còn một loại/phòng khác để cơ sở Active không bị tắt toàn bộ khả năng bán.
        var spare = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput("Phòng dự phòng"), HomestayTestHost.Prices(), h.Owner);
        await h.Rooms.AddRoomAsync(spare.Id, "DP1", h.Owner);
        var original = await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id);
        var rooms = await h.Db.Rooms.Where(r => r.RoomTypeId == b.RoomTypeId).ToListAsync();
        RoomAvailabilityChange change;
        if (type) change = await h.Rooms.SetTypeActiveAsync(b.RoomTypeId, false, h.Owner);
        else
        {
            change = await h.Rooms.SetRoomActiveAsync(b.RoomId, false, h.Owner);
            foreach (var room in rooms.Where(r => r.Id != b.RoomId)) await h.Rooms.SetRoomActiveAsync(room.Id, false, h.Owner);
        }
        Assert.Contains(b.Code, change.UpcomingBookingCodes);
        Assert.Empty(await h.H.Bookings().FreeRoomIdsAsync(b.RoomTypeId, b.CheckIn.AddDays(10), b.CheckOut.AddDays(10)));
        await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().CreateAsync(h.H.Request(b.CheckIn.AddDays(10), b.CheckOut.AddDays(10))));
        var other = rooms.Single(r => r.Id != b.RoomId);
        await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().ConfirmAsync(b, other.Id, h.Owner));
        Assert.Equal(original, await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        b = await h.Db.Bookings.SingleAsync();
        // Xác nhận phòng cũ của đơn đã nhận vẫn được: không phải chọn một phòng mới đã tắt.
        await h.H.Bookings().ConfirmAsync(b, null, h.Owner); Assert.Equal(BookingStatus.Confirmed, b.Status);
        if (type) await h.Rooms.SetTypeActiveAsync(b.RoomTypeId, true, h.Owner);
        else foreach (var room in rooms) await h.Rooms.SetRoomActiveAsync(room.Id, true, h.Owner);
        Assert.NotEmpty(await h.H.Bookings().FreeRoomIdsAsync(b.RoomTypeId, b.CheckIn.AddDays(10), b.CheckOut.AddDays(10)));
    }
    [Fact]
    public async Task FuturePriceVersionIsUpdatedAndOldBookingsKeepTheirNightSnapshots()
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.BookAsync(); var total = b.TotalAmount;
        var day = h.Today.AddDays(2);
        var p = await h.Rooms.SetPriceAsync(b.RoomTypeId, DayType.Weekday, day, 900000, h.Owner);
        var again = await h.Rooms.SetPriceAsync(b.RoomTypeId, DayType.Weekday, day, 950000, h.Owner); Assert.Equal(p.Id, again.Id);
        Assert.Equal(500000, (await h.H.Pricing().QuoteAsync(b.RoomTypeId, day.AddDays(-1), day)).Total);
        Assert.Equal(950000, (await h.H.Pricing().QuoteAsync(b.RoomTypeId, day, day.AddDays(1))).Total);
        Assert.Equal(total, (await h.Db.Bookings.SingleAsync()).TotalAmount);
        Assert.Equal(total, (await h.Db.BookingNights.Where(n => n.BookingId == b.Id).ToListAsync()).Sum(n => n.Price));
    }
    [Theory]
    [InlineData("past")] [InlineData("effectiveToday")] [InlineData("zero")]
    public async Task PastOrEffectivePriceCannotBeRewritten(string bad)
    {
        using var h = await HomestayTestHost.CreateAsync();
        var t = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(), h.Owner);
        await Assert.ThrowsAsync<BookingException>(() => h.Rooms.SetPriceAsync(t.Id, DayType.Weekday,
            bad == "past" ? h.Today.AddDays(-1) : bad == "effectiveToday" ? h.Today : h.Today.AddDays(1), bad == "zero" ? 0 : 700000, h.Owner));
        Assert.All(t.Prices, p => Assert.Equal(600000, p.Price));
    }
    [Theory]
    [InlineData("quote", true)] [InlineData("submit", true)] [InlineData("approve", true)] [InlineData("finance", true)]
    [InlineData("quote", false)] [InlineData("submit", false)] [InlineData("approve", false)] [InlineData("finance", false)]
    public async Task EveryChangeSelectionStepRejectsDisabledTargetTypeOrRoom(string step, bool type)
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.DepositedAsync(10);
        var target = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(1000000), h.Owner);
        var room = await h.Rooms.AddRoomAsync(target.Id, "NEW", h.Owner);
        var input = new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeRoomType, NewRoomTypeId: target.Id);
        BookingChangeRequest? request = null;
        if (step is "approve" or "finance") request = await h.H.Changes().SubmitAsync(input, h.Customer);
        if (step == "finance")
        {
            await h.H.Changes().ApproveAsync(request!.Id, "Đồng ý", h.Management);
            Assert.Equal(ChangeRequestStatus.AwaitingPayment, request.Status);
            await h.H.Receipts().SubmitChangeRequestAsync(request.Id, "test-bill.png", null, h.Customer);
        }
        if (type) await h.Rooms.SetTypeActiveAsync(target.Id, false, h.Owner); else await h.Rooms.SetRoomActiveAsync(room.Id, false, h.Owner);
        if (step == "quote") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().QuoteAsync(input, h.Customer));
        if (step == "submit") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().SubmitAsync(input, h.Customer));
        if (step == "approve") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().ApproveAsync(request!.Id, "Đồng ý", h.Management));
        if (step == "finance") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().ConfirmAsync(request!.Id, "Đã nhận", HomestayTestHost.Principal(h.P.Accountant.AccountId!, "Employee")));
        Assert.Equal(b.RoomTypeId, (await h.Db.Bookings.SingleAsync()).RoomTypeId);
        Assert.Equal(2, await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
    }
}
