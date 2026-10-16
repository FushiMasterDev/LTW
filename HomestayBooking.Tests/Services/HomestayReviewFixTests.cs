using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class HomestayReviewFixTests
{
    public static IEnumerable<object[]> RegistrationLocks()
    {
        foreach (var status in new[] { OwnerRegistrationStatus.PendingReview, OwnerRegistrationStatus.UnderReview, OwnerRegistrationStatus.NeedsRevision })
        foreach (var home in new[] { HomestayStatus.Draft, HomestayStatus.PendingReview })
        foreach (var actor in new[] { "owner", "consultant", "manager" }) yield return [status, home, actor];
    }

    [Theory, MemberData(nameof(RegistrationLocks))]
    public async Task OpenRegistrationStateGatesAllContentPaths(OwnerRegistrationStatus status, HomestayStatus homeStatus, string who)
    {
        using var h = await HomestayTestHost.CreateAsync();
        var r = who == "consultant"
            ? await h.P.Registrations.CreateAssistedAsync(h.P.Input(), h.P.Consultant)
            : await h.P.DraftAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        var actor = who == "consultant" ? h.P.Consultant : who == "manager" ? h.Manager : await h.P.OwnerActorAsync(r);
        var type = new RoomType { HomestayId = home.Id, CategoryId = h.H.RoomType.CategoryId, Name = "Có sẵn", MaxGuests = 2 };
        var doc = new VerificationDocument { HomestayId = home.Id, DocumentType = DocumentType.PropertyPhoto, FileUrl = "old.png" };
        var b1Doc = new VerificationDocument { HomestayId = home.Id, DocumentType = DocumentType.PropertyPhoto, FileUrl = "b1.png" };
        var image = new HomestayImage { HomestayId = home.Id, Url = "/images/demo/old.png" };
        h.Db.AddRange(type, doc, b1Doc, image);
        r.Status = status; home.Status = homeStatus;
        if (status == OwnerRegistrationStatus.NeedsRevision)
        {
            r.ReviewedByEmployeeId = h.Manager.EmployeeId; r.ReviewedAt = h.H.Clock.Now.UtcDateTime; r.ReviewNote = "Bổ sung";
        }
        await h.Db.SaveChangesAsync();
        var id = home.Id; var typeId = type.Id; var docId = doc.Id; var imageId = image.Id;
        var b1DocId = b1Doc.Id;
        var before = await h.Db.AuditLogs.CountAsync();
        var edits = new List<Func<Task>> {
            () => h.Content.UpdateAsync(id, h.ContentInput(), actor),
            () => h.Content.UpdateLocationAsync(id, new("Địa chỉ mới", h.Home.AreaId, 21m, 105m), actor),
            () => h.Images.UploadAsync(id, null, PartnerTestHost.Image(), false, 0, actor),
            () => h.Images.UpdateAsync(imageId, null, 1, actor),
            () => h.Images.SetCoverAsync(imageId, actor),
            () => h.Images.DeleteAsync(imageId, actor),
            () => h.Rooms.CreateTypeAsync(id, h.TypeInput(), HomestayTestHost.Prices(), actor),
            () => h.Rooms.AddRoomAsync(typeId, "M1", actor),
            () => h.Rooms.SetPriceAsync(typeId, DayType.Weekday, h.Today.AddDays(1), 700000, actor),
            () => h.Policies.ReplaceAsync(id, PolicyScope.All, "Mới", [new(0, 0)], actor),
            () => h.Documents.UploadAsync(id, DocumentType.FireSafety, PartnerTestHost.Image(), h.Today, actor),
            () => h.Documents.DeleteAsync(docId, actor)
        };
        // B1 vẫn giữ quyền riêng: nhân viên chỉ sửa hồ sơ tạo thay trước khi chủ kích hoạt.
        if (who != "manager") edits.AddRange([
            () => h.P.Registrations.UpdateDraftAsync(r.Id, id, h.P.Input(), actor),
            () => h.P.Registrations.UploadDocumentAsync(id, DocumentType.FireSafety, PartnerTestHost.Image(), h.Today, actor),
            () => h.P.Registrations.DeleteDocumentAsync(b1DocId, actor)
        ]);
        foreach (var edit in edits)
        {
            if (status == OwnerRegistrationStatus.NeedsRevision) await edit();
            else Assert.Contains("Hồ sơ đang duyệt", (await Assert.ThrowsAsync<BookingException>(edit)).Message);
        }
        if (status != OwnerRegistrationStatus.NeedsRevision)
        {
            Assert.Equal(before, await h.Db.AuditLogs.CountAsync());
            Assert.Equal(homeStatus, (await h.Db.Homestays.SingleAsync(x => x.Id == id)).Status);
            Assert.Equal("Cơ sở mới", (await h.Db.Homestays.SingleAsync(x => x.Id == id)).Name);
            Assert.Equal(id, (await h.Access.AuthorizeAsync(id, actor, readOnly: true)).Id);
        }
    }

    [Theory]
    [InlineData("location")] [InlineData("document")]
    public async Task SuspendedEditsNeverBypassResume(string edit)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        await h.Reviews.SuspendAsync(h.HomeId, "Tạm ngưng", h.Manager);
        var notices = await h.Db.Notifications.CountAsync(n => n.UserId == h.Manager.AccountId);
        if (edit == "location") await h.Content.UpdateLocationAsync(h.HomeId, new(h.Home.Address, h.Home.AreaId, 21m, 105m), h.Owner);
        else await h.Documents.UploadAsync(h.HomeId, DocumentType.FireSafety, PartnerTestHost.Image(), h.Today, h.Owner);
        Assert.Equal(HomestayStatus.Suspended, h.Home.Status);
        Assert.True(await h.Db.Notifications.CountAsync(n => n.UserId == h.Manager.AccountId) > notices);
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews.ApproveAsync(h.HomeId, h.Manager));
        Assert.Equal(HomestayStatus.Suspended, h.Home.Status);
        Assert.True((await h.Reviews.ResumeAsync(h.HomeId, "Đã kiểm tra", h.Manager)).Succeeded);
        Assert.Equal(HomestayStatus.Active, h.Home.Status);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task ActiveHomestayCannotDisableLastSellingRoomOrType(bool type)
    {
        using var h = await HomestayTestHost.CreateAsync();
        var rooms = await h.Db.Rooms.OrderBy(r => r.Id).ToListAsync();
        await h.Rooms.SetRoomActiveAsync(rooms[0].Id, false, h.Owner);
        var auditCount = await h.Db.AuditLogs.CountAsync();
        var error = await Assert.ThrowsAsync<BookingException>(() => type
            ? h.Rooms.SetTypeActiveAsync(h.H.RoomType.Id, false, h.Owner)
            : h.Rooms.SetRoomActiveAsync(rooms[1].Id, false, h.Owner));
        Assert.Contains("khóa ngày", error.Message); Assert.Contains("tạm ngưng", error.Message);
        Assert.True((await h.Db.Rooms.SingleAsync(r => r.Id == rooms[1].Id)).IsActive);
        Assert.True((await h.Db.RoomTypes.SingleAsync()).IsActive);
        Assert.Equal(auditCount, await h.Db.AuditLogs.CountAsync());
    }

    [Theory]
    [InlineData(HomestayStatus.Draft)] [InlineData(HomestayStatus.PendingReview)] [InlineData(HomestayStatus.Suspended)]
    public async Task NonActiveHomestayCanDisableAllSales(HomestayStatus status)
    {
        using var h = await HomestayTestHost.CreateAsync(); h.Home.Status = status; await h.Db.SaveChangesAsync();
        foreach (var id in await h.Db.Rooms.Select(r => r.Id).ToListAsync()) await h.Rooms.SetRoomActiveAsync(id, false, h.Owner);
        await h.Rooms.SetTypeActiveAsync(h.H.RoomType.Id, false, h.Owner);
        Assert.False(await h.Db.Rooms.AnyAsync(r => r.IsActive)); Assert.False(await h.Db.RoomTypes.AnyAsync(t => t.IsActive));
    }

    [Fact]
    public async Task ActiveHomestayCanDisableTypeIfAnotherSellingTypeRemains()
    {
        using var h = await HomestayTestHost.CreateAsync();
        var other = await h.Rooms.CreateTypeAsync(h.HomeId, h.TypeInput(), HomestayTestHost.Prices(), h.Owner);
        await h.Rooms.AddRoomAsync(other.Id, "M1", h.Owner);
        await h.Rooms.SetTypeActiveAsync(h.H.RoomType.Id, false, h.Owner);
        Assert.True(await h.Db.Rooms.AnyAsync(r => r.IsActive && r.RoomType.IsActive));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ResubmissionUsesLegacyDocumentsOnlyForPreviouslyApprovedHome(bool approved)
    {
        using var h = await HomestayTestHost.CreateAsync(ready: true);
        var registration = await h.Db.OwnerRegistrations.SingleAsync();
        foreach (var d in await h.Db.VerificationDocuments.ToListAsync())
        {
            if (d.DocumentType == DocumentType.PropertyPhoto) h.Db.Remove(d);
            else { d.HomestayId = null; d.OwnerRegistrationId = registration.Id; }
        }
        h.Home.Status = HomestayStatus.Draft; h.Home.ApprovedAt = approved ? h.H.Clock.Now.UtcDateTime : null;
        await h.Db.SaveChangesAsync();
        var result = await h.Reviews.SubmitAsync(h.HomeId, h.Owner);
        Assert.Equal(approved, result.Succeeded);
        Assert.Equal(approved ? HomestayStatus.PendingReview : HomestayStatus.Draft, h.Home.Status);
        if (!approved) Assert.Equal(3, result.Missing.Count);
    }
}
