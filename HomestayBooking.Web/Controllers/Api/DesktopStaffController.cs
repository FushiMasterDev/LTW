using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api;

[ApiController]
[Authorize(Policy = "DesktopConsultant")]
[Route("api/desktop")]
public sealed class DesktopStaffController(
    ApplicationDbContext db,
    StaffBookingService staffBookings,
    CustomerService customers,
    AvailabilityService availability) : ControllerBase
{
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int TotalPages, int TotalCount);
    public sealed record BookingCreate(string Phone, string FullName, string? Email, int RoomTypeId, DateOnly CheckIn,
        DateOnly CheckOut, int Guests, string? SpecialRequests, string Channel, Guid SubmissionId);
    public sealed record AssignInput(int EmployeeId);
    public sealed record StatusInput(string Status, string Note, int? NewRoomId);
    public sealed record CustomerInput(string? Note, string? FullName, string? Email, string? PhoneNumber);
    public sealed record AreaOption(int Id, string Name);
    public sealed record RoomTypeOption(int Id, string Name, int MaxGuests, int OnSaleRooms);
    public sealed record AvailabilityRow(int HomestayId, string HomestayName, int AreaId, string AreaName,
        int RoomTypeId, string RoomTypeName, int MaxGuests, int FreeRooms, int TotalRooms, decimal TotalPrice);
    public sealed record BookingRow(int Id, string Code, string GuestName, string Phone, string Homestay, string RoomType,
        string? Room, DateOnly CheckIn, DateOnly CheckOut, decimal TotalAmount, decimal DepositAmount,
        string Status, string Channel, string? Assignee, DateTime HoldExpiresAt, DateTime? ConfirmDeadlineAt);

    private Actor CurrentActor => new(ActorType.Employee, User.FindFirstValue(ClaimTypes.NameIdentifier),
        int.TryParse(User.FindFirstValue("employee_id"), out var id) ? id : null);

    [HttpGet("areas")]
    public async Task<IActionResult> Areas() => Ok(await db.Areas.AsNoTracking().OrderBy(a => a.Name).Select(a => new AreaOption(a.Id, a.Name)).ToListAsync());

    [HttpGet("booking-homestays")]
    public async Task<IActionResult> Homestays() => Ok(await staffBookings.HomestaysAsync(CurrentActor));

    [HttpGet("assignees")]
    public async Task<IActionResult> Assignees() => Ok(await staffBookings.AssigneesAsync(CurrentActor));

    [HttpGet("booking-homestays/{homestayId:int}/room-types")]
    public async Task<IActionResult> RoomTypes(int homestayId)
    {
        await StaffBookingService.EmployeeAsync(db, CurrentActor);
        var sellableHomes = availability.BookableHomestays().Select(h => h.Id);
        var rows = await db.RoomTypes.AsNoTracking().Where(t => t.HomestayId == homestayId && t.IsActive && sellableHomes.Contains(t.HomestayId))
            .OrderBy(t => t.Name).Select(t => new RoomTypeOption(t.Id, t.Name, t.MaxGuests, t.Rooms.Count(r => r.IsActive))).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut,
        [FromQuery] int? areaId, [FromQuery] int guests = 1, [FromQuery] int page = 1)
    {
        try
        {
            var nights = await availability.StaffNightAvailabilityAsync(checkIn, checkOut, areaId, guests, CurrentActor);
            var grouped = nights.GroupBy(n => new { n.HomestayId, n.HomestayName, n.AreaName, n.RoomTypeId, n.RoomTypeName })
                .Select(g => new { g.Key.HomestayId, g.Key.HomestayName, g.Key.AreaName, g.Key.RoomTypeId, g.Key.RoomTypeName,
                    FreeRooms = g.Min(n => n.FreeRooms), TotalRooms = g.Max(n => n.TotalRooms), TotalPrice = g.Sum(n => n.TotalPrice) })
                .Where(x => x.FreeRooms > 0).ToList();
            var typeIds = grouped.Select(g => g.RoomTypeId).ToArray();
            var typeInfo = await db.RoomTypes.AsNoTracking().Where(t => typeIds.Contains(t.Id))
                .Select(t => new { t.Id, t.MaxGuests, t.Homestay.AreaId }).ToDictionaryAsync(t => t.Id);
            var rows = grouped.Select(g => new AvailabilityRow(g.HomestayId, g.HomestayName, typeInfo[g.RoomTypeId].AreaId, g.AreaName,
                g.RoomTypeId, g.RoomTypeName, typeInfo[g.RoomTypeId].MaxGuests, g.FreeRooms, g.TotalRooms, g.TotalPrice))
                .OrderBy(x => x.HomestayName).ThenBy(x => x.RoomTypeName).ToList();
            var total = rows.Count; var totalPages = Math.Max(1, (int)Math.Ceiling(total / 10d)); page = Math.Clamp(page, 1, totalPages);
            return Ok(new PagedResult<AvailabilityRow>(rows.Skip((page - 1) * 10).Take(10).ToList(), page, totalPages, total));
        }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking(BookingCreate input)
    {
        if (!Enum.TryParse<SourceChannel>(input.Channel, true, out var channel) || channel == SourceChannel.Web)
            return Problem(detail: "Kênh đặt phòng không hợp lệ.", statusCode: 400);
        try
        {
            var booking = await staffBookings.CreateForCustomerAsync(new StaffBookingInput(input.Phone, input.FullName, input.Email,
                input.RoomTypeId, input.CheckIn, input.CheckOut, input.Guests, input.SpecialRequests, channel, input.SubmissionId), CurrentActor);
            return Ok(new { booking.Id, booking.Code, Status = booking.Status.ToString(), booking.HoldExpiresAt, booking.DepositAmount, AmountDue = booking.AmountDue });
        }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> SearchBookings([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int? homestayId,
        [FromQuery] DateOnly? checkInFrom, [FromQuery] DateOnly? checkInTo, [FromQuery] int? assignedEmployeeId,
        [FromQuery] bool unassigned = false, [FromQuery] int page = 1)
    {
        if (!string.IsNullOrEmpty(status) && !Enum.TryParse<BookingStatus>(status, true, out _)) return Problem(detail: "Trạng thái đơn không hợp lệ.", statusCode: 400);
        var result = await staffBookings.SearchAsync(new StaffBookingQuery { Q = q, Status = Enum.TryParse<BookingStatus>(status, true, out var parsed) ? parsed : null,
            HomestayId = homestayId, CheckInFrom = checkInFrom, CheckInTo = checkInTo, AssignedEmployeeId = assignedEmployeeId, Unassigned = unassigned, Page = page }, CurrentActor);
        var items = result.Select(b => new BookingRow(b.Id, b.Code, b.ContactName, b.ContactPhone, b.Homestay.Name, b.RoomType.Name,
            b.Room.RoomCode, b.CheckIn, b.CheckOut, b.TotalAmount, b.DepositAmount, b.Status.ToString(), b.SourceChannel.ToString(),
            b.AssignedEmployee?.Person.FullName, DateTime.SpecifyKind(b.HoldExpiresAt, DateTimeKind.Utc), b.ConfirmDeadlineAt)).ToList();
        return Ok(new PagedResult<BookingRow>(items, result.PageIndex, result.TotalPages, result.TotalCount));
    }

    [HttpGet("bookings/{id:int}")]
    public async Task<IActionResult> BookingDetails(int id)
    {
        StaffBookingDetails details;
        try { details = await staffBookings.DetailsAsync(id, CurrentActor); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 404); }
        var b = details.Booking;
        return Ok(new { booking = new { b.Id, b.Code, b.ContactName, b.ContactPhone, b.CheckIn, b.CheckOut, b.Guests, b.TotalAmount, b.DepositAmount,
            Status = b.Status.ToString(), Channel = b.SourceChannel.ToString(), Homestay = b.Homestay.Name, RoomType = b.RoomType.Name, Room = b.Room.RoomCode },
            nights = b.Nights.Select(n => new { n.Date, DayType = n.DayType.ToString(), n.Price }),
            statusHistory = details.History.Select(h => new { h.ChangedAt, FromStatus = h.FromStatus.HasValue ? h.FromStatus.Value.ToString() : null,
                ToStatus = h.ToStatus.ToString(), Actor = h.ActorType.ToString(), ActorName = h.ActorAccount?.Person.FullName, h.Note }),
            payments = details.Payments.Select(p => new { p.Type, p.Method, p.Amount, Status = p.Status.ToString(), p.SubmittedAt }),
            changeRequests = details.Changes.Select(r => new { r.Id, Type = r.RequestType.ToString(), Status = r.Status.ToString(), r.CreatedAt, r.NewCheckIn, r.NewCheckOut, r.ChangeFee, r.AdditionalDeposit, r.DecisionNote, r.RejectionReason }),
            auditLogs = details.Audit.Select(a => new { a.CreatedAt, Action = a.Action.ToString(), a.OldValues, a.NewValues }) });
    }

    [HttpPost("bookings/{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignInput input)
    {
        try { var b = await staffBookings.AssignAsync(id, input.EmployeeId, CurrentActor); return Ok(new { b.Id, b.Code, AssignedEmployeeId = b.AssignedEmployeeId }); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpPost("bookings/{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, StatusInput input)
    {
        if (!Enum.TryParse<BookingStatus>(input.Status, true, out var status)) return Problem(detail: "Trạng thái đơn không hợp lệ.", statusCode: 400);
        try { var b = await staffBookings.ChangeStatusAsync(id, status, input.NewRoomId, input.Note, CurrentActor); return Ok(new { b.Id, b.Code, Status = b.Status.ToString() }); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpGet("customers")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string? q, [FromQuery] int page = 1)
    {
        var result = await customers.SearchAsync(q, page, CurrentActor);
        return Ok(new PagedResult<object>(result.Select(c => (object)new { c.Id, c.FullName, c.PhoneNumber, c.Email, c.CitizenId, c.StudentId, c.HasAccount }).ToList(), result.PageIndex, result.TotalPages, result.TotalCount));
    }

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> Customer(int id)
    {
        CustomerDetails result;
        try { result = await customers.DetailsAsync(id, CurrentActor); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 404); }
        var bookings = await db.Bookings.AsNoTracking().Where(b => b.CustomerId == id).OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Code, Homestay = b.Homestay.Name, RoomType = b.RoomType.Name, b.CheckIn, b.CheckOut,
                b.TotalAmount, Status = b.Status.ToString(), b.CreatedAt }).ToListAsync();
        return Ok(new { customer = result.Customer, bookings, vouchers = result.UsableVouchers.Select(v => new { v.Code, v.Amount, v.Balance, v.ExpiresOn, Status = v.Status.ToString(), v.Reason }) });
    }

    [HttpPut("customers/{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, CustomerInput input)
    {
        try { return Ok(await customers.UpdateAsync(id, new CustomerUpdate(input.Note, input.FullName, input.Email, input.PhoneNumber), CurrentActor)); }
        catch (BookingException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }
}
