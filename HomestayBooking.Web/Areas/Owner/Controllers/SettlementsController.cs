using System.Security.Claims;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Owner.Controllers;
using HomestayBooking.Web.Models;

[Area(AppRoles.Owner), Authorize(Policy = AppPolicies.ApprovedOwner)]
public class SettlementsController(SettlementService settlements, IStringLocalizer<SharedResource> L) : Controller
{
    private Actor Actor => new(ActorType.Owner,User.FindFirstValue(ClaimTypes.NameIdentifier));
    public async Task<IActionResult> Index(SettlementStatus? status, int? page) => View(await settlements.ListAsync(new(Status:status,Page:page),Actor));
    [HttpGet("/Owner/Settlements/{id:int}")]
    [HttpGet("/Owner/Settlements/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try { return View(await settlements.DetailsAsync(id,Actor)); }
        catch (BookingException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Confirm(int id) => Save(id, () => settlements.ConfirmAsync(id,Actor));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Discrepancy(int id, string note) => Save(id, () => settlements.ReportDiscrepancyAsync(id,note,Actor));
    private async Task<IActionResult> Save(int id, Func<Task<Settlement>> action)
    {
        try { await settlements.DetailsAsync(id,Actor); } catch(BookingException) { return NotFound(); }
        try {
            if (!ModelState.IsValid) throw new BookingException("Thông tin không hợp lệ. Vui lòng kiểm tra các trường và thử lại.");
            await action(); TempData["Success"] = L["Đã lưu thay đổi."].Value;
        } catch(BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Details),new {id});
    }
}
