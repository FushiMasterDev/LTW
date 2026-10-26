using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmApprovalAndContract
{
    private Guna2TabControl tabs = null!;
    private TabPage tabChanges = null!, tabContracts = null!, tabVouchers = null!;
    private Guna2DataGridView dgvChanges = null!, dgvContracts = null!, dgvVouchers = null!;
    private Guna2TextBox txtDecisionNote = null!, txtContractNo = null!, txtHomestayId = null!, txtCommission = null!, txtContractReason = null!, txtVoucherBooking = null!, txtVoucherNote = null!;
    private Guna2Button btnApproveChange = null!, btnRejectChange = null!, btnCreateContract = null!, btnIssueVoucher = null!, btnRevokeVoucher = null!, btnReload = null!;
    private Guna2HtmlLabel lblPolicy = null!;
    private void InitializeComponent()
    {
        tabs = new Guna2TabControl(); tabChanges = new TabPage(); tabContracts = new TabPage(); tabVouchers = new TabPage(); dgvChanges = new Guna2DataGridView(); dgvContracts = new Guna2DataGridView(); dgvVouchers = new Guna2DataGridView(); txtDecisionNote = new Guna2TextBox(); txtContractNo = new Guna2TextBox(); txtHomestayId = new Guna2TextBox(); txtCommission = new Guna2TextBox(); txtContractReason = new Guna2TextBox(); txtVoucherBooking = new Guna2TextBox(); txtVoucherNote = new Guna2TextBox(); btnApproveChange = new Guna2Button(); btnRejectChange = new Guna2Button(); btnCreateContract = new Guna2Button(); btnIssueVoucher = new Guna2Button(); btnRevokeVoucher = new Guna2Button(); btnReload = new Guna2Button(); lblPolicy = new Guna2HtmlLabel(); SuspendLayout();
        tabs.Location = new Point(14, 14); tabs.Size = new Size(1160, 690); tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; tabs.TabButtonSelectedState.FillColor = Color.FromArgb(30, 58, 47); tabs.TabButtonSelectedState.ForeColor = Color.White; tabs.TabButtonIdleState.ForeColor = Color.FromArgb(30, 58, 47);
        tabChanges.Text = "Đổi / Hủy đơn"; tabContracts.Text = "Hợp đồng & Homestay"; tabVouchers.Text = "Voucher";
        dgvChanges.Location = new Point(16, 18); dgvChanges.Size = new Size(1110, 430); dgvChanges.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; dgvChanges.BackgroundColor = Color.White; dgvChanges.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        txtDecisionNote.Location = new Point(16, 468); txtDecisionNote.Size = new Size(560, 48); txtDecisionNote.BorderRadius = 12; txtDecisionNote.PlaceholderText = "Ghi chú quyết định (bắt buộc khi từ chối)";
        btnApproveChange.Text = "Duyệt bước 1"; btnApproveChange.Location = new Point(600, 470); btnApproveChange.Size = new Size(210, 44); btnApproveChange.FillColor = Color.FromArgb(30, 58, 47); btnApproveChange.ForeColor = Color.White; btnApproveChange.BorderRadius = 22;
        btnRejectChange.Text = "Từ chối yêu cầu"; btnRejectChange.Location = new Point(830, 470); btnRejectChange.Size = new Size(210, 44); btnRejectChange.FillColor = Color.FromArgb(200, 90, 50); btnRejectChange.ForeColor = Color.White; btnRejectChange.BorderRadius = 22;
        lblPolicy.Location = new Point(16, 538); lblPolicy.Size = new Size(1080, 58); lblPolicy.BackColor = Color.Transparent; lblPolicy.ForeColor = Color.FromArgb(160, 95, 30); tabChanges.Controls.Add(lblPolicy); tabChanges.Controls.Add(btnRejectChange); tabChanges.Controls.Add(btnApproveChange); tabChanges.Controls.Add(txtDecisionNote); tabChanges.Controls.Add(dgvChanges);
        dgvContracts.Location = new Point(16, 18); dgvContracts.Size = new Size(1110, 380); dgvContracts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; dgvContracts.BackgroundColor = Color.White; dgvContracts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        txtContractNo.Location = new Point(16, 420); txtContractNo.Size = new Size(250, 40); txtContractNo.PlaceholderText = "Số hợp đồng"; txtContractNo.BorderRadius = 10;
        txtHomestayId.Location = new Point(280, 420); txtHomestayId.Size = new Size(250, 40); txtHomestayId.PlaceholderText = "Mã Homestay"; txtHomestayId.BorderRadius = 10;
        txtCommission.Location = new Point(544, 420); txtCommission.Size = new Size(250, 40); txtCommission.PlaceholderText = "Hoa hồng 10–15%"; txtCommission.BorderRadius = 10;
        txtContractReason.Location = new Point(16, 476); txtContractReason.Size = new Size(778, 48); txtContractReason.PlaceholderText = "Lý do chấm dứt / thông tin hợp đồng"; txtContractReason.BorderRadius = 12;
        btnCreateContract.Text = "Lập hợp đồng mới"; btnCreateContract.Location = new Point(820, 426); btnCreateContract.Size = new Size(250, 44); btnCreateContract.FillColor = Color.FromArgb(30, 58, 47); btnCreateContract.ForeColor = Color.White; btnCreateContract.BorderRadius = 22; tabContracts.Controls.Add(btnCreateContract); tabContracts.Controls.Add(txtContractReason); tabContracts.Controls.Add(txtCommission); tabContracts.Controls.Add(txtHomestayId); tabContracts.Controls.Add(txtContractNo); tabContracts.Controls.Add(dgvContracts);
        dgvVouchers.Location = new Point(16, 18); dgvVouchers.Size = new Size(1110, 400); dgvVouchers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; dgvVouchers.BackgroundColor = Color.White; dgvVouchers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        txtVoucherBooking.Location = new Point(16, 438); txtVoucherBooking.Size = new Size(300, 40); txtVoucherBooking.PlaceholderText = "Mã đơn nguồn"; txtVoucherBooking.BorderRadius = 10;
        txtVoucherNote.Location = new Point(330, 438); txtVoucherNote.Size = new Size(460, 40); txtVoucherNote.PlaceholderText = "Ghi chú cấp voucher"; txtVoucherNote.BorderRadius = 10;
        btnIssueVoucher.Text = "Cấp Voucher 5%"; btnIssueVoucher.Location = new Point(820, 438); btnIssueVoucher.Size = new Size(230, 42); btnIssueVoucher.FillColor = Color.FromArgb(30, 58, 47); btnIssueVoucher.ForeColor = Color.White; btnIssueVoucher.BorderRadius = 21;
        btnRevokeVoucher.Text = "Thu hồi Voucher"; btnRevokeVoucher.Location = new Point(820, 494); btnRevokeVoucher.Size = new Size(230, 42); btnRevokeVoucher.FillColor = Color.FromArgb(200, 90, 50); btnRevokeVoucher.ForeColor = Color.White; btnRevokeVoucher.BorderRadius = 21; tabVouchers.Controls.Add(btnRevokeVoucher); tabVouchers.Controls.Add(btnIssueVoucher); tabVouchers.Controls.Add(txtVoucherNote); tabVouchers.Controls.Add(txtVoucherBooking); tabVouchers.Controls.Add(dgvVouchers);
        btnReload.Text = "Tải lại"; btnReload.Location = new Point(1030, 14); btnReload.Size = new Size(144, 42); btnReload.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnReload.FillColor = Color.FromArgb(200, 90, 50); btnReload.ForeColor = Color.White; btnReload.BorderRadius = 21;
        tabs.TabPages.Add(tabChanges); tabs.TabPages.Add(tabContracts); tabs.TabPages.Add(tabVouchers); Controls.Add(btnReload); Controls.Add(tabs); BackColor = Color.FromArgb(250, 247, 242); ClientSize = new Size(1200, 720); Text = "Phê duyệt Nghiệp vụ, Hợp đồng & Voucher"; ResumeLayout(false);
    }
}
