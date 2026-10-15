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

public sealed class BookingChangeOptionsTests
{
    [Theory]
    [InlineData(nameof(BookingOptions.ChangeMinDaysBefore), 1, 90)]
    [InlineData(nameof(BookingOptions.MaxChangesPerBooking), 1, 10)]
    [InlineData(nameof(BookingOptions.ChangeWindowDays), 1, 365)]
    public void ChangeLimitsValidateInvariantBoundaries(string property, int min, int max)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "vi-VN", "en-US" })
            foreach (var value in new[] { min - 1, min, max, max + 1 })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var options = new BookingOptions();
                typeof(BookingOptions).GetProperty(property)!.SetValue(options, value);
                var result = new DataAnnotationValidateOptions<BookingOptions>(Options.DefaultName).Validate(Options.DefaultName, options);
                Assert.Equal(value >= min && value <= max, result.Succeeded);
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
