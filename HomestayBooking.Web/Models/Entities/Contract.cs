namespace HomestayBooking.Web.Models.Entities;

/// <summary>Hợp đồng hợp tác; hoa hồng là điều khoản của hợp đồng (D6, BR-02). Gia hạn = hợp đồng mới.</summary>
public class Contract
{
    public int Id { get; set; }
    public int HomestayId { get; set; }

    /// <summary>Ví dụ HD-2026-0035, unique.</summary>
    public string ContractNo { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Tỷ lệ hoa hồng, ví dụ 0.12 = 12%.</summary>
    public decimal CommissionRate { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public int? SignedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Homestay Homestay { get; set; } = null!;
    public Employee? SignedByEmployee { get; set; }
}
