using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayTransactionTests
{
    [Fact]
    public async Task PublicImageAuditFailureRollsBackMetadataAndCleansOnlyNewFile()
    {
        using var h = await HomestayTestHost.CreateAsync();
        await using var db = h.H.NewContext(new FailAudit());
        var service = new HomestayImageService(db, new(db, h.P.Registrations), h.Environment, h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(h.HomeId, null, PartnerTestHost.Image(), false, 0, h.Owner));
        Assert.Empty(h.Db.HomestayImages); Assert.Empty(h.Db.AuditLogs);
        Assert.Empty(Directory.GetFiles(h.Environment.WebRootPath, "*", SearchOption.AllDirectories));
    }
    [Fact]
    public async Task CoverChangeFailureRestoresOldCover()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var old = await h.Images.UploadAsync(h.HomeId, null, PartnerTestHost.Image(), true, 0, h.Owner);
        var next = await h.Images.UploadAsync(h.HomeId, null, PartnerTestHost.Image(), false, 1, h.Owner);
        var audits = await h.Db.AuditLogs.CountAsync();
        await using var db = h.H.NewContext(new FailAudit());
        var service = new HomestayImageService(db, new(db, h.P.Registrations), h.Environment, h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetCoverAsync(next.Id, h.Owner));
        h.Db.ChangeTracker.Clear(); Assert.Equal(old.Id, (await h.Db.HomestayImages.SingleAsync(i => i.IsCover)).Id); Assert.Equal(audits, await h.Db.AuditLogs.CountAsync());
    }
    [Fact]
    public async Task PolicyReplacementFailureKeepsOriginalActiveRulesAndBookingSnapshot()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var old = await h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Cũ", [new(0, 50)], h.Owner);
        var b = await h.DepositedAsync(); var count = await h.Db.CancellationPolicies.CountAsync();
        await using var db = h.H.NewContext(new FailAudit());
        var service = new HomestayPolicyService(db, new(db, h.P.Registrations), h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplaceAsync(h.HomeId, PolicyScope.All, "Mới", [new(0, 100)], h.Owner));
        h.Db.ChangeTracker.Clear(); Assert.True((await h.Db.CancellationPolicies.SingleAsync(p => p.Id == old.Id)).IsActive);
        Assert.Equal(count, await h.Db.CancellationPolicies.CountAsync()); Assert.Equal(old.Id, (await h.Db.Bookings.SingleAsync()).CancellationPolicyId);
    }
    [Fact]
    public async Task RoomNightPkConflictIsTranslatedAndWholeBlockRolledBack()
    {
        using var h = await HomestayTestHost.CreateAsync(); var room = await h.Db.Rooms.FirstAsync();
        var existing = await h.Blocks.CreateAsync(room.Id, h.Today.AddDays(10), h.Today.AddDays(11), RoomBlockReason.OtherChannel, null, h.Owner);
        var audits = await h.Db.AuditLogs.CountAsync(); var start = h.Today.AddDays(2);
        await using var db = h.H.NewContext(new CompetingNight(room.Id, start, existing.Id));
        var service = new RoomBlockService(db, new(db, h.P.Registrations), h.H.Clock);
        var ex = await Assert.ThrowsAsync<RoomBlockConflictException>(() => service.CreateAsync(room.Id, start, start.AddDays(1), RoomBlockReason.Maintenance, null, h.Owner));
        Assert.Contains("Lịch phòng", ex.Message); Assert.Single(h.Db.RoomBlocks); Assert.Single(h.Db.RoomNights);
        Assert.Equal(audits, await h.Db.AuditLogs.CountAsync());
    }
    [Fact]
    public async Task PendingListUsesLastPendingTransitionNotLaterContentEditTime()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var first = await h.Reviews.CreateAsync(new("Một", "Hà Nội", h.Home.AreaId, "0912000001", "Mô tả"), h.Owner);
        var second = await h.Reviews.CreateAsync(new("Hai", "Hà Nội", h.Home.AreaId, "0912000001", "Mô tả"), h.Owner);
        foreach (var home in new[] { first, second }) foreach (var kind in new[] { DocumentType.BusinessLicense, DocumentType.FireSafety, DocumentType.PropertyPhoto })
            await h.Documents.UploadAsync(home.Id, kind, PartnerTestHost.Image(), null, h.Owner);
        await h.Reviews.SubmitAsync(first.Id, h.Owner); var firstTime = h.H.Clock.Now.UtcDateTime;
        h.H.Clock.Advance(TimeSpan.FromHours(1)); await h.Reviews.SubmitAsync(second.Id, h.Owner);
        h.H.Clock.Advance(TimeSpan.FromHours(1)); await h.Content.UpdateAsync(first.Id, h.ContentInput(), h.Manager);
        var pending = await h.Reviews.PendingAsync(h.Manager);
        Assert.Equal(new[] { first.Id, second.Id }, pending.Select(p => p.Homestay.Id)); Assert.Equal(firstTime, pending[0].SubmittedAt);
    }
    [Fact]
    public async Task UnapprovedOwnerCannotUseSeparateHomestaySubmission()
    {
        using var h = await HomestayTestHost.CreateAsync(); h.Home.Status = HomestayStatus.Draft; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.SubmitAsync(h.HomeId, h.Owner));
    }
    [Fact]
    public async Task OnlyFuturePricesAreNotEnoughForActivation()
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = HomestayStatus.PendingReview;
        foreach (var price in h.Db.RoomTypePrices) price.EffectiveFrom = price.EffectiveFrom.AddYears(2);
        await h.Db.SaveChangesAsync();
        var result = await h.Reviews.ApproveAsync(h.HomeId, h.Manager);
        Assert.Contains(result.Missing, m => m.Contains("ba giá"));
    }
    private sealed class FailAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added)) throw new InvalidOperationException("Lỗi audit giả lập");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class CompetingNight(int roomId, DateOnly date, int blockId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Ghi tranh chấp sau lượt kiểm tra phòng trống, trước insert; cùng transaction để luôn rollback sạch.
            await eventData.Context!.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoomNights (RoomId, Date, RoomBlockId) VALUES ({roomId}, {date}, {blockId})", cancellationToken);
            return result;
        }
    }
}
