namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Giấy tờ xác minh (BR-12, v5 mục 4.6): nộp kèm hồ sơ đăng ký Owner, hoặc của một homestay thêm sau.
/// Đúng một trong OwnerRegistrationId và HomestayId có giá trị. File lưu ngoài wwwroot.
/// </summary>
public class VerificationDocument
{
    public int Id { get; set; }
    public int? OwnerRegistrationId { get; set; }
    public int? HomestayId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OwnerRegistration? OwnerRegistration { get; set; }
    public Homestay? Homestay { get; set; }
}
