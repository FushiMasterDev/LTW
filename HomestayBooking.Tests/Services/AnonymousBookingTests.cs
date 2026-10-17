using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using HomestayBooking.Web;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;

namespace HomestayBooking.Tests.Services;

/// <summary>FR-03: đặt phòng không cần đăng nhập qua đúng BookingsController, SQLite có ràng buộc thật.</summary>
public sealed class AnonymousBookingTests
{
    private static (BookingsController Controller, GuestCustomerService Guests) Controller(BackofficeTestHost h, ClaimsPrincipal user)
    {
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e => e.EnvironmentName).Returns("Testing"); env.SetupGet(e => e.ContentRootPath).Returns(h.P.DirectoryPath);
        var http = new DefaultHttpContext { User = user };
        http.Features.Set<ISessionFeature>(new SessionFeature { Session = new TestSession() });
        var controller = new BookingsController(h.Db, h.H.Bookings(), h.H.Pricing(), h.H.Refunds(), h.H.Receipts(), new ReceiptFileStore(env.Object),
            new(Options.Create(h.H.Bank)), new(h.Db, h.P.Users, new(h.Db, new HttpContextAccessor())), h.H.Vouchers(), h.P.Users, env.Object, h.H.Clock,
            new KeyLocalizer(), new(h.Db, h.H.Changes(), new EphemeralDataProtectionProvider(), h.H.Clock, Options.Create(h.H.Options)))
        { ControllerContext = new() { HttpContext = http } };
        return (controller, h.Guests());
    }

    private static ClaimsPrincipal Anonymous => new(new ClaimsIdentity());

    private static BookingCreateViewModel Model(BackofficeTestHost h, int from, int nights = 2, string phone = "0977666001", int? voucher = null) => new()
    {
        RoomTypeId = h.H.RoomType.Id, CheckIn = h.Today.AddDays(from), CheckOut = h.Today.AddDays(from + nights), Guests = 1,
        ContactName = "Khách vãng lai", ContactPhone = phone, VoucherId = voucher,
    };

    [Fact]
    public async Task AnonymousGuestBooksAndCanOpenDetailsInSameSession()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var (controller, guests) = Controller(h, Anonymous);
        Assert.IsType<ViewResult>(await controller.Create(h.H.RoomType.Id, h.Today.AddDays(5), h.Today.AddDays(7)));
        var redirect = Assert.IsType<RedirectToActionResult>(await controller.Create(Model(h, 5), guests));
        var code = Assert.IsType<string>(redirect.RouteValues!["id"]);
        var booking = await h.Db.Bookings.AsNoTracking().Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account).SingleAsync(b => b.Code == code);
        Assert.Equal("+84977666001", booking.Customer.Person.PhoneNumber); Assert.Null(booking.Customer.Person.Account);
        Assert.Equal(SourceChannel.Web, booking.SourceChannel); Assert.Null(booking.CreatedByEmployeeId); Assert.Equal(0, booking.DiscountAmount);
        // Mã đơn vào lookup session như tra cứu FR-04: xem được chi tiết mà không cần đăng nhập.
        Assert.IsType<ViewResult>(await controller.Details(code));
        // Phiên khác (chưa tra cứu) không xem được.
        Assert.IsType<NotFoundResult>(await Controller(h, Anonymous).Controller.Details(code));
    }

    [Fact]
    public async Task AnonymousGuestCannotUseVoucher()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var voucher = await h.VoucherAsync();
        var (controller, guests) = Controller(h, Anonymous);
        var view = Assert.IsType<ViewResult>(await controller.Create(Model(h, 5, voucher: voucher.Id), guests));
        Assert.False(view.ViewData.ModelState.IsValid);
        Assert.False(await h.Db.Bookings.AnyAsync(b => b.ContactPhone == "+84977666001"));
    }

    [Fact]
    public async Task AnonymousGuestStillLimitedToThreeAwaitingBookings()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var (controller, guests) = Controller(h, Anonymous);
        for (var i = 0; i < 3; i++) Assert.IsType<RedirectToActionResult>(await controller.Create(Model(h, 5 + i * 3, 1), guests));
        var view = Assert.IsType<ViewResult>(await controller.Create(Model(h, 20, 1), guests));
        Assert.False(view.ViewData.ModelState.IsValid);
        Assert.Equal(3, await h.Db.Bookings.CountAsync(b => b.ContactPhone == "+84977666001"));
    }

    [Fact]
    public async Task SignedInCustomerKeepsOwnAccountFlow()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var account = await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Customer.PersonId);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account.Id),
            new Claim(ClaimTypes.Role, "Customer"), new Claim(AppClaimTypes.PersonId, h.H.Customer.PersonId.ToString())], "test"));
        var (controller, guests) = Controller(h, user);
        // SĐT liên hệ khác người đặt: đơn vẫn thuộc khách đang đăng nhập, không tạo khách mới.
        var redirect = Assert.IsType<RedirectToActionResult>(await controller.Create(Model(h, 5, phone: "0977666002"), guests));
        var booking = await h.Db.Bookings.AsNoTracking().SingleAsync(b => b.Code == (string)redirect.RouteValues!["id"]!);
        Assert.Equal(h.H.Customer.Id, booking.CustomerId);
        Assert.False(await h.Db.PersonProfiles.AnyAsync(p => p.PhoneNumber == "+84977666002"));
    }

    private sealed class KeyLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = [];
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
    }
}
