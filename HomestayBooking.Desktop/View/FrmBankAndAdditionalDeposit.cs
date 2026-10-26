using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Models.Accountant;

namespace HomestayBooking.Desktop.View;

internal partial class FrmBankAndAdditionalDeposit : Form, IAccountantRefreshable
{
    private readonly AccountantController? _api;
    private int _transactionPage = 1, _depositPage = 1, _transactionPages = 1, _depositPages = 1;
    private BankTransactionDto? SelectedTransaction => dgvTransactions.CurrentRow?.DataBoundItem as BankTransactionDto;
    private AdditionalDepositDto? SelectedDeposit => dgvDeposits.CurrentRow?.DataBoundItem as AdditionalDepositDto;
    public FrmBankAndAdditionalDeposit()
    {
        InitializeComponent();
        WireEvents();
    }
    public FrmBankAndAdditionalDeposit(EmployeeSession session) : this() { _api = new AccountantController(session); cboMatchStatus.Items.AddRange(["Cần xử lý", "Unmatched", "Underpaid", "LatePayment", "Matched", "ManuallyMatched", "Refunded", "Ignored"]); cboMatchStatus.SelectedIndex = 0; Shown += async (_, _) => await RefreshCurrentAsync(); }
    private void WireEvents()
    {
        btnRefresh.Click += async (_, _) => await RefreshCurrentAsync(); cboMatchStatus.SelectedIndexChanged += async (_, _) => { _transactionPage = 1; await LoadTransactionsAsync(); };
        tabs.SelectedIndexChanged += async (_, _) => await RefreshCurrentAsync();
        btnTransactionPrev.Click += async (_, _) => { if (_transactionPage > 1) { _transactionPage--; await LoadTransactionsAsync(); } }; btnTransactionNext.Click += async (_, _) => { if (_transactionPage < _transactionPages) { _transactionPage++; await LoadTransactionsAsync(); } };
        btnDepositPrev.Click += async (_, _) => { if (_depositPage > 1) { _depositPage--; await LoadDepositsAsync(); } }; btnDepositNext.Click += async (_, _) => { if (_depositPage < _depositPages) { _depositPage++; await LoadDepositsAsync(); } };
        dgvTransactions.CurrentCellChanged += (_, _) => UpdateTransactionButtons();
        btnMatchTransaction.Click += async (_, _) => await MatchTransactionAsync(); btnIgnoreTransaction.Click += async (_, _) => await IgnoreTransactionAsync();
        dgvDeposits.CurrentCellChanged += (_, _) => UpdateDepositButtons();
        btnMarkRefunded.Click += async (_, _) => await RefundTransactionAsync(); btnConfirmDeposit.Click += async (_, _) => await DecideDepositAsync(true); btnRejectDeposit.Click += async (_, _) => await DecideDepositAsync(false);
    }
    public Task RefreshDataAsync() => RefreshCurrentAsync();
    private Task RefreshCurrentAsync() => tabs.SelectedTab == tabTransactions ? LoadTransactionsAsync() : LoadDepositsAsync();
    private async Task LoadTransactionsAsync() { if (_api is null) return; try { SetBusy(true); var r = await _api.GetBankTransactionsAsync(cboMatchStatus.SelectedIndex <= 0 ? null : cboMatchStatus.Text, _transactionPage); var rows = r?.Items ?? []; dgvTransactions.DataSource = rows; _transactionPage = r?.Page ?? 1; _transactionPages = Math.Max(1, r?.TotalPages ?? 1); lblTransactionPage.Text = $"Trang {_transactionPage}/{_transactionPages} · {r?.TotalCount ?? 0} giao dịch"; } catch (Exception ex) { DesktopApi.ShowError(this, ex); } finally { SetBusy(false); } }
    private async Task LoadDepositsAsync() { if (_api is null) return; try { SetBusy(true); var r = await _api.GetAdditionalDepositsAsync(_depositPage); dgvDeposits.DataSource = r?.Items ?? []; _depositPage = r?.Page ?? 1; _depositPages = Math.Max(1, r?.TotalPages ?? 1); lblDepositPage.Text = $"Trang {_depositPage}/{_depositPages} · {r?.TotalCount ?? 0} yêu cầu"; UpdateDepositButtons(); } catch (Exception ex) { DesktopApi.ShowError(this, ex); } finally { SetBusy(false); } }
    // Chỉ khoản còn mở (chuyển thiếu, đến muộn, không rõ đơn) mới xử lý tay được.
    private void UpdateTransactionButtons() { var open = SelectedTransaction is { MatchStatus: "Unmatched" or "Underpaid" or "LatePayment" }; btnMarkRefunded.Enabled = open; btnMatchTransaction.Enabled = open; btnIgnoreTransaction.Enabled = open; }
    private async Task MatchTransactionAsync() { var row = SelectedTransaction; if (row is null) return; var code = txtMatchBookingCode.Text.Trim(); if (code.Length == 0) { MessageBox.Show(this, "Nhập mã đơn cần khớp."); txtMatchBookingCode.Focus(); return; } try { await _api!.MatchTransactionAsync(row.Id, code, string.IsNullOrWhiteSpace(txtTransactionNote.Text) ? null : txtTransactionNote.Text.Trim()); MessageBox.Show(this, $"Đã khớp giao dịch với đơn {code}, đơn chuyển sang Đã đặt cọc."); txtMatchBookingCode.Clear(); txtTransactionNote.Clear(); await LoadTransactionsAsync(); } catch (Exception ex) { DesktopApi.ShowError(this, ex); } }
    private async Task IgnoreTransactionAsync() { var row = SelectedTransaction; if (row is null) return; var reason = txtTransactionNote.Text.Trim(); if (reason.Length == 0) { MessageBox.Show(this, "Nhập lý do bỏ qua vào ô ghi chú."); txtTransactionNote.Focus(); return; } if (MessageBox.Show(this, "Bỏ qua giao dịch này? Chỉ dùng cho tiền vào không liên quan đặt phòng.", "Bỏ qua giao dịch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; try { await _api!.IgnoreTransactionAsync(row.Id, reason); MessageBox.Show(this, "Đã bỏ qua giao dịch."); txtTransactionNote.Clear(); await LoadTransactionsAsync(); } catch (Exception ex) { DesktopApi.ShowError(this, ex); } }
    private void UpdateDepositButtons() { var enabled = SelectedDeposit is not null; btnConfirmDeposit.Enabled = enabled; btnRejectDeposit.Enabled = enabled; }
    private async Task RefundTransactionAsync() { var row = SelectedTransaction; if (row is null) return; var bankRef = txtRefundBankRef.Text.Trim(); if (bankRef.Length == 0) { MessageBox.Show(this, "Nhập mã giao dịch ngân hàng hoàn trả."); txtRefundBankRef.Focus(); return; } try { await _api!.RefundTransactionAsync(row.Id, bankRef, txtTransactionNote.Text.Trim()); MessageBox.Show(this, "Đã ghi nhận hoàn trả giao dịch."); txtRefundBankRef.Clear(); await LoadTransactionsAsync(); } catch (Exception ex) { DesktopApi.ShowError(this, ex); } }
    private async Task DecideDepositAsync(bool confirm) { var row = SelectedDeposit; if (row is null) return; var note = (confirm ? txtFinanceNote.Text : txtRejectionReason.Text).Trim(); if (note.Length == 0) { MessageBox.Show(this, confirm ? "Nhập ghi chú tài chính." : "Nhập lý do từ chối."); (confirm ? txtFinanceNote : txtRejectionReason).Focus(); return; } try { if (confirm) await _api!.ConfirmAdditionalDepositAsync(row.ChangeRequestId, note); else await _api!.RejectAdditionalDepositAsync(row.ChangeRequestId, note); MessageBox.Show(this, confirm ? "Đã xác nhận cọc bổ sung và áp dụng đổi lịch." : "Đã từ chối cọc bổ sung."); txtFinanceNote.Clear(); txtRejectionReason.Clear(); await LoadDepositsAsync(); } catch (Exception ex) { DesktopApi.ShowError(this, ex); } }
    private void SetBusy(bool busy) { btnRefresh.Enabled = !busy; }

    private void FrmBankAndAdditionalDeposit_Load(object sender, EventArgs e)
    {

    }

    private void btnTransactionPrev_Click(object sender, EventArgs e)
    {

    }
}
