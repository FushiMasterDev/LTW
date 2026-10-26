using Guna.UI2.WinForms;
using HomestayBooking.Desktop.View;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HomestayBooking.Desktop.Tests;

/// <summary>
/// Mở form thật, gọi DesktopHost thật (seed SQLite), kiểm dữ liệu lên bảng và các nút gọi đúng API.
/// Chạy trên Windows: khởi động DesktopHost rồi đặt FUJI_API_URL (mặc định http://127.0.0.1:5080).
/// </summary>
public sealed class DesktopUiTests
{
    private static string[] Errors => DialogWatcher.Messages.Where(m =>
        m.Contains("thất bại") || m.Contains("không hợp lệ") || m.Contains("chưa cung cấp") || m.Contains("không có quyền")
        || m.Contains("Không kết nối") || m.Contains("lỗi", StringComparison.OrdinalIgnoreCase)).ToArray();

    [Theory]
    [InlineData("NV001", typeof(FrmEmployeeHome))]
    [InlineData("KT001", typeof(FrmAccountantDashboard))]
    [InlineData("QL001", typeof(FrmAdminDashboard))]
    [InlineData("AD001", typeof(FrmAdminDashboard))]
    public void LoginOpensDashboardForPosition(string code, Type dashboard) => Ui.Run(() =>
    {
        var login = Ui.Show(new FrmLogin());
        Ui.Field<Guna2TextBox>(login, "txtEmployeeCode").Text = code;
        Ui.Field<Guna2TextBox>(login, "txtPassword").Text = "Fuji@2026";
        Form? opened = null;
        // Dashboard mở bằng ShowDialog nên chỉ timer chạy được bên trong vòng lặp modal của nó.
        using var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += (_, _) =>
        {
            var form = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType() == dashboard);
            if (form is null) return;
            timer.Stop();
            Ui.Settle(1500);
            opened = form; form.Close();
        };
        timer.Start();
        Ui.Click(Ui.Field<Guna2Button>(login, "btnLogin"));
        Ui.Until(() => opened is not null && login.Visible, $"mở {dashboard.Name} rồi quay lại màn đăng nhập");
        Assert.Empty(Errors);
    });

    [Fact]
    public void WrongPasswordShowsServerMessage() => Ui.Run(() =>
    {
        var login = Ui.Show(new FrmLogin());
        Ui.Field<Guna2TextBox>(login, "txtEmployeeCode").Text = "NV001";
        Ui.Field<Guna2TextBox>(login, "txtPassword").Text = "sai-mat-khau";
        Ui.Click(Ui.Field<Guna2Button>(login, "btnLogin"));
        Ui.Until(() => !DialogWatcher.Seen.IsEmpty, "thông báo đăng nhập sai");
        Assert.DoesNotContain(DialogWatcher.Messages, m => m.Contains("không hợp lệ") || m.Contains("Không kết nối"));
    });

    [Fact]
    public void ConsultantBookingListShowsColumnsAndDetails() => Ui.Run(() =>
    {
        Ui.Login("NV001");
        var form = Ui.Show(new FrmBookingManagement());
        var grid = Ui.Field<Guna2DataGridView>(form, "dgvBookings");
        Ui.Until(() => grid.Rows.Count > 0, "danh sách đơn");
        Assert.True(grid.Columns["code"]!.Visible && grid.Columns["status"]!.Visible && !grid.Columns["customerId"]!.Visible, "bảng đơn chỉ hiện cột cần đọc");
        Ui.Until(() => Ui.Field<Guna2ComboBox>(form, "cboAssignTo").Items.Count > 0, "danh sách nhân viên nhận đơn");
        grid.CurrentCell = grid.Rows[0].Cells["code"];
        var nights = Ui.Field<Guna2DataGridView>(form, "dgvBookingNights");
        Ui.Until(() => nights.Rows.Count > 0, "giá từng đêm của đơn");
        Assert.True(Ui.Field<Guna2DataGridView>(form, "dgvStatusHistory").Columns.Contains("toStatus"));
        Assert.Empty(Errors);
    });

    [Fact]
    public void ConsultantCustomerSearchLoadsProfile() => Ui.Run(() =>
    {
        Ui.Login("NV001");
        var form = Ui.Show(new FrmCustomerManagement());
        Ui.Click(Ui.Field<Guna2Button>(form, "btnSearchCustomer"));
        var grid = Ui.Field<Guna2DataGridView>(form, "dgvCustomers");
        Ui.Until(() => grid.Rows.Count > 0, "danh sách khách");
        grid.CurrentCell = grid.Rows[0].Cells["phoneNumber"];
        Ui.Until(() => Ui.Field<Guna2TextBox>(form, "txtCustPhone").Text.Length > 0, "hồ sơ khách");
        Assert.Empty(Errors);
    });

    [Fact]
    public void ConsultantCreatesBookingFromAvailability() => Ui.Run(() =>
    {
        Ui.Login("NV001");
        var form = Ui.Show(new FrmCreateBooking());
        Ui.Until(() => Ui.Field<Guna2ComboBox>(form, "cboHomestay").Items.Count > 0 && Ui.Field<Guna2ComboBox>(form, "cboArea").Items.Count > 1, "ô chọn homestay và khu vực");
        Ui.Field<Guna2DateTimePicker>(form, "dtpCheckIn").Value = DateTime.Today.AddDays(40);
        Ui.Field<Guna2DateTimePicker>(form, "dtpCheckOut").Value = DateTime.Today.AddDays(41);
        Ui.Click(Ui.Field<Guna2Button>(form, "btnCheckAvailability"));
        var offers = Ui.Field<Guna2DataGridView>(form, "dgvAvailability");
        Ui.Until(() => offers.Rows.Count > 0, "phòng trống");
        offers.CurrentCell = offers.Rows[0].Cells["homestayName"];
        typeof(DataGridView).GetMethod("OnCellDoubleClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(offers, [new DataGridViewCellEventArgs(0, 0)]);
        var roomType = Ui.Field<Guna2ComboBox>(form, "cboRoomType");
        Ui.Until(() => roomType.SelectedValue is int id && id == Convert.ToInt32(offers.Rows[0].Cells["roomTypeId"].Value), "chọn loại phòng từ bảng phòng trống");
        Ui.Field<Guna2TextBox>(form, "txtPhone").Text = "0991234588";
        Ui.Field<Guna2TextBox>(form, "txtFullName").Text = "Khách Test Giao Diện";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnSubmitBooking"));
        var result = Ui.Field<Label>(form, "lblResult");
        Ui.Until(() => result.Text.StartsWith("Mã đơn"), "kết quả đặt hộ");
        Assert.Contains("AwaitingPayment", result.Text);
        Assert.Empty(Errors);
    });

    [Fact]
    public void AccountantApprovesReceiptWithImage() => Ui.Run(() =>
    {
        var form = Ui.Show(new FrmReceiptReview(Ui.Login("KT001")));
        var grid = Ui.Field<Guna2DataGridView>(form, "dgvReceipts");
        Ui.Until(() => grid.Rows.Count > 0, "bill chờ duyệt");
        var before = grid.Rows.Count;
        grid.CurrentCell = grid.Rows[0].Cells[0];
        var picture = Ui.Field<Guna2PictureBox>(form, "picReceipt");
        var message = Ui.Field<Guna2HtmlLabel>(form, "lblImageMessage");
        Ui.Until(() => picture.Image is not null || message.Text.StartsWith("PDF"), "ảnh bill tải qua API có token");
        Ui.Click(Ui.Field<Guna2Button>(form, "btnApprove"));
        Ui.Until(() => DialogWatcher.Messages.Any(m => m.StartsWith("Đã duyệt bill")), "thông báo duyệt bill");
        Ui.Until(() => grid.Rows.Count == before - 1, "bill đã duyệt rời danh sách chờ");
        Assert.Empty(Errors);
    });

    [Fact]
    public void AccountantHandlesOpenBankTransactions() => Ui.Run(() =>
    {
        var form = Ui.Show(new FrmBankAndAdditionalDeposit(Ui.Login("KT001")));
        var grid = Ui.Field<Guna2DataGridView>(form, "dgvTransactions");
        Ui.Until(() => grid.Rows.Count >= 2, "ít nhất 2 giao dịch cần xử lý tay");
        var before = grid.Rows.Count;

        grid.CurrentCell = grid.Rows[0].Cells[0];
        Ui.Field<Guna2TextBox>(form, "txtMatchBookingCode").Text = "KHONG-CO-DON";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnMatchTransaction"));
        Ui.Until(() => DialogWatcher.Messages.Any(m => m.Contains("Không tìm thấy đơn")), "lỗi mã đơn không tồn tại từ API");

        Ui.Field<Guna2TextBox>(form, "txtTransactionNote").Text = "Khoản thu nội bộ (test giao diện)";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnIgnoreTransaction"));
        Ui.Until(() => DialogWatcher.Messages.Contains("Đã bỏ qua giao dịch."), "thông báo bỏ qua");
        Ui.Until(() => grid.Rows.Count == before - 1, "giao dịch đã bỏ qua rời danh sách");

        grid.CurrentCell = grid.Rows[0].Cells[0];
        Ui.Field<Guna2TextBox>(form, "txtRefundBankRef").Text = "FT-UI-TEST";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnMarkRefunded"));
        Ui.Until(() => DialogWatcher.Messages.Contains("Đã ghi nhận hoàn trả giao dịch."), "thông báo chuyển trả");
        Ui.Until(() => grid.Rows.Count == before - 2, "giao dịch đã chuyển trả rời danh sách");

        var tabs = Ui.Field<Guna2TabControl>(form, "tabs");
        tabs.SelectedTab = Ui.Field<TabPage>(form, "tabDeposits");
        Ui.Until(() => Ui.Field<Guna2HtmlLabel>(form, "lblDepositPage").Text.Contains("yêu cầu"), "tab cọc bổ sung tải xong");
        Assert.DoesNotContain(DialogWatcher.Messages, m => m.Contains("chưa cung cấp") || m.Contains("không có quyền"));
    });

    [Fact]
    public void AccountantCatalogLookupLoadsAllPages() => Ui.Run(() =>
    {
        var session = Ui.Login("KT001");
        var total = Ui.ApiTotal("areas", session);
        var form = Ui.Show(new FrmCatalogLookup(session));
        var areas = Ui.Field<Guna2DataGridView>(form, "dgvAreas");
        Ui.Until(() => areas.Rows.Count == total, $"đủ {total} khu vực qua mọi trang");
        Assert.True(Ui.Field<Guna2DataGridView>(form, "dgvHolidays").Rows.Count > 0);
        Assert.Empty(Errors);
    });

    [Fact]
    public void ManagerReportLoads() => Ui.Run(() =>
    {
        var form = Ui.Show(new FrmAnalyticsReports(Ui.Login("QL001")));
        var state = Ui.Field<Guna2HtmlLabel>(form, "lblReportState");
        Ui.Until(() => state.Text?.StartsWith("Dữ liệu từ") == true, "báo cáo quản lý");
        Assert.True(Ui.Field<Guna2DataGridView>(form, "dgvEmployees").Columns.Contains("Mã_NV"));
        Assert.True(Ui.Field<Guna2ComboBox>(form, "cboArea").Items.Count > 1);
    });

    [Fact]
    public void ManagerAddsUpdatesAndDeletesHoliday() => Ui.Run(() =>
    {
        var form = Ui.Show(new FrmCatalogManagement(Ui.Login("QL001")));
        var grid = Ui.Field<Guna2DataGridView>(form, "dgvItems");
        Ui.Until(() => grid.Rows.Count > 0, "danh sách khu vực");
        Ui.Click(Ui.Field<Guna2Button>(form, "btnHolidays"));
        Ui.Until(() => grid.Rows.Count > 0 && grid.Columns.Contains("Date"), "danh sách ngày lễ");

        Ui.Click(Ui.Field<Guna2Button>(form, "btnClear"));
        Ui.Field<Guna2DateTimePicker>(form, "dtpHolidayDate").Value = new DateTime(2031, 12, 29);
        Ui.Field<Guna2TextBox>(form, "txtHolidayName").Text = "Ngày lễ test";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnAdd"));
        var status = Ui.Field<Guna2HtmlLabel>(form, "lblStatus");
        Ui.Until(() => status.Text == "Đã thêm.", "thêm ngày lễ");

        DataGridViewRow Row() => grid.Rows.Cast<DataGridViewRow>().Single(r => (string)r.Cells["Date"].Value == "2031-12-29");
        grid.CurrentCell = Row().Cells["Date"];
        Ui.Field<Guna2TextBox>(form, "txtHolidayName").Text = "Ngày lễ test đã sửa";
        Ui.Click(Ui.Field<Guna2Button>(form, "btnUpdate"));
        Ui.Until(() => status.Text == "Đã cập nhật.", "sửa ngày lễ");
        Assert.Equal("Ngày lễ test đã sửa", Row().Cells["Name"].Value);

        grid.CurrentCell = Row().Cells["Date"];
        Ui.Click(Ui.Field<Guna2Button>(form, "btnDelete"));
        Ui.Until(() => status.Text == "Đã xóa.", "xóa ngày lễ");
        Assert.DoesNotContain(grid.Rows.Cast<DataGridViewRow>(), r => (string)r.Cells["Date"].Value == "2031-12-29");
        Assert.Empty(Errors);
    });
}
