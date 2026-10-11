namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Dữ liệu đặc thù của Customer (v5 mục 4.5), quan hệ 1–1 khóa chung với Customer.
/// CitizenId/StudentId là dữ liệu nhạy cảm: giới hạn quyền xem, che một phần khi hiển thị.
/// </summary>
public class CustomerProfile
{
    public int CustomerId { get; set; }
    public string? CitizenId { get; set; }
    public string? StudentId { get; set; }
    public StudentVerificationStatus StudentVerificationStatus { get; set; } = StudentVerificationStatus.NotSubmitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
