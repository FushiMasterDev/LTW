using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class OwnerScheduleServiceTests
{
    [Fact]
    public async Task ScheduleIncludesBoundaryArrivalsAndDeparturesWithGuestDetails()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var arrival = await h.BookAsync(1); arrival.SpecialRequests = "Nhận phòng muộn";
        var departure = await h.BookAsync(0, BookingStatus.CheckedIn);
        await h.BookAsync(4); await h.Db.SaveChangesAsync();
        var result = await h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.Today.AddDays(1), h.Today.AddDays(2), h.B.Owner);
        var a = Assert.Single(result.Arrivals); var d = Assert.Single(result.Departures);
        Assert.Equal(arrival.Id, a.BookingId); Assert.Equal(departure.Id, d.BookingId);
        Assert.Equal(arrival.ContactName, a.ContactName); Assert.Equal(arrival.ContactPhone, a.ContactPhone);
        Assert.Equal(arrival.Guests, a.Guests); Assert.Equal(await h.Db.Rooms.Where(r => r.Id == arrival.RoomId).Select(r => r.RoomCode).SingleAsync(), a.RoomCode); Assert.Equal("Nhận phòng muộn", a.SpecialRequests);
        Assert.Equal(2, (await h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.P.Manager)).Arrivals.Count);
        Assert.Single((await h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.Today, h.Today.AddDays(30), h.P.Admin)).Departures);
    }

    [Theory]
    [InlineData("other-owner")] [InlineData("consultant")] [InlineData("accountant")]
    [InlineData("customer")] [InlineData("locked")] [InlineData("other-home")]
    public async Task UnauthorizedScheduleReadIsRejected(string bad)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var actor = bad == "other-owner" ? new Actor(ActorType.Owner, h.Customer.AccountId)
            : bad == "consultant" ? h.P.Consultant : bad == "accountant" ? h.P.Accountant : bad == "customer" ? h.Customer : h.B.Owner;
        if (bad == "locked") { (await h.Db.Users.SingleAsync(u => u.Id == actor.AccountId)).Status = AccountStatus.Locked; await h.Db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<BookingException>(() => h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.Today, h.Today, actor, bad == "other-home" ? -1 : null));
    }

    [Theory]
    [InlineData(0, true)] [InlineData(30, true)] [InlineData(31, false)] [InlineData(-1, false)]
    public async Task InclusiveDateRangeIsLimitedTo31Days(int offset, bool allowed)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        if (allowed) Assert.Empty((await h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.Today, h.Today.AddDays(offset), h.B.Owner)).Arrivals);
        else await Assert.ThrowsAsync<BookingException>(() => h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.Today, h.Today.AddDays(offset), h.B.Owner));
    }
}
