using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HomestayBooking.Web.DesktopApi;

public static class DesktopApiRegistration
{
    public const string StaffPolicy = "DesktopStaff";
    public const string LoginLimiter = "DesktopLogin";

    public static IServiceCollection AddDesktopApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DesktopApiOptions>().Bind(configuration.GetSection(DesktopApiOptions.Section)).ValidateDataAnnotations();
        services.AddSingleton<DesktopApiRuntime>();
        services.AddScoped<DesktopExceptionFilter>();
        // AddAuthentication() sans defaults preserves Identity.Application for all existing MVC/API routes.
        services.AddAuthentication().AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<DesktopApiRuntime, Microsoft.Extensions.Options.IOptions<IdentityOptions>>((options, runtime, identity) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true, ValidIssuer = runtime.Options.Issuer,
                    ValidateAudience = true, ValidAudience = runtime.Options.Audience,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = runtime.Key,
                    ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero,
                    NameClaimType = identity.Value.ClaimsIdentity.UserNameClaimType,
                    RoleClaimType = identity.Value.ClaimsIdentity.RoleClaimType,
                };
                options.Events = new()
                {
                    OnTokenValidated = async context =>
                    {
                        if (!runtime.Enabled) { context.Fail("Desktop API disabled."); return; }
                        var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                        // Factory puts Identity's stamp and NameIdentifier into the JWT unchanged.
                        var user = await signIn.ValidateSecurityStampAsync(context.Principal);
                        if (user is null || await signIn.UserManager.IsLockedOutAsync(user))
                        { context.Fail("Account unavailable or security stamp changed."); return; }
                        // Refresh employment/role claims too, so termination or a position change is effective immediately.
                        var factory = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
                        var fresh = await factory.CreateAsync(user);
                        if (!fresh.IsInRole(AppRoles.Employee) || !fresh.IsActiveEmployee())
                        { context.Fail("Active employee required."); return; }
                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(fresh.Claims,
                            JwtBearerDefaults.AuthenticationScheme, identity.Value.ClaimsIdentity.UserNameClaimType, identity.Value.ClaimsIdentity.RoleClaimType));
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        return WriteProblemAsync(context.HttpContext, 401, "Unauthorized");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, 403, "Forbidden"),
                };
            });
        services.AddAuthorizationBuilder().AddPolicy(StaffPolicy, p => p.RequireRole(AppRoles.Employee)
            .RequireClaim(AppClaimTypes.EmployeePosition, nameof(EmployeePosition.Consultant), nameof(EmployeePosition.Manager), nameof(EmployeePosition.Administrator)));
        services.AddRateLimiter(options =>
        {
            // Endpoint policy only: no GlobalLimiter and no changes to web login.
            options.AddPolicy(LoginLimiter, context =>
            {
                var runtime = context.RequestServices.GetRequiredService<DesktopApiRuntime>();
                return RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new()
                {
                    PermitLimit = runtime.Options.LoginPermitLimit,
                    Window = TimeSpan.FromSeconds(runtime.Options.LoginWindowSeconds), QueueLimit = 0,
                    AutoReplenishment = true,
                });
            });
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await WriteProblemAsync(context.HttpContext, 429, "Too Many Requests", ct);
            };
        });
        return services;
    }

    public static Task WriteProblemAsync(HttpContext context, int status, string title, CancellationToken ct = default)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title,
            Instance = context.Request.Path }, options: null, contentType: "application/problem+json", cancellationToken: ct);
    }
}
