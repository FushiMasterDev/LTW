using System.Security.Claims;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>Số thông báo chưa đọc của tài khoản đang đăng nhập (FR-15), dùng cho chuông thông báo.</summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
[Produces("application/json")]
public class NotificationsApiController(NotificationService notifications) : ControllerBase
{
    public record UnreadCount(int Count);

    /// <summary>Đếm thông báo chưa đọc. Chưa đăng nhập thì cookie xác thực chuyển hướng hoặc trả 401.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCount>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCount>> Count()
    {
        var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return account is null ? Unauthorized() : Ok(new UnreadCount(await notifications.UnreadCountAsync(account)));
    }
}
