using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop")]
[Authorize(Policy = DesktopApiRegistration.StaffPolicy)]
public sealed class DesktopBookingsController(ApplicationDbContext db, StaffBookingService bookings, AvailabilityService availability) : DesktopController(db)
{
    /// <summary>Tìm đơn theo StaffBookingQuery, phân trang 10 đơn; timestamp UTC, ngày yyyy-MM-dd.</summary>
    /// <response code="200">Trang đơn.</response>
    /// <response code="400">Bộ lọc không hợp lệ.</response>
    [HttpGet("bookings")]
    [ProducesResponseType<DesktopPage<BookingDto>>(200)]
    public async Task<ActionResult<DesktopPage<BookingDto>>> Search([FromQuery] StaffBookingQuery query)
    {
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value)) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        return Ok(DesktopPage<BookingDto>.From(await bookings.SearchAsync(query, await CurrentActorAsync()), DesktopMapping.Booking));
    }

    /// <summary>Chi tiết đơn, giá từng đêm, lịch sử trạng thái, thay đổi, thanh toán và audit; timestamp UTC.</summary>
    /// <response code="200">Chi tiết qua StaffBookingService.</response>
    /// <response code="404">Không có đơn.</response>
    [HttpGet("bookings/{id:int}")]
    [ProducesResponseType<BookingDetailsDto>(200)]
    public async Task<ActionResult<BookingDetailsDto>> Details(int id)
    {
        var d = await bookings.DetailsAsync(id, await CurrentActorAsync());
        return Ok(new BookingDetailsDto(DesktopMapping.Booking(d.Booking),
            d.Booking.Nights.OrderBy(n => n.Date).Select(n => new BookingNightDto(n.Date, n.DayType.ToString(), n.Price)).ToList(),
            d.History.Select(h => new BookingHistoryDto(h.Id, h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ActorType.ToString(),
                h.ActorAccount?.Person.FullName, h.Note, Utc(h.ChangedAt))).ToList(),
            d.Changes.Select(c => new BookingChangeDto(c.Id, c.RequestType.ToString(), c.Status.ToString(), c.NewCheckIn, c.NewCheckOut,
                c.NewRoomTypeId, c.PriceDifference, c.RefundAmount, Utc(c.CreatedAt))).ToList(),
            d.Payments.Select(p => new PaymentDto(p.Id, p.Type.ToString(), p.Method.ToString(), p.Amount, p.Status.ToString(),
                Utc(p.SubmittedAt), Utc(p.ReviewedAt), p.RejectReason)).ToList(),
            d.Audit.Select(a => new AuditDto(a.Id, a.Action.ToString(), a.Account?.Person.FullName, a.OldValues, a.NewValues, Utc(a.CreatedAt))).ToList()));
    }

    /// <summary>Đặt hộ qua CreateForCustomerAsync. Gửi lại cùng submissionId để không tạo đơn đôi; không cần antiforgery.</summary>
    /// <response code="201">Đơn đã tạo hoặc đơn của lần gửi trước cùng submissionId.</response>
    /// <response code="400">Sai dữ liệu hoặc không còn phòng theo luật đặt phòng chung.</response>
    /// <response code="409">Xung đột ghi dữ liệu.</response>
    [HttpPost("bookings")]
    [ProducesResponseType<BookingStatusDto>(201)]
    public async Task<ActionResult<BookingStatusDto>> Create(BookingCreateRequest request)
    {
        if (request.SubmissionId == Guid.Empty) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
        var b = await bookings.CreateForCustomerAsync(new(request.Phone, request.FullName, request.Email, request.RoomTypeId,
            request.CheckIn, request.CheckOut, request.Guests, request.SpecialRequests, request.Channel, request.SubmissionId), await CurrentActorAsync());
        return CreatedAtAction(nameof(Details), new { id = b.Id }, new BookingStatusDto(b.Id, b.Code, b.Status.ToString()));
    }

    /// <summary>Giao đơn qua AssignAsync; quyền giao lại của Tư vấn vẫn được service kiểm tra.</summary>
    /// <response code="200">Trạng thái đơn sau giao.</response>
    /// <response code="400">Người nhận hoặc quyền giao đơn không hợp lệ.</response>
    /// <response code="404">Không có đơn.</response>
    /// <response code="409">Đơn vừa thay đổi.</response>
    [HttpPost("bookings/{id:int}/assign")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public async Task<ActionResult<BookingStatusDto>> Assign(int id, AssignRequest request)
    {
        var b = await bookings.AssignAsync(id, request.EmployeeId, await CurrentActorAsync());
        return Ok(new BookingStatusDto(b.Id, b.Code, b.Status.ToString()));
    }

    /// <summary>Đổi trạng thái qua ChangeStatusAsync, có ghi chú; không cho phép đổi trạng thái tiền hoặc hủy trực tiếp.</summary>
    /// <response code="200">Trạng thái sau thao tác.</response>
    /// <response code="400">Chuyển trạng thái hoặc ghi chú không hợp lệ.</response>
    /// <response code="404">Không có đơn.</response>
    /// <response code="409">Đơn vừa thay đổi.</response>
    [HttpPost("bookings/{id:int}/status")]
    [ProducesResponseType<BookingStatusDto>(200)]
    public async Task<ActionResult<BookingStatusDto>> Status(int id, StatusRequest request)
    {
        var b = await bookings.ChangeStatusAsync(id, request.Status, request.NewRoomId, request.Note, await CurrentActorAsync());
        return Ok(new BookingStatusDto(b.Id, b.Code, b.Status.ToString()));
    }

    /// <summary>Nhân viên nhận đơn hợp lệ từ AssigneesAsync, phân trang.</summary>
    /// <response code="200">Trang người nhận, position dạng chữ.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet("assignees")]
    [ProducesResponseType<DesktopPage<AssigneeDto>>(200)]
    public async Task<ActionResult<DesktopPage<AssigneeDto>>> Assignees([FromQuery] int? page) =>
        Ok(DesktopPage<AssigneeDto>.Slice((await bookings.AssigneesAsync(await CurrentActorAsync()))
            .Select(e => new AssigneeDto(e.EmployeeId, e.EmployeeCode, e.FullName, e.Position.ToString())), page));

    /// <summary>Lựa chọn homestay từ HomestaysAsync, phân trang. Service hiện chỉ cung cấp id/tên homestay.</summary>
    /// <response code="200">Trang lựa chọn homestay.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet("booking-homestays")]
    [ProducesResponseType<DesktopPage<BookingHomestayDto>>(200)]
    public async Task<ActionResult<DesktopPage<BookingHomestayDto>>> Homestays([FromQuery] int? page) =>
        Ok(DesktopPage<BookingHomestayDto>.Slice((await bookings.HomestaysAsync(await CurrentActorAsync()))
            .Select(h => new BookingHomestayDto(h.Id, h.Name)), page));

    /// <summary>Phòng trống theo StaffAvailabilityAsync; khoảng [checkIn, checkOut), ngày yyyy-MM-dd, phân trang.</summary>
    /// <response code="200">Trang lựa chọn còn phòng và giá.</response>
    /// <response code="400">Ngày hoặc số khách không hợp lệ.</response>
    [HttpGet("availability")]
    [ProducesResponseType<DesktopPage<AvailabilityDto>>(200)]
    public async Task<ActionResult<DesktopPage<AvailabilityDto>>> Availability([FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut, [FromQuery] int? areaId, [FromQuery] int guests = 1, [FromQuery] int? page = null) =>
        Ok(DesktopPage<AvailabilityDto>.Slice((await availability.StaffAvailabilityAsync(checkIn, checkOut, areaId, guests, await CurrentActorAsync()))
            .Select(o => new AvailabilityDto(o.HomestayId, o.HomestayName, o.AreaId, o.AreaName, o.RoomTypeId, o.RoomTypeName,
                o.CategoryId, o.MaxGuests, o.FreeRooms, o.TotalRooms, o.TotalPrice, o.Rating, o.ReviewCount)), page));
}
