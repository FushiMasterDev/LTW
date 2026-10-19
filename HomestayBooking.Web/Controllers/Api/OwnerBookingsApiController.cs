using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>
/// Chủ homestay xác nhận/từ chối đơn đã đặt cọc (FR-05), cùng quy tắc với Owner/Bookings: chỉ đơn của homestay mình,
/// đổi trạng thái qua BookingService. Đăng nhập bằng cookie nên POST phải gửi token chống giả mạo qua header
/// <c>RequestVerificationToken</c> (bộ lọc AutoValidateAntiforgeryToken toàn cục).
/// </summary>
[ApiController]
[Authorize(Policy = AppPolicies.ApprovedOwner)]
[Route("api/bookings")]
[Produces("application/json")]
public class OwnerBookingsApiController(ApplicationDbContext db, BookingService bookings, IStringLocalizer<SharedResource> L) : ControllerBase
{
    public record ConfirmRequest(int? RoomId);

    public record RejectRequest(string Reason);

    /// <param name="Status">Tên trạng thái sau thao tác (Confirmed, Rejected…).</param>
    public record BookingStatusResult(int Id, string Code, string Status);

    private Actor CurrentActor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>Xác nhận đơn đang chờ chủ xác nhận; có thể chỉ định phòng cụ thể còn trống.</summary>
    /// <response code="200">Đơn đã xác nhận.</response>
    /// <response code="400">Thiếu token chống giả mạo, hoặc đơn không ở trạng thái xác nhận được.</response>
    /// <response code="404">Không có đơn đã đặt cọc này trong homestay của bạn.</response>
    /// <response code="409">Đơn vừa được cập nhật bởi thao tác khác.</response>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType<BookingStatusResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<BookingStatusResult>> Confirm(int id, [FromBody] ConfirmRequest? request) =>
        RunAsync(id, b => bookings.ConfirmAsync(b, request?.RoomId, CurrentActor));

    /// <summary>Từ chối đơn; lý do bắt buộc (tối đa 500 ký tự). Fuji hoàn 100% tiền cọc cho khách.</summary>
    /// <response code="200">Đơn đã bị từ chối.</response>
    /// <response code="400">Thiếu lý do, thiếu token hoặc sai trạng thái.</response>
    /// <response code="404">Không có đơn đã đặt cọc này trong homestay của bạn.</response>
    /// <response code="409">Đơn vừa được cập nhật bởi thao tác khác.</response>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType<BookingStatusResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingStatusResult>> Reject(int id, [FromBody] RejectRequest request)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500)
        {
            return Problem(detail: L["Vui lòng nhập lý do từ chối (tối đa 500 ký tự)."].Value, statusCode: StatusCodes.Status400BadRequest);
        }
        return await RunAsync(id, b => bookings.ChangeStatusAsync(b, BookingStatus.Rejected, CurrentActor, reason));
    }

    /// <summary>Đơn không thuộc homestay của mình hoặc chưa đặt cọc → 404, không lộ là đơn có tồn tại (như Owner/Bookings).</summary>
    private async Task<ActionResult<BookingStatusResult>> RunAsync(int id, Func<Booking, Task> action)
    {
        var personId = User.PersonId();
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == id && b.Homestay.Owner.PersonId == personId && b.DepositedAt != null);
        if (booking is null)
        {
            return Problem(detail: L["Không tìm thấy đơn đặt phòng."].Value, statusCode: StatusCodes.Status404NotFound);
        }
        try
        {
            await action(booking);
            return Ok(new BookingStatusResult(booking.Id, booking.Code, booking.Status.ToString()));
        }
        catch (BookingException ex)
        {
            return Problem(detail: L[ex.ResourceKey, ex.Arguments].Value, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(detail: L["Đơn vừa được cập nhật bởi thao tác khác, vui lòng xem lại."].Value, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
