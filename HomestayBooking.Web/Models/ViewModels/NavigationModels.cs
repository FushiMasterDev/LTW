using System.Security.Claims;
using HomestayBooking.Web.Identity;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Một khu làm việc nội bộ (Area) mà tài khoản được vào. Label là khóa chuỗi tiếng Việt để L[...] dịch.</summary>
public record WorkspaceLink(string Area, string Label, string Icon);

/// <summary>Một mục trong sidebar dashboard; chỉ trỏ tới action đã có.</summary>
public record SidebarLink(string Area, string Controller, string Label, string Icon, int? Count = null);

public static class Workspaces
{
    /// <summary>
    /// Khu làm việc theo đúng điều kiện phân quyền của từng Area:
    /// Admin = policy Management, Accounting = policy Accounting, Staff = policy Employee, Owner = role Owner.
    /// </summary>
    public static List<WorkspaceLink> For(ClaimsPrincipal user)
    {
        var links = new List<WorkspaceLink>();
        if (user.IsManagement())
        {
            links.Add(new("Admin", "Quản trị", "bi-shield-lock"));
        }
        if (user.IsAccounting())
        {
            links.Add(new("Accounting", "Kế toán", "bi-calculator"));
        }
        if (user.IsActiveEmployee())
        {
            links.Add(new("Staff", "Nhân viên", "bi-headset"));
        }
        if (user.IsInRole(AppRoles.Owner))
        {
            links.Add(new("Owner", "Chủ homestay", "bi-house-gear"));
        }
        return links;
    }
}
