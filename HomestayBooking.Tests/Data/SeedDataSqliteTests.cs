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
            // Tin nhắc ngắn vẫn giữ đủ tên cơ sở dài nhất trong dữ liệu seed và liên kết chi tiết.
            var longest = (await db.Homestays.AsNoTracking().ToListAsync()).MaxBy(h => h.Name.Length)!;
            var sample = new HomestayBooking.Web.Models.Entities.Booking {
                Code = "FJ261110-001", Status = HomestayBooking.Web.Models.Entities.BookingStatus.Confirmed,
                Homestay = longest, CheckIn = new(2026, 11, 11) };
            var shortMessage = HomestayBooking.Web.Services.ReminderService.CustomerText(sample, "http://localhost:5011");
            Assert.True(shortMessage.Length <= 160); Assert.Contains(longest.Name, shortMessage);
            Assert.Contains("/Bookings/Details/" + sample.Code, shortMessage);
            var requests = await db.BookingChangeRequests.Include(r => r.Booking).ToListAsync();
            Assert.Equal(5, requests.Count);
            Assert.All(requests, r =>
            {
                Assert.NotNull(r.RequestedByAccountId);
                Assert.NotNull(r.ChangeFee);
                Assert.NotNull(r.DepositCarriedOver);
                Assert.NotNull(r.AdditionalDeposit);
                Assert.NotNull(r.NewTotalAmount);
            });
            var managerId = await db.Employees.Where(e => e.EmployeeCode == "QL001").Select(e => e.Person.Account!.Id).SingleAsync();
            Assert.All(requests.Where(r => r.Status != HomestayBooking.Web.Models.Entities.ChangeRequestStatus.Pending), r => Assert.Equal(managerId, r.DecidedByAccountId));
            var applied = Assert.Single(requests, r => r.Status == HomestayBooking.Web.Models.Entities.ChangeRequestStatus.Applied);
            Assert.Equal(0m, applied.AdditionalDeposit);
            Assert.Equal(0m, applied.ChangeFee);
            Assert.Equal(applied.NewTotalAmount, applied.Booking.TotalAmount);
            Assert.Equal(applied.DepositCarriedOver, applied.Booking.DepositAmount);
            Assert.Equal(HomestayBooking.Web.Models.Entities.BookingStatus.Deposited, applied.Booking.Status);
            Assert.Null(applied.Booking.ConfirmedAt);
            var rejected = Assert.Single(requests, r => r.Status == HomestayBooking.Web.Models.Entities.ChangeRequestStatus.Rejected);
            Assert.False(string.IsNullOrWhiteSpace(rejected.RejectionReason));
            Assert.Equal(rejected.RejectionReason, rejected.DecisionNote);

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
