using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Data;

public class SeedDataTests
{
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<AccountLookupService>();
        return services.BuildServiceProvider();
    }

    // 5 nhân viên + 6 chủ homestay + 1 khách có tài khoản; owner đầu tiên kiêm khách hàng nên không thêm Account
    private static readonly int ExpectedAccounts =
        SeedData.EmployeeData.Length + SeedData.OwnerData.Length
        + SeedData.CustomerData.Count(c => c.HasAccount && SeedData.OwnerData.All(o => o.Phone != c.Phone));

    [Fact]
    public async Task InitializeAsync_CreatesRolesPeopleCategoriesAndHomestays()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(AppRoles.All.Length, await db.Roles.CountAsync());
        Assert.Equal(SeedData.EmployeeData.Length, await db.Employees.CountAsync());
        Assert.Equal(SeedData.OwnerData.Length, await db.Owners.CountAsync());
        Assert.Equal(SeedData.CustomerData.Length, await db.Customers.CountAsync());
        Assert.Equal(ExpectedAccounts, await db.Users.CountAsync());
        Assert.Equal(35, await db.Homestays.CountAsync());
        Assert.True(await db.Rooms.CountAsync() >= 70);
        Assert.NotEmpty(await db.Holidays.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_SeedsValidBusinessData()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var homestays = await db.Homestays
            .Include(h => h.Contracts)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Rooms)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .ToListAsync();

        Assert.All(homestays, h =>
        {
            Assert.Equal(HomestayStatus.Active, h.Status);
            // D6, BR-02: mỗi homestay đúng một hợp đồng đang hiệu lực, hoa hồng 10–15%
            var contract = Assert.Single(h.Contracts, c => c.Status == ContractStatus.Active);
            Assert.InRange(contract.CommissionRate, 0.10m, 0.15m);
            Assert.NotEmpty(h.RoomTypes);
        });

        Assert.All(homestays.SelectMany(h => h.RoomTypes), t =>
        {
            Assert.True(t.MaxGuests > 0);
            Assert.NotEmpty(t.Rooms);
            // D3, BR-08: đủ 3 mức giá, cuối tuần và lễ không thấp hơn ngày thường
            var price = t.Prices.ToDictionary(p => p.DayType, p => p.Price);
            Assert.Equal(3, price.Count);
            Assert.True(price[DayType.Weekend] >= price[DayType.Weekday]);
            Assert.True(price[DayType.Holiday] >= price[DayType.Weekend]);
        });

        // Chính sách hủy chung (BR-04) và chính sách lễ riêng của homestay Ba Vì (D10)
        var general = await db.CancellationPolicies.Include(p => p.Rules).SingleAsync(p => p.HomestayId == null);
        Assert.Equal([0, 3, 7], general.Rules.Select(r => r.MinDaysBefore).Order());
        Assert.Equal(5, await db.CancellationPolicies.CountAsync(p => p.HomestayId != null && p.Scope == PolicyScope.Holiday));

        // Owner đã duyệt có tài khoản nhận tiền đối soát
        Assert.All(await db.Owners.Where(o => o.Homestays.Any()).ToListAsync(), o => Assert.False(string.IsNullOrEmpty(o.BankAccountNo)));
    }

    [Fact]
    public async Task InitializeAsync_SeedsAccountsFollowingV5Rules()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // SĐT lưu dạng E.164; mỗi Customer có Customer Profile; homestay chỉ thuộc Owner đã duyệt
        Assert.All(await db.PersonProfiles.ToListAsync(), p => Assert.StartsWith("+84", p.PhoneNumber));
        Assert.Equal(await db.Customers.CountAsync(), await db.CustomerProfiles.CountAsync());
        Assert.False(await db.Homestays.AnyAsync(h => !h.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)));
        Assert.True(await db.OwnerRegistrations.AnyAsync(r => r.Status == OwnerRegistrationStatus.PendingReview));

        // Khách do nhân viên tạo chưa có Account
        Assert.True(await db.Customers.AnyAsync(c => c.Person.Account == null));
    }

    [Fact]
    public async Task InitializeAsync_RunTwice_DoesNotDuplicateData()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(35, await db.Homestays.CountAsync());
        Assert.Equal(ExpectedAccounts, await db.Users.CountAsync());
        Assert.Equal(SeedData.OwnerData.Length, await db.OwnerRegistrations.CountAsync());
        Assert.Equal(AppRoles.All.Length, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task SeededEmployee_CanBeFoundByCodeAndSignInWithDefaultPassword()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var lookup = scope.ServiceProvider.GetRequiredService<AccountLookupService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await lookup.FindForEmployeeLoginAsync("ad001");

        Assert.NotNull(admin);
        Assert.True(await userManager.CheckPasswordAsync(admin, SeedData.DefaultPassword));
        Assert.True(await userManager.IsInRoleAsync(admin, AppRoles.Employee));
    }

    [Fact]
    public async Task SeededOwnerWhoIsAlsoCustomer_HasOneAccountWithBothRoles()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var lookup = scope.ServiceProvider.GetRequiredService<AccountLookupService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var account = await lookup.FindForPhoneLoginAsync("0912 000 001");

        Assert.NotNull(account);
        Assert.Equal([AppRoles.Customer, AppRoles.Owner], (await userManager.GetRolesAsync(account)).Order());
    }

    [Fact]
    public async Task InitializeAsync_ExistingAccountWithoutRole_GetsRoleAssigned()
    {
        await using var services = BuildServices();
        using (var scope = services.CreateScope())
        {
            // Mô phỏng lần seed trước dừng sau khi tạo Account nhưng trước khi gán role
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var person = new PersonProfile { FullName = "Lê Văn Hùng", PhoneNumber = "+84912000001" };
            db.PersonProfiles.Add(person);
            await db.SaveChangesAsync();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = new ApplicationUser { PersonId = person.Id, Status = AccountStatus.Active };
            Assert.True((await userManager.CreateAsync(account, SeedData.DefaultPassword)).Succeeded);
        }

        await SeedData.InitializeAsync(services);

        using var verify = services.CreateScope();
        var lookup = verify.ServiceProvider.GetRequiredService<AccountLookupService>();
        var manager = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await lookup.FindForPhoneLoginAsync("0912000001");
        Assert.NotNull(existing);
        Assert.True(await manager.IsInRoleAsync(existing, AppRoles.Owner));
        Assert.Equal(ExpectedAccounts, await manager.Users.CountAsync());
    }
}
