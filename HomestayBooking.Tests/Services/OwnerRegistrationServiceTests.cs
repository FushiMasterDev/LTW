using System.Text.Json;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace HomestayBooking.Tests.Services;

public sealed class OwnerRegistrationServiceTests
{
    public static IEnumerable<object[]> Transitions()
    {
        // Kỳ vọng độc lập với CanTransition: đủ 49 cặp, kể cả trạng thái không đổi.
        var allowed = new (OwnerRegistrationStatus, OwnerRegistrationStatus)[] {
            (OwnerRegistrationStatus.Draft, OwnerRegistrationStatus.WaitingOwnerActivation),
            (OwnerRegistrationStatus.Draft, OwnerRegistrationStatus.PendingReview),
            (OwnerRegistrationStatus.WaitingOwnerActivation, OwnerRegistrationStatus.Draft),
            (OwnerRegistrationStatus.PendingReview, OwnerRegistrationStatus.UnderReview),
            (OwnerRegistrationStatus.UnderReview, OwnerRegistrationStatus.NeedsRevision),
            (OwnerRegistrationStatus.UnderReview, OwnerRegistrationStatus.Approved),
            (OwnerRegistrationStatus.UnderReview, OwnerRegistrationStatus.Rejected),
            (OwnerRegistrationStatus.NeedsRevision, OwnerRegistrationStatus.PendingReview) };
        foreach (var from in Enum.GetValues<OwnerRegistrationStatus>())
        foreach (var to in Enum.GetValues<OwnerRegistrationStatus>())
            yield return [from, to, allowed.Contains((from, to))];
    }
    [Theory, MemberData(nameof(Transitions))]
    public void AllTransitionPairs(OwnerRegistrationStatus from, OwnerRegistrationStatus to, bool allowed) =>
        Assert.Equal(allowed, OwnerRegistrationService.CanTransition(from, to));

    [Fact]
    public async Task SelfRegistrationCreatesIdentityOwnerAndTwoDrafts()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync();
        var user = await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId);
        Assert.True(await h.Users.CheckPasswordAsync(user, PartnerTestHost.Password));
        Assert.True(await h.Users.IsInRoleAsync(user, AppRoles.Owner));
        Assert.Equal("+84912345678", r.Owner.Person.PhoneNumber);
        Assert.Equal(OwnerRegistrationSource.SelfService, r.Source); Assert.Equal(OwnerRegistrationStatus.Draft, r.Status);
        Assert.Null(r.CreatedByEmployeeId); Assert.Null(r.SubmittedAt);
        Assert.Equal(HomestayStatus.Draft, (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status);
        Assert.Equal(user.Id, (await h.Db.AuditLogs.SingleAsync(a => a.TableName == "OwnerRegistrations")).AccountId);
    }
    [Theory]
    [InlineData("owner")] [InlineData("customer")] [InlineData("employee")]
    public async Task ExistingPhoneCannotBeClaimedWithoutOwningAccount(string kind)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = kind == "owner" ? await h.Db.PersonProfiles.SingleAsync(p => p.Id == h.H.Homestay.Owner.PersonId)
            : kind == "customer" ? await h.Db.PersonProfiles.SingleAsync(p => p.Id == h.H.Customer.PersonId)
            : await h.Db.PersonProfiles.FirstAsync(p => p.Employee != null);
        var before = await h.Db.PersonProfiles.CountAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RegisterAsync(h.Input(person.PhoneNumber), PartnerTestHost.Password));
        Assert.Equal(before, await h.Db.PersonProfiles.CountAsync()); Assert.Empty(h.Db.OwnerRegistrations);
    }
    [Fact]
    public async Task ExistingCustomerMustAuthenticateToReuseOwnPersonAccount()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var account = await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Customer.PersonId);
        var before = await h.Db.Users.CountAsync();
        var r = await h.Registrations.RegisterAsync(h.Input(h.H.Customer.Person.PhoneNumber), "unused",
            new Actor(ActorType.Customer, account.Id));
        Assert.Equal(account.PersonId, r.Owner.PersonId); Assert.Equal(before, await h.Db.Users.CountAsync());
        Assert.True(await h.Users.IsInRoleAsync(account, AppRoles.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RegisterAsync(h.Input(h.H.Customer.Person.PhoneNumber), "unused", new Actor(ActorType.Owner, account.Id)));
    }
    [Fact]
    public async Task InvalidPasswordRollsBackPersonOwnerAndDraft()
    {
        using var h = await PartnerTestHost.CreateAsync(); var count = await h.Db.PersonProfiles.CountAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RegisterAsync(h.Input(), "x"));
        Assert.Equal(count, await h.Db.PersonProfiles.CountAsync()); Assert.Empty(h.Db.OwnerRegistrations);
    }
    [Fact]
    public async Task AssistedInvitationHasNoPasswordAndOnlyOwnerActivates()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.Registrations.CreateAssistedAsync(h.Input(), h.Consultant);
        Assert.Equal(h.Consultant.EmployeeId, r.CreatedByEmployeeId); Assert.Null(r.Owner.Person.Account);
        Assert.Equal(OwnerRegistrationSource.EmployeeAssisted, r.Source);
        var invite = await h.Registrations.InviteAsync(r.Id, h.Consultant);
        var user = await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId);
        Assert.False(await h.Users.HasPasswordAsync(user)); Assert.Equal(AccountStatus.PendingActivation, user.Status);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, invite.Token, PartnerTestHost.Password, h.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, "0999999999", invite.Token, PartnerTestHost.Password));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, "bad-token", PartnerTestHost.Password));
        r = await h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, invite.Token, PartnerTestHost.Password);
        Assert.Equal(OwnerRegistrationStatus.Draft, r.Status);
        user = await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId);
        Assert.Equal(AccountStatus.Active, user.Status); Assert.True(await h.Users.CheckPasswordAsync(user, PartnerTestHost.Password));
        Assert.DoesNotContain(invite.Token, string.Join(" ", await h.Db.AuditLogs.Select(a => a.NewValues).ToListAsync()));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ActivateAsync(r.Id, h.Input().PhoneNumber, invite.Token, PartnerTestHost.Password));
    }
    [Theory]
    [InlineData("BusinessLicense")] [InlineData("FireSafety")] [InlineData("PropertyPhoto")]
    [InlineData("BankName")] [InlineData("BankAccountNo")] [InlineData("BankAccountName")]
    [InlineData("FullName")] [InlineData("Expired")]
    public async Task IncompleteSubmissionReturnsMissingAndChangesNothing(string missing)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.ReadyAsync();
        if (Enum.TryParse<DocumentType>(missing, out var type))
            h.Db.VerificationDocuments.Remove(await h.Db.VerificationDocuments.SingleAsync(d => d.DocumentType == type));
        else if (missing == "Expired") (await h.Db.VerificationDocuments.FirstAsync()).ExpiryDate = h.Today.AddDays(-1);
        else if (missing == "FullName") r.Owner.Person.FullName = " ";
        else if (missing == "BankName") r.Owner.BankName = null;
        else if (missing == "BankAccountNo") r.Owner.BankAccountNo = " ";
        else r.Owner.BankAccountName = null;
        await h.Db.SaveChangesAsync();
        var result = await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r));
        Assert.False(result.Succeeded); Assert.NotEmpty(result.Missing);
        Assert.Equal(OwnerRegistrationStatus.Draft, result.Registration.Status); Assert.Null(result.Registration.SubmittedAt);
        Assert.Equal(HomestayStatus.Draft, (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status);
    }
    [Fact]
    public async Task CompleteSubmissionSetsVietnamInstantAndHomestayPending()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.ReadyAsync(); var actor = await h.OwnerActorAsync(r);
        var result = await h.Registrations.SubmitAsync(r.Id, actor);
        Assert.True(result.Succeeded); Assert.Equal(OwnerRegistrationStatus.PendingReview, r.Status);
        Assert.Equal(h.H.Clock.Now.UtcDateTime, r.SubmittedAt); Assert.Equal(h.H.Clock.Now.UtcDateTime, r.UpdatedAt);
        Assert.Equal(HomestayStatus.PendingReview, (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.SubmitAsync(r.Id, actor));
        Assert.Single(await h.Db.AuditLogs.Where(a => a.TableName == "Homestays" && a.Action == AuditAction.Update).ToListAsync());
    }
    [Theory]
    [InlineData("staff")] [InlineData("otherOwner")] [InlineData("system")]
    public async Task SubmissionRequiresActualOwner(string who)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.ReadyAsync();
        var actor = who == "staff" ? h.Consultant : who == "system" ? Actor.System : new Actor(ActorType.Owner,
            (await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Homestay.Owner.PersonId)).Id);
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.SubmitAsync(r.Id, actor));
    }
    [Theory]
    [InlineData(EmployeePosition.Consultant)] [InlineData(EmployeePosition.Accountant)]
    public async Task NonManagerCannotApproveOrReject(EmployeePosition position)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        var actor = position == EmployeePosition.Consultant ? h.Consultant : h.Accountant;
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ApproveAsync(r.Id, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RejectAsync(r.Id, "Thiếu", actor));
        Assert.Equal(OwnerRegistrationStatus.UnderReview, (await h.Db.OwnerRegistrations.SingleAsync()).Status);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task CreatorCannotMakeFinalDecision(bool approve)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        r.CreatedByEmployeeId = h.Manager.EmployeeId; r.Source = OwnerRegistrationSource.EmployeeAssisted; await h.Db.SaveChangesAsync();
        if (approve) await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ApproveAsync(r.Id, h.Manager));
        else await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RejectAsync(r.Id, "Không hợp lệ", h.Manager));
    }
    [Theory]
    [InlineData(true, "")] [InlineData(true, " ")] [InlineData(false, "")] [InlineData(false, " ")]
    public async Task RevisionAndRejectionRequireContent(bool revision, string note)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        if (revision) await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RequestRevisionAsync(r.Id, note, h.Consultant));
        else await Assert.ThrowsAsync<BookingException>(() => h.Registrations.RejectAsync(r.Id, note, h.Manager));
    }
    [Theory]
    [InlineData("approve")] [InlineData("reject")] [InlineData("revise")]
    public async Task DecisionsRecordActorTimeAuditNotifyAndRespectSeparateHomestayApproval(string step)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        var account = await h.Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId);
        var stamp = account.SecurityStamp; var actor = step == "revise" ? h.Consultant : h.Manager;
        r = step == "approve" ? await h.Registrations.ApproveAsync(r.Id, actor)
            : step == "reject" ? await h.Registrations.RejectAsync(r.Id, "Không đủ điều kiện", actor)
            : await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung giấy", actor);
        Assert.Equal(actor.EmployeeId, r.ReviewedByEmployeeId); Assert.Equal(h.H.Clock.Now.UtcDateTime, r.ReviewedAt);
        var log = await h.Db.AuditLogs.Where(x => x.TableName == "OwnerRegistrations").OrderBy(x => x.Id).LastAsync(); Assert.Equal(actor.AccountId, log.AccountId);
        Assert.NotNull(log.OldValues); Assert.NotNull(log.NewValues); using var oldJson = JsonDocument.Parse(log.OldValues!);
        Assert.Contains(step == "approve" ? "Approved" : step == "reject" ? "Rejected" : "NeedsRevision", log.NewValues);
        Assert.Single(await h.Db.Notifications.Where(n => n.UserId == account.Id).ToListAsync());
        Assert.Equal(step == "reject" ? HomestayStatus.Draft : HomestayStatus.PendingReview,
            (await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId)).Status);
        if (step == "approve") Assert.NotEqual(stamp, account.SecurityStamp);
    }
    [Fact]
    public async Task RevisionResubmissionResetsSlaWithoutLegacyRevalidation()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung", h.Consultant);
        var before = r.SubmittedAt;
        foreach (var d in h.Db.VerificationDocuments) d.ExpiryDate = h.Today.AddDays(30);
        await h.Db.SaveChangesAsync(); h.H.Clock.Advance(TimeSpan.FromDays(1));
        Assert.True((await h.Registrations.SubmitAsync(r.Id, await h.OwnerActorAsync(r))).Succeeded);
        Assert.True(r.SubmittedAt > before); Assert.Equal(OwnerRegistrationStatus.PendingReview, r.Status);
    }
    [Theory]
    [InlineData(-1, false)] [InlineData(0, true)] [InlineData(1, true)]
    public async Task UploadExpiryBoundaryAndPrivateStorage(int days, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId); var actor = await h.OwnerActorAsync(r);
        if (!allowed) await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.BusinessLicense, PartnerTestHost.Image(), h.Today.AddDays(days), actor));
        else
        {
            var doc = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.BusinessLicense, PartnerTestHost.Image(), h.Today.AddDays(days), actor);
            Assert.Equal(home.Id, doc.HomestayId); Assert.Null(doc.OwnerRegistrationId);
            Assert.True(File.Exists(Path.Combine(h.DirectoryPath, "App_Data", "receipts", doc.FileUrl)));
            Assert.DoesNotContain("wwwroot", doc.FileUrl);
            await h.Registrations.DeleteDocumentAsync(doc.Id, actor); Assert.Empty(h.Db.VerificationDocuments);
        }
    }
    [Theory]
    [InlineData("invalid")] [InlineData("large")]
    public async Task UploadRejectsBadTypeAndSize(string kind)
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.DraftAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        var bytes = kind == "large" ? new byte[ReceiptFileStore.MaxBytes + 1] : new byte[12];
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "doc", "fake.png");
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, file, null, awaitActor()));
        Actor awaitActor() => new(ActorType.Owner, r.Owner.Person.Account!.Id);
        Assert.Empty(h.Db.VerificationDocuments);
    }
    [Fact]
    public async Task DocumentsLockedAfterSubmissionExceptNeedsRevisionAndLegacyLinksUntouched()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId); var actor = await h.OwnerActorAsync(r);
        var doc = await h.Db.VerificationDocuments.FirstAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.DeleteDocumentAsync(doc.Id, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor));
        await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung", h.Consultant);
        var uploaded = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.PropertyPhoto, PartnerTestHost.Image(), null, actor);
        await h.Registrations.DeleteDocumentAsync(uploaded.Id, actor);
        var legacy = new VerificationDocument { OwnerRegistrationId = r.Id, DocumentType = DocumentType.BusinessLicense, FileUrl = "legacy.jpg" };
        h.Db.Add(legacy); await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.DeleteDocumentAsync(legacy.Id, actor));
        Assert.Equal(r.Id, (await h.Db.VerificationDocuments.SingleAsync(x => x.Id == legacy.Id)).OwnerRegistrationId);
    }
    [Fact]
    public async Task InactiveOrForgedEmployeeCannotReview()
    {
        using var h = await PartnerTestHost.CreateAsync(); var r = await h.UnderReviewAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ApproveAsync(r.Id, h.Manager with { AccountId = h.Consultant.AccountId }));
        var employee = await h.Db.Employees.SingleAsync(e => e.Id == h.Manager.EmployeeId);
        employee.EmploymentStatus = EmploymentStatus.Resigned; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Registrations.ApproveAsync(r.Id, h.Manager));
    }
}
