using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Models.ViewModels.Ui;
namespace HomestayBooking.Web.Models.ViewModels;

public record OwnerHomestaysModel(PaginatedList<OwnerHomestaySummary> Homes, string? Query);
public record OwnerEditorModel(HomestayEditor Home, string Tab, DateOnly Today)
{
    public OwnerContentForm Content { get; init; } = new();
    public OwnerLocationForm Location { get; init; } = new();
    public IReadOnlyList<AreaOption> Areas { get; init; } = [];
}
public class OwnerContentForm
{
    [Required, StringLength(200), Display(Name="Tên homestay")] public string Name { get; set; } = "";
    [Display(Name="Mô tả")] public string? DescriptionHtml { get; set; }
    [Display(Name="Hướng dẫn nhận phòng")] public string? CheckinGuideHtml { get; set; }
    [Required, Display(Name="Số điện thoại liên hệ")] public string ContactPhone { get; set; } = "";
    [Display(Name="Giờ nhận phòng")] public TimeOnly CheckinTime { get; set; }
    [Display(Name="Giờ trả phòng")] public TimeOnly CheckoutTime { get; set; }
}
public class OwnerLocationForm
{
    [Range(1,int.MaxValue), Display(Name="Khu vực")] public int AreaId { get; set; }
    [Required, StringLength(300), Display(Name="Địa chỉ")] public string Address { get; set; } = "";
    [Display(Name="Vĩ độ")] public decimal? Latitude { get; set; }
    [Display(Name="Kinh độ")] public decimal? Longitude { get; set; }
}
public record OwnerCalendarModel(IReadOnlyList<OwnerHomestaySummary> Homes, int? HomestayId, int? RoomId,
    RoomCalendarModel Calendar, IReadOnlyList<CalendarNight> Nights, DateOnly Today);
public record OwnerScheduleModel(OwnerSchedule Schedule, string Period, DateOnly From, DateOnly Through);
public record OwnerReviewsModel(IReadOnlyList<OwnerHomestaySummary> Homes, int? HomestayId, ReviewPage? Page);

public static class OwnerWorkspaceLabels
{
    public static string DisplayName(this DayType day) => day switch { DayType.Weekday => "Ngày thường", DayType.Weekend => "Cuối tuần", _ => "Ngày lễ" };
    public static string DisplayName(this DocumentType type) => type switch { DocumentType.BusinessLicense => "Giấy phép kinh doanh", DocumentType.FireSafety => "Giấy PCCC", DocumentType.PropertyPhoto => "Ảnh thực tế", _ => "Khác" };
}
