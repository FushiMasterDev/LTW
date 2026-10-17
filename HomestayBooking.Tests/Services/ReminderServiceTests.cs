using System.Globalization;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class ReminderServiceTests
{
    [Theory]
    [InlineData(0, 8, false)] [InlineData(1, 8, false)] [InlineData(1, 9, true)] [InlineData(2, 10, false)]
    [InlineData(0, 9, true)] [InlineData(0, 21, false)] [InlineData(1, 20, true)]
    public async Task TimingUsesVietnamDateAndQuietHoursEvenOnArrivalDay(int days, int localHour, bool expected)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        h.H.Clock.Now = new(2026, 11, 10, localHour - 7, 0, 0, TimeSpan.Zero);
        var b = await h.BookAsync(days);
        var home = await h.Db.Homestays.SingleAsync(); home.ContactPhone = "+84912000001"; home.CheckinGuideHtml = "<p>Cửa chính</p><script>khonggui()</script><p>Gọi chủ</p>";
        await h.Db.SaveChangesAsync();
        Assert.Equal(expected ? 1 : 0, await h.Reminders().SendCustomersAsync());
        Assert.Equal(0, await h.Reminders().SendCustomersAsync()); Assert.Equal(expected ? 1 : 0, h.Sender.Calls.Count);
        Assert.Equal(expected ? 1 : 0, await h.Db.Notifications.CountAsync(n => n.Url == ReminderService.CustomerUrl(b.Code)));
        if (expected)
        {
            var call = Assert.Single(h.Sender.Calls); Assert.Equal(b.ContactPhone, call.Phone);
            Assert.DoesNotContain(home.Address, call.Message); Assert.Contains(home.ContactPhone, call.Message);
            Assert.Contains(h.Options.PublicBaseUrl + "/Bookings/Details/" + b.Code, call.Message); Assert.True(call.Message.Length <= 160);
            Assert.DoesNotContain("Cửa chính", call.Message); Assert.DoesNotContain("maps", call.Message);
            Assert.DoesNotContain("script", call.Message); Assert.DoesNotContain("khonggui", call.Message);
            var log = await h.Db.MessageLogs.SingleAsync(); Assert.Equal(ReminderService.CustomerTemplate, log.TemplateCode); Assert.Equal(b.Id, log.BookingId);
            Assert.Single(await h.Db.Notifications.Where(n => n.UserId == h.Customer.AccountId && n.Message.Contains("Sắp đến lịch")).ToListAsync());
        }
    }

    [Theory]
    [InlineData(BookingStatus.AwaitingPayment)] [InlineData(BookingStatus.Deposited)]
    [InlineData(BookingStatus.CheckedIn)] [InlineData(BookingStatus.Completed)]
    public async Task OnlyConfirmedBookingsReceiveCustomerReminder(BookingStatus status)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync(0, status);
        Assert.Equal(0, await h.Reminders().SendCustomersAsync()); Assert.Empty(h.Sender.Calls); Assert.Empty(h.Db.MessageLogs);
    }

    [Fact]
    public async Task ShortCustomerMessageContainsOnlySummaryAndLinkEvenWithLongName()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync();
        b.Homestay.Latitude = 21.123456m; b.Homestay.Longitude = 105.123456m;
        b.Homestay.CheckinGuideHtml = "<div>" + new string('X', 400) + "</div><style>secret</style>";
        b.Homestay.Name = new string('Y', 200); b.Homestay.ContactPhone = "+84912000001";
        h.Options.PublicBaseUrl = "https://fuji.example.vn/app";
        await h.Db.SaveChangesAsync();
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            await h.Reminders().SendCustomersAsync();
            var text = Assert.Single(h.Sender.Calls).Message;
            Assert.Equal(160, text.Length); Assert.Contains(b.Code, text); Assert.Contains(b.CheckIn.ToString("dd/MM/yyyy"), text);
            Assert.Contains("14:00", text); Assert.Contains("+84912000001", text); Assert.Contains(h.Options.PublicBaseUrl + "/Bookings/Details/" + b.Code, text);
            Assert.DoesNotContain("21.123456", text); Assert.DoesNotContain(b.Homestay.Address, text); Assert.DoesNotContain("secret", text); Assert.DoesNotContain("<", text);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task ReconfirmationAfterDateChangeAllowsNewReminder()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(10); h.Options.CustomerDaysBefore = 30;
        Assert.Equal(1, await h.Reminders().SendCustomersAsync());
        h.H.Clock.Advance(TimeSpan.FromMinutes(1));
        var oldCheckin = b.CheckIn;
        var request = await h.H.Changes().SubmitAsync(new(b.Id, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(7), b.CheckOut.AddDays(7)),
            HomestayTestHost.Principal(h.Customer.AccountId!, "Customer"));
        await h.H.Changes().ApproveAsync(request.Id, "Đồng ý lịch mới", HomestayTestHost.Principal(h.P.Manager.AccountId!, "Employee"));
        Assert.Equal(oldCheckin.AddDays(7), b.CheckIn); Assert.Equal(BookingStatus.Deposited, b.Status); Assert.Null(b.ConfirmedAt);
        await h.H.Bookings().ConfirmAsync(b, null, h.B.Owner);
        Assert.Equal(1, await h.Reminders().SendCustomersAsync()); Assert.Equal(0, await h.Reminders().SendCustomersAsync());
        Assert.Equal(2, h.Sender.Calls.Count); Assert.Equal(2, await h.Db.MessageLogs.CountAsync(m => m.Status == MessageStatus.Sent));
        Assert.Equal(2, await h.Db.Notifications.CountAsync(n => n.Url == ReminderService.CustomerUrl(b.Code)));
    }

    [Fact]
    public async Task FailedReminderRetriesThreeCyclesThenStopsUntilReconfirmed()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(); h.Sender.Fails = _ => true;
        for (var i = 0; i < 5; i++) Assert.Equal(0, await h.Reminders().SendCustomersAsync());
        Assert.Equal(6, h.Sender.Calls.Count); Assert.Equal(6, await h.Db.MessageLogs.CountAsync(m => m.Status == MessageStatus.Failed));
        Assert.Equal(3, await h.Db.MessageLogs.CountAsync(m => m.Channel == MessageChannel.Zalo));
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); b.Status = BookingStatus.Deposited; b.ConfirmedAt = null; await h.Db.SaveChangesAsync();
        await h.H.Bookings().ConfirmAsync(b, null, h.B.Owner); h.Sender.Fails = _ => false;
        Assert.Equal(1, await h.Reminders().SendCustomersAsync());
    }

    [Fact]
    public async Task FallbackSuccessDoesNotRetryAndDoesNotSendToAccountPhoneInsteadOfContact()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(); b.ContactPhone = "+84912345678"; await h.Db.SaveChangesAsync();
        h.Sender.Fails = c => c == MessageChannel.Zalo;
        Assert.Equal(1, await h.Reminders().SendCustomersAsync()); Assert.Equal(0, await h.Reminders().SendCustomersAsync());
        Assert.Equal(2, h.Sender.Calls.Count); Assert.All(h.Sender.Calls, c => Assert.Equal("+84912345678", c.Phone));
    }

    [Fact]
    public async Task OwnerArrivalsGroupBookingsSplitAppNoticesAndDeduplicateByOwnerDate()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var type = await h.B.TypeAsync(500000);
        for (var i = 0; i < 5; i++)
        {
            if (i > 0) { type.Rooms.Add(new Room { RoomCode = "N" + i }); await h.Db.SaveChangesAsync(); }
            var b = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(1), h.Today.AddDays(2)) with { RoomTypeId = type.Id, ContactName = new string('X', 100) });
            await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System); await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        }
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        Assert.Equal(1, await h.Reminders().SendOwnersAsync()); Assert.Equal(0, await h.Reminders().SendOwnersAsync());
        var call = Assert.Single(h.Sender.Calls); Assert.Equal("+84912000001", call.Phone);
        foreach (var code in await h.Db.Bookings.Select(b => b.Code).ToListAsync()) Assert.Contains(code, call.Message);
        var notices = await h.Db.Notifications.Where(n => n.Url == ReminderService.OwnerUrl(h.H.Homestay.OwnerId, h.Today.AddDays(1))).ToListAsync();
        Assert.True(notices.Count > 1); Assert.All(notices, n => { Assert.Equal(h.B.Owner.AccountId, n.UserId); Assert.True(n.Message.Length <= 500); });
        var log = await h.Db.MessageLogs.SingleAsync(); Assert.Equal(ReminderService.OwnerTemplate, log.TemplateCode); Assert.Null(log.BookingId);
    }

    [Theory]
    [InlineData("before-hour")] [InlineData("deposited")] [InlineData("two-days")] [InlineData("quiet")]
    public async Task OwnerArrivalsSkipWrongTimeStateOrDate(string bad)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync(bad == "two-days" ? 2 : 1, bad == "deposited" ? BookingStatus.Deposited : BookingStatus.Confirmed);
        if (bad == "before-hour") h.H.Clock.Now = new(2026, 11, 10, 1, 0, 0, TimeSpan.Zero);
        if (bad == "quiet") h.H.Clock.Now = new(2026, 11, 10, 16, 30, 0, TimeSpan.Zero);
        Assert.Equal(0, await h.Reminders().SendOwnersAsync()); Assert.Empty(h.Sender.Calls);
        Assert.Empty(await h.Db.Notifications.Where(n => n.Url != null && n.Url.Contains("arrivals=")).ToListAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BothChannelsFailButAppNoticeIsCommittedBeforeSenderAndNeverRepeated(bool ownerReminder)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync();
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912000001"; await h.Db.SaveChangesAsync();
        var url = ownerReminder ? ReminderService.OwnerUrl(h.H.Homestay.OwnerId, h.Today.AddDays(1)) : ReminderService.CustomerUrl(b.Code);
        var recipient = ownerReminder ? h.B.Owner.AccountId : h.Customer.AccountId;
        h.Sender.Fails = _ => true;
        h.Sender.BeforeSend = async _ =>
        {
            Assert.Null(h.Db.Database.CurrentTransaction);
            await using var other = h.H.NewContext();
            Assert.Equal(recipient, (await other.Notifications.SingleAsync(n => n.Url == url)).UserId);
        };
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync());
        Assert.Equal(0, ownerReminder ? await h.Reminders().SendOwnersAsync() : await h.Reminders().SendCustomersAsync());
        Assert.Single(await h.Db.Notifications.Where(n => n.Url == url).ToListAsync());
        Assert.Equal(4, h.Sender.Calls.Count); Assert.Equal(4, await h.Db.MessageLogs.CountAsync(m => m.Status == MessageStatus.Failed));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("khong-hop-le")]
    public async Task InvalidHomestayPhoneStillNotifiesOwnerWithoutCreatingMessageLog(string? phone)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync();
        (await h.Db.Homestays.SingleAsync()).ContactPhone = phone; await h.Db.SaveChangesAsync();
        Assert.Equal(0, await h.Reminders().SendOwnersAsync()); Assert.Equal(0, await h.Reminders().SendOwnersAsync());
        var notice = Assert.Single(await h.Db.Notifications.Where(n => n.Url == ReminderService.OwnerUrl(h.H.Homestay.OwnerId, h.Today.AddDays(1))).ToListAsync());
        Assert.Equal(h.B.Owner.AccountId, notice.UserId); Assert.Contains(b.Code, notice.Message);
        Assert.Empty(h.Db.MessageLogs); Assert.Empty(h.Sender.Calls);
    }

    [Theory]
    [InlineData(nameof(ReminderOptions.CustomerDaysBefore), -1, false)] [InlineData(nameof(ReminderOptions.CustomerDaysBefore), 366, false)]
    [InlineData(nameof(ReminderOptions.CustomerDaysBefore), 0, true)] [InlineData(nameof(ReminderOptions.CustomerDaysBefore), 365, true)]
    [InlineData(nameof(ReminderOptions.SendHour), -1, false)] [InlineData(nameof(ReminderOptions.SendHour), 24, false)]
    [InlineData(nameof(ReminderOptions.SendHour), 0, true)] [InlineData(nameof(ReminderOptions.SendHour), 23, false)]
    [InlineData(nameof(ReminderOptions.SendHour), 20, true)]
    [InlineData(nameof(ReminderOptions.QuietFromHour), 0, false)] [InlineData(nameof(ReminderOptions.QuietFromHour), 24, false)]
    [InlineData(nameof(ReminderOptions.QuietFromHour), 9, false)] [InlineData(nameof(ReminderOptions.QuietFromHour), 10, true)]
    public void ReminderOptionsValidateRanges(string property, int value, bool valid)
    {
        var options = new ReminderOptions(); typeof(ReminderOptions).GetProperty(property)!.SetValue(options, value);
        Assert.Equal(valid, new DataAnnotationValidateOptions<ReminderOptions>(Options.DefaultName).Validate(Options.DefaultName, options).Succeeded);
    }
}
