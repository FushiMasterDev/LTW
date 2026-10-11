using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using static HomestayBooking.Tests.Identity.IdentityTestHost;

namespace HomestayBooking.Tests.Identity;

public class AccountLookupServiceTests
{
    [Theory]
    [InlineData("0987000001")]
    [InlineData("+84 987 000 001")]
    [InlineData("84987000001")]
    public async Task FindForPhoneLogin_AnyPhoneFormat_FindsCustomer(string input)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Khách A", [AppRoles.Customer]);

        var found = await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForPhoneLoginAsync(input);

        Assert.Equal(user.Id, found?.Id);
    }

    [Fact]
    public async Task FindForPhoneLogin_EmployeeOnlyAccount_ReturnsNull()
    {
        // Không dùng SĐT để đăng nhập cổng nội bộ (v5 mục 8.2)
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        await CreateAccountAsync(scope.ServiceProvider, "+84901000011", "Nhân viên", [AppRoles.Employee],
            p => p.Employee = new Employee { EmployeeCode = "NV001" });

        var found = await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForPhoneLoginAsync("0901000011");

        Assert.Null(found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("không-phải-số")]
    [InlineData("0987999999")]
    public async Task FindForPhoneLogin_UnknownOrInvalid_ReturnsNull(string? input)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        await CreateAccountAsync(scope.ServiceProvider, "+84987000001", "Khách A", [AppRoles.Customer]);

        Assert.Null(await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForPhoneLoginAsync(input));
    }

    [Theory]
    [InlineData("NV001")]
    [InlineData(" nv001 ")]
    public async Task FindForEmployeeLogin_CodeIsCaseInsensitiveAndTrimmed(string input)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        var user = await CreateAccountAsync(scope.ServiceProvider, "+84901000011", "Nhân viên", [AppRoles.Employee],
            p => p.Employee = new Employee { EmployeeCode = "NV001" });

        var found = await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForEmployeeLoginAsync(input);

        Assert.Equal(user.Id, found?.Id);
    }

    [Theory]
    [InlineData(EmploymentStatus.Suspended)]
    [InlineData(EmploymentStatus.Resigned)]
    public async Task FindForEmployeeLogin_NotActiveEmployee_ReturnsNull(EmploymentStatus status)
    {
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        await CreateAccountAsync(scope.ServiceProvider, "+84901000011", "Nhân viên", [AppRoles.Employee],
            p => p.Employee = new Employee { EmployeeCode = "NV001", EmploymentStatus = status });

        Assert.Null(await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForEmployeeLoginAsync("NV001"));
    }

    [Fact]
    public async Task FindForEmployeeLogin_EmployeeRecordWithoutEmployeeRole_ReturnsNull()
    {
        // Có hồ sơ EMPLOYEE nhưng Account chưa được cấp role Employee (v5 mục 7)
        await using var services = BuildServices();
        using var scope = services.CreateScope();
        await CreateAccountAsync(scope.ServiceProvider, "+84901000011", "Nhân viên", [AppRoles.Customer],
            p => p.Employee = new Employee { EmployeeCode = "NV001" });

        Assert.Null(await scope.ServiceProvider.GetRequiredService<AccountLookupService>().FindForEmployeeLoginAsync("NV001"));
    }
}
