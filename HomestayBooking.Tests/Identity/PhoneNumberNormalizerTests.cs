using HomestayBooking.Web.Identity;

namespace HomestayBooking.Tests.Identity;

public class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("0912345678", "+84912345678")]
    [InlineData("0912 345 678", "+84912345678")]
    [InlineData("0912.345.678", "+84912345678")]
    [InlineData("+84 912-345-678", "+84912345678")]
    [InlineData("84912345678", "+84912345678")]
    [InlineData("(024) 3826 1234", "+842438261234")] // số cố định Hà Nội
    public void ToE164_ValidVietnameseNumbers(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.ToE164(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("091234567")]      // thiếu số
    [InlineData("091234567890")]   // thừa số
    [InlineData("+1 415 555 0100")] // không phải số Việt Nam
    [InlineData("00912345678")]
    [InlineData("09123abc78")]
    public void ToE164_InvalidNumbers_ReturnNull(string? input)
    {
        Assert.Null(PhoneNumberNormalizer.ToE164(input));
    }

    [Fact]
    public void ToNational_ConvertsBack()
    {
        Assert.Equal("0912345678", PhoneNumberNormalizer.ToNational("+84912345678"));
    }
}
