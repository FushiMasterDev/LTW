using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed partial class ChangeRequestUiTests
{
    private record Case(int BookingId, string Code, string CustomerId, string OwnerPhone, int OtherBookingId, int RoomTypeId, DateOnly Start, DateOnly End);

    private static Case Setup(TestWebApplicationFactory factory, bool additional = false) => factory.WithDb<Case>(db =>
    {
        var today = VietnamTime.Today(TimeProvider.System); var now = DateTime.UtcNow;
        var homestay = db.Homestays.Include(h => h.Owner).ThenInclude(o => o.Person)
            .Where(h => h.Status == HomestayStatus.Active && h.Owner.Registrations.Any(r => r.Status == OwnerRegistrationStatus.Approved)).First();
        var customer = db.Customers.Include(c => c.Person).ThenInclude(p => p.Account).Single(c => c.Person.PhoneNumber == "+84987000001");
        var other = db.Customers.First(c => c.Id != customer.Id);
        var type = new RoomType { HomestayId = homestay.Id, CategoryId = db.RoomCategories.First().Id, Name = "Integration room", MaxGuests = 2 };
        type.Rooms.Add(new Room { RoomCode = "IT-01" }); type.Rooms.Add(new Room { RoomCode = "IT-02" });
        foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new RoomTypePrice { DayType = day, Price = 1_000_000, EffectiveFrom = today.AddYears(-1) });
        db.RoomTypes.Add(type); db.SaveChanges();
        Booking Make(int customerId, int number) => new() { Code = $"FJ{today:yyMMdd}-{900 + number}", HomestayId = homestay.Id,
            RoomTypeId = type.Id, RoomId = type.Rooms.ElementAt(number - 1).Id, CustomerId = customerId,
            CheckIn = today.AddDays(additional ? 4 : 10), CheckOut = today.AddDays(additional ? 6 : 12), Guests = 2,
            ContactName = "UI test customer", ContactPhone = "+84987000001", TotalAmount = 2_000_000, DepositAmount = 600_000,
            DepositRate = .30m, CommissionRate = .12m, Status = BookingStatus.Confirmed, DepositedAt = now.AddMinutes(-15),
            ConfirmedAt = now.AddMinutes(-10), HoldExpiresAt = now.AddMinutes(-20),
            CancellationPolicyId = db.CancellationPolicies.First(p => p.HomestayId == null && p.Scope == PolicyScope.All).Id };
        var booking = Make(customer.Id, 1); var othersBooking = Make(other.Id, 2);
        db.AddRange(booking, othersBooking); db.SaveChanges();
        foreach (var b in new[] { booking, othersBooking })
            for (var d = b.CheckIn; d < b.CheckOut; d = d.AddDays(1))
            { db.RoomNights.Add(new RoomNight { RoomId = b.RoomId, Date = d, BookingId = b.Id }); db.BookingNights.Add(new BookingNight { BookingId = b.Id, Date = d, Price = 1_000_000, DayType = DayType.Weekday }); }
        db.SaveChanges();
        return new(booking.Id, booking.Code, customer.Person.Account!.Id, PhoneNumberNormalizer.ToNational(homestay.Owner.Person.PhoneNumber), othersBooking.Id,
            type.Id, booking.CheckIn.AddDays(7), booking.CheckOut.AddDays(7));
    });

    private static string Html(string value) => WebUtility.HtmlDecode(value);
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values) => new(
        values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value)).Append(new("__RequestVerificationToken", token)));
    private static string Preview(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"PreviewToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static (string, string)[] Input(Case c, string type = "ChangeDates") => [
        ("RequestType", type), ("NewCheckIn", c.Start.ToString("yyyy-MM-dd")), ("NewCheckOut", c.End.ToString("yyyy-MM-dd"))];
    private static void Denied(HttpResponseMessage response) { Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString); }

    private static async Task<int> SubmitCustomer(TestWebApplicationFactory factory, Case c, HttpClient client)
    {
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var quote = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}", Form(token, Input(c)));
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode); var html = await quote.Content.ReadAsStringAsync();
        Assert.Contains("data-quote-total=\"2000000\"", html); Assert.NotEmpty(Preview(html));
        var submitted = await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, [..Input(c), ("PreviewToken", Preview(html))]));
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        return factory.WithDb(db => db.BookingChangeRequests.Single(r => r.BookingId == c.BookingId).Id);
    }

    private static ClaimsPrincipal Principal(string accountId, string role) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, accountId), new Claim(ClaimTypes.Role, role)], "test"));

    private static async Task<int> Awaiting(TestWebApplicationFactory factory, Case c, bool submitted = true)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<BookingChangeRequestService>();
        var r = await service.SubmitAsync(new(c.BookingId, ChangeRequestType.ChangeDates, c.Start, c.End), Principal(c.CustomerId, AppRoles.Customer));
        var manager = db.Employees.Include(e => e.Person).ThenInclude(p => p.Account).Single(e => e.EmployeeCode == "QL001");
        await service.ApproveAsync(r.Id, "Manager approval", Principal(manager.Person.Account!.Id, AppRoles.Employee));
        Assert.Equal(ChangeRequestStatus.AwaitingPayment, r.Status);
        if (submitted) await scope.ServiceProvider.GetRequiredService<ReceiptReviewService>().SubmitChangeRequestAsync(r.Id, "test-only.jpg", null, Principal(c.CustomerId, AppRoles.Customer));
        return r.Id;
    }

    [Fact]
    public async Task CustomerQuotesSubmitsAndDuplicatePostDoesNotCreateAnotherRequest()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInByPhoneAsync("0987000001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var quoted = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}", Form(token, Input(c)));
        var html = await quoted.Content.ReadAsStringAsync(); Assert.NotEmpty(Preview(html));
        Assert.Contains("data-quote-total=\"2000000\"", html);
        for (var i = 0; i < 2; i++)
            await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, [.. Input(c), ("PreviewToken", Preview(html)), ("RequestedByAccountId", "forged")]));
        f.WithDb(db => { var r = db.BookingChangeRequests.Single(r => r.BookingId == c.BookingId); Assert.Equal(c.CustomerId, r.RequestedByAccountId); Assert.Equal(ChangeRequestStatus.Pending, r.Status); return true; });
        var details = Html(await client.GetStringAsync($"/Bookings/Details/{c.Code}")); Assert.Contains("Chờ quản lý duyệt", details);
        Assert.DoesNotContain($"href=\"/Bookings/Change/{c.Code}\"", details);
    }

    [Fact]
    public async Task CustomerCannotSubmitCancelButExistingImmediateCancellationStillWorks()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInByPhoneAsync("0987000001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var response = await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, Input(c, "Cancel")));
        Assert.Contains("Vui lòng dùng nút hủy đơn", Html(await response.Content.ReadAsStringAsync()));
        Assert.Equal(0, f.WithDb(db => db.BookingChangeRequests.Count(r => r.BookingId == c.BookingId)));
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Bookings/Cancel/{c.Code}", Form(token, ("reason", "Khách tự hủy")))).StatusCode);
        Assert.Equal(BookingStatus.Cancelled, f.WithDb(db => db.Bookings.Single(b => b.Id == c.BookingId).Status));
    }

    [Fact]
    public async Task CustomerCannotAccessOrChangeAnotherCustomersBooking()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInByPhoneAsync("0987000001");
        var otherCode = f.WithDb(db => db.Bookings.Single(b => b.Id == c.OtherBookingId).Code);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Bookings/Change/{otherCode}")).StatusCode);
        foreach (var action in new[] { "QuoteChange", "SubmitChange", "UploadChangeReceipt" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/Bookings/{action}/{otherCode}", Form(token, Input(c)))).StatusCode);
        Denied(await client.GetAsync("/Staff/ChangeApprovals")); Denied(await client.GetAsync("/Accounting/AdditionalDeposits"));
    }

    [Fact]
    public async Task AnonymousLookupCanReadRequestStatusWithoutChangeActions()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f);
        using (var scope = f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<BookingChangeRequestService>().SubmitAsync(new(c.BookingId, ChangeRequestType.ChangeDates, c.Start, c.End), Principal(c.CustomerId, AppRoles.Customer));
        var client = f.CreateClient(); var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Bookings/Lookup");
        var response = await client.PostAsync("/Bookings/Lookup", Form(token, ("Code", c.Code), ("PhoneNumber", "0987000001")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var html = Html(await client.GetStringAsync($"/Bookings/Details/{c.Code}")); Assert.Contains("Chờ quản lý duyệt", html);
        Assert.DoesNotContain("Xác nhận gửi yêu cầu", html); Assert.DoesNotContain($"href=\"/Bookings/Change/{c.Code}\"", html);
        var post = await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, Input(c)));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode); Assert.Contains("/Account/Login", post.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ConsultantCanSearchAndSendCancellationButCannotApprove()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInEmployeeAsync("NV001");
        var search = await client.GetStringAsync($"/Staff/ChangeRequests?q={c.Code}"); Assert.Contains(c.Code, search);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Staff/ChangeRequests/Create/{c.BookingId}");
        var quote = await client.PostAsync($"/Staff/ChangeRequests/Quote/{c.BookingId}", Form(token, ("RequestType", "Cancel")));
        var preview = Preview(await quote.Content.ReadAsStringAsync()); Assert.NotEmpty(preview);
        for (var i = 0; i < 2; i++)
            await client.PostAsync($"/Staff/ChangeRequests/Submit/{c.BookingId}", Form(token, ("RequestType", "Cancel"), ("PreviewToken", preview)));
        var r = f.WithDb(db => db.BookingChangeRequests.Single(r => r.BookingId == c.BookingId)); Assert.Equal(ChangeRequestType.Cancel, r.RequestType); Assert.Equal(ActorType.Employee, r.RequestedByType);
        Denied(await client.PostAsync($"/Staff/ChangeApprovals/Approve/{r.Id}", Form(token, ("Note", "Không được duyệt"))));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManagerCanApproveOrRejectWithRequiredNoteAndDuplicateIsHarmless(bool reject)
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var customer = await f.SignInByPhoneAsync("0987000001");
        var id = await SubmitCustomer(f, c, customer); var manager = await f.SignInEmployeeAsync("QL001");
        var token = await TestWebApplicationFactory.TokenFromAsync(manager, "/Staff/ChangeApprovals");
        var action = reject ? "Reject" : "Approve";
        await manager.PostAsync($"/Staff/ChangeApprovals/{action}/{id}", Form(token, ("Note", " ")));
        Assert.Equal(ChangeRequestStatus.Pending, f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id).Status));
        for (var i = 0; i < 2; i++) await manager.PostAsync($"/Staff/ChangeApprovals/{action}/{id}", Form(token, ("Note", "Manager decision")));
        Assert.Equal(reject ? ChangeRequestStatus.Rejected : ChangeRequestStatus.Applied, f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id).Status));
        Assert.Equal(1, f.WithDb(db => db.AuditLogs.Count(a => a.TableName == "BookingChangeRequests" && a.RecordId == id.ToString() && a.Action == AuditAction.Update)));
    }

    [Theory]
    [InlineData("KT001")] [InlineData("AD001")] [InlineData("NV001")]
    public async Task OtherEmployeesCannotEnterManagerApprovalOrPost(string employee)
    {
        using var f = new TestWebApplicationFactory(); Setup(f); var client = await f.SignInEmployeeAsync(employee);
        Denied(await client.GetAsync("/Staff/ChangeApprovals"));
        Denied(await client.PostAsync("/Staff/ChangeApprovals/Approve/1", new FormUrlEncodedContent([])));
    }

    [Fact]
    public async Task OwnerSeesHistoryAndHasNoApprovalButtonsOrPermission()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var customer = await f.SignInByPhoneAsync("0987000001");
        await SubmitCustomer(f, c, customer); var owner = await f.SignInByPhoneAsync(c.OwnerPhone);
        var html = Html(await owner.GetStringAsync($"/Owner/Bookings/Details/{c.BookingId}"));
        Assert.Contains("Yêu cầu đổi", html); Assert.Contains("Lịch cũ", html); Assert.Contains("Lịch mới", html);
        Assert.DoesNotContain("/Staff/ChangeApprovals", html); Assert.DoesNotContain("/Accounting/AdditionalDeposits", html);
        Denied(await owner.PostAsync("/Staff/ChangeApprovals/Approve/1", new FormUrlEncodedContent([])));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AccountantCanConfirmOrRejectButNotOverwriteManagerNote(bool reject)
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f, true); var id = await Awaiting(f, c);
        var client = await f.SignInEmployeeAsync("KT001"); var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Accounting/AdditionalDeposits");
        var list = Html(await client.GetStringAsync("/Accounting/AdditionalDeposits")); Assert.Contains(c.Code, list); Assert.Contains($"CR{id}", list); Assert.Contains("Xem ảnh đã gửi", list);
        var action = reject ? "Reject" : "Confirm";
        await client.PostAsync($"/Accounting/AdditionalDeposits/{action}/{id}", Form(token, ("Note", "")));
        Assert.Equal(ChangeRequestStatus.AwaitingPayment, f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id).Status));
        for (var i = 0; i < 2; i++) await client.PostAsync($"/Accounting/AdditionalDeposits/{action}/{id}", Form(token, ("Note", "Accountant decision")));
        var r = f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id));
        Assert.Equal(reject ? ChangeRequestStatus.Rejected : ChangeRequestStatus.Applied, r.Status); Assert.Equal("Manager approval", r.DecisionNote);
        Assert.Equal(reject ? PaymentStatus.Rejected : PaymentStatus.Approved, f.WithDb(db => db.Payments.Single(p => p.ChangeRequestId == id).Status));
    }

    [Theory]
    [InlineData("QL001")] [InlineData("AD001")] [InlineData("NV001")]
    public async Task OtherEmployeesCannotConfirmAdditionalDeposit(string employee)
    {
        using var f = new TestWebApplicationFactory(); Setup(f); var client = await f.SignInEmployeeAsync(employee);
        Denied(await client.GetAsync("/Accounting/AdditionalDeposits"));
        Denied(await client.PostAsync("/Accounting/AdditionalDeposits/Confirm/1", new FormUrlEncodedContent([])));
    }

    [Theory]
    [InlineData("customer-quote")] [InlineData("customer-submit")] [InlineData("receipt")]
    [InlineData("staff-quote")] [InlineData("staff-submit")] [InlineData("approve")] [InlineData("reject")]
    [InlineData("finance-confirm")] [InlineData("finance-reject")]
    public async Task AllNewPostActionsRequireAntiforgery(string action)
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f);
        var employee = action.StartsWith("finance") ? "KT001" : action is "approve" or "reject" ? "QL001" : "NV001";
        var client = action.StartsWith("customer") || action == "receipt" ? await f.SignInByPhoneAsync("0987000001") : await f.SignInEmployeeAsync(employee);
        var path = action switch { "customer-quote" => $"/Bookings/QuoteChange/{c.Code}", "customer-submit" => $"/Bookings/SubmitChange/{c.Code}",
            "receipt" => $"/Bookings/UploadChangeReceipt/{c.Code}", "staff-quote" => $"/Staff/ChangeRequests/Quote/{c.BookingId}",
            "staff-submit" => $"/Staff/ChangeRequests/Submit/{c.BookingId}", "approve" => "/Staff/ChangeApprovals/Approve/1", "reject" => "/Staff/ChangeApprovals/Reject/1",
            "finance-confirm" => "/Accounting/AdditionalDeposits/Confirm/1", _ => "/Accounting/AdditionalDeposits/Reject/1" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent([]))).StatusCode);
    }

    [Fact]
    public async Task LegacyRequestHasOnlyRejectionAndManagerCanRejectIt()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f);
        var id = f.WithDb(db => { var r = new BookingChangeRequest { BookingId = c.BookingId, RequestType = ChangeRequestType.ChangeDates, NewCheckIn = c.Start, NewCheckOut = c.End }; db.Add(r); db.SaveChanges(); return r.Id; });
        var client = await f.SignInEmployeeAsync("QL001"); var html = Html(await client.GetStringAsync("/Staff/ChangeApprovals"));
        var section = Regex.Match(html, $"data-approval=\"{id}\"[\\s\\S]*?</section>").Value;
        Assert.Contains("Yêu cầu cũ, cần gửi lại", section); Assert.DoesNotContain($"/Staff/ChangeApprovals/Approve/{id}", section);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Staff/ChangeApprovals");
        await client.PostAsync($"/Staff/ChangeApprovals/Reject/{id}", Form(token, ("Note", "Vui lòng gửi lại yêu cầu mới")));
        Assert.Equal(ChangeRequestStatus.Rejected, f.WithDb(db => db.BookingChangeRequests.Single(r => r.Id == id).Status));
    }

    [Fact]
    public async Task PreviewCannotBeTamperedWithOrReusedForAnotherInput()
    {
        using var f = new TestWebApplicationFactory(); var c = Setup(f); var client = await f.SignInByPhoneAsync("0987000001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, $"/Bookings/Change/{c.Code}");
        var quote = await client.PostAsync($"/Bookings/QuoteChange/{c.Code}", Form(token, Input(c)));
        var preview = Preview(await quote.Content.ReadAsStringAsync());
        var tampered = c with { Start = c.Start.AddDays(1), End = c.End.AddDays(1) };
        await client.PostAsync($"/Bookings/SubmitChange/{c.Code}", Form(token, [..Input(tampered), ("PreviewToken", preview)]));
        Assert.Equal(0, f.WithDb(db => db.BookingChangeRequests.Count(r => r.BookingId == c.BookingId)));
    }
}
