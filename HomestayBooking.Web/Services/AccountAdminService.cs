using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record EmployeeAccountInput(string FullName, string Phone, string? Email, string EmployeeCode, EmployeePosition Position, string Password,
    string? Department = null);
public record AccountQuery(string? Term = null, string? Role = null, AccountStatus? Status = null, int? Page = 1, EmployeePosition? Position = null);
public record AccountSummary(string Id, string? FullName, string Phone, string? Email, string? EmployeeCode,
    EmployeePosition? Position, EmploymentStatus? EmploymentStatus, AccountStatus Status, IReadOnlyList<string> Roles, string? Department = null);

/// <summary>FR-21: chỉ Administrator sửa; Identity quản lý mật khẩu/stamp, không ghi bí mật vào audit.</summary>
public class AccountAdminService(ApplicationDbContext db, UserManager<ApplicationUser> users, AccountActivityLogger activities, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static object State(ApplicationUser u) => new { u.Id, u.PersonId, Status = u.Status.ToString(), u.PasswordChangedAt };
    private static object EmployeeState(Employee e) => new { e.PersonId, e.EmployeeCode, e.Department, Position = e.Position.ToString(), EmploymentStatus = e.EmploymentStatus.ToString() };
    private static void Identity(IdentityResult result)
    {
        if (!result.Succeeded) throw new BookingException("Không thể cập nhật tài khoản: {0}", string.Join(" ", result.Errors.Select(e => e.Description)));
    }
    private void Activity(ApplicationUser u, AccountEventType type, Actor actor, string detail)
    {
        activities.Add(u.Id, type, actor.Type, actor.AccountId, detail);
        foreach (var e in db.ChangeTracker.Entries<AccountActivityLog>().Where(e => e.State == EntityState.Added)) e.Entity.CreatedAt = Now;
    }
    private async Task<T> WriteAsync<T>(Actor actor, Func<Task<T>> action)
    {
        try
        {
            return await HomestayWrite.RunAsync(db, async () =>
            {
                await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Administrator);
                // Mọi thay đổi quản trị lấy cùng khóa hiện có trước khi đếm, không cần Serializable/schema mới.
                if (db.Database.IsRelational()) await db.Employees.Where(e => e.Position == EmployeePosition.Administrator)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.UpdatedAt, e => e.UpdatedAt));
                if (actor.EmployeeId is int id && db.Employees.Local.SingleOrDefault(e => e.Id == id) is { } current) await db.Entry(current).ReloadAsync();
                if (actor.AccountId is { } account && db.Users.Local.SingleOrDefault(u => u.Id == account) is { } user) await db.Entry(user).ReloadAsync();
                await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Administrator);
                return await action();
            });
        }
        catch (DbUpdateConcurrencyException) { throw new BookingException("Tài khoản vừa được người khác xử lý, vui lòng tải lại."); }
        catch (DbUpdateException e) when (e.IsUniqueConstraintViolation() || e.GetBaseException().Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal))
        { throw new BookingException("Số điện thoại hoặc mã nhân viên đã được sử dụng."); }
    }

    public Task<ApplicationUser> CreateEmployeeAsync(EmployeeAccountInput input, Actor actor) =>
        WriteAsync(actor, () => CreateEmployeeCoreAsync(input, actor));

    /// <summary>FR-21: Quản trị viên đầu tiên khi triển khai thật; chỉ tạo khi chưa có Administrator nào (kiểm lại trong transaction).</summary>
    internal async Task<ApplicationUser?> BootstrapAdministratorAsync(EmployeeAccountInput input)
    {
        try
        {
            return await HomestayWrite.RunAsync(db, async () =>
                await db.Employees.AnyAsync(e => e.Position == EmployeePosition.Administrator) ? null
                    : await CreateEmployeeCoreAsync(input with { Position = EmployeePosition.Administrator }, Actor.System));
        }
        catch (DbUpdateException e) when (e.IsUniqueConstraintViolation() || e.GetBaseException().Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal))
        { throw new BookingException("Số điện thoại hoặc mã nhân viên đã được sử dụng."); }
    }

    private async Task<ApplicationUser> CreateEmployeeCoreAsync(EmployeeAccountInput input, Actor actor)
    {
        var phone = PhoneNumberNormalizer.ToE164(input.Phone) ?? throw new BookingException("Số điện thoại không hợp lệ.");
        var name = input.FullName?.Trim() ?? ""; var code = input.EmployeeCode?.Trim().ToUpperInvariant() ?? ""; var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        var department = string.IsNullOrWhiteSpace(input.Department) ? null : input.Department.Trim();
        if (name.Length is < 1 or > 100 || code.Length is < 1 or > 20 || !Enum.IsDefined(input.Position)
            || email?.Length > 256 || department?.Length > 100 || email is not null && !new EmailAddressAttribute().IsValid(email)) throw new BookingException("Thông tin nhân viên không hợp lệ.");
        if (await db.PersonProfiles.AnyAsync(p => p.PhoneNumber == phone)) throw new BookingException("Số điện thoại đã thuộc hồ sơ khác, không tự ghép tài khoản.");
        if (await db.Employees.AnyAsync(e => e.EmployeeCode == code)) throw new BookingException("Mã nhân viên đã được sử dụng.");
        var person = new PersonProfile { FullName = name, PhoneNumber = phone, Email = email, CreatedAt = Now, UpdatedAt = Now };
        var employee = new Employee { Person = person, EmployeeCode = code, Department = department, Position = input.Position, EmploymentStatus = EmploymentStatus.Active, CreatedAt = Now, UpdatedAt = Now };
        var user = new ApplicationUser { Person = person, Email = email, Status = AccountStatus.Active, PasswordChangedAt = Now, CreatedAt = Now, UpdatedAt = Now };
        db.Employees.Add(employee);
        // shortcut: chưa bắt đổi mật khẩu lần đầu, nâng cấp khi có luồng kích hoạt cho nhân viên
        Identity(await users.CreateAsync(user, input.Password));
        Identity(await users.AddToRoleAsync(user, AppRoles.Employee));
        var role = await db.UserRoles.SingleAsync(r => r.UserId == user.Id);
        role.AssignedByAccountId = actor.AccountId; role.AssignedAt = Now;
        await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "PersonProfiles", person.Id.ToString(), null, new { person.FullName, person.PhoneNumber, person.Email }, AuditAction.Insert);
        PartnerServiceSupport.Audit(db, clock, actor, "Employees", employee.Id.ToString(), null, EmployeeState(employee), AuditAction.Insert);
        PartnerServiceSupport.Audit(db, clock, actor, "AspNetUsers", user.Id, null, State(user), AuditAction.Insert);
        Activity(user, AccountEventType.AccountCreated, actor, actor.Type == ActorType.System ? "Tạo Quản trị viên đầu tiên từ cấu hình triển khai." : "Quản trị viên tạo tài khoản nhân viên.");
        Activity(user, AccountEventType.RoleAssigned, actor, AppRoles.Employee);
        return user;
    }

    private async Task<ApplicationUser> AccountAsync(string id)
    {
        if (db.Database.IsRelational()) await db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.UpdatedAt, u => u.UpdatedAt));
        var user = await db.Users.Include(u => u.Person).ThenInclude(p => p.Employee).SingleOrDefaultAsync(u => u.Id == id)
            ?? throw new BookingException("Không tìm thấy tài khoản.");
        await db.Entry(user).ReloadAsync(); if (user.Person.Employee is { } e) await db.Entry(e).ReloadAsync();
        return user;
    }
    private async Task ProtectAsync(ApplicationUser user, Actor actor, EmployeePosition? position = null, EmploymentStatus? employment = null, AccountStatus? status = null)
    {
        var e = user.Person.Employee;
        var removesAdmin = e?.Position == EmployeePosition.Administrator && e.EmploymentStatus == EmploymentStatus.Active && user.Status == AccountStatus.Active
            && (position is not null && position != EmployeePosition.Administrator || employment is not null && employment != EmploymentStatus.Active || status is not null && status != AccountStatus.Active);
        if (removesAdmin && !await db.Employees.AnyAsync(x => x.Id != e!.Id && x.Position == EmployeePosition.Administrator && x.EmploymentStatus == EmploymentStatus.Active
            && x.Person.Account != null && x.Person.Account.Status == AccountStatus.Active)) throw new BookingException("Phải còn ít nhất một Administrator đang làm việc có tài khoản Active.");
        if (user.Id == actor.AccountId && (position is not null && position != EmployeePosition.Administrator || employment is not null && employment != EmploymentStatus.Active || status is not null && status != AccountStatus.Active))
            throw new BookingException("Không được tự khóa, hạ chức vụ hoặc cho nghỉ chính mình.");
    }

    public Task<ApplicationUser> ChangeEmploymentAsync(string id, EmployeePosition position, EmploymentStatus employment, Actor actor) => WriteAsync(actor, async () =>
    {
        if (!Enum.IsDefined(position) || !Enum.IsDefined(employment)) throw new BookingException("Chức vụ hoặc trạng thái làm việc không hợp lệ.");
        var user = await AccountAsync(id); var employee = user.Person.Employee ?? throw new BookingException("Tài khoản không phải nhân viên.");
        // Đổi chức vụ không được vô tình mở tài khoản đang Locked; chỉ kích hoạt khi trạng thái làm việc đổi về Active.
        var status = employment != employee.EmploymentStatus
            ? employment == EmploymentStatus.Active ? AccountStatus.Active : AccountStatus.Disabled
            : employment == EmploymentStatus.Active ? user.Status : AccountStatus.Disabled;
        await ProtectAsync(user, actor, position, employment, status);
        var old = new { Employee = EmployeeState(employee), Account = State(user) };
        employee.Position = position; employee.EmploymentStatus = employment; employee.UpdatedAt = user.UpdatedAt = Now; user.Status = status;
        Identity(await users.UpdateSecurityStampAsync(user));
        PartnerServiceSupport.Audit(db, clock, actor, "Employees", employee.Id.ToString(), old.Employee, EmployeeState(employee));
        PartnerServiceSupport.Audit(db, clock, actor, "AspNetUsers", id, old.Account, State(user));
        Activity(user, AccountEventType.StatusChanged, actor, "Cập nhật chức vụ/trạng thái làm việc và hiệu lực phiên đăng nhập.");
        return user;
    });

    public Task<ApplicationUser> SetLockedAsync(string id, bool locked, string reason, Actor actor) => WriteAsync(actor, async () =>
    {
        reason = Note(reason); var user = await AccountAsync(id); var status = locked ? AccountStatus.Locked : AccountStatus.Active;
        if (user.Status != (locked ? AccountStatus.Active : AccountStatus.Locked)) throw new BookingException("Chỉ khóa tài khoản Active hoặc mở lại tài khoản Locked.");
        if (!locked && user.Person.Employee is { EmploymentStatus: not EmploymentStatus.Active }) throw new BookingException("Nhân viên ngừng làm việc không được mở tài khoản Active.");
        await ProtectAsync(user, actor, status: status); var old = State(user); user.Status = status; user.UpdatedAt = Now;
        Identity(await users.UpdateSecurityStampAsync(user));
        PartnerServiceSupport.Audit(db, clock, actor, "AspNetUsers", id, old, new { Account = State(user), Reason = reason });
        Activity(user, AccountEventType.StatusChanged, actor, reason); return user;
    });

    public Task<ApplicationUser> ResetEmployeePasswordAsync(string id, string newPassword, Actor actor) => WriteAsync(actor, async () =>
    {
        var user = await AccountAsync(id); if (user.Person.Employee is null) throw new BookingException("Chỉ được đặt lại mật khẩu nhân viên.");
        var old = State(user); var token = await users.GeneratePasswordResetTokenAsync(user);
        Identity(await users.ResetPasswordAsync(user, token, newPassword)); user.PasswordChangedAt = user.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "AspNetUsers", id, old, State(user));
        Activity(user, AccountEventType.PasswordReset, actor, "Quản trị viên đặt lại mật khẩu nhân viên."); return user;
    });
    private static string Note(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > 1000
        ? throw new BookingException("Lý do phải có từ 1 đến 1000 ký tự.") : value.Trim();

    private static AccountSummary Summary(ApplicationUser u) => new(u.Id, u.Person.FullName, u.Person.PhoneNumber, u.Person.Email, u.Person.Employee?.EmployeeCode,
        u.Person.Employee?.Position, u.Person.Employee?.EmploymentStatus, u.Status, u.UserRoles.Select(r => r.Role!.Name!).Order().ToList(), u.Person.Employee?.Department);

    public async Task<AccountSummary> DetailsAsync(string id, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        var user = await db.Users.AsNoTracking().Include(u => u.Person).ThenInclude(p => p.Employee)
            .Include(u => u.UserRoles).ThenInclude(r => r.Role).SingleOrDefaultAsync(u => u.Id == id)
            ?? throw new BookingException("Không tìm thấy tài khoản.");
        return Summary(user);
    }

    public async Task<PaginatedList<AccountSummary>> ListAsync(AccountQuery query, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        if (query.Status is { } status && !Enum.IsDefined(status)) throw new BookingException("Trạng thái tài khoản không hợp lệ.");
        if (query.Position is { } position && !Enum.IsDefined(position)) throw new BookingException("Chức vụ không hợp lệ.");
        var rows = db.Users.AsNoTracking();
        if (query.Position is { } filter) rows = rows.Where(u => u.Person.Employee != null && u.Person.Employee.Position == filter);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var term = query.Term.Trim(); var phone = PhoneNumberNormalizer.ToE164(term);
            rows = rows.Where(u => (u.Person.FullName != null && u.Person.FullName.Contains(term)) || (phone != null && u.Person.PhoneNumber == phone)
                || (u.Person.Employee != null && u.Person.Employee.EmployeeCode.Contains(term)));
        }
        if (query.Status is { } s) rows = rows.Where(u => u.Status == s);
        if (!string.IsNullOrWhiteSpace(query.Role)) rows = rows.Where(u => u.UserRoles.Any(r => r.Role!.Name == query.Role));
        var page = await PaginatedList<ApplicationUser>.CreateAsync(rows.Include(u => u.Person).ThenInclude(p => p.Employee)
            .Include(u => u.UserRoles).ThenInclude(r => r.Role).OrderBy(u => u.Person.FullName).ThenBy(u => u.Id), query.Page);
        return new(page.Select(Summary), page.TotalCount, page.PageIndex, page.PageSize);
    }
}
