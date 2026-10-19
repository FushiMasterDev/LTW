using System.Buffers.Binary;
using System.Text;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Lưu ảnh bill ngoài wwwroot (App_Data/receipts) vì là dữ liệu nhạy cảm: chỉ mở được qua controller có kiểm tra quyền.
/// Kiểm tra định dạng bằng nội dung file (chữ ký + cấu trúc), không tin phần mở rộng hay Content-Type trình duyệt gửi lên;
/// phần mở rộng và Content-Type khai báo phải khớp với nội dung, nếu không thì từ chối.
/// </summary>
public class ReceiptFileStore
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private readonly string _root;

    public ReceiptFileStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "receipts");
    }

    /// <summary>
    /// Lưu file, trả về tên file (lưu vào Payments.ReceiptImageUrl / VerificationDocuments.FileUrl). Ném BookingException nếu
    /// file không hợp lệ. <paramref name="allowPdf"/> chỉ bật cho giấy tờ đối tác; bill và ảnh công khai chỉ nhận ảnh.
    /// Ghi đúng phần dữ liệu đã kiểm tra, không đọc lại từ request.
    /// </summary>
    public async Task<string> SaveAsync(IFormFile file, string bookingCode, bool allowPdf = false)
    {
        var (extension, content) = await ReadValidatedAsync(file, allowPdf);

        Directory.CreateDirectory(_root);
        var fileName = $"{bookingCode}-{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_root, fileName), content);
        return fileName;
    }

    /// <summary>Kiểm dung lượng, tên, phần mở rộng, Content-Type và cấu trúc ảnh; trả về phần mở rộng chuẩn (.jpg/.png/.webp).</summary>
    public static async Task<string> ValidateImageAsync(IFormFile file) => (await ReadValidatedAsync(file, allowPdf: false)).Extension;

    private static async Task<(string Extension, byte[] Content)> ReadValidatedAsync(IFormFile file, bool allowPdf)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            throw new BookingException(allowPdf ? "File phải có dung lượng từ 1 byte đến 5MB." : "Ảnh phải có dung lượng từ 1 byte đến 5MB.");
        }
        if (!IsSafeFileName(file.FileName))
        {
            throw new BookingException("Tên file không hợp lệ.");
        }

        var content = new byte[file.Length];
        await using (var stream = file.OpenReadStream())
        {
            await stream.ReadExactlyAsync(content);
        }

        var extension = DetectExtension(content) ?? (allowPdf && IsPdf(content) ? ".pdf" : null)
            ?? throw new BookingException(allowPdf ? "Chỉ nhận ảnh JPG, PNG, WEBP hoặc PDF." : "Chỉ nhận ảnh JPG, PNG hoặc WEBP.");

        var declared = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ExtensionsFor(extension).Contains(declared))
        {
            throw new BookingException("Đuôi file không khớp với nội dung file.");
        }
        var mime = file.ContentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(mime) && mime != "application/octet-stream" && !MimeTypesFor(extension).Contains(mime))
        {
            throw new BookingException("Loại file khai báo không khớp với nội dung file.");
        }

        var complete = extension switch
        {
            ".png" => IsCompletePng(content),
            ".jpg" => IsCompleteJpeg(content),
            ".webp" => IsCompleteWebp(content),
            _ => IsCompletePdf(content),
        };
        if (!complete)
        {
            throw new BookingException(extension == ".pdf" ? "File PDF bị hỏng hoặc không đầy đủ." : "File ảnh bị hỏng hoặc không đầy đủ.");
        }
        return (extension, content);
    }

    /// <summary>Đường dẫn thật của file đã lưu; null nếu tên không hợp lệ hoặc file không tồn tại.</summary>
    public string? Resolve(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
        {
            return null;
        }
        var path = Path.Combine(_root, fileName);
        return File.Exists(path) ? path : null;
    }

    public static string ContentTypeOf(string fileName) => Path.GetExtension(fileName) switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "image/jpeg",
    };

    public static string? DetectExtension(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
        {
            return ".jpg";
        }
        if (h.Length >= 8 && h[..8].SequenceEqual(PngSignature))
        {
            return ".png";
        }
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }
        return null;
    }

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static bool IsPdf(ReadOnlySpan<byte> h) => h.StartsWith("%PDF-"u8);

    private static string[] ExtensionsFor(string extension) => extension switch
    {
        ".jpg" => [".jpg", ".jpeg"],
        _ => [extension],
    };

    private static string[] MimeTypesFor(string extension) => extension switch
    {
        ".jpg" => ["image/jpeg", "image/jpg", "image/pjpeg"],
        ".png" => ["image/png"],
        ".webp" => ["image/webp"],
        _ => ["application/pdf"],
    };

    /// <summary>Chỉ tên file trần: không thư mục, không "..", không ký tự điều khiển, dài tối đa 255.</summary>
    private static bool IsSafeFileName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 255 && name == Path.GetFileName(name)
        && !name.Contains('/') && !name.Contains('\\') && !name.Contains("..") && !name.Any(char.IsControl);

    /// <summary>PNG đủ khối: IHDR đầu tiên với kích thước &gt; 0, mọi khối đúng CRC và nằm trọn trong file, kết thúc bằng IEND.</summary>
    internal static bool IsCompletePng(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8 + 25 + 12 || !data[..8].SequenceEqual(PngSignature)) return false;
        var offset = 8;
        var first = true;
        while (offset + 12 <= data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            if (length > (uint)(data.Length - offset - 12)) return false;
            var type = data.Slice(offset + 4, 4);
            var body = data.Slice(offset + 8, (int)length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(data[(offset + 8 + (int)length)..]);
            if (Crc32(data.Slice(offset + 4, 4 + (int)length)) != crc) return false;
            if (first)
            {
                if (!type.SequenceEqual("IHDR"u8) || length != 13) return false;
                if (BinaryPrimitives.ReadUInt32BigEndian(body) == 0 || BinaryPrimitives.ReadUInt32BigEndian(body[4..]) == 0) return false;
                first = false;
            }
            if (type.SequenceEqual("IEND"u8)) return length == 0;
            offset += 12 + (int)length;
        }
        return false;
    }

    /// <summary>JPEG: các đoạn tiêu đề nằm trọn trong file, có khung ảnh (SOF) với kích thước &gt; 0 trước dữ liệu (SOS), và có dấu kết thúc (EOI).</summary>
    internal static bool IsCompleteJpeg(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return false;
        var offset = 2;
        var sawFrame = false;
        while (offset + 4 <= data.Length)
        {
            if (data[offset] != 0xFF) return false;
            var marker = data[offset + 1];
            if (marker == 0xFF) { offset++; continue; }
            if (marker == 0xD9) return false; // kết thúc trước khi có dữ liệu ảnh
            if (marker is >= 0xD0 and <= 0xD7 or 0x01) { offset += 2; continue; }
            int length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
            if (length < 2 || offset + 2 + length > data.Length) return false;
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                if (length < 8) return false;
                var height = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 7)..]);
                if (height == 0 || width == 0) return false;
                sawFrame = true;
            }
            if (marker == 0xDA)
            {
                var scan = data[(offset + 2 + length)..];
                return sawFrame && scan.Length > 2 && scan.LastIndexOf((ReadOnlySpan<byte>)[0xFF, 0xD9]) >= 0;
            }
            offset += 2 + length;
        }
        return false;
    }

    /// <summary>WEBP: kích thước RIFF khớp độ dài file, khối đầu là VP8/VP8L/VP8X hợp lệ và nằm trọn trong file.</summary>
    internal static bool IsCompleteWebp(ReadOnlySpan<byte> data)
    {
        if (data.Length < 21 || !data[..4].SequenceEqual("RIFF"u8) || !data[8..12].SequenceEqual("WEBP"u8)) return false;
        var riffSize = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (riffSize + 8L != data.Length && riffSize + 9L != data.Length) return false;
        var chunk = data[12..16];
        var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        if (chunkSize == 0 || 20L + chunkSize > data.Length) return false;
        if (chunk.SequenceEqual("VP8L"u8)) return data[20] == 0x2F;
        if (chunk.SequenceEqual("VP8 "u8)) return chunkSize >= 10 && data.Slice(23, 3).SequenceEqual((ReadOnlySpan<byte>)[0x9D, 0x01, 0x2A]);
        if (chunk.SequenceEqual("VP8X"u8)) return chunkSize == 10;
        return false;
    }

    /// <summary>PDF: bắt đầu bằng %PDF-, có ít nhất một đối tượng và có dấu kết thúc %%EOF ở 1KB cuối file.</summary>
    internal static bool IsCompletePdf(ReadOnlySpan<byte> data)
    {
        if (!IsPdf(data)) return false;
        var tail = data[Math.Max(0, data.Length - 1024)..];
        return tail.IndexOf("%%EOF"u8) >= 0 && data.IndexOf(" obj"u8) >= 0;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
