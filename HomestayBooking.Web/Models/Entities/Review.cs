namespace HomestayBooking.Web.Models.Entities;

/// <summary>Mỗi đơn hoàn thành được đánh giá một lần (BR-09); chủ homestay được trả lời (FR-06).</summary>
public class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    /// <summary>Lưu thừa để tính điểm trung bình nhanh.</summary>
    public int HomestayId { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime? RepliedAt { get; set; }

    /// <summary>Fuji ẩn đánh giá vi phạm.</summary>
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public Homestay Homestay { get; set; } = null!;
}
