using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class NotificationServiceTests
{
    private static async Task<(BookingTestHost H, string Me, string Other)> HostAsync()
    {
        var h = new BookingTestHost();
        var users = await h.Db.Users.Select(u => u.Id).Take(2).ToListAsync();
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 25; i++)
            h.Db.Notifications.Add(new Notification { UserId = users[0], Message = $"Tin {i}", CreatedAt = start.AddMinutes(i), IsRead = i < 5 });
        h.Db.Notifications.Add(new Notification { UserId = users[1], Message = "Của người khác", CreatedAt = start });
        await h.Db.SaveChangesAsync();
        return (h, users[0], users[1]);
    }

    [Fact]
    public async Task ListsOnlyMineNewestFirstWithPagingAndUnreadCount()
    {
        var (h, me, _) = await HostAsync(); using var _h = h; var sut = new NotificationService(h.Db);
        var page1 = await sut.ListAsync(me, 1, 10);
        Assert.Equal(25, page1.TotalCount); Assert.Equal(3, page1.TotalPages);
        Assert.Equal("Tin 24", page1[0].Message); Assert.All(page1, n => Assert.Equal(me, n.UserId));
        Assert.Equal(new[] { "Tin 24", "Tin 23" }, (await sut.LatestAsync(me, 2)).Select(n => n.Message));
        Assert.Equal(20, await sut.UnreadCountAsync(me));
    }

    [Fact]
    public async Task MarkReadOnlyTouchesOwnNotifications()
    {
        var (h, me, other) = await HostAsync(); using var _h = h; var sut = new NotificationService(h.Db);
        var foreign = await h.Db.Notifications.SingleAsync(n => n.UserId == other);
        Assert.False(await sut.MarkReadAsync(foreign.Id, me));
        Assert.Equal(1, await sut.UnreadCountAsync(other));
        var mine = await h.Db.Notifications.FirstAsync(n => n.UserId == me && !n.IsRead);
        Assert.True(await sut.MarkReadAsync(mine.Id, me));
        Assert.Equal(19, await sut.UnreadCountAsync(me));
        Assert.Equal(19, await sut.MarkAllReadAsync(me));
        Assert.Equal(0, await sut.UnreadCountAsync(me)); Assert.Equal(1, await sut.UnreadCountAsync(other));
    }
}
