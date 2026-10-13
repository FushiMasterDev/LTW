using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class BankMatchStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource.</summary>
    public static string DisplayName(this BankMatchStatus status) => status switch
    {
        BankMatchStatus.Matched => "Tự khớp",
        BankMatchStatus.Underpaid => "Chuyển thiếu",
        BankMatchStatus.LatePayment => "Đến muộn",
        BankMatchStatus.Unmatched => "Không rõ đơn",
        BankMatchStatus.ManuallyMatched => "Khớp tay",
        BankMatchStatus.Refunded => "Đã hoàn",
        BankMatchStatus.Ignored => "Bỏ qua",
        _ => status.ToString(),
    };

    public static string ToneClass(this BankMatchStatus status) => status switch
    {
        BankMatchStatus.Matched or BankMatchStatus.ManuallyMatched => "tone-emerald",
        BankMatchStatus.Underpaid => "tone-amber",
        BankMatchStatus.LatePayment => "tone-orange",
        BankMatchStatus.Unmatched => "tone-rose",
        BankMatchStatus.Refunded => "tone-sky",
        _ => "tone-slate",
    };

    /// <summary>Việc kế toán cần làm với giao dịch ở trạng thái này.</summary>
    public static string Hint(this BankMatchStatus status) => status switch
    {
        BankMatchStatus.Underpaid => "Liên hệ khách chuyển đủ rồi khớp, hoặc hoàn lại khoản đã chuyển.",
        BankMatchStatus.LatePayment => "Đơn đã hết hạn hoặc bị hủy: hoàn tiền cho khách, hoặc nhờ nhân viên tư vấn tạo đơn mới nếu còn phòng.",
        BankMatchStatus.Unmatched => "Không đọc được mã đơn: tìm đơn theo số tiền và thời gian để khớp tay, hoặc hoàn lại.",
        _ => string.Empty,
    };
}
