using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Ảnh bill — đường dự phòng khi giao dịch không tự khớp được (D11, D13): khách tải bill, đơn chuyển
/// PaymentSubmitted và vẫn giữ phòng; kế toán hoặc quản lý duyệt/từ chối. Nếu tiền về qua webhook trước
/// khi ai duyệt thì BankMatchingService tự duyệt luôn bill này.
/// </summary>
public class ReceiptReviewService
{
    private static readonly EmployeePosition[] Reviewers = [EmployeePosition.Accountant, EmployeePosition.Manager];

    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;
    private readonly NotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly BookingOptions _options;

    public ReceiptReviewService(
        ApplicationDbContext db,
        BookingService bookings,
        NotificationService notifications,
        TimeProvider clock,
        IOptions<BookingOptions> options)
    {
        _db = db;
        _bookings = bookings;
        _notifications = notifications;
        _clock = clock;
        _options = options.Value;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    /// <summary>Khách gửi ảnh bill (đã lưu file, truyền đường dẫn) cho đơn đang chờ thanh toán và còn hạn giữ phòng.</summary>
    public async Task<Payment> SubmitAsync(int bookingId, string receiptPath, string? transferContent, Actor actor)
    {
        var booking = await _db.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");
        if (booking.Status != BookingStatus.AwaitingPayment || booking.HoldExpiresAt < UtcNow)
        {
            throw new BookingException("Đơn này không còn chờ thanh toán.");
        }

        var payment = new Payment
        {
            Type = PaymentType.Deposit,
            Method = PaymentMethod.ManualReceipt,
            Amount = booking.AmountDue,
            ReceiptImageUrl = receiptPath,
            TransferContent = string.IsNullOrWhiteSpace(transferContent) ? null : transferContent.Trim(),
            SubmittedByAccountId = actor.AccountId,
            SubmittedAt = UtcNow,
        };
        booking.Payments.Add(payment);
        await _bookings.ApplyStatusAsync(booking, BookingStatus.PaymentSubmitted, actor, "Khách gửi ảnh bill.");
        _notifications.Add(await ReviewerAccountIdsAsync(), $"Đơn {booking.Code} có ảnh bill chờ duyệt.");
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task ApproveAsync(int paymentId, int employeeId)
    {
        var (payment, actor) = await LoadForReviewAsync(paymentId, employeeId);
        payment.Status = PaymentStatus.Approved;
        payment.ReviewedByEmployeeId = employeeId;
        payment.ReviewedAt = UtcNow;
        await _bookings.ApplyStatusAsync(payment.Booking, BookingStatus.Deposited, actor, "Kế toán duyệt ảnh bill.");
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Từ chối bill: đơn về chờ thanh toán với hạn giữ phòng mới 15 phút (BookingService.ApplyStatusAsync).
    /// Bị từ chối đến lần thứ MaxReceiptRejections (mặc định 2) thì đơn hết hạn và mở lại phòng.
    /// </summary>
    public async Task RejectAsync(int paymentId, int employeeId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BookingException("Vui lòng nhập lý do từ chối.");
        }
        var (payment, actor) = await LoadForReviewAsync(paymentId, employeeId);
        payment.Status = PaymentStatus.Rejected;
        payment.ReviewedByEmployeeId = employeeId;
        payment.ReviewedAt = UtcNow;
        payment.RejectReason = reason.Trim();

        var booking = payment.Booking;
        var rejections = 1 + await _db.Payments.CountAsync(p =>
            p.BookingId == booking.Id && p.Id != payment.Id && p.Method == PaymentMethod.ManualReceipt && p.Status == PaymentStatus.Rejected);
        var giveUp = rejections >= _options.MaxReceiptRejections;
        await _bookings.ApplyStatusAsync(booking, giveUp ? BookingStatus.Expired : BookingStatus.AwaitingPayment, actor,
            $"Từ chối bill lần {rejections}: {payment.RejectReason}");

        var customer = await _db.Bookings
            .Where(b => b.Id == booking.Id)
            .Select(b => b.Customer.Person.Account == null ? null : b.Customer.Person.Account.Id)
            .SingleAsync();
        _notifications.Add([customer], giveUp
            ? $"Ảnh bill đơn {booking.Code} bị từ chối lần {rejections} ({payment.RejectReason}) nên đơn đã hết hạn. Nếu bạn đã chuyển tiền, vui lòng liên hệ Fuji để được hoàn."
            : $"Ảnh bill đơn {booking.Code} chưa hợp lệ ({payment.RejectReason}). Bạn có {_options.HoldMinutes} phút để thanh toán lại.");
        await _db.SaveChangesAsync();
    }

    private async Task<(Payment Payment, Actor Actor)> LoadForReviewAsync(int paymentId, int employeeId)
    {
        var reviewer = await _db.Employees
            .Where(e => e.Id == employeeId && e.EmploymentStatus == EmploymentStatus.Active && Reviewers.Contains(e.Position))
            .Select(e => new { e.Id, AccountId = e.Person.Account == null ? null : e.Person.Account.Id })
            .SingleOrDefaultAsync()
            ?? throw new BookingException("Chỉ kế toán hoặc quản lý được duyệt bill.");

        var payment = await _db.Payments.Include(p => p.Booking).SingleOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new BookingException("Không tìm thấy khoản thanh toán.");
        if (payment.Method != PaymentMethod.ManualReceipt || payment.Status != PaymentStatus.Submitted
            || payment.Booking.Status != BookingStatus.PaymentSubmitted)
        {
            throw new BookingException("Bill này đã được xử lý.");
        }
        return (payment, new Actor(ActorType.Employee, reviewer.AccountId, reviewer.Id));
    }

    private Task<List<string>> ReviewerAccountIdsAsync(params EmployeePosition[] positions)
    {
        positions = positions.Length == 0 ? Reviewers : positions;
        return _db.Employees
            .Where(e => positions.Contains(e.Position) && e.EmploymentStatus == EmploymentStatus.Active && e.Person.Account != null)
            .Select(e => e.Person.Account!.Id)
            .ToListAsync();
    }

    /// <summary>Bill chờ duyệt quá 2 giờ hành chính → báo quản lý một lần. Ngoài giờ không tính (bill gửi tối duyệt sáng hôm sau).</summary>
    public async Task<int> AlertSlowReviewsAsync()
    {
        var now = UtcNow;
        var pending = await _db.Payments
            .Include(p => p.Booking)
            .Where(p => p.Method == PaymentMethod.ManualReceipt && p.Status == PaymentStatus.Submitted && p.ReviewAlertSentAt == null)
            .ToListAsync();
        var overdue = pending
            .Where(p => VietnamTime.OfficeMinutesBetween(p.SubmittedAt, now, _options.OfficeStartHour, _options.OfficeEndHour)
                        >= _options.ReceiptReviewAlertMinutes)
            .ToList();
        if (overdue.Count == 0)
        {
            return 0;
        }

        var managers = await ReviewerAccountIdsAsync(EmployeePosition.Manager);
        foreach (var payment in overdue)
        {
            _notifications.Add(managers,
                $"Bill đơn {payment.Booking.Code} đã chờ duyệt quá {_options.ReceiptReviewAlertMinutes / 60} giờ làm việc.");
            payment.ReviewAlertSentAt = now;
        }
        await _db.SaveChangesAsync();
        return overdue.Count;
    }
}
