using HomestayBooking.Web.BackgroundJobs;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class BookingTimeoutJobTests
{
    [Fact]
    public async Task SettlementAndCompensationJobsRunDespiteEarlierJobFailuresAndAreIdempotent()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.CompensationAsync(HomestayBooking.Web.Models.Entities.BookingStatus.NoShow);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(() =>
        {
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns((Type t) => t == typeof(SettlementService) ? h.Service()
                : t == typeof(CompensationService) ? h.Compensation() : throw new InvalidOperationException("Tác vụ trước lỗi giả lập"));
            var scope = new Mock<IServiceScope>(); scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object); return scope.Object;
        });
        var job = new BookingTimeoutJob(factory.Object, NullLogger<BookingTimeoutJob>.Instance, h.H.Clock, Options.Create(h.Options));
        await job.RunOnceAsync(); var auditCount = await h.Db.AuditLogs.CountAsync(); await job.RunOnceAsync();
        Assert.Single(h.Db.Settlements); Assert.Equal(HomestayBooking.Web.Models.Entities.PaymentStatus.Rejected, (await h.Db.Payments.SingleAsync()).Status);
        Assert.Equal(auditCount, await h.Db.AuditLogs.CountAsync());
    }

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
        await new BookingTimeoutJob(scopes.Object, NullLogger<BookingTimeoutJob>.Instance, h.H.Clock, Options.Create(new SettlementOptions())).RunOnceAsync();
        Assert.Equal(11, created); Assert.Equal(created, disposed); Assert.Equal(2, reminderRequests);
        Assert.Equal(2, h.Sender.Calls.Count);
    }

    [Fact]
    public async Task CancellationStopsTheBatchWithoutCreatingScope()
    {
        var scopes = new Mock<IServiceScopeFactory>(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new BookingTimeoutJob(scopes.Object, NullLogger<BookingTimeoutJob>.Instance, TimeProvider.System, Options.Create(new SettlementOptions())).RunOnceAsync(cancel.Token));
        scopes.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task SettlementRunsEveryHourButCompensationRunsEachMinuteAndLateCompletionIsRebuilt()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BookingAsync();
        var late = await h.BookingAsync(HomestayBooking.Web.Models.Entities.BookingStatus.Confirmed);
        var generations = 0; var rejections = 0;
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(() =>
        {
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns((Type t) =>
            {
                if (t == typeof(SettlementService)) { generations++; return h.Service(); }
                if (t == typeof(CompensationService)) { rejections++; return h.Compensation(); }
                throw new InvalidOperationException("Tác vụ khác lỗi giả lập");
            });
            var scope = new Mock<IServiceScope>(); scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object); return scope.Object;
        });
        var job = new BookingTimeoutJob(factory.Object, NullLogger<BookingTimeoutJob>.Instance, h.H.Clock, Options.Create(h.Options));
        await job.RunOnceAsync(); Assert.Single(h.Db.SettlementItems);
        late.Status = HomestayBooking.Web.Models.Entities.BookingStatus.Completed; await h.Db.SaveChangesAsync();
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await job.RunOnceAsync();
        h.H.Clock.Advance(TimeSpan.FromMinutes(58)); await job.RunOnceAsync();
        Assert.Equal(1, generations); Assert.Equal(3, rejections); Assert.Single(h.Db.SettlementItems);
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await job.RunOnceAsync();
        Assert.Equal(2, generations); Assert.Equal(4, rejections); Assert.Equal(2, await h.Db.SettlementItems.CountAsync());
    }

    [Fact]
    public async Task FailedGenerationDoesNotDelayRetryUntilNextHour()
    {
        using var h = await SettlementTestHost.CreateAsync(); await h.BookingAsync();
        var attempts = 0; var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(() =>
        {
            var provider = new Mock<IServiceProvider>();
            provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns((Type t) =>
            {
                if (t == typeof(SettlementService) && ++attempts > 1) return h.Service();
                throw new InvalidOperationException("Lỗi lần đầu giả lập");
            });
            var scope = new Mock<IServiceScope>(); scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object); return scope.Object;
        });
        var job = new BookingTimeoutJob(factory.Object, NullLogger<BookingTimeoutJob>.Instance, h.H.Clock, Options.Create(h.Options));
        await job.RunOnceAsync(); Assert.Empty(h.Db.Settlements);
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await job.RunOnceAsync(); Assert.Single(h.Db.Settlements);
        h.H.Clock.Advance(TimeSpan.FromMinutes(1)); await job.RunOnceAsync(); Assert.Equal(2, attempts);
    }
}
