using System.IdentityModel.Tokens.Jwt;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;

namespace HomestayBooking.Web.Controllers.Api.Desktop.Owner;

[Route("api/desktop/owner/auth")]
public sealed class DesktopOwnerAuthController(ApplicationDbContext db, DesktopApiRuntime runtime, AccountLookupService lookup,
    SignInManager<ApplicationUser> signIn, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IAuthorizationService authorization, TimeProvider clock, IStringLocalizer<SharedResource> localizer) : DesktopOwnerController
{
    /// <summary>Đăng nhập chủ bằng SĐT và mật khẩu, không tạo cookie; chỉ chủ Active đã duyệt nhận JWT.</summary>
    /// <response code="200">JWT, hạn UTC và hồ sơ chủ.</response>
    /// <response code="401">Sai thông tin hoặc không đủ điều kiện, cùng thông báo như web.</response>
    /// <response code="429">Vượt giới hạn DesktopLogin theo IP.</response>
    /// <response code="503">Desktop chưa được cấu hình; web vẫn chạy.</response>
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting(DesktopApiRegistration.LoginLimiter)]
    [ProducesResponseType<OwnerLoginResponse>(200)]
    [ProducesResponseType<ProblemDetails>(429)]
    [ProducesResponseType<ProblemDetails>(503)]
    public async Task<ActionResult<OwnerLoginResponse>> Login(OwnerLoginRequest request)
    {
        if (!runtime.Enabled) return Problem(statusCode: 503, title: "Desktop API is disabled");
        var user = await lookup.FindForPhoneLoginAsync(request.PhoneNumber);
        if (user is not null)
        {
            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var principal = await claimsFactory.CreateAsync(user);
                if (!principal.IsInRole(AppRoles.Employee) && principal.IsInRole(AppRoles.Owner)
                    && (await authorization.AuthorizeAsync(principal, AppPolicies.ApprovedOwner)).Succeeded
                    && await db.Owners.AnyAsync(o => o.PersonId == user.PersonId && o.Person.Employee == null))
                {
                    var now = clock.GetUtcNow().UtcDateTime;
                    var expires = now.AddMinutes(runtime.Options.TokenMinutes);
                    var jwt = new JwtSecurityToken(runtime.Options.Issuer, runtime.Options.Audience, principal.Claims, now, expires,
                        new SigningCredentials(runtime.Key, SecurityAlgorithms.HmacSha256));
                    return Ok(new OwnerLoginResponse(new JwtSecurityTokenHandler().WriteToken(jwt), expires, await OwnerAsync(user.PersonId)));
                }
            }
            if (result.IsNotAllowed) await signIn.UserManager.AccessFailedAsync(user);
        }
        return Problem(statusCode: 401, detail: localizer["Thông tin đăng nhập không đúng hoặc tài khoản tạm thời không thể đăng nhập."].Value);
    }

    /// <summary>Hồ sơ chủ từ Bearer đã kiểm lại trạng thái, stamp và duyệt trong request hiện tại.</summary>
    /// <response code="200">Hồ sơ chủ.</response>
    [HttpGet("me")]
    [ProducesResponseType<OwnerDto>(200)]
    public async Task<ActionResult<OwnerDto>> Me() => Ok(await OwnerAsync(User.PersonId()!.Value));

    private Task<OwnerDto> OwnerAsync(int personId) => db.Owners.AsNoTracking().Where(o => o.PersonId == personId)
        .Select(o => new OwnerDto(o.Id, o.Person.FullName, o.Person.PhoneNumber)).SingleAsync();
}
