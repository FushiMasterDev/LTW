using System.Net;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Data;
using HomestayBooking.Tests.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.FileProviders;

namespace HomestayBooking.Tests.Integration;

/// <summary>
/// Chạy app thật (Program.cs: middleware, policy, antiforgery, Identity) nhưng thay SQL Server bằng CSDL InMemory riêng
/// cho mỗi factory — không chạm CSDL phát triển. Dữ liệu demo do fixture Development chuẩn bị riêng;
/// ứng dụng vẫn dùng môi trường "Testing" để cookie có SecurePolicy.Always như môi trường thật.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "it-" + Guid.NewGuid();
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    public ConcurrentQueue<Exception> Errors { get; } = new();

    public TestWebApplicationFactory()
    {
        // Cookie Secure chỉ được gửi qua HTTPS
        ClientOptions.BaseAddress = new Uri("https://localhost");
        ClientOptions.AllowAutoRedirect = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Chỉ provider của fixture được seed demo; ứng dụng Testing chỉ seed dữ liệu nền.
        using (var seedServices = SeedTestServices.Build(services =>
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_databaseName, _databaseRoot))))
        {
            SeedData.InitializeAsync(seedServices).GetAwaiter().GetResult();
        }

        builder.UseEnvironment("Testing");
        builder.UseSetting("DataProtection:KeysPath", Path.Combine(Path.GetTempPath(), "fuji-test-keys-" + _databaseName));
        builder.ConfigureLogging(logging => logging.AddProvider(new ErrorCapture(Errors)));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_databaseName, _databaseRoot));

            // Ảnh bill của test nằm trong thư mục tạm, không ghi vào biên lai của app người dùng.
            services.RemoveAll<ReceiptFileStore>();
            services.AddSingleton(sp => new ReceiptFileStore(new ReceiptEnvironment(sp.GetRequiredService<IWebHostEnvironment>())));

            // Job mỗi phút đổi trạng thái đơn trong nền → làm test không ổn định
            var job = services.Single(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(BookingTimeoutJob));
            services.Remove(job);
        });
    }

    public T WithDb<T>(Func<ApplicationDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>Lấy token antiforgery từ form trên trang (cookie antiforgery do client giữ).</summary>
    public static async Task<string> TokenFromAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, $"Không thấy token antiforgery trên {url}");
        return match.Groups[1].Value;
    }

    /// <summary>Đăng nhập qua form thật (/Account/Login hoặc /Account/EmployeeLogin) → client mang cookie đăng nhập.</summary>
    public async Task<HttpClient> SignInAsync(string loginPath, string userField, string userName)
    {
        var client = CreateClient();
        var token = await TokenFromAsync(client, loginPath);
        var response = await client.PostAsync(loginPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [userField] = userName,
            ["Password"] = SeedData.DefaultPassword,
            ["__RequestVerificationToken"] = token,
        }));
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Đăng nhập {userName} thất bại: {(int)response.StatusCode}");
        return client;
    }

    public Task<HttpClient> SignInByPhoneAsync(string phone) => SignInAsync("/Account/Login", "PhoneNumber", phone);

    public Task<HttpClient> SignInEmployeeAsync(string code) => SignInAsync("/Account/EmployeeLogin", "EmployeeCode", code);
    private sealed class ReceiptEnvironment(IWebHostEnvironment source) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = source.EnvironmentName;
        public string ApplicationName { get; set; } = source.ApplicationName;
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LTW-receipt-test-" + Guid.NewGuid().ToString("N"));
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = source.WebRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = source.WebRootFileProvider;
    }

    private sealed class ErrorCapture(ConcurrentQueue<Exception> errors) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(errors);
        public void Dispose() { }
        private sealed class CaptureLogger(ConcurrentQueue<Exception> errors) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
            { if (level >= LogLevel.Error && error is not null) errors.Enqueue(error); }
        }
    }

}
