using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.DesktopHost;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HomestayBooking.Tests.Integration;

/// <summary>Chạy entry point host thật trên Kestrel và SQLite file tạm, không SQL Server.</summary>
public sealed class DesktopStandaloneHostTests
{
    private sealed class Host : IAsyncDisposable
    {
        public WebApplication App { get; }
        public HttpClient Client { get; }
        private Host(WebApplication app)
        {
            App = app;
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = new Uri(address) };
        }

        public static async Task<Host> StartAsync(string root, params string[] extra)
        {
            var app = await DesktopHostProgram.BuildAsync([
                "--contentRoot", root, "--urls", "http://127.0.0.1:0",
                "--Logging:LogLevel:Default", "Warning", "--Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning", .. extra]);
            try { await app.StartAsync(); return new(app); }
            catch { await app.DisposeAsync(); throw; }
        }

        public async Task<T> WithServices<T>(Func<IServiceProvider, Task<T>> action)
        {
            await using var scope = App.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider);
        }

        public Task<T> WithDb<T>(Func<ApplicationDbContext, Task<T>> action) =>
            WithServices(p => action(p.GetRequiredService<ApplicationDbContext>()));

        public async Task<string> LoginAsync(string code)
        {
            var owner = code.StartsWith("09", StringComparison.Ordinal);
            var response = owner
                ? await Client.PostAsJsonAsync("/api/desktop/owner/auth/login", new { phoneNumber = code, password = SeedData.DefaultPassword })
                : await Client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = code, password = SeedData.DefaultPassword });
            await Expect(response, HttpStatusCode.OK);
            Assert.False(response.Headers.Contains("Set-Cookie"));
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("accessToken").GetString()!;
        }

        public void Bearer(string token) => Client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
            SqliteConnection.ClearAllPools();
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "fuji-desktop-host-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        foreach (var relative in new[] { Path.Combine("Data", "SampleFiles"), Path.Combine("App_Data", "public", "images", "demo") })
        {
            var source = Path.Combine(AppContext.BaseDirectory, relative);
            Assert.True(Directory.Exists(source), "Thiếu asset khi đóng gói: " + relative);
            var destination = Path.Combine(root, relative);
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        return root;
    }

    private static async Task Expect(HttpResponseMessage response, HttpStatusCode code) =>
        Assert.True(response.StatusCode == code, $"{response.RequestMessage?.RequestUri}: {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode code)
    {
        await Expect(response, code);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task RealHostSupportsAllRoleFamiliesFilesWritesAndPersistentRestartWithoutReseeding()
    {
        var root = CreateRoot();
        // Test-only key: never used outside this isolated test database.
        string[] options = ["--DesktopApi:SigningKey", "standalone-test-key-only-0123456789abcdef"];
        int bookingId, areaId, blockId, roomId, homeId, documentId, initialHolidayCount;
        string nvToken;
        var start = VietnamTime.Today(TimeProvider.System).AddDays(20);
        try
        {
            await using (var host = await Host.StartAsync(root, options))
            {
                Assert.True(File.Exists(Path.Combine(root, "App_Data", "fuji-desktop.db")));
                initialHolidayCount = await host.WithDb(db => db.Holidays.CountAsync());
                Assert.Equal(35, await host.WithDb(db => db.Homestays.CountAsync()));
                Assert.Equal(SeedData.SampleBookingCount, await host.WithDb(db => db.Bookings.CountAsync()));
                Assert.True(await host.WithDb(db => db.HomestayImages.AnyAsync()));
                Assert.True(await host.WithDb(db => db.VerificationDocuments.AnyAsync()));
                var swagger = await host.Client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
                var paths = swagger.GetProperty("paths").EnumerateObject().ToList();
                Assert.All(paths, p => Assert.StartsWith("/api/desktop/", p.Name));
                Assert.Equal(92, paths.Sum(p => p.Value.EnumerateObject().Count()));
                Assert.Equal("bearer", swagger.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
                var docPaths = swagger.GetProperty("paths");
                Assert.False(docPaths.GetProperty("/api/desktop/auth/login").GetProperty("post").TryGetProperty("security", out _));
                Assert.True(docPaths.GetProperty("/api/desktop/auth/me").GetProperty("get").TryGetProperty("security", out _));
                Assert.False(docPaths.GetProperty("/api/desktop/owner/auth/login").GetProperty("post").TryGetProperty("security", out _));
                Assert.Equal("Desktop Owner", docPaths.GetProperty("/api/desktop/owner/auth/me").GetProperty("get").GetProperty("tags")[0].GetString());
                await Expect(await host.Client.GetAsync("/swagger/index.html"), HttpStatusCode.OK);
                foreach (var path in new[] { "/", "/Account/Login", "/Owner/Dashboard", "/api/homestays", "/api/payments/webhook", "/healthz", "/images/demo/room-studio.webp", "/App_Data/fuji-desktop.db" })
                    await Problem(await host.Client.GetAsync(path), HttpStatusCode.NotFound);
                await Problem(await host.Client.GetAsync("/api/desktop/bookings"), HttpStatusCode.Unauthorized);

                nvToken = await host.LoginAsync("NV001");
                var manager = await host.LoginAsync("QL001");
                var accountant = await host.LoginAsync("KT001");
                var owner = await host.LoginAsync("0912000001");
                await host.LoginAsync("AD001");
                host.Bearer(nvToken);
                foreach (var path in new[] { "auth/me", "areas", "bookings", "customers", "assignees", "booking-homestays", $"availability?checkIn={start:yyyy-MM-dd}&checkOut={start.AddDays(1):yyyy-MM-dd}&guests=2" })
                    await Expect(await host.Client.GetAsync("/api/desktop/" + path), HttpStatusCode.OK);
                await Problem(await host.Client.GetAsync("/api/desktop/owner/dashboard"), HttpStatusCode.Forbidden);
                await Problem(await host.Client.GetAsync("/api/desktop/receipts"), HttpStatusCode.Forbidden);
                await Problem(await host.Client.PostAsJsonAsync("/api/desktop/areas", new { name = "Không được phép", province = "Hà Nội" }), HttpStatusCode.Forbidden);
                var room = await host.WithDb(db => db.Rooms.Where(r => r.RoomType.Homestay.Owner.Person.PhoneNumber == "+84912000001")
                    .OrderBy(r => r.Id).Select(r => new { r.Id, r.RoomTypeId, r.RoomType.HomestayId }).FirstAsync());
                roomId = room.Id; homeId = room.HomestayId;
                var request = new { phone = "0988888777", fullName = "Khách host SQLite", roomTypeId = room.RoomTypeId,
                    checkIn = start, checkOut = start.AddDays(1), guests = 2, channel = "Phone", submissionId = Guid.NewGuid() };
                var created = await host.Client.PostAsJsonAsync("/api/desktop/bookings", request);
                await Expect(created, HttpStatusCode.Created);
                bookingId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                var repeated = await host.Client.PostAsJsonAsync("/api/desktop/bookings", request);
                await Expect(repeated, HttpStatusCode.Created);
                Assert.Equal(bookingId, (await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
                await Expect(await host.Client.GetAsync($"/api/desktop/bookings/{bookingId}"), HttpStatusCode.OK);

                host.Bearer(manager);
                foreach (var report in new[] { "management", "bookings", "financial", "employees", "homestays" })
                    await Expect(await host.Client.GetAsync($"/api/desktop/reports/{report}?from={start.AddDays(-60):yyyy-MM-dd}&to={start.AddDays(30):yyyy-MM-dd}"), HttpStatusCode.OK);
                var area = await host.Client.PostAsJsonAsync("/api/desktop/areas", new { name = "Khu vực host lưu lại", province = "Hà Nội", areaGroup = "InnerCity" });
                await Expect(area, HttpStatusCode.Created);
                areaId = (await area.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                // Removing a seed row proves restart does not merely re-run idempotent seed.
                var holiday = await host.WithDb(db => db.Holidays.OrderByDescending(h => h.Date).Select(h => h.Date).FirstAsync());
                await Expect(await host.Client.DeleteAsync($"/api/desktop/holidays/{holiday:yyyy-MM-dd}"), HttpStatusCode.NoContent);

                var paymentId = await host.WithServices(async p =>
                {
                    var bytes = await File.ReadAllBytesAsync(Path.Combine(root, "Data", "SampleFiles", SeedData.SampleReceiptFile));
                    using var stream = new MemoryStream(bytes);
                    var file = new FormFile(stream, 0, bytes.Length, "file", "bill.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
                    var name = await p.GetRequiredService<ReceiptFileStore>().SaveAsync(file, "host-test");
                    Assert.True(File.Exists(Path.Combine(root, "App_Data", "receipts", name)));
                    return (await p.GetRequiredService<ReceiptReviewService>().SubmitAsync(bookingId, name, "Cọc demo", Actor.System)).Id;
                });
                host.Bearer(accountant);
                await Expect(await host.Client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.OK);
                await Problem(await host.Client.GetAsync("/api/desktop/bookings"), HttpStatusCode.Forbidden);
                await Problem(await host.Client.GetAsync("/api/desktop/owner/dashboard"), HttpStatusCode.Forbidden);
                await Expect(await host.Client.GetAsync("/api/desktop/receipts"), HttpStatusCode.OK);
                var bill = await host.Client.GetAsync($"/api/desktop/receipts/{paymentId}/file");
                await Expect(bill, HttpStatusCode.OK);
                Assert.Equal("image/png", bill.Content.Headers.ContentType?.MediaType);
                await Expect(await host.Client.PostAsync($"/api/desktop/receipts/{paymentId}/approve", null), HttpStatusCode.NoContent);
                Assert.Equal(BookingStatus.Deposited, await host.WithDb(db => db.Bookings.Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync()));

                host.Bearer(owner);
                await Problem(await host.Client.GetAsync("/api/desktop/bookings"), HttpStatusCode.Forbidden);
                await Problem(await host.Client.GetAsync("/api/desktop/areas"), HttpStatusCode.Forbidden);
                foreach (var path in new[] { "auth/me", "dashboard", "schedule?period=week", "bookings", $"homestays/{homeId}", $"calendar?homestayId={homeId}", "reviews", "settlements" })
                    await Expect(await host.Client.GetAsync("/api/desktop/owner/" + path), HttpStatusCode.OK);
                await Expect(await host.Client.PostAsJsonAsync($"/api/desktop/owner/bookings/{bookingId}/confirm", new { }), HttpStatusCode.OK);
                var blocks = $"/api/desktop/owner/homestays/{homeId}/rooms/{roomId}/blocks";
                var range = new { start = start.AddDays(2), end = start.AddDays(4), reason = "Maintenance" };
                var block = await host.Client.PostAsJsonAsync(blocks, range);
                await Expect(block, HttpStatusCode.Created);
                blockId = (await block.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                await Problem(await host.Client.PostAsJsonAsync(blocks, range), HttpStatusCode.Conflict);
                documentId = await host.WithDb(db => db.VerificationDocuments.Where(d => d.HomestayId == homeId).Select(d => d.Id).FirstAsync());
                await Expect(await host.Client.GetAsync($"/api/desktop/owner/documents/{documentId}/file"), HttpStatusCode.OK);
                using var upload = new MultipartFormDataContent();
                var image = new ByteArrayContent(await File.ReadAllBytesAsync(Path.Combine(root, "Data", "SampleFiles", SeedData.SampleDocumentFile)));
                image.Headers.ContentType = new("image/png");
                upload.Add(image, "file", "photo.png");
                upload.Add(new StringContent("false"), "cover");
                upload.Add(new StringContent("20"), "sortOrder");
                await Expect(await host.Client.PostAsync($"/api/desktop/owner/homestays/{homeId}/images", upload), HttpStatusCode.Created);
                Assert.Single(Directory.EnumerateFiles(Path.Combine(root, "App_Data", "public", "uploads", "homestays", homeId.ToString())));
                await Problem(await host.Client.GetAsync("/uploads/homestays/1/photo.png"), HttpStatusCode.NotFound);
            }

            await using (var restarted = await Host.StartAsync(root, options))
            {
                restarted.Bearer(nvToken);
                await Expect(await restarted.Client.GetAsync($"/api/desktop/bookings/{bookingId}"), HttpStatusCode.OK);
                Assert.Equal(SeedData.SampleBookingCount + 1, await restarted.WithDb(db => db.Bookings.CountAsync()));
                Assert.Equal(35, await restarted.WithDb(db => db.Homestays.CountAsync()));
                Assert.Equal(BookingStatus.Confirmed, await restarted.WithDb(db => db.Bookings.Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync()));
                Assert.True(await restarted.WithDb(db => db.Areas.AnyAsync(a => a.Id == areaId && a.Name == "Khu vực host lưu lại")));
                Assert.Equal(initialHolidayCount - 1, await restarted.WithDb(db => db.Holidays.CountAsync()));
                Assert.Equal(2, await restarted.WithDb(db => db.RoomNights.CountAsync(n => n.RoomBlockId == blockId)));
                restarted.Bearer(await restarted.LoginAsync("0912000001"));
                await Expect(await restarted.Client.GetAsync($"/api/desktop/owner/documents/{documentId}/file"), HttpStatusCode.OK);
                await Expect(await restarted.Client.DeleteAsync($"/api/desktop/owner/homestays/{homeId}/rooms/{roomId}/blocks/{blockId}"), HttpStatusCode.NoContent);
            }
            await using (var reset = await Host.StartAsync(root, [.. options, "--reset"]))
            {
                Assert.Equal(SeedData.SampleBookingCount, await reset.WithDb(db => db.Bookings.CountAsync()));
                Assert.Equal(initialHolidayCount, await reset.WithDb(db => db.Holidays.CountAsync()));
                Assert.False(await reset.WithDb(db => db.Areas.AnyAsync(a => a.Name == "Khu vực host lưu lại")));
                await reset.LoginAsync("NV001");
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task EphemeralKeyRestartInvalidatesTokensAndBothLoginsShareRateLimit()
    {
        var root = CreateRoot();
        string token;
        try
        {
            await using (var host = await Host.StartAsync(root, "--DesktopApi:LoginPermitLimit", "2", "--DesktopApi:LoginWindowSeconds", "600"))
            {
                token = await host.LoginAsync("NV001");
                await host.LoginAsync("0912000001");
                var rejected = await host.Client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "QL001", password = SeedData.DefaultPassword });
                await Problem(rejected, HttpStatusCode.TooManyRequests);
                Assert.True(rejected.Headers.Contains("Retry-After"));
                await Problem(await host.Client.PostAsJsonAsync("/api/desktop/owner/auth/login", new { phoneNumber = "0912000001", password = SeedData.DefaultPassword }), HttpStatusCode.TooManyRequests);
            }
            await using (var restarted = await Host.StartAsync(root))
            {
                restarted.Bearer(token);
                await Problem(await restarted.Client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.Unauthorized);
                restarted.Bearer(await restarted.LoginAsync("NV001"));
                await Expect(await restarted.Client.GetAsync("/api/desktop/auth/me"), HttpStatusCode.OK);
                var roomType = await restarted.WithDb(db => db.RoomTypes.OrderBy(t => t.Id).Select(t => t.Id).FirstAsync());
                var checkIn = VietnamTime.Today(TimeProvider.System).AddDays(25);
                var created = await restarted.Client.PostAsJsonAsync("/api/desktop/bookings", new
                {
                    phone = "0988888666", fullName = "Khách kiểm tra hết hạn", roomTypeId = roomType,
                    checkIn, checkOut = checkIn.AddDays(1), guests = 1, channel = "Phone", submissionId = Guid.NewGuid(),
                });
                await Expect(created, HttpStatusCode.Created);
                var overdueId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                await restarted.WithDb(async db =>
                {
                    var b = await db.Bookings.SingleAsync(b => b.Id == overdueId);
                    b.HoldExpiresAt = DateTime.UtcNow.AddMinutes(-1);
                    await db.SaveChangesAsync();
                    return b.Id;
                });
                var logger = new Mock<ILogger<DesktopMaintenanceJob>>();
                var job = new DesktopMaintenanceJob(restarted.App.Services.GetRequiredService<IServiceScopeFactory>(), logger.Object);
                await job.RunOnceAsync();
                Assert.Equal(BookingStatus.Expired, await restarted.WithDb(db => db.Bookings.Where(b => b.Id == overdueId).Select(b => b.Status).SingleAsync()));
                Assert.Equal(0, await restarted.WithDb(db => db.RoomNights.CountAsync(n => n.BookingId == overdueId)));
                Assert.DoesNotContain(logger.Invocations, i => i.Method.Name == "Log" && i.Arguments[0] is LogLevel.Error);
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
