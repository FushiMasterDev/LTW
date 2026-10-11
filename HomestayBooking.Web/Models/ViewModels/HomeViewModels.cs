namespace HomestayBooking.Web.Models.ViewModels;

public record AreaSummary(int Id, string Name, int HomestayCount);

public class HomeIndexViewModel
{
    public int HomestayCount { get; init; }
    public int RoomCount { get; init; }
    public IReadOnlyList<AreaSummary> Areas { get; init; } = [];
}
