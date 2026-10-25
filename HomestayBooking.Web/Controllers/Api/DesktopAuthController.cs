using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Controllers.Api;

[ApiController]
[Route("api/desktop/auth")]
[AllowAnonymous]
public sealed class DesktopAuthController(
    AccountLookupService lookup,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    IConfiguration configuration) : ControllerBase
{
    public sealed record LoginRequest(string EmployeeCode, string Password);
    public sealed record LoginResponse(string EmployeeCode, string FullName, string Position, string Department, string AccessToken, DateTime ExpiresAtUtc);
    public sealed record ForgotPasswordRequest(string EmployeeCode);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await lookup.FindForEmployeeLoginAsync(request.EmployeeCode);
        if (user is null)
            return Unauthorized(new { message = "Mã nhân viên hoặc mật khẩu không đúng, hoặc tài khoản chưa thể đăng nhập." });

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password ?? string.Empty, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsNotAllowed) await users.AccessFailedAsync(user);
            return Unauthorized(new { message = "Mã nhân viên hoặc mật khẩu không đúng, hoặc tài khoản chưa thể đăng nhập." });
        }

        var employee = await db.Employees.AsNoTracking().Where(e => e.PersonId == user.PersonId)
            .Select(e => new { e.Id, e.EmployeeCode, e.Position, e.Department, e.Person.FullName }).SingleAsync();
        var signingKey = configuration["DesktopAuth:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
            return StatusCode(503, new { message = "Đăng nhập desktop chưa được cấu hình trên máy chủ." });

        var expires = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, employee.FullName ?? employee.EmployeeCode),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, AppRoles.Employee),
            new System.Security.Claims.Claim(AppClaimTypes.EmployeePosition, employee.Position.ToString()),
            new System.Security.Claims.Claim("employee_id", employee.Id.ToString()),
            new System.Security.Claims.Claim("employee_code", employee.EmployeeCode)
        };
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: configuration["DesktopAuth:Issuer"] ?? "FujiHomestay",
            audience: configuration["DesktopAuth:Audience"] ?? "FujiHomestay.Desktop",
            claims: claims,
            expires: expires,
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(employee.EmployeeCode, employee.FullName ?? employee.EmployeeCode, employee.Position.ToString(), employee.Department ?? string.Empty,
            new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token), expires));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        _ = await lookup.FindForEmployeeLoginAsync(request.EmployeeCode);
        return Ok(new { message = "Vui lòng liên hệ quản trị viên hệ thống để được hỗ trợ đặt lại mật khẩu." });
    }
}
