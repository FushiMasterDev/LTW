using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/areas")]
public sealed class DesktopAreasController(ApplicationDbContext db, CatalogEditorService editor, CatalogDeletionService deletion) : DesktopController(db)
{
    /// <summary>Đọc danh mục, phân trang 10 mục; mọi nhân viên.</summary>
    /// <response code="200">Trang danh mục.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<AreaDto>>(200)]
    public async Task<ActionResult<DesktopPage<AreaDto>>> List([FromQuery] int? page)
    {
        var rows = await PaginatedList<AreaDto>.CreateAsync(Db.Areas.AsNoTracking().OrderBy(a => a.Province).ThenBy(a => a.Name).ThenBy(a => a.Id).Select(a => new AreaDto(a.Id, a.Name, a.Province, a.AreaGroup.ToString())), page);
        return Ok(DesktopPage<AreaDto>.From(rows, x => x));
    }

    /// <summary>Đọc một mục danh mục; mọi nhân viên.</summary>
    /// <response code="200">Mục tìm được.</response>
    /// <response code="404">Không có mục này.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AreaDto>(200)]
    public async Task<ActionResult<AreaDto>> Get(int id) => Ok(await Db.Areas.AsNoTracking().Where(a => a.Id == id)
        .Select(a => new AreaDto(a.Id, a.Name, a.Province, a.AreaGroup.ToString())).SingleOrDefaultAsync() ?? throw new KeyNotFoundException("Không tìm thấy khu vực."));

    /// <summary>Thêm danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="201">Đã thêm, Location trỏ tới mục mới.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="409">Trùng giá trị duy nhất.</response>
    [HttpPost, Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<AreaDto>(201)]
    public async Task<ActionResult<AreaDto>> Create(AreaRequest request)
    {
        var saved = await editor.SaveAreaAsync(0, request.Name, request.Province, request.AreaGroup);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, new AreaDto(saved.Id, saved.Name, saved.Province, saved.AreaGroup.ToString()));
    }

    /// <summary>Sửa danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="200">Mục sau khi sửa.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="404">Không có mục này.</response>
    /// <response code="409">Trùng hoặc cập nhật đồng thời.</response>
    [HttpPut("{id:int}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<AreaDto>(200)]
    public async Task<ActionResult<AreaDto>> Update([Range(1, int.MaxValue)] int id, AreaRequest request)
    {
        var saved = await editor.SaveAreaAsync(id, request.Name, request.Province, request.AreaGroup);
        return Ok(new AreaDto(saved.Id, saved.Name, saved.Province, saved.AreaGroup.ToString()));
    }

    /// <summary>Xóa qua CatalogDeletionService; giữ kiểm đang sử dụng và audit như web, chỉ Management.</summary>
    /// <response code="204">Đã xóa.</response>
    /// <response code="400">Mục đang được sử dụng.</response>
    /// <response code="404">Không có mục này.</response>
    /// <response code="409">Cập nhật đồng thời.</response>
    [HttpDelete("{id:int}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id)
    {
        await deletion.DeleteAsync(CatalogKind.Area, id, await CurrentActorAsync());
        return NoContent();
    }
}
