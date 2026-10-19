using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class ProductionErrorHandlingTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task FailedWebhookPostRendersFriendly500WithoutRequiringBrowserAntiforgery()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Configure<MvcOptions>(options => options.Filters.Add(new ThrowingPostFilter()))));
        var client = host.CreateClient();
        var response = await client.PostAsJsonAsync("/api/bank-webhook/sepay", new
        {
            Id = 1, TransactionDate = "2026-10-20 10:00:00", AccountNumber = "QA",
            TransferType = "in", TransferAmount = 1000,
        });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Fuji", html);
        Assert.DoesNotContain("QA error sentinel", html);
        Assert.DoesNotContain(nameof(InvalidOperationException), html);
    }

    // Test-only fault inside MVC, after the real exception-handler middleware and antiforgery filters.
    private sealed class ThrowingPostFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Request.Path == "/api/bank-webhook/sepay")
                throw new InvalidOperationException("QA error sentinel");
            await next();
        }
    }
}
