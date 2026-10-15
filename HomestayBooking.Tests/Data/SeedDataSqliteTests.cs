using HomestayBooking.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Data;

public class SeedDataSqliteTests
{
    [Fact]
    public async Task InitializeAsync_OnSqlite_SatisfiesChecksAndCanRunTwice()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var services = BuildServices(connection);

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.IsType<SqliteTestDbContext>(db);
            await db.Database.EnsureCreatedAsync();

            // Tạo schema từ model SQLite dùng trong test, bao gồm các CHECK.
            // Đánh dấu migration đã áp dụng để InitializeAsync không chạy DDL của SQL Server.
            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            // Migration gắn với ApplicationDbContext, không phải SqliteTestDbContext.
            // Chỉ dùng context gốc để lấy danh sách migration, không tạo hay migrate schema của nó.
            await using var migrationContext = new ApplicationDbContext(
                scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
            var migrations = migrationContext.Database.GetMigrations().ToArray();
            Assert.NotEmpty(migrations);
            foreach (var id in migrations)
            {
                await db.Database.ExecuteSqlRawAsync(
                    history.GetInsertScript(new HistoryRow(id, ProductInfo.GetVersion())));
            }

            Assert.Equal(migrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }

        await SeedData.InitializeAsync(services);

        int bookingsAfterFirstRun;
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            bookingsAfterFirstRun = await db.Bookings.CountAsync();
            Assert.True(bookingsAfterFirstRun > 0);
            Assert.True(await db.Payments.AnyAsync());
            Assert.True(await db.Settlements.AnyAsync());
            Assert.True(await db.BookingChangeRequests.AnyAsync());
        }

        await SeedData.InitializeAsync(services);

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(bookingsAfterFirstRun, await db.Bookings.CountAsync());
            Assert.True(await db.Payments.AnyAsync());
            Assert.True(await db.Settlements.AnyAsync());
            Assert.True(await db.BookingChangeRequests.AnyAsync());
        }
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        return SeedTestServices.Build(services =>
        {
            services.AddDbContext<ApplicationDbContext>(o =>
                o.UseSqlite(connection).ConfigureWarnings(w =>
                    // Đổi decimal sang double là chủ ý của test nên model khác snapshot SQL Server.
                    w.Ignore(RelationalEventId.PendingModelChangesWarning)));
            // Helper hiện có nhận options của ApplicationDbContext, không phải lớp con.
            services.AddScoped<ApplicationDbContext, SqliteTestDbContext>();
        });
    }
}
