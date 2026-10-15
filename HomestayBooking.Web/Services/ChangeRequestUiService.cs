using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>Báo giá có chữ ký và dữ liệu hiển thị, không nhận người gửi hoặc số tiền từ form.</summary>
public class ChangeRequestUiService(ApplicationDbContext db, BookingChangeRequestService changes,
    IDataProtectionProvider protection, TimeProvider clock, IOptions<BookingOptions> options)
{
    private readonly IDataProtector _protector = protection.CreateProtector("BookingChangePreview.v1");
    private record Preview(string AccountId, ChangeBookingRequest Input, BookingChangeQuote Quote, DateTime ExpiresAt);

    public async Task PrepareAsync(ChangeRequestForm form, Booking booking, bool staff)
    {
        form.Booking = booking; form.IsStaff = staff;
        form.RoomTypes = await db.RoomTypes.AsNoTracking().Where(t => t.HomestayId == booking.HomestayId && t.IsActive)
            .OrderBy(t => t.Name).Select(t => new ChangeRoomOption(t.Id, t.Name, t.MaxGuests)).ToListAsync();
    }

    public async Task QuoteAsync(ChangeRequestForm form, ClaimsPrincipal principal)
    {
        if (!form.IsStaff && form.RequestType == ChangeRequestType.Cancel) throw new BookingException("Vui lòng dùng nút hủy đơn");
        var input = form.Input(form.Booking.Id);
        form.Quote = await changes.QuoteAsync(input, principal);
        form.PreviewToken = _protector.Protect(JsonSerializer.Serialize(new Preview(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!, input, form.Quote,
            clock.GetUtcNow().UtcDateTime.AddMinutes(options.Value.HoldMinutes))));
    }

    public async Task SubmitAsync(ChangeRequestForm form, ClaimsPrincipal principal)
    {
        Preview? preview;
        try { preview = JsonSerializer.Deserialize<Preview>(_protector.Unprotect(form.PreviewToken ?? "")); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        { throw new BookingException("Vui lòng xem lại báo giá trước khi gửi yêu cầu."); }
        if (preview is null || preview.AccountId != principal.FindFirstValue(ClaimTypes.NameIdentifier)
            || preview.Input != form.Input(form.Booking.Id) || preview.ExpiresAt < clock.GetUtcNow().UtcDateTime)
            throw new BookingException("Vui lòng xem lại báo giá trước khi gửi yêu cầu.");
        if (preview.Quote != await changes.QuoteAsync(preview.Input, principal))
            throw new BookingException("Báo giá đã thay đổi, vui lòng xem lại trước khi gửi.");
        await changes.SubmitAsync(preview.Input, principal);
    }

    public async Task<CustomerChangeRequests> CustomerAsync(Booking booking, ClaimsPrincipal principal, VietQrService qr, bool own)
    {
        var history = await HistoryAsync([booking.Id]);
        var current = history.Select(h => h.Request).FirstOrDefault(r => r.Status == ChangeRequestStatus.AwaitingPayment);
        var submitted = current is not null && await db.Payments.AnyAsync(p => p.ChangeRequestId == current.Id
            && p.Type == PaymentType.AdditionalDeposit && (p.Status == PaymentStatus.Submitted || p.Status == PaymentStatus.Approved));
        string? image = null;
        if (own && current is not null && !submitted && current.PaymentDeadlineAt >= clock.GetUtcNow().UtcDateTime)
        {
            try { image = await changes.GetPaymentQrAsync(current.Id, principal, qr); }
            catch (BookingException) { image = null; }
        }
        return new(history, own && await changes.CanChangeAsync(booking.Id, principal),
            current?.Id, image, current?.PaymentDeadlineAt, submitted);
    }

    public static string Version(IEnumerable<BookingChangeRequest> requests) => string.Join("|", requests.OrderBy(r => r.Id)
        .Select(r => $"{r.Id}:{r.Status}:{r.Payments.Count(p => p.Status is PaymentStatus.Submitted or PaymentStatus.Approved)}"));

    public async Task<List<ChangeRequestHistoryRow>> HistoryAsync(IReadOnlyCollection<int> bookingIds, IReadOnlyCollection<int>? requestIds = null)
    {
        var query = db.BookingChangeRequests.AsNoTracking().Where(r => bookingIds.Contains(r.BookingId));
        if (requestIds is not null) query = query.Where(r => requestIds.Contains(r.Id));
        var requests = await query.Include(r => r.RequestedByAccount).ThenInclude(a => a!.Person)
            .Include(r => r.Booking).ThenInclude(b => b.Homestay).Include(r => r.NewRoomType).Include(r => r.Payments)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToListAsync();
        var ids = requests.Select(r => r.Id.ToString()).ToList();
        var audits = await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "BookingChangeRequests"
            && a.Action == AuditAction.Insert && ids.Contains(a.RecordId)).OrderBy(a => a.Id).ToListAsync();
        var types = await db.RoomTypes.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name);
        var rows = new List<ChangeRequestHistoryRow>();
        foreach (var r in requests)
        {
            DateOnly? start = null, end = null; string? oldType = null;
            var audit = audits.FirstOrDefault(a => a.RecordId == r.Id.ToString());
            if (audit?.NewValues is { } json)
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Booking", out var b))
                    {
                        if (b.TryGetProperty("CheckIn", out var ci) && DateOnly.TryParse(ci.GetString(), out var d)) start = d;
                        if (b.TryGetProperty("CheckOut", out var co) && DateOnly.TryParse(co.GetString(), out d)) end = d;
                        if (b.TryGetProperty("RoomTypeId", out var rt) && rt.TryGetInt32(out var type)) oldType = types.GetValueOrDefault(type);
                    }
                }
                catch (JsonException) { /* Lịch sử cũ thiếu dữ liệu: hiển thị rõ là không có lịch cũ. */ }
            }
            rows.Add(new(r, start, end, oldType, r.NewRoomType?.Name ?? oldType));
        }
        return rows;
    }
}
