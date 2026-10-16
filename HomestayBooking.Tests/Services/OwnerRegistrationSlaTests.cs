using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class OwnerRegistrationSlaTests
{
    [Theory]
    [InlineData("2026-11-13", "", "2026-11-18")]
    [InlineData("2026-11-12", "2026-11-13", "2026-11-18")]
    [InlineData("2026-11-14", "", "2026-11-18")]
    [InlineData("2026-11-15", "2026-11-16", "2026-11-19")]
    public void ThreeBusinessDaysPreserveLocalTimeExcludeWeekendAndHolidays(string submitted, string holiday, string expected)
    {
        var holidays = string.IsNullOrEmpty(holiday) ? new HashSet<DateOnly>() : [DateOnly.Parse(holiday)];
        var result = OwnerRegistrationService.ReviewDeadlineLocal(DateOnly.Parse(submitted).ToDateTime(new TimeOnly(10, 15)), holidays);
        Assert.Equal(DateOnly.Parse(expected).ToDateTime(new TimeOnly(10, 15)), result);
    }
    [Theory]
    [InlineData(OwnerRegistrationStatus.PendingReview, true)]
    [InlineData(OwnerRegistrationStatus.UnderReview, true)]
    [InlineData(OwnerRegistrationStatus.NeedsRevision, false)]
    public async Task AlertsOnceOnlyForPendingOrUnderReview(OwnerRegistrationStatus status, bool expected)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        if (status == OwnerRegistrationStatus.NeedsRevision) await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung", h.Consultant);
        else { r.Status = status; await h.Db.SaveChangesAsync(); }
        h.H.Clock.Advance(TimeSpan.FromDays(8));
        var before = await h.Db.Notifications.CountAsync();
        Assert.Equal(expected ? 1 : 0, await h.Registrations.AlertOverdueAsync());
        Assert.Equal(expected ? before + 1 : before, await h.Db.Notifications.CountAsync());
        Assert.Equal(0, await h.Registrations.AlertOverdueAsync());
        if (expected)
        {
            var notification = await h.Db.Notifications.OrderBy(n => n.Id).LastAsync();
            Assert.Equal(h.Manager.AccountId, notification.UserId); Assert.Equal(OwnerRegistrationService.RegistrationUrl(r.Id), notification.Url);
            Assert.Equal(h.H.Clock.Now.UtcDateTime, notification.CreatedAt);
            var audit = await h.Db.AuditLogs.OrderBy(a => a.Id).LastAsync(); Assert.Null(audit.AccountId); Assert.Contains("System", audit.NewValues);
        }
    }
    [Fact]
    public async Task DeadlineInclusiveNotYetOverdue()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        h.H.Clock.Advance(TimeSpan.FromDays(3));
        Assert.Equal(0, await h.Registrations.AlertOverdueAsync());
        h.H.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, await h.Registrations.AlertOverdueAsync());
    }
    [Fact]
    public async Task ResubmissionPermitsNewAlertAndNotifiesEveryWorkingManager()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        var second = await h.StaffAsync(EmployeePosition.Manager); var inactive = await h.StaffAsync(EmployeePosition.Manager);
        (await h.Db.Employees.SingleAsync(e => e.Id == inactive.EmployeeId)).EmploymentStatus = EmploymentStatus.Resigned;
        foreach (var d in h.Db.VerificationDocuments) d.ExpiryDate = h.Today.AddDays(30);
        await h.Db.SaveChangesAsync(); h.H.Clock.Advance(TimeSpan.FromDays(8));
        Assert.Equal(1, await h.Registrations.AlertOverdueAsync());
        var notices = await h.Db.Notifications.Where(n => n.Url == OwnerRegistrationService.RegistrationUrl(r.Id)).ToListAsync();
        Assert.Equal(2, notices.Count); Assert.Contains(notices, n => n.UserId == second.AccountId); Assert.DoesNotContain(notices, n => n.UserId == inactive.AccountId);
        await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung", h.Consultant);
        h.H.Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True((await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r))).Succeeded);
        Assert.Equal(0, await h.Registrations.AlertOverdueAsync());
        h.H.Clock.Advance(TimeSpan.FromDays(8));
        Assert.Equal(1, await h.Registrations.AlertOverdueAsync());
        Assert.Equal(2, await h.Db.Notifications.CountAsync(n => n.Url == OwnerRegistrationService.RegistrationUrl(r.Id) && n.CreatedAt > r.SubmittedAt));
    }
}
