using HomestayBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.DesktopHost;

/// <summary>Giữ model/ràng buộc Web; REAL cho decimal để CHECK, sort và aggregate tiền dùng số.</summary>
public class DesktopDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }
}
