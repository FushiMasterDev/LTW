using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;

[Area("Staff"), Authorize(Policy = AppPolicies.Employee)]
public abstract class StaffController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer) : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // The existing employee login lands all non-management employees on Staff/Dashboard.
        if (User.EmployeePosition() == EmployeePosition.Accountant && context.ActionDescriptor.RouteValues["controller"] == "Dashboard")
        { context.Result = RedirectToAction("Index", "Dashboard", new { area = "Accounting" }); return; }
        if (User.EmployeePosition() is not (EmployeePosition.Consultant or EmployeePosition.Manager or EmployeePosition.Administrator))
        { context.Result = Forbid(); return; }
        await next();
    }
    protected IStringLocalizer<SharedResource> L => localizer;
    // Identity lookup only; every operation is authorized again by its service.
    protected async Task<Actor> CurrentActorAsync() => new(ActorType.Employee,
        User.FindFirstValue(ClaimTypes.NameIdentifier), await db.Employees.Where(e => e.PersonId == User.PersonId())
            .Select(e => (int?)e.Id).SingleOrDefaultAsync());
    protected async Task<bool> SaveAsync(Func<Task> action)
    {
        try {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            await action(); TempData["Success"] = L["Đã lưu thay đổi."].Value; return true;
        } catch (BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; return false; }
    }
    protected void Error(BookingException ex) => TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value;
}
