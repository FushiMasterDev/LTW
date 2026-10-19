using System.Net;
using System.Net.Http.Headers;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Integration;

public sealed partial class ChangeRequestUiTests
{
    [Fact]
    public async Task CustomerSeesQrBeforeSubmissionAndWaitingMessageAfterUploadingReceipt()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f, true); var id = await Awaiting(f, c, false);
        var customer = await f.SignInByPhoneAsync("0987000001");
        var url = $"/Bookings/Details/{c.Code}";
        var before = Html(await customer.GetStringAsync(url));
        Assert.Contains($"data-additional-payment=\"{id}\"", before); Assert.Contains($"CR{id} {c.Code}", before);
        Assert.Contains("amount=300000", before); Assert.Contains("data-change-countdown", before);
        var token = await TestWebApplicationFactory.TokenFromAsync(customer, url);
        var bytes = HomestayBooking.Tests.Services.TestFiles.Png;
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), "__RequestVerificationToken"); content.Add(new StringContent(id.ToString()), "requestId");
        var image = new ByteArrayContent(bytes); image.Headers.ContentType = new MediaTypeHeaderValue("image/png"); content.Add(image, "receipt", "receipt.png");
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await customer.PostAsync($"/Bookings/UploadChangeReceipt/{c.Code}", content)).StatusCode);
        var after = Html(await customer.GetStringAsync(url));
        Assert.Contains("Đã nộp cọc bổ sung, chờ kế toán xác nhận.", after); Assert.DoesNotContain("data-additional-payment=", after);
        var payment = f.WithDb(db => db.Payments.Single(p => p.ChangeRequestId == id));
        Assert.Equal(PaymentStatus.Submitted, payment.Status); Assert.Equal(PaymentType.AdditionalDeposit, payment.Type);
        Assert.Equal(BookingStatus.Confirmed, f.WithDb(db => db.Bookings.Single(b => b.Id == c.BookingId).Status));
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync($"/Bookings/Receipt?paymentId={payment.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.CreateClient().GetAsync($"/Bookings/Receipt?paymentId={payment.Id}")).StatusCode);
        var accountant = await f.SignInEmployeeAsync("KT001");
        Assert.Equal(HttpStatusCode.OK, (await accountant.GetAsync($"/Bookings/Receipt?paymentId={payment.Id}")).StatusCode);
    }

    [Fact]
    public async Task VoucherBookingShowsExplanationAndNoChangeButton()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f);
        f.WithDb(db => {
            var manager = db.Employees.Single(e => e.EmployeeCode == "QL001");
            var b = db.Bookings.Single(b => b.Id == c.BookingId);
            b.Voucher = new Voucher { Code = "UI-VOUCHER", CustomerId = b.CustomerId, Amount = 100_000, Balance = 50_000,
                IssuedByEmployeeId = manager.Id, Reason = "Demo", ExpiresOn = c.End.AddDays(30) };
            b.DiscountAmount = 50_000; db.SaveChanges(); return true;
        });
        var client = await f.SignInByPhoneAsync("0987000001"); var html = Html(await client.GetStringAsync($"/Bookings/Details/{c.Code}"));
        Assert.Contains("Đơn dùng voucher, vui lòng liên hệ nhân viên", html); Assert.DoesNotContain($"href=\"/Bookings/Change/{c.Code}\"", html);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var quote = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}", Form(token, Input(c)));
        Assert.Contains("Đơn dùng voucher, vui lòng liên hệ nhân viên", Html(await quote.Content.ReadAsStringAsync()));
        Assert.Equal(0, f.WithDb(db => db.BookingChangeRequests.Count(r => r.BookingId == c.BookingId)));
    }

    [Fact]
    public async Task AccountantListIncludesTimelyReceiptAfterDeadlineAndCanConfirm()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f, true); var id = await Awaiting(f, c);
        PastDeadline(f, id, true);
        var client = await f.SignInEmployeeAsync("KT001"); var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Accounting/AdditionalDeposits");
        var html = Html(await client.GetStringAsync("/Accounting/AdditionalDeposits")); Assert.Contains($"CR{id}", html);
        await client.PostAsync($"/Accounting/AdditionalDeposits/Confirm/{id}", Form(token, ("Note", "Xác nhận sau hạn nộp")));
        Assert.Equal(ChangeRequestStatus.Applied, f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id).Status));
        var owner = await f.SignInByPhoneAsync(c.OwnerPhone);
        var details = await owner.GetStringAsync($"/Owner/Bookings/Details/{c.BookingId}");
        Assert.Contains($"/Owner/Bookings/Confirm/{c.BookingId}", details);
        Assert.DoesNotContain("/Staff/ChangeApprovals", details);
    }

    [Theory]
    [InlineData(ChangeRequestStatus.Applied)] [InlineData(ChangeRequestStatus.Rejected)] [InlineData(ChangeRequestStatus.Expired)]
    public async Task CustomerSeesFinalRequestStatusAndReason(ChangeRequestStatus finalStatus)
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f, true);
        var id = await Awaiting(f, c, finalStatus != ChangeRequestStatus.Expired);
        if (finalStatus == ChangeRequestStatus.Expired) PastDeadline(f, id, false);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var accountantId = db.Users.Where(u => u.Person.Employee != null && u.Person.Employee.EmployeeCode == "KT001").Select(u => u.Id).Single();
            var service = scope.ServiceProvider.GetRequiredService<BookingChangeRequestService>();
            if (finalStatus == ChangeRequestStatus.Applied) await service.ConfirmAsync(id, "Đã đối chiếu", Principal(accountantId, AppRoles.Employee));
            else if (finalStatus == ChangeRequestStatus.Rejected) await service.RejectFinanceAsync(id, "Chưa nhận được tiền", Principal(accountantId, AppRoles.Employee));
            else Assert.Equal(1, await service.ExpireUnpaidAsync());
        }
        var client = await f.SignInByPhoneAsync("0987000001"); var html = Html(await client.GetStringAsync($"/Bookings/Details/{c.Code}"));
        Assert.Contains($"data-status=\"{finalStatus}\"", html);
        if (finalStatus == ChangeRequestStatus.Rejected) Assert.Contains("Chưa nhận được tiền", html);
        Assert.DoesNotContain("data-additional-payment=", html);
    }

    [Theory]
    [InlineData("NV001", false, false)] [InlineData("QL001", true, false)]
    [InlineData("KT001", false, true)] [InlineData("AD001", false, false)]
    public async Task SidebarShowsOnlyPermittedChangeActions(string user, bool manager, bool accountant)
    {
        using var f = new TestWebApplicationFactory(); Setup(f); var client = await f.SignInEmployeeAsync(user);
        // Kiểm tra sidebar của màn mới; GroupBy cũ của Dashboard chưa được InMemory hỗ trợ.
        var response = await client.GetAsync("/Staff/ChangeRequests");
        Assert.True(response.IsSuccessStatusCode, string.Join("\n", f.Errors.Select(e => e.ToString())));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("/Staff/ChangeRequests", html);
        Assert.Equal(manager, html.Contains("/Staff/ChangeApprovals")); Assert.Equal(accountant, html.Contains("/Accounting/AdditionalDeposits"));
    }

    [Fact]
    public async Task EnglishFormAndBusinessErrorsAreTranslated()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInByPhoneAsync("0987000001");
        var html = await client.GetStringAsync($"/Bookings/Change/{c.Code}?culture=en-US&ui-culture=en-US");
        Assert.Contains("Change dates / room type", html); Assert.Contains("Review quote", html);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var response = await client.PostAsync($"/Bookings/SubmitChange/{c.Code}?culture=en-US&ui-culture=en-US", Form(token, Input(c, "Cancel")));
        Assert.Contains("Please use the Cancel booking button", await response.Content.ReadAsStringAsync());
        f.Services.GetRequiredService<IOptions<BookingOptions>>().Value.ChangeMinDaysBefore = 11;
        response = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}?culture=en-US&ui-culture=en-US", Form(token, Input(c)));
        Assert.Contains("at least 11 days before check-in", await response.Content.ReadAsStringAsync());
    }


    [Theory]
    [InlineData(ChangeRequestType.ChangeRoomType)] [InlineData(ChangeRequestType.ChangeDatesAndRoomType)]
    public async Task CustomerCanQuoteAndSubmitBothRoomChangeForms(ChangeRequestType requestType)
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f);
        var roomType = f.WithDb(db => {
            var original = db.RoomTypes.Single(t => t.Id == c.RoomTypeId);
            var type = new RoomType { HomestayId = original.HomestayId, CategoryId = original.CategoryId, Name = "Upgrade UI", MaxGuests = 2 };
            type.Rooms.Add(new Room { RoomCode = "UP-UI" });
            foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new RoomTypePrice { DayType = day, Price = 1_500_000, EffectiveFrom = c.Start.AddYears(-1) });
            db.Add(type); db.SaveChanges(); return type.Id;
        });
        var client = await f.SignInByPhoneAsync("0987000001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var fields = new (string, string)[] { ("RequestType", requestType.ToString()), ("NewRoomTypeId", roomType.ToString()),
            ("NewCheckIn", c.Start.ToString("yyyy-MM-dd")), ("NewCheckOut", c.End.ToString("yyyy-MM-dd")) };
        var quote = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}", Form(token, fields));
        var html = await quote.Content.ReadAsStringAsync(); Assert.Contains("data-quote-total=\"3000000\"", html);
        Assert.NotEmpty(Preview(html));
        await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, [..fields, ("PreviewToken", Preview(html))]));
        var request = f.WithDb(db => db.BookingChangeRequests.Single(r => r.BookingId == c.BookingId));
        Assert.Equal(requestType, request.RequestType); Assert.Equal(roomType, request.NewRoomTypeId); Assert.Equal(300_000, request.AdditionalDeposit);
    }

    private static void PastDeadline(TestWebApplicationFactory f, int id, bool payment) => f.WithDb(db => {
        var r = db.BookingChangeRequests.Single(r => r.Id == id); var now = DateTime.UtcNow;
        r.CreatedAt = now.AddMinutes(-40); r.DecidedAt = now.AddMinutes(-30); r.PaymentDeadlineAt = now.AddMinutes(-15);
        if (payment) db.Payments.Single(p => p.ChangeRequestId == id).SubmittedAt = now.AddMinutes(-20);
        db.SaveChanges(); return true;
    });
}
