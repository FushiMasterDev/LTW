using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>FR-25: form công khai "Trở thành đối tác", map sang OwnerRegistrationInput.</summary>
public class PartnerRegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(200, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên homestay")]
    public string HomestayName { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Giới thiệu ngắn")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn {0}.")]
    [Display(Name = "Khu vực")]
    public int? AreaId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(300, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Địa chỉ homestay")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Điện thoại homestay")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Họ tên chủ homestay")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Số điện thoại đăng nhập")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Ngân hàng")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(30, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Số tài khoản")]
    public string BankAccountNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Chủ tài khoản")]
    public string BankAccountName { get; set; } = string.Empty;

    /// <summary>Chỉ bắt buộc khi chưa đăng nhập: tài khoản có sẵn giữ mật khẩu cũ.</summary>
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} phải có từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string? ConfirmPassword { get; set; }

    // Hiển thị
    public IReadOnlyList<SelectListItem> Areas { get; set; } = [];
    public bool SignedIn { get; set; }

    public OwnerRegistrationInput ToInput() => new(FullName, PhoneNumber, HomestayName, Address, AreaId ?? 0, ContactPhone,
        Description, BankName, BankAccountNo, BankAccountName);
}

/// <param name="Registration">Hồ sơ gần nhất của chủ (MineAsync).</param>
/// <param name="Editable">Còn được tải giấy tờ và gửi hồ sơ (nháp hoặc cần bổ sung).</param>
/// <param name="Missing">Danh sách còn thiếu do SubmitAsync trả về ở lần gửi trước.</param>
public record PartnerStatusViewModel(OwnerRegistrationView Registration, bool Editable, IReadOnlyList<string> Missing);
