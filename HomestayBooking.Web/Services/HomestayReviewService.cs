using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record HomestayDraftInput(string Name, string Address, int AreaId, string ContactPhone, string? Description);
public record HomestayReviewResult(Homestay Homestay, IReadOnlyList<string> Missing)
{
    public bool Succeeded => Missing.Count == 0;
}
public record PendingHomestay(Homestay Homestay, DateTime? SubmittedAt);
/// <summary>FirstReview = homestay chưa từng được duyệt (mới); false = duyệt lại sau khi đổi địa chỉ/giấy tờ (quyết định 16/10).</summary>
public record ReviewFieldChange(string Field, string? Before, string? After);
public record PendingHomestayDetails(HomestayEditor Content, DateTime? SubmittedAt, bool FirstReview, string? OwnerName, string OwnerPhone)
{
    public string? ChangeReason { get; init; }
    public HomestayEditor? BeforeContent { get; init; }
    public IReadOnlyList<ReviewFieldChange> Changes { get; init; } = [];
    public IReadOnlyList<HomestayEditorDocument> LegacyDocuments { get; init; } = [];
}

public class HomestayReviewService(ApplicationDbContext db, HomestayAccessService access,
    OwnerRegistrationService registrations, NotificationService notifications, TimeProvider clock)
{
    public static bool CanTransition(HomestayStatus from, HomestayStatus to) => (from, to) switch
    {
        (HomestayStatus.Draft, HomestayStatus.PendingReview or HomestayStatus.Ended) => true,
        (HomestayStatus.PendingReview, HomestayStatus.Active or HomestayStatus.Draft or HomestayStatus.Ended) => true,
        (HomestayStatus.Active, HomestayStatus.Suspended or HomestayStatus.PendingReview or HomestayStatus.Ended) => true,
        (HomestayStatus.Suspended, HomestayStatus.Active or HomestayStatus.Ended) => true,
        _ => false,
    };
    internal static void Move(Homestay home, HomestayStatus to, TimeProvider clock)
    {
        if (!CanTransition(home.Status, to)) throw new BookingException("Không được chuyển trạng thái homestay từ {0} sang {1}.", home.Status, to);
        home.Status = to; home.UpdatedAt = clock.GetUtcNow().UtcDateTime;
    }
    internal static object State(Homestay h) => new { h.Id, h.OwnerId, h.Name, h.Address, h.AreaId, h.Latitude, h.Longitude,
        h.ContactPhone, h.DescriptionHtml, h.CheckinGuideHtml, h.CheckinTime, h.CheckoutTime,
        Status = h.Status.ToString(), h.ApprovedByEmployeeId, h.ApprovedAt, h.UpdatedAt };

    public Task<Homestay> CreateAsync(HomestayDraftInput input, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var owner = actor.Type == ActorType.Owner && actor.AccountId is not null ? await db.Owners.Include(o => o.Person)
            .SingleOrDefaultAsync(o => o.Person.Account != null && o.Person.Account.Id == actor.AccountId
                && o.Person.Account.Status == AccountStatus.Active && o.Person.Employee == null) : null;
        if (owner is null || !await db.OwnerRegistrations.AnyAsync(r => r.OwnerId == owner.Id && r.Status == OwnerRegistrationStatus.Approved))
            throw new BookingException("Chỉ chủ đã được duyệt được thêm homestay.");
        await registrations.ValidateHomestayAsync(new(owner.Person.FullName ?? "", owner.Person.PhoneNumber,
            input.Name, input.Address, input.AreaId, input.ContactPhone, input.Description));
        var home = new Homestay { OwnerId = owner.Id, Name = input.Name.Trim(), Address = input.Address.Trim(), AreaId = input.AreaId,
            ContactPhone = HomestayBooking.Web.Identity.PhoneNumberNormalizer.ToE164(input.ContactPhone),
            DescriptionHtml = new Ganss.Xss.HtmlSanitizer().Sanitize(input.Description ?? ""),
            CreatedAt = clock.GetUtcNow().UtcDateTime, UpdatedAt = clock.GetUtcNow().UtcDateTime };
        db.Homestays.Add(home); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", home.Id.ToString(), null, State(home), AuditAction.Insert);
        return home;
    });

    public Task<HomestayReviewResult> SubmitAsync(int id, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var home = await access.AuthorizeAsync(id, actor, allowConsultant: false);
        if (actor.Type != ActorType.Owner) throw new BookingException("Chỉ chủ được tự nộp homestay.");
        if (!await db.OwnerRegistrations.AnyAsync(r => r.OwnerId == home.OwnerId && r.Status == OwnerRegistrationStatus.Approved))
            throw new BookingException("Chủ chưa được duyệt, chưa thể nộp homestay riêng.");
        if (!CanTransition(home.Status, HomestayStatus.PendingReview) || home.Status != HomestayStatus.Draft)
            throw new BookingException("Chỉ nộp duyệt homestay nháp.");
        var previouslyApproved = home.ApprovedAt is not null;
        var missing = await registrations.MissingDocumentsAsync(home, requirePhoto: !previouslyApproved, allowLegacy: previouslyApproved);
        if (missing.Count > 0) return new HomestayReviewResult(home, missing);
        var old = State(home); Move(home, HomestayStatus.PendingReview, clock);
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old, State(home));
        await HomestayManagerMessages.SendAsync(db, notifications, clock, id, "Có nội dung chờ duyệt");
        return new HomestayReviewResult(home, []);
    });

    public async Task<List<PendingHomestay>> PendingAsync(Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        var homes = await db.Homestays.AsNoTracking().Where(h => h.Status == HomestayStatus.PendingReview).ToListAsync();
        var ids = homes.Select(h => h.Id.ToString()).ToArray();
        var logs = await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "Homestays" && ids.Contains(a.RecordId)).ToListAsync();
        return homes.Select(h => new PendingHomestay(h, logs.Where(a => a.RecordId == h.Id.ToString()
            && JsonStatus(a.NewValues) == "PendingReview" && JsonStatus(a.OldValues) != "PendingReview")
            .Select(a => (DateTime?)a.CreatedAt).Max())).OrderBy(x => x.SubmittedAt ?? DateTime.MinValue).ThenBy(x => x.Homestay.Id).ToList();
    }
    /// <summary>Nội dung đầy đủ cho trang duyệt; chỉ Quản lý/Quản trị, chỉ homestay đang chờ duyệt.</summary>
    public async Task<PendingHomestayDetails> PendingDetailsAsync(int id, Actor actor)
    {
        var pending = (await PendingAsync(actor)).SingleOrDefault(p => p.Homestay.Id == id)
            ?? throw new BookingException("Homestay không ở trạng thái chờ duyệt.");
        var content = await new HomestayContentService(db, access, notifications, clock).EditorAsync(id, actor, allowConsultant: false);
        var owner = await db.Homestays.AsNoTracking().Where(h => h.Id == id)
            .Select(h => new { h.Owner.Person.FullName, h.Owner.Person.PhoneNumber }).SingleAsync();
        var logs = await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "Homestays" && a.RecordId == id.ToString()
            && a.CreatedAt == pending.SubmittedAt).OrderByDescending(a => a.Id).ToListAsync();
        var log = logs.FirstOrDefault(a => JsonStatus(a.NewValues) == "PendingReview" && JsonStatus(a.OldValues) != "PendingReview");
        var changes = new List<ReviewFieldChange>(); string? reason = null; HomestayEditor? beforeContent = null;
        if (log?.OldValues is { } oldJson && log.NewValues is { } newJson)
        {
            using var oldDoc = JsonDocument.Parse(oldJson); using var newDoc = JsonDocument.Parse(newJson);
            static JsonElement Values(JsonElement e) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("Values", out var v) ? v : e;
            static JsonElement Home(JsonElement e) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("Homestay", out var h) ? h : e;
            if (oldDoc.RootElement.ValueKind == JsonValueKind.Object && oldDoc.RootElement.TryGetProperty("Snapshot", out var snapshot) && snapshot.ValueKind == JsonValueKind.Object)
                beforeContent = snapshot.Deserialize<HomestayEditor>();
            var before = Home(Values(oldDoc.RootElement)); var after = JsonSerializer.SerializeToElement(content);
            foreach (var field in new[] { "Address", "AreaId", "Latitude", "Longitude", "Name", "DescriptionHtml", "ContactPhone" })
                if (before.ValueKind == JsonValueKind.Object && after.ValueKind == JsonValueKind.Object && before.TryGetProperty(field, out var b) && after.TryGetProperty(field, out var a) && b.ToString() != a.ToString())
                    changes.Add(new(field, b.ValueKind == JsonValueKind.Null ? null : b.ToString(), a.ValueKind == JsonValueKind.Null ? null : a.ToString()));
            if (Values(newDoc.RootElement).TryGetProperty("ChangeReason", out var why)) reason = why.GetString();
            else if (changes.Any(c => new[] { "Address", "AreaId", "Latitude", "Longitude" }.Contains(c.Field))) reason = "Thay đổi địa chỉ / vị trí";
        }
        if (beforeContent is { } saved)
        {
            // Compare actual stored metadata with the current editor; never reconstruct old values from notifications.
            static string RoomSummary(HomestayEditor h) => string.Join("\n", h.RoomTypes.Select(t =>
                $"{t.Name} · {t.CategoryName} · {t.MaxGuests} · {t.AreaM2} · {t.Description} · {t.IsActive}: "
                + string.Join(", ", t.Rooms.Select(r => $"{r.RoomCode} ({r.IsActive})"))));
            static string PriceSummary(HomestayEditor h) => string.Join("\n", h.RoomTypes.SelectMany(t => t.CurrentPrices.Concat(t.UpcomingPrices)
                .Select(p => $"{t.Name} · {p.DayType} · {p.EffectiveFrom:yyyy-MM-dd} · {p.Price}")));
            static string ImageSummary(HomestayEditor h) => string.Join("\n", h.Images.Select(i => $"{i.Url} · {i.RoomTypeId} · {i.SortOrder} · {i.IsCover}"));
            static string DocumentSummary(HomestayEditor h) => string.Join("\n", h.Documents.Select(d => $"{d.Id} · {d.Type} · {d.OriginalFileName} · {d.ExpiryDate:yyyy-MM-dd}"));
            static string AmenitySummary(HomestayEditor h) => string.Join(", ", h.Amenities.Where(a => a.Selected).Select(a => a.Name));
            static string PolicySummary(HomestayEditor h) => string.Join("\n", h.Policies.Select(p => $"{p.Scope} · {p.Name}: "
                + string.Join(", ", p.Rules.Select(r => $"{r.MinDaysBefore} / {r.RefundPercent}%"))));
            foreach (var (field, oldValue, newValue) in new[] {
                ("RoomTypes", RoomSummary(saved), RoomSummary(content)), ("Prices", PriceSummary(saved), PriceSummary(content)),
                ("Images", ImageSummary(saved), ImageSummary(content)), ("Documents", DocumentSummary(saved), DocumentSummary(content)),
                ("Amenities", AmenitySummary(saved), AmenitySummary(content)), ("Policies", PolicySummary(saved), PolicySummary(content)),
                ("DescriptionHtml", saved.DescriptionHtml, content.DescriptionHtml), ("Name", saved.Name, content.Name),
                ("CheckinGuideHtml", saved.CheckinGuideHtml, content.CheckinGuideHtml), ("ContactPhone", saved.ContactPhone, content.ContactPhone),
                ("CheckinTime", saved.CheckinTime.ToString("HH:mm"), content.CheckinTime.ToString("HH:mm")),
                ("CheckoutTime", saved.CheckoutTime.ToString("HH:mm"), content.CheckoutTime.ToString("HH:mm")) })
                if (oldValue != newValue && changes.All(c => c.Field != field)) changes.Add(new(field, oldValue, newValue));
        }
        var today = VietnamTime.Today(clock);
        var legacy = await db.VerificationDocuments.AsNoTracking().Where(d => d.OwnerRegistrationId != null
            && d.OwnerRegistration!.OwnerId == pending.Homestay.OwnerId && d.OwnerRegistration.Status == OwnerRegistrationStatus.Approved
            && (d.ExpiryDate == null || d.ExpiryDate >= today)).OrderBy(d => d.Id)
            .Select(d => new HomestayEditorDocument(d.Id, d.DocumentType, d.OriginalFileName, d.ExpiryDate, d.CreatedAt)).ToListAsync();
        return new(content, pending.SubmittedAt, pending.Homestay.ApprovedAt is null, owner.FullName, owner.PhoneNumber)
        { ChangeReason = reason, BeforeContent = beforeContent, Changes = changes, LegacyDocuments = legacy };
    }

    private static string? JsonStatus(string? json)
    {
        if (json is null) return null;
        using var doc = JsonDocument.Parse(json);
        static string? Read(JsonElement e)
        {
            if (e.ValueKind != JsonValueKind.Object) return null;
            if (e.TryGetProperty("Status", out var s) && s.ValueKind == JsonValueKind.String) return s.GetString();
            foreach (var property in new[] { "Values", "Homestay" }) if (e.TryGetProperty(property, out var nested) && Read(nested) is { } status) return status;
            return null;
        }
        return Read(doc.RootElement);
    }

    public Task<HomestayReviewResult> ApproveAsync(int id, Actor actor) => ActivateAsync(id, actor, null, HomestayStatus.PendingReview);
    public Task<HomestayReviewResult> ResumeAsync(int id, string reason, Actor actor) => ActivateAsync(id, actor, Reason(reason), HomestayStatus.Suspended);
    private Task<HomestayReviewResult> ActivateAsync(int id, Actor actor, string? reason, HomestayStatus expected) => HomestayWrite.RunAsync(db, async () =>
    {
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        var home = await access.AuthorizeAsync(id, actor, allowConsultant: false);
        if (home.Status != expected) throw new BookingException("Trạng thái homestay không phù hợp để duyệt/mở lại.");
        var missing = await MissingForActivationAsync(home);
        if (missing.Count > 0) return new HomestayReviewResult(home, missing);
        var old = State(home); Move(home, HomestayStatus.Active, clock);
        home.ApprovedByEmployeeId = employee.Id; home.ApprovedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old, new { Homestay = State(home), Reason = reason });
        await NotifyOwnerAsync(home, "đã được duyệt hoạt động"); return new HomestayReviewResult(home, []);
    });
    private async Task<List<string>> MissingForActivationAsync(Homestay home)
    {
        var missing = new List<string>(); var today = VietnamTime.Today(clock);
        if (!await db.OwnerRegistrations.AnyAsync(r => r.OwnerId == home.OwnerId && r.Status == OwnerRegistrationStatus.Approved)) missing.Add("Chủ chưa được duyệt");
        if (!await db.Contracts.Covering(today, today).AnyAsync(c => c.HomestayId == home.Id)) missing.Add("Hợp đồng đang hiệu lực");
        missing.AddRange(await registrations.MissingDocumentsAsync(home, requirePhoto: false, allowLegacy: true));
        var types = await db.RoomTypes.Include(t => t.Rooms).Include(t => t.Prices).Where(t => t.HomestayId == home.Id && t.IsActive).AsSplitQuery().ToListAsync();
        var usable = types.Where(t => t.Rooms.Any(r => r.IsActive)).ToList();
        if (usable.Count == 0) missing.Add("Loại phòng có phòng đang bán");
        if (!usable.Any(t => Enum.GetValues<DayType>().All(day => t.Prices.Any(p => p.DayType == day && p.Price > 0 && p.EffectiveFrom <= today))))
            missing.Add("Đủ ba giá hiệu lực hôm nay");
        if (!await db.HomestayImages.AnyAsync(i => i.HomestayId == home.Id && i.IsCover)) missing.Add("Ảnh bìa");
        if (string.IsNullOrWhiteSpace(home.Address)) missing.Add("Địa chỉ");
        if (string.IsNullOrWhiteSpace(home.ContactPhone)) missing.Add("Số điện thoại liên hệ");
        if (string.IsNullOrWhiteSpace(home.DescriptionHtml)) missing.Add("Mô tả");
        return missing;
    }
    public Task<Homestay> RejectAsync(int id, string reason, Actor actor) => ChangeAsync(id, HomestayStatus.PendingReview, HomestayStatus.Draft, reason, actor);
    public Task<Homestay> SuspendAsync(int id, string reason, Actor actor) => ChangeAsync(id, HomestayStatus.Active, HomestayStatus.Suspended, reason, actor);
    public Task<Homestay> EndAsync(int id, string reason, Actor actor) => ChangeAsync(id, null, HomestayStatus.Ended, reason, actor);
    private Task<Homestay> ChangeAsync(int id, HomestayStatus? expected, HomestayStatus target, string reason, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        reason = Reason(reason); var home = await access.AuthorizeAsync(id, actor, allowConsultant: false);
        if (expected is not null && home.Status != expected) throw new BookingException("Trạng thái homestay không phù hợp.");
        if (target == HomestayStatus.Ended)
        {
            if (await db.Contracts.AnyAsync(c => c.HomestayId == id && c.Status == ContractStatus.Active)) throw new BookingException("Còn hợp đồng Active, chưa kết thúc homestay được.");
            var holds = Enum.GetValues<BookingStatus>().Where(s => s.HoldsRoom() && s != BookingStatus.Completed).ToArray();
            var codes = await db.Bookings.Where(b => b.HomestayId == id && holds.Contains(b.Status)).Select(b => b.Code).ToListAsync();
            if (codes.Count > 0) throw new BookingException("Còn đơn giữ phòng: {0}.", string.Join(", ", codes));
        }
        var old = State(home); Move(home, target, clock);
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", id.ToString(), old, new { Homestay = State(home), Reason = reason });
        await NotifyOwnerAsync(home, target == HomestayStatus.Draft ? "cần chỉnh sửa trước khi nộp lại" : target == HomestayStatus.Ended ? "đã kết thúc hợp tác" : "đã tạm ngưng");
        return home;
    });
    private static string Reason(string reason) => !string.IsNullOrWhiteSpace(reason) ? reason.Trim() : throw new BookingException("Vui lòng nhập lý do.");
    private async Task NotifyOwnerAsync(Homestay home, string message)
    {
        var account = await db.Owners.Where(o => o.Id == home.OwnerId).Select(o => o.Person.Account == null ? null : o.Person.Account.Id).SingleAsync();
        PartnerServiceSupport.Notify(db, notifications, clock, [account], $"Homestay #{home.Id} {message}. Vui lòng xem chi tiết.", $"/Owner/Homestays/{home.Id}");
    }
}
