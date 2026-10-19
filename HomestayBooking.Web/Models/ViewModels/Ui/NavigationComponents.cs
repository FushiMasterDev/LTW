using System.Security.Claims;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Models.ViewModels.Ui;

/// <param name="Label">Nhãn tab.</param>
/// <param name="Href">Đường dẫn trang trong ứng dụng.</param>
/// <param name="Active">Trang hiện tại.</param>
/// <param name="Count">Số việc đang chờ.</param>
/// <param name="Visible">Điều kiện hiển thị đã được tính theo quyền hiện có.</param>
/// <param name="Icon">Bootstrap Icon tùy chọn.</param>
public record TabItem(string Label, string Href, bool Active = false, int? Count = null, bool Visible = true, string? Icon = null);

/// <param name="Label">Tên vùng điều hướng.</param>
/// <param name="Items">Các tab, không tự tải dữ liệu.</param>
public record TabsModel(string Label, IReadOnlyList<TabItem> Items)
{
    public static TabsModel Accounting(ClaimsPrincipal user, AccountingCounts? counts, string? current)
    {
        TabItem Tab(string controller, string label, int? count, string icon, bool visible = true) =>
            new(label, "/Accounting/" + controller, Active: string.Equals(current, controller, StringComparison.OrdinalIgnoreCase), Count: count, Visible: visible, Icon: icon);
        return new("Kế toán", [
        Tab("Dashboard", "Tổng quan", null, "speedometer2"),
        Tab("AdditionalDeposits", "Cọc bổ sung", counts?.PendingAdditionalDeposits, "wallet2",
            user.IsInRole(AppRoles.Employee) && user.EmployeePosition() == EmployeePosition.Accountant),
        Tab("Receipts", "Bill chờ duyệt", counts?.PendingReceipts, "receipt"),
        Tab("Transactions", "Giao dịch cần xử lý", counts?.OpenTransactions, "bank"),
        Tab("Settlements", "Đối soát chủ homestay", null, "journal-check"),
        Tab("Compensations", "Khoản bù trùng phòng", null, "cash-stack"),
        Tab("Refunds", "Chờ hoàn tiền", counts?.PendingRefunds, "arrow-counterclockwise"),
        ]);
    }
}

/// <param name="Label">Nhãn của cấp điều hướng.</param>
/// <param name="Href">Null cho trang hiện tại.</param>
/// <param name="HideOnMobile">Ẩn cấp cha trên điện thoại, vẫn giữ tên trang cuối.</param>
public record BreadcrumbItem(string Label, string? Href = null, bool HideOnMobile = false);

/// <param name="Items">Các cấp từ gốc tới trang hiện tại.</param>
public record BreadcrumbModel(IReadOnlyList<BreadcrumbItem> Items);

/// <param name="UnreadCount">Tổng số thông báo chưa đọc của tài khoản.</param>
/// <param name="Items">Năm thông báo mới nhất, hoặc mẫu để preview trên UI Kit.</param>
/// <param name="Id">Id duy nhất khi có nhiều bản preview trên một trang.</param>
public record NotificationBellModel(int UnreadCount, IReadOnlyList<Notification> Items, string Id = "fx-notifications");

/// <param name="Label">Khóa resource đặt tên cho tiến trình (aria-label).</param>
/// <param name="Steps">Khóa resource của từng bước theo thứ tự.</param>
/// <param name="Current">Chỉ số bước hiện tại (0 là bước đầu); các bước trước đã xong.</param>
public record StepsModel(string Label, IReadOnlyList<string> Steps, int Current);

/// <param name="Title">Khóa resource tiêu đề cột ảnh.</param>
/// <param name="Points">Khóa resource của ba ý ngắn.</param>
public record AuthAsideModel(string Title, IReadOnlyList<string> Points);
