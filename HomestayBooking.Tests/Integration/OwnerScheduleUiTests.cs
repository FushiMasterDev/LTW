using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerScheduleUiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;
    public OwnerScheduleUiTests(TestWebApplicationFactory factory) => this.factory = factory;
    private int OwnHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == "+84912000001").Select(h => h.Id).First());
    private int OtherHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));
    [Fact]
    public async Task ApprovedOwner_CanOpenScreen_AnonymousAndCustomerAreBlocked()
    {
        const string screen = "Schedule";
        var owner = await factory.SignInByPhoneAsync("0912000001");
        var customer = await factory.SignInByPhoneAsync("0987000001");
        var path = "/Owner/" + screen;
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).StatusCode);
        Assert.StartsWith("/Account/Login", (await factory.CreateClient().GetAsync(path)).Headers.Location!.AbsolutePath);
        Assert.StartsWith("/Account/AccessDenied", (await customer.GetAsync(path)).Headers.Location!.AbsolutePath);
        var unapproved = await factory.SignInByPhoneAsync("0912000006");
        Assert.StartsWith("/Account/AccessDenied", (await unapproved.GetAsync(path)).Headers.Location!.AbsolutePath);
    }
    [Fact]
    public async Task CrossOwnerHomeFilter_IsRejected()
    {
        const string controller = "Schedule";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/Owner/{controller}?homestayId={OtherHome}")).StatusCode);
    }
    [Theory]
    [InlineData("today")]
    [InlineData("tomorrow")]
    [InlineData("week")]
    public async Task Schedule_PeriodsRender(string period)
    {
        var client = await factory.SignInByPhoneAsync("0912000001");
        var response = await client.GetAsync("/Owner/Schedule?period="+period);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Contains("Lịch nhận–trả",await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task NotificationAliases_OpenOwnerScreenAndKeepArrivalDate()
    {
        var client=await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/Owner/Homestays/{OwnHome}")).StatusCode);
        var response=await client.GetAsync("/Owner/Bookings?ownerId=999999&arrivals=2026-10-20");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.Contains("/Owner/Schedule",response.Headers.Location!.OriginalString);
        Assert.Contains("2026-10-20",response.Headers.Location.OriginalString);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/Owner/Schedule?period=week&date=9999-12-31")).StatusCode);
    }
}
