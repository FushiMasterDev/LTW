using System.Net;
using System.Security.Claims;
using HomestayBooking.Tests.Integration;
using HomestayBooking.Tests.Services;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HomestayBooking.Tests.Ui;

public class NotificationsControllerTests
{
    private static NotificationsController Controller(BookingTestHost h, string account)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account)], "test")) };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new NotificationsController(new NotificationService(h.Db))
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()), Url = new UrlHelper(action),
        };
    }

    private static async Task<(string Mine, Notification Own, Notification Other)> AddAsync(BookingTestHost h)
    {
        var mine = await h.Db.Users.Where(u => u.PersonId == h.Customer.PersonId).Select(u => u.Id).SingleAsync();
        var other = await h.Db.Users.Where(u => u.PersonId != h.Customer.PersonId).Select(u => u.Id).FirstAsync();
        var own = new Notification { UserId = mine, Message = "Thông báo của tôi", Url = "/Bookings/Mine" };
        var foreign = new Notification { UserId = other, Message = "Thông báo riêng của người khác" };
        h.Db.Notifications.AddRange(own, foreign); await h.Db.SaveChangesAsync();
        return (mine, own, foreign);
    }

    [Fact]
    public async Task IndexOnlyListsCurrentAccountsNotifications()
    {
        using var h = new BookingTestHost(); var (mine, own, _) = await AddAsync(h);
        var page = Assert.IsType<PaginatedList<Notification>>(Assert.IsType<ViewResult>(await Controller(h, mine).Index(null)).Model);
        Assert.Equal(own.Id, Assert.Single(page).Id);
    }

    [Theory]
    [InlineData("/Bookings/Mine")] [InlineData("~/Bookings/Mine")]
    public async Task MarkReadUpdatesOwnedNotificationAndOnlyRedirectsLocally(string url)
    {
        using var h = new BookingTestHost(); var (mine, own, other) = await AddAsync(h);
        var result = Assert.IsType<LocalRedirectResult>(await Controller(h, mine).MarkRead(own.Id, url));
        Assert.Equal(url, result.Url);
        Assert.True((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == own.Id)).IsRead);
        Assert.False((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == other.Id)).IsRead);
    }

    [Theory]
    [InlineData("https://evil.example/")] [InlineData("//evil.example/")] [InlineData("/\\evil.example/")]
    public async Task MarkReadNeverRedirectsOutsideTheSite(string url)
    {
        using var h = new BookingTestHost(); var (mine, own, _) = await AddAsync(h);
        var result = Assert.IsType<RedirectToActionResult>(await Controller(h, mine).MarkRead(own.Id, url));
        Assert.Equal("Index", result.ActionName);
        Assert.True((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == own.Id)).IsRead);
    }

    [Fact]
    public async Task ForeignNotificationCannotBeMarkedOrUsedForRedirect()
    {
        using var h = new BookingTestHost(); var (mine, _, other) = await AddAsync(h);
        var controller = Controller(h, mine);
        Assert.IsType<RedirectToActionResult>(await controller.MarkRead(other.Id, "/Bookings/Mine"));
        Assert.False((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == other.Id)).IsRead);
        Assert.Contains("Không tìm thấy", controller.TempData["Error"]?.ToString());
    }

    [Fact]
    public async Task MarkAllReadDoesNotChangeAnotherAccountsRows()
    {
        using var h = new BookingTestHost(); var (mine, own, other) = await AddAsync(h);
        Assert.IsType<RedirectToActionResult>(await Controller(h, mine).MarkAllRead());
        Assert.True((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == own.Id)).IsRead);
        Assert.False((await h.Db.Notifications.AsNoTracking().SingleAsync(n => n.Id == other.Id)).IsRead);
    }

    [Theory]
    [InlineData(EmployeePosition.Accountant, true)] [InlineData(EmployeePosition.Manager, false)] [InlineData(EmployeePosition.Administrator, false)] [InlineData(EmployeePosition.Consultant, false)]
    public void AccountingTabsKeepAdditionalDepositPermissionAndActiveSelection(EmployeePosition position, bool allowed)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, AppRoles.Employee), new Claim(AppClaimTypes.EmployeePosition, position.ToString()),
        ], "test"));
        var tabs = TabsModel.Accounting(user, new AccountingCounts(1, 2, 3, 100, 4), "Receipts");
        Assert.Equal(allowed, tabs.Items.Single(t => t.Href.EndsWith("AdditionalDeposits")).Visible);
        Assert.Equal("/Accounting/Receipts", Assert.Single(tabs.Items, t => t.Active).Href);
    }

    [Fact]
    public async Task HttpPostsRequireAntiforgeryAndListRequiresAuthentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/Notifications")).StatusCode);
        using var customer = await factory.SignInByPhoneAsync("0987000001");
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/Notifications")).StatusCode);
        foreach (var path in new[] { "/Notifications/MarkRead/1", "/Notifications/MarkAllRead" })
            Assert.Equal(HttpStatusCode.BadRequest, (await customer.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        Assert.Empty(factory.Errors);
    }
}
