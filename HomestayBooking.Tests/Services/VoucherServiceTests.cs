using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

/// <summary>
/// Voucher (Q4, D14). Đơn mẫu: 19–22/11/2026 = đêm thứ Năm 500k + thứ Sáu 600k + thứ Bảy 600k = 1.700.000đ,
/// cọc 510.000đ, hoa hồng 12% = 204.000đ → giảm tối đa 20% hoa hồng = 40.800 → làm tròn xuống 40.000đ.
/// </summary>
public sealed class VoucherServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();
    private readonly Employee _manager;

    public VoucherServiceTests()
    {
        var person = new PersonProfile { FullName = "Quản lý", PhoneNumber = "+84901000031" };
        _host.Db.Users.Add(new ApplicationUser { Person = person, Status = AccountStatus.Active });
        _manager = new Employee { Person = person, EmployeeCode = "QL001", Position = EmployeePosition.Manager };
        _host.Db.Employees.Add(_manager);
        _host.Db.SaveChanges();
        _host.Db.ChangeTracker.Clear();
    }

    public void Dispose() => _host.Dispose();

    private Voucher AddVoucher(decimal balance, int? customerId = null, DateOnly? expiresOn = null)
    {
        var voucher = new Voucher
        {
            Code = "VC" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerId = customerId ?? _host.Customer.Id,
            Amount = Math.Max(balance, 135_000),
            Balance = balance,
            ExpiresOn = expiresOn ?? new DateOnly(2027, 5, 10),
            Reason = "Trùng phòng",
            IssuedByEmployeeId = _manager.Id,
        };
        _host.Db.Vouchers.Add(voucher);
        _host.Db.SaveChanges();
        _host.Db.ChangeTracker.Clear();
        return voucher;
    }

    private Task<Booking> BookAsync(int? voucherId) =>
        _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)) with { VoucherId = voucherId });

    private decimal BalanceOf(int voucherId) => _host.Db.Vouchers.AsNoTracking().Single(v => v.Id == voucherId).Balance;

    [Theory]
    [InlineData(2_700_000, 135_000)]   // 5%
    [InlineData(500_000, 50_000)]      // 25k → tối thiểu 50k
    [InlineData(10_000_000, 200_000)]  // 500k → tối đa 200k
    public void IssueAmountFor_IsFivePercentClamped(decimal sourceTotal, decimal expected)
    {
        Assert.Equal(expected, _host.Vouchers().IssueAmountFor(sourceTotal));
    }

    [Theory]
    [InlineData(2_500_000, 0.12, 60_000)]  // Fuji nhận 300k hoa hồng → chịu tối đa 60k
    [InlineData(2_700_000, 0.12, 64_000)]  // 64.800 → làm tròn xuống, không vượt 20%
    [InlineData(1_700_000, 0.12, 40_000)]
    public void MaxDiscount_IsTwentyPercentOfCommissionRoundedDown(decimal total, decimal rate, decimal expected)
    {
        Assert.Equal(expected, VoucherService.MaxDiscount(total, rate, 0.20m));
    }

    [Fact]
    public async Task Create_WithVoucher_DiscountsDepositAndKeepsBalance()
    {
        var voucher = AddVoucher(135_000);

        var booking = await BookAsync(voucher.Id);

        Assert.Equal(510_000, booking.DepositAmount);
        Assert.Equal(40_000, booking.DiscountAmount);
        Assert.Equal(470_000, booking.AmountDue);
        Assert.Equal(95_000, BalanceOf(voucher.Id));
        // Fuji vẫn giữ ≥ 80% hoa hồng
        Assert.True(booking.TotalAmount * booking.CommissionRate - booking.DiscountAmount >= booking.TotalAmount * booking.CommissionRate * 0.8m);
    }

    [Fact]
    public async Task Create_VoucherSmallerThanCap_UsesItUp_CancelMakesItActiveAgain()
    {
        var voucher = AddVoucher(30_000);

        var booking = await BookAsync(voucher.Id);
        Assert.Equal(30_000, booking.DiscountAmount);
        Assert.Equal(VoucherStatus.UsedUp, _host.Db.Vouchers.AsNoTracking().Single().Status);

        _host.Db.ChangeTracker.Clear();
        var tracked = await _host.Db.Bookings.SingleAsync();
        await _host.Bookings().ChangeStatusAsync(tracked, BookingStatus.Cancelled, new Actor(ActorType.Customer));

        var restored = _host.Db.Vouchers.AsNoTracking().Single();
        Assert.Equal(30_000, restored.Balance);
        Assert.Equal(VoucherStatus.Active, restored.Status);
    }

    [Fact]
    public async Task BankTransfer_OfAmountDue_IsEnoughToDeposit()
    {
        var booking = await BookAsync(AddVoucher(135_000).Id);

        var result = await _host.Matching().ProcessAsync(new(BankProvider.SePay, "SP-1", BookingTestHost.FujiAccount, 470_000,
            booking.Code, _host.Clock.Now.UtcDateTime));

        Assert.Equal(BankMatchStatus.Matched, result.Status);
        Assert.Equal(BookingStatus.Deposited, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task CancelAfterDeposit_RefundsCashPaidAndRestoresVoucher()
    {
        var voucher = AddVoucher(135_000);
        var booking = await BookAsync(voucher.Id);
        await _host.Matching().ProcessAsync(new(BankProvider.SePay, "SP-1", BookingTestHost.FujiAccount, 470_000,
            booking.Code, _host.Clock.Now.UtcDateTime));
        _host.Db.ChangeTracker.Clear();

        var tracked = await _host.Db.Bookings.SingleAsync();
        await _host.Bookings().ChangeStatusAsync(tracked, BookingStatus.Cancelled, new Actor(ActorType.Customer));

        // Hủy trước 9 ngày → hoàn 100% số tiền khách đã chuyển (không phải 100% cọc)
        Assert.Equal(470_000, tracked.RefundAmount);
        Assert.Equal(135_000, BalanceOf(voucher.Id));
    }

    [Fact]
    public async Task CancelWithHalfRefund_RestoresHalfOfDiscount()
    {
        var voucher = AddVoucher(135_000);
        var booking = await BookAsync(voucher.Id);
        await _host.Matching().ProcessAsync(new(BankProvider.SePay, "SP-1", BookingTestHost.FujiAccount, 470_000,
            booking.Code, _host.Clock.Now.UtcDateTime));
        _host.Db.ChangeTracker.Clear();
        _host.Clock.Advance(TimeSpan.FromDays(4)); // còn 5 ngày → hoàn 50%

        var tracked = await _host.Db.Bookings.SingleAsync();
        await _host.Bookings().ChangeStatusAsync(tracked, BookingStatus.Cancelled, new Actor(ActorType.Customer));

        Assert.Equal(235_000, tracked.RefundAmount);   // 50% của 470.000 tiền mặt
        Assert.Equal(115_000, BalanceOf(voucher.Id));  // 95.000 + 50% của 40.000
        // Khách mất 235.000 tiền mặt + 20.000 voucher = 255.000 = 50% cọc 510.000, như khi không dùng voucher
    }

    [Fact]
    public async Task VoucherOfAnotherCustomer_IsRefusedAndNothingIsHeld()
    {
        var otherPerson = new PersonProfile { FullName = "Khách khác", PhoneNumber = "+84987000099" };
        var other = new Customer { Person = otherPerson };
        _host.Db.Customers.Add(other);
        _host.Db.SaveChanges();
        _host.Db.ChangeTracker.Clear();
        var voucher = AddVoucher(135_000, other.Id);

        await Assert.ThrowsAsync<BookingException>(() => BookAsync(voucher.Id));

        Assert.Empty(_host.Db.Bookings.AsNoTracking());
        Assert.Empty(_host.Db.RoomNights.AsNoTracking());
        Assert.Equal(135_000, BalanceOf(voucher.Id));
    }

    [Fact]
    public async Task ExpiredVoucher_IsRefused()
    {
        var voucher = AddVoucher(135_000, expiresOn: new DateOnly(2026, 11, 9));

        await Assert.ThrowsAsync<BookingException>(() => BookAsync(voucher.Id));
    }

    [Fact]
    public async Task SameVoucherUsedConcurrently_SecondBookingAsksToRetry()
    {
        var voucher = AddVoucher(135_000);
        using var otherRequest = _host.NewContext();
        // Request thứ hai đã đọc voucher (số dư 135k) trước khi request thứ nhất trừ
        _ = await otherRequest.Vouchers.SingleAsync(v => v.Id == voucher.Id);

        await BookAsync(voucher.Id);
        var ex = await Assert.ThrowsAsync<BookingException>(() =>
            _host.Bookings(otherRequest).CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)) with { VoucherId = voucher.Id }));

        Assert.Contains("thử lại", ex.Message);
        Assert.Equal(95_000, BalanceOf(voucher.Id));
        Assert.Single(_host.Db.Bookings.AsNoTracking());
    }

    [Fact]
    public async Task Issue_ByManager_CreatesVoucherFromSourceBooking_OnlyOnce()
    {
        var source = await BookAsync(null);

        var voucher = await _host.Vouchers().IssueAsync(source.Code.ToLowerInvariant(), _manager.Id, "Trùng phòng, đã chuyển khách sang homestay khác");

        Assert.Equal(85_000, voucher.Amount); // 5% × 1.700.000
        Assert.Equal(voucher.Amount, voucher.Balance);
        Assert.Equal(new DateOnly(2027, 5, 10), voucher.ExpiresOn);
        Assert.Matches("^VC[A-Z2-9]{8}$", voucher.Code);
        await Assert.ThrowsAsync<BookingException>(() => _host.Vouchers().IssueAsync(source.Code, _manager.Id, "Lần hai"));
    }

    [Fact]
    public async Task Issue_ByConsultant_IsRefused()
    {
        var source = await BookAsync(null);
        var consultant = _host.Db.Employees.Single(e => e.EmployeeCode == "NV001");

        await Assert.ThrowsAsync<BookingException>(() => _host.Vouchers().IssueAsync(source.Code, consultant.Id, "x"));
    }

    [Fact]
    public async Task Expire_MarksOverdueVouchers()
    {
        var overdue = AddVoucher(50_000, expiresOn: new DateOnly(2026, 11, 9));
        var valid = AddVoucher(50_000);

        Assert.Equal(1, await _host.Vouchers().ExpireAsync());

        Assert.Equal(VoucherStatus.Expired, _host.Db.Vouchers.AsNoTracking().Single(v => v.Id == overdue.Id).Status);
        Assert.Equal(VoucherStatus.Active, _host.Db.Vouchers.AsNoTracking().Single(v => v.Id == valid.Id).Status);
    }
}
