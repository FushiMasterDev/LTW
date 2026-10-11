using Microsoft.AspNetCore.Identity;

namespace HomestayBooking.Web.Models.Entities;

/// <summary>ACCOUNT_ROLE (v5 mục 4.3): khóa chính (UserId, RoleId) của Identity, thêm người cấp và thời điểm cấp.</summary>
public class ApplicationUserRole : IdentityUserRole<string>
{
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null khi do quy trình hệ thống cấp.</summary>
    public string? AssignedByAccountId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public IdentityRole Role { get; set; } = null!;
    public ApplicationUser? AssignedByAccount { get; set; }
}
