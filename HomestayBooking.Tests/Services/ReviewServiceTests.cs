using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class ReviewServiceTests
{
    [Theory]
    [InlineData(1)] [InlineData(5)]
    public async Task AccountCustomerCreatesOnceWithBookingHomestayAndPlainText(int rating)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed);
        var r = await h.Reviews().CreateAsync(b.Id, rating, "  <b>Nhận xét dạng chữ</b>  ", h.Customer);
        Assert.Equal(b.HomestayId, r.HomestayId); Assert.Equal("<b>Nhận xét dạng chữ</b>", r.Comment);
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.B.Owner.AccountId && n.Message.Contains("đánh giá"));
        var audit = await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Reviews"); Assert.Equal(r.Id.ToString(), audit.RecordId); Assert.Equal(h.Customer.AccountId, audit.AccountId);
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Reviews().CreateAsync(b.Id, 4, "Lặp", h.Customer));
        Assert.Contains("đã được đánh giá", ex.Message); Assert.Single(h.Db.Reviews);
    }

    [Theory]
    [InlineData("deposited")] [InlineData("confirmed")] [InlineData("other")]
    [InlineData("employee")] [InlineData("locked")] [InlineData("rating0")] [InlineData("rating6")] [InlineData("long")]
    public async Task RejectsInvalidStateActorAndInput(string bad)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, bad == "deposited" ? BookingStatus.Deposited : bad == "confirmed" ? BookingStatus.Confirmed : BookingStatus.Completed);
        var actor = bad == "other" ? h.Customer with { AccountId = h.B.Owner.AccountId } : bad == "employee" ? h.P.Manager : h.Customer;
        if (bad == "locked") { (await h.Db.Users.SingleAsync(u => u.Id == actor.AccountId)).Status = AccountStatus.Locked; await h.Db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews().CreateAsync(b.Id, bad == "rating0" ? 0 : bad == "rating6" ? 6 : 4, bad == "long" ? new string('X', 1001) : "Hợp lệ", actor));
        Assert.Empty(h.Db.Reviews);
    }

    [Theory]
    [InlineData("", null)] [InlineData(" \t\n", null)] [InlineData("abc", "abc")]
    public async Task CommentTrimAndOptionalEmpty(string input, string? expected)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed);
        Assert.Equal(expected, (await h.Reviews().CreateAsync(b.Id, 3, input, h.Customer)).Comment);
    }

    [Theory]
    [InlineData("contact", true)] [InlineData("customer", true)] [InlineData("wrong-code", false)]
    [InlineData("wrong-phone", false)] [InlineData("invalid-phone", false)]
    public async Task LookupWorksWithoutAccountUsingEitherNormalizedPhone(string kind, bool allowed)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var customer = new Customer { Person = new() { FullName = "Khách điện thoại", PhoneNumber = "+84912345678" } };
        h.Db.Add(customer); await h.Db.SaveChangesAsync();
        var b = await h.H.Bookings().CreateAsync(h.H.Request(h.Today, h.Today.AddDays(1)) with { CustomerId = customer.Id, ContactPhone = "+84988888888" });
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.CheckedIn, Actor.System);
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Completed, Actor.System);
        var code = kind == "wrong-code" ? "FJ000000-999" : " " + b.Code.ToLowerInvariant() + " ";
        var phone = kind == "customer" ? "0912 345 678" : kind == "wrong-phone" ? "0911111111" : kind == "invalid-phone" ? "bad" : "0988888888";
        if (allowed)
        {
            var r = await h.Reviews().CreateByLookupAsync(code, phone, 5, new string('X', 1000));
            Assert.Equal(1000, r.Comment!.Length); Assert.Null((await h.Db.AuditLogs.SingleAsync(a => a.TableName == "Reviews")).AccountId);
        }
        else await Assert.ThrowsAsync<BookingException>(() => h.Reviews().CreateByLookupAsync(code, phone, 5, null));
    }

    [Fact]
    public async Task OwnerCanReplyAndEditWithTimeAuditAndCustomerNotice()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed);
        var r = await h.Reviews().CreateAsync(b.Id, 5, null, h.Customer);
        await h.Reviews().ReplyAsync(r.Id, "  Cảm ơn  ", h.B.Owner); var first = r.RepliedAt;
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await h.Reviews().ReplyAsync(r.Id, new string('X', 1000), h.B.Owner);
        Assert.True(r.RepliedAt > first); Assert.Equal(1000, r.OwnerReply!.Length);
        var log = await h.Db.AuditLogs.Where(a => a.TableName == "Reviews" && a.Action == AuditAction.Update).OrderBy(a => a.Id).LastAsync();
        Assert.Equal(h.B.Owner.AccountId, log.AccountId); Assert.Contains("RepliedAt", log.NewValues);
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Customer.AccountId && n.Message.Contains("đã trả lời"));
        Assert.All(h.Db.Notifications, n => Assert.True(n.Message.Length <= 500));
    }

    [Theory]
    [InlineData(" ")] [InlineData("\t\r\n\u00a0")] [InlineData("long")] [InlineData("other-owner")] [InlineData("manager")]
    public async Task ReplyRejectsWhitespaceTooLongAndOtherActor(string bad)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed);
        var r = await h.Reviews().CreateAsync(b.Id, 5, null, h.Customer);
        var actor = bad == "other-owner" ? new Actor(ActorType.Owner, h.Customer.AccountId) : bad == "manager" ? h.P.Manager : h.B.Owner;
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews().ReplyAsync(r.Id, bad == "long" ? new string('X', 1001) : bad.StartsWith("other") || bad == "manager" ? "Trả lời" : bad, actor));
        Assert.Null((await h.Db.Reviews.SingleAsync()).OwnerReply);
    }

    [Fact]
    public async Task ModerationAndSharedRatingsExcludeHiddenReviewsEverywhere()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var b1 = await h.BookAsync(0, BookingStatus.Completed); var r1 = await h.Reviews().CreateAsync(b1.Id, 5, null, h.Customer);
        var b2 = await h.BookAsync(0, BookingStatus.Completed); var r2 = await h.Reviews().CreateAsync(b2.Id, 1, null, h.Customer);
        Assert.Equal(3, (await h.Reviews().ListAsync(b1.HomestayId)).Average);
        await h.Reviews().SetHiddenAsync(r2.Id, true, "Vi phạm", h.P.Manager);
        var visible = await h.Reviews().ListAsync(b1.HomestayId); Assert.Single(visible.Reviews); Assert.Equal(5, visible.Average); Assert.Equal(1, visible.Count);
        var all = await h.Reviews().ListAsync(b1.HomestayId, includeHidden: true, actor: h.P.Admin); Assert.Equal(2, all.Reviews.Count); Assert.Equal(5, all.Average);
        var featured = Assert.Single(await h.B.Availability().FeaturedAsync()); Assert.Equal(5, featured.Rating); Assert.Equal(1, featured.ReviewCount);
        var staff = Assert.Single(await h.B.Availability().StaffAvailabilityAsync(h.Today.AddDays(5), h.Today.AddDays(6), null, 2, h.P.Manager)); Assert.Equal(5, staff.Rating);
        await h.Reviews().SetHiddenAsync(r2.Id, false, "Đã kiểm tra", h.P.Admin); Assert.Equal(3, (await h.Reviews().ListAsync(b1.HomestayId)).Average);
    }

    [Theory]
    [InlineData("consultant")] [InlineData("accountant")] [InlineData("owner")] [InlineData("empty")]
    public async Task ModerationAndHiddenReadingRequireManagement(string who)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed); var r = await h.Reviews().CreateAsync(b.Id, 5, null, h.Customer);
        var actor = who == "consultant" ? h.P.Consultant : who == "accountant" ? h.P.Accountant : who == "owner" ? h.B.Owner : h.P.Manager;
        await Assert.ThrowsAsync<BookingException>(() => h.Reviews().SetHiddenAsync(r.Id, true, who == "empty" ? " " : "Lý do", actor));
        if (who != "empty") await Assert.ThrowsAsync<BookingException>(() => h.Reviews().ListAsync(b.HomestayId, includeHidden: true, actor: actor));
        Assert.False((await h.Db.Reviews.SingleAsync()).IsHidden);
    }

    [Fact]
    public async Task PublicReviewsAreNewestFirstAndPaginated()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var ids = new List<int>();
        for (var i = 0; i < 11; i++)
        {
            var b = await h.BookAsync(0, BookingStatus.Completed);
            ids.Add((await h.Reviews().CreateAsync(b.Id, 5, null, h.Customer)).Id);
            h.H.Clock.Advance(TimeSpan.FromDays(3));
        }
        var first = await h.Reviews().ListAsync(h.H.Homestay.Id); var last = await h.Reviews().ListAsync(h.H.Homestay.Id, page: 99);
        Assert.Equal(ids.AsEnumerable().Reverse().Take(10), first.Reviews.Select(r => r.Id));
        Assert.Equal(11, first.Count); Assert.True(first.Reviews.HasNextPage); Assert.Equal(2, last.Reviews.PageIndex); Assert.Equal(ids[0], Assert.Single(last.Reviews).Id);
        var empty = await h.Reviews().ListAsync(-1); Assert.Empty(empty.Reviews); Assert.Null(empty.Average); Assert.Equal(0, empty.Count);
    }

    [Fact]
    public async Task UniqueIndexRaceIsTranslatedAndAuditRollsBack()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.Completed); var before = await h.Db.Notifications.CountAsync();
        await using var db = h.H.NewContext(new RivalReview(b.Id, b.HomestayId, h.H.Clock.Now.UtcDateTime));
        var ex = await Assert.ThrowsAsync<BookingException>(() => h.Reviews(db).CreateAsync(b.Id, 4, null, h.Customer));
        Assert.Equal("Đơn này đã được đánh giá.", ex.Message); Assert.Empty(h.Db.Reviews);
        Assert.Equal(before, await h.Db.Notifications.CountAsync()); Assert.Empty(await h.Db.AuditLogs.Where(a => a.TableName == "Reviews").ToListAsync());
    }
    private sealed class RivalReview(int bookingId, int homeId, DateTime now) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Review>().Any(e => e.State == EntityState.Added))
                await eventData.Context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Reviews (BookingId, HomestayId, Rating, IsHidden, CreatedAt) VALUES ({bookingId}, {homeId}, 5, 0, {now})", cancellationToken);
            return result;
        }
    }
}
