using Microsoft.AspNetCore.Identity;

namespace HomestayBooking.Web.Identity;

/// <summary>Lỗi mật khẩu bằng tiếng Việt (là khóa trong SharedResource để dịch sang tiếng Anh).</summary>
public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = "Mật khẩu phải có ít nhất 8 ký tự." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải có ít nhất một chữ số." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Mật khẩu phải có ít nhất một chữ thường." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Mật khẩu phải có ít nhất một chữ hoa." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Mật khẩu phải có ít nhất một ký tự đặc biệt (ví dụ @, #, !)." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Mật khẩu hiện tại không đúng." };
}
