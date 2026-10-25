using System.Drawing;
using System.IO;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Models.Accountant;

namespace HomestayBooking.Desktop.View;

internal partial class FrmReceiptReview : Form
{
    private readonly AccountantController? _api;
    private List<ReceiptDto> _rows = [];
    private int _page = 1;
    private int _totalPages = 1;
    private float _zoom = 1f;
    private ReceiptDto? Selected => dgvReceipts.CurrentRow?.DataBoundItem as ReceiptDto;
    public FrmReceiptReview() { InitializeComponent(); Text = "Duyệt Bill Cọc"; }
    public FrmReceiptReview(EmployeeSession session) : this()
    {
        _api = new AccountantController(session);
        cboStatus.Items.AddRange(["Submitted", "Approved", "Rejected"]); cboStatus.SelectedIndex = 0;
        btnSearch.Click += async (_, _) => { _page = 1; await LoadRowsAsync(); };
        btnPrevPage.Click += async (_, _) => { if (_page > 1) { _page--; await LoadRowsAsync(); } };
        btnNextPage.Click += async (_, _) => { if (_page < _totalPages) { _page++; await LoadRowsAsync(); } };
        dgvReceipts.SelectionChanged += async (_, _) => await SelectReceiptAsync();
        btnApprove.Click += async (_, _) => await ApproveAsync(); btnReject.Click += async (_, _) => await RejectAsync();
        btnZoomIn.Click += (_, _) => { _zoom = Math.Min(3f, _zoom + 0.15f); ApplyZoom(); }; btnZoomOut.Click += (_, _) => { _zoom = Math.Max(0.25f, _zoom - 0.15f); ApplyZoom(); };
        btnRotate.Click += (_, _) => { if (picReceipt.Image is not null) picReceipt.Image.RotateFlip(RotateFlipType.Rotate90FlipNone); picReceipt.Invalidate(); };
        btnResetImage.Click += (_, _) => { _zoom = 1f; ApplyZoom(); };
        Shown += async (_, _) => await LoadRowsAsync();
    }
    private async Task LoadRowsAsync()
    {
        try
        {
            var result = await _api!.GetReceiptsAsync(cboStatus.Text, _page);
            _rows = result?.Items ?? []; _page = result?.Page ?? 1; _totalPages = Math.Max(1, result?.TotalPages ?? 1);
            dgvReceipts.DataSource = null; dgvReceipts.DataSource = _rows;
            lblPageInfo.Text = $"Trang {_page}/{_totalPages}"; UpdateSelection();
        }
        catch (Exception ex) { Models.DesktopApi.ShowError(this, ex); }
    }
    private async Task SelectReceiptAsync()
    {
        UpdateSelection();
        var receipt = Selected; if (receipt is null || _api is null) return;
        try
        {
            var (bytes, contentType) = await _api.GetReceiptFileAsync(receipt.PaymentId);
            if (contentType == "application/pdf") { lblImageMessage.Text = "Bill là PDF. Đang lưu tệp tạm..."; var path = Path.Combine(Path.GetTempPath(), $"receipt-{receipt.PaymentId}.pdf"); await File.WriteAllBytesAsync(path, bytes); lblImageMessage.Text = $"PDF đã lưu: {path}"; return; }
            using var stream = new MemoryStream(bytes); using var source = Image.FromStream(stream); picReceipt.Image?.Dispose(); picReceipt.Image = new Bitmap(source); _zoom = 1f; ApplyZoom(); lblImageMessage.Text = "";
        }
        catch (Exception ex) { lblImageMessage.Text = ex.Message; }
    }
    private void ApplyZoom()
    {
        if (picReceipt.Image is null) return;
        picReceipt.SizeMode = _zoom <= 1f ? PictureBoxSizeMode.Zoom : PictureBoxSizeMode.StretchImage;
        if (_zoom > 1f) { picReceipt.Width = (int)(picReceipt.Parent!.ClientSize.Width * _zoom); picReceipt.Height = (int)(picReceipt.Parent.ClientSize.Height * _zoom); }
        else { picReceipt.Dock = DockStyle.Fill; }
    }
    private void UpdateSelection()
    {
        var r = Selected; lblBookingCodeValue.Text = r?.BookingCode ?? "—"; lblAmountDueValue.Text = r is null ? "—" : $"{r.AmountDue:N0} đ";
        lblTransferContentValue.Text = r?.TransferContent ?? "—"; lblRejectionCountValue.Text = r is null ? "—" : r.PreviousRejections.ToString();
        var enabled = r?.Status == "Submitted"; btnApprove.Enabled = enabled; btnReject.Enabled = enabled;
    }
    private async Task ApproveAsync()
    {
        var r = Selected; if (r is null || MessageBox.Show(this, "Xác nhận bill và chuyển đơn sang Đã đặt cọc?", "Duyệt bill", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { await _api!.ApproveReceiptAsync(r.PaymentId); MessageBox.Show(this, "Đã duyệt bill, đơn chuyển sang Đã đặt cọc (Deposited).", "Fuji Homestay"); await LoadRowsAsync(); }
        catch (Exception ex) { Models.DesktopApi.ShowError(this, ex); }
    }
    private async Task RejectAsync()
    {
        var r = Selected; var reason = txtRejectReason.Text.Trim(); if (r is null) return;
        if (reason.Length == 0) { MessageBox.Show(this, "Vui lòng nhập lý do từ chối."); txtRejectReason.Focus(); return; }
        try { await _api!.RejectReceiptAsync(r.PaymentId, reason); MessageBox.Show(this, "Đã từ chối bill.", "Fuji Homestay"); txtRejectReason.Clear(); await LoadRowsAsync(); }
        catch (Exception ex) { Models.DesktopApi.ShowError(this, ex); }
    }
}
