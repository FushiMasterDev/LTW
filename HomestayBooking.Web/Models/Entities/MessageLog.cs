namespace HomestayBooking.Web.Models.Entities;

/// <summary>Tin gửi ra ngoài (SMS/Zalo/Email), để đối chiếu chi phí (NFR-12). Không lưu nội dung OTP.</summary>
public class MessageLog
{
    public long Id { get; set; }
    public MessageChannel Channel { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientEmail { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking? Booking { get; set; }
}
