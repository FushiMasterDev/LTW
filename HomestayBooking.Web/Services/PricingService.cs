using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public record NightPrice(DateOnly Date, DayType DayType, decimal Price);

public record PriceQuote(IReadOnlyList<NightPrice> Nights, decimal Total, decimal DepositRate, decimal Deposit)
{
    public bool HasHoliday => Nights.Any(n => n.DayType == DayType.Holiday);
}

/// <summary>Tính giá theo từng đêm (BR-08) và tiền cọc (BR-01) — fuji_homestay_database_v1 mục 7.1.</summary>
public class PricingService
{
    private readonly ApplicationDbContext _db;
    private readonly BookingOptions _options;

    public PricingService(ApplicationDbContext db, IOptions<BookingOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    /// <summary>Đêm có trong Holidays → giá lễ; đêm thứ Sáu, thứ Bảy → giá cuối tuần; còn lại → giá ngày thường.</summary>
    public static DayType DayTypeOf(DateOnly night, IReadOnlySet<DateOnly> holidays) =>
        holidays.Contains(night) ? DayType.Holiday
        : night.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday ? DayType.Weekend
        : DayType.Weekday;

    /// <summary>Làm tròn đến 1.000đ.</summary>
    /// <summary>Giá đang áp dụng hôm nay cho từng (loại phòng, loại ngày): bảng giá có EffectiveFrom gần nhất ≤ hôm nay.</summary>
    public static Dictionary<(int RoomTypeId, DayType DayType), decimal> CurrentPrices(IEnumerable<RoomTypePrice> prices, DateOnly today) =>
        prices
            .Where(p => p.EffectiveFrom <= today)
            .GroupBy(p => (p.RoomTypeId, p.DayType))
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.EffectiveFrom)!.Price);

    public static decimal RoundToThousand(decimal amount) =>
        Math.Round(amount / 1_000m, MidpointRounding.AwayFromZero) * 1_000m;

    public async Task<PriceQuote> QuoteAsync(int roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
        {
            throw new BookingException("Ngày trả phòng phải sau ngày nhận phòng.");
        }
        if (checkOut.DayNumber - checkIn.DayNumber > _options.MaxNights)
        {
            throw new BookingException("Thời gian lưu trú quá dài.");
        }

        var lastNight = checkOut.AddDays(-1);
        var holidays = (await _db.Holidays
                .Where(h => h.Date >= checkIn && h.Date <= lastNight)
                .Select(h => h.Date)
                .ToListAsync())
            .ToHashSet();
        var prices = await _db.RoomTypePrices.AsNoTracking()
            .Where(p => p.RoomTypeId == roomTypeId && p.EffectiveFrom <= lastNight)
            .ToListAsync();

        var nights = new List<NightPrice>();
        for (var night = checkIn; night < checkOut; night = night.AddDays(1))
        {
            var dayType = DayTypeOf(night, holidays);
            // Giá hiệu lực của đêm: dòng có EffectiveFrom lớn nhất mà ≤ đêm đó
            var price = prices
                .Where(p => p.DayType == dayType && p.EffectiveFrom <= night)
                .MaxBy(p => p.EffectiveFrom)
                ?? throw new BookingException("Loại phòng này chưa có bảng giá cho ngày đã chọn.");
            nights.Add(new NightPrice(night, dayType, price.Price));
        }

        var total = nights.Sum(n => n.Price);
        return new PriceQuote(nights, total, _options.DepositRate, RoundToThousand(total * _options.DepositRate));
    }
}
