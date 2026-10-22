using HomestayBooking.Web;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

var reverseProxyEnabled = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
    options.KnownProxies.Add(IPAddress.Loopback.MapToIPv6());
});

var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "App_Data/keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keysPath, builder.Environment.ContentRootPath)))
    .SetApplicationName("FujiHomestay");
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddScoped<PublicUrlService>();

// Cookie chỉ gửi qua HTTPS ở môi trường thật; khi dev cho phép http://localhost
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.AddHomestayCore(builder.Configuration,
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Kiểm tra lại cookie mỗi phút: tài khoản bị vô hiệu hóa hoặc đổi security stamp sẽ bị đăng xuất
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = cookieSecurePolicy;
    // Web API (/api/...) trả 401/403 thay vì chuyển hướng sang trang đăng nhập/không có quyền
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// Không mã hóa chữ tiếng Việt thành &#x...; trong HTML (Razor vẫn chống XSS với các ký tự đặc biệt)
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    string[] cultures = ["vi-VN", "en-US"];
    o.SetDefaultCulture(cultures[0])
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
    // Chỉ đổi ngôn ngữ khi người dùng chọn (cookie) hoặc ?culture=; không theo ngôn ngữ trình duyệt
    o.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
    ];
});
builder.Services.AddControllersWithViews(o =>
    {
        o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        if (!builder.Environment.IsDevelopment()) o.Conventions.Add(new ProductionControllerConvention());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

// TempData có thể chứa lời mời đối tác/mật khẩu tạm đã mã hóa, cũng cần cookie Secure.
builder.Services.Configure<CookieTempDataProviderOptions>(o => o.Cookie.SecurePolicy = cookieSecurePolicy);

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "RequestVerificationToken";
    // X-Frame-Options đặt chung là DENY ở middleware header bảo mật (app không dùng iframe), không để antiforgery đặt SAMEORIGIN
    o.SuppressXFrameOptionsHeader = true;
    o.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Fuji Homestay API", Version = "v1", Description = "API công khai cho tìm kiếm, lịch trống, báo giá, tra cứu đơn và số thông báo chưa đọc." });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
    // Chỉ đưa Web API (/api/...) lên Swagger; các action MVC có attribute route (alias /{id}) không phải API.
    // Nhóm /api/desktop dành cho WinForms, có Swagger riêng ở HomestayBooking.DesktopHost.
    o.DocInclusionPredicate((_, api) => api.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true
        && !api.RelativePath.StartsWith("api/desktop/", StringComparison.OrdinalIgnoreCase));
});

var app = builder.Build();
// Eagerly validate the isolated configuration and log disabled state without stopping the website.
_ = app.Services.GetRequiredService<DesktopApiRuntime>();

if (reverseProxyEnabled)
{
    // Funnel terminates TLS and may omit X-Forwarded-Proto. Inspect the socket peer
    // before forwarded headers replace it with the public client's address.
    if (builder.Configuration.GetValue("ReverseProxy:AssumeHttps", true))
        app.Use(async (context, next) =>
        {
            var peer = context.Connection.RemoteIpAddress;
            if (peer is not null && IPAddress.IsLoopback(peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer))
                context.Request.Scheme = "https";
            await next();
        });
    app.UseForwardedHeaders();
}

// Header bảo mật cơ bản cho mọi phản hồi (QA trước ra mắt). CSP đầy đủ chưa bật: script chống nháy theme trong
// _HeadAssets là inline, cần nonce trước khi siết script-src.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // Chỉ đặt mặc định: trang nào tự đặt chặt hơn (ví dụ mật khẩu tạm dùng Referrer-Policy: no-referrer) thì giữ nguyên
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
        headers.TryAdd("Content-Security-Policy", "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'");
        return Task.CompletedTask;
    });
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    if (!reverseProxyEnabled) app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api/desktop"),
    branch => branch.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}"));
// Also cover unmatched desktop routes without re-executing an HTML error page.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/desktop"), branch =>
    branch.UseStatusCodePages(async context => await DesktopApiRegistration.WriteProblemAsync(
        context.HttpContext, context.HttpContext.Response.StatusCode,
        Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode))));
// The public proxy owns TLS/HSTS/redirection; never redirect its HTTP backend.
if (!reverseProxyEnabled)
    app.UseWhen(context => context.Request.Path != "/healthz", branch => branch.UseHttpsRedirection());

// MapStaticAssets chỉ phục vụ file có sẵn lúc build; ảnh upload lúc chạy (wwwroot/uploads) cần UseStaticFiles
app.UseStaticFiles();

app.UseRequestLocalization(); // cấu hình ở RequestLocalizationOptions phía trên

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers(); // Web API (attribute routing)
app.MapHealthChecks("/healthz").AllowAnonymous();

await SeedData.InitializeAsync(app.Services);
using (var bootstrapScope = app.Services.CreateScope())
{
    // FR-21: ngoài Development, tạo Quản trị viên đầu tiên từ cấu hình bí mật nếu CSDL chưa có ai.
    await bootstrapScope.ServiceProvider.GetRequiredService<AdminBootstrapService>().RunAsync();
}

app.Run();
