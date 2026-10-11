using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Identity;

/// <summary>
/// Đưa thông tin hồ sơ vào cookie để layout và policy không phải truy vấn CSDL mỗi request.
/// Cookie được kiểm tra lại mỗi phút (SecurityStampValidator) và tạo lại claim từ đây, nên khi nhân viên
/// nghỉ việc hoặc Owner bị hủy duyệt, quyền tương ứng mất trong tối đa một phút.
/// </summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly ApplicationDbContext _db;

    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        ApplicationDbContext db)
        : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var person = await _db.PersonProfiles.AsNoTracking()
            .Where(p => p.Id == user.PersonId)
            .Select(p => new
            {
                p.FullName,
                p.PhoneNumber,
                Employee = p.Employee == null ? null : new { p.Employee.Position, p.Employee.EmploymentStatus },
                OwnerApproved = p.Owner != null
                    && p.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved),
            })
            .SingleAsync();

        identity.AddClaim(new Claim(AppClaimTypes.PersonId, user.PersonId.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.FullName, person.FullName ?? PhoneNumberNormalizer.ToNational(person.PhoneNumber)));

        var roles = identity.FindAll(identity.RoleClaimType).Select(c => c.Value).ToHashSet();
        if (roles.Contains(AppRoles.Employee) && person.Employee is { EmploymentStatus: EmploymentStatus.Active })
        {
            identity.AddClaim(new Claim(AppClaimTypes.EmployeePosition, person.Employee.Position.ToString()));
        }
        if (roles.Contains(AppRoles.Owner) && person.OwnerApproved)
        {
            identity.AddClaim(new Claim(AppClaimTypes.OwnerApproved, "true"));
        }

        return identity;
    }
}
