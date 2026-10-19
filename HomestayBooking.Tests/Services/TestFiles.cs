using Microsoft.AspNetCore.Http;

namespace HomestayBooking.Tests.Services;

/// <summary>File thật nhỏ nhất cho test upload: ReceiptFileStore kiểm cả chữ ký lẫn cấu trúc, nên không dùng vài byte đầu giả.</summary>
public static class TestFiles
{
    /// <summary>PNG 1×1 hợp lệ (IHDR, IDAT, IEND đúng CRC).</summary>
    public static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    /// <summary>JPEG 1×1 hợp lệ (SOI, SOF0, SOS, EOI).</summary>
    public static byte[] Jpeg => Convert.FromBase64String("/9j/4AAQSkZJRgABAQAASABIAAD/4QBMRXhpZgAATU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAA6ABAAMAAAABAAEAAKACAAQAAAABAAAAAaADAAQAAAABAAAAAQAAAAD/7QA4UGhvdG9zaG9wIDMuMAA4QklNBAQAAAAAAAA4QklNBCUAAAAAABDUHYzZjwCyBOmACZjs+EJ+/8AAEQgAAQABAwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/bAEMAAgICAgICAwICAwUDAwMFBgUFBQUGCAYGBgYGCAoICAgICAgKCgoKCgoKCgwMDAwMDA4ODg4ODw8PDw8PDw8PD//bAEMBAgICBAQEBwQEBxALCQsQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEP/dAAQAAf/aAAwDAQACEQMRAD8A7iiiiv8AQA+XP//Z");

    /// <summary>WEBP lossless 1×1 hợp lệ (RIFF khớp độ dài, khối VP8L).</summary>
    public static byte[] Webp => Convert.FromBase64String("UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==");

    /// <summary>PDF tối thiểu có đối tượng và %%EOF.</summary>
    public static byte[] Pdf => "%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF\n"u8.ToArray();

    public static IFormFile Form(byte[] bytes, string fileName, string? contentType = null, string name = "file") =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, name, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType ?? Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png", ".webp" => "image/webp", ".pdf" => "application/pdf", ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream",
            },
        };
}
