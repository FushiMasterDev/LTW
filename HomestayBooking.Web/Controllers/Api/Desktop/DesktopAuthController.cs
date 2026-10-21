using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/auth")]
public sealed class DesktopAuthController(ApplicationDbContext db, DesktopApiRuntime runtime, AccountLookupService lookup,
    SignInManager<ApplicationUser> signIn, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    TimeProvider clock, IStringLocalizer<SharedResource> localizer) : DesktopController(db)
{
    /// <summary>Đăng nhập nhân viên bằng mã + mật khẩu. Không tạo cookie; token và expiresAt (UTC) mặc định hết hạn sau 60 phút.</summary>
    /// <response code="200">JWT và hồ sơ nhân viên.</response>
    /// <response code="400">Thiếu thông tin.</response>
    /// <response code="401">Thông tin sai hoặc tài khoản không được đăng nhập, cùng một thông báo.</response>
    /// <response code="429">Quá số lần thử theo IP.</response>
    /// <response code="503">Desktop API chưa cấu hình khóa hợp lệ; web vẫn hoạt động.</response>
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting(DesktopApiRegistration.LoginLimiter)]
    [ProducesResponseType<LoginResponse>(200)]
    [ProducesResponseType<ProblemDetails>(429)]
    [ProducesResponseType<ProblemDetails>(503)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (!runtime.Enabled) return Problem(statusCode: 503, title: "Desktop API is disabled");
        var user = await lookup.FindForEmployeeLoginAsync(request.EmployeeCode);
        if (user is not null)
        {
            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var principal = await claimsFactory.CreateAsync(user);
                var now = clock.GetUtcNow().UtcDateTime;
                var expires = now.AddMinutes(runtime.Options.TokenMinutes);
                // JwtSecurityToken with original factory claims avoids outbound/inbound claim renaming.
                var token = new JwtSecurityToken(runtime.Options.Issuer, runtime.Options.Audience, principal.Claims,
                    now, expires, new SigningCredentials(runtime.Key, SecurityAlgorithms.HmacSha256));
                return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, await EmployeeAsync(user.PersonId)));
            }
            if (result.IsNotAllowed) await signIn.UserManager.AccessFailedAsync(user);
        }
        // AccountController has no login event in AccountActivityLog; preserve that behavior and the exact error.
        return Problem(statusCode: 401, detail: localizer["Thông tin đăng nhập không đúng hoặc tài khoản tạm thời không thể đăng nhập."].Value);
    }

    /// <summary>Nhân viên hiện tại, xác định bằng NameIdentifier/person_id của Bearer (không lấy từ cookie).</summary>
    /// <response code="200">Hồ sơ nhân viên, position là chữ.</response>
    /// <response code="401">Token không hợp lệ hoặc tài khoản không còn hoạt động.</response>
    [HttpGet("me")]
    [ProducesResponseType<EmployeeDto>(200)]
    public async Task<ActionResult<EmployeeDto>> Me() => Ok(await EmployeeAsync(User.PersonId()!.Value));

    private Task<EmployeeDto> EmployeeAsync(int personId) => Db.Employees.AsNoTracking().Where(e => e.PersonId == personId)
        .Select(e => new EmployeeDto(e.Id, e.EmployeeCode, e.Person.FullName, e.Position.ToString())).SingleAsync();
}
