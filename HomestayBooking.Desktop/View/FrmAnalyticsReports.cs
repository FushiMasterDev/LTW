using System.Text.Json;
using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Models.Admin;

namespace HomestayBooking.Desktop.View;

internal partial class FrmAnalyticsReports : Form, IAccountantRefreshable
{
    private readonly AdminController _api;
    private readonly EmployeeSession _session;

    public FrmAnalyticsReports() : this(AppSession.Current ?? new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }
    public FrmAnalyticsReports(EmployeeSession session)
    {
        InitializeComponent();
        _session = session;
        _api = new AdminController(session);
        dtpFrom.Value = DateTime.Today.AddDays(-29);
        dtpTo.Value = DateTime.Today;
        btnLoad.Click += async (_, _) => await RefreshDataAsync();
        Shown += async (_, _) => { await LoadAreasAsync(); await RefreshDataAsync(); };
    }

    public async Task RefreshDataAsync()
    {
        var from = dtpFrom.Value.Date;
        var to = dtpTo.Value.Date;
        if (to < from) { MessageBox.Show(this, "Đến ngày phải bằng hoặc sau Từ ngày."); return; }
        if ((to - from).TotalDays > 366) { MessageBox.Show(this, "Khoảng báo cáo tối đa là 366 ngày."); return; }
        var areaId = cboArea.SelectedValue?.ToString();
        var query = $"from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" + (int.TryParse(areaId, out var id) && id > 0 ? $"&areaId={id}" : "");
        try
        {
            var report = await _api.GetAsync<JsonElement>($"reports/management?{query}");
            RenderReport(report);
            lblReportState.Text = $"Dữ liệu từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}";
        }
        catch (Exception ex) { lblReportState.Text = ex.Message; }
    }

    private async Task LoadAreasAsync()
    {
        try
        {
            var rows = await _api.GetAllAsync<AdminAreaDto>("areas");
            cboArea.DataSource = new[] { new AdminAreaDto(0, "Tất cả khu vực", "", "") }.Concat(rows).ToList();
            cboArea.DisplayMember = nameof(AdminAreaDto.Name); cboArea.ValueMember = nameof(AdminAreaDto.Id);
        }
        catch { cboArea.Items.Add("Tất cả khu vực"); cboArea.SelectedIndex = 0; }
    }

    private void RenderReport(JsonElement report)
    {
        var bookings = report.GetProperty("bookings"); var finance = report.GetProperty("finance");
        lblBookings.Text = $"{Get(bookings, "total")}\nTổng đơn";
        lblDeposited.Text = $"{Get(bookings, "depositedCohort")}\nĐơn đã cọc";
        lblRevenue.Text = $"{Money(finance, "completedRoomAmount")} đ\nDoanh thu phòng hoàn tất";
        lblNet.Text = $"{Money(finance, "fujiNet")} đ\nDoanh thu ròng Fuji";
        dgvBreakdown.DataSource = report.GetProperty("bookings").GetProperty("breakdown").EnumerateArray().Select(e => new { Trạng_thái = Get(e, "status"), Kênh = Get(e, "channel"), Số_lượng = Get(e, "count") }).ToList();
        dgvEmployees.DataSource = report.GetProperty("employees").EnumerateArray().Select(e => new { Mã_NV = Get(e, "code"), Họ_tên = Get(e, "name"), Đơn_tạo = Get(e, "created"), Đã_xác_nhận = Get(e, "confirmed"), Tỷ_lệ_chốt = Get(e, "conversionPercent"), Đang_xử_lý = Get(e, "assignedOpen") }).ToList();
        dgvHomestays.DataSource = report.GetProperty("homestays").EnumerateArray().Select(e => new { Homestay = Get(e, "name"), Đánh_giá = Get(e, "rating"), Lượt_đánh_giá = Get(e, "reviewCount") }).ToList();
    }
    private static string Get(JsonElement e, string key) => e.TryGetProperty(key, out var v) ? v.ToString() : "—";
    private static string Money(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.TryGetDecimal(out var n) ? n.ToString("N0") : "0";
}
