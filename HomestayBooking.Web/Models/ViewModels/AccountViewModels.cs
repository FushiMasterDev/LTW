using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Validation;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Cổng Customer/Owner: số điện thoại + mật khẩu (v5 mục 8.1).</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>Cổng nội bộ: mã nhân viên + mật khẩu (v5 mục 8.2).</summary>
public class EmployeeLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(20, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Mã nhân viên")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} phải có từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
