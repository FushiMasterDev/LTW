using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class HomestayStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource để dịch.</summary>
    public static string DisplayName(this HomestayStatus status) => status switch
    {
        HomestayStatus.Draft => "Bản nháp",
        HomestayStatus.PendingReview => "Chờ duyệt",
        HomestayStatus.Active => "Đang hoạt động",
        HomestayStatus.Suspended => "Tạm ngưng",
        HomestayStatus.Ended => "Ngừng hợp tác",
        _ => status.ToString(),
    };

    public static string ToneClass(this HomestayStatus status) => status switch
    {
        HomestayStatus.Active => "tone-emerald",
        HomestayStatus.PendingReview => "tone-amber",
        HomestayStatus.Suspended => "tone-orange",
        _ => "tone-slate",
    };
}
