using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

// Form chỉ chứa nội dung đổi; người gửi/người duyệt luôn lấy từ principal và kiểm tra lại hồ sơ.
public record ChangeBookingRequest(int BookingId, ChangeRequestType RequestType,
    DateOnly? NewCheckIn = null, DateOnly? NewCheckOut = null, int? NewRoomTypeId = null);

public record BookingChangeQuote(decimal NewTotalAmount, decimal ChangeFee, decimal DepositCarriedOver,
    decimal RequiredDeposit, decimal AdditionalDeposit, decimal? RefundAmount, int? RefundPercent);

/// <summary>Gửi yêu cầu, duyệt nghiệp vụ và xác nhận cọc bổ sung để đổi/hủy trên cùng đơn.</summary>
public partial class BookingChangeRequestService
{
    public const string Processed = "Yêu cầu đã được xử lý";
    public const string PriceChanged = "giá đã thay đổi, vui lòng gửi lại yêu cầu";
    public const string VoucherBlocked = "Đơn dùng voucher, vui lòng liên hệ nhân viên";
    public const string NoRooms = "Loại phòng này đã hết phòng trong thời gian bạn chọn.";
    public const string OpenRequest = "Đơn đã có một yêu cầu đang chờ xử lý.";
    public const string LegacyRequest = "Yêu cầu cũ, cần gửi lại; quản lý chỉ được từ chối.";

    private readonly ApplicationDbContext _db;
    private readonly PricingService _pricing;
    private readonly RefundService _refunds;
    private readonly BookingService _bookings;
    private readonly NotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly BookingOptions _options;
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public BookingChangeRequestService(ApplicationDbContext db, PricingService pricing, RefundService refunds,
        BookingService bookings, NotificationService notifications, TimeProvider clock, IOptions<BookingOptions> options)
    {
        _db = db; _pricing = pricing; _refunds = refunds; _bookings = bookings;
        _notifications = notifications; _clock = clock; _options = options.Value;
    }

    private IQueryable<Booking> BookingQuery => _db.Bookings
        .Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
        .Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account);

    private async Task<Booking> BookingAsync(int id) => await BookingQuery.SingleOrDefaultAsync(b => b.Id == id)
        ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");

    private async Task<Actor> ActorAsync(ClaimsPrincipal principal, Booking booking, bool manager = false)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || id is null)
            throw new BookingException("Vui lòng đăng nhập.");
        var user = await _db.Users.AsNoTracking().Include(u => u.Person).ThenInclude(p => p.Employee)
            .SingleOrDefaultAsync(u => u.Id == id);
        if (user?.Status != AccountStatus.Active) throw new BookingException("Tài khoản không hoạt động.");
        var employee = user.Person.Employee;
        if (principal.IsInRole(AppRoles.Employee) && employee?.EmploymentStatus == EmploymentStatus.Active)
        {
            if (manager && employee.Position != EmployeePosition.Manager)
                throw new BookingException("Chỉ quản lý được duyệt yêu cầu.");
            return new Actor(ActorType.Employee, id, employee.Id);
        }
        if (!manager && principal.IsInRole(AppRoles.Customer) && booking.Customer.PersonId == user.PersonId)
            return new Actor(ActorType.Customer, id);
        throw new BookingException(manager ? "Chỉ quản lý được duyệt yêu cầu." : "Bạn không có quyền gửi yêu cầu cho đơn này.");
    }

    private async Task ValidateBookingAsync(Booking b, bool cancel, bool checkNotice = true)
    {
        if (b.Status is not (BookingStatus.Deposited or BookingStatus.Confirmed) || b.CheckedInAt is not null)
            throw new BookingException("Chỉ đổi/hủy đơn đã cọc hoặc xác nhận, chưa nhận phòng.");
        if (await _db.SettlementItems.AnyAsync(i => i.BookingId == b.Id))
            throw new BookingException("Đơn đã có đối soát, vui lòng liên hệ nhân viên.");
        if (b.RefundAmount is not null || await _db.Payments.AnyAsync(p => p.BookingId == b.Id && p.Type == PaymentType.Refund))
            throw new BookingException("Đơn đã có khoản hoàn tiền, vui lòng liên hệ nhân viên.");
        if (cancel) return;
        if (b.DiscountAmount > 0) throw new BookingException(VoucherBlocked);
        if (checkNotice && b.CheckIn.DayNumber - VietnamTime.Today(_clock).DayNumber < _options.ChangeMinDaysBefore)
            throw new BookingException("Chỉ đổi trước ngày nhận phòng ít nhất {0} ngày.", _options.ChangeMinDaysBefore);
        if (await _db.BookingChangeRequests.CountAsync(r => r.BookingId == b.Id
            && r.Status == ChangeRequestStatus.Applied && r.RequestType != ChangeRequestType.Cancel) >= _options.MaxChangesPerBooking)
            throw new BookingException("Đơn đã đạt số lần đổi tối đa.");
    }

    private sealed record Target(DateOnly CheckIn, DateOnly CheckOut, int RoomTypeId, PriceQuote Price);

    private async Task<Target?> TargetAsync(Booking b, ChangeBookingRequest input)
    {
        var dates = input.RequestType is ChangeRequestType.ChangeDates or ChangeRequestType.ChangeDatesAndRoomType;
        var type = input.RequestType is ChangeRequestType.ChangeRoomType or ChangeRequestType.ChangeDatesAndRoomType;
        if (!Enum.IsDefined(input.RequestType) || (dates && (input.NewCheckIn is null || input.NewCheckOut is null))
            || (!dates && (input.NewCheckIn is not null || input.NewCheckOut is not null))
            || (type != input.NewRoomTypeId.HasValue))
            throw new BookingException("Nội dung yêu cầu không khớp loại đổi/hủy.");
        if (input.RequestType == ChangeRequestType.Cancel) return null;
        var start = input.NewCheckIn ?? b.CheckIn;
        var end = input.NewCheckOut ?? b.CheckOut;
        if ((dates && start == b.CheckIn && end == b.CheckOut)
            || (type && input.NewRoomTypeId == b.RoomTypeId))
            throw new BookingException("Thông tin mới phải khác thông tin hiện tại của đơn.");
        var today = VietnamTime.Today(_clock);
        if (start < today || end <= start || end > today.AddDays(_options.ChangeWindowDays))
            throw new BookingException("Ngày mới không hợp lệ hoặc nằm ngoài thời hạn cho phép.");
        var roomType = await _db.RoomTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == (input.NewRoomTypeId ?? b.RoomTypeId));
        if (roomType is null || !roomType.IsActive || roomType.HomestayId != b.HomestayId || b.Homestay.Status != HomestayStatus.Active)
            throw new BookingException("Loại phòng phải đang hoạt động và thuộc cùng homestay.");
        if (!await _db.Contracts.Covering(today, end).AnyAsync(c => c.HomestayId == b.HomestayId))
            throw new BookingException("Hợp đồng không còn hiệu lực đến ngày trả phòng mới.");
        if (b.Guests > roomType.MaxGuests) throw new BookingException("Số khách vượt quá sức chứa của phòng.");
        return new Target(start, end, roomType.Id, await _pricing.QuoteAsync(roomType.Id, start, end));
    }

    private async Task<int> AvailableRoomAsync(Booking b, Target target, int? requestId = null)
    {
        var rooms = await _db.Rooms.Where(r => r.RoomTypeId == target.RoomTypeId && r.IsActive)
            .Where(r => !r.RoomNights.Any(n => n.Date >= target.CheckIn && n.Date < target.CheckOut
                && n.BookingId != b.Id && (requestId == null || n.BookingChangeRequestId != requestId)))
            .OrderBy(r => r.Id == b.RoomId ? 0 : 1).ThenBy(r => r.Id).Select(r => r.Id).ToListAsync();
        if (rooms.Count == 0) throw new BookingException(NoRooms);
        return rooms[0];
    }

    private async Task<BookingChangeQuote> QuoteAsync(Booking b, ChangeBookingRequest input)
    {
        await ValidateBookingAsync(b, input.RequestType == ChangeRequestType.Cancel);
        if (await _db.BookingChangeRequests.AnyAsync(r => r.BookingId == b.Id
            && (r.Status == ChangeRequestStatus.Pending || r.Status == ChangeRequestStatus.AwaitingPayment)))
            throw new BookingException(OpenRequest);
        var target = await TargetAsync(b, input);
        var refund = await _refunds.QuoteCancellationAsync(b);
        if (target is null) return new(0, 0, 0, 0, 0, refund.RefundAmount, refund.RefundPercent);
        await AvailableRoomAsync(b, target);
        var fee = input.RequestType == ChangeRequestType.ChangeRoomType ? 0 : b.AmountDue - refund.RefundAmount;
        var carried = b.AmountDue - fee;
        var required = PricingService.RoundToThousand(b.DepositRate * target.Price.Total);
        ValidateMoney(target.Price.Total, carried, required);
        return new(target.Price.Total, fee, carried, required, Math.Max(0, required - carried), null, null);
    }

    private static void ValidateMoney(decimal total, decimal carried, decimal required)
    {
        if (total < carried)
            throw new BookingException("Tổng tiền mới nhỏ hơn cọc chuyển sang; vui lòng chọn lịch hoặc loại phòng khác.");
        if (required <= 0 || required > total) throw new BookingException("Tiền cọc mới không hợp lệ.");
    }

    public async Task<BookingChangeQuote> QuoteAsync(ChangeBookingRequest input, ClaimsPrincipal principal)
    {
        var booking = await BookingAsync(input.BookingId);
        await ActorAsync(principal, booking);
        return await QuoteAsync(booking, input);
    }

    /// <summary>Điều kiện hiển thị nút đổi; khi gửi vẫn kiểm tra lại toàn bộ nghiệp vụ.</summary>
    public async Task<bool> CanChangeAsync(int bookingId, ClaimsPrincipal principal)
    {
        try
        {
            var booking = await BookingAsync(bookingId);
            await ActorAsync(principal, booking);
            await ValidateBookingAsync(booking, false);
            return !await _db.BookingChangeRequests.AnyAsync(r => r.BookingId == bookingId
                && (r.Status == ChangeRequestStatus.Pending || r.Status == ChangeRequestStatus.AwaitingPayment));
        }
        catch (BookingException) { return false; }
    }

    public Task<BookingChangeRequest> SubmitAsync(ChangeBookingRequest input, ClaimsPrincipal principal) => WriteAsync(async () =>
    {
        var booking = await BookingAsync(input.BookingId);
        var actor = await ActorAsync(principal, booking);
        if (input.RequestType == ChangeRequestType.Cancel && actor.Type == ActorType.Customer)
            throw new BookingException("Vui lòng dùng nút hủy đơn");
        var quote = await QuoteAsync(booking, input);
        // Ghi lại cùng Status để chặn cuộc đua với thao tác đổi trạng thái của đơn.
        _db.Entry(booking).Property(b => b.Status).IsModified = true;
        var request = new BookingChangeRequest { BookingId = booking.Id, RequestType = input.RequestType,
            RequestedByType = actor.Type, RequestedByAccountId = actor.AccountId,
            NewCheckIn = input.NewCheckIn, NewCheckOut = input.NewCheckOut, NewRoomTypeId = input.NewRoomTypeId,
            NewTotalAmount = quote.NewTotalAmount, ChangeFee = quote.ChangeFee, DepositCarriedOver = quote.DepositCarriedOver,
            AdditionalDeposit = quote.AdditionalDeposit, RefundAmount = quote.RefundAmount,
            PriceDifference = input.RequestType == ChangeRequestType.Cancel ? null : quote.NewTotalAmount - booking.TotalAmount,
            CreatedAt = Now };
        _db.BookingChangeRequests.Add(request);
        await _db.SaveChangesAsync();
        Audit(request.Id, actor, null, JsonSerializer.Serialize(new { Step = "Submitted", quote.RefundPercent,
            Request = RequestState(request), Booking = BookingState(booking) }), AuditAction.Insert);
        return request;
    });

    private static bool IsLegacy(BookingChangeRequest r) => r.RequestedByAccountId is null || r.ChangeFee is null
        || r.DepositCarriedOver is null || r.AdditionalDeposit is null || r.NewTotalAmount is null
        || (r.RequestType == ChangeRequestType.Cancel && r.RefundAmount is null);

    private async Task<(BookingChangeRequest Request, Booking Booking, Actor Actor)> DecisionAsync(int id, ClaimsPrincipal principal)
    {
        var request = await _db.BookingChangeRequests.SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new BookingException("Không tìm thấy yêu cầu.");
        var booking = await BookingAsync(request.BookingId);
        var actor = await ActorAsync(principal, booking, manager: true);
        if (actor.AccountId == request.RequestedByAccountId) throw new BookingException("Người gửi không được tự duyệt yêu cầu.");
        // Context có thể đang theo dõi bản cũ; đọc trạng thái hiện tại trước khi kiểm tra nội dung.
        // Concurrency token vẫn bảo vệ trường hợp trạng thái đổi sau lần đọc này.
        if (request.Status != ChangeRequestStatus.Pending || await _db.BookingChangeRequests.AsNoTracking()
            .AnyAsync(r => r.Id == id && r.Status != ChangeRequestStatus.Pending)) throw new BookingException(Processed);
        return (request, booking, actor);
    }

    private static string Note(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 500)
            throw new BookingException("Vui lòng nhập ghi chú/lý do có nội dung, tối đa 500 ký tự.");
        return value.Trim();
    }

    public async Task<BookingChangeRequest> RejectAsync(int id, string reason, ClaimsPrincipal principal)
    {
        var status = await _db.BookingChangeRequests.AsNoTracking().Where(r => r.Id == id)
            .Select(r => (ChangeRequestStatus?)r.Status).SingleOrDefaultAsync();
        return status == ChangeRequestStatus.AwaitingPayment
            ? await RejectFinanceAsync(id, reason, principal)
            : await RejectBusinessAsync(id, reason, principal);
    }

    private Task<BookingChangeRequest> RejectBusinessAsync(int id, string reason, ClaimsPrincipal principal) => WriteAsync(async () =>
    {
        var (request, booking, actor) = await DecisionAsync(id, principal);
        Reject(request, actor, Note(reason));
        Notify(booking, "đã bị từ chối");
        return request;
    });

    private void Reject(BookingChangeRequest request, Actor actor, string reason)
    {
        var old = JsonSerializer.Serialize(RequestState(request));
        request.Status = ChangeRequestStatus.Rejected;
        request.DecidedByAccountId = actor.AccountId; request.DecidedAt = Now;
        request.DecisionNote = request.RejectionReason = reason;
        Audit(request.Id, actor, old, JsonSerializer.Serialize(new { Step = "ManagerRejected", Request = RequestState(request) }));
    }

    public Task<BookingChangeRequest> ApproveAsync(int id, string note, ClaimsPrincipal principal) => WriteAsync(async () =>
    {
        note = Note(note);
        var (request, booking, actor) = await DecisionAsync(id, principal);
        if (IsLegacy(request)) throw new BookingException(LegacyRequest);
        await ValidateBookingAsync(booking, request.RequestType == ChangeRequestType.Cancel);
        var input = new ChangeBookingRequest(booking.Id, request.RequestType, request.NewCheckIn, request.NewCheckOut, request.NewRoomTypeId);
        var target = await TargetAsync(booking, input);
        if (target is not null && target.Price.Total != request.NewTotalAmount)
        {
            Reject(request, actor, PriceChanged); Notify(booking, "bị từ chối vì giá đã thay đổi");
            return request;
        }
        var old = JsonSerializer.Serialize(RequestState(request));
        var oldBooking = JsonSerializer.Serialize(BookingState(booking));
        _db.Entry(booking).Property(b => b.Status).IsModified = true;
        request.DecidedByAccountId = actor.AccountId; request.DecidedAt = Now; request.DecisionNote = note;
        if (target is null)
        {
            // Suy tỷ lệ từ tiền hoàn đã chốt, cùng cách tính của SettlementCalculator.
            var percent = booking.AmountDue > 0
                ? (int)Math.Round(100m * request.RefundAmount!.Value / booking.AmountDue) : 0;
            await _bookings.ApplyChangeRequestCancellationAsync(booking, request.RefundAmount!.Value, percent, actor, note);
            request.Status = ChangeRequestStatus.Applied;
        }
        else
        {
            if (request.ChangeFee < 0 || request.DepositCarriedOver < 0
                || request.ChangeFee + request.DepositCarriedOver != booking.AmountDue)
                throw new BookingException("Tiền cọc của đơn đã thay đổi, vui lòng gửi lại yêu cầu.");
            var required = PricingService.RoundToThousand(booking.DepositRate * target.Price.Total);
            ValidateMoney(target.Price.Total, request.DepositCarriedOver!.Value, required);
            if (request.AdditionalDeposit != Math.Max(0, required - request.DepositCarriedOver.Value))
                throw new BookingException("Tiền cọc của đơn đã thay đổi, vui lòng gửi lại yêu cầu.");
            var room = await AvailableRoomAsync(booking, target, request.Id);
            request.NewRoomId = room;
            if (request.AdditionalDeposit > 0)
            {
                await HoldNightsAsync(booking, request, target, room);
                request.Status = ChangeRequestStatus.AwaitingPayment;
                request.PaymentDeadlineAt = request.DecidedAt.Value.AddMinutes(_options.HoldMinutes);
            }
            else await ApplyChangeAsync(booking, request, target, room, actor, note);
        }
        Audit(request.Id, actor, old, JsonSerializer.Serialize(new { Step = "ManagerApproved", Request = RequestState(request) }));
        if (request.Status == ChangeRequestStatus.Applied)
            _db.AuditLogs.Add(new AuditLog { AccountId = actor.AccountId, TableName = "Bookings", RecordId = booking.Id.ToString(),
                Action = AuditAction.Update, OldValues = oldBooking, NewValues = JsonSerializer.Serialize(BookingState(booking)), CreatedAt = Now });
        Notify(booking, request.Status == ChangeRequestStatus.Applied ? "đã được áp dụng" : "đang chờ nộp cọc bổ sung");
        return request;
    });

    private void Notify(Booking b, string status) => _notifications.Add(
        [b.Customer.Person.Account?.Id, b.Homestay.Owner.Person.Account?.Id], $"Yêu cầu đổi/hủy đơn {b.Code} {status}.");

    private static object BookingState(Booking b) => new { b.Id, b.Code, b.CheckIn, b.CheckOut, b.RoomTypeId, b.RoomId,
        b.TotalAmount, b.DepositAmount, b.ChangeFeeAmount, b.DiscountAmount, b.RefundAmount, Status = b.Status.ToString(),
        b.ConfirmedAt, b.ConfirmDeadlineAt, b.ConfirmAlertSentAt, b.CancellationPolicyId };
    private static object RequestState(BookingChangeRequest r) => new { r.Id, r.BookingId, Type = r.RequestType.ToString(),
        Status = r.Status.ToString(), r.RequestedByAccountId, r.NewCheckIn, r.NewCheckOut, r.NewRoomTypeId, r.NewRoomId,
        r.NewTotalAmount, r.ChangeFee, r.DepositCarriedOver, r.AdditionalDeposit, r.RefundAmount,
        r.DecidedByAccountId, r.DecidedAt, r.DecisionNote, r.RejectionReason, r.PaymentDeadlineAt,
        r.FinanceConfirmedByAccountId, r.FinanceConfirmedAt, r.FinanceNote };
    private void Audit(int id, Actor actor, string? old, string current, AuditAction action = AuditAction.Update) =>
        _db.AuditLogs.Add(new AuditLog { AccountId = actor.AccountId, TableName = "BookingChangeRequests", RecordId = id.ToString(),
            Action = action, OldValues = old, NewValues = current, CreatedAt = Now });

    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        await using var transaction = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            var result = await action();
            await _db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            if (ex is DbUpdateConcurrencyException) throw new BookingException(Processed);
            var message = ex.InnerException?.Message ?? ex.Message;
            if (ex is DbUpdateException && (message.Contains("PK_RoomNights", StringComparison.Ordinal)
                || message.Contains("UNIQUE constraint failed: RoomNights.RoomId, RoomNights.Date", StringComparison.Ordinal)))
                throw new BookingException(NoRooms);
            if (ex is DbUpdateException && (message.Contains("IX_BookingChangeRequests_OneActivePerBooking", StringComparison.Ordinal)
                || message.Contains("UNIQUE constraint failed: BookingChangeRequests.BookingId", StringComparison.Ordinal)))
                throw new BookingException(OpenRequest);
            throw;
        }
    }
}
