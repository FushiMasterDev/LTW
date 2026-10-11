using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Models.ViewModels;

// Form thêm/sửa danh mục của Admin. Id = 0 nghĩa là thêm mới.

public class AreaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên khu vực")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tỉnh/thành")]
    public string Province { get; set; } = "Hà Nội";

    [Display(Name = "Nhóm khu vực")]
    public AreaGroup AreaGroup { get; set; } = AreaGroup.InnerCity;
}

public class RoomCategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên loại phòng")]
    public string Name { get; set; } = string.Empty;
}

public class AmenityFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên tiện nghi")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [RegularExpression(@"^bi-[a-z0-9-]+$", ErrorMessage = "{0} phải là class Bootstrap Icons, ví dụ bi-wifi.")]
    [Display(Name = "Biểu tượng")]
    public string? Icon { get; set; }
}

public class HolidayFormViewModel
{
    /// <summary>True khi thêm mới; khi sửa, ngày là khóa chính nên không đổi được.</summary>
    public bool IsNew { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên ngày lễ")]
    public string Name { get; set; } = string.Empty;
}
