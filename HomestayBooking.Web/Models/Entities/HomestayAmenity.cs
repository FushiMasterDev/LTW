namespace HomestayBooking.Web.Models.Entities;

public class HomestayAmenity
{
    public int HomestayId { get; set; }
    public int AmenityId { get; set; }

    public Homestay Homestay { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
