using HomestayBooking.DesktopHost;
using HomestayBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Data;

/// <summary>Test dùng cùng phép chuyển decimal → REAL như host SQLite độc lập.</summary>
internal sealed class SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options) : DesktopDbContext(options);
