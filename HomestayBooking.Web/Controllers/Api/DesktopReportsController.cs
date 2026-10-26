using System.Security.Claims;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api;

[ApiController]
[Authorize(Policy = AppPolicies.DesktopManagement)]
[Route("api/desktop/reports")]
public sealed class DesktopReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("management")]
    public async Task<IActionResult> Management([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int? areaId)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366)
            return Problem(detail: "Khoảng thời gian báo cáo không hợp lệ (tối đa 366 ngày).", statusCode: 400);
        var employeeId = int.TryParse(User.FindFirstValue("employee_id"), out var id) ? id : 0;
        var result = await reports.ManagementAsync(new ReportQuery(from, to, AreaId: areaId),
            new Actor(ActorType.Employee, User.FindFirstValue(ClaimTypes.NameIdentifier), employeeId));
        return Ok(result);
    }
}
