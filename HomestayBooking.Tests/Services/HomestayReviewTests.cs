using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayReviewTests
{
    public static IEnumerable<object[]> Transitions()
    {
        var allowed = new (HomestayStatus, HomestayStatus)[] {
            (HomestayStatus.Draft, HomestayStatus.PendingReview), (HomestayStatus.Draft, HomestayStatus.Ended),
            (HomestayStatus.PendingReview, HomestayStatus.Active), (HomestayStatus.PendingReview, HomestayStatus.Draft), (HomestayStatus.PendingReview, HomestayStatus.Ended),
            (HomestayStatus.Active, HomestayStatus.PendingReview), (HomestayStatus.Active, HomestayStatus.Suspended), (HomestayStatus.Active, HomestayStatus.Ended),
            (HomestayStatus.Suspended, HomestayStatus.Active), (HomestayStatus.Suspended, HomestayStatus.Ended) };
        foreach (var from in Enum.GetValues<HomestayStatus>()) foreach (var to in Enum.GetValues<HomestayStatus>()) yield return [from, to, allowed.Contains((from, to))];
    }
    [Theory, MemberData(nameof(Transitions))]
    public void EveryStatusPair(HomestayStatus from, HomestayStatus to, bool expected) => Assert.Equal(expected, HomestayReviewService.CanTransition(from, to));
    [Fact]
    public async Task ApprovedOwnerCreatesAndSubmitsHomestayWithoutAnotherRegistration()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); var registrations = await h.Db.OwnerRegistrations.CountAsync();
        var home = await h.Reviews.CreateAsync(new("Mới", "Hà Nội", h.Home.AreaId, "0912000001", "<p>Mô tả</p>"), h.Owner);
        Assert.Equal(HomestayStatus.Draft, home.Status); Assert.Equal(registrations, await h.Db.OwnerRegistrations.CountAsync());
        var missing = await h.Reviews.SubmitAsync(home.Id, h.Owner); Assert.Equal(3, missing.Missing.Count); Assert.Equal(HomestayStatus.Draft, home.Status);
        foreach (var kind in new[] { DocumentType.BusinessLicense, DocumentType.FireSafety, DocumentType.PropertyPhoto })
            await h.Documents.UploadAsync(home.Id, kind, PartnerTestHost.Image(), h.Today, h.Owner);
        Assert.True((await h.Reviews.SubmitAsync(home.Id, h.Owner)).Succeeded); Assert.Equal(HomestayStatus.PendingReview, home.Status);
        Assert.NotNull((await h.Reviews.PendingAsync(h.Manager)).Single(x => x.Homestay.Id == home.Id).SubmittedAt);
    }
    [Fact]
    public async Task OwnerWithoutApprovalCannotCreateHomestay()
    {
        using var h = await HomestayTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.CreateAsync(new("Mới", "Hà Nội", h.Home.AreaId, "0912000001", "Mô tả"), h.Owner));
    }
    [Theory]
    [InlineData("owner", "Chủ chưa được duyệt")]
    [InlineData("contract", "Hợp đồng đang hiệu lực")]
    [InlineData("BusinessLicense", "Giấy phép kinh doanh")]
    [InlineData("FireSafety", "Giấy PCCC")]
    [InlineData("expired", "Giấy PCCC")]
    [InlineData("room", "phòng đang bán")]
    [InlineData("type", "phòng đang bán")]
    [InlineData("price", "ba giá")]
    [InlineData("cover", "Ảnh bìa")]
    [InlineData("address", "Địa chỉ")]
    [InlineData("phone", "Số điện thoại")]
    [InlineData("description", "Mô tả")]
    public async Task EachMissingActivationConditionIsReportedWithoutStateChange(string bad, string message)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = HomestayStatus.PendingReview;
        if (bad == "owner") h.Db.OwnerRegistrations.Remove(await h.Db.OwnerRegistrations.SingleAsync());
        if (bad == "contract") (await h.Db.Contracts.SingleAsync()).Status = ContractStatus.Expired;
        if (Enum.TryParse<DocumentType>(bad, out var kind)) h.Db.VerificationDocuments.Remove(await h.Db.VerificationDocuments.SingleAsync(d => d.DocumentType == kind));
        if (bad == "expired") (await h.Db.VerificationDocuments.SingleAsync(d => d.DocumentType == DocumentType.FireSafety)).ExpiryDate = h.Today.AddDays(-1);
        if (bad == "room") foreach (var r in h.Db.Rooms) r.IsActive = false;
        if (bad == "type") (await h.Db.RoomTypes.SingleAsync()).IsActive = false;
        if (bad == "price") h.Db.RoomTypePrices.RemoveRange(await h.Db.RoomTypePrices.Where(p => p.DayType == DayType.Holiday).ToListAsync());
        if (bad == "cover") h.Db.HomestayImages.Remove(await h.Db.HomestayImages.SingleAsync());
        if (bad == "address") h.Home.Address = " ";
        if (bad == "phone") h.Home.ContactPhone = null;
        if (bad == "description") h.Home.DescriptionHtml = " ";
        await h.Db.SaveChangesAsync();
        var result = await h.Reviews.ApproveAsync(h.HomeId, h.Manager);
        Assert.False(result.Succeeded); Assert.Contains(result.Missing, m => m.Contains(message)); Assert.Equal(HomestayStatus.PendingReview, h.Home.Status);
        Assert.Empty(h.Db.AuditLogs); Assert.Empty(h.Db.Notifications);
    }
    [Fact]
    public async Task LegacyApprovedOwnerDocumentsWithoutPropertyPhotoStillAllowApproval()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); var registration = await h.Db.OwnerRegistrations.SingleAsync();
        foreach (var d in h.Db.VerificationDocuments) { d.HomestayId = null; d.OwnerRegistrationId = registration.Id; }
        h.Db.VerificationDocuments.Remove(await h.Db.VerificationDocuments.SingleAsync(d => d.DocumentType == DocumentType.PropertyPhoto));
        h.Home.Status = HomestayStatus.PendingReview; await h.Db.SaveChangesAsync();
        var result = await h.Reviews.ApproveAsync(h.HomeId, h.Manager); Assert.True(result.Succeeded);
        Assert.Equal(HomestayStatus.Active, h.Home.Status); Assert.Equal(h.Manager.EmployeeId, h.Home.ApprovedByEmployeeId); Assert.Equal(h.H.Clock.Now.UtcDateTime, h.Home.ApprovedAt);
        Assert.All(h.Db.VerificationDocuments, d => { Assert.Equal(registration.Id, d.OwnerRegistrationId); Assert.Null(d.HomestayId); });
        Assert.Equal(h.Owner.AccountId, (await h.Db.Notifications.SingleAsync()).UserId);
    }
    [Theory]
    [InlineData("consultant")] [InlineData("accountant")] [InlineData("owner")]
    public async Task OnlyManagementMayApproveRejectSuspend(string who)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = HomestayStatus.PendingReview; await h.Db.SaveChangesAsync();
        var actor = who == "consultant" ? h.P.Consultant : who == "accountant" ? h.P.Accountant : h.Owner;
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.ApproveAsync(h.HomeId, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.RejectAsync(h.HomeId, "Không đủ", actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.SuspendAsync(h.HomeId, "Kiểm tra", actor));
    }
    [Fact]
    public async Task RejectionReasonIsAuditOnlyAndCannotBeEmpty()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = HomestayStatus.PendingReview; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.RejectAsync(h.HomeId, " ", h.Manager));
        var note = new string('X', 1000); await h.Reviews.RejectAsync(h.HomeId, note, h.Manager);
        Assert.Equal(HomestayStatus.Draft, h.Home.Status); Assert.Contains(note, (await h.Db.AuditLogs.SingleAsync()).NewValues);
        Assert.DoesNotContain(note, (await h.Db.Notifications.SingleAsync()).Message);
    }
    [Fact]
    public async Task SuspensionResumeRechecksConditionsAndPreservesExistingBooking()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); var b = await h.BookAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.SuspendAsync(h.HomeId, " ", h.Manager));
        await h.Reviews.SuspendAsync(h.HomeId, "Bảo trì", h.Manager); Assert.Equal(HomestayStatus.Suspended, h.Home.Status);
        Assert.Equal(2, await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(10), h.Today.AddDays(11))));
        (await h.Db.HomestayImages.SingleAsync()).IsCover = false; await h.Db.SaveChangesAsync();
        Assert.False((await h.Reviews.ResumeAsync(h.HomeId, "Mở lại", h.Manager)).Succeeded);
        (await h.Db.HomestayImages.SingleAsync()).IsCover = true; await h.Db.SaveChangesAsync();
        Assert.True((await h.Reviews.ResumeAsync(h.HomeId, "Đã khắc phục", h.P.Admin)).Succeeded);
    }
    [Theory]
    [InlineData("contract", false)] [InlineData("booking", false)] [InlineData("completed", true)] [InlineData("empty", true)]
    public async Task EndedChecksContractAndLiveHoldingBookings(string scenario, bool allowed)
    {
        using var h = await HomestayTestHost.CreateAsync(); Booking? b = null;
        if (scenario is "booking" or "completed")
        {
            b = await h.BookAsync();
            if (scenario == "completed") { b.Status = BookingStatus.Completed; b.ConfirmedAt = h.H.Clock.Now.UtcDateTime; }
        }
        if (scenario != "contract") (await h.Db.Contracts.SingleAsync()).Status = ContractStatus.Terminated;
        await h.Db.SaveChangesAsync();
        if (allowed) { await h.Reviews.EndAsync(h.HomeId, "Kết thúc", h.Manager); Assert.Equal(HomestayStatus.Ended, h.Home.Status); }
        else await Assert.ThrowsAsync<BookingException>(() => h.Reviews.EndAsync(h.HomeId, "Kết thúc", h.Manager));
        if (b is not null) Assert.Equal(2, await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
    }
}
