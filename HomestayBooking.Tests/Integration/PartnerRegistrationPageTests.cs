using System.Net;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Tests.Integration;

/// <summary>FR-25 trang công khai: khách chưa đăng nhập tạo hồ sơ đối tác, được đăng nhập với vai trò chủ và xem trạng thái hồ sơ.</summary>
public sealed class PartnerRegistrationPageTests
{
    [Fact]
    public async Task AnonymousVisitorCreatesDraftAndSeesStatus()
    {
        using var f = new TestWebApplicationFactory();
        var areaId = f.WithDb(db => db.Areas.OrderBy(a => a.Id).Select(a => a.Id).First());
        var client = f.CreateClient();
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Partner");
        var response = await client.PostAsync("/Partner", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["HomestayName"] = "Nhà Thử Đối Tác", ["AreaId"] = areaId.ToString(),
            ["Address"] = "1 Ngõ Thử, Hà Nội", ["ContactPhone"] = "0977000222", ["FullName"] = "Chủ Thử", ["PhoneNumber"] = "0977000111",
            ["BankName"] = "MB Bank", ["BankAccountNo"] = "0123456789", ["BankAccountName"] = "CHU THU",
            ["Password"] = "Fuji@2026", ["ConfirmPassword"] = "Fuji@2026",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Partner/Status", response.Headers.Location?.OriginalString);
        Assert.Equal(OwnerRegistrationStatus.Draft, f.WithDb(db => db.OwnerRegistrations.Single(r => r.Owner.Person.PhoneNumber == "+84977000111").Status));
        var status = WebUtility.HtmlDecode(await client.GetStringAsync("/Partner/Status"));
        Assert.Contains("Nhà Thử Đối Tác", status); Assert.Contains("name=\"homestayId\"", status);
    }

    [Fact]
    public async Task SignedInCustomerGetsOwnerRoleWithoutSigningInAgain()
    {
        using var f = new TestWebApplicationFactory();
        var areaId = f.WithDb(db => db.Areas.OrderBy(a => a.Id).Select(a => a.Id).First());
        var client = await f.SignInByPhoneAsync("0987000001");
        var token = await TestWebApplicationFactory.TokenFromAsync(client, "/Partner");
        var response = await client.PostAsync("/Partner", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["HomestayName"] = "Nhà Của Khách Cũ", ["AreaId"] = areaId.ToString(),
            ["Address"] = "2 Ngõ Thử, Hà Nội", ["ContactPhone"] = "0977000333", ["FullName"] = "Nguyễn Văn Minh", ["PhoneNumber"] = "0987000001",
            ["BankName"] = "MB Bank", ["BankAccountNo"] = "0123456780", ["BankAccountName"] = "NGUYEN VAN MINH",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Partner/Status", response.Headers.Location?.OriginalString);
        // Cookie phải được làm mới để có vai trò Owner, nếu không /Partner/Status chuyển sang AccessDenied.
        var status = await client.GetAsync("/Partner/Status");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Contains("Nhà Của Khách Cũ", WebUtility.HtmlDecode(await status.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task EmployeeCannotUsePublicPartnerForm()
    {
        using var f = new TestWebApplicationFactory();
        var client = await f.SignInEmployeeAsync("NV001");
        var response = await client.GetAsync("/Partner");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(0, f.WithDb(db => db.OwnerRegistrations.Count(r => r.Owner.Person.Employee != null)));
    }
}
