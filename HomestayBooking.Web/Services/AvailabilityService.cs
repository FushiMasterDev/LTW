using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public record AvailabilityOffer(int HomestayId, string HomestayName, int AreaId, string AreaName,
    int RoomTypeId, string RoomTypeName, int CategoryId, int MaxGuests, int FreeRooms, int TotalRooms,
    decimal? TotalPrice, double? Rating, int ReviewCount);

/// <summary>Điều kiện bán dùng chung; mọi khoảng lưu trú đều là [nhận, trả), không cache tồn phòng.</summary>
public class AvailabilityService(ApplicationDbContext db, TimeProvider clock, IOptions<BookingOptions> options)
{
    public IQueryable<Homestay> BookableHomestays(DateOnly? checkOut = null)
    {
        var today = VietnamTime.Today(clock);
        return db.Homestays.Where(h => h.Status == HomestayStatus.Active
            && db.Contracts.Covering(today, checkOut ?? today).Any(c => c.HomestayId == h.Id));
    }

    public IQueryable<RoomNight> NightsDuring(DateOnly checkIn, DateOnly checkOut) =>
        db.RoomNights.Where(n => n.Date >= checkIn && n.Date < checkOut);

    public IQueryable<Room> SellableRooms(DateOnly? checkIn = null, DateOnly? checkOut = null, int guests = 1)
    {
        var homes = BookableHomestays(checkOut).Select(h => h.Id);
        var rooms = db.Rooms.Where(r => r.IsActive && r.RoomType.IsActive && r.RoomType.MaxGuests >= guests
            && homes.Contains(r.RoomType.HomestayId));
        if (checkIn.HasValue && checkOut.HasValue)
        {
            var occupied = NightsDuring(checkIn.Value, checkOut.Value).Select(n => n.RoomId);
            rooms = rooms.Where(r => !occupied.Contains(r.Id));
        }
        return rooms;
    }

    public Task<List<int>> FreeRoomIdsAsync(int typeId, DateOnly checkIn, DateOnly checkOut) =>
        SellableRooms(checkIn, checkOut).Where(r => r.RoomTypeId == typeId).OrderBy(r => r.Id).Select(r => r.Id).ToListAsync();

    private void Normalize(HomestaySearchQuery query)
    {
        var today = VietnamTime.Today(clock);
        query.Guests = Math.Clamp(query.Guests, 1, 20);
        if (query.CheckIn < today) query.CheckIn = today;
        if (query.HasDates && query.CheckOut!.Value.DayNumber - query.CheckIn!.Value.DayNumber > options.Value.MaxNights)
            throw new BookingException("Thời gian lưu trú quá dài.");
        if (query.MinPrice < 0 || query.MaxPrice < 0 || (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice))
            throw new BookingException("Khoảng giá không hợp lệ.");
        if (!Enum.IsDefined(query.Sort)) throw new BookingException("Cách sắp xếp không hợp lệ.");
    }

    // Nạp giá/ngày lễ một lần và đêm bị chiếm một lần cho cả danh sách, không gọi QuoteAsync trong vòng lặp.
    private async Task<(List<AvailabilityOffer> Offers, List<Homestay> Homes)> LoadAsync(HomestaySearchQuery query)
    {
        var roomsQuery = SellableRooms(checkOut: query.HasDates ? query.CheckOut : null, guests: query.Guests);
        if (query.AreaId is int area) roomsQuery = roomsQuery.Where(r => r.RoomType.Homestay.AreaId == area);
        if (query.CategoryId is int category) roomsQuery = roomsQuery.Where(r => r.RoomType.CategoryId == category);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            roomsQuery = roomsQuery.Where(r => r.RoomType.Homestay.Name.Contains(term) || r.RoomType.Homestay.Area.Name.Contains(term));
        }
        foreach (var amenity in query.AmenityIds.Distinct())
            roomsQuery = roomsQuery.Where(r => r.RoomType.Homestay.HomestayAmenities.Any(a => a.AmenityId == amenity));
        var rooms = await roomsQuery.Select(r => new { r.Id, r.RoomTypeId }).ToListAsync();
        var typeIds = rooms.Select(r => r.RoomTypeId).Distinct().ToArray();
        var types = await db.RoomTypes.AsNoTracking().Where(t => typeIds.Contains(t.Id))
            .Include(t => t.Homestay).ThenInclude(h => h.Area)
            .Include(t => t.Homestay).ThenInclude(h => h.Images)
            .Include(t => t.Homestay).ThenInclude(h => h.HomestayAmenities).ThenInclude(a => a.Amenity).AsSplitQuery().ToListAsync();
        var homes = types.Select(t => t.Homestay).DistinctBy(h => h.Id).ToList();
        var homeIds = homes.Select(h => h.Id).ToArray();
        var ratings = await db.Reviews.Where(r => homeIds.Contains(r.HomestayId) && !r.IsHidden).GroupBy(r => r.HomestayId)
            .Select(g => new { Id = g.Key, Rating = g.Average(r => (double)r.Rating), Count = g.Count() }).ToDictionaryAsync(r => r.Id);
        var prices = await db.RoomTypePrices.AsNoTracking().Where(p => typeIds.Contains(p.RoomTypeId)).ToListAsync();
        var occupied = new HashSet<int>(); var holidays = new HashSet<DateOnly>();
        if (query.HasDates)
        {
            var ids = rooms.Select(r => r.Id).ToArray();
            occupied = (await NightsDuring(query.CheckIn!.Value, query.CheckOut!.Value).Where(n => ids.Contains(n.RoomId))
                .Select(n => n.RoomId).Distinct().ToListAsync()).ToHashSet();
            holidays = (await db.Holidays.Where(h => h.Date >= query.CheckIn && h.Date < query.CheckOut).Select(h => h.Date).ToListAsync()).ToHashSet();
        }
        var current = PricingService.CurrentPrices(prices, VietnamTime.Today(clock));
        var offers = new List<AvailabilityOffer>();
        foreach (var type in types)
        {
            decimal? price;
            if (query.HasDates)
            {
                try { price = PricingService.QuoteFromPrices(type.Id, query.CheckIn!.Value, query.CheckOut!.Value, prices,
                    holidays, options.Value.DepositRate, Math.Max(31, options.Value.MaxNights)).Total; }
                catch (BookingException) { continue; }
            }
            else price = current.TryGetValue((type.Id, DayType.Weekday), out var value) ? value : null;
            var typeRooms = rooms.Where(r => r.RoomTypeId == type.Id).ToList();
            ratings.TryGetValue(type.HomestayId, out var rating);
            offers.Add(new(type.HomestayId, type.Homestay.Name, type.Homestay.AreaId, type.Homestay.Area.Name, type.Id, type.Name,
                type.CategoryId, type.MaxGuests, typeRooms.Count(r => !occupied.Contains(r.Id)), typeRooms.Count, price, rating?.Rating, rating?.Count ?? 0));
        }
        return (offers, homes);
    }

    public async Task<HomestaySearchViewModel> SearchAsync(HomestaySearchQuery query)
    {
        Normalize(query);
        var (offers, homes) = await LoadAsync(query);
        var cards = offers.Where(o => o.FreeRooms > 0).GroupBy(o => o.HomestayId).Select(g =>
        {
            var home = homes.Single(h => h.Id == g.Key);
            return new HomestayCard(home.Id, home.Name, home.Area.Name, home.Images.FirstOrDefault(i => i.IsCover)?.Url,
                query.HasDates && g.Min(o => o.TotalPrice) is decimal total
                    ? PricingService.RoundToThousand(total / (query.CheckOut!.Value.DayNumber - query.CheckIn!.Value.DayNumber))
                    : g.Min(o => o.TotalPrice),
                offers.Where(o => o.HomestayId == g.Key).Max(o => o.MaxGuests),
                home.HomestayAmenities.OrderBy(a => a.AmenityId).Take(4).Select(a => a.Amenity.Name).ToList());
        }).Where(c => (query.MinPrice == null || c.FromPrice >= query.MinPrice) && (query.MaxPrice == null || c.FromPrice <= query.MaxPrice));
        cards = query.Sort switch
        {
            HomestaySort.PriceAscending => cards.OrderBy(c => c.FromPrice ?? decimal.MaxValue).ThenBy(c => c.Name),
            HomestaySort.PriceDescending => cards.OrderByDescending(c => c.FromPrice ?? decimal.MinValue).ThenBy(c => c.Name),
            HomestaySort.Rating => cards.OrderByDescending(c => offers.First(o => o.HomestayId == c.Id).Rating ?? 0).ThenBy(c => c.Name),
            _ => cards.OrderBy(c => c.Name),
        };
        var all = cards.ToList(); const int size = 9;
        var page = Math.Clamp(query.Page ?? 1, 1, Math.Max(1, (int)Math.Ceiling(all.Count / (double)size)));
        return new() { Query = query, Areas = await db.Areas.OrderBy(a => a.Name).Select(a => new AreaOption(a.Id, a.Name, a.AreaGroup)).ToListAsync(),
            Results = new PaginatedList<HomestayCard>(all.Skip((page - 1) * size).Take(size), all.Count, page, size) };
    }

    public async Task<List<AvailabilityOffer>> StaffAvailabilityAsync(DateOnly checkIn, DateOnly checkOut, int? areaId, int guests, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
        ValidateStaffDates(checkIn, checkOut, guests);
        return (await LoadAsync(new() { CheckIn = checkIn, CheckOut = checkOut, AreaId = areaId, Guests = guests })).Offers
            .OrderBy(o => o.HomestayName).ThenBy(o => o.RoomTypeName).ToList();
    }

    private void ValidateStaffDates(DateOnly start, DateOnly end, int guests)
    {
        if (start < VietnamTime.Today(clock) || end <= start || end.DayNumber - start.DayNumber > 31 || guests < 1)
            throw new BookingException("Chọn khoảng ngày từ hôm nay, tối đa 31 đêm và số khách lớn hơn 0.");
    }

    internal async Task<List<AvailabilityOffer>> AlternativesAsync(Booking booking)
    {
        var offers = (await LoadAsync(new() { CheckIn = booking.CheckIn, CheckOut = booking.CheckOut, Guests = booking.Guests })).Offers;
        return offers.Where(o => o.FreeRooms > 0).OrderBy(o => o.HomestayId == booking.HomestayId ? 0 : o.AreaId == booking.Homestay.AreaId ? 1 : 2)
            .ThenBy(o => o.TotalPrice).ThenByDescending(o => o.Rating).ToList();
    }

    public async Task<List<FeaturedHomestay>> FeaturedAsync(int count = 6)
    {
        var (offers, homes) = await LoadAsync(new() { Guests = 1 });
        return offers.GroupBy(o => o.HomestayId).Select(g =>
        {
            var home = homes.Single(h => h.Id == g.Key); var first = g.First();
            return new FeaturedHomestay(home.Id, home.Name, home.Area.Name, home.Images.FirstOrDefault(i => i.IsCover)?.Url,
                g.Min(o => o.TotalPrice), first.Rating, first.ReviewCount);
        }).OrderByDescending(h => h.Rating ?? 0).ThenByDescending(h => h.ReviewCount).ThenBy(h => h.Name).Take(count).ToList();
    }
}
