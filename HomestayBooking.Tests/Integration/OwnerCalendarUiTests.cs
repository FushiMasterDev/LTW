using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerCalendarUiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;
    public OwnerCalendarUiTests(TestWebApplicationFactory factory) => this.factory = factory;
    private int OwnHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == "+84912000001").Select(h => h.Id).First());
    private int OtherHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));
    [Fact]
    public async Task ApprovedOwner_CanOpenScreen_AnonymousAndCustomerAreBlocked()
    {
        const string screen = "Calendar";
        var owner = await factory.SignInByPhoneAsync("0912000001");
        var customer = await factory.SignInByPhoneAsync("0987000001");
        var path = "/Owner/" + screen;
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).StatusCode);
        Assert.StartsWith("/Account/Login", (await factory.CreateClient().GetAsync(path)).Headers.Location!.AbsolutePath);
        Assert.StartsWith("/Account/AccessDenied", (await customer.GetAsync(path)).Headers.Location!.AbsolutePath);
        var unapproved = await factory.SignInByPhoneAsync("0912000006");
        Assert.StartsWith("/Account/AccessDenied", (await unapproved.GetAsync(path)).Headers.Location!.AbsolutePath);
    }
    [Fact]
    public async Task NewMutations_RequireAntiforgery()
    {
        const string action = "Calendar/Create";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Owner/{action}/1",new FormUrlEncodedContent([]))).StatusCode);
    }
    [Fact]
    public async Task CrossOwnerHomeFilter_IsRejected()
    {
        const string controller = "Calendar";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/Owner/{controller}?homestayId={OtherHome}")).StatusCode);
    }
    [Fact]
    public async Task Calendar_CreatesAndUnlocksTwoNights_RejectsOtherOwnersRoom()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var rooms=factory.WithDb(db=>new {
            Own=db.Rooms.Where(r=>r.RoomType.Homestay.Owner.Person.PhoneNumber=="+84912000001").Select(r=>new {r.Id,r.RoomType.HomestayId}).First(),
            Other=db.Rooms.Where(r=>r.RoomType.Homestay.Owner.Person.PhoneNumber!="+84912000001").Select(r=>r.Id).First()
        });
        var start=VietnamTime.Today(TimeProvider.System).AddDays(180);
        var token=await TestWebApplicationFactory.TokenFromAsync(client,"/Owner/Calendar");
        var fields=new[] {("homestayId",rooms.Own.HomestayId.ToString()),("roomId",rooms.Own.Id.ToString()),("start",start.ToString("yyyy-MM-dd")),("end",start.AddDays(2).ToString("yyyy-MM-dd"))};
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync("/Owner/Calendar/Create",Form(token,fields))).StatusCode);
        var block=factory.WithDb(db=>db.RoomBlocks.Single(b=>b.RoomId==rooms.Own.Id && b.StartDate==start));
        Assert.Equal(2,factory.WithDb(db=>db.RoomNights.Count(n=>n.RoomBlockId==block.Id)));
        var blocked=fields.Where(x=>x.Item1!="roomId").Append(("roomId",rooms.Other.ToString())).ToArray();
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync("/Owner/Calendar/Create",Form(token,blocked))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync("/Owner/Calendar/Unlock",Form(token,("homestayId",rooms.Own.HomestayId.ToString()),("roomId",rooms.Own.Id.ToString()),("blockId",block.Id.ToString())))).StatusCode);
        Assert.False(factory.WithDb(db=>db.RoomBlocks.Any(b=>b.Id==block.Id)));
        // InMemory does not run SQL cascade deletes. The browser smoke verifies released nights on SQL Server.
    }
    [Fact]
    public async Task MissingDates_ReturnsToValidCalendarWithError_AndCreatesNoBlock()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var room=factory.WithDb(db=>db.Rooms.Where(r=>r.RoomType.Homestay.Owner.Person.PhoneNumber=="+84912000001").Select(r=>new {r.Id,r.RoomType.HomestayId}).First());
        var before=factory.WithDb(db=>db.RoomBlocks.Count());
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Calendar?homestayId={room.HomestayId}");
        var result=await client.PostAsync("/Owner/Calendar/Create",Form(token,("homestayId",room.HomestayId.ToString()),("roomId",room.Id.ToString())));
        Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);
        var page=await client.GetAsync(result.Headers.Location);
        Assert.Equal(HttpStatusCode.OK,page.StatusCode);
        Assert.Contains("Khoảng khóa, lý do hoặc ghi chú không hợp lệ",await page.Content.ReadAsStringAsync());
        Assert.Equal(before,factory.WithDb(db=>db.RoomBlocks.Count()));
    }
}
