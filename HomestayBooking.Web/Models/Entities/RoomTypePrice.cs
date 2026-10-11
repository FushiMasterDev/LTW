namespace HomestayBooking.Web.Models.Entities;

/// <summary>Giá một đêm theo loại ngày, có ngày hiệu lực (BR-08). Đổi giá = thêm dòng mới.</summary>
public class RoomTypePrice
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public DayType DayType { get; set; }
    public decimal Price { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RoomType RoomType { get; set; } = null!;
}
