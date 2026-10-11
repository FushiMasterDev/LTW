namespace HomestayBooking.Web.Models;

/// <summary>
/// Policy phân quyền. Role chỉ trả lời "tài khoản thuộc nhóm nào"; policy kiểm tra thêm điều kiện nghiệp vụ
/// (nhân viên còn làm việc, chức vụ, Owner đã được duyệt) — v5 mục 2, 4.7, 4.8.
/// </summary>
public static class AppPolicies
{
    /// <summary>Nhân viên đang làm việc (role Employee + claim chức vụ, chỉ có khi EmploymentStatus = Active).</summary>
    public const string Employee = "Employee";

    /// <summary>Quản trị danh mục, tài khoản: nhân viên chức vụ Quản lý hoặc Quản trị hệ thống.</summary>
    public const string Management = "Management";

    /// <summary>Owner có hồ sơ đăng ký đã được duyệt: được quản lý homestay, giá, nhận booking.</summary>
    public const string ApprovedOwner = "ApprovedOwner";
}
