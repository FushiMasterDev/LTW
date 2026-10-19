using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public partial class OwnerRegistrationService
{
    private static readonly DocumentType[] RequiredDocuments = [DocumentType.BusinessLicense, DocumentType.FireSafety, DocumentType.PropertyPhoto];
    private static string DocumentName(DocumentType type) => type switch
    {
        DocumentType.BusinessLicense => "Giấy phép kinh doanh", DocumentType.FireSafety => "Giấy PCCC",
        DocumentType.PropertyPhoto => "Ảnh thực tế", _ => "Giấy tờ khác",
    };

    private async Task<Homestay> EditableHomestayAsync(int id, Actor actor)
    {
        var home = await db.Homestays.SingleOrDefaultAsync(h => h.Id == id) ?? throw new BookingException("Không tìm thấy homestay.");
        var registration = await db.OwnerRegistrations.Include(r => r.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(r => r.OwnerId == home.OwnerId && OwnerRegistration.OpenStatuses.Contains(r.Status));
        if (registration is not null) await AuthorizeEditorAsync(registration, actor);
        else await OwnerAsync(home.OwnerId, actor);
        EnsureDraftEditable(registration, home);
        return home;
    }

    public async Task<VerificationDocument> UploadDocumentAsync(int homestayId, DocumentType type, IFormFile file,
        DateOnly? expiryDate, Actor actor)
    {
        string? stored = null;
        try
        {
            return await WriteAsync(async () =>
            {
                var home = await EditableHomestayAsync(homestayId, actor);
                var document = await StoreDocumentAsync(home, type, file, expiryDate, actor);
                stored = document.FileUrl;
                return document;
            });
        }
        catch
        {
            // Chỉ dọn file vừa tạo bởi thao tác thất bại, không xóa giấy tờ đã tồn tại.
            if (stored is not null && files.Resolve(stored) is { } path) File.Delete(path);
            throw;
        }
    }

    /// <summary>Dùng chung B1/B2 sau chốt quyền của từng luồng; không tự thay trạng thái cơ sở.</summary>
    internal async Task<VerificationDocument> StoreDocumentAsync(Homestay home, DocumentType type, IFormFile file,
        DateOnly? expiryDate, Actor actor)
    {
        if (!Enum.IsDefined(type)) throw new BookingException("Loại giấy tờ không hợp lệ.");
        if (Path.GetFileName(file.FileName).Length > 255) throw new BookingException("Tên file quá dài.");
        if (expiryDate < VietnamTime.Today(clock)) throw new BookingException("Giấy tờ đã hết hạn.");
        var stored = await files.SaveAsync(file, $"HS{home.Id}", allowPdf: true);
        try
        {
            var document = new VerificationDocument { HomestayId = home.Id, DocumentType = type,
                FileUrl = stored, OriginalFileName = Path.GetFileName(file.FileName), ExpiryDate = expiryDate, CreatedAt = Now };
            db.VerificationDocuments.Add(document); await db.SaveChangesAsync();
            PartnerServiceSupport.Audit(db, clock, actor, "VerificationDocuments", document.Id.ToString(), null,
                new { document.HomestayId, Type = type.ToString(), document.FileUrl, document.ExpiryDate }, AuditAction.Insert);
            return document;
        }
        catch { DeleteNewDocumentFile(stored); throw; }
    }

    internal void DeleteNewDocumentFile(string stored)
    {
        if (files.Resolve(stored) is { } path) File.Delete(path);
    }

    internal async Task<List<string>> MissingDocumentsAsync(Homestay home, bool requirePhoto, bool allowLegacy = false)
    {
        var today = VietnamTime.Today(clock);
        var types = await db.VerificationDocuments.Where(d => (d.HomestayId == home.Id
            || (allowLegacy && d.OwnerRegistrationId != null && d.OwnerRegistration!.OwnerId == home.OwnerId
                && d.OwnerRegistration.Status == OwnerRegistrationStatus.Approved))
            && (d.ExpiryDate == null || d.ExpiryDate >= today)).Select(d => d.DocumentType).ToListAsync();
        return RequiredDocuments.Where(t => requirePhoto || t != DocumentType.PropertyPhoto)
            .Where(t => !types.Contains(t)).Select(t => $"{home.Name}: {DocumentName(t)} còn hạn").ToList();
    }

    public Task<int> DeleteDocumentAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        var document = await db.VerificationDocuments.SingleOrDefaultAsync(d => d.Id == id)
            ?? throw new BookingException("Không tìm thấy giấy tờ.");
        if (document.HomestayId is not int homeId) throw new BookingException("Không sửa liên kết giấy tờ lịch sử của hồ sơ cũ.");
        await EditableHomestayAsync(homeId, actor);
        db.VerificationDocuments.Remove(document);
        // Giữ file riêng tư để truy vết; xóa bản ghi nghiệp vụ, không thay liên kết giấy tờ cũ.
        PartnerServiceSupport.Audit(db, clock, actor, "VerificationDocuments", id.ToString(),
            new { document.HomestayId, document.FileUrl, document.ExpiryDate, Type = document.DocumentType.ToString() },
            new { Deleted = true }, AuditAction.Delete);
        return id;
    });

    /// <summary>Hàm thuần: giữ giờ địa phương, cộng ba ngày làm việc sau ngày nộp, bỏ thứ Bảy/CN và ngày lễ.</summary>
    public static DateTime ReviewDeadlineLocal(DateTime submittedLocal, IReadOnlySet<DateOnly> holidays)
    {
        var deadline = submittedLocal;
        for (var count = 0; count < 3;)
        {
            deadline = deadline.AddDays(1);
            if (deadline.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(DateOnly.FromDateTime(deadline))) count++;
        }
        return deadline;
    }

    public Task<int> AlertOverdueAsync() => WriteAsync(async () =>
    {
        var pending = await db.OwnerRegistrations.Where(r => (r.Status == OwnerRegistrationStatus.PendingReview
            || r.Status == OwnerRegistrationStatus.UnderReview) && r.SubmittedAt != null).ToListAsync();
        var holidays = (await db.Holidays.Select(h => h.Date).ToListAsync()).ToHashSet();
        var managers = await PartnerServiceSupport.Managers(db).ToListAsync();
        if (managers.Count == 0) return 0;
        var alerted = 0;
        foreach (var registration in pending)
        {
            var submitted = registration.SubmittedAt!.Value;
            if (VietnamTime.ToLocal(Now) <= ReviewDeadlineLocal(VietnamTime.ToLocal(DateTime.SpecifyKind(submitted, DateTimeKind.Utc)), holidays)) continue;
            var url = RegistrationUrl(registration.Id);
            if (await db.Notifications.AnyAsync(n => n.Url == url && n.CreatedAt > submitted)) continue;
            PartnerServiceSupport.Notify(db, notifications, clock, managers,
                $"Hồ sơ đối tác #{registration.Id} đã quá hạn ba ngày làm việc, cần thẩm định.", url);
            PartnerServiceSupport.Audit(db, clock, Actor.System, "OwnerRegistrations", registration.Id.ToString(),
                new { registration.SubmittedAt }, new { Step = "SlaAlert", registration.SubmittedAt, AlertedAt = Now });
            alerted++;
        }
        return alerted;
    });
}
