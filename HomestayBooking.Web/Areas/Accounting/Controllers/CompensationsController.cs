using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

public class CompensationsController(ApplicationDbContext db, CompensationService compensations,
    AccountingService accounting, IStringLocalizer<SharedResource> L) : AccountingController(db)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await compensations.PendingAsync(await CurrentActorAsync());
        await LoadCountsAsync(accounting);
        return View(rows);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RecordPaid(int id, string bankRef) => Save(actor => compensations.RecordPaidAsync(id, bankRef, actor));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id, string reason) => Save(actor => compensations.RejectAsync(id, reason, actor));
    private async Task<IActionResult> Save(Func<Actor, Task<Payment>> action)
    {
        if (User.EmployeePosition() != EmployeePosition.Accountant) return Forbid();
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            await action(await CurrentActorAsync()); TempData["Success"] = L["Đã lưu thay đổi."].Value;
        }
        catch (BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Index));
    }
}
