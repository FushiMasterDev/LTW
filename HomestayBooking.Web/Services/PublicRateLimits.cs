using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public sealed class PublicRateLimitOptions
{
    public const string Section = "RateLimits";
    [Range(1, int.MaxValue)] public int PermitLimit { get; set; } = 10;
    [Range(1, 86400)] public int WindowSeconds { get; set; } = 60;
}

public static class PublicRateLimits
{
    public const string Policy = "PublicLookupAndRegistration";

    public static IServiceCollection AddPublicRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PublicRateLimitOptions>().Bind(configuration.GetSection(PublicRateLimitOptions.Section))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddRateLimiter(options => options.AddPolicy(Policy, context =>
        {
            var limits = context.RequestServices.GetRequiredService<IOptions<PublicRateLimitOptions>>().Value;
            var ip = context.Connection.RemoteIpAddress;
            var key = (ip?.IsIPv4MappedToIPv6 == true ? ip.MapToIPv4() : ip)?.ToString() ?? "unknown";
            // Cùng ngân sách cho bốn endpoint để đổi route không né được giới hạn; không có global limiter.
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new()
            {
                PermitLimit = limits.PermitLimit, Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0, AutoReplenishment = true,
            });
        }));
        return services;
    }
}
