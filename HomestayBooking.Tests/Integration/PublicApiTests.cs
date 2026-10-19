using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

/// <summary>Web API RESTful (QUY_TRINH_BTL mục 8) trên pipeline thật: tìm kiếm, lịch trống, báo giá, tra cứu đơn, số thông báo chưa đọc.</summary>
public sealed class PublicApiTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private int RoomTypeId() => factory.WithDb(db => db.RoomTypes
        .Where(t => t.IsActive && t.Homestay.Status == HomestayStatus.Active && t.Rooms.Any(r => r.IsActive))
        .OrderBy(t => t.Id).Select(t => t.Id).First());

    private static DateOnly Today => VietnamTime.Today(TimeProvider.System);

    [Fact]
    public async Task SearchReturnsPagedHomestaysAndRejectsReversedPriceRange()
    {
        var client = factory.CreateClient();
        var page = await client.GetFromJsonAsync<JsonElement>("/api/homestays?guests=2&page=1", Json);
        Assert.True(page.GetProperty("totalCount").GetInt32() > 0);
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.False(string.IsNullOrEmpty(page.GetProperty("items")[0].GetProperty("name").GetString()));

        var bad = await client.GetAsync("/api/homestays?minPrice=900000&maxPrice=100000");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var problem = await bad.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task AvailabilityListsBlockedNightsAndRejectsBadRanges()
    {
        var client = factory.CreateClient();
        var id = RoomTypeId();
        var ok = await client.GetAsync($"/api/rooms/{id}/availability?from={Today:yyyy-MM-dd}&to={Today.AddDays(30):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var nights = await ok.Content.ReadFromJsonAsync<string[]>(Json);
        Assert.NotNull(nights);
        Assert.All(nights, n => Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", n));

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/rooms/{id}/availability?from={Today:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/rooms/{id}/availability?from={Today:yyyy-MM-dd}&to={Today.AddDays(93):yyyy-MM-dd}")).StatusCode);
    }

    [Fact]
    public async Task QuoteReturnsNightlyPricesAndDepositOrProblem()
    {
        var client = factory.CreateClient();
        var id = RoomTypeId();
        var checkIn = Today.AddDays(20);
        var quote = await client.GetFromJsonAsync<JsonElement>($"/api/rooms/{id}/quote?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkIn.AddDays(2):yyyy-MM-dd}", Json);
        var nights = quote.GetProperty("nights");
        Assert.Equal(2, nights.GetArrayLength());
        var total = nights.EnumerateArray().Sum(n => n.GetProperty("price").GetDecimal());
        Assert.Equal(total, quote.GetProperty("total").GetDecimal());
        Assert.True(quote.GetProperty("deposit").GetDecimal() > 0);
        Assert.Contains(nights[0].GetProperty("dayType").GetString(), new[] { "Weekday", "Weekend", "Holiday" });

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/rooms/{id}/quote?checkIn={Today.AddDays(-1):yyyy-MM-dd}&checkOut={Today.AddDays(1):yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/rooms/{id}/quote?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkIn:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/rooms/999999/quote?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkIn.AddDays(1):yyyy-MM-dd}")).StatusCode);
    }

    [Fact]
    public async Task BookingLookupNeedsMatchingPhoneAndHidesExistence()
    {
        var (code, phone) = factory.WithDb(db => db.Bookings.OrderBy(b => b.Id).Select(b => new { b.Code, b.ContactPhone }).AsEnumerable()
            .Select(b => (b.Code, b.ContactPhone)).First());
        var client = factory.CreateClient();
        var local = "0" + phone[3..];

        var found = await client.GetFromJsonAsync<JsonElement>($"/api/bookings/{code.ToLowerInvariant()}?phone={local}", Json);
        Assert.Equal(code, found.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(found.GetProperty("homestay").GetString()));
        Assert.True(Enum.TryParse<BookingStatus>(found.GetProperty("status").GetString(), out _));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/bookings/{code}?phone=0900000000")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/bookings/{code}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/bookings/FJ000000-000?phone={local}")).StatusCode);
    }

    [Fact]
    public async Task UnreadCountRequiresSignInAndMatchesAccount()
    {
        var anonymous = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var denied = await anonymous.GetAsync("/api/notifications/unread-count");
        Assert.True(denied.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect, $"Mong đợi 401/302, nhận {denied.StatusCode}");

        var client = await factory.SignInByPhoneAsync("0987000001");
        var expected = factory.WithDb(db => db.Notifications.Count(n => !n.IsRead
            && n.UserId == db.Users.Where(u => u.Person.PhoneNumber == "+84987000001").Select(u => u.Id).First()));
        var count = await client.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count", Json);
        Assert.Equal(expected, count.GetProperty("count").GetInt32());
    }
}
