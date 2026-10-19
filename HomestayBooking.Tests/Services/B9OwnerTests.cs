using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class B9OwnerTests
{
    [Fact]
    public async Task EditorCatalogIsCompleteForInactiveHomeAndMarksSelection()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var selected = new Amenity { Name = "Đã chọn", Icon = "bi-wifi" };
        var unused = new Amenity { Name = "Chưa sử dụng" };
        var category = new RoomCategory { Name = "Danh mục chưa sử dụng" };
        h.Db.AddRange(selected, unused, category); await h.Db.SaveChangesAsync();
        h.Db.HomestayAmenities.Add(new() { HomestayId = h.HomeId, AmenityId = selected.Id });
        h.Home.Status = HomestayStatus.Suspended; await h.Db.SaveChangesAsync();
        var editor = await h.Content.EditorAsync(h.HomeId, h.Owner);
        Assert.Contains(editor.Amenities, a => a.Id == selected.Id && a.Selected && a.Icon == "bi-wifi");
        Assert.Contains(editor.Amenities, a => a.Id == unused.Id && !a.Selected);
        Assert.Contains(editor.Categories, c => c.Id == category.Id);
        await Assert.ThrowsAsync<BookingException>(() => h.Content.EditorAsync(h.HomeId, new(ActorType.Owner, "other")));
    }

    [Fact]
    public async Task SummaryCountsOnlyOwnDepositedAndTodaysArrivalsAndBlocksNonOwner()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        await h.BookAsync(0); var overdue = await h.BookAsync(3, BookingStatus.Deposited);
        overdue.ConfirmDeadlineAt = h.H.Clock.Now.UtcDateTime.AddMinutes(-1);
        await h.BookAsync(6, BookingStatus.Deposited); await h.Db.SaveChangesAsync();
        var result = await h.Schedule.OwnerBookingSummaryAsync(h.B.Owner);
        Assert.Equal(new OwnerBookingSummary(2, 1, 1), result);
        await Assert.ThrowsAsync<BookingException>(() => h.Schedule.OwnerBookingSummaryAsync(h.Customer));
        await Assert.ThrowsAsync<BookingException>(() => h.Schedule.OwnerBookingSummaryAsync(h.P.Consultant));
        // A genuine second owner must see zero, even when the first has overdue orders.
        var second = new Owner { Person = new() { FullName = "Chủ khác", PhoneNumber = "+84911000222" } };
        h.Db.Owners.Add(second); await h.Db.SaveChangesAsync();
        h.Db.Users.Add(new() { Id = "other-owner", PersonId = second.PersonId, UserName = "other-owner", NormalizedUserName = "OTHER-OWNER", Status = AccountStatus.Active });
        await h.Db.SaveChangesAsync();
        Assert.Equal(new OwnerBookingSummary(0, 0, 0), await h.Schedule.OwnerBookingSummaryAsync(new(ActorType.Owner, "other-owner")));
    }

    [Theory]
    [InlineData(BookingStatus.Deposited)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    public async Task BookingNotificationsLinkToRecipientsWorkspace(BookingStatus to)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var b = await h.B.BookingAsync(to is BookingStatus.Deposited or BookingStatus.Expired ? BookingStatus.AwaitingPayment : BookingStatus.Deposited);
        var before = await h.Db.Notifications.MaxAsync(n => (int?)n.Id) ?? 0;
        await h.H.Bookings().ChangeStatusAsync(b, to, Actor.System, "Thử thông báo B9");
        var notices = await h.Db.Notifications.Where(n => n.Id > before).ToListAsync();
        Assert.NotEmpty(notices);
        foreach (var n in notices)
            Assert.Equal(n.UserId == h.B.Owner.AccountId ? $"/Owner/Bookings/Details/{b.Id}"
                : n.UserId == h.P.Consultant.AccountId ? $"/Staff/Bookings/{b.Id}" : $"/Bookings/Details/{b.Code}", n.Url);
    }
}
