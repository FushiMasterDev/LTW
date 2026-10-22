using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.RateLimiting;

namespace HomestayBooking.Web.Controllers;

/// <summary>
/// FR-25 "Trở thành đối tác": form công khai tạo hồ sơ (RegisterAsync), rồi chủ đã đăng nhập tải giấy tờ
/// (UploadDocumentAsync), gửi hồ sơ (SubmitAsync) và theo dõi trạng thái (MineAsync). Quyền và trạng thái do service kiểm.
/// </summary>
public class PartnerController(
    OwnerRegistrationService registrations,
    OwnerRegistrationQueryService queries,
    AccountLookupService lookup,
    SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    private const string MissingKey = "PartnerMissing";
    private bool SignedIn => User.Identity?.IsAuthenticated == true;
    private Actor OwnerActor() => new(ActorType.Owner, users.GetUserId(User));

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (User.IsActiveEmployee())
        {
            TempData["Error"] = localizer["Nhân viên không đăng ký đối tác ở cổng khách hàng."].Value;
            return RedirectToAction("Index", "Home");
        }
        if (User.IsInRole(AppRoles.Owner) && await queries.MineAsync(OwnerActor()) is not null)
        {
            return RedirectToAction(nameof(Status));
        }
        var model = new PartnerRegisterViewModel();
        if (SignedIn && await users.GetUserAsync(User) is { } user)
        {
            var person = await db.PersonProfiles.AsNoTracking().SingleAsync(p => p.Id == user.PersonId);
            model.FullName = person.FullName ?? string.Empty;
            model.PhoneNumber = PhoneNumberNormalizer.ToNational(person.PhoneNumber);
        }
        return View(await FillAsync(model));
    }

    [HttpPost]
    [EnableRateLimiting(PublicRateLimits.Policy)]
    public async Task<IActionResult> Index(PartnerRegisterViewModel model)
    {
        if (User.IsActiveEmployee())
        {
            return Forbid();
        }
        if (!SignedIn && string.IsNullOrEmpty(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), localizer["Vui lòng nhập {0}.", localizer["Mật khẩu"]]);
        }
        if (!ModelState.IsValid)
        {
            return View(await FillAsync(model));
        }
        try
        {
            // Đã đăng nhập: service chỉ ghép hồ sơ vào đúng tài khoản đang dùng, mật khẩu cũ giữ nguyên.
            var actor = SignedIn ? new Actor(ActorType.Customer, users.GetUserId(User)) : null;
            await registrations.RegisterAsync(model.ToInput(), model.Password ?? string.Empty, actor);
            var user = SignedIn ? await users.GetUserAsync(User) : await lookup.FindForPhoneLoginAsync(model.PhoneNumber);
            if (user is not null)
            {
                // Vai trò Owner vừa được thêm: đăng nhập lại để cookie có vai trò mới.
                if (SignedIn) await signIn.RefreshSignInAsync(user);
                else await signIn.SignInAsync(user, isPersistent: false);
            }
            TempData["Success"] = localizer["Đã tạo hồ sơ đối tác. Tải giấy tờ rồi gửi hồ sơ để Fuji thẩm định."].Value;
            return RedirectToAction(nameof(Status));
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError(string.Empty, localizer[ex.ResourceKey, ex.Arguments]);
            return View(await FillAsync(model));
        }
    }

    [Authorize(Roles = AppRoles.Owner)]
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var registration = await queries.MineAsync(OwnerActor());
        if (registration is null)
        {
            return RedirectToAction(nameof(Index));
        }
        var missing = TempData[MissingKey] is string text ? text.Split('\n', StringSplitOptions.RemoveEmptyEntries) : [];
        var editable = registration.Status is OwnerRegistrationStatus.Draft or OwnerRegistrationStatus.NeedsRevision;
        return View(new PartnerStatusViewModel(registration, editable, missing));
    }

    [Authorize(Roles = AppRoles.Owner)]
    [HttpPost]
    [RequestSizeLimit(ReceiptFileStore.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(int homestayId, DocumentType type, IFormFile? file, DateOnly? expiryDate)
    {
        try
        {
            if (file is null) throw new BookingException("Vui lòng chọn file giấy tờ.");
            await registrations.UploadDocumentAsync(homestayId, type, file, expiryDate, OwnerActor());
            TempData["Success"] = localizer["Đã tải giấy tờ lên."].Value;
        }
        catch (BookingException ex)
        {
            TempData["Error"] = localizer[ex.ResourceKey, ex.Arguments].Value;
        }
        return RedirectToAction(nameof(Status));
    }

    [Authorize(Roles = AppRoles.Owner)]
    [HttpPost]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            var result = await registrations.SubmitAsync(id, OwnerActor());
            if (result.Succeeded)
            {
                TempData["Success"] = localizer["Đã gửi hồ sơ. Nhân viên Fuji thẩm định trong 3 ngày làm việc."].Value;
            }
            else
            {
                TempData[MissingKey] = string.Join('\n', result.Missing);
                TempData["Error"] = localizer["Hồ sơ còn thiếu, vui lòng bổ sung rồi gửi lại."].Value;
            }
        }
        catch (BookingException ex)
        {
            TempData["Error"] = localizer[ex.ResourceKey, ex.Arguments].Value;
        }
        return RedirectToAction(nameof(Status));
    }

    /// <summary>Xem lại giấy tờ riêng tư của chính chủ; service chặn người khác.</summary>
    [Authorize(Roles = AppRoles.Owner)]
    [HttpGet]
    public async Task<IActionResult> Document(int id)
    {
        try
        {
            var file = await queries.OpenDocumentAsync(id, OwnerActor());
            return PhysicalFile(file.Path, file.ContentType, file.DownloadName);
        }
        catch (BookingException)
        {
            return NotFound();
        }
    }

    [AllowAnonymous,HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public IActionResult Activate(int id,string? token)
    {
        if(User.IsActiveEmployee()) return Forbid();
        Response.Headers["Referrer-Policy"]="no-referrer";
        var savedToken=TempData["PartnerActivationToken:"+id] as string;
        return View(new PartnerActivationForm {Id=id,Token=token ?? savedToken ?? "",
            PhoneNumber=TempData["PartnerActivationPhone:"+id] as string ?? ""});
    }
    [AllowAnonymous,HttpPost,ValidateAntiForgeryToken,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Activate(PartnerActivationForm form)
    {
        if(User.IsActiveEmployee()) return Forbid();
        Response.Headers["Referrer-Policy"]="no-referrer";
        try {
            if(!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            await registrations.ActivateAsync(form.Id,form.PhoneNumber,form.Token,form.Password,SignedIn ? OwnerActor() : null);
            TempData["Success"]=localizer["Đã kích hoạt tài khoản. Vui lòng đăng nhập để bổ sung và gửi hồ sơ."].Value;
            return RedirectToAction("Login","Account");
        } catch(BookingException ex) {
            TempData["Error"]=localizer[ex.ResourceKey,ex.Arguments].Value;
            TempData["PartnerActivationToken:"+form.Id]=form.Token;
            TempData["PartnerActivationPhone:"+form.Id]=form.PhoneNumber;
            return RedirectToAction(nameof(Activate),new {id=form.Id});
        }
    }

    private async Task<PartnerRegisterViewModel> FillAsync(PartnerRegisterViewModel model)
    {
        model.SignedIn = SignedIn;
        model.Areas = await db.Areas.AsNoTracking().OrderBy(a => a.Name)
            .Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToListAsync();
        return model;
    }
}
