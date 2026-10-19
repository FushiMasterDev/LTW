using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Models.ViewModels.Ui;

namespace HomestayBooking.Web.Models.ViewModels;

public class ContractCreateForm
{
    [Display(Name = "Mã homestay"), Range(1, int.MaxValue)] public int HomestayId { get; set; }
    [Display(Name = "Số hợp đồng"), Required, StringLength(30)] public string ContractNo { get; set; } = "";
    [Display(Name = "Ngày bắt đầu"), DataType(DataType.Date)] public DateOnly StartDate { get; set; }
    [Display(Name = "Ngày kết thúc"), DataType(DataType.Date)] public DateOnly EndDate { get; set; }
    [Display(Name = "Hoa hồng (%)"), Range(10, 15)] public decimal CommissionPercent { get; set; } = 10;
}
public record AdminContractList(PaginatedList<ContractRow> Rows, DateOnly Today);
public record AdminContractDetails(ContractDetails Details, DateOnly Today);
public record AdminReviewDetails(HomestayEditor Content, PendingHomestayDetails? Pending);
public record AdminReportModel(ReportQuery Query, string Tab, BookingReport? Bookings = null, FinancialReport? Finance = null,
    IReadOnlyList<EmployeePerformance>? Employees = null, IReadOnlyList<HomestayReport>? Homestays = null, IReadOnlyList<AdminReportMonth>? Months = null);
public record AdminReportMonth(DateOnly From, DateOnly To, int? Bookings, decimal? NetRevenue);
public record AdminAccountList(PaginatedList<AccountSummary> Rows, EmployeePosition? Position);
public class EmployeeCreateForm
{
    [Required, StringLength(20), Display(Name = "Mã nhân viên")] public string EmployeeCode { get; set; } = "";
    [Required, StringLength(100), Display(Name = "Họ tên")] public string FullName { get; set; } = "";
    [Required, Phone, Display(Name = "Số điện thoại")] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(256), Display(Name = "Email")] public string? Email { get; set; }
    [Display(Name = "Chức vụ")] public EmployeePosition Position { get; set; }
    [StringLength(100), Display(Name = "Phòng ban")] public string? Department { get; set; }
}
public record AdminDashboardModel(ReportQuery Query, ManagementReport Report, PaginatedList<ContractRow> Expiring,
    int PendingContent, int PendingRegistrations);

/// <summary>Canvas và bảng thay thế dùng cùng dữ liệu đã nạp. Labels/Series chỉ phục vụ trình bày, không tính nghiệp vụ.</summary>
public record ReportChartModel(string Id, string Title, string Type, IReadOnlyList<string> Labels,
    IReadOnlyList<ReportChartSeries> Series, DataTableModel Table);
public record ReportChartSeries(string Label, IReadOnlyList<double?> Values, string Tone = "accent");

public record TemporaryEmployeePassword(string Target, string Password);

public static class AdminLabels
{
    public static string Label(Enum value) => value switch
    {
        ContractStatus.Active => "Đang hiệu lực", ContractStatus.Expired => "Đã hết hạn", ContractStatus.Terminated => "Đã chấm dứt",
        AccountStatus.PendingActivation => "Chờ kích hoạt", AccountStatus.Active => "Đang hoạt động", AccountStatus.Locked => "Đã khóa", AccountStatus.Disabled => "Ngừng sử dụng",
        EmployeePosition.Consultant => "Tư vấn", EmployeePosition.Accountant => "Kế toán", EmployeePosition.Manager => "Quản lý", EmployeePosition.Administrator => "Quản trị viên",
        EmploymentStatus.Active => "Đang làm việc", EmploymentStatus.Suspended => "Tạm ngừng làm việc", EmploymentStatus.Resigned => "Đã nghỉ việc",
        _ => value.ToString()
    };
}
