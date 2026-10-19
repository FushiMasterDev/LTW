using System.Data;
using System.Diagnostics;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace HomestayBooking.Tests.Services;

/// <summary>Chỉ chạy khi RUN_PERF_TESTS=1 để bộ test thường vẫn nhanh.</summary>
public sealed class PerformanceFactAttribute : FactAttribute
{
    public PerformanceFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_PERF_TESTS") != "1") Skip = "Đặt RUN_PERF_TESTS=1 để đo NFR-02/NFR-05 trên SQL Server.";
    }
}

/// <summary>Các test dùng chung ConstraintTest phải chạy tuần tự: test báo cáo kiểm database rỗng trước khi dựng dữ liệu.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConstraintTestDatabaseCollection { public const string Name = "ConstraintTest SQL Server"; }

/// <summary>NFR-02 (FR-08 ≤ 5 giây) và NFR-05 (tìm kiếm ≤ 3 giây) với dữ liệu cỡ 150 homestay, ~1.500 phòng, một năm lịch.</summary>
[Collection(ConstraintTestDatabaseCollection.Name)]
public sealed class AvailabilityPerformanceTests(ITestOutputHelper output)
{
    private const string TestDatabase = "HomestayBooking_ConstraintTest";
    private const int Homestays = 150, TypesPerHome = 5, RoomsPerType = 2, Days = 365, BlockIdBase = 1_000_000;

    [PerformanceFact]
    public async Task StaffAvailabilityAndDatedSearchStayWithinTargets()
    {
        var configured = new ConfigurationBuilder().AddUserSecrets<ApplicationDbContext>(optional: true).Build().GetConnectionString("Default");
        Assert.False(string.IsNullOrWhiteSpace(configured), "Cần user-secrets của Web để chạy test hiệu năng trên ConstraintTest.");
        var connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = TestDatabase, TrustServerCertificate = true, PersistSecurityInfo = false,
            ApplicationName = nameof(AvailabilityPerformanceTests),
        };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection.ConnectionString, o => o.CommandTimeout(300)).Options);
        await db.Database.OpenConnectionAsync();
        Assert.Equal(TestDatabase, db.Database.GetDbConnection().Database);
        Assert.False(await db.Homestays.AnyAsync(), "ConstraintTest phải rỗng trước khi đo.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var clock = TimeProvider.System; var today = VietnamTime.Today(clock);
            var consultant = new Employee
            {
                EmployeeCode = "PERF-NV", Position = EmployeePosition.Consultant,
                Person = new PersonProfile { FullName = "Đo hiệu năng", PhoneNumber = "+84901000990", Account = new ApplicationUser { Status = AccountStatus.Active } },
            };
            var category = new RoomCategory { Name = "Loại đo hiệu năng" };
            var areas = Enumerable.Range(1, 10).Select(i => new Area { Name = $"Khu đo {i}", Province = "Hà Nội", AreaGroup = AreaGroup.InnerCity }).ToList();
            var owner = new Owner { Person = new PersonProfile { FullName = "Chủ đo", PhoneNumber = "+84901000991" } };
            var rooms = new List<Room>();
            for (var h = 0; h < Homestays; h++)
            {
                var home = new Homestay { Name = $"Homestay đo {h:000}", Address = "Hà Nội", Status = HomestayStatus.Active, Owner = owner, Area = areas[h % areas.Count] };
                home.Contracts.Add(new Contract { ContractNo = $"PERF-{h:000}", StartDate = today.AddDays(-30), EndDate = today.AddYears(2), CommissionRate = .12m });
                for (var t = 0; t < TypesPerHome; t++)
                {
                    var type = new RoomType { Homestay = home, Name = $"Loại {t}", MaxGuests = 2 + t % 3, Category = category };
                    foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new RoomTypePrice { DayType = day, Price = 400_000 + t * 100_000, EffectiveFrom = today.AddDays(-30) });
                    for (var r = 0; r < RoomsPerType; r++) { var room = new Room { RoomCode = $"P{t}{r}" }; type.Rooms.Add(room); rooms.Add(room); }
                    db.RoomTypes.Add(type);
                }
            }
            db.Employees.Add(consultant);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // ~60% lấp đầy: mỗi phòng lặp khối 3 đêm bận / 2 đêm trống, lệch pha theo phòng để không trùng ngày.
            var blocks = new DataTable(); foreach (var (name, type) in new[] { ("Id", typeof(int)), ("RoomId", typeof(int)), ("StartDate", typeof(DateTime)),
                ("EndDate", typeof(DateTime)), ("Reason", typeof(string)), ("CreatedByAccountId", typeof(string)), ("CreatedAt", typeof(DateTime)) }) blocks.Columns.Add(name, type);
            var nights = new DataTable(); nights.Columns.Add("RoomId", typeof(int)); nights.Columns.Add("Date", typeof(DateTime)); nights.Columns.Add("RoomBlockId", typeof(int));
            var blockId = BlockIdBase; var now = DateTime.UtcNow;
            foreach (var (room, index) in rooms.Select((r, i) => (r, i)))
                for (var d = index % 5; d + 3 <= Days; d += 5)
                {
                    var start = today.AddDays(d); blockId++;
                    blocks.Rows.Add(blockId, room.Id, start.ToDateTime(TimeOnly.MinValue), start.AddDays(3).ToDateTime(TimeOnly.MinValue), "OtherChannel", consultant.Person.Account!.Id, now);
                    for (var n = 0; n < 3; n++) nights.Rows.Add(room.Id, start.AddDays(n).ToDateTime(TimeOnly.MinValue), blockId);
                }
            var sql = (SqlConnection)db.Database.GetDbConnection(); var tx = (SqlTransaction)transaction.GetDbTransaction();
            await BulkAsync(sql, tx, "dbo.RoomBlocks", blocks, SqlBulkCopyOptions.KeepIdentity);
            await BulkAsync(sql, tx, "dbo.RoomNights", nights, SqlBulkCopyOptions.Default);
            output.WriteLine($"Dữ liệu: {Homestays} homestay, {rooms.Count} phòng, {blocks.Rows.Count} khóa, {nights.Rows.Count} đêm bận.");

            var actor = new Actor(ActorType.Employee, consultant.Person.Account!.Id, consultant.Id);
            var availability = new AvailabilityService(db, clock, Options.Create(new BookingOptions()));
            var checkIn = today.AddDays(60);
            var staff = await WorstOfThreeAsync(() => availability.StaffAvailabilityAsync(checkIn, checkIn.AddDays(31), null, 2, actor));
            var matrix = await WorstOfThreeAsync(() => availability.StaffNightAvailabilityAsync(checkIn, checkIn.AddDays(31), null, 2, actor));
            Assert.Equal(Homestays * TypesPerHome * 31, (await availability.StaffNightAvailabilityAsync(checkIn, checkIn.AddDays(31), null, 2, actor)).Count);
            output.WriteLine($"B9 StaffNightAvailabilityAsync 150 cơ sở × 31 đêm: chậm nhất {matrix.TotalMilliseconds:N0} ms (3 lần).");
            Assert.True(matrix < TimeSpan.FromSeconds(5), $"Ma trận mất {matrix.TotalMilliseconds:N0} ms.");
            var search = await WorstOfThreeAsync(() => availability.SearchAsync(new HomestaySearchQuery { CheckIn = checkIn, CheckOut = checkIn.AddDays(3), Guests = 2 }));
            output.WriteLine($"FR-08 StaffAvailabilityAsync 31 đêm: chậm nhất {staff.TotalMilliseconds:N0} ms (NFR-02 ≤ 5.000 ms).");
            output.WriteLine($"FR-01 SearchAsync có ngày: chậm nhất {search.TotalMilliseconds:N0} ms (NFR-05 ≤ 3.000 ms).");
            Assert.True(staff < TimeSpan.FromSeconds(5), $"FR-08 mất {staff.TotalMilliseconds:N0} ms.");
            Assert.True(search < TimeSpan.FromSeconds(3), $"Tìm kiếm mất {search.TotalMilliseconds:N0} ms.");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task BulkAsync(SqlConnection connection, SqlTransaction transaction, string table, DataTable rows, SqlBulkCopyOptions options)
    {
        using var copy = new SqlBulkCopy(connection, options, transaction) { DestinationTableName = table, BulkCopyTimeout = 300, BatchSize = 20_000 };
        foreach (DataColumn column in rows.Columns) copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        await copy.WriteToServerAsync(rows);
    }

    private static async Task<TimeSpan> WorstOfThreeAsync<T>(Func<Task<T>> run)
    {
        var worst = TimeSpan.Zero;
        for (var i = 0; i < 3; i++)
        {
            var watch = Stopwatch.StartNew(); await run(); watch.Stop();
            if (watch.Elapsed > worst) worst = watch.Elapsed;
        }
        return worst;
    }
}
