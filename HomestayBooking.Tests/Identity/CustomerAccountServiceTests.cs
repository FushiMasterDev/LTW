using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Identity;

public class CustomerAccountServiceTests
{
    private static async Task<IServiceScope> NewScopeAsync()
    {
        var scope = IdentityTestHost.BuildServices().CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            await roles.CreateAsync(new IdentityRole(role));
        }
        return scope;
    }

    [Fact]
    public async Task Register_NewPhone_CreatesPersonCustomerAndAccount()
    {
        using var scope = await NewScopeAsync();
        var service = scope.ServiceProvider.GetRequiredService<CustomerAccountService>();

        var result = await service.RegisterAsync("Trần Thị Demo", "0977 123 456", null, "Demo@2026");

        Assert.True(result.Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var person = await db.PersonProfiles.Include(p => p.Customer).SingleAsync();
        Assert.Equal("+84977123456", person.PhoneNumber);
        Assert.NotNull(person.Customer);
        Assert.Equal(AccountStatus.Active, result.User!.Status);
        Assert.True(await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().IsInRoleAsync(result.User, AppRoles.Customer));
    }

    [Fact]
    public async Task Register_PhoneOfCallCenterCustomer_AttachesAccountToExistingProfile()
    {
        using var scope = await NewScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.PersonProfiles.Add(new PersonProfile { FullName = "Khách gọi điện", PhoneNumber = "+84987000002", Customer = new Customer() });
        await db.SaveChangesAsync();

        var result = await scope.ServiceProvider.GetRequiredService<CustomerAccountService>()
            .RegisterAsync("Lê Văn Hùng", "0987000002", null, "Demo@2026");

        Assert.True(result.Succeeded);
        var person = await db.PersonProfiles.Include(p => p.Customer).SingleAsync();
        Assert.Equal(person.Id, result.User!.PersonId);
        Assert.Equal("Lê Văn Hùng", person.FullName);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task Register_PhoneWithAccount_IsRefused()
    {
        using var scope = await NewScopeAsync();
        await IdentityTestHost.CreateAccountAsync(scope.ServiceProvider, "+84977123456", "Đã có", [AppRoles.Customer]);

        var result = await scope.ServiceProvider.GetRequiredService<CustomerAccountService>()
            .RegisterAsync("Người khác", "0977123456", null, "Demo@2026");

        Assert.False(result.Succeeded);
        Assert.Contains("đã có tài khoản", result.Errors.Single());
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsVietnameseErrors()
    {
        using var scope = await NewScopeAsync();

        var result = await scope.ServiceProvider.GetRequiredService<CustomerAccountService>()
            .RegisterAsync("A", "0977123456", null, "abcdefgh");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("chữ số"));
    }

    [Fact]
    public async Task EnsureCustomer_OwnerWithoutCustomer_AddsCustomerAndRole()
    {
        using var scope = await NewScopeAsync();
        var owner = await IdentityTestHost.CreateAccountAsync(scope.ServiceProvider, "+84912000009", "Chủ nhà", [AppRoles.Owner],
            p => p.Owner = new Owner());

        var customer = await scope.ServiceProvider.GetRequiredService<CustomerAccountService>().EnsureCustomerAsync(owner);

        Assert.NotNull(customer);
        Assert.True(await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().IsInRoleAsync(owner, AppRoles.Customer));
    }

    [Fact]
    public async Task EnsureCustomer_Employee_ReturnsNull()
    {
        using var scope = await NewScopeAsync();
        var employee = await IdentityTestHost.CreateAccountAsync(scope.ServiceProvider, "+84901000099", "Nhân viên", [AppRoles.Employee],
            p => p.Employee = new Employee { EmployeeCode = "NV099" });

        Assert.Null(await scope.ServiceProvider.GetRequiredService<CustomerAccountService>().EnsureCustomerAsync(employee));
    }
}
