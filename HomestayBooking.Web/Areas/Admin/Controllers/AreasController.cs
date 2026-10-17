using HomestayBooking.Web.Services;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class AreasController : AdminController
{
    private readonly ApplicationDbContext _db;
    private readonly CatalogDeletionService _deletion;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AreasController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer, CatalogDeletionService deletion)
    {
        _db = db;
        _deletion = deletion;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? q, int? page)
    {
        var query = _db.Areas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a => a.Name.Contains(q.Trim()) || a.Province.Contains(q.Trim()));
        }

        ViewData["Query"] = q;
        ViewBag.HomestayCounts = await _db.Homestays.GroupBy(h => h.AreaId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await PaginatedList<Area>.CreateAsync(query.OrderBy(a => a.Province).ThenBy(a => a.Name), page));
    }

    public IActionResult Create() => View("Form", new AreaFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(AreaFormViewModel model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        _db.Areas.Add(new Area { Name = model.Name.Trim(), Province = model.Province.Trim(), AreaGroup = model.AreaGroup });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Request khác vừa ghi cùng giá trị sau bước kiểm tra trùng ở trên
            ModelState.AddModelError(nameof(model.Name), _localizer["Khu vực này đã có trong tỉnh/thành."]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã thêm \"{0}\".", model.Name.Trim()].Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var area = await _db.Areas.FindAsync(id);
        if (area is null)
        {
            return NotFound();
        }

        return View("Form", new AreaFormViewModel { Id = area.Id, Name = area.Name, Province = area.Province, AreaGroup = area.AreaGroup });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AreaFormViewModel model)
    {
        var area = await _db.Areas.FindAsync(id);
        if (area is null)
        {
            return NotFound();
        }

        model.Id = id;
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        area.Name = model.Name.Trim();
        area.Province = model.Province.Trim();
        area.AreaGroup = model.AreaGroup;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Request khác vừa ghi cùng giá trị sau bước kiểm tra trùng ở trên
            ModelState.AddModelError(nameof(model.Name), _localizer["Khu vực này đã có trong tỉnh/thành."]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã cập nhật \"{0}\".", area.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var area = await _db.Areas.FindAsync(id);
        if (area is null)
        {
            return NotFound();
        }

        try { await _deletion.DeleteAsync(CatalogKind.Area, id, User); }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.ResourceKey, ex.Arguments].Value;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = _localizer["Đã xóa \"{0}\".", area.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(AreaFormViewModel model)
    {
        // Unique theo (tỉnh/thành, tên): "Trung tâm" có thể có ở nhiều tỉnh
        var name = model.Name?.Trim();
        var province = model.Province?.Trim();
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(province)
            && await _db.Areas.AnyAsync(a => a.Name == name && a.Province == province && a.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), _localizer["Khu vực này đã có trong tỉnh/thành."]);
        }
    }
}
