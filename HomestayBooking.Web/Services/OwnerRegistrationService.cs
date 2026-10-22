using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record OwnerRegistrationInput(string FullName, string PhoneNumber, string HomestayName,
    string Address, int AreaId, string ContactPhone, string? Description = null,
    string? BankName = null, string? BankAccountNo = null, string? BankAccountName = null, string? BookingCode = null);
public record OwnerActivationInvitation(int RegistrationId, string Token);
public record OwnerSubmissionResult(OwnerRegistration Registration, IReadOnlyList<string> Missing)
{
    public bool Succeeded => Missing.Count == 0;
}

/// <summary>FR-25/26: duyệt người chủ riêng; giấy tờ pháp lý mới gắn từng homestay (D17).</summary>
public partial class OwnerRegistrationService(ApplicationDbContext db, UserManager<ApplicationUser> users,
    NotificationService notifications, ReceiptFileStore files, TimeProvider clock,
    BookingCodeVerificationService verification, AccountActivityLogger activityLogger)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public static string RegistrationUrl(int id) => $"/Staff/OwnerRegistrations/{id}";

    public static bool CanTransition(OwnerRegistrationStatus from, OwnerRegistrationStatus to) => (from, to) switch
    {
        (OwnerRegistrationStatus.Draft, OwnerRegistrationStatus.WaitingOwnerActivation or OwnerRegistrationStatus.PendingReview) => true,
        (OwnerRegistrationStatus.WaitingOwnerActivation, OwnerRegistrationStatus.Draft) => true,
        (OwnerRegistrationStatus.PendingReview, OwnerRegistrationStatus.UnderReview) => true,
        (OwnerRegistrationStatus.UnderReview, OwnerRegistrationStatus.NeedsRevision or OwnerRegistrationStatus.Approved or OwnerRegistrationStatus.Rejected) => true,
        (OwnerRegistrationStatus.NeedsRevision, OwnerRegistrationStatus.PendingReview) => true,
        _ => false,
    };

    public Task<OwnerRegistration> RegisterAsync(OwnerRegistrationInput input, string password, Actor? actor = null) =>
        WriteAsync(async () =>
        {
            var (person, owner, verified) = await PreparePersonAsync(input, actor, assisted: false);
            var account = person.Account;
            if (account is null)
            {
                // shortcut: chưa xác minh OTP, nâng cấp khi có dịch vụ SMS
                account = new ApplicationUser { Person = person, Status = AccountStatus.Active,
                    PasswordChangedAt = Now, CreatedAt = Now, UpdatedAt = Now };
                EnsureIdentity(await users.CreateAsync(account, password));
                if (verified)
                    activityLogger.Add(account.Id, AccountEventType.AccountCreated, ActorType.Owner, account.Id,
                        "Tự đăng ký chủ, gắn hồ sơ có sẵn sau xác minh bằng mã đơn");
            }
            if (!await users.IsInRoleAsync(account, AppRoles.Owner))
                EnsureIdentity(await users.AddToRoleAsync(account, AppRoles.Owner));
            var ownerActor = new Actor(ActorType.Owner, account.Id);
            return await CreateDraftAsync(input, owner, OwnerRegistrationSource.SelfService, ownerActor);
        });

    public Task<OwnerRegistration> CreateAssistedAsync(OwnerRegistrationInput input, Actor actor) =>
        WriteAsync(async () =>
        {
            await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
            var (_, owner, _) = await PreparePersonAsync(input, null, assisted: true);
            return await CreateDraftAsync(input, owner, OwnerRegistrationSource.EmployeeAssisted, actor);
        });

    private async Task<(PersonProfile Person, Owner Owner, bool Verified)> PreparePersonAsync(OwnerRegistrationInput input, Actor? actor, bool assisted)
    {
        var phone = PhoneNumberNormalizer.ToE164(input.PhoneNumber)
            ?? throw new BookingException("Số điện thoại không hợp lệ.");
        await ValidateHomestayAsync(input);
        var person = await db.PersonProfiles.Include(p => p.Account).Include(p => p.Owner).Include(p => p.Employee)
            .SingleOrDefaultAsync(p => p.PhoneNumber == phone);
        if (person is not null && ((person.Account is not null || person.Owner is not null)
            && (assisted || actor?.AccountId is null || actor.AccountId != person.Account?.Id
                || person.Account?.Status != AccountStatus.Active)))
            throw new BookingException("Số điện thoại đã thuộc chủ hoặc tài khoản khác; không tự ghép hồ sơ.");
        if (person?.Employee is not null || actor?.Type is ActorType.Employee or ActorType.System)
            throw new BookingException("Nhân viên không được đặt mật khẩu thay chủ homestay.");
        if (person?.Owner is not null && await db.OwnerRegistrations.AnyAsync(r => r.OwnerId == person.Owner.Id
            && OwnerRegistration.OpenStatuses.Contains(r.Status)))
            throw new BookingException("Chủ đã có một hồ sơ đang xử lý.");
        var verified = !assisted && await verification.VerifyAsync(person, input.BookingCode);
        person ??= new PersonProfile { PhoneNumber = phone, CreatedAt = Now };
        person.FullName = input.FullName.Trim(); person.UpdatedAt = Now;
        person.Owner ??= new Owner { CreatedAt = Now };
        person.Owner.BankName = input.BankName?.Trim(); person.Owner.BankAccountNo = input.BankAccountNo?.Trim();
        person.Owner.BankAccountName = input.BankAccountName?.Trim(); person.Owner.UpdatedAt = Now;
        if (person.Id == 0) db.PersonProfiles.Add(person);
        // Identity cần PersonId thật trước khi tạo Account; toàn bộ vẫn nằm trong cùng transaction.
        await db.SaveChangesAsync();
        return (person, person.Owner, verified);
    }

    internal async Task ValidateHomestayAsync(OwnerRegistrationInput input)
    {
        if (input.FullName.Trim().Length > 100 || input.BankName?.Trim().Length > 100
            || input.BankAccountNo?.Trim().Length > 30 || input.BankAccountName?.Trim().Length > 100)
            throw new BookingException("Họ tên hoặc thông tin ngân hàng quá dài.");
        if (string.IsNullOrWhiteSpace(input.HomestayName) || input.HomestayName.Trim().Length > 200
            || string.IsNullOrWhiteSpace(input.Address) || input.Address.Trim().Length > 300)
            throw new BookingException("Vui lòng nhập tên homestay và địa chỉ hợp lệ.");
        if (!await db.Areas.AnyAsync(a => a.Id == input.AreaId)) throw new BookingException("Khu vực không hợp lệ.");
        if (PhoneNumberNormalizer.ToE164(input.ContactPhone) is null) throw new BookingException("Số điện thoại liên hệ không hợp lệ.");
    }

    private async Task<OwnerRegistration> CreateDraftAsync(OwnerRegistrationInput input, Owner owner, OwnerRegistrationSource source, Actor actor)
    {
        var registration = new OwnerRegistration { Owner = owner, Source = source,
            CreatedByEmployeeId = source == OwnerRegistrationSource.EmployeeAssisted ? actor.EmployeeId : null,
            CreatedAt = Now, UpdatedAt = Now };
        var homestay = new Homestay { Owner = owner, AreaId = input.AreaId, Name = input.HomestayName.Trim(),
            Address = input.Address.Trim(), ContactPhone = PhoneNumberNormalizer.ToE164(input.ContactPhone),
            // Mô tả ở bước tiếp nhận là văn bản, mã hóa trước khi lưu vào trường HTML.
            DescriptionHtml = System.Net.WebUtility.HtmlEncode(input.Description), CreatedAt = Now, UpdatedAt = Now };
        db.OwnerRegistrations.Add(registration); db.Homestays.Add(homestay);
        // Lấy Id thật trước khi ghi audit; WriteAsync vẫn giữ cả hai lần lưu trong cùng transaction.
        await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "OwnerRegistrations", registration.Id.ToString(), null,
            new { OwnerId = owner.Id, Profile = ProfileState(owner), Registration = State(registration) }, AuditAction.Insert);
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", homestay.Id.ToString(), null, HomeState(homestay), AuditAction.Insert);
        return registration;
    }

    public Task<OwnerActivationInvitation> InviteAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
        var registration = await LoadAsync(id);
        if (registration.Source != OwnerRegistrationSource.EmployeeAssisted) throw new BookingException("Chỉ gửi mời cho hồ sơ do nhân viên tạo thay.");
        CheckTransition(registration, OwnerRegistrationStatus.WaitingOwnerActivation);
        var old = State(registration);
        var account = registration.Owner.Person.Account;
        if (account is not null) throw new BookingException("Chủ đã có tài khoản; không được đặt mật khẩu thay.");
        account = new ApplicationUser { PersonId = registration.Owner.PersonId, Status = AccountStatus.PendingActivation,
            CreatedAt = Now, UpdatedAt = Now };
        EnsureIdentity(await users.CreateAsync(account));
        EnsureIdentity(await users.AddToRoleAsync(account, AppRoles.Owner));
        registration.Status = OwnerRegistrationStatus.WaitingOwnerActivation; registration.UpdatedAt = Now;
        Audit(registration, actor, old);
        // Token chỉ chuyển qua đường mời riêng cho chủ, không ghi token/mật khẩu vào audit.
        return new OwnerActivationInvitation(id, await users.GeneratePasswordResetTokenAsync(account));
    });

    public Task<OwnerActivationInvitation> ResendInviteAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
        var registration = await LoadAsync(id);
        var account = registration.Owner.Person.Account;
        if (registration.Status != OwnerRegistrationStatus.WaitingOwnerActivation || account is null
            || account.Status != AccountStatus.PendingActivation || await users.HasPasswordAsync(account))
            throw new BookingException("Chỉ gửi lại lời mời khi chủ chưa kích hoạt và chưa có mật khẩu.");
        var old = State(registration);
        // Đổi security stamp để mọi token mời cũ mất hiệu lực, không ghi token vào audit.
        EnsureIdentity(await users.UpdateSecurityStampAsync(account));
        registration.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "OwnerRegistrations", id.ToString(), old,
            new { Step = "ResendInvite", Registration = State(registration) });
        return new OwnerActivationInvitation(id, await users.GeneratePasswordResetTokenAsync(account));
    });

    public Task<OwnerRegistration> ActivateAsync(int id, string phone, string token, string password, Actor? actor = null) => WriteAsync(async () =>
    {
        var registration = await LoadAsync(id);
        var person = registration.Owner.Person;
        if (actor?.Type is ActorType.Employee or ActorType.System
            || (actor?.AccountId is not null && (actor.AccountId != person.Account?.Id
                || await db.Employees.AnyAsync(e => e.Person.Account != null && e.Person.Account.Id == actor.AccountId))))
            throw new BookingException("Chỉ chủ được tự đặt mật khẩu kích hoạt.");
        CheckTransition(registration, OwnerRegistrationStatus.Draft);
        if (PhoneNumberNormalizer.ToE164(phone) != person.PhoneNumber || person.Account is null
            || person.Account.Status != AccountStatus.PendingActivation || await users.HasPasswordAsync(person.Account))
            throw new BookingException("Thông tin kích hoạt không hợp lệ.");
        // shortcut: chưa xác minh OTP, nâng cấp khi có dịch vụ SMS
        EnsureIdentity(await users.ResetPasswordAsync(person.Account, token, password));
        person.Account.Status = AccountStatus.Active; person.Account.PasswordChangedAt = Now; person.Account.UpdatedAt = Now;
        var old = State(registration);
        registration.Status = OwnerRegistrationStatus.Draft; registration.UpdatedAt = Now;
        Audit(registration, new Actor(ActorType.Owner, person.Account.Id), old);
        return registration;
    });

    public Task<OwnerRegistration> UpdateDraftAsync(int id, int homestayId, OwnerRegistrationInput input, Actor actor) => WriteAsync(async () =>
    {
        var registration = await LoadAsync(id); await AuthorizeEditorAsync(registration, actor);
        await ValidateHomestayAsync(input);
        var home = await db.Homestays.SingleOrDefaultAsync(h => h.Id == homestayId && h.OwnerId == registration.OwnerId)
            ?? throw new BookingException("Homestay không thuộc hồ sơ của chủ.");
        EnsureDraftEditable(registration, home);
        if (PhoneNumberNormalizer.ToE164(input.PhoneNumber) != registration.Owner.Person.PhoneNumber)
            throw new BookingException("Không đổi số điện thoại định danh qua hồ sơ đối tác.");
        var old = new { Registration = State(registration), Profile = ProfileState(registration.Owner) };
        var previousHome = HomeState(home);
        home.Name = input.HomestayName.Trim(); home.Address = input.Address.Trim(); home.AreaId = input.AreaId;
        home.ContactPhone = PhoneNumberNormalizer.ToE164(input.ContactPhone);
        home.DescriptionHtml = System.Net.WebUtility.HtmlEncode(input.Description); home.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "Homestays", home.Id.ToString(), previousHome, HomeState(home));
        var owner = registration.Owner;
        owner.Person.FullName = input.FullName.Trim(); owner.Person.UpdatedAt = Now;
        owner.BankName = input.BankName?.Trim(); owner.BankAccountNo = input.BankAccountNo?.Trim();
        owner.BankAccountName = input.BankAccountName?.Trim(); owner.UpdatedAt = Now;
        registration.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "OwnerRegistrations", id.ToString(), old,
            new { Registration = State(registration), Profile = ProfileState(owner) });
        return registration;
    });

    public Task<OwnerSubmissionResult> SubmitAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        var registration = await LoadAsync(id); await OwnerAsync(registration.OwnerId, actor);
        CheckTransition(registration, OwnerRegistrationStatus.PendingReview);
        var missing = new List<string>();
        var owner = registration.Owner;
        if (string.IsNullOrWhiteSpace(owner.Person.FullName)) missing.Add("Họ tên chủ");
        if (PhoneNumberNormalizer.ToE164(owner.Person.PhoneNumber) is null) missing.Add("Số điện thoại chủ");
        if (string.IsNullOrWhiteSpace(owner.BankName)) missing.Add("Tên ngân hàng");
        if (string.IsNullOrWhiteSpace(owner.BankAccountNo)) missing.Add("Số tài khoản ngân hàng");
        if (string.IsNullOrWhiteSpace(owner.BankAccountName)) missing.Add("Tên chủ tài khoản ngân hàng");
        // Chỉ xét các cơ sở nộp mới; không áp chuẩn hồ sơ mới lên homestay Active/giấy tờ lịch sử.
        var homestays = await db.Homestays.Where(h => h.OwnerId == owner.Id
            && (h.Status == HomestayStatus.Draft || (registration.Status == OwnerRegistrationStatus.NeedsRevision && h.Status == HomestayStatus.PendingReview))).ToListAsync();
        if (homestays.Count == 0) missing.Add("Ít nhất một homestay nháp");
        var readyHomes = new List<Homestay>();
        var missingDocuments = new List<string>();
        foreach (var home in homestays)
        {
            var homeMissing = new List<string>();
            if (string.IsNullOrWhiteSpace(home.Name) || string.IsNullOrWhiteSpace(home.Address)
                || PhoneNumberNormalizer.ToE164(home.ContactPhone ?? "") is null) homeMissing.Add($"Thông tin homestay {home.Id}");
            homeMissing.AddRange(await MissingDocumentsAsync(home, requirePhoto: true));
            if (homeMissing.Count == 0) readyHomes.Add(home);
            else missingDocuments.AddRange(homeMissing);
        }
        if (readyHomes.Count == 0) missing.AddRange(missingDocuments);
        if (missing.Count > 0) return new OwnerSubmissionResult(registration, missing);
        var old = State(registration);
        foreach (var home in readyHomes)
        {
            var previous = HomeState(home); home.Status = HomestayStatus.PendingReview; home.UpdatedAt = Now;
            PartnerServiceSupport.Audit(db, clock, actor, "Homestays", home.Id.ToString(), previous, HomeState(home));
        }
        registration.Status = OwnerRegistrationStatus.PendingReview; registration.SubmittedAt = Now; registration.UpdatedAt = Now;
        Audit(registration, actor, old);
        return new OwnerSubmissionResult(registration, []);
    });

    public Task<OwnerRegistration> TakeAsync(int id, Actor actor) => DecideAsync(id, OwnerRegistrationStatus.UnderReview, null, actor);
    public Task<OwnerRegistration> RequestRevisionAsync(int id, string note, Actor actor) => DecideAsync(id, OwnerRegistrationStatus.NeedsRevision, note, actor);
    public Task<OwnerRegistration> ApproveAsync(int id, Actor actor) => DecideAsync(id, OwnerRegistrationStatus.Approved, null, actor);
    public Task<OwnerRegistration> RejectAsync(int id, string note, Actor actor) => DecideAsync(id, OwnerRegistrationStatus.Rejected, note, actor);

    private Task<OwnerRegistration> DecideAsync(int id, OwnerRegistrationStatus target, string? note, Actor actor) => WriteAsync(async () =>
    {
        var final = target is OwnerRegistrationStatus.Approved or OwnerRegistrationStatus.Rejected;
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor,
            final ? [EmployeePosition.Manager, EmployeePosition.Administrator] : [EmployeePosition.Consultant, EmployeePosition.Manager]);
        var registration = await LoadAsync(id);
        CheckTransition(registration, target);
        if (final && registration.CreatedByEmployeeId == employee.Id) throw new BookingException("Người tạo hồ sơ thay không được tự duyệt hoặc từ chối.");
        if (target is OwnerRegistrationStatus.NeedsRevision or OwnerRegistrationStatus.Rejected && string.IsNullOrWhiteSpace(note))
            throw new BookingException("Vui lòng nhập ghi chú yêu cầu bổ sung hoặc lý do từ chối.");
        if (note?.Trim().Length > 1000) throw new BookingException("Ghi chú hồ sơ không quá 1000 ký tự.");
        var old = State(registration);
        registration.Status = target; registration.ReviewNote = note?.Trim();
        registration.ReviewedByEmployeeId = employee.Id; registration.ReviewedAt = Now; registration.UpdatedAt = Now;
        Audit(registration, actor, old);
        if (target == OwnerRegistrationStatus.Rejected)
        {
            foreach (var home in await db.Homestays.Where(h => h.OwnerId == registration.OwnerId && h.Status == HomestayStatus.PendingReview).ToListAsync())
            {
                var previous = HomeState(home); home.Status = HomestayStatus.Draft; home.UpdatedAt = Now;
                PartnerServiceSupport.Audit(db, clock, actor, "Homestays", home.Id.ToString(), previous, HomeState(home));
            }
        }
        if (target == OwnerRegistrationStatus.Approved && registration.Owner.Person.Account is { } account)
            EnsureIdentity(await users.UpdateSecurityStampAsync(account));
        var ownerMessage = target switch
        {
            OwnerRegistrationStatus.NeedsRevision => "cần bổ sung",
            OwnerRegistrationStatus.Approved => "đã được duyệt",
            OwnerRegistrationStatus.Rejected => "bị từ chối",
            _ => null,
        };
        if (ownerMessage is not null)
            PartnerServiceSupport.Notify(db, notifications, clock, [registration.Owner.Person.Account?.Id],
                $"Hồ sơ đối tác #{id} {ownerMessage}. Vui lòng xem chi tiết hồ sơ.", "/Partner/Status");
        return registration;
    });

    private async Task<OwnerRegistration> LoadAsync(int id) => await db.OwnerRegistrations
        .Include(r => r.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
        .SingleOrDefaultAsync(r => r.Id == id) ?? throw new BookingException("Không tìm thấy hồ sơ đối tác.");

    private async Task OwnerAsync(int ownerId, Actor actor)
    {
        if (actor.Type != ActorType.Owner || actor.AccountId is null || !await db.Owners.AnyAsync(o => o.Id == ownerId
            && o.Person.Account != null && o.Person.Account.Id == actor.AccountId && o.Person.Account.Status == AccountStatus.Active
            && o.Person.Employee == null))
            throw new BookingException("Chỉ chủ tài khoản được thao tác hồ sơ của mình.");
    }

    // Dùng chung cho B1/B2: hồ sơ đang duyệt khóa mọi thay đổi, kể cả do Quản lý hỗ trợ.
    internal static void EnsureDraftEditable(OwnerRegistration? registration, Homestay home)
    {
        if (registration is not null && registration.Status is not (OwnerRegistrationStatus.Draft
            or OwnerRegistrationStatus.WaitingOwnerActivation or OwnerRegistrationStatus.NeedsRevision))
            throw new BookingException("Hồ sơ đang duyệt, chỉ được sửa khi còn nháp, chờ kích hoạt hoặc cần bổ sung.");
        if (home.Status != HomestayStatus.Draft
            && !(registration?.Status == OwnerRegistrationStatus.NeedsRevision && home.Status == HomestayStatus.PendingReview))
            throw new BookingException("Chỉ sửa homestay nháp hoặc homestay chờ duyệt khi hồ sơ cần bổ sung.");
    }

    internal async Task AuthorizeEditorAsync(OwnerRegistration registration, Actor actor)
    {
        if (actor.Type == ActorType.Employee)
        {
            await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
            if (registration.Source != OwnerRegistrationSource.EmployeeAssisted
                || (registration.Owner.Person.Account is { Status: not AccountStatus.PendingActivation }))
                throw new BookingException("Sau khi chủ kích hoạt, chỉ chủ được sửa hồ sơ và giấy tờ; nhân viên chỉ sửa hồ sơ tạo thay chưa kích hoạt.");
        }
        else await OwnerAsync(registration.OwnerId, actor);
    }

    private static void CheckTransition(OwnerRegistration registration, OwnerRegistrationStatus target)
    {
        if (!CanTransition(registration.Status, target)) throw new BookingException("Không được chuyển trạng thái hồ sơ từ {0} sang {1}.", registration.Status, target);
    }
    private static void EnsureIdentity(IdentityResult result)
    {
        if (!result.Succeeded) throw new BookingException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
    private static object State(OwnerRegistration r) => new { r.OwnerId, Source = r.Source.ToString(), Status = r.Status.ToString(),
        r.CreatedByEmployeeId, r.SubmittedAt, r.ReviewedByEmployeeId, r.ReviewedAt, r.ReviewNote, r.UpdatedAt };
    private static object HomeState(Homestay h) => new { h.OwnerId, h.Name, h.Address, h.AreaId, h.ContactPhone, h.DescriptionHtml, Status = h.Status.ToString(), h.UpdatedAt };
    private static object ProfileState(Owner o) => new { o.Person.FullName, o.Person.PhoneNumber, o.BankName, o.BankAccountNo, o.BankAccountName };
    private void Audit(OwnerRegistration r, Actor actor, object old) => PartnerServiceSupport.Audit(db, clock, actor,
        "OwnerRegistrations", r.Id.ToString(), old, State(r));

    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        try
        {
            var result = await action(); await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            var message = ex.InnerException?.Message ?? ex.Message;
            if (ex is DbUpdateException && (message.Contains("IX_OwnerRegistrations_OwnerId_Open") || message.Contains("UNIQUE constraint failed: OwnerRegistrations.OwnerId")))
                throw new BookingException("Chủ đã có một hồ sơ đang xử lý.");
            if (ex is DbUpdateException && (message.Contains("IX_PersonProfiles_PhoneNumber") || message.Contains("UNIQUE constraint failed: PersonProfiles.PhoneNumber")))
                throw new BookingException("Số điện thoại đã thuộc chủ hoặc tài khoản khác; không tự ghép hồ sơ.");
            throw;
        }
    }
}
