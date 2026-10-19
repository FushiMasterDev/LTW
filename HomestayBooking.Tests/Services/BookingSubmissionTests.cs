using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class BookingSubmissionTests
{
    [Fact]
    public async Task ReplayAcrossContextsReturnsOriginalBookingWithoutHoldingAnotherRoom()
    {
        using var host = new BookingTestHost();
        var request = host.Request(new(2026, 11, 20), new(2026, 11, 22)) with { SubmissionId = Guid.NewGuid() };
        var first = await host.Bookings().CreateAsync(request);
        await using var other = host.NewContext();
        var replay = await host.Bookings(other).CreateAsync(request);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(1, await other.Bookings.CountAsync());
        Assert.Equal(2, await other.RoomNights.CountAsync());
    }

    [Fact]
    public async Task ReplayAfterCancellationDoesNotCreateAnotherBooking()
    {
        using var host = new BookingTestHost();
        var request = host.Request(new(2026, 11, 20), new(2026, 11, 22)) with { SubmissionId = Guid.NewGuid() };
        var first = await host.Bookings().CreateAsync(request);
        await host.Bookings().ChangeStatusAsync(first, BookingStatus.Cancelled, request.Actor);
        var replay = await host.Bookings().CreateAsync(request);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(BookingStatus.Cancelled, replay.Status);
        Assert.Empty(await host.Db.RoomNights.ToListAsync());
    }

    [Fact]
    public async Task SeparateFormsCanStillBookTwoRoomsForSameStay()
    {
        using var host = new BookingTestHost();
        var request = host.Request(new(2026, 11, 20), new(2026, 11, 22));
        var first = await host.Bookings().CreateAsync(request with { SubmissionId = Guid.NewGuid() });
        var second = await host.Bookings().CreateAsync(request with { SubmissionId = Guid.NewGuid() });
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.RoomId, second.RoomId);
    }
}
