namespace HomestayBooking.Web.Models.Entities;

/// <summary>
/// Hồ sơ nghiệp vụ chủ homestay (v5 mục 4.6). Có thể tạo trước Account (nhân viên nhập hồ sơ nháp).
/// Trạng thái duyệt nằm ở OwnerRegistration, không ở Account.
/// </summary>
public class Owner
{
    public int Id { get; set; }
    public int PersonId { get; set; }

    // Tài khoản nhận tiền đối soát (BR-03); bắt buộc trước khi hồ sơ được duyệt (kiểm tra ở service)
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? BankAccountName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PersonProfile Person { get; set; } = null!;
    public ICollection<OwnerRegistration> Registrations { get; set; } = new List<OwnerRegistration>();
    public ICollection<Homestay> Homestays { get; set; } = new List<Homestay>();
}
