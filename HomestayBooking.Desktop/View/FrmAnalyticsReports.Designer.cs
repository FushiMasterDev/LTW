using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmAnalyticsReports
{
    private Guna2Panel pnlFilter = null!;
    private Guna2DateTimePicker dtpFrom = null!, dtpTo = null!;
    private Guna2ComboBox cboArea = null!;
    private Guna2Button btnLoad = null!;
    private Guna2HtmlLabel lblReportState = null!, lblBookings = null!, lblDeposited = null!, lblRevenue = null!, lblNet = null!, lblBreakdown = null!, lblEmployees = null!, lblHomestays = null!;
    private Guna2DataGridView dgvBreakdown = null!, dgvEmployees = null!, dgvHomestays = null!;
    private void InitializeComponent()
    {
        pnlFilter = new Guna2Panel(); dtpFrom = new Guna2DateTimePicker(); dtpTo = new Guna2DateTimePicker(); cboArea = new Guna2ComboBox(); btnLoad = new Guna2Button();
        lblReportState = new Guna2HtmlLabel(); lblBookings = new Guna2HtmlLabel(); lblDeposited = new Guna2HtmlLabel(); lblRevenue = new Guna2HtmlLabel(); lblNet = new Guna2HtmlLabel(); lblBreakdown = new Guna2HtmlLabel(); lblEmployees = new Guna2HtmlLabel(); lblHomestays = new Guna2HtmlLabel();
        dgvBreakdown = new Guna2DataGridView(); dgvEmployees = new Guna2DataGridView(); dgvHomestays = new Guna2DataGridView(); SuspendLayout();
        pnlFilter.Location = new Point(18, 18); pnlFilter.Size = new Size(1160, 68); pnlFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlFilter.FillColor = Color.White; pnlFilter.BorderRadius = 15;
        dtpFrom.Location = new Point(14, 15); dtpFrom.Size = new Size(190, 38); dtpFrom.BorderRadius = 10; dtpFrom.Format = DateTimePickerFormat.Short; dtpFrom.Font = new Font("Segoe UI", 9F);
        dtpTo.Location = new Point(214, 15); dtpTo.Size = new Size(190, 38); dtpTo.BorderRadius = 10; dtpTo.Format = DateTimePickerFormat.Short; dtpTo.Font = new Font("Segoe UI", 9F);
        cboArea.Location = new Point(414, 15); cboArea.Size = new Size(260, 38); cboArea.BorderRadius = 10; cboArea.Font = new Font("Segoe UI", 9F); cboArea.ItemHeight = 28;
        btnLoad.Text = "Xem báo cáo"; btnLoad.Location = new Point(690, 14); btnLoad.Size = new Size(150, 40); btnLoad.FillColor = Color.FromArgb(200, 90, 50); btnLoad.ForeColor = Color.White; btnLoad.BorderRadius = 20; btnLoad.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblReportState.Location = new Point(850, 22); lblReportState.Size = new Size(290, 28); lblReportState.BackColor = Color.Transparent; lblReportState.ForeColor = Color.FromArgb(30, 58, 47);
        pnlFilter.Controls.Add(dtpFrom); pnlFilter.Controls.Add(dtpTo); pnlFilter.Controls.Add(cboArea); pnlFilter.Controls.Add(btnLoad); pnlFilter.Controls.Add(lblReportState);
        lblBookings.Location = new Point(18, 102); lblBookings.Size = new Size(270, 76); lblBookings.BackColor = Color.White; lblBookings.ForeColor = Color.FromArgb(30, 58, 47); lblBookings.Font = new Font("Segoe UI", 14F, FontStyle.Bold); lblBookings.BorderStyle = BorderStyle.FixedSingle;
        lblDeposited.Location = new Point(308, 102); lblDeposited.Size = new Size(270, 76); lblDeposited.BackColor = Color.White; lblDeposited.ForeColor = Color.FromArgb(30, 58, 47); lblDeposited.Font = new Font("Segoe UI", 14F, FontStyle.Bold); lblDeposited.BorderStyle = BorderStyle.FixedSingle;
        lblRevenue.Location = new Point(598, 102); lblRevenue.Size = new Size(270, 76); lblRevenue.BackColor = Color.White; lblRevenue.ForeColor = Color.FromArgb(30, 58, 47); lblRevenue.Font = new Font("Segoe UI", 14F, FontStyle.Bold); lblRevenue.BorderStyle = BorderStyle.FixedSingle;
        lblNet.Location = new Point(888, 102); lblNet.Size = new Size(290, 76); lblNet.BackColor = Color.White; lblNet.ForeColor = Color.FromArgb(200, 90, 50); lblNet.Font = new Font("Segoe UI", 14F, FontStyle.Bold); lblNet.BorderStyle = BorderStyle.FixedSingle;
        lblBreakdown.Text = "Đơn theo trạng thái"; lblBreakdown.Location = new Point(18, 192); lblBreakdown.Size = new Size(500, 28); lblBreakdown.BackColor = Color.Transparent; lblBreakdown.Font = new Font("Segoe UI", 11F, FontStyle.Bold); lblBreakdown.ForeColor = Color.FromArgb(30, 58, 47);
        dgvBreakdown.Location = new Point(18, 222); dgvBreakdown.Size = new Size(520, 210); dgvBreakdown.Anchor = AnchorStyles.Top | AnchorStyles.Left; dgvBreakdown.BackgroundColor = Color.White; dgvBreakdown.BorderStyle = BorderStyle.None; dgvBreakdown.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        lblEmployees.Text = "Hiệu suất nhân viên"; lblEmployees.Location = new Point(558, 192); lblEmployees.Size = new Size(600, 28); lblEmployees.BackColor = Color.Transparent; lblEmployees.Font = new Font("Segoe UI", 11F, FontStyle.Bold); lblEmployees.ForeColor = Color.FromArgb(30, 58, 47);
        dgvEmployees.Location = new Point(558, 222); dgvEmployees.Size = new Size(620, 210); dgvEmployees.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; dgvEmployees.BackgroundColor = Color.White; dgvEmployees.BorderStyle = BorderStyle.None; dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        lblHomestays.Text = "Chất lượng Homestay"; lblHomestays.Location = new Point(18, 450); lblHomestays.Size = new Size(600, 28); lblHomestays.BackColor = Color.Transparent; lblHomestays.Font = new Font("Segoe UI", 11F, FontStyle.Bold); lblHomestays.ForeColor = Color.FromArgb(30, 58, 47);
        dgvHomestays.Location = new Point(18, 480); dgvHomestays.Size = new Size(1160, 230); dgvHomestays.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; dgvHomestays.BackgroundColor = Color.White; dgvHomestays.BorderStyle = BorderStyle.None; dgvHomestays.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Controls.Add(dgvHomestays); Controls.Add(lblHomestays); Controls.Add(dgvEmployees); Controls.Add(lblEmployees); Controls.Add(dgvBreakdown); Controls.Add(lblBreakdown); Controls.Add(lblNet); Controls.Add(lblRevenue); Controls.Add(lblDeposited); Controls.Add(lblBookings); Controls.Add(pnlFilter);
        BackColor = Color.FromArgb(250, 247, 242); ClientSize = new Size(1200, 740); Text = "Báo cáo & Phân tích"; ResumeLayout(false);
    }
}
