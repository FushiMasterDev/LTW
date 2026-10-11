namespace HomestayBooking.Web.Identity;

/// <summary>Claim riêng của ứng dụng, được làm mới mỗi lần SecurityStampValidator kiểm tra lại cookie.</summary>
public static class AppClaimTypes
{
    public const string FullName = "full_name";
    public const string PersonId = "person_id";

    /// <summary>Chức vụ nhân viên; chỉ có khi nhân viên đang làm việc.</summary>
    public const string EmployeePosition = "employee_position";

    /// <summary>"true" khi Owner có hồ sơ đăng ký đã được duyệt.</summary>
    public const string OwnerApproved = "owner_approved";
}
