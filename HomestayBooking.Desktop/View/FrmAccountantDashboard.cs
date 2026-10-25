using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmAccountantDashboard : Form
{
    private readonly EmployeeSession _session;
    public FrmAccountantDashboard() { InitializeComponent(); _session = new EmployeeSession("KT001", "Kế toán", "Accountant", "", "", DateTime.MaxValue); WireEvents(); }
    public FrmAccountantDashboard(EmployeeSession session) : this()
    {
        _session = session;
        AppSession.Current = session;
        lblEmployee.Text = $"{session.EmployeeCode}\n{session.FullName}";
        ShowScreen(new FrmReceiptReview(session));
    }
    private void WireEvents()
    {
        btnNavReceipts.Click += (_, _) => ShowScreen(new FrmReceiptReview(_session));
        btnNavTransactions.Click += (_, _) => ShowScreen(new FrmBankAndAdditionalDeposit());
        btnNavRefunds.Click += (_, _) => ShowScreen(new FrmRefundAndCompensation());
        btnNavSettlements.Click += (_, _) => ShowScreen(new FrmSettlementManagement());
        btnNavCatalog.Click += (_, _) => ShowScreen(new FrmCatalogLookup(_session));
        btnRefresh.Click += (_, _) => { if (pnlContent.Controls.Count > 0) pnlContent.Controls[0].Refresh(); };
        btnLogout.Click += (_, _) => { AppSession.Current = null; Close(); };
    }
    private void ShowScreen(Form screen)
    {
        foreach (Control control in pnlContent.Controls) control.Dispose();
        pnlContent.Controls.Clear(); screen.TopLevel = false; screen.FormBorderStyle = FormBorderStyle.None; screen.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(screen); screen.Show(); lblHeader.Text = screen.Text;
    }
}
