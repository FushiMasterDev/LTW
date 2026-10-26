using System.Drawing;
using Guna.UI2.WinForms;

namespace HomestayBooking.Desktop.View;

partial class FrmReceiptReview
{
    private Guna2Panel pnlLeft = null!, pnlRight = null!, pnlFilter = null!, pnlAction = null!, pnlDetailCard = null!, pnlViewer = null!, pnlImageTools = null!;
    private Guna2ComboBox cboStatus = null!; private Guna2Button btnSearch = null!,btnPrevPage = null!,btnNextPage = null!,btnApprove = null!,btnReject = null!,btnZoomIn = null!,btnZoomOut = null!,btnRotate = null!,btnResetImage = null!;
    private Guna2DataGridView dgvReceipts = null!; private Guna2PictureBox picReceipt = null!; private Guna2TextBox txtRejectReason = null!;
    private Guna2HtmlLabel lblPageInfo = null!,lblBookingCodeValue = null!,lblAmountDueValue = null!,lblTransferContentValue = null!,lblRejectionCountValue = null!,lblImageMessage = null!;
    private void InitializeComponent()
    {
        pnlLeft=new Guna2Panel(); pnlRight=new Guna2Panel(); pnlFilter=new Guna2Panel(); pnlAction=new Guna2Panel(); pnlDetailCard=new Guna2Panel(); pnlViewer=new Guna2Panel(); pnlImageTools=new Guna2Panel();
        cboStatus=new Guna2ComboBox(); btnSearch=new Guna2Button(); btnPrevPage=new Guna2Button(); btnNextPage=new Guna2Button(); btnApprove=new Guna2Button(); btnReject=new Guna2Button(); btnZoomIn=new Guna2Button(); btnZoomOut=new Guna2Button(); btnRotate=new Guna2Button(); btnResetImage=new Guna2Button();
        dgvReceipts=new Guna2DataGridView(); picReceipt=new Guna2PictureBox(); txtRejectReason=new Guna2TextBox(); lblPageInfo=new Guna2HtmlLabel(); lblBookingCodeValue=new Guna2HtmlLabel(); lblAmountDueValue=new Guna2HtmlLabel(); lblTransferContentValue=new Guna2HtmlLabel(); lblRejectionCountValue=new Guna2HtmlLabel(); lblImageMessage=new Guna2HtmlLabel();
        SuspendLayout();
        pnlLeft.Dock=DockStyle.Left; pnlLeft.Width=600; pnlLeft.FillColor=Color.FromArgb(250,247,242); pnlLeft.Padding=new Padding(12);
        pnlRight.Dock=DockStyle.Fill; pnlRight.FillColor=Color.FromArgb(250,247,242); pnlRight.Padding=new Padding(12);
        pnlFilter.Location=new Point(12,12); pnlFilter.Size=new Size(576,52); pnlFilter.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right; pnlFilter.FillColor=Color.White; pnlFilter.BorderRadius=14;
        cboStatus.Location=new Point(10,9); cboStatus.Size=new Size(145,34); cboStatus.BorderRadius=10; cboStatus.BorderColor=Color.FromArgb(220,227,231); cboStatus.Font=new Font("Segoe UI",9F); cboStatus.ForeColor=Color.FromArgb(30,58,47); cboStatus.ItemHeight=26;
        btnSearch.Text="Tải danh sách"; btnSearch.Location=new Point(164,8); btnSearch.Size=new Size(122,36); btnSearch.FillColor=Color.FromArgb(200,90,50); btnSearch.ForeColor=Color.White; btnSearch.BorderRadius=18; btnSearch.Font=new Font("Segoe UI",9F,FontStyle.Bold);
        btnPrevPage.Text="‹"; btnPrevPage.Location=new Point(374,8); btnPrevPage.Size=new Size(38,36); btnPrevPage.FillColor=Color.FromArgb(250,247,242); btnPrevPage.ForeColor=Color.FromArgb(30,58,47); btnPrevPage.BorderRadius=18;
        btnNextPage.Text="›"; btnNextPage.Location=new Point(530,8); btnNextPage.Size=new Size(38,36); btnNextPage.FillColor=Color.FromArgb(250,247,242); btnNextPage.ForeColor=Color.FromArgb(30,58,47); btnNextPage.BorderRadius=18; btnNextPage.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        lblPageInfo.Text="Trang 1/1"; lblPageInfo.Location=new Point(416,17); lblPageInfo.Size=new Size(100,22); lblPageInfo.ForeColor=Color.FromArgb(104,120,124); lblPageInfo.BackColor=Color.Transparent; lblPageInfo.AutoSize=false;
        pnlFilter.Controls.Add(cboStatus); pnlFilter.Controls.Add(btnSearch); pnlFilter.Controls.Add(btnPrevPage); pnlFilter.Controls.Add(btnNextPage); pnlFilter.Controls.Add(lblPageInfo);
        dgvReceipts.Location=new Point(12,74); dgvReceipts.Size=new Size(576,360); dgvReceipts.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right; dgvReceipts.AutoGenerateColumns=true; dgvReceipts.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; dgvReceipts.ReadOnly=true; dgvReceipts.AllowUserToAddRows=false; dgvReceipts.SelectionMode=DataGridViewSelectionMode.FullRowSelect; dgvReceipts.MultiSelect=false; dgvReceipts.BackgroundColor=Color.White; dgvReceipts.BorderStyle=BorderStyle.None; dgvReceipts.ColumnHeadersHeight=38; dgvReceipts.ThemeStyle.HeaderStyle.BackColor=Color.FromArgb(30,58,47); dgvReceipts.ThemeStyle.HeaderStyle.ForeColor=Color.White; dgvReceipts.ThemeStyle.HeaderStyle.Font=new Font("Segoe UI",9F,FontStyle.Bold); dgvReceipts.ThemeStyle.RowsStyle.BackColor=Color.White; dgvReceipts.ThemeStyle.RowsStyle.ForeColor=Color.FromArgb(30,58,47); dgvReceipts.ThemeStyle.RowsStyle.Font=new Font("Segoe UI",9F); dgvReceipts.ThemeStyle.AlternatingRowsStyle.BackColor=Color.FromArgb(250,247,242); dgvReceipts.ThemeStyle.GridColor=Color.FromArgb(234,240,238);
        pnlDetailCard.Location=new Point(12,446); pnlDetailCard.Size=new Size(576,220); pnlDetailCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom; pnlDetailCard.FillColor=Color.White; pnlDetailCard.BorderRadius=15; pnlDetailCard.Padding=new Padding(14);
        lblBookingCodeValue.Text="Mã đơn: —"; lblBookingCodeValue.Location=new Point(16,16); lblBookingCodeValue.Size=new Size(530,30); lblBookingCodeValue.Font=new Font("Segoe UI",12F,FontStyle.Bold); lblBookingCodeValue.ForeColor=Color.FromArgb(30,58,47); lblBookingCodeValue.BackColor=Color.Transparent; lblBookingCodeValue.AutoSize=false;
        lblAmountDueValue.Text="Số tiền phải cọc: —"; lblAmountDueValue.Location=new Point(16,50); lblAmountDueValue.Size=new Size(530,32); lblAmountDueValue.Font=new Font("Segoe UI",14F,FontStyle.Bold); lblAmountDueValue.ForeColor=Color.FromArgb(200,90,50); lblAmountDueValue.BackColor=Color.Transparent; lblAmountDueValue.AutoSize=false;
        lblTransferContentValue.Text="Nội dung chuyển khoản: —"; lblTransferContentValue.Location=new Point(16,90); lblTransferContentValue.Size=new Size(530,48); lblTransferContentValue.Font=new Font("Segoe UI",9F); lblTransferContentValue.ForeColor=Color.FromArgb(30,58,47); lblTransferContentValue.BackColor=Color.Transparent; lblTransferContentValue.AutoSize=false;
        lblRejectionCountValue.Text="Số lần từ chối: —"; lblRejectionCountValue.Location=new Point(16,148); lblRejectionCountValue.Size=new Size(530,28); lblRejectionCountValue.Font=new Font("Segoe UI",9F,FontStyle.Bold); lblRejectionCountValue.ForeColor=Color.FromArgb(104,120,124); lblRejectionCountValue.BackColor=Color.Transparent; lblRejectionCountValue.AutoSize=false;
        pnlDetailCard.Controls.Add(lblBookingCodeValue); pnlDetailCard.Controls.Add(lblAmountDueValue); pnlDetailCard.Controls.Add(lblTransferContentValue); pnlDetailCard.Controls.Add(lblRejectionCountValue);
        pnlLeft.Controls.Add(pnlDetailCard); pnlLeft.Controls.Add(dgvReceipts); pnlLeft.Controls.Add(pnlFilter);
        pnlViewer.Dock=DockStyle.Fill; pnlViewer.FillColor=Color.White; pnlViewer.BorderRadius=15; pnlViewer.Padding=new Padding(10); pnlViewer.AutoScroll=true;
        pnlImageTools.Dock=DockStyle.Top; pnlImageTools.Height=48; pnlImageTools.FillColor=Color.White;
        btnZoomIn.Text="+"; btnZoomOut.Text="−"; btnRotate.Text="Xoay 90°"; btnResetImage.Text="Vừa khung";
        btnZoomIn.Location=new Point(10,8); btnZoomIn.Size=new Size(42,34); btnZoomOut.Location=new Point(56,8); btnZoomOut.Size=new Size(42,34); btnRotate.Location=new Point(102,8); btnRotate.Size=new Size(82,34); btnResetImage.Location=new Point(190,8); btnResetImage.Size=new Size(104,34);
        btnZoomIn.FillColor=Color.FromArgb(250,247,242); btnZoomIn.ForeColor=Color.FromArgb(30,58,47); btnZoomIn.BorderRadius=17; btnZoomOut.FillColor=Color.FromArgb(250,247,242); btnZoomOut.ForeColor=Color.FromArgb(30,58,47); btnZoomOut.BorderRadius=17; btnRotate.FillColor=Color.FromArgb(250,247,242); btnRotate.ForeColor=Color.FromArgb(30,58,47); btnRotate.BorderRadius=17; btnResetImage.FillColor=Color.FromArgb(200,90,50); btnResetImage.ForeColor=Color.White; btnResetImage.BorderRadius=17;
        picReceipt.Dock=DockStyle.Fill; picReceipt.SizeMode=PictureBoxSizeMode.Zoom; picReceipt.BackColor=Color.FromArgb(250,247,242);
        lblImageMessage.Dock=DockStyle.Bottom; lblImageMessage.Height=38; lblImageMessage.Text="Chọn một bill để xem ảnh"; lblImageMessage.ForeColor=Color.FromArgb(104,120,124); lblImageMessage.BackColor=Color.Transparent; lblImageMessage.AutoSize=false;
        pnlImageTools.Controls.Add(btnZoomIn); pnlImageTools.Controls.Add(btnZoomOut); pnlImageTools.Controls.Add(btnRotate); pnlImageTools.Controls.Add(btnResetImage);
        pnlViewer.Controls.Add(picReceipt); pnlViewer.Controls.Add(lblImageMessage); pnlViewer.Controls.Add(pnlImageTools);
        pnlAction.Dock=DockStyle.Bottom; pnlAction.Height=142; pnlAction.FillColor=Color.White; pnlAction.BorderRadius=15; pnlAction.Padding=new Padding(10);
        txtRejectReason.PlaceholderText="Nhập lý do nếu từ chối (VD: Số tiền không khớp, ảnh mờ...)"; txtRejectReason.Location=new Point(10,8); txtRejectReason.Size=new Size(530,48); txtRejectReason.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right; txtRejectReason.Multiline=true; txtRejectReason.BorderRadius=12; txtRejectReason.BorderColor=Color.FromArgb(220,227,231); txtRejectReason.Font=new Font("Segoe UI",9F);
        btnApprove.Text="Duyệt cọc"; btnApprove.Location=new Point(10,68); btnApprove.Size=new Size(220,44); btnApprove.FillColor=Color.FromArgb(30,58,47); btnApprove.ForeColor=Color.White; btnApprove.BorderRadius=22; btnApprove.Font=new Font("Segoe UI",9F,FontStyle.Bold); btnApprove.Enabled=false;
        btnReject.Text="Từ chối bill"; btnReject.Location=new Point(240,68); btnReject.Size=new Size(220,44); btnReject.FillColor=Color.FromArgb(200,90,50); btnReject.ForeColor=Color.White; btnReject.BorderRadius=22; btnReject.Font=new Font("Segoe UI",9F,FontStyle.Bold); btnReject.Enabled=false;
        pnlAction.Controls.Add(txtRejectReason); pnlAction.Controls.Add(btnApprove); pnlAction.Controls.Add(btnReject);
        pnlRight.Controls.Add(pnlViewer); pnlRight.Controls.Add(pnlAction);
        Controls.Add(pnlRight); Controls.Add(pnlLeft); BackColor=Color.FromArgb(250,247,242); Font=new Font("Segoe UI",9F); ClientSize=new Size(1180,700); MinimumSize=new Size(900,600); Text="Duyệt Bill Cọc"; ResumeLayout(false);
    }
}
