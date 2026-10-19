using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerSettlementsUiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;
    public OwnerSettlementsUiTests(TestWebApplicationFactory factory) => this.factory = factory;
    private int OwnHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == "+84912000001").Select(h => h.Id).First());
    private int OtherHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));
    [Fact]
    public async Task ApprovedOwner_CanOpenScreen_AnonymousAndCustomerAreBlocked()
    {
        const string screen = "Settlements";
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
        const string action = "Settlements/Confirm";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Owner/{action}/1",new FormUrlEncodedContent([]))).StatusCode);
    }
    [Fact]
    public async Task Settlement_NotificationRouteAndDetails_EnforceOwnershipAndHideDrafts()
    {
        var client=await factory.SignInByPhoneAsync("0912000001");
        var ids = factory.WithDb(db => new {
            Own = db.Settlements.Where(s=>s.Homestay.Owner.Person.PhoneNumber=="+84912000001" && s.Status!=SettlementStatus.Draft).Select(s=>s.Id).First(),
            Other = db.Settlements.Where(s=>s.Homestay.Owner.Person.PhoneNumber!="+84912000001").Select(s=>s.Id).First(),
            Draft = db.Settlements.Where(s=>s.Status==SettlementStatus.Draft).Select(s=>s.Id).First()
        });
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/Owner/Settlements/{ids.Own}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/Owner/Settlements/Details/{ids.Own}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/Owner/Settlements/{ids.Other}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/Owner/Settlements/{ids.Draft}")).StatusCode);
    }
    [Fact]
    public async Task Settlement_ConfirmUsesService_AndForeignMutationIsNotFound()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var ids=factory.WithDb(db=> {
            var own=db.Settlements.First(s=>s.Homestay.Owner.Person.PhoneNumber=="+84912000001");
            own.Status=SettlementStatus.Sent;
            var other=db.Settlements.First(s=>s.Homestay.Owner.Person.PhoneNumber!="+84912000001");
            db.SaveChanges();
            return (Own:own.Id,Other:other.Id,OtherStatus:other.Status);
        });
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Settlements/{ids.Own}");
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync($"/Owner/Settlements/Confirm/{ids.Own}",Form(token))).StatusCode);
        Assert.Equal(SettlementStatus.Confirmed,factory.WithDb(db=>db.Settlements.Single(s=>s.Id==ids.Own).Status));
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync($"/Owner/Settlements/Confirm/{ids.Other}",Form(token))).StatusCode);
        Assert.Equal(ids.OtherStatus,factory.WithDb(db=>db.Settlements.Single(s=>s.Id==ids.Other).Status));
    }
}
