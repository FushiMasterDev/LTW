using System.Security.Claims;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed partial class BookingChangeRequestServiceTests
{
    private sealed class Scenario : IDisposable
    {
        public BookingTestHost H { get; }
        public ApplicationDbContext Db => H.Db;
        public ClaimsPrincipal Customer { get; }
        public ClaimsPrincipal Manager { get; }
        public ClaimsPrincipal Consultant => Principal(Db.Users.Where(u => u.Person.Employee != null
            && u.Person.Employee.EmployeeCode == "NV001").Select(u => u.Id).Single(), AppRoles.Employee, EmployeePosition.Consultant);
        public int ManagerId { get; }
        public DateOnly Today => VietnamTime.Today(H.Clock);
        private int _sequence;
        public Scenario(string? connection = null)
        {
            H = new BookingTestHost(connection);
            foreach (var price in Db.RoomTypePrices) price.Price = 1_000_000;
            var employee = Staff(EmployeePosition.Manager);
            ManagerId = employee.Id;
            Manager = Principal(employee.Person.Account!.Id, AppRoles.Employee, employee.Position);
            Customer = Principal(Db.Users.Single(u => u.PersonId == H.Customer.PersonId).Id, AppRoles.Customer);
            Db.SaveChanges(); Db.ChangeTracker.Clear();
        }
        public Employee Staff(EmployeePosition position)
        {
            var user = new ApplicationUser { Status = AccountStatus.Active,
                Person = new PersonProfile { PhoneNumber = "+84901888" + (++_sequence).ToString("000") } };
            var employee = new Employee { Person = user.Person, EmployeeCode = "WF" + _sequence, Position = position };
            Db.AddRange(user, employee); Db.SaveChanges();
            return employee;
        }
        public RoomType Type(decimal nightly, int capacity = 2)
        {
            var type = new RoomType { HomestayId = H.Homestay.Id, CategoryId = H.RoomType.CategoryId,
                Name = "Loại mới " + (++_sequence), MaxGuests = capacity };
            type.Rooms.Add(new Room { RoomCode = "M1" });
            foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new RoomTypePrice {
                DayType = day, Price = nightly, EffectiveFrom = Today.AddYears(-1) });
            Db.Add(type); Db.SaveChanges(); return type;
        }
        public async Task<Booking> Book(int days = 10, bool confirmed = true)
        {
            var b = await H.Bookings().CreateAsync(H.Request(Today.AddDays(days), Today.AddDays(days + 2)));
            await H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
            if (confirmed) await H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
            return b;
        }
        public ChangeBookingRequest Dates(Booking b, int shift = 7) => new(b.Id, ChangeRequestType.ChangeDates,
            b.CheckIn.AddDays(shift), b.CheckOut.AddDays(shift));
        public BookingChangeRequestService Service(ApplicationDbContext? db = null) => H.Changes(db);
        public void Dispose() => H.Dispose();
    }

    private static ClaimsPrincipal Principal(string id, string role, EmployeePosition? position = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Role, role) };
        if (position is not null) claims.Add(new(AppClaimTypes.EmployeePosition, position.ToString()!));
        return new(new ClaimsIdentity(claims, "test"));
    }

    [Theory]
    [InlineData(2, false)] [InlineData(3, true)] [InlineData(4, true)]
    public async Task MinimumDaysBoundary(int days, bool allowed)
    {
        using var s = new Scenario(); var b = await s.Book(days);
        if (allowed) Assert.NotNull(await s.Service().QuoteAsync(s.Dates(b), s.Customer));
        else await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(s.Dates(b), s.Customer));
    }

    [Theory]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, false)]
    public async Task MaximumAppliedChangesCountsSameBooking(int count, bool allowed)
    {
        using var s = new Scenario(); var b = await s.Book();
        for (var i = 0; i < count; i++) s.Db.BookingChangeRequests.Add(new BookingChangeRequest {
            BookingId = b.Id, RequestType = ChangeRequestType.ChangeDates, NewCheckIn = b.CheckIn, NewCheckOut = b.CheckOut,
            Status = ChangeRequestStatus.Applied, DecidedByAccountId = s.Manager.FindFirstValue(ClaimTypes.NameIdentifier),
            DecidedAt = s.H.Clock.Now.UtcDateTime, DecisionNote = "Lịch sử" });
        await s.Db.SaveChangesAsync();
        if (allowed) Assert.NotNull(await s.Service().QuoteAsync(s.Dates(b), s.Customer));
        else await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(s.Dates(b), s.Customer));
    }

    [Theory]
    [InlineData(89, true)] [InlineData(90, true)] [InlineData(91, false)]
    public async Task NewCheckoutWindowBoundary(int days, bool allowed)
    {
        using var s = new Scenario(); var b = await s.Book();
        var input = new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeDates, s.Today.AddDays(days-2), s.Today.AddDays(days));
        if (allowed) Assert.NotNull(await s.Service().QuoteAsync(input, s.Customer));
        else await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(input, s.Customer));
    }

    [Theory]
    [InlineData(0, 0, 600000, 0)]
    [InlineData(1, 300000, 300000, 300000)]
    [InlineData(2, 0, 600000, 300000)]
    [InlineData(3, 0, 600000, 0)]
    public async Task FourApprovedMoneyExamples(int example, int fee, int carry, int additional)
    {
        using var s = new Scenario(); var b = await s.Book(example == 1 ? 4 : 10);
        var originalDate = b.CheckIn; var originalRoom = b.RoomId; var code = b.Code;
        ChangeBookingRequest input = example < 2 ? s.Dates(b)
            : new(b.Id, ChangeRequestType.ChangeRoomType, NewRoomTypeId: s.Type(example == 2 ? 1_500_000 : 500_000).Id);
        var request = await s.Service().SubmitAsync(input, s.Customer);
        Assert.Equal(fee, request.ChangeFee); Assert.Equal(carry, request.DepositCarriedOver);
        Assert.Equal(additional, request.AdditionalDeposit);
        var result = await s.Service().ApproveAsync(request.Id, "Đồng ý", s.Manager);
        s.Db.ChangeTracker.Clear(); b = await s.Db.Bookings.SingleAsync();
        Assert.Equal(code, b.Code); Assert.Equal(1, await s.Db.Bookings.CountAsync());
        if (additional == 0)
        {
            Assert.Equal(ChangeRequestStatus.Applied, result.Status);
            Assert.Equal(carry, b.DepositAmount); Assert.Equal(fee, b.ChangeFeeAmount);
            Assert.Equal(BookingStatus.Deposited, b.Status); Assert.Null(b.ConfirmedAt);
            Assert.Equal(s.H.Clock.Now.UtcDateTime.AddMinutes(s.H.Options.ConfirmMinutes), b.ConfirmDeadlineAt);
            Assert.Null(b.ConfirmAlertSentAt);
            Assert.Equal(example == 3 ? 1_000_000 : 2_000_000, b.TotalAmount);
            Assert.Equal(b.TotalAmount, (await s.Db.BookingNights.ToListAsync()).Sum(n => n.Price));
        }
        else
        {
            Assert.Equal(ChangeRequestStatus.AwaitingPayment, result.Status);
            Assert.Equal(result.DecidedAt!.Value.AddMinutes(s.H.Options.HoldMinutes), result.PaymentDeadlineAt);
            Assert.Equal(originalDate, b.CheckIn); Assert.Equal(originalRoom, b.RoomId);
            Assert.Equal(600_000, b.DepositAmount); Assert.Equal(0, b.ChangeFeeAmount);
            Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingChangeRequestId == request.Id));
            Assert.Equal(2, await s.Db.RoomNights.CountAsync(n => n.BookingId == b.Id));
            Assert.All(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == request.Id).ToListAsync(), n => {
                Assert.Null(n.BookingId); Assert.Null(n.RoomBlockId); Assert.Equal(result.NewRoomId, n.RoomId); });
        }
    }

    [Fact]
    public async Task CombinedChangeAppliesInPlaceAndWritesHistoryAuditNotifications()
    {
        using var s = new Scenario(); var b = await s.Book(); var roomType = s.Type(500_000);
        var beforeHistory = await s.Db.BookingStatusHistories.CountAsync();
        var beforeNotifications = await s.Db.Notifications.CountAsync();
        var input = new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeDatesAndRoomType,
            b.CheckIn.AddDays(7), b.CheckOut.AddDays(7), roomType.Id);
        var r = await s.Service().SubmitAsync(input, s.Customer);
        await s.Service().ApproveAsync(r.Id, "Đổi cả ngày và phòng", s.Manager);
        s.Db.ChangeTracker.Clear(); b = await s.Db.Bookings.SingleAsync();
        Assert.Equal(roomType.Id, b.RoomTypeId); Assert.Equal(input.NewCheckIn, b.CheckIn);
        Assert.Equal(600_000, b.DepositAmount); Assert.Equal(0, b.ChangeFeeAmount);
        Assert.Equal(beforeHistory+1, await s.Db.BookingStatusHistories.CountAsync());
        var h = await s.Db.BookingStatusHistories.OrderBy(x => x.Id).LastAsync();
        Assert.Equal(BookingStatus.Confirmed, h.FromStatus); Assert.Equal(BookingStatus.Deposited, h.ToStatus);
        Assert.Equal(s.Manager.FindFirstValue(ClaimTypes.NameIdentifier), h.ActorAccountId);
        Assert.Equal(beforeNotifications+2, await s.Db.Notifications.CountAsync());
        var audit = await s.Db.AuditLogs.SingleAsync(a => a.TableName == "Bookings");
        Assert.Contains("Confirmed", audit.OldValues); Assert.Contains("Deposited", audit.NewValues);
        Assert.Contains("RoomTypeId", audit.OldValues); Assert.Contains("RoomTypeId", audit.NewValues);
        Assert.Equal(2, await s.Db.AuditLogs.CountAsync(a => a.TableName == "BookingChangeRequests"));
        Assert.False(BookingService.CanTransition(BookingStatus.Confirmed, BookingStatus.Deposited));
    }

    [Fact]
    public async Task OverlapReusesNightsAndReleasesOnlyOldDates()
    {
        using var s = new Scenario(); var b = await s.Book(); var originalRoom = b.RoomId;
        var r = await s.Service().SubmitAsync(s.Dates(b, 1), s.Customer);
        await s.Service().ApproveAsync(r.Id, "Đồng ý", s.Manager);
        var held = await s.Db.RoomNights.AsNoTracking().OrderBy(n => n.Date).ToListAsync();
        Assert.Equal(2, held.Count); Assert.All(held, n => Assert.Equal(originalRoom, n.RoomId));
        Assert.Equal(r.NewCheckIn, held[0].Date); Assert.Equal(r.NewCheckOut!.Value.AddDays(-1), held[1].Date);
        Assert.All(held, n => Assert.Equal(b.Id, n.BookingId));
    }

    [Fact]
    public async Task FeeIsFrozenAtSubmissionRatherThanApprovalDate()
    {
        using var s = new Scenario(); var b = await s.Book(10);
        var r = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        s.H.Clock.Advance(TimeSpan.FromDays(4));
        Assert.Equal(300_000, (await s.H.Refunds().QuoteCancellationAsync(b)).RefundAmount);
        await s.Service().ApproveAsync(r.Id, "Đồng ý", s.Manager);
        Assert.Equal(0, b.ChangeFeeAmount); Assert.Equal(600_000, b.DepositAmount);
    }

    private static async Task VoucherAsync(Scenario s, Booking b)
    {
        var voucher = new Voucher { Code = "CHANGE-VOUCHER", CustomerId = s.H.Customer.Id, Amount = 100_000,
            Balance = 60_000, Reason = "Xin lỗi", ExpiresOn = s.Today.AddDays(90), IssuedByEmployeeId = s.ManagerId };
        s.Db.Vouchers.Add(voucher); b.Voucher = voucher; b.DiscountAmount = 40_000;
        await s.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task CancellationFreezesRefundAndVoucherPercentage()
    {
        using var s = new Scenario(); var b = await s.Book(4); await VoucherAsync(s,b);
        var r = await s.Service().SubmitAsync(new(b.Id, ChangeRequestType.Cancel), s.Consultant);
        Assert.Equal(280_000, r.RefundAmount);
        s.H.Clock.Advance(TimeSpan.FromDays(3));
        Assert.Equal(0, (await s.H.Refunds().QuoteCancellationAsync(b)).RefundAmount);
        await s.Service().ApproveAsync(r.Id, "Đồng ý hủy", s.Manager);
        Assert.Equal(BookingStatus.Cancelled, b.Status); Assert.Equal(280_000,b.RefundAmount);
        Assert.Equal(80_000,(await s.Db.Vouchers.SingleAsync()).Balance);
        Assert.Empty(await s.Db.RoomNights.ToListAsync());
    }

    [Theory]
    [InlineData(ChangeRequestType.ChangeDates)] [InlineData(ChangeRequestType.ChangeRoomType)]
    [InlineData(ChangeRequestType.ChangeDatesAndRoomType)]
    public async Task VoucherBlocksEveryChange(ChangeRequestType type)
    {
        using var s = new Scenario(); var b = await s.Book(); await VoucherAsync(s,b);
        var input = type == ChangeRequestType.ChangeDates ? s.Dates(b)
            : new ChangeBookingRequest(b.Id,type,type == ChangeRequestType.ChangeDatesAndRoomType ? b.CheckIn.AddDays(1) : null,
                type == ChangeRequestType.ChangeDatesAndRoomType ? b.CheckOut.AddDays(1) : null,s.Type(500_000).Id);
        var ex = await Assert.ThrowsAsync<BookingException>(() => s.Service().SubmitAsync(input,s.Customer));
        Assert.Equal(BookingChangeRequestService.VoucherBlocked,ex.Message);
        Assert.Empty(await s.Db.BookingChangeRequests.ToListAsync());
    }

    [Fact]
    public async Task DowngradeBelowCarriedDepositIsBlocked()
    {
        using var s = new Scenario();var b=await s.Book();var type=s.Type(250_000);
        var ex=await Assert.ThrowsAsync<BookingException>(()=>s.Service().QuoteAsync(new(b.Id,ChangeRequestType.ChangeRoomType,NewRoomTypeId:type.Id),s.Customer));
        Assert.Contains("nhỏ hơn cọc chuyển",ex.Message);
    }

    [Fact]
    public async Task OneOpenRequestAlsoBlocksResubmissionAfterBusinessApproval()
    {
        using var s = new Scenario();var b=await s.Book(4);
        var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        var ex=await Assert.ThrowsAsync<BookingException>(()=>s.Service().SubmitAsync(s.Dates(b),s.Customer));
        Assert.Equal(BookingChangeRequestService.OpenRequest,ex.Message);
        await s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager);
        ex=await Assert.ThrowsAsync<BookingException>(()=>s.Service().SubmitAsync(s.Dates(b),s.Customer));
        Assert.Equal(BookingChangeRequestService.OpenRequest,ex.Message);
        Assert.Equal(1,await s.Db.BookingChangeRequests.CountAsync());
    }

    [Theory]
    [InlineData(EmployeePosition.Administrator)] [InlineData(EmployeePosition.Accountant)]
    [InlineData(EmployeePosition.Consultant)]
    public async Task OtherStaffCannotApproveEvenWithForgedManagerClaim(EmployeePosition position)
    {
        using var s = new Scenario();var b=await s.Book();var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        var employee=s.Staff(position);
        var fake=Principal(employee.Person.Account!.Id,AppRoles.Employee,EmployeePosition.Manager);
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id,"Đồng ý",fake));
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().RejectAsync(r.Id,"Từ chối",fake));
    }

    [Fact]
    public async Task OwnerAndOtherCustomerCannotActOnBooking()
    {
        using var s = new Scenario();var b=await s.Book();var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        var owner=Principal(s.Db.Users.Single(u=>u.PersonId==s.H.Homestay.Owner.PersonId).Id,AppRoles.Owner);
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id,"Đồng ý",owner));
        var outsider=s.Staff(EmployeePosition.Consultant);
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().SubmitAsync(s.Dates(b),Principal(outsider.Person.Account!.Id,AppRoles.Customer)));
    }

    [Fact]
    public async Task SenderCannotApproveOwnRequest()
    {
        using var s = new Scenario();var b=await s.Book();
        var r=await s.Service().SubmitAsync(s.Dates(b),s.Manager);
        Assert.Equal(s.Manager.FindFirstValue(ClaimTypes.NameIdentifier),r.RequestedByAccountId);
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager));
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().RejectAsync(r.Id,"Từ chối",s.Manager));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task InactiveManagerCannotDecide(bool disableAccount)
    {
        using var s = new Scenario();var b=await s.Book();var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        var employee=await s.Db.Employees.Include(e=>e.Person).ThenInclude(p=>p.Account).SingleAsync(e=>e.Id==s.ManagerId);
        if(disableAccount)employee.Person.Account!.Status=AccountStatus.Disabled;else employee.EmploymentStatus=EmploymentStatus.Suspended;
        await s.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager));
    }

    [Fact]
    public async Task RepricingRejectsWithBothReasonsAndPreservesBooking()
    {
        using var s = new Scenario();var b=await s.Book();var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        foreach(var price in await s.Db.RoomTypePrices.ToListAsync())price.Price+=100_000;
        await s.Db.SaveChangesAsync();
        var result=await s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager);
        Assert.Equal(ChangeRequestStatus.Rejected,result.Status);
        Assert.Equal(BookingChangeRequestService.PriceChanged,result.DecisionNote);
        Assert.Equal(result.DecisionNote,result.RejectionReason);
        Assert.Equal(BookingStatus.Confirmed,b.Status);Assert.Equal(2_000_000,b.TotalAmount);
        Assert.Empty(await s.Db.RoomNights.Where(n=>n.BookingChangeRequestId!=null).ToListAsync());
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task LegacyRequestCanOnlyBeRejectedAndKeepsUniqueUntilThen(bool missingSender)
    {
        using var s = new Scenario();var b=await s.Book();
        var r=new BookingChangeRequest{BookingId=b.Id,RequestType=ChangeRequestType.Cancel,
            RequestedByAccountId=missingSender?null:s.Customer.FindFirstValue(ClaimTypes.NameIdentifier),
            ChangeFee=missingSender?0:null,DepositCarriedOver=0,AdditionalDeposit=0,NewTotalAmount=0,RefundAmount=0};
        s.Db.Add(r);await s.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager));
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().SubmitAsync(new(b.Id,ChangeRequestType.Cancel),s.Consultant));
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().RejectAsync(r.Id,"  ",s.Manager));
        await s.Service().RejectAsync(r.Id,"Yêu cầu cũ, vui lòng gửi lại",s.Manager);
        var stored=await s.Db.BookingChangeRequests.AsNoTracking().SingleAsync();
        Assert.Equal(ChangeRequestStatus.Rejected,stored.Status);Assert.Equal(stored.DecisionNote,stored.RejectionReason);
        Assert.NotNull(stored.DecidedAt);Assert.Equal(s.Manager.FindFirstValue(ClaimTypes.NameIdentifier),stored.DecidedByAccountId);
        await s.Service().SubmitAsync(new(b.Id,ChangeRequestType.Cancel),s.Consultant);
    }

    [Fact]
    public async Task DepositedBookingKeepsDepositTimestampAndDoesNotAddFakeStatusHistory()
    {
        using var s = new Scenario();var b=await s.Book(10,false);var depositedAt=b.DepositedAt;
        var histories=await s.Db.BookingStatusHistories.CountAsync();
        var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().ApproveAsync(r.Id," ",s.Manager));
        await s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager);
        s.Db.ChangeTracker.Clear();b=await s.Db.Bookings.SingleAsync();
        Assert.Equal(depositedAt,b.DepositedAt);Assert.Equal(histories,await s.Db.BookingStatusHistories.CountAsync());
    }

    [Theory]
    [InlineData("status")] [InlineData("checkedIn")] [InlineData("settled")] [InlineData("capacity")]
    [InlineData("otherHomestay")] [InlineData("blocked")]
    public async Task InvalidBookingOrTargetIsBlocked(string kind)
    {
        using var s = new Scenario();var b=await s.Book();var input=s.Dates(b);
        if(kind=="status")await s.H.Bookings().ChangeStatusAsync(b,BookingStatus.Cancelled,Actor.System);
        if(kind=="checkedIn"){b.CheckedInAt=s.H.Clock.Now.UtcDateTime;await s.Db.SaveChangesAsync();}
        if(kind=="settled")
        {
            var settlement=new Settlement{HomestayId=b.HomestayId,Year=2026,Month=11,PeriodNo=1};
            settlement.Items.Add(new SettlementItem{BookingId=b.Id,ItemType=SettlementItemType.Completed,RoomAmount=2_000_000,
                DepositAmount=600_000,RetainedAmount=600_000,CommissionRate=.12m,CommissionAmount=240_000,PayoutAmount=360_000});
            s.Db.Add(settlement);await s.Db.SaveChangesAsync();
        }
        if(kind is "capacity" or "otherHomestay")
        {
            var type=s.Type(500_000,kind=="capacity"?1:2);
            if(kind=="otherHomestay")
            {
                var other=new Homestay{Name="Khác",Address="Khác",OwnerId=s.H.Homestay.OwnerId,AreaId=s.H.Homestay.AreaId,Status=HomestayStatus.Active};
                s.Db.Add(other);await s.Db.SaveChangesAsync();type.HomestayId=other.Id;await s.Db.SaveChangesAsync();
            }
            input=new(b.Id,ChangeRequestType.ChangeRoomType,NewRoomTypeId:type.Id);
        }
        if(kind=="blocked")
        {
            foreach(var room in await s.Db.Rooms.ToListAsync())
            {
                var block=new RoomBlock{RoomId=room.Id,StartDate=input.NewCheckIn!.Value,EndDate=input.NewCheckOut!.Value,
                    CreatedByAccountId=s.Manager.FindFirstValue(ClaimTypes.NameIdentifier)!};
                block.Nights.Add(new RoomNight{RoomId=room.Id,Date=block.StartDate});s.Db.Add(block);
            }
            await s.Db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<BookingException>(()=>s.Service().QuoteAsync(input,s.Customer));
    }

    private static async Task<string> Snapshot(ApplicationDbContext db) => JsonSerializer.Serialize(new {
        Bookings=await db.Bookings.AsNoTracking().Select(b=>new{b.Id,b.Code,b.CheckIn,b.CheckOut,b.RoomId,b.RoomTypeId,b.TotalAmount,b.DepositAmount,b.ChangeFeeAmount,b.Status,b.ConfirmedAt,b.DepositedAt,b.ConfirmDeadlineAt,b.ConfirmAlertSentAt,b.RefundAmount,b.CancelledAt,b.CancelReason,b.UpdatedAt}).ToListAsync(),
        Nights=await db.RoomNights.AsNoTracking().OrderBy(n=>n.RoomId).ThenBy(n=>n.Date).Select(n=>new{n.RoomId,n.Date,n.BookingId,n.RoomBlockId,n.BookingChangeRequestId}).ToListAsync(),
        Prices=await db.BookingNights.AsNoTracking().OrderBy(n=>n.BookingId).ThenBy(n=>n.Date).Select(n=>new{n.BookingId,n.Date,n.Price,n.DayType}).ToListAsync(),
        Requests=await db.BookingChangeRequests.AsNoTracking().Select(r=>new{r.Id,r.Status,r.DecidedAt,r.DecidedByAccountId,r.DecisionNote,r.RejectionReason,r.NewRoomId,r.PaymentDeadlineAt,r.NewTotalAmount,r.ChangeFee,r.DepositCarriedOver,r.AdditionalDeposit,r.RefundAmount}).ToListAsync(),
        Histories=await db.BookingStatusHistories.CountAsync(),Audits=await db.AuditLogs.CountAsync(),Notifications=await db.Notifications.CountAsync() });

    private sealed class FailAfterWrite : SaveChangesInterceptor
    {
        public bool Reached { get; private set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data,int result,CancellationToken token=default)
        {Reached=true;throw new InvalidOperationException("Lỗi thử sau khi đã ghi, trước commit.");}
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailureAfterPhysicalWritesRollsBackAllBusinessTables(bool needsAdditionalDeposit)
    {
        using var s = new Scenario();var b=await s.Book(needsAdditionalDeposit ? 4 : 10);var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        var before=await Snapshot(s.Db);var failure=new FailAfterWrite();
        await using var context=s.H.NewContext(failure);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>s.Service(context).ApproveAsync(r.Id,"Đồng ý",s.Manager));
        Assert.True(failure.Reached);Assert.Equal(before,await Snapshot(s.Db));
    }

    [Fact]
    public async Task StaleDecisionDoesNotApplyTwice()
    {
        using var s = new Scenario();var b=await s.Book();var r=await s.Service().SubmitAsync(s.Dates(b),s.Customer);
        await using var stale=s.H.NewContext();
        await stale.BookingChangeRequests.SingleAsync();await stale.Bookings.SingleAsync();
        await s.Service().ApproveAsync(r.Id,"Đồng ý",s.Manager);
        var before=await Snapshot(s.Db);
        var ex=await Assert.ThrowsAsync<BookingException>(()=>s.Service(stale).ApproveAsync(r.Id,"Đồng ý lần nữa",s.Manager));
        Assert.Equal(BookingChangeRequestService.Processed,ex.Message);Assert.Equal(before,await Snapshot(s.Db));
    }

    [Fact]
    public async Task CompetingRequestsForSameRoomHaveOnlyOneWinner()
    {
        var path=Path.Combine(Path.GetTempPath(),"booking-change-"+Guid.NewGuid().ToString("N")+".sqlite");
        using var s = new Scenario($"DataSource={path};Default Timeout=5");
        var b1=await s.Book();var b2=await s.Book();var type=s.Type(500_000);
        var r1=await s.Service().SubmitAsync(new(b1.Id,ChangeRequestType.ChangeRoomType,NewRoomTypeId:type.Id),s.Customer);
        var r2=await s.Service().SubmitAsync(new(b2.Id,ChangeRequestType.ChangeRoomType,NewRoomTypeId:type.Id),s.Customer);
        async Task<bool> Attempt(int id)
        {
            await using var db=s.H.NewIndependentContext();
            try{await s.Service(db).ApproveAsync(id,"Đồng ý",s.Manager);return true;}
            catch(BookingException ex){Assert.Equal(BookingChangeRequestService.NoRooms,ex.Message);return false;}
        }
        var outcomes=await Task.WhenAll(Task.Run(()=>Attempt(r1.Id)),Task.Run(()=>Attempt(r2.Id)));
        Assert.Single(outcomes,x=>x);Assert.Single(outcomes,x=>!x);
        s.Db.ChangeTracker.Clear();
        Assert.Equal(1,await s.Db.BookingChangeRequests.CountAsync(r=>r.Status==ChangeRequestStatus.Applied));
        Assert.Equal(1,await s.Db.BookingChangeRequests.CountAsync(r=>r.Status==ChangeRequestStatus.Pending));
        Assert.Equal(2,await s.Db.RoomNights.CountAsync(n=>n.RoomId==type.Rooms.Single().Id));
        // File SQLite tạm được giữ để kiểm tra khi cần; không xóa database.
    }
    private sealed class RoomCollision(int bookingId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken token = default)
        {
            var db = data.Context!;
            var night = db.ChangeTracker.Entries<RoomNight>().First(e => e.State == EntityState.Added).Entity;
            // Chèn sau lúc kiểm tra phòng để SQL thực sự báo trùng PK khi SaveChanges tiếp tục.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoomNights (RoomId, Date, BookingId) VALUES ({night.RoomId}, {night.Date}, {bookingId})", token);
            return result;
        }
    }

    [Fact]
    public async Task ActualRoomNightPrimaryKeyCollisionIsTranslatedAndRolledBack()
    {
        using var s = new Scenario(); var b = await s.Book();
        var r = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        var before = await Snapshot(s.Db);
        await using var context = s.H.NewContext(new RoomCollision(b.Id));
        var ex = await Assert.ThrowsAsync<BookingException>(() => s.Service(context).ApproveAsync(r.Id, "Đồng ý", s.Manager));
        Assert.Equal(BookingChangeRequestService.NoRooms, ex.Message);
        Assert.Equal(before, await Snapshot(s.Db));
    }

    [Fact]
    public async Task QuoteIsReadOnlyAndConfiguredLimitsAreUsed()
    {
        using var s = new Scenario(); var b = await s.Book(4);
        var before = await Snapshot(s.Db);
        await s.Service().QuoteAsync(s.Dates(b), s.Customer);
        Assert.Equal(before, await Snapshot(s.Db));
        s.H.Options.ChangeMinDaysBefore = 5;
        await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(s.Dates(b), s.Customer));
        s.H.Options.ChangeMinDaysBefore = 3;
        s.H.Options.ChangeWindowDays = 7;
        await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(s.Dates(b), s.Customer));
    }

    [Fact]
    public async Task DowngradeAtExactCarriedDepositBoundaryIsAccepted()
    {
        using var s = new Scenario(); var b = await s.Book(); var type = s.Type(300_000);
        var r = await s.Service().SubmitAsync(new(b.Id, ChangeRequestType.ChangeRoomType, NewRoomTypeId: type.Id), s.Customer);
        await s.Service().ApproveAsync(r.Id, "Đồng ý", s.Manager);
        Assert.Equal(600_000, b.TotalAmount); Assert.Equal(b.TotalAmount, b.DepositAmount);
        Assert.Equal(0, r.AdditionalDeposit);
    }

    [Fact]
    public async Task TwoAppliedChangesAccumulateFeesOnSameBookingAndBlockThird()
    {
        using var s = new Scenario(); var b = await s.Book(); var code = b.Code;
        var first = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        await s.Service().ApproveAsync(first.Id, "Đồng ý lần 1", s.Manager);
        s.H.Clock.Advance(TimeSpan.FromDays(13));
        var type = s.Type(500_000);
        var second = await s.Service().SubmitAsync(new(b.Id, ChangeRequestType.ChangeDatesAndRoomType,
            b.CheckIn.AddDays(7), b.CheckOut.AddDays(7), type.Id), s.Customer);
        Assert.Equal(300_000, second.ChangeFee); Assert.Equal(0, second.AdditionalDeposit);
        await s.Service().ApproveAsync(second.Id, "Đồng ý lần 2", s.Manager);
        Assert.Equal(code, b.Code); Assert.Equal(300_000, b.ChangeFeeAmount); Assert.Equal(300_000, b.DepositAmount);
        Assert.Equal(2, await s.Db.BookingChangeRequests.CountAsync(r => r.BookingId == b.Id && r.Status == ChangeRequestStatus.Applied));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().SubmitAsync(s.Dates(b), s.Customer));
    }

    [Theory]
    [InlineData(ChangeRequestType.ChangeDates)]
    [InlineData(ChangeRequestType.ChangeRoomType)]
    [InlineData(ChangeRequestType.ChangeDatesAndRoomType)]
    public async Task UnchangedFieldsCannotConsumeAChangeOrChargeFee(ChangeRequestType type)
    {
        using var s = new Scenario(); var b = await s.Book(4);
        var input = new ChangeBookingRequest(b.Id, type,
            type == ChangeRequestType.ChangeRoomType ? null : b.CheckIn,
            type == ChangeRequestType.ChangeRoomType ? null : b.CheckOut,
            type == ChangeRequestType.ChangeDates ? null : b.RoomTypeId);
        var before = await Snapshot(s.Db);
        await Assert.ThrowsAsync<BookingException>(() => s.Service().SubmitAsync(input, s.Customer));
        Assert.Equal(before, await Snapshot(s.Db));
    }

    private sealed class LostDecision : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken token = default) => throw new DbUpdateConcurrencyException();
    }

    [Fact]
    public async Task ConcurrencyExceptionIsTranslatedWithoutPartialChanges()
    {
        using var s = new Scenario(); var b = await s.Book();
        var r = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        var before = await Snapshot(s.Db);
        await using var db = s.H.NewContext(new LostDecision());
        var ex = await Assert.ThrowsAsync<BookingException>(() => s.Service(db).ApproveAsync(r.Id, "Đồng ý", s.Manager));
        Assert.Equal(BookingChangeRequestService.Processed, ex.Message);
        Assert.Equal(before, await Snapshot(s.Db));
    }

    [Fact]
    public async Task CustomerMustUseImmediateCancellationInsteadOfSendingCancelRequest()
    {
        using var s = new Scenario(); var booking = await s.Book();
        var ex = await Assert.ThrowsAsync<BookingException>(() => s.Service().SubmitAsync(new(booking.Id, ChangeRequestType.Cancel), s.Customer));
        Assert.Equal("Vui lòng dùng nút hủy đơn", ex.Message);
        Assert.Empty(await s.Db.BookingChangeRequests.ToListAsync());
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

}
