using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HomestayBooking.Tests.Services;

public sealed class AdminBootstrapServiceTests
{
    private const string Password = "Admin@Bootstrap2026";

    private static (AdminBootstrapService Service, ListLogger Logs) Service(PartnerTestHost h, string environment, AdminBootstrapOptions options)
    {
        var env = new Mock<IHostEnvironment>(); env.SetupGet(e => e.EnvironmentName).Returns(environment);
        var logs = new ListLogger();
        var admin = new AccountAdminService(h.Db, h.Users, new(h.Db, new HttpContextAccessor()), h.H.Clock);
        return (new(h.Db, admin, Options.Create(options), env.Object, logs), logs);
    }

    private static AdminBootstrapOptions Config() => new() { EmployeeCode = "qt001", FullName = "Quản trị đầu tiên", Phone = "0977888001", Password = Password };

    /// <summary>Host B1 luôn có sẵn một Administrator; hạ chức vụ để mô phỏng CSDL triển khai mới.</summary>
    private static async Task RemoveAdministratorsAsync(PartnerTestHost h)
    {
        foreach (var e in await h.Db.Employees.Where(e => e.Position == EmployeePosition.Administrator).ToListAsync()) e.Position = EmployeePosition.Manager;
        await h.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task ProductionWithoutAdministratorCreatesExactlyOneFromConfig()
    {
        using var h = await PartnerTestHost.CreateAsync(); await RemoveAdministratorsAsync(h);
        var (service, logs) = Service(h, Environments.Production, Config());
        Assert.True(await service.RunAsync());
        Assert.False(await service.RunAsync());
        var admin = await h.Db.Employees.Include(e => e.Person).ThenInclude(p => p.Account).SingleAsync(e => e.Position == EmployeePosition.Administrator);
        Assert.Equal("QT001", admin.EmployeeCode); Assert.Equal("+84977888001", admin.Person.PhoneNumber);
        Assert.Equal(AccountStatus.Active, admin.Person.Account!.Status);
        Assert.True(await h.Users.CheckPasswordAsync(admin.Person.Account, Password));
        Assert.Contains(await h.Db.AccountActivityLogs.Where(l => l.AccountId == admin.Person.Account.Id).ToListAsync(),
            l => l.EventType == AccountEventType.AccountCreated && l.ActorType == ActorType.System);
        Assert.DoesNotContain(logs.Messages, m => m.Contains(Password));
        Assert.DoesNotContain(await h.Db.AuditLogs.ToListAsync(), a => (a.NewValues ?? "").Contains(Password));
    }

    [Fact]
    public async Task ExistingAdministratorMeansNothingIsCreated()
    {
        using var h = await PartnerTestHost.CreateAsync();
        var before = await h.Db.Employees.CountAsync();
        Assert.False(await Service(h, Environments.Production, Config()).Service.RunAsync());
        Assert.Equal(before, await h.Db.Employees.CountAsync());
    }

    [Fact]
    public async Task MissingConfigurationLogsErrorAndCreatesNothing()
    {
        using var h = await PartnerTestHost.CreateAsync(); await RemoveAdministratorsAsync(h);
        var missing = Config(); missing.Password = null;
        var (service, logs) = Service(h, Environments.Production, missing);
        Assert.False(await service.RunAsync());
        Assert.Contains(logs.Levels, l => l == LogLevel.Error);
        Assert.False(await h.Db.Employees.AnyAsync(e => e.Position == EmployeePosition.Administrator));
    }

    [Fact]
    public async Task DevelopmentNeverBootstraps()
    {
        using var h = await PartnerTestHost.CreateAsync(); await RemoveAdministratorsAsync(h);
        Assert.False(await Service(h, Environments.Development, Config()).Service.RunAsync());
        Assert.False(await h.Db.Employees.AnyAsync(e => e.Position == EmployeePosition.Administrator));
    }

    private sealed class ListLogger : ILogger<AdminBootstrapService>
    {
        public List<string> Messages { get; } = [];
        public List<LogLevel> Levels { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Levels.Add(logLevel); Messages.Add(formatter(state, exception)); }
    }
}
