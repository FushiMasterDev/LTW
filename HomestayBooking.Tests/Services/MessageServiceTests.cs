using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomestayBooking.Tests.Services;

public sealed class MessageServiceTests
{
    [Theory]
    [InlineData("success", 1, true)] [InlineData("fallback", 2, true)] [InlineData("failure", 2, false)]
    public async Task ChannelFallbackPersistsMetadataOnlyAndPendingBeforeSend(string scenario, int count, bool sent)
    {
        using var h = await GuestServicesTestHost.CreateAsync(); const string body = "Nội dung riêng tư không được lưu";
        h.Sender.Fails = c => scenario == "failure" || scenario == "fallback" && c == MessageChannel.Zalo;
        h.Sender.BeforeSend = async channel => Assert.Equal(MessageStatus.Pending,
            (await h.Db.MessageLogs.AsNoTracking().OrderByDescending(m => m.Id).FirstAsync()).Status);
        var result = await h.Messages().SendAsync("0987 000 001", "Test", null, body);
        Assert.Equal(sent, result.Succeeded); Assert.Equal(count, result.Attempts); Assert.Equal(count, h.Sender.Calls.Count);
        var logs = await h.Db.MessageLogs.OrderBy(m => m.Id).ToListAsync(); Assert.Equal(count, logs.Count);
        Assert.Equal(MessageChannel.Zalo, logs[0].Channel);
        if (count == 2) Assert.Equal(MessageChannel.Sms, logs[1].Channel);
        Assert.All(logs, log =>
        {
            Assert.Equal("+84987000001", log.RecipientPhone);
            if (log.Status == MessageStatus.Sent) { Assert.NotNull(log.SentAt); Assert.Null(log.ErrorMessage); }
            else { Assert.Equal(MessageStatus.Failed, log.Status); Assert.Null(log.SentAt); Assert.False(string.IsNullOrWhiteSpace(log.ErrorMessage)); Assert.True(log.ErrorMessage!.Length <= 500); }
            Assert.DoesNotContain(body, log.ErrorMessage ?? "");
        });
        Assert.DoesNotContain(body, string.Join(" ", await h.Db.AuditLogs.Select(a => a.NewValues).ToListAsync()));
        Assert.DoesNotContain(typeof(MessageLog).GetProperties(), p => p.Name is "Content" or "Body" or "Message");
        Assert.Empty(await h.Db.AuditLogs.Where(a => a.TableName == "MessageLogs").ToListAsync());
    }

    [Fact]
    public async Task SendingFailureDoesNotAbortBusinessTransaction()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); h.Sender.Fails = _ => true;
        await using (var transaction = await h.Db.Database.BeginTransactionAsync())
        {
            (await h.Db.Customers.SingleAsync()).Note = "Nghiệp vụ vẫn lưu";
            await h.Db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.False((await h.Messages().SendAsync("0987000001", "Test", null, "Nội dung")).Succeeded);
        h.Db.ChangeTracker.Clear(); Assert.Equal("Nghiệp vụ vẫn lưu", (await h.Db.Customers.SingleAsync()).Note);
        Assert.Equal(2, await h.Db.MessageLogs.CountAsync(m => m.Status == MessageStatus.Failed));
    }

    [Theory]
    [InlineData("phone")] [InlineData("template")] [InlineData("long-template")] [InlineData("message")]
    public async Task InvalidInputCreatesNoLogAndSendsNothing(string bad)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Messages().SendAsync(bad == "phone" ? "bad" : "0987000001",
            bad == "template" ? " " : bad == "long-template" ? new string('X', 51) : "Test", null, bad == "message" ? " " : "Nội dung"));
        Assert.Empty(h.Db.MessageLogs); Assert.Empty(h.Sender.Calls);
    }

    [Fact]
    public async Task CallerMustCommitBusinessTransactionBeforeSending()
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        await using var tx = await h.Db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<BookingException>(() => h.Messages().SendAsync("0987000001", "Test", null, "Nội dung"));
        Assert.Empty(h.Db.MessageLogs); Assert.Empty(h.Sender.Calls);
    }

    [Fact]
    public async Task DefaultSenderOnlyLogsSimulationWithoutMessageOrRecipient()
    {
        var logger = new CaptureLogger<LoggingMessageSender>();
        await new LoggingMessageSender(logger).SendAsync(MessageChannel.Zalo, "+84987000001", "Nội dung bí mật");
        var text = Assert.Single(logger.Lines); Assert.Contains("Giả lập", text); Assert.DoesNotContain("Nội dung bí mật", text); Assert.DoesNotContain("+84987000001", text);
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
    }
}
