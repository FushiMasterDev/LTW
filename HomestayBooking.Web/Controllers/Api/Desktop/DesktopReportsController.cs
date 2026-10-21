using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

/// <summary>Báo cáo Management. From/To gồm hai đầu theo ngày Việt Nam, yyyy-MM-dd; giữ nguyên công thức ReportService.</summary>
[Route("api/desktop/reports")]
[Authorize(Policy = AppPolicies.Management)]
public sealed class DesktopReportsController(ApplicationDbContext db, ReportService reports) : DesktopController(db)
{
    /// <summary>Báo cáo quản lý tổng hợp theo ReportQuery, không thay đổi dữ liệu nguồn.</summary>
    /// <response code="200">Các phần đặt phòng, tài chính, nhân viên và homestay.</response>
    /// <response code="400">Khoảng báo cáo sai hoặc quá 366 ngày.</response>
    [HttpGet("management")]
    [ProducesResponseType<ManagementReportDto>(200)]
    public async Task<ActionResult<ManagementReportDto>> Management([FromQuery] ReportQuery query)
    {
        var r = await reports.ManagementAsync(query, await CurrentActorAsync());
        return Ok(new ManagementReportDto(DesktopReportMapping.Bookings(r.Bookings), DesktopReportMapping.Finance(r.Finance),
            r.Employees.Select(DesktopReportMapping.Employee).ToList(), r.Homestays.Select(DesktopReportMapping.Homestay).ToList()));
    }

    /// <summary>Báo cáo bookings theo ReportQuery; chỉ Management.</summary>
    /// <response code="200">Báo cáo theo service hiện có.</response>
    /// <response code="400">Khoảng ngày không hợp lệ.</response>
    [HttpGet("bookings")]
    [ProducesResponseType<BookingReportDto>(200)]
    public async Task<ActionResult<BookingReportDto>> Bookings([FromQuery] ReportQuery query) =>
        Ok(DesktopReportMapping.Bookings(await reports.BookingsAsync(query, await CurrentActorAsync())));

    /// <summary>Báo cáo financial theo ReportQuery; chỉ Management.</summary>
    /// <response code="200">Báo cáo theo service hiện có.</response>
    /// <response code="400">Khoảng ngày không hợp lệ.</response>
    [HttpGet("financial")]
    [ProducesResponseType<FinancialReportDto>(200)]
    public async Task<ActionResult<FinancialReportDto>> Financial([FromQuery] ReportQuery query) =>
        Ok(DesktopReportMapping.Finance(await reports.FinancialAsync(query, await CurrentActorAsync())));

    /// <summary>Báo cáo employees theo ReportQuery; chỉ Management, phân trang.</summary>
    /// <response code="200">Trang báo cáo.</response>
    /// <response code="400">Khoảng ngày hoặc page không hợp lệ.</response>
    [HttpGet("employees")]
    [ProducesResponseType<DesktopPage<EmployeePerformanceDto>>(200)]
    public async Task<ActionResult<DesktopPage<EmployeePerformanceDto>>> Employees([FromQuery] ReportQuery query, [FromQuery] int? page) =>
        Ok(DesktopPage<EmployeePerformanceDto>.Slice((await reports.EmployeesAsync(query, await CurrentActorAsync())).Select(DesktopReportMapping.Employee), page));

    /// <summary>Báo cáo homestays theo ReportQuery; chỉ Management, phân trang.</summary>
    /// <response code="200">Trang báo cáo.</response>
    /// <response code="400">Khoảng ngày hoặc page không hợp lệ.</response>
    [HttpGet("homestays")]
    [ProducesResponseType<DesktopPage<HomestayReportDto>>(200)]
    public async Task<ActionResult<DesktopPage<HomestayReportDto>>> Homestays([FromQuery] ReportQuery query, [FromQuery] int? page) =>
        Ok(DesktopPage<HomestayReportDto>.Slice((await reports.HomestaysAsync(query, await CurrentActorAsync())).Select(DesktopReportMapping.Homestay), page));
}
