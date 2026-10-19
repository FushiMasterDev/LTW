using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

public class SettlementOptions
{
    public const string Section = "Settlements";
    [Range(0, 30)] public int GraceDays { get; set; } = 2;
    [Range(1, 48)] public int LookbackPeriods { get; set; } = 4;
    [Range(5, 1440)] public int GenerateEveryMinutes { get; set; } = 60;
}

public record SettlementDraftResult(Settlement? Settlement, IReadOnlyList<string> UnclosedBookingCodes);
public record SettlementLine(SettlementItem Item, string BookingCode, decimal FujiNetRevenue);
public record SettlementDetails(Settlement Settlement, IReadOnlyList<SettlementLine> Lines,
    IReadOnlyList<string> UnclosedBookingCodes, IReadOnlyList<Payment> Compensations, IReadOnlyList<AuditLog> History);
public record SettlementQuery(SettlementPeriod? Period = null, SettlementStatus? Status = null, int? HomestayId = null, int? Page = 1);

/// <summary>BR-03/D7/D8: chỉ SettlementCalculator tính tiền, bảng đã gửi luôn giữ snapshot.</summary>
public class SettlementService(ApplicationDbContext db, NotificationService notifications, TimeProvider clock, IOptions<SettlementOptions> options)
{
    private static readonly EmployeePosition[] Readers = [EmployeePosition.Accountant, EmployeePosition.Manager, EmployeePosition.Administrator];
    private static readonly BookingStatus[] Unclosed = [BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.Deposited,
        BookingStatus.PaymentSubmitted, BookingStatus.AwaitingPayment];
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public static bool CanTransition(SettlementStatus from, SettlementStatus to) => (from, to) switch
    {
        (SettlementStatus.Draft, SettlementStatus.Sent) => true,
        (SettlementStatus.Sent, SettlementStatus.Confirmed or SettlementStatus.Paid or SettlementStatus.Draft) => true,
        (SettlementStatus.Confirmed, SettlementStatus.Paid or SettlementStatus.Draft) => true,
        _ => false,
    };
    private static void ValidatePeriod(SettlementPeriod p)
    {
        if (p.Year is < 2020 or > 2100 || p.Month is < 1 or > 12 || p.PeriodNo is < 1 or > 2)
            throw new BookingException("Kỳ đối soát không hợp lệ.");
    }
    private bool Closed(SettlementPeriod p) => p.End.AddDays(options.Value.GraceDays) < VietnamTime.Today(clock);
    private static object State(Settlement s) => new { s.HomestayId, s.Year, s.Month, s.PeriodNo, Status = s.Status.ToString(),
        s.TotalRoomAmount, s.TotalRetained, s.TotalCommission, s.PayoutAmount, s.SentAt, s.ConfirmedAt, s.PaidAt, s.PaidByEmployeeId, s.PaidBankRef };
    private static object ItemState(SettlementItem i) => new { i.BookingId, i.ItemType, i.RoomAmount, i.DepositAmount, i.RefundAmount,
        i.RetainedAmount, i.CommissionRate, i.CommissionAmount, i.PayoutAmount, i.DiscountAmount };
    private static string ItemsState(IEnumerable<SettlementItem> rows) => JsonSerializer.Serialize(rows.OrderBy(i => i.BookingId).Select(ItemState));

    public async Task<SettlementDraftResult> GenerateAsync(int homestayId, SettlementPeriod period, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, Readers);
        ValidatePeriod(period);
        if (!Closed(period)) throw new BookingException("Kỳ chưa kết thúc thời gian chờ đối soát.");
        return await GenerateCoreAsync(homestayId, period, actor);
    }

    public async Task<IReadOnlyList<SettlementDraftResult>> GeneratePeriodAsync(SettlementPeriod period, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, Readers);
        ValidatePeriod(period);
        if (!Closed(period)) throw new BookingException("Kỳ chưa kết thúc thời gian chờ đối soát.");
        return await GeneratePeriodCoreAsync(period, actor);
    }

    public async Task<int> GenerateRecentAsync()
    {
        var period = SettlementCalculator.PeriodOf(VietnamTime.Today(clock));
        // Tìm kỳ gần nhất đã hết grace, rồi lùi đúng LookbackPeriods kỳ (kể cả qua năm mới).
        while (!Closed(period)) period = SettlementCalculator.PeriodOf(period.Start.AddDays(-1));
        var count = 0;
        for (var i = 0; i < options.Value.LookbackPeriods && period.Year >= 2020; i++)
        {
            count += (await GeneratePeriodCoreAsync(period, Actor.System)).Count(r => r.Settlement is not null);
            period = SettlementCalculator.PeriodOf(period.Start.AddDays(-1));
        }
        return count;
    }

    private async Task<IReadOnlyList<SettlementDraftResult>> GeneratePeriodCoreAsync(SettlementPeriod p, Actor actor)
    {
        var homes = await db.Bookings.Where(b => b.CheckOut >= p.Start && b.CheckOut <= p.End).Select(b => b.HomestayId)
            .Union(db.Settlements.Where(s => s.Year == p.Year && s.Month == p.Month && s.PeriodNo == p.PeriodNo).Select(s => s.HomestayId))
            .Distinct().Order().ToListAsync();
        var result = new List<SettlementDraftResult>();
        foreach (var home in homes) result.Add(await GenerateCoreAsync(home, p, actor));
        return result;
    }

    private async Task<SettlementDraftResult> GenerateCoreAsync(int homeId, SettlementPeriod period, Actor actor)
    {
        // Đọc snapshot trước, không mở transaction/khóa homestay nếu bảng không cần ghi lại.
        var snapshot = await db.Settlements.AsNoTracking().SingleOrDefaultAsync(s => s.HomestayId == homeId && s.Year == period.Year && s.Month == period.Month && s.PeriodNo == period.PeriodNo);
        if (snapshot is not null && snapshot.Status != SettlementStatus.Draft) return new(snapshot, []);
        var unclosed = await UnclosedAsync(homeId, period);
        var (built, oldItems) = await CalculateDraftAsync(homeId, period, snapshot);
        if (!NeedsRebuild(snapshot, built, oldItems))
        {
            if (snapshot is null && !await db.Homestays.AnyAsync(h => h.Id == homeId)) throw new BookingException("Không tìm thấy homestay.");
            return new(snapshot, unclosed);
        }
        return await WriteAsync<SettlementDraftResult>(async () =>
        {
            await LockHomeAsync(homeId);
            var row = await db.Settlements.SingleOrDefaultAsync(s => s.HomestayId == homeId && s.Year == period.Year && s.Month == period.Month && s.PeriodNo == period.PeriodNo);
            if (row is not null) await db.Entry(row).ReloadAsync();
            if (row is not null && row.Status != SettlementStatus.Draft) return new(row, []);
            // Kiểm tra lại dưới khóa: snapshot đọc trước có thể đã thay đổi hoặc được người khác gửi.
            var currentUnclosed = await UnclosedAsync(homeId, period);
            await RebuildAsync(homeId, period, row, actor);
            row ??= db.Settlements.Local.SingleOrDefault(s => s.HomestayId == homeId && s.Year == period.Year && s.Month == period.Month && s.PeriodNo == period.PeriodNo);
            return new(row, currentUnclosed);
        });
    }

    private async Task<(Settlement Built, List<SettlementItem> OldItems)> CalculateDraftAsync(int homeId, SettlementPeriod p, Settlement? row)
    {
        var id = row?.Id ?? 0;
        var bookings = await db.Bookings.AsNoTracking().Where(b => b.HomestayId == homeId && b.CheckOut >= p.Start && b.CheckOut <= p.End
            && !db.SettlementItems.Any(i => i.BookingId == b.Id && i.SettlementId != id)).ToListAsync();
        var built = SettlementCalculator.Build(homeId, p, bookings);
        var oldItems = row is null ? [] : await db.SettlementItems.AsNoTracking().Where(i => i.SettlementId == row.Id).ToListAsync();
        return (built, oldItems);
    }

    private static bool NeedsRebuild(Settlement? row, Settlement built, IReadOnlyList<SettlementItem> oldItems) => row is null
        ? built.Items.Count > 0
        : row.TotalRoomAmount != built.TotalRoomAmount || row.TotalRetained != built.TotalRetained
            || row.TotalCommission != built.TotalCommission || row.PayoutAmount != built.PayoutAmount || ItemsState(oldItems) != ItemsState(built.Items);

    private async Task RebuildAsync(int homeId, SettlementPeriod p, Settlement? row, Actor actor)
    {
        var (built, oldItems) = await CalculateDraftAsync(homeId, p, row);
        if (!NeedsRebuild(row, built, oldItems)) return;
        var old = row is null ? null : new { Settlement = State(row), Items = oldItems.Select(ItemState).ToArray() };
        if (row is null)
        {
            row = new Settlement { HomestayId = homeId, Year = p.Year, Month = p.Month, PeriodNo = p.PeriodNo, CreatedAt = Now, UpdatedAt = Now };
            db.Settlements.Add(row); await db.SaveChangesAsync();
        }
        else
        {
            // Xóa dòng nháp cũ và lưu trước khi thêm lại, tránh unique BookingId; cùng transaction.
            var items = await db.SettlementItems.Where(i => i.SettlementId == row.Id).ToListAsync();
            db.SettlementItems.RemoveRange(items); await db.SaveChangesAsync();
        }
        row.TotalRoomAmount = built.TotalRoomAmount; row.TotalRetained = built.TotalRetained;
        row.TotalCommission = built.TotalCommission; row.PayoutAmount = built.PayoutAmount; row.UpdatedAt = Now;
        foreach (var item in built.Items)
        {
            item.Booking = null!; item.SettlementId = row.Id; db.SettlementItems.Add(item);
        }
        await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Settlements", row.Id.ToString(), old,
            new { Settlement = State(row), Items = built.Items.Select(ItemState).ToArray() }, old is null ? AuditAction.Insert : AuditAction.Update);
    }

    private Task<List<string>> UnclosedAsync(int homeId, SettlementPeriod p) => db.Bookings.AsNoTracking()
        .Where(b => b.HomestayId == homeId && b.CheckOut >= p.Start && b.CheckOut <= p.End && Unclosed.Contains(b.Status))
        .OrderBy(b => b.Code).Select(b => b.Code).ToListAsync();

    public Task<Settlement> SendAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        var row = await LoadLockedAsync(id); EnsureTransition(row, SettlementStatus.Sent);
        var p = Period(row); if (!Closed(p)) throw new BookingException("Kỳ chưa kết thúc thời gian chờ đối soát.");
        var unclosed = await UnclosedAsync(row.HomestayId, p);
        if (unclosed.Count > 0) throw new BookingException("Còn đơn chưa chốt trong kỳ: {0}.", string.Join(", ", unclosed));
        await RebuildAsync(row.HomestayId, p, row, actor);
        if (!await db.SettlementItems.AnyAsync(i => i.SettlementId == id)) throw new BookingException("Không gửi bảng đối soát rỗng.");
        var old = State(row); row.Status = SettlementStatus.Sent; row.SentAt = row.UpdatedAt = Now;
        Audit(row, actor, old, "Sent"); await NotifyOwnerAsync(row, "đã được gửi, vui lòng kiểm tra số liệu"); return row;
    });

    public Task<Settlement> ConfirmAsync(int id, Actor actor) => WriteAsync(async () =>
    {
        var row = await LoadLockedAsync(id); await OwnerAsync(row.HomestayId, actor);
        EnsureTransition(row, SettlementStatus.Confirmed); var old = State(row);
        row.Status = SettlementStatus.Confirmed; row.ConfirmedAt = row.UpdatedAt = Now;
        Audit(row, actor, old, "OwnerConfirmed"); await NotifyAccountantsAsync(row, "đã được chủ xác nhận"); return row;
    });

    public Task<Settlement> ConfirmOnBehalfAsync(int id, string reason, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        reason = RequiredNote(reason, 500);
        var row = await LoadLockedAsync(id); EnsureTransition(row, SettlementStatus.Confirmed);
        var old = State(row); row.Status = SettlementStatus.Confirmed; row.ConfirmedAt = row.UpdatedAt = Now;
        Audit(row, actor, old, "AccountantConfirmedOnBehalf", $"Kế toán xác nhận thay chủ: {reason}");
        await NotifyOwnerAsync(row, "đã được kế toán xác nhận thay chủ"); return row;
    });

    public Task<Settlement> ReportDiscrepancyAsync(int id, string note, Actor actor) => WriteAsync(async () =>
    {
        note = RequiredNote(note, 500); var row = await LoadLockedAsync(id); await OwnerAsync(row.HomestayId, actor);
        if (row.Status == SettlementStatus.Draft) throw new BookingException("Chủ không được xem bảng nháp.");
        var old = State(row); row.UpdatedAt = Now;
        Audit(row, actor, old, "OwnerDiscrepancy", note); await NotifyAccountantsAsync(row, "có phản hồi số liệu từ chủ"); return row;
    });

    public Task<Settlement> ReopenAsync(int id, string reason, Actor actor) => WriteAsync(async () =>
    {
        reason = RequiredNote(reason, 500); await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        var row = await LoadLockedAsync(id); EnsureTransition(row, SettlementStatus.Draft); var old = State(row);
        row.Status = SettlementStatus.Draft; row.SentAt = row.ConfirmedAt = null; row.UpdatedAt = Now;
        Audit(row, actor, old, "Reopened", reason);
        await RebuildAsync(row.HomestayId, Period(row), row, actor);
        await NotifyOwnerAsync(row, "đã được mở lại để kế toán điều chỉnh"); return row;
    });

    public Task<Settlement> RecordPaidAsync(int id, string? bankRef, Actor actor) => RecordPaidCoreAsync(id, bankRef, null, Now, actor);

    public Task<Settlement> RecordPaidAsync(int id, string? bankRef, decimal amount, DateTime paidAt, Actor actor) =>
        RecordPaidCoreAsync(id, bankRef, amount, paidAt, actor);

    private Task<Settlement> RecordPaidCoreAsync(int id, string? bankRef, decimal? amount, DateTime paidAt, Actor actor) => WriteAsync(async () =>
    {
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant);
        var row = await LoadLockedAsync(id); EnsureTransition(row, SettlementStatus.Paid);
        if (amount is not null && amount != row.PayoutAmount)
            throw new BookingException("Số tiền chuyển phải bằng đúng số phải trả ({0:N0}đ); không hỗ trợ chi từng phần.", row.PayoutAmount);
        if (paidAt.Kind != DateTimeKind.Utc) throw new BookingException("Thời điểm chuyển tiền phải theo UTC.");
        if (paidAt > Now) throw new BookingException("Ngày chuyển tiền không được ở tương lai.");
        if (row.SentAt is null || paidAt < row.SentAt || row.ConfirmedAt is not null && paidAt < row.ConfirmedAt)
            throw new BookingException("Ngày chuyển tiền không được trước thời điểm gửi/xác nhận đối soát.");
        await PartnerServiceSupport.RequireOwnerBankAsync(db, row.HomestayId);
        bankRef = string.IsNullOrWhiteSpace(bankRef) ? null : bankRef.Trim();
        if (row.PayoutAmount > 0) bankRef = AccountingService.RequireTransferReference(bankRef);
        else if (bankRef?.Length > 100) throw new BookingException("Mã giao dịch tối đa 100 ký tự.");
        var old = State(row); row.Status = SettlementStatus.Paid; row.PaidAt = paidAt; row.UpdatedAt = Now;
        row.PaidByEmployeeId = employee.Id; row.PaidBankRef = bankRef;
        Audit(row, actor, old, "Paid", $"Số tiền: {row.PayoutAmount}; ngày chuyển UTC: {paidAt:O}"); await NotifyOwnerAsync(row, "đã được Fuji chuyển tiền"); return row;
    });

    public async Task<PaginatedList<Settlement>> ListAsync(SettlementQuery query, Actor actor)
    {
        var rows = db.Settlements.AsNoTracking().Include(s => s.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).AsQueryable();
        if (actor.Type == ActorType.Employee) await PartnerServiceSupport.EmployeeAsync(db, actor, Readers);
        else
        {
            var ownerId = await OwnerIdAsync(actor);
            rows = rows.Where(s => s.Homestay.OwnerId == ownerId && s.Status != SettlementStatus.Draft);
        }
        if (query.Period is { } p) { ValidatePeriod(p); rows = rows.Where(s => s.Year == p.Year && s.Month == p.Month && s.PeriodNo == p.PeriodNo); }
        if (query.Status is { } status) rows = rows.Where(s => s.Status == status);
        if (query.HomestayId is int home) rows = rows.Where(s => s.HomestayId == home);
        return await PaginatedList<Settlement>.CreateAsync(rows.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ThenByDescending(s => s.PeriodNo).ThenBy(s => s.HomestayId), query.Page);
    }

    public async Task<SettlementDetails> DetailsAsync(int id, Actor actor)
    {
        var row = await db.Settlements.AsNoTracking().Include(s => s.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person)
            .Include(s => s.Items).ThenInclude(i => i.Booking)
            .SingleOrDefaultAsync(s => s.Id == id) ?? throw new BookingException("Không tìm thấy bảng đối soát.");
        if (actor.Type == ActorType.Employee) await PartnerServiceSupport.EmployeeAsync(db, actor, Readers);
        else { await OwnerAsync(row.HomestayId, actor); if (row.Status == SettlementStatus.Draft) throw new BookingException("Chủ không được xem bảng nháp."); }
        var p = Period(row);
        var compensation = await db.Payments.AsNoTracking().Where(x => x.Type == PaymentType.Compensation && x.Booking.HomestayId == row.HomestayId
            && x.Booking.CheckOut >= p.Start && x.Booking.CheckOut <= p.End).Include(x => x.Booking).OrderBy(x => x.Id).ToListAsync();
        var history = await db.AuditLogs.AsNoTracking().Where(a => a.TableName == "Settlements" && a.RecordId == id.ToString()).OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).ToListAsync();
        return new(row, row.Items.OrderBy(i => i.Booking.Code).Select(i => new SettlementLine(i, i.Booking.Code, SettlementCalculator.FujiNetRevenue(i))).ToList(),
            row.Status == SettlementStatus.Draft ? await UnclosedAsync(row.HomestayId, p) : [], compensation, history);
    }

    private static SettlementPeriod Period(Settlement s) => new(s.Year, s.Month, s.PeriodNo);
    private static void EnsureTransition(Settlement s, SettlementStatus to)
    { if (!CanTransition(s.Status, to)) throw new BookingException("Bảng đối soát đã được xử lý hoặc không thể chuyển trạng thái này."); }
    private static string RequiredNote(string? note, int max)
    { note = note?.Trim(); return string.IsNullOrEmpty(note) || note.Length > max ? throw new BookingException("Vui lòng nhập lý do/ghi chú từ 1 đến {0} ký tự.", max) : note; }
    private void Audit(Settlement row, Actor actor, object old, string step, string? note = null) =>
        PartnerServiceSupport.Audit(db, clock, actor, "Settlements", row.Id.ToString(), old, new { Step = step, Settlement = State(row), Note = note });
    private async Task LockHomeAsync(int id)
    {
        if (db.Database.IsRelational())
        { if (await db.Homestays.Where(h => h.Id == id).ExecuteUpdateAsync(s => s.SetProperty(h => h.UpdatedAt, h => h.UpdatedAt)) != 1) throw new BookingException("Không tìm thấy homestay."); }
        else if (!await db.Homestays.AnyAsync(h => h.Id == id)) throw new BookingException("Không tìm thấy homestay.");
    }
    private async Task<Settlement> LoadLockedAsync(int id)
    {
        var homeId = await db.Settlements.Where(s => s.Id == id).Select(s => (int?)s.HomestayId).SingleOrDefaultAsync()
            ?? throw new BookingException("Không tìm thấy bảng đối soát.");
        await LockHomeAsync(homeId);
        var row = await db.Settlements.Include(s => s.Homestay).SingleAsync(s => s.Id == id); await db.Entry(row).ReloadAsync(); return row;
    }
    private async Task<int> OwnerIdAsync(Actor actor)
    {
        var owner = actor.Type != ActorType.Owner || actor.AccountId is null ? null : await db.Owners.Where(o => o.Person.Employee == null
            && o.Person.Account != null && o.Person.Account.Id == actor.AccountId && o.Person.Account.Status == AccountStatus.Active).Select(o => (int?)o.Id).SingleOrDefaultAsync();
        return owner ?? throw new BookingException("Bạn không có quyền xem đối soát của chủ này.");
    }
    private async Task OwnerAsync(int homeId, Actor actor)
    { var owner = await OwnerIdAsync(actor); if (!await db.Homestays.AnyAsync(h => h.Id == homeId && h.OwnerId == owner)) throw new BookingException("Bảng đối soát không thuộc homestay của bạn."); }
    private async Task NotifyOwnerAsync(Settlement row, string status)
    {
        var account = await db.Homestays.Where(h => h.Id == row.HomestayId).Select(h => h.Owner.Person.Account == null ? null : h.Owner.Person.Account.Id).SingleAsync();
        PartnerServiceSupport.Notify(db, notifications, clock, [account], $"Bảng đối soát #{row.Id} kỳ {row.PeriodNo}/{row.Month}/{row.Year} {status}.", $"/Owner/Settlements/{row.Id}");
    }
    private async Task NotifyAccountantsAsync(Settlement row, string status)
    {
        var accounts = await db.Employees.Where(e => e.Position == EmployeePosition.Accountant && e.EmploymentStatus == EmploymentStatus.Active
            && e.Person.Account != null && e.Person.Account.Status == AccountStatus.Active).Select(e => e.Person.Account!.Id).ToListAsync();
        PartnerServiceSupport.Notify(db, notifications, clock, accounts, $"Bảng đối soát #{row.Id} {status}.", $"/Accounting/Settlements/{row.Id}");
    }
    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        try { return await HomestayWrite.RunAsync(db, action); }
        catch (DbUpdateException ex) when (ex.GetBaseException().Message.Contains("IX_Settlements_", StringComparison.Ordinal)
            || ex.GetBaseException().Message.Contains("IX_SettlementItems_BookingId", StringComparison.Ordinal)
            || ex.GetBaseException().Message.Contains("UNIQUE constraint failed: Settlement", StringComparison.Ordinal))
        { throw new BookingException("Bảng đối soát hoặc đơn vừa được người khác xử lý, vui lòng tải lại."); }
    }
}
