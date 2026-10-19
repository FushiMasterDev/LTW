using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.ViewComponents;

public record SidebarGroup(WorkspaceLink Workspace, IReadOnlyList<SidebarLink> Links);
public record SidebarModel(IReadOnlyList<SidebarGroup> Groups, string? Area, string? Controller);

/// <summary>
/// Menu dashboard theo quyền: một nhóm cho mỗi khu làm việc của tài khoản, chỉ trỏ tới action đã có.
/// Số đếm lấy qua service (không tự truy vấn); số của khu Kế toán do controller truyền vào qua AccountingCounts.
/// </summary>
public class SidebarViewComponent(IAuthorizationService authorization, BookingChangeRequestService changes, AccountingService accounting)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(AccountingCounts? counts = null)
    {
        var user = HttpContext.User;
        var canApproveChange = (await authorization.AuthorizeAsync(user, AppPolicies.ChangeRequestApprove)).Succeeded;
        var canConfirmChange = (await authorization.AuthorizeAsync(user, AppPolicies.ChangeRequestFinance)).Succeeded;
        var pendingChanges = canApproveChange ? await changes.PendingCountAsync() : 0;
        var additionalCount = canConfirmChange ? await accounting.PendingAdditionalDeposits().CountAsync() : 0;

        var groups = new List<SidebarGroup>();
        foreach (var w in Workspaces.For(user))
        {
            SidebarLink[] links = w.Area switch
            {
                "Admin" =>
                [
                    new("Admin", "Dashboard", "Tổng quan", "bi-speedometer2"),
                    new("Admin", "Areas", "Khu vực", "bi-geo-alt"),
                    new("Admin", "RoomCategories", "Loại phòng", "bi-grid-3x3-gap"),
                    new("Admin", "Amenities", "Tiện nghi", "bi-stars"),
                    new("Admin", "Holidays", "Ngày lễ", "bi-calendar-heart"),
                    new("Admin", "Vouchers", "Voucher", "bi-ticket-perforated"),
                ],
                "Accounting" =>
                [
                    new("Accounting", "Dashboard", "Tổng quan", "bi-speedometer2"),
                    new("Accounting", "AdditionalDeposits", "Cọc bổ sung", "bi-wallet2", additionalCount),
                    new("Accounting", "Receipts", "Bill chờ duyệt", "bi-receipt", counts?.PendingReceipts),
                    new("Accounting", "Transactions", "Giao dịch cần xử lý", "bi-bank", counts?.OpenTransactions),
                    new("Accounting", "Refunds", "Chờ hoàn tiền", "bi-arrow-counterclockwise", counts?.PendingRefunds),
                ],
                "Staff" =>
                [
                    new("Staff", "Dashboard", "Tổng quan", "bi-speedometer2"),
                    new("Staff", "ChangeRequests", "Gửi yêu cầu thay khách", "bi-arrow-repeat"),
                    new("Staff", "ChangeApprovals", "Duyệt yêu cầu đổi/hủy", "bi-calendar-check", pendingChanges),
                ],
                // Đơn đặt phòng cần policy ApprovedOwner
                "Owner" when user.IsApprovedOwner() =>
                [
                    new("Owner", "Dashboard", "Tổng quan", "bi-speedometer2"),
                    new("Owner", "Bookings", "Đơn đặt phòng", "bi-journal-check"),
                    new("Owner", "Homestays", "Homestay của tôi", "bi-house-gear"),
                    new("Owner", "Calendar", "Lịch phòng", "bi-calendar3"),
                    new("Owner", "Schedule", "Lịch nhận–trả", "bi-calendar-check"),
                    new("Owner", "Settlements", "Đối soát", "bi-receipt"),
                    new("Owner", "Reviews", "Đánh giá của khách", "bi-chat-square-text"),
                ],
                _ => [new(w.Area, "Dashboard", "Tổng quan", "bi-speedometer2")],
            };
            groups.Add(new(w, links.Where(l => (l.Controller != "ChangeApprovals" || canApproveChange)
                && (l.Controller != "AdditionalDeposits" || canConfirmChange)).ToList()));
        }
        var route = ViewContext.RouteData.Values;
        return View(new SidebarModel(groups, route["area"]?.ToString(), route["controller"]?.ToString()));
    }
}
