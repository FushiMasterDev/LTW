using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HomestayBooking.Tests.Data;

public class SeedDataMigrationTests
{
    [Fact]
    public async Task InitializeAsync_AutoMigrateDisabledWithPendingMigrations_LogsAndThrowsWithoutMigrating()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var migrator = new Mock<IMigrator>(MockBehavior.Strict);
        await using var efServices = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .AddSingleton(migrator.Object)
            .BuildServiceProvider();

        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSingleton(loggerFactory.Object);
        collection.AddDataProtection();
        collection.AddOptions<DatabaseOptions>().Configure(o => o.AutoMigrate = false);
        collection.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlite(connection).UseInternalServiceProvider(efServices));
        collection.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        await using var services = collection.BuildServiceProvider();

        string[] pending;
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Tạo schema SQLite nhưng để toàn bộ migration chưa được đánh dấu áp dụng.
            await db.Database.EnsureCreatedAsync();
            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            Assert.NotEmpty(pending);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SeedData.InitializeAsync(services));
        Assert.Contains("Database:AutoMigrate", error.Message);
        Assert.All(pending, id => Assert.Contains(id, error.Message));
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => pending.All(id => value!.ToString()!.Contains(id))),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        migrator.VerifyNoOtherCalls();

        await using var finalScope = services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await finalDb.Database.GetAppliedMigrationsAsync());
        Assert.Equal(pending, (await finalDb.Database.GetPendingMigrationsAsync()).ToArray());
        Assert.False(await finalDb.Users.AnyAsync());
        Assert.False(await finalDb.Roles.AnyAsync());
    }
}
