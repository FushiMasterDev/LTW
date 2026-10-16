using HomestayBooking.Web;
using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Cookie chỉ gửi qua HTTPS ở môi trường thật; khi dev cho phép http://localhost
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.Section));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        // Email nằm ở PERSON_PROFILE và có thể trống; định danh đăng nhập là SĐT hoặc mã nhân viên (v5)
        o.User.RequireUniqueEmail = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<AppSignInManager>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
    .AddErrorDescriber<VietnameseIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.Employee, p => p
        .RequireRole(AppRoles.Employee)
        .RequireClaim(AppClaimTypes.EmployeePosition))
    .AddPolicy(AppPolicies.Management, p => p
        .RequireRole(AppRoles.Employee)
        .RequireClaim(AppClaimTypes.EmployeePosition,
            nameof(EmployeePosition.Manager), nameof(EmployeePosition.Administrator)))
    .AddPolicy(AppPolicies.Accounting, p => p
        .RequireRole(AppRoles.Employee)
        .RequireClaim(AppClaimTypes.EmployeePosition,
            nameof(EmployeePosition.Accountant), nameof(EmployeePosition.Manager), nameof(EmployeePosition.Administrator)))
    .AddPolicy(AppPolicies.ApprovedOwner, p => p
        .RequireRole(AppRoles.Owner)
        .RequireClaim(AppClaimTypes.OwnerApproved, "true"));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.ChangeRequestApprove, p => p.RequireRole(AppRoles.Employee)
        .RequireClaim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Manager)))
    .AddPolicy(AppPolicies.ChangeRequestFinance, p => p.RequireRole(AppRoles.Employee)
        .RequireClaim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Accountant)));

builder.Services.AddScoped<AccountLookupService>();
builder.Services.AddScoped<AccountActivityLogger>();

// Nghiệp vụ đặt phòng và thanh toán (fuji_homestay_database_v1 mục 6, 7)
builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<BankAccountOptions>(builder.Configuration.GetSection(BankAccountOptions.Section));
builder.Services.Configure<VoucherOptions>(builder.Configuration.GetSection(VoucherOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<BookingCodeGenerator>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<BookingChangeRequestService>();
builder.Services.AddScoped<ChangeRequestUiService>();
builder.Services.AddScoped<BankMatchingService>();
builder.Services.AddScoped<ReceiptReviewService>();
builder.Services.AddScoped<VietQrService>();
builder.Services.AddScoped<CustomerAccountService>();
builder.Services.AddScoped<OwnerRegistrationService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddSingleton<ReceiptFileStore>();
builder.Services.AddHostedService<BookingTimeoutJob>();

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
        o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "RequestVerificationToken";
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");
app.UseHttpsRedirection();

// MapStaticAssets chỉ phục vụ file có sẵn lúc build; ảnh upload lúc chạy (wwwroot/uploads) cần UseStaticFiles
app.UseStaticFiles();

app.UseRequestLocalization(); // cấu hình ở RequestLocalizationOptions phía trên

app.UseRouting();
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

await SeedData.InitializeAsync(app.Services);

app.Run();
