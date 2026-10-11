using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Services;

public sealed class PricingServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Theory]
    [InlineData(2026, 11, 19, DayType.Weekday)] // thứ Năm
    [InlineData(2026, 11, 20, DayType.Weekend)] // thứ Sáu
    [InlineData(2026, 11, 21, DayType.Weekend)] // thứ Bảy
    [InlineData(2026, 11, 22, DayType.Weekday)] // Chủ nhật: đêm Chủ nhật tính giá ngày thường
    [InlineData(2027, 1, 1, DayType.Holiday)]   // thứ Sáu nhưng là ngày lễ → giá lễ
    public void DayTypeOf_FollowsBR08(int y, int m, int d, DayType expected)
    {
        var holidays = new HashSet<DateOnly> { new(2027, 1, 1) };

        Assert.Equal(expected, PricingService.DayTypeOf(new DateOnly(y, m, d), holidays));
    }

    [Fact]
    public async Task Quote_ThursdayToSunday_PricesEachNight()
    {
        var quote = await _host.Pricing().QuoteAsync(_host.RoomType.Id, new(2026, 11, 19), new(2026, 11, 22));

        Assert.Equal([500_000m, 600_000m, 600_000m], quote.Nights.Select(n => n.Price));
        Assert.Equal(1_700_000m, quote.Total);
        Assert.Equal(510_000m, quote.Deposit); // BR-01: 30%
        Assert.False(quote.HasHoliday);
    }

    [Fact]
    public async Task Quote_UsesPriceEffectiveOnEachNight()
    {
        // 30/11 (thứ Hai) còn giá cũ 500.000đ; 1/12 (thứ Ba) áp giá mới 550.000đ
        var quote = await _host.Pricing().QuoteAsync(_host.RoomType.Id, new(2026, 11, 30), new(2026, 12, 2));

        Assert.Equal([500_000m, 550_000m], quote.Nights.Select(n => n.Price));
    }

    [Fact]
    public async Task Quote_HolidayNight_UsesHolidayPrice()
    {
        var quote = await _host.Pricing().QuoteAsync(_host.RoomType.Id, new(2026, 12, 31), new(2027, 1, 2));

        Assert.Equal([DayType.Weekday, DayType.Holiday], quote.Nights.Select(n => n.DayType));
        Assert.Equal(1_300_000m, quote.Total);
        Assert.True(quote.HasHoliday);
    }

    [Theory]
    [InlineData(370_350, 370_000)]
    [InlineData(370_500, 371_000)]
    [InlineData(510_000, 510_000)]
    public void RoundToThousand_RoundsHalfUp(decimal input, decimal expected)
    {
        Assert.Equal(expected, PricingService.RoundToThousand(input));
    }

    [Fact]
    public async Task Quote_CheckOutNotAfterCheckIn_Throws()
    {
        await Assert.ThrowsAsync<BookingException>(() =>
            _host.Pricing().QuoteAsync(_host.RoomType.Id, new(2026, 11, 20), new(2026, 11, 20)));
    }

    [Fact]
    public async Task Quote_NoPriceForNight_Throws()
    {
        // Trước ngày bắt đầu bảng giá
        await Assert.ThrowsAsync<BookingException>(() =>
            _host.Pricing().QuoteAsync(_host.RoomType.Id, new(2025, 12, 30), new(2025, 12, 31)));
    }
}
