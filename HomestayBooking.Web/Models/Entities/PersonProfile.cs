namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Thông tin cá nhân dùng chung cho Customer, Owner, Employee (v5 mục 4.1).
/// Có thể tồn tại trước Account (ví dụ nhân viên tạo khách qua điện thoại).
/// </summary>
public class PersonProfile
{
    public int Id { get; set; }

    /// <summary>Có thể chưa đủ khi là hồ sơ nháp.</summary>
    public string? FullName { get; set; }

    /// <summary>Chuẩn hóa E.164 (+84...), unique; là định danh đăng nhập của Customer/Owner.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Account { get; set; }
    public Customer? Customer { get; set; }
    public Owner? Owner { get; set; }
    public Employee? Employee { get; set; }
}
