using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>
/// Lịch trống và báo giá công khai theo loại phòng (FR-02). "{id}" là mã loại phòng: khách đặt theo loại phòng,
/// hệ thống tự xếp phòng cụ thể khi giữ đêm.
/// </summary>
[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public class RoomsApiController(AvailabilityService availability, PricingService pricing, TimeProvider clock) : ControllerBase
{
    /// <param name="DayType">Weekday, Weekend hoặc Holiday.</param>
    public record NightQuote(DateOnly Date, string DayType, decimal Price);

    public record RoomQuote(int RoomTypeId, DateOnly CheckIn, DateOnly CheckOut, int FreeRooms, decimal Total, decimal DepositRate,
        decimal Deposit, IReadOnlyList<NightQuote> Nights);

    /// <summary>Các đêm đã kín (không còn phòng nào của loại phòng) trong khoảng [from, to), tối đa 92 đêm.</summary>
    /// <response code="200">Mảng ngày dạng yyyy-MM-dd.</response>
    /// <response code="400">Thiếu hoặc sai khoảng ngày.</response>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<string>>> Availability(int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (id <= 0 || from is null || to is null)
        {
            return Problem(detail: "Cần mã loại phòng, from và to.", statusCode: StatusCodes.Status400BadRequest);
        }
        try
        {
            var nights = await availability.BlockedDatesAsync(id, from.Value, to.Value);
            return Ok(nights.Select(n => n.ToString("yyyy-MM-dd")));
        }
        catch (BookingException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Báo giá theo từng đêm (ngày thường/cuối tuần/lễ), tổng tiền, tiền cọc và số phòng còn trống. Không tính voucher.</summary>
    /// <response code="200">Báo giá.</response>
    /// <response code="400">Ngày nhận ở quá khứ, ngày trả không sau ngày nhận hoặc khoảng quá dài.</response>
    /// <response code="404">Loại phòng không tồn tại hoặc chưa có bảng giá cho khoảng ngày.</response>
    [HttpGet("{id:int}/quote")]
    [ProducesResponseType<RoomQuote>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomQuote>> Quote(int id, [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut)
    {
        if (checkIn < VietnamTime.Today(clock))
        {
            return Problem(detail: "Ngày nhận phòng không được ở quá khứ.", statusCode: StatusCodes.Status400BadRequest);
        }
        try
        {
            var quote = await pricing.QuoteAsync(id, checkIn, checkOut);
            var free = (await availability.FreeRoomIdsAsync(id, checkIn, checkOut)).Count;
            return Ok(new RoomQuote(id, checkIn, checkOut, free, quote.Total, quote.DepositRate, quote.Deposit,
                quote.Nights.Select(n => new NightQuote(n.Date, n.DayType.ToString(), n.Price)).ToList()));
        }
        catch (BookingException ex) when (ex.Message.Contains("chưa có bảng giá", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (BookingException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
