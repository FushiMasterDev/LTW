namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Một đêm bị chiếm của một phòng. Khóa chính (RoomId, Date) là cơ chế chống trùng phòng (BR-05, NFR-01).
/// Đúng một trong BookingId (đơn đang giữ phòng) và RoomBlockId (chủ khóa ngày) có giá trị.
/// </summary>
public class RoomNight
{
    public int RoomId { get; set; }
    public DateOnly Date { get; set; }
    public int? BookingId { get; set; }
    public int? RoomBlockId { get; set; }

    public Room Room { get; set; } = null!;
    public Booking? Booking { get; set; }
    public RoomBlock? RoomBlock { get; set; }
}
