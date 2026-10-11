using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Services;

public sealed class RefundAndCodeTests : IDisposable
{
    private readonly BookingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task BookingCode_IncrementsPerDayAndRestartsNextDay()
    {
        var codes = new BookingCodeGenerator(_host.Db);

        Assert.Equal("FJ261110-001", await codes.NextAsync(new(2026, 11, 10)));
        Assert.Equal("FJ261110-002", await codes.NextAsync(new(2026, 11, 10)));
        Assert.Equal("FJ261111-001", await codes.NextAsync(new(2026, 11, 11)));
    }

    [Fact]
    public async Task SelectPolicy_NoHolidayNight_UsesGeneralPolicy()
    {
        var policy = await _host.Refunds().SelectPolicyAsync(_host.Homestay.Id, hasHolidayNight: false);

        Assert.Equal(_host.GeneralPolicy.Id, policy.Id);
    }

    [Fact]
    public async Task SelectPolicy_HolidayNight_PrefersHomestayHolidayPolicy()
    {
        var policy = await _host.Refunds().SelectPolicyAsync(_host.Homestay.Id, hasHolidayNight: true);

        Assert.Equal(_host.HomestayHolidayPolicy.Id, policy.Id);
    }

    // Hôm nay 10/11/2026; cọc 600.000đ; chính sách chung 7 ngày 100%, 3 ngày 50%, dưới 3 ngày 0%
    [Theory]
    [InlineData(20, 100, 600_000)] // trước 10 ngày
    [InlineData(17, 100, 600_000)] // đúng 7 ngày
    [InlineData(15, 50, 300_000)]  // trước 5 ngày
    [InlineData(13, 50, 300_000)]  // đúng 3 ngày
    [InlineData(11, 0, 0)]         // trước 1 ngày
    public async Task QuoteCancellation_DepositedBooking_FollowsBR04(int checkInDay, int percent, decimal refund)
    {
        var booking = new Booking
        {
            Status = BookingStatus.Deposited,
            CheckIn = new(2026, 11, checkInDay),
            DepositAmount = 600_000,
            CancellationPolicyId = _host.GeneralPolicy.Id,
        };

        var quote = await _host.Refunds().QuoteCancellationAsync(booking);

        Assert.Equal(percent, quote.RefundPercent);
        Assert.Equal(refund, quote.RefundAmount);
    }

    [Fact]
    public async Task QuoteCancellation_NoDepositYet_RefundsNothing()
    {
        var booking = new Booking
        {
            Status = BookingStatus.AwaitingPayment,
            CheckIn = new(2026, 11, 30),
            DepositAmount = 600_000,
            CancellationPolicyId = _host.GeneralPolicy.Id,
        };

        Assert.Equal(0, (await _host.Refunds().QuoteCancellationAsync(booking)).RefundAmount);
    }
}
