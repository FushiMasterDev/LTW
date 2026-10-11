namespace HomestayBooking.Web.Models.Entities;

/// <summary>Loại phòng của một homestay; khách đặt theo loại phòng, giá theo loại phòng (D2, D3).</summary>
public class RoomType
{
    public int Id { get; set; }
    public int HomestayId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public int? AreaM2 { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Homestay Homestay { get; set; } = null!;
    public RoomCategory Category { get; set; } = null!;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<RoomTypePrice> Prices { get; set; } = new List<RoomTypePrice>();
    public ICollection<HomestayImage> Images { get; set; } = new List<HomestayImage>();
}
