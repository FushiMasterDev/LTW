using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Services;

public class ReceiptFileStoreTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }, ".png")]
    [InlineData(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, ".webp")]
    [InlineData(new byte[] { (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0, 0, 0, 0, 0, 0, 0, 0 }, null)] // SVG có thể chứa script
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', 0, 0, 0, 0, 0, 0, 0, 0 }, null)]
    public void DetectExtension_TrustsFileSignatureOnly(byte[] header, string? expected)
    {
        Assert.Equal(expected, ReceiptFileStore.DetectExtension(header));
    }

    [Fact]
    public void CurrentPrices_PicksLatestEffectivePriceUpToToday()
    {
        var prices = new[]
        {
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 500_000, EffectiveFrom = new(2026, 1, 1) },
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 550_000, EffectiveFrom = new(2026, 10, 1) },
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 600_000, EffectiveFrom = new(2026, 12, 1) },
        };

        var current = PricingService.CurrentPrices(prices, new(2026, 10, 11));

        Assert.Equal(550_000, current[(1, DayType.Weekday)]);
    }
}
