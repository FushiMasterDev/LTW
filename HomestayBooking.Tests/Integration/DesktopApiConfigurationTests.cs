using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Web.DesktopApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class DesktopApiConfigurationTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("short")]
    [InlineData("minutes")]
    [InlineData("issuer")]
    [InlineData("limiter")]
    public async Task InvalidProductionConfigurationDisablesDesktopAndKeepsWebAlive(string invalid)
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.PostConfigure<DesktopApiOptions>(o =>
            {
                o.SigningKey = invalid == "missing" ? null : invalid == "short" ? "short" : new string('x', 64);
                if (invalid == "minutes") o.TokenMinutes = 0;
                if (invalid == "issuer") o.Issuer = "";
                if (invalid == "limiter") o.LoginPermitLimit = 0;
            }));
        });
        var client = host.CreateClient();
        Assert.False(host.Services.GetRequiredService<DesktopApiRuntime>().Enabled);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/EmployeeLogin")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/desktop/auth/login", new { employeeCode = "QL001", password = "placeholder" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(503, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task SwaggerHasBearerOnlyForDesktopAndKeepsLegacyRoutes()
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var client = host.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("bearer", doc.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = doc.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/desktop/auth/me").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/desktop/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/homestays").GetProperty("get").TryGetProperty("security", out _));
        Assert.Equal(10, paths.EnumerateObject().Where(p => !p.Name.StartsWith("/api/desktop/"))
            .Sum(p => p.Value.EnumerateObject().Count(o => new[] { "get", "post", "put", "delete" }.Contains(o.Name))));
        Assert.Equal(43, paths.EnumerateObject().Where(p => p.Name.StartsWith("/api/desktop/"))
            .Sum(p => p.Value.EnumerateObject().Count(o => new[] { "get", "post", "put", "delete" }.Contains(o.Name))));
    }
}
