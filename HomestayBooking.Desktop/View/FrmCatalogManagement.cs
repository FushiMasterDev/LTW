using System.Text.Json;
using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Models.Admin;

namespace HomestayBooking.Desktop.View;

internal partial class FrmCatalogManagement : Form, IAccountantRefreshable
{
    private readonly AdminController _api;
    private string _active = "areas";
    private int? _selectedId;
    public FrmCatalogManagement() : this(AppSession.Current ?? new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }
    public FrmCatalogManagement(EmployeeSession session)
    {
        InitializeComponent(); _api = new AdminController(session);
        btnAreas.Click += async (_, _) => await SelectTabAsync("areas"); btnAmenities.Click += async (_, _) => await SelectTabAsync("amenities"); btnCategories.Click += async (_, _) => await SelectTabAsync("room-categories"); btnHolidays.Click += async (_, _) => await SelectTabAsync("holidays");
        dgvItems.SelectionChanged += (_, _) => SelectRow(); btnAdd.Click += async (_, _) => await SaveAsync(false); btnUpdate.Click += async (_, _) => await SaveAsync(true); btnDelete.Click += async (_, _) => await DeleteAsync(); btnClear.Click += (_, _) => Clear();
        Shown += async (_, _) => await RefreshDataAsync();
    }
    public async Task RefreshDataAsync() => await SelectTabAsync(_active);
    private async Task SelectTabAsync(string tab)
    {
        _active = tab; _selectedId = null; Clear(); lblIdLabel.Visible = tab != "holidays"; txtId.Visible = tab != "holidays";
        txtAreaName.Visible = tab == "areas"; txtProvince.Visible = tab == "areas"; cboAreaGroup.Visible = tab == "areas";
        txtAmenityName.Visible = tab == "amenities"; txtAmenityIcon.Visible = tab == "amenities";
        txtCategoryName.Visible = tab == "room-categories"; dtpHolidayDate.Visible = tab == "holidays"; txtHolidayName.Visible = tab == "holidays";
        try
        {
            object? data = tab switch
            {
                "areas" => await _api.GetAsync<List<AdminAreaDto>>("accountant/areas"),
                "amenities" => await _api.GetAsync<List<AdminCatalogItemDto>>("accountant/amenities"),
                "room-categories" => await _api.GetAsync<List<AdminCatalogItemDto>>("accountant/room-categories"),
                _ => await _api.GetAsync<List<AdminHolidayDto>>("accountant/holidays")
            };
            dgvItems.DataSource = data;
            lblStatus.Text = $"Danh mục: {tab} · Máy chủ hiện chỉ hỗ trợ tải danh sách.";
        }
        catch (Exception ex) { lblStatus.Text = ex.Message; }
    }
    private void SelectRow()
    {
        if (dgvItems.CurrentRow?.DataBoundItem is null) return;
        var item = dgvItems.CurrentRow.DataBoundItem;
        if (item is AdminAreaDto area) { _selectedId = area.Id; txtAreaName.Text = area.Name; txtProvince.Text = area.Province; cboAreaGroup.Text = area.Group; }
        else if (item is AdminCatalogItemDto catalog) { _selectedId = catalog.Id; if (_active == "amenities") txtAmenityName.Text = catalog.Name; else txtCategoryName.Text = catalog.Name; }
        else if (item is AdminHolidayDto holiday) { dtpHolidayDate.Value = DateTime.TryParse(holiday.Date, out var date) ? date : DateTime.Today; txtHolidayName.Text = holiday.Name; }
    }
    private async Task SaveAsync(bool update)
    {
        MessageBox.Show(this, "Backend hiện chưa cung cấp API tạo/cập nhật danh mục Desktop. Dữ liệu không được thay đổi.", "Chưa hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Task.CompletedTask;
    }
    private async Task DeleteAsync()
    {
        MessageBox.Show(this, "Backend hiện chưa cung cấp API xóa danh mục Desktop. Dữ liệu không được thay đổi.", "Chưa hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Task.CompletedTask;
    }
    private void Clear() { _selectedId = null; txtAreaName.Clear(); txtProvince.Clear(); txtAmenityName.Clear(); txtAmenityIcon.Clear(); txtCategoryName.Clear(); txtHolidayName.Clear(); dtpHolidayDate.Enabled = true; }
}
