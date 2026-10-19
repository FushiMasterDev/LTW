using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class ContractsController(ApplicationDbContext db, ContractService contracts, TimeProvider clock,
    IStringLocalizer<SharedResource> localizer) : AdminServiceController(db, localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index(ContractStatus? status, string? search, int? expiringWithinDays, int? page)
    {
        if (!ModelState.IsValid || status is { } s && !Enum.IsDefined(s) || expiringWithinDays is < 0 or > 366)
            return BadRequest();
        return View(new AdminContractList(await contracts.ListAsync(new(status, search, expiringWithinDays, page), await CurrentActorAsync()), VietnamTime.Today(clock)));
    }
    [HttpGet("/Admin/Contracts/{id:int}")]
    [HttpGet("/Admin/Contracts/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try { return View(new AdminContractDetails(await contracts.DetailsAsync(id, await CurrentActorAsync()), VietnamTime.Today(clock))); }
        catch (BookingException) { return NotFound(); }
    }
    [HttpGet]
    public IActionResult Create(int? homestayId) => View(RestoreForm("ContractForm", new ContractCreateForm { HomestayId = homestayId ?? 0,
        StartDate = VietnamTime.Today(clock), EndDate = VietnamTime.Today(clock).AddYears(1) }));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractCreateForm form)
    {
        int? id = null;
        await SaveAsync(async () => id = (await contracts.CreateAsync(new(form.HomestayId, form.ContractNo,
            form.StartDate, form.EndDate, form.CommissionPercent / 100m), await CurrentActorAsync())).Id);
        if (id is null) RememberForm("ContractForm", form);
        return id is { } created ? RedirectToAction(nameof(Details), new { id = created }) : RedirectToAction(nameof(Create), new { homestayId = form.HomestayId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Extend(int id, DateOnly endDate)
    {
        await SaveAsync(async () => await contracts.ExtendAsync(id, endDate, await CurrentActorAsync()));
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int id, string reason)
    {
        await SaveAsync(async () => await contracts.TerminateAsync(id, reason, await CurrentActorAsync()));
        return RedirectToAction(nameof(Details), new { id });
    }
}
