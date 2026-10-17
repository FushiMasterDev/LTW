using System.Net;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Tests.Integration;

/// <summary>FR-03 trên pipeline thật: trang đặt phòng mở cho khách chưa đăng nhập, POST vẫn bắt antiforgery.</summary>
public sealed class AnonymousBookingPageTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private int RoomTypeId() => factory.WithDb(db => db.RoomTypes
        .Where(t => t.IsActive && t.Homestay.Status == HomestayStatus.Active && t.Rooms.Any(r => r.IsActive))
        .OrderBy(t => t.Id).Select(t => t.Id).First());

    [Fact]
    public async Task AnonymousGetCreateShowsFormInsteadOfLogin()
    {
        var response = await factory.CreateClient().GetAsync($"/Bookings/Create?roomTypeId={RoomTypeId()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"bookingForm\"", html); Assert.Contains("name=\"ContactPhone\"", html);
    }

    [Fact]
    public async Task AnonymousPostWithoutAntiforgeryTokenIsRejected()
    {
        var before = factory.WithDb(db => db.Bookings.Count());
        var response = await factory.CreateClient().PostAsync("/Bookings/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["RoomTypeId"] = RoomTypeId().ToString(), ["ContactName"] = "Khách", ["ContactPhone"] = "0977666009", ["Guests"] = "1",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, factory.WithDb(db => db.Bookings.Count()));
    }
}
