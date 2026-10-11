using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class VoucherStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource.</summary>
    public static string DisplayName(this VoucherStatus status) => status switch
    {
        VoucherStatus.Active => "Còn dùng được",
        VoucherStatus.UsedUp => "Đã dùng hết",
        VoucherStatus.Expired => "Hết hạn",
        VoucherStatus.Revoked => "Đã thu hồi",
        _ => status.ToString(),
    };

    public static string ToneClass(this VoucherStatus status) => status switch
    {
        VoucherStatus.Active => "tone-emerald",
        VoucherStatus.UsedUp => "tone-sky",
        VoucherStatus.Revoked => "tone-rose",
        _ => "tone-slate",
    };
}
