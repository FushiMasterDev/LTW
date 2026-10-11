using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Identity;

namespace HomestayBooking.Web.Validation;

/// <summary>Số điện thoại Việt Nam chuẩn hóa được về E.164. Để trống thì do [Required] xử lý.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class VietnamesePhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || (value is string s && (string.IsNullOrWhiteSpace(s) || PhoneNumberNormalizer.ToE164(s) is not null));
}
