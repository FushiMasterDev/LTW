using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers;

public partial class BookingsController
{
    private Task<Booking?> OwnChangeBookingAsync(string id) => _db.Bookings.AsNoTracking()
        .Include(b => b.Homestay).Include(b => b.RoomType).Include(b => b.Customer)
        .SingleOrDefaultAsync(b => b.Code == id && b.Customer.PersonId == User.PersonId());

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public async Task<IActionResult> Change(string id)
    {
        var booking = await OwnChangeBookingAsync(id);
        if (booking is null) return NotFound();
        var form = new ChangeRequestForm { NewCheckIn = booking.CheckIn, NewCheckOut = booking.CheckOut };
        await _changeUi.PrepareAsync(form, booking, false);
        return View(form);
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    public Task<IActionResult> QuoteChange(string id, ChangeRequestForm form) => ChangeFormAsync(id, form, false);

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    public Task<IActionResult> SubmitChange(string id, ChangeRequestForm form) => ChangeFormAsync(id, form, true);

    private async Task<IActionResult> ChangeFormAsync(string id, ChangeRequestForm form, bool submit)
    {
        var booking = await OwnChangeBookingAsync(id);
        if (booking is null) return NotFound();
        await _changeUi.PrepareAsync(form, booking, false);
        if (ModelState.IsValid)
        {
            try
            {
                if (form.RequestType == ChangeRequestType.Cancel) throw new BookingException("Vui lòng dùng nút hủy đơn");
                if (submit)
                {
                    await _changeUi.SubmitAsync(form, User);
                    TempData["Success"] = _localizer["Đã gửi yêu cầu đổi, vui lòng chờ quản lý duyệt."].Value;
                    return RedirectToAction(nameof(Details), new { id });
                }
                await _changeUi.QuoteAsync(form, User);
                ModelState.Remove(nameof(form.PreviewToken));
            }
            catch (BookingException ex) { ModelState.AddModelError(string.Empty, _localizer[ex.ResourceKey, ex.Arguments]); }
        }
        return View("Change", form);
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [RequestSizeLimit(ReceiptFileStore.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadChangeReceipt(string id, int requestId, IFormFile? receipt, string? transferContent)
    {
        var booking = await OwnChangeBookingAsync(id);
        if (booking is null || !await _db.BookingChangeRequests.AnyAsync(r => r.Id == requestId && r.BookingId == booking.Id)) return NotFound();
        try
        {
            if (receipt is null) throw new BookingException("Vui lòng chọn ảnh bill.");
            if (!await _db.BookingChangeRequests.AnyAsync(r => r.Id == requestId && r.Status == ChangeRequestStatus.AwaitingPayment
                && r.PaymentDeadlineAt >= UtcNow && !r.Payments.Any(p => p.Status != PaymentStatus.Rejected)))
                throw new BookingException("Yêu cầu đã đóng hoặc đã có khoản nộp.");
            var path = await _receiptFiles.SaveAsync(receipt, $"CR{requestId}-{booking.Code}");
            await _receipts.SubmitChangeRequestAsync(requestId, path, transferContent, User);
            TempData["Success"] = _localizer["Đã gửi cọc bổ sung, vui lòng chờ kế toán xác nhận."].Value;
        }
        catch (BookingException ex) { TempData["Error"] = _localizer[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
