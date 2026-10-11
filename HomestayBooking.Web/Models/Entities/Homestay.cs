namespace HomestayBooking.Web.Models.Entities;

public class Homestay
{
    public int Id { get; set; }

    /// <summary>Chủ homestay (v5 mục 4.9: HOMESTAY.owner_id → OWNER, không trỏ tới Account).</summary>
    public int OwnerId { get; set; }

    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Địa chỉ chính xác chỉ gửi khách sau khi đơn được xác nhận (BR-13).</summary>
    public string Address { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>HTML đã lọc bằng HtmlSanitizer.</summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>Hướng dẫn nhận phòng, chỉ gửi khách sau khi xác nhận (BR-13). HTML đã lọc.</summary>
    public string? CheckinGuideHtml { get; set; }

    /// <summary>SĐT liên hệ (E.164), chỉ gửi khách sau khi xác nhận (BR-13).</summary>
    public string? ContactPhone { get; set; }

    public TimeOnly CheckinTime { get; set; } = new(14, 0);
    public TimeOnly CheckoutTime { get; set; } = new(12, 0);

    /// <summary>Chỉ Active (và có hợp đồng hiệu lực) mới hiện cho khách.</summary>
    public HomestayStatus Status { get; set; } = HomestayStatus.Draft;

    public int? ApprovedByEmployeeId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Owner Owner { get; set; } = null!;
    public Area Area { get; set; } = null!;
    public Employee? ApprovedByEmployee { get; set; }
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    public ICollection<HomestayImage> Images { get; set; } = new List<HomestayImage>();
    public ICollection<HomestayAmenity> HomestayAmenities { get; set; } = new List<HomestayAmenity>();
    public ICollection<CancellationPolicy> CancellationPolicies { get; set; } = new List<CancellationPolicy>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}
