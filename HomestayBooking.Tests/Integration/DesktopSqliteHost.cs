using HomestayBooking.Tests.Data;
using HomestayBooking.Web.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomestayBooking.Tests.Integration;

/// <summary>Real relational provider for booking transactions/ExecuteUpdate, still using TestWebApplicationFactory's HTTP pipeline.</summary>
internal sealed class DesktopSqliteHost : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TestWebApplicationFactory _root = new();
    public WebApplicationFactory<Program> Host { get; private set; } = null!;

    public static async Task<DesktopSqliteHost> CreateAsync()
    {
        var fixture = new DesktopSqliteHost();
        try
        {
            await fixture._connection.OpenAsync();
            void Database(IServiceCollection services)
            {
                services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(fixture._connection)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
                services.AddScoped<ApplicationDbContext, SqliteTestDbContext>();
            }
            await using (var seed = SeedTestServices.Build(Database))
            {
                await using (var scope = seed.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await db.Database.EnsureCreatedAsync();
                    var history = db.GetService<IHistoryRepository>();
                    await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
                    await using var original = new ApplicationDbContext(scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
                    foreach (var id in original.Database.GetMigrations())
                        await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, ProductInfo.GetVersion())));
                }
                await SeedData.InitializeAsync(seed);
            }
            fixture.Host = fixture._root.WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                Database(services);
            }));
            return fixture;
        }
        catch { fixture.Dispose(); throw; }
    }

    public T WithDb<T>(Func<ApplicationDbContext, T> action)
    {
        using var scope = Host.Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
    public void Dispose() { Host?.Dispose(); _root.Dispose(); _connection.Dispose(); }
}
