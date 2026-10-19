using System.Security.Claims;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Owner.Controllers;

[Area(AppRoles.Owner), Authorize(Policy = AppPolicies.ApprovedOwner)]
public class ReviewsController(HomestayContentService content, ReviewService reviews, IStringLocalizer<SharedResource> L) : Controller
{
    private Actor Actor => new(ActorType.Owner,User.FindFirstValue(ClaimTypes.NameIdentifier));
    public async Task<IActionResult> Index(int? homestayId, int? page)
    {
        var homes = await content.OwnerHomestaysAsync(Actor);
        homestayId ??= homes.FirstOrDefault()?.Id;
        if (homestayId is int id && !homes.Any(h=>h.Id==id)) return NotFound();
        return View(new OwnerReviewsModel(homes,homestayId,homestayId is int h ? await reviews.ListAsync(h,page,actor:Actor) : null));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, int homestayId, string reply, int? page)
    {
        if (!(await content.OwnerHomestaysAsync(Actor)).Any(h=>h.Id==homestayId)) return NotFound();
        try {
            if (!ModelState.IsValid) throw new BookingException("Nội dung phải có từ 1 đến 1000 ký tự.");
            await reviews.ReplyAsync(id,reply,Actor); TempData["Success"] = L["Đã gửi trả lời đánh giá."].Value;
        } catch(BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index),new {homestayId,page});
    }
}
