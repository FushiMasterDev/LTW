using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Integration;

public sealed class DesktopApiFixture : IDisposable
{
    public TestWebApplicationFactory Root { get; } = new();
    public WebApplicationFactory<Program> Host { get; }
    public DesktopApiFixture() => Host = Root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        s.PostConfigure<DesktopApiOptions>(o => o.LoginPermitLimit = 1000)));
    public void Dispose() { Host.Dispose(); Root.Dispose(); }
}

public sealed class DesktopApiTests(DesktopApiFixture fixture) : IClassFixture<DesktopApiFixture>
{
    private WebApplicationFactory<Program> Host => fixture.Host;
    private TestWebApplicationFactory Root => fixture.Root;
    private static DateOnly Today => VietnamTime.Today(TimeProvider.System);

    private async Task<(HttpClient Client, JsonElement Login)> Login(string code = "QL001", WebApplicationFactory<Program>? host = null)
    {
        var client = (host ?? Host).CreateClient();
        var response = await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = code, password = SeedData.DefaultPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(response.Headers, h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("accessToken").GetString());
        return (client, login);
    }

    private static async Task<JsonElement> Problem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, json.GetProperty("status").GetInt32());
        return json;
    }

    [Fact]
    public async Task LoginAndMePreserveFactoryClaimsWithoutCreatingCookie()
    {
        var (client, login) = await Login("NV001");
        Assert.Equal("Consultant", login.GetProperty("employee").GetProperty("position").GetString());
        Assert.EndsWith("Z", login.GetProperty("expiresAt").GetString());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("accessToken").GetString());
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Employee");
        Assert.Contains(token.Claims, c => c.Type == AppClaimTypes.EmployeePosition && c.Value == "Consultant");
        Assert.Contains(token.Claims, c => c.Type == "AspNet.Identity.SecurityStamp");
        Assert.InRange(token.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/desktop/auth/me");
        Assert.Equal("NV001", me.GetProperty("code").GetString());
        Assert.Equal(login.GetProperty("employee").GetProperty("id").GetInt32(), me.GetProperty("id").GetInt32());
        using var scope = Host.Services.CreateScope();
        var schemes = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal(IdentityConstants.ApplicationScheme, (await schemes.GetDefaultAuthenticateSchemeAsync())!.Name);
    }

    [Fact]
    public async Task InvalidCredentialsOwnersCustomersAndNonActiveAccountsHaveSameError()
    {
        var client = Host.CreateClient();
        string? message = null;
        foreach (var input in new[] { ("NO_SUCH_EMPLOYEE", "wrong"), ("NV001", "wrong"), ("0987000001", SeedData.DefaultPassword), ("0912000001", SeedData.DefaultPassword) })
        {
            var p = await Problem(await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = input.Item1, password = input.Item2 }), HttpStatusCode.Unauthorized);
            message ??= p.GetProperty("detail").GetString();
            Assert.Equal(message, p.GetProperty("detail").GetString());
        }
        foreach (var status in new[] { AccountStatus.Locked, AccountStatus.Disabled })
        {
            SetEmployee("NV002", u => u.Status = status);
            var p = await Problem(await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "NV002", password = SeedData.DefaultPassword }), HttpStatusCode.Unauthorized);
            Assert.Equal(message, p.GetProperty("detail").GetString());
        }
        SetEmployee("NV002", u => { u.Status = AccountStatus.Active; u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5); });
        await Problem(await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "NV002", password = SeedData.DefaultPassword }), HttpStatusCode.Unauthorized);
        SetEmployee("NV002", u => { u.LockoutEnd = null; u.AccessFailedCount = 0; });
    }

    [Fact]
    public async Task RateLimitIsOnlyOnDesktopLoginAndReturnsProblemWithRetryAfter()
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(s => s.PostConfigure<DesktopApiOptions>(o => o.LoginPermitLimit = 2)));
        var client = host.CreateClient();
        for (var i = 0; i < 2; i++)
            await Problem(await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "unknown", password = "bad" }), HttpStatusCode.Unauthorized);
        var limited = await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "unknown", password = "bad" });
        await Problem(limited, HttpStatusCode.TooManyRequests);
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/EmployeeLogin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/homestays")).StatusCode);
        await Problem(await client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingExpiredBadSignatureWrongAudienceAndCookieOnlyAreRejectedAsJson()
    {
        var client = Host.CreateClient();
        await Problem(await client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
        var cookie = await Root.SignInEmployeeAsync("QL001");
        await Problem(await cookie.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
        var (_, login) = await Login();
        var original = new JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("accessToken").GetString());
        var runtime = Host.Services.GetRequiredService<DesktopApiRuntime>();
        foreach (var variant in new[] { "expired", "signature", "audience", "issuer" })
        {
            var jwt = new JwtSecurityToken(variant == "issuer" ? "wrong" : runtime.Options.Issuer,
                variant == "audience" ? "wrong" : runtime.Options.Audience,
                original.Claims.Where(c => c.Type is not ("iss" or "aud" or "nbf" or "exp" or "iat")),
                DateTime.UtcNow.AddHours(-2), variant == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(variant == "signature" ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)) : runtime.Key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
            await Problem(await client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task LockStatusSecurityStampAndEmploymentChangesInvalidateIssuedTokensImmediately()
    {
        foreach (var change in new[] { "status", "stamp", "lockout", "employment" })
        {
            var (client, _) = await Login("NV002");
            if (change == "status") SetEmployee("NV002", u => u.Status = AccountStatus.Disabled);
            if (change == "stamp") SetEmployee("NV002", u => u.SecurityStamp = Guid.NewGuid().ToString());
            if (change == "lockout") SetEmployee("NV002", u => u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5));
            if (change == "employment") Root.WithDb(db => { db.Employees.Single(e => e.EmployeeCode == "NV002").EmploymentStatus = EmploymentStatus.Resigned; return db.SaveChanges(); });
            await Problem(await client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
            SetEmployee("NV002", u => { u.Status = AccountStatus.Active; u.LockoutEnd = null; u.AccessFailedCount = 0; });
            Root.WithDb(db => { db.Employees.Single(e => e.EmployeeCode == "NV002").EmploymentStatus = EmploymentStatus.Active; return db.SaveChanges(); });
        }
    }

    [Theory]
    [InlineData("NV001", "/api/desktop/receipts")]
    [InlineData("NV001", "/api/desktop/reports/management?from=2026-01-01&to=2026-02-01")]
    [InlineData("KT001", "/api/desktop/bookings")]
    [InlineData("KT001", "/api/desktop/customers")]
    [InlineData("KT001", "/api/desktop/assignees")]
    [InlineData("KT001", "/api/desktop/booking-homestays")]
    [InlineData("KT001", "/api/desktop/booking-homestays/1/room-types")]
    [InlineData("KT001", "/api/desktop/availability")]
    [InlineData("KT001", "/api/desktop/reports/financial?from=2026-01-01&to=2026-02-01")]
    public async Task PositionPoliciesUseBearerClaims(string code, string path)
    {
        var (client, _) = await Login(code);
        await Problem(await client.GetAsync(path), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/desktop/areas")).StatusCode);
    }

    [Theory]
    [InlineData("NV001")]
    [InlineData("KT001")]
    public async Task OnlyManagementWritesCatalogs(string code)
    {
        var (client, _) = await Login(code);
        foreach (var route in new[] { "areas", "amenities", "room-categories", "holidays" })
        {
            await Problem(await client.PostAsJsonAsync("/api/desktop/" + route, new { }), HttpStatusCode.Forbidden);
            await Problem(await client.PutAsJsonAsync($"/api/desktop/{route}/1", new { }), HttpStatusCode.Forbidden);
            await Problem(await client.DeleteAsync($"/api/desktop/{route}/1"), HttpStatusCode.Forbidden);
        }
    }

    [Theory]
    [InlineData("areas")]
    [InlineData("amenities")]
    [InlineData("room-categories")]
    [InlineData("holidays")]
    public async Task CatalogCrudPaginationDuplicateValidationAndNotFound(string route)
    {
        var (client, _) = await Login();
        var url = "/api/desktop/" + route;
        var suffix = Guid.NewGuid().ToString("N")[..8];
        object payload = route switch
        {
            "areas" => new { name = "Desktop " + suffix, province = "Test", areaGroup = "Suburb" },
            "amenities" => new { name = "Desktop " + suffix, icon = "bi-wifi" },
            "holidays" => new { date = "2097-06-17", name = "Desktop " + suffix },
            _ => new { name = "Desktop " + suffix },
        };
        var created = await client.PostAsJsonAsync(url, payload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var json = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = route == "holidays" ? json.GetProperty("date").GetString() : json.GetProperty("id").GetInt32().ToString();
        if (route == "areas") Assert.Equal("Suburb", json.GetProperty("areaGroup").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url + "/" + id)).StatusCode);
        var list = await client.GetFromJsonAsync<JsonElement>(url + "?page=1");
        Assert.True(list.GetProperty("totalCount").GetInt32() > 0);
        Assert.True(list.GetProperty("items").GetArrayLength() <= 10);
        Assert.Equal(1, list.GetProperty("page").GetInt32());
        Assert.True(list.GetProperty("totalPages").GetInt32() >= 1);
        await Problem(await client.GetAsync(url + "?page=invalid"), HttpStatusCode.BadRequest);
        await Problem(await client.PostAsJsonAsync(url, payload), HttpStatusCode.Conflict);
        await Problem(await client.PostAsJsonAsync(url, new { name = "" }), HttpStatusCode.BadRequest);
        var missing = route == "holidays" ? "2098-01-23" : "9999999";
        await Problem(await client.GetAsync(url + "/" + missing), HttpStatusCode.NotFound);
        await Problem(await client.PutAsJsonAsync(url + "/" + missing, payload), HttpStatusCode.NotFound);
        await Problem(await client.DeleteAsync(url + "/" + missing), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url + "/" + id, payload)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url + "/" + id)).StatusCode);
        await Problem(await client.GetAsync(url + "/" + id), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CatalogDeletionKeepsInUseGuardAndAudit()
    {
        var (client, _) = await Login();
        var used = Root.WithDb(db => db.Homestays.Select(h => h.AreaId).First());
        await Problem(await client.DeleteAsync($"/api/desktop/areas/{used}"), HttpStatusCode.BadRequest);
        var created = await client.PostAsJsonAsync("/api/desktop/areas", new { name = "Audit test", province = "Desktop" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/desktop/areas/{id}")).StatusCode);
        Assert.True(Root.WithDb(db => db.AuditLogs.Any(a => a.TableName == "Areas" && a.RecordId == id.ToString() && a.Action == AuditAction.Delete)));
    }

    [Fact]
    public async Task BookingReadsCreateReplayAssignStatusAndAvailabilityUseServices()
    {
        using var sqlite = await DesktopSqliteHost.CreateAsync();
        var (client, _) = await Login("NV001", sqlite.Host);
        var date = Today.AddDays(24);
        var offers = await client.GetFromJsonAsync<JsonElement>($"/api/desktop/availability?checkIn={date:yyyy-MM-dd}&checkOut={date.AddDays(1):yyyy-MM-dd}&guests=1");
        var offer = offers.GetProperty("items").EnumerateArray().First(o => o.GetProperty("freeRooms").GetInt32() > 0);
        var request = new { phone = "0991234567", fullName = "Desktop Test", roomTypeId = offer.GetProperty("roomTypeId").GetInt32(),
            checkIn = date, checkOut = date.AddDays(1), guests = 1, channel = "Phone", submissionId = Guid.NewGuid() };
        var created = await client.PostAsJsonAsync("/api/desktop/bookings", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var b = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = b.GetProperty("id").GetInt32();
        var replay = await client.PostAsJsonAsync("/api/desktop/bookings", request);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(id, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        Assert.Equal(1, sqlite.WithDb(db => db.Bookings.Count(row => row.Id == id)));
        var details = await client.GetFromJsonAsync<JsonElement>($"/api/desktop/bookings/{id}");
        Assert.Equal("AwaitingPayment", details.GetProperty("booking").GetProperty("status").GetString());
        Assert.EndsWith("Z", details.GetProperty("booking").GetProperty("createdAt").GetString());
        Assert.Equal(date.ToString("yyyy-MM-dd"), details.GetProperty("booking").GetProperty("checkIn").GetString());
        var list = await client.GetFromJsonAsync<JsonElement>("/api/desktop/bookings?status=AwaitingPayment&q=" + b.GetProperty("code").GetString());
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
        foreach (var route in new[] { "bookings", "assignees", "booking-homestays" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/desktop/" + route)).StatusCode);
            await Problem(await client.GetAsync($"/api/desktop/{route}?page=bad"), HttpStatusCode.BadRequest);
        }
        var employee = sqlite.WithDb(db => db.Employees.Single(e => e.EmployeeCode == "NV001").Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/desktop/bookings/{id}/assign", new { employeeId = employee })).StatusCode);
        await Problem(await client.PostAsJsonAsync($"/api/desktop/bookings/{id}/assign", new { employeeId = 9999999 }), HttpStatusCode.BadRequest);
        await Problem(await client.GetAsync("/api/desktop/bookings/9999999"), HttpStatusCode.NotFound);
        await Problem(await client.PostAsJsonAsync("/api/desktop/bookings/9999999/assign", new { employeeId = employee }), HttpStatusCode.NotFound);
        await Problem(await client.PostAsJsonAsync($"/api/desktop/bookings/{id}/status", new { status = "Cancelled", note = "Not allowed" }), HttpStatusCode.BadRequest);
        await Problem(await client.PostAsJsonAsync("/api/desktop/bookings/9999999/status", new { status = "Confirmed", note = "Missing" }), HttpStatusCode.NotFound);
        await Problem(await client.PostAsJsonAsync("/api/desktop/bookings", request with { channel = "Web", submissionId = Guid.NewGuid() }), HttpStatusCode.BadRequest);
        await Problem(await client.GetAsync($"/api/desktop/availability?checkIn={date:yyyy-MM-dd}&checkOut={date:yyyy-MM-dd}"), HttpStatusCode.BadRequest);
        // Same service transition as web: owner-deposited booking -> Confirmed.
        var deposited = sqlite.WithDb(db => db.Bookings.First(row => row.Status == BookingStatus.Deposited).Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/desktop/bookings/{deposited}/status", new { status = "Confirmed", note = "Desktop confirmation" })).StatusCode);
    }

    [Fact]
    public async Task ConsultantGetsSeededHomestayRoomTypesWithCategoryCapacityAndActiveRoomCount()
    {
        var (client, _) = await Login("NV001");
        var homeId = Root.WithDb(db => db.RoomTypes.Where(t => t.IsActive && t.Rooms.Any(r => r.IsActive)
            && t.Homestay.Status == HomestayStatus.Active
            && db.Contracts.Any(c => c.HomestayId == t.HomestayId && c.Status == ContractStatus.Active
                && c.StartDate <= Today && c.EndDate >= Today)).Select(t => t.HomestayId).First());
        var expected = Root.WithDb(db => db.RoomTypes.Where(t => t.HomestayId == homeId && t.IsActive && t.Rooms.Any(r => r.IsActive))
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Select(t => new BookingRoomTypeDto(t.Id, t.Name, t.CategoryId, t.Category.Name, t.MaxGuests, t.Rooms.Count(r => r.IsActive))).ToList());

        var actual = await client.GetFromJsonAsync<List<BookingRoomTypeDto>>($"/api/desktop/booking-homestays/{homeId}/room-types");
        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RoomTypesUseSellableScopeExcludeInactiveInventoryAndReturnAllOptionsWithoutPagination()
    {
        using var sqlite = await DesktopSqliteHost.CreateAsync();
        var (client, _) = await Login("NV001", sqlite.Host);
        var homeId = sqlite.WithDb(db => db.Homestays.Where(h => h.Status == HomestayStatus.Active
            && db.Contracts.Any(c => c.HomestayId == h.Id && c.Status == ContractStatus.Active
                && c.StartDate <= Today && c.EndDate >= Today.AddDays(30))).Select(h => h.Id).First());
        var expected = sqlite.WithDb(db =>
        {
            foreach (var type in db.RoomTypes.Where(t => t.HomestayId == homeId)) type.IsActive = false;
            var categoryId = db.RoomCategories.Select(c => c.Id).First();
            var types = Enumerable.Range(1, 11).Select(i => new RoomType
            {
                HomestayId = homeId, CategoryId = categoryId, Name = $"Desktop {i:00}", MaxGuests = i,
                Rooms = [new() { RoomCode = "A" }, new() { RoomCode = "B", IsActive = false }],
                Prices = Enum.GetValues<DayType>().Select(day => new RoomTypePrice { DayType = day, Price = 500000 }).ToList(),
            }).ToList();
            db.RoomTypes.AddRange(types);
            db.RoomTypes.AddRange(
                new RoomType { HomestayId = homeId, CategoryId = categoryId, Name = "Inactive type", MaxGuests = 2,
                    IsActive = false, Rooms = [new() { RoomCode = "A" }] },
                new RoomType { HomestayId = homeId, CategoryId = categoryId, Name = "Inactive rooms", MaxGuests = 2,
                    Rooms = [new() { RoomCode = "A", IsActive = false }] },
                new RoomType { HomestayId = homeId, CategoryId = categoryId, Name = "No rooms", MaxGuests = 2 });
            db.SaveChanges();
            return types.Select(t => t.Id).ToArray();
        });
        var url = $"/api/desktop/booking-homestays/{homeId}/room-types";
        var actual = (await client.GetFromJsonAsync<List<BookingRoomTypeDto>>(url))!;
        Assert.Equal(expected, actual.Select(t => t.Id));
        Assert.All(actual, t => Assert.Equal(1, t.ActiveRoomCount));

        foreach (var status in Enum.GetValues<HomestayStatus>().Where(s => s != HomestayStatus.Active))
        {
            sqlite.WithDb(db => { db.Homestays.Single(h => h.Id == homeId).Status = status; return db.SaveChanges(); });
            Assert.Empty((await client.GetFromJsonAsync<List<BookingRoomTypeDto>>(url))!);
        }
        sqlite.WithDb(db => { db.Homestays.Single(h => h.Id == homeId).Status = HomestayStatus.Active; return db.SaveChanges(); });
        foreach (var condition in new[] { "expired-status", "terminated", "expired-date", "future", "missing" })
        {
            sqlite.WithDb(db =>
            {
                foreach (var contract in db.Contracts.Where(c => c.HomestayId == homeId))
                {
                    contract.Status = condition == "expired-status" ? ContractStatus.Expired
                        : condition == "terminated" ? ContractStatus.Terminated : ContractStatus.Active;
                    contract.StartDate = condition == "future" ? Today.AddDays(1) : Today.AddDays(-30);
                    contract.EndDate = condition == "expired-date" ? Today.AddDays(-1) : Today.AddDays(30);
                    if (condition == "missing") db.Contracts.Remove(contract);
                }
                return db.SaveChanges();
            });
            Assert.Empty((await client.GetFromJsonAsync<List<BookingRoomTypeDto>>(url))!);
        }
        // Existing homestays without sellable types are also 200/[], including when only rooms are disabled.
        sqlite.WithDb(db =>
        {
            db.Contracts.Add(new() { HomestayId = homeId, ContractNo = "Desktop-RoomTypes", StartDate = Today.AddDays(-1),
                EndDate = Today.AddDays(30), CommissionRate = 0.12m });
            foreach (var room in db.Rooms.Where(r => r.RoomType.HomestayId == homeId)) room.IsActive = false;
            return db.SaveChanges();
        });
        Assert.Empty((await client.GetFromJsonAsync<List<BookingRoomTypeDto>>(url))!);
    }

    [Fact]
    public async Task RoomTypesMissingHomestayIs404AndRequiresBearer()
    {
        const string url = "/api/desktop/booking-homestays/9999999/room-types";
        using var anonymous = Host.CreateClient();
        await Problem(await anonymous.GetAsync(url), HttpStatusCode.Unauthorized);
        var (client, _) = await Login("NV001");
        var problem = await Problem(await client.GetAsync(url), HttpStatusCode.NotFound);
        Assert.Equal("Không tìm thấy homestay.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CustomersPreserveMaskingAndUpdateRules()
    {
        var (client, _) = await Login("NV001");
        var list = await client.GetFromJsonAsync<JsonElement>("/api/desktop/customers?q=&page=1");
        Assert.True(list.GetProperty("totalCount").GetInt32() > 0);
        await Problem(await client.GetAsync("/api/desktop/customers?page=bad"), HttpStatusCode.BadRequest);
        var id = Root.WithDb(db => db.Customers.First(c => c.Person.Account == null).Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/desktop/customers/{id}")).StatusCode);
        var updated = await client.PutAsJsonAsync($"/api/desktop/customers/{id}", new { note = "Desktop note" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Desktop note", (await updated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("note").GetString());
        await Problem(await client.PutAsJsonAsync($"/api/desktop/customers/{id}", new { phoneNumber = "0900000000" }), HttpStatusCode.BadRequest);
        await Problem(await client.GetAsync("/api/desktop/customers/9999999"), HttpStatusCode.NotFound);
        await Problem(await client.PutAsJsonAsync("/api/desktop/customers/9999999", new { note = "Test" }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReceiptsReadPrivateFileApproveRejectAndRejectBadOperations()
    {
        var (client, _) = await Login("KT001");
        var list = await client.GetFromJsonAsync<JsonElement>("/api/desktop/receipts?status=Submitted&page=1");
        Assert.True(list.GetProperty("totalCount").GetInt32() > 0);
        await Problem(await client.GetAsync("/api/desktop/receipts?status=bad"), HttpStatusCode.BadRequest);
        var ids = Root.WithDb(db => db.Payments.Where(p => p.Type == PaymentType.Deposit && p.Method == PaymentMethod.ManualReceipt
            && p.Status == PaymentStatus.Submitted && p.Booking.Status == BookingStatus.PaymentSubmitted).OrderBy(p => p.Id).Take(2).Select(p => p.Id).ToArray());
        Assert.Equal(2, ids.Length);
        // Factory deliberately isolates receipt files; create a valid test image using the store's own validation.
        using (var scope = Host.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ReceiptFileStore>();
            var bytes = System.IO.File.ReadAllBytes(Path.Combine(Host.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().ContentRootPath,
                "Data", "SampleFiles", "sample-receipt.png"));
            using var stream = new MemoryStream(bytes);
            var file = new Microsoft.AspNetCore.Http.FormFile(stream, 0, bytes.Length, "receipt", "test.png")
                { Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(), ContentType = "image/png" };
            var name = await store.SaveAsync(file, "DESKTOP-TEST");
            Root.WithDb(db => { db.Payments.Single(p => p.Id == ids[0]).ReceiptImageUrl = name; return db.SaveChanges(); });
        }
        var image = await client.GetAsync($"/api/desktop/receipts/{ids[0]}/file");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        await Problem(await client.GetAsync("/api/desktop/receipts/9999999/file"), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/desktop/receipts/{ids[0]}/approve", null)).StatusCode);
        await Problem(await client.PostAsync($"/api/desktop/receipts/{ids[0]}/approve", null), HttpStatusCode.BadRequest);
        await Problem(await client.PostAsync("/api/desktop/receipts/9999999/approve", null), HttpStatusCode.NotFound);
        await Problem(await client.PostAsJsonAsync($"/api/desktop/receipts/{ids[1]}/reject", new { reason = "" }), HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/desktop/receipts/{ids[1]}/reject", new { reason = "Invalid bill" })).StatusCode);
        await Problem(await client.PostAsJsonAsync("/api/desktop/receipts/9999999/reject", new { reason = "Missing" }), HttpStatusCode.NotFound);
        var (admin, _) = await Login("AD001");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/desktop/receipts")).StatusCode);
        await Problem(await admin.PostAsync($"/api/desktop/receipts/{ids[1]}/approve", null), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("management")]
    [InlineData("bookings")]
    [InlineData("financial")]
    [InlineData("employees")]
    [InlineData("homestays")]
    public async Task EveryReportDelegatesAndValidatesDates(string report)
    {
        var (client, _) = await Login();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/desktop/reports/{report}?from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}")).StatusCode);
        await Problem(await client.GetAsync($"/api/desktop/reports/{report}?from={Today:yyyy-MM-dd}&to={Today.AddDays(-1):yyyy-MM-dd}"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DesktopFilterMapsConcurrencyAndUniqueRacesAndDoesNotChangeWebErrors()
    {
        using var host = Host.WithWebHostBuilder(b => b.ConfigureServices(s => s.Configure<MvcOptions>(o => o.Filters.Add(new RaceFilter()))));
        var client = host.CreateClient();
        var login = await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "QL001", password = SeedData.DefaultPassword });
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        foreach (var fault in new[] { "concurrency", "unique" })
            await Problem(await client.GetAsync("/api/desktop/areas?fault=" + fault), HttpStatusCode.Conflict);
        await Problem(await client.GetAsync("/api/desktop/areas?fault=unknown"), HttpStatusCode.InternalServerError);
        var web404 = await client.GetAsync("/missing-web-page");
        Assert.Equal(HttpStatusCode.NotFound, web404.StatusCode);
        Assert.Equal("text/html", web404.Content.Headers.ContentType?.MediaType);
        var web403 = await client.GetAsync("/Account/AccessDenied");
        Assert.Equal(HttpStatusCode.Forbidden, web403.StatusCode);
        Assert.Equal("text/html", web403.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CookieWorkspacesAntiforgeryAndMvcQuoteKeepExistingBehavior()
    {
        foreach (var (code, path) in new[] { ("AD001", "/Admin/Dashboard"), ("NV001", "/Staff/Dashboard"), ("KT001", "/Accounting/Dashboard") })
        {
            var client = await Root.SignInEmployeeAsync(code);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(code == "AD001" ? "/Admin/Areas/Create" : code == "NV001" ? "/Staff/Bookings/Create" : "/Accounting/Receipts/Approve/1", new FormUrlEncodedContent([]))).StatusCode);
        }
        var anon = Host.CreateClient();
        var room = Root.WithDb(db => db.RoomTypes.First(t => t.IsActive).Id);
        var quote = await anon.GetAsync($"/Bookings/Quote?roomTypeId={room}&checkIn={Today.AddDays(20):yyyy-MM-dd}&checkOut={Today.AddDays(21):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode);
        Assert.Equal("application/json", quote.Content.Headers.ContentType?.MediaType);
        var json = await quote.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.Equal(JsonValueKind.String, json.GetProperty("nights")[0].GetProperty("dayType").ValueKind);
        Assert.Contains(json.GetProperty("nights")[0].GetProperty("dayType").GetString(), new[] { "Ngày thường", "Cuối tuần", "Ngày lễ" });
    }

    private void SetEmployee(string code, Action<ApplicationUser> action) => Root.WithDb(db =>
    {
        var u = db.Users.Single(u => u.Person.Employee != null && u.Person.Employee.EmployeeCode == code);
        action(u); return db.SaveChanges();
    });

    private sealed class RaceFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.Path != "/api/desktop/areas") return;
            var fault = context.HttpContext.Request.Query["fault"].ToString();
            if (fault == "concurrency") throw new DbUpdateConcurrencyException("race");
            if (fault == "unique") throw new CatalogConflictException("Name", "Tên này đã tồn tại.");
            if (fault == "unknown") throw new InvalidOperationException("Private sentinel");
        }
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
