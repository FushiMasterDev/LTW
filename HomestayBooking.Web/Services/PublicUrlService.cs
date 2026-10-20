using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>URL bên ngoài luôn dùng cùng nguồn với lời nhắc, không phụ thuộc Host do client gửi.</summary>
public sealed class PublicUrlService(IOptions<ReminderOptions> options)
{
    public string Absolute(string path) => options.Value.PublicBaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
}
