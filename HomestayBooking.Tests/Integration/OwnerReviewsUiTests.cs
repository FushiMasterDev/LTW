using System.Net;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

public sealed class OwnerReviewsUiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;
    public OwnerReviewsUiTests(TestWebApplicationFactory factory) => this.factory = factory;
    private int OwnHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber == "+84912000001").Select(h => h.Id).First());
    private int OtherHome => factory.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) => new(
        fields.Append(("__RequestVerificationToken",token)).Select(x=>new KeyValuePair<string,string>(x.Item1,x.Item2)));
    [Fact]
    public async Task ApprovedOwner_CanOpenScreen_AnonymousAndCustomerAreBlocked()
    {
        const string screen = "Reviews";
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
        const string action = "Reviews/Reply";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync($"/Owner/{action}/1",new FormUrlEncodedContent([]))).StatusCode);
    }
    [Fact]
    public async Task CrossOwnerHomeFilter_IsRejected()
    {
        const string controller = "Reviews";
        var client = await factory.SignInByPhoneAsync("0912000001");
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/Owner/{controller}?homestayId={OtherHome}")).StatusCode);
    }
    [Fact]
    public async Task Review_ReplyIsEncoded_AndCannotEditOtherOwnersReview()
    {
        using var factory=new TestWebApplicationFactory();
        var client=await factory.SignInByPhoneAsync("0912000001");
        var reviews=factory.WithDb(db=>new {
            Own=db.Reviews.Where(r=>r.Homestay.Owner.Person.PhoneNumber=="+84912000001" && !r.IsHidden).Select(r=>new {r.Id,r.HomestayId}).First(),
            Other=db.Reviews.Where(r=>r.Homestay.Owner.Person.PhoneNumber!="+84912000001").Select(r=>r.Id).First()
        });
        var token=await TestWebApplicationFactory.TokenFromAsync(client,$"/Owner/Reviews?homestayId={reviews.Own.HomestayId}");
        var fields=new[] {("homestayId",reviews.Own.HomestayId.ToString()),("reply","<script>alert(1)</script>")};
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync($"/Owner/Reviews/Reply/{reviews.Own.Id}",Form(token,fields))).StatusCode);
        var html=await (await client.GetAsync($"/Owner/Reviews?homestayId={reviews.Own.HomestayId}")).Content.ReadAsStringAsync();
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;",html);
        var before=factory.WithDb(db=>db.Reviews.Single(r=>r.Id==reviews.Other).OwnerReply);
        await client.PostAsync($"/Owner/Reviews/Reply/{reviews.Other}",Form(token,fields));
        Assert.Equal(before,factory.WithDb(db=>db.Reviews.Single(r=>r.Id==reviews.Other).OwnerReply));
    }
}
