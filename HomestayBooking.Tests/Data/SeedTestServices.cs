using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HomestayBooking.Tests.Data;

internal static class SeedTestServices
{
    public static ServiceProvider Build(
        Action<IServiceCollection> configureDatabase,
        string? environmentName = "Development")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddOptions<DatabaseOptions>().Configure(o => o.AutoMigrate = false);
        if (environmentName is not null)
        {
            // Đường dẫn không tồn tại để seed không chép file mẫu vào App_Data thật.
            var path = Path.Combine(Path.GetTempPath(), "homestay-seed-tests", Guid.NewGuid().ToString());
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
            environment.SetupGet(e => e.ContentRootPath).Returns(path);
            environment.SetupGet(e => e.WebRootPath).Returns(Path.Combine(path, "wwwroot"));
            services.AddSingleton(environment.Object);
        }

        configureDatabase(services);
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<AccountLookupService>();
        return services.BuildServiceProvider();
    }
}
