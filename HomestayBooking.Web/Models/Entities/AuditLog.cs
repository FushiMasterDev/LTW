namespace HomestayBooking.Web.Models.Entities;

/// <summary>Thay đổi dữ liệu nhạy cảm (giá, hợp đồng, thanh toán, đối soát) — NFR-04.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public string? AccountId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }

    /// <summary>JSON.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON.</summary>
    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Account { get; set; }
}
