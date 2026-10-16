using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class CustomerServiceTests
{
    [Theory]
    [InlineData(null, "Ghi chú cũ")] [InlineData("", null)]
    public async Task ProfileOnlyEditPreservesNoteUnlessExplicitlyCleared(string? note, string? expected)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var customer = new Customer { Note = "Ghi chú cũ", Person = new() { PhoneNumber = "+84912345678", FullName = "Tên cũ" } };
        h.Db.Add(customer); await h.Db.SaveChangesAsync();
        var result = await h.Customers().UpdateAsync(customer.Id, new(note, FullName: "Tên mới"), h.Consultant);
        Assert.Equal("Tên mới", result.FullName); Assert.Equal(expected, result.Note);
        h.Db.ChangeTracker.Clear();
        Assert.Equal(expected, (await h.Db.Customers.SingleAsync(c => c.Id == customer.Id)).Note);
        var log = await h.Db.AuditLogs.SingleAsync();
        var old = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(log.OldValues!);
        var updated = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(log.NewValues!).GetProperty("Values");
        Assert.Equal("Ghi chú cũ", old.GetProperty("Note").GetString());
        Assert.Equal(expected, updated.GetProperty("Note").GetString());
    }

    [Theory]
    [InlineData("name")] [InlineData("national")] [InlineData("e164")] [InlineData("email")]
    public async Task SearchNormalizesPhoneAndDetailsIncludeBookingsAndOnlyUsableVouchers(string field)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var b = await h.BookingAsync(); var v = await h.VoucherAsync();
        var c = await h.Db.Customers.Include(c => c.Person).SingleAsync(); c.Person.Email = "khach@example.com"; await h.Db.SaveChangesAsync();
        var term = field == "name" ? "Nguyễn" : field == "national" ? "0987 000 001" : field == "e164" ? "+84987000001" : "khach@example.com";
        Assert.Equal(c.Id, Assert.Single(await h.Customers().SearchAsync(term, 1, h.Consultant)).Id);
        Assert.Empty(await h.Customers().SearchAsync("Không có", 1, h.Manager));
        var details = await h.Customers().DetailsAsync(c.Id, h.Consultant);
        Assert.Equal(b.Id, Assert.Single(details.Bookings).Id); Assert.Equal(v.Id, Assert.Single(details.UsableVouchers).Id);
        v.Status = VoucherStatus.Expired; await h.Db.SaveChangesAsync(); Assert.Empty((await h.Customers().DetailsAsync(c.Id, h.Manager)).UsableVouchers);
    }

    [Theory]
    [InlineData("consultant", "********9012", "****5678")]
    [InlineData("manager", "123456789012", "12345678")]
    [InlineData("admin", "123456789012", "12345678")]
    public async Task SensitiveIdsAreMaskedInDetailsSearchAndUpdateResponses(string who, string citizen, string student)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var c = await h.Db.Customers.Include(c => c.Profile).SingleAsync();
        c.Profile!.CitizenId = "123456789012"; c.Profile.StudentId = "12345678"; await h.Db.SaveChangesAsync();
        var actor = who == "consultant" ? h.Consultant : who == "manager" ? h.Manager : h.P.Admin;
        var details = (await h.Customers().DetailsAsync(c.Id, actor)).Customer;
        var search = Assert.Single(await h.Customers().SearchAsync(null, 1, actor));
        var updated = await h.Customers().UpdateAsync(c.Id, new("Ghi chú"), actor);
        foreach (var dto in new[] { details, search, updated }) { Assert.Equal(citizen, dto.CitizenId); Assert.Equal(student, dto.StudentId); }
        Assert.Equal("123456789012", c.Profile.CitizenId);
    }

    [Theory]
    [InlineData("phone")] [InlineData("name")] [InlineData("email")] [InlineData("note")]
    public async Task AccountCustomerIdentityAndNoteLengthAreProtected(string field)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var id = h.H.Customer.Id;
        var input = field == "phone" ? new CustomerUpdate("", PhoneNumber: "0912345678") : field == "name" ? new CustomerUpdate("", FullName: "Khác")
            : field == "email" ? new CustomerUpdate("", Email: "new@example.com") : new CustomerUpdate(new string('X', 1001));
        await Assert.ThrowsAsync<BookingException>(() => h.Customers().UpdateAsync(id, input, h.Manager));
        Assert.Empty(h.Db.AuditLogs); Assert.Null((await h.Db.Customers.SingleAsync()).Note);
    }

    [Fact]
    public async Task PhoneCustomerWithoutAccountMayEditNameEmailAndNoteWithAudit()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var c = new Customer { Person = new() { PhoneNumber = "+84912345678", FullName = "Điện thoại" } };
        h.Db.Add(c); await h.Db.SaveChangesAsync();
        var dto = await h.Customers().UpdateAsync(c.Id, new(new string('X', 1000), "Tên mới", "new@example.com", "0912345678"), h.Consultant);
        Assert.Equal("Tên mới", dto.FullName); Assert.Equal("new@example.com", dto.Email); Assert.False(dto.HasAccount); Assert.Equal(1000, dto.Note!.Length);
        var audit = await h.Db.AuditLogs.SingleAsync(); Assert.Equal(c.Id.ToString(), audit.RecordId); Assert.Equal(h.Consultant.AccountId, audit.AccountId);
        Assert.Contains("Điện thoại", System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(audit.OldValues!).GetProperty("FullName").GetString());
    }

    [Theory]
    [InlineData("accountant")] [InlineData("owner")] [InlineData("customer")]
    public async Task UnauthorizedActorsCannotReadOrEditCustomer(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var actor = who == "accountant" ? h.P.Accountant : who == "owner" ? h.Owner : new Actor(ActorType.Customer);
        await Assert.ThrowsAsync<BookingException>(() => h.Customers().SearchAsync(null, 1, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Customers().DetailsAsync(h.H.Customer.Id, actor));
        await Assert.ThrowsAsync<BookingException>(() => h.Customers().UpdateAsync(h.H.Customer.Id, new("Không được"), actor));
    }
}
