using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.RateLimiting;

namespace HomestayBooking.Web.Controllers;

/// <summary>
/// Hai cổng đăng nhập dùng chung bảng Account (fuij_homestay_login_design_v5 mục 8):
/// Customer/Owner bằng SĐT + mật khẩu; Employee bằng mã nhân viên + mật khẩu.
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccountLookupService _lookup;
    private readonly AccountActivityLogger _activityLogger;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly CustomerAccountService _customers;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        AccountLookupService lookup,
        AccountActivityLogger activityLogger,
        IStringLocalizer<SharedResource> localizer,
        CustomerAccountService customers)
    {
        _customers = customers;
        _signInManager = signInManager;
        _userManager = userManager;
        _lookup = lookup;
        _activityLogger = activityLogger;
        _localizer = localizer;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }

        // Cookie chỉ có một LoginPath; trang nội bộ thì chuyển sang cổng nhân viên
        if (IsEmployeeArea(returnUrl))
        {
            return RedirectToAction(nameof(EmployeeLogin), new { returnUrl });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _lookup.FindForPhoneLoginAsync(model.PhoneNumber);
        if (!await TrySignInAsync(user, model.Password, model.RememberMe))
        {
            return View(model);
        }

        return await RedirectAfterSignInAsync(user!, model.ReturnUrl);
    }

    /// <summary>Khách tự đăng ký bằng SĐT + mật khẩu (tạm thời chưa có bước OTP).</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true
            ? LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/")
            : View(new RegisterViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(PublicRateLimits.Policy)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _customers.RegisterAsync(model.FullName, model.PhoneNumber, model.Email, model.Password, model.BookingCode);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, _localizer[error]);
            }
            return View(model);
        }

        await _signInManager.SignInAsync(result.User!, isPersistent: false);
        TempData["Success"] = _localizer["Đăng ký thành công. Chào mừng bạn đến Fuji Homestay!"].Value;
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult EmployeeLogin(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true
            ? LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/")
            : View(new EmployeeLoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> EmployeeLogin(EmployeeLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _lookup.FindForEmployeeLoginAsync(model.EmployeeCode);
        if (!await TrySignInAsync(user, model.Password, model.RememberMe))
        {
            return View(model);
        }

        return await RedirectAfterSignInAsync(user!, model.ReturnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.PasswordChangedAt = user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _activityLogger.LogAsync(user.Id, AccountEventType.PasswordChanged, ActorTypeOf(User), user.Id);

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = _localizer["Đã đổi mật khẩu."].Value;
        return RedirectToAction(nameof(ChangePassword));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>
    /// Mọi trường hợp thất bại (không tìm thấy, sai mật khẩu, tài khoản không Active, đang bị khóa tạm)
    /// đều trả cùng một thông báo, để không dò được SĐT/mã nhân viên nào tồn tại (v5 mục 8.1, 8.2).
    /// </summary>
    private async Task<bool> TrySignInAsync(ApplicationUser? user, string password, bool rememberMe)
    {
        if (user is not null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return true;
            }
            if (result.IsNotAllowed)
            {
                // Account không Active: SignInManager trả trước khi đếm sai, nên tự đếm để vẫn chặn đoán mật khẩu
                await _userManager.AccessFailedAsync(user);
            }
        }

        ModelState.AddModelError(string.Empty,
            _localizer["Thông tin đăng nhập không đúng hoặc tài khoản tạm thời không thể đăng nhập."]);
        return false;
    }

    private async Task<IActionResult> RedirectAfterSignInAsync(ApplicationUser user, string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        // Claim chức vụ chỉ có trong principal mới tạo, nên xác định trang đích từ CSDL
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        if (principal.IsManagement())
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        if (principal.IsActiveEmployee())
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Staff" });
        }
        if (principal.IsInRole(AppRoles.Owner))
        {
            return RedirectToAction("Index", "Dashboard", new { area = AppRoles.Owner });
        }
        return RedirectToAction("Index", "Home");
    }

    private bool IsEmployeeArea(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
        && (returnUrl!.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
            || returnUrl.StartsWith("/Staff", StringComparison.OrdinalIgnoreCase));

    private static ActorType ActorTypeOf(System.Security.Claims.ClaimsPrincipal principal) =>
        principal.IsActiveEmployee() ? ActorType.Employee
        : principal.IsInRole(AppRoles.Owner) ? ActorType.Owner
        : ActorType.Customer;
}
