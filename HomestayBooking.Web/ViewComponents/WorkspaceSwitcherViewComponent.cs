using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.ViewComponents;

/// <summary>Lối vào các khu làm việc nội bộ trên header trang khách: một khu thì một link, nhiều khu thì dropdown.</summary>
public class WorkspaceSwitcherViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View(Workspaces.For(HttpContext.User));
}
