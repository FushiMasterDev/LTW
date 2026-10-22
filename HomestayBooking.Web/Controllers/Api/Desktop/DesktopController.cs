using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

/// <summary>Bearer only. Ngày yyyy-MM-dd, mọi timestamp UTC (hậu tố Z). Không nhận cookie hoặc yêu cầu antiforgery.</summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
[ServiceFilter(typeof(DesktopExceptionFilter))]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class DesktopApiController : ControllerBase;

/// <summary>Giữ nguyên policy nhân viên cho toàn bộ endpoint đợt 1.</summary>
[Authorize(Policy = AppPolicies.Employee)]
public abstract class DesktopController(ApplicationDbContext db) : DesktopApiController
{
    protected ApplicationDbContext Db { get; } = db;
    protected async Task<Actor> CurrentActorAsync() => new(ActorType.Employee,
        User.FindFirstValue(ClaimTypes.NameIdentifier), await Db.Employees.Where(e => e.PersonId == User.PersonId())
            .Select(e => (int?)e.Id).SingleOrDefaultAsync());
    protected Task<int> CurrentEmployeeIdAsync() => Db.Employees.Where(e => e.PersonId == User.PersonId()).Select(e => e.Id).SingleAsync();
}
