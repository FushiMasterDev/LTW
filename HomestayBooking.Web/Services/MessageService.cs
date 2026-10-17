using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public interface IMessageSender
{
    Task SendAsync(MessageChannel channel, string recipientPhone, string message, CancellationToken cancellationToken = default);
}

public class LoggingMessageSender(ILogger<LoggingMessageSender> logger) : IMessageSender
{
    public Task SendAsync(MessageChannel channel, string recipientPhone, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // shortcut: chưa nối Zalo/SMS thật, thay khi Fuji duyệt dự toán (NFR-12)
        logger.LogInformation("Giả lập gửi tin qua {Channel}; chưa nối dịch vụ ngoài. Không ghi nội dung tin.", channel);
        return Task.CompletedTask;
    }
}

public record MessageSendResult(bool Succeeded, long LastLogId, int Attempts);

/// <summary>Mỗi lần thử một dòng, sender luôn chạy ngoài transaction; không audit lặp nhật ký gửi tin.</summary>
public class MessageService(ApplicationDbContext db, IMessageSender sender, TimeProvider clock, ILogger<MessageService> logger)
{
    internal const string TimeoutError = "Quá thời gian chờ kết quả gửi";

    public async Task<MessageSendResult> SendAsync(string phoneNumber, string templateCode, int? bookingId, string message,
        CancellationToken cancellationToken = default)
    {
        EnsureOutsideTransaction();
        if (string.IsNullOrWhiteSpace(message)) throw new BookingException("Nội dung tin không hợp lệ.");
        var pending = await HomestayWrite.RunAsync(db, async () =>
        {
            // Caller nghiệp vụ phải commit trước khi gửi; dùng dòng đơn để tuần tự hóa kiểm tra/tạo Pending.
            if (bookingId is int id && db.Database.IsRelational()) await db.Bookings.Where(b => b.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, b => b.Status), cancellationToken);
            return await ReserveAsync(phoneNumber, templateCode, bookingId, DateTime.MinValue, int.MaxValue, cancellationToken);
        });
        return pending is null ? new(false, 0, 0) : await DeliverAsync(pending, message, cancellationToken: cancellationToken);
    }

    // Gọi trong transaction ngắn sau khi caller đã khóa dòng đơn/chủ; không gọi dịch vụ ngoài ở đây.
    internal async Task<MessageLog?> ReserveAsync(string phoneNumber, string templateCode, int? bookingId, DateTime since,
        int maxAttempts, CancellationToken cancellationToken)
    {
        var phone = PhoneNumberNormalizer.ToE164(phoneNumber) ?? throw new BookingException("Số điện thoại nhận tin không hợp lệ.");
        if (string.IsNullOrWhiteSpace(templateCode) || templateCode.Length > 50) throw new BookingException("Mã mẫu tin không hợp lệ.");
        var attempts = db.MessageLogs.Where(m => m.TemplateCode == templateCode && m.BookingId == bookingId
            && (bookingId != null || m.RecipientPhone == phone) && m.CreatedAt >= since);
        var cutoff = clock.GetUtcNow().UtcDateTime.AddMinutes(-10);
        if (db.Database.IsRelational()) await attempts.Where(m => m.Status == MessageStatus.Pending && m.CreatedAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MessageStatus.Failed).SetProperty(m => m.ErrorMessage, TimeoutError), cancellationToken);
        else
        {
            foreach (var stale in await attempts.Where(m => m.Status == MessageStatus.Pending && m.CreatedAt < cutoff).ToListAsync(cancellationToken))
            { stale.Status = MessageStatus.Failed; stale.ErrorMessage = TimeoutError; }
            await db.SaveChangesAsync(cancellationToken);
        }
        if (await attempts.AnyAsync(m => m.Status == MessageStatus.Sent || m.Status == MessageStatus.Pending, cancellationToken)
            || await attempts.CountAsync(m => m.Channel == MessageChannel.Zalo, cancellationToken) >= maxAttempts) return null;
        return await AddPendingAsync(phone, templateCode, bookingId, MessageChannel.Zalo, cancellationToken);
    }

    private async Task<MessageLog> AddPendingAsync(string phone, string template, int? bookingId, MessageChannel channel, CancellationToken cancellationToken)
    {
        var log = new MessageLog { Channel = channel, RecipientPhone = phone, TemplateCode = template,
            BookingId = bookingId, Status = MessageStatus.Pending, CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.MessageLogs.Add(log); await db.SaveChangesAsync(cancellationToken);
        db.Entry(log).State = EntityState.Detached;
        return log;
    }

    internal async Task<MessageSendResult> DeliverAsync(MessageLog pending, string message, Func<Task>? onSent = null,
        Func<bool>? canSend = null, CancellationToken cancellationToken = default, Func<Task>? lockForCompletion = null)
    {
        EnsureOutsideTransaction();
        if (string.IsNullOrWhiteSpace(message)) throw new BookingException("Nội dung tin không hợp lệ.");
        var attempts = 0;
        while (true)
        {
            if (canSend is not null && !canSend()) return new(false, pending.Id, attempts);
            // Đọc bản mới để không dùng trạng thái tracking cũ sau khi một lượt job đã xử lý timeout.
            var current = await db.MessageLogs.AsNoTracking().SingleAsync(m => m.Id == pending.Id, cancellationToken);
            if (current.Status != MessageStatus.Pending) return new(false, pending.Id, attempts);
            EnsureOutsideTransaction();
            string? error = null;
            try
            {
                attempts++;
                await sender.SendAsync(current.Channel, current.RecipientPhone!, message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // Không lưu ex.Message: nhà cung cấp có thể chèn nguyên nội dung/SĐT vào lỗi.
                error = $"Không gửi được qua {current.Channel} ({ex.GetType().Name}).";
                if (error.Length > 500) error = error[..500];
                logger.LogWarning("Gửi tin thất bại qua {Channel}; loại lỗi {ErrorType}.", current.Channel, ex.GetType().Name);
            }
            var next = await HomestayWrite.RunAsync(db, async () =>
            {
                // Cùng thứ tự khóa với bước giữ Pending: đơn/chủ trước, MessageLog sau, tránh khóa chéo.
                if (current.BookingId is int id && db.Database.IsRelational()) await db.Bookings.Where(b => b.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, b => b.Status), cancellationToken);
                if (lockForCompletion is not null) await lockForCompletion();
                var completed = false;
                if (db.Database.IsRelational()) completed = await db.MessageLogs.Where(m => m.Id == current.Id && m.Status == MessageStatus.Pending)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, error == null ? MessageStatus.Sent : MessageStatus.Failed)
                        .SetProperty(m => m.SentAt, error == null ? clock.GetUtcNow().UtcDateTime : (DateTime?)null)
                        .SetProperty(m => m.ErrorMessage, error), cancellationToken) == 1;
                else
                {
                    var log = await db.MessageLogs.SingleAsync(m => m.Id == current.Id, cancellationToken);
                    if (log.Status == MessageStatus.Pending)
                    { log.Status = error == null ? MessageStatus.Sent : MessageStatus.Failed; log.SentAt = error == null ? clock.GetUtcNow().UtcDateTime : null; log.ErrorMessage = error; completed = true; }
                }
                if (!completed) return (Saved: false, Next: (MessageLog?)null);
                if (error is null && onSent is not null) await onSent();
                // Giữ liên tục dấu Pending trong DB khi chuyển Zalo → SMS; không có khoảng trống cho lượt khác.
                var sms = error is not null && current.Channel == MessageChannel.Zalo && (canSend is null || canSend())
                    ? await AddPendingAsync(current.RecipientPhone!, current.TemplateCode, current.BookingId, MessageChannel.Sms, cancellationToken) : null;
                return (Saved: true, Next: sms);
            });
            if (next.Saved && error is null) return new(true, current.Id, attempts);
            if (next.Next is null) return new(false, current.Id, attempts);
            pending = next.Next;
        }
    }

    private void EnsureOutsideTransaction()
    {
        if (db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new BookingException("Phải kết thúc transaction nghiệp vụ trước khi gửi tin.");
    }
}
