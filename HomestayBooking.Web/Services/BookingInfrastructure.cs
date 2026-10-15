using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>Ngày giờ Việt Nam: ngày ở, mã đơn, số ngày trước khi nhận phòng đều tính theo giờ Việt Nam.</summary>
public static class VietnamTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    /// <summary>Số phút giờ hành chính (thứ Hai–thứ Sáu, [startHour, endHour) giờ Việt Nam) nằm trong khoảng [fromUtc, toUtc).</summary>
    public static double OfficeMinutesBetween(DateTime fromUtc, DateTime toUtc, int startHour, int endHour)
    {
        if (toUtc <= fromUtc)
        {
            return 0;
        }
        var from = ToLocal(fromUtc);
        var to = ToLocal(toUtc);
        var minutes = 0.0;
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }
            var open = day.AddHours(startHour);
            var close = day.AddHours(endHour);
            var start = from > open ? from : open;
            var end = to < close ? to : close;
            if (end > start)
            {
                minutes += (end - start).TotalMinutes;
            }
        }
        return minutes;
    }

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow().UtcDateTime));
}

/// <summary>Lỗi nghiệp vụ có thông báo hiển thị được cho người dùng (là khóa trong SharedResource).</summary>
public class BookingException : Exception
{
    public string ResourceKey { get; }
    public object[] Arguments { get; }

    public BookingException(string message, params object[] arguments)
        : base(arguments.Length == 0 ? message : string.Format(System.Globalization.CultureInfo.InvariantCulture, message, arguments))
    {
        ResourceKey = message;
        Arguments = arguments;
    }
}

/// <summary>Ai thực hiện một thao tác trên đơn — ghi vào BookingStatusHistories.</summary>
public record Actor(ActorType Type, string? AccountId = null, int? EmployeeId = null)
{
    public static readonly Actor System = new(ActorType.System);
}

/// <summary>Cấu hình đặt phòng (appsettings: "Booking").</summary>
public class BookingOptions
{
    public const string Section = "Booking";

    /// <summary>Giữ phòng chờ thanh toán (D5).</summary>
    public int HoldMinutes { get; set; } = 15;

    /// <summary>Hạn homestay xác nhận sau khi có cọc (D12, BR-06).</summary>
    public int ConfirmMinutes { get; set; } = 30;

    /// <summary>Tỷ lệ cọc (BR-01).</summary>
    [Range(typeof(decimal), "0.0001", "1", ParseLimitsInInvariantCulture = true)]
    public decimal DepositRate { get; set; } = 0.30m;

    /// <summary>Số ngày tối thiểu trước ngày nhận phòng cũ để được đổi.</summary>
    [Range(typeof(int), "1", "90", ParseLimitsInInvariantCulture = true)]
    public int ChangeMinDaysBefore { get; set; } = 3;

    /// <summary>Số yêu cầu đổi đã áp dụng tối đa trên chính đơn.</summary>
    [Range(typeof(int), "1", "10", ParseLimitsInInvariantCulture = true)]
    public int MaxChangesPerBooking { get; set; } = 2;

    /// <summary>Ngày nhận/trả phòng mới phải nằm trong số ngày này kể từ lúc gửi.</summary>
    [Range(typeof(int), "1", "365", ParseLimitsInInvariantCulture = true)]
    public int ChangeWindowDays { get; set; } = 90;

    /// <summary>Số đơn chờ thanh toán tối đa của một khách (chống giữ phòng ảo).</summary>
    public int MaxAwaitingPerCustomer { get; set; } = 3;

    public int MaxNights { get; set; } = 30;

    /// <summary>Bill bị từ chối đến lần này thì đơn hết hạn, mở lại phòng (chống gửi bill giả để giữ phòng mãi).</summary>
    public int MaxReceiptRejections { get; set; } = 2;

    /// <summary>Bill chờ duyệt quá số phút làm việc này → báo quản lý.</summary>
    public int ReceiptReviewAlertMinutes { get; set; } = 120;

    /// <summary>Giờ hành chính (giờ Việt Nam, thứ Hai–thứ Sáu) để tính thời gian chờ duyệt bill.</summary>
    public int OfficeStartHour { get; set; } = 8;

    public int OfficeEndHour { get; set; } = 17;
}

/// <summary>Tài khoản MB Bank nhận tiền cọc và khóa webhook (appsettings: "BankAccount"; khóa để trong user-secrets).</summary>
public class BankAccountOptions
{
    public const string Section = "BankAccount";

    /// <summary>Mã ngân hàng theo VietQR: MB = MB Bank (BIN 970422).</summary>
    public string BankCode { get; set; } = "MB";

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Khóa API dịch vụ theo dõi tài khoản (SePay) gửi trong header "Authorization: Apikey ...".</summary>
    public string? WebhookApiKey { get; set; }
}

/// <summary>Quy tắc voucher (appsettings: "Voucher"), Q4 / D14.</summary>
public class VoucherOptions
{
    public const string Section = "Voucher";

    /// <summary>Giá trị voucher = tỷ lệ này × tổng tiền đơn gốc, làm tròn nghìn, kẹp trong [MinAmount, MaxAmount].</summary>
    public decimal IssueRate { get; set; } = 0.05m;

    public decimal MinAmount { get; set; } = 50_000m;
    public decimal MaxAmount { get; set; } = 200_000m;
    public int ValidMonths { get; set; } = 6;

    /// <summary>Mỗi đơn giảm tối đa tỷ lệ này của hoa hồng Fuji trên đơn đó (Fuji nhận 300k thì chịu tối đa 60k).</summary>
    public decimal MaxDiscountShareOfCommission { get; set; } = 0.20m;
}
