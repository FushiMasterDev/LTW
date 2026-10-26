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
    private string? _selectedHoliday;
    public FrmCatalogManagement() : this(AppSession.Current ?? new EmployeeSession("", "", "Manager", "", "", DateTime.MaxValue)) { }
    public FrmCatalogManagement(EmployeeSession session)
    {
        InitializeComponent(); _api = new AdminController(session);
        btnAreas.Click += async (_, _) => await SelectTabAsync("areas"); btnAmenities.Click += async (_, _) => await SelectTabAsync("amenities"); btnCategories.Click += async (_, _) => await SelectTabAsync("room-categories"); btnHolidays.Click += async (_, _) => await SelectTabAsync("holidays");
        // CurrentCellChanged thay vì SelectionChanged: lúc SelectionChanged chạy, CurrentRow có thể vẫn là dòng cũ (đã sửa nhầm ngày lễ).
        dgvItems.CurrentCellChanged += (_, _) => SelectRow(); btnAdd.Click += async (_, _) => await SaveAsync(false); btnUpdate.Click += async (_, _) => await SaveAsync(true); btnDelete.Click += async (_, _) => await DeleteAsync(); btnClear.Click += (_, _) => Clear();
        Shown += async (_, _) => await RefreshDataAsync();
    }
    public async Task RefreshDataAsync() => await SelectTabAsync(_active);
    private async Task SelectTabAsync(string tab)
    {
        _active = tab; Clear(); lblIdLabel.Visible = tab != "holidays"; txtId.Visible = tab != "holidays";
        txtAreaName.Visible = tab == "areas"; txtProvince.Visible = tab == "areas"; cboAreaGroup.Visible = tab == "areas";
        txtAmenityName.Visible = tab == "amenities"; txtAmenityIcon.Visible = tab == "amenities";
        txtCategoryName.Visible = tab == "room-categories"; dtpHolidayDate.Visible = tab == "holidays"; txtHolidayName.Visible = tab == "holidays";
        try
        {
            object data = tab switch
            {
                "areas" => await _api.GetAllAsync<AdminAreaDto>("areas"),
                "amenities" => await _api.GetAllAsync<AdminAmenityDto>("amenities"),
                "room-categories" => await _api.GetAllAsync<AdminCatalogItemDto>("room-categories"),
                _ => await _api.GetAllAsync<AdminHolidayDto>("holidays")
            };
            dgvItems.DataSource = data;
            lblStatus.Text = $"Danh mục: {tab}";
        }
        catch (Exception ex) { lblStatus.Text = ex.Message; }
    }
    private void SelectRow()
    {
        if (dgvItems.CurrentRow?.DataBoundItem is null) return;
        var item = dgvItems.CurrentRow.DataBoundItem;
        if (item is AdminAreaDto area) { _selectedId = area.Id; txtId.Text = area.Id.ToString(); txtAreaName.Text = area.Name; txtProvince.Text = area.Province; cboAreaGroup.Text = area.AreaGroup; }
        else if (item is AdminAmenityDto amenity) { _selectedId = amenity.Id; txtId.Text = amenity.Id.ToString(); txtAmenityName.Text = amenity.Name; txtAmenityIcon.Text = amenity.Icon ?? ""; }
        else if (item is AdminCatalogItemDto category) { _selectedId = category.Id; txtId.Text = category.Id.ToString(); txtCategoryName.Text = category.Name; }
        // Ngày lễ dùng chính ngày làm khóa: Cập nhật chỉ đổi tên của ngày đang chọn.
        else if (item is AdminHolidayDto holiday) { _selectedHoliday = holiday.Date; dtpHolidayDate.Value = DateTime.TryParse(holiday.Date, out var date) ? date : DateTime.Today; txtHolidayName.Text = holiday.Name; }
    }
    private string? SelectedKey => _active == "holidays" ? _selectedHoliday : _selectedId?.ToString();
    private async Task SaveAsync(bool update)
    {
        if (update && SelectedKey is null) { MessageBox.Show(this, "Chọn một mục trong danh sách để cập nhật."); return; }
        object body = _active switch
        {
            "areas" => new { name = txtAreaName.Text.Trim(), province = txtProvince.Text.Trim(), areaGroup = string.IsNullOrWhiteSpace(cboAreaGroup.Text) ? "InnerCity" : cboAreaGroup.Text },
            "amenities" => new { name = txtAmenityName.Text.Trim(), icon = string.IsNullOrWhiteSpace(txtAmenityIcon.Text) ? null : txtAmenityIcon.Text.Trim() },
            "room-categories" => new { name = txtCategoryName.Text.Trim() },
            _ when update => new { name = txtHolidayName.Text.Trim() },
            _ => new { date = dtpHolidayDate.Value.ToString("yyyy-MM-dd"), name = txtHolidayName.Text.Trim() }
        };
        try
        {
            if (update) await _api.PutAsync<JsonElement>($"{_active}/{SelectedKey}", body);
            else await _api.PostAsync<JsonElement>(_active, body);
            await SelectTabAsync(_active);
            lblStatus.Text = update ? "Đã cập nhật." : "Đã thêm.";
        }
        catch (Exception ex) { DesktopApi.ShowError(this, ex); }
    }
    private async Task DeleteAsync()
    {
        if (SelectedKey is not { } key) { MessageBox.Show(this, "Chọn một mục trong danh sách để xóa."); return; }
        if (MessageBox.Show(this, "Xóa mục này? Mục đang được dùng sẽ không xóa được.", "Xóa danh mục", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await _api.DeleteAsync<object>($"{_active}/{key}");
            await SelectTabAsync(_active);
            lblStatus.Text = "Đã xóa.";
        }
        catch (Exception ex) { DesktopApi.ShowError(this, ex); }
    }
    private void Clear() { _selectedId = null; _selectedHoliday = null; txtId.Clear(); txtAreaName.Clear(); txtProvince.Clear(); txtAmenityName.Clear(); txtAmenityIcon.Clear(); txtCategoryName.Clear(); txtHolidayName.Clear(); dtpHolidayDate.Enabled = true; }
}
