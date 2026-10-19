using System.Globalization;
using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class B9WorkspaceUiTests
{
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values) => new(
        values.Append(("__RequestVerificationToken", token)).Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));

    [Fact]
    public async Task OwnerCanSelectAndClearAmenitiesAndCreateRoomTypeWithCatalog()
    {
        using var f = new TestWebApplicationFactory(); var client = await f.SignInByPhoneAsync("0912000001");
        var home = f.WithDb(db => db.Homestays.First(h => h.Owner.Person.PhoneNumber == "+84912000001").Id);
        var path = $"/Owner/Homestays/Edit/{home}?tab=amenities";
        var token = await TestWebApplicationFactory.TokenFromAsync(client, path);
        var amenity = f.WithDb(db => db.Amenities.OrderBy(a => a.Id).First().Id);
        await client.PostAsync($"/Owner/Homestays/Amenities/{home}", Form(token, ("amenityIds", amenity.ToString())));
        Assert.Equal(amenity, f.WithDb(db => db.HomestayAmenities.Single(a => a.HomestayId == home).AmenityId));
        await client.PostAsync($"/Owner/Homestays/Amenities/{home}", Form(token));
        Assert.Equal(0, f.WithDb(db => db.HomestayAmenities.Count(a => a.HomestayId == home)));
        var category = f.WithDb(db => db.RoomCategories.First().Id);
        await client.PostAsync($"/Owner/Homestays/CreateRoomType/{home}", Form(token, ("name", "Loại phòng B9"), ("categoryId", category.ToString()),
            ("maxGuests", "3"), ("weekdayPrice", "500000"), ("weekendPrice", "600000"), ("holidayPrice", "700000")));
        Assert.Equal(3, f.WithDb(db => db.RoomTypePrices.Count(p => p.RoomType.Name == "Loại phòng B9")));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/Owner/Homestays/Amenities/{home}", new FormUrlEncodedContent([]))).StatusCode);
        var foreign = f.WithDb(db => db.Homestays.First(h => h.Owner.Person.PhoneNumber != "+84912000001").Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/Owner/Homestays/Amenities/{foreign}", Form(token))).StatusCode);
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task AccountantOnBehalfRequiresReasonAndPaymentFormBlocksMistypedAmount()
    {
        using var f = new TestWebApplicationFactory(); var client = await f.SignInEmployeeAsync("KT001");
        var id = f.WithDb(db => { var s = db.Settlements.First(); s.Status = SettlementStatus.Sent; s.SentAt = DateTime.UtcNow.AddHours(-1);
            s.ConfirmedAt = s.PaidAt = null; db.SaveChanges(); return s.Id; });
        var path = $"/Accounting/Settlements/{id}"; var token = await TestWebApplicationFactory.TokenFromAsync(client, path);
        Assert.Contains("on-behalf-reason", await client.GetStringAsync(path));
        await client.PostAsync($"/Accounting/Settlements/ConfirmOnBehalf/{id}", Form(token, ("reason", " ")));
        Assert.Equal(SettlementStatus.Sent, f.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        await client.PostAsync($"/Accounting/Settlements/ConfirmOnBehalf/{id}", Form(token, ("reason", "Đã đối chiếu cùng chủ")));
        Assert.Equal(SettlementStatus.Confirmed, f.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        var amount = f.WithDb(db => db.Settlements.Single(s => s.Id == id).PayoutAmount);
        var when = VietnamTime.ToLocal(DateTime.UtcNow).AddSeconds(1).ToString("yyyy-MM-ddTHH:mm:ss");
        // Wrong amount is checked before date; no financial write may happen.
        await client.PostAsync($"/Accounting/Settlements/RecordPaid/{id}", Form(token, ("bankRef", "B9-TYPO"), ("amount", (amount+1).ToString(CultureInfo.InvariantCulture)), ("paidAt", when)));
        Assert.Equal(SettlementStatus.Confirmed, f.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        Assert.Contains("không hỗ trợ chi từng phần", WebUtility.HtmlDecode(await client.GetStringAsync(path)));
        var manager = await f.SignInEmployeeAsync("QL001"); var managerToken = await TestWebApplicationFactory.TokenFromAsync(manager,path);
        Assert.Equal("/Account/AccessDenied", (await manager.PostAsync($"/Accounting/Settlements/ConfirmOnBehalf/{id}", Form(managerToken,("reason","Bị chặn")))).Headers.Location!.AbsolutePath);
        Assert.Empty(f.Errors);
    }
    [Fact]
    public async Task RejectableCompensationTabPostsReasonAndRemovesProcessedRow()
    {
        using var f = new TestWebApplicationFactory();
        var id = f.WithDb(db => {
            var booking = db.Bookings.First(b => b.Status == BookingStatus.Confirmed);
            var p = new Payment { BookingId = booking.Id, Type = PaymentType.Compensation, Method = PaymentMethod.ManualTransfer,
                Amount = 120000, Status = PaymentStatus.Submitted };
            db.Payments.Add(p); db.SaveChanges(); return p.Id;
        });
        var client = await f.SignInEmployeeAsync("KT001"); var path = "/Accounting/Compensations?rejectable=true";
        var token = await TestWebApplicationFactory.TokenFromAsync(client,path); var html = await client.GetStringAsync(path);
        Assert.Contains($"/Accounting/Compensations/Reject/{id}", html); Assert.Contains("data-confirm-variant=\"danger\"", html);
        var rejectedForm = await client.PostAsync($"/Accounting/Compensations/Reject/{id}", Form(token,("reason"," ")));
        Assert.Contains("rejectable=True", rejectedForm.Headers.Location!.OriginalString);
        Assert.Equal(PaymentStatus.Submitted, f.WithDb(db=>db.Payments.Single(p=>p.Id==id).Status));
        await client.PostAsync($"/Accounting/Compensations/Reject/{id}", Form(token,("reason","B9: khoản bù không còn phù hợp")));
        Assert.Equal(PaymentStatus.Rejected, f.WithDb(db=>db.Payments.Single(p=>p.Id==id).Status));
        Assert.DoesNotContain($"/Accounting/Compensations/Reject/{id}", await client.GetStringAsync(path));
        Assert.Empty(f.Errors);
    }

}
