using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class StaffWorkspaceUiTests
{
    private static FormUrlEncodedContent Form(string token, params (string Key,string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));

    [Theory]
    [InlineData("/Staff/Bookings")]
    [InlineData("/Staff/Bookings/Create")]
    [InlineData("/Staff/Availability")]
    [InlineData("/Staff/Customers")]
    [InlineData("/Staff/OwnerRegistrations")]
    [InlineData("/Staff/Dashboard")]
    public async Task Screens_RenderForConsultant_AndBlockOtherRoles(string path)
    {
        using var f=new TestWebApplicationFactory();
        var employee=await f.SignInEmployeeAsync("NV001");
        Assert.Equal(HttpStatusCode.OK,(await employee.GetAsync(path)).StatusCode);
        var customer=await f.SignInByPhoneAsync("0987000001");
        Assert.StartsWith("/Account/AccessDenied",(await customer.GetAsync(path)).Headers.Location!.AbsolutePath);
        var accountant=await f.SignInEmployeeAsync("KT001");
        Assert.StartsWith(path=="/Staff/Dashboard" ? "/Accounting" : "/Account/AccessDenied",new Uri(f.ClientOptions.BaseAddress!, (await accountant.GetAsync(path)).Headers.Location!).AbsolutePath);
        Assert.StartsWith("/Account/Login",(await f.CreateClient().GetAsync(path)).Headers.Location!.AbsolutePath);
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Bookings_SearchPhone_ClaimAndReassign_RejectMoneyStatusAndMissingToken()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var data=f.WithDb(db=> {
            var b=db.Bookings.First(); b.AssignedEmployeeId=null; db.SaveChanges();
            return new {b.Id,b.Code,b.ContactPhone,b.Status,Self=db.Employees.Single(e=>e.EmployeeCode=="NV001").Id,Other=db.Employees.Single(e=>e.EmployeeCode=="NV002").Id};
        });
        var search=await client.GetStringAsync("/Staff/Bookings?Q="+Uri.EscapeDataString(data.ContactPhone));Assert.Contains(data.Code,search);
        var path=$"/Staff/Bookings/{data.Id}";var token=await TestWebApplicationFactory.TokenFromAsync(client,path);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Staff/Bookings/Assign/{data.Id}",new FormUrlEncodedContent([]))).StatusCode);
        await client.PostAsync($"/Staff/Bookings/Assign/{data.Id}",Form(token,("employeeId",data.Self.ToString())));
        Assert.Equal(data.Self,f.WithDb(db=>db.Bookings.Single(b=>b.Id==data.Id).AssignedEmployeeId));
        await client.PostAsync($"/Staff/Bookings/Status/{data.Id}",Form(token,("to","Deposited"),("note","Không được tự duyệt tiền")));
        Assert.Equal(data.Status,f.WithDb(db=>db.Bookings.Single(b=>b.Id==data.Id).Status));
        var manager=await f.SignInEmployeeAsync("QL001");token=await TestWebApplicationFactory.TokenFromAsync(manager,path);
        await manager.PostAsync($"/Staff/Bookings/Assign/{data.Id}",Form(token,("employeeId",data.Other.ToString())));
        Assert.Equal(data.Other,f.WithDb(db=>db.Bookings.Single(b=>b.Id==data.Id).AssignedEmployeeId));
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task AssistedBooking_ShowsAvailableRooms_AndInvalidInputUsesPrg()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var day=VietnamTime.Today(TimeProvider.System).AddDays(45);var end=day.AddDays(1);
        var path=$"/Staff/Bookings/Create?Phone=0977550505&CheckIn={day:yyyy-MM-dd}&CheckOut={end:yyyy-MM-dd}";
        var html=await client.GetStringAsync(path);
        var select=Regex.Match(html,"<select[^>]*id=\"RoomTypeId\"[\\s\\S]*?</select>").Value;
        var type=Regex.Match(select,"value=\"([0-9]+)\"").Groups[1].Value;Assert.NotEmpty(type);
        var token=await TestWebApplicationFactory.TokenFromAsync(client,path);
        var count=f.WithDb(db=>db.Bookings.Count());
        var invalid=await client.PostAsync("/Staff/Bookings/Create",Form(token,("Phone","bad"),("FullName",""),("RoomTypeId",type),("Channel","Web")));
        Assert.Equal(HttpStatusCode.Redirect,invalid.StatusCode);Assert.Equal(count,f.WithDb(db=>db.Bookings.Count()));Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Availability_IsFreshPerNight_AndInvalidRangeDoesNotCrash()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");var day=VietnamTime.Today(TimeProvider.System).AddDays(30);
        var response=await client.GetAsync($"/Staff/Availability?checkIn={day:yyyy-MM-dd}&checkOut={day.AddDays(2):yyyy-MM-dd}");
        Assert.True(response.Headers.CacheControl!.NoStore);var html=await response.Content.ReadAsStringAsync();
        Assert.Contains(day.ToString("dd/MM/yyyy"),html);Assert.Contains(day.AddDays(1).ToString("dd/MM/yyyy"),html);
        Assert.DoesNotContain("data-room-calendar",html);Assert.Contains("Làm mới",html);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/Staff/Availability?checkIn=bad&guests=0")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/Staff/Availability?checkIn=9999-12-31")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/Staff/Bookings/Create?CheckIn=9999-12-31")).StatusCode);Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Customer_DetailsHistory_EncodesNotes_AndPreservesAccountIdentity()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var data=f.WithDb(db=>db.Customers.Select(c=>new {c.Id,c.Person.FullName}).First());
        var path=$"/Staff/Customers/Details/{data.Id}";var token=await TestWebApplicationFactory.TokenFromAsync(client,path);
        await client.PostAsync($"/Staff/Customers/Update/{data.Id}",Form(token,("Note","<script>alert(1)</script>")));
        var html=await client.GetStringAsync(path);Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;",html);Assert.Contains("Lịch sử đặt phòng",html);
        Assert.Equal(data.FullName,f.WithDb(db=>db.Customers.Where(c=>c.Id==data.Id).Select(c=>c.Person.FullName).Single()));
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Staff/Customers/Update/{data.Id}",new FormUrlEncodedContent([]))).StatusCode);Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Registration_ConsultantCanTakeAndRequestRevision_ButCannotApproveOrReject()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var id=f.WithDb(db=>db.OwnerRegistrations.First(r=>r.Status==OwnerRegistrationStatus.PendingReview).Id);
        var path=$"/Staff/OwnerRegistrations/{id}";var token=await TestWebApplicationFactory.TokenFromAsync(client,path);
        await client.PostAsync($"/Staff/OwnerRegistrations/Take/{id}",Form(token));
        Assert.Equal(OwnerRegistrationStatus.UnderReview,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));
        foreach(var action in new[] {"Approve","Reject"}) {
            var blocked=await client.PostAsync($"/Staff/OwnerRegistrations/{action}/{id}",Form(token,("note","Không được duyệt")));
            Assert.StartsWith("/Account/AccessDenied",blocked.Headers.Location!.AbsolutePath);
        }
        await client.PostAsync($"/Staff/OwnerRegistrations/Revision/{id}",Form(token,("note","Bổ sung giấy PCCC còn hạn")));
        Assert.Equal(OwnerRegistrationStatus.NeedsRevision,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/Staff/OwnerRegistrations/Document/2147483647")).StatusCode);
        Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Registration_ManagerCanApproveAfterTaking()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("QL001");
        var id=f.WithDb(db=>db.OwnerRegistrations.First(r=>r.Status==OwnerRegistrationStatus.PendingReview).Id);
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Staff/OwnerRegistrations/{id}");
        await client.PostAsync($"/Staff/OwnerRegistrations/Take/{id}",Form(token));
        await client.PostAsync($"/Staff/OwnerRegistrations/Approve/{id}",Form(token));
        Assert.Equal(OwnerRegistrationStatus.Approved,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task Overbooking_ReportsAndRefundsThroughService_ManagementPolicyProtectsRefund()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");var day=VietnamTime.Today(TimeProvider.System);
        var id=f.WithDb(db=>db.Bookings.First(b=>b.Status==BookingStatus.Confirmed && b.CheckIn>day && !b.ChangeRequests.Any(r=>r.Status==ChangeRequestStatus.Pending || r.Status==ChangeRequestStatus.AwaitingPayment)).Id);
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Staff/Bookings/{id}");
        var report=await client.PostAsync($"/Staff/Overbooking/Report/{id}",Form(token,("note","Kiểm tra trùng phòng")));
        Assert.Contains("Overbooking",report.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/Staff/Overbooking?id={id}")).StatusCode);
        var blocked=await client.PostAsync($"/Staff/Overbooking/Refund/{id}",Form(token,("note","Không tìm được phương án")));
        Assert.StartsWith("/Account/AccessDenied",blocked.Headers.Location!.AbsolutePath);
        var customer=await f.SignInByPhoneAsync("0987000001");Assert.StartsWith("/Account/AccessDenied",(await customer.GetAsync($"/Staff/Overbooking?id={id}")).Headers.Location!.AbsolutePath);
        var manager=await f.SignInEmployeeAsync("QL001");token=await TestWebApplicationFactory.TokenFromAsync(manager,$"/Staff/Overbooking?id={id}");
        await manager.PostAsync($"/Staff/Overbooking/Refund/{id}",Form(token,("note","Không tìm được phương án khách đồng ý")));
        Assert.Equal(BookingStatus.Cancelled,f.WithDb(db=>db.Bookings.Single(b=>b.Id==id).Status));Assert.Empty(f.Errors);
    }

    [Fact]
    public async Task ContentReview_IsDeniedToConsultant_AndDashboardHasNoContentQueue()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        Assert.StartsWith("/Account/AccessDenied",(await client.GetAsync("/Staff/Homestays/1")).Headers.Location!.AbsolutePath);
        var html=await client.GetStringAsync("/Staff/Dashboard");Assert.DoesNotContain("/Staff/Homestays",html);Assert.DoesNotContain("/Admin/Homestays",html);
        Assert.Contains("Tỷ lệ chốt của tôi",html);Assert.Contains("Đơn quá 30 phút chưa xác nhận",html);Assert.Empty(f.Errors);
        var manager=await f.SignInEmployeeAsync("QL001");
        Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync("/Staff/Homestays/1")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_ListsOverdueBeyondFirstPage_AndExcludesUnexpiredAndConfirmedBookings()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var expected=f.WithDb(db=> {
            var rows=db.Bookings.OrderBy(b=>b.Id).Take(25).ToList();
            Assert.Equal(25,rows.Count);
            foreach(var b in db.Bookings) b.Status=BookingStatus.Expired;
            foreach(var b in rows) {
                b.Status=BookingStatus.Deposited;
                b.ConfirmDeadlineAt=DateTime.UtcNow.AddHours(-1);
            }
            rows[0].ConfirmDeadlineAt=DateTime.UtcNow.AddHours(1);
            rows[1].Status=BookingStatus.Confirmed;
            db.SaveChanges();
            return new {Overdue=rows.Skip(2).Select(b=>b.Code).ToArray(),Excluded=rows.Take(2).Select(b=>b.Code).ToArray()};
        });
        var html=await client.GetStringAsync("/Staff/Dashboard");
        var table=Regex.Match(html,"<section id=\"overdue\"[\\s\\S]*?</section>").Value;
        Assert.NotEmpty(table);
        foreach(var code in expected.Overdue) Assert.Contains(code,table);
        foreach(var code in expected.Excluded) Assert.DoesNotContain(code,table);
        Assert.Empty(f.Errors);
    }
    [Fact]
    public async Task Invitation_IsPrivateOneTime_ResendInvalidatesOldToken_AndOwnerActivatesThemself()
    {
        using var f=new TestWebApplicationFactory();var client=await f.SignInEmployeeAsync("NV001");
        var setup=f.WithDb(db=>new {
            Actor=db.Employees.Where(e=>e.EmployeeCode=="NV001").Select(e=>new Actor(ActorType.Employee,e.Person.Account!.Id,e.Id)).Single(),
            Area=db.Areas.First().Id
        });
        int id;
        using(var scope=f.Services.CreateScope()) {
            var r=await scope.ServiceProvider.GetRequiredService<OwnerRegistrationService>().CreateAssistedAsync(
                new("Chủ thử lời mời","0977550511","Nhà thử lời mời","Địa chỉ kiểm thử",setup.Area,"0977550511",
                    BankName:"MB",BankAccountNo:"0123456789",BankAccountName:"CHU THU LOI MOI"),setup.Actor);
            id=r.Id;
        }
        var path=$"/Staff/OwnerRegistrations/{id}";var token=await TestWebApplicationFactory.TokenFromAsync(client,path);
        await client.PostAsync($"/Staff/OwnerRegistrations/Invite/{id}",Form(token));
        async Task<string> ReadInvitation()
        {
            var response=await client.GetAsync($"/Staff/OwnerRegistrations/Invitation/{id}");
            Assert.True(response.Headers.CacheControl!.NoStore);
            var html=await response.Content.ReadAsStringAsync();
            var match=Regex.Match(html,"<textarea[^>]*id=\"invitation-url\"[^>]*>([\\s\\S]*?)</textarea>");
            Assert.True(match.Success,"Private handoff must contain the activation link.");
            return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
        }
        var oldUrl=await ReadInvitation();
        Assert.DoesNotContain("id=\"invitation-url\"",await client.GetStringAsync($"/Staff/OwnerRegistrations/Invitation/{id}"));
        Assert.Equal(OwnerRegistrationStatus.WaitingOwnerActivation,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));
        await client.PostAsync($"/Staff/OwnerRegistrations/ResendInvite/{id}",Form(token));
        var newUrl=await ReadInvitation();
        var owner=f.CreateClient();
        async Task Activate(string url)
        {
            var uri=new Uri(url);var privateToken=QueryHelpers.ParseQuery(uri.Query)["token"].ToString();
            var verification=await TestWebApplicationFactory.TokenFromAsync(owner,uri.PathAndQuery);
            var response=await owner.PostAsync("/Partner/Activate",Form(verification,("Id",id.ToString()),("Token",privateToken),
                ("PhoneNumber","0977550511"),("Password","Fuji@2026"),("ConfirmPassword","Fuji@2026")));
            Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        }
        await Activate(oldUrl);
        Assert.Equal(OwnerRegistrationStatus.WaitingOwnerActivation,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));
        await Activate(newUrl);
        Assert.Equal(OwnerRegistrationStatus.Draft,f.WithDb(db=>db.OwnerRegistrations.Single(r=>r.Id==id).Status));
        Assert.True(f.WithDb(db=>db.Users.Any(u=>u.Person.PhoneNumber=="+84977550511" && u.Status==AccountStatus.Active && u.PasswordHash!=null)));
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsync("/Partner/Activate",new FormUrlEncodedContent([]))).StatusCode);
        var denied=await client.GetAsync(new Uri(newUrl).PathAndQuery);
        Assert.True(denied.Headers.Location!.AbsolutePath=="/Account/AccessDenied");
        Assert.Empty(f.Errors);
    }

}
