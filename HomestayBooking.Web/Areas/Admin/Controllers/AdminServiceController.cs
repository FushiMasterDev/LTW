using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

/// <summary>Tra định danh nhân viên; dữ liệu và quyền nghiệp vụ do service kiểm tra.</summary>
public abstract class AdminServiceController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer) : AdminController
{
    protected IStringLocalizer<SharedResource> L => localizer;
    protected async Task<Actor> CurrentActorAsync() => new(ActorType.Employee,
        User.FindFirstValue(ClaimTypes.NameIdentifier), await db.Employees.Where(e => e.PersonId == User.PersonId())
            .Select(e => (int?)e.Id).SingleOrDefaultAsync());
    protected async Task<bool> SaveAsync(Func<Task> action)
    {
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            await action(); TempData["Success"] = L["Đã lưu thay đổi."].Value; return true;
        }
        catch (BookingException ex) { Error(ex); return false; }
    }
    protected void RememberForm<T>(string key, T form)
    {
        TempData[key] = JsonSerializer.Serialize(form);
        TempData[key + "Errors"] = JsonSerializer.Serialize(ModelState.Where(m => m.Value!.Errors.Count > 0)
            .ToDictionary(m => m.Key, m => m.Value!.Errors.Select(e => e.ErrorMessage).ToArray()));
    }
    protected T RestoreForm<T>(string key, T fallback)
    {
        if (TempData[key + "Errors"] is string errors)
            foreach (var entry in JsonSerializer.Deserialize<Dictionary<string, string[]>>(errors)!)
                foreach (var error in entry.Value) ModelState.AddModelError(entry.Key, error);
        return TempData[key] is string json ? JsonSerializer.Deserialize<T>(json)! : fallback;
    }
    protected void Error(BookingException ex) => TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value;
}
