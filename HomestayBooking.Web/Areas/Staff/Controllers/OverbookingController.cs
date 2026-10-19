using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
public class OverbookingController(ApplicationDbContext db,StaffBookingService bookings,OverbookingService incidents,
    AvailabilityService availability,IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index(int id,int? page=null)
    {
        var actor=await CurrentActorAsync();
        try {
            var b=(await bookings.DetailsAsync(id,actor)).Booking;
            var alternatives=await incidents.AlternativesAsync(id,actor);
            var choices=alternatives.Where(a=>a.Offer.RoomTypeId!=b.RoomTypeId).ToList();
            const int size=6;var current=Math.Clamp(page ?? 1,1,Math.Max(1,(int)Math.Ceiling(choices.Count/(double)size)));
            return View(new StaffOverbookingModel(b,new PaginatedList<OverbookingAlternative>(choices.Skip((current-1)*size).Take(size),choices.Count,current,size),
                await availability.FreeRoomsAsync(b.RoomTypeId,b.CheckIn,b.CheckOut,actor)));
        } catch(BookingException ex){Error(ex);return RedirectToAction("Details","Bookings",new {id});}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id,string note)
    {
        var saved=await SaveAsync(async()=>await incidents.ReportAsync(id,note,await CurrentActorAsync()));
        return saved ? RedirectToAction(nameof(Index),new {id}) : RedirectToAction("Details","Bookings",new {id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id,int roomId,string note)
    {
        var saved=await SaveAsync(async()=>await incidents.MoveRoomAsync(id,roomId,note,await CurrentActorAsync()));
        return saved ? RedirectToAction("Details","Bookings",new {id}) : RedirectToAction(nameof(Index),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(int id,int roomTypeId,string note)
    {
        int? replacement=null;
        await SaveAsync(async()=>replacement=(await incidents.ReplaceAsync(id,roomTypeId,note,await CurrentActorAsync())).Id);
        return replacement is int next ? RedirectToAction("Details","Bookings",new {id=next}) : RedirectToAction(nameof(Index),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy=AppPolicies.Management)]
    public async Task<IActionResult> Refund(int id,string note)
    {
        var saved=await SaveAsync(async()=>await incidents.RefundAsync(id,note,await CurrentActorAsync()));
        return saved ? RedirectToAction("Details","Bookings",new {id}) : RedirectToAction(nameof(Index),new {id});
    }
}
