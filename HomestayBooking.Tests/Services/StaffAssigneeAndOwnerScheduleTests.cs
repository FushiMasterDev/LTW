using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class StaffAssigneeAndOwnerScheduleTests
{
    [Fact]
    public async Task AssigneesAreActiveConsultantsAndManagersEveryoneAssignAccepts()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var list = await h.Staff().AssigneesAsync(h.Consultant);
        Assert.Contains(list, a => a.EmployeeId == h.Consultant.EmployeeId);
        Assert.Contains(list, a => a.EmployeeId == h.Manager.EmployeeId);
        Assert.All(list, a => Assert.True(a.Position is EmployeePosition.Consultant or EmployeePosition.Manager));
        Assert.DoesNotContain(list, a => a.EmployeeId == h.P.Admin.EmployeeId || a.EmployeeId == h.P.Accountant.EmployeeId);
        var b = await h.BookingAsync(BookingStatus.Deposited);
        foreach (var a in list) await h.Staff().AssignAsync(b.Id, a.EmployeeId, h.Manager);
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().AssigneesAsync(h.Owner));
    }

    [Fact]
    public async Task OwnerScheduleResolvesOwnerFromAccount()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        await h.BookAsync(1); await h.Db.SaveChangesAsync();
        var mine = await h.Schedule.ReadAsync(h.B.Owner);
        var byId = await h.Schedule.ReadAsync(h.H.Homestay.OwnerId, h.B.Owner);
        Assert.Equal(byId.Arrivals.Select(a => a.BookingId), mine.Arrivals.Select(a => a.BookingId));
        Assert.Single(mine.Arrivals);
        await Assert.ThrowsAsync<BookingException>(() => h.Schedule.ReadAsync(h.P.Manager));
        await Assert.ThrowsAsync<BookingException>(() => h.Schedule.ReadAsync(h.Customer));
    }
}
