using HomestayBooking.Web.Services;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class AmenitiesController : AdminController
{
    private readonly ApplicationDbContext _db;
    private readonly CatalogEditorService _editor;
    private readonly CatalogDeletionService _deletion;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AmenitiesController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer, CatalogDeletionService deletion, CatalogEditorService editor)
    {
        _db = db;
        _editor = editor;
        _deletion = deletion;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? q, int? page)
    {
        var query = _db.Amenities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a => a.Name.Contains(q.Trim()));
        }

        ViewData["Query"] = q;
        ViewBag.HomestayCounts = await _db.HomestayAmenities.GroupBy(ha => ha.AmenityId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await PaginatedList<Amenity>.CreateAsync(query.OrderBy(a => a.Name), page));
    }

    public IActionResult Create() => View("Form", new AmenityFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(AmenityFormViewModel model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await _editor.SaveAmenityAsync(0, model.Name, model.Icon);
        }
        catch (CatalogConflictException ex)
        {
            ModelState.AddModelError(ex.Field, _localizer[ex.ResourceKey]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã thêm \"{0}\".", model.Name.Trim()].Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity is null)
        {
            return NotFound();
        }

        return View("Form", new AmenityFormViewModel { Id = amenity.Id, Name = amenity.Name, Icon = amenity.Icon });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AmenityFormViewModel model)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity is null)
        {
            return NotFound();
        }

        model.Id = id;
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await _editor.SaveAmenityAsync(id, model.Name, model.Icon);
        }
        catch (CatalogConflictException ex)
        {
            ModelState.AddModelError(ex.Field, _localizer[ex.ResourceKey]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã cập nhật \"{0}\".", amenity.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Chỉ xóa tiện nghi chưa được homestay dùng; không xóa liên kết cascade ngoài ý muốn.</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity is null)
        {
            return NotFound();
        }

        try { await _deletion.DeleteAsync(CatalogKind.Amenity, id, User); }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.ResourceKey, ex.Arguments].Value;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = _localizer["Đã xóa \"{0}\".", amenity.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(AmenityFormViewModel model)
    {
        if (await _editor.AmenityExistsAsync(model.Id, model.Name))
            ModelState.AddModelError(nameof(model.Name), _localizer["Tên này đã tồn tại."]);
    }
}
