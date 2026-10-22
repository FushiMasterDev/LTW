using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Data;

/// <summary>
/// Dữ liệu nền: role, danh mục, ngày lễ và chính sách hủy chung ở mọi môi trường.
/// Nhân viên, chủ, khách, homestay và dữ liệu nghiệp vụ mẫu chỉ tạo ở Development.
/// Chạy lại nhiều lần không bị nhân đôi: mỗi phần chỉ thêm khi chưa có.
/// </summary>
public static partial class SeedData
{
    /// <summary>Mật khẩu chung của các tài khoản mẫu (ghi trong README để demo).</summary>
    public const string DefaultPassword = "Fuji@2026";

    /// <summary>Nhân viên Fuji: đăng nhập bằng mã nhân viên (v5 mục 8.2).</summary>
    public static readonly (string Code, string FullName, string Phone, string Email, EmployeePosition Position, string Department)[] EmployeeData =
    [
        ("AD001", "Quản trị viên Fuji", "0901000001", "admin@fuji.vn", EmployeePosition.Administrator, "Kỹ thuật"),
        ("QL001", "Nguyễn Thị Hương", "0901000002", "huong.nt@fuji.vn", EmployeePosition.Manager, "Ban giám đốc"),
        ("KT001", "Phạm Thu Trang", "0901000003", "ketoan@fuji.vn", EmployeePosition.Accountant, "Kế toán"),
        ("NV001", "Nguyễn Thị Lan", "0901000011", "lan.nt@fuji.vn", EmployeePosition.Consultant, "Tư vấn đặt phòng"),
        ("NV002", "Trần Văn Minh", "0901000012", "minh.tv@fuji.vn", EmployeePosition.Consultant, "Tư vấn đặt phòng"),
    ];

    /// <summary>Chủ homestay: đăng nhập bằng SĐT. Người cuối có hồ sơ đang chờ duyệt để demo quyền hạn chế.</summary>
    public static readonly (string FullName, string Phone, OwnerRegistrationStatus Status)[] OwnerData =
    [
        ("Lê Văn Hùng", "0912000001", OwnerRegistrationStatus.Approved),
        ("Phạm Thu Hà", "0912000002", OwnerRegistrationStatus.Approved),
        ("Đỗ Quang Huy", "0912000003", OwnerRegistrationStatus.Approved),
        ("Vũ Minh Anh", "0912000004", OwnerRegistrationStatus.Approved),
        ("Hoàng Thị Mai", "0912000005", OwnerRegistrationStatus.Approved),
        ("Ngô Đức Thắng", "0912000006", OwnerRegistrationStatus.PendingReview),
    ];

    /// <summary>
    /// Khách hàng. HasAccount = false: khách do nhân viên tạo qua điện thoại, chưa đăng ký (v5 mục 5.2).
    /// Owner đầu tiên cũng là khách hàng để minh họa một Account có nhiều vai trò (v5 mục 1.2).
    /// </summary>
    public static readonly (string FullName, string Phone, bool HasAccount)[] CustomerData =
    [
        ("Nguyễn Văn Minh", "0987000001", true),
        ("Trần Thị Bích", "0987000002", false),
        ("Lê Văn Hùng", "0912000001", true),
        // Khách mẫu cho đơn đặt phòng mẫu (SeedData.Bookings.cs)
        ("Đặng Thị Thu", "0987000003", true),
        ("Bùi Quốc Bảo", "0987000004", true),
        ("Phan Ngọc Ánh", "0987000005", true),
        ("Trịnh Văn Long", "0987000006", false),
        ("Võ Thị Hồng", "0987000007", true),
        ("Hồ Minh Khang", "0987000008", false),
        ("Lý Thanh Tâm", "0987000009", true),
        ("Mai Đức Anh", "0987000010", true),
        ("Chu Thị Ngân", "0987000011", false),
        ("Tạ Quang Vinh", "0987000012", true),
        ("Đinh Thị Yến", "0987000013", true),
        ("Kiều Văn Phúc", "0987000014", false),
    ];

    // Khu vực Hà Nội; Sóc Sơn, Ba Vì là ngoại thành
    private static readonly (string Name, AreaGroup Group)[] AreaData =
    [
        ("Hoàn Kiếm", AreaGroup.InnerCity),
        ("Ba Đình", AreaGroup.InnerCity),
        ("Tây Hồ", AreaGroup.InnerCity),
        ("Đống Đa", AreaGroup.InnerCity),
        ("Long Biên", AreaGroup.InnerCity),
        ("Sóc Sơn", AreaGroup.Suburb),
        ("Ba Vì", AreaGroup.Suburb),
    ];

    private const string Province = "Hà Nội";

    private static readonly string[] RoomCategoryNames =
        ["Phòng đơn", "Phòng đôi", "Phòng gia đình", "Nguyên căn", "Phòng tập thể"];

    /// <summary>Ngày bắt đầu áp bảng giá mẫu và hợp đồng mẫu.</summary>
    private static readonly DateOnly PriceEffectiveFrom = new(2026, 1, 1);
    private static readonly DateOnly ContractStart = new(2026, 6, 1);

    private static readonly (string Name, string Icon)[] AmenityData =
    [
        ("Wifi", "bi-wifi"),
        ("Điều hòa", "bi-snow"),
        ("Bể bơi", "bi-water"),
        ("Khu BBQ", "bi-fire"),
        ("Bãi đỗ xe", "bi-p-circle"),
        ("Bữa sáng", "bi-cup-hot"),
        ("Bếp riêng", "bi-egg-fried"),
        ("Cho phép thú cưng", "bi-heart"),
        ("Ban công", "bi-door-open"),
        ("Máy giặt", "bi-droplet"),
    ];

    // Ngày lễ dương lịch và Tết Nguyên đán. Admin kiểm tra và bổ sung Giỗ Tổ 2027/2028, không tự đoán lịch âm.
    private static readonly (DateOnly Date, string Name)[] HolidayData =
    [
        (new(2026, 1, 1), "Tết Dương lịch"),
        (new(2026, 2, 16), "Tết Nguyên đán"),
        (new(2026, 2, 17), "Tết Nguyên đán"),
        (new(2026, 2, 18), "Tết Nguyên đán"),
        (new(2026, 2, 19), "Tết Nguyên đán"),
        (new(2026, 2, 20), "Tết Nguyên đán"),
        (new(2026, 4, 26), "Giỗ Tổ Hùng Vương"),
        (new(2026, 4, 30), "Ngày Giải phóng miền Nam"),
        (new(2026, 5, 1), "Quốc tế Lao động"),
        (new(2026, 9, 1), "Quốc khánh"),
        (new(2026, 9, 2), "Quốc khánh"),
        (new(2027, 1, 1), "Tết Dương lịch"),
        (new(2027, 2, 5), "Tết Nguyên đán"),
        (new(2027, 2, 6), "Tết Nguyên đán"),
        (new(2027, 2, 7), "Tết Nguyên đán"),
        (new(2027, 2, 8), "Tết Nguyên đán"),
        (new(2027, 2, 9), "Tết Nguyên đán"),
        (new(2027, 4, 30), "Ngày Giải phóng miền Nam"),
        (new(2027, 5, 1), "Quốc tế Lao động"),
        (new(2027, 9, 2), "Quốc khánh"),
        (new(2028, 1, 1), "Tết Dương lịch"),
        (new(2028, 1, 25), "Tết Nguyên đán"),
        (new(2028, 1, 26), "Tết Nguyên đán"),
        (new(2028, 1, 27), "Tết Nguyên đán"),
        (new(2028, 1, 28), "Tết Nguyên đán"),
        (new(2028, 1, 29), "Tết Nguyên đán"),
        (new(2028, 4, 30), "Ngày Giải phóng miền Nam"),
        (new(2028, 5, 1), "Quốc tế Lao động"),
        (new(2028, 9, 2), "Quốc khánh"),
    ];

    // 5 homestay cho mỗi khu vực, theo thứ tự AreaData
    private static readonly (string Name, string Address)[] HomestayData =
    [
        ("Phố Cổ Corner House", "12 Hàng Bạc, Hoàn Kiếm, Hà Nội"),
        ("Hồ Gươm Little Home", "35 Lò Sũ, Hoàn Kiếm, Hà Nội"),
        ("Tạ Hiện Night Stay", "8 Tạ Hiện, Hoàn Kiếm, Hà Nội"),
        ("Hàng Gai Heritage", "61 Hàng Gai, Hoàn Kiếm, Hà Nội"),
        ("Old Quarter Loft", "20 Mã Mây, Hoàn Kiếm, Hà Nội"),
        ("Ba Đình Green Villa", "15 Đội Cấn, Ba Đình, Hà Nội"),
        ("Liễu Giai Sky Home", "102 Liễu Giai, Ba Đình, Hà Nội"),
        ("Trúc Bạch Lakeside", "7 Trúc Bạch, Ba Đình, Hà Nội"),
        ("Kim Mã Cozy Nest", "45 Kim Mã, Ba Đình, Hà Nội"),
        ("Ngọc Hà Garden", "28 Ngọc Hà, Ba Đình, Hà Nội"),
        ("Tây Hồ Sunset Homestay", "52 Từ Hoa, Tây Hồ, Hà Nội"),
        ("Quảng An Lotus House", "18 Quảng An, Tây Hồ, Hà Nội"),
        ("Xuân Diệu Westlake Studio", "33 Xuân Diệu, Tây Hồ, Hà Nội"),
        ("Nhật Chiêu Hideaway", "9 Nhật Chiêu, Tây Hồ, Hà Nội"),
        ("Yên Phụ Riverside", "140 Yên Phụ, Tây Hồ, Hà Nội"),
        ("Văn Miếu Scholar Home", "25 Quốc Tử Giám, Đống Đa, Hà Nội"),
        ("Hoàng Cầu Lake View", "68 Hoàng Cầu, Đống Đa, Hà Nội"),
        ("Thái Hà Urban Nest", "120 Thái Hà, Đống Đa, Hà Nội"),
        ("Khâm Thiên Retro House", "77 Khâm Thiên, Đống Đa, Hà Nội"),
        ("Láng Hạ Family Stay", "40 Láng Hạ, Đống Đa, Hà Nội"),
        ("Long Biên Bridge House", "6 Ngọc Thụy, Long Biên, Hà Nội"),
        ("Bồ Đề Garden Home", "88 Bồ Đề, Long Biên, Hà Nội"),
        ("Vinhomes Riverside Suite", "Hoa Lan 3, Phúc Lợi, Long Biên, Hà Nội"),
        ("Gia Thụy Quiet Corner", "15 Gia Thụy, Long Biên, Hà Nội"),
        ("Sài Đồng Family Villa", "52 Sài Đồng, Long Biên, Hà Nội"),
        ("Sóc Sơn Pine Hill", "Thôn Đền Gióng, Phù Linh, Sóc Sơn, Hà Nội"),
        ("Hàm Lợn Lake Camp", "Hồ Hàm Lợn, Nam Sơn, Sóc Sơn, Hà Nội"),
        ("Đồng Đò Forest Villa", "Hồ Đồng Đò, Minh Trí, Sóc Sơn, Hà Nội"),
        ("Minh Phú Garden Retreat", "Xã Minh Phú, Sóc Sơn, Hà Nội"),
        ("Sóc Sơn Hilltop House", "Xã Bắc Sơn, Sóc Sơn, Hà Nội"),
        ("Ba Vì Mountain Lodge", "Xã Ba Vì, Ba Vì, Hà Nội"),
        ("Tản Viên Cloud Villa", "Xã Tản Lĩnh, Ba Vì, Hà Nội"),
        ("Suối Ngọc Bamboo House", "Xã Vân Hòa, Ba Vì, Hà Nội"),
        ("Đá Chông Stone Villa", "Xã Minh Quang, Ba Vì, Hà Nội"),
        ("Khoang Xanh Eco Stay", "Xã Vân Hòa, Ba Vì, Hà Nội"),
    ];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Database.IsRelational())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            if (options.AutoMigrate)
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
                if (pending.Length > 0)
                {
                    var names = string.Join(", ", pending);
                    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("HomestayBooking.Web.Data.SeedData");
                    logger.LogError("Database:AutoMigrate đang tắt nhưng còn migration chưa áp dụng: {Migrations}", names);
                    throw new InvalidOperationException(
                        $"Database:AutoMigrate đang tắt nhưng còn migration chưa áp dụng: {names}. Hãy triển khai schema trước khi khởi động.");
                }
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var environment = scope.ServiceProvider.GetService<IWebHostEnvironment>();
        await SeedAsync(scope.ServiceProvider, environment?.IsDevelopment() == true);
    }

    /// <summary>Seed demo đầy đủ vào schema đã tạo. Bên gọi quản lý scope/transaction; không áp migration SQL Server.</summary>
    public static Task SeedDevelopmentDataAsync(IServiceProvider scopedServices) => SeedAsync(scopedServices, includeDemo: true);

    private static async Task SeedAsync(IServiceProvider services, bool includeDemo)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedRolesAsync(roleManager);
        await SeedCategoriesAsync(db);
        if (!includeDemo) return;
        await SeedPeopleAsync(db, userManager);
        await SeedHomestaysAsync(db);
        await SeedSampleBookingsAsync(db, services);
        await SeedOperationsAsync(db, services);
        await SeedDemoImagesAsync(db, services);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await EnsureSucceeded(roleManager.CreateAsync(new IdentityRole(role)), $"role {role}");
            }
        }
    }

    /// <summary>
    /// Tạo Person Profile, hồ sơ nghiệp vụ, Account và role theo v5. Mỗi bước kiểm tra trước nên chạy lại được
    /// kể cả khi lần trước dừng giữa chừng.
    /// </summary>
    private static async Task SeedPeopleAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        foreach (var e in EmployeeData)
        {
            var person = await EnsurePersonAsync(db, e.FullName, e.Phone, e.Email);
            person.Employee ??= new Employee
            {
                EmployeeCode = e.Code,
                Position = e.Position,
                Department = e.Department,
            };
            await db.SaveChangesAsync();
            await EnsureAccountAsync(userManager, person, AppRoles.Employee);
        }

        var reviewer = await db.Employees.SingleAsync(x => x.EmployeeCode == "QL001");
        foreach (var o in OwnerData)
        {
            var person = await EnsurePersonAsync(db, o.FullName, o.Phone, email: null);
            if (person.Owner is null)
            {
                var now = DateTime.UtcNow;
                var approved = o.Status == OwnerRegistrationStatus.Approved;
                // Owner đã duyệt phải có tài khoản nhận tiền đối soát
                person.Owner = approved
                    ? new Owner
                    {
                        BankName = "MB Bank",
                        BankAccountNo = "0" + o.Phone[1..],
                        BankAccountName = o.FullName.ToUpperInvariant(),
                    }
                    : new Owner();
                person.Owner.Registrations.Add(new OwnerRegistration
                {
                    Source = OwnerRegistrationSource.SelfService,
                    Status = o.Status,
                    SubmittedAt = now,
                    ReviewedByEmployeeId = approved ? reviewer.Id : null,
                    ReviewedAt = approved ? now : null,
                });
                await db.SaveChangesAsync();
            }
            await EnsureAccountAsync(userManager, person, AppRoles.Owner);
        }

        foreach (var c in CustomerData)
        {
            var person = await EnsurePersonAsync(db, c.FullName, c.Phone, email: null);
            // Customer luôn được tạo cùng Customer Profile (v5 mục 4.5)
            person.Customer ??= new Customer { Profile = new CustomerProfile() };
            await db.SaveChangesAsync();
            if (c.HasAccount)
            {
                await EnsureAccountAsync(userManager, person, AppRoles.Customer);
            }
        }
    }

    private static async Task<PersonProfile> EnsurePersonAsync(ApplicationDbContext db, string fullName, string phone, string? email)
    {
        var e164 = PhoneNumberNormalizer.ToE164(phone)
                   ?? throw new InvalidOperationException($"Seed: số điện thoại không hợp lệ {phone}.");
        var person = await db.PersonProfiles
            .Include(p => p.Customer)
            .Include(p => p.Owner)
            .Include(p => p.Employee)
            .SingleOrDefaultAsync(p => p.PhoneNumber == e164);
        if (person is null)
        {
            person = new PersonProfile { FullName = fullName, PhoneNumber = e164, Email = email };
            db.PersonProfiles.Add(person);
            await db.SaveChangesAsync();
        }
        return person;
    }

    /// <summary>Một Person chỉ có một Account; thêm vai trò vào Account sẵn có thay vì tạo Account mới (v5 mục 5.3).</summary>
    private static async Task EnsureAccountAsync(UserManager<ApplicationUser> userManager, PersonProfile person, string role)
    {
        var account = await userManager.Users.SingleOrDefaultAsync(u => u.PersonId == person.Id);
        if (account is null)
        {
            account = new ApplicationUser
            {
                PersonId = person.Id,
                Status = AccountStatus.Active,
                PasswordChangedAt = DateTime.UtcNow,
            };
            await EnsureSucceeded(userManager.CreateAsync(account, DefaultPassword), $"account {person.PhoneNumber}");
        }

        if (!await userManager.IsInRoleAsync(account, role))
        {
            await EnsureSucceeded(userManager.AddToRoleAsync(account, role), $"role {role} for {person.PhoneNumber}");
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        if (!await db.Areas.AnyAsync())
        {
            db.Areas.AddRange(AreaData.Select(a => new Area { Name = a.Name, Province = Province, AreaGroup = a.Group }));
        }

        if (!await db.RoomCategories.AnyAsync())
        {
            db.RoomCategories.AddRange(RoomCategoryNames.Select(n => new RoomCategory { Name = n }));
        }

        if (!await db.Amenities.AnyAsync())
        {
            db.Amenities.AddRange(AmenityData.Select(a => new Amenity { Name = a.Name, Icon = a.Icon }));
        }

        if (!await db.Holidays.AnyAsync())
        {
            db.Holidays.AddRange(HolidayData.Select(h => new Holiday { Date = h.Date, Name = h.Name }));
        }

        // Chính sách hủy chung của Fuji (BR-04): ≥ 7 ngày hoàn 100%, 3–6 ngày 50%, dưới 3 ngày 0%
        if (!await db.CancellationPolicies.AnyAsync(p => p.HomestayId == null))
        {
            db.CancellationPolicies.Add(new CancellationPolicy
            {
                Name = "Chính sách hủy chung của Fuji",
                Scope = PolicyScope.All,
                Rules =
                {
                    new CancellationRule { MinDaysBefore = 7, RefundPercent = 100 },
                    new CancellationRule { MinDaysBefore = 3, RefundPercent = 50 },
                    new CancellationRule { MinDaysBefore = 0, RefundPercent = 0 },
                },
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedHomestaysAsync(ApplicationDbContext db)
    {
        if (await db.Homestays.AnyAsync())
        {
            return;
        }

        // Chỉ Owner đã được duyệt mới có homestay (v5 mục 6)
        var owners = await db.Owners
            .Include(o => o.Person)
            .Where(o => o.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved))
            .OrderBy(o => o.Person.PhoneNumber)
            .ToListAsync();
        if (owners.Count == 0)
        {
            throw new InvalidOperationException("Seed homestay thất bại: không có Owner nào đã được duyệt.");
        }
        var manager = await db.Employees.SingleAsync(e => e.EmployeeCode == "QL001");
        var areas = await db.Areas.ToDictionaryAsync(a => a.Name);
        var categories = await db.RoomCategories.ToDictionaryAsync(c => c.Name);
        var amenities = await db.Amenities.OrderBy(a => a.Id).ToListAsync();
        decimal[] commissionRates = [0.10m, 0.12m, 0.15m];
        var now = DateTime.UtcNow;

        for (var i = 0; i < HomestayData.Length; i++)
        {
            var (name, address) = HomestayData[i];
            var area = AreaData[i / 5];
            var isSuburb = area.Group == AreaGroup.Suburb;
            var owner = owners[i % owners.Count];

            var homestay = new Homestay
            {
                Name = name,
                Address = address,
                Area = areas[area.Name],
                Owner = owner,
                Status = HomestayStatus.Active,
                ApprovedByEmployeeId = manager.Id,
                ApprovedAt = now,
                ContactPhone = owner.Person.PhoneNumber,
                CheckinTime = new TimeOnly(14, 0),
                CheckoutTime = new TimeOnly(12, 0),
                // Nội dung mẫu cố định, không chứa ký tự HTML đặc biệt nên ghép thẳng
                DescriptionHtml = $"<p>{HomestayDescriptions[i]}</p>",
                CheckinGuideHtml = "<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>",
            };

            // Hoa hồng nằm trong hợp đồng (D6), không nằm ở homestay
            homestay.Contracts.Add(new Contract
            {
                ContractNo = $"HD-2026-{i + 1:0000}",
                StartDate = ContractStart,
                EndDate = new DateOnly(2028, 5, 31),
                CommissionRate = commissionRates[i % commissionRates.Length],
                Status = ContractStatus.Active,
                SignedByEmployeeId = manager.Id,
            });

            // Tiện nghi: 4–6 mục, xoay vòng theo chỉ số để dữ liệu đa dạng; ngoại thành luôn có BBQ và bãi đỗ xe
            var amenityCount = 4 + i % 3;
            var picked = Enumerable.Range(0, amenityCount).Select(k => amenities[(i + k * 3) % amenities.Count]);
            if (isSuburb)
            {
                picked = picked.Concat(amenities.Where(a => a.Name is "Khu BBQ" or "Bãi đỗ xe"));
            }
            foreach (var amenity in picked.DistinctBy(a => a.Id))
            {
                homestay.HomestayAmenities.Add(new HomestayAmenity { Amenity = amenity });
            }

            foreach (var roomType in BuildRoomTypes(i, isSuburb, categories))
            {
                homestay.RoomTypes.Add(roomType);
            }

            // Homestay ở Ba Vì có chính sách hủy riêng dịp lễ Tết, được ưu tiên hơn chính sách chung (D10)
            if (area.Name == "Ba Vì")
            {
                homestay.CancellationPolicies.Add(new CancellationPolicy
                {
                    Name = "Chính sách hủy dịp lễ Tết",
                    Scope = PolicyScope.Holiday,
                    Rules =
                    {
                        new CancellationRule { MinDaysBefore = 14, RefundPercent = 100 },
                        new CancellationRule { MinDaysBefore = 7, RefundPercent = 50 },
                        new CancellationRule { MinDaysBefore = 0, RefundPercent = 0 },
                    },
                });
            }

            db.Homestays.Add(homestay);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Loại phòng mẫu: Phòng đôi (2 phòng), Phòng gia đình (1 phòng); ngoại thành thêm Nguyên căn,
    /// nội thành xen kẽ thêm Phòng đơn. Giá cơ bản 400.000–1.000.000đ/đêm.
    /// </summary>
    private static IEnumerable<RoomType> BuildRoomTypes(int index, bool isSuburb, IReadOnlyDictionary<string, RoomCategory> categories)
    {
        var basePrice = 400_000m + index % 7 * 100_000m;

        yield return CreateRoomType(index, isSuburb, "Phòng đôi", categories["Phòng đôi"], 2, basePrice, "101", "102");
        yield return CreateRoomType(index, isSuburb, "Phòng gia đình", categories["Phòng gia đình"], 4, basePrice * 1.6m, "201");

        if (isSuburb)
        {
            yield return CreateRoomType(index, isSuburb, "Nguyên căn", categories["Nguyên căn"], 10, basePrice * 4, "Căn A");
        }
        else if (index % 2 == 0)
        {
            yield return CreateRoomType(index, isSuburb, "Phòng đơn", categories["Phòng đơn"], 1, basePrice * 0.8m, "103");
        }
    }

    /// <summary>Giá cuối tuần +20%, lễ Tết +50% so với ngày thường (tài liệu bảng giá Fuji).</summary>
    private static RoomType CreateRoomType(int index, bool isSuburb, string name, RoomCategory category, int maxGuests, decimal weekday, params string[] roomCodes)
    {
        var (description, areaM2) = RoomTypeDetails(category.Name, isSuburb, index);
        var roomType = new RoomType
        {
            Name = name, Category = category, MaxGuests = maxGuests, Description = description, AreaM2 = areaM2,
        };
        foreach (var code in roomCodes)
        {
            roomType.Rooms.Add(new Room { RoomCode = code });
        }
        foreach (var (dayType, factor) in new[] { (DayType.Weekday, 1m), (DayType.Weekend, 1.2m), (DayType.Holiday, 1.5m) })
        {
            roomType.Prices.Add(new RoomTypePrice
            {
                DayType = dayType,
                Price = RoundToTenThousand(weekday * factor),
                EffectiveFrom = PriceEffectiveFrom,
            });
        }
        return roomType;
    }

    private static decimal RoundToTenThousand(decimal value) =>
        Math.Round(value / 10_000m, MidpointRounding.AwayFromZero) * 10_000m;

    private static async Task EnsureSucceeded(Task<IdentityResult> operation, string what)
    {
        var result = await operation;
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed {what} thất bại: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
