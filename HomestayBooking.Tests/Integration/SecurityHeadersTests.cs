namespace HomestayBooking.Tests.Integration;

/// <summary>Header bảo mật cơ bản (QA trước ra mắt) có trên trang và API; trang tự đặt chặt hơn thì không bị ghi đè.</summary>
public sealed class SecurityHeadersTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/api/homestays")]
    public async Task ResponsesCarryBaselineSecurityHeaders(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        string Header(string name) => string.Join(",", response.Headers.TryGetValues(name, out var v) ? v : []);
        Assert.Equal("nosniff", Header("X-Content-Type-Options"));
        Assert.Equal("DENY", Header("X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Header("Referrer-Policy"));
        Assert.Contains("camera=()", Header("Permissions-Policy"));
        Assert.Contains("frame-ancestors 'none'", Header("Content-Security-Policy"));
    }
}
