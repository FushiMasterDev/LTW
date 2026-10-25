#nullable enable
using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
partial class FrmEmployeeHome
{
    private System.ComponentModel.IContainer? components; private Guna2Panel pnlSidebar = null!; private Guna2Panel pnlHeader = null!; private Guna2Panel pnlWorkspace = null!; private Label lblEmployee = null!; private Label lblBrand = null!; private Label lblHeader = null!; private Guna2Button btnBookings = null!; private Guna2Button btnCreateBooking = null!; private Guna2Button btnCustomers = null!; private Guna2Button btnLogout = null!;
    protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container(); pnlSidebar = new Guna2Panel(); pnlHeader = new Guna2Panel(); pnlWorkspace = new Guna2Panel(); lblEmployee = new Label(); lblBrand = new Label(); lblHeader = new Label(); btnBookings = new Guna2Button(); btnCreateBooking = new Guna2Button(); btnCustomers = new Guna2Button(); btnLogout = new Guna2Button(); pnlSidebar.SuspendLayout(); pnlHeader.SuspendLayout(); SuspendLayout();
        pnlSidebar.Dock = DockStyle.Left; pnlSidebar.FillColor = Color.FromArgb(30,58,47); pnlSidebar.Size = new Size(245,760); lblBrand.Text="FUJI HOMESTAY";lblBrand.Location=new Point(26,30);lblBrand.Size=new Size(200,34);lblBrand.ForeColor=Color.White;lblBrand.Font=new Font("Segoe UI",15,FontStyle.Bold);pnlSidebar.Controls.Add(lblBrand);
        btnBookings.BorderRadius=8; btnBookings.FillColor=Color.FromArgb(19,132,111); btnBookings.ForeColor=Color.White; btnBookings.Font=new Font("Segoe UI",9,FontStyle.Bold); btnBookings.Location=new Point(16,134); btnBookings.Size=new Size(213,46); btnBookings.Text="Quản lý đơn & lịch sử"; btnBookings.Click+=btnBookings_Click;
        btnCreateBooking.BorderRadius=8; btnCreateBooking.FillColor=Color.FromArgb(30,58,47); btnCreateBooking.ForeColor=Color.White; btnCreateBooking.Font=new Font("Segoe UI",9); btnCreateBooking.Location=new Point(16,190); btnCreateBooking.Size=new Size(213,46); btnCreateBooking.Text="Tra cứu & đặt phòng hộ";btnCreateBooking.HoverState.FillColor=Color.FromArgb(19,132,111); btnCreateBooking.Click+=btnCreateBooking_Click;
        btnCustomers.BorderRadius=8; btnCustomers.FillColor=Color.FromArgb(30,58,47); btnCustomers.ForeColor=Color.White; btnCustomers.Font=new Font("Segoe UI",9); btnCustomers.Location=new Point(16,246); btnCustomers.Size=new Size(213,46); btnCustomers.Text="Hồ sơ khách hàng";btnCustomers.HoverState.FillColor=Color.FromArgb(19,132,111); btnCustomers.Click+=btnCustomers_Click;
        lblEmployee.ForeColor=Color.FromArgb(220,235,234); lblEmployee.Font=new Font("Segoe UI",9); lblEmployee.Location=new Point(24,620); lblEmployee.Size=new Size(200,54); lblEmployee.Text="Nhân viên";
        btnLogout.BorderRadius=8; btnLogout.FillColor=Color.FromArgb(30,58,47); btnLogout.ForeColor=Color.White; btnLogout.Location=new Point(16,690); btnLogout.Size=new Size(213,42); btnLogout.Text="Đăng xuất";btnLogout.HoverState.FillColor=Color.FromArgb(19,132,111); btnLogout.Click+=btnLogout_Click; pnlSidebar.Controls.Add(btnBookings); pnlSidebar.Controls.Add(btnCreateBooking); pnlSidebar.Controls.Add(btnCustomers); pnlSidebar.Controls.Add(lblEmployee); pnlSidebar.Controls.Add(btnLogout);
        pnlHeader.Dock=DockStyle.Top; pnlHeader.FillColor=Color.White; pnlHeader.Size=new Size(935,68);lblHeader.Text="Không gian làm việc nhân viên";lblHeader.Location=new Point(26,21);lblHeader.Size=new Size(500,28);lblHeader.ForeColor=Color.FromArgb(38,61,69);lblHeader.Font=new Font("Segoe UI",12,FontStyle.Bold);pnlHeader.Controls.Add(lblHeader); pnlWorkspace.Dock=DockStyle.Fill; pnlWorkspace.FillColor=Color.FromArgb(243,247,246);
        AutoScaleMode=AutoScaleMode.Font; BackColor=Color.FromArgb(243,247,246); ClientSize=new Size(1180,760); Controls.Add(pnlWorkspace); Controls.Add(pnlHeader); Controls.Add(pnlSidebar); MinimumSize=new Size(1000,650); StartPosition=FormStartPosition.CenterScreen; Text="Fuji Homestay | Nhân viên"; pnlSidebar.ResumeLayout(false); pnlHeader.ResumeLayout(false); ResumeLayout(false);
    }
}
partial class RolePlaceholderForm
{
    private System.ComponentModel.IContainer? components; private Label lblMessage=null!; protected override void Dispose(bool disposing) { if(disposing) components?.Dispose(); base.Dispose(disposing); }
    private void InitializeComponent() { components=new System.ComponentModel.Container(); lblMessage=new Label(); SuspendLayout(); lblMessage.Dock=DockStyle.Fill; lblMessage.Font=new Font("Segoe UI",18,FontStyle.Bold); lblMessage.TextAlign=ContentAlignment.MiddleCenter; lblMessage.Text="Khu vực nhân viên"; AutoScaleMode=AutoScaleMode.Font; BackColor=Color.FromArgb(243,247,246); ClientSize=new Size(800,450); Controls.Add(lblMessage); Text="Khu vực nhân viên"; ResumeLayout(false); }
}




