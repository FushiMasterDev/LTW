using System.Globalization;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/holidays")]
public sealed class DesktopHolidaysController(ApplicationDbContext db, CatalogEditorService editor, CatalogDeletionService deletion) : DesktopController(db)
{
    /// <summary>Danh sách ngày lễ phân trang, ngày yyyy-MM-dd; mọi nhân viên.</summary>
    /// <response code="200">Trang ngày lễ.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<HolidayDto>>(200)]
    public async Task<ActionResult<DesktopPage<HolidayDto>>> List([FromQuery] int? page)
    {
        var rows = await PaginatedList<HolidayDto>.CreateAsync(Db.Holidays.AsNoTracking().OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Date, h.Name)), page);
        return Ok(DesktopPage<HolidayDto>.From(rows, x => x));
    }

    /// <summary>Đọc ngày lễ; id là ngày yyyy-MM-dd, mọi nhân viên.</summary>
    /// <response code="200">Ngày lễ.</response>
    /// <response code="404">Không có ngày lễ hoặc id sai định dạng.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<HolidayDto>(200)]
    public async Task<ActionResult<HolidayDto>> Get(string id)
    {
        var date = DateId(id);
        return Ok(await Db.Holidays.AsNoTracking().Where(h => h.Date == date).Select(h => new HolidayDto(h.Date, h.Name))
            .SingleOrDefaultAsync() ?? throw new KeyNotFoundException("Không tìm thấy ngày lễ."));
    }

    /// <summary>Thêm ngày lễ, chỉ Management.</summary>
    /// <response code="201">Đã thêm.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="409">Ngày đã có trong danh sách.</response>
    [HttpPost, Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<HolidayDto>(201)]
    public async Task<ActionResult<HolidayDto>> Create(HolidayRequest request)
    {
        var saved = await editor.CreateHolidayAsync(request.Date!.Value, request.Name);
        return CreatedAtAction(nameof(Get), new { id = saved.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }, new HolidayDto(saved.Date, saved.Name));
    }

    /// <summary>Sửa tên ngày lễ, ngày là khóa chính giữ nguyên như web; chỉ Management.</summary>
    /// <response code="200">Ngày lễ sau sửa.</response>
    /// <response code="400">Tên không hợp lệ.</response>
    /// <response code="404">Không có ngày lễ.</response>
    [HttpPut("{id}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<HolidayDto>(200)]
    public async Task<ActionResult<HolidayDto>> Update(string id, HolidayUpdateRequest request)
    {
        var saved = await editor.UpdateHolidayAsync(DateId(id), request.Name);
        return Ok(new HolidayDto(saved.Date, saved.Name));
    }

    /// <summary>Xóa ngày lễ qua CatalogDeletionService; chỉ Management.</summary>
    /// <response code="204">Đã xóa, giá đã chốt của đơn cũ không đổi.</response>
    /// <response code="404">Không có ngày lễ.</response>
    [HttpDelete("{id}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string id)
    {
        await deletion.DeleteAsync(CatalogKind.Holiday, DateId(id), await CurrentActorAsync());
        return NoContent();
    }

    private static DateOnly DateId(string id) => DateOnly.TryParseExact(id, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var date) ? date : throw new KeyNotFoundException("Không tìm thấy ngày lễ.");
}
