using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmCatalogManagement
{
    private Guna2Panel pnlTabs = null!, pnlEditor = null!;
    private Guna2Button btnAreas = null!, btnAmenities = null!, btnCategories = null!, btnHolidays = null!, btnAdd = null!, btnUpdate = null!, btnDelete = null!, btnClear = null!;
    private Guna2DataGridView dgvItems = null!;
    private Guna2TextBox txtId = null!, txtAreaName = null!, txtProvince = null!, txtAmenityName = null!, txtAmenityIcon = null!, txtCategoryName = null!, txtHolidayName = null!;
    private Guna2ComboBox cboAreaGroup = null!;
    private Guna2DateTimePicker dtpHolidayDate = null!;
    private Guna2HtmlLabel lblIdLabel = null!, lblStatus = null!;
    private void InitializeComponent()
    {
        pnlTabs = new Guna2Panel(); pnlEditor = new Guna2Panel(); btnAreas = new Guna2Button(); btnAmenities = new Guna2Button(); btnCategories = new Guna2Button(); btnHolidays = new Guna2Button(); btnAdd = new Guna2Button(); btnUpdate = new Guna2Button(); btnDelete = new Guna2Button(); btnClear = new Guna2Button(); dgvItems = new Guna2DataGridView(); txtId = new Guna2TextBox(); txtAreaName = new Guna2TextBox(); txtProvince = new Guna2TextBox(); txtAmenityName = new Guna2TextBox(); txtAmenityIcon = new Guna2TextBox(); txtCategoryName = new Guna2TextBox(); txtHolidayName = new Guna2TextBox(); cboAreaGroup = new Guna2ComboBox(); dtpHolidayDate = new Guna2DateTimePicker(); lblIdLabel = new Guna2HtmlLabel(); lblStatus = new Guna2HtmlLabel(); SuspendLayout();
        pnlTabs.Location = new Point(14, 14); pnlTabs.Size = new Size(1160, 54); pnlTabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlTabs.FillColor = Color.White; pnlTabs.BorderRadius = 14;
        btnAreas.Text = "Khu vực"; btnAreas.Location = new Point(10, 8); btnAreas.Size = new Size(140, 38); btnAreas.FillColor = Color.FromArgb(30, 58, 47); btnAreas.ForeColor = Color.White; btnAreas.BorderRadius = 18;
        btnAmenities.Text = "Tiện nghi"; btnAmenities.Location = new Point(158, 8); btnAmenities.Size = new Size(140, 38); btnAmenities.FillColor = Color.FromArgb(30, 58, 47); btnAmenities.ForeColor = Color.White; btnAmenities.BorderRadius = 18;
        btnCategories.Text = "Loại phòng"; btnCategories.Location = new Point(306, 8); btnCategories.Size = new Size(140, 38); btnCategories.FillColor = Color.FromArgb(30, 58, 47); btnCategories.ForeColor = Color.White; btnCategories.BorderRadius = 18;
        btnHolidays.Text = "Ngày lễ"; btnHolidays.Location = new Point(454, 8); btnHolidays.Size = new Size(140, 38); btnHolidays.FillColor = Color.FromArgb(30, 58, 47); btnHolidays.ForeColor = Color.White; btnHolidays.BorderRadius = 18; pnlTabs.Controls.Add(btnAreas); pnlTabs.Controls.Add(btnAmenities); pnlTabs.Controls.Add(btnCategories); pnlTabs.Controls.Add(btnHolidays);
        dgvItems.Location = new Point(14, 82); dgvItems.Size = new Size(670, 610); dgvItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left; dgvItems.BackgroundColor = Color.White; dgvItems.BorderStyle = BorderStyle.None; dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        pnlEditor.Location = new Point(700, 82); pnlEditor.Size = new Size(474, 610); pnlEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; pnlEditor.FillColor = Color.White; pnlEditor.BorderRadius = 15;
        lblIdLabel.Text = "Mã danh mục"; lblIdLabel.Location = new Point(18, 18); lblIdLabel.Size = new Size(160, 24); lblIdLabel.BackColor = Color.Transparent; txtId.Location = new Point(18, 45); txtId.Size = new Size(430, 38); txtId.ReadOnly = true; txtId.BorderRadius = 10; txtId.PlaceholderText = "Tự động";
        txtAreaName.Location = new Point(18, 94); txtAreaName.Size = new Size(430, 42); txtAreaName.BorderRadius = 12; txtAreaName.PlaceholderText = "Tên khu vực";
        txtProvince.Location = new Point(18, 148); txtProvince.Size = new Size(430, 42); txtProvince.BorderRadius = 12; txtProvince.PlaceholderText = "Tỉnh / Thành phố";
        cboAreaGroup.Location = new Point(18, 202); cboAreaGroup.Size = new Size(430, 42); cboAreaGroup.BorderRadius = 12; cboAreaGroup.Items.AddRange(new object[] { "InnerCity", "Suburb", "OtherProvince" });
        txtAmenityName.Location = new Point(18, 94); txtAmenityName.Size = new Size(430, 42); txtAmenityName.BorderRadius = 12; txtAmenityName.PlaceholderText = "Tên tiện nghi";
        txtAmenityIcon.Location = new Point(18, 148); txtAmenityIcon.Size = new Size(430, 42); txtAmenityIcon.BorderRadius = 12; txtAmenityIcon.PlaceholderText = "Mã icon (bi-wifi)";
        txtCategoryName.Location = new Point(18, 94); txtCategoryName.Size = new Size(430, 42); txtCategoryName.BorderRadius = 12; txtCategoryName.PlaceholderText = "Tên loại phòng";
        dtpHolidayDate.Location = new Point(18, 94); dtpHolidayDate.Size = new Size(430, 42); dtpHolidayDate.BorderRadius = 10; dtpHolidayDate.Format = DateTimePickerFormat.Short;
        txtHolidayName.Location = new Point(18, 148); txtHolidayName.Size = new Size(430, 42); txtHolidayName.BorderRadius = 12; txtHolidayName.PlaceholderText = "Tên ngày lễ";
        btnAdd.Text = "Thêm mới"; btnAdd.Location = new Point(18, 280); btnAdd.Size = new Size(205, 44); btnAdd.FillColor = Color.FromArgb(30, 58, 47); btnAdd.ForeColor = Color.White; btnAdd.BorderRadius = 22;
        btnUpdate.Text = "Cập nhật"; btnUpdate.Location = new Point(238, 280); btnUpdate.Size = new Size(210, 44); btnUpdate.FillColor = Color.FromArgb(200, 90, 50); btnUpdate.ForeColor = Color.White; btnUpdate.BorderRadius = 22;
        btnDelete.Text = "Xóa"; btnDelete.Location = new Point(18, 336); btnDelete.Size = new Size(205, 44); btnDelete.FillColor = Color.FromArgb(160, 55, 45); btnDelete.ForeColor = Color.White; btnDelete.BorderRadius = 22;
        btnClear.Text = "Làm mới ô nhập"; btnClear.Location = new Point(238, 336); btnClear.Size = new Size(210, 44); btnClear.FillColor = Color.FromArgb(232, 235, 231); btnClear.ForeColor = Color.FromArgb(30, 58, 47); btnClear.BorderRadius = 22;
        lblStatus.Location = new Point(18, 402); lblStatus.Size = new Size(430, 140); lblStatus.BackColor = Color.Transparent; lblStatus.ForeColor = Color.FromArgb(104, 120, 124); lblStatus.Text = "Đang tải danh mục...";
        pnlEditor.Controls.Add(lblStatus); pnlEditor.Controls.Add(btnClear); pnlEditor.Controls.Add(btnDelete); pnlEditor.Controls.Add(btnUpdate); pnlEditor.Controls.Add(btnAdd); pnlEditor.Controls.Add(txtHolidayName); pnlEditor.Controls.Add(dtpHolidayDate); pnlEditor.Controls.Add(txtCategoryName); pnlEditor.Controls.Add(txtAmenityIcon); pnlEditor.Controls.Add(txtAmenityName); pnlEditor.Controls.Add(cboAreaGroup); pnlEditor.Controls.Add(txtProvince); pnlEditor.Controls.Add(txtAreaName); pnlEditor.Controls.Add(txtId); pnlEditor.Controls.Add(lblIdLabel);
        Controls.Add(pnlEditor); Controls.Add(dgvItems); Controls.Add(pnlTabs); BackColor = Color.FromArgb(250, 247, 242); ClientSize = new Size(1200, 720); Text = "Quản lý Danh mục"; ResumeLayout(false);
    }
}
