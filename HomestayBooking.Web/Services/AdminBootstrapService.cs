using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>Cấu hình "Bootstrap:Administrator"; Password chỉ đặt qua user-secrets/biến môi trường, không trong appsettings.json.</summary>
public class AdminBootstrapOptions
{
    public const string Section = "Bootstrap:Administrator";
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>FR-21: tạo Quản trị viên đầu tiên ngoài Development; Development dùng seed demo như cũ.</summary>
public class AdminBootstrapService(ApplicationDbContext db, AccountAdminService accounts, IOptions<AdminBootstrapOptions> options,
    IHostEnvironment environment, ILogger<AdminBootstrapService> logger)
{
    public async Task<bool> RunAsync()
    {
        if (environment.IsDevelopment()) return false;
        // Đã có Administrator (kể cả đang khóa/nghỉ) thì không tự tạo thêm; khôi phục quyền là việc của người vận hành.
        if (await db.Employees.AnyAsync(e => e.Position == EmployeePosition.Administrator)) return false;
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.EmployeeCode) || string.IsNullOrWhiteSpace(o.FullName)
            || string.IsNullOrWhiteSpace(o.Phone) || string.IsNullOrWhiteSpace(o.Password))
        {
            logger.LogError("Chưa có Quản trị viên nào và thiếu cấu hình {Section} (EmployeeCode, FullName, Phone, Password); không tự tạo tài khoản.",
                AdminBootstrapOptions.Section);
            return false;
        }
        var created = await accounts.BootstrapAdministratorAsync(new EmployeeAccountInput(o.FullName, o.Phone, o.Email, o.EmployeeCode,
            EmployeePosition.Administrator, o.Password));
        if (created is null) return false;
        logger.LogWarning("Đã tạo Quản trị viên đầu tiên {EmployeeCode}; hãy xóa {Section}:Password khỏi cấu hình.",
            o.EmployeeCode.Trim().ToUpperInvariant(), AdminBootstrapOptions.Section);
        return true;
    }
}
