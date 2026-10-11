namespace HomestayBooking.Web.Models.Entities;

public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên class Bootstrap Icons, ví dụ "bi-wifi".</summary>
    public string? Icon { get; set; }

    public ICollection<HomestayAmenity> HomestayAmenities { get; set; } = new List<HomestayAmenity>();
}
