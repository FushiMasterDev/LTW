using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Tìm Account cho hai cổng đăng nhập (v5 mục 8). Trả null cho mọi trường hợp không đủ điều kiện
/// để controller chỉ báo một thông báo lỗi chung, không tiết lộ SĐT/mã nhân viên có tồn tại hay không.
/// Trạng thái Account và mật khẩu do SignInManager kiểm tra sau đó.
/// </summary>
public class AccountLookupService
{
    private readonly ApplicationDbContext _db;

    public AccountLookupService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Cổng Customer/Owner: SĐT (chuẩn hóa E.164) → PERSON_PROFILE → ACCOUNT, cần role Customer hoặc Owner.</summary>
    public async Task<ApplicationUser?> FindForPhoneLoginAsync(string? phoneNumber)
    {
        var phone = PhoneNumberNormalizer.ToE164(phoneNumber);
        if (phone is null)
        {
            return null;
        }

        return await _db.Users
            .Where(u => u.Person.PhoneNumber == phone)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == AppRoles.Customer || ur.Role.Name == AppRoles.Owner))
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Cổng nội bộ: mã nhân viên → EMPLOYEE → person_id → ACCOUNT, cần role Employee và nhân viên đang làm việc.
    /// Không dùng SĐT để đăng nhập cổng nội bộ (v5 mục 8.2).
    /// </summary>
    public async Task<ApplicationUser?> FindForEmployeeLoginAsync(string? employeeCode)
    {
        var code = employeeCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        return await _db.Users
            .Where(u => u.Person.Employee != null
                        && u.Person.Employee.EmployeeCode == code
                        && u.Person.Employee.EmploymentStatus == EmploymentStatus.Active)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == AppRoles.Employee))
            .SingleOrDefaultAsync();
    }
}
