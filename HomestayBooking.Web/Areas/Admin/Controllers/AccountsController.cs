using System.Security.Cryptography;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class AccountsController(ApplicationDbContext db, AccountAdminService accounts, IStringLocalizer<SharedResource> localizer)
    : AdminServiceController(db, localizer)
{
    private bool CanWrite => User.EmployeePosition() == EmployeePosition.Administrator;
    [HttpGet]
    public async Task<IActionResult> Index(string? term, EmployeePosition? position, AccountStatus? status, int? page)
    {
        if (!ModelState.IsValid || position is { } p && !Enum.IsDefined(p) || status is { } s && !Enum.IsDefined(s)) return BadRequest();
        var rows = await accounts.ListAsync(new(term, AppRoles.Employee, status, page, position), await CurrentActorAsync());
        return View(new AdminAccountList(rows, position));
    }
    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        try { var account = await accounts.DetailsAsync(id, await CurrentActorAsync());
            return account.EmployeeCode is null ? NotFound() : View(account); }
        catch (BookingException) { return NotFound(); }
    }
    [HttpGet]
    public IActionResult Create() => CanWrite ? View(RestoreForm("EmployeeForm", new EmployeeCreateForm())) : Forbid();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateForm form)
    {
        if (!CanWrite) return Forbid();
        var password = NewPassword();
        var saved = await SaveAsync(async () => await accounts.CreateEmployeeAsync(new(form.FullName, form.Phone, form.Email,
            form.EmployeeCode, form.Position, password, form.Department), await CurrentActorAsync()));
        if (!saved) { RememberForm("EmployeeForm", form); return RedirectToAction(nameof(Create)); }
        Temporary(password, form.EmployeeCode); return RedirectToAction(nameof(TemporaryPassword));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Employment(string id, EmployeePosition position, EmploymentStatus employment)
    {
        if (!CanWrite) return Forbid();
        await SaveAsync(async () => await accounts.ChangeEmploymentAsync(id, position, employment, await CurrentActorAsync()));
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id, bool locked, string reason)
    {
        if (!CanWrite) return Forbid();
        await SaveAsync(async () => await accounts.SetLockedAsync(id, locked, reason, await CurrentActorAsync()));
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (!CanWrite) return Forbid();
        var password = NewPassword();
        var saved = await SaveAsync(async () => await accounts.ResetEmployeePasswordAsync(id, password, await CurrentActorAsync()));
        if (!saved) return RedirectToAction(nameof(Details), new { id });
        Temporary(password, id); return RedirectToAction(nameof(TemporaryPassword));
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult TemporaryPassword()
    {
        if (!CanWrite) return Forbid();
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var password = TempData["EmployeeTemporaryPassword"] as string;
        var target = TempData["EmployeePasswordTarget"] as string;
        return password is null ? RedirectToAction(nameof(Index)) : View(new TemporaryEmployeePassword(target ?? "", password));
    }
    private static string NewPassword() => "Fj!" + RandomNumberGenerator.GetHexString(20) + "a9";
    private void Temporary(string password, string target)
    { TempData["EmployeeTemporaryPassword"] = password; TempData["EmployeePasswordTarget"] = target; }
}
