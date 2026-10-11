namespace HomestayBooking.Web.Models;

/// <summary>
/// Vai trò của Account (ACCOUNT_ROLE, v5 mục 4.3). Một Account có thể có nhiều vai trò.
/// Quản lý/kế toán/quản trị không phải role riêng mà là chức vụ của Employee (xem <see cref="AppPolicies"/>).
/// </summary>
public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Owner = "Owner";
    public const string Employee = "Employee";

    public static readonly string[] All = [Customer, Owner, Employee];
}
