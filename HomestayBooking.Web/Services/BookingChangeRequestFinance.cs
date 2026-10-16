using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public partial class BookingChangeRequestService
{
    private async Task HoldNightsAsync(Booking booking, BookingChangeRequest request, Target target, int room)
    {
        var nights = await _db.RoomNights.Where(n => n.RoomId == room && n.Date >= target.CheckIn && n.Date < target.CheckOut).ToListAsync();
        for (var date = target.CheckIn; date < target.CheckOut; date = date.AddDays(1))
        {
            var night = nights.SingleOrDefault(n => n.Date == date);
            if (night is null) _db.RoomNights.Add(new RoomNight { RoomId = room, Date = date, BookingChangeRequestId = request.Id });
            else if (night.BookingId != booking.Id && night.BookingChangeRequestId != request.Id)
                throw new BookingException(NoRooms);
        }
    }

    /// <summary>Dùng chung khi Manager duyệt không thu thêm và khi Accountant xác nhận tiền.</summary>
    private async Task ApplyChangeAsync(Booking booking, BookingChangeRequest request, Target target, int room, Actor actor, string note)
    {
        await HoldNightsAsync(booking, request, target, room);
        // Bao gồm cả đêm vừa thêm trong cùng context, chưa có trong kết quả truy vấn SQL.
        await _db.RoomNights.Where(n => n.RoomId == room && n.Date >= target.CheckIn && n.Date < target.CheckOut).LoadAsync();
        foreach (var night in _db.RoomNights.Local.Where(n => n.RoomId == room && n.Date >= target.CheckIn && n.Date < target.CheckOut))
        {
            night.BookingChangeRequestId = null; night.BookingId = booking.Id;
        }
        var oldNights = await _db.BookingNights.Where(n => n.BookingId == booking.Id).ToListAsync();
        foreach (var n in oldNights.Where(n => n.Date < target.CheckIn || n.Date >= target.CheckOut)) _db.BookingNights.Remove(n);
        foreach (var n in target.Price.Nights)
        {
            var existing = oldNights.SingleOrDefault(x => x.Date == n.Date);
            if (existing is null) _db.BookingNights.Add(new BookingNight { BookingId = booking.Id, Date = n.Date, Price = n.Price, DayType = n.DayType });
            else { existing.Price = n.Price; existing.DayType = n.DayType; }
        }
        _db.RoomNights.RemoveRange(await _db.RoomNights.Where(n => n.BookingId == booking.Id
            && (n.RoomId != room || n.Date < target.CheckIn || n.Date >= target.CheckOut)).ToListAsync());
        booking.CheckIn = target.CheckIn; booking.CheckOut = target.CheckOut;
        booking.RoomTypeId = target.RoomTypeId; booking.RoomId = room; booking.TotalAmount = target.Price.Total;
        booking.DepositAmount = request.DepositCarriedOver!.Value + request.AdditionalDeposit!.Value;
        booking.ChangeFeeAmount += request.ChangeFee!.Value;
        booking.CancellationPolicyId = (await _refunds.SelectPolicyAsync(booking.HomestayId, target.Price.HasHoliday)).Id;
        _bookings.ApplyChangeRequestReconfirmation(booking, actor, note);
        request.Status = ChangeRequestStatus.Applied;
    }

    public async Task<string> GetPaymentQrAsync(int id, ClaimsPrincipal principal, VietQrService qr)
    {
        var request = await _db.BookingChangeRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new BookingException("Không tìm thấy yêu cầu.");
        var booking = await BookingAsync(request.BookingId);
        await CustomerForPaymentAsync(booking, principal);
        if (request.Status != ChangeRequestStatus.AwaitingPayment || request.PaymentDeadlineAt < Now)
            throw new BookingException(Processed);
        return qr.BuildImageUrl(request.AdditionalDeposit!.Value, $"CR{request.Id} {booking.Code}");
    }

    private async Task CustomerForPaymentAsync(Booking booking, ClaimsPrincipal principal)
    {
        var actor = await ActorAsync(principal, booking);
        if (actor.Type != ActorType.Customer) throw new BookingException("Chỉ khách sở hữu đơn được nộp bill.");
    }

    private async Task<(BookingChangeRequest Request, Booking Booking, Actor Actor)> FinanceAsync(int id, ClaimsPrincipal principal)
    {
        var account = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var employee = principal.Identity?.IsAuthenticated == true && principal.IsInRole(AppRoles.Employee)
            ? await _db.Employees.AsNoTracking().Where(e => e.Position == EmployeePosition.Accountant
                && e.EmploymentStatus == EmploymentStatus.Active && e.Person.Account != null
                && e.Person.Account.Id == account && e.Person.Account.Status == AccountStatus.Active)
                .Select(e => new { e.Id }).SingleOrDefaultAsync() : null;
        if (employee is null) throw new BookingException("Chỉ kế toán đang làm việc được xác nhận cọc bổ sung.");
        var request = await _db.BookingChangeRequests.SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new BookingException("Không tìm thấy yêu cầu.");
        if (account == request.RequestedByAccountId || account == request.DecidedByAccountId)
            throw new BookingException("Người gửi hoặc người duyệt nghiệp vụ không được xác nhận tiền.");
        var query = _db.BookingChangeRequests.Where(r => r.Id == id && r.Status == ChangeRequestStatus.AwaitingPayment);
        if (request.Status != ChangeRequestStatus.AwaitingPayment || (_db.Database.IsRelational()
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, r => r.Status)) != 1
            : !await query.AnyAsync())) throw new BookingException(Processed);
        return (request, await BookingAsync(request.BookingId), new Actor(ActorType.Employee, account, employee.Id));
    }

    public Task<BookingChangeRequest> ConfirmAsync(int id, string note, ClaimsPrincipal principal) => WriteAsync(async () =>
    {
        note = Note(note);
        var (request, booking, actor) = await FinanceAsync(id, principal);
        var payment = await ChangeRequestPayments.Valid(_db).Include(p => p.BankTransaction)
            .SingleOrDefaultAsync(p => p.ChangeRequestId == id && p.Status == PaymentStatus.Submitted)
            ?? throw new BookingException("Chưa có khoản cọc bổ sung hợp lệ nộp đúng hạn.");
        if (IsLegacy(request)) throw new BookingException(LegacyRequest);
        // Hạn báo trước đã kiểm khi duyệt nghiệp vụ; không biến thành hạn kế toán duyệt.
        await ValidateBookingAsync(booking, false, checkNotice: false);
        var target = await TargetAsync(booking, new(booking.Id, request.RequestType, request.NewCheckIn, request.NewCheckOut, request.NewRoomTypeId), allowPendingReview: true)
            ?? throw new BookingException("Yêu cầu hủy không có cọc bổ sung.");
        if (target.Price.Total != request.NewTotalAmount)
        {
            await RejectPaidAsync(request, booking, actor, PriceChanged);
            return request;
        }
        var required = PricingService.RoundToThousand(booking.DepositRate * target.Price.Total);
        ValidateMoney(target.Price.Total, request.DepositCarriedOver!.Value, required);
        if (request.ChangeFee + request.DepositCarriedOver != booking.AmountDue
            || request.AdditionalDeposit != Math.Max(0, required - request.DepositCarriedOver.Value))
            throw new BookingException("Tiền cọc của đơn đã thay đổi, vui lòng gửi lại yêu cầu.");
        var room = request.NewRoomId ?? throw new BookingException(NoRooms);
        if (!await _db.Rooms.AnyAsync(r => r.Id == room && r.RoomTypeId == target.RoomTypeId && r.IsActive && r.RoomType.IsActive))
            throw new BookingException(NoRooms);
        // Không tự chiếm lại phòng nếu phần giữ của yêu cầu đã bị mất.
        for (var date = target.CheckIn; date < target.CheckOut; date = date.AddDays(1))
            if (!await _db.RoomNights.AnyAsync(n => n.RoomId == room && n.Date == date
                && (n.BookingId == booking.Id || n.BookingChangeRequestId == id))) throw new BookingException(NoRooms);
        var old = JsonSerializer.Serialize(RequestState(request));
        var oldBooking = JsonSerializer.Serialize(BookingState(booking));
        _db.Entry(booking).Property(b => b.Status).IsModified = true;
        payment.Status = PaymentStatus.Approved; payment.ReviewedByEmployeeId = actor.EmployeeId; payment.ReviewedAt = Now;
        request.FinanceConfirmedByAccountId = actor.AccountId; request.FinanceConfirmedAt = Now; request.FinanceNote = note;
        await ApplyChangeAsync(booking, request, target, room, actor, note);
        Audit(id, actor, old, JsonSerializer.Serialize(new { Step = "FinanceConfirmed", PaymentId = payment.Id, Request = RequestState(request) }));
        _db.AuditLogs.Add(new AuditLog { AccountId = actor.AccountId, TableName = "Bookings", RecordId = booking.Id.ToString(),
            Action = AuditAction.Update, OldValues = oldBooking, NewValues = JsonSerializer.Serialize(BookingState(booking)), CreatedAt = Now });
        Notify(booking, "đã được kế toán xác nhận và áp dụng");
        return request;
    });

    public Task<BookingChangeRequest> RejectFinanceAsync(int id, string reason, ClaimsPrincipal principal) => WriteAsync(async () =>
    {
        reason = Note(reason);
        var (request, booking, actor) = await FinanceAsync(id, principal);
        await RejectPaidAsync(request, booking, actor, reason);
        return request;
    });

    private async Task RejectPaidAsync(BookingChangeRequest request, Booking booking, Actor actor, string reason)
    {
        var old = JsonSerializer.Serialize(RequestState(request));
        var payment = await _db.Payments.Include(p => p.BankTransaction).SingleOrDefaultAsync(p => p.ChangeRequestId == request.Id
            && p.Type == PaymentType.AdditionalDeposit && p.Status != PaymentStatus.Rejected);
        if (payment is not null)
        {
            payment.Status = PaymentStatus.Rejected; payment.ReviewedByEmployeeId = actor.EmployeeId;
            payment.ReviewedAt = Now; payment.RejectReason = reason;
            if (payment.BankTransaction is { } tx)
            {
                tx.MatchStatus = BankMatchStatus.Unmatched;
                // Ưu tiên mã yêu cầu và lý do, giới hạn đúng độ dài cột Note.
                var note = $"CR{request.Id}: {reason}. {tx.Note}";
                tx.Note = note[..Math.Min(500, note.Length)];
            }
        }
        request.Status = ChangeRequestStatus.Rejected;
        request.RejectionReason = reason;
        _db.RoomNights.RemoveRange(await _db.RoomNights.Where(n => n.BookingChangeRequestId == request.Id).ToListAsync());
        Audit(request.Id, actor, old, JsonSerializer.Serialize(new { Step = "FinanceRejected", PaymentId = payment?.Id, Request = RequestState(request) }));
        Notify(booking, "bị kế toán từ chối: " + reason);
    }

    /// <summary>Mỗi yêu cầu được chốt bằng một UPDATE có điều kiện, cùng transaction nhả phòng và ghi log.</summary>
    public async Task<int> ExpireUnpaidAsync()
    {
        var now = Now;
        var ids = await _db.BookingChangeRequests.AsNoTracking().Where(r => r.Status == ChangeRequestStatus.AwaitingPayment
            && r.PaymentDeadlineAt < now).Select(r => r.Id).ToListAsync();
        var count = 0;
        foreach (var id in ids)
            count += await WriteAsync(async () =>
            {
                var old = await _db.BookingChangeRequests.AsNoTracking().SingleAsync(r => r.Id == id);
                var query = _db.BookingChangeRequests.Where(r => r.Id == id && r.Status == ChangeRequestStatus.AwaitingPayment
                    && r.PaymentDeadlineAt < now && !ChangeRequestPayments.Valid(_db).Any(p => p.ChangeRequestId == r.Id));
                if (_db.Database.IsRelational())
                {
                    if (await query.ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ChangeRequestStatus.Expired)) == 0) return 0;
                    // ExecuteUpdate không cập nhật tracker; nạp lại để không ghi đè trạng thái mới.
                    var tracked = _db.ChangeTracker.Entries<BookingChangeRequest>().SingleOrDefault(e => e.Entity.Id == id);
                    if (tracked is not null) await tracked.ReloadAsync();
                }
                else
                {
                    var request = await query.SingleOrDefaultAsync();
                    if (request is null) return 0;
                    request.Status = ChangeRequestStatus.Expired;
                }
                _db.RoomNights.RemoveRange(await _db.RoomNights.Where(n => n.BookingChangeRequestId == id).ToListAsync());
                Audit(id, Actor.System, JsonSerializer.Serialize(RequestState(old)),
                    JsonSerializer.Serialize(new { Step = "Expired", Actor = "System", Status = "Expired", ExpiredAt = now }));
                var booking = await BookingAsync(old.BookingId);
                _notifications.Add([booking.Customer.Person.Account?.Id], $"Yêu cầu CR{id} của đơn {booking.Code} hết hạn nộp cọc bổ sung.");
                return 1;
            });
        return count;
    }
}
