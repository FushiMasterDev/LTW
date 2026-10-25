using Guna.UI2.WinForms;
namespace HomestayBooking.Desktop.View;
partial class FrmCatalogLookup
{
    private Guna2TabControl tabs=null!; private TabPage tabHolidays=null!,tabAreas=null!,tabRoomCategories=null!,tabAmenities=null!; private Guna2DataGridView dgvHolidays=null!,dgvAreas=null!,dgvRoomCategories=null!,dgvAmenities=null!; private Guna2Button btnRefresh=null!;
    private void InitializeComponent()
    {
        tabs=new Guna2TabControl();tabHolidays=new TabPage("Ngày lễ");tabAreas=new TabPage("Khu vực");tabRoomCategories=new TabPage("Loại phòng");tabAmenities=new TabPage("Tiện nghi");dgvHolidays=new Guna2DataGridView();dgvAreas=new Guna2DataGridView();dgvRoomCategories=new Guna2DataGridView();dgvAmenities=new Guna2DataGridView();btnRefresh=new Guna2Button();
        foreach(var grid in new[]{dgvHolidays,dgvAreas,dgvRoomCategories,dgvAmenities}){grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AutoGenerateColumns=true;grid.AllowUserToAddRows=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;}
        tabHolidays.Controls.Add(dgvHolidays);tabAreas.Controls.Add(dgvAreas);tabRoomCategories.Controls.Add(dgvRoomCategories);tabAmenities.Controls.Add(dgvAmenities);tabs.Dock=DockStyle.Fill;tabs.TabPages.AddRange(new[]{tabHolidays,tabAreas,tabRoomCategories,tabAmenities});btnRefresh.Text="Làm mới";btnRefresh.Dock=DockStyle.Top;btnRefresh.Height=42;Controls.Add(tabs);Controls.Add(btnRefresh);Text="Tra cứu Ngày lễ & Danh mục";ClientSize=new System.Drawing.Size(1000,650);ResumeLayout(false);
    }
}
