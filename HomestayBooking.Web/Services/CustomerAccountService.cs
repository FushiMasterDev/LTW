using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record RegisterResult(ApplicationUser? User, IReadOnlyList<string> Errors)
{
    public bool Succeeded => User is not null;
}

/// <summary>
/// Tài khoản khách hàng (v5 mục 5.1, 5.3). Tạm thời đăng ký bằng SĐT + mật khẩu, Account Active ngay;
/// bước xác thực OTP sẽ thêm sau. SĐT đã có hồ sơ (ví dụ nhân viên tạo khi khách gọi điện) thì gắn Account vào hồ sơ đó.
/// </summary>
public class CustomerAccountService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccountActivityLogger _activityLogger;

    public CustomerAccountService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AccountActivityLogger activityLogger)
    {
        _db = db;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    public async Task<RegisterResult> RegisterAsync(string fullName, string phoneNumber, string? email, string password)
    {
        var e164 = PhoneNumberNormalizer.ToE164(phoneNumber);
        if (e164 is null)
        {
            return new(null, ["Số điện thoại không hợp lệ."]);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var person = await _db.PersonProfiles
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .SingleOrDefaultAsync(p => p.PhoneNumber == e164);
        if (person?.Account is not null)
        {
            return new(null, ["Số điện thoại này đã có tài khoản. Vui lòng đăng nhập."]);
        }

        var now = DateTime.UtcNow;
        if (person is null)
        {
            person = new PersonProfile { PhoneNumber = e164 };
            _db.PersonProfiles.Add(person);
        }
        // Hồ sơ có sẵn: người đăng ký là chủ SĐT nên được cập nhật tên/email
        person.FullName = fullName.Trim();
        person.Email = string.IsNullOrWhiteSpace(email) ? person.Email : email.Trim();
        person.UpdatedAt = now;
        person.Customer ??= new Customer { Profile = new CustomerProfile() };
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            PersonId = person.Id,
            Email = person.Email,
            Status = AccountStatus.Active,
            PasswordChangedAt = now,
        };
        var created = await _userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return new(null, created.Errors.Select(e => e.Description).ToList());
        }
        var role = await _userManager.AddToRoleAsync(user, AppRoles.Customer);
        if (!role.Succeeded)
        {
            return new(null, role.Errors.Select(e => e.Description).ToList());
        }

        _activityLogger.Add(user.Id, AccountEventType.AccountCreated, ActorType.Customer, user.Id, "Tự đăng ký (SĐT + mật khẩu)");
        _activityLogger.Add(user.Id, AccountEventType.RoleAssigned, ActorType.System, null, AppRoles.Customer);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(user, []);
    }

    /// <summary>
    /// Customer của người đang đăng nhập. Chủ homestay chưa từng đặt phòng thì tạo hồ sơ Customer và thêm vai trò
    /// Customer vào Account sẵn có (một người một Account, v5 mục 5.3). Nhân viên không đặt phòng ở cổng khách → null.
    /// </summary>
    public async Task<Customer?> EnsureCustomerAsync(ApplicationUser user)
    {
        var person = await _db.PersonProfiles
            .Include(p => p.Customer)
            .Include(p => p.Employee)
            .SingleAsync(p => p.Id == user.PersonId);
        if (person.Customer is not null)
        {
            return person.Customer;
        }
        if (person.Employee is not null)
        {
            return null;
        }

        person.Customer = new Customer { Profile = new CustomerProfile() };
        await _db.SaveChangesAsync();
        if (!await _userManager.IsInRoleAsync(user, AppRoles.Customer))
        {
            await _userManager.AddToRoleAsync(user, AppRoles.Customer);
            await _activityLogger.LogAsync(user.Id, AccountEventType.RoleAssigned, ActorType.System, null, AppRoles.Customer);
        }
        return person.Customer;
    }
}
