using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Form mẫu của trang Dev/UiKit: đủ kiểu ô nhập để xem form-field ở trạng thái thường và lỗi.</summary>
public class UiKitViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn {0}.")]
    [Display(Name = "Khu vực")]
    public int? AreaId { get; set; }

    [Range(1, 20, ErrorMessage = "{0} từ {1} đến {2}.")]
    [Display(Name = "Số khách")]
    public int Guests { get; set; } = 2;

    [Display(Name = "Ngày nhận phòng")]
    [DataType(DataType.Date)]
    public DateOnly? CheckIn { get; set; }

    [StringLength(500)]
    [Display(Name = "Yêu cầu đặc biệt")]
    public string? Note { get; set; }

    [Display(Name = "Nhận thông báo qua Zalo")]
    public bool Subscribe { get; set; }

    public IReadOnlyList<SelectListItem> Areas { get; } =
    [
        new("Tây Hồ", "1"), new("Hoàn Kiếm", "2"), new("Ba Vì", "3"),
    ];
}
