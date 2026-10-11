using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Controllers;

public class LanguageController : Controller
{
    private readonly RequestLocalizationOptions _localizationOptions;

    public LanguageController(IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _localizationOptions = localizationOptions.Value;
    }

    /// <summary>Lưu ngôn ngữ đã chọn vào cookie của CookieRequestCultureProvider (giữ 1 năm).</summary>
    [HttpPost]
    public IActionResult Set(string culture, string? returnUrl)
    {
        var supported = _localizationOptions.SupportedUICultures?.Any(c => c.Name == culture) == true;
        if (supported)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                });
        }

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
