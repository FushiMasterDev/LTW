using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

/// <summary>Bookings/Mine chia đơn của khách vào ba tab; mỗi đơn hiện ở đúng một tab.</summary>
public sealed class MyBookingsTabsTests
{
    [Fact]
    public async Task EachBookingAppearsInExactlyOneTab()
    {
        using var f = new TestWebApplicationFactory();
        var today = VietnamTime.Today(TimeProvider.System);
        var (cancelled, completed, pastConfirmed, upcoming) = f.WithDb(db =>
        {
            var own = db.Bookings.Where(b => b.Customer.Person.PhoneNumber == "+84987000001").OrderBy(b => b.Id).ToList();
            Assert.True(own.Count >= 4, "Dữ liệu mẫu cần ít nhất 4 đơn của 0987000001");
            // Chỉ giữ 4 đơn cho khách để không phụ thuộc phân trang; các đơn còn lại chuyển sang khách khác.
            var otherCustomerId = db.Customers.First(c => c.Id != own[0].CustomerId).Id;
            foreach (var b in own.Skip(4)) b.CustomerId = otherCustomerId;
            void Set(Booking b, BookingStatus status, int fromToday)
            {
                b.Status = status; b.CheckIn = today.AddDays(fromToday); b.CheckOut = today.AddDays(fromToday + 2);
            }
            Set(own[0], BookingStatus.Cancelled, 10);
            Set(own[1], BookingStatus.Completed, -5);
            Set(own[2], BookingStatus.Confirmed, -5);
            Set(own[3], BookingStatus.Confirmed, 10);
            db.SaveChanges();
            return (own[0].Code, own[1].Code, own[2].Code, own[3].Code);
        });
        var client = await f.SignInByPhoneAsync("0987000001");
        var tabs = new Dictionary<string, string>
        {
            ["upcoming"] = await client.GetStringAsync("/Bookings/Mine"),
            ["past"] = await client.GetStringAsync("/Bookings/Mine?tab=past"),
            ["cancelled"] = await client.GetStringAsync("/Bookings/Mine?tab=cancelled"),
        };

        void InOnly(string code, string tab)
        {
            foreach (var (name, html) in tabs)
                Assert.True(html.Contains($"/Bookings/Details/{code}\"") == (name == tab), $"Đơn {code} ở tab {name}, mong đợi chỉ ở {tab}");
        }
        InOnly(cancelled, "cancelled");
        InOnly(completed, "past");
        InOnly(pastConfirmed, "past");
        InOnly(upcoming, "upcoming");
    }
}
