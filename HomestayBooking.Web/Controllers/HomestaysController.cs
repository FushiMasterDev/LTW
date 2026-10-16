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
    private const int PageSize = 9;

    private readonly ApplicationDbContext _db;
    private readonly PricingService _pricing;
    private readonly TimeProvider _clock;
    private readonly BookingOptions _options;

    public HomestaysController(ApplicationDbContext db, PricingService pricing, TimeProvider clock, IOptions<BookingOptions> options)
    {
        _db = db;
        _pricing = pricing;
        _clock = clock;
        _options = options.Value;
    }

    private IQueryable<Homestay> Bookable(DateOnly today, DateOnly? checkOut = null) =>
        _db.Homestays.Where(h => h.Status == HomestayStatus.Active
                                 && _db.Contracts.Covering(today, checkOut ?? today).Any(c => c.HomestayId == h.Id));

    public async Task<IActionResult> Index(HomestaySearchQuery query)
    {
        var today = VietnamTime.Today(_clock);
        query.Guests = Math.Clamp(query.Guests, 1, 20);
        if (query.CheckIn < today)
        {
            query.CheckIn = today;
        }

        var homestays = Bookable(today, query.HasDates ? query.CheckOut : null);
        if (query.AreaId is int areaId)
        {
            homestays = homestays.Where(h => h.AreaId == areaId);
        }
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim();
            homestays = homestays.Where(h => h.Name.Contains(q) || h.Area.Name.Contains(q));
        }

        // Còn ít nhất một loại phòng đủ sức chứa (và có phòng trống nếu đã chọn ngày)
        var guests = query.Guests;
        if (query.HasDates)
        {
            DateOnly checkIn = query.CheckIn!.Value, checkOut = query.CheckOut!.Value;
            homestays = homestays.Where(h => h.RoomTypes.Any(t => t.IsActive && t.MaxGuests >= guests
                && t.Rooms.Any(r => r.IsActive && !r.RoomNights.Any(n => n.Date >= checkIn && n.Date < checkOut))));
        }
        else
        {
            homestays = homestays.Where(h => h.RoomTypes.Any(t => t.IsActive && t.MaxGuests >= guests));
        }

        var rows = await homestays
            .OrderBy(h => h.Name)
            .Select(h => new
            {
                h.Id,
                h.Name,
                AreaName = h.Area.Name,
                Cover = h.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault(),
                MaxGuests = h.RoomTypes.Where(t => t.IsActive).Max(t => (int?)t.MaxGuests) ?? 0,
                Amenities = h.HomestayAmenities.OrderBy(a => a.AmenityId).Select(a => a.Amenity.Name).Take(4).ToList(),
                RoomTypeIds = h.RoomTypes.Where(t => t.IsActive && t.MaxGuests >= guests).Select(t => t.Id).ToList(),
            })
            .ToListAsync();

        // Giá "từ" = giá ngày thường thấp nhất đang áp dụng. Lọc giá và phân trang trong bộ nhớ (số homestay nhỏ).
        var roomTypeIds = rows.SelectMany(r => r.RoomTypeIds).ToList();
        var prices = PricingService.CurrentPrices(
            await _db.RoomTypePrices.AsNoTracking().Where(p => roomTypeIds.Contains(p.RoomTypeId)).ToListAsync(), today);
        var cards = rows
            .Select(r => new HomestayCard(r.Id, r.Name, r.AreaName, r.Cover,
                r.RoomTypeIds.Select(id => prices.TryGetValue((id, DayType.Weekday), out var p) ? p : (decimal?)null).Min(),
                r.MaxGuests, r.Amenities))
            .Where(c => query.MaxPrice is null || c.FromPrice <= query.MaxPrice)
            .ToList();

        var page = Math.Max(1, query.Page ?? 1);
        var model = new HomestaySearchViewModel
        {
            Query = query,
            Areas = await _db.Areas.OrderBy(a => a.Name).Select(a => new AreaOption(a.Id, a.Name, a.AreaGroup)).ToListAsync(),
            Results = new PaginatedList<HomestayCard>(cards.Skip((page - 1) * PageSize).Take(PageSize), cards.Count, page, PageSize),
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id, HomestaySearchQuery query)
    {
        var today = VietnamTime.Today(_clock);
        var homestay = await Bookable(today, query.HasDates ? query.CheckOut : null)
            .AsNoTracking()
            .Include(h => h.Area)
            .Include(h => h.Images)
            .Include(h => h.HomestayAmenities).ThenInclude(a => a.Amenity)
            .Include(h => h.RoomTypes.Where(t => t.IsActive)).ThenInclude(t => t.Category)
            .Include(h => h.RoomTypes.Where(t => t.IsActive)).ThenInclude(t => t.Prices)
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

        var offers = new List<RoomTypeOffer>();
        foreach (var type in homestay.RoomTypes.OrderBy(t => t.MaxGuests).ThenBy(t => t.Name))
        {
            int? free = null;
            PriceQuote? quote = null;
            if (query.HasDates)
            {
                free = await _db.Rooms.CountAsync(r => r.RoomTypeId == type.Id && r.IsActive
                    && !r.RoomNights.Any(n => n.Date >= query.CheckIn && n.Date < query.CheckOut));
                try
                {
                    quote = await _pricing.QuoteAsync(type.Id, query.CheckIn!.Value, query.CheckOut!.Value);
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
