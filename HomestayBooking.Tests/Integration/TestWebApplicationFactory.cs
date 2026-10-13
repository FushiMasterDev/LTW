using System.Net;
using System.Text.RegularExpressions;
using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HomestayBooking.Tests.Integration;

/// <summary>
/// Chạy app thật (Program.cs: middleware, policy, antiforgery, Identity) nhưng thay SQL Server bằng CSDL InMemory riêng
/// cho mỗi factory — không chạm CSDL phát triển. SeedData.InitializeAsync gặp CSDL không quan hệ thì EnsureCreated rồi seed
/// đầy đủ (giống SeedDataTests). Môi trường "Testing" để cookie dùng SecurePolicy.Always như môi trường thật.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "it-" + Guid.NewGuid();

    public TestWebApplicationFactory()
    {
        // Cookie Secure chỉ được gửi qua HTTPS
        ClientOptions.BaseAddress = new Uri("https://localhost");
        ClientOptions.AllowAutoRedirect = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_databaseName));

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
}
