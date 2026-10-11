namespace HomestayBooking.Web.Models.Entities;

/// <summary>Khu vực (Hoàn Kiếm, Sa Pa...). Không liên quan tới MVC Areas.</summary>
public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Tỉnh/thành: Fuji mở rộng ra cả nước.</summary>
    public string Province { get; set; } = string.Empty;

    public AreaGroup AreaGroup { get; set; } = AreaGroup.InnerCity;

    public ICollection<Homestay> Homestays { get; set; } = new List<Homestay>();
}
