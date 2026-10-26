using System.Text.Json;
using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmEmployeeAccountAdmin : Form, IAccountantRefreshable
{
    private readonly AdminController _api;
    private readonly bool _canManage;
    public FrmEmployeeAccountAdmin() : this(AppSession.Current ?? new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }
    public FrmEmployeeAccountAdmin(EmployeeSession session)
    {
        InitializeComponent(); _api = new AdminController(session); _canManage = session.Position == "Administrator";
        pnlRightActions.Enabled = _canManage; lblAccessNote.Visible = !_canManage;
        cboFilterPosition.Items.AddRange(new object[] { "All", "Consultant", "Accountant", "Manager", "Administrator" }); cboFilterPosition.SelectedIndex = 0;
        cboFilterStatus.Items.AddRange(new object[] { "All", "Active", "Suspended", "Resigned" }); cboFilterStatus.SelectedIndex = 0;
        btnSearch.Click += async (_, _) => await RefreshDataAsync(); btnCreate.Click += (_, _) => Unsupported(); btnUpdate.Click += (_, _) => Unsupported(); btnToggleLock.Click += (_, _) => Unsupported(); btnResetPassword.Click += (_, _) => Unsupported();
        Shown += async (_, _) => await RefreshDataAsync();
    }
    public async Task RefreshDataAsync()
    {
        try { var rows = await _api.GetAsync<JsonElement>("employees"); dgvEmployees.DataSource = rows.ValueKind == JsonValueKind.Array ? rows.EnumerateArray().Select(e => JsonSerializer.Deserialize<Dictionary<string, object?>>(e.GetRawText())!).ToList() : null; }
        catch (Exception ex) { lblStatus.Text = ex.Message; }
    }
    private void Unsupported() => MessageBox.Show(this, "Backend hiện chưa cung cấp API quản trị tài khoản nhân sự cho Desktop. Không có thay đổi nào được lưu.", "Chưa hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
