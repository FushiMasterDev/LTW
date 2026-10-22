using HomestayBooking.Web.Services;

namespace HomestayBooking.DesktopHost;

/// <summary>Các tác vụ nội bộ; không gửi tin, sinh đối soát hoặc gọi dịch vụ bên ngoài.</summary>
public sealed class DesktopMaintenanceJob(IServiceScopeFactory scopes, ILogger<DesktopMaintenanceJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do { await RunOnceAsync(stoppingToken); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        (string Name, Func<IServiceProvider, Task> Run)[] jobs =
        [
            ("Hết hạn giữ phòng", async p => { await p.GetRequiredService<BookingService>().ExpireOverdueHoldsAsync(); }),
            ("Hết hạn yêu cầu đổi", async p => { await p.GetRequiredService<BookingChangeRequestService>().ExpireUnpaidAsync(); }),
            ("Quá hạn xác nhận", async p => { await p.GetRequiredService<BookingService>().AlertOverdueConfirmationsAsync(); }),
            ("Bill chậm duyệt", async p => { await p.GetRequiredService<ReceiptReviewService>().AlertSlowReviewsAsync(); }),
            ("Hết hạn voucher", async p => { await p.GetRequiredService<VoucherService>().ExpireAsync(); }),
            ("Hết hạn hợp đồng", async p => { await p.GetRequiredService<ContractService>().ExpireAsync(); }),
            ("Quá hạn hồ sơ chủ", async p => { await p.GetRequiredService<OwnerRegistrationService>().AlertOverdueAsync(); }),
        ];
        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var scope = scopes.CreateScope();
                await job.Run(scope.ServiceProvider);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Desktop maintenance: lỗi {Job}; tiếp tục tác vụ kế tiếp.", job.Name);
            }
        }
    }
}
