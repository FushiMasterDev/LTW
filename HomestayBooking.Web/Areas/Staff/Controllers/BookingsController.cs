using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
public class BookingsController(ApplicationDbContext db, StaffBookingService bookings, CustomerService customers,
    AvailabilityService availability, PricingService pricing, TimeProvider clock, IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index(StaffBookingQuery query)
    {
        if (!ModelState.IsValid) { TempData["Error"] = L["Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường."].Value; return RedirectToAction(nameof(Index)); }
        var actor = await CurrentActorAsync();
        return View(new StaffBookingsModel(query, await bookings.SearchAsync(query,actor), await bookings.AssigneesAsync(actor)) { Homestays = await bookings.HomestaysAsync(actor) });
    }
    [HttpGet("/Staff/Bookings/{id:int}")]
    [HttpGet("/Staff/Bookings/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var actor = await CurrentActorAsync();
        try {
            var details = await bookings.DetailsAsync(id,actor); var b = details.Booking;
            var rooms = b.Status == BookingStatus.Deposited ? await availability.FreeRoomsAsync(b.RoomTypeId,b.CheckIn,b.CheckOut,actor) : [];
            return View(new StaffBookingModel(details,await bookings.AssigneesAsync(actor), rooms,actor.EmployeeId,VietnamTime.Today(clock)));
        } catch (BookingException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int employeeId)
    {
        await SaveAsync(async () => await bookings.AssignAsync(id,employeeId,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, BookingStatus to, int? newRoomId, string note)
    {
        await SaveAsync(async () => await bookings.ChangeStatusAsync(id,to,newRoomId,note,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
    [HttpGet]
    public async Task<IActionResult> Create([FromQuery] StaffCreateForm form)
    {
        var today = VietnamTime.Today(clock);
        if (form.CheckIn == default) form.CheckIn=today.AddDays(1);
        if (form.CheckOut == default) form.CheckOut=form.CheckIn<DateOnly.MaxValue ? form.CheckIn.AddDays(1) : form.CheckIn;
        // GET is a lookup/quote; required customer fields are validated only when creating.
        ModelState.Clear();
        var actor=await CurrentActorAsync();
        form.Areas=(await availability.SearchAsync(new())).Areas;
        if (!string.IsNullOrWhiteSpace(form.Phone)) {
            form.Customers=await customers.SearchAsync(form.Phone,1,actor);
            if (form.Customers.Count == 1) {
                form.FullName=string.IsNullOrWhiteSpace(form.FullName) ? form.Customers[0].FullName ?? "" : form.FullName;
                form.Email ??= form.Customers[0].Email;
            }
        }
        try {
            form.Offers=await availability.StaffAvailabilityAsync(form.CheckIn,form.CheckOut,form.AreaId,form.Guests,actor);
            if (form.RoomTypeId is int type && form.Offers.Any(o=>o.RoomTypeId==type && o.FreeRooms>0))
                form.Quote=await pricing.QuoteAsync(type,form.CheckIn,form.CheckOut);
        } catch (BookingException ex) { Error(ex); }
        return View(form);
    }
    [HttpPost, ActionName("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConfirmed(StaffCreateForm form)
    {
        Booking? booking=null;
        await SaveAsync(async () => booking=await bookings.CreateForCustomerAsync(new(form.Phone,form.FullName,form.Email,
            form.RoomTypeId ?? 0,form.CheckIn,form.CheckOut,form.Guests,form.SpecialRequests,form.Channel),await CurrentActorAsync()));
        return booking is not null ? RedirectToAction(nameof(Details),new {id=booking.Id}) : RedirectToAction(nameof(Create),
            new {form.Phone,form.FullName,form.Email,form.Channel,form.CheckIn,form.CheckOut,form.Guests,form.AreaId,form.RoomTypeId,form.SpecialRequests});
    }
}
