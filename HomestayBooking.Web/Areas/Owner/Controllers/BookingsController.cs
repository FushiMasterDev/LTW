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
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly TimeProvider _clock;
    private readonly ChangeRequestUiService _changeUi;

    public BookingsController(ApplicationDbContext db, BookingService bookings, IStringLocalizer<SharedResource> localizer, TimeProvider clock, ChangeRequestUiService changeUi)
    {
        _db = db;
        _bookings = bookings;
        _localizer = localizer;
        _clock = clock;
        _changeUi = changeUi;
    }

    private IQueryable<Booking> OwnBookings()
    {
        var personId = User.PersonId();
        return _db.Bookings.Where(b => b.Homestay.Owner.PersonId == personId && b.DepositedAt != null);
    }

    private Actor CurrentActor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>Việc cần làm trước: chờ xác nhận, rồi theo ngày nhận phòng gần nhất.</summary>
    public async Task<IActionResult> Index(BookingStatus? status, string? q, int? page, DateOnly? arrivals = null)
    {
        if (arrivals is { } date) return RedirectToAction("Index", "Schedule", new { date = date.ToString("yyyy-MM-dd") });
        var query = OwnBookings().AsNoTracking();
        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var phone = PhoneNumberNormalizer.ToE164(term);
            query = query.Where(b => b.Code.Contains(term) || b.ContactName.Contains(term) || (phone != null && b.ContactPhone == phone));
        }

        var rows = query
            // Chờ xác nhận → sắp ở / đang ở (CheckIn gần nhất trước) → đã đóng (CheckIn gần đây trước)
            .OrderBy(b => b.Status == BookingStatus.Deposited ? 0
                : b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn ? 1 : 2)
            .ThenBy(b => b.Status == BookingStatus.Deposited || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn
                ? b.CheckIn : DateOnly.MaxValue)
            .ThenByDescending(b => b.CheckIn)
            .Select(b => new OwnerBookingRow(
                b.Id, b.Code, b.Status, b.ContactName, b.ContactPhone, b.Homestay.Name, b.RoomType.Name, b.Room.RoomCode,
                b.CheckIn, b.CheckOut, b.Guests, b.TotalAmount, b.ConfirmDeadlineAt));

        ViewData["Status"] = status;
        ViewData["Query"] = q;
        return View(await PaginatedList<OwnerBookingRow>.CreateAsync(rows, page));
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await OwnBookings().AsNoTracking()
            .Include(b => b.Homestay)
            .Include(b => b.RoomType)
            .Include(b => b.Room)
            .Include(b => b.Nights)
            .Include(b => b.Histories)
            .SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null)
        {
            return NotFound();
        }

        // Phòng để đổi khi xác nhận (Q8): cùng tiêu chí "trống" với lúc đặt
        var freeIds = booking.Status == BookingStatus.Deposited
            ? await _bookings.FreeRoomIdsAsync(booking.RoomTypeId, booking.CheckIn, booking.CheckOut)
            : [];
        var freeRooms = await _db.Rooms.AsNoTracking()
            .Where(r => freeIds.Contains(r.Id))
            .OrderBy(r => r.RoomCode)
            .Select(r => new OwnerRoomOption(r.Id, r.RoomCode))
            .ToListAsync();
        var history = await _changeUi.HistoryAsync([booking.Id]);
        return View(new OwnerBookingDetailsViewModel(booking, freeRooms, VietnamTime.Today(_clock)) { ChangeHistory = history });
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
            return await OwnBookings().AnyAsync(b => b.Id == id) ? RedirectToAction(nameof(Details), new { id }) : NotFound();
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
        var booking = await OwnBookings().SingleOrDefaultAsync(b => b.Id == id);
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
