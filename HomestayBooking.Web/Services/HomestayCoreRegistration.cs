using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Đăng ký Identity, policy và nghiệp vụ dùng chung; bên gọi chọn provider và job nền.</summary>
public static class HomestayCoreRegistration
{
    public static IServiceCollection AddHomestayCore(this IServiceCollection services, IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDatabase, bool registerBackgroundJobs = true)
    {
        services.AddDbContext<ApplicationDbContext>(configureDatabase);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.Section));

        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
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

        services.AddAuthorizationBuilder()
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

        services.AddAuthorizationBuilder()
            .AddPolicy(AppPolicies.ChangeRequestApprove, p => p.RequireRole(AppRoles.Employee)
                .RequireClaim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Manager)))
            .AddPolicy(AppPolicies.ChangeRequestFinance, p => p.RequireRole(AppRoles.Employee)
                .RequireClaim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Accountant)));

        services.AddDesktopApi(configuration);
        services.AddPublicRateLimits(configuration);

        services.AddScoped<AccountLookupService>();
        services.AddScoped<AccountActivityLogger>();
        services.AddMemoryCache();
        services.AddSingleton<BookingCodeVerificationAttempts>();
        services.AddScoped<BookingCodeVerificationService>();

        // Nghiệp vụ đặt phòng và thanh toán (fuji_homestay_database_v1 mục 6, 7)
        services.AddOptions<BookingOptions>()
            .Bind(configuration.GetSection(BookingOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<BankAccountOptions>(configuration.GetSection(BankAccountOptions.Section));
        services.Configure<VoucherOptions>(configuration.GetSection(VoucherOptions.Section));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PricingService>();
        services.AddScoped<AccountingService>();
        services.AddScoped<BookingCodeGenerator>();
        services.AddScoped<RefundService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<VoucherService>();
        services.AddScoped<BookingService>();
        services.AddScoped<BookingChangeRequestService>();
        services.AddScoped<ChangeRequestUiService>();
        services.AddScoped<BankMatchingService>();
        services.AddScoped<ReceiptReviewService>();
        services.AddScoped<VietQrService>();
        services.AddScoped<CustomerAccountService>();
        services.AddScoped<OwnerRegistrationService>();
        services.AddScoped<ContractService>();
        services.AddScoped<HomestayAccessService>();
        services.AddScoped<HomestayContentService>();
        services.AddScoped<RoomService>();
        services.AddScoped<HomestayImageService>();
        services.AddScoped<RoomBlockService>();
        services.AddScoped<HomestayDocumentService>();
        services.AddScoped<HomestayPolicyService>();
        services.AddScoped<HomestayReviewService>();
        services.AddScoped<AvailabilityService>();
        services.AddScoped<StaffBookingService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<OverbookingService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<IMessageSender, LoggingMessageSender>();
        services.AddScoped<MessageService>();
        services.AddScoped<ReminderService>();
        services.AddScoped<OwnerScheduleService>();
        services.AddScoped<OwnerBookingQueryService>();
        services.AddScoped<OwnerDashboardService>();
        services.AddScoped<OwnerResourceService>();
        services.AddScoped<SettlementService>();
        services.AddScoped<CompensationService>();
        services.AddScoped<ReportService>();
        services.AddScoped<AccountAdminService>();
        services.AddScoped<CatalogDeletionService>();
        services.AddScoped<CatalogEditorService>();
        services.AddScoped<GuestCustomerService>();
        services.AddScoped<OwnerRegistrationQueryService>();
        services.AddScoped<AdminBootstrapService>();
        services.Configure<AdminBootstrapOptions>(configuration.GetSection(AdminBootstrapOptions.Section));
        services.AddOptions<SettlementOptions>()
            .Bind(configuration.GetSection(SettlementOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<ReminderOptions>()
            .Bind(configuration.GetSection(ReminderOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ReceiptFileStore>();
        if (registerBackgroundJobs) services.AddHostedService<BookingTimeoutJob>();

        return services;
    }
}
