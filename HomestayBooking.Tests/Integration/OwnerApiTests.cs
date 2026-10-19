using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

/// <summary>
/// Web API ghi của chủ homestay (QUY_TRINH_BTL mục 8): xác nhận/từ chối đơn, khóa/mở ngày. Cookie đăng nhập thật,
/// policy ApprovedOwner, token chống giả mạo qua header RequestVerificationToken, lọc theo chủ sở hữu.
/// </summary>
public sealed class OwnerApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static DateOnly Today => VietnamTime.Today(TimeProvider.System);

    private static (int BookingId, string OwnerPhone, int OtherOwnersBookingId) PickBookings(TestWebApplicationFactory f) => f.WithDb(db =>
    {
        var target = db.Bookings
            .Where(b => b.Status == BookingStatus.Deposited
                        && b.Homestay.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)
                        && b.Homestay.Owner.Person.Account != null)
            .Select(b => new { b.Id, b.Homestay.OwnerId, b.Homestay.Owner.Person.PhoneNumber })
            .OrderBy(b => b.Id).First();
        var other = db.Bookings.Where(b => b.DepositedAt != null && b.Homestay.OwnerId != target.OwnerId).Select(b => b.Id).First();
        return (target.Id, PhoneNumberNormalizer.ToNational(target.PhoneNumber), other);
    });

    private static (int RoomId, int OtherOwnersRoomId) PickRooms(TestWebApplicationFactory f, string ownerPhone) => f.WithDb(db =>
    {
        var e164 = PhoneNumberNormalizer.ToE164(ownerPhone);
        var mine = db.Rooms.Where(r => r.IsActive && r.RoomType.Homestay.Owner.Person.PhoneNumber == e164).OrderBy(r => r.Id).Select(r => r.Id).First();
        var other = db.Rooms.Where(r => r.RoomType.Homestay.Owner.Person.PhoneNumber != e164).OrderBy(r => r.Id).Select(r => r.Id).First();
        return (mine, other);
    });

    private static async Task<HttpRequestMessage> RequestAsync(HttpClient client, HttpMethod method, string url, object? body = null, bool withToken = true)
    {
        var request = new HttpRequestMessage(method, url);
        if (withToken) request.Headers.Add("RequestVerificationToken", await TestWebApplicationFactory.TokenFromAsync(client, "/Owner/Calendar"));
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        return request;
    }

    private static BookingStatus StatusOf(TestWebApplicationFactory f, int id) => f.WithDb(db => db.Bookings.Where(b => b.Id == id).Select(b => b.Status).Single());

    [Fact]
    public async Task OwnerConfirmsOwnDepositedBookingButNotOthersAndNotWithoutToken()
    {
        using var f = new TestWebApplicationFactory();
        var (bookingId, ownerPhone, otherId) = PickBookings(f);
        var client = await f.SignInByPhoneAsync(ownerPhone);

        var noToken = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{bookingId}/confirm", new { }, withToken: false));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        Assert.Equal(BookingStatus.Deposited, StatusOf(f, bookingId));

        var others = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{otherId}/confirm", new { }));
        Assert.Equal(HttpStatusCode.NotFound, others.StatusCode);

        var ok = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{bookingId}/confirm", new { }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Confirmed", body.GetProperty("status").GetString());
        Assert.Equal(BookingStatus.Confirmed, StatusOf(f, bookingId));

        var again = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{bookingId}/confirm", new { }));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task RejectNeedsReasonAndRefundsDeposit()
    {
        using var f = new TestWebApplicationFactory();
        var (bookingId, ownerPhone, _) = PickBookings(f);
        var client = await f.SignInByPhoneAsync(ownerPhone);

        var noReason = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{bookingId}/reject", new { reason = "  " }));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal(BookingStatus.Deposited, StatusOf(f, bookingId));

        var ok = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/bookings/{bookingId}/reject", new { reason = "Phòng đang sửa chữa" }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(BookingStatus.Rejected, StatusOf(f, bookingId));
    }

    [Fact]
    public async Task OwnerBlocksAndUnblocksOwnRoomWithConflictAndOwnershipChecks()
    {
        using var f = new TestWebApplicationFactory();
        var (_, ownerPhone, _) = PickBookings(f);
        var (roomId, otherRoomId) = PickRooms(f, ownerPhone);
        var client = await f.SignInByPhoneAsync(ownerPhone);
        var start = Today.AddDays(300);
        var range = new { start, end = start.AddDays(2), reason = "Maintenance", note = "Sơn lại phòng" };

        var created = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/rooms/{roomId}/blocks", range));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var block = await created.Content.ReadFromJsonAsync<JsonElement>(Json);
        var blockId = block.GetProperty("id").GetInt32();
        Assert.Equal("Maintenance", block.GetProperty("reason").GetString());
        Assert.Equal(2, f.WithDb(db => db.RoomNights.Count(n => n.RoomBlockId == blockId)));
        Assert.EndsWith($"/api/rooms/{roomId}/blocks/{blockId}", created.Headers.Location?.OriginalString);

        var conflict = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/rooms/{roomId}/blocks", range));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.True((await conflict.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("conflicts").GetArrayLength() > 0);

        var past = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/rooms/{roomId}/blocks",
            new { start = Today.AddDays(-3), end = Today.AddDays(-1) }));
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);

        var notMine = await client.SendAsync(await RequestAsync(client, HttpMethod.Post, $"/api/rooms/{otherRoomId}/blocks", range));
        Assert.Equal(HttpStatusCode.NotFound, notMine.StatusCode);
        var wrongRoom = await client.SendAsync(await RequestAsync(client, HttpMethod.Delete, $"/api/rooms/{otherRoomId}/blocks/{blockId}"));
        Assert.Equal(HttpStatusCode.NotFound, wrongRoom.StatusCode);

        var deleted = await client.SendAsync(await RequestAsync(client, HttpMethod.Delete, $"/api/rooms/{roomId}/blocks/{blockId}"));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        // Khóa hoàn toàn ở tương lai bị xóa hẳn (đêm khóa xóa theo cascade ở SQL Server)
        Assert.False(f.WithDb(db => db.RoomBlocks.Any(b => b.Id == blockId)));
    }

    [Fact]
    public async Task NonOwnersGet401Or403WithoutRedirect()
    {
        using var f = new TestWebApplicationFactory();
        var (bookingId, _, _) = PickBookings(f);
        var anonymous = f.CreateClient();
        var response = await anonymous.PostAsync($"/api/bookings/{bookingId}/confirm", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var customer = await f.SignInByPhoneAsync("0987000001");
        var forbidden = await customer.PostAsync($"/api/rooms/1/blocks", JsonContent.Create(new { start = Today.AddDays(10), end = Today.AddDays(11) }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(BookingStatus.Deposited, StatusOf(f, bookingId));
    }
}
