using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayContentTests
{
    [Fact]
    public async Task ContentSanitizesBothHtmlFieldsReplacesAmenitiesAndKeepsStatus()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var amenity = new Amenity { Name = "Wifi" }; h.Db.Add(amenity); await h.Db.SaveChangesAsync();
        var input = h.ContentInput("<p>Đẹp</p><script>alert(1)</script><img src=x onerror=alert(1)>") with {
            CheckinGuideHtml = "<a href='javascript:alert(1)'>X</a><script>x()</script>", AmenityIds = [amenity.Id] };
        var home = await h.Content.UpdateAsync(h.HomeId, input, h.Owner);
        Assert.Equal(HomestayStatus.Active, home.Status); Assert.Equal("+84912000001", home.ContactPhone);
        Assert.DoesNotContain("<script", home.DescriptionHtml); Assert.DoesNotContain("onerror", home.DescriptionHtml);
        Assert.DoesNotContain("javascript:", home.CheckinGuideHtml); Assert.DoesNotContain("<script", home.CheckinGuideHtml);
        Assert.Single(h.Db.HomestayAmenities); await h.Content.UpdateAsync(h.HomeId, h.ContentInput(), h.Manager);
        Assert.Empty(h.Db.HomestayAmenities);
        var log = await h.Db.AuditLogs.OrderBy(a => a.Id).FirstAsync(); Assert.Equal(h.Owner.AccountId, log.AccountId);
        Assert.Contains("DescriptionHtml", log.OldValues); Assert.Contains("DescriptionHtml", log.NewValues);
    }
    [Theory]
    [InlineData("name")] [InlineData("phone")] [InlineData("amenity")]
    public async Task InvalidContentRollsBack(string bad)
    {
        using var h = await HomestayTestHost.CreateAsync(); var original = h.Home.Name;
        var input = bad == "name" ? h.ContentInput() with { Name = " " } : bad == "phone" ? h.ContentInput() with { ContactPhone = "bad" } : h.ContentInput() with { AmenityIds = [999999] };
        await Assert.ThrowsAsync<BookingException>(() => h.Content.UpdateAsync(h.HomeId, input, h.Owner));
        Assert.Equal(original, h.Home.Name); Assert.Empty(h.Db.AuditLogs);
    }
    [Theory]
    [InlineData("otherOwner")] [InlineData("consultant")] [InlineData("accountant")]
    [InlineData("locked")] [InlineData("employeeAsOwner")] [InlineData("ended")]
    public async Task SharedAccessBlocksUnauthorizedEditingAcrossModules(string who)
    {
        using var h = await HomestayTestHost.CreateAsync(); Actor actor = h.Owner;
        if (who == "otherOwner") actor = new(ActorType.Owner, h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id);
        if (who == "consultant") actor = h.P.Consultant;
        if (who == "accountant") actor = h.P.Accountant;
        if (who == "employeeAsOwner") actor = h.Manager with { Type = ActorType.Owner };
        if (who == "locked") (await h.Db.Users.SingleAsync(u => u.Id == h.Owner.AccountId)).Status = AccountStatus.Locked;
        if (who == "ended") h.Home.Status = HomestayStatus.Ended;
        await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Content.UpdateAsync(h.HomeId, h.ContentInput(), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Images.UploadAsync(h.HomeId, null, PartnerTestHost.Image(), false, 0, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Mới", [new(0, 0)], actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Documents.UploadAsync(h.HomeId, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor));
    }
    [Theory]
    [InlineData(HomestayStatus.Active)] [InlineData(HomestayStatus.Suspended)]
    [InlineData(HomestayStatus.Draft)] [InlineData(HomestayStatus.PendingReview)]
    public async Task LocationChangeRequeuesOnlyActiveAndPreservesBookings(HomestayStatus status)
    {
        using var h = await HomestayTestHost.CreateAsync(); var b = await h.DepositedAsync();
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        var code = b.Code; var room = b.RoomId; var checkIn = b.CheckIn; var nights = await h.Db.RoomNights.CountAsync();
        h.Home.Status = status; await h.Db.SaveChangesAsync();
        var result = await h.Content.UpdateLocationAsync(h.HomeId, new("Địa chỉ mới", h.Home.AreaId, 21m, 105m), h.Owner);
        Assert.Equal(status == HomestayStatus.Active ? HomestayStatus.PendingReview : status, result.Homestay.Status);
        Assert.Contains(code, result.UpcomingBookingCodes); Assert.Equal(nights, await h.Db.RoomNights.CountAsync());
        var persisted = await h.Db.Bookings.SingleAsync(); Assert.Equal(room, persisted.RoomId); Assert.Equal(checkIn, persisted.CheckIn); Assert.Equal(BookingStatus.Confirmed, persisted.Status);
        if (status is HomestayStatus.Active or HomestayStatus.Suspended)
        {
            Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Manager.AccountId && n.Message.Contains(code));
            await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().QuoteAsync(new(b.Id, ChangeRequestType.ChangeDates, checkIn.AddDays(7), b.CheckOut.AddDays(7)), h.Customer));
        }
    }
    [Theory]
    [InlineData("area")] [InlineData("latitude")] [InlineData("longitude")]
    public async Task EachLocationFieldTriggersReviewAndInvalidCoordinatesAreRejected(string field)
    {
        using var h = await HomestayTestHost.CreateAsync(); var area = new Area { Name = "Khu mới", Province = "Hà Nội" }; h.Db.Add(area); await h.Db.SaveChangesAsync();
        var input = new HomestayLocationInput(h.Home.Address, field == "area" ? area.Id : h.Home.AreaId, field == "latitude" ? 22 : null, field == "longitude" ? 106 : null);
        await h.Content.UpdateLocationAsync(h.HomeId, input, h.Manager); Assert.Equal(HomestayStatus.PendingReview, h.Home.Status);
        await Assert.ThrowsAsync<BookingException>(() => h.Content.UpdateLocationAsync(h.HomeId, input with { Latitude = 91 }, h.Manager));
    }
    [Fact]
    public async Task ConsultantCanEditOnlyAssistedBeforeActivationManagerCanSupportAfterward()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var r = await h.P.Registrations.CreateAssistedAsync(h.P.Input(), h.P.Consultant);
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        await h.Content.UpdateAsync(home.Id, h.ContentInput(), h.P.Consultant);
        var invite = await h.P.Registrations.InviteAsync(r.Id, h.P.Consultant);
        await h.P.Registrations.ActivateAsync(r.Id, h.P.Input().PhoneNumber, invite.Token, PartnerTestHost.Password);
        await Assert.ThrowsAsync<BookingException>(() => h.Content.UpdateAsync(home.Id, h.ContentInput(), h.P.Consultant));
        await h.Content.UpdateAsync(home.Id, h.ContentInput(), h.Manager);
        Assert.Equal(h.Manager.AccountId, (await h.Db.AuditLogs.OrderBy(a => a.Id).LastAsync()).AccountId);
    }
}
