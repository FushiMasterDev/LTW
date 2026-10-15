using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Controllers;

/// <summary>
/// Đặt phòng và thanh toán của khách. Xem đơn được khi: là khách của đơn (đăng nhập), hoặc đã tra cứu đúng
/// mã đơn + SĐT trong phiên này (khách đặt qua điện thoại chưa có tài khoản).
/// </summary>
public partial class BookingsController : Controller
{
    private const string LookupSessionKey = "lookup_codes";
    private const int PageSize = 10;

    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;
    private readonly PricingService _pricing;
    private readonly RefundService _refunds;
    private readonly ReceiptReviewService _receipts;
    private readonly ReceiptFileStore _receiptFiles;
    private readonly VietQrService _qr;
    private readonly CustomerAccountService _customers;
    private readonly VoucherService _vouchers;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly TimeProvider _clock;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ChangeRequestUiService _changeUi;

    public BookingsController(
        ApplicationDbContext db,
        BookingService bookings,
        PricingService pricing,
        RefundService refunds,
        ReceiptReviewService receipts,
        ReceiptFileStore receiptFiles,
        VietQrService qr,
        CustomerAccountService customers,
        VoucherService vouchers,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        TimeProvider clock,
        IStringLocalizer<SharedResource> localizer,
        ChangeRequestUiService changeUi)
    {
        _db = db;
        _bookings = bookings;
        _pricing = pricing;
        _refunds = refunds;
        _receipts = receipts;
        _receiptFiles = receiptFiles;
        _qr = qr;
        _customers = customers;
        _vouchers = vouchers;
        _userManager = userManager;
        _env = env;
        _clock = clock;
        _localizer = localizer;
        _changeUi = changeUi;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    // ---------------------------------------------------------------- Báo giá (AJAX)

    /// <summary>Giá từng đêm + tiền cọc + số phòng trống cho form đặt phòng.</summary>
    [HttpGet]
    public async Task<IActionResult> Quote(int roomTypeId, DateOnly checkIn, DateOnly checkOut, int? voucherId)
    {
        if (checkIn < VietnamTime.Today(_clock))
        {
            return Json(new { ok = false, error = _localizer["Ngày nhận phòng không được ở quá khứ."].Value });
        }
        try
        {
            var quote = await _pricing.QuoteAsync(roomTypeId, checkIn, checkOut);
            var free = (await _bookings.FreeRoomIdsAsync(roomTypeId, checkIn, checkOut)).Count;
            var discount = voucherId is int id ? await PreviewDiscountAsync(id, roomTypeId, quote.Total) : 0;
            return Json(new
            {
                ok = true,
                freeRooms = free,
                total = quote.Total,
                deposit = quote.Deposit,
                discount,
                amountDue = quote.Deposit - discount,
                nights = quote.Nights.Select(n => new { date = n.Date.ToString("dd/MM (ddd)"), dayType = DayTypeLabel(n.DayType), price = n.Price }),
            });
        }
        catch (BookingException ex)
        {
            return Json(new { ok = false, error = _localizer[ex.Message].Value });
        }
    }

    /// <summary>Số tiền voucher sẽ giảm cho đơn này; 0 nếu voucher không thuộc khách đang đăng nhập hoặc không dùng được.</summary>
    private async Task<decimal> PreviewDiscountAsync(int voucherId, int roomTypeId, decimal total)
    {
        var personId = User.PersonId();
        var today = VietnamTime.Today(_clock);
        var voucher = await _db.Vouchers.AsNoTracking().SingleOrDefaultAsync(v => v.Id == voucherId
            && v.Customer.PersonId == personId && v.Status == VoucherStatus.Active && v.Balance > 0 && v.ExpiresOn >= today);
        var commissionRate = await _db.RoomTypes
            .Where(t => t.Id == roomTypeId)
            .SelectMany(t => t.Homestay.Contracts)
            .Where(c => c.Status == ContractStatus.Active && c.StartDate <= today && c.EndDate >= today)
            .Select(c => (decimal?)c.CommissionRate)
            .SingleOrDefaultAsync();
        return voucher is null || commissionRate is null ? 0 : _vouchers.PreviewDiscount(voucher, total, commissionRate.Value);
    }

    private string DayTypeLabel(DayType type) => type switch
    {
        DayType.Weekend => _localizer["Cuối tuần"],
        DayType.Holiday => _localizer["Ngày lễ"],
        _ => _localizer["Ngày thường"],
    };

    // ---------------------------------------------------------------- Đặt phòng

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int roomTypeId, DateOnly? checkIn, DateOnly? checkOut, int guests = 2)
    {
        if (User.IsActiveEmployee())
        {
            TempData["Error"] = _localizer["Tài khoản nhân viên không đặt phòng ở cổng khách hàng."].Value;
            return RedirectToAction(nameof(HomestaysController.Index), "Homestays");
        }

        var model = new BookingCreateViewModel { RoomTypeId = roomTypeId, CheckIn = checkIn, CheckOut = checkOut, Guests = guests };
        if (!await FillDisplayAsync(model))
        {
            return NotFound();
        }

        var personId = User.PersonId();
        var person = await _db.PersonProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == personId);
        model.ContactName = person?.FullName ?? string.Empty;
        model.ContactPhone = person is null ? string.Empty : PhoneNumberNormalizer.ToNational(person.PhoneNumber);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        if (User.IsActiveEmployee())
        {
            return Forbid();
        }
        if (!ModelState.IsValid)
        {
            await FillDisplayAsync(model);
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        var customer = user is null ? null : await _customers.EnsureCustomerAsync(user);
        if (customer is null)
        {
            return Forbid();
        }

        try
        {
            var booking = await _bookings.CreateAsync(new CreateBookingRequest(
                customer.Id, model.RoomTypeId, model.CheckIn!.Value, model.CheckOut!.Value, model.Guests,
                model.ContactName, PhoneNumberNormalizer.ToE164(model.ContactPhone)!, model.SpecialRequests,
                SourceChannel.Web, new Actor(ActorType.Customer, user!.Id), model.VoucherId));
            return RedirectToAction(nameof(Details), new { id = booking.Code });
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError(string.Empty, _localizer[ex.Message]);
            _db.ChangeTracker.Clear();
            await FillDisplayAsync(model);
            return View(model);
        }
    }

    private async Task<bool> FillDisplayAsync(BookingCreateViewModel model)
    {
        var today = VietnamTime.Today(_clock);
        var type = await _db.RoomTypes.AsNoTracking()
            .Where(t => t.Id == model.RoomTypeId && t.IsActive && t.Homestay.Status == HomestayStatus.Active)
            .Select(t => new { t.Name, t.MaxGuests, t.HomestayId, HomestayName = t.Homestay.Name })
            .SingleOrDefaultAsync();
        if (type is null)
        {
            return false;
        }
        model.RoomTypeName = type.Name;
        model.MaxGuests = type.MaxGuests;
        model.HomestayId = type.HomestayId;
        model.HomestayName = type.HomestayName;
        var personId = User.PersonId();
        var customerId = await _db.Customers.Where(c => c.PersonId == personId).Select(c => (int?)c.Id).SingleOrDefaultAsync();
        model.Vouchers = customerId is int cid
            ? (await _vouchers.UsableAsync(cid)).Select(v => new VoucherOption(v.Id, v.Code, v.Balance, v.ExpiresOn)).ToList()
            : [];
        if (model.CheckIn >= today && model.CheckOut > model.CheckIn)
        {
            try
            {
                model.Quote = await _pricing.QuoteAsync(model.RoomTypeId, model.CheckIn!.Value, model.CheckOut!.Value);
            }
            catch (BookingException)
            {
                model.Quote = null;
            }
        }
        return true;
    }

    // ---------------------------------------------------------------- Xem đơn, thanh toán

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var booking = await _db.Bookings.AsNoTracking()
            .Include(b => b.Homestay).ThenInclude(h => h.Area)
            .Include(b => b.RoomType)
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .Include(b => b.Nights)
            .Include(b => b.Payments)
            .Include(b => b.Histories)
            .Include(b => b.CancellationPolicy).ThenInclude(p => p.Rules)
            .Include(b => b.Voucher)
            .AsSplitQuery()
            .SingleOrDefaultAsync(b => b.Code == id);
        if (booking is null || !CanAccess(booking))
        {
            return NotFound();
        }

        var awaiting = booking.Status == BookingStatus.AwaitingPayment && booking.HoldExpiresAt > UtcNow;
        var cancelQuote = await _refunds.QuoteCancellationAsync(booking);
        return View(new BookingDetailsViewModel
        {
            Booking = booking,
            Bank = _qr.Account,
            QrImageUrl = awaiting ? _qr.BuildImageUrl(booking.AmountDue, booking.Code) : null,
            CanUploadReceipt = awaiting,
            CancelQuote = cancelQuote,
            CanCancel = BookingService.CanTransition(booking.Status, BookingStatus.Cancelled) && cancelQuote.DaysBefore >= 0,
            ShowAddress = booking.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed,
            ChangeRequests = await _changeUi.CustomerAsync(booking, User, _qr, booking.Customer.PersonId == User.PersonId()),
            PendingReceipt = booking.Payments.FirstOrDefault(p => p.Type == PaymentType.Deposit && p.Method == PaymentMethod.ManualReceipt && p.Status == PaymentStatus.Submitted),
            LastRejectedReceipt = booking.Payments.Where(p => p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Rejected).MaxBy(p => p.ReviewedAt),
            IsOwnAccount = booking.Customer.PersonId == User.PersonId(),
            ShowDevSimulator = _env.IsDevelopment(),
        });
    }

    /// <summary>Trang thanh toán hỏi mỗi 5 giây; trạng thái đổi (tiền về, hết hạn) thì trang tự tải lại.</summary>
    [HttpGet]
    public async Task<IActionResult> Status(string id)
    {
        var booking = await _db.Bookings.AsNoTracking()
            .Include(b => b.Customer)
            .SingleOrDefaultAsync(b => b.Code == id);
        if (booking is null || !CanAccess(booking))
        {
            return NotFound();
        }
        var requests = await _db.BookingChangeRequests.AsNoTracking().Include(r => r.Payments)
            .Where(r => r.BookingId == booking.Id).ToListAsync();
        return Json(new { status = booking.Status.ToString(), holdExpiresAt = DateTime.SpecifyKind(booking.HoldExpiresAt, DateTimeKind.Utc),
            changeVersion = ChangeRequestUiService.Version(requests) });
    }

    [HttpPost]
    [RequestSizeLimit(ReceiptFileStore.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadReceipt(string id, IFormFile? receipt, string? transferContent)
    {
        var booking = await _db.Bookings.AsNoTracking().Include(b => b.Customer).SingleOrDefaultAsync(b => b.Code == id);
        if (booking is null || !CanAccess(booking))
        {
            return NotFound();
        }
        if (receipt is null)
        {
            TempData["Error"] = _localizer["Vui lòng chọn ảnh bill."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var fileName = await _receiptFiles.SaveAsync(receipt, booking.Code);
            await _receipts.SubmitAsync(booking.Id, fileName, transferContent, CustomerActor());
            TempData["Success"] = _localizer["Đã gửi ảnh bill. Fuji sẽ kiểm tra và xác nhận cho bạn."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Ảnh bill: khách của đơn hoặc kế toán/quản lý.</summary>
    [HttpGet]
    public async Task<IActionResult> Receipt(int paymentId)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(p => p.Booking).ThenInclude(b => b.Customer)
            .SingleOrDefaultAsync(p => p.Id == paymentId);
        var reviewer = User.EmployeePosition() is EmployeePosition.Accountant or EmployeePosition.Manager or EmployeePosition.Administrator;
        if (payment is null || !(reviewer || CanAccess(payment.Booking)))
        {
            return NotFound();
        }
        var path = _receiptFiles.Resolve(payment.ReceiptImageUrl);
        return path is null ? NotFound() : PhysicalFile(path, ReceiptFileStore.ContentTypeOf(path));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(string id, string? reason)
    {
        var booking = await _db.Bookings.Include(b => b.Customer).SingleOrDefaultAsync(b => b.Code == id);
        if (booking is null || !CanAccess(booking))
        {
            return NotFound();
        }
        if (booking.CheckIn < VietnamTime.Today(_clock))
        {
            TempData["Error"] = _localizer["Đã qua ngày nhận phòng, vui lòng liên hệ Fuji."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var note = string.IsNullOrWhiteSpace(reason) ? "Khách tự hủy." : $"Khách tự hủy: {reason.Trim()}";
            await _bookings.ChangeStatusAsync(booking, BookingStatus.Cancelled, CustomerActor(), note);
            TempData["Success"] = booking.RefundAmount > 0
                ? _localizer["Đã hủy đơn. Fuji sẽ hoàn {0:N0}đ trong 3–5 ngày làm việc.", booking.RefundAmount!].Value
                : _localizer["Đã hủy đơn."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = _localizer[ex.Message].Value;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = _localizer["Trạng thái đơn vừa thay đổi, vui lòng xem lại."].Value;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------------------------------------------------------- Danh sách, tra cứu

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine(int? page)
    {
        var personId = User.PersonId();
        var query = _db.Bookings.AsNoTracking()
            .Where(b => b.Customer.PersonId == personId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new MyBookingRow(b.Code, b.Homestay.Name, b.RoomType.Name, b.CheckIn, b.CheckOut, b.TotalAmount, b.Status, b.CreatedAt));
        var customerId = await _db.Customers.Where(c => c.PersonId == personId).Select(c => (int?)c.Id).SingleOrDefaultAsync();
        ViewBag.Vouchers = customerId is int cid
            ? (await _vouchers.UsableAsync(cid)).Select(v => new VoucherOption(v.Id, v.Code, v.Balance, v.ExpiresOn)).ToList()
            : new List<VoucherOption>();
        return View(await PaginatedList<MyBookingRow>.CreateAsync(query, page, PageSize));
    }

    [HttpGet]
    public IActionResult Lookup() => View(new BookingLookupViewModel());

    [HttpPost]
    public async Task<IActionResult> Lookup(BookingLookupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.Code.Trim().ToUpperInvariant();
        var phone = PhoneNumberNormalizer.ToE164(model.PhoneNumber);
        // Khớp SĐT liên hệ của đơn hoặc SĐT của khách đặt; sai thì báo chung, không lộ đơn có tồn tại hay không
        var found = await _db.Bookings.AnyAsync(b => b.Code == code
            && (b.ContactPhone == phone || b.Customer.Person.PhoneNumber == phone));
        if (!found)
        {
            ModelState.AddModelError(string.Empty, _localizer["Không tìm thấy đơn khớp với mã đơn và số điện thoại."]);
            return View(model);
        }

        var codes = LookupCodes();
        codes.Add(code);
        HttpContext.Session.SetString(LookupSessionKey, JsonSerializer.Serialize(codes));
        return RedirectToAction(nameof(Details), new { id = code });
    }

    private HashSet<string> LookupCodes()
    {
        var json = HttpContext.Session.GetString(LookupSessionKey);
        return json is null ? [] : JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
    }

    private bool CanAccess(Booking booking) =>
        (User.PersonId() is int personId && booking.Customer.PersonId == personId) || LookupCodes().Contains(booking.Code);

    private Actor CustomerActor() => new(ActorType.Customer, _userManager.GetUserId(User));
}
