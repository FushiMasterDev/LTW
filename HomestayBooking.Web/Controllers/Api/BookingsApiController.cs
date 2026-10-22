using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>Tra cứu đơn công khai bằng mã đơn + số điện thoại (FR-03), cùng điều kiện với trang /Bookings/Lookup.</summary>
[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingsApiController(ApplicationDbContext db) : ControllerBase
{
    /// <param name="Status">Tên trạng thái đơn (AwaitingPayment, Deposited, Confirmed, Cancelled, Expired…).</param>
    public record BookingSummary(string Code, string Status, string Homestay, string RoomType, DateOnly CheckIn,
        DateOnly CheckOut, int Guests, decimal TotalAmount, decimal DepositAmount, DateTime HoldExpiresAt);

    /// <summary>Trạng thái và số tiền của đơn. Sai mã hoặc sai SĐT đều trả 404 để không lộ đơn có tồn tại hay không.</summary>
    /// <response code="200">Tóm tắt đơn.</response>
    /// <response code="404">Không có đơn khớp mã và số điện thoại.</response>
    [HttpGet("{code}")]
    [EnableRateLimiting(PublicRateLimits.Policy)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<BookingSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingSummary>> Lookup(string code, [FromQuery] string? phone)
    {
        var normalizedPhone = PhoneNumberNormalizer.ToE164(phone);
        if (normalizedPhone is null)
        {
            return Problem(detail: "Không tìm thấy đơn khớp với mã đơn và số điện thoại.", statusCode: StatusCodes.Status404NotFound);
        }
        var normalizedCode = code.Trim().ToUpperInvariant();
        // Khớp SĐT liên hệ của đơn hoặc SĐT của khách đặt (như BookingsController.Lookup)
        var booking = await db.Bookings.AsNoTracking()
            .Where(b => b.Code == normalizedCode && (b.ContactPhone == normalizedPhone || b.Customer.Person.PhoneNumber == normalizedPhone))
            .Select(b => new { b.Code, b.Status, Homestay = b.Homestay.Name, RoomType = b.RoomType.Name, b.CheckIn, b.CheckOut, b.Guests,
                b.TotalAmount, b.DepositAmount, b.HoldExpiresAt })
            .SingleOrDefaultAsync();
        return booking is null
            ? Problem(detail: "Không tìm thấy đơn khớp với mã đơn và số điện thoại.", statusCode: StatusCodes.Status404NotFound)
            : Ok(new BookingSummary(booking.Code, booking.Status.ToString(), booking.Homestay, booking.RoomType, booking.CheckIn,
                booking.CheckOut, booking.Guests, booking.TotalAmount, booking.DepositAmount, DateTime.SpecifyKind(booking.HoldExpiresAt, DateTimeKind.Utc)));
    }
}
