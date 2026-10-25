#nullable enable
using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
public partial class FrmCreateBooking
{
    private System.ComponentModel.IContainer? components;
    private Guna2Panel pnlSearchAvailability = null!, pnlCreateBooking = null!;
    private Guna2ComboBox cboArea = null!, cboHomestay = null!, cboRoomType = null!, cboChannel = null!;
    private Guna2DateTimePicker dtpCheckIn = null!, dtpCheckOut = null!;
    private Guna2NumericUpDown numGuests = null!;
    private Guna2Button btnCheckAvailability = null!, btnPrevPage = null!, btnNextPage = null!, btnSubmitBooking = null!, btnResetForm = null!;
    private Guna2DataGridView dgvAvailability = null!;
    private Guna2TextBox txtPhone = null!, txtFullName = null!, txtEmail = null!, txtSpecialRequests = null!;
    private Label lblPageInfo = null!, lblResult = null!, lblSearchTitle = null!, lblCreateTitle = null!;
    protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pnlSearchAvailability = new Guna2Panel(); pnlCreateBooking = new Guna2Panel();
        cboArea = new Guna2ComboBox(); cboHomestay = new Guna2ComboBox(); cboRoomType = new Guna2ComboBox(); cboChannel = new Guna2ComboBox();
        dtpCheckIn = new Guna2DateTimePicker(); dtpCheckOut = new Guna2DateTimePicker(); numGuests = new Guna2NumericUpDown();
        btnCheckAvailability = new Guna2Button(); btnPrevPage = new Guna2Button(); btnNextPage = new Guna2Button(); btnSubmitBooking = new Guna2Button(); btnResetForm = new Guna2Button();
        dgvAvailability = new Guna2DataGridView(); txtPhone = new Guna2TextBox(); txtFullName = new Guna2TextBox(); txtEmail = new Guna2TextBox(); txtSpecialRequests = new Guna2TextBox();
        lblPageInfo = new Label(); lblResult = new Label(); lblSearchTitle = new Label(); lblCreateTitle = new Label();
        SuspendLayout();

        var green = Color.FromArgb(19, 132, 111); var greenHover = Color.FromArgb(16, 112, 94);
        var ink = Color.FromArgb(30, 58, 47); var muted = Color.FromArgb(104, 120, 124); var border = Color.FromArgb(220, 227, 231);
        foreach (var panel in new[] { pnlSearchAvailability, pnlCreateBooking }) { panel.FillColor = Color.White; panel.BorderRadius = 14; panel.Padding = new Padding(12); }
        foreach (var combo in new[] { cboArea, cboHomestay, cboRoomType, cboChannel }) { combo.BorderRadius = 8; combo.BorderColor = border; combo.Font = new Font("Segoe UI", 9F); combo.ForeColor = ink; combo.ItemHeight = 28; }
        foreach (var picker in new[] { dtpCheckIn, dtpCheckOut }) { picker.BorderRadius = 8; picker.FillColor = Color.White; picker.BorderColor = border; picker.ForeColor = ink; picker.Font = new Font("Segoe UI", 9F); picker.Height = 34; }
        numGuests.BorderRadius = 8; numGuests.BorderColor = border; numGuests.Font = new Font("Segoe UI", 9F); numGuests.Height = 34; numGuests.Minimum = 1; numGuests.Value = 1;
        foreach (var input in new[] { txtPhone, txtFullName, txtEmail, txtSpecialRequests }) { input.BorderRadius = 8; input.BorderColor = border; input.Font = new Font("Segoe UI", 9F); input.ForeColor = ink; }
        txtPhone.PlaceholderText = "Số điện thoại *"; txtFullName.PlaceholderText = "Họ tên người ở *"; txtEmail.PlaceholderText = "Email (không bắt buộc)";
        txtSpecialRequests.PlaceholderText = "Yêu cầu đặc biệt (không bắt buộc)"; txtSpecialRequests.Multiline = true;
        dgvAvailability.ThemeStyle.HeaderStyle.BackColor = green; dgvAvailability.ThemeStyle.HeaderStyle.ForeColor = Color.White; dgvAvailability.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvAvailability.ThemeStyle.RowsStyle.BackColor = Color.White; dgvAvailability.ThemeStyle.RowsStyle.ForeColor = ink; dgvAvailability.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvAvailability.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246); dgvAvailability.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvAvailability.BackgroundColor = Color.White; dgvAvailability.BorderStyle = BorderStyle.None; dgvAvailability.ColumnHeadersHeight = 34; dgvAvailability.RowTemplate.Height = 30;
        foreach (var button in new[] { btnCheckAvailability, btnSubmitBooking }) { button.FillColor = green; button.ForeColor = Color.White; button.BorderRadius = 8; button.Font = new Font("Segoe UI", 9F, FontStyle.Bold); button.HoverState.FillColor = greenHover; }
        foreach (var button in new[] { btnPrevPage, btnNextPage, btnResetForm }) { button.FillColor = Color.FromArgb(239, 246, 244); button.ForeColor = green; button.BorderRadius = 8; button.Font = new Font("Segoe UI", 9F, FontStyle.Bold); button.HoverState.FillColor = Color.FromArgb(224, 239, 234); }

        Text = "Tra cứu & Đặt phòng hộ"; BackColor = Color.FromArgb(243, 247, 246); Font = new Font("Segoe UI", 9F); AutoScaleMode = AutoScaleMode.Font; ClientSize = new Size(900, 650); MinimumSize = new Size(900, 650);
        pnlSearchAvailability.Location = new Point(16, 16); pnlSearchAvailability.Size = new Size(868, 292); pnlSearchAvailability.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pnlCreateBooking.Location = new Point(16, 320); pnlCreateBooking.Size = new Size(868, 314); pnlCreateBooking.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblSearchTitle.Location = new Point(16, 12); lblSearchTitle.Size = new Size(400, 25); lblSearchTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold); lblSearchTitle.ForeColor = ink; lblSearchTitle.Text = "Tra cứu phòng trống";
        lblCreateTitle.Location = new Point(16, 12); lblCreateTitle.Size = new Size(400, 25); lblCreateTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold); lblCreateTitle.ForeColor = ink; lblCreateTitle.Text = "Thông tin đặt phòng hộ";
        pnlSearchAvailability.Controls.Add(lblSearchTitle);
        cboArea.Location = new Point(16, 48); cboArea.Size = new Size(178, 34); cboArea.DropDownStyle = ComboBoxStyle.DropDownList;
        dtpCheckIn.Location = new Point(204, 48); dtpCheckIn.Size = new Size(157, 34); dtpCheckIn.Format = DateTimePickerFormat.Custom; dtpCheckIn.CustomFormat = "dd/MM/yyyy";
        dtpCheckOut.Location = new Point(371, 48); dtpCheckOut.Size = new Size(157, 34); dtpCheckOut.Format = DateTimePickerFormat.Custom; dtpCheckOut.CustomFormat = "dd/MM/yyyy";
        numGuests.Location = new Point(538, 48); numGuests.Size = new Size(100, 34);
        btnCheckAvailability.Location = new Point(648, 48); btnCheckAvailability.Size = new Size(188, 36); btnCheckAvailability.Text = "Kiểm tra phòng trống";
        dgvAvailability.Location = new Point(16, 96); dgvAvailability.Size = new Size(820, 145); dgvAvailability.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        dgvAvailability.ReadOnly = true; dgvAvailability.AllowUserToAddRows = false; dgvAvailability.AllowUserToDeleteRows = false; dgvAvailability.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvAvailability.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        lblPageInfo.Location = new Point(16, 252); lblPageInfo.Size = new Size(440, 24); lblPageInfo.ForeColor = muted; lblPageInfo.Text = "Trang 1";
        btnPrevPage.Location = new Point(656, 248); btnPrevPage.Size = new Size(84, 34); btnPrevPage.Text = "Trước";
        btnNextPage.Location = new Point(752, 248); btnNextPage.Size = new Size(84, 34); btnNextPage.Text = "Sau";
        pnlSearchAvailability.Controls.AddRange(new Control[] { cboArea, dtpCheckIn, dtpCheckOut, numGuests, btnCheckAvailability, dgvAvailability, lblPageInfo, btnPrevPage, btnNextPage });

        pnlCreateBooking.Controls.Add(lblCreateTitle);
        cboHomestay.Location = new Point(16, 48); cboHomestay.Size = new Size(258, 34); cboHomestay.DropDownStyle = ComboBoxStyle.DropDownList;
        cboRoomType.Location = new Point(290, 48); cboRoomType.Size = new Size(258, 34); cboRoomType.DropDownStyle = ComboBoxStyle.DropDownList;
        cboChannel.Location = new Point(564, 48); cboChannel.Size = new Size(272, 34); cboChannel.DropDownStyle = ComboBoxStyle.DropDownList; cboChannel.Items.AddRange(new object[] { "Zalo", "Messenger", "Phone", "Other" });
        txtPhone.Location = new Point(16, 94); txtPhone.Size = new Size(258, 36); txtFullName.Location = new Point(290, 94); txtFullName.Size = new Size(258, 36); txtEmail.Location = new Point(564, 94); txtEmail.Size = new Size(272, 36);
        txtSpecialRequests.Location = new Point(16, 142); txtSpecialRequests.Size = new Size(520, 82); txtSpecialRequests.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblResult.Location = new Point(16, 235); lblResult.Size = new Size(520, 52); lblResult.ForeColor = muted; lblResult.Font = new Font("Segoe UI", 8.5F); lblResult.Text = "";
        btnSubmitBooking.Location = new Point(564, 154); btnSubmitBooking.Size = new Size(272, 42); btnSubmitBooking.Text = "Tạo đặt phòng";
        btnResetForm.Location = new Point(564, 204); btnResetForm.Size = new Size(272, 38); btnResetForm.Text = "Làm mới thông tin";
        pnlCreateBooking.Controls.AddRange(new Control[] { cboHomestay, cboRoomType, cboChannel, txtPhone, txtFullName, txtEmail, txtSpecialRequests, lblResult, btnSubmitBooking, btnResetForm });
        Controls.Add(pnlSearchAvailability); Controls.Add(pnlCreateBooking); ResumeLayout(false);
    }
}
