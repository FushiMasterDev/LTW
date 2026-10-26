#nullable enable
using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
public partial class FrmBookingManagement
{
 private System.ComponentModel.IContainer? components; private Guna2Panel pnlFilter=null!,pnlBookingList=null!,pnlBookingDetails=null!,pnlBookingActions=null!; private Guna2TextBox txtSearchQ=null!,txtStatusNote=null!,txtNewRoomId=null!; private Guna2ComboBox cboStatus=null!,cboAssignTo=null!,cboTargetStatus=null!; private Guna2DataGridView dgvBookings=null!,dgvBookingNights=null!,dgvStatusHistory=null!,dgvPayments=null!,dgvChangeRequests=null!,dgvAuditLogs=null!; private Guna2Button btnSearch=null!,btnAssignBooking=null!,btnUpdateStatus=null!,btnPrev=null!,btnNext=null!; private Label lblPage=null!; private TabControl tabBookingDetails=null!; private TabPage tabNights=null!,tabStatusHistory=null!,tabPayments=null!,tabChangeRequests=null!,tabAuditLogs=null!;
 protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}
    private void InitializeComponent()
    {
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges37 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges38 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges31 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges32 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges33 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges34 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges35 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges36 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges43 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges44 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle19 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle20 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle21 = new DataGridViewCellStyle();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges39 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges40 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges41 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges42 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges45 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges46 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle22 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle23 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle24 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle25 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle26 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle27 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle28 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle29 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle30 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle31 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle32 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle33 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle34 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle35 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle36 = new DataGridViewCellStyle();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges59 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges60 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges47 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges48 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges49 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges50 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges51 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges52 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges53 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges54 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges55 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges56 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges57 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges58 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        pnlFilter = new Guna2Panel();
        txtSearchQ = new Guna2TextBox();
        cboStatus = new Guna2ComboBox();
        btnSearch = new Guna2Button();
        pnlBookingList = new Guna2Panel();
        dgvBookings = new Guna2DataGridView();
        btnPrev = new Guna2Button();
        btnNext = new Guna2Button();
        lblPage = new Label();
        pnlBookingDetails = new Guna2Panel();
        tabBookingDetails = new TabControl();
        tabNights = new TabPage();
        dgvBookingNights = new Guna2DataGridView();
        tabStatusHistory = new TabPage();
        dgvStatusHistory = new Guna2DataGridView();
        tabPayments = new TabPage();
        dgvPayments = new Guna2DataGridView();
        tabChangeRequests = new TabPage();
        dgvChangeRequests = new Guna2DataGridView();
        tabAuditLogs = new TabPage();
        dgvAuditLogs = new Guna2DataGridView();
        pnlBookingActions = new Guna2Panel();
        cboAssignTo = new Guna2ComboBox();
        btnAssignBooking = new Guna2Button();
        cboTargetStatus = new Guna2ComboBox();
        txtNewRoomId = new Guna2TextBox();
        txtStatusNote = new Guna2TextBox();
        btnUpdateStatus = new Guna2Button();
        pnlFilter.SuspendLayout();
        pnlBookingList.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvBookings).BeginInit();
        pnlBookingDetails.SuspendLayout();
        tabBookingDetails.SuspendLayout();
        tabNights.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvBookingNights).BeginInit();
        tabStatusHistory.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvStatusHistory).BeginInit();
        tabPayments.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPayments).BeginInit();
        tabChangeRequests.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvChangeRequests).BeginInit();
        tabAuditLogs.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).BeginInit();
        pnlBookingActions.SuspendLayout();
        SuspendLayout();
        // 
        // pnlFilter
        // 
        pnlFilter.BackColor = Color.White;
        pnlFilter.BorderRadius = 12;
        pnlFilter.Controls.Add(txtSearchQ);
        pnlFilter.Controls.Add(cboStatus);
        pnlFilter.Controls.Add(btnSearch);
        pnlFilter.CustomizableEdges = customizableEdges37;
        pnlFilter.FillColor = Color.White;
        pnlFilter.Location = new Point(16, 16);
        pnlFilter.Name = "pnlFilter";
        pnlFilter.ShadowDecoration.CustomizableEdges = customizableEdges38;
        pnlFilter.Size = new Size(868, 76);
        pnlFilter.TabIndex = 0;
        // 
        // txtSearchQ
        // 
        txtSearchQ.BorderColor = Color.FromArgb(220, 227, 231);
        txtSearchQ.BorderRadius = 8;
        txtSearchQ.CustomizableEdges = customizableEdges31;
        txtSearchQ.DefaultText = "";
        txtSearchQ.Font = new Font("Segoe UI", 9F);
        txtSearchQ.Location = new Point(12, 18);
        txtSearchQ.Margin = new Padding(4, 5, 4, 5);
        txtSearchQ.Name = "txtSearchQ";
        txtSearchQ.PlaceholderText = "Mã đơn, tên hoặc số điện thoại";
        txtSearchQ.SelectedText = "";
        txtSearchQ.ShadowDecoration.CustomizableEdges = customizableEdges32;
        txtSearchQ.Size = new Size(300, 28);
        txtSearchQ.TabIndex = 0;
        // 
        // cboStatus
        // 
        cboStatus.BackColor = Color.Transparent;
        cboStatus.BorderColor = Color.FromArgb(220, 227, 231);
        cboStatus.BorderRadius = 8;
        cboStatus.CustomizableEdges = customizableEdges33;
        cboStatus.DrawMode = DrawMode.OwnerDrawFixed;
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.FocusedColor = Color.Empty;
        cboStatus.Font = new Font("Segoe UI", 9F);
        cboStatus.ForeColor = Color.FromArgb(68, 88, 112);
        cboStatus.ItemHeight = 28;
        cboStatus.Items.AddRange(new object[] { "Tất cả", "AwaitingPayment", "PaymentSubmitted", "Deposited", "Confirmed", "CheckedIn", "Completed", "Expired", "Rejected", "Cancelled", "NoShow" });
        cboStatus.Location = new Point(325, 18);
        cboStatus.Name = "cboStatus";
        cboStatus.ShadowDecoration.CustomizableEdges = customizableEdges34;
        cboStatus.Size = new Size(250, 34);
        cboStatus.TabIndex = 1;
        // 
        // btnSearch
        // 
        btnSearch.BorderRadius = 8;
        btnSearch.CustomizableEdges = customizableEdges35;
        btnSearch.FillColor = Color.FromArgb(19, 132, 111);
        btnSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnSearch.ForeColor = Color.White;
        btnSearch.HoverState.FillColor = Color.FromArgb(16, 112, 94);
        btnSearch.Location = new Point(700, 14);
        btnSearch.Name = "btnSearch";
        btnSearch.ShadowDecoration.CustomizableEdges = customizableEdges36;
        btnSearch.Size = new Size(150, 36);
        btnSearch.TabIndex = 2;
        btnSearch.Text = "Tìm kiếm";
        // 
        // pnlBookingList
        // 
        pnlBookingList.BackColor = Color.White;
        pnlBookingList.BorderRadius = 12;
        pnlBookingList.Controls.Add(dgvBookings);
        pnlBookingList.Controls.Add(btnPrev);
        pnlBookingList.Controls.Add(btnNext);
        pnlBookingList.Controls.Add(lblPage);
        pnlBookingList.CustomizableEdges = customizableEdges43;
        pnlBookingList.FillColor = Color.White;
        pnlBookingList.Location = new Point(16, 104);
        pnlBookingList.Name = "pnlBookingList";
        pnlBookingList.ShadowDecoration.CustomizableEdges = customizableEdges44;
        pnlBookingList.Size = new Size(868, 190);
        pnlBookingList.TabIndex = 1;
        // 
        // dgvBookings
        // 
        dgvBookings.AllowUserToAddRows = false;
        dgvBookings.AllowUserToDeleteRows = false;
        dataGridViewCellStyle19.BackColor = Color.FromArgb(243, 247, 246);
        dgvBookings.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle19;
        dgvBookings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        dataGridViewCellStyle20.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle20.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle20.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle20.ForeColor = Color.White;
        dataGridViewCellStyle20.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle20.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle20.WrapMode = DataGridViewTriState.True;
        dgvBookings.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle20;
        dgvBookings.ColumnHeadersHeight = 34;
        dgvBookings.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle21.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle21.BackColor = Color.White;
        dataGridViewCellStyle21.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle21.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle21.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle21.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle21.WrapMode = DataGridViewTriState.False;
        dgvBookings.DefaultCellStyle = dataGridViewCellStyle21;
        dgvBookings.GridColor = Color.FromArgb(234, 240, 238);
        dgvBookings.Location = new Point(8, 8);
        dgvBookings.Name = "dgvBookings";
        dgvBookings.ReadOnly = true;
        dgvBookings.RowHeadersVisible = false;
        dgvBookings.RowHeadersWidth = 62;
        dgvBookings.RowTemplate.Height = 30;
        dgvBookings.Size = new Size(854, 140);
        dgvBookings.TabIndex = 0;
        dgvBookings.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvBookings.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvBookings.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvBookings.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvBookings.ThemeStyle.HeaderStyle.Height = 34;
        dgvBookings.ThemeStyle.ReadOnly = true;
        dgvBookings.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvBookings.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvBookings.ThemeStyle.RowsStyle.Height = 30;
        // 
        // btnPrev
        // 
        btnPrev.BorderRadius = 8;
        btnPrev.CustomizableEdges = customizableEdges39;
        btnPrev.FillColor = Color.FromArgb(239, 246, 244);
        btnPrev.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnPrev.ForeColor = Color.FromArgb(19, 132, 111);
        btnPrev.Location = new Point(650, 151);
        btnPrev.Name = "btnPrev";
        btnPrev.ShadowDecoration.CustomizableEdges = customizableEdges40;
        btnPrev.Size = new Size(80, 30);
        btnPrev.TabIndex = 1;
        btnPrev.Text = "Trước";
        // 
        // btnNext
        // 
        btnNext.BorderRadius = 8;
        btnNext.CustomizableEdges = customizableEdges41;
        btnNext.FillColor = Color.FromArgb(239, 246, 244);
        btnNext.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNext.ForeColor = Color.FromArgb(19, 132, 111);
        btnNext.Location = new Point(740, 151);
        btnNext.Name = "btnNext";
        btnNext.ShadowDecoration.CustomizableEdges = customizableEdges42;
        btnNext.Size = new Size(80, 30);
        btnNext.TabIndex = 2;
        btnNext.Text = "Sau";
        // 
        // lblPage
        // 
        lblPage.Location = new Point(10, 154);
        lblPage.Name = "lblPage";
        lblPage.Size = new Size(250, 24);
        lblPage.TabIndex = 3;
        lblPage.Text = "Trang 1";
        // 
        // pnlBookingDetails
        // 
        pnlBookingDetails.BackColor = Color.White;
        pnlBookingDetails.BorderRadius = 12;
        pnlBookingDetails.Controls.Add(tabBookingDetails);
        pnlBookingDetails.CustomizableEdges = customizableEdges45;
        pnlBookingDetails.FillColor = Color.White;
        pnlBookingDetails.Location = new Point(16, 306);
        pnlBookingDetails.Name = "pnlBookingDetails";
        pnlBookingDetails.ShadowDecoration.CustomizableEdges = customizableEdges46;
        pnlBookingDetails.Size = new Size(868, 198);
        pnlBookingDetails.TabIndex = 2;
        // 
        // tabBookingDetails
        // 
        tabBookingDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabBookingDetails.Controls.Add(tabNights);
        tabBookingDetails.Controls.Add(tabStatusHistory);
        tabBookingDetails.Controls.Add(tabPayments);
        tabBookingDetails.Controls.Add(tabChangeRequests);
        tabBookingDetails.Controls.Add(tabAuditLogs);
        tabBookingDetails.Location = new Point(8, 8);
        tabBookingDetails.Name = "tabBookingDetails";
        tabBookingDetails.SelectedIndex = 0;
        tabBookingDetails.Size = new Size(852, 182);
        tabBookingDetails.TabIndex = 0;
        // 
        // tabNights
        // 
        tabNights.Controls.Add(dgvBookingNights);
        tabNights.Location = new Point(4, 34);
        tabNights.Name = "tabNights";
        tabNights.Size = new Size(844, 144);
        tabNights.TabIndex = 0;
        tabNights.Text = "Giá từng đêm";
        // 
        // dgvBookingNights
        // 
        dgvBookingNights.AllowUserToAddRows = false;
        dgvBookingNights.AllowUserToDeleteRows = false;
        dataGridViewCellStyle22.BackColor = Color.FromArgb(243, 247, 246);
        dgvBookingNights.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle22;
        dataGridViewCellStyle23.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle23.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle23.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle23.ForeColor = Color.White;
        dataGridViewCellStyle23.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle23.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle23.WrapMode = DataGridViewTriState.True;
        dgvBookingNights.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle23;
        dgvBookingNights.ColumnHeadersHeight = 34;
        dgvBookingNights.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle24.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle24.BackColor = Color.White;
        dataGridViewCellStyle24.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle24.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle24.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle24.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle24.WrapMode = DataGridViewTriState.False;
        dgvBookingNights.DefaultCellStyle = dataGridViewCellStyle24;
        dgvBookingNights.Dock = DockStyle.Fill;
        dgvBookingNights.GridColor = Color.FromArgb(234, 240, 238);
        dgvBookingNights.Location = new Point(0, 0);
        dgvBookingNights.Name = "dgvBookingNights";
        dgvBookingNights.ReadOnly = true;
        dgvBookingNights.RowHeadersVisible = false;
        dgvBookingNights.RowHeadersWidth = 62;
        dgvBookingNights.RowTemplate.Height = 30;
        dgvBookingNights.Size = new Size(844, 144);
        dgvBookingNights.TabIndex = 0;
        dgvBookingNights.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvBookingNights.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvBookingNights.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvBookingNights.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvBookingNights.ThemeStyle.HeaderStyle.Height = 34;
        dgvBookingNights.ThemeStyle.ReadOnly = true;
        dgvBookingNights.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvBookingNights.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvBookingNights.ThemeStyle.RowsStyle.Height = 30;
        // 
        // tabStatusHistory
        // 
        tabStatusHistory.Controls.Add(dgvStatusHistory);
        tabStatusHistory.Location = new Point(4, 34);
        tabStatusHistory.Name = "tabStatusHistory";
        tabStatusHistory.Size = new Size(844, 144);
        tabStatusHistory.TabIndex = 1;
        tabStatusHistory.Text = "Trạng thái";
        // 
        // dgvStatusHistory
        // 
        dgvStatusHistory.AllowUserToAddRows = false;
        dgvStatusHistory.AllowUserToDeleteRows = false;
        dataGridViewCellStyle25.BackColor = Color.FromArgb(243, 247, 246);
        dgvStatusHistory.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle25;
        dataGridViewCellStyle26.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle26.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle26.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle26.ForeColor = Color.White;
        dataGridViewCellStyle26.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle26.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle26.WrapMode = DataGridViewTriState.True;
        dgvStatusHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle26;
        dgvStatusHistory.ColumnHeadersHeight = 34;
        dgvStatusHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle27.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle27.BackColor = Color.White;
        dataGridViewCellStyle27.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle27.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle27.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle27.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle27.WrapMode = DataGridViewTriState.False;
        dgvStatusHistory.DefaultCellStyle = dataGridViewCellStyle27;
        dgvStatusHistory.Dock = DockStyle.Fill;
        dgvStatusHistory.GridColor = Color.FromArgb(234, 240, 238);
        dgvStatusHistory.Location = new Point(0, 0);
        dgvStatusHistory.Name = "dgvStatusHistory";
        dgvStatusHistory.ReadOnly = true;
        dgvStatusHistory.RowHeadersVisible = false;
        dgvStatusHistory.RowHeadersWidth = 62;
        dgvStatusHistory.RowTemplate.Height = 30;
        dgvStatusHistory.Size = new Size(844, 144);
        dgvStatusHistory.TabIndex = 0;
        dgvStatusHistory.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvStatusHistory.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvStatusHistory.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvStatusHistory.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvStatusHistory.ThemeStyle.HeaderStyle.Height = 34;
        dgvStatusHistory.ThemeStyle.ReadOnly = true;
        dgvStatusHistory.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvStatusHistory.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvStatusHistory.ThemeStyle.RowsStyle.Height = 30;
        // 
        // tabPayments
        // 
        tabPayments.Controls.Add(dgvPayments);
        tabPayments.Location = new Point(4, 34);
        tabPayments.Name = "tabPayments";
        tabPayments.Size = new Size(844, 144);
        tabPayments.TabIndex = 2;
        tabPayments.Text = "Thanh toán & Bill";
        // 
        // dgvPayments
        // 
        dgvPayments.AllowUserToAddRows = false;
        dgvPayments.AllowUserToDeleteRows = false;
        dataGridViewCellStyle28.BackColor = Color.FromArgb(243, 247, 246);
        dgvPayments.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle28;
        dataGridViewCellStyle29.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle29.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle29.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle29.ForeColor = Color.White;
        dataGridViewCellStyle29.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle29.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle29.WrapMode = DataGridViewTriState.True;
        dgvPayments.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle29;
        dgvPayments.ColumnHeadersHeight = 34;
        dgvPayments.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle30.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle30.BackColor = Color.White;
        dataGridViewCellStyle30.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle30.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle30.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle30.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle30.WrapMode = DataGridViewTriState.False;
        dgvPayments.DefaultCellStyle = dataGridViewCellStyle30;
        dgvPayments.Dock = DockStyle.Fill;
        dgvPayments.GridColor = Color.FromArgb(234, 240, 238);
        dgvPayments.Location = new Point(0, 0);
        dgvPayments.Name = "dgvPayments";
        dgvPayments.ReadOnly = true;
        dgvPayments.RowHeadersVisible = false;
        dgvPayments.RowHeadersWidth = 62;
        dgvPayments.RowTemplate.Height = 30;
        dgvPayments.Size = new Size(844, 144);
        dgvPayments.TabIndex = 0;
        dgvPayments.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvPayments.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvPayments.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvPayments.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvPayments.ThemeStyle.HeaderStyle.Height = 34;
        dgvPayments.ThemeStyle.ReadOnly = true;
        dgvPayments.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvPayments.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvPayments.ThemeStyle.RowsStyle.Height = 30;
        // 
        // tabChangeRequests
        // 
        tabChangeRequests.Controls.Add(dgvChangeRequests);
        tabChangeRequests.Location = new Point(4, 34);
        tabChangeRequests.Name = "tabChangeRequests";
        tabChangeRequests.Size = new Size(844, 144);
        tabChangeRequests.TabIndex = 3;
        tabChangeRequests.Text = "Đổi/Hủy";
        // 
        // dgvChangeRequests
        // 
        dgvChangeRequests.AllowUserToAddRows = false;
        dgvChangeRequests.AllowUserToDeleteRows = false;
        dataGridViewCellStyle31.BackColor = Color.FromArgb(243, 247, 246);
        dgvChangeRequests.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle31;
        dataGridViewCellStyle32.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle32.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle32.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle32.ForeColor = Color.White;
        dataGridViewCellStyle32.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle32.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle32.WrapMode = DataGridViewTriState.True;
        dgvChangeRequests.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle32;
        dgvChangeRequests.ColumnHeadersHeight = 34;
        dgvChangeRequests.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle33.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle33.BackColor = Color.White;
        dataGridViewCellStyle33.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle33.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle33.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle33.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle33.WrapMode = DataGridViewTriState.False;
        dgvChangeRequests.DefaultCellStyle = dataGridViewCellStyle33;
        dgvChangeRequests.Dock = DockStyle.Fill;
        dgvChangeRequests.GridColor = Color.FromArgb(234, 240, 238);
        dgvChangeRequests.Location = new Point(0, 0);
        dgvChangeRequests.Name = "dgvChangeRequests";
        dgvChangeRequests.ReadOnly = true;
        dgvChangeRequests.RowHeadersVisible = false;
        dgvChangeRequests.RowHeadersWidth = 62;
        dgvChangeRequests.RowTemplate.Height = 30;
        dgvChangeRequests.Size = new Size(844, 144);
        dgvChangeRequests.TabIndex = 0;
        dgvChangeRequests.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvChangeRequests.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvChangeRequests.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvChangeRequests.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvChangeRequests.ThemeStyle.HeaderStyle.Height = 34;
        dgvChangeRequests.ThemeStyle.ReadOnly = true;
        dgvChangeRequests.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvChangeRequests.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvChangeRequests.ThemeStyle.RowsStyle.Height = 30;
        // 
        // tabAuditLogs
        // 
        tabAuditLogs.Controls.Add(dgvAuditLogs);
        tabAuditLogs.Location = new Point(4, 34);
        tabAuditLogs.Name = "tabAuditLogs";
        tabAuditLogs.Size = new Size(844, 144);
        tabAuditLogs.TabIndex = 4;
        tabAuditLogs.Text = "Nhật ký";
        // 
        // dgvAuditLogs
        // 
        dgvAuditLogs.AllowUserToAddRows = false;
        dgvAuditLogs.AllowUserToDeleteRows = false;
        dataGridViewCellStyle34.BackColor = Color.FromArgb(243, 247, 246);
        dgvAuditLogs.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle34;
        dataGridViewCellStyle35.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle35.BackColor = Color.FromArgb(19, 132, 111);
        dataGridViewCellStyle35.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle35.ForeColor = Color.White;
        dataGridViewCellStyle35.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle35.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle35.WrapMode = DataGridViewTriState.True;
        dgvAuditLogs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle35;
        dgvAuditLogs.ColumnHeadersHeight = 34;
        dgvAuditLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle36.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle36.BackColor = Color.White;
        dataGridViewCellStyle36.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle36.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle36.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle36.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle36.WrapMode = DataGridViewTriState.False;
        dgvAuditLogs.DefaultCellStyle = dataGridViewCellStyle36;
        dgvAuditLogs.Dock = DockStyle.Fill;
        dgvAuditLogs.GridColor = Color.FromArgb(234, 240, 238);
        dgvAuditLogs.Location = new Point(0, 0);
        dgvAuditLogs.Name = "dgvAuditLogs";
        dgvAuditLogs.ReadOnly = true;
        dgvAuditLogs.RowHeadersVisible = false;
        dgvAuditLogs.RowHeadersWidth = 62;
        dgvAuditLogs.RowTemplate.Height = 30;
        dgvAuditLogs.Size = new Size(844, 144);
        dgvAuditLogs.TabIndex = 0;
        dgvAuditLogs.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(243, 247, 246);
        dgvAuditLogs.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvAuditLogs.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(19, 132, 111);
        dgvAuditLogs.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvAuditLogs.ThemeStyle.HeaderStyle.Height = 34;
        dgvAuditLogs.ThemeStyle.ReadOnly = true;
        dgvAuditLogs.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvAuditLogs.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvAuditLogs.ThemeStyle.RowsStyle.Height = 30;
        // 
        // pnlBookingActions
        // 
        pnlBookingActions.BackColor = Color.White;
        pnlBookingActions.BorderRadius = 12;
        pnlBookingActions.Controls.Add(cboAssignTo);
        pnlBookingActions.Controls.Add(btnAssignBooking);
        pnlBookingActions.Controls.Add(cboTargetStatus);
        pnlBookingActions.Controls.Add(txtNewRoomId);
        pnlBookingActions.Controls.Add(txtStatusNote);
        pnlBookingActions.Controls.Add(btnUpdateStatus);
        pnlBookingActions.CustomizableEdges = customizableEdges59;
        pnlBookingActions.FillColor = Color.White;
        pnlBookingActions.Location = new Point(16, 516);
        pnlBookingActions.Name = "pnlBookingActions";
        pnlBookingActions.ShadowDecoration.CustomizableEdges = customizableEdges60;
        pnlBookingActions.Size = new Size(868, 118);
        pnlBookingActions.TabIndex = 3;
        // 
        // cboAssignTo
        // 
        cboAssignTo.BackColor = Color.Transparent;
        cboAssignTo.BorderColor = Color.FromArgb(220, 227, 231);
        cboAssignTo.BorderRadius = 8;
        cboAssignTo.CustomizableEdges = customizableEdges47;
        cboAssignTo.DrawMode = DrawMode.OwnerDrawFixed;
        cboAssignTo.DropDownStyle = ComboBoxStyle.DropDownList;
        cboAssignTo.FocusedColor = Color.Empty;
        cboAssignTo.Font = new Font("Segoe UI", 9F);
        cboAssignTo.ForeColor = Color.FromArgb(68, 88, 112);
        cboAssignTo.ItemHeight = 28;
        cboAssignTo.Location = new Point(12, 12);
        cboAssignTo.Name = "cboAssignTo";
        cboAssignTo.ShadowDecoration.CustomizableEdges = customizableEdges48;
        cboAssignTo.Size = new Size(180, 34);
        cboAssignTo.TabIndex = 0;
        // 
        // btnAssignBooking
        // 
        btnAssignBooking.BorderRadius = 8;
        btnAssignBooking.CustomizableEdges = customizableEdges49;
        btnAssignBooking.FillColor = Color.FromArgb(19, 132, 111);
        btnAssignBooking.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnAssignBooking.ForeColor = Color.White;
        btnAssignBooking.HoverState.FillColor = Color.FromArgb(16, 112, 94);
        btnAssignBooking.Location = new Point(205, 10);
        btnAssignBooking.Name = "btnAssignBooking";
        btnAssignBooking.ShadowDecoration.CustomizableEdges = customizableEdges50;
        btnAssignBooking.Size = new Size(180, 34);
        btnAssignBooking.TabIndex = 1;
        btnAssignBooking.Text = "Giao đơn / Nhận phụ trách";
        // 
        // cboTargetStatus
        // 
        cboTargetStatus.BackColor = Color.Transparent;
        cboTargetStatus.BorderColor = Color.FromArgb(220, 227, 231);
        cboTargetStatus.BorderRadius = 8;
        cboTargetStatus.CustomizableEdges = customizableEdges51;
        cboTargetStatus.DrawMode = DrawMode.OwnerDrawFixed;
        cboTargetStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTargetStatus.FocusedColor = Color.Empty;
        cboTargetStatus.Font = new Font("Segoe UI", 9F);
        cboTargetStatus.ForeColor = Color.FromArgb(68, 88, 112);
        cboTargetStatus.ItemHeight = 28;
        cboTargetStatus.Items.AddRange(new object[] { "Confirmed", "Rejected", "CheckedIn", "Completed", "NoShow" });
        cboTargetStatus.Location = new Point(400, 12);
        cboTargetStatus.Name = "cboTargetStatus";
        cboTargetStatus.ShadowDecoration.CustomizableEdges = customizableEdges52;
        cboTargetStatus.Size = new Size(150, 34);
        cboTargetStatus.TabIndex = 2;
        // 
        // txtNewRoomId
        // 
        txtNewRoomId.BorderColor = Color.FromArgb(220, 227, 231);
        txtNewRoomId.BorderRadius = 8;
        txtNewRoomId.CustomizableEdges = customizableEdges53;
        txtNewRoomId.DefaultText = "";
        txtNewRoomId.Font = new Font("Segoe UI", 9F);
        txtNewRoomId.Location = new Point(560, 12);
        txtNewRoomId.Margin = new Padding(4, 5, 4, 5);
        txtNewRoomId.Name = "txtNewRoomId";
        txtNewRoomId.PlaceholderText = "";
        txtNewRoomId.SelectedText = "";
        txtNewRoomId.ShadowDecoration.CustomizableEdges = customizableEdges54;
        txtNewRoomId.Size = new Size(80, 28);
        txtNewRoomId.TabIndex = 3;
        // 
        // txtStatusNote
        // 
        txtStatusNote.BorderColor = Color.FromArgb(220, 227, 231);
        txtStatusNote.BorderRadius = 8;
        txtStatusNote.CustomizableEdges = customizableEdges55;
        txtStatusNote.DefaultText = "";
        txtStatusNote.Font = new Font("Segoe UI", 9F);
        txtStatusNote.Location = new Point(12, 55);
        txtStatusNote.Margin = new Padding(4, 5, 4, 5);
        txtStatusNote.Multiline = true;
        txtStatusNote.Name = "txtStatusNote";
        txtStatusNote.PlaceholderText = "Ghi chú bắt buộc";
        txtStatusNote.SelectedText = "";
        txtStatusNote.ShadowDecoration.CustomizableEdges = customizableEdges56;
        txtStatusNote.Size = new Size(620, 42);
        txtStatusNote.TabIndex = 4;
        // 
        // btnUpdateStatus
        // 
        btnUpdateStatus.BorderRadius = 8;
        btnUpdateStatus.CustomizableEdges = customizableEdges57;
        btnUpdateStatus.FillColor = Color.FromArgb(19, 132, 111);
        btnUpdateStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnUpdateStatus.ForeColor = Color.White;
        btnUpdateStatus.HoverState.FillColor = Color.FromArgb(16, 112, 94);
        btnUpdateStatus.Location = new Point(650, 56);
        btnUpdateStatus.Name = "btnUpdateStatus";
        btnUpdateStatus.ShadowDecoration.CustomizableEdges = customizableEdges58;
        btnUpdateStatus.Size = new Size(200, 38);
        btnUpdateStatus.TabIndex = 5;
        btnUpdateStatus.Text = "Cập nhật trạng thái";
        // 
        // FrmBookingManagement
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(243, 247, 246);
        ClientSize = new Size(907, 650);
        Controls.Add(pnlFilter);
        Controls.Add(pnlBookingList);
        Controls.Add(pnlBookingDetails);
        Controls.Add(pnlBookingActions);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(900, 650);
        Name = "FrmBookingManagement";
        Text = "Quản lý Đơn đặt phòng & Theo dõi Tiến độ";
        pnlFilter.ResumeLayout(false);
        pnlBookingList.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvBookings).EndInit();
        pnlBookingDetails.ResumeLayout(false);
        tabBookingDetails.ResumeLayout(false);
        tabNights.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvBookingNights).EndInit();
        tabStatusHistory.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvStatusHistory).EndInit();
        tabPayments.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvPayments).EndInit();
        tabChangeRequests.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvChangeRequests).EndInit();
        tabAuditLogs.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).EndInit();
        pnlBookingActions.ResumeLayout(false);
        ResumeLayout(false);
    }
}








