using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Sinh mã đơn FJyyMMdd-NNN (BR-07). Tăng bộ đếm bằng một câu UPDATE nguyên tử; phải gọi trong transaction
/// của việc tạo đơn để hai đơn tạo cùng lúc không lấy trùng số (dòng bộ đếm bị khóa tới khi commit).
/// </summary>
public class BookingCodeGenerator
{
    private readonly ApplicationDbContext _db;

    public BookingCodeGenerator(ApplicationDbContext db)
    {
        _db = db;
    }

    public static string Format(DateOnly date, int sequence) => $"FJ{date:yyMMdd}-{sequence:000}";

    public async Task<string> NextAsync(DateOnly date)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var updated = await _db.BookingCodeCounters
                .Where(c => c.Date == date)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastSeq, c => c.LastSeq + 1));
            if (updated == 1)
            {
                var seq = await _db.BookingCodeCounters.Where(c => c.Date == date).Select(c => c.LastSeq).SingleAsync();
                return Format(date, seq);
            }

            // Ngày mới: tạo bộ đếm. Request khác tạo trước (trùng khóa chính) → thử UPDATE lại
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO BookingCodeCounters (Date, LastSeq) VALUES ({date}, 1)");
                return Format(date, 1);
            }
            catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
            {
            }
        }

        throw new InvalidOperationException("Không sinh được mã đơn.");
    }
}
