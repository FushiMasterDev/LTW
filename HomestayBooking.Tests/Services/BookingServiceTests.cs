using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class BookingServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static readonly DateOnly CheckIn = new(2026, 11, 19);
    private static readonly DateOnly CheckOut = new(2026, 11, 22);

    [Fact]
    public async Task Create_HoldsRoomImmediatelyFor15Minutes()
    {
        var booking = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut));

        Assert.Equal("FJ261110-001", booking.Code);
        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.Equal(_host.Clock.Now.UtcDateTime.AddMinutes(15), booking.HoldExpiresAt);
        // D4: phòng bị giữ ngay, mỗi đêm một dòng RoomNights
        Assert.Equal(3, await _host.Db.RoomNights.CountAsync(n => n.BookingId == booking.Id));
        // Snapshot tiền: giá từng đêm, cọc 30%, hoa hồng theo hợp đồng, chính sách chung
        Assert.Equal(1_700_000m, booking.TotalAmount);
        Assert.Equal(510_000m, booking.DepositAmount);
        Assert.Equal(0.12m, booking.CommissionRate);
        Assert.Equal(_host.GeneralPolicy.Id, booking.CancellationPolicyId);
        Assert.Equal(3, await _host.Db.BookingNights.CountAsync(n => n.BookingId == booking.Id));
        Assert.Single(await _host.Db.BookingStatusHistories.Where(h => h.BookingId == booking.Id).ToListAsync());
    }

    [Fact]
    public async Task Create_UntilRoomTypeFull_ThenRejects()
    {
        var bookings = _host.Bookings();
        var first = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));
        var second = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));

        // Loại phòng có 2 phòng: hai đơn được gán hai phòng khác nhau, đơn thứ ba hết phòng
        Assert.NotEqual(first.RoomId, second.RoomId);
        var ex = await Assert.ThrowsAsync<BookingException>(() => bookings.CreateAsync(_host.Request(CheckIn, CheckOut)));
        Assert.Contains("hết phòng", ex.Message);
    }

    [Fact]
    public async Task Create_AnotherRequestHoldsFirstRoom_GetsNextRoom()
    {
        // Request khác (context khác) giữ phòng đầu tiên; request này được gán phòng còn lại.
        // Nhánh "bị giữ giữa lúc kiểm tra và lúc ghi" (catch trong CreateAsync) khó tái hiện ổn định trong unit test.
        var free = await _host.Bookings().FreeRoomIdsAsync(_host.RoomType.Id, CheckIn, CheckOut);
        await using (var other = _host.NewContext())
        {
            await _host.Bookings(other).CreateAsync(_host.Request(CheckIn, CheckOut));
        }

        var booking = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut));

        Assert.Equal(free[1], booking.RoomId);
    }

    [Fact]
    public async Task Create_CheckInInPast_Throws()
    {
        await Assert.ThrowsAsync<BookingException>(() => _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 9), new(2026, 11, 11))));
    }

    [Fact]
    public async Task Create_TooManyGuests_Throws()
    {
        await Assert.ThrowsAsync<BookingException>(() => _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut, guests: 3)));
    }

    [Fact]
    public async Task Create_HomestayNotActive_Throws()
    {
        await _host.Db.Homestays.ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HomestayStatus.Suspended));

        await Assert.ThrowsAsync<BookingException>(() => _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut)));
    }

    [Fact]
    public async Task Create_FourthAwaitingBookingOfCustomer_Throws()
    {
        var bookings = _host.Bookings();
        await bookings.CreateAsync(_host.Request(new(2026, 12, 1), new(2026, 12, 2)));
        await bookings.CreateAsync(_host.Request(new(2026, 12, 3), new(2026, 12, 4)));
        await bookings.CreateAsync(_host.Request(new(2026, 12, 5), new(2026, 12, 6)));

        var ex = await Assert.ThrowsAsync<BookingException>(() => bookings.CreateAsync(_host.Request(new(2026, 12, 7), new(2026, 12, 8))));
        Assert.Contains("quá nhiều đơn chờ thanh toán", ex.Message);
    }

    [Fact]
    public async Task ExpireOverdueHolds_After15Minutes_ReleasesRoom()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));

        _host.Clock.Advance(TimeSpan.FromMinutes(14));
        Assert.Equal(0, await bookings.ExpireOverdueHoldsAsync());

        _host.Clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(1, await bookings.ExpireOverdueHoldsAsync());

        var saved = await _host.Db.Bookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Expired, saved.Status);
        Assert.Equal(0, await _host.Db.RoomNights.CountAsync());
        Assert.Equal(2, (await bookings.FreeRoomIdsAsync(_host.RoomType.Id, CheckIn, CheckOut)).Count);
    }

    [Fact]
    public async Task ExpireOverdueHolds_BillSubmitted_KeepsRoom()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));
        await bookings.ChangeStatusAsync(booking, BookingStatus.PaymentSubmitted, new Actor(ActorType.Customer));

        _host.Clock.Advance(TimeSpan.FromHours(10));

        Assert.Equal(0, await bookings.ExpireOverdueHoldsAsync());
        Assert.Equal(3, await _host.Db.RoomNights.CountAsync());
    }

    [Theory]
    [InlineData(BookingStatus.AwaitingPayment, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.AwaitingPayment, BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Deposited, BookingStatus.Completed)]
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired, BookingStatus.Deposited)]
    public void CanTransition_InvalidTransitions_AreRejected(BookingStatus from, BookingStatus to)
    {
        Assert.False(BookingService.CanTransition(from, to));
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_Throws()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));

        await Assert.ThrowsAsync<BookingException>(() =>
            bookings.ChangeStatusAsync(booking, BookingStatus.Confirmed, new Actor(ActorType.Owner)));
    }

    [Fact]
    public async Task FullLifecycle_DepositConfirmCheckInComplete()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));

        await bookings.ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);
        Assert.Equal(_host.Clock.Now.UtcDateTime.AddMinutes(30), booking.ConfirmDeadlineAt);
        await bookings.ChangeStatusAsync(booking, BookingStatus.Confirmed, new Actor(ActorType.Owner));
        await bookings.ChangeStatusAsync(booking, BookingStatus.CheckedIn, new Actor(ActorType.Owner));
        await bookings.ChangeStatusAsync(booking, BookingStatus.Completed, new Actor(ActorType.Owner));

        Assert.Equal(5, await _host.Db.BookingStatusHistories.CountAsync(h => h.BookingId == booking.Id));
        // Đơn hoàn thành vẫn giữ RoomNights để thống kê
        Assert.Equal(3, await _host.Db.RoomNights.CountAsync());
        // Chủ homestay và khách được thông báo
        Assert.True(await _host.Db.Notifications.AnyAsync(n => n.Message.Contains("đã đặt cọc")));
        Assert.True(await _host.Db.Notifications.AnyAsync(n => n.Message.Contains("đã được homestay xác nhận")));
    }

    [Fact]
    public async Task Rejected_RefundsFullDepositAndReleasesRoom()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));
        await bookings.ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);

        await bookings.ChangeStatusAsync(booking, BookingStatus.Rejected, new Actor(ActorType.Owner), "Phòng đang sửa");

        Assert.Equal(booking.DepositAmount, booking.RefundAmount);
        Assert.Equal(0, await _host.Db.RoomNights.CountAsync());
    }

    [Fact]
    public async Task Cancelled_DepositedTenDaysBefore_RefundsFullDeposit()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(new(2026, 11, 20), new(2026, 11, 21)));
        await bookings.ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);

        await bookings.ChangeStatusAsync(booking, BookingStatus.Cancelled, new Actor(ActorType.Customer));

        Assert.Equal(booking.DepositAmount, booking.RefundAmount);
    }

    [Fact]
    public async Task AlertOverdueConfirmations_AlertsOnceAfter30Minutes()
    {
        var bookings = _host.Bookings();
        var booking = await bookings.CreateAsync(_host.Request(CheckIn, CheckOut));
        await bookings.ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);

        _host.Clock.Advance(TimeSpan.FromMinutes(29));
        Assert.Equal(0, await bookings.AlertOverdueConfirmationsAsync());

        _host.Clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(1, await bookings.AlertOverdueConfirmationsAsync());
        Assert.Equal(0, await bookings.AlertOverdueConfirmationsAsync());
        // Khách tự đặt, chưa có người phụ trách → báo nhân viên tư vấn
        Assert.True(await _host.Db.Notifications.AnyAsync(n => n.Message.Contains("chưa xác nhận")));
    }

    [Fact]
    public async Task ChangeStatus_StatusChangedByAnotherRequest_ThrowsConcurrency()
    {
        var booking = await _host.Bookings().CreateAsync(_host.Request(CheckIn, CheckOut));

        // Request khác (job hết hạn) đọc cùng đơn
        await using var other = _host.NewContext();
        var stale = await other.Bookings.SingleAsync(b => b.Id == booking.Id);

        await _host.Bookings().ChangeStatusAsync(booking, BookingStatus.Deposited, Actor.System);

        // Không ghi đè trạng thái "Đã đặt cọc" bằng "Hết hạn"
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _host.Bookings(other).ChangeStatusAsync(stale, BookingStatus.Expired, Actor.System));
    }
}
