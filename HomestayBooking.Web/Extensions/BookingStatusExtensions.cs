using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class BookingStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource để dịch.</summary>
    public static string DisplayName(this BookingStatus status) => status switch
    {
        BookingStatus.AwaitingPayment => "Chờ thanh toán",
        BookingStatus.PaymentSubmitted => "Chờ duyệt cọc",
        BookingStatus.Deposited => "Đã đặt cọc",
        BookingStatus.Confirmed => "Đã xác nhận",
        BookingStatus.CheckedIn => "Đã nhận phòng",
        BookingStatus.Completed => "Hoàn thành",
        BookingStatus.Expired => "Hết hạn giữ phòng",
        BookingStatus.Rejected => "Homestay từ chối",
        BookingStatus.Cancelled => "Đã hủy",
        BookingStatus.NoShow => "Không đến",
        _ => status.ToString(),
    };

    /// <summary>Class màu (tone-*) trong tokens.css cho badge trạng thái.</summary>
    public static string ToneClass(this BookingStatus status) => status switch
    {
        BookingStatus.AwaitingPayment => "tone-amber",
        BookingStatus.PaymentSubmitted => "tone-orange",
        BookingStatus.Deposited => "tone-violet",
        BookingStatus.Confirmed => "tone-sky",
        BookingStatus.CheckedIn => "tone-teal",
        BookingStatus.Completed => "tone-emerald",
        BookingStatus.Rejected => "tone-rose",
        _ => "tone-slate",
    };

    /// <summary>Biểu tượng Bootstrap Icons cho từng trạng thái.</summary>
    public static string Icon(this BookingStatus status) => status switch
    {
        BookingStatus.AwaitingPayment => "bi-qr-code",
        BookingStatus.PaymentSubmitted => "bi-receipt",
        BookingStatus.Deposited => "bi-wallet2",
        BookingStatus.Confirmed => "bi-check2-circle",
        BookingStatus.CheckedIn => "bi-door-open",
        BookingStatus.Completed => "bi-trophy",
        BookingStatus.Expired => "bi-hourglass-bottom",
        BookingStatus.Rejected => "bi-x-circle",
        BookingStatus.Cancelled => "bi-slash-circle",
        BookingStatus.NoShow => "bi-person-x",
        _ => "bi-circle",
    };

    /// <summary>Các trạng thái đang chiếm lịch phòng (RoomNights) — mục 6.1.</summary>
    public static bool HoldsRoom(this BookingStatus status) => status is
        BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted or BookingStatus.Deposited
        or BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed;
}
