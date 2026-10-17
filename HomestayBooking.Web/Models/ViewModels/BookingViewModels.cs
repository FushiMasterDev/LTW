using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Validation;

namespace HomestayBooking.Web.Models.ViewModels;

public class BookingCreateViewModel
{
    public int RoomTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [Display(Name = "Ngày nhận phòng")]
    public DateOnly? CheckIn { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [Display(Name = "Ngày trả phòng")]
    public DateOnly? CheckOut { get; set; }

    [Range(1, 20, ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Số khách")]
    public int Guests { get; set; } = 2;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Họ tên người nhận phòng")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Số điện thoại liên hệ")]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Yêu cầu đặc biệt")]
    public string? SpecialRequests { get; set; }

    [Display(Name = "Voucher")]
    public int? VoucherId { get; set; }

    // Hiển thị
    public IReadOnlyList<VoucherOption> Vouchers { get; set; } = [];
    public string HomestayName { get; set; } = string.Empty;
    public int HomestayId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public PriceQuote? Quote { get; set; }
}

public class BookingLookupViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(20)]
    [Display(Name = "Mã đơn")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Số điện thoại đặt phòng")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class BookingDetailsViewModel
{
    public CustomerChangeRequests? ChangeRequests { get; init; }
    public Booking Booking { get; init; } = null!;
    public string? QrImageUrl { get; init; }
    public BankAccountOptions Bank { get; init; } = new();
    public RefundQuote? CancelQuote { get; init; }
    public bool CanCancel { get; init; }
    public bool CanUploadReceipt { get; init; }

    /// <summary>Địa chỉ, SĐT, hướng dẫn nhận phòng chỉ hiện khi đơn đã được xác nhận (BR-13).</summary>
    public bool ShowAddress { get; init; }

    public Payment? PendingReceipt { get; init; }
    public Payment? LastRejectedReceipt { get; init; }
    public bool IsOwnAccount { get; init; }
    public bool ShowDevSimulator { get; init; }

    /// <summary>Đơn đã có đánh giá (mỗi đơn tối đa một, BR-09): ẩn form viết đánh giá.</summary>
    public bool HasReview { get; init; }
}

public record MyBookingRow(string Code, string HomestayName, string RoomTypeName, DateOnly CheckIn, DateOnly CheckOut,
    decimal TotalAmount, BookingStatus Status, DateTime CreatedAt);

public class SimulateBankViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [Display(Name = "Nội dung chuyển khoản")]
    public string Content { get; set; } = string.Empty;

    [Range(1000, 1_000_000_000, ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Số tiền")]
    public decimal Amount { get; set; }

    [Display(Name = "Tài khoản nhận")]
    public string AccountNumber { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [VietnamesePhone(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "{0} không hợp lệ.")]
    [Display(Name = "Email (không bắt buộc)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} phải có từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public record VoucherOption(int Id, string Code, decimal Balance, DateOnly ExpiresOn);

public class VoucherIssueViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(20)]
    [Display(Name = "Mã đơn bị ảnh hưởng")]
    public string BookingCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(500, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Lý do cấp")]
    public string Reason { get; set; } = string.Empty;
}

public record VoucherRow(int Id, string Code, string CustomerName, string CustomerPhone, decimal Amount, decimal Balance,
    DateOnly ExpiresOn, VoucherStatus Status, string Reason, string? SourceBookingCode, string IssuedBy, DateTime IssuedAt, int UsedCount);
