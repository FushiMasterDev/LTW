using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record RegistrationQuery(OwnerRegistrationStatus? Status = null, bool OverdueOnly = false, string? Term = null, int? Page = 1);
public record RegistrationRow(int Id, string? OwnerName, string Phone, OwnerRegistrationStatus Status, OwnerRegistrationSource Source,
    DateTime? SubmittedAt, DateTime? DeadlineLocal, bool Overdue, string? Reviewer, int Homestays, int ValidDocuments);
public record RegistrationDocument(int Id, int? HomestayId, DocumentType Type, string? OriginalFileName, DateOnly? ExpiryDate, bool Expired);
public record RegistrationHomestay(int Id, string Name, string Address, HomestayStatus Status, IReadOnlyList<RegistrationDocument> Documents);
public record RegistrationDetails(int Id, OwnerRegistrationStatus Status, OwnerRegistrationSource Source, string? OwnerName, string Phone,
    string? Email, string? BankName, string? BankAccountNo, string? BankAccountName, DateTime? SubmittedAt, DateTime? DeadlineLocal,
    bool Overdue, string? Reviewer, string? ReviewNote, AccountStatus? AccountStatus, IReadOnlyList<RegistrationHomestay> Homestays,
    IReadOnlyList<RegistrationDocument> LegacyDocuments, IReadOnlyList<AuditLog> History);
public record OwnerRegistrationView(int Id, OwnerRegistrationStatus Status, DateTime? SubmittedAt, string? ReviewNote,
    IReadOnlyList<RegistrationHomestay> Homestays, IReadOnlyList<RegistrationDocument> LegacyDocuments);
public record PrivateDocumentFile(string Path, string ContentType, string DownloadName);

/// <summary>FR-25/FR-26: đọc hồ sơ cho người thẩm định và cho chính chủ; thao tác duyệt vẫn ở OwnerRegistrationService.</summary>
public class OwnerRegistrationQueryService(ApplicationDbContext db, ReceiptFileStore files, TimeProvider clock)
{
    private const int PageSize = 20;
    private static readonly EmployeePosition[] Reviewers = [EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator];
    private static readonly OwnerRegistrationStatus[] Reviewing = [OwnerRegistrationStatus.PendingReview, OwnerRegistrationStatus.UnderReview];

    public async Task<PaginatedList<RegistrationRow>> ListAsync(RegistrationQuery query, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, Reviewers);
        var rows = db.OwnerRegistrations.AsNoTracking();
        if (query.Status is { } status) rows = rows.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var term = query.Term.Trim(); var phone = PhoneNumberNormalizer.ToE164(term);
            rows = rows.Where(r => (r.Owner.Person.FullName != null && r.Owner.Person.FullName.Contains(term))
                || (phone != null && r.Owner.Person.PhoneNumber == phone));
        }
        var today = VietnamTime.Today(clock);
        var data = await rows.Select(r => new
        {
            r.Id, r.Owner.Person.FullName, r.Owner.Person.PhoneNumber, r.Status, r.Source, r.SubmittedAt,
            Reviewer = r.ReviewedByEmployee == null ? null : r.ReviewedByEmployee.Person.FullName,
            Homestays = r.Owner.Homestays.Count(),
            ValidDocuments = db.VerificationDocuments.Count(d => (d.OwnerRegistrationId == r.Id || d.Homestay!.OwnerId == r.OwnerId)
                && (d.ExpiryDate == null || d.ExpiryDate >= today)),
        }).ToListAsync();
        var holidays = await HolidaysAsync();
        // Hạn xử lý cần lịch ngày lễ nên tính trong bộ nhớ; số hồ sơ đối tác nhỏ.
        var result = data.Select(r =>
        {
            var (deadline, overdue) = Deadline(r.Status, r.SubmittedAt, holidays);
            return new RegistrationRow(r.Id, r.FullName, r.PhoneNumber, r.Status, r.Source, r.SubmittedAt, deadline, overdue,
                r.Reviewer, r.Homestays, r.ValidDocuments);
        }).Where(r => !query.OverdueOnly || r.Overdue)
            .OrderBy(r => r.DeadlineLocal ?? DateTime.MaxValue).ThenBy(r => r.Id).ToList();
        var page = Math.Max(1, query.Page ?? 1);
        return new(result.Skip((page - 1) * PageSize).Take(PageSize), result.Count, page, PageSize);
    }

    public async Task<RegistrationDetails> DetailsAsync(int id, Actor actor)
    {
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor, Reviewers);
        var r = await db.OwnerRegistrations.AsNoTracking().Include(x => x.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .Include(x => x.ReviewedByEmployee).ThenInclude(e => e!.Person)
            .SingleOrDefaultAsync(x => x.Id == id) ?? throw new BookingException("Không tìm thấy hồ sơ đối tác.");
        var (deadline, overdue) = Deadline(r.Status, r.SubmittedAt, await HolidaysAsync());
        var bankNo = r.Owner.BankAccountNo;
        if (employee.Position == EmployeePosition.Consultant && bankNo is not null)
            bankNo = bankNo.Length <= 4 ? new string('*', bankNo.Length) : new string('*', bankNo.Length - 4) + bankNo[^4..];
        var history = await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "OwnerRegistrations" && a.RecordId == id.ToString())
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).ToListAsync();
        return new(r.Id, r.Status, r.Source, r.Owner.Person.FullName, r.Owner.Person.PhoneNumber, r.Owner.Person.Email,
            r.Owner.BankName, bankNo, r.Owner.BankAccountName, r.SubmittedAt, deadline, overdue, r.ReviewedByEmployee?.Person.FullName,
            r.ReviewNote, r.Owner.Person.Account?.Status, await HomestaysAsync(r.OwnerId), await LegacyAsync(r.Id), history);
    }

    /// <summary>FR-25: chủ theo dõi hồ sơ gần nhất của mình; không lộ người thẩm định nội bộ.</summary>
    public async Task<OwnerRegistrationView?> MineAsync(Actor actor)
    {
        var ownerId = await OwnerIdAsync(actor) ?? throw new BookingException("Bạn không có quyền xem hồ sơ đối tác này.");
        var r = await db.OwnerRegistrations.AsNoTracking().Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync();
        if (r is null) return null;
        var note = r.Status is OwnerRegistrationStatus.NeedsRevision or OwnerRegistrationStatus.Rejected ? r.ReviewNote : null;
        return new(r.Id, r.Status, r.SubmittedAt, note, await HomestaysAsync(ownerId), await LegacyAsync(r.Id));
    }

    /// <summary>File giấy tờ riêng tư: chỉ người thẩm định hoặc đúng chủ; không bao giờ trả đường dẫn trong wwwroot.</summary>
    public async Task<PrivateDocumentFile> OpenDocumentAsync(int documentId, Actor actor)
    {
        var doc = await db.VerificationDocuments.AsNoTracking().Where(d => d.Id == documentId)
            .Select(d => new { d.FileUrl, d.OriginalFileName, OwnerId = d.Homestay != null ? (int?)d.Homestay.OwnerId : d.OwnerRegistration!.OwnerId })
            .SingleOrDefaultAsync() ?? throw new BookingException("Không tìm thấy giấy tờ.");
        if (actor.Type == ActorType.Employee) await PartnerServiceSupport.EmployeeAsync(db, actor, Reviewers);
        else if (doc.OwnerId is null || await OwnerIdAsync(actor) != doc.OwnerId) throw new BookingException("Bạn không có quyền xem giấy tờ này.");
        var path = files.Resolve(doc.FileUrl) ?? throw new BookingException("File giấy tờ không còn trên máy chủ.");
        return new(path, ReceiptFileStore.ContentTypeOf(doc.FileUrl), doc.OriginalFileName ?? doc.FileUrl);
    }

    private (DateTime? Deadline, bool Overdue) Deadline(OwnerRegistrationStatus status, DateTime? submitted, IReadOnlySet<DateOnly> holidays)
    {
        if (!Reviewing.Contains(status) || submitted is null) return (null, false);
        var deadline = OwnerRegistrationService.ReviewDeadlineLocal(VietnamTime.ToLocal(DateTime.SpecifyKind(submitted.Value, DateTimeKind.Utc)), holidays);
        return (deadline, VietnamTime.ToLocal(clock.GetUtcNow().UtcDateTime) > deadline);
    }

    private async Task<IReadOnlySet<DateOnly>> HolidaysAsync() => (await db.Holidays.Select(h => h.Date).ToListAsync()).ToHashSet();

    private async Task<IReadOnlyList<RegistrationHomestay>> HomestaysAsync(int ownerId)
    {
        var today = VietnamTime.Today(clock);
        var homes = await db.Homestays.AsNoTracking().Where(h => h.OwnerId == ownerId).OrderBy(h => h.Id)
            .Select(h => new { h.Id, h.Name, h.Address, h.Status }).ToListAsync();
        var ids = homes.Select(h => h.Id).ToArray();
        var docs = await db.VerificationDocuments.AsNoTracking().Where(d => d.HomestayId != null && ids.Contains(d.HomestayId.Value))
            .OrderBy(d => d.Id).ToListAsync();
        return homes.Select(h => new RegistrationHomestay(h.Id, h.Name, h.Address, h.Status,
            docs.Where(d => d.HomestayId == h.Id).Select(d => Document(d, today)).ToList())).ToList();
    }

    private async Task<IReadOnlyList<RegistrationDocument>> LegacyAsync(int registrationId)
    {
        var today = VietnamTime.Today(clock);
        return (await db.VerificationDocuments.AsNoTracking().Where(d => d.OwnerRegistrationId == registrationId).OrderBy(d => d.Id).ToListAsync())
            .Select(d => Document(d, today)).ToList();
    }

    private static RegistrationDocument Document(VerificationDocument d, DateOnly today) =>
        new(d.Id, d.HomestayId, d.DocumentType, d.OriginalFileName, d.ExpiryDate, d.ExpiryDate < today);

    private async Task<int?> OwnerIdAsync(Actor actor) => actor.Type != ActorType.Owner || actor.AccountId is null ? null
        : await db.Owners.Where(o => o.Person.Employee == null && o.Person.Account != null && o.Person.Account.Id == actor.AccountId
            && o.Person.Account.Status == AccountStatus.Active).Select(o => (int?)o.Id).SingleOrDefaultAsync();
}
