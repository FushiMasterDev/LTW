using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class B9AdminTests
{
    [Fact]
    public async Task AccountPositionIsFilteredBeforePagingAndDetailsRequireManagement()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var service = new AccountAdminService(h.Db, h.Users, new(h.Db, new HttpContextAccessor()), h.H.Clock);
        var list = await service.ListAsync(new(Position: EmployeePosition.Accountant), h.Manager);
        Assert.Single(list); Assert.Equal(h.Accountant.AccountId, list[0].Id);
        var details = await service.DetailsAsync(h.Accountant.AccountId!, h.Manager);
        Assert.Equal(list[0].Id, details.Id); Assert.Equal(list[0].Position, details.Position); Assert.Equal(list[0].Roles, details.Roles);
        await Assert.ThrowsAsync<BookingException>(() => service.DetailsAsync(h.Accountant.AccountId!, h.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => service.ListAsync(new(Position: EmployeePosition.Accountant), h.Consultant));
    }

    [Fact]
    public async Task PendingReviewReturnsExactAuditedLocationChangesAndEligibleLegacyDocuments()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        h.Home.ApprovedAt = h.H.Clock.Now.UtcDateTime;
        var old = h.Home.Address;
        var registration = await h.Db.OwnerRegistrations.SingleAsync();
        h.Db.VerificationDocuments.Add(new() { OwnerRegistrationId = registration.Id, DocumentType = DocumentType.BusinessLicense, FileUrl = "legacy.png" });
        h.Db.VerificationDocuments.Add(new() { OwnerRegistrationId = registration.Id, DocumentType = DocumentType.FireSafety, FileUrl = "expired.png", ExpiryDate = h.Today.AddDays(-1) });
        await h.Db.SaveChangesAsync();
        await h.Content.UpdateLocationAsync(h.HomeId, new("B9 địa chỉ mới", h.Home.AreaId, null, null), h.Owner);
        var d = await h.Reviews.PendingDetailsAsync(h.HomeId, h.Manager);
        Assert.Equal("Thay đổi địa chỉ / vị trí", d.ChangeReason);
        Assert.Contains(d.Changes, c => c.Field == "Address" && c.Before == old && c.After == "B9 địa chỉ mới");
        Assert.Equal(DocumentType.BusinessLicense, Assert.Single(d.LegacyDocuments).Type);
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.PendingDetailsAsync(h.HomeId, h.Owner));
    }
}
