using System.Text.RegularExpressions;

namespace HomestayBooking.Web.Identity;

/// <summary>
/// Chuẩn hóa số điện thoại Việt Nam về E.164 trước khi lưu hoặc tra cứu (v5 mục 4.1):
/// "0912 345 678", "84912345678", "+84 912-345-678" đều thành "+84912345678".
/// </summary>
public static partial class PhoneNumberNormalizer
{
    /// <summary>Trả null khi không phải số Việt Nam hợp lệ (9–10 chữ số sau mã quốc gia).</summary>
    public static string? ToE164(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = Separators().Replace(input.Trim(), string.Empty);
        if (digits.StartsWith("+84"))
        {
            digits = digits[3..];
        }
        else if (digits.StartsWith("84") && digits.Length >= 11)
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith('0'))
        {
            digits = digits[1..];
        }
        else
        {
            return null;
        }

        return NationalNumber().IsMatch(digits) ? "+84" + digits : null;
    }

    /// <summary>Hiển thị dạng trong nước: +84912345678 → 0912345678.</summary>
    public static string ToNational(string e164) =>
        e164.StartsWith("+84") ? "0" + e164[3..] : e164;

    [GeneratedRegex(@"[\s.\-()]")]
    private static partial Regex Separators();

    // Không bắt đầu bằng 0 sau khi bỏ tiền tố; di động 9 số, cố định 9–10 số
    [GeneratedRegex(@"^[1-9]\d{8,9}$")]
    private static partial Regex NationalNumber();
}
