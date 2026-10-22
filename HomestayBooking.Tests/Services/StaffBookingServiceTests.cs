using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class StaffBookingServiceTests
{
    [Theory]
    [InlineData("code")] [InlineData("contact")] [InlineData("customer")]
    [InlineData("national")] [InlineData("e164")] [InlineData("customer-phone")]
    public async Task SearchMatchesEachIdentityFieldAndFilters(string field)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        b.ContactName = "Người ở khác"; b.ContactPhone = "+84988888888"; await h.Db.SaveChangesAsync();
        var term = field switch { "code" => b.Code, "contact" => "Người ở", "customer" => "Nguyễn Văn Minh",
            "national" => "0988 888 888", "e164" => "+84988888888", _ => "0987000001" };
        var query = new StaffBookingQuery { Q = term, Status = BookingStatus.Confirmed, HomestayId = b.HomestayId,
            CheckInFrom = b.CheckIn, CheckInTo = b.CheckIn, AssignedEmployeeId = h.Consultant.EmployeeId };
        Assert.Equal(b.Id, Assert.Single(await h.Staff().SearchAsync(query, h.Consultant)).Id);
        query.Status = BookingStatus.Completed; Assert.Empty(await h.Staff().SearchAsync(query, h.Manager));
        query.Status = b.Status; query.Unassigned = true; Assert.Empty(await h.Staff().SearchAsync(query, h.Manager));
    }

    [Fact]
    public async Task ConsultantMayReceiveUnassignedThenReassignOwnBookingButNotOthers()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var creator = b.CreatedByEmployeeId;
        await h.Staff().AssignAsync(b.Id, h.Manager.EmployeeId!.Value, h.Consultant);
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().AssignAsync(b.Id, h.Consultant.EmployeeId!.Value, h.Consultant));
        await h.Staff().AssignAsync(b.Id, h.Consultant.EmployeeId!.Value, h.Manager);
        b = await h.Db.Bookings.SingleAsync(); b.AssignedEmployeeId = null; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().AssignAsync(b.Id, h.Manager.EmployeeId!.Value, h.Consultant));
        var assigned = await h.Staff().AssignAsync(b.Id, h.Consultant.EmployeeId!.Value, h.Consultant);
        Assert.Equal(creator, assigned.CreatedByEmployeeId);
        Assert.Contains(await h.Db.Notifications.ToListAsync(), n => n.UserId == h.Consultant.AccountId && n.Message.Contains(b.Code));
        var audits = await h.Db.AuditLogs.Where(a => a.TableName == "Bookings" && a.RecordId == b.Id.ToString()).ToListAsync();
        Assert.Equal(3, audits.Count); Assert.All(audits, a => { Assert.NotNull(a.OldValues); Assert.NotNull(a.NewValues); });
    }

    [Theory]
    [InlineData("accountant")] [InlineData("admin")] [InlineData("inactive")] [InlineData("locked")]
    public async Task AssignmentRequiresActiveConsultantOrManagerRecipient(string kind)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var recipient = kind == "accountant" ? h.P.Accountant : kind == "admin" ? h.P.Admin : h.Manager;
        if (kind == "inactive") (await h.Db.Employees.SingleAsync(e => e.Id == recipient.EmployeeId)).EmploymentStatus = EmploymentStatus.Suspended;
        if (kind == "locked") (await h.Db.Users.SingleAsync(u => u.Id == recipient.AccountId)).Status = AccountStatus.Locked;
        await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().AssignAsync(b.Id, recipient.EmployeeId!.Value, h.Manager));
        Assert.Equal(h.Consultant.EmployeeId, (await h.Db.Bookings.SingleAsync()).AssignedEmployeeId);
    }

    [Theory]
    [InlineData("accountant")] [InlineData("owner")] [InlineData("customer")] [InlineData("wrong-account")]
    public async Task AllStaffOperationsRequireDatabaseVerifiedEmployee(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        var actor = who == "accountant" ? h.P.Accountant : who == "owner" ? h.Owner : who == "customer" ? new Actor(ActorType.Customer) : h.Consultant with { AccountId = h.Manager.AccountId };
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().SearchAsync(new(), actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().DetailsAsync(b.Id, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().RoomTypesAsync(b.HomestayId, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().AssignAsync(b.Id, h.Manager.EmployeeId!.Value, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, BookingStatus.CheckedIn, null, "Đã gọi", actor));
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)] [InlineData(BookingStatus.Deposited)] [InlineData(BookingStatus.PaymentSubmitted)] [InlineData(BookingStatus.Expired)]
    public async Task StaffCannotBypassCancellationRequestOrFinance(BookingStatus target)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, target, null, "Không được", h.Manager));
        Assert.Equal(BookingStatus.Confirmed, (await h.Db.Bookings.SingleAsync()).Status);
    }

    [Fact]
    public async Task StaffConfirmationRoomSwapAndStatusHistoryRecordEmployeeAndRequiredNote()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(BookingStatus.Deposited);
        var room = (await h.H.Bookings().FreeRoomIdsAsync(b.RoomTypeId, b.CheckIn, b.CheckOut)).Single();
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, BookingStatus.Confirmed, room, " ", h.Consultant));
        await h.Staff().ChangeStatusAsync(b.Id, BookingStatus.Confirmed, room, "Đã gọi chủ xác nhận", h.Consultant);
        b = await h.Db.Bookings.SingleAsync(); Assert.Equal(room, b.RoomId); Assert.Equal(BookingStatus.Confirmed, b.Status);
        var details = await h.Staff().DetailsAsync(b.Id, h.Manager);
        var history = details.History.Last(); Assert.Equal(ActorType.Employee, history.ActorType); Assert.Equal(h.Consultant.AccountId, history.ActorAccountId);
        Assert.Contains("Đã gọi chủ", history.Note); Assert.Contains("Đổi phòng", history.Note);
        Assert.Single(details.Audit); Assert.Equal(h.Consultant.AccountId, details.Audit[0].AccountId);
    }

    [Fact]
    public async Task ExistingDateRulesStillGateNoShowAndCheckinThenAllowCompletion()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, BookingStatus.NoShow, null, "Gọi khách", h.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, BookingStatus.CheckedIn, null, "Gọi chủ", h.Consultant));
        h.H.Clock.Advance(TimeSpan.FromDays(5)); await h.Staff().ChangeStatusAsync(b.Id, BookingStatus.CheckedIn, null, "Chủ xác nhận đã đến", h.Manager);
        await h.Staff().ChangeStatusAsync(b.Id, BookingStatus.Completed, null, "Chủ xác nhận trả phòng", h.P.Admin);
        Assert.Equal(BookingStatus.Completed, (await h.Db.Bookings.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(5, false)] [InlineData(6, true)]
    public async Task StaffNoShowUsesExistingDayBoundary(int days, bool allowed)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); h.H.Clock.Advance(TimeSpan.FromDays(days));
        if (allowed)
        {
            await h.Staff().ChangeStatusAsync(b.Id, BookingStatus.NoShow, null, "Chủ xác nhận khách không đến", h.Consultant);
            Assert.Equal(BookingStatus.NoShow, (await h.Db.Bookings.SingleAsync()).Status);
        }
        else await Assert.ThrowsAsync<BookingException>(() => h.Staff().ChangeStatusAsync(b.Id, BookingStatus.NoShow, null, "Chưa hết ngày", h.Consultant));
    }

    [Fact]
    public async Task SearchPaginatesAndDetailsOrderMoneyChangesAndAudits()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        for (var i = 0; i < 12; i++)
        {
            var b = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(5 + i * 2), h.Today.AddDays(7 + i * 2)));
            await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        }
        var first = await h.Staff().SearchAsync(new() { Unassigned = true, Page = 1 }, h.Manager);
        var second = await h.Staff().SearchAsync(new() { Unassigned = true, Page = 2 }, h.Manager);
        Assert.Equal(12, first.TotalCount); Assert.Equal(10, first.Count); Assert.Equal(2, second.Count);
        var booking = first.First();
        await h.Staff().AssignAsync(booking.Id, h.Consultant.EmployeeId!.Value, h.Manager);
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await h.Staff().ChangeStatusAsync(booking.Id, BookingStatus.Confirmed, null, "Đã gọi chủ", h.Consultant);
        var customer = HomestayTestHost.Principal(h.Db.Users.Single(u => u.PersonId == h.H.Customer.PersonId).Id, "Customer");
        var request = await h.H.Changes().SubmitAsync(new(booking.Id, ChangeRequestType.ChangeDates, booking.CheckIn.AddDays(1), booking.CheckOut.AddDays(1)), customer);
        h.Db.Payments.Add(new() { BookingId = booking.Id, Type = PaymentType.Deposit, Method = PaymentMethod.BankAutoMatch, Amount = booking.AmountDue, Status = PaymentStatus.Approved });
        await h.Db.SaveChangesAsync();
        var details = await h.Staff().DetailsAsync(booking.Id, h.Manager);
        Assert.Equal(request.Id, Assert.Single(details.Changes).Id); Assert.Single(details.Payments); Assert.Equal(2, details.Audit.Count);
        Assert.Equal(details.History.OrderBy(x => x.ChangedAt).ThenBy(x => x.Id).Select(x => x.Id), details.History.Select(x => x.Id));
        Assert.Equal(details.Audit.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => x.Id), details.Audit.Select(x => x.Id));
    }
}
