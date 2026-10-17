using System.Net;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Tests.Integration;

/// <summary>FR-06 trên pipeline thật: đơn đã hoàn thành chưa có đánh giá hiện form, gửi xong thì hiện lại đánh giá thay cho form.</summary>
public sealed class GuestReviewPageTests
{
    [Fact]
    public async Task CustomerReviewsCompletedBookingFromDetailsPage()
    {
        using var f = new TestWebApplicationFactory();
        var (id, code) = f.WithDb(db =>
        {
            var booking = db.Bookings.Where(b => b.Status == BookingStatus.Completed && b.Customer.Person.PhoneNumber == "+84987000001")
                .OrderBy(b => b.Id).Select(b => new { b.Id, b.Code }).First();
            db.Reviews.RemoveRange(db.Reviews.Where(r => r.BookingId == booking.Id));
            db.SaveChanges();
            return (booking.Id, booking.Code);
        });
        var client = await f.SignInByPhoneAsync("0987000001");
        var url = $"/Bookings/Details/{code}";
        Assert.Contains("name=\"rating\"", await client.GetStringAsync(url));

        var token = await TestWebApplicationFactory.TokenFromAsync(client, url);
        var response = await client.PostAsync($"/Bookings/Review/{code}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["rating"] = "5", ["comment"] = "Phòng sạch, chủ nhà <b>nhiệt tình</b>",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(5, f.WithDb(db => db.Reviews.Single(r => r.BookingId == id).Rating));
        var after = await client.GetStringAsync(url);
        Assert.DoesNotContain("name=\"rating\"", after);
        Assert.Contains("&lt;b&gt;", after); Assert.DoesNotContain("<b>nhi", after);
        Assert.Contains("Phòng sạch, chủ nhà <b>nhiệt tình</b>", WebUtility.HtmlDecode(after));
    }
}
