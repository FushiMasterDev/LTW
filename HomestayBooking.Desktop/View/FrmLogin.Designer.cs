#nullable enable
using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmLogin
{
    private System.ComponentModel.IContainer? components;
    private Guna2Panel card = null!;
    private Guna2TextBox txtEmployeeCode = null!;
    private Guna2TextBox txtPassword = null!;
    private Guna2Button btnLogin = null!;
    private Guna2Button btnForgot = null!;
    private Guna2Button btnShowPassword = null!;
    private Guna2CheckBox chkRemember = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLogin));
        card = new Guna2Panel();
        txtEmployeeCode = new Guna2TextBox();
        txtPassword = new Guna2TextBox();
        btnShowPassword = new Guna2Button();
        chkRemember = new Guna2CheckBox();
        btnForgot = new Guna2Button();
        btnLogin = new Guna2Button();
        guna2HtmlLabel1 = new Guna2HtmlLabel();
        guna2HtmlLabel2 = new Guna2HtmlLabel();
        card.SuspendLayout();
        SuspendLayout();
        // 
        // card
        // 
        card.BackColor = Color.Transparent;
        card.BorderRadius = 18;
        card.Controls.Add(guna2HtmlLabel2);
        card.Controls.Add(guna2HtmlLabel1);
        card.Controls.Add(txtEmployeeCode);
        card.Controls.Add(txtPassword);
        card.Controls.Add(btnShowPassword);
        card.Controls.Add(chkRemember);
        card.Controls.Add(btnForgot);
        card.Controls.Add(btnLogin);
        card.CustomizableEdges = customizableEdges11;
        card.FillColor = Color.White;
        card.Font = new Font("Times New Roman", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        card.ForeColor = Color.Black;
        card.Location = new Point(370, 108);
        card.Name = "card";
        card.ShadowDecoration.BorderRadius = 18;
        card.ShadowDecoration.Color = Color.FromArgb(28, 24, 46, 63);
        card.ShadowDecoration.CustomizableEdges = customizableEdges12;
        card.ShadowDecoration.Depth = 12;
        card.ShadowDecoration.Enabled = true;
        card.Size = new Size(457, 500);
        card.TabIndex = 0;
        // 
        // txtEmployeeCode
        // 
        txtEmployeeCode.BorderColor = Color.FromArgb(220, 227, 231);
        txtEmployeeCode.BorderRadius = 10;
        txtEmployeeCode.CharacterCasing = CharacterCasing.Upper;
        txtEmployeeCode.Cursor = Cursors.IBeam;
        txtEmployeeCode.CustomizableEdges = customizableEdges1;
        txtEmployeeCode.DefaultText = "";
        txtEmployeeCode.Font = new Font("Segoe UI", 10F);
        txtEmployeeCode.Location = new Point(36, 143);
        txtEmployeeCode.Margin = new Padding(4, 5, 4, 5);
        txtEmployeeCode.Name = "txtEmployeeCode";
        txtEmployeeCode.PlaceholderText = "Ví dụ: NV001";
        txtEmployeeCode.SelectedText = "";
        txtEmployeeCode.ShadowDecoration.CustomizableEdges = customizableEdges2;
        txtEmployeeCode.Size = new Size(368, 48);
        txtEmployeeCode.TabIndex = 0;
        // 
        // txtPassword
        // 
        txtPassword.BorderColor = Color.FromArgb(220, 227, 231);
        txtPassword.BorderRadius = 10;
        txtPassword.Cursor = Cursors.IBeam;
        txtPassword.CustomizableEdges = customizableEdges3;
        txtPassword.DefaultText = "";
        txtPassword.Font = new Font("Segoe UI", 10F);
        txtPassword.Location = new Point(36, 220);
        txtPassword.Margin = new Padding(4, 5, 4, 5);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '●';
        txtPassword.PlaceholderText = "Nhập mật khẩu";
        txtPassword.SelectedText = "";
        txtPassword.ShadowDecoration.CustomizableEdges = customizableEdges4;
        txtPassword.Size = new Size(282, 48);
        txtPassword.TabIndex = 1;
        txtPassword.UseSystemPasswordChar = true;
        // 
        // btnShowPassword
        // 
        btnShowPassword.BorderRadius = 9;
        btnShowPassword.CustomizableEdges = customizableEdges5;
        btnShowPassword.FillColor = Color.FromArgb(239, 246, 244);
        btnShowPassword.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnShowPassword.ForeColor = Color.FromArgb(19, 132, 111);
        btnShowPassword.Location = new Point(326, 220);
        btnShowPassword.Name = "btnShowPassword";
        btnShowPassword.ShadowDecoration.CustomizableEdges = customizableEdges6;
        btnShowPassword.Size = new Size(78, 48);
        btnShowPassword.TabIndex = 2;
        btnShowPassword.Text = "Hiện";
        // 
        // chkRemember
        // 
        chkRemember.AutoSize = true;
        chkRemember.CheckedState.BorderRadius = 3;
        chkRemember.CheckedState.BorderThickness = 0;
        chkRemember.CheckedState.FillColor = Color.FromArgb(19, 132, 111);
        chkRemember.Font = new Font("Segoe UI", 9F);
        chkRemember.ForeColor = Color.FromArgb(76, 91, 103);
        chkRemember.Location = new Point(36, 304);
        chkRemember.Name = "chkRemember";
        chkRemember.Size = new Size(211, 29);
        chkRemember.TabIndex = 3;
        chkRemember.Text = "Ghi nhớ mã nhân viên";
        chkRemember.UncheckedState.BorderRadius = 3;
        chkRemember.UncheckedState.BorderThickness = 0;
        // 
        // btnForgot
        // 
        btnForgot.CustomizableEdges = customizableEdges7;
        btnForgot.FillColor = Color.Transparent;
        btnForgot.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnForgot.ForeColor = Color.FromArgb(19, 132, 111);
        btnForgot.Location = new Point(265, 304);
        btnForgot.Name = "btnForgot";
        btnForgot.ShadowDecoration.CustomizableEdges = customizableEdges8;
        btnForgot.Size = new Size(139, 36);
        btnForgot.TabIndex = 4;
        btnForgot.Text = "Quên mật khẩu?";
        // 
        // btnLogin
        // 
        btnLogin.BorderRadius = 11;
        btnLogin.CustomizableEdges = customizableEdges9;
        btnLogin.FillColor = Color.FromArgb(19, 132, 111);
        btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(36, 366);
        btnLogin.Name = "btnLogin";
        btnLogin.ShadowDecoration.CustomizableEdges = customizableEdges10;
        btnLogin.Size = new Size(368, 54);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "ĐĂNG NHẬP";
        // 
        // guna2HtmlLabel1
        // 
        guna2HtmlLabel1.BackColor = Color.Transparent;
        guna2HtmlLabel1.Font = new Font("Times New Roman", 22F, FontStyle.Regular, GraphicsUnit.Point, 0);
        guna2HtmlLabel1.ForeColor = Color.FromArgb(30, 58, 47);
        guna2HtmlLabel1.Location = new Point(20, 57);
        guna2HtmlLabel1.Name = "guna2HtmlLabel1";
        guna2HtmlLabel1.Size = new Size(257, 53);
        guna2HtmlLabel1.TabIndex = 6;
        guna2HtmlLabel1.Text = "Fuji Homestay";
        guna2HtmlLabel1.Click += guna2HtmlLabel1_Click;
        // 
        // guna2HtmlLabel2
        // 
        guna2HtmlLabel2.BackColor = Color.Transparent;
        guna2HtmlLabel2.ForeColor = Color.DarkGray;
        guna2HtmlLabel2.Location = new Point(20, 451);
        guna2HtmlLabel2.Name = "guna2HtmlLabel2";
        guna2HtmlLabel2.Size = new Size(311, 27);
        guna2HtmlLabel2.TabIndex = 7;
        guna2HtmlLabel2.Text = "Hệ thống Quản trị & Điều hành Nội bộ";
        // 
        // FrmLogin
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        BackColor = Color.White;
        BackgroundImage = (Image?)resources.GetObject("$this.BackgroundImage")!;
        ClientSize = new Size(1196, 759);
        Controls.Add(card);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(1196, 759);
        Name = "FrmLogin";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Đăng nhập | Fuji Homestay";
        Load += FrmLogin_Load;
        card.ResumeLayout(false);
        card.PerformLayout();
        ResumeLayout(false);
    }

    private Guna2HtmlLabel guna2HtmlLabel1 = null!;
    private Guna2HtmlLabel guna2HtmlLabel2 = null!;
}


