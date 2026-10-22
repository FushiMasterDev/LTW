using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Tests.Services;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace HomestayBooking.Tests.Integration;

public sealed class DesktopOwnerFixture : IDisposable
{
    public TestWebApplicationFactory Root { get; } = new();
    public WebApplicationFactory<Program> Host { get; }
    public string FileRoot { get; } = Path.Combine(Path.GetTempPath(), "LTW-desktop-owner-" + Guid.NewGuid().ToString("N"));
    public DesktopOwnerFixture()
    {
        Host = Root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.PostConfigure<DesktopApiOptions>(o => o.LoginPermitLimit = 1000);
            s.RemoveAll<HomestayImageService>();
            s.AddScoped(sp => new HomestayImageService(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<HomestayAccessService>(),
                new FileEnvironment(FileRoot), sp.GetRequiredService<TimeProvider>()));
            s.RemoveAll<ReceiptFileStore>();
            s.AddSingleton(new ReceiptFileStore(new FileEnvironment(FileRoot)));
        }));
    }
    public T WithDb<T>(Func<ApplicationDbContext, T> action)
    {
        using var scope = Host.Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
    public void Dispose()
    {
        Host.Dispose(); Root.Dispose();
        if (Directory.Exists(FileRoot)) Directory.Delete(FileRoot, true);
    }
    private sealed class FileEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HomestayBooking.Web";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class DesktopOwnerApiTests(DesktopOwnerFixture fixture) : IClassFixture<DesktopOwnerFixture>
{
    private const string Prefix = "/api/desktop/owner";
    private const string Phone = "+84912000001";
    private static DateOnly Today => VietnamTime.Today(TimeProvider.System);
    private int Home => fixture.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == Phone).OrderBy(h => h.Id).Select(h => h.Id).First());
    private int OtherHome => fixture.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != Phone).OrderBy(h => h.Id).Select(h => h.Id).First());
    private int Type => fixture.WithDb(db => db.RoomTypes.Where(t => t.HomestayId == Home).OrderBy(t => t.Id).Select(t => t.Id).First());
    private int Room => fixture.WithDb(db => db.Rooms.Where(r => r.RoomTypeId == Type).OrderBy(r => r.Id).Select(r => r.Id).First());

    private async Task<(HttpClient Client, JsonElement Login)> Login(string phone = "0912000001")
    {
        var c = fixture.Host.CreateClient();
        var response = await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = phone, password = SeedData.DefaultPassword });
        var d = await Expect(response, HttpStatusCode.OK);
        Assert.DoesNotContain(response.Headers, h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
        c.DefaultRequestHeaders.Authorization = new("Bearer", d.GetProperty("accessToken").GetString());
        return (c, d);
    }
    private static async Task<JsonElement> Expect(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: expected {(int)status}, got {(int)response.StatusCode}: {body}");
        if (status == HttpStatusCode.NoContent) return default;
        var d = JsonSerializer.Deserialize<JsonElement>(body);
        if ((int)status >= 400)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal((int)status, d.GetProperty("status").GetInt32());
        }
        return d;
    }
    private static async Task<JsonElement> Send(HttpClient c, string method, string path, object? body, HttpStatusCode status)
    {
        using var req = new HttpRequestMessage(new HttpMethod(method), Prefix + path);
        if (body is not null) req.Content = JsonContent.Create(body);
        return await Expect(await c.SendAsync(req), status);
    }
    private void SetUser(Action<ApplicationUser> action) => fixture.WithDb(db =>
    {
        var u = db.Users.Single(u => u.Person.PhoneNumber == Phone); action(u); return db.SaveChanges();
    });
    private static MultipartFormDataContent Upload(byte[] bytes, string name, string mime, params (string Key, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes); file.Headers.ContentType = new(mime);
        form.Add(file, "file", name);
        foreach (var (key, value) in fields) form.Add(new StringContent(value), key);
        return form;
    }

    [Fact]
    public async Task PhoneLoginAndMeUseFactoryClaimsAndSharedTokenConfiguration()
    {
        var (c, login) = await Login("+84 912 000 001");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("accessToken").GetString());
        Assert.Contains(jwt.Claims, x => x.Type == ClaimTypes.Role && x.Value == "Owner");
        Assert.Contains(jwt.Claims, x => x.Type == AppClaimTypes.OwnerApproved && x.Value == "true");
        Assert.Contains(jwt.Claims, x => x.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(jwt.Claims, x => x.Type == "AspNet.Identity.SecurityStamp");
        var runtime = fixture.Host.Services.GetRequiredService<DesktopApiRuntime>();
        Assert.Equal(runtime.Options.Issuer, jwt.Issuer);
        Assert.Contains(runtime.Options.Audience, jwt.Audiences);
        Assert.InRange(jwt.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(runtime.Options.TokenMinutes - 1), TimeSpan.FromMinutes(runtime.Options.TokenMinutes + 1));
        Assert.EndsWith("Z", login.GetProperty("expiresAt").GetString());
        var me = await Send(c, "GET", "/auth/me", null, HttpStatusCode.OK);
        Assert.Equal(login.GetProperty("owner").GetProperty("id").GetInt32(), me.GetProperty("id").GetInt32());
        Assert.Equal(Phone, me.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task InvalidPhonesPasswordsCustomersEmployeesUnapprovedAndLockedUseOneErrorAndWebLockoutCounting()
    {
        var c = fixture.Host.CreateClient(); string? message = null;
        foreach (var (phone, password) in new[] { ("0900000000", "wrong"), ("0912000001", "wrong"), ("0987000001", SeedData.DefaultPassword),
            ("0912000006", SeedData.DefaultPassword), (fixture.WithDb(db => db.Employees.Select(e => e.Person.PhoneNumber).First()), SeedData.DefaultPassword) })
        {
            var d = await Expect(await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = phone, password }), HttpStatusCode.Unauthorized);
            message ??= d.GetProperty("detail").GetString(); Assert.Equal(message, d.GetProperty("detail").GetString());
        }
        Assert.True(fixture.WithDb(db => db.Users.Single(u => u.Person.PhoneNumber == Phone).AccessFailedCount) > 0);
        foreach (var state in new[] { AccountStatus.Locked, AccountStatus.Disabled })
        {
            SetUser(u => u.Status = state);
            var d = await Expect(await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = Phone, password = SeedData.DefaultPassword }), HttpStatusCode.Unauthorized);
            Assert.Equal(message, d.GetProperty("detail").GetString());
        }
        SetUser(u => { u.Status = AccountStatus.Active; u.AccessFailedCount = 0; u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5); });
        await Expect(await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = Phone, password = SeedData.DefaultPassword }), HttpStatusCode.Unauthorized);
        SetUser(u => { u.LockoutEnd = null; u.AccessFailedCount = 0; });
    }

    [Theory]
    [InlineData("status")]
    [InlineData("stamp")]
    [InlineData("lockout")]
    [InlineData("approval")]
    [InlineData("role")]
    public async Task RevocationReloadsStatusStampLockoutApprovalAndRoleEveryRequest(string change)
    {
        var (c, _) = await Login();
        if (change == "status") SetUser(u => u.Status = AccountStatus.Locked);
        if (change == "stamp") SetUser(u => u.SecurityStamp = Guid.NewGuid().ToString());
        if (change == "lockout") SetUser(u => u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5));
        var approved = fixture.WithDb(db => db.OwnerRegistrations.Where(r => r.Owner.Person.PhoneNumber == Phone && r.Status == OwnerRegistrationStatus.Approved).Select(r => r.Id).ToArray());
        if (change == "approval") fixture.WithDb(db => { foreach (var r in db.OwnerRegistrations.Where(r => approved.Contains(r.Id))) r.Status = OwnerRegistrationStatus.Rejected; return db.SaveChanges(); });
        if (change == "role")
        {
            using var scope = fixture.Host.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var u = await users.Users.SingleAsync(u => u.Person.PhoneNumber == Phone);
            Assert.True((await users.RemoveFromRoleAsync(u, "Owner")).Succeeded);
        }
        try { await Send(c, "GET", "/auth/me", null, HttpStatusCode.Unauthorized); }
        finally
        {
            SetUser(u => { u.Status = AccountStatus.Active; u.LockoutEnd = null; u.AccessFailedCount = 0; });
            fixture.WithDb(db => { foreach (var r in db.OwnerRegistrations.Where(r => approved.Contains(r.Id))) r.Status = OwnerRegistrationStatus.Approved; return db.SaveChanges(); });
            if (change == "role")
            {
                using var scope = fixture.Host.Services.CreateScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var u = await users.Users.SingleAsync(u => u.Person.PhoneNumber == Phone);
                Assert.True((await users.AddToRoleAsync(u, "Owner")).Succeeded);
            }
        }
    }

    [Fact]
    public async Task BearerOnlyAndCrossRoleTokensAre403WhileCookieOnlyIs401()
    {
        var (owner, _) = await Login();
        foreach (var path in new[] { "/auth/me", "/bookings", "/areas", "/receipts", "/reports/management" })
            await Expect(await owner.GetAsync("/api/desktop" + path), HttpStatusCode.Forbidden);
        var staff = fixture.Host.CreateClient();
        var token = await Expect(await staff.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "QL001", password = SeedData.DefaultPassword }), HttpStatusCode.OK);
        staff.DefaultRequestHeaders.Authorization = new("Bearer", token.GetProperty("accessToken").GetString());
        foreach (var path in new[] { "/auth/me", "/dashboard", "/bookings", "/homestays", "/reviews", "/settlements", "/calendar", "/schedule" })
            await Send(staff, "GET", path, null, HttpStatusCode.Forbidden);
        var cookie = await fixture.Root.SignInByPhoneAsync("0912000001");
        await Send(cookie, "GET", "/dashboard", null, HttpStatusCode.Unauthorized);
        await Send(fixture.Host.CreateClient(), "GET", "/dashboard", null, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginLimiterAndInvalidProductionConfigurationAlsoCoverOwner()
    {
        using var limited = fixture.Host.WithWebHostBuilder(b => b.ConfigureServices(s => s.PostConfigure<DesktopApiOptions>(o => o.LoginPermitLimit = 1)));
        var c = limited.CreateClient();
        await Expect(await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = "0900000000", password = "wrong" }), HttpStatusCode.Unauthorized);
        var response = await c.PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = "0900000000", password = "wrong" });
        await Expect(response, HttpStatusCode.TooManyRequests); Assert.NotNull(response.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Account/Login")).StatusCode);
        using var production = fixture.Host.WithWebHostBuilder(b => { b.UseEnvironment("Production"); b.ConfigureServices(s => s.PostConfigure<DesktopApiOptions>(o => o.SigningKey = null)); });
        await Expect(await production.CreateClient().PostAsJsonAsync(Prefix + "/auth/login", new { phoneNumber = Phone, password = "wrong" }), HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ReadsMatchWebScopePaginationDatesEnumsAndFilters()
    {
        var (c, _) = await Login();
        var d = await Send(c, "GET", "/dashboard", null, HttpStatusCode.OK);
        Assert.Equal("Approved", d.GetProperty("registrationStatus").GetString());
        Assert.All(d.GetProperty("homestays").GetProperty("items").EnumerateArray(), h => Assert.NotEqual(OtherHome, h.GetProperty("id").GetInt32()));
        var homes = await Send(c, "GET", "/homestays?page=9999", null, HttpStatusCode.OK);
        Assert.Equal(homes.GetProperty("totalPages").GetInt32(), homes.GetProperty("page").GetInt32());
        Assert.Equal(0, (await Send(c, "GET", "/homestays?q=NO_MATCH_482039", null, HttpStatusCode.OK)).GetProperty("totalCount").GetInt32());
        var editor = await Send(c, "GET", $"/homestays/{Home}", null, HttpStatusCode.OK);
        Assert.Equal(JsonValueKind.String, editor.GetProperty("status").ValueKind);
        Assert.NotEmpty(editor.GetProperty("roomTypes").EnumerateArray());
        Assert.NotEmpty((await Send(c, "GET", "/homestays/areas", null, HttpStatusCode.OK)).GetProperty("items").EnumerateArray());
        var rows = await Send(c, "GET", "/bookings?status=Deposited", null, HttpStatusCode.OK);
        Assert.All(rows.GetProperty("items").EnumerateArray(), b => Assert.Equal("Deposited", b.GetProperty("status").GetString()));
        var id = fixture.WithDb(db => db.Bookings.Where(b => b.Homestay.Owner.Person.PhoneNumber == Phone && b.DepositedAt != null).Select(b => b.Id).First());
        var detail = await Send(c, "GET", $"/bookings/{id}", null, HttpStatusCode.OK);
        Assert.True(DateOnly.TryParseExact(detail.GetProperty("booking").GetProperty("checkIn").GetString(), "yyyy-MM-dd", out _));
        var code = detail.GetProperty("booking").GetProperty("code").GetString();
        Assert.True((await Send(c, "GET", "/bookings?q=" + code, null, HttpStatusCode.OK)).GetProperty("totalCount").GetInt32() >= 1);
        await Send(c, "GET", "/bookings?status=999", null, HttpStatusCode.BadRequest);
        await Send(c, "GET", "/bookings?page=bad", null, HttpStatusCode.BadRequest);
        await Send(c, "GET", "/schedule?period=bad", null, HttpStatusCode.BadRequest);
        await Send(c, "GET", "/schedule?period=week&date=9999-12-31", null, HttpStatusCode.BadRequest);
        foreach (var period in new[] { "today", "tomorrow", "week" }) await Send(c, "GET", "/schedule?period=" + period, null, HttpStatusCode.OK);
        var cal = await Send(c, "GET", $"/calendar?homestayId={Home}&month={Today:yyyy-MM}", null, HttpStatusCode.OK);
        Assert.All(cal.GetProperty("nights").EnumerateArray(), n => Assert.Equal(JsonValueKind.String, n.GetProperty("state").ValueKind));
        await Send(c, "GET", "/calendar?roomId=2147483647", null, HttpStatusCode.NotFound);
        await Send(c, "GET", "/settlements?status=999", null, HttpStatusCode.BadRequest);
        await Send(c, "GET", "/settlements", null, HttpStatusCode.OK);
        await Send(c, "GET", "/reviews", null, HttpStatusCode.OK);
    }

    [Fact]
    public async Task EveryResourceFamilyHidesForeignAndMissingIdsIncludingNestedResources()
    {
        var (c, _) = await Login();
        var foreignImage = fixture.WithDb(db =>
        {
            var image = new HomestayImage { HomestayId = OtherHome, Url = "/images/foreign-owner-proof.png", SortOrder = 0 };
            db.HomestayImages.Add(image); db.SaveChanges(); return image.Id;
        });
        var foreign = fixture.WithDb(db => new
        {
            Booking = db.Bookings.Where(b => b.Homestay.Owner.Person.PhoneNumber != Phone && b.DepositedAt != null).Select(b => b.Id).FirstOrDefault(),
            Type = db.RoomTypes.Where(t => t.HomestayId == OtherHome).Select(t => t.Id).FirstOrDefault(),
            Room = db.Rooms.Where(r => r.RoomType.HomestayId == OtherHome).Select(r => r.Id).FirstOrDefault(),
            Image = foreignImage,
            Review = db.Reviews.Where(r => r.Homestay.Owner.Person.PhoneNumber != Phone).Select(r => r.Id).FirstOrDefault(),
            Settlement = db.Settlements.Where(s => s.Homestay.Owner.Person.PhoneNumber != Phone && s.Status != SettlementStatus.Draft).Select(s => s.Id).FirstOrDefault(),
            Document = db.VerificationDocuments.Where(d => d.Homestay != null && d.Homestay.Owner.Person.PhoneNumber != Phone).Select(d => d.Id).FirstOrDefault(),
        });
        Assert.True(new[] { foreign.Booking, foreign.Type, foreign.Room, foreign.Image, foreign.Review, foreign.Settlement, foreign.Document }.All(id => id > 0), JsonSerializer.Serialize(foreign));
        foreach (var home in new[] { OtherHome, int.MaxValue })
        {
            await Send(c, "GET", $"/homestays/{home}", null, HttpStatusCode.NotFound);
            await Send(c, "GET", $"/schedule?homestayId={home}", null, HttpStatusCode.NotFound);
            await Send(c, "GET", $"/calendar?homestayId={home}", null, HttpStatusCode.NotFound);
            await Send(c, "GET", $"/reviews?homestayId={home}", null, HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{home}/content", new { name = "IDOR", contactPhone = "0912000001", checkinTime = "14:00", checkoutTime = "12:00" }, HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{home}/location", new { address = "IDOR", areaId = 1 }, HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{home}/amenities", new { amenityIds = Array.Empty<int>() }, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/homestays/{home}/room-types", TypeBody("IDOR"), HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{home}/policies", PolicyBody(), HttpStatusCode.NotFound);
            await Send(c, "DELETE", $"/homestays/{home}/policies/All", null, HttpStatusCode.NotFound);
            foreach (var uploadPath in new[] { "images", "documents" })
                await Expect(await c.PostAsync(Prefix + $"/homestays/{home}/{uploadPath}", Upload(TestFiles.Png, "proof.png", "image/png", ("type", "PropertyPhoto"))), HttpStatusCode.NotFound);
        }
        foreach (var id in new[] { foreign.Booking, int.MaxValue })
        {
            await Send(c, "GET", $"/bookings/{id}", null, HttpStatusCode.NotFound);
            foreach (var action in new[] { "confirm", "reject", "check-in", "complete", "no-show" })
                await Send(c, "POST", $"/bookings/{id}/{action}", new { reason = "IDOR" }, HttpStatusCode.NotFound);
        }
        foreach (var type in new[] { foreign.Type, int.MaxValue })
        {
            await Send(c, "PUT", $"/homestays/{Home}/room-types/{type}", TypeBody("IDOR"), HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{Home}/room-types/{type}/active", new { active = true }, HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{Home}/room-types/{type}/prices", new { dayType = "Weekday", effectiveFrom = Today.AddDays(1), price = 500000 }, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/homestays/{Home}/room-types/{type}/rooms", new { code = "IDOR" }, HttpStatusCode.NotFound);
        }
        foreach (var room in new[] { foreign.Room, int.MaxValue })
        {
            await Send(c, "PUT", $"/homestays/{Home}/rooms/{room}", new { code = "IDOR" }, HttpStatusCode.NotFound);
            await Send(c, "PUT", $"/homestays/{Home}/rooms/{room}/active", new { active = true }, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/homestays/{Home}/rooms/{room}/blocks", new { start = Today.AddDays(100), end = Today.AddDays(101) }, HttpStatusCode.NotFound);
            await Send(c, "DELETE", $"/homestays/{Home}/rooms/{room}/blocks/1", null, HttpStatusCode.NotFound);
        }
        foreach (var id in new[] { foreign.Image, int.MaxValue })
        {
            await Send(c, "PUT", $"/homestays/{Home}/images/{id}", new { sortOrder = 0 }, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/homestays/{Home}/images/{id}/cover", null, HttpStatusCode.NotFound);
            await Send(c, "DELETE", $"/homestays/{Home}/images/{id}", null, HttpStatusCode.NotFound);
        }
        foreach (var id in new[] { foreign.Document, int.MaxValue })
        {
            await Send(c, "GET", $"/documents/{id}/file", null, HttpStatusCode.NotFound);
            await Send(c, "DELETE", $"/homestays/{Home}/documents/{id}", null, HttpStatusCode.NotFound);
        }
        foreach (var id in new[] { foreign.Review, int.MaxValue })
            await Send(c, "POST", $"/reviews/{id}/reply", new { reply = "IDOR" }, HttpStatusCode.NotFound);
        foreach (var id in new[] { foreign.Settlement, int.MaxValue })
        {
            await Send(c, "GET", $"/settlements/{id}", null, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/settlements/{id}/confirm", null, HttpStatusCode.NotFound);
            await Send(c, "POST", $"/settlements/{id}/discrepancy", new { note = "IDOR" }, HttpStatusCode.NotFound);
        }
        var ownDeposited = fixture.WithDb(db => db.Bookings.Where(b => b.Homestay.Owner.Person.PhoneNumber == Phone && b.Status == BookingStatus.Deposited).Select(b => b.Id).First());
        await Send(c, "POST", $"/bookings/{ownDeposited}/confirm", new { roomId = foreign.Room }, HttpStatusCode.NotFound);
        Assert.Equal(BookingStatus.Deposited, fixture.WithDb(db => db.Bookings.Single(b => b.Id == ownDeposited).Status));
        Assert.True(fixture.WithDb(db => db.HomestayImages.Any(i => i.Id == foreignImage && i.HomestayId == OtherHome)));
        var ownUnpaid = fixture.WithDb(db => db.Bookings.Where(b => b.Homestay.Owner.Person.PhoneNumber == Phone && b.DepositedAt == null).Select(b => b.Id).First());
        await Send(c, "GET", $"/bookings/{ownUnpaid}", null, HttpStatusCode.NotFound);
    }

    private object TypeBody(string name = "Desktop type") => new { name, categoryId = fixture.WithDb(db => db.RoomCategories.Select(c => c.Id).First()),
        maxGuests = 3, areaM2 = 30, description = "Desktop", weekdayPrice = 500000, weekendPrice = 600000, holidayPrice = 700000 };
    private static object PolicyBody() => new { scope = "All", name = "Desktop cancellation", rules = new[] { new { minDaysBefore = 0, refundPercent = 0 }, new { minDaysBefore = 7, refundPercent = 100 } } };

    [Fact]
    public async Task ContentLocationAmenitiesDelegateSanitizationValidationAndReviewBehavior()
    {
        var (c, _) = await Login();
        var id = Home; var before = fixture.WithDb(db => db.HomestayAmenities.Count(a => a.HomestayId == id));
        await Send(c, "PUT", $"/homestays/{id}/content", new { name = " Desktop name ", descriptionHtml = "<script>alert(1)</script><p>Safe</p>",
            checkinGuideHtml = "<b>Guide</b>", contactPhone = "0912000001", checkinTime = "14:00", checkoutTime = "12:00" }, HttpStatusCode.OK);
        Assert.Equal("Desktop name", fixture.WithDb(db => db.Homestays.Single(h => h.Id == id).Name));
        Assert.DoesNotContain("<script>", fixture.WithDb(db => db.Homestays.Single(h => h.Id == id).DescriptionHtml)!);
        Assert.Equal(before, fixture.WithDb(db => db.HomestayAmenities.Count(a => a.HomestayId == id)));
        await Send(c, "PUT", $"/homestays/{id}/content", new { name = "Invalid", contactPhone = "bad", checkinTime = "14:00", checkoutTime = "12:00" }, HttpStatusCode.BadRequest);
        await Send(c, "PUT", $"/homestays/{id}/amenities", new { amenityIds = new[] { int.MaxValue } }, HttpStatusCode.BadRequest);
        var amenity = fixture.WithDb(db => db.Amenities.Select(a => a.Id).First());
        await Send(c, "PUT", $"/homestays/{id}/amenities", new { amenityIds = new[] { amenity } }, HttpStatusCode.OK);
        Assert.Equal(new[] { amenity }, fixture.WithDb(db => db.HomestayAmenities.Where(a => a.HomestayId == id).Select(a => a.AmenityId).ToArray()));
        await Send(c, "PUT", $"/homestays/{id}/location", new { address = "Invalid", areaId = int.MaxValue }, HttpStatusCode.BadRequest);
        fixture.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.Active; return db.SaveChanges(); });
        var saved = await Send(c, "PUT", $"/homestays/{id}/location", new { address = "Desktop changed location", areaId = fixture.WithDb(db => db.Homestays.Single(h => h.Id == id).AreaId), latitude = 11, longitude = 108 }, HttpStatusCode.OK);
        Assert.Equal("PendingReview", saved.GetProperty("status").GetString());
        Assert.Equal(HomestayStatus.PendingReview, fixture.WithDb(db => db.Homestays.Single(h => h.Id == id).Status));
    }

    [Fact]
    public async Task RoomsTypesPricesAndActiveWritesHaveSuccessAndValidationFailures()
    {
        var (c, _) = await Login(); var id = Home;
        var type = (await Send(c, "POST", $"/homestays/{id}/room-types", TypeBody("Desktop " + Guid.NewGuid().ToString("N")), HttpStatusCode.Created)).GetProperty("id").GetInt32();
        await Send(c, "POST", $"/homestays/{id}/room-types", new { name = "Invalid", maxGuests = 0 }, HttpStatusCode.BadRequest);
        var body = TypeBody("Edited " + type);
        await Send(c, "PUT", $"/homestays/{id}/room-types/{type}", body, HttpStatusCode.OK);
        await Send(c, "PUT", $"/homestays/{id}/room-types/{type}", new { name = "bad", categoryId = int.MaxValue, maxGuests = 2 }, HttpStatusCode.BadRequest);
        var room = (await Send(c, "POST", $"/homestays/{id}/room-types/{type}/rooms", new { code = "Desktop-01" }, HttpStatusCode.Created)).GetProperty("id").GetInt32();
        await Send(c, "POST", $"/homestays/{id}/room-types/{type}/rooms", new { code = "Desktop-01" }, HttpStatusCode.BadRequest);
        await Send(c, "PUT", $"/homestays/{id}/rooms/{room}", new { code = "Desktop-02" }, HttpStatusCode.OK);
        await Send(c, "PUT", $"/homestays/{id}/rooms/{room}", new { code = "" }, HttpStatusCode.BadRequest);
        await Send(c, "PUT", $"/homestays/{id}/room-types/{type}/prices", new { dayType = "Holiday", effectiveFrom = Today.AddDays(15), price = 765000 }, HttpStatusCode.OK);
        Assert.Equal(765000, fixture.WithDb(db => db.RoomTypePrices.Single(p => p.RoomTypeId == type && p.EffectiveFrom == Today.AddDays(15)).Price));
        await Send(c, "PUT", $"/homestays/{id}/room-types/{type}/prices", new { dayType = "Holiday", effectiveFrom = Today.AddDays(-1), price = 765000 }, HttpStatusCode.BadRequest);
        foreach (var (path, entityId) in new[] { ("rooms", room), ("room-types", type) })
        {
            await Send(c, "PUT", $"/homestays/{id}/{path}/{entityId}/active", new { active = false }, HttpStatusCode.OK);
            await Send(c, "PUT", $"/homestays/{id}/{path}/{entityId}/active", new { active = true }, HttpStatusCode.OK);
            await Send(c, "PUT", $"/homestays/{id}/{path}/{entityId}/active", new { active = "bad" }, HttpStatusCode.BadRequest);
        }
        Assert.True(fixture.WithDb(db => db.Rooms.Single(r => r.Id == room).IsActive));
        Assert.True(fixture.WithDb(db => db.RoomTypes.Single(t => t.Id == type).IsActive));
    }

    [Theory]
    [InlineData("confirm", "Confirmed")]
    [InlineData("reject", "Rejected")]
    [InlineData("check-in", "CheckedIn")]
    [InlineData("complete", "Completed")]
    [InlineData("no-show", "NoShow")]
    public async Task EveryBookingWriteUsesServiceAndRejectsInvalidTransitions(string action, string expected)
    {
        var (c, _) = await Login();
        var id = fixture.WithDb(db =>
        {
            var source = db.Bookings.First(b => b.Homestay.Owner.Person.PhoneNumber == Phone && b.DepositedAt != null);
            var b = new Booking { Code = "DESK-" + Guid.NewGuid().ToString("N")[..12], CustomerId = source.CustomerId, HomestayId = source.HomestayId,
                RoomTypeId = source.RoomTypeId, RoomId = source.RoomId, CancellationPolicyId = source.CancellationPolicyId,
                ContactName = "Desktop guest", ContactPhone = source.ContactPhone, CheckIn = Today.AddDays(-1), CheckOut = Today.AddDays(1),
                Status = action is "confirm" or "reject" ? BookingStatus.Deposited : action == "complete" ? BookingStatus.CheckedIn : BookingStatus.Confirmed,
                DepositedAt = DateTime.UtcNow.AddMinutes(-10), ConfirmDeadlineAt = DateTime.UtcNow.AddMinutes(20), TotalAmount = 1000000, DepositAmount = 300000, Guests = 2 };
            db.Bookings.Add(b); db.SaveChanges(); return b.Id;
        });
        var result = await Send(c, "POST", $"/bookings/{id}/{action}", new { reason = "Desktop reason" }, HttpStatusCode.OK);
        Assert.Equal(expected, result.GetProperty("status").GetString());
        Assert.Equal(expected, fixture.WithDb(db => db.Bookings.Single(b => b.Id == id).Status.ToString()));
        Assert.True(fixture.WithDb(db => db.BookingStatusHistories.Any(h => h.BookingId == id && h.ActorType == ActorType.Owner)));
        await Send(c, "POST", $"/bookings/{id}/{action}", new { reason = "Desktop reason" }, HttpStatusCode.BadRequest);
        if (action == "reject")
        {
            Assert.Equal(300000, fixture.WithDb(db => db.Bookings.Single(b => b.Id == id).RefundAmount));
            await Send(c, "POST", $"/bookings/{id}/reject", new { reason = "" }, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task BlocksDetectConflictsAndUnlockOnlyTheActualOwnedRoom()
    {
        var (c, _) = await Login(); var home = Home; var room = Room;
        var path = $"/homestays/{home}/rooms/{room}/blocks";
        var range = new { start = Today.AddDays(600), end = Today.AddDays(602), reason = "Maintenance", note = "Painting" };
        var block = await Send(c, "POST", path, range, HttpStatusCode.Created);
        var id = block.GetProperty("id").GetInt32();
        Assert.Equal("Maintenance", block.GetProperty("reason").GetString());
        Assert.Equal(2, fixture.WithDb(db => db.RoomNights.Count(n => n.RoomBlockId == id)));
        await Send(c, "POST", path, range, HttpStatusCode.Conflict);
        await Send(c, "POST", path, new { start = Today.AddDays(-2), end = Today.AddDays(-1) }, HttpStatusCode.BadRequest);
        var otherRoom = fixture.WithDb(db => db.Rooms.Where(r => r.RoomType.HomestayId == OtherHome).Select(r => r.Id).First());
        await Send(c, "DELETE", $"/homestays/{home}/rooms/{otherRoom}/blocks/{id}", null, HttpStatusCode.NotFound);
        var foreignBlock = fixture.WithDb(db => { var b = new RoomBlock { RoomId = otherRoom, StartDate = Today.AddDays(603), EndDate = Today.AddDays(604), CreatedByAccountId = db.Users.First().Id }; db.RoomBlocks.Add(b); db.SaveChanges(); return b.Id; });
        await Send(c, "DELETE", path + "/" + foreignBlock, null, HttpStatusCode.NotFound);
        await Send(c, "DELETE", path + "/" + id, null, HttpStatusCode.NoContent);
        await Send(c, "DELETE", path + "/" + id, null, HttpStatusCode.NotFound);
        Assert.False(fixture.WithDb(db => db.RoomBlocks.Any(b => b.Id == id)));
    }

    [Fact]
    public async Task ImagesUploadUpdateCoverDeleteAndPoliciesShareWebValidators()
    {
        var (c, _) = await Login(); var id = Home; var path = Prefix + $"/homestays/{id}/images";
        var image = await Expect(await c.PostAsync(path, Upload(TestFiles.Png, "valid.png", "image/png", ("sortOrder", "2"))), HttpStatusCode.Created);
        var imageId = image.GetProperty("id").GetInt32();
        Assert.StartsWith("/uploads/homestays/", image.GetProperty("url").GetString());
        Assert.True(File.Exists(Path.Combine(fixture.FileRoot, "wwwroot", image.GetProperty("url").GetString()!.TrimStart('/'))));
        await Send(c, "PUT", $"/homestays/{id}/images/{imageId}", new { typeId = Type, sortOrder = 3 }, HttpStatusCode.OK);
        await Send(c, "PUT", $"/homestays/{id}/images/{imageId}", new { sortOrder = -1 }, HttpStatusCode.BadRequest);
        var foreignType = fixture.WithDb(db => db.RoomTypes.Where(t => t.HomestayId == OtherHome).Select(t => t.Id).First());
        await Send(c, "PUT", $"/homestays/{id}/images/{imageId}", new { typeId = foreignType, sortOrder = 0 }, HttpStatusCode.NotFound);
        await Expect(await c.PostAsync(path, Upload(TestFiles.Png, "proof.png", "image/png", ("typeId", foreignType.ToString()))), HttpStatusCode.NotFound);
        await Send(c, "POST", $"/homestays/{id}/images/{imageId}/cover", null, HttpStatusCode.OK);
        Assert.True(fixture.WithDb(db => db.HomestayImages.Single(i => i.Id == imageId).IsCover));
        Assert.Equal(1, fixture.WithDb(db => db.HomestayImages.Count(i => i.HomestayId == id && i.IsCover)));
        fixture.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.Active; return db.SaveChanges(); });
        await Send(c, "DELETE", $"/homestays/{id}/images/{imageId}", null, HttpStatusCode.BadRequest);
        fixture.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.PendingReview; return db.SaveChanges(); });
        await Send(c, "DELETE", $"/homestays/{id}/images/{imageId}", null, HttpStatusCode.NoContent);
        Assert.False(fixture.WithDb(db => db.HomestayImages.Any(i => i.Id == imageId)));
        await Send(c, "PUT", $"/homestays/{id}/policies", PolicyBody(), HttpStatusCode.OK);
        Assert.True(fixture.WithDb(db => db.CancellationPolicies.Any(p => p.HomestayId == id && p.IsActive && p.Name == "Desktop cancellation")));
        await Send(c, "PUT", $"/homestays/{id}/policies", new { scope = "All", name = "Invalid", rules = new[] { new { minDaysBefore = 7, refundPercent = 120 } } }, HttpStatusCode.BadRequest);
        await Send(c, "DELETE", $"/homestays/{id}/policies/999", null, HttpStatusCode.BadRequest);
        await Send(c, "DELETE", $"/homestays/{id}/policies/All", null, HttpStatusCode.NoContent);
        Assert.False(fixture.WithDb(db => db.CancellationPolicies.Any(p => p.HomestayId == id && p.IsActive && p.Scope == PolicyScope.All)));
    }

    [Theory]
    [InlineData("images", "wrong-type")]
    [InlineData("images", "oversize")]
    [InlineData("images", "bad-mime")]
    [InlineData("images", "missing")]
    [InlineData("documents", "wrong-type")]
    [InlineData("documents", "oversize")]
    [InlineData("documents", "bad-mime")]
    [InlineData("documents", "missing")]
    public async Task UploadsRejectInvalidTypeSizeMimeAndMissingFiles(string kind, string fault)
    {
        var (c, _) = await Login();
        var bytes = fault == "oversize" ? new byte[ReceiptFileStore.MaxBytes + 1] : fault == "wrong-type" ? "invalid"u8.ToArray() : TestFiles.Png;
        using var form = fault == "missing" ? new MultipartFormDataContent() : Upload(bytes, fault == "wrong-type" ? "bad.exe" : "image.png", fault == "bad-mime" ? "text/plain" : "image/png", ("type", "Other"));
        await Expect(await c.PostAsync(Prefix + $"/homestays/{Home}/{kind}", form), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PrivateDocumentsUploadDownloadAndDeletionProtectOwnerAndNeverExposeStoragePaths()
    {
        var (c, _) = await Login(); var id = Home;
        var d = await Expect(await c.PostAsync(Prefix + $"/homestays/{id}/documents", Upload(TestFiles.Pdf, "proof.pdf", "application/pdf", ("type", "BusinessLicense"), ("expiry", Today.AddDays(90).ToString("yyyy-MM-dd")))), HttpStatusCode.Created);
        var documentId = d.GetProperty("id").GetInt32();
        var fileUrl = d.GetProperty("fileUrl").GetString()!;
        Assert.Equal(Prefix + $"/documents/{documentId}/file", fileUrl);
        Assert.Equal("BusinessLicense", d.GetProperty("type").GetString());
        Assert.EndsWith("Z", d.GetProperty("createdAt").GetString());
        Assert.DoesNotContain("App_Data", d.GetRawText());
        var response = await c.GetAsync(fileUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestFiles.Pdf, await response.Content.ReadAsByteArrayAsync());
        var otherPhone = fixture.WithDb(db => db.Owners.Where(o => o.Person.PhoneNumber != Phone && o.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)
            && o.Person.Account != null && o.Person.Account.Status == AccountStatus.Active).Select(o => o.Person.PhoneNumber).First());
        var (other, _) = await Login(otherPhone);
        await Expect(await other.GetAsync(fileUrl), HttpStatusCode.NotFound);
        await Expect(await fixture.Host.CreateClient().GetAsync(fileUrl), HttpStatusCode.Unauthorized);
        var stored = fixture.WithDb(db => db.VerificationDocuments.Single(x => x.Id == documentId).FileUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Host.CreateClient().GetAsync("/App_Data/receipts/" + stored)).StatusCode);
        fixture.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.Active; return db.SaveChanges(); });
        await Send(c, "DELETE", $"/homestays/{id}/documents/{documentId}", null, HttpStatusCode.BadRequest);
        fixture.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.PendingReview; return db.SaveChanges(); });
        await Send(c, "DELETE", $"/homestays/{id}/documents/{documentId}", null, HttpStatusCode.NoContent);
        await Expect(await c.GetAsync(fileUrl), HttpStatusCode.NotFound);
        var missing = fixture.WithDb(db => { var doc = new VerificationDocument { HomestayId = id, DocumentType = DocumentType.Other, FileUrl = "not-present.pdf" }; db.VerificationDocuments.Add(doc); db.SaveChanges(); return doc.Id; });
        await Send(c, "GET", $"/documents/{missing}/file", null, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReviewsAndSettlementsDelegateWritesAndKeepDraftsHidden()
    {
        var (c, _) = await Login();
        var review = fixture.WithDb(db => db.Reviews.Where(r => r.Homestay.Owner.Person.PhoneNumber == Phone && !r.IsHidden).Select(r => new { r.Id, r.HomestayId }).First());
        await Send(c, "POST", $"/reviews/{review.Id}/reply", new { reply = "  Thanks <b>guest</b>  " }, HttpStatusCode.OK);
        Assert.Equal("Thanks <b>guest</b>", fixture.WithDb(db => db.Reviews.Single(r => r.Id == review.Id).OwnerReply));
        await Send(c, "POST", $"/reviews/{review.Id}/reply", new { reply = " " }, HttpStatusCode.BadRequest);
        var page = await Send(c, "GET", $"/reviews?homestayId={review.HomestayId}", null, HttpStatusCode.OK);
        Assert.Contains(page.GetProperty("reviews").GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetInt32() == review.Id);
        var settlement = fixture.WithDb(db => { var s = db.Settlements.First(s => s.Homestay.Owner.Person.PhoneNumber == Phone); s.Status = SettlementStatus.Sent; db.SaveChanges(); return s.Id; });
        var detail = await Send(c, "GET", $"/settlements/{settlement}", null, HttpStatusCode.OK);
        Assert.Equal("Sent", detail.GetProperty("settlement").GetProperty("status").GetString());
        await Send(c, "POST", $"/settlements/{settlement}/discrepancy", new { note = "Check totals" }, HttpStatusCode.OK);
        Assert.True(fixture.WithDb(db => db.AuditLogs.Any(a => a.TableName == "Settlements" && a.RecordId == settlement.ToString() && a.NewValues != null && a.NewValues.Contains("OwnerDiscrepancy"))));
        await Send(c, "POST", $"/settlements/{settlement}/discrepancy", new { note = "" }, HttpStatusCode.BadRequest);
        await Send(c, "POST", $"/settlements/{settlement}/confirm", null, HttpStatusCode.OK);
        Assert.Equal(SettlementStatus.Confirmed, fixture.WithDb(db => db.Settlements.Single(s => s.Id == settlement).Status));
        await Send(c, "POST", $"/settlements/{settlement}/confirm", null, HttpStatusCode.BadRequest);
        fixture.WithDb(db => { db.Settlements.Single(s => s.Id == settlement).Status = SettlementStatus.Draft; return db.SaveChanges(); });
        await Send(c, "GET", $"/settlements/{settlement}", null, HttpStatusCode.NotFound);
        await Send(c, "POST", $"/settlements/{settlement}/confirm", null, HttpStatusCode.NotFound);
        await Send(c, "POST", $"/settlements/{settlement}/discrepancy", new { note = "Hidden draft" }, HttpStatusCode.NotFound);
        var all = await Send(c, "GET", "/settlements?status=Draft", null, HttpStatusCode.OK);
        Assert.Equal(0, all.GetProperty("totalCount").GetInt32());
    }
}
