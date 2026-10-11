namespace HomestayBooking.Web.Models.Entities;

/// <summary>Hồ sơ đăng ký và xét duyệt Owner (v5 mục 4.7). Mỗi Owner chỉ có một hồ sơ đang xử lý.</summary>
public class OwnerRegistration
{
    /// <summary>Các trạng thái còn đang xử lý; dùng cho filtered unique index và truy vấn.</summary>
    public static readonly OwnerRegistrationStatus[] OpenStatuses =
    [
        OwnerRegistrationStatus.Draft,
        OwnerRegistrationStatus.WaitingOwnerActivation,
        OwnerRegistrationStatus.PendingReview,
        OwnerRegistrationStatus.UnderReview,
        OwnerRegistrationStatus.NeedsRevision,
    ];

    public int Id { get; set; }
    public int OwnerId { get; set; }
    public OwnerRegistrationSource Source { get; set; }
    public OwnerRegistrationStatus Status { get; set; } = OwnerRegistrationStatus.Draft;

    /// <summary>Null khi Owner tự đăng ký.</summary>
    public int? CreatedByEmployeeId { get; set; }

    public int? ReviewedByEmployeeId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Owner Owner { get; set; } = null!;
    public Employee? CreatedByEmployee { get; set; }
    public Employee? ReviewedByEmployee { get; set; }
    public ICollection<VerificationDocument> Documents { get; set; } = new List<VerificationDocument>();
}
