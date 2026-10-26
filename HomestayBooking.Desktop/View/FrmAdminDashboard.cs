using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmAdminDashboard : Form
{
    private readonly EmployeeSession _session;

    public FrmAdminDashboard() : this(new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }

    public FrmAdminDashboard(EmployeeSession session)
    {
        InitializeComponent();
        _session = session;
        AppSession.Current = session;
        lblEmployee.Text = $"{session.EmployeeCode}\n{session.FullName}";
        lblRole.Text = session.Position == "Manager" ? "Phân hệ Quản lý" : "Phân hệ Quản trị viên";
        btnNavEmployees.Visible = session.Position == "Administrator";
        ShowScreen(new FrmAnalyticsReports(session));
        btnNavReports.Click += (_, _) => ShowScreen(new FrmAnalyticsReports(_session));
        btnNavCatalog.Click += (_, _) => ShowScreen(new FrmCatalogManagement(_session));
        btnNavBookings.Click += (_, _) => ShowScreen(new FrmBookingManagement());
        btnNavCreateBooking.Click += (_, _) => ShowScreen(new FrmCreateBooking());
        btnNavCustomers.Click += (_, _) => ShowScreen(new FrmCustomerManagement());
        btnNavReceipts.Click += (_, _) => ShowScreen(new FrmReceiptReview(_session, session.Position == "Manager"));
        btnNavEmployees.Click += (_, _) => ShowScreen(new FrmEmployeeAccountAdmin(_session));
        btnNavContracts.Click += (_, _) => ShowScreen(new FrmApprovalAndContract(_session));
        btnRefresh.Click += async (_, _) =>
        {
            if (pnlContent.Controls.Count > 0 && pnlContent.Controls[0] is IAccountantRefreshable refreshable)
                await refreshable.RefreshDataAsync();
        };
        btnLogout.Click += (_, _) => { AppSession.Current = null; Close(); };
    }

    private void ShowScreen(Form screen)
    {
        foreach (Control control in pnlContent.Controls) control.Dispose();
        pnlContent.Controls.Clear();
        screen.TopLevel = false;
        screen.FormBorderStyle = FormBorderStyle.None;
        screen.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(screen);
        screen.Show();
        lblHeader.Text = screen.Text;
        btnNavReports.FillColor = screen is FrmAnalyticsReports ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavCatalog.FillColor = screen is FrmCatalogManagement ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavBookings.FillColor = screen is FrmBookingManagement ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavCustomers.FillColor = screen is FrmCustomerManagement ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavReceipts.FillColor = screen is FrmReceiptReview ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavEmployees.FillColor = screen is FrmEmployeeAccountAdmin ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
        btnNavContracts.FillColor = screen is FrmApprovalAndContract ? Color.FromArgb(200, 90, 50) : Color.FromArgb(30, 58, 47);
    }
}
