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

[Route("api/desktop/amenities")]
public sealed class DesktopAmenitiesController(ApplicationDbContext db, CatalogEditorService editor, CatalogDeletionService deletion) : DesktopController(db)
{
    /// <summary>Đọc danh mục, phân trang 10 mục; mọi nhân viên.</summary>
    /// <response code="200">Trang danh mục.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<AmenityDto>>(200)]
    public async Task<ActionResult<DesktopPage<AmenityDto>>> List([FromQuery] int? page)
    {
        var rows = await PaginatedList<AmenityDto>.CreateAsync(Db.Amenities.AsNoTracking().OrderBy(a => a.Name).ThenBy(a => a.Id).Select(a => new AmenityDto(a.Id, a.Name, a.Icon)), page);
        return Ok(DesktopPage<AmenityDto>.From(rows, x => x));
    }

    /// <summary>Đọc một mục danh mục; mọi nhân viên.</summary>
    /// <response code="200">Mục tìm được.</response>
    /// <response code="404">Không có mục này.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AmenityDto>(200)]
    public async Task<ActionResult<AmenityDto>> Get(int id) => Ok(await Db.Amenities.AsNoTracking().Where(a => a.Id == id)
        .Select(a => new AmenityDto(a.Id, a.Name, a.Icon)).SingleOrDefaultAsync() ?? throw new KeyNotFoundException("Không tìm thấy tiện nghi."));

    /// <summary>Thêm danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="201">Đã thêm, Location trỏ tới mục mới.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="409">Trùng giá trị duy nhất.</response>
    [HttpPost, Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<AmenityDto>(201)]
    public async Task<ActionResult<AmenityDto>> Create(AmenityRequest request)
    {
        var saved = await editor.SaveAmenityAsync(0, request.Name, request.Icon);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, new AmenityDto(saved.Id, saved.Name, saved.Icon));
    }

    /// <summary>Sửa danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="200">Mục sau khi sửa.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="404">Không có mục này.</response>
    /// <response code="409">Trùng hoặc cập nhật đồng thời.</response>
    [HttpPut("{id:int}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<AmenityDto>(200)]
    public async Task<ActionResult<AmenityDto>> Update([Range(1, int.MaxValue)] int id, AmenityRequest request)
    {
        var saved = await editor.SaveAmenityAsync(id, request.Name, request.Icon);
        return Ok(new AmenityDto(saved.Id, saved.Name, saved.Icon));
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
        await deletion.DeleteAsync(CatalogKind.Amenity, id, await CurrentActorAsync());
        return NoContent();
    }
}
