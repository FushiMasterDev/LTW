using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Owner.Controllers;

/// <summary>
/// Đơn của homestay mình (database v1 mục 9): chỉ thấy đơn từ khi đã đặt cọc. Mọi action lọc theo chủ sở hữu
/// ở server; đổi trạng thái đi qua BookingService (mục 6.1).
/// </summary>
[Area(AppRoles.Owner)]
[Authorize(Policy = AppPolicies.ApprovedOwner)]
public class BookingsController : Controller
{
    private readonly OwnerBookingQueryService _queries;
    private readonly BookingService _bookings;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BookingsController(OwnerBookingQueryService queries, BookingService bookings, IStringLocalizer<SharedResource> localizer)
    {
        _queries = queries;
        _bookings = bookings;
        _localizer = localizer;
    }

    private Actor CurrentActor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>Việc cần làm trước: chờ xác nhận, rồi theo ngày nhận phòng gần nhất.</summary>
    public async Task<IActionResult> Index(BookingStatus? status, string? q, int? page, DateOnly? arrivals = null)
    {
        if (arrivals is { } date) return RedirectToAction("Index", "Schedule", new { date = date.ToString("yyyy-MM-dd") });
        ViewData["Status"] = status;
        ViewData["Query"] = q;
        return View(await _queries.SearchAsync(User.PersonId(), status, q, page));
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _queries.DetailsAsync(id, User.PersonId());
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public Task<IActionResult> Confirm(int id, int? roomId) =>
        RunAsync(id, "Đã xác nhận đơn {0}.", b => _bookings.ConfirmAsync(b, roomId, CurrentActor));

    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500)
        {
            TempData["Error"] = _localizer["Vui lòng nhập lý do từ chối (tối đa 500 ký tự)."].Value;
            return (await _queries.FindAsync(id, User.PersonId())) is not null ? RedirectToAction(nameof(Details), new { id }) : NotFound();
        }
        return await RunAsync(id, "Đã từ chối đơn {0}. Fuji sẽ hoàn 100% tiền cọc cho khách.",
            b => _bookings.ChangeStatusAsync(b, BookingStatus.Rejected, CurrentActor, reason));
    }

    [HttpPost]
    public Task<IActionResult> CheckIn(int id) =>
        RunAsync(id, "Đã ghi nhận khách nhận phòng đơn {0}.", b => _bookings.ChangeStatusAsync(b, BookingStatus.CheckedIn, CurrentActor));

    [HttpPost]
    public Task<IActionResult> Complete(int id) =>
        RunAsync(id, "Đã hoàn thành đơn {0}.", b => _bookings.ChangeStatusAsync(b, BookingStatus.Completed, CurrentActor));

    [HttpPost]
    public Task<IActionResult> NoShow(int id) =>
        RunAsync(id, "Đã đánh dấu khách không đến đơn {0}.", b => _bookings.ChangeStatusAsync(b, BookingStatus.NoShow, CurrentActor));

    /// <summary>Đơn không thuộc homestay của mình → 404, không lộ là đơn có tồn tại.</summary>
    private async Task<IActionResult> RunAsync(int id, string successMessage, Func<Booking, Task> action)
    {
        var booking = await _queries.FindAsync(id, User.PersonId());
        if (booking is null)
        {
            return NotFound();
        }

        try
        {
            await action(booking);
            TempData["Success"] = _localizer[successMessage, booking.Code].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = _localizer["Đơn vừa được cập nhật bởi thao tác khác, vui lòng xem lại."].Value;
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}

public record OwnerBookingRow(
    int Id, string Code, BookingStatus Status, string ContactName, string ContactPhone, string HomestayName,
    string RoomTypeName, string RoomCode, DateOnly CheckIn, DateOnly CheckOut, int Guests, decimal TotalAmount,
    DateTime? ConfirmDeadlineAt);

public record OwnerRoomOption(int Id, string RoomCode);

public record OwnerBookingDetailsViewModel(Booking Booking, List<OwnerRoomOption> FreeRooms, DateOnly Today)
{
    public List<HomestayBooking.Web.Models.ViewModels.ChangeRequestHistoryRow> ChangeHistory { get; init; } = [];
}
