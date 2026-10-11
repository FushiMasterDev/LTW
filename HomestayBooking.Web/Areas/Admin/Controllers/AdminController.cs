using HomestayBooking.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

/// <summary>Lớp cơ sở cho mọi controller khu vực Quản trị: nhân viên chức vụ Quản lý / Quản trị hệ thống.</summary>
[Area("Admin")]
[Authorize(Policy = AppPolicies.Management)]
public abstract class AdminController : Controller
{
}
