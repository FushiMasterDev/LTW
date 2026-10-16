using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record OverbookingAlternative(AvailabilityOffer Offer, decimal Difference);
public record OverbookingRefund(Booking Booking, Voucher ApologyVoucher);

/// <summary>BR-11/Q3: Fuji xử lý sự cố sau xác nhận; không thu thêm cọc, không trừ bù vào cơ sở cũ.</summary>
public class OverbookingService(ApplicationDbContext db, BookingService bookings, AvailabilityService availability,
    VoucherService vouchers, NotificationService notifications, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static object State(Booking b) => new { b.Id, b.Code, Status = b.Status.ToString(), b.RoomId, b.RoomTypeId, b.HomestayId,
        b.CustomerId, b.CheckIn, b.CheckOut, b.TotalAmount, b.DepositRate, b.DepositAmount, b.DiscountAmount, b.VoucherId,
        b.RefundAmount, b.ReplacedByBookingId, b.CreatedByEmployeeId, b.AssignedEmployeeId, b.CommissionRate, b.CancellationPolicyId };

    private async Task<Booking> ConfirmedAsync(int id, Actor actor, bool ownerAllowed = false)
    {
        if (actor.Type == ActorType.Employee) await StaffBookingService.EmployeeAsync(db, actor);
        else if (!ownerAllowed || actor.Type != ActorType.Owner || actor.AccountId is null)
            throw new BookingException("Bạn không có quyền xử lý trùng phòng.");
        var booking = await db.Bookings.Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
            .Include(b => b.AssignedEmployee).ThenInclude(e => e!.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(b => b.Id == id) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        if (actor.Type == ActorType.Owner && !await db.Owners.AnyAsync(o => o.Id == booking.Homestay.OwnerId
            && o.Person.Account != null && o.Person.Account.Id == actor.AccountId && o.Person.Account.Status == AccountStatus.Active && o.Person.Employee == null))
            throw new BookingException("Chỉ chủ đúng homestay được báo trùng phòng.");
        if (booking.Status != BookingStatus.Confirmed || VietnamTime.Today(clock) >= booking.CheckOut)
            throw new BookingException("Chỉ xử lý trùng phòng cho đơn đã xác nhận, chưa đến ngày trả phòng.");
        if (await db.BookingChangeRequests.AnyAsync(r => r.BookingId == id && (r.Status == ChangeRequestStatus.Pending || r.Status == ChangeRequestStatus.AwaitingPayment)))
            throw new BookingException("Đơn đang có yêu cầu đổi/hủy, vui lòng xử lý yêu cầu đó trước.");
        return booking;
    }

    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        try { return await HomestayWrite.RunAsync(db, action); }
        catch (DbUpdateConcurrencyException) { throw new BookingException("Đơn đã được xử lý, vui lòng tải lại."); }
        catch (DbUpdateException ex) when (ex.GetBaseException().Message.Contains("RoomNights", StringComparison.Ordinal))
        { throw new BookingException("Phòng được chọn vừa có người đặt, vui lòng chọn phòng khác."); }
    }

    public Task<Booking> ReportAsync(int id, string note, Actor actor) => WriteAsync(async () =>
    {
        note = StaffBookingService.RequiredNote(note); var booking = await ConfirmedAsync(id, actor, ownerAllowed: true);
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", id.ToString(), State(booking), new { Step = "OverbookingReported", Note = note, Booking = State(booking) });
        var managers = await PartnerServiceSupport.Managers(db).ToListAsync();
        PartnerServiceSupport.Notify(db, notifications, clock, managers.Append(booking.AssignedEmployee?.Person.Account?.Id),
            $"Đơn {booking.Code} được báo trùng phòng, cần liên hệ khách và tìm phương án thay thế.", $"/Staff/Bookings/{id}");
        return booking;
    });

    public async Task<List<OverbookingAlternative>> AlternativesAsync(int id, Actor actor)
    {
        var booking = await ConfirmedAsync(id, actor);
        return (await availability.AlternativesAsync(booking)).Select(o => new OverbookingAlternative(o, o.TotalPrice!.Value - booking.TotalAmount)).ToList();
    }

    public Task<Booking> MoveRoomAsync(int id, int roomId, string note, Actor actor) => WriteAsync(async () =>
    {
        note = StaffBookingService.RequiredNote(note); var booking = await ConfirmedAsync(id, actor); var old = State(booking);
        var move = await bookings.MoveRoomAsync(booking, roomId);
        var historyNote = "Đổi phòng do trùng phòng; " + move + "; " + note;
        if (historyNote.Length > 500) throw new BookingException("Ghi chú kèm thông tin đổi phòng không được quá 500 ký tự.");
        db.Entry(booking).Property(b => b.Status).IsModified = true;
        db.BookingStatusHistories.Add(new() { BookingId = id, FromStatus = booking.Status, ToStatus = booking.Status,
            ActorType = actor.Type, ActorAccountId = actor.AccountId, Note = historyNote, ChangedAt = Now });
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", id.ToString(), old, new { Step = "OverbookingRoomMoved", Note = note, Booking = State(booking) });
        PartnerServiceSupport.Notify(db, notifications, clock, [booking.Customer.Person.Account?.Id, booking.Homestay.Owner.Person.Account?.Id],
            $"Đơn {booking.Code} đã được chuyển sang phòng khác cùng loại do trùng phòng; giá và tiền cọc giữ nguyên.", $"/Bookings/Details/{id}");
        return booking;
    });

    public Task<Booking> ReplaceAsync(int id, int roomTypeId, string note, Actor actor) => WriteAsync(async () =>
    {
        note = StaffBookingService.RequiredNote(note); var oldBooking = await ConfirmedAsync(id, actor); var old = State(oldBooking);
        if (roomTypeId == oldBooking.RoomTypeId) throw new BookingException("Cùng loại phòng và homestay, vui lòng dùng chuyển phòng tại chỗ.");
        var request = new CreateBookingRequest(oldBooking.CustomerId, roomTypeId, oldBooking.CheckIn, oldBooking.CheckOut, oldBooking.Guests,
            oldBooking.ContactName, oldBooking.ContactPhone, oldBooking.SpecialRequests, oldBooking.SourceChannel, actor);
        var replacement = await bookings.CreateInTransactionAsync(request, n =>
        {
            if (n.TotalAmount < oldBooking.DepositAmount) throw new BookingException("Tổng đơn thay thế nhỏ hơn cọc cũ; vui lòng chọn phương án khác.");
            n.DepositRate = oldBooking.DepositRate; n.DepositAmount = oldBooking.DepositAmount;
            n.VoucherId = oldBooking.VoucherId; n.DiscountAmount = oldBooking.DiscountAmount;
            n.CreatedByEmployeeId = oldBooking.CreatedByEmployeeId; n.AssignedEmployeeId = actor.EmployeeId;
        }, replacement: true);
        await bookings.ApplyStatusAsync(replacement, BookingStatus.Deposited, actor, $"Nhận cọc chuyển từ đơn {oldBooking.Code} do trùng phòng.");
        if (("Trùng phòng: " + note).Length > 500) throw new BookingException("Ghi chú kèm lý do trùng phòng không được quá 500 ký tự.");
        await bookings.ApplyFujiCancellationAsync(oldBooking, actor, "Trùng phòng: " + note, transferDeposit: true);
        oldBooking.ReplacedByBookingId = replacement.Id;
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", oldBooking.Id.ToString(), old,
            new { Step = "OverbookingReplaced", Note = note, Booking = State(oldBooking) });
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", replacement.Id.ToString(), null,
            new { Step = "OverbookingReplacementCreated", SourceBookingId = id, Booking = State(replacement) }, AuditAction.Insert);
        if (replacement.TotalAmount > oldBooking.TotalAmount)
        {
            var compensation = new Payment { BookingId = replacement.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
                Amount = replacement.TotalAmount - oldBooking.TotalAmount, Status = PaymentStatus.Submitted,
                SubmittedByAccountId = actor.AccountId, SubmittedAt = Now, TransferContent = $"Fuji bù trùng phòng {oldBooking.Code} → {replacement.Code}" };
            db.Payments.Add(compensation); await db.SaveChangesAsync();
            PartnerServiceSupport.Audit(db, clock, actor, "Payments", compensation.Id.ToString(), null,
                new { compensation.BookingId, Type = compensation.Type.ToString(), compensation.Amount, Status = compensation.Status.ToString() }, AuditAction.Insert);
        }
        PartnerServiceSupport.Notify(db, notifications, clock, [oldBooking.Customer.Person.Account?.Id],
            $"Đơn {oldBooking.Code} được thay bằng {replacement.Code}; cọc đã chuyển sang, không cần nộp thêm. Đang chờ homestay xác nhận.", $"/Bookings/Details/{replacement.Id}");
        return replacement;
    });

    public Task<OverbookingRefund> RefundAsync(int id, string note, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        note = StaffBookingService.RequiredNote(note); var booking = await ConfirmedAsync(id, actor); var old = State(booking);
        if (("Trùng phòng, không tìm được thay thế: " + note).Length > 500) throw new BookingException("Ghi chú kèm lý do trùng phòng không được quá 500 ký tự.");
        await bookings.ApplyFujiCancellationAsync(booking, actor, "Trùng phòng, không tìm được thay thế: " + note);
        var voucher = await vouchers.IssueAsync(booking.Code, actor.EmployeeId!.Value, note);
        PartnerServiceSupport.Audit(db, clock, actor, "Bookings", id.ToString(), old, new { Step = "OverbookingRefunded", Note = note, Booking = State(booking) });
        PartnerServiceSupport.Audit(db, clock, actor, "Vouchers", voucher.Id.ToString(), null,
            new { voucher.Code, voucher.CustomerId, voucher.SourceBookingId, voucher.Amount, voucher.Balance, voucher.ExpiresOn }, AuditAction.Insert);
        PartnerServiceSupport.Notify(db, notifications, clock, [booking.Customer.Person.Account?.Id],
            $"Fuji xin lỗi vì sự cố đơn {booking.Code}. Tiền cọc được hoàn 100%, khoản hoàn đang chờ kế toán chuyển trả.", $"/Bookings/Details/{id}");
        return new OverbookingRefund(booking, voucher);
    });
}
