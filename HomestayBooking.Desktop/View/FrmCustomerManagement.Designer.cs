#nullable enable
using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
public partial class FrmCustomerManagement
{
 private System.ComponentModel.IContainer? components; private Guna2Panel pnlCustomerLeft=null!,pnlCustomerProfile=null!,pnlCustomerHistory=null!;private Guna2TextBox txtCustomerSearch=null!,txtCustFullName=null!,txtCustPhone=null!,txtCustEmail=null!,txtCustCitizenId=null!,txtCustStudentId=null!,txtCustNote=null!;private Guna2DataGridView dgvCustomers=null!,dgvCustomerBookings=null!,dgvCustomerVouchers=null!;private Guna2Button btnSearchCustomer=null!,btnSaveCustomer=null!,btnCustPrev=null!,btnCustNext=null!;private Label lblCustPage=null!,lblAccountBadge=null!;private TabControl tabCustomerHistory=null!;private TabPage tabCustomerBookings=null!,tabCustomerVouchers=null!;
 protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}
    private void InitializeComponent()
    {
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges25 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges26 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges13 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges14 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges15 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges16 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges17 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges18 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges19 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges20 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges21 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges22 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges23 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges24 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges27 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges28 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
        pnlCustomerLeft = new Guna2Panel();
        txtCustomerSearch = new Guna2TextBox();
        btnSearchCustomer = new Guna2Button();
        dgvCustomers = new Guna2DataGridView();
        btnCustPrev = new Guna2Button();
        btnCustNext = new Guna2Button();
        lblCustPage = new Label();
        pnlCustomerProfile = new Guna2Panel();
        txtCustFullName = new Guna2TextBox();
        txtCustPhone = new Guna2TextBox();
        txtCustEmail = new Guna2TextBox();
        txtCustCitizenId = new Guna2TextBox();
        txtCustStudentId = new Guna2TextBox();
        lblAccountBadge = new Label();
        txtCustNote = new Guna2TextBox();
        btnSaveCustomer = new Guna2Button();
        pnlCustomerHistory = new Guna2Panel();
        tabCustomerHistory = new TabControl();
        tabCustomerBookings = new TabPage();
        dgvCustomerBookings = new Guna2DataGridView();
        tabCustomerVouchers = new TabPage();
        dgvCustomerVouchers = new Guna2DataGridView();
        pnlCustomerLeft.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
        pnlCustomerProfile.SuspendLayout();
        pnlCustomerHistory.SuspendLayout();
        tabCustomerHistory.SuspendLayout();
        tabCustomerBookings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomerBookings).BeginInit();
        tabCustomerVouchers.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomerVouchers).BeginInit();
        SuspendLayout();
        // 
        // pnlCustomerLeft
        // 
        pnlCustomerLeft.BackColor = Color.White;
        pnlCustomerLeft.BorderRadius = 12;
        pnlCustomerLeft.Controls.Add(txtCustomerSearch);
        pnlCustomerLeft.Controls.Add(btnSearchCustomer);
        pnlCustomerLeft.Controls.Add(dgvCustomers);
        pnlCustomerLeft.Controls.Add(btnCustPrev);
        pnlCustomerLeft.Controls.Add(btnCustNext);
        pnlCustomerLeft.Controls.Add(lblCustPage);
        pnlCustomerLeft.CustomizableEdges = customizableEdges9;
        pnlCustomerLeft.FillColor = Color.White;
        pnlCustomerLeft.Location = new Point(16, 16);
        pnlCustomerLeft.Name = "pnlCustomerLeft";
        pnlCustomerLeft.ShadowDecoration.CustomizableEdges = customizableEdges10;
        pnlCustomerLeft.Size = new Size(390, 618);
        pnlCustomerLeft.TabIndex = 0;
        // 
        // txtCustomerSearch
        // 
        txtCustomerSearch.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustomerSearch.BorderRadius = 8;
        txtCustomerSearch.CustomizableEdges = customizableEdges1;
        txtCustomerSearch.DefaultText = "";
        txtCustomerSearch.Font = new Font("Segoe UI", 9F);
        txtCustomerSearch.Location = new Point(12, 14);
        txtCustomerSearch.Margin = new Padding(4, 5, 4, 5);
        txtCustomerSearch.Name = "txtCustomerSearch";
        txtCustomerSearch.PlaceholderText = "Tên, số điện thoại hoặc email";
        txtCustomerSearch.SelectedText = "";
        txtCustomerSearch.ShadowDecoration.CustomizableEdges = customizableEdges2;
        txtCustomerSearch.Size = new Size(245, 28);
        txtCustomerSearch.TabIndex = 0;
        // 
        // btnSearchCustomer
        // 
        btnSearchCustomer.BorderRadius = 8;
        btnSearchCustomer.CustomizableEdges = customizableEdges3;
        btnSearchCustomer.FillColor = Color.FromArgb(19, 132, 111);
        btnSearchCustomer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnSearchCustomer.ForeColor = Color.White;
        btnSearchCustomer.HoverState.FillColor = Color.FromArgb(16, 112, 94);
        btnSearchCustomer.Location = new Point(266, 10);
        btnSearchCustomer.Name = "btnSearchCustomer";
        btnSearchCustomer.ShadowDecoration.CustomizableEdges = customizableEdges4;
        btnSearchCustomer.Size = new Size(110, 36);
        btnSearchCustomer.TabIndex = 1;
        btnSearchCustomer.Text = "Tìm khách";
        // 
        // dgvCustomers
        // 
        dgvCustomers.AllowUserToAddRows = false;
        dgvCustomers.AllowUserToDeleteRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomers.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.White;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        dgvCustomers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvCustomers.ColumnHeadersHeight = 34;
        dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        dgvCustomers.DefaultCellStyle = dataGridViewCellStyle3;
        dgvCustomers.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomers.Location = new Point(10, 55);
        dgvCustomers.Name = "dgvCustomers";
        dgvCustomers.ReadOnly = true;
        dgvCustomers.RowHeadersVisible = false;
        dgvCustomers.RowHeadersWidth = 62;
        dgvCustomers.RowTemplate.Height = 30;
        dgvCustomers.Size = new Size(368, 510);
        dgvCustomers.TabIndex = 2;
        dgvCustomers.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomers.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomers.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvCustomers.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvCustomers.ThemeStyle.HeaderStyle.Height = 34;
        dgvCustomers.ThemeStyle.ReadOnly = true;
        dgvCustomers.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvCustomers.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvCustomers.ThemeStyle.RowsStyle.Height = 30;
        // 
        // btnCustPrev
        // 
        btnCustPrev.BorderRadius = 8;
        btnCustPrev.CustomizableEdges = customizableEdges5;
        btnCustPrev.FillColor = Color.FromArgb(239, 246, 244);
        btnCustPrev.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnCustPrev.ForeColor = Color.FromArgb(19, 132, 111);
        btnCustPrev.Location = new Point(190, 575);
        btnCustPrev.Name = "btnCustPrev";
        btnCustPrev.ShadowDecoration.CustomizableEdges = customizableEdges6;
        btnCustPrev.Size = new Size(89, 30);
        btnCustPrev.TabIndex = 3;
        btnCustPrev.Text = "Trước";
        // 
        // btnCustNext
        // 
        btnCustNext.BorderRadius = 8;
        btnCustNext.CustomizableEdges = customizableEdges7;
        btnCustNext.FillColor = Color.FromArgb(239, 246, 244);
        btnCustNext.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnCustNext.ForeColor = Color.FromArgb(19, 132, 111);
        btnCustNext.Location = new Point(285, 575);
        btnCustNext.Name = "btnCustNext";
        btnCustNext.ShadowDecoration.CustomizableEdges = customizableEdges8;
        btnCustNext.Size = new Size(80, 30);
        btnCustNext.TabIndex = 4;
        btnCustNext.Text = "Sau";
        // 
        // lblCustPage
        // 
        lblCustPage.Location = new Point(12, 580);
        lblCustPage.Name = "lblCustPage";
        lblCustPage.Size = new Size(150, 24);
        lblCustPage.TabIndex = 5;
        lblCustPage.Text = "Trang 1";
        // 
        // pnlCustomerProfile
        // 
        pnlCustomerProfile.BackColor = Color.White;
        pnlCustomerProfile.BorderRadius = 12;
        pnlCustomerProfile.Controls.Add(txtCustFullName);
        pnlCustomerProfile.Controls.Add(txtCustPhone);
        pnlCustomerProfile.Controls.Add(txtCustEmail);
        pnlCustomerProfile.Controls.Add(txtCustCitizenId);
        pnlCustomerProfile.Controls.Add(txtCustStudentId);
        pnlCustomerProfile.Controls.Add(lblAccountBadge);
        pnlCustomerProfile.Controls.Add(txtCustNote);
        pnlCustomerProfile.Controls.Add(btnSaveCustomer);
        pnlCustomerProfile.CustomizableEdges = customizableEdges25;
        pnlCustomerProfile.FillColor = Color.White;
        pnlCustomerProfile.Location = new Point(418, 16);
        pnlCustomerProfile.Name = "pnlCustomerProfile";
        pnlCustomerProfile.ShadowDecoration.CustomizableEdges = customizableEdges26;
        pnlCustomerProfile.Size = new Size(466, 260);
        pnlCustomerProfile.TabIndex = 1;
        // 
        // txtCustFullName
        // 
        txtCustFullName.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustFullName.BorderRadius = 8;
        txtCustFullName.CustomizableEdges = customizableEdges11;
        txtCustFullName.DefaultText = "";
        txtCustFullName.Font = new Font("Segoe UI", 9F);
        txtCustFullName.Location = new Point(15, 15);
        txtCustFullName.Margin = new Padding(4, 5, 4, 5);
        txtCustFullName.Name = "txtCustFullName";
        txtCustFullName.PlaceholderText = "";
        txtCustFullName.SelectedText = "";
        txtCustFullName.ShadowDecoration.CustomizableEdges = customizableEdges12;
        txtCustFullName.Size = new Size(210, 28);
        txtCustFullName.TabIndex = 0;
        // 
        // txtCustPhone
        // 
        txtCustPhone.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustPhone.BorderRadius = 8;
        txtCustPhone.CustomizableEdges = customizableEdges13;
        txtCustPhone.DefaultText = "";
        txtCustPhone.Font = new Font("Segoe UI", 9F);
        txtCustPhone.Location = new Point(240, 15);
        txtCustPhone.Margin = new Padding(4, 5, 4, 5);
        txtCustPhone.Name = "txtCustPhone";
        txtCustPhone.PlaceholderText = "";
        txtCustPhone.ReadOnly = true;
        txtCustPhone.SelectedText = "";
        txtCustPhone.ShadowDecoration.CustomizableEdges = customizableEdges14;
        txtCustPhone.Size = new Size(215, 28);
        txtCustPhone.TabIndex = 1;
        // 
        // txtCustEmail
        // 
        txtCustEmail.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustEmail.BorderRadius = 8;
        txtCustEmail.CustomizableEdges = customizableEdges15;
        txtCustEmail.DefaultText = "";
        txtCustEmail.Font = new Font("Segoe UI", 9F);
        txtCustEmail.Location = new Point(15, 55);
        txtCustEmail.Margin = new Padding(4, 5, 4, 5);
        txtCustEmail.Name = "txtCustEmail";
        txtCustEmail.PlaceholderText = "";
        txtCustEmail.SelectedText = "";
        txtCustEmail.ShadowDecoration.CustomizableEdges = customizableEdges16;
        txtCustEmail.Size = new Size(440, 28);
        txtCustEmail.TabIndex = 2;
        // 
        // txtCustCitizenId
        // 
        txtCustCitizenId.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustCitizenId.BorderRadius = 8;
        txtCustCitizenId.CustomizableEdges = customizableEdges17;
        txtCustCitizenId.DefaultText = "";
        txtCustCitizenId.Font = new Font("Segoe UI", 9F);
        txtCustCitizenId.Location = new Point(15, 95);
        txtCustCitizenId.Margin = new Padding(4, 5, 4, 5);
        txtCustCitizenId.Name = "txtCustCitizenId";
        txtCustCitizenId.PlaceholderText = "";
        txtCustCitizenId.ReadOnly = true;
        txtCustCitizenId.SelectedText = "";
        txtCustCitizenId.ShadowDecoration.CustomizableEdges = customizableEdges18;
        txtCustCitizenId.Size = new Size(210, 28);
        txtCustCitizenId.TabIndex = 3;
        // 
        // txtCustStudentId
        // 
        txtCustStudentId.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustStudentId.BorderRadius = 8;
        txtCustStudentId.CustomizableEdges = customizableEdges19;
        txtCustStudentId.DefaultText = "";
        txtCustStudentId.Font = new Font("Segoe UI", 9F);
        txtCustStudentId.Location = new Point(240, 95);
        txtCustStudentId.Margin = new Padding(4, 5, 4, 5);
        txtCustStudentId.Name = "txtCustStudentId";
        txtCustStudentId.PlaceholderText = "";
        txtCustStudentId.ReadOnly = true;
        txtCustStudentId.SelectedText = "";
        txtCustStudentId.ShadowDecoration.CustomizableEdges = customizableEdges20;
        txtCustStudentId.Size = new Size(215, 28);
        txtCustStudentId.TabIndex = 4;
        // 
        // lblAccountBadge
        // 
        lblAccountBadge.Font = new Font("Segoe UI", 8.5F);
        lblAccountBadge.ForeColor = Color.FromArgb(104, 120, 124);
        lblAccountBadge.Location = new Point(15, 130);
        lblAccountBadge.Name = "lblAccountBadge";
        lblAccountBadge.Size = new Size(440, 28);
        lblAccountBadge.TabIndex = 5;
        // 
        // txtCustNote
        // 
        txtCustNote.BorderColor = Color.FromArgb(220, 227, 231);
        txtCustNote.BorderRadius = 8;
        txtCustNote.CustomizableEdges = customizableEdges21;
        txtCustNote.DefaultText = "";
        txtCustNote.Font = new Font("Segoe UI", 9F);
        txtCustNote.Location = new Point(15, 165);
        txtCustNote.Margin = new Padding(4, 5, 4, 5);
        txtCustNote.MaxLength = 1000;
        txtCustNote.Multiline = true;
        txtCustNote.Name = "txtCustNote";
        txtCustNote.PlaceholderText = "Ghi chú nội bộ";
        txtCustNote.SelectedText = "";
        txtCustNote.ShadowDecoration.CustomizableEdges = customizableEdges22;
        txtCustNote.Size = new Size(325, 75);
        txtCustNote.TabIndex = 6;
        // 
        // btnSaveCustomer
        // 
        btnSaveCustomer.BorderRadius = 8;
        btnSaveCustomer.CustomizableEdges = customizableEdges23;
        btnSaveCustomer.FillColor = Color.FromArgb(19, 132, 111);
        btnSaveCustomer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnSaveCustomer.ForeColor = Color.White;
        btnSaveCustomer.HoverState.FillColor = Color.FromArgb(16, 112, 94);
        btnSaveCustomer.Location = new Point(350, 190);
        btnSaveCustomer.Name = "btnSaveCustomer";
        btnSaveCustomer.ShadowDecoration.CustomizableEdges = customizableEdges24;
        btnSaveCustomer.Size = new Size(105, 40);
        btnSaveCustomer.TabIndex = 7;
        btnSaveCustomer.Text = "Lưu";
        // 
        // pnlCustomerHistory
        // 
        pnlCustomerHistory.BackColor = Color.White;
        pnlCustomerHistory.BorderRadius = 12;
        pnlCustomerHistory.Controls.Add(tabCustomerHistory);
        pnlCustomerHistory.CustomizableEdges = customizableEdges27;
        pnlCustomerHistory.FillColor = Color.White;
        pnlCustomerHistory.Location = new Point(418, 290);
        pnlCustomerHistory.Name = "pnlCustomerHistory";
        pnlCustomerHistory.ShadowDecoration.CustomizableEdges = customizableEdges28;
        pnlCustomerHistory.Size = new Size(466, 344);
        pnlCustomerHistory.TabIndex = 2;
        // 
        // tabCustomerHistory
        // 
        tabCustomerHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabCustomerHistory.Controls.Add(tabCustomerBookings);
        tabCustomerHistory.Controls.Add(tabCustomerVouchers);
        tabCustomerHistory.Location = new Point(8, 8);
        tabCustomerHistory.Name = "tabCustomerHistory";
        tabCustomerHistory.SelectedIndex = 0;
        tabCustomerHistory.Size = new Size(450, 328);
        tabCustomerHistory.TabIndex = 0;
        // 
        // tabCustomerBookings
        // 
        tabCustomerBookings.Controls.Add(dgvCustomerBookings);
        tabCustomerBookings.Location = new Point(4, 34);
        tabCustomerBookings.Name = "tabCustomerBookings";
        tabCustomerBookings.Size = new Size(442, 290);
        tabCustomerBookings.TabIndex = 0;
        tabCustomerBookings.Text = "Đơn đặt phòng";
        // 
        // dgvCustomerBookings
        // 
        dgvCustomerBookings.AllowUserToAddRows = false;
        dgvCustomerBookings.AllowUserToDeleteRows = false;
        dataGridViewCellStyle4.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomerBookings.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
        dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle5.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle5.ForeColor = Color.White;
        dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
        dgvCustomerBookings.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
        dgvCustomerBookings.ColumnHeadersHeight = 34;
        dgvCustomerBookings.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle6.BackColor = Color.White;
        dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle6.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
        dgvCustomerBookings.DefaultCellStyle = dataGridViewCellStyle6;
        dgvCustomerBookings.Dock = DockStyle.Fill;
        dgvCustomerBookings.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomerBookings.Location = new Point(0, 0);
        dgvCustomerBookings.Name = "dgvCustomerBookings";
        dgvCustomerBookings.ReadOnly = true;
        dgvCustomerBookings.RowHeadersVisible = false;
        dgvCustomerBookings.RowHeadersWidth = 62;
        dgvCustomerBookings.RowTemplate.Height = 30;
        dgvCustomerBookings.Size = new Size(442, 290);
        dgvCustomerBookings.TabIndex = 0;
        dgvCustomerBookings.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomerBookings.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomerBookings.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvCustomerBookings.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvCustomerBookings.ThemeStyle.HeaderStyle.Height = 34;
        dgvCustomerBookings.ThemeStyle.ReadOnly = true;
        dgvCustomerBookings.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvCustomerBookings.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvCustomerBookings.ThemeStyle.RowsStyle.Height = 30;
        // 
        // tabCustomerVouchers
        // 
        tabCustomerVouchers.Controls.Add(dgvCustomerVouchers);
        tabCustomerVouchers.Location = new Point(4, 34);
        tabCustomerVouchers.Name = "tabCustomerVouchers";
        tabCustomerVouchers.Size = new Size(442, 290);
        tabCustomerVouchers.TabIndex = 1;
        tabCustomerVouchers.Text = "Voucher";
        // 
        // dgvCustomerVouchers
        // 
        dgvCustomerVouchers.AllowUserToAddRows = false;
        dgvCustomerVouchers.AllowUserToDeleteRows = false;
        dataGridViewCellStyle7.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomerVouchers.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle7;
        dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle8.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle8.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle8.ForeColor = Color.White;
        dataGridViewCellStyle8.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle8.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle8.WrapMode = DataGridViewTriState.True;
        dgvCustomerVouchers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
        dgvCustomerVouchers.ColumnHeadersHeight = 34;
        dgvCustomerVouchers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle9.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle9.BackColor = Color.White;
        dataGridViewCellStyle9.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle9.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle9.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle9.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle9.WrapMode = DataGridViewTriState.False;
        dgvCustomerVouchers.DefaultCellStyle = dataGridViewCellStyle9;
        dgvCustomerVouchers.Dock = DockStyle.Fill;
        dgvCustomerVouchers.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomerVouchers.Location = new Point(0, 0);
        dgvCustomerVouchers.Name = "dgvCustomerVouchers";
        dgvCustomerVouchers.ReadOnly = true;
        dgvCustomerVouchers.RowHeadersVisible = false;
        dgvCustomerVouchers.RowHeadersWidth = 62;
        dgvCustomerVouchers.RowTemplate.Height = 30;
        dgvCustomerVouchers.Size = new Size(442, 290);
        dgvCustomerVouchers.TabIndex = 0;
        dgvCustomerVouchers.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvCustomerVouchers.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvCustomerVouchers.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvCustomerVouchers.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvCustomerVouchers.ThemeStyle.HeaderStyle.Height = 34;
        dgvCustomerVouchers.ThemeStyle.ReadOnly = true;
        dgvCustomerVouchers.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvCustomerVouchers.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvCustomerVouchers.ThemeStyle.RowsStyle.Height = 30;
        // 
        // FrmCustomerManagement
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(243, 247, 246);
        ClientSize = new Size(900, 650);
        Controls.Add(pnlCustomerLeft);
        Controls.Add(pnlCustomerProfile);
        Controls.Add(pnlCustomerHistory);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(900, 650);
        Name = "FrmCustomerManagement";
        Text = "Hồ sơ & Lịch sử Hoạt động Khách hàng";
        pnlCustomerLeft.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
        pnlCustomerProfile.ResumeLayout(false);
        pnlCustomerHistory.ResumeLayout(false);
        tabCustomerHistory.ResumeLayout(false);
        tabCustomerBookings.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvCustomerBookings).EndInit();
        tabCustomerVouchers.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvCustomerVouchers).EndInit();
        ResumeLayout(false);
    }
}






