using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
public class DashboardController(ApplicationDbContext db,StaffBookingService bookings,BookingChangeRequestService changes,
    OwnerRegistrationQueryService registrations,ReportService reports,TimeProvider clock,IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index(DateOnly? from,DateOnly? to)
    {
        var actor=await CurrentActorAsync();var today=VietnamTime.Today(clock);
        var start=from ?? new DateOnly(today.Year,today.Month,1);var end=to ?? today;
        EmployeePerformance? performance=null;
        try {performance=(await reports.EmployeesAsync(new(start,end),actor,actor.EmployeeId)).SingleOrDefault();}
        catch(BookingException ex){Error(ex);}
        var pending=await bookings.SearchAsync(new() {Status=BookingStatus.Deposited},actor);
        var overdue=new List<Booking>();var page=pending;
        while(true){
            overdue.AddRange(page.Where(b=>b.ConfirmDeadlineAt<clock.GetUtcNow().UtcDateTime));
            if(!page.HasNextPage) break;
            page=await bookings.SearchAsync(new() {Status=BookingStatus.Deposited,Page=page.PageIndex+1},actor);
        }
        var waiting=(await registrations.ListAsync(new(Status:OwnerRegistrationStatus.PendingReview),actor)).TotalCount;
        var reviewing=(await registrations.ListAsync(new(Status:OwnerRegistrationStatus.UnderReview),actor)).TotalCount;
        return View(new StaffDashboardModel(pending.TotalCount,await changes.PendingCountAsync(),waiting+reviewing,overdue,performance,start,end));
    }
}
