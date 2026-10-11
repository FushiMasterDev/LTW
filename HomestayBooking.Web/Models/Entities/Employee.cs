namespace HomestayBooking.Web.Models.Entities;

/// <summary>Hồ sơ nhân viên Fuji (v5 mục 4.8). Đăng nhập bằng EmployeeCode, mật khẩu kiểm tra qua Account.</summary>
public class Employee
{
    public int Id { get; set; }
    public int PersonId { get; set; }

    /// <summary>Mã nhân viên, unique, dùng để đăng nhập cổng nội bộ.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    public string? Department { get; set; }
    public EmployeePosition Position { get; set; } = EmployeePosition.Consultant;
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PersonProfile Person { get; set; } = null!;
}
