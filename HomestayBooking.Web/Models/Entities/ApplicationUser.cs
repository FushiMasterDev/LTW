using Microsoft.AspNetCore.Identity;

namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// ACCOUNT dùng chung (v5 mục 4.2): chỉ lo xác thực và trạng thái tài khoản.
/// Mật khẩu băm, khóa sau nhiều lần sai: dùng sẵn của Identity (PasswordHash, AccessFailedCount, LockoutEnd).
/// google_sub lưu ở AspNetUserLogins (LoginProvider = "Google", ProviderKey = sub), khóa chính bảo đảm unique.
/// Vai trò (ACCOUNT_ROLE) lưu ở AspNetUserRoles — xem <see cref="ApplicationUserRole"/>.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Identity bắt buộc UserName duy nhất, nhưng định danh đăng nhập thật là SĐT (PERSON_PROFILE) hoặc mã nhân viên.
    /// Dùng luôn Id để không lưu SĐT lần thứ hai trong ACCOUNT (v5 mục 4.2).
    /// </summary>
    public ApplicationUser()
    {
        UserName = Id;
    }

    public int PersonId { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.PendingActivation;
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PersonProfile Person { get; set; } = null!;
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
