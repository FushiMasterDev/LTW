using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HomestayBooking.Tests.Services;

/// <summary>Identity thật và các service B1 trên SQLite có CHECK; file thử tách riêng trong thư mục tạm.</summary>
internal sealed class PartnerTestHost : IDisposable
{
    public BookingTestHost H { get; } = new();
    public ApplicationDbContext Db => H.Db;
    public DateOnly Today => VietnamTime.Today(H.Clock);
    public ServiceProvider Services { get; }
    public UserManager<ApplicationUser> Users => Services.GetRequiredService<UserManager<ApplicationUser>>();
    public OwnerRegistrationService Registrations { get; }
    public ContractService Contracts => new(Db, new NotificationService(Db), H.Clock);
    public Actor Manager { get; private set; } = null!;
    public Actor Admin { get; private set; } = null!;
    public Actor Consultant { get; private set; } = null!;
    public Actor Accountant { get; private set; } = null!;
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "LTW-B1-files-" + Guid.NewGuid().ToString("N"));
    public const string Password = "Owner@Test2026";
    private int _sequence;

    private PartnerTestHost()
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddSingleton(Db);
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        Services = services.BuildServiceProvider();
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e => e.ContentRootPath).Returns(DirectoryPath);
        Registrations = new(Db, Users, new NotificationService(Db), new ReceiptFileStore(env.Object), H.Clock);
    }
    public static async Task<PartnerTestHost> CreateAsync()
    {
        var h = new PartnerTestHost();
        var roles = h.Services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All) Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        h.Manager = await h.StaffAsync(EmployeePosition.Manager);
        h.Admin = await h.StaffAsync(EmployeePosition.Administrator);
        h.Consultant = await h.StaffAsync(EmployeePosition.Consultant);
        h.Accountant = await h.StaffAsync(EmployeePosition.Accountant);
        return h;
    }
    public async Task<Actor> StaffAsync(EmployeePosition position)
    {
        var person = new PersonProfile { PhoneNumber = "+84911999" + (++_sequence).ToString("000"), FullName = position.ToString() };
        var employee = new Employee { Person = person, EmployeeCode = "B1-" + _sequence, Position = position };
        var user = new ApplicationUser { Person = person, Status = AccountStatus.Active };
        Db.AddRange(user, employee); await Db.SaveChangesAsync();
        return new(ActorType.Employee, user.Id, employee.Id);
    }
    public OwnerRegistrationInput Input(string phone = "0912345678") => new("Chủ mới", phone, "Cơ sở mới", "Hà Nội", H.Homestay.AreaId,
        phone, "Mô tả", "MB", "123456789", "CHU MOI");
    public async Task<OwnerRegistration> DraftAsync() => await Registrations.RegisterAsync(Input(), Password);
    public async Task<Actor> OwnerActorAsync(OwnerRegistration r) => new(ActorType.Owner,
        (await Db.Users.SingleAsync(u => u.PersonId == r.Owner.PersonId)).Id);
    public async Task<OwnerRegistration> ReadyAsync()
    {
        var r = await DraftAsync(); await AddDocumentsAsync(r); return r;
    }
    public async Task AddDocumentsAsync(OwnerRegistration r)
    {
        var home = Db.Homestays.Single(h => h.OwnerId == r.OwnerId);
        foreach (var type in new[] { DocumentType.BusinessLicense, DocumentType.FireSafety, DocumentType.PropertyPhoto })
            Db.VerificationDocuments.Add(new() { HomestayId = home.Id, DocumentType = type, FileUrl = type + ".jpg", ExpiryDate = Today });
        await Db.SaveChangesAsync();
    }
    public async Task<OwnerRegistration> UnderReviewAsync()
    {
        var r = await ReadyAsync(); Assert.True((await Registrations.SubmitAsync(r.Id, await OwnerActorAsync(r))).Succeeded);
        return await Registrations.TakeAsync(r.Id, Consultant);
    }
    public async Task ApproveExistingOwnerAsync()
    {
        Db.OwnerRegistrations.Add(new() { OwnerId = H.Homestay.OwnerId, Status = OwnerRegistrationStatus.Approved,
            ReviewedByEmployeeId = Manager.EmployeeId, ReviewedAt = H.Clock.Now.UtcDateTime });
        await Db.SaveChangesAsync();
    }
    /// <summary>File thật theo đuôi của tên (.jpg/.webp/.pdf, còn lại PNG), Content-Type khớp.</summary>
    public static IFormFile Image(string name = "proof.png")
    {
        var bytes = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => TestFiles.Jpeg, ".webp" => TestFiles.Webp, ".pdf" => TestFiles.Pdf, _ => TestFiles.Png,
        };
        return TestFiles.Form(bytes, name, name: "document");
    }
    public void Dispose()
    {
        Services.Dispose(); H.Dispose();
        if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, recursive: true);
    }
}
