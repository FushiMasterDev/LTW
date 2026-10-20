using HomestayBooking.Web.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HomestayBooking.Web.Services;

/// <summary>Chỉ trả trạng thái; không công bố chuỗi kết nối hay lỗi SQL.</summary>
public sealed class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
