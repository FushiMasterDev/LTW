using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmEmployeeAccountAdmin
{
    private Guna2Panel pnlRightActions = null!;
    private Guna2DataGridView dgvEmployees = null!;
    private Guna2TextBox txtSearchEmployee = null!, txtEmpCode = null!, txtFullName = null!, txtPhone = null!, txtEmail = null!, txtDepartment = null!, txtInitialPassword = null!, txtLockReason = null!, txtNewPassword = null!;
    private Guna2ComboBox cboFilterPosition = null!, cboFilterStatus = null!, cboPosition = null!, cboEmploymentStatus = null!;
    private Guna2Button btnSearch = null!, btnCreate = null!, btnUpdate = null!, btnToggleLock = null!, btnResetPassword = null!;
    private Guna2HtmlLabel lblAccessNote = null!, lblStatus = null!;
    private void InitializeComponent()
    {
        pnlRightActions = new Guna2Panel(); dgvEmployees = new Guna2DataGridView(); txtSearchEmployee = new Guna2TextBox(); txtEmpCode = new Guna2TextBox(); txtFullName = new Guna2TextBox(); txtPhone = new Guna2TextBox(); txtEmail = new Guna2TextBox(); txtDepartment = new Guna2TextBox(); txtInitialPassword = new Guna2TextBox(); txtLockReason = new Guna2TextBox(); txtNewPassword = new Guna2TextBox(); cboFilterPosition = new Guna2ComboBox(); cboFilterStatus = new Guna2ComboBox(); cboPosition = new Guna2ComboBox(); cboEmploymentStatus = new Guna2ComboBox(); btnSearch = new Guna2Button(); btnCreate = new Guna2Button(); btnUpdate = new Guna2Button(); btnToggleLock = new Guna2Button(); btnResetPassword = new Guna2Button(); lblAccessNote = new Guna2HtmlLabel(); lblStatus = new Guna2HtmlLabel(); SuspendLayout();
        txtSearchEmployee.Location = new Point(14, 14); txtSearchEmployee.Size = new Size(330, 40); txtSearchEmployee.BorderRadius = 12; txtSearchEmployee.PlaceholderText = "Tìm mã NV, họ tên, SĐT";
        cboFilterPosition.Location = new Point(354, 14); cboFilterPosition.Size = new Size(180, 40); cboFilterPosition.BorderRadius = 10;
        cboFilterStatus.Location = new Point(544, 14); cboFilterStatus.Size = new Size(170, 40); cboFilterStatus.BorderRadius = 10;
        btnSearch.Text = "Tìm kiếm"; btnSearch.Location = new Point(724, 14); btnSearch.Size = new Size(120, 40); btnSearch.FillColor = Color.FromArgb(30, 58, 47); btnSearch.ForeColor = Color.White; btnSearch.BorderRadius = 20;
        dgvEmployees.Location = new Point(14, 68); dgvEmployees.Size = new Size(830, 620); dgvEmployees.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left; dgvEmployees.BackgroundColor = Color.White; dgvEmployees.BorderStyle = BorderStyle.None; dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        pnlRightActions.Location = new Point(860, 14); pnlRightActions.Size = new Size(314, 674); pnlRightActions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; pnlRightActions.FillColor = Color.White; pnlRightActions.BorderRadius = 15;
        txtEmpCode.Location = new Point(14, 18); txtEmpCode.Size = new Size(286, 38); txtEmpCode.BorderRadius = 10; txtEmpCode.PlaceholderText = "Mã nhân viên"; txtEmpCode.CharacterCasing = CharacterCasing.Upper;
        txtFullName.Location = new Point(14, 64); txtFullName.Size = new Size(286, 38); txtFullName.BorderRadius = 10; txtFullName.PlaceholderText = "Họ và tên";
        txtPhone.Location = new Point(14, 110); txtPhone.Size = new Size(286, 38); txtPhone.BorderRadius = 10; txtPhone.PlaceholderText = "Số điện thoại";
        txtEmail.Location = new Point(14, 156); txtEmail.Size = new Size(286, 38); txtEmail.BorderRadius = 10; txtEmail.PlaceholderText = "Email";
        txtDepartment.Location = new Point(14, 202); txtDepartment.Size = new Size(286, 38); txtDepartment.BorderRadius = 10; txtDepartment.PlaceholderText = "Phòng ban";
        cboPosition.Location = new Point(14, 248); cboPosition.Size = new Size(286, 38); cboPosition.BorderRadius = 10; cboPosition.Items.AddRange(new object[] { "Consultant", "Accountant", "Manager", "Administrator" });
        cboEmploymentStatus.Location = new Point(14, 294); cboEmploymentStatus.Size = new Size(286, 38); cboEmploymentStatus.BorderRadius = 10; cboEmploymentStatus.Items.AddRange(new object[] { "Active", "Suspended", "Resigned" });
        txtInitialPassword.Location = new Point(14, 340); txtInitialPassword.Size = new Size(286, 38); txtInitialPassword.BorderRadius = 10; txtInitialPassword.PlaceholderText = "Mật khẩu khởi tạo";
        btnCreate.Text = "Tạo nhân viên mới"; btnCreate.Location = new Point(14, 386); btnCreate.Size = new Size(286, 40); btnCreate.FillColor = Color.FromArgb(30, 58, 47); btnCreate.ForeColor = Color.White; btnCreate.BorderRadius = 20;
        btnUpdate.Text = "Lưu chức vụ / Trạng thái"; btnUpdate.Location = new Point(14, 434); btnUpdate.Size = new Size(286, 40); btnUpdate.FillColor = Color.FromArgb(200, 90, 50); btnUpdate.ForeColor = Color.White; btnUpdate.BorderRadius = 20;
        txtLockReason.Location = new Point(14, 486); txtLockReason.Size = new Size(286, 38); txtLockReason.BorderRadius = 10; txtLockReason.PlaceholderText = "Lý do khóa / mở khóa";
        btnToggleLock.Text = "Khóa / Mở khóa Tài khoản"; btnToggleLock.Location = new Point(14, 532); btnToggleLock.Size = new Size(286, 40); btnToggleLock.FillColor = Color.FromArgb(30, 58, 47); btnToggleLock.ForeColor = Color.White; btnToggleLock.BorderRadius = 20;
        txtNewPassword.Location = new Point(14, 578); txtNewPassword.Size = new Size(160, 38); txtNewPassword.BorderRadius = 10; txtNewPassword.PlaceholderText = "Mật khẩu mới";
        btnResetPassword.Text = "Đặt lại"; btnResetPassword.Location = new Point(184, 578); btnResetPassword.Size = new Size(116, 38); btnResetPassword.FillColor = Color.FromArgb(200, 90, 50); btnResetPassword.ForeColor = Color.White; btnResetPassword.BorderRadius = 19;
        lblAccessNote.Text = "Quản lý (Manager) chỉ có quyền xem danh sách nhân sự. Các thao tác tạo mới, đổi chức vụ và khóa tài khoản thuộc quyền Quản trị viên (AD001)."; lblAccessNote.Location = new Point(14, 624); lblAccessNote.Size = new Size(286, 42); lblAccessNote.BackColor = Color.Transparent; lblAccessNote.ForeColor = Color.FromArgb(160, 95, 30);
        pnlRightActions.Controls.Add(lblAccessNote); pnlRightActions.Controls.Add(btnResetPassword); pnlRightActions.Controls.Add(txtNewPassword); pnlRightActions.Controls.Add(btnToggleLock); pnlRightActions.Controls.Add(txtLockReason); pnlRightActions.Controls.Add(btnUpdate); pnlRightActions.Controls.Add(btnCreate); pnlRightActions.Controls.Add(txtInitialPassword); pnlRightActions.Controls.Add(cboEmploymentStatus); pnlRightActions.Controls.Add(cboPosition); pnlRightActions.Controls.Add(txtDepartment); pnlRightActions.Controls.Add(txtEmail); pnlRightActions.Controls.Add(txtPhone); pnlRightActions.Controls.Add(txtFullName); pnlRightActions.Controls.Add(txtEmpCode);
        lblStatus.Location = new Point(14, 694); lblStatus.Size = new Size(830, 24); lblStatus.BackColor = Color.Transparent;
        Controls.Add(lblStatus); Controls.Add(pnlRightActions); Controls.Add(dgvEmployees); Controls.Add(btnSearch); Controls.Add(cboFilterStatus); Controls.Add(cboFilterPosition); Controls.Add(txtSearchEmployee); BackColor = Color.FromArgb(250, 247, 242); ClientSize = new Size(1200, 730); Text = "Quản trị Nhân sự & Tài khoản"; ResumeLayout(false);
    }
}
