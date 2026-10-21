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

[Route("api/desktop/room-categories")]
public sealed class DesktopRoomCategoriesController(ApplicationDbContext db, CatalogEditorService editor, CatalogDeletionService deletion) : DesktopController(db)
{
    /// <summary>Đọc danh mục, phân trang 10 mục; mọi nhân viên.</summary>
    /// <response code="200">Trang danh mục.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<CategoryDto>>(200)]
    public async Task<ActionResult<DesktopPage<CategoryDto>>> List([FromQuery] int? page)
    {
        var rows = await PaginatedList<CategoryDto>.CreateAsync(Db.RoomCategories.AsNoTracking().OrderBy(a => a.Name).ThenBy(a => a.Id).Select(a => new CategoryDto(a.Id, a.Name)), page);
        return Ok(DesktopPage<CategoryDto>.From(rows, x => x));
    }

    /// <summary>Đọc một mục danh mục; mọi nhân viên.</summary>
    /// <response code="200">Mục tìm được.</response>
    /// <response code="404">Không có mục này.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(200)]
    public async Task<ActionResult<CategoryDto>> Get(int id) => Ok(await Db.RoomCategories.AsNoTracking().Where(a => a.Id == id)
        .Select(a => new CategoryDto(a.Id, a.Name)).SingleOrDefaultAsync() ?? throw new KeyNotFoundException("Không tìm thấy danh mục loại phòng."));

    /// <summary>Thêm danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="201">Đã thêm, Location trỏ tới mục mới.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="409">Trùng giá trị duy nhất.</response>
    [HttpPost, Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<CategoryDto>(201)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
    {
        var saved = await editor.SaveCategoryAsync(0, request.Name);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, new CategoryDto(saved.Id, saved.Name));
    }

    /// <summary>Sửa danh mục qua cùng service với MVC; chỉ Management.</summary>
    /// <response code="200">Mục sau khi sửa.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="404">Không có mục này.</response>
    /// <response code="409">Trùng hoặc cập nhật đồng thời.</response>
    [HttpPut("{id:int}"), Authorize(Policy = AppPolicies.Management)]
    [ProducesResponseType<CategoryDto>(200)]
    public async Task<ActionResult<CategoryDto>> Update([Range(1, int.MaxValue)] int id, CategoryRequest request)
    {
        var saved = await editor.SaveCategoryAsync(id, request.Name);
        return Ok(new CategoryDto(saved.Id, saved.Name));
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
        await deletion.DeleteAsync(CatalogKind.RoomCategory, id, await CurrentActorAsync());
        return NoContent();
    }
}
