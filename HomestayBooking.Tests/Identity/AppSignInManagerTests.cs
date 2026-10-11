using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HomestayBooking.Tests.Identity.IdentityTestHost;

namespace HomestayBooking.Tests.Identity;

public class AppSignInManagerTests
{
    [Fact]
    public async Task SignInManager_IsAppSignInManager()
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();

        Assert.IsType<AppSignInManager>(scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>());
    }

    [Theory]
    [InlineData(AccountStatus.Active, true)]
    [InlineData(AccountStatus.PendingActivation, false)]
    [InlineData(AccountStatus.Locked, false)]
    [InlineData(AccountStatus.Disabled, false)]
    public async Task CanSignInAsync_OnlyActiveAccount(AccountStatus status, bool expected)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Khách A", [AppRoles.Customer], status: status);

        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        Assert.Equal(expected, await signIn.CanSignInAsync(user));
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_DisabledAccount_IsNotAllowedEvenWithCorrectPassword()
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Khách A", [AppRoles.Customer], status: AccountStatus.Disabled);
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var result = await signIn.CheckPasswordSignInAsync(user, Password, lockoutOnFailure: true);

        Assert.True(result.IsNotAllowed);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidateSecurityStampAsync_AccountDisabledAfterSignIn_ReturnsNull()
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Khách A", [AppRoles.Customer]);
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var principal = await signIn.CreateUserPrincipalAsync(user);
        Assert.NotNull(await signIn.ValidateSecurityStampAsync(principal));

        user.Status = AccountStatus.Disabled;
        await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().UpdateAsync(user);

        Assert.Null(await signIn.ValidateSecurityStampAsync(principal));
    }

    [Fact]
    public async Task CreateUserPrincipalAsync_IncludesPersonClaims()
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Nguyễn Văn Minh", [AppRoles.Customer]);
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var principal = await signIn.CreateUserPrincipalAsync(user);

        Assert.Equal("Nguyễn Văn Minh", principal.DisplayName());
        Assert.Equal(user.PersonId, principal.PersonId());
        Assert.False(principal.IsActiveEmployee());
        Assert.False(principal.IsApprovedOwner());
    }

    [Fact]
    public async Task CreateUserPrincipalAsync_PersonWithoutName_UsesNationalPhone()
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", null!, [AppRoles.Customer]);
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var principal = await signIn.CreateUserPrincipalAsync(user);

        Assert.Equal("0987000001", principal.DisplayName());
    }

    [Theory]
    [InlineData(EmployeePosition.Manager, EmploymentStatus.Active, true, true)]
    [InlineData(EmployeePosition.Consultant, EmploymentStatus.Active, true, false)]
    [InlineData(EmployeePosition.Manager, EmploymentStatus.Suspended, false, false)]
    [InlineData(EmployeePosition.Administrator, EmploymentStatus.Resigned, false, false)]
    public async Task CreateUserPrincipalAsync_EmployeeClaimsFollowEmploymentStatus(
        EmployeePosition position, EmploymentStatus employment, bool isEmployee, bool isManagement)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84901000002", "Nguyễn Thị Hương", [AppRoles.Employee],
            p => p.Employee = new Employee { EmployeeCode = "QL001", Position = position, EmploymentStatus = employment });
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var principal = await signIn.CreateUserPrincipalAsync(user);

        Assert.Equal(isEmployee, principal.IsActiveEmployee());
        Assert.Equal(isManagement, principal.IsManagement());
    }

    [Theory]
    [InlineData(OwnerRegistrationStatus.Approved, true)]
    [InlineData(OwnerRegistrationStatus.PendingReview, false)]
    [InlineData(OwnerRegistrationStatus.Rejected, false)]
    public async Task CreateUserPrincipalAsync_OwnerApprovedClaimFollowsRegistration(OwnerRegistrationStatus status, bool approved)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84912000001", "Lê Văn Hùng", [AppRoles.Owner],
            p => p.Owner = new Owner { Registrations = { new OwnerRegistration { Status = status } } });
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var principal = await signIn.CreateUserPrincipalAsync(user);

        Assert.True(principal.IsInRole(AppRoles.Owner));
        Assert.Equal(approved, principal.IsApprovedOwner());
    }

    [Fact]
    public async Task CreateUserPrincipalAsync_OwnerRoleWithoutApproval_StillHasNoApprovedClaimAfterReject()
    {
        // Quyền quản lý homestay mất khi hồ sơ không còn Approved, dù Account vẫn có role Owner (v5 mục 4.7)
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84912000001", "Lê Văn Hùng", [AppRoles.Owner],
            p => p.Owner = new Owner { Registrations = { new OwnerRegistration { Status = OwnerRegistrationStatus.Approved } } });
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        Assert.True((await signIn.CreateUserPrincipalAsync(user)).IsApprovedOwner());

        var registration = await db.OwnerRegistrations.SingleAsync();
        registration.Status = OwnerRegistrationStatus.Rejected;
        await db.SaveChangesAsync();

        Assert.False((await signIn.CreateUserPrincipalAsync(user)).IsApprovedOwner());
    }
}
