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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AmenitiesController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
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

        _db.Amenities.Add(new Amenity { Name = model.Name.Trim(), Icon = NormalizeIcon(model.Icon) });
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

        amenity.Name = model.Name.Trim();
        amenity.Icon = NormalizeIcon(model.Icon);
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

        TempData["Success"] = _localizer["Đã cập nhật \"{0}\".", amenity.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Xóa tiện nghi; liên kết với homestay (HomestayAmenities) bị xóa theo (cascade).</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity is null)
        {
            return NotFound();
        }

        _db.Amenities.Remove(amenity);
        await _db.SaveChangesAsync();
        TempData["Success"] = _localizer["Đã xóa \"{0}\".", amenity.Name].Value;
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeIcon(string? icon) =>
        string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();

    private async Task ValidateUniqueNameAsync(AmenityFormViewModel model)
    {
        var name = model.Name?.Trim();
        if (!string.IsNullOrEmpty(name) && await _db.Amenities.AnyAsync(a => a.Name == name && a.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), _localizer["Tên này đã tồn tại."]);
        }
    }
}
