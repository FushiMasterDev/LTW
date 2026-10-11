namespace HomestayBooking.Web.Models.Entities;

/// <summary>Giá từng đêm đã chốt khi đặt (BR-08). Khóa chính (BookingId, Date).</summary>
public class BookingNight
{
    public int BookingId { get; set; }
    public DateOnly Date { get; set; }
    public DayType DayType { get; set; }
    public decimal Price { get; set; }

    public Booking Booking { get; set; } = null!;
}
