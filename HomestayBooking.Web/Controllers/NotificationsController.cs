using System.Security.Claims;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers;

[Authorize]
public class NotificationsController(NotificationService notifications) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? page)
    {
        var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account is null) return Challenge();
        return View(await notifications.ListAsync(account, page));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl)
    {
        var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account is null) return Challenge();
        if (!await notifications.MarkReadAsync(id, account))
        {
            TempData["Error"] = "Không tìm thấy thông báo của bạn.";
            return RedirectToAction(nameof(Index));
        }
        // Đường dẫn từ form chỉ dùng để điều hướng sau khi service xác nhận quyền sở hữu.
        if (returnUrl is not null && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account is null) return Challenge();
        await notifications.MarkAllReadAsync(account);
        TempData["Success"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
        return RedirectToAction(nameof(Index));
    }
}
