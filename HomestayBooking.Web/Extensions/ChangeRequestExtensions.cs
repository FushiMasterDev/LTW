using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class ChangeRequestExtensions
{
    public static string DisplayName(this ChangeRequestType type) => type switch
    {
        ChangeRequestType.ChangeDates => "Đổi ngày",
        ChangeRequestType.ChangeRoomType => "Đổi loại phòng",
        ChangeRequestType.ChangeDatesAndRoomType => "Đổi ngày và loại phòng",
        ChangeRequestType.Cancel => "Hủy thay khách",
        _ => "Yêu cầu đổi",
    };

    public static string DisplayName(this ChangeRequestStatus status) => status switch
    {
        ChangeRequestStatus.Pending => "Chờ quản lý duyệt",
        ChangeRequestStatus.AwaitingPayment => "Chờ cọc bổ sung",
        ChangeRequestStatus.Applied => "Đã áp dụng",
        ChangeRequestStatus.Rejected => "Đã từ chối",
        ChangeRequestStatus.Expired => "Hết hạn nộp cọc bổ sung",
        _ => "Yêu cầu đổi",
    };
}
