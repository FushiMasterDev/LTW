using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Identity;

/// <summary>Dựng Identity giống Program.cs trên CSDL InMemory, kèm hàm tạo hồ sơ + Account theo v5.</summary>
internal static class IdentityTestHost
{
    public const string Password = "Fuji@2026";

    public static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(o => o
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager<AppSignInManager>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddErrorDescriber<VietnameseIdentityErrorDescriber>()
            .AddDefaultTokenProviders();
        services.AddScoped<AccountLookupService>();
        services.AddScoped<AccountActivityLogger>();
        services.AddScoped<CustomerAccountService>();
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BookingCodeVerificationAttempts>();
        services.AddScoped<BookingCodeVerificationService>();
        return services.BuildServiceProvider();
    }

    /// <summary>Tạo Person + Account (Active, mật khẩu chung) + role; hồ sơ nghiệp vụ do <paramref name="configure"/> gắn vào Person.</summary>
    public static async Task<ApplicationUser> CreateAccountAsync(
        IServiceProvider services, string phone, string fullName, string[] roles,
        Action<PersonProfile>? configure = null, AccountStatus status = AccountStatus.Active)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var person = new PersonProfile { FullName = fullName, PhoneNumber = phone };
        configure?.Invoke(person);
        db.PersonProfiles.Add(person);
        await db.SaveChangesAsync();

        var user = new ApplicationUser { PersonId = person.Id, Status = status };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRolesAsync(user, roles)).Succeeded);

        services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { RequestServices = services };
        return user;
    }
}
