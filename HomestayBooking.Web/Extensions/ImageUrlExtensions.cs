namespace HomestayBooking.Web.Extensions;

public static class ImageUrlExtensions
{
    private const string DemoFolder = "/images/demo/";

    /// <summary>
    /// Ảnh nhỏ cho thẻ danh sách. Ảnh mẫu trong /images/demo có sẵn bản "-sm.webp";
    /// ảnh khác (chủ homestay tải lên) giữ nguyên URL.
    /// </summary>
    public static string Thumbnail(this string url) =>
        url.StartsWith(DemoFolder, StringComparison.Ordinal)
        && url.EndsWith(".webp", StringComparison.Ordinal)
        && !url.EndsWith("-sm.webp", StringComparison.Ordinal)
            ? url[..^".webp".Length] + "-sm.webp"
            : url;
}
