using HomestayBooking.Tests.Data;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

/// <summary>Đồng hồ điều khiển được để kiểm tra hạn giữ phòng 15 phút, hạn xác nhận 30 phút.</summary>
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>
/// CSDL SQLite in-memory (thực thi ràng buộc thật) kèm dữ liệu tối thiểu và các service nối với nhau như trong Program.cs.
/// "Bây giờ" = 10:00 thứ Ba 10/11/2026 giờ Việt Nam.
/// </summary>
public sealed class BookingTestHost : IDisposable
{
    public const string FujiAccount = "0901999999";

    private readonly SqliteConnection _connection;

    public TestClock Clock { get; } = new() { Now = new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero) };
    public BookingOptions Options { get; } = new();
    public VoucherOptions VoucherRules { get; } = new();
    public BankAccountOptions Bank { get; } = new() { BankCode = "MB", AccountNumber = FujiAccount, AccountName = "CONG TY FUJI" };
    public ApplicationDbContext Db { get; }

    public Homestay Homestay { get; }
    public RoomType RoomType { get; }
    public Customer Customer { get; }
    public CancellationPolicy GeneralPolicy { get; }
    public CancellationPolicy HomestayHolidayPolicy { get; }

    public BookingTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Db = NewContext();
        Db.Database.EnsureCreated();

        var ownerPerson = new PersonProfile { FullName = "Chủ nhà", PhoneNumber = "+84912000001" };
        Db.Users.Add(new ApplicationUser { Person = ownerPerson, Status = AccountStatus.Active });
        Homestay = new Homestay
        {
            Name = "Tây Hồ Sunset",
            Address = "52 Từ Hoa",
            Status = HomestayStatus.Active,
            Owner = new Owner { Person = ownerPerson },
            Area = new Area { Name = "Tây Hồ", Province = "Hà Nội" },
        };
        Homestay.Contracts.Add(new Contract
        {
            ContractNo = "HD-1", StartDate = new(2026, 6, 1), EndDate = new(2027, 5, 31), CommissionRate = 0.12m,
        });
        RoomType = new RoomType { Homestay = Homestay, Name = "Phòng đôi", Category = new RoomCategory { Name = "Phòng đôi" }, MaxGuests = 2 };
        RoomType.Rooms.Add(new Room { RoomCode = "101" });
        RoomType.Rooms.Add(new Room { RoomCode = "102" });
        // Giá từ 1/1/2026; giá ngày thường tăng lên 550.000đ từ 1/12/2026
        RoomType.Prices.Add(new RoomTypePrice { DayType = DayType.Weekday, Price = 500_000, EffectiveFrom = new(2026, 1, 1) });
        RoomType.Prices.Add(new RoomTypePrice { DayType = DayType.Weekend, Price = 600_000, EffectiveFrom = new(2026, 1, 1) });
        RoomType.Prices.Add(new RoomTypePrice { DayType = DayType.Holiday, Price = 750_000, EffectiveFrom = new(2026, 1, 1) });
        RoomType.Prices.Add(new RoomTypePrice { DayType = DayType.Weekday, Price = 550_000, EffectiveFrom = new(2026, 12, 1) });
        Db.RoomTypes.Add(RoomType);
        Db.Holidays.Add(new Holiday { Date = new(2027, 1, 1), Name = "Tết Dương lịch" });

        GeneralPolicy = new CancellationPolicy
        {
            Name = "Chung", Scope = PolicyScope.All,
            Rules = { new() { MinDaysBefore = 7, RefundPercent = 100 }, new() { MinDaysBefore = 3, RefundPercent = 50 }, new() { MinDaysBefore = 0, RefundPercent = 0 } },
        };
        HomestayHolidayPolicy = new CancellationPolicy
        {
            Homestay = Homestay, Name = "Lễ riêng", Scope = PolicyScope.Holiday,
            Rules = { new() { MinDaysBefore = 14, RefundPercent = 100 }, new() { MinDaysBefore = 0, RefundPercent = 0 } },
        };
        Db.CancellationPolicies.AddRange(GeneralPolicy, HomestayHolidayPolicy);

        var customerPerson = new PersonProfile { FullName = "Nguyễn Văn Minh", PhoneNumber = "+84987000001" };
        Db.Users.Add(new ApplicationUser { Person = customerPerson, Status = AccountStatus.Active });
        Customer = new Customer { Person = customerPerson, Profile = new CustomerProfile() };
        Db.Customers.Add(Customer);

        var consultant = new PersonProfile { FullName = "Nhân viên", PhoneNumber = "+84901000011" };
        Db.Users.Add(new ApplicationUser { Person = consultant, Status = AccountStatus.Active });
        Db.Employees.Add(new Employee { Person = consultant, EmployeeCode = "NV001" });
        Db.SaveChanges();
        Db.ChangeTracker.Clear();
    }

    /// <summary>Context mới trên cùng CSDL — mô phỏng một request khác.</summary>
    public ApplicationDbContext NewContext() =>
        new SqliteTestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);

    public PricingService Pricing(ApplicationDbContext? db = null) => new(db ?? Db, Microsoft.Extensions.Options.Options.Create(Options));

    public RefundService Refunds(ApplicationDbContext? db = null) => new(db ?? Db, Clock);

    public BookingService Bookings(ApplicationDbContext? db = null)
    {
        db ??= Db;
        return new BookingService(db, Pricing(db), Refunds(db), new BookingCodeGenerator(db), new NotificationService(db),
            Clock, Microsoft.Extensions.Options.Options.Create(Options), Vouchers(db));
    }

    public VoucherService Vouchers(ApplicationDbContext? db = null) =>
        new(db ?? Db, new NotificationService(db ?? Db), Clock, Microsoft.Extensions.Options.Options.Create(VoucherRules));

    public ReceiptReviewService Receipts(ApplicationDbContext? db = null)
    {
        db ??= Db;
        return new ReceiptReviewService(db, Bookings(db), new NotificationService(db), Clock,
            Microsoft.Extensions.Options.Options.Create(Options));
    }

    public BankMatchingService Matching(ApplicationDbContext? db = null)
    {
        db ??= Db;
        return new BankMatchingService(db, Bookings(db), Microsoft.Extensions.Options.Options.Create(Bank), Clock);
    }

    public AccountingService Accounting(ApplicationDbContext? db = null)
    {
        db ??= Db;
        return new AccountingService(db, Bookings(db), new NotificationService(db), Clock);
    }

    public CreateBookingRequest Request(DateOnly checkIn, DateOnly checkOut, int guests = 2) =>
        new(Customer.Id, RoomType.Id, checkIn, checkOut, guests, "Nguyễn Văn Minh", "+84987000001", null, SourceChannel.Web,
            new Actor(ActorType.Customer));

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
