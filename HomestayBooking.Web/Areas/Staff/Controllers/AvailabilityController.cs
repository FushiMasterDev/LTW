using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class AvailabilityController(ApplicationDbContext db,AvailabilityService availability,TimeProvider clock,IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index(DateOnly? checkIn,DateOnly? checkOut,int? areaId,int guests=2,int? page=null)
    {
        var start=checkIn ?? VietnamTime.Today(clock);var end=checkOut ?? (start<DateOnly.MaxValue ? start.AddDays(1) : start);
        var areas=(await availability.SearchAsync(new())).Areas;
        var nights=new List<StaffNightAvailability>();
        try {
            if(!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            var actor=await CurrentActorAsync();
            // Validate the entire range before expanding; each count comes from the existing service.
            await availability.StaffAvailabilityAsync(start,end,areaId,guests,actor);
            for(var night=start;night<end;night=night.AddDays(1))
                nights.AddRange((await availability.StaffAvailabilityAsync(night,night.AddDays(1),areaId,guests,actor)).Select(o=>new StaffNightAvailability(night,o)));
        } catch(BookingException ex){Error(ex);}
        var sorted=nights.OrderBy(n=>n.Offer.HomestayName).ThenBy(n=>n.Offer.RoomTypeName).ThenBy(n=>n.Night).ToList();
        const int size=30;var current=Math.Clamp(page ?? 1,1,Math.Max(1,(int)Math.Ceiling(sorted.Count/(double)size)));
        return View(new StaffAvailabilityModel(start,end,areaId,guests,areas,
            new PaginatedList<StaffNightAvailability>(sorted.Skip((current-1)*size).Take(size),sorted.Count,current,size),clock.GetUtcNow().UtcDateTime));
    }
}
