using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayMediaPolicyTests
{
    [Fact]
    public async Task PublicImageCoverSwitchAndDeleteUseSafeGeneratedPath()
    {
        using var h = await HomestayTestHost.CreateAsync();
        // Tên có đường dẫn bị từ chối hẳn; tên thường vẫn được lưu bằng đường dẫn sinh ra, không dùng tên khách gửi
        await Assert.ThrowsAsync<BookingException>(() => h.Images.UploadAsync(h.HomeId, h.H.RoomType.Id, PartnerTestHost.Image("../../secret.png"), true, 1, h.Owner));
        Assert.Empty(h.Db.HomestayImages);
        var a = await h.Images.UploadAsync(h.HomeId, h.H.RoomType.Id, PartnerTestHost.Image("secret.png"), true, 1, h.Owner);
        Assert.StartsWith($"/uploads/homestays/{h.HomeId}/", a.Url); Assert.DoesNotContain("secret", a.Url);
        var b = await h.Images.UploadAsync(h.HomeId, null, PartnerTestHost.Image(), false, 2, h.Owner);
        await Assert.ThrowsAsync<BookingException>(() => h.Images.DeleteAsync(a.Id, h.Owner));
        await h.Images.SetCoverAsync(b.Id, h.Owner);
        Assert.Single(await h.Db.HomestayImages.Where(i => i.IsCover).ToListAsync());
        Assert.Equal(b.Id, (await h.Db.HomestayImages.SingleAsync(i => i.IsCover)).Id);
        await h.Images.UpdateAsync(b.Id, h.H.RoomType.Id, 0, h.Owner);
        var path = Path.Combine(h.Environment.WebRootPath, a.Url.TrimStart('/'));
        Assert.True(File.Exists(path)); await h.Images.DeleteAsync(a.Id, h.Owner); Assert.False(File.Exists(path));
    }
    [Theory]
    [InlineData("magic")] [InlineData("size")] [InlineData("otherType")]
    public async Task InvalidPublicImageIsRejectedBeforeFileOrMetadataCreation(string bad)
    {
        using var h = await HomestayTestHost.CreateAsync(); int? type = null;
        if (bad == "otherType")
        {
            var home = new Homestay { OwnerId = h.Home.OwnerId, AreaId = h.Home.AreaId, Name = "Khác", Address = "Khác" };
            var t = new RoomType { Homestay = home, Name = "Khác", CategoryId = h.H.RoomType.CategoryId, MaxGuests = 2 }; h.Db.Add(t); await h.Db.SaveChangesAsync(); type = t.Id;
        }
        var bytes = bad == "size" ? new byte[ReceiptFileStore.MaxBytes + 1] : new byte[12];
        var file = bad == "otherType" ? PartnerTestHost.Image() : new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "fake.jpg");
        await Assert.ThrowsAsync<BookingException>(() => h.Images.UploadAsync(h.HomeId, type, file, false, 0, h.Owner));
        Assert.Empty(h.Db.HomestayImages); Assert.False(Directory.Exists(h.Environment.WebRootPath));
    }
    [Theory]
    [InlineData(HomestayStatus.Active, DocumentType.FireSafety, HomestayStatus.PendingReview)]
    [InlineData(HomestayStatus.Suspended, DocumentType.BusinessLicense, HomestayStatus.Suspended)]
    [InlineData(HomestayStatus.Active, DocumentType.PropertyPhoto, HomestayStatus.Active)]
    [InlineData(HomestayStatus.Active, DocumentType.Other, HomestayStatus.Active)]
    public async Task LegalUploadRequeuesButOtherDocumentsDoNot(HomestayStatus initial, DocumentType type, HomestayStatus expected)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = initial; await h.Db.SaveChangesAsync();
        var count = await h.Db.VerificationDocuments.CountAsync();
        var doc = await h.Documents.UploadAsync(h.HomeId, type, PartnerTestHost.Image(), h.Today, h.Owner);
        Assert.Equal(expected, h.Home.Status); Assert.Equal(count + 1, await h.Db.VerificationDocuments.CountAsync());
        Assert.Equal(h.HomeId, doc.HomestayId); Assert.Null(doc.OwnerRegistrationId);
        if (expected == HomestayStatus.PendingReview) {
            Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Manager.AccountId);
            var details = await h.Reviews.PendingDetailsAsync(h.HomeId, h.Manager);
            Assert.NotNull(details.BeforeContent); Assert.Equal(count, details.BeforeContent.Documents.Count);
            Assert.Contains(details.Changes, c => c.Field == "Documents");
        }
    }
    [Theory]
    [InlineData(HomestayStatus.Active)] [InlineData(HomestayStatus.Suspended)]
    public async Task OperatingHomestayCannotDeleteOldDocuments(HomestayStatus status)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true); h.Home.Status = status; await h.Db.SaveChangesAsync();
        var doc = await h.Db.VerificationDocuments.FirstAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Documents.DeleteAsync(doc.Id, h.Owner));
        Assert.Equal(3, await h.Db.VerificationDocuments.CountAsync());
    }
    [Fact]
    public async Task PolicyReplacementPreservesOldRefundAndRemovalRestoresGeneralPolicy()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var old = await h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Cũ", [new(0, 0), new(3, 50), new(7, 100)], h.Owner);
        var b = await h.DepositedAsync(5); Assert.Equal(old.Id, b.CancellationPolicyId);
        var before = await h.H.Refunds().QuoteCancellationAsync(b);
        var replacement = await h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Mới", [new(0, 100)], h.Owner);
        Assert.NotEqual(old.Id, replacement.Id); Assert.False(old.IsActive);
        Assert.Equal(old.Id, b.CancellationPolicyId); Assert.Equal(before, await h.H.Refunds().QuoteCancellationAsync(b));
        Assert.Equal(replacement.Id, (await h.H.Refunds().SelectPolicyAsync(h.HomeId, false)).Id);
        var newBooking = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(8), h.Today.AddDays(10)));
        await h.H.Bookings().ChangeStatusAsync(newBooking, BookingStatus.Deposited, Actor.System);
        Assert.Equal(100, (await h.H.Refunds().QuoteCancellationAsync(newBooking)).RefundPercent);
        await h.Policies.RemoveAsync(h.HomeId, PolicyScope.All, h.Owner);
        Assert.Equal(h.H.GeneralPolicy.Id, (await h.H.Refunds().SelectPolicyAsync(h.HomeId, false)).Id);
        Assert.Equal(3, await h.Db.CancellationRules.CountAsync(r => r.PolicyId == old.Id));
    }
    [Theory]
    [InlineData("missingZero")] [InlineData("duplicate")] [InlineData("decreasing")] [InlineData("negative")] [InlineData("over100")]
    public async Task InvalidPolicyRulesCannotDisableExistingPolicy(string bad)
    {
        using var h = await HomestayTestHost.CreateAsync();
        var old = await h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Đang dùng", [new(0, 0), new(7, 100)], h.Owner);
        CancellationRuleInput[] rules = bad switch { "missingZero" => [new(1, 0)], "duplicate" => [new(0, 0), new(0, 100)],
            "decreasing" => [new(0, 100), new(7, 50)], "negative" => [new(0, -1)], _ => [new(0, 101)] };
        await Assert.ThrowsAsync<BookingException>(() => h.Policies.ReplaceAsync(h.HomeId, PolicyScope.All, "Sai", rules, h.Owner));
        Assert.True((await h.Db.CancellationPolicies.SingleAsync(p => p.Id == old.Id)).IsActive);
    }
}
