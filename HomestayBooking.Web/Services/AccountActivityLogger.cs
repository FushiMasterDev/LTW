using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>Ghi ACCOUNT_ACTIVITY_LOG (append-only, v5 mục 4.10). Không truyền mật khẩu/OTP vào Detail.</summary>
public class AccountActivityLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountActivityLogger(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Thêm bản ghi vào DbContext; lưu cùng SaveChanges của thao tác nghiệp vụ để không có log "mồ côi".</summary>
    public void Add(string? accountId, AccountEventType eventType, ActorType actorType, string? actorId, string? detail = null)
    {
        _db.AccountActivityLogs.Add(new AccountActivityLog
        {
            AccountId = accountId,
            EventType = eventType,
            ActorType = actorType,
            ActorId = actorId,
            Detail = detail,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        });
    }

    public async Task LogAsync(string? accountId, AccountEventType eventType, ActorType actorType, string? actorId, string? detail = null)
    {
        Add(accountId, eventType, actorType, actorId, detail);
        await _db.SaveChangesAsync();
    }
}
