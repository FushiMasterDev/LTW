using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class ContractQueryTests
{
    [Fact]
    public async Task ListFiltersExpiringAndSearchAndDetailsReadsHistory()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        (await h.Db.Contracts.SingleAsync()).Status = ContractStatus.Terminated; await h.Db.SaveChangesAsync();
        var c = await h.Contracts.CreateAsync(new(h.H.Homestay.Id, "HD-Q1", h.Today, h.Today.AddDays(20), .12m), h.Manager);
        Assert.Equal(2, (await h.Contracts.ListAsync(new(), h.Manager)).TotalCount);
        Assert.Equal(c.Id, Assert.Single(await h.Contracts.ListAsync(new(ExpiringWithinDays: 30), h.Manager)).Id);
        Assert.Empty(await h.Contracts.ListAsync(new(ExpiringWithinDays: 10), h.Manager));
        Assert.Equal(c.Id, Assert.Single(await h.Contracts.ListAsync(new(Search: "HD-Q1"), h.Manager)).Id);
        Assert.Equal(c.Id, Assert.Single(await h.Contracts.ListAsync(new(Status: ContractStatus.Active), h.Manager)).Id);
        await h.Contracts.ExtendAsync(c.Id, h.Today.AddDays(60), h.Manager);
        await h.Contracts.TerminateAsync(c.Id, "Chủ dừng kinh doanh", h.Admin);
        var details = await h.Contracts.DetailsAsync(c.Id, h.Manager);
        Assert.Equal(ContractStatus.Terminated, details.Contract.Status); Assert.Equal(h.H.Homestay.Name, details.Contract.HomestayName);
        Assert.Equal(3, details.History.Count);
        Assert.Equal(h.Today.AddDays(60), details.History[1].EndDate);
        Assert.Equal(ContractStatus.Terminated, details.History[2].Status); Assert.Equal("Chủ dừng kinh doanh", details.History[2].Reason);
    }

    [Fact]
    public async Task OnlyManagementReadsContractsAndExpiryLinksToAdmin()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        var id = (await h.Db.Contracts.SingleAsync()).Id;
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.ListAsync(new(), h.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.DetailsAsync(id, h.Accountant));
        var contract = await h.Db.Contracts.SingleAsync(); contract.EndDate = h.Today.AddDays(-1); await h.Db.SaveChangesAsync();
        await h.Contracts.ExpireAsync();
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.Url == $"/Admin/Contracts/{id}");
    }
}
