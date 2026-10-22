using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop.Owner;

[Route("api/desktop/owner")]
public sealed class DesktopOwnerWorkspaceController(OwnerDashboardService dashboard, OwnerBookingQueryService queries,
    BookingService bookings, OwnerScheduleService schedules, HomestayContentService content, RoomBlockService blocks,
    OwnerResourceService access, TimeProvider clock) : DesktopOwnerController
{
    /// <summary>Tổng quan của chủ: hồ sơ, homestay phân trang và số đơn cần xử lý như web.</summary>
    /// <response code="200">Tổng quan riêng của chủ hiện tại.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType<OwnerDashboardDto>(200)]
    public async Task<ActionResult<OwnerDashboardDto>> Dashboard([FromQuery] int? page)
    {
        var d = await dashboard.ReadAsync(User.PersonId(), true, Actor) ?? throw new BookingException("Không tìm thấy homestay.");
        return Ok(new OwnerDashboardDto(d.RegistrationStatus?.ToString(), d.ReviewNote,
            DesktopPage<OwnerHomestayDto>.Slice(d.Homestays.Select(h => new OwnerHomestayDto(h.Id, h.Name, h.AreaName, h.Status.ToString(), null, h.RoomCount)), page),
            d.ArrivalsToday, d.PendingConfirmation, d.OverdueConfirmation));
    }

    /// <summary>Lịch nhận/trả theo today, tomorrow, week hoặc ngày date; lọc homestay của mình.</summary>
    /// <response code="200">Lịch gồm cả ngày đầu/cuối.</response>
    /// <response code="400">Sai period/ngày.</response>
    /// <response code="404">Homestay ngoài phạm vi.</response>
    [HttpGet("schedule")]
    [ProducesResponseType<OwnerScheduleDto>(200)]
    public async Task<ActionResult<OwnerScheduleDto>> Schedule([FromQuery] string period = "today", [FromQuery] int? homestayId = null, [FromQuery] DateOnly? date = null)
    {
        if (homestayId is int home) await access.HomeAsync(home, Actor);
        if (period is not ("today" or "tomorrow" or "week")) throw new BookingException("Khoảng lịch nhận–trả phải từ 1 đến 31 ngày.");
        var today = VietnamTime.Today(clock);
        var from = date ?? (period == "tomorrow" ? today.AddDays(1) : today);
        if (period == "week" && from > DateOnly.MaxValue.AddDays(-6)) throw new BookingException("Khoảng lịch nhận–trả phải từ 1 đến 31 ngày.");
        var through = period == "week" ? from.AddDays(6) : from;
        var d = await schedules.ReadAsync(Actor, from, through, homestayId);
        return Ok(new OwnerScheduleDto(period, from, through, d.Arrivals.Select(DesktopOwnerMapping.Schedule).ToList(), d.Departures.Select(DesktopOwnerMapping.Schedule).ToList()));
    }

    /// <summary>Đơn đã cọc của mình, cùng lọc và thứ tự ưu tiên như Owner/Bookings; phân trang.</summary>
    /// <response code="200">Trang đơn.</response>
    /// <response code="400">Sai bộ lọc.</response>
    [HttpGet("bookings")]
    [ProducesResponseType<DesktopPage<OwnerBookingRowDto>>(200)]
    public async Task<ActionResult<DesktopPage<OwnerBookingRowDto>>> Bookings([FromQuery] BookingStatus? status, [FromQuery] string? q, [FromQuery] int? page)
    {
        if (status is { } s && !Enum.IsDefined(s)) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        return Ok(DesktopPage<OwnerBookingRowDto>.From(await queries.SearchAsync(User.PersonId(), status, q, page), b => new(b.Id, b.Code, b.Status.ToString(),
            b.ContactName, b.ContactPhone, b.HomestayName, b.RoomTypeName, b.RoomCode, b.CheckIn, b.CheckOut, b.Guests, b.TotalAmount, Utc(b.ConfirmDeadlineAt))));
    }

    /// <summary>Chi tiết đơn đã cọc, phòng còn trống khi xác nhận và lịch sử như web; không trả entity.</summary>
    /// <response code="200">Chi tiết đơn.</response>
    /// <response code="404">Không có đơn trong phạm vi của chủ.</response>
    [HttpGet("bookings/{id:int}")]
    [ProducesResponseType<OwnerBookingDetailsDto>(200)]
    public async Task<ActionResult<OwnerBookingDetailsDto>> Booking(int id)
    {
        var d = await queries.DetailsAsync(id, User.PersonId()) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        var b = d.Booking;
        return Ok(new OwnerBookingDetailsDto(new(b.Id, b.Code, b.Status.ToString(), b.ContactName, b.ContactPhone, b.HomestayId, b.Homestay.Name,
            b.RoomTypeId, b.RoomType.Name, b.RoomId, b.Room.RoomCode, b.CheckIn, b.CheckOut, b.Guests, b.TotalAmount, b.DepositAmount,
            Utc(b.ConfirmDeadlineAt), b.SpecialRequests, b.CancelReason), d.FreeRooms.Select(r => new OwnerRoomOptionDto(r.Id, r.RoomCode)).ToList(), d.Today,
            b.Nights.OrderBy(n => n.Date).Select(n => new BookingNightDto(n.Date, n.DayType.ToString(), n.Price)).ToList(),
            b.Histories.OrderBy(h => h.ChangedAt).Select(h => new BookingHistoryDto(h.Id, h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ActorType.ToString(), null, h.Note, Utc(h.ChangedAt))).ToList(),
            d.ChangeHistory.Select(h => new OwnerChangeDto(h.Request.Id, h.Request.RequestType.ToString(), h.Request.Status.ToString(), h.OldCheckIn, h.OldCheckOut,
                h.OldRoomType, h.Request.NewCheckIn, h.Request.NewCheckOut, h.NewRoomType, h.Request.PriceDifference, h.Request.RefundAmount, h.Request.ChangeFee,
                h.Request.DepositCarriedOver, h.Request.AdditionalDeposit, h.Request.NewTotalAmount, Utc(h.Request.CreatedAt), Utc(h.Request.DecidedAt),
                h.Request.DecisionNote, h.Request.RejectionReason, h.IsLegacy)).ToList()));
    }

    /// <summary>Xác nhận đơn đã cọc, tùy chọn đổi sang phòng trống cùng loại qua BookingService.</summary>
    /// <response code="200">Trạng thái mới.</response>
    /// <response code="400">Sai trạng thái hoặc phòng không phù hợp.</response>
    [HttpPost("bookings/{id:int}/confirm")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public Task<ActionResult<BookingStatusDto>> Confirm(int id, OwnerConfirmRequest request) => Run(id, async b =>
    {
        if (request.RoomId is int room) await access.RoomAsync(b.HomestayId, room, Actor);
        await bookings.ConfirmAsync(b, request.RoomId, Actor);
    });

    /// <summary>Từ chối có lý do, hoàn cọc theo nghiệp vụ hiện có.</summary>
    /// <response code="200">Đã từ chối.</response>
    /// <response code="400">Thiếu lý do hoặc sai trạng thái.</response>
    [HttpPost("bookings/{id:int}/reject")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public Task<ActionResult<BookingStatusDto>> Reject(int id, ReasonRequest request) => Run(id, b => bookings.ChangeStatusAsync(b, BookingStatus.Rejected, Actor, request.Reason.Trim()));

    /// <summary>Ghi nhận nhận phòng, giữ kiểm ngày của BookingService.</summary>
    /// <response code="200">Đã nhận phòng.</response>
    /// <response code="400">Chưa đến ngày hoặc sai trạng thái.</response>
    [HttpPost("bookings/{id:int}/check-in")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public Task<ActionResult<BookingStatusDto>> CheckIn(int id) => Run(id, b => bookings.ChangeStatusAsync(b, BookingStatus.CheckedIn, Actor));

    /// <summary>Hoàn thành đơn khách đã nhận phòng qua BookingService.</summary>
    /// <response code="200">Đã hoàn thành.</response>
    /// <response code="400">Sai trạng thái/ngày.</response>
    [HttpPost("bookings/{id:int}/complete")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public Task<ActionResult<BookingStatusDto>> Complete(int id) => Run(id, b => bookings.ChangeStatusAsync(b, BookingStatus.Completed, Actor));

    /// <summary>Đánh dấu khách không đến sau ngày nhận phòng, theo luật giữ cọc chung.</summary>
    /// <response code="200">Đã ghi nhận không đến.</response>
    /// <response code="400">Chưa đến ngày hoặc sai trạng thái.</response>
    [HttpPost("bookings/{id:int}/no-show")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public Task<ActionResult<BookingStatusDto>> NoShow(int id) => Run(id, b => bookings.ChangeStatusAsync(b, BookingStatus.NoShow, Actor));

    private async Task<ActionResult<BookingStatusDto>> Run(int id, Func<Booking, Task> action)
    {
        var b = await queries.FindAsync(id, User.PersonId()) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        await action(b);
        return Ok(new BookingStatusDto(b.Id, b.Code, b.Status.ToString()));
    }

    /// <summary>Lịch tháng yyyy-MM, cùng lưới tuần như web; roomId tùy chọn chỉ được thuộc homestay đã chọn.</summary>
    /// <response code="200">Lưới ngày và trạng thái các đêm của homestay.</response>
    /// <response code="404">Homestay/phòng ngoài phạm vi.</response>
    [HttpGet("calendar")]
    [ProducesResponseType<OwnerCalendarDto>(200)]
    public async Task<ActionResult<OwnerCalendarDto>> Calendar([FromQuery] int? homestayId, [FromQuery] int? roomId, [FromQuery] string? month)
    {
        homestayId ??= (await content.OwnerHomestaysAsync(Actor)).FirstOrDefault()?.Id;
        if (homestayId is int home) await access.HomeAsync(home, Actor);
        var today = VietnamTime.Today(clock);
        var date = RoomCalendarModel.ParseMonth(month, today);
        var shape = new RoomCalendarModel(date, []);
        var nights = homestayId is int h ? await blocks.CalendarAsync(h, shape.Days.First(), shape.Days.Last().AddDays(1), Actor) : [];
        var rooms = nights.Select(n => new OwnerRoomOptionDto(n.RoomId, n.RoomCode)).Distinct().ToList();
        if (roomId is int selected && !rooms.Any(r => r.Id == selected)) throw new BookingException("Không tìm thấy phòng.");
        return Ok(new OwnerCalendarDto(date.ToString("yyyy-MM"), homestayId, roomId ?? rooms.FirstOrDefault()?.Id, today, shape.Days, rooms,
            nights.Where(n => roomId == null || n.RoomId == roomId).Select(n => new OwnerCalendarNightDto(n.RoomId, n.RoomCode, n.Date, n.State.ToString(), n.Code, n.BookingStatus?.ToString(), n.BlockReason?.ToString())).ToList()));
    }

    /// <summary>Khóa đêm [start,end), dùng cùng validator và kiểm trùng của RoomBlockService.</summary>
    /// <response code="201">Khóa đã tạo.</response>
    /// <response code="400">Ngày/lý do sai.</response>
    /// <response code="409">Đêm đã bị chiếm.</response>
    [HttpPost("homestays/{homeId:int}/rooms/{roomId:int}/blocks")]
    [ProducesResponseType<OwnerBlockDto>(201)]
    public async Task<ActionResult<OwnerBlockDto>> CreateBlock(int homeId, int roomId, OwnerBlockRequest request)
    {
        await access.RoomAsync(homeId, roomId, Actor);
        var b = await blocks.CreateAsync(roomId, request.Start, request.End, request.Reason, request.Note, Actor);
        return Created($"{Request.PathBase}/api/desktop/owner/calendar?homestayId={homeId}&roomId={roomId}&month={b.StartDate:yyyy-MM}",
            new OwnerBlockDto(b.Id, b.RoomId, b.StartDate, b.EndDate, b.Reason.ToString(), b.Note));
    }

    /// <summary>Mở phần đêm từ hôm nay trở đi của đúng khóa trên đúng phòng thuộc chủ.</summary>
    /// <response code="204">Đã mở khóa.</response>
    /// <response code="400">Khóa đã kết thúc.</response>
    [HttpDelete("homestays/{homeId:int}/rooms/{roomId:int}/blocks/{blockId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Unlock(int homeId, int roomId, int blockId)
    {
        await access.BlockAsync(homeId, roomId, blockId, Actor);
        await blocks.UnlockAsync(blockId, Actor);
        return NoContent();
    }
}
