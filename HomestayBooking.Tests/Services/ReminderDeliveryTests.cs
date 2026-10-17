using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class ReminderDeliveryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SharedContactPhoneGetsAllHomestaysAndEachOwnerGetsOnlyTheirAppSchedule(bool differentOwners)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var first = await h.BookAsync();
        var nextType = await h.B.TypeAsync(500000, sameHome: false);
        (await h.Db.Homestays.SingleAsync(x => x.Id == first.HomestayId)).ContactPhone = "+84912000001";
        nextType.Homestay.ContactPhone = "+84912000001";
        if (differentOwners)
        {
            nextType.Homestay.Owner = new Owner { Person = new PersonProfile { FullName = "Chủ cơ sở khác", PhoneNumber = "+84912000998",
                Account = new ApplicationUser { Status = AccountStatus.Active } } };
        }
        await h.Db.SaveChangesAsync();
        var second = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(1), h.Today.AddDays(2)) with { RoomTypeId = nextType.Id });
        await h.H.Bookings().ChangeStatusAsync(second, BookingStatus.Deposited, Actor.System); await h.H.Bookings().ChangeStatusAsync(second, BookingStatus.Confirmed, Actor.System);
        Assert.Equal(differentOwners ? 2 : 1, await h.Reminders().SendOwnersAsync());
        Assert.Equal(0, await h.Reminders().SendOwnersAsync()); var call = Assert.Single(h.Sender.Calls);
        Assert.Contains(first.Code, call.Message); Assert.Contains(second.Code, call.Message);
        var originalNotices = await h.Db.Notifications.Where(n => n.UserId == h.B.Owner.AccountId && n.Url == ReminderService.OwnerUrl(h.H.Homestay.OwnerId, h.Today.AddDays(1))).ToListAsync();
        Assert.Contains(originalNotices, n => n.Message.Contains(first.Code));
        if (differentOwners)
        {
            Assert.DoesNotContain(originalNotices, n => n.Message.Contains(second.Code));
            var ownerId = nextType.Homestay.Owner.Person.Account!.Id;
            var newOwnerNotices = await h.Db.Notifications.Where(n => n.UserId == ownerId && n.Url == ReminderService.OwnerUrl(nextType.Homestay.OwnerId, h.Today.AddDays(1))).ToListAsync();
            Assert.Contains(newOwnerNotices, n => n.Message.Contains(second.Code)); Assert.DoesNotContain(newOwnerNotices, n => n.Message.Contains(first.Code));
        }
        else Assert.Contains(originalNotices, n => n.Message.Contains(second.Code));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task SlowOrFailingSenderHoldsNoDatabaseTransactionOrBookingOwnerLock(bool ownerReminder, bool fails)
    {
        var path = Path.Combine(Path.GetTempPath(), "LTW-B4-send-" + Guid.NewGuid().ToString("N") + ".db");
        using var h = new BookingTestHost("Data Source=" + path + ";Default Timeout=1;Pooling=False");
        var b = await h.Bookings().CreateAsync(h.Request(VietnamTime.Today(h.Clock).AddDays(1), VietnamTime.Today(h.Clock).AddDays(2)));
        await h.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        await h.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new RecordingMessageSender { Fails = _ => fails };
        sender.BeforeSend = async _ => { Assert.Null(h.Db.Database.CurrentTransaction); started.TrySetResult(); await release.Task; };
        var reminder = Service(h.Db, h.Clock, sender);
        var sending = ownerReminder ? reminder.SendOwnersAsync() : reminder.SendCustomersAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await using var other = h.NewIndependentContext();
            Assert.Single(await other.MessageLogs.Where(m => m.Status == MessageStatus.Pending).ToListAsync());
            Assert.Equal(1, await other.Bookings.Where(x => x.Id == b.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.SpecialRequests, "Đơn vẫn sửa được")));
            Assert.Equal(1, await other.Owners.ExecuteUpdateAsync(s => s.SetProperty(o => o.BankName, "Ngân hàng cập nhật")));
            var second = Service(other, h.Clock, sender);
            Assert.Equal(0, ownerReminder ? await second.SendOwnersAsync() : await second.SendCustomersAsync());
            Assert.Single(sender.Calls);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(fails ? 0 : 1, await sending);
        Assert.Equal(fails ? 2 : 1, sender.Calls.Count);
        Assert.Equal("Đơn vẫn sửa được", await h.Db.Bookings.AsNoTracking().Where(x => x.Id == b.Id).Select(x => x.SpecialRequests).SingleAsync());
        // File SQLite thử nằm trong thư mục tạm; không liên quan SQL Server hay dữ liệu ứng dụng.
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task ResultSaveFailureKeepsCommittedPendingAndNextRunDoesNotResend(bool fallback, bool ownerReminder)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync();
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        h.Sender.Fails = channel => fallback && channel == MessageChannel.Zalo;
        await using var broken = h.H.NewContext(new FailResultSave());
        await Assert.ThrowsAsync<InvalidOperationException>(() => ownerReminder ? h.Reminders(broken).SendOwnersAsync() : h.Reminders(broken).SendCustomersAsync());
        var calls = h.Sender.Calls.Count;
        var logs = await h.Db.MessageLogs.AsNoTracking().ToListAsync();
        Assert.Single(logs, m => m.Status == MessageStatus.Pending); Assert.DoesNotContain(logs, m => m.Status == MessageStatus.Sent);
        var url = ownerReminder ? ReminderService.OwnerUrl(h.H.Homestay.OwnerId, h.Today.AddDays(1))
            : ReminderService.CustomerUrl((await h.Db.Bookings.SingleAsync()).Code);
        Assert.Single(await h.Db.Notifications.Where(n => n.Url == url).ToListAsync());
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync()); h.H.Clock.Advance(TimeSpan.FromMinutes(9));
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync()); Assert.Equal(calls, h.Sender.Calls.Count);
        Assert.Single(await h.Db.Notifications.Where(n => n.Url == url).ToListAsync());
    }

    [Theory]
    [InlineData(10, false, false)] [InlineData(11, true, false)] [InlineData(10, false, true)] [InlineData(11, true, true)]
    public async Task StalePendingIsFailedAndRetriedExactlyOnceAfterTenMinutes(int minutes, bool retry, bool ownerReminder)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync();
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        var pending = new MessageLog { BookingId = ownerReminder ? null : b.Id, TemplateCode = ownerReminder ? ReminderService.OwnerTemplate : ReminderService.CustomerTemplate, Channel = MessageChannel.Zalo,
            RecipientPhone = ownerReminder ? "+84912000001" : b.ContactPhone, CreatedAt = h.H.Clock.Now.UtcDateTime };
        h.Db.Add(pending); await h.Db.SaveChangesAsync(); h.Db.Entry(pending).State = EntityState.Detached;
        h.H.Clock.Advance(TimeSpan.FromMinutes(minutes));
        h.Sender.BeforeSend = async _ =>
        {
            Assert.Null(h.Db.Database.CurrentTransaction);
            await using var next = h.H.NewContext();
            Assert.Equal(0, ownerReminder ? await h.Reminders(next).SendOwnersAsync() : await h.Reminders(next).SendCustomersAsync());
        };
        Assert.Equal(retry ? 1 : 0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync());
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync()); Assert.Equal(retry ? 1 : 0, h.Sender.Calls.Count);
        var old = await h.Db.MessageLogs.AsNoTracking().SingleAsync(m => m.Id == pending.Id);
        Assert.Equal(retry ? MessageStatus.Failed : MessageStatus.Pending, old.Status);
        Assert.Equal(retry ? "Quá thời gian chờ kết quả gửi" : null, old.ErrorMessage);
        Assert.Empty(await h.Db.AuditLogs.Where(a => a.TableName == "MessageLogs").ToListAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task QuietNightWaitsUntilNineNextMorning(bool ownerReminder)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync(ownerReminder ? 2 : 1);
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        h.H.Clock.Now = new(2026, 11, 10, 16, 30, 0, TimeSpan.Zero);
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync());
        Assert.Empty(h.Sender.Calls); Assert.Empty(h.Db.MessageLogs);
        h.H.Clock.Now = new(2026, 11, 11, 2, 0, 0, TimeSpan.Zero);
        Assert.Equal(1, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync());
        Assert.Single(h.Sender.Calls);
    }

    [Fact]
    public async Task FallbackCannotSendSmsAfterQuietHoursHaveStarted()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync(); h.Sender.Fails = _ => true;
        h.H.Clock.Now = new(2026, 11, 10, 13, 59, 0, TimeSpan.Zero);
        h.Sender.BeforeSend = _ => { h.H.Clock.Advance(TimeSpan.FromMinutes(1)); return Task.CompletedTask; };
        Assert.Equal(0, await h.Reminders().SendCustomersAsync()); Assert.Single(h.Sender.Calls);
        Assert.Equal(MessageChannel.Zalo, h.Sender.Calls[0].Channel);
        Assert.Empty(await h.Db.MessageLogs.Where(m => m.Channel == MessageChannel.Sms).ToListAsync());
    }

    [Fact]
    public async Task SameDayConfirmationAtSevenWaitsUntilNine()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); h.H.Clock.Now = new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);
        await h.BookAsync(0); Assert.Equal(0, await h.Reminders().SendCustomersAsync()); Assert.Empty(h.Db.MessageLogs);
        h.H.Clock.Advance(TimeSpan.FromHours(2)); Assert.Equal(1, await h.Reminders().SendCustomersAsync()); Assert.Single(h.Sender.Calls);
    }

    [Theory]
    [InlineData("https://fuji.vn", true)] [InlineData("http://localhost:5011/app", true)]
    [InlineData("", false)] [InlineData("/relative", false)] [InlineData("ftp://fuji.vn", false)]
    [InlineData("https://user:pass@fuji.vn", false)] [InlineData("https://fuji.vn?a=1", false)] [InlineData("https://fuji.vn/#anchor", false)]
    [InlineData("https://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.vn", false)]
    public void PublicUrlValidationIsAppliedAtStartup(string url, bool valid)
    {
        var options = new ReminderOptions { PublicBaseUrl = url };
        Assert.Equal(valid, new DataAnnotationValidateOptions<ReminderOptions>(Options.DefaultName).Validate(Options.DefaultName, options).Succeeded);
    }

    private static ReminderService Service(ApplicationDbContext db, TimeProvider clock, RecordingMessageSender sender) => new(db,
        new(db, sender, clock, NullLogger<MessageService>.Instance), new(db), clock, Options.Create(new ReminderOptions()), NullLogger<ReminderService>.Instance);

    private sealed class FailResultSave : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Chỉ lỗi bước lưu Sent, sau khi Reserve đã commit thông báo app và Pending.
            if (await ((ApplicationDbContext)eventData.Context!).MessageLogs.AsNoTracking().AnyAsync(m => m.Status == MessageStatus.Sent, cancellationToken))
                throw new InvalidOperationException("Giả lập lỗi lưu kết quả gửi");
            return result;
        }
    }
}
