using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayEditorTests
{
    [Fact]
    public async Task OwnerListsOnlyOwnHomestaysWithCoverAndRoomCount()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var list = await h.Content.OwnerHomestaysAsync(h.Owner);
        var row = Assert.Single(list);
        Assert.Equal(h.HomeId, row.Id); Assert.Equal("/images/demo/cover.jpg", row.CoverUrl);
        Assert.Equal(await h.Db.Rooms.CountAsync(r => r.RoomType.HomestayId == h.HomeId), row.RoomCount);
        var otherOwner = new Actor(ActorType.Owner, h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id);
        Assert.Empty(await h.Content.OwnerHomestaysAsync(otherOwner));
        await Assert.ThrowsAsync<BookingException>(() => h.Content.OwnerHomestaysAsync(h.Manager));
    }

    [Fact]
    public async Task EditorSplitsCurrentAndUpcomingPricesAndHidesDocumentFiles()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var typeId = h.H.RoomType.Id;
        h.Db.RoomTypePrices.Add(new() { RoomTypeId = typeId, DayType = DayType.Weekday, Price = 999000, EffectiveFrom = h.Today.AddDays(30) });
        h.Db.RoomTypePrices.Add(new() { RoomTypeId = typeId, DayType = DayType.Weekday, Price = 111000, EffectiveFrom = h.Today.AddYears(-5) });
        await h.Db.SaveChangesAsync();
        var editor = await h.Content.EditorAsync(h.HomeId, h.Owner);
        var type = editor.RoomTypes.Single(t => t.Id == typeId);
        var weekday = type.CurrentPrices.Single(p => p.DayType == DayType.Weekday);
        Assert.True(weekday.EffectiveFrom <= h.Today); Assert.NotEqual(111000, weekday.Price);
        Assert.Equal(type.CurrentPrices.Count, type.CurrentPrices.Select(p => p.DayType).Distinct().Count());
        Assert.Contains(type.UpcomingPrices, p => p.Price == 999000 && p.EffectiveFrom == h.Today.AddDays(30));
        Assert.All(type.UpcomingPrices, p => Assert.True(p.EffectiveFrom > h.Today));
        Assert.Equal(3, editor.Documents.Count); Assert.True(editor.Images[0].IsCover);
        Assert.Equal((await h.Content.EditorAsync(h.HomeId, h.Manager)).Name, editor.Name);
    }

    [Fact]
    public async Task EditorBlocksOtherOwnerAndAccountant()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var otherOwner = new Actor(ActorType.Owner, h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id);
        await Assert.ThrowsAsync<BookingException>(() => h.Content.EditorAsync(h.HomeId, otherOwner));
        await Assert.ThrowsAsync<BookingException>(() => h.Content.EditorAsync(h.HomeId, h.P.Accountant));
    }
}

public sealed class PendingHomestayDetailsTests
{
    [Fact]
    public async Task ManagerSeesFullContentOfReReviewOthersAreBlocked()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        h.Home.ApprovedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.PendingDetailsAsync(h.HomeId, h.Manager));
        await h.Content.UpdateLocationAsync(h.HomeId, new("99 Đường mới", h.Home.AreaId, null, null), h.Owner);
        var details = await h.Reviews.PendingDetailsAsync(h.HomeId, h.Manager);
        Assert.Equal(h.HomeId, details.Content.Id); Assert.Equal("99 Đường mới", details.Content.Address);
        Assert.False(details.FirstReview); Assert.Equal("+84912000001", details.OwnerPhone); Assert.Equal(3, details.Content.Documents.Count);
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.PendingDetailsAsync(h.HomeId, h.P.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.PendingDetailsAsync(h.HomeId, h.Owner));
    }
}
