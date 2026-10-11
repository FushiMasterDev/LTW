namespace HomestayBooking.Web.Models.Entities;

/// <summary>Ảnh homestay; có RoomTypeId khi là ảnh của một loại phòng.</summary>
public class HomestayImage
{
    public int Id { get; set; }
    public int HomestayId { get; set; }
    public int? RoomTypeId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Mỗi homestay một ảnh bìa.</summary>
    public bool IsCover { get; set; }

    public Homestay Homestay { get; set; } = null!;
    public RoomType? RoomType { get; set; }
}
