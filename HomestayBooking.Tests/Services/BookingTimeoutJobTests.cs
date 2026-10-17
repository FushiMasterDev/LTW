using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class BookingTimeoutJobTests
{
    [Fact]
    public async Task FailingJobsHaveSeparateScopesAndDoNotPreventEitherReminder()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); await h.BookAsync();
        (await h.Db.Homestays.SingleAsync()).ContactPhone = "+84912345678"; await h.Db.SaveChangesAsync();
        var scopes = new Mock<IServiceScopeFactory>(); var created = 0; var disposed = 0; var reminderRequests = 0;
        scopes.Setup(f => f.CreateScope()).Returns(() =>
        {
            created++;
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns((Type type) =>
            {
                if (type == typeof(ReminderService)) { reminderRequests++; return h.Reminders(); }
                throw new InvalidOperationException("Tác vụ khác lỗi giả lập");
            });
            var scope = new Mock<IServiceScope>(); scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object); scope.Setup(s => s.Dispose()).Callback(() => disposed++); return scope.Object;
        });
        await new BookingTimeoutJob(scopes.Object, NullLogger<BookingTimeoutJob>.Instance).RunOnceAsync();
        Assert.Equal(9, created); Assert.Equal(created, disposed); Assert.Equal(2, reminderRequests);
        Assert.Equal(2, h.Sender.Calls.Count);
    }

    [Fact]
    public async Task CancellationStopsTheBatchWithoutCreatingScope()
    {
        var scopes = new Mock<IServiceScopeFactory>(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new BookingTimeoutJob(scopes.Object, NullLogger<BookingTimeoutJob>.Instance).RunOnceAsync(cancel.Token));
        scopes.Verify(f => f.CreateScope(), Times.Never);
    }
}
