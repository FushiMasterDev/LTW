using System.Security.Claims;
using HomestayBooking.Web;
using HomestayBooking.Web.Areas.Staff.Controllers;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class StaffBookingControllerTests
{
    [Fact]
    public async Task CreateConfirmed_UsesAuthenticatedEmployee_AndCreatesCustomerAndBookingInSqlite()
    {
        using var h=await BackofficeTestHost.CreateAsync();
        var person=h.Db.Employees.Single(e=>e.Id==h.Consultant.EmployeeId).PersonId;
        var http=new DefaultHttpContext {User=new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier,h.Consultant.AccountId!),new Claim(AppClaimTypes.PersonId,person.ToString()),
            new Claim(AppClaimTypes.EmployeePosition,EmployeePosition.Consultant.ToString())},"test"))};
        var localizer=new Mock<IStringLocalizer<SharedResource>>();
        localizer.Setup(x=>x[It.IsAny<string>()]).Returns((string key)=>new LocalizedString(key,key));
        var controller=new BookingsController(h.Db,h.Staff(),h.Customers(),h.Availability(),h.H.Pricing(),h.H.Clock,localizer.Object) {
            ControllerContext=new ControllerContext {HttpContext=http},TempData=new TempDataDictionary(http,Mock.Of<ITempDataProvider>())};
        var result=Assert.IsType<RedirectToActionResult>(await controller.CreateConfirmed(new StaffCreateForm {
            Phone="0977550505",FullName="Khách kiểm tra Staff",RoomTypeId=h.H.RoomType.Id,CheckIn=h.Today.AddDays(10),
            CheckOut=h.Today.AddDays(12),Guests=2,Channel=SourceChannel.Zalo}));
        Assert.Equal("Details",result.ActionName);
        var b=Assert.Single(h.Db.Bookings);
        Assert.Equal("+84977550505",b.ContactPhone);Assert.Equal(SourceChannel.Zalo,b.SourceChannel);
        Assert.Equal(h.Consultant.EmployeeId,b.CreatedByEmployeeId);Assert.Equal(h.Consultant.EmployeeId,b.AssignedEmployeeId);
        Assert.True(h.Db.Customers.Any(c=>c.Person.PhoneNumber==b.ContactPhone));
    }
}
