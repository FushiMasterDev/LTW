namespace HomestayBooking.Web.Models.Entities;

/// <summary>Danh mục loại phòng dùng chung (Phòng đôi, Nguyên căn...) để lọc tìm kiếm. Admin quản lý.</summary>
public class RoomCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
}
