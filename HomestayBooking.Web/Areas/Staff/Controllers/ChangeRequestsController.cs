using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Policy = AppPolicies.Employee)]
public class ChangeRequestsController(ApplicationDbContext db, ChangeRequestUiService ui, IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index(string? q, int? page)
    {
        ViewData["Query"] = q;
        var query = db.Bookings.AsNoTracking().Include(b => b.Homestay).Include(b => b.RoomType)
            .Where(b => b.Status == BookingStatus.Deposited || b.Status == BookingStatus.Confirmed);
        if (string.IsNullOrWhiteSpace(q)) query = query.Where(b => false);
        else
        {
            var term = q.Trim(); var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(b => b.Code.Contains(term) || (phone != null && (b.ContactPhone == phone || b.Customer.Person.PhoneNumber == phone)));
        }
        return View(await PaginatedList<Booking>.CreateAsync(query.OrderByDescending(b => b.CreatedAt), page));
    }

    private Task<Booking?> BookingAsync(int id) => db.Bookings.AsNoTracking().Include(b => b.Homestay).Include(b => b.RoomType)
        .SingleOrDefaultAsync(b => b.Id == id);

    public async Task<IActionResult> Create(int id)
    {
        var b = await BookingAsync(id); if (b is null) return NotFound();
        var form = new ChangeRequestForm { NewCheckIn = b.CheckIn, NewCheckOut = b.CheckOut };
        await ui.PrepareAsync(form, b, true);
        return View("~/Views/Bookings/Change.cshtml", form);
    }

    [HttpPost]
    public Task<IActionResult> Quote(int id, ChangeRequestForm form) => HandleAsync(id, form, false);

    [HttpPost]
    public Task<IActionResult> Submit(int id, ChangeRequestForm form) => HandleAsync(id, form, true);

    private async Task<IActionResult> HandleAsync(int id, ChangeRequestForm form, bool submit)
    {
        var b = await BookingAsync(id); if (b is null) return NotFound();
        await ui.PrepareAsync(form, b, true);
        if (ModelState.IsValid)
        {
            try
            {
                if (submit)
                {
                    await ui.SubmitAsync(form, User);
                    TempData["Success"] = localizer["Đã gửi yêu cầu thay khách, vui lòng chờ quản lý duyệt."].Value;
                    return RedirectToAction(nameof(Index), new { q = b.Code });
                }
                await ui.QuoteAsync(form, User); ModelState.Remove(nameof(form.PreviewToken));
            }
            catch (BookingException ex) { ModelState.AddModelError(string.Empty, localizer[ex.ResourceKey, ex.Arguments]); }
        }
        return View("~/Views/Bookings/Change.cshtml", form);
    }
}
