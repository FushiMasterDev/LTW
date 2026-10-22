using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class GuestCustomerServiceTests
{
    private static readonly Actor Guest = new(ActorType.Customer);

    [Fact]
    public async Task NewPhoneCreatesPersonAndCustomerWithoutAccount()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var customer = await h.Guests().FindOrCreateAsync("0977 555 001", " Khách Mới ", "khach@fuji.vn", Guest);
        var person = await h.Db.PersonProfiles.Include(p => p.Account).SingleAsync(p => p.Id == customer.PersonId);
        Assert.Equal("+84977555001", person.PhoneNumber); Assert.Equal("Khách Mới", person.FullName); Assert.Equal("khach@fuji.vn", person.Email);
        Assert.Null(person.Account);
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.TableName == "Customers" && a.RecordId == customer.Id.ToString() && a.Action == AuditAction.Insert);
        // Gọi lại cùng SĐT dùng lại đúng khách, không tạo thêm hồ sơ.
        Assert.Equal(customer.Id, (await h.Guests().FindOrCreateAsync("+84977555001", "Tên khác", null, Guest)).Id);
        Assert.Equal(1, await h.Db.PersonProfiles.CountAsync(p => p.PhoneNumber == "+84977555001"));
        Assert.Equal("Khách Mới", (await h.Db.PersonProfiles.SingleAsync(p => p.Id == customer.PersonId)).FullName);
    }

    [Fact]
    public async Task AccountHolderKeepsNameAndEmailAndReusesCustomer()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var person = await h.Db.PersonProfiles.SingleAsync(p => p.Id == h.H.Customer.PersonId);
        var (name, email) = (person.FullName, person.Email);
        var customer = await h.Guests().FindOrCreateAsync(person.PhoneNumber, "Người khác đặt hộ", "khac@fuji.vn", Guest);
        Assert.Equal(h.H.Customer.Id, customer.Id);
        var after = await h.Db.PersonProfiles.AsNoTracking().SingleAsync(p => p.Id == person.Id);
        Assert.Equal(name, after.FullName); Assert.Equal(email, after.Email);
    }

    [Fact]
    public async Task OwnerWithAccountGetsCustomerAndCustomerRole()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var ownerPerson = await h.Db.PersonProfiles.Include(p => p.Account).SingleAsync(p => p.Id == h.H.Homestay.Owner.PersonId);
        Assert.False(await h.Db.Customers.AnyAsync(c => c.PersonId == ownerPerson.Id));
        var customer = await h.Guests().FindOrCreateAsync(ownerPerson.PhoneNumber, "Chủ đi du lịch", null, Guest);
        Assert.Equal(ownerPerson.Id, customer.PersonId);
        Assert.True(await h.P.Users.IsInRoleAsync(ownerPerson.Account!, AppRoles.Customer));
    }

    [Theory]
    [InlineData("12345", null)] [InlineData("0977555002", "khong-phai-email")]
    public async Task InvalidPhoneOrEmailIsRejected(string phone, string? email)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Guests().FindOrCreateAsync(phone, "Khách", email, Guest));
        Assert.False(await h.Db.PersonProfiles.AnyAsync(p => p.PhoneNumber == "+84977555002"));
    }

    [Fact]
    public async Task EmployeePhoneCannotBeUsedAsGuest()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var phone = (await h.Db.Employees.Include(e => e.Person).FirstAsync()).Person.PhoneNumber;
        await Assert.ThrowsAsync<BookingException>(() => h.Guests().FindOrCreateAsync(phone, "Nhân viên", null, Guest));
    }

    [Fact]
    public async Task UniqueViolationDuringSaveIsRetriedWithoutDuplicate()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        // Ngay trước lần lưu đầu, một bản cùng SĐT xuất hiện → unique lỗi → rollback và chạy lại đúng một lần.
        var race = new RaceInterceptor("+84977555003");
        await using var db = h.H.NewContext(race);
        var guests = new GuestCustomerService(db, h.P.Customers(db), h.H.Clock);
        var customer = await guests.FindOrCreateAsync("0977555003", "Khách đua", null, Guest);
        Assert.True(race.Fired);
        Assert.Equal(1, await h.Db.PersonProfiles.CountAsync(p => p.PhoneNumber == "+84977555003"));
        Assert.Equal(customer.Id, (await h.Db.Customers.SingleAsync(c => c.Person.PhoneNumber == "+84977555003")).Id);
    }

    [Fact]
    public async Task ConsultantBooksForBrandNewPhoneCustomer()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var booking = await h.Staff().CreateForCustomerAsync(new("0977555004", "Khách gọi điện", null, h.H.RoomType.Id,
            h.Today.AddDays(5), h.Today.AddDays(7), 2, null, SourceChannel.Phone), h.Consultant);
        var stored = await h.Db.Bookings.Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account).SingleAsync(b => b.Id == booking.Id);
        Assert.Equal("+84977555004", stored.Customer.Person.PhoneNumber); Assert.Null(stored.Customer.Person.Account);
        Assert.Equal(h.Consultant.EmployeeId, stored.CreatedByEmployeeId); Assert.Equal(h.Consultant.EmployeeId, stored.AssignedEmployeeId);
        Assert.Equal(SourceChannel.Phone, stored.SourceChannel); Assert.Equal(BookingStatus.AwaitingPayment, stored.Status);
    }

    [Fact]
    public async Task StaffBookingRejectsWebChannelAndAccountant()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        StaffBookingInput Input(SourceChannel channel) => new("0977555005", "Khách", null, h.H.RoomType.Id, h.Today.AddDays(5), h.Today.AddDays(7), 2, null, channel);
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().CreateForCustomerAsync(Input(SourceChannel.Web), h.Consultant));
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().CreateForCustomerAsync(Input(SourceChannel.Zalo), h.P.Accountant));
        Assert.False(await h.Db.PersonProfiles.AnyAsync(p => p.PhoneNumber == "+84977555005"));
        Assert.False(await h.Db.Bookings.AnyAsync(b => b.ContactPhone == "+84977555005"));
    }

    private sealed class RaceInterceptor(string phone) : SaveChangesInterceptor
    {
        public bool Fired { get; private set; }
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Fired && eventData.Context!.ChangeTracker.Entries<PersonProfile>().Any(e => e.State == EntityState.Added && e.Entity.PhoneNumber == phone))
            {
                Fired = true;
                await eventData.Context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO PersonProfiles (PhoneNumber, FullName, CreatedAt, UpdatedAt) VALUES ({phone}, {"Yêu cầu song song"}, {DateTime.UtcNow}, {DateTime.UtcNow})", cancellationToken);
            }
            return result;
        }
    }
}
