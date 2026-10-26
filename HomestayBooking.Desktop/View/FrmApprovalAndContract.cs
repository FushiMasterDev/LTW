using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmApprovalAndContract : Form, IAccountantRefreshable
{
    private readonly AdminController _api;
    public FrmApprovalAndContract() : this(AppSession.Current ?? new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }
    public FrmApprovalAndContract(EmployeeSession session)
    {
        InitializeComponent(); _api = new AdminController(session);
        btnApproveChange.Enabled = session.Position == "Manager"; btnRejectChange.Enabled = session.Position == "Manager";
        if (session.Position == "Administrator") lblPolicy.Text = "Quản trị viên chỉ xem; policy ChangeRequestApprove dành riêng cho Manager.";
        btnReload.Click += async (_, _) => await RefreshDataAsync();
        btnApproveChange.Click += async (_, _) => await UnsupportedAsync("duyệt đổi/hủy"); btnRejectChange.Click += async (_, _) => await UnsupportedAsync("từ chối đổi/hủy");
        btnCreateContract.Click += async (_, _) => await UnsupportedAsync("hợp đồng"); btnIssueVoucher.Click += async (_, _) => await UnsupportedAsync("voucher"); btnRevokeVoucher.Click += async (_, _) => await UnsupportedAsync("thu hồi voucher");
        Shown += async (_, _) => await RefreshDataAsync();
    }
    public async Task RefreshDataAsync()
    {
        lblPolicy.Text = "Backend chưa cung cấp endpoint Desktop cho đổi/hủy, hợp đồng, duyệt homestay hoặc voucher.";
        await Task.CompletedTask;
    }
    private async Task UnsupportedAsync(string operation)
    {
        try { await _api.PostAsync<object>("management/unsupported", new { }); }
        catch (Exception ex) { MessageBox.Show(this, $"Không thể {operation}: {ex.Message}", "Fuji Homestay", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }
}
