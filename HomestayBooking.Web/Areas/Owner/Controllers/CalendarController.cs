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
public class CalendarController(HomestayContentService content, RoomBlockService blocks, TimeProvider clock,
    IStringLocalizer<SharedResource> L) : Controller
{
    private Actor Actor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));
    public async Task<IActionResult> Index(int? homestayId, int? roomId, string? month)
    {
        var homes = await content.OwnerHomestaysAsync(Actor);
        homestayId ??= homes.FirstOrDefault()?.Id;
        if (homestayId is int id && !homes.Any(h => h.Id == id)) return NotFound();
        var today = VietnamTime.Today(clock);
        var date = RoomCalendarModel.ParseMonth(month, today);
        // Guard navigation at DateOnly boundaries before building padded weeks.
        if (date.Year is < 1900 or > 2100) return BadRequest();
        var shape = new RoomCalendarModel(date, []);
        var nights = homestayId is int home ? await blocks.CalendarAsync(home, shape.Days.First(), shape.Days.Last().AddDays(1), Actor) : [];
        var rooms = nights.Select(n => new CalendarRoom(n.RoomId,n.RoomCode)).Distinct().ToList();
        if (roomId is int selected && !rooms.Any(r => r.Id == selected)) return NotFound();
        roomId ??= rooms.FirstOrDefault()?.Id;
        return View(new OwnerCalendarModel(homes, homestayId, roomId,
            new RoomCalendarModel(date, nights.Where(n => n.RoomId == roomId).ToList()) { RangeSelection=true, MinSelectableDate=today }, nights, today));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int homestayId, int roomId, DateOnly start, DateOnly end, RoomBlockReason reason = RoomBlockReason.OtherChannel, string? note = null)
    {
        var homes = await content.OwnerHomestaysAsync(Actor);
        if (!homes.Any(h=>h.Id==homestayId)) return NotFound();
        var editor = await content.EditorAsync(homestayId,Actor);
        if (!editor.RoomTypes.Any(t=>t.Rooms.Any(r=>r.Id==roomId))) return NotFound();
        try {
            if (!ModelState.IsValid) throw new BookingException("Khoảng khóa, lý do hoặc ghi chú không hợp lệ; không khóa ngày quá khứ.");
            await blocks.CreateAsync(roomId,start,end,reason,note,Actor);
            TempData["Success"] = L["Đã khóa ngày phòng."].Value;
        } catch (RoomBlockConflictException ex) {
            TempData["Error"] = L["Lịch phòng bị trùng: {0}. Vui lòng chọn khoảng ngày khác.", string.Join("; ",ex.Conflicts.Select(c=>$"{c.Date:dd/MM/yyyy}: {c.Code}"))].Value;
        } catch (BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index), new { homestayId,roomId, month=start.ToString("yyyy-MM") });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int homestayId, int roomId, int blockId, string? month)
    {
        if (!(await content.OwnerHomestaysAsync(Actor)).Any(h=>h.Id==homestayId)) return NotFound();
        // UnlockAsync authorizes the block's actual room; posted home/room only control the return URL.
        try { await blocks.UnlockAsync(blockId,Actor); TempData["Success"] = L["Đã mở khóa ngày phòng."].Value; }
        catch (BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index),new {homestayId,roomId,month});
    }
}
