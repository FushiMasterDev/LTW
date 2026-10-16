using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class PartnerReviewTests
{
    private static Actor Staff(PartnerTestHost h, EmployeePosition position) => position switch
    {
        EmployeePosition.Consultant => h.Consultant,
        EmployeePosition.Manager => h.Manager,
        EmployeePosition.Administrator => h.Admin,
        _ => h.Accountant,
    };

    [Theory]
    [InlineData(EmployeePosition.Consultant)]
    [InlineData(EmployeePosition.Manager)]
    [InlineData(EmployeePosition.Administrator)]
    public async Task ResendInvalidatesOldTokenAndNewTokenActivates(EmployeePosition position)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        var original = await h.Registrations.InviteAsync(r.Id, h.Consultant);
        var stamp = (await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId)).SecurityStamp;
        var actor = Staff(h, position);
        var replacement = await h.Registrations.ResendInviteAsync(r.Id, actor);
        Assert.NotEqual(stamp, (await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId)).SecurityStamp);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, original.Token, PartnerTestHost.Password));
        Assert.Equal(OwnerRegistrationStatus.WaitingOwnerActivation, (await h.Db.OwnerRegistrations.SingleAsync()).Status);
        r = await h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, replacement.Token, PartnerTestHost.Password);
        Assert.Equal(OwnerRegistrationStatus.Draft, r.Status);
        Assert.Equal(AccountStatus.Active, r.Owner.Person.Account!.Status);
        var log = await h.Db.AuditLogs.SingleAsync(a => a.TableName == "OwnerRegistrations" && a.NewValues!.Contains("ResendInvite"));
        Assert.Equal(actor.AccountId, log.AccountId); Assert.Equal(r.Id.ToString(), log.RecordId);
        var json = string.Join(" ", await h.Db.AuditLogs.Select(a => a.OldValues + a.NewValues).ToListAsync());
        Assert.DoesNotContain(original.Token, json); Assert.DoesNotContain(replacement.Token, json);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ResendInviteAsync(r.Id, actor));
    }

    [Fact]
    public async Task InvalidTokenCanRecoverThroughResend()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        var original = await h.Registrations.InviteAsync(r.Id, h.Consultant);
        // Token bị sửa đại diện cho token không còn hợp lệ; không thay thời gian thật của Identity.
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, "invalid-token", PartnerTestHost.Password));
        var replacement = await h.Registrations.ResendInviteAsync(r.Id, h.Manager);
        r = await h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, replacement.Token, PartnerTestHost.Password);
        Assert.Equal(AccountStatus.Active, r.Owner.Person.Account!.Status);
        Assert.True(await h.Users.CheckPasswordAsync(r.Owner.Person.Account, PartnerTestHost.Password));
    }

    [Theory]
    [InlineData("draft")] [InlineData("noAccount")] [InlineData("active")] [InlineData("hasPassword")]
    public async Task ResendRequiresWaitingPendingAccountWithoutPassword(string invalid)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        if (invalid == "noAccount") r.Status = OwnerRegistrationStatus.WaitingOwnerActivation;
        else
        {
            await h.Registrations.InviteAsync(r.Id, h.Consultant);
            if (invalid == "draft") r.Status = OwnerRegistrationStatus.Draft;
            else if (invalid == "active") r.Owner.Person.Account!.Status = AccountStatus.Active;
            else Assert.True((await h.Users.AddPasswordAsync(r.Owner.Person.Account!, PartnerTestHost.Password)).Succeeded);
        }
        await h.Db.SaveChangesAsync();
        var before = await h.Db.AuditLogs.CountAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ResendInviteAsync(r.Id, h.Manager));
        Assert.Equal(before, await h.Db.AuditLogs.CountAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ResendRejectsAccountantAndInactiveManager(bool inactiveManager)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        await h.Registrations.InviteAsync(r.Id, h.Consultant);
        if (inactiveManager)
        {
            (await h.Db.Employees.SingleAsync(e => e.Id == h.Manager.EmployeeId)).EmploymentStatus = EmploymentStatus.Suspended;
            await h.Db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ResendInviteAsync(r.Id, inactiveManager ? h.Manager : h.Accountant));
    }

    [Theory]
    [InlineData("revision", "cần bổ sung")]
    [InlineData("reject", "bị từ chối")]
    [InlineData("approve", "đã được duyệt")]
    public async Task VietnameseNoticeOmitsReviewNoteAndFitsFiveHundred(string action, string phrase)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        var note = new string('X', 1000);
        if (action == "revision") r = await h.Registrations.RequestRevisionAsync(r.Id, note, h.Consultant);
        else if (action == "reject") r = await h.Registrations.RejectAsync(r.Id, note, h.Manager);
        else r = await h.Registrations.ApproveAsync(r.Id, h.Manager);
        var message = (await h.Db.Notifications.SingleAsync()).Message;
        Assert.Contains(phrase, message); Assert.DoesNotContain(note, message);
        Assert.InRange(message.Length, 1, 500);
        if (action != "approve") Assert.Equal(note, r.ReviewNote);
    }

    [Theory]
    [InlineData(HomestayStatus.Draft, true)]
    [InlineData(HomestayStatus.PendingReview, true)]
    [InlineData(HomestayStatus.Active, false)]
    [InlineData(HomestayStatus.Suspended, false)]
    [InlineData(HomestayStatus.Ended, false)]
    public async Task NeedsRevisionDoesNotUnlockDocumentsOfOperatingOrClosedHomestays(HomestayStatus status, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        await h.Registrations.RequestRevisionAsync(r.Id, "Cần bổ sung", h.Consultant);
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId); home.Status = status; await h.Db.SaveChangesAsync();
        var actor = await h.OwnerActorAsync(r); var existing = await h.Db.VerificationDocuments.FirstAsync();
        if (allowed)
        {
            await h.Registrations.DeleteDocumentAsync(existing.Id, actor);
            Assert.NotNull(await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor));
        }
        else
        {
            var before = await h.Db.VerificationDocuments.CountAsync();
            await Assert.ThrowsAsync<BookingException>(() => h.Registrations.DeleteDocumentAsync(existing.Id, actor));
            await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor));
            Assert.Equal(before, await h.Db.VerificationDocuments.CountAsync()); Assert.False(Directory.Exists(h.DirectoryPath));
        }
    }

    [Theory]
    [InlineData(EmployeePosition.Consultant)]
    [InlineData(EmployeePosition.Manager)]
    [InlineData(EmployeePosition.Administrator)]
    public async Task EmployeeMayEditAssistedDraftOnlyUntilOwnerActivates(EmployeePosition position)
    {
        using var h = await PartnerTestHost.CreateAsync(); var actor = Staff(h, position);
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        // Chưa Account: nhân viên sửa nháp và giấy tờ được.
        await h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input() with { BankName = "VCB" }, actor);
        var beforeInvitation = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.BusinessLicense, PartnerTestHost.Image(), null, actor);
        await h.Registrations.DeleteDocumentAsync(beforeInvitation.Id, actor);
        var invite = await h.Registrations.InviteAsync(r.Id, actor);
        // PendingActivation: nhân viên vẫn được sửa, chủ chưa có tài khoản hoạt động thì chưa được sửa.
        await h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input() with { BankName = "MB" }, actor);
        var document = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor);
        var owner = await h.OwnerActorAsync(r);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input(), owner));
        r = await h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, invite.Token, PartnerTestHost.Password);
        // Đã kích hoạt: chỉ chủ sửa được, kể cả thông tin ngân hàng và giấy tờ.
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input() with { BankName = "KHAC" }, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.DeleteDocumentAsync(document.Id, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.FireSafety, PartnerTestHost.Image(), null, actor));
        await h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input() with { BankName = "BIDV" }, owner);
        await h.Registrations.DeleteDocumentAsync(document.Id, owner);
        Assert.NotNull(await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.FireSafety, PartnerTestHost.Image(), null, owner));
        Assert.Equal("BIDV", (await h.Db.Owners.SingleAsync(o => o.Id == r.OwnerId)).BankName);
    }

    [Fact]
    public async Task EmployeeCannotEditSelfServiceRegistrationEvenWhenAccountIsPending()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        await h.Registrations.InviteAsync(r.Id, h.Consultant);
        r.Source = OwnerRegistrationSource.SelfService; await h.Db.SaveChangesAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UpdateDraftAsync(r.Id, home.Id, h.Input(), h.Manager));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, h.Manager));
    }

    [Fact]
    public async Task AllFourInsertAuditsUseActualPrimaryKey()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        var document = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.BusinessLicense, PartnerTestHost.Image(), null, await h.OwnerActorAsync(r));
        await h.ApproveExistingOwnerAsync();
        var previous = await h.Db.Contracts.SingleAsync(); await h.Contracts.TerminateAsync(previous.Id, "Ký hợp đồng mới", h.Manager);
        var contract = await h.Contracts.CreateAsync(new(h.H.Homestay.Id, "HD-ID", h.Today, h.Today.AddYears(1), .12m), h.Manager);
        foreach (var (table, id) in new[] { ("OwnerRegistrations", r.Id), ("Homestays", home.Id), ("Contracts", contract.Id), ("VerificationDocuments", document.Id) })
        {
            Assert.True(id > 0);
            var audit = await h.Db.AuditLogs.SingleAsync(a => a.TableName == table && a.RecordId == id.ToString() && a.Action == AuditAction.Insert);
            Assert.NotNull(audit.NewValues); Assert.NotNull(audit.AccountId);
        }
        Assert.All(await h.Db.AuditLogs.Where(a => a.Action == AuditAction.Insert).ToListAsync(), a => Assert.True(int.TryParse(a.RecordId, out var id) && id > 0));
    }

    [Fact]
    public async Task InsertThenAuditFailureRollsBackContractAndKeepsVietnameseUniqueGuard()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        var previous = await h.Db.Contracts.SingleAsync(); previous.Status = ContractStatus.Terminated; await h.Db.SaveChangesAsync();
        await using var db = h.H.NewContext(new FailAuditSave());
        var service = new ContractService(db, new NotificationService(db), h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new(h.H.Homestay.Id, "HD-ROLLBACK", h.Today, h.Today.AddYears(1), .12m), h.Manager));
        Assert.Single(h.Db.Contracts); Assert.Empty(h.Db.AuditLogs);
        var contract = await h.Contracts.CreateAsync(new(h.H.Homestay.Id, "HD-SUCCESS", h.Today, h.Today.AddYears(1), .12m), h.Manager);
        var error = await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(new(h.H.Homestay.Id, "HD-DUPLICATE", h.Today, h.Today.AddYears(1), .12m), h.Manager));
        Assert.Contains("đã có hợp đồng Active", error.Message);
        Assert.NotNull(await h.Db.AuditLogs.SingleAsync(a => a.RecordId == contract.Id.ToString() && a.TableName == "Contracts"));
    }

    private sealed class FailAuditSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Lỗi lưu audit giả lập sau khi tạo bản ghi");
            return ValueTask.FromResult(result);
        }
    }
}
