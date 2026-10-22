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
    public async Task WebSwaggerListsOnlyLegacyApiWithoutDesktopOrBearer()
    {
        using var root = new TestWebApplicationFactory();
        using var host = root.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var client = host.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(doc.TryGetProperty("components", out var components) && components.TryGetProperty("securitySchemes", out _));
        var paths = doc.GetProperty("paths").EnumerateObject().ToList();
        Assert.DoesNotContain(paths, p => p.Name.StartsWith("/api/desktop/"));
        Assert.Equal(10, paths.Sum(p => p.Value.EnumerateObject().Count(o => new[] { "get", "post", "put", "delete" }.Contains(o.Name))));
    }
}
