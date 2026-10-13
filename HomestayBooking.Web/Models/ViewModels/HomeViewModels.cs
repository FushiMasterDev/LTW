namespace HomestayBooking.Web.Models.ViewModels;

public record AreaSummary(int Id, string Name, int HomestayCount);

/// <summary>Thẻ homestay nổi bật ở trang chủ: điểm đánh giá thật (đánh giá không bị ẩn) và giá ngày thường thấp nhất.</summary>
public record FeaturedHomestay(int Id, string Name, string AreaName, string? CoverUrl, decimal? FromPrice, double? Rating, int ReviewCount);

public class HomeIndexViewModel
{
    public int HomestayCount { get; init; }
    public int RoomCount { get; init; }
    public IReadOnlyList<AreaSummary> Areas { get; init; } = [];
    public IReadOnlyList<FeaturedHomestay> Featured { get; init; } = [];
}
