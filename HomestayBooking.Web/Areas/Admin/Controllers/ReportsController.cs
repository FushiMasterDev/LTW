using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class ReportsController(ApplicationDbContext db, ReportService reports, TimeProvider clock,
    IStringLocalizer<SharedResource> localizer) : AdminServiceController(db, localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string tab = "overview")
    {
        var today = VietnamTime.Today(clock);
        var query = new ReportQuery(from ?? new DateOnly(today.Year, today.Month, 1), to ?? today);
        if (tab is not ("overview" or "bookings" or "finance" or "employees" or "homestays")) tab = "overview";
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            var actor = await CurrentActorAsync();
            switch (tab)
            {
                case "bookings": return View(new AdminReportModel(query, tab, Bookings: await reports.BookingsAsync(query, actor), Months: await MonthsAsync(query, actor, true, false)));
                case "finance": return View(new AdminReportModel(query, tab, Finance: await reports.FinancialAsync(query, actor), Months: await MonthsAsync(query, actor, false, true)));
                case "employees": return View(new AdminReportModel(query, tab, Employees: await reports.EmployeesAsync(query, actor)));
                case "homestays": return View(new AdminReportModel(query, tab, Homestays: await reports.HomestaysAsync(query, actor)));
                default:
                    var all = await reports.ManagementAsync(query, actor);
                    return View(new AdminReportModel(query, tab, all.Bookings, all.Finance, all.Employees, all.Homestays, await MonthsAsync(query, actor, true, true)));
            }
        }
        catch (BookingException ex) { Error(ex); return View(new AdminReportModel(query, tab)); }
    }
    private async Task<IReadOnlyList<AdminReportMonth>> MonthsAsync(ReportQuery query, Actor actor, bool bookings, bool finance)
    {
        var result = new List<AdminReportMonth>();
        for (var start = query.From; start <= query.To;)
        {
            var last = new DateOnly(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));
            var end = last < query.To ? last : query.To;
            var slice = query with { From = start, To = end };
            var count = bookings ? (await reports.BookingsAsync(slice, actor)).Total : (int?)null;
            var amount = finance ? (await reports.FinancialAsync(slice, actor)).FujiNet : (decimal?)null;
            result.Add(new(start, end, count, amount));
            if (end == query.To) break;
            start = end.AddDays(1);
        }
        return result;
    }
}
