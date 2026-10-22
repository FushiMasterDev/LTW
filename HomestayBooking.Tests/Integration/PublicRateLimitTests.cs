using System.Net;
using System.Text.Json;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class PublicRateLimitTests
{
    private static void Configure(IServiceCollection services)
    {
        services.PostConfigure<PublicRateLimitOptions>(o => { o.PermitLimit = 2; o.WindowSeconds = 120; });
        services.AddSingleton<IStartupFilter, TestRemoteIp>();
    }

    [Theory]
    [InlineData("/Bookings/Lookup")]
    [InlineData("/Account/Register")]
    [InlineData("/Partner/Index")]
    public async Task MvcPostsAboveConfiguredLimitReturn429HtmlAndGetFormsRemainAvailable(string path)
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(Configure));
        using var client = host.CreateClient();
        var token = await TestWebApplicationFactory.TokenFromAsync(client, path);
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
                { ["__RequestVerificationToken"] = token }));
            Assert.Equal(i < 2 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, response.StatusCode);
            if (i == 2)
            {
                Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
                Assert.Contains("Thử quá nhiều lần", await response.Content.ReadAsStringAsync());
                Assert.NotNull(response.Headers.RetryAfter);
            }
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        Assert.Empty(root.Errors);
    }

    [Fact]
    public async Task LookupApiReturnsProblemDetailsAndPartitionsByIpWithNoGlobalLimiter()
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(Configure));
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Remote-IP", "192.0.2.1");
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/bookings/unknown{i}?phone=0900000000")).StatusCode);
        var limited = await client.GetAsync("/api/bookings/another?phone=0900000001");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        Assert.Equal(429, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Too Many Requests", json.RootElement.GetProperty("title").GetString());
        Assert.NotNull(limited.Headers.RetryAfter);
        // IPv4-mapped IPv6 phải dùng cùng ngân sách với IPv4 của cùng IP.
        client.DefaultRequestHeaders.Remove("X-Test-Remote-IP");
        client.DefaultRequestHeaders.Add("X-Test-Remote-IP", "::ffff:192.0.2.1");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/bookings/other")).StatusCode);
        using var other = host.CreateClient();
        other.DefaultRequestHeaders.Add("X-Test-Remote-IP", "192.0.2.2");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/bookings/unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/homestays")).StatusCode);
    }

    [Theory]
    [InlineData("/Bookings/Lookup")]
    [InlineData("/Account/Register")]
    [InlineData("/Partner/Index")]
    public async Task LookupAndRegistrationEndpointsShareIpBudget(string path)
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(Configure));
        using var client = host.CreateClient();
        await client.GetAsync("/api/bookings/unknown1");
        await client.GetAsync("/api/bookings/unknown2");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, path);
        var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
            { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/Account/Register")]
    [InlineData("/Partner/Index")]
    public async Task RegistrationFormsExposeOptionalBookingCodeWithVisibleLabel(string path)
    {
        using var host = new TestWebApplicationFactory();
        using var client = host.CreateClient();
        var html = await client.GetStringAsync(path);
        Assert.Contains("name=\"BookingCode\"", html);
        Assert.Contains("Mã đơn đã đặt (nếu từng đặt phòng bằng số này)", html);
        Assert.DoesNotContain("data-val-required=\"Vui lòng nhập Mã đơn", html);
    }

    private sealed class TestRemoteIp : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers["X-Test-Remote-IP"].ToString(), out var ip))
                    context.Connection.RemoteIpAddress = ip;
                await nextMiddleware();
            });
            next(app);
        };
    }
}
