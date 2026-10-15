using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Data;

public class SeedDataTests
{
    private static ServiceProvider BuildServices(string? environmentName = "Development")
    {
        var dbName = Guid.NewGuid().ToString();
        return SeedTestServices.Build(
            services => services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName)),
            environmentName);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    [InlineData(null)]
    public async Task InitializeAsync_OutsideDevelopment_SeedsOnlyFoundation(string? environmentName)
    {
        await using var services = BuildServices(environmentName);

        await SeedData.InitializeAsync(services);
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(AppRoles.All.Length, await db.Roles.CountAsync());
        Assert.Equal(7, await db.Areas.CountAsync());
        Assert.Equal(5, await db.RoomCategories.CountAsync());
        Assert.Equal(10, await db.Amenities.CountAsync());
        Assert.NotEmpty(await db.Holidays.ToListAsync());
        var policy = await db.CancellationPolicies.Include(p => p.Rules)
            .SingleAsync(p => p.HomestayId == null);
        Assert.Equal(3, policy.Rules.Count);

        Assert.False(await db.Users.AnyAsync());
        Assert.False(await db.PersonProfiles.AnyAsync());
        Assert.False(await db.Employees.AnyAsync());
        Assert.False(await db.Owners.AnyAsync());
        Assert.False(await db.Customers.AnyAsync());
        Assert.False(await db.Homestays.AnyAsync());
        Assert.False(await db.Rooms.AnyAsync());
        Assert.False(await db.Bookings.AnyAsync());
        Assert.False(await db.Payments.AnyAsync());
        Assert.False(await db.BankTransactions.AnyAsync());
        Assert.False(await db.Settlements.AnyAsync());
        Assert.False(await db.RoomBlocks.AnyAsync());
        Assert.False(await db.VerificationDocuments.AnyAsync());
        Assert.False(await db.BookingChangeRequests.AnyAsync());
        Assert.False(await db.HomestayImages.AnyAsync());
    }

    // 5 nhân viên + 6 chủ homestay + 1 khách có tài khoản; owner đầu tiên kiêm khách hàng nên không thêm Account
    private static readonly int ExpectedAccounts =
        SeedData.EmployeeData.Length + SeedData.OwnerData.Length
        + SeedData.CustomerData.Count(c => c.HasAccount && SeedData.OwnerData.All(o => o.Phone != c.Phone));

    [Fact]
    public async Task InitializeAsync_CreatesRolesPeopleCategoriesAndHomestays()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(AppRoles.All.Length, await db.Roles.CountAsync());
        Assert.Equal(SeedData.EmployeeData.Length, await db.Employees.CountAsync());
        Assert.Equal(SeedData.OwnerData.Length, await db.Owners.CountAsync());
        Assert.Equal(SeedData.CustomerData.Length, await db.Customers.CountAsync());
        Assert.Equal(ExpectedAccounts, await db.Users.CountAsync());
        Assert.Equal(35, await db.Homestays.CountAsync());
        Assert.True(await db.Rooms.CountAsync() >= 70);
        Assert.NotEmpty(await db.Holidays.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_SeedsValidBusinessData()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var homestays = await db.Homestays
            .Include(h => h.Contracts)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Rooms)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .ToListAsync();

        Assert.All(homestays, h =>
        {
            Assert.Equal(HomestayStatus.Active, h.Status);
            // D6, BR-02: mỗi homestay đúng một hợp đồng đang hiệu lực, hoa hồng 10–15%
            var contract = Assert.Single(h.Contracts, c => c.Status == ContractStatus.Active);
            Assert.InRange(contract.CommissionRate, 0.10m, 0.15m);
            Assert.NotEmpty(h.RoomTypes);
        });

        Assert.All(homestays.SelectMany(h => h.RoomTypes), t =>
        {
            Assert.True(t.MaxGuests > 0);
            Assert.NotEmpty(t.Rooms);
            // D3, BR-08: đủ 3 mức giá, cuối tuần và lễ không thấp hơn ngày thường
            var price = t.Prices.ToDictionary(p => p.DayType, p => p.Price);
            Assert.Equal(3, price.Count);
            Assert.True(price[DayType.Weekend] >= price[DayType.Weekday]);
            Assert.True(price[DayType.Holiday] >= price[DayType.Weekend]);
        });

        // Chính sách hủy chung (BR-04) và chính sách lễ riêng của homestay Ba Vì (D10)
        var general = await db.CancellationPolicies.Include(p => p.Rules).SingleAsync(p => p.HomestayId == null);
        Assert.Equal([0, 3, 7], general.Rules.Select(r => r.MinDaysBefore).Order());
        Assert.Equal(5, await db.CancellationPolicies.CountAsync(p => p.HomestayId != null && p.Scope == PolicyScope.Holiday));

        // Owner đã duyệt có tài khoản nhận tiền đối soát
        Assert.All(await db.Owners.Where(o => o.Homestays.Any()).ToListAsync(), o => Assert.False(string.IsNullOrEmpty(o.BankAccountNo)));
    }

    [Fact]
    public async Task InitializeAsync_SeedsAccountsFollowingV5Rules()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // SĐT lưu dạng E.164; mỗi Customer có Customer Profile; homestay chỉ thuộc Owner đã duyệt
        Assert.All(await db.PersonProfiles.ToListAsync(), p => Assert.StartsWith("+84", p.PhoneNumber));
        Assert.Equal(await db.Customers.CountAsync(), await db.CustomerProfiles.CountAsync());
        Assert.False(await db.Homestays.AnyAsync(h => !h.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)));
        Assert.True(await db.OwnerRegistrations.AnyAsync(r => r.Status == OwnerRegistrationStatus.PendingReview));

        // Khách do nhân viên tạo chưa có Account
        Assert.True(await db.Customers.AnyAsync(c => c.Person.Account == null));
    }

    [Fact]
    public async Task InitializeAsync_RunTwice_DoesNotDuplicateData()
    {
        await using var services = BuildServices();

        await SeedData.InitializeAsync(services);
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(35, await db.Homestays.CountAsync());
        Assert.Equal(ExpectedAccounts, await db.Users.CountAsync());
        Assert.Equal(SeedData.OwnerData.Length, await db.OwnerRegistrations.CountAsync());
        Assert.Equal(AppRoles.All.Length, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task SeededEmployee_CanBeFoundByCodeAndSignInWithDefaultPassword()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var lookup = scope.ServiceProvider.GetRequiredService<AccountLookupService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await lookup.FindForEmployeeLoginAsync("ad001");

        Assert.NotNull(admin);
        Assert.True(await userManager.CheckPasswordAsync(admin, SeedData.DefaultPassword));
        Assert.True(await userManager.IsInRoleAsync(admin, AppRoles.Employee));
    }

    [Fact]
    public async Task SeededOwnerWhoIsAlsoCustomer_HasOneAccountWithBothRoles()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var lookup = scope.ServiceProvider.GetRequiredService<AccountLookupService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var account = await lookup.FindForPhoneLoginAsync("0912 000 001");

        Assert.NotNull(account);
        Assert.Equal([AppRoles.Customer, AppRoles.Owner], (await userManager.GetRolesAsync(account)).Order());
    }

    [Fact]
    public async Task InitializeAsync_ExistingAccountWithoutRole_GetsRoleAssigned()
    {
        await using var services = BuildServices();
        using (var scope = services.CreateScope())
        {
            // Mô phỏng lần seed trước dừng sau khi tạo Account nhưng trước khi gán role
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var person = new PersonProfile { FullName = "Lê Văn Hùng", PhoneNumber = "+84912000001" };
            db.PersonProfiles.Add(person);
            await db.SaveChangesAsync();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = new ApplicationUser { PersonId = person.Id, Status = AccountStatus.Active };
            Assert.True((await userManager.CreateAsync(account, SeedData.DefaultPassword)).Succeeded);
        }

        await SeedData.InitializeAsync(services);

        using var verify = services.CreateScope();
        var lookup = verify.ServiceProvider.GetRequiredService<AccountLookupService>();
        var manager = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await lookup.FindForPhoneLoginAsync("0912000001");
        Assert.NotNull(existing);
        Assert.True(await manager.IsInRoleAsync(existing, AppRoles.Owner));
        Assert.Equal(ExpectedAccounts, await manager.Users.CountAsync());
    }
    [Fact]
    public async Task InitializeAsync_SeedsDistinctDescriptions()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var descriptions = await db.Homestays.Select(h => h.DescriptionHtml).ToListAsync();
        Assert.Equal(descriptions.Count, descriptions.Distinct().Count());
        Assert.All(await db.RoomTypes.ToListAsync(), t =>
        {
            Assert.False(string.IsNullOrEmpty(t.Description));
            Assert.True(t.AreaM2 > 0);
        });
    }

    [Fact]
    public async Task InitializeAsync_SampleBookingsFollowBusinessRules()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bookings = await db.Bookings
            .Include(b => b.Nights).Include(b => b.RoomNights).Include(b => b.Payments).Include(b => b.Histories).Include(b => b.ChangeRequests)
            .ToListAsync();

        Assert.Equal(SeedData.SampleBookingCount, bookings.Count);
        // Đủ mọi trạng thái để demo các màn hình
        BookingStatus[] expected =
        [
            BookingStatus.PaymentSubmitted, BookingStatus.Deposited, BookingStatus.Confirmed, BookingStatus.CheckedIn,
            BookingStatus.Completed, BookingStatus.Expired, BookingStatus.Rejected, BookingStatus.Cancelled, BookingStatus.NoShow,
        ];
        Assert.All(expected, s => Assert.Contains(bookings, b => b.Status == s));
        Assert.Equal(bookings.Count, bookings.Select(b => b.Code).Distinct().Count());

        Assert.All(bookings, b =>
        {
            Assert.Equal(b.CheckOut.DayNumber - b.CheckIn.DayNumber, b.Nights.Count);
            Assert.Equal(b.Nights.Sum(n => n.Price), b.TotalAmount);
            Assert.Equal(PricingService.RoundToThousand(b.TotalAmount * 0.30m), b.DepositAmount);
            // Chỉ trạng thái giữ phòng mới chiếm lịch phòng
            Assert.Equal(b.Status.HoldsRoom() ? b.Nights.Count : 0, b.RoomNights.Count);
            // Lịch sử nối tiếp nhau và kết thúc ở trạng thái hiện tại
            var history = b.Histories.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id).ToList();
            Assert.Null(history[0].FromStatus);
            Assert.Equal(b.Status, history[^1].ToStatus);
            for (var i = 1; i < history.Count; i++)
            {
                Assert.Equal(history[i - 1].ToStatus, history[i].FromStatus);
                if (history[i].FromStatus == BookingStatus.Confirmed && history[i].ToStatus == BookingStatus.Deposited)
                {
                    // Đổi đã Applied phải xác nhận lại; đây là thao tác riêng, không mở transition chung.
                    var applied = Assert.Single(b.ChangeRequests, r => r.Status == ChangeRequestStatus.Applied);
                    Assert.Equal(applied.DecidedAt, history[i].ChangedAt);
                    Assert.Equal(applied.DecidedByAccountId, history[i].ActorAccountId);
                    Assert.Equal(ActorType.Employee, history[i].ActorType);
                    Assert.Null(b.ConfirmedAt);
                }
                else Assert.True(BookingService.CanTransition(history[i].FromStatus!.Value, history[i].ToStatus));
            }
            Assert.True(b.Payments.Count(p => p.Type == PaymentType.Deposit) <= 1);
            Assert.True(b.CreatedAt < b.CheckIn.ToDateTime(new TimeOnly(14, 0)));
        });

        var roomNights = await db.RoomNights.ToListAsync();
        Assert.Equal(roomNights.Count, roomNights.Select(n => (n.RoomId, n.Date)).Distinct().Count());

        // Mã đơn đã sinh không trùng với mã đơn thật đặt sau đó
        var counters = await db.BookingCodeCounters.ToDictionaryAsync(c => c.Date, c => c.LastSeq);
        Assert.All(bookings, b =>
        {
            var date = DateOnly.ParseExact(b.Code[2..8], "yyMMdd");
            Assert.True(counters[date] >= int.Parse(b.Code[9..]));
        });

        // Có việc cho kế toán: bill chờ duyệt, giao dịch thiếu tiền / đến muộn / không mã đơn
        Assert.True(await db.Payments.CountAsync(p => p.Method == PaymentMethod.ManualReceipt && p.Status == PaymentStatus.Submitted) >= 2);
        foreach (var status in new[] { BankMatchStatus.Underpaid, BankMatchStatus.LatePayment, BankMatchStatus.Unmatched })
        {
            Assert.True(await db.BankTransactions.AnyAsync(t => t.MatchStatus == status), status.ToString());
        }

        var reviews = await db.Reviews.Include(r => r.Booking).ToListAsync();
        Assert.Equal(SeedData.SampleReviewCount, reviews.Count);
        Assert.All(reviews, r => Assert.Equal(BookingStatus.Completed, r.Booking.Status));
        Assert.Contains(reviews, r => r.IsHidden);
        Assert.Contains(reviews, r => r.OwnerReply != null);

        // Mỗi đơn bị homestay từ chối có một voucher xin lỗi cho đúng khách đó
        var vouchers = await db.Vouchers.Include(v => v.SourceBooking).ToListAsync();
        Assert.Equal(bookings.Count(b => b.Status == BookingStatus.Rejected), vouchers.Count);
        Assert.All(vouchers, v =>
        {
            Assert.Equal(v.SourceBooking!.CustomerId, v.CustomerId);
            Assert.InRange(v.Amount, 50_000, 200_000);
            Assert.Equal(v.Amount, v.Balance);
        });
    }

    [Fact]
    public async Task InitializeAsync_SeedsOperationsData()
    {
        await using var services = BuildServices();
        await SeedData.InitializeAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var today = DateOnly.FromDateTime(VietnamTime.ToLocal(DateTime.UtcNow));

        // Đối soát: chỉ kỳ đã kết thúc, đơn đúng kỳ theo CheckOut, tổng khớp từng dòng, công thức mục 7.3
        var settlements = await db.Settlements.Include(s => s.Items).ThenInclude(i => i.Booking).ToListAsync();
        Assert.NotEmpty(settlements);
        // Kỳ gần nhất xoay vòng các bước để demo; kỳ cũ hơn đã trả
        Assert.Contains(settlements, s => s.Status == SettlementStatus.Draft);
        Assert.Contains(settlements, s => s.Status == SettlementStatus.Paid);
        Assert.All(settlements, s => Assert.True(s.CreatedAt <= DateTime.UtcNow && (s.SentAt ?? s.CreatedAt) >= s.CreatedAt));
        Assert.All(settlements, s =>
        {
            var period = new SettlementPeriod(s.Year, s.Month, s.PeriodNo);
            Assert.True(period.End < today);
            Assert.NotEmpty(s.Items);
            Assert.Equal(s.Items.Sum(i => i.PayoutAmount), s.PayoutAmount);
            Assert.Equal(s.TotalRetained - s.TotalCommission, s.PayoutAmount);
            Assert.Equal(s.Status == SettlementStatus.Paid, s.PaidAt != null && s.PaidBankRef != null);
            Assert.All(s.Items, i =>
            {
                Assert.Equal(s.HomestayId, i.Booking.HomestayId);
                Assert.Equal(period, SettlementCalculator.PeriodOf(i.Booking.CheckOut));
                Assert.True(i.RetainedAmount > 0);
                Assert.Equal(i.RetainedAmount - i.CommissionAmount, i.PayoutAmount);
            });
        });
        // Mọi đơn hoàn thành của kỳ đã qua đều đã vào đối soát
        var completedPast = await db.Bookings.Where(b => b.Status == BookingStatus.Completed).ToListAsync();
        Assert.All(completedPast.Where(b => SettlementCalculator.PeriodOf(b.CheckOut).End < today),
            b => Assert.Contains(settlements.SelectMany(s => s.Items), i => i.BookingId == b.Id));

        // Khóa ngày không đè lên đêm đã có đơn
        var blocks = await db.RoomBlocks.Include(b => b.Nights).ToListAsync();
        Assert.Equal(SeedData.SampleRoomBlockCount, blocks.Count);
        Assert.All(blocks, b => Assert.Equal(b.EndDate.DayNumber - b.StartDate.DayNumber, b.Nights.Count));
        var roomNights = await db.RoomNights.ToListAsync();
        Assert.Equal(roomNights.Count, roomNights.Select(n => (n.RoomId, n.Date)).Distinct().Count());

        // Mỗi homestay có giấy phép kinh doanh và PCCC; hồ sơ Owner chờ duyệt có đủ giấy tờ
        var documents = await db.VerificationDocuments.ToListAsync();
        Assert.All(await db.Homestays.Select(h => h.Id).ToListAsync(), id =>
        {
            Assert.Contains(documents, d => d.HomestayId == id && d.DocumentType == DocumentType.BusinessLicense);
            Assert.Contains(documents, d => d.HomestayId == id && d.DocumentType == DocumentType.FireSafety);
        });
        var pending = await db.OwnerRegistrations.SingleAsync(r => r.Status == OwnerRegistrationStatus.PendingReview);
        Assert.True(documents.Count(d => d.OwnerRegistrationId == pending.Id) >= 4);

        var requests = await db.BookingChangeRequests.Include(r => r.Booking).ToListAsync();
        Assert.Contains(requests, r => r.Status == ChangeRequestStatus.Pending);
        Assert.Contains(requests, r => r.RequestType == ChangeRequestType.Cancel && r.RefundAmount != null);
        Assert.All(requests.Where(r => r.Status != ChangeRequestStatus.Pending), r => Assert.NotNull(r.DecidedAt));

        var refunded = await db.BankTransactions.SingleAsync(t => t.MatchStatus == BankMatchStatus.Refunded);
        Assert.NotNull(refunded.RefundedAt);
    }
}
