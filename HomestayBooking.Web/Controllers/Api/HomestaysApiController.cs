using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>Tìm kiếm homestay công khai (FR-01): cùng bộ lọc, sắp xếp và phân trang với trang /Homestays.</summary>
[ApiController]
[Route("api/homestays")]
[Produces("application/json")]
public class HomestaysApiController(AvailabilityService availability) : ControllerBase
{
    public record HomestayItem(int Id, string Name, string Area, string? CoverUrl, decimal? FromPrice, int MaxGuests,
        double? Rating, int ReviewCount, IReadOnlyList<string> Amenities);

    public record HomestayPage(IReadOnlyList<HomestayItem> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    /// <summary>Danh sách homestay theo khu vực, ngày, số khách, khoảng giá, loại phòng, tiện nghi; sắp xếp và phân trang.</summary>
    /// <response code="200">Một trang kết quả.</response>
    /// <response code="400">Bộ lọc không hợp lệ (ví dụ khoảng giá ngược, quá 92 đêm).</response>
    [HttpGet]
    [ProducesResponseType<HomestayPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HomestayPage>> Search([FromQuery] HomestaySearchQuery query)
    {
        try
        {
            var results = (await availability.SearchAsync(query)).Results;
            var items = results.Select(h => new HomestayItem(h.Id, h.Name, h.AreaName, h.CoverUrl, h.FromPrice, h.MaxGuests,
                h.Rating, h.ReviewCount, h.Amenities)).ToList();
            return Ok(new HomestayPage(items, results.PageIndex, results.PageSize, results.TotalCount, results.TotalPages));
        }
        catch (BookingException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
