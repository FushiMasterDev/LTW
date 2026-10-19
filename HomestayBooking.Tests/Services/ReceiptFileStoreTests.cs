using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Services;

public class ReceiptFileStoreTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }, ".png")]
    [InlineData(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, ".webp")]
    [InlineData(new byte[] { (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0, 0, 0, 0, 0, 0, 0, 0 }, null)] // SVG có thể chứa script
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', 0, 0, 0, 0, 0, 0, 0, 0 }, null)]
    public void DetectExtension_TrustsFileSignatureOnly(byte[] header, string? expected)
    {
        Assert.Equal(expected, ReceiptFileStore.DetectExtension(header));
    }

    [Fact]
    public void CurrentPrices_PicksLatestEffectivePriceUpToToday()
    {
        var prices = new[]
        {
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 500_000, EffectiveFrom = new(2026, 1, 1) },
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 550_000, EffectiveFrom = new(2026, 10, 1) },
            new RoomTypePrice { RoomTypeId = 1, DayType = DayType.Weekday, Price = 600_000, EffectiveFrom = new(2026, 12, 1) },
        };

        var current = PricingService.CurrentPrices(prices, new(2026, 10, 11));

        Assert.Equal(550_000, current[(1, DayType.Weekday)]);
    }
}

/// <summary>TC07 (QUY_TRINH_BTL 13.2) và lỗi QA trước ra mắt: upload phải kiểm cả đuôi, Content-Type, tên file và cấu trúc file.</summary>
public class UploadValidationTests
{
    public static TheoryData<string, string> ValidImages => new()
    {
        { "anh.png", ".png" }, { "anh.jpg", ".jpg" }, { "anh.jpeg", ".jpg" }, { "anh.webp", ".webp" }, { "ANH.PNG", ".png" },
    };

    private static byte[] BytesFor(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => TestFiles.Jpeg, ".webp" => TestFiles.Webp, ".pdf" => TestFiles.Pdf, _ => TestFiles.Png,
    };

    [Theory, MemberData(nameof(ValidImages))]
    public async Task RealImagesWithMatchingNameAndTypeAreAccepted(string name, string extension)
    {
        Assert.Equal(extension, await ReceiptFileStore.ValidateImageAsync(TestFiles.Form(BytesFor(name), name)));
    }

    [Fact]
    public async Task OctetStreamOrMissingContentTypeFallsBackToContentCheck()
    {
        Assert.Equal(".png", await ReceiptFileStore.ValidateImageAsync(TestFiles.Form(TestFiles.Png, "a.png", "application/octet-stream")));
        Assert.Equal(".png", await ReceiptFileStore.ValidateImageAsync(TestFiles.Form(TestFiles.Png, "a.png", "")));
    }

    [Fact]
    public async Task TruncatedOrCorruptedImagesAreRejected()
    {
        var png = TestFiles.Png;
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(png[..20], "a.png")));
        var badCrc = png.ToArray(); badCrc[30] ^= 0xFF;
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(badCrc, "a.png")));
        byte[] headerOnly = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(headerOnly, "a.png")));
        var jpeg = TestFiles.Jpeg;
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(jpeg[..(jpeg.Length / 2)], "a.jpg")));
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(TestFiles.Webp[..20], "a.webp")));
    }

    [Theory]
    [InlineData("anh.exe", null)]
    [InlineData("anh.jpg", null)]
    [InlineData("anh", null)]
    [InlineData("anh.png", "image/jpeg")]
    [InlineData("anh.png", "text/html")]
    [InlineData("../anh.png", null)]
    [InlineData("..\\anh.png", null)]
    [InlineData("thu/muc/anh.png", null)]
    public async Task MismatchedNameTypeOrPathIsRejected(string name, string? contentType)
    {
        await Assert.ThrowsAsync<BookingException>(() => ReceiptFileStore.ValidateImageAsync(TestFiles.Form(TestFiles.Png, name, contentType ?? "image/png")));
    }

    [Fact]
    public async Task PdfOnlyAllowedWhereEnabled()
    {
        using var root = new TempRoot();
        var store = root.Store();
        await Assert.ThrowsAsync<BookingException>(() => store.SaveAsync(TestFiles.Form(TestFiles.Pdf, "giay-phep.pdf"), "FJ1"));
        var saved = await store.SaveAsync(TestFiles.Form(TestFiles.Pdf, "giay-phep.pdf"), "HS1", allowPdf: true);
        Assert.EndsWith(".pdf", saved);
        Assert.Equal("application/pdf", ReceiptFileStore.ContentTypeOf(saved));
        Assert.Equal(TestFiles.Pdf, await File.ReadAllBytesAsync(store.Resolve(saved)!));
        await Assert.ThrowsAsync<BookingException>(() => store.SaveAsync(TestFiles.Form(TestFiles.Pdf[..20], "hong.pdf"), "HS1", allowPdf: true));
        await Assert.ThrowsAsync<BookingException>(() => store.SaveAsync(TestFiles.Form(TestFiles.Pdf, "giay-phep.png"), "HS1", allowPdf: true));
        await Assert.ThrowsAsync<BookingException>(() => store.SaveAsync(TestFiles.Form(TestFiles.Png, "anh.pdf"), "HS1", allowPdf: true));
    }

    private sealed class TempRoot : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "fuji-upload-" + Guid.NewGuid().ToString("N"));
        public ReceiptFileStore Store()
        {
            var env = new Moq.Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(_path);
            return new ReceiptFileStore(env.Object);
        }
        public void Dispose() { if (Directory.Exists(_path)) Directory.Delete(_path, recursive: true); }
    }
}
