using System.Net;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Tests.Integration;

/// <summary>
/// Phân quyền khu Owner/Bookings qua middleware thật: cookie đăng nhập Identity, policy ApprovedOwner,
/// filter AutoValidateAntiforgeryToken. Đăng nhập bằng tài khoản seed và form đăng nhập thật.
/// </summary>
public sealed class OwnerBookingsAuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OwnerBookingsAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Đơn đã cọc của một Owner đã duyệt, kèm SĐT đăng nhập của Owner đó và một đơn đã cọc của Owner khác.</summary>
    private static (int BookingId, string OwnerPhone, int OtherOwnersBookingId) PickBookings(TestWebApplicationFactory factory) =>
        factory.WithDb(db =>
        {
            var target = db.Bookings
                .Where(b => b.Status == BookingStatus.Deposited
                            && b.Homestay.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)
                            && b.Homestay.Owner.Person.Account != null)
                .Select(b => new { b.Id, b.Homestay.OwnerId, b.Homestay.Owner.Person.PhoneNumber })
                .OrderBy(b => b.Id)
                .First();
            var other = db.Bookings
                .Where(b => b.DepositedAt != null && b.Homestay.OwnerId != target.OwnerId)
                .Select(b => b.Id)
                .First();
            return (target.Id, PhoneNumberNormalizer.ToNational(target.PhoneNumber), other);
        });

    private BookingStatus StatusOf(int id) => _factory.WithDb(db => db.Bookings.Where(b => b.Id == id).Select(b => b.Status).Single());

    private static void AssertRedirectsTo(HttpResponseMessage response, string path)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.StartsWith(path, location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString);
    }


    [Fact]
    public async Task Anonymous_GetList_RedirectsToLogin()
    {
        var response = await _factory.CreateClient().GetAsync("/Owner/Bookings");

        AssertRedirectsTo(response, "/Account/Login");
    }

    [Fact]
    public async Task Anonymous_PostConfirm_RedirectsToLogin_AndChangesNothing()
    {
        var (bookingId, _, _) = PickBookings(_factory);

        var response = await _factory.CreateClient().PostAsync($"/Owner/Bookings/Confirm/{bookingId}", new FormUrlEncodedContent([]));

        AssertRedirectsTo(response, "/Account/Login");
        Assert.Equal(BookingStatus.Deposited, StatusOf(bookingId));
    }

    [Theory]
    [InlineData("phone", "0987000001")] // khách hàng, không có role Owner
    [InlineData("phone", "0912000006")] // Owner có hồ sơ đang chờ duyệt (thiếu claim owner_approved)
    [InlineData("employee", "NV001")]   // nhân viên tư vấn
    [InlineData("employee", "QL001")]   // quản lý
    public async Task SignedInWithoutApprovedOwner_GetsAccessDenied(string kind, string user)
    {
        var client = kind == "phone" ? await _factory.SignInByPhoneAsync(user) : await _factory.SignInEmployeeAsync(user);

        AssertRedirectsTo(await client.GetAsync("/Owner/Bookings"), "/Account/AccessDenied");
        AssertRedirectsTo(await client.PostAsync("/Owner/Bookings/Confirm/1", new FormUrlEncodedContent([])), "/Account/AccessDenied");
    }

    [Fact]
    public async Task ApprovedOwner_SeesOwnBooking_ButNotOtherOwners()
    {
        var (bookingId, ownerPhone, otherBookingId) = PickBookings(_factory);
        var client = await _factory.SignInByPhoneAsync(ownerPhone);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Owner/Bookings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Owner/Bookings/Details/{bookingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Owner/Bookings/Details/{otherBookingId}")).StatusCode);
    }

    [Fact]
    public async Task ApprovedOwner_PostWithoutAntiforgeryToken_IsRejected()
    {
        var (bookingId, ownerPhone, _) = PickBookings(_factory);
        var client = await _factory.SignInByPhoneAsync(ownerPhone);

        var response = await client.PostAsync($"/Owner/Bookings/Confirm/{bookingId}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(BookingStatus.Deposited, StatusOf(bookingId));
    }

    [Fact]
    public async Task ApprovedOwner_PostOtherOwnersBooking_WithValidToken_IsNotFound()
    {
        var (bookingId, ownerPhone, otherBookingId) = PickBookings(_factory);
        var client = await _factory.SignInByPhoneAsync(ownerPhone);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Owner/Bookings/Details/{bookingId}");
        var before = StatusOf(otherBookingId);

        var response = await client.PostAsync($"/Owner/Bookings/Reject/{otherBookingId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["reason"] = "Thử truy cập chéo",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, StatusOf(otherBookingId));
    }

    [Fact]
    public async Task PostRejection_ShowsFriendlyErrorPageWithOriginalStatus()
    {
        // Re-execute trang lỗi giữ method POST: HomeController.StatusCodePage phải nhận POST (trước đây chỉ GET → 405)
        var (bookingId, ownerPhone, otherBookingId) = PickBookings(_factory);
        var client = await _factory.SignInByPhoneAsync(ownerPhone);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Owner/Bookings/Details/{bookingId}");

        var response = await client.PostAsync($"/Owner/Bookings/CheckIn/{otherBookingId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("class=\"status-code\"", html);
        Assert.Contains("Không tìm thấy trang", html);
    }

    [Fact]
    public async Task ApprovedOwner_PostConfirmWithToken_ConfirmsBooking()
    {
        // Factory riêng vì test này đổi dữ liệu; các test khác dùng chung factory của class
        using var factory = new TestWebApplicationFactory();
        var (bookingId, ownerPhone, _) = PickBookings(factory);
        var client = await factory.SignInByPhoneAsync(ownerPhone);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Owner/Bookings/Details/{bookingId}");

        var response = await client.PostAsync($"/Owner/Bookings/Confirm/{bookingId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        }));

        AssertRedirectsTo(response, $"/Owner/Bookings/Details/{bookingId}");
        Assert.Equal(BookingStatus.Confirmed, factory.WithDb(db => db.Bookings.Where(b => b.Id == bookingId).Select(b => b.Status).Single()));
    }
}
