namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Chính sách hủy (BR-04). HomestayId = null là chính sách chung của Fuji;
/// chính sách riêng của homestay được ưu tiên (D10). Đã có đơn dùng thì không sửa, tạo chính sách mới.
/// </summary>
public class CancellationPolicy
{
    public int Id { get; set; }
    public int? HomestayId { get; set; }
    public PolicyScope Scope { get; set; } = PolicyScope.All;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Homestay? Homestay { get; set; }
    public ICollection<CancellationRule> Rules { get; set; } = new List<CancellationRule>();
}

/// <summary>Hủy trước ngày nhận phòng ≥ MinDaysBefore ngày → hoàn RefundPercent% tiền cọc.</summary>
public class CancellationRule
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int MinDaysBefore { get; set; }
    public int RefundPercent { get; set; }

    public CancellationPolicy Policy { get; set; } = null!;
}
