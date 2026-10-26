using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
partial class FrmBankAndAdditionalDeposit
{
 private Guna2TabControl tabs=null!; private TabPage tabTransactions=null!,tabDeposits=null!; private Guna2DataGridView dgvTransactions=null!,dgvDeposits=null!; private Guna2TextBox txtRefundBankRef=null!,txtFinanceNote=null!,txtRejectionReason=null!,txtTransactionNote=null!; private Guna2ComboBox cboMatchStatus=null!; private Guna2TextBox txtMatchBookingCode=null!; private Guna2Button btnMatchTransaction=null!,btnIgnoreTransaction=null!; private Guna2Button btnMarkRefunded=null!,btnConfirmDeposit=null!,btnRejectDeposit=null!,btnRefresh=null!,btnTransactionPrev=null!,btnTransactionNext=null!,btnDepositPrev=null!,btnDepositNext=null!; private Guna2HtmlLabel lblTransactionPage=null!,lblDepositPage=null!;
    private void InitializeComponent()
    {
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges13 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges14 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
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
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges25 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges26 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        tabs = new Guna2TabControl();
        tabTransactions = new TabPage();
        cboMatchStatus = new Guna2ComboBox();
        dgvTransactions = new Guna2DataGridView();
        btnRefresh = new Guna2Button();
        lblTransactionPage = new Guna2HtmlLabel();
        btnTransactionPrev = new Guna2Button();
        btnTransactionNext = new Guna2Button();
        txtRefundBankRef = new Guna2TextBox();
        txtTransactionNote = new Guna2TextBox();
        btnMarkRefunded = new Guna2Button();
        txtMatchBookingCode = new Guna2TextBox();
        btnMatchTransaction = new Guna2Button();
        btnIgnoreTransaction = new Guna2Button();
        tabDeposits = new TabPage();
        dgvDeposits = new Guna2DataGridView();
        lblDepositPage = new Guna2HtmlLabel();
        btnDepositPrev = new Guna2Button();
        btnDepositNext = new Guna2Button();
        txtFinanceNote = new Guna2TextBox();
        txtRejectionReason = new Guna2TextBox();
        btnConfirmDeposit = new Guna2Button();
        btnRejectDeposit = new Guna2Button();
        tabs.SuspendLayout();
        tabTransactions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvTransactions).BeginInit();
        tabDeposits.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvDeposits).BeginInit();
        SuspendLayout();
        // 
        // tabs
        // 
        tabs.Alignment = TabAlignment.Left;
        tabs.Controls.Add(tabTransactions);
        tabs.Controls.Add(tabDeposits);
        tabs.Dock = DockStyle.Fill;
        tabs.ItemSize = new Size(220, 40);
        tabs.Location = new Point(0, 0);
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(1150, 763);
        tabs.TabButtonHoverState.BorderColor = Color.Empty;
        tabs.TabButtonHoverState.FillColor = Color.FromArgb(200, 90, 50);
        tabs.TabButtonHoverState.Font = new Font("Segoe UI Semibold", 10F);
        tabs.TabButtonHoverState.ForeColor = Color.White;
        tabs.TabButtonHoverState.InnerColor = Color.FromArgb(40, 52, 70);
        tabs.TabButtonIdleState.BorderColor = Color.Empty;
        tabs.TabButtonIdleState.FillColor = Color.FromArgb(33, 42, 57);
        tabs.TabButtonIdleState.Font = new Font("Segoe UI Semibold", 10F);
        tabs.TabButtonIdleState.ForeColor = Color.FromArgb(156, 160, 167);
        tabs.TabButtonIdleState.InnerColor = Color.FromArgb(33, 42, 57);
        tabs.TabButtonSelectedState.BorderColor = Color.Empty;
        tabs.TabButtonSelectedState.FillColor = Color.FromArgb(30, 58, 47);
        tabs.TabButtonSelectedState.Font = new Font("Segoe UI Semibold", 10F);
        tabs.TabButtonSelectedState.ForeColor = Color.White;
        tabs.TabButtonSelectedState.InnerColor = Color.FromArgb(76, 132, 255);
        tabs.TabButtonSize = new Size(220, 40);
        tabs.TabIndex = 0;
        tabs.TabMenuBackColor = Color.FromArgb(33, 42, 57);
        // 
        // tabTransactions
        // 
        tabTransactions.Controls.Add(cboMatchStatus);
        tabTransactions.Controls.Add(dgvTransactions);
        tabTransactions.Controls.Add(btnRefresh);
        tabTransactions.Controls.Add(lblTransactionPage);
        tabTransactions.Controls.Add(btnTransactionPrev);
        tabTransactions.Controls.Add(btnTransactionNext);
        tabTransactions.Controls.Add(txtRefundBankRef);
        tabTransactions.Controls.Add(txtTransactionNote);
        tabTransactions.Controls.Add(btnMarkRefunded);
        tabTransactions.Controls.Add(txtMatchBookingCode);
        tabTransactions.Controls.Add(btnMatchTransaction);
        tabTransactions.Controls.Add(btnIgnoreTransaction);
        tabTransactions.Location = new Point(224, 4);
        tabTransactions.Name = "tabTransactions";
        tabTransactions.Size = new Size(922, 755);
        tabTransactions.TabIndex = 0;
        tabTransactions.Text = "Giao dịch ngân hàng";
        // 
        // cboMatchStatus
        // 
        cboMatchStatus.BackColor = Color.Transparent;
        cboMatchStatus.BorderColor = Color.FromArgb(220, 227, 231);
        cboMatchStatus.BorderRadius = 11;
        cboMatchStatus.CustomizableEdges = customizableEdges1;
        cboMatchStatus.DrawMode = DrawMode.OwnerDrawFixed;
        cboMatchStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboMatchStatus.FocusedColor = Color.Empty;
        cboMatchStatus.Font = new Font("Segoe UI", 9F);
        cboMatchStatus.ForeColor = Color.FromArgb(30, 58, 47);
        cboMatchStatus.ItemHeight = 28;
        cboMatchStatus.Location = new Point(16, 14);
        cboMatchStatus.Name = "cboMatchStatus";
        cboMatchStatus.ShadowDecoration.CustomizableEdges = customizableEdges2;
        cboMatchStatus.Size = new Size(210, 34);
        cboMatchStatus.TabIndex = 1;
        // 
        // dgvTransactions
        // 
        dgvTransactions.AllowUserToAddRows = false;
        dgvTransactions.AllowUserToDeleteRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 247, 242);
        dgvTransactions.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dgvTransactions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.White;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        dgvTransactions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvTransactions.ColumnHeadersHeight = 38;
        dgvTransactions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        dgvTransactions.DefaultCellStyle = dataGridViewCellStyle3;
        dgvTransactions.GridColor = Color.FromArgb(234, 240, 238);
        dgvTransactions.Location = new Point(16, 68);
        dgvTransactions.MultiSelect = false;
        dgvTransactions.Name = "dgvTransactions";
        dgvTransactions.ReadOnly = true;
        dgvTransactions.RowHeadersVisible = false;
        dgvTransactions.RowHeadersWidth = 62;
        dgvTransactions.Size = new Size(890, 380);
        dgvTransactions.TabIndex = 0;
        dgvTransactions.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(250, 247, 242);
        dgvTransactions.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvTransactions.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(30, 58, 47);
        dgvTransactions.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvTransactions.ThemeStyle.HeaderStyle.Height = 38;
        dgvTransactions.ThemeStyle.ReadOnly = true;
        dgvTransactions.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvTransactions.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvTransactions.ThemeStyle.RowsStyle.Height = 33;
        // 
        // btnRefresh
        // 
        btnRefresh.BorderRadius = 21;
        btnRefresh.CustomizableEdges = customizableEdges3;
        btnRefresh.FillColor = Color.FromArgb(200, 90, 50);
        btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnRefresh.ForeColor = Color.White;
        btnRefresh.Location = new Point(238, 12);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.ShadowDecoration.CustomizableEdges = customizableEdges4;
        btnRefresh.Size = new Size(159, 42);
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "Tải giao dịch";
        // 
        // lblTransactionPage
        // 
        lblTransactionPage.AutoSize = false;
        lblTransactionPage.BackColor = Color.Transparent;
        lblTransactionPage.ForeColor = Color.FromArgb(104, 120, 124);
        lblTransactionPage.Location = new Point(394, 21);
        lblTransactionPage.Name = "lblTransactionPage";
        lblTransactionPage.Size = new Size(3, 2);
        lblTransactionPage.TabIndex = 3;
        lblTransactionPage.Text = "Chọn bộ lọc rồi tải giao dịch";
        // 
        // btnTransactionPrev
        // 
        btnTransactionPrev.BorderRadius = 18;
        btnTransactionPrev.CustomizableEdges = customizableEdges5;
        btnTransactionPrev.FillColor = Color.FromArgb(250, 247, 242);
        btnTransactionPrev.Font = new Font("Segoe UI", 9F);
        btnTransactionPrev.ForeColor = Color.FromArgb(30, 58, 47);
        btnTransactionPrev.Location = new Point(690, 14);
        btnTransactionPrev.Name = "btnTransactionPrev";
        btnTransactionPrev.ShadowDecoration.CustomizableEdges = customizableEdges6;
        btnTransactionPrev.Size = new Size(85, 38);
        btnTransactionPrev.TabIndex = 4;
        btnTransactionPrev.Text = "Trước";
        btnTransactionPrev.Click += btnTransactionPrev_Click;
        // 
        // btnTransactionNext
        // 
        btnTransactionNext.BorderRadius = 18;
        btnTransactionNext.CustomizableEdges = customizableEdges7;
        btnTransactionNext.FillColor = Color.FromArgb(250, 247, 242);
        btnTransactionNext.Font = new Font("Segoe UI", 9F);
        btnTransactionNext.ForeColor = Color.FromArgb(30, 58, 47);
        btnTransactionNext.Location = new Point(802, 14);
        btnTransactionNext.Name = "btnTransactionNext";
        btnTransactionNext.ShadowDecoration.CustomizableEdges = customizableEdges8;
        btnTransactionNext.Size = new Size(85, 38);
        btnTransactionNext.TabIndex = 5;
        btnTransactionNext.Text = "Sau";
        // 
        // txtRefundBankRef
        // 
        txtRefundBankRef.BorderColor = Color.FromArgb(220, 227, 231);
        txtRefundBankRef.BorderRadius = 12;
        txtRefundBankRef.CustomizableEdges = customizableEdges9;
        txtRefundBankRef.DefaultText = "";
        txtRefundBankRef.Font = new Font("Segoe UI", 9F);
        txtRefundBankRef.Location = new Point(26, 691);
        txtRefundBankRef.Margin = new Padding(4, 5, 4, 5);
        txtRefundBankRef.Name = "txtRefundBankRef";
        txtRefundBankRef.PlaceholderText = "Mã giao dịch ngân hàng hoàn trả";
        txtRefundBankRef.SelectedText = "";
        txtRefundBankRef.ShadowDecoration.CustomizableEdges = customizableEdges10;
        txtRefundBankRef.Size = new Size(291, 42);
        txtRefundBankRef.TabIndex = 6;
        // 
        // txtTransactionNote
        // 
        txtTransactionNote.BorderColor = Color.FromArgb(220, 227, 231);
        txtTransactionNote.BorderRadius = 12;
        txtTransactionNote.CustomizableEdges = customizableEdges11;
        txtTransactionNote.DefaultText = "";
        txtTransactionNote.Font = new Font("Segoe UI", 9F);
        txtTransactionNote.Location = new Point(325, 691);
        txtTransactionNote.Margin = new Padding(4, 5, 4, 5);
        txtTransactionNote.Name = "txtTransactionNote";
        txtTransactionNote.PlaceholderText = "Ghi chú (không bắt buộc)";
        txtTransactionNote.SelectedText = "";
        txtTransactionNote.ShadowDecoration.CustomizableEdges = customizableEdges12;
        txtTransactionNote.Size = new Size(300, 42);
        txtTransactionNote.TabIndex = 7;
        // 
        // btnMarkRefunded
        // 
        btnMarkRefunded.BorderRadius = 21;
        btnMarkRefunded.CustomizableEdges = customizableEdges13;
        btnMarkRefunded.Enabled = false;
        btnMarkRefunded.FillColor = Color.FromArgb(200, 90, 50);
        btnMarkRefunded.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnMarkRefunded.ForeColor = Color.White;
        btnMarkRefunded.Location = new Point(632, 691);
        btnMarkRefunded.Name = "btnMarkRefunded";
        btnMarkRefunded.ShadowDecoration.CustomizableEdges = customizableEdges14;
        btnMarkRefunded.Size = new Size(280, 42);
        btnMarkRefunded.TabIndex = 8;
        btnMarkRefunded.Text = "Ghi nhận đã hoàn giao dịch";
        // 
        // txtMatchBookingCode
        // 
        txtMatchBookingCode.BorderColor = Color.FromArgb(220, 227, 231);
        txtMatchBookingCode.BorderRadius = 12;
        txtMatchBookingCode.DefaultText = "";
        txtMatchBookingCode.Font = new Font("Segoe UI", 9F);
        txtMatchBookingCode.Location = new Point(26, 631);
        txtMatchBookingCode.Margin = new Padding(4, 5, 4, 5);
        txtMatchBookingCode.Name = "txtMatchBookingCode";
        txtMatchBookingCode.PlaceholderText = "Mã đơn cần khớp, ví dụ FJ261020-001";
        txtMatchBookingCode.SelectedText = "";
        txtMatchBookingCode.Size = new Size(291, 42);
        txtMatchBookingCode.TabIndex = 9;
        // 
        // btnMatchTransaction
        // 
        btnMatchTransaction.BorderRadius = 21;
        btnMatchTransaction.Enabled = false;
        btnMatchTransaction.FillColor = Color.FromArgb(30, 58, 47);
        btnMatchTransaction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnMatchTransaction.ForeColor = Color.White;
        btnMatchTransaction.Location = new Point(325, 631);
        btnMatchTransaction.Name = "btnMatchTransaction";
        btnMatchTransaction.Size = new Size(300, 42);
        btnMatchTransaction.TabIndex = 10;
        btnMatchTransaction.Text = "Khớp tay với mã đơn";
        // 
        // btnIgnoreTransaction
        // 
        btnIgnoreTransaction.BorderRadius = 21;
        btnIgnoreTransaction.Enabled = false;
        btnIgnoreTransaction.FillColor = Color.FromArgb(104, 120, 124);
        btnIgnoreTransaction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnIgnoreTransaction.ForeColor = Color.White;
        btnIgnoreTransaction.Location = new Point(632, 631);
        btnIgnoreTransaction.Name = "btnIgnoreTransaction";
        btnIgnoreTransaction.Size = new Size(280, 42);
        btnIgnoreTransaction.TabIndex = 11;
        btnIgnoreTransaction.Text = "Bỏ qua (ghi chú là lý do)";
        // 
        // tabDeposits
        // 
        tabDeposits.Controls.Add(dgvDeposits);
        tabDeposits.Controls.Add(lblDepositPage);
        tabDeposits.Controls.Add(btnDepositPrev);
        tabDeposits.Controls.Add(btnDepositNext);
        tabDeposits.Controls.Add(txtFinanceNote);
        tabDeposits.Controls.Add(txtRejectionReason);
        tabDeposits.Controls.Add(btnConfirmDeposit);
        tabDeposits.Controls.Add(btnRejectDeposit);
        tabDeposits.Location = new Point(224, 4);
        tabDeposits.Name = "tabDeposits";
        tabDeposits.Size = new Size(922, 755);
        tabDeposits.TabIndex = 1;
        tabDeposits.Text = "Cọc bổ sung";
        // 
        // dgvDeposits
        // 
        dgvDeposits.AllowUserToAddRows = false;
        dataGridViewCellStyle4.BackColor = Color.FromArgb(250, 247, 242);
        dgvDeposits.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
        dgvDeposits.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle5.BackColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle5.ForeColor = Color.White;
        dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
        dgvDeposits.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
        dgvDeposits.ColumnHeadersHeight = 38;
        dgvDeposits.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle6.BackColor = Color.White;
        dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle6.ForeColor = Color.FromArgb(30, 58, 47);
        dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(231, 229, 255);
        dataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(71, 69, 94);
        dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
        dgvDeposits.DefaultCellStyle = dataGridViewCellStyle6;
        dgvDeposits.GridColor = Color.FromArgb(234, 240, 238);
        dgvDeposits.Location = new Point(16, 16);
        dgvDeposits.MultiSelect = false;
        dgvDeposits.Name = "dgvDeposits";
        dgvDeposits.ReadOnly = true;
        dgvDeposits.RowHeadersVisible = false;
        dgvDeposits.RowHeadersWidth = 62;
        dgvDeposits.Size = new Size(828, 602);
        dgvDeposits.TabIndex = 0;
        dgvDeposits.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(250, 247, 242);
        dgvDeposits.ThemeStyle.GridColor = Color.FromArgb(234, 240, 238);
        dgvDeposits.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(30, 58, 47);
        dgvDeposits.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvDeposits.ThemeStyle.HeaderStyle.Height = 38;
        dgvDeposits.ThemeStyle.ReadOnly = true;
        dgvDeposits.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
        dgvDeposits.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 58, 47);
        dgvDeposits.ThemeStyle.RowsStyle.Height = 33;
        // 
        // lblDepositPage
        // 
        lblDepositPage.AutoSize = false;
        lblDepositPage.BackColor = Color.Transparent;
        lblDepositPage.ForeColor = Color.FromArgb(104, 120, 124);
        lblDepositPage.Location = new Point(16, 447);
        lblDepositPage.Name = "lblDepositPage";
        lblDepositPage.Size = new Size(3, 2);
        lblDepositPage.TabIndex = 1;
        lblDepositPage.Text = "Cọc bổ sung chờ xử lý";
        // 
        // btnDepositPrev
        // 
        btnDepositPrev.BorderRadius = 18;
        btnDepositPrev.CustomizableEdges = customizableEdges15;
        btnDepositPrev.FillColor = Color.FromArgb(250, 247, 242);
        btnDepositPrev.Font = new Font("Segoe UI", 9F);
        btnDepositPrev.ForeColor = Color.FromArgb(30, 58, 47);
        btnDepositPrev.Location = new Point(643, 648);
        btnDepositPrev.Name = "btnDepositPrev";
        btnDepositPrev.ShadowDecoration.CustomizableEdges = customizableEdges16;
        btnDepositPrev.Size = new Size(85, 38);
        btnDepositPrev.TabIndex = 2;
        btnDepositPrev.Text = "Trước";
        // 
        // btnDepositNext
        // 
        btnDepositNext.BorderRadius = 18;
        btnDepositNext.CustomizableEdges = customizableEdges17;
        btnDepositNext.FillColor = Color.FromArgb(250, 247, 242);
        btnDepositNext.Font = new Font("Segoe UI", 9F);
        btnDepositNext.ForeColor = Color.FromArgb(30, 58, 47);
        btnDepositNext.Location = new Point(734, 648);
        btnDepositNext.Name = "btnDepositNext";
        btnDepositNext.ShadowDecoration.CustomizableEdges = customizableEdges18;
        btnDepositNext.Size = new Size(85, 38);
        btnDepositNext.TabIndex = 3;
        btnDepositNext.Text = "Sau";
        // 
        // txtFinanceNote
        // 
        txtFinanceNote.BorderColor = Color.FromArgb(220, 227, 231);
        txtFinanceNote.BorderRadius = 12;
        txtFinanceNote.CustomizableEdges = customizableEdges19;
        txtFinanceNote.DefaultText = "";
        txtFinanceNote.Font = new Font("Segoe UI", 9F);
        txtFinanceNote.Location = new Point(4, 694);
        txtFinanceNote.Margin = new Padding(4, 5, 4, 5);
        txtFinanceNote.Name = "txtFinanceNote";
        txtFinanceNote.PlaceholderText = "Ghi chú tài chính (bắt buộc)";
        txtFinanceNote.SelectedText = "";
        txtFinanceNote.ShadowDecoration.CustomizableEdges = customizableEdges20;
        txtFinanceNote.Size = new Size(300, 42);
        txtFinanceNote.TabIndex = 4;
        // 
        // txtRejectionReason
        // 
        txtRejectionReason.BorderColor = Color.FromArgb(220, 227, 231);
        txtRejectionReason.BorderRadius = 12;
        txtRejectionReason.CustomizableEdges = customizableEdges21;
        txtRejectionReason.DefaultText = "";
        txtRejectionReason.Font = new Font("Segoe UI", 9F);
        txtRejectionReason.Location = new Point(312, 694);
        txtRejectionReason.Margin = new Padding(4, 5, 4, 5);
        txtRejectionReason.Name = "txtRejectionReason";
        txtRejectionReason.PlaceholderText = "Lý do từ chối (bắt buộc)";
        txtRejectionReason.SelectedText = "";
        txtRejectionReason.ShadowDecoration.CustomizableEdges = customizableEdges22;
        txtRejectionReason.Size = new Size(280, 42);
        txtRejectionReason.TabIndex = 5;
        // 
        // btnConfirmDeposit
        // 
        btnConfirmDeposit.BorderRadius = 21;
        btnConfirmDeposit.CustomizableEdges = customizableEdges23;
        btnConfirmDeposit.Enabled = false;
        btnConfirmDeposit.FillColor = Color.FromArgb(30, 58, 47);
        btnConfirmDeposit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnConfirmDeposit.ForeColor = Color.White;
        btnConfirmDeposit.Location = new Point(4, 644);
        btnConfirmDeposit.Name = "btnConfirmDeposit";
        btnConfirmDeposit.ShadowDecoration.CustomizableEdges = customizableEdges24;
        btnConfirmDeposit.Size = new Size(350, 42);
        btnConfirmDeposit.TabIndex = 6;
        btnConfirmDeposit.Text = "Xác nhận tiền đã vào · Áp dụng đổi";
        // 
        // btnRejectDeposit
        // 
        btnRejectDeposit.BorderRadius = 21;
        btnRejectDeposit.CustomizableEdges = customizableEdges25;
        btnRejectDeposit.Enabled = false;
        btnRejectDeposit.FillColor = Color.FromArgb(200, 90, 50);
        btnRejectDeposit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnRejectDeposit.ForeColor = Color.White;
        btnRejectDeposit.Location = new Point(599, 694);
        btnRejectDeposit.Name = "btnRejectDeposit";
        btnRejectDeposit.ShadowDecoration.CustomizableEdges = customizableEdges26;
        btnRejectDeposit.Size = new Size(220, 42);
        btnRejectDeposit.TabIndex = 7;
        btnRejectDeposit.Text = "Từ chối tài chính";
        // 
        // FrmBankAndAdditionalDeposit
        // 
        BackColor = Color.FromArgb(250, 247, 242);
        ClientSize = new Size(1150, 763);
        Controls.Add(tabs);
        Font = new Font("Segoe UI", 9F);
        Name = "FrmBankAndAdditionalDeposit";
        Text = "Kiểm kê Giao dịch Ngân hàng & Cọc bổ sung";
        tabs.ResumeLayout(false);
        tabTransactions.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvTransactions).EndInit();
        tabDeposits.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvDeposits).EndInit();
        ResumeLayout(false);
    }
}
