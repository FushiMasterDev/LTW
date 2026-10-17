using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

internal sealed class SettlementTestHost : IDisposable
{
    public BackofficeTestHost B { get; }
    public BookingTestHost H => B.H;
    public PartnerTestHost P => B.P;
    public ApplicationDbContext Db => B.Db;
    public SettlementOptions Options { get; } = new();
    public SettlementPeriod Period { get; } = new(2026, 10, 2);
    private int _seq;
    private SettlementTestHost(BackofficeTestHost b) => B = b;
    public static async Task<SettlementTestHost> CreateAsync() => new(await BackofficeTestHost.CreateAsync());
    public SettlementService Service(ApplicationDbContext? db = null) => new(db ?? Db, new(db ?? Db), H.Clock, Microsoft.Extensions.Options.Options.Create(Options));
    public CompensationService Compensation(ApplicationDbContext? db = null) => new(db ?? Db, H.Accounting(db), new(db ?? Db), H.Clock);
    public async Task<Booking> BookingAsync(BookingStatus status = BookingStatus.Completed, DateOnly? checkOut = null, decimal refund = 0, decimal fee = 0, decimal discount = 0)
    {
        var date = checkOut ?? Period.End;
        var b = new Booking { Code = "STL" + (++_seq).ToString("000000"), CustomerId = H.Customer.Id, HomestayId = H.Homestay.Id,
            RoomTypeId = H.RoomType.Id, RoomId = await Db.Rooms.Select(r => r.Id).FirstAsync(), CheckIn = date.AddDays(-2), CheckOut = date,
            ContactName = "Khách đối soát", ContactPhone = "+84987000001", Guests = 2, TotalAmount = 2000000, DepositAmount = 600000, CommissionRate = .12m,
            CancellationPolicyId = H.GeneralPolicy.Id, Status = status, RefundAmount = refund, ChangeFeeAmount = fee,
            DepositedAt = status is BookingStatus.AwaitingPayment or BookingStatus.PaymentSubmitted ? null : H.Clock.Now.UtcDateTime.AddDays(-30),
            ConfirmedAt = status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed or BookingStatus.NoShow ? H.Clock.Now.UtcDateTime.AddDays(-20) : null };
        if (discount > 0) { b.VoucherId = (await B.VoucherAsync()).Id; b.DiscountAmount = discount; }
        Db.Add(b); await Db.SaveChangesAsync(); return b;
    }
    public async Task<Settlement> DraftAsync()
    { await BookingAsync(); return (await Service().GenerateAsync(H.Homestay.Id, Period, P.Accountant)).Settlement!; }
    public async Task BankAsync()
    { var owner = await Db.Owners.SingleAsync(); owner.BankName = "MB"; owner.BankAccountNo = "12345678"; owner.BankAccountName = "CHU NHA"; await Db.SaveChangesAsync(); }
    public async Task<Payment> CompensationAsync(BookingStatus status = BookingStatus.Completed, bool replacedAgain = false)
    {
        var target = await BookingAsync(status);
        var old = await BookingAsync(BookingStatus.Cancelled); old.ReplacedByBookingId = target.Id;
        if (replacedAgain) target.ReplacedByBookingId = (await BookingAsync()).Id;
        var p = new Payment { BookingId = target.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer, Amount = 300000,
            Status = PaymentStatus.Submitted, SubmittedAt = H.Clock.Now.UtcDateTime };
        Db.Add(p); await Db.SaveChangesAsync(); return p;
    }
    public void Dispose() => B.Dispose();
}
