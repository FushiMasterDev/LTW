using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Ui;

public class RoomCalendarModelTests
{
    [Fact]
    public void MonthStartingMidweekPadsToCompleteMondayWeeks()
    {
        var model = new RoomCalendarModel(new(2026, 7, 15), []);
        Assert.Equal(new DateOnly(2026, 7, 1), model.Month);
        Assert.Equal(new DateOnly(2026, 6, 29), model.Days[0]);
        Assert.Equal(new DateOnly(2026, 8, 2), model.Days[^1]);
        Assert.Equal(35, model.Days.Count);
        Assert.All(model.Days.Where((_, index) => index % 7 == 0), date => Assert.Equal(DayOfWeek.Monday, date.DayOfWeek));
    }

    [Fact]
    public void LeapFebruaryContains29NightsAndCompleteWeeks()
    {
        var model = new RoomCalendarModel(new(2028, 2, 1), []);
        Assert.Equal(29, model.Days.Count(d => d.Month == 2));
        Assert.Contains(new DateOnly(2028, 2, 29), model.Days);
        Assert.Equal(new DateOnly(2028, 1, 31), model.Days[0]);
        Assert.Equal(new DateOnly(2028, 3, 5), model.Days[^1]);
    }

    [Fact]
    public void StayAcrossTwoMonthsShowsBothOccupiedNightsButNotCheckoutNight()
    {
        CalendarNight[] nights = [new(1, "101", new(2026, 10, 31), CalendarNightState.Booked, "FJ-DEMO"), new(1, "101", new(2026, 11, 1), CalendarNightState.Booked, "FJ-DEMO")];
        var october = new RoomCalendarModel(new(2026, 10, 1), nights);
        var november = new RoomCalendarModel(new(2026, 11, 1), nights);
        var room = Assert.Single(november.Rooms);
        foreach (var model in new[] { october, november })
        {
            Assert.Contains(new DateOnly(2026, 10, 31), model.Days);
            Assert.Contains(new DateOnly(2026, 11, 1), model.Days);
            Assert.Equal(CalendarNightState.Booked, model.Night(room, new(2026, 10, 31)).State);
            Assert.Equal(CalendarNightState.Booked, model.Night(room, new(2026, 11, 1)).State);
        }
        Assert.Equal(CalendarNightState.Free, november.Night(room, new(2026, 11, 2)).State);
    }
}
