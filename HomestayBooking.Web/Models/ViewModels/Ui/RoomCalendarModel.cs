using System.Globalization;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;

namespace HomestayBooking.Web.Models.ViewModels.Ui;

/// <param name="Id">Id phòng.</param>
/// <param name="Code">Mã phòng hiển thị.</param>
public record CalendarRoom(int Id, string Code);

/// <summary>Lưới tháng bắt đầu thứ Hai; chỉ dựng dữ liệu hiển thị, không truy vấn hoặc thay đổi lịch.</summary>
public record RoomCalendarModel
{
    public DateOnly Month { get; }
    public IReadOnlyList<CalendarRoom> Rooms { get; }
    public IReadOnlyList<DateOnly> Days { get; }
    private readonly Dictionary<(int Room, DateOnly Date), CalendarNight> _nights;

    /// <param name="month">Tháng cần hiển thị; chuẩn hóa về ngày đầu tháng.</param>
    /// <param name="nights">Dữ liệu CalendarAsync đã nạp cả khoảng ngày.</param>
    /// <param name="rooms">Danh sách phòng khi không có dòng lịch; mặc định suy từ DTO.</param>
    public RoomCalendarModel(DateOnly month, IReadOnlyList<CalendarNight> nights, IReadOnlyList<CalendarRoom>? rooms = null)
    {
        if (month.Year is < 2 or > 9998) throw new ArgumentOutOfRangeException(nameof(month));
        Month = new(month.Year, month.Month, 1);
        Rooms = rooms?.ToArray() ?? nights.Select(n => new CalendarRoom(n.RoomId, n.RoomCode)).Distinct().ToArray();
        _nights = nights.ToDictionary(n => (n.RoomId, n.Date));
        var leading = ((int)Month.DayOfWeek + 6) % 7;
        var count = (leading + DateTime.DaysInMonth(Month.Year, Month.Month) + 6) / 7 * 7;
        Days = Enumerable.Range(0, count).Select(i => Month.AddDays(i - leading)).ToArray();
    }

    public CalendarNight Night(CalendarRoom room, DateOnly date) => _nights.GetValueOrDefault((room.Id, date))
        ?? new CalendarNight(room.Id, room.Code, date, CalendarNightState.Free);

    public static DateOnly ParseMonth(string? text, DateOnly fallback) =>
        DateOnly.TryParseExact(text, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date.Year is >= 2 and <= 9998
            ? date : new(fallback.Year, fallback.Month, 1);

    public static (string Label, string Tone, string Icon) Describe(CalendarNight night) => night.State switch
    {
        CalendarNightState.Booked => ("Đã đặt", "tone-sky", "calendar-check"),
        CalendarNightState.ChangeRequestHeld => ("Giữ đổi", "tone-violet", "hourglass-split"),
        CalendarNightState.Blocked when night.BlockReason == RoomBlockReason.Maintenance => ("Bảo trì", "tone-amber", "wrench"),
        CalendarNightState.Blocked => ("Khóa", "tone-slate", "lock"),
        _ => ("Trống", "tone-emerald", "check2"),
    };

    public static string MonthLink(PathString path, IQueryCollection query, DateOnly month)
    {
        var values = query.Where(q => !q.Key.Equals("month", StringComparison.OrdinalIgnoreCase))
            .SelectMany(q => q.Value.Select(v => new KeyValuePair<string, string?>(q.Key, v))).ToList();
        values.Add(new("month", month.ToString("yyyy-MM", CultureInfo.InvariantCulture)));
        return path + QueryString.Create(values).ToString();
    }
}
