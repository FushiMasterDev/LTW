namespace HomestayBooking.Web.Models.Entities;

/// <summary>Chủ homestay khóa ngày (khách kênh khác, bảo trì). EndDate là ngày trả, không tính đêm đó.</summary>
public class RoomBlock
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public RoomBlockReason Reason { get; set; }
    public string? Note { get; set; }
    public string CreatedByAccountId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
    public ApplicationUser CreatedByAccount { get; set; } = null!;
    public ICollection<RoomNight> Nights { get; set; } = new List<RoomNight>();
}
