using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Properties;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomestayBooking.Desktop.View;

public partial class FrmLogin : Form
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };

    public FrmLogin()
    {
        InitializeComponent();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLogin));
        BackgroundImage = (Image?)resources.GetObject("$this.BackgroundImage")!;
        chkRemember.Checked = !string.IsNullOrEmpty(Properties.Settings.Default.RememberEmployeeCode);
        txtEmployeeCode.Text = Properties.Settings.Default.RememberEmployeeCode;
        btnLogin.Click += async (_, _) => await LoginAsync();
        btnForgot.Click += (_, _) => OpenForgotPassword();
        btnShowPassword.Click += (_, _) => TogglePassword();
        AcceptButton = btnLogin;
        Shown += (_, _) => txtEmployeeCode.Focus();
    }

    private async Task LoginAsync()
    {
        var code = txtEmployeeCode.Text.Trim();
        var password = txtPassword.Text;
        if (code.Length == 0 || password.Length == 0)
        {
            ShowMessage("Vui lòng nhập mã nhân viên và mật khẩu.");
            return;
        }

        SetBusy(true);
        try
        {
            var baseUrl = DesktopSettings.ApiBaseUrl.TrimEnd('/');
            using var response = await Client.PostAsJsonAsync($"{baseUrl}/api/desktop/auth/login", new { employeeCode = code, password });
            if (!response.IsSuccessStatusCode)
            {
                ShowMessage(DesktopApi.ErrorMessage(response.StatusCode, await response.Content.ReadAsStringAsync()));
                return;
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (login?.Employee is null) { ShowMessage("Máy chủ trả về dữ liệu đăng nhập không hợp lệ."); return; }
            var session = new EmployeeSession(login.Employee.Code, login.Employee.FullName ?? login.Employee.Code,
                login.Employee.Position, "", login.AccessToken, login.ExpiresAt);
            if (!string.Equals(session.Position, "Consultant", StringComparison.Ordinal)
                && !string.Equals(session.Position, "Accountant", StringComparison.Ordinal)
                && !string.Equals(session.Position, "Manager", StringComparison.Ordinal)
                && !string.Equals(session.Position, "Administrator", StringComparison.Ordinal))
            {
                ShowMessage("Chức vụ này chưa được hỗ trợ trên ứng dụng desktop.");
                return;
            }
            Properties.Settings.Default.RememberEmployeeCode = chkRemember.Checked ? code : string.Empty;
            Properties.Settings.Default.Save();
            Hide();
            using Form home = session.Position switch
            {
                "Accountant" => new FrmAccountantDashboard(session),
                "Manager" or "Administrator" => new FrmAdminDashboard(session),
                _ => new FrmEmployeeHome(session)
            };
            home.ShowDialog(this);
            txtPassword.Clear();
            Show();
        }
        catch (HttpRequestException)
        {
            ShowMessage("Không kết nối được máy chủ. Hãy kiểm tra địa chỉ API và mạng.");
        }
        catch (TaskCanceledException)
        {
            ShowMessage("Máy chủ phản hồi quá lâu. Vui lòng thử lại.");
        }
        catch (JsonException)
        {
            ShowMessage("Phản hồi đăng nhập từ máy chủ không hợp lệ.");
        }
        finally { SetBusy(false); }
    }

    // Nhân viên không tự đặt lại mật khẩu; quản trị viên cấp lại trên web.
    private void OpenForgotPassword() => ShowMessage("Vui lòng liên hệ quản trị viên hệ thống để được hỗ trợ đặt lại mật khẩu.");

    private void TogglePassword()
    {
        txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
        btnShowPassword.Text = txtPassword.UseSystemPasswordChar ? "Hiện" : "Ẩn";
    }

    private void SetBusy(bool busy)
    {
        btnLogin.Enabled = !busy;
        btnForgot.Enabled = !busy;
        txtEmployeeCode.Enabled = !busy;
        txtPassword.Enabled = !busy;
        btnLogin.Text = busy ? "ĐANG XỬ LÝ..." : "ĐĂNG NHẬP";
        UseWaitCursor = busy;
    }

    private void ShowMessage(string message) => MessageBox.Show(this, message, "Fuji Homestay", MessageBoxButtons.OK,
        message.StartsWith("Vui lòng liên hệ quản trị viên", StringComparison.Ordinal) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

    private void FrmLogin_Load(object? sender, EventArgs e)
    {

    }

    private void guna2HtmlLabel1_Click(object? sender, EventArgs e)
    {

    }
}

internal sealed record LoginEmployee(int Id, string Code, string? FullName, string Position);
internal sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, LoginEmployee Employee);
