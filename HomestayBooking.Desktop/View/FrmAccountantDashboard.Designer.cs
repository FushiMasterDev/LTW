using System.Drawing;
using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmAccountantDashboard
{
    private Guna2Panel pnlSidebar = null!, pnlHeader = null!, pnlContent = null!;
    private Label lblBrand = null!, lblEmployee = null!, lblHeader = null!;
    private Guna2Button btnNavReceipts = null!, btnNavTransactions = null!, btnNavRefunds = null!, btnNavSettlements = null!, btnNavCatalog = null!, btnLogout = null!, btnRefresh = null!;
    private void InitializeComponent()
    {
        pnlSidebar = new Guna2Panel(); pnlHeader = new Guna2Panel(); pnlContent = new Guna2Panel(); lblBrand = new Label(); lblEmployee = new Label(); lblHeader = new Label();
        btnNavReceipts = new Guna2Button(); btnNavTransactions = new Guna2Button(); btnNavRefunds = new Guna2Button(); btnNavSettlements = new Guna2Button(); btnNavCatalog = new Guna2Button(); btnLogout = new Guna2Button(); btnRefresh = new Guna2Button();
        SuspendLayout(); pnlSidebar.Dock = DockStyle.Left; pnlSidebar.Width = 240; pnlSidebar.FillColor = Color.FromArgb(30,58,47);
        lblBrand.Text = "Fuji Homestay\nPhân hệ Kế toán"; lblBrand.ForeColor = Color.White; lblBrand.Location = new Point(20,24); lblBrand.Size = new Size(200,60); lblBrand.Font = new Font("Segoe UI",14,FontStyle.Bold);
        lblEmployee.Text = "KT001"; lblEmployee.ForeColor = Color.White; lblEmployee.Location = new Point(20,100); lblEmployee.Size = new Size(200,44);
        btnNavReceipts.Text = "Duyệt Bill Cọc"; btnNavReceipts.Location = new Point(12,166); btnNavReceipts.Size = new Size(216,44);
        btnNavTransactions.Text = "Kiểm kê & Cọc bổ sung"; btnNavTransactions.Location = new Point(12,216); btnNavTransactions.Size = new Size(216,44);
        btnNavRefunds.Text = "Hoàn tiền & Khoản bù"; btnNavRefunds.Location = new Point(12,266); btnNavRefunds.Size = new Size(216,44);
        btnNavSettlements.Text = "Đối soát Chủ Homestay"; btnNavSettlements.Location = new Point(12,316); btnNavSettlements.Size = new Size(216,44);
        btnNavCatalog.Text = "Ngày lễ & Danh mục"; btnNavCatalog.Location = new Point(12,366); btnNavCatalog.Size = new Size(216,44);
        btnNavReceipts.FillColor=Color.FromArgb(200,90,50); btnNavReceipts.ForeColor=Color.White; btnNavReceipts.BorderRadius=18; btnNavReceipts.Font=new Font("Segoe UI",9,FontStyle.Bold); btnNavReceipts.HoverState.FillColor=Color.FromArgb(200,90,50);
        btnNavTransactions.FillColor=Color.FromArgb(30,58,47); btnNavTransactions.ForeColor=Color.White; btnNavTransactions.BorderRadius=18; btnNavTransactions.Font=new Font("Segoe UI",9,FontStyle.Bold); btnNavTransactions.HoverState.FillColor=Color.FromArgb(43,76,61);
        btnNavRefunds.FillColor=Color.FromArgb(30,58,47); btnNavRefunds.ForeColor=Color.White; btnNavRefunds.BorderRadius=18; btnNavRefunds.Font=new Font("Segoe UI",9,FontStyle.Bold); btnNavRefunds.HoverState.FillColor=Color.FromArgb(43,76,61);
        btnNavSettlements.FillColor=Color.FromArgb(30,58,47); btnNavSettlements.ForeColor=Color.White; btnNavSettlements.BorderRadius=18; btnNavSettlements.Font=new Font("Segoe UI",9,FontStyle.Bold); btnNavSettlements.HoverState.FillColor=Color.FromArgb(43,76,61);
        btnNavCatalog.FillColor=Color.FromArgb(30,58,47); btnNavCatalog.ForeColor=Color.White; btnNavCatalog.BorderRadius=18; btnNavCatalog.Font=new Font("Segoe UI",9,FontStyle.Bold); btnNavCatalog.HoverState.FillColor=Color.FromArgb(43,76,61);
        btnLogout.Text = "Đăng xuất"; btnLogout.Location = new Point(12,650); btnLogout.Size = new Size(216,44); btnLogout.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        btnLogout.FillColor=Color.FromArgb(200,90,50); btnLogout.ForeColor=Color.White; btnLogout.BorderRadius=18; btnLogout.Font=new Font("Segoe UI",9,FontStyle.Bold);
        pnlSidebar.Controls.Add(lblBrand); pnlSidebar.Controls.Add(lblEmployee); pnlSidebar.Controls.Add(btnNavReceipts); pnlSidebar.Controls.Add(btnNavTransactions); pnlSidebar.Controls.Add(btnNavRefunds); pnlSidebar.Controls.Add(btnNavSettlements); pnlSidebar.Controls.Add(btnNavCatalog); pnlSidebar.Controls.Add(btnLogout);

        pnlHeader.Dock = DockStyle.Top; pnlHeader.Height = 60; pnlHeader.FillColor = Color.White; lblHeader.Text = "Duyệt Bill Cọc"; lblHeader.Location = new Point(18,18); lblHeader.Size = new Size(500,28); lblHeader.Font = new Font("Segoe UI",12,FontStyle.Bold);
        btnRefresh.Text = "Làm mới"; btnRefresh.Location = new Point(850,10); btnRefresh.Size = new Size(100,40); btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnRefresh.FillColor = Color.FromArgb(200,90,50); btnRefresh.ForeColor = Color.White; btnRefresh.BorderRadius = 20;
        pnlHeader.Controls.Add(lblHeader); pnlHeader.Controls.Add(btnRefresh); pnlContent.Dock = DockStyle.Fill; pnlContent.FillColor = Color.FromArgb(250,247,242);
        Text = "Fuji Homestay | Kế toán"; BackColor = Color.FromArgb(250,247,242); ClientSize = new Size(1200,760); MinimumSize = new Size(1000,650); Controls.Add(pnlContent); Controls.Add(pnlHeader); Controls.Add(pnlSidebar); StartPosition = FormStartPosition.CenterScreen; ResumeLayout(false);
    }
}
