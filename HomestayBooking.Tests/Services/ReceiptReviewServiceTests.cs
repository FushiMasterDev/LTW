using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class ReceiptReviewServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();
    private readonly Employee _accountant;
    private readonly Employee _manager;
    private readonly Employee _consultant;

    public ReceiptReviewServiceTests()
    {
        _accountant = AddEmployee("KT001", "+84901000021", EmployeePosition.Accountant);
        _manager = AddEmployee("QL001", "+84901000031", EmployeePosition.Manager);
        _consultant = _host.Db.Employees.Single(e => e.EmployeeCode == "NV001");
        _host.Db.ChangeTracker.Clear();
    }

    public void Dispose() => _host.Dispose();

    private Employee AddEmployee(string code, string phone, EmployeePosition position)
    {
        var person = new PersonProfile { FullName = code, PhoneNumber = phone };
        _host.Db.Users.Add(new ApplicationUser { Person = person, Status = AccountStatus.Active });
        var employee = new Employee { Person = person, EmployeeCode = code, Position = position };
        _host.Db.Employees.Add(employee);
        _host.Db.SaveChanges();
        return employee;
    }

    private async Task<(Booking Booking, Payment Payment)> SubmittedAsync()
    {
        var booking = await _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)));
        var payment = await _host.Receipts().SubmitAsync(booking.Id, "receipts/a.jpg", booking.Code, new Actor(ActorType.Customer));
        _host.Db.ChangeTracker.Clear();
        return (booking, payment);
    }

    private Task<Booking> LoadAsync(int id) =>
        _host.Db.Bookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == id);

    [Fact]
    public async Task Submit_KeepsRoomAndNotifiesReviewers()
    {
        var (booking, _) = await SubmittedAsync();

        var saved = await LoadAsync(booking.Id);
        Assert.Equal(BookingStatus.PaymentSubmitted, saved.Status);
        Assert.NotNull(saved.PaymentSubmittedAt);
        Assert.Equal(3, await _host.Db.RoomNights.CountAsync(n => n.BookingId == booking.Id));
        var payment = Assert.Single(saved.Payments);
        Assert.Equal(PaymentMethod.ManualReceipt, payment.Method);
        Assert.Equal(booking.DepositAmount, payment.Amount);
        Assert.Equal(2, await _host.Db.Notifications.CountAsync(n => n.Message.Contains("chờ duyệt")));
    }

    [Fact]
    public async Task Submit_AfterHoldExpired_IsRefused()
    {
        var booking = await _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)));
        _host.Clock.Advance(TimeSpan.FromMinutes(16));

        await Assert.ThrowsAsync<BookingException>(() =>
            _host.Receipts().SubmitAsync(booking.Id, "receipts/a.jpg", null, new Actor(ActorType.Customer)));
    }

    [Fact]
    public async Task Approve_ByAccountant_DepositsBooking()
    {
        var (booking, payment) = await SubmittedAsync();

        await _host.Receipts().ApproveAsync(payment.Id, _accountant.Id);

        var saved = await LoadAsync(booking.Id);
        Assert.Equal(BookingStatus.Deposited, saved.Status);
        var approved = Assert.Single(saved.Payments);
        Assert.Equal(PaymentStatus.Approved, approved.Status);
        Assert.Equal(_accountant.Id, approved.ReviewedByEmployeeId);
    }

    [Fact]
    public async Task Approve_ByConsultant_IsRefused()
    {
        var (_, payment) = await SubmittedAsync();

        await Assert.ThrowsAsync<BookingException>(() => _host.Receipts().ApproveAsync(payment.Id, _consultant.Id));
    }

    [Fact]
    public async Task Reject_GivesCustomerNewFifteenMinutes_ThenResubmitWorks()
    {
        var (booking, payment) = await SubmittedAsync();
        _host.Clock.Advance(TimeSpan.FromHours(3)); // hạn giữ ban đầu đã qua từ lâu

        await _host.Receipts().RejectAsync(payment.Id, _manager.Id, "Ảnh mờ");
        _host.Db.ChangeTracker.Clear();

        var saved = await LoadAsync(booking.Id);
        Assert.Equal(BookingStatus.AwaitingPayment, saved.Status);
        Assert.Equal(_host.Clock.Now.UtcDateTime.AddMinutes(15), saved.HoldExpiresAt);
        Assert.Equal(0, await _host.Bookings().ExpireOverdueHoldsAsync()); // job không hủy ngay

        _host.Db.ChangeTracker.Clear();
        await _host.Receipts().SubmitAsync(booking.Id, "receipts/b.jpg", null, new Actor(ActorType.Customer));
        Assert.Equal(2, (await LoadAsync(booking.Id)).Payments.Count);
    }

    [Fact]
    public async Task Review_Twice_IsRefused()
    {
        var (_, payment) = await SubmittedAsync();
        await _host.Receipts().ApproveAsync(payment.Id, _accountant.Id);
        _host.Db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<BookingException>(() => _host.Receipts().RejectAsync(payment.Id, _manager.Id, "x"));
    }

    [Fact]
    public async Task BankMoneyArrives_WhileBillPending_ApprovesBillAutomatically()
    {
        var (booking, payment) = await SubmittedAsync();

        await _host.Matching().ProcessAsync(new(BankProvider.SePay, "SP-9", BookingTestHost.FujiAccount, booking.DepositAmount,
            booking.Code, _host.Clock.Now.UtcDateTime));

        var saved = await LoadAsync(booking.Id);
        Assert.Equal(BookingStatus.Deposited, saved.Status);
        var approved = Assert.Single(saved.Payments);
        Assert.Equal(payment.Id, approved.Id);
        Assert.Null(approved.ReviewedByEmployeeId);
    }

    [Fact]
    public async Task SlowReview_AlertsManagersOnceAfterTwoOfficeHours()
    {
        await SubmittedAsync(); // 10:00 thứ Ba
        _host.Clock.Advance(TimeSpan.FromMinutes(119));
        Assert.Equal(0, await _host.Receipts().AlertSlowReviewsAsync());

        _host.Clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(1, await _host.Receipts().AlertSlowReviewsAsync());
        Assert.Equal(0, await _host.Receipts().AlertSlowReviewsAsync());
        Assert.Equal(1, await _host.Db.Notifications.CountAsync(n => n.Message.Contains("giờ làm việc")));
    }

    [Theory]
    // 22:00 thứ Ba → 09:00 thứ Tư: chỉ tính 08:00–09:00
    [InlineData("2026-11-10T15:00:00Z", "2026-11-11T02:00:00Z", 60)]
    // 16:00 thứ Sáu → 09:00 thứ Hai: 1 giờ thứ Sáu + 1 giờ thứ Hai
    [InlineData("2026-11-13T09:00:00Z", "2026-11-16T02:00:00Z", 120)]
    // Trọn trong giờ làm việc
    [InlineData("2026-11-10T03:00:00Z", "2026-11-10T04:30:00Z", 90)]
    public void OfficeMinutesBetween_SkipsNightsAndWeekends(string from, string to, double expected)
    {
        var minutes = VietnamTime.OfficeMinutesBetween(
            DateTime.Parse(from).ToUniversalTime(), DateTime.Parse(to).ToUniversalTime(), 8, 17);
        Assert.Equal(expected, minutes);
    }
}
