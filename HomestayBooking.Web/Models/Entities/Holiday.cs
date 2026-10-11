namespace HomestayBooking.Web.Models.Entities;

/// <summary>Ngày áp giá lễ (BR-08). Khóa chính là ngày.</summary>
public class Holiday
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}
