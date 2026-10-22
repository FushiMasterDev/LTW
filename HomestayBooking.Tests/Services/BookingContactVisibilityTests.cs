using System.Security.Claims;
using HomestayBooking.Web;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class BookingContactVisibilityTests
{
    public static IEnumerable<object[]> AllStatuses() => Enum.GetValues<BookingStatus>().Select(s => new object[] { s, s is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed });

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void EveryStatusUsesOneBr13RuleForContactAndReminder(BookingStatus status, bool visible)
    {
        Assert.Equal(visible, BookingContactVisibility.CanView(status));
        var b = new Booking { Status = status, Homestay = new() { Address = "Riêng tư", ContactPhone = "+84912345678", Latitude = 21, Longitude = 105, CheckinGuideHtml = "Hướng dẫn" } };
        if (visible) { Assert.DoesNotContain("Riêng tư", ReminderService.CustomerText(b, "https://fuji.vn")); Assert.Contains("+84912345678", ReminderService.CustomerText(b, "https://fuji.vn")); }
        else Assert.Throws<BookingException>(() => ReminderService.CustomerText(b, "https://fuji.vn"));
        BookingContactVisibility.RedactIfHidden(b);
        if (visible) Assert.Equal("Riêng tư", b.Homestay.Address);
        else { Assert.Empty(b.Homestay.Address); Assert.Null(b.Homestay.ContactPhone); Assert.Null(b.Homestay.Latitude); Assert.Null(b.Homestay.Longitude); Assert.Null(b.Homestay.CheckinGuideHtml); }
    }

    [Theory]
    [InlineData(BookingStatus.Deposited, false)] [InlineData(BookingStatus.Confirmed, true)]
    public async Task CustomerDetailsModelCannotExposeHiddenContactsAndRedactionIsNeverSaved(BookingStatus status, bool visible)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(5, status);
        var home = await h.Db.Homestays.SingleAsync(); home.ContactPhone = "+84912345678"; home.Latitude = 21; home.Longitude = 105; home.CheckinGuideHtml = "Bí mật"; await h.Db.SaveChangesAsync();
        var address = home.Address;
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e => e.EnvironmentName).Returns("Testing"); env.SetupGet(e => e.ContentRootPath).Returns(h.P.DirectoryPath);
        var files = new ReceiptFileStore(env.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, h.Customer.AccountId!),
            new Claim(ClaimTypes.Role, "Customer"), new Claim(AppClaimTypes.PersonId, h.H.Customer.PersonId.ToString())], "test"));
        var controller = new BookingsController(h.Db, h.H.Bookings(), h.H.Pricing(), h.H.Refunds(), h.H.Receipts(), files, new(Options.Create(h.H.Bank)),
            h.P.Customers(), h.H.Vouchers(), h.P.Users, env.Object, h.H.Clock,
            Mock.Of<IStringLocalizer<SharedResource>>(), new(h.Db, h.H.Changes(), new EphemeralDataProtectionProvider(), h.H.Clock, Options.Create(h.H.Options)))
        { ControllerContext = new() { HttpContext = new DefaultHttpContext { User = principal } } };
        var model = Assert.IsType<BookingDetailsViewModel>(Assert.IsType<ViewResult>(await controller.Details(b.Code)).Model);
        Assert.Equal(visible, model.ShowAddress);
        if (visible) { Assert.Equal(address, model.Booking.Homestay.Address); Assert.Equal("Bí mật", model.Booking.Homestay.CheckinGuideHtml); }
        else { Assert.Empty(model.Booking.Homestay.Address); Assert.Null(model.Booking.Homestay.ContactPhone); Assert.Null(model.Booking.Homestay.Latitude); Assert.Null(model.Booking.Homestay.CheckinGuideHtml); }
        await h.Db.SaveChangesAsync(); Assert.Equal(address, await h.Db.Homestays.AsNoTracking().Select(x => x.Address).SingleAsync());
    }
}
