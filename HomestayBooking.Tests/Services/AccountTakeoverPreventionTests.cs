using System.Text.Json;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class AccountTakeoverPreventionTests
{
    private const string Phone = "0987654321";
    private const string E164 = "+84987654321";

    private static async Task<PersonProfile> GuestAsync(PartnerTestHost h, bool customer = true)
    {
        var person = new PersonProfile { FullName = "Khách vãng lai", PhoneNumber = E164,
            Email = "guest@example.test", Address = "Địa chỉ cũ",
            Customer = customer ? new Customer { Note = "Nội bộ", Profile = new CustomerProfile { CitizenId = "123456789012" } } : null };
        h.Db.Add(person);
        await h.Db.SaveChangesAsync();
        return person;
    }

    private static Task<Booking> BookAsync(PartnerTestHost h, int customerId, int days = 5) =>
        h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(days), h.Today.AddDays(days + 1)) with
        { CustomerId = customerId, ContactPhone = "+84900000000" });

    private static async Task<RegisterResult> RegisterAsync(PartnerTestHost h, bool owner, string? code,
        string phone = Phone)
    {
        if (!owner)
            return await h.Customers().RegisterAsync("Tên mới", phone, "new@example.test", PartnerTestHost.Password, code);
        try
        {
            var registration = await h.Registrations.RegisterAsync(h.Input(phone) with { BookingCode = code }, PartnerTestHost.Password);
            return new(await h.Db.Users.SingleAsync(u => u.PersonId == registration.Owner.PersonId), []);
        }
        catch (BookingException ex) { return new(null, [ex.ResourceKey]); }
    }

    // Đọc bằng context khác để kiểm tra trạng thái đã lưu, kể cả khi request bị rollback/clear tracker.
    private static async Task<string> SnapshotAsync(PartnerTestHost h)
    {
        await using var db = h.H.NewContext();
        return JsonSerializer.Serialize(new
        {
            People = await db.PersonProfiles.OrderBy(p => p.Id).Select(p => new
                { p.Id, p.FullName, p.PhoneNumber, p.Email, p.Address, p.CreatedAt, p.UpdatedAt }).ToListAsync(),
            Customers = await db.Customers.OrderBy(c => c.Id).Select(c => new { c.Id, c.PersonId, c.Note }).ToListAsync(),
            Profiles = await db.CustomerProfiles.OrderBy(p => p.CustomerId).ToListAsync(),
            Accounts = await db.Users.CountAsync(), Roles = await db.UserRoles.CountAsync(),
            Owners = await db.Owners.CountAsync(), Registrations = await db.OwnerRegistrations.CountAsync(),
            Homestays = await db.Homestays.CountAsync(), Audits = await db.AuditLogs.CountAsync(),
            Activity = await db.AccountActivityLogs.CountAsync(), Bookings = await db.Bookings.CountAsync(),
        });
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ProfileWithoutBookingsKeepsExistingBehavior(bool owner, bool customer)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h, customer);
        var count = await h.Db.PersonProfiles.CountAsync();
        var result = await RegisterAsync(h, owner, null);
        Assert.True(result.Succeeded, string.Join(";", result.Errors));
        Assert.Equal(person.Id, result.User!.PersonId);
        Assert.Equal(count, await h.Db.PersonProfiles.CountAsync());
        Assert.DoesNotContain(await h.Db.AccountActivityLogs.ToListAsync(), l => l.Detail?.Contains("xác minh bằng mã đơn") == true);
    }

    public static IEnumerable<object?[]> InvalidCodes() =>
        new[] { false, true }.SelectMany(owner => new string?[] { null, "  ", "WRONG", "other" }
            .Select(code => new object?[] { owner, code }));

    [Theory, MemberData(nameof(InvalidCodes))]
    public async Task MissingWrongAndOtherCustomersCodeRefuseWithoutAnyWrite(bool owner, string? code)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        await BookAsync(h, person.Customer!.Id);
        var other = await BookAsync(h, h.H.Customer.Id, 10);
        // SĐT liên hệ trùng vẫn không chứng minh đơn thuộc Customer của hồ sơ cần nhận.
        other.ContactPhone = E164;
        await h.Db.SaveChangesAsync();
        if (code == "other") code = other.Code;
        var before = await SnapshotAsync(h);

        var result = await RegisterAsync(h, owner, code);

        Assert.False(result.Succeeded);
        Assert.Equal(BookingCodeVerificationService.VerificationRequired, Assert.Single(result.Errors));
        Assert.Equal(before, await SnapshotAsync(h));
    }

    public static IEnumerable<object[]> BookingStatuses() =>
        new[] { false, true }.SelectMany(owner => Enum.GetValues<BookingStatus>()
            .Select(status => new object[] { owner, status }));

    [Theory, MemberData(nameof(BookingStatuses))]
    public async Task CorrectCodeTrimmedAndCaseInsensitiveLinksExactProfileForEveryStatus(bool owner, BookingStatus status)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        var booking = await BookAsync(h, person.Customer!.Id);
        booking.Status = status;
        if (status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed or BookingStatus.NoShow)
            booking.ConfirmedAt = h.H.Clock.GetUtcNow().UtcDateTime;
        await h.Db.SaveChangesAsync();
        var people = await h.Db.PersonProfiles.CountAsync();
        var result = await RegisterAsync(h, owner, "  " + booking.Code.ToLowerInvariant() + "  ");

        Assert.True(result.Succeeded, string.Join(";", result.Errors));
        Assert.Equal(person.Id, result.User!.PersonId);
        Assert.Equal(people, await h.Db.PersonProfiles.CountAsync());
        Assert.Equal(person.Customer.Id, (await h.Db.Bookings.SingleAsync(b => b.Id == booking.Id)).CustomerId);
        var log = Assert.Single(await h.Db.AccountActivityLogs.Where(l => l.EventType == AccountEventType.AccountCreated).ToListAsync());
        Assert.Equal(result.User.Id, log.AccountId);
        Assert.Equal(owner ? ActorType.Owner : ActorType.Customer, log.ActorType);
        Assert.Contains("xác minh bằng mã đơn", log.Detail);
        Assert.DoesNotContain(booking.Code, log.Detail);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FifthFailureBlocksEvenCorrectSixthAttemptUntilFifteenMinutesAfterLock(bool owner)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        var booking = await BookAsync(h, person.Customer!.Id);
        var before = await SnapshotAsync(h);
        for (var i = 0; i < 5; i++)
        {
            var result = await RegisterAsync(h, owner, "wrong");
            Assert.False(result.Succeeded);
            Assert.Equal(i == 4 ? BookingCodeVerificationService.TooManyAttempts : BookingCodeVerificationService.VerificationRequired,
                Assert.Single(result.Errors));
            if (i < 4) h.H.Clock.Advance(TimeSpan.FromMinutes(1));
        }
        Assert.Equal(BookingCodeVerificationService.TooManyAttempts,
            Assert.Single((await RegisterAsync(h, owner, booking.Code, E164)).Errors));
        h.H.Clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        Assert.False((await RegisterAsync(h, owner, booking.Code)).Succeeded);
        Assert.Equal(before, await SnapshotAsync(h));
        h.H.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True((await RegisterAsync(h, owner, booking.Code)).Succeeded);
    }

    [Fact]
    public async Task BothRegistrationPathsAndPhoneFormatsShareCounterButOtherPhonesAreIndependent()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        var booking = await BookAsync(h, person.Customer!.Id);
        for (var i = 0; i < 5; i++)
            Assert.False((await RegisterAsync(h, i % 2 == 0, null, i % 2 == 0 ? Phone : E164)).Succeeded);
        Assert.Equal(BookingCodeVerificationService.TooManyAttempts,
            Assert.Single((await RegisterAsync(h, false, booking.Code)).Errors));
        Assert.True((await RegisterAsync(h, false, null, "0977123456")).Succeeded);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailuresOutsideRollingWindowDoNotLockPhone(bool owner)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        var booking = await BookAsync(h, person.Customer!.Id);
        for (var i = 0; i < 4; i++) Assert.False((await RegisterAsync(h, owner, null)).Succeeded);
        h.H.Clock.Advance(TimeSpan.FromMinutes(15));
        Assert.Equal(BookingCodeVerificationService.VerificationRequired,
            Assert.Single((await RegisterAsync(h, owner, "wrong")).Errors));
        Assert.True((await RegisterAsync(h, owner, booking.Code)).Succeeded);
    }

    [Fact]
    public async Task AssistedRegistrationStillReusesGuestWithBookingsWithoutVerification()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var person = await GuestAsync(h);
        await BookAsync(h, person.Customer!.Id);
        var registration = await h.Registrations.CreateAssistedAsync(h.Input(Phone), h.Consultant);
        Assert.Equal(person.Id, registration.Owner.PersonId);
        Assert.False(await h.Db.Users.AnyAsync(u => u.PersonId == person.Id));
    }
}
