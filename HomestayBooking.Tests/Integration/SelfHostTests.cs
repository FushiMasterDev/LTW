using System.Net;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Integration;

public sealed class SelfHostTests
{
    [Theory]
    [InlineData(true, "127.0.0.1", "https", "203.0.113.10")]
    [InlineData(true, "::1", "https", "203.0.113.10")]
    [InlineData(true, "192.0.2.5", "http", "192.0.2.5")]
    [InlineData(false, "127.0.0.1", "http", "127.0.0.1")]
    public async Task OnlyEnabledLoopbackProxyCanForward(bool enabled, string remote, string scheme, string ip)
    {
        using var factory = new TestWebApplicationFactory();
        using var host = Configure(factory, enabled, remote);
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Host = "fuji.example";
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
        // healthz bypasses redirection, so this test can inspect ignored headers as well.
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(scheme, response.Headers.GetValues("Test-Scheme").Single());
        Assert.Equal(ip, response.Headers.GetValues("Test-IP").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        if (enabled && remote is "127.0.0.1" or "::1")
        {
            var login = await client.GetAsync("/Account/EmployeeLogin");
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.Contains(login.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
            Assert.True(login.Headers.Contains("Strict-Transport-Security"));
        }
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/Dev/UiKit")]
    [InlineData("/Dev/SimulateBank")]
    public async Task ProductionHasNoDevelopmentEndpointsEvenForAnonymousRequests(string path)
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task HealthFailureIs503WithoutDatabaseDetails()
    {
        using var factory = new TestWebApplicationFactory();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration("database", _ => new FailedDatabase(), null, null));
            })));
        using var client = host.CreateClient();
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AbsoluteInvitationAndQrUseConfigurationInsteadOfRequestHost()
    {
        using var factory = new TestWebApplicationFactory();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Reminders:PublicBaseUrl", "https://fuji.example");
            builder.UseSetting("BankAccount:QrImageBaseUrl", "https://qr.example/image/");
        });
        int registrationId;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var employee = db.Employees.Where(e => e.EmployeeCode == "NV001")
                .Select(e => new { e.Id, AccountId = e.Person.Account!.Id }).Single();
            var registration = await scope.ServiceProvider.GetRequiredService<OwnerRegistrationService>()
                .CreateAssistedAsync(new("Chủ thử URL", "0977000555", "Nhà thử URL", "Hà Nội", db.Areas.First().Id, "0977000555"),
                    new Actor(ActorType.Employee, employee.AccountId, employee.Id));
            registrationId = registration.Id;
            var qr = scope.ServiceProvider.GetRequiredService<VietQrService>();
            Assert.StartsWith("https://qr.example/image/", qr.BuildImageUrl(300, "FJ TEST"));
        }
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Host = "request-host.example";
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Account/EmployeeLogin");
        var login = await client.PostAsync("/Account/EmployeeLogin", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["EmployeeCode"] = "NV001", ["Password"] = "Fuji@2026", ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Staff/OwnerRegistrations/Details/{registrationId}");
        var invite = await client.PostAsync($"/Staff/OwnerRegistrations/Invite/{registrationId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, invite.StatusCode);
        Assert.Contains(invite.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.Contains("CookieTempDataProvider") && cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
        var page = await client.GetStringAsync(invite.Headers.Location);
        Assert.Contains("https://fuji.example/Partner/Activate", page);
        Assert.DoesNotContain("https://request-host.example/Partner/Activate", page);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/uploads/verified-bank-qr.png")]
    public async Task BookingQrNeverFallsBackToDemoAccountInProduction(string? fallbackUrl)
    {
        using var factory = new TestWebApplicationFactory();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("BankAccount:BankCode", "TESTBANK");
            if (fallbackUrl is not null) builder.UseSetting("BankAccount:FallbackQrImageUrl", fallbackUrl);
        });
        // Prepare a view fixture. Booking creation/transactions are exercised separately on relational databases.
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customerId = db.Customers.Where(c => c.Person.PhoneNumber == "+84987000001").Select(c => c.Id).Single();
            var original = db.Bookings.First(b => b.CustomerId == customerId);
            var day = VietnamTime.Today(TimeProvider.System).AddDays(60);
            db.Bookings.Add(new Booking
            {
                Code = "FJ-QR-VIEW", CustomerId = customerId, ContactName = "Khách thử QR", ContactPhone = "+84987000001",
                HomestayId = original.HomestayId, RoomTypeId = original.RoomTypeId, RoomId = original.RoomId, CheckIn = day, CheckOut = day.AddDays(1), Guests = 1,
                Status = BookingStatus.AwaitingPayment, HoldExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, TotalAmount = 500000, DepositAmount = 150000,
                DepositRate = .30m, CommissionRate = .12m, CancellationPolicyId = original.CancellationPolicyId,
                Nights = [new BookingNight { Date = day, DayType = DayType.Weekday, Price = 500000 }],
            });
            await db.SaveChangesAsync();
        }
        using var client = host.CreateClient();
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Account/Login");
        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["PhoneNumber"] = "0987000001", ["Password"] = "Fuji@2026", ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var detail = await client.GetStringAsync("/Bookings/Details/FJ-QR-VIEW");
        Assert.DoesNotContain("/images/qr-mb.png", detail);
        Assert.Contains($"data-qr-fallback=\"{fallbackUrl}\"", detail);
        Assert.Contains("<dd>TESTBANK</dd>", detail);
    }

    [Fact]
    public async Task KeysCookieAndOldAntiforgerySurviveNewHost()
    {
        var path = Path.Combine(Path.GetTempPath(), "fuji-keys-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            string cookie, token, protectedValue;
            using (var factory = new TestWebApplicationFactory())
            using (var host = factory.WithWebHostBuilder(builder => builder.UseSetting("DataProtection:KeysPath", path)))
            {
                // Retrieve the antiforgery cookie with an independent client so it is present in headers.
                using var raw = host.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, BaseAddress = new Uri("https://localhost") });
                var page = await raw.GetAsync("/Account/EmployeeLogin");
                cookie = page.Headers.GetValues("Set-Cookie").Single(c => c.Contains("Antiforgery")).Split(';')[0];
                token = System.Text.RegularExpressions.Regex.Match(await page.Content.ReadAsStringAsync(), "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
                protectedValue = host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("restart").Protect("persistent");
            }
            Assert.NotEmpty(Directory.GetFiles(path, "key-*.xml"));
            using var nextFactory = new TestWebApplicationFactory();
            using var nextHost = nextFactory.WithWebHostBuilder(builder => builder.UseSetting("DataProtection:KeysPath", path));
            using var nextClient = nextHost.CreateClient();
            Assert.Equal("persistent", nextHost.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("restart").Unprotect(protectedValue));
            nextClient.DefaultRequestHeaders.Add("Cookie", cookie);
            var response = await nextClient.PostAsync("/Account/EmployeeLogin", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token, ["EmployeeCode"] = "AD001", ["Password"] = "Fuji@2026"
            }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("Identity.Application"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    private static WebApplicationFactory<Program> Configure(TestWebApplicationFactory factory, bool enabled, string remote) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ReverseProxy:Enabled", enabled.ToString());
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new RemoteAddress(remote)));
        });

    private sealed class RemoteAddress(string remote) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, following) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Test-Scheme"] = context.Request.Scheme;
                    context.Response.Headers["Test-IP"] = context.Connection.RemoteIpAddress?.ToString();
                    return Task.CompletedTask;
                });
                await following();
            });
            next(app);
        };
    }

    private sealed class FailedDatabase : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Unhealthy("secret database connection error"));
    }
}
