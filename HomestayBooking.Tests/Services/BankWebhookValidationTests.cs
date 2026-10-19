using HomestayBooking.Web.Controllers.Api;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class BankWebhookValidationTests
{
    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-02-30 10:00:00")]
    [InlineData("")]
    public async Task InvalidTransactionDateReturnsProblemBeforeProcessingMoney(string transactionDate)
    {
        var controller = Controller();
        var result = Assert.IsAssignableFrom<ObjectResult>(await controller.SePay(Payload(transactionDate)));
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ProblemDetails>(result.Value).Status);
    }

    [Fact]
    public async Task InvalidDateStillRequiresWebhookAuthentication()
    {
        var controller = Controller();
        controller.Request.Headers.Authorization = "Apikey wrong";
        Assert.IsType<UnauthorizedResult>(await controller.SePay(Payload("not-a-date")));
    }

    private static BankWebhookController Controller()
    {
        var controller = new BankWebhookController(null!, Options.Create(new BankAccountOptions { WebhookApiKey = "test-only-key" }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.Request.Headers.Authorization = "Apikey test-only-key";
        return controller;
    }

    private static BankWebhookController.SePayPayload Payload(string date) =>
        new(1, "MB", date, "0338405548", null, "QA", "in", 1000, null, null);
}
