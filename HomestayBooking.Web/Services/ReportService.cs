using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record ReportQuery(DateOnly From, DateOnly To, int? HomestayId = null, int? AreaId = null);
public record BookingBreakdown(BookingStatus Status, SourceChannel Channel, int Count);
public record BookingReport(int Total, IReadOnlyList<BookingBreakdown> Breakdown, int DepositedCohort,
    int CustomerCancelled, double CancellationPercent, int Rejected, double RejectionPercent, int Expired);
public record FinancialReport(decimal CompletedRoomAmount, decimal Retained, decimal Commission, decimal FujiNet,
    decimal CompensationPaid, bool IsProvisional = true);
public record EmployeePerformance(int EmployeeId, string Code, string? Name, int Created, int Confirmed,
    double ConversionPercent, decimal CompletedRoomAmount, int AssignedOpen, int AssignedFinished);
public record ConfirmationSummary(int HomestayId, int Count, double AverageMinutes, int WithinThirtyMinutes);
public record HomestayIncidentMonth(int HomestayId, int Year, int Month, int Reported, int Replaced);
public record HomestayReport(int HomestayId, string Name, double? Rating, int ReviewCount, double? AverageConfirmMinutes,
    double? ConfirmWithinThirtyPercent, IReadOnlyList<HomestayIncidentMonth> Incidents);
public record ManagementReport(BookingReport Bookings, FinancialReport Finance, IReadOnlyList<EmployeePerformance> Employees,
    IReadOnlyList<HomestayReport> Homestays);

/// <summary>FR-20/X6: tách đường đọc theo quyền; chỉ phần tiền nạp đơn để dùng công thức đối soát chung.</summary>
public class ReportService(ApplicationDbContext db)
{
    private static readonly EmployeePosition[] Managers = [EmployeePosition.Manager, EmployeePosition.Administrator];
    private static readonly BookingStatus[] Finished = [BookingStatus.Completed, BookingStatus.Cancelled, BookingStatus.Rejected, BookingStatus.Expired, BookingStatus.NoShow];
    private static void Validate(ReportQuery q)
    {
        if (q.To < q.From || q.To.DayNumber - q.From.DayNumber >= 366 || q.To == DateOnly.MaxValue || q.From == DateOnly.MinValue)
            throw new BookingException("Khoảng báo cáo phải từ 1 đến 366 ngày.");
    }
    // Dữ liệu lưu UTC; khoảng ngày người dùng nhập gồm cả hai đầu theo giờ Việt Nam.
    private static (DateTime From, DateTime Until) Utc(ReportQuery q) =>
        (VietnamTime.ToUtc(q.From.ToDateTime(TimeOnly.MinValue)),
         VietnamTime.ToUtc(q.To.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    private IQueryable<Booking> Bookings(ReportQuery q)
    {
        var rows = db.Bookings.AsNoTracking();
        if (q.HomestayId is int h) rows = rows.Where(b => b.HomestayId == h);
        if (q.AreaId is int a) rows = rows.Where(b => b.Homestay.AreaId == a);
        return rows;
    }
    private IQueryable<Booking> Created(ReportQuery q)
    { var (utcFrom, until) = Utc(q); return Bookings(q).Where(b => b.CreatedAt >= utcFrom && b.CreatedAt < until); }

    public async Task<ManagementReport> ManagementAsync(ReportQuery q, Actor actor)
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, Managers); Validate(q);
        return new(await BookingCoreAsync(q), await FinancialCoreAsync(q), await EmployeeCoreAsync(q, null), await HomestayCoreAsync(q));
    }
    public async Task<BookingReport> BookingsAsync(ReportQuery q, Actor actor)
    { await PartnerServiceSupport.EmployeeAsync(db, actor, Managers); Validate(q); return await BookingCoreAsync(q); }
    private async Task<BookingReport> BookingCoreAsync(ReportQuery q)
    {
        var rows = Created(q);
        var breakdown = await rows.GroupBy(b => new { b.Status, b.SourceChannel })
            .Select(g => new BookingBreakdown(g.Key.Status, g.Key.SourceChannel, g.Count())).ToListAsync();
        var cohort = rows.Where(b => b.DepositedAt != null && b.ReplacedByBookingId == null);
        var counts = await cohort.GroupBy(b => b.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        var total = counts.Sum(g => g.Count); var cancelled = counts.Where(g => g.Status == BookingStatus.Cancelled).Sum(g => g.Count);
        // Tỷ lệ bị chủ từ chối dùng tổng đơn đã tạo (khác mẫu số tỷ lệ khách hủy đã cọc).
        var all = breakdown.Sum(g => g.Count); var rejected = breakdown.Where(g => g.Status == BookingStatus.Rejected).Sum(g => g.Count);
        return new(all, breakdown, total, cancelled, Percent(cancelled, total), rejected, Percent(rejected, all),
            breakdown.Where(g => g.Status == BookingStatus.Expired).Sum(g => g.Count));
    }

    public async Task<FinancialReport> FinancialAsync(ReportQuery q, Actor actor)
    { await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Accountant, EmployeePosition.Manager, EmployeePosition.Administrator); Validate(q); return await FinancialCoreAsync(q); }
    private async Task<FinancialReport> FinancialCoreAsync(ReportQuery q)
    {
        var bookings = await Bookings(q).Where(b => b.CheckOut >= q.From && b.CheckOut <= q.To).ToListAsync();
        var items = bookings.Select(SettlementCalculator.ItemFor).OfType<SettlementItem>().ToList();
        var (utcFrom, until) = Utc(q);
        var compensation = await db.Payments.AsNoTracking().Where(p => p.Type == PaymentType.Compensation && p.Status == PaymentStatus.Approved
            && p.ReviewedAt >= utcFrom && p.ReviewedAt < until && (q.HomestayId == null || p.Booking.HomestayId == q.HomestayId)
            && (q.AreaId == null || p.Booking.Homestay.AreaId == q.AreaId)).SumAsync(p => (decimal?)p.Amount) ?? 0;
        return new(bookings.Where(b => b.Status == BookingStatus.Completed && b.ReplacedByBookingId == null).Sum(b => b.TotalAmount),
            items.Sum(i => i.RetainedAmount), items.Sum(i => i.CommissionAmount), items.Sum(SettlementCalculator.FujiNetRevenue), compensation);
    }

    public async Task<IReadOnlyList<EmployeePerformance>> EmployeesAsync(ReportQuery q, Actor actor, int? employeeId = null)
    {
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Consultant, EmployeePosition.Manager, EmployeePosition.Administrator);
        Validate(q);
        if (employee.Position == EmployeePosition.Consultant)
        {
            if (employeeId is not null && employeeId != employee.Id) throw new BookingException("Bạn chỉ được xem hiệu suất của chính mình.");
            employeeId = employee.Id;
        }
        return await EmployeeCoreAsync(q, employeeId);
    }
    private async Task<IReadOnlyList<EmployeePerformance>> EmployeeCoreAsync(ReportQuery q, int? employeeId)
    {
        var employees = await db.Employees.AsNoTracking().Where(e => (e.Position == EmployeePosition.Consultant || e.Position == EmployeePosition.Manager)
            && (employeeId == null || e.Id == employeeId)).Select(e => new { e.Id, e.EmployeeCode, e.Person.FullName }).ToListAsync();
        var created = await Created(q).Where(b => b.CreatedByEmployeeId != null && (employeeId == null || b.CreatedByEmployeeId == employeeId))
            .GroupBy(b => b.CreatedByEmployeeId!.Value).Select(g => new { Id = g.Key, Count = g.Count(), Confirmed = g.Count(b => b.ConfirmedAt != null) }).ToListAsync();
        // Doanh số theo ngày trả phòng D7; người tạo luôn là CreatedByEmployeeId (D23).
        var sales = await Bookings(q).Where(b => b.CheckOut >= q.From && b.CheckOut <= q.To && b.Status == BookingStatus.Completed
            && b.ReplacedByBookingId == null && b.CreatedByEmployeeId != null && (employeeId == null || b.CreatedByEmployeeId == employeeId))
            .GroupBy(b => b.CreatedByEmployeeId!.Value).Select(g => new { Id = g.Key, Amount = g.Sum(b => b.TotalAmount) }).ToListAsync();
        // Công việc đã giao thuộc nhóm đơn tạo trong khoảng; không thay người tạo khi giao lại.
        var assigned = await Created(q).Where(b => b.AssignedEmployeeId != null && (employeeId == null || b.AssignedEmployeeId == employeeId))
            .GroupBy(b => b.AssignedEmployeeId!.Value).Select(g => new { Id = g.Key, Count = g.Count(), Done = g.Count(b => Finished.Contains(b.Status)) }).ToListAsync();
        return employees.Select(e =>
        {
            var c = created.SingleOrDefault(g => g.Id == e.Id); var a = assigned.SingleOrDefault(g => g.Id == e.Id);
            return new EmployeePerformance(e.Id, e.EmployeeCode, e.FullName, c?.Count ?? 0, c?.Confirmed ?? 0, Percent(c?.Confirmed ?? 0, c?.Count ?? 0),
                sales.SingleOrDefault(g => g.Id == e.Id)?.Amount ?? 0, (a?.Count ?? 0) - (a?.Done ?? 0), a?.Done ?? 0);
        }).ToList();
    }

    public async Task<IReadOnlyList<HomestayReport>> HomestaysAsync(ReportQuery q, Actor actor)
    { await PartnerServiceSupport.EmployeeAsync(db, actor, Managers); Validate(q); return await HomestayCoreAsync(q); }
    private async Task<IReadOnlyList<HomestayReport>> HomestayCoreAsync(ReportQuery q)
    {
        var homes = await db.Homestays.AsNoTracking().Where(h => (q.HomestayId == null || h.Id == q.HomestayId) && (q.AreaId == null || h.AreaId == q.AreaId))
            .Select(h => new { h.Id, h.Name }).ToListAsync();
        var (utcFrom, until) = Utc(q);
        var ratings = await ReviewRatings.Summaries(db.Reviews.Where(r => r.CreatedAt >= utcFrom && r.CreatedAt < until
            && (q.HomestayId == null || r.HomestayId == q.HomestayId) && (q.AreaId == null || r.Homestay.AreaId == q.AreaId))).ToListAsync();
        var confirmed = Bookings(q).Where(b => b.DepositedAt >= utcFrom && b.DepositedAt < until && b.ConfirmedAt != null);
        List<ConfirmationSummary> confirmations;
        if (db.Database.IsSqlServer())
            confirmations = await confirmed.GroupBy(b => b.HomestayId).Select(g => new ConfirmationSummary(g.Key, g.Count(),
                g.Average(b => (double)EF.Functions.DateDiffSecond(b.DepositedAt!.Value, b.ConfirmedAt!.Value) / 60),
                g.Count(b => b.ConfirmedAt <= b.DepositedAt!.Value.AddMinutes(30)))).ToListAsync();
        else
            // SQLite/InMemory không có DateDiffSecond; Ticks được SQLite dịch ngay tại CSDL.
            confirmations = await confirmed.GroupBy(b => b.HomestayId).Select(g => new ConfirmationSummary(g.Key, g.Count(),
                g.Average(b => (b.ConfirmedAt!.Value.Ticks - b.DepositedAt!.Value.Ticks) / (double)TimeSpan.TicksPerMinute),
                g.Count(b => b.ConfirmedAt <= b.DepositedAt!.Value.AddMinutes(30)))).ToListAsync();
        var reported = await (from a in db.AuditLogs.AsNoTracking()
            join b in Bookings(q) on a.RecordId equals b.Id.ToString()
            where a.TableName == "Bookings" && a.CreatedAt >= utcFrom && a.CreatedAt < until
                && a.NewValues != null && a.NewValues.Contains("\"Step\":\"OverbookingReported\"")
            group a by new { b.HomestayId, a.CreatedAt.AddHours(VietnamTime.OffsetHours).Year, a.CreatedAt.AddHours(VietnamTime.OffsetHours).Month } into g
            select new { g.Key.HomestayId, g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
        var replaced = await Bookings(q).Where(b => b.ReplacedByBookingId != null && b.CancelledAt >= utcFrom && b.CancelledAt < until)
            .GroupBy(b => new { b.HomestayId, b.CancelledAt!.Value.AddHours(VietnamTime.OffsetHours).Year, b.CancelledAt!.Value.AddHours(VietnamTime.OffsetHours).Month })
            .Select(g => new { g.Key.HomestayId, g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
        return homes.Select(h =>
        {
            var rating = ratings.SingleOrDefault(r => r.Id == h.Id); var c = confirmations.SingleOrDefault(c => c.HomestayId == h.Id);
            var months = reported.Where(r => r.HomestayId == h.Id).Select(r => (r.Year, r.Month))
                .Union(replaced.Where(r => r.HomestayId == h.Id).Select(r => (r.Year, r.Month))).OrderBy(m => m.Year).ThenBy(m => m.Month);
            return new HomestayReport(h.Id, h.Name, rating?.Rating, rating?.Count ?? 0, c?.AverageMinutes,
                c is null ? null : Percent(c.WithinThirtyMinutes, c.Count), months.Select(m => new HomestayIncidentMonth(h.Id, m.Year, m.Month,
                    reported.SingleOrDefault(r => r.HomestayId == h.Id && r.Year == m.Year && r.Month == m.Month)?.Count ?? 0,
                    replaced.SingleOrDefault(r => r.HomestayId == h.Id && r.Year == m.Year && r.Month == m.Month)?.Count ?? 0)).ToList());
        }).ToList();
    }
    private static double Percent(int count, int total) => total == 0 ? 0 : 100d * count / total;
}
