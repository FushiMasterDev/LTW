using System.Security.Claims;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class ChangeRequestPoliciesTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public ChangeRequestPoliciesTests(TestWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData(EmployeePosition.Manager, true, false)]
    [InlineData(EmployeePosition.Accountant, false, true)]
    [InlineData(EmployeePosition.Administrator, false, false)]
    [InlineData(EmployeePosition.Consultant, false, false)]
    public async Task ExactPositionPolicies(EmployeePosition position, bool approve, bool finance)
    {
        using var scope = _factory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, AppRoles.Employee), new Claim(AppClaimTypes.EmployeePosition, position.ToString())], "test"));
        Assert.Equal(approve, (await authorization.AuthorizeAsync(principal, null, AppPolicies.ChangeRequestApprove)).Succeeded);
        Assert.Equal(finance, (await authorization.AuthorizeAsync(principal, null, AppPolicies.ChangeRequestFinance)).Succeeded);
    }

    [Fact]
    public async Task OwnerWithPositionClaimStillCannotApproveOrConfirmFinance()
    {
        using var scope = _factory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, AppRoles.Owner), new Claim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Manager))], "test"));
        Assert.False((await authorization.AuthorizeAsync(principal, null, AppPolicies.ChangeRequestApprove)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(principal, null, AppPolicies.ChangeRequestFinance)).Succeeded);
    }
}
