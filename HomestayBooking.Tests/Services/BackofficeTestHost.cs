using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

internal sealed class BackofficeTestHost : IDisposable
{
    public PartnerTestHost P { get; }
    public BookingTestHost H => P.H;
    public ApplicationDbContext Db => P.Db;
    public DateOnly Today => P.Today;
    public Actor Manager => P.Manager;
    public Actor Consultant => P.Consultant;
    public AvailabilityService Availability(ApplicationDbContext? db = null) => new(db ?? Db, H.Clock, Options.Create(H.Options));
    public StaffBookingService Staff(ApplicationDbContext? db = null) => new(db ?? Db, H.Bookings(db), new(db ?? Db), H.Clock, Guests(db), Availability(db));
    public GuestCustomerService Guests(ApplicationDbContext? db = null) =>
        new(db ?? Db, P.Customers(db), H.Clock);
    public CustomerService Customers(ApplicationDbContext? db = null) => new(db ?? Db, H.Vouchers(db), H.Clock);
    public OverbookingService Overbooking(ApplicationDbContext? db = null) => new(db ?? Db, H.Bookings(db), Availability(db), H.Vouchers(db), new(db ?? Db), H.Clock);
    public Actor Owner => new(ActorType.Owner, Db.Users.Single(u => u.PersonId == H.Homestay.Owner.PersonId).Id);
    private BackofficeTestHost(PartnerTestHost p) => P = p;
    public static async Task<BackofficeTestHost> CreateAsync() => new(await PartnerTestHost.CreateAsync());
    public async Task<Booking> BookingAsync(BookingStatus status = BookingStatus.Confirmed, int? voucherId = null)
    {
        var b = await H.Bookings().CreateAsync(H.Request(Today.AddDays(5), Today.AddDays(7)) with { Actor = Consultant, VoucherId = voucherId });
        if (status == BookingStatus.AwaitingPayment) return b;
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        if (status == BookingStatus.Deposited) return b;
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        if (status == BookingStatus.Confirmed) return b;
        H.Clock.Advance(TimeSpan.FromDays(5)); await H.Bookings().ChangeStatusAsync(b, BookingStatus.CheckedIn, Actor.System);
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.Completed, Actor.System); return b;
    }
    public async Task<RoomType> TypeAsync(decimal nightly, bool sameHome = true, bool sameArea = true, string? name = null)
    {
        var home = sameHome ? await Db.Homestays.SingleAsync(h => h.Id == H.Homestay.Id) : new Homestay { Name = name ?? "Cơ sở thay thế", Address = "Địa chỉ riêng chưa gửi khách",
            ContactPhone = "+84912000999", Status = HomestayStatus.Active, OwnerId = H.Homestay.OwnerId,
            AreaId = sameArea ? H.Homestay.AreaId : 0 };
        if (!sameHome)
        {
            if (!sameArea) home.Area = new Area { Name = "Khu khác", Province = "Hà Nội" };
            home.Contracts.Add(new() { ContractNo = "HD-" + Guid.NewGuid(), StartDate = Today.AddYears(-1), EndDate = Today.AddYears(1), CommissionRate = .15m });
        }
        var type = new RoomType { Name = name ?? "Loại " + Guid.NewGuid().ToString("N")[..8], Homestay = home, CategoryId = H.RoomType.CategoryId, MaxGuests = 4 };
        type.Rooms.Add(new() { RoomCode = "M1" });
        foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new() { DayType = day, Price = nightly, EffectiveFrom = Today.AddYears(-1) });
        Db.Add(type); await Db.SaveChangesAsync(); return type;
    }
    public async Task<Voucher> VoucherAsync()
    {
        var v = new Voucher { Code = "TEST-B3", CustomerId = H.Customer.Id, Amount = 100000, Balance = 100000,
            ExpiresOn = Today.AddMonths(6), IssuedByEmployeeId = Manager.EmployeeId!.Value, Reason = "Thử chuyển voucher" };
        Db.Add(v); await Db.SaveChangesAsync(); return v;
    }
    public void Dispose() => P.Dispose();
}
