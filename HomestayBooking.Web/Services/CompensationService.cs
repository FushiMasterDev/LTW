using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>BR-11/Q3: Fuji trả khoản bù riêng, không làm đổi tổng bảng đối soát.</summary>
public class CompensationService(ApplicationDbContext db, AccountingService accounting, NotificationService notifications, TimeProvider clock)
{
    private static readonly BookingStatus[] ClosedWithoutStay = [BookingStatus.Rejected, BookingStatus.Cancelled, BookingStatus.Expired, BookingStatus.NoShow];
    private static readonly EmployeePosition[] Readers = [EmployeePosition.Accountant, EmployeePosition.Manager, EmployeePosition.Administrator];
    public async Task<IReadOnlyList<Payment>> PendingAsync(Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, Readers);
        return await db.Payments.AsNoTracking().Include(p => p.Booking).Where(p => p.Type == PaymentType.Compensation && p.Status == PaymentStatus.Submitted
            && p.Booking.Status == BookingStatus.Completed && p.Booking.ReplacedByBookingId == null
            && db.Bookings.Any(b => b.ReplacedByBookingId == p.BookingId)).OrderBy(p => p.SubmittedAt).ThenBy(p => p.Id).ToListAsync();
    }

    public Task<Payment> RecordPaidAsync(int id, string bankRef, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        var p = await LoadLockedAsync(id);
        if (p.Booking.Status != BookingStatus.Completed || p.Booking.ReplacedByBookingId != null || !await db.Bookings.AnyAsync(b => b.ReplacedByBookingId == p.BookingId))
            throw new BookingException("Chỉ chuyển khoản bù khi đơn thay thế đã Completed và không bị thay tiếp.");
        await PartnerServiceSupport.RequireOwnerBankAsync(db, p.Booking.HomestayId);
        await accounting.RecordTransferInTransactionAsync(p, actor, bankRef);
        var owner = await db.Homestays.Where(h => h.Id == p.Booking.HomestayId).Select(h => h.Owner.Person.Account == null ? null : h.Owner.Person.Account.Id).SingleAsync();
        PartnerServiceSupport.Notify(db, notifications, clock, [owner], $"Fuji đã chuyển riêng khoản bù {p.Amount:N0}đ cho đơn {p.Booking.Code}.", $"/Owner/Bookings/Details/{p.BookingId}");
        return p;
    });

    public Task<Payment> RejectAsync(int id, string reason, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new BookingException("Lý do từ chối phải có từ 1 đến 500 ký tự.");
        var p = await LoadLockedAsync(id);
        if (p.Booking.Status == BookingStatus.Completed) throw new BookingException("Đơn đã Completed, không được từ chối khoản bù thủ công.");
        Reject(p, reason.Trim(), actor); return p;
    });

    public async Task<int> RejectClosedAsync()
    {
        var ids = await db.Payments.Where(p => p.Type == PaymentType.Compensation && p.Status == PaymentStatus.Submitted
            && (ClosedWithoutStay.Contains(p.Booking.Status) || p.Booking.ReplacedByBookingId != null)).Select(p => p.Id).ToListAsync();
        var count = 0;
        foreach (var id in ids)
        {
            count += await HomestayWrite.RunAsync(db, async () =>
            {
                var p = await LoadLockedAsync(id, required: false);
                if (p.Status != PaymentStatus.Submitted || !ClosedWithoutStay.Contains(p.Booking.Status) && p.Booking.ReplacedByBookingId is null) return 0;
                Reject(p, p.Booking.ReplacedByBookingId != null ? "Đơn thay thế đã được thay tiếp, không chi khoản bù này." : $"Đơn thay thế kết thúc ở trạng thái {p.Booking.Status}, không chi khoản bù.", Actor.System);
                return 1;
            });
        }
        return count;
    }

    private void Reject(Payment p, string reason, Actor actor)
    {
        var old = AccountingService.TransferState(p); p.Status = PaymentStatus.Rejected;
        p.RejectReason = reason; p.ReviewedAt = clock.GetUtcNow().UtcDateTime; p.ReviewedByEmployeeId = actor.EmployeeId;
        PartnerServiceSupport.Audit(db, clock, actor, "Payments", p.Id.ToString(), old, AccountingService.TransferState(p));
    }
    private async Task<Payment> LoadLockedAsync(int id, bool required = true)
    {
        var bookingId = await db.Payments.Where(p => p.Id == id && p.Type == PaymentType.Compensation).Select(p => (int?)p.BookingId).SingleOrDefaultAsync()
            ?? throw new BookingException("Không tìm thấy khoản bù.");
        await accounting.LockTransferBookingAsync(bookingId);
        var p = await db.Payments.Include(p => p.Booking).SingleAsync(p => p.Id == id);
        await db.Entry(p).ReloadAsync(); await db.Entry(p.Booking).ReloadAsync();
        if (required && p.Status != PaymentStatus.Submitted) throw new BookingException("Khoản bù đã được xử lý.");
        return p;
    }
}
