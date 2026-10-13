using System.Reflection;
using System.Security.Claims;
using HomestayBooking.Tests.Data;
using HomestayBooking.Web;
using HomestayBooking.Web.Areas.Owner.Controllers;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Moq;
using OwnerBookingsController = HomestayBooking.Web.Areas.Owner.Controllers.BookingsController;

namespace HomestayBooking.Tests.Services;

/// <summary>Owner xử lý đơn: xác nhận (kèm đổi phòng Q8), từ chối, nhận phòng, hoàn thành, không đến; chặn đơn của người khác.</summary>
public sealed class OwnerBookingTests : IDisposable
{
    private readonly BookingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    // "Bây giờ" của host là 10/11/2026
    private static readonly DateOnly CheckIn = new(2026, 11, 19);
    private static readonly DateOnly CheckOut = new(2026, 11, 22);

    private async Task<Booking> DepositedAsync()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));
        await bookings.ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);
        return booking;
    }

    private async Task<Booking> ConfirmedAsync()
    {
        var booking = await DepositedAsync();
        await _host.Bookings().ConfirmAsync(booking, null, new Actor(ActorType.Owner));
        return booking;
    }

    private OwnerBookingsController Controller(int personId)
    {
        var localizer = new Mock<IStringLocalizer<SharedResource>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
        localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string k, object[] a) => new LocalizedString(k, string.Format(k, a)));
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppClaimTypes.PersonId, personId.ToString())], "test")),
        };
        return new OwnerBookingsController(_host.Db, _host.Bookings(), localizer.Object, _host.Clock)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
        };
    }

    private int OwnerPersonId => _host.Db.Owners.Where(o => o.Homestays.Any(h => h.Id == _host.Homestay.Id)).Select(o => o.PersonId).Single();

    private async Task<BookingStatus> StatusOf(int id) =>
        await _host.NewContext().Bookings.Where(b => b.Id == id).Select(b => b.Status).SingleAsync();

    // ---------------------------------------------------------------- Service

    [Fact]
    public async Task Confirm_WithRoomSwap_MovesNightsAndRecordsHistory()
    {
        var booking = await DepositedAsync();
        var oldRoom = booking.RoomId;
        var otherRoom = (await _host.Bookings().FreeRoomIdsAsync(_host.RoomType.Id, CheckIn, CheckOut)).Single();

        await _host.Bookings().ConfirmAsync(booking, otherRoom, new Actor(ActorType.Owner));

        var db = _host.NewContext();
        var saved = await db.Bookings.SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        Assert.Equal(otherRoom, saved.RoomId);
        Assert.Equal(3, await db.RoomNights.CountAsync(n => n.BookingId == booking.Id && n.RoomId == otherRoom));
        Assert.False(await db.RoomNights.AnyAsync(n => n.RoomId == oldRoom));
        Assert.Contains(await db.BookingStatusHistories.Where(h => h.BookingId == booking.Id).Select(h => h.Note).ToListAsync(),
            n => n != null && n.StartsWith("Đổi phòng"));
    }

    [Fact]
    public async Task Confirm_SwapToOccupiedRoom_Throws()
    {
        var booking = await DepositedAsync();
        var other = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut)); // giữ phòng còn lại

        await Assert.ThrowsAsync<BookingException>(() => _host.Bookings().ConfirmAsync(booking, other.RoomId, new Actor(ActorType.Owner)));
        Assert.Equal(BookingStatus.Deposited, await StatusOf(booking.Id));
    }

    [Fact]
    public async Task Confirm_NotDeposited_Throws()
    {
        var booking = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut));

        await Assert.ThrowsAsync<BookingException>(() => _host.Bookings().ConfirmAsync(booking, null, new Actor(ActorType.Owner)));
    }

    [Fact]
    public async Task CheckIn_BeforeCheckInDay_Throws()
    {
        var booking = await ConfirmedAsync();

        var ex = await Assert.ThrowsAsync<BookingException>(() =>
            _host.Bookings().ChangeStatusAsync(booking, BookingStatus.CheckedIn, new Actor(ActorType.Owner)));
        Assert.Equal("Chưa đến ngày nhận phòng.", ex.Message);
    }

    [Theory]
    [InlineData(0)]               // 10/11 10:00 — trước ngày nhận phòng
    [InlineData(9 * 24)]          // 19/11 10:00 — đúng ngày nhận phòng
    [InlineData(9 * 24 + 13)]     // 19/11 23:00 — vẫn là ngày nhận phòng theo giờ Việt Nam
    public async Task NoShow_UpToCheckInDay_Throws(int hoursFromNow)
    {
        var booking = await ConfirmedAsync();
        _host.Clock.Advance(TimeSpan.FromHours(hoursFromNow));

        var ex = await Assert.ThrowsAsync<BookingException>(() =>
            _host.Bookings().ChangeStatusAsync(booking, BookingStatus.NoShow, new Actor(ActorType.Owner)));
        Assert.Equal("Chỉ đánh dấu khách không đến từ ngày sau ngày nhận phòng.", ex.Message);
        Assert.Equal(BookingStatus.Confirmed, await StatusOf(booking.Id));
    }

    [Fact]
    public async Task NoShow_FromDayAfterCheckIn_KeepsDepositAndReleasesRoom()
    {
        var booking = await ConfirmedAsync();
        _host.Clock.Advance(TimeSpan.FromHours(9 * 24 + 14)); // 20/11 00:00 giờ Việt Nam

        await _host.Bookings().ChangeStatusAsync(booking, BookingStatus.NoShow, new Actor(ActorType.Owner));

        Assert.Equal(BookingStatus.NoShow, await StatusOf(booking.Id));
        Assert.Equal(0m, booking.RefundAmount);
        Assert.False(await _host.Db.RoomNights.AnyAsync(n => n.BookingId == booking.Id));
    }

    [Fact]
    public async Task NoShow_FromWrongStatus_Throws()
    {
        // Đơn mới đặt cọc (chưa xác nhận) không được chuyển sang Không đến dù đã qua ngày nhận phòng
        var booking = await DepositedAsync();
        _host.Clock.Advance(TimeSpan.FromDays(10));

        await Assert.ThrowsAsync<BookingException>(() =>
            _host.Bookings().ChangeStatusAsync(booking, BookingStatus.NoShow, new Actor(ActorType.Owner)));
    }

    [Fact]
    public async Task Complete_BeforeCheckIn_Throws()
    {
        var booking = await ConfirmedAsync();

        await Assert.ThrowsAsync<BookingException>(() =>
            _host.Bookings().ChangeStatusAsync(booking, BookingStatus.Completed, new Actor(ActorType.Owner)));
    }

    // ---------------------------------------------------------------- Controller

    [Fact]
    public async Task Controller_OwnerRunsFullFlow()
    {
        var booking = await DepositedAsync();
        var controller = Controller(OwnerPersonId);

        Assert.IsType<RedirectToActionResult>(await controller.Confirm(booking.Id, null));
        Assert.Equal(BookingStatus.Confirmed, await StatusOf(booking.Id));

        _host.Clock.Advance(TimeSpan.FromDays(9));
        _host.Db.ChangeTracker.Clear();
        await controller.CheckIn(booking.Id);
        _host.Db.ChangeTracker.Clear();
        await controller.Complete(booking.Id);

        Assert.Equal(BookingStatus.Completed, await StatusOf(booking.Id));
        Assert.True(controller.TempData.ContainsKey("Success"));
    }

    [Fact]
    public async Task Controller_Index_OrdersPendingThenUpcomingThenRecentClosed()
    {
        var bookings = _host.Bookings();
        async Task<Booking> Deposited(int day)
        {
            var b = await bookings.CreateAsync(_host.Request(new DateOnly(2026, 11, day), new DateOnly(2026, 11, day + 1)));
            await bookings.ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
            return b;
        }
        var owner = new Actor(ActorType.Owner);
        var closedEarly = await Deposited(20);
        await bookings.ChangeStatusAsync(closedEarly, BookingStatus.Rejected, owner, "Sửa phòng");
        var closedLate = await Deposited(28);
        await bookings.ChangeStatusAsync(closedLate, BookingStatus.Rejected, owner, "Sửa phòng");
        var confirmedLate = await Deposited(25);
        await bookings.ConfirmAsync(confirmedLate, null, owner);
        var confirmedSoon = await Deposited(15);
        await bookings.ConfirmAsync(confirmedSoon, null, owner);
        var pending = await Deposited(27);

        var view = Assert.IsType<ViewResult>(await Controller(OwnerPersonId).Index(null, null, null));
        var ids = Assert.IsAssignableFrom<IEnumerable<OwnerBookingRow>>(view.Model).Select(r => r.Id).ToList();

        Assert.Equal([pending.Id, confirmedSoon.Id, confirmedLate.Id, closedLate.Id, closedEarly.Id], ids);
    }

    [Fact]
    public async Task Controller_RejectWithoutReason_DoesNothing()
    {
        var booking = await DepositedAsync();
        var controller = Controller(OwnerPersonId);

        await controller.Reject(booking.Id, "   ");

        Assert.Equal(BookingStatus.Deposited, await StatusOf(booking.Id));
        Assert.True(controller.TempData.ContainsKey("Error"));
    }

    [Fact]
    public async Task Controller_RejectWithReason_RefundsFullDeposit()
    {
        var booking = await DepositedAsync();

        await Controller(OwnerPersonId).Reject(booking.Id, "Phòng đang sửa");

        var saved = await _host.NewContext().Bookings.SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Rejected, saved.Status);
        Assert.Equal("Phòng đang sửa", saved.CancelReason);
        Assert.Equal(saved.DepositAmount - saved.DiscountAmount, saved.RefundAmount);
    }

    [Fact]
    public async Task Controller_InvalidTransition_ShowsErrorAndKeepsStatus()
    {
        var booking = await DepositedAsync();
        var controller = Controller(OwnerPersonId);

        await controller.Complete(booking.Id);

        Assert.Equal(BookingStatus.Deposited, await StatusOf(booking.Id));
        Assert.True(controller.TempData.ContainsKey("Error"));
    }

    [Fact]
    public async Task Controller_OtherOwner_GetsNotFoundOnEveryAction()
    {
        var booking = await DepositedAsync();
        var otherPerson = new PersonProfile { FullName = "Chủ khác", PhoneNumber = "+84912000099" };
        _host.Db.Owners.Add(new Owner { Person = otherPerson });
        await _host.Db.SaveChangesAsync();
        var controller = Controller(otherPerson.Id);

        Assert.IsType<NotFoundResult>(await controller.Details(booking.Id));
        Assert.IsType<NotFoundResult>(await controller.Confirm(booking.Id, null));
        Assert.IsType<NotFoundResult>(await controller.Reject(booking.Id, "x"));
        Assert.IsType<NotFoundResult>(await controller.Reject(booking.Id, null));
        Assert.IsType<NotFoundResult>(await controller.CheckIn(booking.Id));
        Assert.IsType<NotFoundResult>(await controller.Complete(booking.Id));
        Assert.IsType<NotFoundResult>(await controller.NoShow(booking.Id));
        Assert.Equal(BookingStatus.Deposited, await StatusOf(booking.Id));
    }

    [Fact]
    public async Task Controller_BookingNotYetDeposited_IsHiddenFromOwner()
    {
        // Owner chỉ thấy đơn từ khi đã cọc (database v1 mục 9)
        var booking = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut));

        Assert.IsType<NotFoundResult>(await Controller(OwnerPersonId).Details(booking.Id));
    }

    [Fact]
    public void Controller_RequiresApprovedOwnerPolicy_AndPostForChanges()
    {
        // Policy ApprovedOwner (Program.cs) = role Owner + claim owner_approved; khách vãng lai bị chặn trước khi vào action
        var type = typeof(OwnerBookingsController);
        Assert.Equal(AppPolicies.ApprovedOwner, type.GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(methods, m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null);
        foreach (var name in new[] { "Confirm", "Reject", "CheckIn", "Complete", "NoShow" })
        {
            Assert.NotNull(type.GetMethod(name)!.GetCustomAttribute<HttpPostAttribute>());
        }
    }

    // ---------------------------------------------------------------- Concurrency

    [Fact]
    public async Task Controller_ConfirmOnStaleRead_DoesNotOverwriteNewerStatus()
    {
        // Context A (_host.Db, dùng bởi controller) đang giữ bản đọc cũ: đơn còn Đã đặt cọc
        var booking = await DepositedAsync();
        // Context B (request khác): khách hủy đơn và lưu trước
        await using (var other = _host.NewContext())
        {
            var fresh = await other.Bookings.SingleAsync(b => b.Id == booking.Id);
            await _host.Bookings(other).ChangeStatusAsync(fresh, BookingStatus.Cancelled, new Actor(ActorType.Customer), "Khách đổi kế hoạch");
        }

        var controller = Controller(OwnerPersonId);
        var result = await controller.Confirm(booking.Id, null);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Đơn vừa được cập nhật bởi thao tác khác, vui lòng xem lại.", controller.TempData["Error"]);
        await using var check = _host.NewContext();
        Assert.Equal(BookingStatus.Cancelled, await check.Bookings.Where(b => b.Id == booking.Id).Select(b => b.Status).SingleAsync());
        Assert.False(await check.BookingStatusHistories.AnyAsync(h => h.BookingId == booking.Id && h.ToStatus == BookingStatus.Confirmed));
    }

    [Fact]
    public async Task Confirm_SwapRacesWithNewBookingForSameRoom_OnlyOneHoldsTheRoom()
    {
        var booking = await DepositedAsync();
        var target = (await _host.Bookings().FreeRoomIdsAsync(_host.RoomType.Id, CheckIn, CheckOut)).Single();
        Booking? rival = null;

        // Ngay lúc context A lưu việc đổi phòng (đã kiểm tra phòng trống xong), request khác đặt mất đúng phòng đó
        var interceptor = new BeforeFirstSave(async () =>
        {
            await using var other = _host.NewContext();
            rival = await _host.Bookings(other).CreateAsync(_host.Request(CheckIn, CheckOut));
        });
        await using var contextA = new SqliteTestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_host.Db.Database.GetDbConnection())
            .AddInterceptors(interceptor)
            .Options);
        var stale = await contextA.Bookings.SingleAsync(b => b.Id == booking.Id);

        var ex = await Assert.ThrowsAsync<BookingException>(() => _host.Bookings(contextA).ConfirmAsync(stale, target, new Actor(ActorType.Owner)));

        Assert.Equal("Phòng được chọn vừa có người đặt, vui lòng chọn phòng khác.", ex.Message);
        Assert.NotNull(rival);
        Assert.Equal(target, rival!.RoomId);
        await using var check = _host.NewContext();
        var saved = await check.Bookings.SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Deposited, saved.Status);
        Assert.Equal(booking.RoomId, saved.RoomId);
        // Phòng đích chỉ thuộc đơn kia; đơn gốc vẫn giữ đủ 3 đêm ở phòng cũ
        Assert.All(await check.RoomNights.Where(n => n.RoomId == target).ToListAsync(), n => Assert.Equal(rival.Id, n.BookingId));
        Assert.Equal(3, await check.RoomNights.CountAsync(n => n.BookingId == booking.Id && n.RoomId == booking.RoomId));
    }

    /// <summary>Chạy một việc đúng một lần, ngay trước lần SaveChanges đầu tiên của context.</summary>
    private sealed class BeforeFirstSave(Func<Task> action) : SaveChangesInterceptor
    {
        private bool _done;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_done)
            {
                _done = true;
                await action();
            }
            return result;
        }
    }
}
