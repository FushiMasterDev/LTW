using System.Security.Claims;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Identity;

/// <summary>
/// Chỉ Account ở trạng thái Active mới đăng nhập được (v5 mục 4.2, 11), ở cả lúc đăng nhập lẫn các request sau:
/// SecurityStampValidator gọi ValidateSecurityStampAsync định kỳ, trả null thì cookie bị hủy.
/// </summary>
public class AppSignInManager : SignInManager<ApplicationUser>
{
    public AppSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user) =>
        user.Status == AccountStatus.Active && await base.CanSignInAsync(user);

    public override async Task<ApplicationUser?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var user = await base.ValidateSecurityStampAsync(principal);
        return user is { Status: AccountStatus.Active } ? user : null;
    }
}
