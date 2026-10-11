namespace HomestayBooking.Web.Models.Entities;

/// <summary>Nhật ký sự kiện tài khoản, append-only (v5 mục 4.10). Không ghi mật khẩu/OTP thô.</summary>
public class AccountActivityLog
{
    public long Id { get; set; }

    /// <summary>Null khi sự kiện xảy ra trước khi Account tồn tại.</summary>
    public string? AccountId { get; set; }

    public AccountEventType EventType { get; set; }
    public ActorType ActorType { get; set; }

    /// <summary>Id người thực hiện (đa hình theo ActorType), không đặt khóa ngoại.</summary>
    public string? ActorId { get; set; }

    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Account { get; set; }
}
