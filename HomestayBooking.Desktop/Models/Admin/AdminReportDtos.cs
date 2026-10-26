namespace HomestayBooking.Desktop.Models.Admin;

internal sealed record ReportBookingBreakdown(string Status, int Count);
internal sealed record AdminBookingReport(int Total, IReadOnlyList<ReportBookingBreakdown> Breakdown, int DepositedCohort, int DepositedConfirmed);
internal sealed record AdminFinancialReport(decimal CompletedRoomAmount, decimal Retained, decimal Commission, decimal FujiNet, decimal Compensation);
internal sealed record EmployeePerformance(int EmployeeId, string EmployeeCode, string FullName, int Assigned, int Confirmed, int Cancelled);
internal sealed record AdminHomestayReport(int HomestayId, string Name, double? Rating, int ReviewCount, double? AverageConfirmMinutes);
internal sealed record AdminManagementReport(AdminBookingReport Bookings, AdminFinancialReport Finance,
    IReadOnlyList<EmployeePerformance> Employees, IReadOnlyList<AdminHomestayReport> Homestays);
internal sealed record AdminAreaDto(int Id, string Name, string Province, string Group);
internal sealed record AdminCatalogItemDto(int Id, string Name);
internal sealed record AdminHolidayDto(string Date, string Name);
