using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>FR-03/FR-07: tìm hoặc tạo Customer theo SĐT, không bao giờ tạo Account.</summary>
public class GuestCustomerService(ApplicationDbContext db, CustomerAccountService accounts, TimeProvider clock)
{
    // shortcut: chưa xác minh OTP khi đặt không đăng nhập, nâng cấp khi có dịch vụ SMS
    public async Task<Customer> FindOrCreateAsync(string phone, string? fullName, string? email, Actor actor)
    {
        var e164 = PhoneNumberNormalizer.ToE164(phone ?? "") ?? throw new BookingException("Số điện thoại không hợp lệ.");
        var name = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        var mail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (name?.Length > 100 || mail?.Length > 256 || mail is not null && !new EmailAddressAttribute().IsValid(mail))
            throw new BookingException("Họ tên hoặc email không hợp lệ.");
        try
        {
            return await HomestayWrite.RunAsync(db, () => CoreAsync(e164, name, mail, actor));
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation()
            || ex.GetBaseException().Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal))
        {
            // Yêu cầu khác vừa tạo cùng SĐT: chạy lại một lần sẽ dùng bản đã có.
            return await HomestayWrite.RunAsync(db, () => CoreAsync(e164, name, mail, actor));
        }
    }

    private async Task<Customer> CoreAsync(string phone, string? name, string? email, Actor actor)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var person = await db.PersonProfiles.Include(p => p.Customer).Include(p => p.Employee).Include(p => p.Account)
            .SingleOrDefaultAsync(p => p.PhoneNumber == phone);
        if (person?.Employee is not null)
            throw new BookingException("Số điện thoại này thuộc tài khoản nhân viên Fuji, vui lòng dùng số khác.");
        if (person?.Account is { } account)
            // Người có tài khoản: dùng đúng luật cổng khách hiện có (thêm vai trò Customer), không sửa tên/email.
            return await accounts.EnsureCustomerAsync(account) ?? throw new BookingException("Không thể đặt phòng cho số điện thoại này.");
        if (person is null)
        {
            person = new PersonProfile { PhoneNumber = phone, FullName = name, Email = email, CreatedAt = now, UpdatedAt = now };
            db.PersonProfiles.Add(person); await db.SaveChangesAsync();
            PartnerServiceSupport.Audit(db, clock, actor, "PersonProfiles", person.Id.ToString(), null,
                new { person.FullName, person.PhoneNumber, person.Email }, AuditAction.Insert);
        }
        else if (person.FullName is null && name is not null || person.Email is null && email is not null)
        {
            var old = new { person.FullName, person.Email };
            person.FullName ??= name; person.Email ??= email; person.UpdatedAt = now;
            PartnerServiceSupport.Audit(db, clock, actor, "PersonProfiles", person.Id.ToString(), old, new { person.FullName, person.Email });
        }
        if (person.Customer is not null) return person.Customer;
        var customer = new Customer { Person = person, Profile = new CustomerProfile(), CreatedAt = now, UpdatedAt = now };
        db.Customers.Add(customer); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Customers", customer.Id.ToString(), null, new { customer.PersonId }, AuditAction.Insert);
        return customer;
    }
}
