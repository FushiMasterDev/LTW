using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>Ngày giờ Việt Nam: ngày ở, mã đơn, số ngày trước khi nhận phòng đều tính theo giờ Việt Nam.</summary>
public static class VietnamTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow().UtcDateTime));
}

/// <summary>Lỗi nghiệp vụ có thông báo hiển thị được cho người dùng (là khóa trong SharedResource).</summary>
public class BookingException : Exception
{
    public BookingException(string message) : base(message)
    {
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
    public decimal DepositRate { get; set; } = 0.30m;

    /// <summary>Số đơn chờ thanh toán tối đa của một khách (chống giữ phòng ảo).</summary>
    public int MaxAwaitingPerCustomer { get; set; } = 3;

    public int MaxNights { get; set; } = 30;
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
