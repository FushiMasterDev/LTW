using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class PartnerWorkflowRegressionTests
{
    [Fact]
    public async Task SubmissionRequiresOneCompleteDraftNotEveryUnfinishedDraft()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.ReadyAsync();
        var unfinished = new Homestay { OwnerId = r.OwnerId, AreaId = h.H.Homestay.AreaId,
            Name = "Nháp chưa có giấy", Address = "Hà Nội", ContactPhone = "+84912345678" };
        h.Db.Add(unfinished); await h.Db.SaveChangesAsync();
        Assert.True((await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r))).Succeeded);
        Assert.Equal(HomestayStatus.Draft, unfinished.Status);
        Assert.Equal(1, await h.Db.Homestays.CountAsync(x => x.OwnerId == r.OwnerId && x.Status == HomestayStatus.PendingReview));
    }
    [Fact]
    public async Task OwnerCanCorrectBankAndDraftHomestayWithoutChangingPhone()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId); var actor = await h.OwnerActorAsync(r);
        var input = h.Input() with { HomestayName = "Đã sửa", BankName = "VCB", Description = "<script>không chạy</script>" };
        await h.Registrations.UpdateDraftAsync(r.Id, home.Id, input, actor);
        Assert.Equal("VCB", r.Owner.BankName); Assert.Equal("Đã sửa", home.Name);
        Assert.DoesNotContain("<script>", home.DescriptionHtml);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UpdateDraftAsync(r.Id, home.Id, input with { PhoneNumber = "0999999999" }, actor));
        Assert.Equal("+84912345678", (await h.Db.Owners.Include(o => o.Person).SingleAsync(o => o.Id == r.OwnerId)).Person.PhoneNumber);
    }
    [Fact]
    public async Task OwnerCannotEditAnotherOwnersDocumentsOrHomestay()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync(); var actor = await h.OwnerActorAsync(r);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(h.H.Homestay.Id,
            DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UpdateDraftAsync(r.Id, h.H.Homestay.Id, h.Input(), actor));
    }
    [Fact]
    public async Task LegacyApprovedOwnerWithActiveHomestayIsNotIncludedInSubmissionScan()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        var old = await h.Db.OwnerRegistrations.SingleAsync();
        h.Db.VerificationDocuments.Add(new() { OwnerRegistrationId = old.Id, DocumentType = DocumentType.BusinessLicense, FileUrl = "legacy.jpg" });
        await h.Db.SaveChangesAsync();
        var r = await h.ReadyAsync(); Assert.True((await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r))).Succeeded);
        Assert.Equal(HomestayStatus.Active, (await h.Db.Homestays.SingleAsync(x => x.Id == h.H.Homestay.Id)).Status);
        Assert.Equal(OwnerRegistrationStatus.Approved, old.Status);
        var document = await h.Db.VerificationDocuments.SingleAsync(d => d.FileUrl == "legacy.jpg");
        Assert.Equal(old.Id, document.OwnerRegistrationId); Assert.Null(document.HomestayId);
    }
    [Fact]
    public async Task ApprovedWithoutAccountDoesNotCreateOne()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        // Hồ sơ cũ có thể đang thẩm định mà chưa có Account; duyệt người chủ không tự cấp mật khẩu.
        r.Status = OwnerRegistrationStatus.UnderReview; r.SubmittedAt = h.H.Clock.Now.UtcDateTime; await h.Db.SaveChangesAsync();
        var count = await h.Db.Users.CountAsync();
        await h.Registrations.ApproveAsync(r.Id, h.Admin);
        Assert.Equal(count, await h.Db.Users.CountAsync()); Assert.Null(r.Owner.Person.Account);
        Assert.Equal(HomestayStatus.Draft, (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status);
    }
    [Theory]
    [InlineData("take")] [InlineData("revision")]
    public async Task AccountantCannotTakeOrRequestRevision(string step)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        if (step == "take") await Assert.ThrowsAsync<BookingException>(() => h.Registrations.TakeAsync(r.Id, h.Accountant));
        else await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung", h.Accountant));
    }
    [Fact]
    public async Task LockedManagerAccountCannotActEvenWithActiveEmployment()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        (await h.Db.Users.SingleAsync(u => u.Id == h.Manager.AccountId)).Status = AccountStatus.Locked; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ApproveAsync(r.Id, h.Manager));
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.ExtendAsync(1, h.Today.AddYears(2), h.Manager));
    }
    [Fact]
    public async Task PhoneAndDraftHomestayRequiredToSubmit()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.ReadyAsync();
        r.Owner.Person.PhoneNumber = "invalid";
        (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status = HomestayStatus.Suspended;
        await h.Db.SaveChangesAsync();
        var result = await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r));
        Assert.False(result.Succeeded); Assert.Contains("Số điện thoại chủ", result.Missing); Assert.Contains("Ít nhất một homestay nháp", result.Missing);
    }
    [Theory]
    [InlineData(ContractStatus.Terminated)] [InlineData(ContractStatus.Expired)]
    public async Task BookingCannotUseClosedContract(ContractStatus status)
    {
        using var h = await PartnerTestHost.CreateAsync(); (await h.Db.Contracts.SingleAsync()).Status = status; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), h.Today.AddDays(4))));
    }
    [Fact]
    public async Task FutureContractCannotBeUsedEvenWhenEndDateCoversStay()
    {
        using var h = await PartnerTestHost.CreateAsync(); (await h.Db.Contracts.SingleAsync()).StartDate = h.Today.AddDays(1); await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), h.Today.AddDays(4))));
    }
    [Fact]
    public async Task ContractAuditFailureRollsBackBusinessUpdate()
    {
        using var h = await PartnerTestHost.CreateAsync(); var contract = await h.Db.Contracts.SingleAsync(); var end = contract.EndDate;
        await using var db = h.H.NewContext(new AuditFailure());
        var service = new ContractService(db, new NotificationService(db), h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExtendAsync(contract.Id, end.AddDays(10), h.Manager));
        h.Db.ChangeTracker.Clear(); Assert.Equal(end, (await h.Db.Contracts.SingleAsync()).EndDate); Assert.Empty(h.Db.AuditLogs);
    }
    [Fact]
    public async Task ConcurrentActiveContractUniqueIsTranslatedAndBothInsertsRollback()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        (await h.Db.Contracts.SingleAsync()).Status = ContractStatus.Terminated; await h.Db.SaveChangesAsync();
        await using var db = h.H.NewContext(new CompetingActiveContract());
        var service = new ContractService(db, new NotificationService(db), h.H.Clock);
        var ex = await Assert.ThrowsAsync<BookingException>(() => service.CreateAsync(new(h.H.Homestay.Id, "HD-RACE", h.Today, h.Today.AddYears(1), .12m), h.Manager));
        Assert.Contains("đã có hợp đồng Active", ex.Message);
        Assert.Single(h.Db.Contracts); Assert.Empty(h.Db.AuditLogs);
    }
    private sealed class AuditFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Lỗi lưu audit giả lập");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class CompetingActiveContract : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            var inserting = db.ChangeTracker.Entries<Contract>().Single(e => e.State == EntityState.Added).Entity;
            db.Add(new Contract { HomestayId = inserting.HomestayId, ContractNo = "HD-COMPETING", StartDate = inserting.StartDate,
                EndDate = inserting.EndDate, CommissionRate = .12m });
            return ValueTask.FromResult(result);
        }
    }
}
