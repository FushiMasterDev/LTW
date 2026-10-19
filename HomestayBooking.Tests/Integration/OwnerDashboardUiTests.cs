using System.Net;
using System.Text.RegularExpressions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerDashboardUiTests
{
    [Fact]
    public async Task Dashboard_CountsOnlyOwnConfirmedArrivalsToday_AndLinksToOwnerScreens()
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInByPhoneAsync("0912000001");
        var today = VietnamTime.Today(TimeProvider.System);
        factory.WithDb(db =>
        {
            // Deliberately include foreign arrivals and own bookings in other states/dates.
            var own = db.Bookings.Where(b => b.Homestay.Owner.Person.PhoneNumber == "+84912000001").OrderBy(b => b.Id).ToList();
            Assert.True(own.Count >= 3);
            foreach (var booking in own)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.CheckIn = today.AddDays(1);
                booking.CheckOut = today.AddDays(3);
            }
            own[0].CheckIn = today;
            own[1].CheckIn = today;
            own[1].Status = BookingStatus.Deposited;
            own[2].CheckIn = today;
            own[2].Status = BookingStatus.CheckedIn;
            var other = db.Bookings.First(b => b.Homestay.Owner.Person.PhoneNumber != "+84912000001");
            other.Status = BookingStatus.Confirmed;
            other.CheckIn = today;
            other.CheckOut = today.AddDays(2);
            return db.SaveChanges();
        });

        var response = await client.GetAsync("/Owner/Dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches(@"Lượt nhận phòng hôm nay</a>\s*<span[^>]*>.*?</span>\s*<strong class=""fx-stat__value"">1</strong>", Regex.Replace(html, @"\r?\n", ""));
        foreach (var screen in new[] { "Homestays", "Calendar", "Schedule", "Settlements", "Reviews" })
            Assert.Contains($"href=\"/Owner/{screen}\"", html);
        Assert.Contains("href=\"/Owner/Bookings?status=Deposited\"", html);
        Assert.Contains("href=\"/Notifications\"", html);
        Assert.Empty(factory.Errors);
    }

    [Fact]
    public async Task UnapprovedOwner_CanReadRegistration_ButHasNoManagementShortcutsOrArrivalCount()
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInByPhoneAsync("0912000006");
        var response = await client.GetAsync("/Owner/Dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Trạng thái hồ sơ đăng ký", html);
        Assert.DoesNotContain("Lượt nhận phòng hôm nay", html);
        Assert.DoesNotContain("href=\"/Owner/Calendar\"", html);
        Assert.DoesNotContain("href=\"/Owner/Bookings?status=Deposited\"", html);
        Assert.Empty(factory.Errors);
    }

    [Fact]
    public async Task AnonymousAndCustomer_CannotReadOwnerDashboard()
    {
        using var factory = new TestWebApplicationFactory();
        var anonymous = await factory.CreateClient().GetAsync("/Owner/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);
        Assert.StartsWith("/Account/Login", anonymous.Headers.Location!.AbsolutePath);
        var customer = await factory.SignInByPhoneAsync("0987000001");
        var denied = await customer.GetAsync("/Owner/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location!.AbsolutePath);
    }
}
