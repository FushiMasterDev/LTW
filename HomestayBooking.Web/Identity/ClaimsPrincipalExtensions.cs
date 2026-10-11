using System.Security.Claims;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Identity;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Họ tên từ claim; hồ sơ chưa có tên thì dùng tên đăng nhập.</summary>
    public static string DisplayName(this ClaimsPrincipal user)
    {
        var fullName = user.FindFirstValue(AppClaimTypes.FullName);
        return string.IsNullOrWhiteSpace(fullName) ? user.Identity?.Name ?? string.Empty : fullName;
    }

    public static int? PersonId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(AppClaimTypes.PersonId), out var id) ? id : null;

    /// <summary>Chức vụ khi là nhân viên đang làm việc; null nếu không phải.</summary>
    public static EmployeePosition? EmployeePosition(this ClaimsPrincipal user) =>
        Enum.TryParse<EmployeePosition>(user.FindFirstValue(AppClaimTypes.EmployeePosition), out var p) ? p : null;

    public static bool IsActiveEmployee(this ClaimsPrincipal user) => user.EmployeePosition() is not null;

    /// <summary>Quản lý hoặc quản trị hệ thống (policy Management).</summary>
    public static bool IsManagement(this ClaimsPrincipal user) =>
        user.EmployeePosition() is Models.Entities.EmployeePosition.Manager or Models.Entities.EmployeePosition.Administrator;

    public static bool IsApprovedOwner(this ClaimsPrincipal user) =>
        user.HasClaim(AppClaimTypes.OwnerApproved, "true");
}
