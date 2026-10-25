using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmEmployeeHome : Form
{
    private EmployeeSession? _session;
    public FrmEmployeeHome() { InitializeComponent(); }
    public FrmEmployeeHome(EmployeeSession session) : this()
    {
        _session = session;
        AppSession.Current = session;
        lblEmployee.Text = $"{session.FullName}\n{session.EmployeeCode} · {RoleName(session.Position)}";
        btnBookings.Visible = session.Position == "Consultant";
        btnCreateBooking.Visible = session.Position == "Consultant";
        btnCustomers.Visible = session.Position == "Consultant";
        if (session.Position == "Consultant") ShowScreen(new FrmBookingManagement());
        else ShowScreen(new RolePlaceholderForm(session.Position));
    }
    private void ShowScreen(Form screen)
    {
        foreach (Control existing in pnlWorkspace.Controls) existing.Dispose();
        pnlWorkspace.Controls.Clear(); screen.TopLevel = false; screen.FormBorderStyle = FormBorderStyle.None; screen.Dock = DockStyle.Fill;
        pnlWorkspace.Controls.Add(screen); screen.Show();
    }
    private void btnBookings_Click(object? sender, EventArgs e) => ShowScreen(new FrmBookingManagement());
    private void btnCreateBooking_Click(object? sender, EventArgs e) => ShowScreen(new FrmCreateBooking());
    private void btnCustomers_Click(object? sender, EventArgs e) => ShowScreen(new FrmCustomerManagement());
    private void btnLogout_Click(object? sender, EventArgs e) { AppSession.Current = null; Close(); }
    private static string RoleName(string p) => p switch { "Consultant" => "Nhân viên tư vấn", "Accountant" => "Kế toán", "Manager" => "Quản lý", "Administrator" => "Quản trị viên", _ => "Nhân viên" };
}

internal sealed partial class RolePlaceholderForm : Form
{
    public RolePlaceholderForm() { InitializeComponent(); }
    public RolePlaceholderForm(string position) : this() => lblMessage.Text = $"Khu vực {position} sẽ được triển khai ở phase tiếp theo.";
}
