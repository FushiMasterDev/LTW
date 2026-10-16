namespace HomestayBooking.Web.Services;

/// <summary>
/// Lưu ảnh bill ngoài wwwroot (App_Data/receipts) vì là dữ liệu nhạy cảm: chỉ mở được qua controller có kiểm tra quyền.
/// Kiểm tra định dạng bằng chữ ký đầu file, không tin phần mở rộng hay Content-Type trình duyệt gửi lên.
/// </summary>
public class ReceiptFileStore
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private readonly string _root;

    public ReceiptFileStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "receipts");
    }

    /// <summary>Lưu file, trả về tên file (lưu vào Payments.ReceiptImageUrl). Ném BookingException nếu file không hợp lệ.</summary>
    public async Task<string> SaveAsync(IFormFile file, string bookingCode)
    {
        var extension = await ValidateImageAsync(file);

        Directory.CreateDirectory(_root);
        var fileName = $"{bookingCode}-{Guid.NewGuid():N}{extension}";
        await using (var output = File.Create(Path.Combine(_root, fileName)))
        {
            await file.CopyToAsync(output);
        }
        return fileName;
    }

    /// <summary>Kiểm dung lượng/chữ ký dùng chung cho biên lai, giấy tờ riêng tư và ảnh công khai.</summary>
    public static async Task<string> ValidateImageAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            throw new BookingException("Ảnh phải có dung lượng từ 1 byte đến 5MB.");
        }

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            _ = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        }
        return DetectExtension(header) ?? throw new BookingException("Chỉ nhận ảnh JPG, PNG hoặc WEBP.");

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
        _ => "image/jpeg",
    };

    public static string? DetectExtension(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
        {
            return ".jpg";
        }
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }
        return null;
    }
}
