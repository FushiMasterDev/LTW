namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Một đêm bị chiếm của một phòng. Khóa chính (RoomId, Date) là cơ chế chống trùng phòng (BR-05, NFR-01).
/// Đúng một chủ: đơn đang giữ phòng, khóa ngày hoặc yêu cầu đổi đang giữ phòng mới.
/// </summary>
public class RoomNight
{
    public int RoomId { get; set; }
    public DateOnly Date { get; set; }
    public int? BookingId { get; set; }
    public int? RoomBlockId { get; set; }
    public int? BookingChangeRequestId { get; set; }

    public Room Room { get; set; } = null!;
    public Booking? Booking { get; set; }
    public RoomBlock? RoomBlock { get; set; }
    public BookingChangeRequest? BookingChangeRequest { get; set; }
}
