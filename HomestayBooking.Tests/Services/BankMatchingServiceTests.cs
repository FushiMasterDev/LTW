using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class BankMatchingServiceTests : IDisposable
{
    private readonly BookingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Theory]
    [InlineData("FJ261110-001", "FJ261110-001")]
    [InlineData("CK FJ261110001 dat coc", "FJ261110-001")]      // ngân hàng bỏ dấu "-"
    [InlineData("fj 261110 - 001", "FJ261110-001")]             // chữ thường, có khoảng trắng
    [InlineData("MBVCB.123.FJ2611101234.CT", "FJ261110-1234")]  // số thứ tự 4 chữ số
    [InlineData("FJ999999001", null)]                           // ngày không hợp lệ
    [InlineData("chuyen tien", null)]
    [InlineData(null, null)]
    public void ExtractBookingCode_HandlesBankFormats(string? content, string? expected)
    {
        Assert.Equal(expected, BankMatchingService.ExtractBookingCode(content));
    }

    private async Task<Booking> NewBookingAsync() =>
        await _host.Bookings().CreateAsync(_host.Request(new(2026, 11, 19), new(2026, 11, 22)));

    private IncomingBankTransaction Tx(string id, decimal amount, string content, string account = BookingTestHost.FujiAccount) =>
        new(BankProvider.SePay, id, account, amount, content, _host.Clock.Now.UtcDateTime);

    [Fact]
    public async Task Process_EnoughMoney_DepositsBookingAutomatically()
    {
        var booking = await NewBookingAsync();

        var result = await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount, $"{booking.Code} dat coc"));

        Assert.Equal(BankMatchStatus.Matched, result.Status);
        Assert.Equal(booking.Code, result.BookingCode);
        var saved = await _host.Db.Bookings.AsNoTracking().Include(b => b.Payments).SingleAsync();
        Assert.Equal(BookingStatus.Deposited, saved.Status);
        var payment = Assert.Single(saved.Payments);
        Assert.Equal(PaymentMethod.BankAutoMatch, payment.Method);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Null(payment.ReviewedByEmployeeId); // hệ thống tự duyệt
        Assert.NotNull(payment.BankTransactionId);
    }

    [Fact]
    public async Task Process_SameWebhookTwice_RecordsOnce()
    {
        var booking = await NewBookingAsync();
        var tx = Tx("SP-1", booking.DepositAmount, booking.Code);

        await _host.Matching().ProcessAsync(tx);
        var second = await _host.Matching().ProcessAsync(tx);

        Assert.True(second.Duplicate);
        Assert.Equal(1, await _host.Db.BankTransactions.CountAsync());
        Assert.Equal(1, await _host.Db.Payments.CountAsync());
    }

    [Fact]
    public async Task Process_Underpaid_KeepsWaitingForPayment()
    {
        var booking = await NewBookingAsync();

        var result = await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount - 10_000, booking.Code));

        Assert.Equal(BankMatchStatus.Underpaid, result.Status);
        Assert.Equal(BookingStatus.AwaitingPayment, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(0, await _host.Db.Payments.CountAsync());
    }

    [Fact]
    public async Task Process_Overpaid_MatchesAndNotesExcess()
    {
        var booking = await NewBookingAsync();

        var result = await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount + 50_000, booking.Code));

        Assert.Equal(BankMatchStatus.Matched, result.Status);
        Assert.Contains("dư", (await _host.Db.BankTransactions.SingleAsync()).Note);
    }

    [Fact]
    public async Task Process_AfterHoldExpired_IsLatePayment()
    {
        var booking = await NewBookingAsync();
        _host.Clock.Advance(TimeSpan.FromMinutes(16));
        await _host.Bookings().ExpireOverdueHoldsAsync();

        var result = await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount, booking.Code));

        Assert.Equal(BankMatchStatus.LatePayment, result.Status);
        Assert.Equal(BookingStatus.Expired, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Process_BillAlreadySubmitted_ApprovesThatPayment()
    {
        var booking = await NewBookingAsync();
        _host.Db.Payments.Add(new Payment
        {
            BookingId = booking.Id,
            Type = PaymentType.Deposit,
            Method = PaymentMethod.ManualReceipt,
            Amount = booking.DepositAmount,
            ReceiptImageUrl = "receipts/a.jpg",
        });
        await _host.Bookings().ChangeStatusAsync(booking, BookingStatus.PaymentSubmitted, new Actor(ActorType.Customer));
        _host.Db.ChangeTracker.Clear();

        await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount, booking.Code));

        // Không tạo khoản thứ hai: khoản bill được duyệt và gắn giao dịch
        var payment = await _host.Db.Payments.SingleAsync();
        Assert.Equal(PaymentMethod.ManualReceipt, payment.Method);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.NotNull(payment.BankTransactionId);
        Assert.Equal(BookingStatus.Deposited, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Process_OtherAccount_IsIgnored()
    {
        var booking = await NewBookingAsync();

        var result = await _host.Matching().ProcessAsync(Tx("SP-1", booking.DepositAmount, booking.Code, account: "123456"));

        Assert.Equal(BankMatchStatus.Ignored, result.Status);
        Assert.Equal(BookingStatus.AwaitingPayment, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Process_NoBookingCode_IsUnmatchedButRecorded()
    {
        var result = await _host.Matching().ProcessAsync(Tx("SP-1", 500_000, "chuyen tien coc phong"));

        Assert.Equal(BankMatchStatus.Unmatched, result.Status);
        Assert.Equal(1, await _host.Db.BankTransactions.CountAsync());
    }

    [Fact]
    public void VietQr_BuildsMbBankUrlWithAmountAndCode()
    {
        var qr = new VietQrService(Options.Create(_host.Bank));

        var url = qr.BuildImageUrl(510_000, "FJ261110-001");

        Assert.Equal(
            "https://img.vietqr.io/image/MB-0901999999-compact2.png?amount=510000&addInfo=FJ261110-001&accountName=CONG%20TY%20FUJI",
            url);
    }
    [Fact]
    public async Task IncidentalCrLettersDoNotTurnAnInitialDepositIntoAChangeRequest()
    {
        var booking = await NewBookingAsync();
        var result = await _host.Matching().ProcessAsync(Tx("INITIAL-TEXT", booking.AmountDue, $"MICROSOFT {booking.Code}"));
        Assert.Equal(BankMatchStatus.Matched, result.Status);
        Assert.Equal(PaymentType.Deposit, (await _host.Db.Payments.SingleAsync()).Type);
    }

    [Theory]
    [InlineData("FJ261020-001 Cruz Nguyen")]
    [InlineData("CREDIT FJ261020-001")]
    public async Task CrWordsWithoutDigitsStillMatchInitialDeposit(string content)
    {
        var booking = await NewBookingAsync();
        booking.Code = "FJ261020-001";
        await _host.Db.SaveChangesAsync();
        var result = await _host.Matching().ProcessAsync(Tx("CR-WORD", booking.AmountDue, content));
        Assert.Equal(BankMatchStatus.Matched, result.Status);
        Assert.Equal(booking.Code, result.BookingCode);
        var payment = await _host.Db.Payments.SingleAsync();
        Assert.Equal(PaymentType.Deposit, payment.Type); Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Null(payment.ChangeRequestId);
        Assert.Equal(BookingStatus.Deposited, (await _host.Db.Bookings.AsNoTracking().SingleAsync()).Status);
    }

}
