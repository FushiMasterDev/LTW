using HomestayBooking.Web.Services;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

/// <summary>Danh mục loại phòng dùng chung để lọc tìm kiếm; loại phòng cụ thể do từng homestay khai báo.</summary>
public class RoomCategoriesController : AdminController
{
    private readonly ApplicationDbContext _db;
    private readonly CatalogDeletionService _deletion;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RoomCategoriesController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer, CatalogDeletionService deletion)
    {
        _db = db;
        _deletion = deletion;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? q, int? page)
    {
        var query = _db.RoomCategories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(t => t.Name.Contains(q.Trim()));
        }

        ViewData["Query"] = q;
        ViewBag.RoomCounts = await _db.RoomTypes.GroupBy(t => t.CategoryId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await PaginatedList<RoomCategory>.CreateAsync(query.OrderBy(t => t.Name), page));
    }

    public IActionResult Create() => View("Form", new RoomCategoryFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(RoomCategoryFormViewModel model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        _db.RoomCategories.Add(new RoomCategory { Name = model.Name.Trim() });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Request khác vừa ghi cùng giá trị sau bước kiểm tra trùng ở trên
            ModelState.AddModelError(nameof(model.Name), _localizer["Tên này đã tồn tại."]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã thêm \"{0}\".", model.Name.Trim()].Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.RoomCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View("Form", new RoomCategoryFormViewModel { Id = category.Id, Name = category.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, RoomCategoryFormViewModel model)
    {
        var category = await _db.RoomCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        model.Id = id;
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        category.Name = model.Name.Trim();
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Request khác vừa ghi cùng giá trị sau bước kiểm tra trùng ở trên
            ModelState.AddModelError(nameof(model.Name), _localizer["Tên này đã tồn tại."]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã cập nhật \"{0}\".", category.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.RoomCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        try { await _deletion.DeleteAsync(CatalogKind.RoomCategory, id, User); }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.ResourceKey, ex.Arguments].Value;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = _localizer["Đã xóa \"{0}\".", category.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(RoomCategoryFormViewModel model)
    {
        var name = model.Name?.Trim();
        if (!string.IsNullOrEmpty(name) && await _db.RoomCategories.AnyAsync(t => t.Name == name && t.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), _localizer["Tên này đã tồn tại."]);
        }
    }
}
