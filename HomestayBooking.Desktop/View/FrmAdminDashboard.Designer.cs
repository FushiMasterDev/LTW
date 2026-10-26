using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmAdminDashboard
{
    private Guna2Panel pnlSidebar = null!, pnlHeader = null!, pnlContent = null!;
    private Guna2HtmlLabel lblEmployee = null!, lblRole = null!, lblHeader = null!;
    private Guna2Button btnNavReports = null!, btnNavCatalog = null!, btnNavBookings = null!, btnNavCreateBooking = null!, btnNavCustomers = null!, btnNavReceipts = null!, btnNavEmployees = null!, btnNavContracts = null!, btnLogout = null!, btnRefresh = null!;

    private void InitializeComponent()
    {
        pnlSidebar = new Guna2Panel(); pnlHeader = new Guna2Panel(); pnlContent = new Guna2Panel();
        lblEmployee = new Guna2HtmlLabel(); lblRole = new Guna2HtmlLabel(); lblHeader = new Guna2HtmlLabel();
        btnNavReports = new Guna2Button(); btnNavCatalog = new Guna2Button(); btnNavBookings = new Guna2Button(); btnNavCreateBooking = new Guna2Button(); btnNavCustomers = new Guna2Button(); btnNavReceipts = new Guna2Button(); btnNavEmployees = new Guna2Button(); btnNavContracts = new Guna2Button(); btnLogout = new Guna2Button(); btnRefresh = new Guna2Button();
        SuspendLayout();
        pnlSidebar.Dock = DockStyle.Left; pnlSidebar.Width = 250; pnlSidebar.FillColor = Color.FromArgb(30, 58, 47); pnlSidebar.Controls.Add(btnLogout); pnlSidebar.Controls.Add(btnNavContracts); pnlSidebar.Controls.Add(btnNavEmployees); pnlSidebar.Controls.Add(btnNavReceipts); pnlSidebar.Controls.Add(btnNavCustomers); pnlSidebar.Controls.Add(btnNavCreateBooking); pnlSidebar.Controls.Add(btnNavBookings); pnlSidebar.Controls.Add(btnNavCatalog); pnlSidebar.Controls.Add(btnNavReports); pnlSidebar.Controls.Add(lblEmployee); pnlSidebar.Controls.Add(lblRole);
        lblRole.Text = "Phân hệ Quản lý"; lblRole.Location = new Point(18, 26); lblRole.Size = new Size(210, 30); lblRole.ForeColor = Color.White; lblRole.BackColor = Color.Transparent; lblRole.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblEmployee.Text = "Mã NV · Họ tên"; lblEmployee.Location = new Point(18, 70); lblEmployee.Size = new Size(210, 44); lblEmployee.ForeColor = Color.FromArgb(230, 238, 232); lblEmployee.BackColor = Color.Transparent;
        btnNavReports.Text = "Báo cáo & Phân tích"; btnNavReports.Location = new Point(12, 138); btnNavReports.Size = new Size(226, 42); btnNavReports.FillColor = Color.FromArgb(200, 90, 50); btnNavReports.ForeColor = Color.White; btnNavReports.BorderRadius = 12; btnNavReports.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNavCatalog.Text = "Quản lý Danh mục"; btnNavCatalog.Location = new Point(12, 188); btnNavCatalog.Size = new Size(226, 42); btnNavCatalog.FillColor = Color.FromArgb(30, 58, 47); btnNavCatalog.ForeColor = Color.White; btnNavCatalog.BorderRadius = 12;
        btnNavBookings.Text = "Giám sát Đơn đặt"; btnNavBookings.Location = new Point(12, 238); btnNavBookings.Size = new Size(226, 42); btnNavBookings.FillColor = Color.FromArgb(30, 58, 47); btnNavBookings.ForeColor = Color.White; btnNavBookings.BorderRadius = 12;
        btnNavCreateBooking.Text = "Đặt hộ khách hàng"; btnNavCreateBooking.Location = new Point(12, 288); btnNavCreateBooking.Size = new Size(226, 42); btnNavCreateBooking.FillColor = Color.FromArgb(30, 58, 47); btnNavCreateBooking.ForeColor = Color.White; btnNavCreateBooking.BorderRadius = 12;
        btnNavCustomers.Text = "Hồ sơ Khách hàng"; btnNavCustomers.Location = new Point(12, 338); btnNavCustomers.Size = new Size(226, 42); btnNavCustomers.FillColor = Color.FromArgb(30, 58, 47); btnNavCustomers.ForeColor = Color.White; btnNavCustomers.BorderRadius = 12;
        btnNavReceipts.Text = "Kiểm tra Bill Cọc"; btnNavReceipts.Location = new Point(12, 388); btnNavReceipts.Size = new Size(226, 42); btnNavReceipts.FillColor = Color.FromArgb(30, 58, 47); btnNavReceipts.ForeColor = Color.White; btnNavReceipts.BorderRadius = 12;
        btnNavEmployees.Text = "Quản trị Nhân sự"; btnNavEmployees.Location = new Point(12, 438); btnNavEmployees.Size = new Size(226, 42); btnNavEmployees.FillColor = Color.FromArgb(30, 58, 47); btnNavEmployees.ForeColor = Color.White; btnNavEmployees.BorderRadius = 12;
        btnNavContracts.Text = "Hợp đồng & Phê duyệt"; btnNavContracts.Location = new Point(12, 488); btnNavContracts.Size = new Size(226, 42); btnNavContracts.FillColor = Color.FromArgb(30, 58, 47); btnNavContracts.ForeColor = Color.White; btnNavContracts.BorderRadius = 12;
        btnLogout.Text = "Đăng xuất"; btnLogout.Dock = DockStyle.Bottom; btnLogout.Height = 48; btnLogout.FillColor = Color.FromArgb(200, 90, 50); btnLogout.ForeColor = Color.White; btnLogout.BorderRadius = 12;
        pnlHeader.Dock = DockStyle.Top; pnlHeader.Height = 60; pnlHeader.FillColor = Color.FromArgb(250, 247, 242); pnlHeader.Controls.Add(btnRefresh); pnlHeader.Controls.Add(lblHeader);
        lblHeader.Text = "Báo cáo & Phân tích"; lblHeader.Location = new Point(22, 15); lblHeader.Size = new Size(600, 32); lblHeader.ForeColor = Color.FromArgb(30, 58, 47); lblHeader.BackColor = Color.Transparent; lblHeader.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        btnRefresh.Text = "Làm mới"; btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnRefresh.Location = new Point(1010, 10); btnRefresh.Size = new Size(126, 40); btnRefresh.FillColor = Color.FromArgb(30, 58, 47); btnRefresh.ForeColor = Color.White; btnRefresh.BorderRadius = 20;
        pnlContent.Dock = DockStyle.Fill; pnlContent.FillColor = Color.FromArgb(250, 247, 242);
        Controls.Add(pnlContent); Controls.Add(pnlHeader); Controls.Add(pnlSidebar);
        AutoScaleDimensions = new SizeF(8F, 20F); AutoScaleMode = AutoScaleMode.Font; ClientSize = new Size(1400, 850); MinimumSize = new Size(1100, 700); StartPosition = FormStartPosition.CenterScreen; Text = "Quản lý & Quản trị | Fuji Homestay"; BackColor = Color.FromArgb(250, 247, 242); ResumeLayout(false);
    }
}
