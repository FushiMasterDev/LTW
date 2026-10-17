using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public class StaffBookingQuery
{
    public string? Q { get; set; }
    public BookingStatus? Status { get; set; }
    public int? HomestayId { get; set; }
    public DateOnly? CheckInFrom { get; set; }
    public DateOnly? CheckInTo { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public bool Unassigned { get; set; }
    public int? Page { get; set; }
}

public record StaffBookingInput(string Phone, string FullName, string? Email, int RoomTypeId, DateOnly CheckIn, DateOnly CheckOut,
    int Guests, string? SpecialRequests, SourceChannel Channel);

public record StaffBookingDetails(Booking Booking, IReadOnlyList<BookingStatusHistory> History,
    IReadOnlyList<BookingChangeRequest> Changes, IReadOnlyList<Payment> Payments, IReadOnlyList<AuditLog> Audit);

/// <summary>FR-09: nhân viên thao tác qua luật đặt phòng chung, không có đường duyệt tiền hoặc hủy trực tiếp.</summary>
public class StaffBookingService(ApplicationDbContext db, BookingService bookings, NotificationService notifications, TimeProvider clock,
    GuestCustomerService guests)
{
    internal static Task<Employee> EmployeeAsync(ApplicationDbContext db, Actor actor) =>
        PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);

    internal static string RequiredNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note) || note.Trim().Length > 500)
            throw new BookingException("Vui lòng nhập ghi chú từ 1 đến 500 ký tự.");
        return note.Trim();
    }

    /// <summary>FR-07: đặt hộ khách gọi điện/Zalo, kể cả khách chưa có hồ sơ; người tạo và phụ trách theo D23 như CreateAsync.</summary>
    public async Task<Booking> CreateForCustomerAsync(StaffBookingInput input, Actor actor)
    {
        await EmployeeAsync(db, actor);
        if (input.Channel == SourceChannel.Web || !Enum.IsDefined(input.Channel))
            throw new BookingException("Đơn nhân viên tạo phải ghi kênh khách liên hệ (điện thoại, Zalo, Messenger hoặc khác).");
        var name = input.FullName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100) throw new BookingException("Vui lòng nhập họ tên khách từ 1 đến 100 ký tự.");
        var customer = await guests.FindOrCreateAsync(input.Phone, name, input.Email, actor);
        return await bookings.CreateAsync(new CreateBookingRequest(customer.Id, input.RoomTypeId, input.CheckIn, input.CheckOut,
            input.Guests, name, PhoneNumberNormalizer.ToE164(input.Phone)!, input.SpecialRequests, input.Channel, actor));
    }

    public async Task<PaginatedList<Booking>> SearchAsync(StaffBookingQuery query, Actor actor)
    {
        await EmployeeAsync(db, actor);
        var rows = db.Bookings.AsNoTracking().Include(b => b.Customer).ThenInclude(c => c.Person).AsQueryable();
        // shortcut: tìm đa trường bằng Contains, chấp nhận quét theo quy mô BTL; chưa thêm index.
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim(); var code = term.ToUpperInvariant(); var phone = PhoneNumberNormalizer.ToE164(term);
            rows = rows.Where(b => b.Code.Contains(code) || b.ContactName.Contains(term)
                || (b.Customer.Person.FullName != null && b.Customer.Person.FullName.Contains(term))
                || (phone != null && (b.ContactPhone == phone || b.Customer.Person.PhoneNumber == phone)));
        }
        if (query.Status is { } status) rows = rows.Where(b => b.Status == status);
        if (query.HomestayId is int home) rows = rows.Where(b => b.HomestayId == home);
        if (query.CheckInFrom is { } from) rows = rows.Where(b => b.CheckIn >= from);
        if (query.CheckInTo is { } to) rows = rows.Where(b => b.CheckIn <= to);
        if (query.Unassigned) rows = rows.Where(b => b.AssignedEmployeeId == null);
        else if (query.AssignedEmployeeId is int assigned) rows = rows.Where(b => b.AssignedEmployeeId == assigned);
        return await PaginatedList<Booking>.CreateAsync(rows.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id), query.Page);
    }

    public async Task<StaffBookingDetails> DetailsAsync(int id, Actor actor)
    {
        await EmployeeAsync(db, actor);
        var booking = await db.Bookings.AsNoTracking().Include(b => b.Customer).ThenInclude(c => c.Person)
            .Include(b => b.Homestay).Include(b => b.RoomType).Include(b => b.Room).Include(b => b.Nights).AsSplitQuery().SingleOrDefaultAsync(b => b.Id == id)
            ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        return new(booking,
            await db.BookingStatusHistories.AsNoTracking().Where(h => h.BookingId == id).OrderBy(h => h.ChangedAt).ThenBy(h => h.Id).ToListAsync(),
            await db.BookingChangeRequests.AsNoTracking().Where(r => r.BookingId == id).OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).ToListAsync(),
            await db.Payments.AsNoTracking().Where(p => p.BookingId == id).OrderBy(p => p.SubmittedAt).ThenBy(p => p.Id).ToListAsync(),
            await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "Bookings" && a.RecordId == id.ToString()).OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).ToListAsync());
    }

    public Task<Booking> AssignAsync(int id, int employeeId, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var handler = await EmployeeAsync(db, actor);
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == id) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        if (handler.Position == EmployeePosition.Consultant && booking.AssignedEmployeeId is int previous && previous != handler.Id)
            throw new BookingException("Tư vấn chỉ nhận đơn chưa có người phụ trách hoặc giao lại đơn mình phụ trách.");
        var target = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account).SingleOrDefaultAsync(e => e.Id == employeeId);
        if (target is null || target.EmploymentStatus != EmploymentStatus.Active || target.Person.Account?.Status != AccountStatus.Active
            || target.Position is not (EmployeePosition.Consultant or EmployeePosition.Manager))
            throw new BookingException("Người nhận phải là Tư vấn hoặc Quản lý đang làm việc, có tài khoản hoạt động.");
        if (handler.Position == EmployeePosition.Consultant && booking.AssignedEmployeeId is null && target.Id != handler.Id)
            throw new BookingException("Tư vấn chỉ được tự nhận đơn chưa có người phụ trách.");
        var old = new { booking.AssignedEmployeeId, booking.CreatedByEmployeeId };
        // Giữ điều kiện người phụ trách cũ ngay trong UPDATE để hai Tư vấn không cùng nhận một đơn.
        if (db.Database.IsRelational() && await db.Bookings.Where(b => b.Id == id && b.AssignedEmployeeId == old.AssignedEmployeeId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.AssignedEmployeeId, employeeId)) != 1)
            throw new BookingException("Đơn vừa được giao cho người khác, vui lòng tải lại.");
        booking.AssignedEmployeeId = target.Id; booking.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", id.ToString(), old, new { booking.AssignedEmployeeId, booking.CreatedByEmployeeId });
        PartnerServiceSupport.Notify(db, notifications, clock, [target.Person.Account!.Id], $"Bạn được giao phụ trách đơn {booking.Code}.", $"/Staff/Bookings/{id}");
        return booking;
    });

    public Task<Booking> ChangeStatusAsync(int id, BookingStatus to, int? newRoomId, string note, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await EmployeeAsync(db, actor); note = RequiredNote(note);
        if (to is not (BookingStatus.Confirmed or BookingStatus.Rejected or BookingStatus.CheckedIn or BookingStatus.Completed or BookingStatus.NoShow))
            throw new BookingException("Nhân viên không được đổi trạng thái tiền hoặc hủy trực tiếp; vui lòng dùng yêu cầu đổi/hủy.");
        if (newRoomId is not null && to != BookingStatus.Confirmed) throw new BookingException("Chỉ chọn lại phòng khi xác nhận đơn.");
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == id) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        var old = new { Status = booking.Status.ToString(), booking.RoomId, booking.RefundAmount };
        if (to == BookingStatus.Confirmed) await bookings.ConfirmAsync(booking, newRoomId, actor, note);
        else await bookings.ChangeStatusAsync(booking, to, actor, note);
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", id.ToString(), old,
            new { Status = booking.Status.ToString(), booking.RoomId, booking.RefundAmount, Note = note, OnBehalfOfOwner = true });
        return booking;
    });
}
