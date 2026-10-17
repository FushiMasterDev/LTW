using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class OwnerRegistrationQueryServiceTests
{
    private static OwnerRegistrationQueryService Query(PartnerTestHost h)
    {
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e => e.ContentRootPath).Returns(h.DirectoryPath);
        return new(h.Db, new ReceiptFileStore(env.Object), h.H.Clock);
    }

    [Theory]
    [InlineData(false, true)] [InlineData(true, false)]
    public async Task OverdueUsesWorkingDaysAndHolidays(bool holidayInWindow, bool overdue)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.UnderReviewAsync(); // nộp 10:00 thứ Ba 10/11/2026 → hạn 10:00 thứ Sáu 13/11
        if (holidayInWindow) { h.Db.Holidays.Add(new Holiday { Date = new(2026, 11, 12), Name = "Nghỉ thử" }); await h.Db.SaveChangesAsync(); }
        // Giấy tờ mẫu hết hạn đúng ngày nộp: còn hạn hôm nay, sau 3 ngày thì không.
        Assert.Equal(3, Assert.Single(await Query(h).ListAsync(new(), h.Consultant), x => x.Id == r.Id).ValidDocuments);
        h.H.Clock.Advance(TimeSpan.FromDays(3) + TimeSpan.FromHours(1));
        var all = await Query(h).ListAsync(new(), h.Consultant);
        var row = Assert.Single(all, x => x.Id == r.Id);
        Assert.Equal(overdue, row.Overdue);
        Assert.Equal(holidayInWindow ? new DateTime(2026, 11, 16, 10, 0, 0) : new DateTime(2026, 11, 13, 10, 0, 0), row.DeadlineLocal);
        Assert.Equal(overdue ? 1 : 0, (await Query(h).ListAsync(new(OverdueOnly: true), h.Consultant)).Count);
        Assert.NotNull(row.Reviewer); Assert.Equal(0, row.ValidDocuments);
    }

    [Fact]
    public async Task ConsultantSeesMaskedBankAccountManagerSeesFull()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.DraftAsync();
        Assert.Equal("*****6789", (await Query(h).DetailsAsync(r.Id, h.Consultant)).BankAccountNo);
        var full = await Query(h).DetailsAsync(r.Id, h.Manager);
        Assert.Equal("123456789", full.BankAccountNo); Assert.Single(full.Homestays);
        await Assert.ThrowsAsync<BookingException>(() => Query(h).DetailsAsync(r.Id, h.Accountant));
    }

    [Fact]
    public async Task OwnerSeesOwnRevisionNoteButNotReviewer()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.UnderReviewAsync();
        await h.Registrations.RequestRevisionAsync(r.Id, "Bổ sung giấy PCCC rõ hơn", h.Consultant);
        var mine = await Query(h).MineAsync(await h.OwnerActorAsync(r));
        Assert.NotNull(mine); Assert.Equal(OwnerRegistrationStatus.NeedsRevision, mine.Status); Assert.Equal("Bổ sung giấy PCCC rõ hơn", mine.ReviewNote);
        Assert.DoesNotContain(typeof(OwnerRegistrationView).GetProperties(), p => p.Name.Contains("Reviewer"));
        // Nhân viên không dùng được đường xem hồ sơ của chủ.
        await Assert.ThrowsAsync<BookingException>(() => Query(h).MineAsync(h.Consultant));
    }

    [Fact]
    public async Task PrivateDocumentOpensOnlyForReviewerOrSameOwner()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var r = await h.DraftAsync(); var owner = await h.OwnerActorAsync(r);
        var home = await h.Db.Homestays.SingleAsync(x => x.OwnerId == r.OwnerId);
        var doc = await h.Registrations.UploadDocumentAsync(home.Id, DocumentType.FireSafety, PartnerTestHost.Image("pccc.png"), h.Today.AddYears(1), owner);
        var file = await Query(h).OpenDocumentAsync(doc.Id, h.Consultant);
        Assert.True(File.Exists(file.Path)); Assert.Equal("image/png", file.ContentType); Assert.Equal("pccc.png", file.DownloadName);
        Assert.Equal(file.Path, (await Query(h).OpenDocumentAsync(doc.Id, owner)).Path);

        var other = await h.Registrations.RegisterAsync(h.Input("0912345679"), PartnerTestHost.Password);
        await Assert.ThrowsAsync<BookingException>(() => Query(h).OpenDocumentAsync(doc.Id, h.OwnerActorAsync(other).Result));
        await Assert.ThrowsAsync<BookingException>(() => Query(h).OpenDocumentAsync(doc.Id, new(ActorType.Customer, owner.AccountId)));
        await Assert.ThrowsAsync<BookingException>(() => Query(h).OpenDocumentAsync(doc.Id, h.Accountant));
        File.Delete(file.Path);
        await Assert.ThrowsAsync<BookingException>(() => Query(h).OpenDocumentAsync(doc.Id, h.Consultant));
    }
}
