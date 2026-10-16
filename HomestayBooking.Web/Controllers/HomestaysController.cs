using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Controllers;

/// <summary>Tìm homestay và xem chi tiết (khách chưa đăng nhập cũng xem được). Chỉ hiện homestay Active có hợp đồng hiệu lực.</summary>
public class HomestaysController : Controller
{

    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;
    private readonly TimeProvider _clock;
    private readonly BookingOptions _options;

    public HomestaysController(ApplicationDbContext db, PricingService pricing, TimeProvider clock, IOptions<BookingOptions> options, AvailabilityService? availability = null)
    {
        _db = db;
        _availability = availability ?? new AvailabilityService(db, clock, options);
        _clock = clock;
        _options = options.Value;
    }

    public async Task<IActionResult> Index(HomestaySearchQuery query) => View(await _availability.SearchAsync(query));

    public async Task<IActionResult> Details(int id, HomestaySearchQuery query)
    {
        var today = VietnamTime.Today(_clock);
        if (query.CheckIn < today) query.CheckIn = today;
        var sellingTypes = await _availability.SellableRooms(checkOut: query.HasDates ? query.CheckOut : null).Select(r => r.RoomTypeId).Distinct().ToArrayAsync();
        var homestay = await _availability.BookableHomestays(query.HasDates ? query.CheckOut : null)
            .AsNoTracking()
            .Include(h => h.Area)
            .Include(h => h.Images)
            .Include(h => h.HomestayAmenities).ThenInclude(a => a.Amenity)
            .Include(h => h.RoomTypes.Where(t => sellingTypes.Contains(t.Id))).ThenInclude(t => t.Category)
            .Include(h => h.RoomTypes.Where(t => sellingTypes.Contains(t.Id))).ThenInclude(t => t.Prices)
            .AsSplitQuery()
            .SingleOrDefaultAsync(h => h.Id == id);
        if (homestay is null)
        {
            return NotFound();
        }
        if (query.CheckIn < today)
        {
            query.CheckIn = today;
        }

        var prices = PricingService.CurrentPrices(homestay.RoomTypes.SelectMany(t => t.Prices), today);
        decimal? Price(int roomTypeId, DayType dayType) => prices.TryGetValue((roomTypeId, dayType), out var p) ? p : null;

        var freeRooms = query.HasDates ? await _availability.SellableRooms(query.CheckIn, query.CheckOut)
            .Where(r => r.RoomType.HomestayId == id).GroupBy(r => r.RoomTypeId)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Id, g => g.Count) : [];
        var holidays = query.HasDates ? (await _db.Holidays.Where(h => h.Date >= query.CheckIn && h.Date < query.CheckOut)
            .Select(h => h.Date).ToListAsync()).ToHashSet() : new HashSet<DateOnly>();
        var offers = new List<RoomTypeOffer>();
        foreach (var type in homestay.RoomTypes.OrderBy(t => t.MaxGuests).ThenBy(t => t.Name))
        {
            int? free = null;
            PriceQuote? quote = null;
            if (query.HasDates)
            {
                free = freeRooms.GetValueOrDefault(type.Id);
                try
                {
                    quote = PricingService.QuoteFromPrices(type.Id, query.CheckIn!.Value, query.CheckOut!.Value, type.Prices, holidays, _options.DepositRate, _options.MaxNights);
                }
                catch (BookingException)
                {
                    // Ngày không hợp lệ (quá dài, thiếu giá) → chỉ hiện bảng giá
                }
            }
            offers.Add(new RoomTypeOffer(type.Id, type.Name, type.Category.Name, type.MaxGuests, type.AreaM2, type.Description,
                Price(type.Id, DayType.Weekday), Price(type.Id, DayType.Weekend), Price(type.Id, DayType.Holiday), free, quote));
        }

        // Chính sách hủy: riêng của homestay nếu có, không thì chính sách chung (cùng thứ tự ưu tiên với RefundService)
        var policies = await _db.CancellationPolicies.AsNoTracking()
            .Include(p => p.Rules)
            .Where(p => p.IsActive && (p.HomestayId == id || p.HomestayId == null))
            .ToListAsync();
        IReadOnlyList<PolicyRuleView>? RulesOf(PolicyScope scope) =>
            (policies.FirstOrDefault(p => p.HomestayId == id && p.Scope == scope)
             ?? policies.FirstOrDefault(p => p.HomestayId == null && p.Scope == scope))
            ?.Rules.OrderByDescending(r => r.MinDaysBefore).Select(r => new PolicyRuleView(r.MinDaysBefore, r.RefundPercent)).ToList();

        return View(new HomestayDetailsViewModel
        {
            Id = homestay.Id,
            Name = homestay.Name,
            AreaName = homestay.Area.Name,
            DescriptionHtml = homestay.DescriptionHtml,
            CheckinTime = homestay.CheckinTime,
            CheckoutTime = homestay.CheckoutTime,
            Images = homestay.Images.Where(i => i.RoomTypeId == null).OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder)
                .Select(i => i.Url).ToList(),
            Amenities = homestay.HomestayAmenities.Select(a => (a.Amenity.Name, a.Amenity.Icon)).ToList(),
            RoomTypes = offers,
            GeneralPolicy = RulesOf(PolicyScope.All) ?? [],
            HolidayPolicy = RulesOf(PolicyScope.Holiday),
            Query = query,
            DepositRate = _options.DepositRate,
        });
    }
}
