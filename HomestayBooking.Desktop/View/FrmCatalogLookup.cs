using Guna.UI2.WinForms;
using HomestayBooking.Desktop.Controllers;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.View;

internal partial class FrmCatalogLookup : Form, IAccountantRefreshable
{
    private readonly AccountantController? _api;
    public FrmCatalogLookup() { InitializeComponent(); btnRefresh.Click += async (_, _) => await LoadCatalogsAsync(); }
    public FrmCatalogLookup(EmployeeSession session) : this()
    {
        _api = new AccountantController(session);
        Shown += async (_, _) => await LoadCatalogsAsync();
    }
    public Task RefreshDataAsync() => LoadCatalogsAsync();
    private async Task LoadCatalogsAsync()
    {
        if (_api is null) return;
        try
        {
            btnRefresh.Enabled = false;
            dgvHolidays.DataSource = await _api.GetHolidaysAsync() ?? [];
            dgvAreas.DataSource = await _api.GetAreasAsync() ?? [];
            dgvRoomCategories.DataSource = await _api.GetRoomCategoriesAsync() ?? [];
            dgvAmenities.DataSource = await _api.GetAmenitiesAsync() ?? [];
        }
        catch (Exception ex) { DesktopApi.ShowError(this, ex); }
        finally { btnRefresh.Enabled = true; }
    }
}
