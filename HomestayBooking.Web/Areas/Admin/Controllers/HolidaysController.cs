using System.Globalization;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

/// <summary>Ngày lễ áp giá PriceHoliday (BR-08). Khóa chính là ngày, truyền qua URL dạng yyyy-MM-dd.</summary>
public class HolidaysController : AdminController
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public HolidaysController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int? year, string? q, int? page)
    {
        if (year is < 1 or > 9999)
        {
            year = null;
        }

        var query = _db.Holidays.AsNoTracking();
        if (year.HasValue)
        {
            var from = new DateOnly(year.Value, 1, 1);
            var to = new DateOnly(year.Value, 12, 31);
            query = query.Where(h => h.Date >= from && h.Date <= to);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(h => h.Name.Contains(q.Trim()));
        }

        ViewData["Query"] = q;
        ViewData["Year"] = year;
        ViewBag.Years = await _db.Holidays.Select(h => h.Date.Year).Distinct().OrderBy(y => y).ToListAsync();
        return View(await PaginatedList<Holiday>.CreateAsync(query.OrderBy(h => h.Date), page));
    }

    public IActionResult Create() => View("Form", new HolidayFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(HolidayFormViewModel model)
    {
        model.IsNew = true;
        if (model.Date.HasValue && await _db.Holidays.AnyAsync(h => h.Date == model.Date.Value))
        {
            ModelState.AddModelError(nameof(model.Date), _localizer["Ngày này đã có trong danh sách."]);
        }
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        _db.Holidays.Add(new Holiday { Date = model.Date!.Value, Name = model.Name.Trim() });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Request khác vừa thêm cùng ngày sau bước kiểm tra trùng ở trên
            ModelState.AddModelError(nameof(model.Date), _localizer["Ngày này đã có trong danh sách."]);
            return View("Form", model);
        }

        TempData["Success"] = _localizer["Đã thêm \"{0}\".", model.Name.Trim()].Value;
        return RedirectToAction(nameof(Index), new { year = model.Date.Value.Year });
    }

    public async Task<IActionResult> Edit(string id)
    {
        var holiday = await FindAsync(id);
        if (holiday is null)
        {
            return NotFound();
        }

        return View("Form", new HolidayFormViewModel { IsNew = false, Date = holiday.Date, Name = holiday.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, HolidayFormViewModel model)
    {
        var holiday = await FindAsync(id);
        if (holiday is null)
        {
            return NotFound();
        }

        // Ngày là khóa chính, không cho đổi khi sửa
        model.IsNew = false;
        model.Date = holiday.Date;
        ModelState.Remove(nameof(model.Date));
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        holiday.Name = model.Name.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = _localizer["Đã cập nhật \"{0}\".", holiday.Name].Value;
        return RedirectToAction(nameof(Index), new { year = holiday.Date.Year });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var holiday = await FindAsync(id);
        if (holiday is null)
        {
            return NotFound();
        }

        _db.Holidays.Remove(holiday);
        await _db.SaveChangesAsync();
        TempData["Success"] = _localizer["Đã xóa \"{0}\".", holiday.Name].Value;
        return RedirectToAction(nameof(Index), new { year = holiday.Date.Year });
    }

    private async Task<Holiday?> FindAsync(string id) =>
        DateOnly.TryParseExact(id, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? await _db.Holidays.FindAsync(date)
            : null;
}
