using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerHomestaysUiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;
    public OwnerHomestaysUiTests(TestWebApplicationFactory factory) => this.factory = factory;
    private int OwnHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == "+84912000001").Select(h => h.Id).First());
    private int OtherHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));
    [Fact]
    public async Task ApprovedOwner_CanOpenScreen_AnonymousAndCustomerAreBlocked()
    {
        const string screen = "Homestays";
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
        const string action = "Homestays/Price";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Owner/{action}/1",new FormUrlEncodedContent([]))).StatusCode);
    }
    [Theory]
    [InlineData("info")]
    [InlineData("location")]
    [InlineData("rooms")]
    [InlineData("images")]
    [InlineData("amenities")]
    [InlineData("policy")]
    [InlineData("documents")]
    public async Task Editor_AllTabsRender_AndOtherOwnerIsHidden(string tab)
    {
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Owner/Homestays/Edit/{OwnHome}?tab={tab}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Owner/Homestays/Edit/{OtherHome}?tab={tab}")).StatusCode);
    }
    [Fact]
    public async Task Content_BindsNestedForm_PreservesAmenities_AndRejectsOtherOwner()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var homes=factory.WithDb(db=> new {
            Own=db.Homestays.Where(h=>h.Owner.Person.PhoneNumber=="+84912000001").Select(h=>h.Id).First(),
            Other=db.Homestays.Where(h=>h.Owner.Person.PhoneNumber!="+84912000001").Select(h=>h.Id).First()
        });
        var before=factory.WithDb(db=>db.HomestayAmenities.Count(a=>a.HomestayId==homes.Own));
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Homestays/Edit/{homes.Own}");
        var fields=new[] { ("Content.Name","Tên sửa kiểm tra"),("Content.ContactPhone","0912000001"),("Content.CheckinTime","14:00"),
            ("Content.CheckoutTime","12:00"),("Content.DescriptionHtml","Mô tả thử") };
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync($"/Owner/Homestays/Content/{homes.Own}",Form(token,fields))).StatusCode);
        Assert.Equal("Tên sửa kiểm tra",factory.WithDb(db=>db.Homestays.Single(h=>h.Id==homes.Own).Name));
        Assert.Equal(before,factory.WithDb(db=>db.HomestayAmenities.Count(a=>a.HomestayId==homes.Own)));
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync($"/Owner/Homestays/Content/{homes.Other}",Form(token,fields))).StatusCode);
    }
    [Fact]
    public async Task Price_WritesEffectiveDate_AndRejectsForeignRoomType()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var types=factory.WithDb(db=>new {
            Own=db.RoomTypes.Where(t=>t.Homestay.Owner.Person.PhoneNumber=="+84912000001").Select(t=>new {t.Id,t.HomestayId}).First(),
            Other=db.RoomTypes.Where(t=>t.Homestay.Owner.Person.PhoneNumber!="+84912000001").Select(t=>t.Id).First()
        });
        var date=VietnamTime.Today(TimeProvider.System).AddDays(190);
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Homestays/Edit/{types.Own.HomestayId}?tab=rooms");
        var fields=new[] {("typeId",types.Own.Id.ToString()),("dayType","Holiday"),("price","1234000"),("effectiveFrom",date.ToString("yyyy-MM-dd"))};
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync($"/Owner/Homestays/Price/{types.Own.HomestayId}",Form(token,fields))).StatusCode);
        Assert.Equal(1234000m,factory.WithDb(db=>db.RoomTypePrices.Single(p=>p.RoomTypeId==types.Own.Id && p.EffectiveFrom==date && p.DayType==DayType.Holiday).Price));
        var foreign=fields.Where(f=>f.Item1!="typeId").Append(("typeId",types.Other.ToString())).ToArray();
        await client.PostAsync($"/Owner/Homestays/Price/{types.Own.HomestayId}",Form(token,foreign));
        Assert.False(factory.WithDb(db=>db.RoomTypePrices.Any(p=>p.RoomTypeId==types.Other && p.EffectiveFrom==date)));
    }
    [Fact]
    public async Task Location_ChangeReturnsActiveHomeToReview_AndInvalidContentIsRetained()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var id=factory.WithDb(db=>db.Homestays.Where(h=>h.Owner.Person.PhoneNumber=="+84912000001" && h.Status==HomestayStatus.Active).Select(h=>h.Id).First());
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Homestays/Edit/{id}?tab=location");
        await client.PostAsync($"/Owner/Homestays/Location/{id}",Form(token,("Location.Address","Địa chỉ kiểm tra quyền duyệt"),("Location.AreaId",factory.WithDb(db=>db.Homestays.Single(h=>h.Id==id).AreaId).ToString())));
        Assert.Equal(HomestayStatus.PendingReview,factory.WithDb(db=>db.Homestays.Single(h=>h.Id==id).Status));
        var html=await (await client.GetAsync($"/Owner/Homestays/Edit/{id}?tab=location")).Content.ReadAsStringAsync();
        Assert.Contains("Đang chờ Quản lý duyệt",html);
        await client.PostAsync($"/Owner/Homestays/Content/{id}",Form(token,("Content.Name","Tên bản nháp chưa lưu"),("Content.ContactPhone","invalid"),("Content.CheckinTime","14:00"),("Content.CheckoutTime","12:00")));
        html=await (await client.GetAsync($"/Owner/Homestays/Edit/{id}")).Content.ReadAsStringAsync();
        Assert.Contains("Tên bản nháp chưa lưu",html);
        Assert.NotEqual("Tên bản nháp chưa lưu",factory.WithDb(db=>db.Homestays.Single(h=>h.Id==id).Name));
    }
}
