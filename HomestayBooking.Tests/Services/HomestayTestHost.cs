using System.Security.Claims;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HomestayBooking.Tests.Services;

/// <summary>B2 dùng lại Identity, đồng hồ và SQLite thật của B1; ảnh công khai nằm trong thư mục tạm riêng.</summary>
internal sealed class HomestayTestHost : IDisposable
{
    public PartnerTestHost P { get; }
    public BookingTestHost H => P.H;
    public ApplicationDbContext Db => P.Db;
    public DateOnly Today => P.Today;
    public HomestayAccessService Access { get; }
    public Actor Owner { get; }
    public Actor Manager => P.Manager;
    public Homestay Home => Db.Homestays.Single(h => h.Id == H.Homestay.Id);
    public int HomeId => H.Homestay.Id;
    public IWebHostEnvironment Environment { get; }
    public HomestayContentService Content => new(Db, Access, new(Db), H.Clock);
    public RoomService Rooms => new(Db, Access, H.Clock);
    public RoomBlockService Blocks => new(Db, Access, H.Clock);
    public HomestayImageService Images => new(Db, Access, Environment, H.Clock);
    public HomestayPolicyService Policies => new(Db, Access, H.Clock);
    public HomestayDocumentService Documents => new(Db, Access, P.Registrations, new(Db), H.Clock);
    public HomestayReviewService Reviews => new(Db, Access, P.Registrations, new(Db), H.Clock);
    private HomestayTestHost(PartnerTestHost p)
    {
        P = p; Access = new(Db, p.Registrations);
        Owner = new(ActorType.Owner, Db.Users.Single(u => u.PersonId == H.Homestay.Owner.PersonId).Id);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ContentRootPath).Returns(p.DirectoryPath);
        environment.SetupGet(e => e.WebRootPath).Returns(Path.Combine(p.DirectoryPath, "wwwroot"));
        Environment = environment.Object;
    }
    public static async Task<HomestayTestHost> CreateAsync(bool ready = false)
    {
        var h = new HomestayTestHost(await PartnerTestHost.CreateAsync());
        if (ready) await h.ReadyAsync(); return h;
    }
    public async Task ReadyAsync()
    {
        await P.ApproveExistingOwnerAsync();
        Home.ContactPhone = "+84912000001"; Home.DescriptionHtml = "<p>Đủ hồ sơ</p>";
        foreach (var type in new[] { DocumentType.BusinessLicense, DocumentType.FireSafety, DocumentType.PropertyPhoto })
            Db.VerificationDocuments.Add(new() { HomestayId = HomeId, DocumentType = type, FileUrl = type + ".png", ExpiryDate = Today.AddYears(1) });
        Db.HomestayImages.Add(new() { HomestayId = HomeId, Url = "/images/demo/cover.jpg", IsCover = true });
        await Db.SaveChangesAsync();
    }
    public HomestayContentInput ContentInput(string? description = null) => new("Tên mới", description ?? "<p>Mô tả mới</p>", "<b>Hướng dẫn</b>", "0912000001", new(15, 0), new(11, 0), []);
    public RoomTypeInput TypeInput(string name = "Loại mới") => new(name, H.RoomType.CategoryId, 3, 30, "Phòng mới");
    public static Dictionary<DayType, decimal> Prices(decimal price = 600000) => Enum.GetValues<DayType>().ToDictionary(d => d, _ => price);
    public Task<Booking> BookAsync(int days = 5) => H.Bookings().CreateAsync(H.Request(Today.AddDays(days), Today.AddDays(days + 2)));
    public static ClaimsPrincipal Principal(string id, string role) => new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Role, role)], "test"));
    public ClaimsPrincipal Customer => Principal(Db.Users.Single(u => u.PersonId == H.Customer.PersonId).Id, "Customer");
    public ClaimsPrincipal Management => Principal(Manager.AccountId!, "Employee");
    public async Task<Booking> DepositedAsync(int days = 5)
    {
        var b = await BookAsync(days); await H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System); return b;
    }
    public void Dispose() => P.Dispose();
}
