using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class OwnerRegistrationStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource để dịch.</summary>
    public static string DisplayName(this OwnerRegistrationStatus status) => status switch
    {
        OwnerRegistrationStatus.Draft => "Hồ sơ nháp",
        OwnerRegistrationStatus.WaitingOwnerActivation => "Chờ chủ homestay kích hoạt",
        OwnerRegistrationStatus.PendingReview => "Chờ duyệt",
        OwnerRegistrationStatus.UnderReview => "Đang thẩm định",
        OwnerRegistrationStatus.NeedsRevision => "Cần bổ sung",
        OwnerRegistrationStatus.Approved => "Đã duyệt",
        OwnerRegistrationStatus.Rejected => "Bị từ chối",
        _ => status.ToString(),
    };

    public static string ToneClass(this OwnerRegistrationStatus status) => status switch
    {
        OwnerRegistrationStatus.Approved => "tone-emerald",
        OwnerRegistrationStatus.Rejected => "tone-rose",
        OwnerRegistrationStatus.NeedsRevision => "tone-orange",
        OwnerRegistrationStatus.PendingReview or OwnerRegistrationStatus.UnderReview => "tone-amber",
        _ => "tone-slate",
    };
}
