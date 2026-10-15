using System.Globalization;
using HomestayBooking.Web.Services;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class BookingOptionsTests
{
    [Fact]
    public void DepositRate_RejectsInvalidRatesAndAcceptsValidRates()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "vi-VN", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var validator = new DataAnnotationValidateOptions<BookingOptions>(Options.DefaultName);

                Assert.True(validator.Validate(Options.DefaultName, new BookingOptions { DepositRate = 0m }).Failed);
                Assert.True(validator.Validate(Options.DefaultName, new BookingOptions { DepositRate = 1.5m }).Failed);
                Assert.True(validator.Validate(Options.DefaultName, new BookingOptions { DepositRate = 0.30m }).Succeeded);
                Assert.True(validator.Validate(Options.DefaultName, new BookingOptions { DepositRate = 1m }).Succeeded);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
