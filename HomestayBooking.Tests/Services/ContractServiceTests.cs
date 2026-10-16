using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HomestayBooking.Tests.Services;

public sealed class ContractServiceTests
{
    private static CreateContractRequest Request(PartnerTestHost h, decimal rate = .12m) =>
        new(h.H.Homestay.Id, "HD-B1", h.Today, h.Today.AddYears(1), rate);
    private static async Task ClearActive(PartnerTestHost h)
    {
        (await h.Db.Contracts.SingleAsync()).Status = ContractStatus.Terminated;
        await h.Db.SaveChangesAsync();
    }
    [Theory]
    [InlineData(.09, false)] [InlineData(.10, true)] [InlineData(.15, true)] [InlineData(.16, false)]
    public async Task CommissionBoundary(decimal rate, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync(); await ClearActive(h);
        if (!allowed) await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(Request(h, rate), h.Manager));
        else
        {
            var c = await h.Contracts.CreateAsync(Request(h, rate), h.Manager);
            Assert.Equal(rate, c.CommissionRate); Assert.Equal(h.Manager.EmployeeId, c.SignedByEmployeeId);
            Assert.Equal(h.Manager.AccountId, (await h.Db.AuditLogs.SingleAsync()).AccountId);
        }
    }
    [Theory]
    [InlineData(0, true)] [InlineData(1, false)] [InlineData(-1, true)]
    public async Task StartDateCannotBeFuture(int days, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync(); await ClearActive(h);
        var input = Request(h) with { StartDate = h.Today.AddDays(days) };
        if (allowed) Assert.NotNull(await h.Contracts.CreateAsync(input, h.Admin));
        else await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(input, h.Admin));
    }
    [Theory]
    [InlineData(-1, false)] [InlineData(0, false)] [InlineData(1, true)]
    public async Task EndDateMustBeAfterToday(int days, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync(); await ClearActive(h);
        var input = Request(h) with { StartDate = h.Today.AddDays(-2), EndDate = h.Today.AddDays(days) };
        if (allowed) Assert.NotNull(await h.Contracts.CreateAsync(input, h.Manager));
        else await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(input, h.Manager));
    }
    [Theory]
    [InlineData("owner")] [InlineData("active")] [InlineData("number")] [InlineData("ended")]
    public async Task CreationGuards(string guard)
    {
        using var h = await PartnerTestHost.CreateAsync();
        if (guard != "owner") await h.ApproveExistingOwnerAsync();
        if (guard != "active") await ClearActive(h);
        if (guard == "ended") { (await h.Db.Homestays.SingleAsync()).Status = HomestayStatus.Ended; await h.Db.SaveChangesAsync(); }
        var input = guard == "number" ? Request(h) with { ContractNo = "HD-1" } : Request(h);
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(input, h.Manager));
        Assert.Single(h.Db.Contracts); Assert.Empty(h.Db.AuditLogs);
    }
    [Theory]
    [InlineData(EmployeePosition.Consultant)] [InlineData(EmployeePosition.Accountant)]
    public async Task UnprivilegedEmployeeCannotManageContracts(EmployeePosition position)
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync(); await ClearActive(h);
        var actor = position == EmployeePosition.Consultant ? h.Consultant : h.Accountant;
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.CreateAsync(Request(h), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.ExtendAsync(1, h.Today.AddYears(2), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.TerminateAsync(1, "Lý do", actor));
    }
    [Theory]
    [InlineData(-1, false)] [InlineData(0, false)] [InlineData(1, true)]
    public async Task ExtensionOnlyMovesEndDateLater(int days, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync(); var original = c.EndDate;
        var rate = c.CommissionRate;
        if (allowed)
        {
            await h.Contracts.ExtendAsync(c.Id, original.AddDays(days), h.Manager);
            Assert.Equal(original.AddDays(days), c.EndDate); Assert.Equal(rate, c.CommissionRate);
            Assert.Equal(h.Manager.AccountId, (await h.Db.AuditLogs.SingleAsync()).AccountId);
        }
        else await Assert.ThrowsAsync<BookingException>(() => h.Contracts.ExtendAsync(c.Id, original.AddDays(days), h.Manager));
    }
    [Theory]
    [InlineData(ContractStatus.Terminated)] [InlineData(ContractStatus.Expired)]
    public async Task ClosedContractsCannotBeExtendedOrTerminated(ContractStatus status)
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync(); c.Status = status; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.ExtendAsync(c.Id, c.EndDate.AddDays(1), h.Manager));
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.TerminateAsync(c.Id, "Hết hợp tác", h.Manager));
    }
    [Theory]
    [InlineData(BookingStatus.AwaitingPayment, false)] [InlineData(BookingStatus.PaymentSubmitted, false)]
    [InlineData(BookingStatus.Deposited, false)] [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.CheckedIn, false)] [InlineData(BookingStatus.Completed, true)]
    public async Task TerminationBlocksHoldingBookingsExceptCompleted(BookingStatus status, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var b = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), h.Today.AddDays(4)));
        // Dữ liệu thử bao phủ mọi trạng thái mà HoldsRoom coi là đang chiếm lịch.
        b.Status = status; b.ConfirmedAt = status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed ? h.H.Clock.Now.UtcDateTime : null;
        await h.Db.SaveChangesAsync(); var c = await h.Db.Contracts.SingleAsync();
        if (allowed)
        {
            var nights = await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id);
            await h.Contracts.TerminateAsync(c.Id, "Dừng", h.Manager);
            Assert.Equal(ContractStatus.Terminated, c.Status);
            Assert.Equal(nights, await h.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
        }
        else
        {
            var ex = await Assert.ThrowsAsync<BookingException>(() => h.Contracts.TerminateAsync(c.Id, "Dừng", h.Manager));
            Assert.Contains(b.Code, ex.Message); Assert.Equal(ContractStatus.Active, (await h.Db.Contracts.SingleAsync()).Status);
        }
    }
    [Fact]
    public async Task TerminationRequiresReasonAndRecordsIt()
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Contracts.TerminateAsync(c.Id, " ", h.Manager));
        c = await h.Contracts.TerminateAsync(c.Id, "Hai bên chấm dứt", h.Manager);
        Assert.Equal(ContractStatus.Terminated, c.Status);
        var log = await h.Db.AuditLogs.SingleAsync(); Assert.Equal(h.Manager.AccountId, log.AccountId);
        Assert.Contains("Active", log.OldValues); Assert.Contains("Terminated", log.NewValues); Assert.Contains("Reason", log.NewValues);
    }
    [Theory]
    [InlineData(-1, 1)] [InlineData(0, 0)] [InlineData(1, 0)]
    public async Task ExpiryUsesVietnamDateAndNotifiesManagersOnly(int days, int expected)
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync(); c.EndDate = h.Today.AddDays(days); await h.Db.SaveChangesAsync();
        Assert.Equal(expected, await h.Contracts.ExpireAsync());
        Assert.Equal(0, await h.Contracts.ExpireAsync());
        if (expected > 0)
        {
            var log = await h.Db.AuditLogs.SingleAsync(); Assert.Null(log.AccountId); Assert.Contains("System", log.NewValues);
            Assert.Equal(h.Manager.AccountId, (await h.Db.Notifications.SingleAsync()).UserId);
        }
        Assert.Equal(expected > 0 ? ContractStatus.Expired : ContractStatus.Active, c.Status);
    }
    [Theory]
    [InlineData(0, true)] [InlineData(1, false)]
    public async Task BookingCheckoutAtContractEndBoundary(int beyond, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync(); c.EndDate = h.Today.AddDays(5); await h.Db.SaveChangesAsync();
        if (allowed) Assert.NotNull(await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), c.EndDate.AddDays(beyond))));
        else await Assert.ThrowsAsync<BookingException>(() => h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), c.EndDate.AddDays(beyond))));
    }
    [Theory]
    [InlineData(0, true)] [InlineData(1, false)]
    public async Task SearchAndDetailsRequireContractThroughCheckout(int beyond, bool allowed)
    {
        using var h = await PartnerTestHost.CreateAsync(); var c = await h.Db.Contracts.SingleAsync(); c.EndDate = h.Today.AddDays(5); await h.Db.SaveChangesAsync();
        // Truy vấn trình bày sẵn có cần APPLY; InMemory kiểm bộ lọc mà không đổi controller để chiều SQLite.
        await using var searchDb = new HomestayBooking.Web.Data.ApplicationDbContext(
            new DbContextOptionsBuilder<HomestayBooking.Web.Data.ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var home = new Homestay { Id = h.H.Homestay.Id, Name = "Tìm kiếm", Address = "Hà Nội", Status = HomestayStatus.Active,
            Area = new Area { Name = "Tây Hồ", Province = "Hà Nội" },
            Owner = new Owner { Person = new PersonProfile { PhoneNumber = "+84911000001" } } };
        home.Contracts.Add(new Contract { ContractNo = "HD-SEARCH", StartDate = c.StartDate, EndDate = c.EndDate, CommissionRate = .12m });
        var type = new RoomType { Name = "Phòng đôi", MaxGuests = 2, Category = new RoomCategory { Name = "Đôi" } };
        type.Rooms.Add(new Room { RoomCode = "101" });
        // Tìm có ngày nay tính đủ kỳ ở; fixture hợp đồng cần giá cho cả cuối tuần/ngày lễ.
        foreach (var day in Enum.GetValues<DayType>())
            type.Prices.Add(new RoomTypePrice { DayType = day, EffectiveFrom = h.Today.AddDays(-1), Price = 500000 });
        home.RoomTypes.Add(type); searchDb.Add(home); await searchDb.SaveChangesAsync();
        var pricing = new PricingService(searchDb, Options.Create(h.H.Options));
        var controller = new HomestaysController(searchDb, pricing, h.H.Clock, Options.Create(h.H.Options));
        var query = new HomestaySearchQuery { CheckIn = h.Today.AddDays(3), CheckOut = c.EndDate.AddDays(beyond), Guests = 2 };
        var result = Assert.IsType<ViewResult>(await controller.Index(query));
        var model = Assert.IsType<HomestaySearchViewModel>(result.Model);
        Assert.Equal(allowed ? 1 : 0, model.Results.Count);
        var detail = await controller.Details(h.H.Homestay.Id, query);
        if (allowed) Assert.IsType<ViewResult>(detail); else Assert.IsType<NotFoundResult>(detail);
    }
    [Fact]
    public async Task LegacyDocumentsWithoutPropertyPhotoDoNotBlockBookingContractOrExtension()
    {
        using var h = await PartnerTestHost.CreateAsync(); await h.ApproveExistingOwnerAsync();
        var registration = await h.Db.OwnerRegistrations.SingleAsync();
        h.Db.VerificationDocuments.Add(new() { OwnerRegistrationId = registration.Id, DocumentType = DocumentType.BusinessLicense, FileUrl = "old.jpg" });
        await h.Db.SaveChangesAsync();
        var b = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(3), h.Today.AddDays(4)));
        var snapshot = b.CommissionRate;
        var c = await h.Db.Contracts.SingleAsync(); await h.Contracts.ExtendAsync(c.Id, c.EndDate.AddDays(5), h.Manager);
        await ClearActive(h); var replacement = await h.Contracts.CreateAsync(Request(h, .15m), h.Manager);
        Assert.Equal(.15m, replacement.CommissionRate); Assert.Equal(snapshot, (await h.Db.Bookings.SingleAsync()).CommissionRate);
        var doc = await h.Db.VerificationDocuments.SingleAsync(); Assert.Equal(registration.Id, doc.OwnerRegistrationId); Assert.Null(doc.HomestayId);
        Assert.Equal(HomestayStatus.Active, (await h.Db.Homestays.SingleAsync()).Status); Assert.Empty(h.Db.VerificationDocuments.Where(d => d.DocumentType == DocumentType.PropertyPhoto));
    }
    [Theory]
    [InlineData("quote")] [InlineData("submit")] [InlineData("approve")]
    public async Task ChangeRequestChecksNewCheckoutAgainstContractAtEveryStep(string step)
    {
        using var h = await PartnerTestHost.CreateAsync(); var start = h.Today.AddDays(5);
        var b = await h.H.Bookings().CreateAsync(h.H.Request(start, start.AddDays(2)));
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        var input = new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeDates, start.AddDays(5), start.AddDays(7));
        var user = await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Customer.PersonId);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Role, "Customer")], "test"));
        BookingChangeRequest? r = null;
        if (step == "approve") r = await h.H.Changes().SubmitAsync(input, principal);
        (await h.Db.Contracts.SingleAsync()).EndDate = input.NewCheckOut!.Value.AddDays(-1); await h.Db.SaveChangesAsync();
        if (step == "quote") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().QuoteAsync(input, principal));
        else if (step == "submit") await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().SubmitAsync(input, principal));
        else
        {
            var manager = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, h.Manager.AccountId!), new Claim(ClaimTypes.Role, "Employee")], "test"));
            await Assert.ThrowsAsync<BookingException>(() => h.H.Changes().ApproveAsync(r!.Id, "Duyệt", manager));
            Assert.Equal(ChangeRequestStatus.Pending, (await h.Db.BookingChangeRequests.SingleAsync()).Status);
        }
    }
}
