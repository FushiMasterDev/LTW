using HomestayBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.DesktopHost;

internal static class DesktopDatabase
{
    // SQLite has its own schema lifecycle. Never mark SQL Server migrations as applied.
    public static async Task InitializeAsync(IServiceProvider services, bool reset)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (reset) await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__DesktopHost" (
                "Id" INTEGER NOT NULL PRIMARY KEY CHECK ("Id" = 1),
                "SchemaVersion" INTEGER NOT NULL
            );
            """);
        var versions = await db.Database.SqlQueryRaw<int>("SELECT SchemaVersion AS Value FROM __DesktopHost").ToListAsync();
        if (versions.Count > 0)
        {
            if (versions.Single() != 1)
                throw new InvalidOperationException("SQLite schema không được hỗ trợ. Sao lưu App_Data trước khi reset dữ liệu.");
            return;
        }
        // Seed and completion marker commit together; interruption rolls back the whole demo dataset.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedData.SeedDevelopmentDataAsync(scope.ServiceProvider);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO __DesktopHost (Id, SchemaVersion) VALUES (1, 1)");
        await transaction.CommitAsync();
    }
}
