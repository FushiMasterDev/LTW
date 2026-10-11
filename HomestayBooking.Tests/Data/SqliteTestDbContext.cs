using HomestayBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Data;

/// <summary>
/// SQLite lưu decimal dạng TEXT nên CHECK so sánh tiền thành so sánh chuỗi ('300000' > '1000000').
/// Chỉ trong test, lưu decimal dạng REAL để CHECK so sánh đúng như trên SQL Server.
/// </summary>
internal sealed class SqliteTestDbContext : ApplicationDbContext
{
    public SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }
}
