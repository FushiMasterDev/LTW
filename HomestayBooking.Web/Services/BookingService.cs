using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public record CreateBookingRequest(
    int CustomerId,
    int RoomTypeId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    string ContactName,
    string ContactPhone,
    string? SpecialRequests,
    SourceChannel SourceChannel,
    Actor Actor,
    int? VoucherId = null);

/// <summary>
/// Tạo đơn và đổi trạng thái đơn — mọi thay đổi trạng thái đi qua <see cref="ChangeStatusAsync"/>
/// (fuji_homestay_database_v1 mục 6): kiểm tra chuyển hợp lệ, cập nhật lịch phòng, ghi lịch sử, thông báo.
/// </summary>
public class BookingService
{
    /// <summary>Các chuyển trạng thái hợp lệ (mục 6.1).</summary>
    private static readonly Dictionary<BookingStatus, BookingStatus[]> Transitions = new()
    {
        [BookingStatus.AwaitingPayment] = [BookingStatus.PaymentSubmitted, BookingStatus.Deposited, BookingStatus.Expired, BookingStatus.Cancelled],
        [BookingStatus.PaymentSubmitted] = [BookingStatus.Deposited, BookingStatus.AwaitingPayment, BookingStatus.Expired, BookingStatus.Cancelled],
        [BookingStatus.Deposited] = [BookingStatus.Confirmed, BookingStatus.Rejected, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.CheckedIn, BookingStatus.Cancelled, BookingStatus.NoShow],
        [BookingStatus.CheckedIn] = [BookingStatus.Completed],
    };

    private readonly ApplicationDbContext _db;
    private readonly PricingService _pricing;
    private readonly RefundService _refunds;
    private readonly BookingCodeGenerator _codes;
    private readonly NotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly BookingOptions _options;
    private readonly VoucherService _vouchers;

    public BookingService(
        ApplicationDbContext db,
        PricingService pricing,
        RefundService refunds,
        BookingCodeGenerator codes,
        NotificationService notifications,
        TimeProvider clock,
        IOptions<BookingOptions> options,
        VoucherService vouchers)
    {
        _vouchers = vouchers;
        _db = db;
        _pricing = pricing;
        _refunds = refunds;
        _codes = codes;
        _notifications = notifications;
        _clock = clock;
        _options = options.Value;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    public static bool CanTransition(BookingStatus from, BookingStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    // ---------------------------------------------------------------- Tạo đơn

    /// <summary>
    /// Tạo đơn AwaitingPayment, gán một phòng trống và giữ phòng ngay (D4), hạn thanh toán 15 phút (D5).
    /// Hai khách đặt cùng lúc vào phòng cuối cùng: khóa chính RoomNights chặn người sau → thử phòng khác.
    /// </summary>
    public async Task<Booking> CreateAsync(CreateBookingRequest request)
    {
        var today = VietnamTime.Today(_clock);
        if (request.CheckIn < today)
        {
            throw new BookingException("Ngày nhận phòng không được ở quá khứ.");
        }

        var roomType = await _db.RoomTypes
            .Include(t => t.Homestay)
            .SingleOrDefaultAsync(t => t.Id == request.RoomTypeId)
            ?? throw new BookingException("Không tìm thấy loại phòng.");
        if (!roomType.IsActive || roomType.Homestay.Status != HomestayStatus.Active)
        {
            throw new BookingException("Loại phòng này hiện không nhận đặt.");
        }
        if (request.Guests < 1 || request.Guests > roomType.MaxGuests)
        {
            throw new BookingException("Số khách vượt quá sức chứa của phòng.");
        }

        var contract = await _db.Contracts
            .Covering(today, request.CheckOut)
            .Where(c => c.HomestayId == roomType.HomestayId)
            .SingleOrDefaultAsync()
            ?? throw new BookingException("Loại phòng này hiện không nhận đặt.");

        var awaiting = await _db.Bookings.CountAsync(b =>
            b.CustomerId == request.CustomerId && b.Status == BookingStatus.AwaitingPayment);
        if (awaiting >= _options.MaxAwaitingPerCustomer)
        {
            throw new BookingException("Bạn đang có quá nhiều đơn chờ thanh toán. Vui lòng thanh toán hoặc hủy bớt đơn.");
        }

        var quote = await _pricing.QuoteAsync(roomType.Id, request.CheckIn, request.CheckOut);
        var policy = await _refunds.SelectPolicyAsync(roomType.HomestayId, quote.HasHoliday);

        var candidateRoomIds = await FreeRoomIdsAsync(roomType.Id, request.CheckIn, request.CheckOut);
        if (candidateRoomIds.Count == 0)
        {
            throw new BookingException("Loại phòng này đã hết phòng trong thời gian bạn chọn.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var code = await _codes.NextAsync(today);
        var now = UtcNow;

        foreach (var roomId in candidateRoomIds)
        {
            var booking = new Booking
            {
                Code = code,
                CustomerId = request.CustomerId,
                ContactName = request.ContactName.Trim(),
                ContactPhone = request.ContactPhone,
                SourceChannel = request.SourceChannel,
                CreatedByEmployeeId = request.Actor.EmployeeId,
                AssignedEmployeeId = request.Actor.EmployeeId,
                HomestayId = roomType.HomestayId,
                RoomTypeId = roomType.Id,
                RoomId = roomId,
                CheckIn = request.CheckIn,
                CheckOut = request.CheckOut,
                Guests = request.Guests,
                SpecialRequests = string.IsNullOrWhiteSpace(request.SpecialRequests) ? null : request.SpecialRequests.Trim(),
                Status = BookingStatus.AwaitingPayment,
                HoldExpiresAt = now.AddMinutes(_options.HoldMinutes),
                TotalAmount = quote.Total,
                DepositRate = quote.DepositRate,
                DepositAmount = quote.Deposit,
                CommissionRate = contract.CommissionRate,
                CancellationPolicyId = policy.Id,
                CreatedAt = now,
                UpdatedAt = now,
            };
            foreach (var night in quote.Nights)
            {
                booking.Nights.Add(new BookingNight { Date = night.Date, DayType = night.DayType, Price = night.Price });
                booking.RoomNights.Add(new RoomNight { RoomId = roomId, Date = night.Date });
            }
            booking.Histories.Add(new BookingStatusHistory
            {
                ToStatus = BookingStatus.AwaitingPayment,
                ActorType = request.Actor.Type,
                ActorAccountId = request.Actor.AccountId,
                ChangedAt = now,
            });
            if (request.VoucherId is int voucherId)
            {
                // Trừ số dư voucher trong cùng lần lưu với đơn: đặt phòng lỗi thì voucher không bị trừ
                await _vouchers.ApplyAsync(booking, voucherId, request.CustomerId);
            }
            _db.Bookings.Add(booking);

            try
            {
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Số dư voucher vừa bị một đơn khác cùng lúc trừ (Balance là concurrency token)
                _db.ChangeTracker.Clear();
                throw new BookingException("Voucher vừa được dùng cho đơn khác, vui lòng thử lại.");
            }
            catch (DbUpdateException)
            {
                // Phòng vừa bị người khác giữ giữa lúc kiểm tra và lúc ghi → bỏ đơn nháp, thử phòng kế tiếp
                _db.ChangeTracker.Clear();
                if (!await IsTakenAsync(roomId, request.CheckIn, request.CheckOut))
                {
                    throw;
                }
            }
        }

        throw new BookingException("Loại phòng này đã hết phòng trong thời gian bạn chọn.");
    }

    /// <summary>Phòng đang kinh doanh của loại phòng mà không có đêm nào bị chiếm trong [checkIn, checkOut).</summary>
    public Task<List<int>> FreeRoomIdsAsync(int roomTypeId, DateOnly checkIn, DateOnly checkOut) =>
        _db.Rooms
            .Where(r => r.RoomTypeId == roomTypeId && r.IsActive && r.RoomType.IsActive)
            .Where(r => !r.RoomNights.Any(n => n.Date >= checkIn && n.Date < checkOut))
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToListAsync();

    private Task<bool> IsTakenAsync(int roomId, DateOnly checkIn, DateOnly checkOut) =>
        _db.RoomNights.AnyAsync(n => n.RoomId == roomId && n.Date >= checkIn && n.Date < checkOut);

    // ---------------------------------------------------------------- Đổi trạng thái

    /// <summary>
    /// Đổi trạng thái đơn và lưu. Status là concurrency token: nếu thao tác khác vừa đổi trạng thái
    /// (ví dụ job hết hạn chạy đúng lúc tiền về), ném DbUpdateConcurrencyException thay vì ghi đè.
    /// </summary>
    public async Task ChangeStatusAsync(Booking booking, BookingStatus to, Actor actor, string? note = null)
    {
        await ApplyStatusAsync(booking, to, actor, note);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Homestay xác nhận đơn đã cọc (D12). Được đổi sang phòng trống khác cùng loại (Q8): chuyển RoomNights
    /// và đổi trạng thái trong cùng một SaveChanges, ghi "Đổi phòng" vào lịch sử.
    /// </summary>
    public async Task ConfirmAsync(Booking booking, int? newRoomId, Actor actor)
    {
        string? note = null;
        if (newRoomId is int roomId && roomId != booking.RoomId)
        {
            if (booking.Status != BookingStatus.Deposited)
            {
                throw new BookingException("Chỉ đổi phòng được khi xác nhận đơn đã đặt cọc.");
            }
            if (!(await FreeRoomIdsAsync(booking.RoomTypeId, booking.CheckIn, booking.CheckOut)).Contains(roomId))
            {
                throw new BookingException("Phòng được chọn không trống hoặc không cùng loại phòng.");
            }
            var codes = await _db.Rooms.Where(r => r.Id == roomId || r.Id == booking.RoomId).ToDictionaryAsync(r => r.Id, r => r.RoomCode);
            var nights = await _db.RoomNights.Where(n => n.BookingId == booking.Id).ToListAsync();
            _db.RoomNights.RemoveRange(nights);
            _db.RoomNights.AddRange(nights.Select(n => new RoomNight { RoomId = roomId, Date = n.Date, BookingId = booking.Id }));
            note = $"Đổi phòng {codes[booking.RoomId]} → {codes[roomId]}";
            booking.RoomId = roomId;
        }

        await ApplyStatusAsync(booking, BookingStatus.Confirmed, actor, note);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && note is not null)
        {
            // Phòng mới vừa bị đơn khác giữ giữa lúc kiểm tra và lúc ghi (khóa chính RoomNights)
            _db.ChangeTracker.Clear();
            throw new BookingException("Phòng được chọn vừa có người đặt, vui lòng chọn phòng khác.");
        }
    }

    /// <summary>Như ChangeStatusAsync nhưng không lưu — để gộp chung một SaveChanges với thao tác khác (ví dụ tạo Payment).</summary>
    public async Task ApplyStatusAsync(Booking booking, BookingStatus to, Actor actor, string? note = null)
    {
        var from = booking.Status;
        if (!CanTransition(from, to))
        {
            throw new BookingException($"Không thể chuyển đơn từ \"{from.DisplayName()}\" sang \"{to.DisplayName()}\".");
        }
        if (to == BookingStatus.CheckedIn && VietnamTime.Today(_clock) < booking.CheckIn)
        {
            throw new BookingException("Chưa đến ngày nhận phòng.");
        }
        // Khách có thể đến muộn trong ngày nhận phòng: chỉ coi là không đến từ ngày hôm sau
        if (to == BookingStatus.NoShow && VietnamTime.Today(_clock) <= booking.CheckIn)
        {
            throw new BookingException("Chỉ đánh dấu khách không đến từ ngày sau ngày nhận phòng.");
        }

        var now = UtcNow;
        var voucherRestorePercent = 100;
        switch (to)
        {
            case BookingStatus.AwaitingPayment:
                // Kế toán từ chối bill: khách có thêm 15 phút để gửi lại (mục 6.2)
                booking.HoldExpiresAt = now.AddMinutes(_options.HoldMinutes);
                break;
            case BookingStatus.PaymentSubmitted:
                booking.PaymentSubmittedAt = now;
                break;
            case BookingStatus.Deposited:
                booking.DepositedAt = now;
                booking.ConfirmDeadlineAt = now.AddMinutes(_options.ConfirmMinutes);
                break;
            case BookingStatus.Confirmed:
                booking.ConfirmedAt = now;
                break;
            case BookingStatus.CheckedIn:
                booking.CheckedInAt = now;
                break;
            case BookingStatus.Completed:
                booking.CompletedAt = now;
                break;
            case BookingStatus.Rejected:
                // Homestay từ chối sau khi khách đã cọc → hoàn 100% (D12)
                booking.RefundAmount = booking.AmountDue;
                booking.CancelledAt = now;
                booking.CancelReason = note;
                break;
            case BookingStatus.Cancelled:
                var refund = await _refunds.QuoteCancellationAsync(booking);
                booking.RefundAmount = refund.RefundAmount;
                // Đã có cọc: voucher trả lại theo cùng tỷ lệ hoàn tiền; chưa có cọc: trả lại đủ
                voucherRestorePercent = from is BookingStatus.Deposited or BookingStatus.Confirmed ? refund.RefundPercent : 100;
                booking.CancelledAt = now;
                booking.CancelReason = note;
                break;
            case BookingStatus.NoShow:
                booking.RefundAmount = 0;
                break;
            case BookingStatus.Expired:
                booking.CancelledAt = now;
                break;
        }

        if (to is BookingStatus.Cancelled or BookingStatus.Rejected or BookingStatus.Expired)
        {
            // Đơn không thành (không do khách vắng mặt) → trả lại số tiền đã giảm vào voucher
            await _vouchers.RestoreAsync(booking, voucherRestorePercent);
        }

        if (!to.HoldsRoom())
        {
            // Mở lại phòng — xóa trong cùng SaveChanges với việc đổi trạng thái: lỗi thì cả hai cùng hủy
            _db.RoomNights.RemoveRange(await _db.RoomNights.Where(n => n.BookingId == booking.Id).ToListAsync());
        }

        booking.Status = to;
        booking.UpdatedAt = now;
        _db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            FromStatus = from,
            ToStatus = to,
            ActorType = actor.Type,
            ActorAccountId = actor.AccountId,
            Note = note,
            ChangedAt = now,
        });

        await NotifyAsync(booking, to);
    }

    /// <summary>Chỉ dùng sau khi yêu cầu đổi đã được duyệt; không mở Transitions chung.</summary>
    internal void ApplyChangeRequestReconfirmation(Booking booking, Actor actor, string note)
    {
        if (booking.Status is not (BookingStatus.Deposited or BookingStatus.Confirmed))
            throw new BookingException("Đơn không còn được đổi.");
        var from = booking.Status;
        booking.Status = BookingStatus.Deposited;
        booking.ConfirmedAt = null;
        booking.ConfirmDeadlineAt = UtcNow.AddMinutes(_options.ConfirmMinutes);
        booking.ConfirmAlertSentAt = null;
        booking.UpdatedAt = UtcNow;
        if (from != booking.Status)
            _db.BookingStatusHistories.Add(new BookingStatusHistory { BookingId = booking.Id,
                FromStatus = from, ToStatus = booking.Status, ActorType = actor.Type,
                ActorAccountId = actor.AccountId, Note = note, ChangedAt = UtcNow });
    }

    /// <summary>Hủy bằng tiền hoàn và tỷ lệ voucher đã chốt lúc gửi, không báo giá lại theo ngày duyệt.</summary>
    internal async Task ApplyChangeRequestCancellationAsync(Booking booking, decimal refund, int refundPercent, Actor actor, string note)
    {
        if (booking.Status is not (BookingStatus.Deposited or BookingStatus.Confirmed)
            || refund < 0 || refund > booking.AmountDue || refundPercent is < 0 or > 100)
            throw new BookingException("Đơn không còn được hủy theo yêu cầu này.");
        var from = booking.Status;
        booking.RefundAmount = refund;
        booking.CancelledAt = booking.UpdatedAt = UtcNow;
        booking.CancelReason = note;
        booking.Status = BookingStatus.Cancelled;
        await _vouchers.RestoreAsync(booking, refundPercent);
        _db.RoomNights.RemoveRange(await _db.RoomNights.Where(n => n.BookingId == booking.Id).ToListAsync());
        _db.BookingStatusHistories.Add(new BookingStatusHistory { BookingId = booking.Id,
            FromStatus = from, ToStatus = booking.Status, ActorType = actor.Type,
            ActorAccountId = actor.AccountId, Note = note, ChangedAt = UtcNow });
    }

    private async Task NotifyAsync(Booking booking, BookingStatus to)
    {
        var accounts = await _db.Bookings
            .Where(b => b.Id == booking.Id)
            .Select(b => new
            {
                Customer = b.Customer.Person.Account == null ? null : b.Customer.Person.Account.Id,
                Owner = b.Homestay.Owner.Person.Account == null ? null : b.Homestay.Owner.Person.Account.Id,
                Staff = b.AssignedEmployee == null || b.AssignedEmployee.Person.Account == null
                    ? null : b.AssignedEmployee.Person.Account.Id,
            })
            .SingleAsync();

        var code = booking.Code;
        switch (to)
        {
            case BookingStatus.Deposited:
                _notifications.Add([accounts.Owner], $"Đơn {code} đã đặt cọc, vui lòng xác nhận trong {_options.ConfirmMinutes} phút.");
                _notifications.Add([accounts.Customer], $"Fuji đã nhận tiền cọc đơn {code}. Đang chờ homestay xác nhận.");
                break;
            case BookingStatus.Confirmed:
                _notifications.Add([accounts.Customer, accounts.Staff], $"Đơn {code} đã được homestay xác nhận.");
                break;
            case BookingStatus.Rejected:
                _notifications.Add([accounts.Customer, accounts.Staff], $"Homestay đã từ chối đơn {code}. Fuji sẽ hoàn 100% tiền cọc.");
                break;
            case BookingStatus.Expired:
                _notifications.Add([accounts.Customer, accounts.Staff], $"Đơn {code} đã hết hạn giữ phòng vì chưa thanh toán.");
                break;
            case BookingStatus.Cancelled:
                _notifications.Add([accounts.Owner, accounts.Staff], $"Đơn {code} đã bị hủy.");
                break;
        }
    }

    // ---------------------------------------------------------------- Việc định kỳ (job mỗi phút)

    /// <summary>Đơn chờ thanh toán quá hạn giữ phòng → Expired, mở lại phòng (mục 6.2 bước 4).</summary>
    public async Task<int> ExpireOverdueHoldsAsync()
    {
        var now = UtcNow;
        var overdueIds = await _db.Bookings
            .Where(b => b.Status == BookingStatus.AwaitingPayment && b.HoldExpiresAt < now)
            .Select(b => b.Id)
            .ToListAsync();

        var expired = 0;
        foreach (var id in overdueIds)
        {
            var booking = await _db.Bookings.SingleAsync(b => b.Id == id);
            try
            {
                await ChangeStatusAsync(booking, BookingStatus.Expired, Actor.System, "Quá hạn giữ phòng chờ thanh toán.");
                expired++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Tiền vừa về và đơn đã đổi trạng thái → bỏ qua đơn này
            }
            _db.ChangeTracker.Clear();
        }
        return expired;
    }

    /// <summary>Đơn đã cọc mà homestay quá 30 phút chưa xác nhận → cảnh báo nhân viên một lần (BR-06).</summary>
    public async Task<int> AlertOverdueConfirmationsAsync()
    {
        var now = UtcNow;
        var overdue = await _db.Bookings
            .Where(b => b.Status == BookingStatus.Deposited && b.ConfirmDeadlineAt < now && b.ConfirmAlertSentAt == null)
            .Select(b => new
            {
                Booking = b,
                Staff = b.AssignedEmployee == null || b.AssignedEmployee.Person.Account == null
                    ? null : b.AssignedEmployee.Person.Account.Id,
            })
            .ToListAsync();
        if (overdue.Count == 0)
        {
            return 0;
        }

        // Đơn khách tự đặt chưa có người phụ trách → báo mọi nhân viên tư vấn đang làm việc
        var consultants = await _db.Employees
            .Where(e => e.Position == EmployeePosition.Consultant && e.EmploymentStatus == EmploymentStatus.Active
                        && e.Person.Account != null)
            .Select(e => e.Person.Account!.Id)
            .ToListAsync();

        foreach (var item in overdue)
        {
            var recipients = item.Staff is null ? consultants : [item.Staff];
            _notifications.Add(recipients,
                $"Đơn {item.Booking.Code} đã quá {_options.ConfirmMinutes} phút homestay chưa xác nhận. Vui lòng gọi nhắc.");
            item.Booking.ConfirmAlertSentAt = now;
        }
        await _db.SaveChangesAsync();
        return overdue.Count;
    }
}
