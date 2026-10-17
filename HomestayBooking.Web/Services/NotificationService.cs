using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Thông báo trong hệ thống (chuông). Add chỉ thêm vào DbContext; lưu cùng SaveChanges của thao tác nghiệp vụ.
/// Các hàm đọc/đánh dấu luôn lọc theo userId của người đang đăng nhập nên không đọc được thông báo của người khác.
/// </summary>
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

    public Task<PaginatedList<Notification>> ListAsync(string userId, int? page, int pageSize = 20) =>
        PaginatedList<Notification>.CreateAsync(Mine(userId), page, pageSize);

    public Task<List<Notification>> LatestAsync(string userId, int take = 5) =>
        Mine(userId).Take(take).ToListAsync();

    public Task<int> UnreadCountAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    /// <summary>Đánh dấu một thông báo đã đọc; false nếu không tồn tại hoặc không thuộc userId.</summary>
    public async Task<bool> MarkReadAsync(int id, string userId) =>
        await _db.Notifications.Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)) > 0;

    public Task<int> MarkAllReadAsync(string userId) =>
        _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    private IQueryable<Notification> Mine(string userId) =>
        _db.Notifications.AsNoTracking().Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id);
}
