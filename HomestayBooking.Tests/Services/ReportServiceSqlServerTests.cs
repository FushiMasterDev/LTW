using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace HomestayBooking.Tests.Services;

/// <summary>Kiểm tra bản dịch truy vấn thật trên SQL Server; không migrate/seed và luôn rollback dữ liệu thử.</summary>
[Collection(ConstraintTestDatabaseCollection.Name)]
public sealed class ReportServiceSqlServerTests(ITestOutputHelper output)
{
    private static string TestDatabase
    {
        get
        {
            var name = Environment.GetEnvironmentVariable("FUJI_SQL_TEST_DATABASE") ?? "HomestayBooking_ConstraintTest";
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^HomestayBooking_(ConstraintTest|SelfHostTest_[A-Za-z0-9_]+)$"))
                throw new InvalidOperationException("Tên database SQL test không được phép.");
            return name;
        }
    }

    [Fact]
    public async Task ReportsTranslateOnSqlServerWithExpectedMoneyConfirmationTimesAndVietnamMonths()
    {
        // Chỉ mượn thông tin máy chủ/xác thực từ user-secrets của Web, thay database TRƯỚC khi kết nối.
        // Không đọc appsettings hay gọi Program/SeedData; không in connection string hoặc mật khẩu.
        var configuration = new ConfigurationBuilder().AddUserSecrets<ApplicationDbContext>(optional: true).Build();
        var configured = configuration.GetConnectionString("Default");
        Assert.False(string.IsNullOrWhiteSpace(configured), "Cần user-secrets của Web để chạy test SQL Server trên ConstraintTest.");
        var connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = TestDatabase,
            TrustServerCertificate = true,
            PersistSecurityInfo = false,
            ApplicationName = nameof(ReportServiceSqlServerTests),
        };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        Assert.True(db.Database.IsSqlServer());
        Assert.Equal(TestDatabase, db.Database.GetDbConnection().Database);

        var before = await SnapshotAsync(db.Database.GetDbConnection());
        Assert.All(before.Where(t => t.Key != "dbo.__EFMigrationsHistory"), t => Assert.Equal(0L, t.Value.Rows));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var manager = Employee("B6SQL-MANAGER", EmployeePosition.Manager, "+84901000901");
                var consultant = Employee("B6SQL-CONSULTANT", EmployeePosition.Consultant, "+84901000902");
                var homestay = new Homestay
                {
                    Name = "Homestay kiểm tra báo cáo SQL Server", Address = "Hà Nội", Status = HomestayStatus.Active,
                    Area = new Area { Name = "Khu vực báo cáo", Province = "Hà Nội", AreaGroup = AreaGroup.InnerCity },
                    Owner = new Owner { Person = new PersonProfile { FullName = "Chủ thử", PhoneNumber = "+84901000903" } },
                };
                var room = new Room
                {
                    RoomCode = "SQL-101", RoomType = new RoomType
                    {
                        Homestay = homestay, Name = "Phòng thử", MaxGuests = 2,
                        Category = new RoomCategory { Name = "Loại phòng báo cáo" },
                    },
                };
                var customer = new Customer { Person = new PersonProfile { FullName = "Khách thử", PhoneNumber = "+84901000904" } };
                var policy = new CancellationPolicy { Name = "Chính sách báo cáo", Scope = PolicyScope.All };
                db.AddRange(manager, consultant, room, customer, policy);
                await db.SaveChangesAsync();

                Booking Booking(string code, BookingStatus status, decimal total, DateOnly checkIn, Employee creator, Employee assignee) => new()
                {
                    Code = code, Status = status, CustomerId = customer.Id, ContactName = "Khách thử", ContactPhone = customer.Person.PhoneNumber,
                    HomestayId = homestay.Id, RoomTypeId = room.RoomTypeId, RoomId = room.Id, Guests = 2,
                    CheckIn = checkIn, CheckOut = checkIn.AddDays(1), TotalAmount = total, DepositRate = .30m, DepositAmount = total * .30m,
                    CommissionRate = .12m, CancellationPolicyId = policy.Id, CreatedByEmployeeId = creator.Id, AssignedEmployeeId = assignee.Id,
                    CreatedAt = Utc(9, 30, 17), UpdatedAt = Utc(10, 1), HoldExpiresAt = Utc(9, 30, 17).AddMinutes(15),
                    Nights = [new BookingNight { Date = checkIn, DayType = DayType.Weekday, Price = total }],
                };

                var completed = Booking("B6SQL-COMPLETED", BookingStatus.Completed, 2_000_000, new(2026, 10, 9), consultant, manager);
                completed.ChangeFeeAmount = 300_000;
                completed.DepositedAt = Utc(10, 1);
                completed.ConfirmedAt = completed.DepositedAt.Value.AddMinutes(19.5);
                completed.CompletedAt = Utc(10, 10);
                completed.RoomNights.Add(new RoomNight { RoomId = room.Id, Date = completed.CheckIn });
                var cancelled = Booking("B6SQL-CANCELLED", BookingStatus.Cancelled, 1_000_000, new(2026, 10, 11), consultant, consultant);
                cancelled.SourceChannel = SourceChannel.Phone;
                cancelled.CreatedAt = Utc(10, 10);
                cancelled.DepositedAt = Utc(10, 10);
                cancelled.CancelledAt = Utc(10, 11);
                cancelled.RefundAmount = 150_000;
                var replaced = Booking("B6SQL-REPLACED", BookingStatus.Cancelled, 3_000_000, completed.CheckIn, manager, manager);
                replaced.DepositedAt = Utc(9, 30, 16, 59);
                // 17:00 UTC ngày 30/09 là 00:00 ngày 01/10 ở VN, phải thuộc tháng 10.
                replaced.CancelledAt = Utc(9, 30, 17);
                replaced.ReplacedByBooking = completed;
                replaced.RefundAmount = 0;
                var confirmed = Booking("B6SQL-CONFIRMED", BookingStatus.Confirmed, 4_000_000, new(2026, 10, 31), consultant, manager);
                confirmed.CreatedAt = Utc(10, 31, 17);
                confirmed.DepositedAt = Utc(10, 31, 17);
                confirmed.ConfirmedAt = confirmed.DepositedAt.Value.AddMinutes(40.5);
                confirmed.RoomNights.Add(new RoomNight { RoomId = room.Id, Date = confirmed.CheckIn });
                db.Bookings.AddRange(completed, cancelled, replaced, confirmed);
                await db.SaveChangesAsync();

                db.Reviews.Add(new Review { BookingId = completed.Id, HomestayId = homestay.Id, Rating = 4, CreatedAt = Utc(10, 11) });
                db.AuditLogs.Add(new AuditLog
                {
                    TableName = "Bookings", RecordId = completed.Id.ToString(CultureInfo.InvariantCulture), AccountId = manager.Person.Account!.Id,
                    Action = AuditAction.Update, NewValues = JsonSerializer.Serialize(new { Values = new { Step = "OverbookingReported" } }),
                    // 17:00 UTC ngày 31/10 là 00:00 ngày 01/11 ở VN, phải thuộc tháng 11.
                    CreatedAt = Utc(10, 31, 17),
                });
                db.Payments.AddRange(new Payment
                {
                    BookingId = completed.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
                    Status = PaymentStatus.Approved, Amount = 250_000, BankRef = "B6SQL-PAID", ReviewedByEmployeeId = manager.Id,
                    SubmittedAt = Utc(10, 31, 16), ReviewedAt = Utc(10, 31, 17),
                }, new Payment
                {
                    BookingId = completed.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
                    Status = PaymentStatus.Approved, Amount = 100_000, BankRef = "B6SQL-OUTSIDE", ReviewedByEmployeeId = manager.Id,
                    SubmittedAt = Utc(11, 1, 16), ReviewedAt = Utc(11, 1, 17),
                });
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                var actor = new Actor(ActorType.Employee, manager.Person.Account.Id, EmployeeId: manager.Id);
                var service = new ReportService(db);
                var query = new ReportQuery(new(2026, 10, 1), new(2026, 11, 1), homestay.Id, homestay.AreaId);
                var management = await service.ManagementAsync(query, actor);
                var financial = await service.FinancialAsync(query, actor);
                var employees = await service.EmployeesAsync(query, actor);
                var homes = await service.HomestaysAsync(query, actor);

                Assert.Equal(4, management.Bookings.Total);
                Assert.Equal(3, management.Bookings.DepositedCohort);
                Assert.Equal(1, management.Bookings.CustomerCancelled);
                Assert.Equal(100d / 3, management.Bookings.CancellationPercent, 8);
                Assert.Equal(0, management.Bookings.Rejected);
                Assert.Equal(0, management.Bookings.Expired);
                Assert.Contains(management.Bookings.Breakdown, b => b.Status == BookingStatus.Cancelled && b.Channel == SourceChannel.Phone && b.Count == 1);
                Assert.Equal(new FinancialReport(2_000_000, 1_050_000, 396_000, 396_000, 250_000), financial);
                Assert.Equal(financial, management.Finance);
                Assert.Equal(2, employees.Count);
                Assert.Equal(management.Employees.OrderBy(e => e.EmployeeId).ToArray(), employees.OrderBy(e => e.EmployeeId).ToArray());
                var consultantReport = Assert.Single(employees, e => e.EmployeeId == consultant.Id);
                Assert.Equal(3, consultantReport.Created);
                Assert.Equal(2, consultantReport.Confirmed);
                Assert.Equal(200d / 3, consultantReport.ConversionPercent, 8);
                Assert.Equal(2_000_000, consultantReport.CompletedRoomAmount);
                Assert.Equal(0, consultantReport.AssignedOpen);
                Assert.Equal(1, consultantReport.AssignedFinished);
                var managerReport = Assert.Single(employees, e => e.EmployeeId == manager.Id);
                Assert.Equal(1, managerReport.Created);
                Assert.Equal(0, managerReport.Confirmed);
                Assert.Equal(0, managerReport.CompletedRoomAmount);
                Assert.Equal(1, managerReport.AssignedOpen);
                Assert.Equal(2, managerReport.AssignedFinished);

                void AssertHome(HomestayReport home)
                {
                    Assert.Equal(4, home.Rating);
                    Assert.Equal(1, home.ReviewCount);
                    Assert.Equal(30, home.AverageConfirmMinutes!.Value, 8);
                    Assert.Equal(50, home.ConfirmWithinThirtyPercent);
                    Assert.Equal(new[] { new HomestayIncidentMonth(homestay.Id, 2026, 10, 0, 1),
                        new HomestayIncidentMonth(homestay.Id, 2026, 11, 1, 0) }, home.Incidents.ToArray());
                }
                AssertHome(Assert.Single(homes));
                AssertHome(Assert.Single(management.Homestays));
                var october = Assert.Single(await service.HomestaysAsync(query with { To = new(2026, 10, 31) }, actor));
                Assert.Equal(19.5, october.AverageConfirmMinutes!.Value, 8);
                Assert.Equal(100, october.ConfirmWithinThirtyPercent);
                Assert.Equal(new HomestayIncidentMonth(homestay.Id, 2026, 10, 0, 1), Assert.Single(october.Incidents));
                var november = Assert.Single(await service.HomestaysAsync(query with { From = new(2026, 11, 1) }, actor));
                Assert.Equal(40.5, november.AverageConfirmMinutes!.Value, 8);
                Assert.Equal(0, november.ConfirmWithinThirtyPercent);
                Assert.Equal(new HomestayIncidentMonth(homestay.Id, 2026, 11, 1, 0), Assert.Single(november.Incidents));
                var consultantActor = new Actor(ActorType.Employee, consultant.Person.Account!.Id, EmployeeId: consultant.Id);
                Assert.Equal(consultantReport, Assert.Single(await service.EmployeesAsync(query, consultantActor)));
                Assert.Equal(0, (await service.FinancialAsync(query with { AreaId = -1 }, actor)).Retained);
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }
        finally
        {
            // Vẫn đối chiếu sau rollback nếu truy vấn hoặc assertion thất bại giữa chừng.
            var after = await SnapshotAsync(db.Database.GetDbConnection());
            Assert.Equal(before.OrderBy(t => t.Key).ToArray(), after.OrderBy(t => t.Key).ToArray());
            output.WriteLine($"{TestDatabase}: {after.Count}/{before.Count} bảng khớp COUNT_BIG và CHECKSUM_AGG sau ROLLBACK.");
            foreach (var (table, snapshot) in after.OrderBy(t => t.Key))
                output.WriteLine($"{table}: rows={snapshot.Rows}; checksum={snapshot.Checksum?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}");
        }
    }

    private static Employee Employee(string code, EmployeePosition position, string phone) => new()
    {
        EmployeeCode = code, Position = position, EmploymentStatus = EmploymentStatus.Active,
        Person = new PersonProfile
        {
            FullName = code, PhoneNumber = phone,
            Account = new ApplicationUser { Status = AccountStatus.Active },
        },
    };

    private static DateTime Utc(int month, int day, int hour = 3, int minute = 0) => new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    private sealed record TableSnapshot(long Rows, int? Checksum);

    private static async Task<Dictionary<string, TableSnapshot>> SnapshotAsync(DbConnection connection)
    {
        var tables = new List<(string Name, string Quoted)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT s.name + '.' + t.name, QUOTENAME(s.name) + '.' + QUOTENAME(t.name) FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE t.is_ms_shipped = 0 ORDER BY s.name, t.name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
        }
        var result = new Dictionary<string, TableSnapshot>();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM {table.Quoted}";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            result.Add(table.Name, new(reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt32(1)));
        }
        return result;
    }
}
