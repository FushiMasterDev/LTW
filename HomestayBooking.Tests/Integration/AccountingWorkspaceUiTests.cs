using System.Globalization;
using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class AccountingWorkspaceUiTests
{
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values) => new(
        values.Append(("__RequestVerificationToken", token)).Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));

    [Theory]
    [InlineData("Settlements")]
    [InlineData("Compensations")]
    public async Task AccountingReadersCanRead_ConsultantAndCustomerCannot(string screen)
    {
        using var factory = new TestWebApplicationFactory();
        var path = "/Accounting/" + screen;
        foreach (var code in new[] { "KT001", "QL001", "AD001" })
        {
            var client = await factory.SignInEmployeeAsync(code);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        }
        foreach (var client in new[] { await factory.SignInEmployeeAsync("NV001"), await factory.SignInByPhoneAsync("0987000001") })
            Assert.Equal("/Account/AccessDenied", (await client.GetAsync(path)).Headers.Location!.AbsolutePath);
        Assert.Equal("/Account/Login", (await factory.CreateClient().GetAsync(path)).Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData("Settlements/GenerateRecent")]
    [InlineData("Settlements/Generate")]
    [InlineData("Settlements/Send/1")]
    [InlineData("Settlements/RecordPaid/1")]
    [InlineData("Settlements/ConfirmOnBehalf/1")]
    [InlineData("Settlements/Reopen/1")]
    [InlineData("Compensations/RecordPaid/1")]
    [InlineData("Compensations/Reject/1")]
    public async Task MutationsRequireAntiforgery(string action)
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInEmployeeAsync("KT001");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Accounting/" + action, new FormUrlEncodedContent([]))).StatusCode);
    }

    [Fact]
    public async Task GenerateUsesLatestClosedPeriod_AndSingleHomestayGenerationIsSupported()
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInEmployeeAsync("KT001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Accounting/Settlements");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Accounting/Settlements/GenerateRecent", Form(token))).StatusCode);
        Assert.Empty(factory.Errors);
        var s = factory.WithDb(db => db.Settlements.First());
        var response = await client.PostAsync("/Accounting/Settlements/Generate", Form(token,
            ("year", s.Year.ToString()), ("month", s.Month.ToString()), ("periodNo", s.PeriodNo.ToString()), ("homestayId", s.HomestayId.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Accounting/Settlements/Details/{s.Id}", response.Headers.Location!.OriginalString);
        var before = factory.WithDb(db => db.Settlements.Count());
        await client.PostAsync("/Accounting/Settlements/Generate", Form(token, ("year", "2100"), ("month", "12"), ("periodNo", "2")));
        Assert.Equal(before, factory.WithDb(db => db.Settlements.Count()));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Accounting/Settlements?year=9999&month=13&periodNo=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Accounting/Settlements?year=2026")).StatusCode);
    }

    [Fact]
    public async Task SettlementFlowPreservesOwnerConfirmationAndPaidImmutability_AmountsMatchService()
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInEmployeeAsync("KT001");
        var id = factory.WithDb(db => {
            var row = db.Settlements.First(s => s.Homestay.Owner.Person.PhoneNumber == "+84912000001");
            row.Status = SettlementStatus.Sent; row.PaidAt = null; row.PaidBankRef = null;
            db.SaveChanges(); return row.Id;
        });
        var path = $"/Accounting/Settlements/{id}";
        var token = await TestWebApplicationFactory.TokenFromAsync(client, path);
        // An accountant cannot impersonate the owner by posting to the owner route.
        Assert.Equal("/Account/AccessDenied", (await client.PostAsync($"/Owner/Settlements/Confirm/{id}", Form(token))).Headers.Location!.AbsolutePath);
        await client.PostAsync($"/Accounting/Settlements/Reopen/{id}", Form(token, ("reason", "Kiểm tra UI 05c")));
        Assert.Equal(SettlementStatus.Draft, factory.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        // Remove unrelated unfinished seed bookings from this period only in the isolated test DB.
        factory.WithDb(db => {
            var s = db.Settlements.Single(s => s.Id == id); var p = new SettlementPeriod(s.Year, s.Month, s.PeriodNo);
            foreach (var b in db.Bookings.Where(b => b.HomestayId == s.HomestayId && b.CheckOut >= p.Start && b.CheckOut <= p.End
                && b.Status != BookingStatus.Completed && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.NoShow)) b.Status = BookingStatus.Expired;
            return db.SaveChanges();
        });
        await client.PostAsync($"/Accounting/Settlements/Send/{id}", Form(token));
        Assert.Equal(SettlementStatus.Sent, factory.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        var owner = await factory.SignInByPhoneAsync("0912000001");
        var ownerToken = await TestWebApplicationFactory.TokenFromAsync(owner, $"/Owner/Settlements/{id}");
        await owner.PostAsync($"/Owner/Settlements/Confirm/{id}", Form(ownerToken));
        Assert.Equal(SettlementStatus.Confirmed, factory.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        using var scope = factory.Services.CreateScope();
        var actor = factory.WithDb(db => db.Employees.Where(e => e.EmployeeCode == "KT001").Select(e => new Actor(ActorType.Employee, e.Person.Account!.Id, e.Id)).Single());
        var details = await scope.ServiceProvider.GetRequiredService<SettlementService>().DetailsAsync(id, actor);
        var html = await client.GetStringAsync(path);
        foreach (var value in new[] { details.Settlement.TotalRoomAmount, details.Settlement.TotalRetained, details.Settlement.TotalCommission, details.Settlement.PayoutAmount })
            Assert.Contains(value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), WebUtility.HtmlDecode(html));
        Assert.Contains("data-confirm-variant=\"danger\"", html);
        await client.PostAsync($"/Accounting/Settlements/RecordPaid/{id}", Form(token, ("bankRef", "UI05C-PAID"), ("amount", details.Settlement.PayoutAmount.ToString("0.##", CultureInfo.InvariantCulture)), ("paidAt", VietnamTime.ToLocal(DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffffff"))));
        Assert.Equal(SettlementStatus.Paid, factory.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        html = await client.GetStringAsync(path);
        Assert.Contains("UI05C-PAID", html);
        Assert.DoesNotContain("/Accounting/Settlements/Reopen/", html);
        await client.PostAsync($"/Accounting/Settlements/Reopen/{id}", Form(token, ("reason", "Bị chặn")));
        Assert.Equal(SettlementStatus.Paid, factory.WithDb(db => db.Settlements.Single(s => s.Id == id).Status));
        Assert.Empty(factory.Errors);
    }

    [Fact]
    public async Task ManagementCanGenerateButCannotSendReopenOrPay()
    {
        using var factory = new TestWebApplicationFactory();
        var client = await factory.SignInEmployeeAsync("QL001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Accounting/Settlements");
        foreach (var action in new[] { "Settlements/Send/1", "Settlements/Reopen/1", "Settlements/RecordPaid/1", "Compensations/RecordPaid/1", "Compensations/Reject/1" })
            Assert.Equal("/Account/AccessDenied", (await client.PostAsync("/Accounting/" + action, Form(token, ("reason", "Kiểm tra"), ("bankRef", "UI")))).Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Accounting/Settlements/GenerateRecent", Form(token))).StatusCode);
    }

    [Fact]
    public async Task CompensationPaymentUsesService_CompletedRejectIsBlocked()
    {
        using var factory = new TestWebApplicationFactory();
        var payment = factory.WithDb(db => {
            var completed = db.Bookings.First(b => b.Status == BookingStatus.Completed);
            var original = db.Bookings.First(b => b.Id != completed.Id && b.Status == BookingStatus.Cancelled);
            original.ReplacedByBookingId = completed.Id;
            var row = new Payment { BookingId = completed.Id, Type = PaymentType.Compensation, Status = PaymentStatus.Submitted, Amount = 120000, Method = PaymentMethod.ManualTransfer };
            db.Payments.Add(row); db.SaveChanges(); return row.Id;
        });
        var client = await factory.SignInEmployeeAsync("KT001");
        var html = await client.GetStringAsync("/Accounting/Compensations");
        Assert.Contains($"/Accounting/Compensations/RecordPaid/{payment}", html);
        Assert.DoesNotContain("/Accounting/Compensations/Reject/", html);
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Accounting/Compensations");
        await client.PostAsync($"/Accounting/Compensations/Reject/{payment}", Form(token, ("reason", "Không hợp lệ")));
        Assert.Equal(PaymentStatus.Submitted, factory.WithDb(db => db.Payments.Single(p => p.Id == payment).Status));
        await client.PostAsync($"/Accounting/Compensations/RecordPaid/{payment}", Form(token, ("bankRef", "UI05C-COMP")));
        Assert.Equal(PaymentStatus.Approved, factory.WithDb(db => db.Payments.Single(p => p.Id == payment).Status));
        Assert.Empty(factory.Errors);
    }
}
