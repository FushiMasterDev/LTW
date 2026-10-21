using HomestayBooking.Web.Services;

namespace HomestayBooking.Web.DesktopApi;

public record BookingBreakdownDto(string Status, string Channel, int Count);
public record BookingReportDto(int Total, IReadOnlyList<BookingBreakdownDto> Breakdown, int DepositedCohort,
    int CustomerCancelled, double CancellationPercent, int Rejected, double RejectionPercent, int Expired);
public record FinancialReportDto(decimal CompletedRoomAmount, decimal Retained, decimal Commission, decimal FujiNet, decimal CompensationPaid, bool IsProvisional);
public record EmployeePerformanceDto(int EmployeeId, string Code, string? Name, int Created, int Confirmed,
    double ConversionPercent, decimal CompletedRoomAmount, int AssignedOpen, int AssignedFinished);
public record HomestayIncidentDto(int HomestayId, int Year, int Month, int Reported, int Replaced);
public record HomestayReportDto(int HomestayId, string Name, double? Rating, int ReviewCount, double? AverageConfirmMinutes,
    double? ConfirmWithinThirtyPercent, IReadOnlyList<HomestayIncidentDto> Incidents);
public record ManagementReportDto(BookingReportDto Bookings, FinancialReportDto Finance, IReadOnlyList<EmployeePerformanceDto> Employees,
    IReadOnlyList<HomestayReportDto> Homestays);

public static class DesktopReportMapping
{
    public static BookingReportDto Bookings(BookingReport r) => new(r.Total,
        r.Breakdown.Select(b => new BookingBreakdownDto(b.Status.ToString(), b.Channel.ToString(), b.Count)).ToList(),
        r.DepositedCohort, r.CustomerCancelled, r.CancellationPercent, r.Rejected, r.RejectionPercent, r.Expired);
    public static FinancialReportDto Finance(FinancialReport r) => new(r.CompletedRoomAmount, r.Retained, r.Commission, r.FujiNet, r.CompensationPaid, r.IsProvisional);
    public static EmployeePerformanceDto Employee(EmployeePerformance e) => new(e.EmployeeId, e.Code, e.Name, e.Created, e.Confirmed,
        e.ConversionPercent, e.CompletedRoomAmount, e.AssignedOpen, e.AssignedFinished);
    public static HomestayReportDto Homestay(HomestayReport h) => new(h.HomestayId, h.Name, h.Rating, h.ReviewCount,
        h.AverageConfirmMinutes, h.ConfirmWithinThirtyPercent,
        h.Incidents.Select(i => new HomestayIncidentDto(i.HomestayId, i.Year, i.Month, i.Reported, i.Replaced)).ToList());
}
