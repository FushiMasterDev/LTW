using System.Globalization;
using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomestayBooking.Tests.Integration;

public sealed class AdminWorkspaceUiTests
{
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values) => new(
        values.Append(("__RequestVerificationToken", token)).Select(v => new KeyValuePair<string, string>(v.Item1, v.Item2)));

    [Theory]
    [InlineData("Contracts")]
    [InlineData("Homestays")]
    [InlineData("Reports")]
    [InlineData("Accounts")]
    [InlineData("Dashboard")]
    public async Task ManagementScreensAllowManagersAndAdministratorsOnly(string screen)
    {
        using var f = new TestWebApplicationFactory();
        foreach (var code in new[] { "QL001", "AD001" })
        {
            var client = await f.SignInEmployeeAsync(code);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/" + screen)).StatusCode);
        }
        foreach (var client in new[] { await f.SignInEmployeeAsync("NV001"), await f.SignInEmployeeAsync("KT001"), await f.SignInByPhoneAsync("0912000001"), await f.SignInByPhoneAsync("0987000001") })
            Assert.Equal("/Account/AccessDenied", (await client.GetAsync("/Admin/" + screen)).Headers.Location!.AbsolutePath);
        Assert.Equal("/Account/Login", (await f.CreateClient().GetAsync("/Admin/" + screen)).Headers.Location!.AbsolutePath);
        Assert.Empty(f.Errors);
    }

    [Theory]
    [InlineData("Contracts/Create")]
    [InlineData("Contracts/Extend/1")]
    [InlineData("Contracts/Terminate/1")]
    [InlineData("Homestays/Approve/1")]
    [InlineData("Homestays/Reject/1")]
    [InlineData("Homestays/Resume/1")]
    [InlineData("Accounts/Create")]
    [InlineData("Accounts/Employment/1")]
    [InlineData("Accounts/Lock/1")]
    [InlineData("Accounts/ResetPassword/1")]
    public async Task EveryMutationRequiresAntiforgery(string action)
    {
        using var f = new TestWebApplicationFactory();
        var client = await f.SignInEmployeeAsync("AD001");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/" + action, new FormUrlEncodedContent([]))).StatusCode);
    }

    [Fact]
    public async Task ContractsCreateExtendAndTerminationUseServiceAndNotificationAlias()
    {
        using var f = new TestWebApplicationFactory();
        var client = await f.SignInEmployeeAsync("QL001");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var homeId = f.WithDb(db => {
            var c = db.Contracts.First(c => c.Status == ContractStatus.Active);
            c.Status = ContractStatus.Terminated; db.SaveChanges(); return c.HomestayId;
        });
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Admin/Contracts/Create");
        var response = await client.PostAsync("/Admin/Contracts/Create", Form(token, ("HomestayId", homeId.ToString()),
            ("ContractNo", "UI05D-TEST"), ("StartDate", today.ToString("yyyy-MM-dd")),
            ("EndDate", today.AddYears(1).ToString("yyyy-MM-dd")), ("CommissionPercent", "12")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var id = f.WithDb(db => db.Contracts.Single(c => c.ContractNo == "UI05D-TEST").Id);
        var path = $"/Admin/Contracts/{id}";
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        await client.PostAsync($"/Admin/Contracts/Extend/{id}", Form(token, ("endDate", today.AddYears(2).ToString("yyyy-MM-dd"))));
        Assert.Equal(today.AddYears(2), f.WithDb(db => db.Contracts.Single(c => c.Id == id).EndDate));
        Assert.Equal(.12m, f.WithDb(db => db.Contracts.Single(c => c.Id == id).CommissionRate));
        await client.PostAsync($"/Admin/Contracts/Extend/{id}", Form(token, ("endDate", today.ToString("yyyy-MM-dd"))));
        Assert.Equal(today.AddYears(2), f.WithDb(db => db.Contracts.Single(c => c.Id == id).EndDate));
        // Held seed bookings block termination; no controller bypass.
        f.WithDb(db => { db.Bookings.First(b => b.HomestayId == homeId).Status = BookingStatus.Confirmed; return db.SaveChanges(); });
        await client.PostAsync($"/Admin/Contracts/Terminate/{id}", Form(token, ("reason", "UI test")));
        Assert.Equal(ContractStatus.Active, f.WithDb(db => db.Contracts.Single(c => c.Id == id).Status));
        f.WithDb(db => { foreach (var booking in db.Bookings.Where(b => b.HomestayId == homeId)) booking.Status = BookingStatus.Completed; return db.SaveChanges(); });
        await client.PostAsync($"/Admin/Contracts/Terminate/{id}", Form(token, ("reason", "UI test")));
        Assert.Equal(ContractStatus.Terminated, f.WithDb(db => db.Contracts.Single(c => c.Id == id).Status));
        f.WithDb(db => { var c=db.Contracts.Single(c => c.Id == id); c.Status=ContractStatus.Active; c.EndDate=DateOnly.MaxValue; return db.SaveChanges(); });
        var boundary = await client.GetStringAsync(path);
        Assert.DoesNotContain("/Admin/Contracts/Extend/", boundary);
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task PendingContentCanBeRejectedApprovedAndResumed_ConsultantCannotAct()
    {
        using var f = new TestWebApplicationFactory();
        var id = f.WithDb(db => { var h = db.Homestays.First(h => h.Status == HomestayStatus.Active); db.HomestayImages.Add(new HomestayImage { HomestayId=h.Id, Url="/images/demo/courtyard.webp", IsCover=true }); h.Status = HomestayStatus.PendingReview; db.SaveChanges(); return h.Id; });
        var manager = await f.SignInEmployeeAsync("QL001");
        var token = await TestWebApplicationFactory.TokenFromAsync(manager, $"/Admin/Homestays/{id}");
        var consultant = await f.SignInEmployeeAsync("NV001");
        var nvToken = await TestWebApplicationFactory.TokenFromAsync(consultant, "/Staff/Bookings");
        Assert.Equal("/Account/AccessDenied", (await consultant.PostAsync($"/Admin/Homestays/Approve/{id}", Form(nvToken))).Headers.Location!.AbsolutePath);
        await manager.PostAsync($"/Admin/Homestays/Reject/{id}", Form(token, ("reason", "Cần cập nhật giấy tờ")));
        Assert.Equal(HomestayStatus.Draft, f.WithDb(db => db.Homestays.Single(h => h.Id == id).Status));
        f.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.PendingReview; return db.SaveChanges(); });
        await manager.PostAsync($"/Admin/Homestays/Approve/{id}", Form(token));
        Assert.Equal(HomestayStatus.Active, f.WithDb(db => db.Homestays.Single(h => h.Id == id).Status));
        f.WithDb(db => { db.Homestays.Single(h => h.Id == id).Status = HomestayStatus.Suspended; return db.SaveChanges(); });
        Assert.Contains("/Admin/Homestays/Resume/", await manager.GetStringAsync($"/Admin/Homestays/{id}"));
        await manager.PostAsync($"/Admin/Homestays/Resume/{id}", Form(token, ("reason", "Đã kiểm tra")));
        Assert.Equal(HomestayStatus.Active, f.WithDb(db => db.Homestays.Single(h => h.Id == id).Status));
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task AccountsManagerReadsButCannotWrite_AdministratorCreatesLocksResetsAndChangesEmployment()
    {
        using var f = new TestWebApplicationFactory();
        var manager = await f.SignInEmployeeAsync("QL001");
        var qlToken = await TestWebApplicationFactory.TokenFromAsync(manager, "/Admin/Accounts");
        Assert.Equal("/Account/AccessDenied", (await manager.GetAsync("/Admin/Accounts/Create")).Headers.Location!.AbsolutePath);
        foreach (var action in new[] { "Create", "Employment/1", "Lock/1", "ResetPassword/1" })
            Assert.Equal("/Account/AccessDenied", (await manager.PostAsync("/Admin/Accounts/" + action, Form(qlToken))).Headers.Location!.AbsolutePath);
        var admin = await f.SignInEmployeeAsync("AD001");
        var token = await TestWebApplicationFactory.TokenFromAsync(admin, "/Admin/Accounts/Create");
        var response = await admin.PostAsync("/Admin/Accounts/Create", Form(token, ("EmployeeCode", "UI05DTEST"), ("FullName", "Nguyễn Hà Kiểm Thử"),
            ("Phone", "0905050505"), ("Email", "ui05d@example.test"), ("Position", "Consultant"), ("Department", "Phòng thử giao diện")));
        Assert.Equal("/Admin/Accounts/TemporaryPassword", response.Headers.Location!.OriginalString);
        response = await admin.GetAsync(response.Headers.Location);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        var secretPage = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-copy-employee-password", secretPage);
        Assert.Equal(HttpStatusCode.Redirect, (await admin.GetAsync("/Admin/Accounts/TemporaryPassword")).StatusCode);
        var id = f.WithDb(db => db.Users.Single(u => u.Person.Employee!.EmployeeCode == "UI05DTEST").Id);
        Assert.Contains("UI05DTEST", await admin.GetStringAsync("/Admin/Accounts?position=Consultant&status=Active"));
        Assert.DoesNotContain("UI05DTEST", await admin.GetStringAsync("/Admin/Accounts?position=Accountant"));
        Assert.Equal("Phòng thử giao diện", f.WithDb(db => db.Employees.Single(e => e.EmployeeCode == "UI05DTEST").Department));
        await admin.PostAsync($"/Admin/Accounts/Lock/{id}", Form(token, ("locked", "true"), ("reason", "Kiểm thử khóa")));
        Assert.Equal(AccountStatus.Locked, f.WithDb(db => db.Users.Single(u => u.Id == id).Status));
        await admin.PostAsync($"/Admin/Accounts/Lock/{id}", Form(token, ("locked", "false"), ("reason", "Kiểm thử mở")));
        Assert.Equal(AccountStatus.Active, f.WithDb(db => db.Users.Single(u => u.Id == id).Status));
        var hash = f.WithDb(db => db.Users.Single(u => u.Id == id).PasswordHash);
        await admin.PostAsync($"/Admin/Accounts/ResetPassword/{id}", Form(token));
        Assert.NotEqual(hash, f.WithDb(db => db.Users.Single(u => u.Id == id).PasswordHash));
        await admin.GetAsync("/Admin/Accounts/TemporaryPassword");
        Assert.Equal(HttpStatusCode.Redirect, (await admin.GetAsync("/Admin/Accounts/TemporaryPassword")).StatusCode);
        await admin.PostAsync($"/Admin/Accounts/Employment/{id}", Form(token, ("position", "Accountant"), ("employment", "Resigned")));
        Assert.Equal(AccountStatus.Disabled, f.WithDb(db => db.Users.Single(u => u.Id == id).Status));
        await admin.PostAsync($"/Admin/Accounts/Lock/{id}", Form(token, ("locked", "false"), ("reason", "Không được mở người đã nghỉ")));
        Assert.Equal(AccountStatus.Disabled, f.WithDb(db => db.Users.Single(u => u.Id == id).Status));
        Assert.Empty(f.Errors);
    }

    [Theory]
    [InlineData(2026)]
    [InlineData(2028)]
    public async Task FiveReportTabsPreserveRangeAndDisplayServiceConversionAndFinancialTotals(int year)
    {
        using var f = new TestWebApplicationFactory();
        var client = await f.SignInEmployeeAsync("QL001");
        var q = new ReportQuery(new(year, 1, 1), new(year, 12, 31));
        using var scope = f.Services.CreateScope();
        var actor = f.WithDb(db => db.Employees.Where(e => e.EmployeeCode == "QL001").Select(e => new Actor(ActorType.Employee, e.Person.Account!.Id, e.Id)).Single());
        var service = scope.ServiceProvider.GetRequiredService<ReportService>();
        foreach (var tab in new[] { "overview", "bookings", "finance", "employees", "homestays" })
        {
            var html = await client.GetStringAsync($"/Admin/Reports?tab={tab}&from={year}-01-01&to={year}-12-31");
            Assert.Contains($"from={year}-01-01", html);
            Assert.Contains($"to={year}-12-31", html);
            if (year == 2028) {
                Assert.DoesNotContain("data-report-chart", html); Assert.Contains("fx-empty", html); continue;
            }
            Assert.Contains("data-report-chart", html);
            Assert.Contains("aria-describedby=", html);
            Assert.Contains("<caption", html);
            if (tab == "employees") foreach (var e in await service.EmployeesAsync(q, actor))
                Assert.Contains(e.ConversionPercent.ToString("N1", CultureInfo.GetCultureInfo("vi-VN")) + "%", WebUtility.HtmlDecode(html));
            if (tab == "finance")
            {
                var finance = await service.FinancialAsync(q, actor);
                var cells = System.Text.RegularExpressions.Regex.Matches(WebUtility.HtmlDecode(html), "<span[^>]*class=\"[^\"]*fx-money[^\"]*\"[^>]*>([^<]+)</span>")
                    .Select(m => m.Groups[1].Value).ToArray();
                foreach (var amount in new[] {finance.CompletedRoomAmount, finance.Retained, finance.Commission, finance.FujiNet, finance.CompensationPaid})
                    Assert.Contains(amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ", cells);
            }
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/Reports?from=2028-12-31&to=2028-01-01")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/Reports?from=not-a-date")).StatusCode);
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task CatalogInUseCannotBeDeletedAndShowsClearError()
    {
        using var f = new TestWebApplicationFactory(); var admin = await f.SignInEmployeeAsync("AD001");
        var id = f.WithDb(db => db.HomestayAmenities.First().AmenityId);
        var token = await TestWebApplicationFactory.TokenFromAsync(admin, "/Admin/Accounts");
        var count = f.WithDb(db => db.HomestayAmenities.Count(a => a.AmenityId == id));
        await admin.PostAsync($"/Admin/Amenities/Delete/{id}", Form(token));
        Assert.Equal(count, f.WithDb(db => db.HomestayAmenities.Count(a => a.AmenityId == id)));
        var html = WebUtility.HtmlDecode(await admin.GetStringAsync("/Admin/Accounts"));
        Assert.Contains("đang được", html);
        Assert.Empty(f.Errors);
    }
}
