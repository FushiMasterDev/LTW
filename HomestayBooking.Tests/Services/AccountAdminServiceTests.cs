using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Services;

public sealed class AccountAdminServiceTests
{
    private static AccountAdminService Service(PartnerTestHost h) => new(h.Db, h.Users, new(h.Db, new HttpContextAccessor()), h.H.Clock);
    private static EmployeeAccountInput Input(string phone = "0977123456", string code = "NV-B6", EmployeePosition position = EmployeePosition.Consultant) =>
        new("Nhân viên mới", phone, "new@fuji.vn", code, position, PartnerTestHost.Password);

    [Theory]
    [InlineData(EmployeePosition.Consultant)] [InlineData(EmployeePosition.Manager)]
    [InlineData(EmployeePosition.Accountant)] [InlineData(EmployeePosition.Administrator)]
    public async Task AdministratorCreatesEmployeeRoleAndRealIdsWithActivities(EmployeePosition position)
    {
        using var h = await PartnerTestHost.CreateAsync(); var user = await Service(h).CreateEmployeeAsync(Input(position: position), h.Admin);
        var person = await h.Db.PersonProfiles.Include(p => p.Employee).SingleAsync(p => p.Id == user.PersonId);
        Assert.Equal("+84977123456", person.PhoneNumber); Assert.Equal(position, person.Employee!.Position);
        Assert.Equal(AccountStatus.Active, user.Status); Assert.True(await h.Users.CheckPasswordAsync(user, PartnerTestHost.Password));
        Assert.True(await h.Users.IsInRoleAsync(user, AppRoles.Employee));
        var role = await h.Db.UserRoles.SingleAsync(r => r.UserId == user.Id); Assert.Equal(h.Admin.AccountId, role.AssignedByAccountId); Assert.Equal(h.H.Clock.Now.UtcDateTime, role.AssignedAt);
        var logs = await h.Db.AccountActivityLogs.Where(l => l.AccountId == user.Id).ToListAsync();
        Assert.Contains(logs, l => l.EventType == AccountEventType.AccountCreated); Assert.Contains(logs, l => l.EventType == AccountEventType.RoleAssigned);
        Assert.All(logs, l => { Assert.Equal(h.Admin.AccountId, l.ActorId); Assert.Equal(h.H.Clock.Now.UtcDateTime, l.CreatedAt); });
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), l => l.TableName == "Employees" && l.RecordId == person.Employee.Id.ToString() && l.Action == AuditAction.Insert);
        Assert.DoesNotContain(await h.Db.AuditLogs.ToListAsync(), l => l.NewValues!.Contains(PartnerTestHost.Password) || l.NewValues.Contains("PasswordHash"));
    }

    [Theory]
    [InlineData("  Tư vấn đặt phòng ", "Tư vấn đặt phòng")] [InlineData("   ", null)] [InlineData(null, null)]
    public async Task DepartmentIsTrimmedStoredAndListed(string? input, string? stored)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var user = await Service(h).CreateEmployeeAsync(Input() with { Department = input }, h.Admin);
        Assert.Equal(stored, (await h.Db.Employees.SingleAsync(e => e.PersonId == user.PersonId)).Department);
        var row = (await Service(h).ListAsync(new(Term: "NV-B6"), h.Admin)).Single(r => r.Id == user.Id);
        Assert.Equal(stored, row.Department);
    }

    [Fact]
    public async Task DepartmentLongerThanColumnIsRejected()
    {
        using var h = await PartnerTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => Service(h).CreateEmployeeAsync(Input() with { Department = new string('x', 101) }, h.Admin));
    }

    [Theory]
    [InlineData("phone")] [InlineData("code")] [InlineData("existing-person")]
    public async Task DuplicatePhoneOrCodeNeverMergesPeople(string kind)
    {
        using var h = await PartnerTestHost.CreateAsync(); await Service(h).CreateEmployeeAsync(Input(), h.Admin);
        if (kind == "existing-person") { h.Db.PersonProfiles.Add(new() { PhoneNumber = "+84977123457" }); await h.Db.SaveChangesAsync(); }
        var people = await h.Db.PersonProfiles.CountAsync(); var users = await h.Db.Users.CountAsync();
        var input = kind == "phone" ? Input(code: "OTHER") : kind == "code" ? Input("0977123457") : Input("0977123457", "OTHER");
        await Assert.ThrowsAsync<BookingException>(() => Service(h).CreateEmployeeAsync(input, h.Admin));
        Assert.Equal(people, await h.Db.PersonProfiles.CountAsync()); Assert.Equal(users, await h.Db.Users.CountAsync());
    }

    [Theory]
    [InlineData("phone")] [InlineData("name")] [InlineData("email")] [InlineData("code")] [InlineData("position")] [InlineData("password")]
    public async Task InvalidInputOrWeakPasswordRollsBackNewPeople(string field)
    {
        using var h = await PartnerTestHost.CreateAsync(); var input = Input();
        input = field switch { "phone" => input with { Phone = "bad" }, "name" => input with { FullName = " " }, "email" => input with { Email = "bad" },
            "code" => input with { EmployeeCode = new string('X', 21) }, "position" => input with { Position = (EmployeePosition)99 }, _ => input with { Password = "weak" } };
        var before = await h.Db.PersonProfiles.CountAsync();
        await Assert.ThrowsAsync<BookingException>(() => Service(h).CreateEmployeeAsync(input, h.Admin));
        Assert.Equal(before, await h.Db.PersonProfiles.CountAsync()); Assert.Empty(await h.Db.AccountActivityLogs.ToListAsync());
    }

    [Theory]
    [InlineData("manager")] [InlineData("consultant")] [InlineData("accountant")] [InlineData("owner")] [InlineData("customer")]
    public async Task OnlyAdministratorCanCreatePromoteOrReset(string role)
    {
        using var h = await PartnerTestHost.CreateAsync(); var actor = role == "manager" ? h.Manager : role == "consultant" ? h.Consultant : role == "accountant" ? h.Accountant : new Actor(role == "owner" ? ActorType.Owner : ActorType.Customer);
        await Assert.ThrowsAsync<BookingException>(() => Service(h).CreateEmployeeAsync(Input(position: EmployeePosition.Administrator), actor));
        await Assert.ThrowsAsync<BookingException>(() => Service(h).ChangeEmploymentAsync(h.Consultant.AccountId!, EmployeePosition.Administrator, EmploymentStatus.Active, actor));
        await Assert.ThrowsAsync<BookingException>(() => Service(h).ResetEmployeePasswordAsync(h.Consultant.AccountId!, PartnerTestHost.Password, actor));
    }

    [Theory]
    [InlineData(EmploymentStatus.Active, AccountStatus.Active)] [InlineData(EmploymentStatus.Suspended, AccountStatus.Disabled)] [InlineData(EmploymentStatus.Resigned, AccountStatus.Disabled)]
    public async Task EmploymentChangesInvalidateStampAndRecordAccountAndEmployee(EmploymentStatus state, AccountStatus expected)
    {
        using var h = await PartnerTestHost.CreateAsync(); var user = await Service(h).CreateEmployeeAsync(Input(), h.Admin); var stamp = user.SecurityStamp;
        await Service(h).ChangeEmploymentAsync(user.Id, EmployeePosition.Manager, state, h.Admin);
        Assert.Equal(expected, user.Status); Assert.NotEqual(stamp, user.SecurityStamp); Assert.Equal(EmployeePosition.Manager, user.Person.Employee!.Position);
        Assert.Equal(state, user.Person.Employee.EmploymentStatus);
        Assert.Contains(await h.Db.AuditLogs.ToListAsync(), a => a.TableName == "Employees" && a.Action == AuditAction.Update && a.AccountId == h.Admin.AccountId);
        Assert.Contains(await h.Db.AccountActivityLogs.ToListAsync(), a => a.AccountId == user.Id && a.EventType == AccountEventType.StatusChanged);
        if (state != EmploymentStatus.Active)
        {
            await Service(h).ChangeEmploymentAsync(user.Id, EmployeePosition.Consultant, EmploymentStatus.Active, h.Admin);
            Assert.Equal(AccountStatus.Active, user.Status);
        }
    }

    [Theory]
    [InlineData("lock")] [InlineData("demote")] [InlineData("suspend")] [InlineData("resign")]
    public async Task CannotRemoveLastAdministratorOrDisableSelf(string operation)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var ex = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            if (operation == "lock") await Service(h).SetLockedAsync(h.Admin.AccountId!, true, "Không cho", h.Admin);
            else await Service(h).ChangeEmploymentAsync(h.Admin.AccountId!, operation == "demote" ? EmployeePosition.Manager : EmployeePosition.Administrator,
                operation == "suspend" ? EmploymentStatus.Suspended : operation == "resign" ? EmploymentStatus.Resigned : EmploymentStatus.Active, h.Admin);
        });
        Assert.Contains("Administrator", ex.Message); var admin = await h.Db.Users.SingleAsync(u => u.Id == h.Admin.AccountId); Assert.Equal(AccountStatus.Active, admin.Status);
    }

    [Theory]
    [InlineData("lock")] [InlineData("demote")] [InlineData("suspend")] [InlineData("resign")]
    public async Task SelfProtectionAppliesEvenWithTwoAdministratorsButOtherAdminMayBeDemoted(string operation)
    {
        using var h = await PartnerTestHost.CreateAsync(); var other = await Service(h).CreateEmployeeAsync(Input(position: EmployeePosition.Administrator), h.Admin);
        await Assert.ThrowsAsync<BookingException>(async () =>
        {
            if (operation == "lock") await Service(h).SetLockedAsync(h.Admin.AccountId!, true, "Tự khóa", h.Admin);
            else await Service(h).ChangeEmploymentAsync(h.Admin.AccountId!, operation == "demote" ? EmployeePosition.Manager : EmployeePosition.Administrator,
                operation == "suspend" ? EmploymentStatus.Suspended : operation == "resign" ? EmploymentStatus.Resigned : EmploymentStatus.Active, h.Admin);
        });
        await Service(h).ChangeEmploymentAsync(other.Id, EmployeePosition.Consultant, EmploymentStatus.Active, h.Admin);
        Assert.Equal(EmployeePosition.Consultant, (await h.Db.Employees.SingleAsync(e => e.PersonId == other.PersonId)).Position);
        Assert.True(await h.Db.Employees.AnyAsync(e => e.Position == EmployeePosition.Administrator && e.EmploymentStatus == EmploymentStatus.Active && e.Person.Account!.Status == AccountStatus.Active));
    }

    [Fact]
    public async Task PositionChangeKeepsManualLockAndEmploymentReactivationRequiresExplicitTransition()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var user = await Service(h).SetLockedAsync(h.Consultant.AccountId!, true, "Đang kiểm tra", h.Admin);
        var stamp = user.SecurityStamp;
        user = await Service(h).ChangeEmploymentAsync(user.Id, EmployeePosition.Manager, EmploymentStatus.Active, h.Admin);
        Assert.Equal(AccountStatus.Locked, user.Status); Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public async Task AuditFailureRollsBackIdentityEmployeeRoleAndActivitiesTogether()
    {
        using var h = await PartnerTestHost.CreateAsync(); var people = await h.Db.PersonProfiles.CountAsync(); var count = await h.Db.Users.CountAsync();
        await using var db = h.H.NewContext(new FailAudit()); var collection = new ServiceCollection();
        collection.AddLogging(); collection.AddDataProtection().UseEphemeralDataProtectionProvider(); collection.AddSingleton<ApplicationDbContext>(db);
        collection.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        await using var services = collection.BuildServiceProvider();
        var service = new AccountAdminService(db, services.GetRequiredService<UserManager<ApplicationUser>>(), new(db, new HttpContextAccessor()), h.H.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEmployeeAsync(Input(), h.Admin));
        Assert.Equal(people, await h.Db.PersonProfiles.CountAsync()); Assert.Equal(count, await h.Db.Users.CountAsync());
        Assert.False(await h.Db.Employees.AnyAsync(e => e.EmployeeCode == "NV-B6")); Assert.Empty(await h.Db.AccountActivityLogs.ToListAsync()); Assert.Empty(await h.Db.AuditLogs.ToListAsync());
    }
    private sealed class FailAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
        { if (e.Context!.ChangeTracker.Entries<AuditLog>().Any(a => a.State == EntityState.Added)) throw new InvalidOperationException("Lỗi audit giả lập"); return ValueTask.FromResult(r); }
    }

    [Theory]
    [InlineData("customer")] [InlineData("owner")] [InlineData("employee")]
    public async Task LockUnlockRequiresReasonAndRecordsActivity(string kind)
    {
        using var h = await PartnerTestHost.CreateAsync();
        var user = kind == "employee" ? await h.Db.Users.SingleAsync(u => u.Id == h.Consultant.AccountId)
            : await h.Db.Users.SingleAsync(u => u.PersonId == (kind == "customer" ? h.H.Customer.PersonId : h.H.Homestay.Owner.PersonId));
        var stamp = user.SecurityStamp;
        await Assert.ThrowsAsync<BookingException>(() => Service(h).SetLockedAsync(user.Id, true, " ", h.Admin));
        user = await Service(h).SetLockedAsync(user.Id, true, "Tạm khóa để kiểm tra", h.Admin); Assert.Equal(AccountStatus.Locked, user.Status); Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.Contains(await h.Db.AccountActivityLogs.ToListAsync(), l => l.AccountId == user.Id && l.EventType == AccountEventType.StatusChanged && l.Detail == "Tạm khóa để kiểm tra");
        user = await Service(h).SetLockedAsync(user.Id, false, "Đã kiểm tra", h.Admin); Assert.Equal(AccountStatus.Active, user.Status);
        await Assert.ThrowsAsync<BookingException>(() => Service(h).SetLockedAsync(user.Id, false, "Không đang khóa", h.Admin));
    }

    [Fact]
    public async Task StoppedEmployeeCannotBeReactivatedByUnlockShortcut()
    {
        using var h = await PartnerTestHost.CreateAsync(); var user = await h.Db.Users.Include(u => u.Person.Employee).SingleAsync(u => u.Id == h.Consultant.AccountId);
        user.Status = AccountStatus.Locked; user.Person.Employee!.EmploymentStatus = EmploymentStatus.Suspended; await h.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => Service(h).SetLockedAsync(user.Id, false, "Không hợp lệ", h.Admin));
    }

    [Fact]
    public async Task PasswordResetUsesIdentityPolicyChangesStampAndNeverLogsPassword()
    {
        using var h = await PartnerTestHost.CreateAsync(); var user = await Service(h).CreateEmployeeAsync(Input(), h.Admin); var stamp = user.SecurityStamp;
        await Assert.ThrowsAsync<BookingException>(() => Service(h).ResetEmployeePasswordAsync(user.Id, "weak", h.Admin));
        user = await Service(h).ResetEmployeePasswordAsync(user.Id, "Next@Test2026", h.Admin);
        Assert.True(await h.Users.CheckPasswordAsync(user, "Next@Test2026")); Assert.NotEqual(stamp, user.SecurityStamp); Assert.Equal(h.H.Clock.Now.UtcDateTime, user.PasswordChangedAt);
        Assert.Contains(await h.Db.AccountActivityLogs.ToListAsync(), l => l.EventType == AccountEventType.PasswordReset && l.AccountId == user.Id);
        Assert.DoesNotContain(await h.Db.AccountActivityLogs.ToListAsync(), l => l.Detail!.Contains("Next@Test2026"));
        Assert.DoesNotContain(await h.Db.AuditLogs.ToListAsync(), l => (l.NewValues ?? "").Contains("Next@Test2026"));
        var customer = await h.Db.Users.SingleAsync(u => u.PersonId == h.H.Customer.PersonId);
        await Assert.ThrowsAsync<BookingException>(() => Service(h).ResetEmployeePasswordAsync(customer.Id, "Next@Test2026", h.Admin));
    }

    [Theory]
    [InlineData("Nhân viên mới")] [InlineData("0977123456")] [InlineData("+84977123456")] [InlineData("NV-B6")]
    public async Task ManagerListsByNamePhoneCodeAndRoleWithoutWriteAccess(string term)
    {
        using var h = await PartnerTestHost.CreateAsync(); var user = await Service(h).CreateEmployeeAsync(Input(), h.Admin);
        var page = await Service(h).ListAsync(new(term, AppRoles.Employee, AccountStatus.Active), h.Manager);
        Assert.Equal(user.Id, Assert.Single(page).Id); Assert.Contains(AppRoles.Employee, page[0].Roles);
        Assert.Empty(await Service(h).ListAsync(new(term, AppRoles.Owner), h.Manager));
        await Assert.ThrowsAsync<BookingException>(() => Service(h).ListAsync(new(), h.Consultant));
    }
}
