using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Bộ lọc tìm homestay (query string).</summary>
public class HomestaySearchQuery
{
    public int? AreaId { get; set; }
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Guests { get; set; } = 2;
    public decimal? MaxPrice { get; set; }
    public string? Q { get; set; }
    public int? Page { get; set; }

    /// <summary>Có đủ ngày hợp lệ để lọc theo phòng trống.</summary>
    public bool HasDates => CheckIn is not null && CheckOut is not null && CheckOut > CheckIn;
}

public record HomestayCard(
    int Id,
    string Name,
    string AreaName,
    string? CoverUrl,
    decimal? FromPrice,
    int MaxGuests,
    IReadOnlyList<string> Amenities);

public record AreaOption(int Id, string Name, AreaGroup Group);

public class HomestaySearchViewModel
{
    public HomestaySearchQuery Query { get; init; } = new();
    public IReadOnlyList<AreaOption> Areas { get; init; } = [];
    public PaginatedList<HomestayCard> Results { get; init; } = new([], 0, 1, PaginatedList<HomestayCard>.DefaultPageSize);
}

public record RoomTypeOffer(
    int Id,
    string Name,
    string CategoryName,
    int MaxGuests,
    int? AreaM2,
    string? Description,
    decimal? WeekdayPrice,
    decimal? WeekendPrice,
    decimal? HolidayPrice,
    int? FreeRooms,
    PriceQuote? Quote);

public record PolicyRuleView(int MinDaysBefore, int RefundPercent);

public class HomestayDetailsViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string AreaName { get; init; } = string.Empty;
    public string? DescriptionHtml { get; init; }
    public TimeOnly CheckinTime { get; init; }
    public TimeOnly CheckoutTime { get; init; }
    public IReadOnlyList<string> Images { get; init; } = [];
    public IReadOnlyList<(string Name, string? Icon)> Amenities { get; init; } = [];
    public IReadOnlyList<RoomTypeOffer> RoomTypes { get; init; } = [];
    public IReadOnlyList<PolicyRuleView> GeneralPolicy { get; init; } = [];
    public IReadOnlyList<PolicyRuleView>? HolidayPolicy { get; init; }
    public HomestaySearchQuery Query { get; init; } = new();
    public decimal DepositRate { get; init; }
}
