namespace HomestayBooking.Web.Models.Entities;

/// <summary>Số thứ tự mã đơn theo ngày (BR-07): tăng trong cùng transaction với việc tạo đơn.</summary>
public class BookingCodeCounter
{
    public DateOnly Date { get; set; }
    public int LastSeq { get; set; }
}
