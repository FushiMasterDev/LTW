namespace HomestayBooking.Web.Models.Entities;

/// <summary>Một phòng cụ thể. Trạng thái theo ngày (trống/đã đặt/bảo trì) suy ra từ RoomNights.</summary>
public class Room
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }

    /// <summary>"101", "Căn A"; unique trong loại phòng.</summary>
    public string RoomCode { get; set; } = string.Empty;

    /// <summary>Còn kinh doanh hay không.</summary>
    public bool IsActive { get; set; } = true;

    public RoomType RoomType { get; set; } = null!;
    public ICollection<RoomNight> RoomNights { get; set; } = new List<RoomNight>();
    public ICollection<RoomBlock> Blocks { get; set; } = new List<RoomBlock>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
