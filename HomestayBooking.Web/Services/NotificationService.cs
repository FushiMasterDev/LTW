using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>Thông báo trong hệ thống (chuông). Chỉ thêm vào DbContext; lưu cùng SaveChanges của thao tác nghiệp vụ.</summary>
public class NotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public void Add(IEnumerable<string?> accountIds, string message, string? url = null)
    {
        foreach (var accountId in accountIds.OfType<string>().Distinct())
        {
            _db.Notifications.Add(new Notification { UserId = accountId, Message = message, Url = url });
        }
    }
}
