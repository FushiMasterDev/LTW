using System.Security.Claims;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.ViewComponents;

public class NotificationBellViewComponent(NotificationService notifications) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = ViewContext.HttpContext.User;
        var account = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || account is null) return Content("");
        var count = await notifications.UnreadCountAsync(account);
        var latest = await notifications.LatestAsync(account);
        return View(new NotificationBellModel(count, latest));
    }
}
