using HomestayBooking.Web.Services;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.BackgroundJobs;

/// <summary>
/// Chạy mỗi phút: đơn quá 15 phút chưa thanh toán → Hết hạn (D5); đơn đã cọc mà homestay quá 30 phút
/// chưa xác nhận → cảnh báo nhân viên (BR-06); bill chờ duyệt quá 2 giờ làm việc → báo quản lý.
/// Lập đối soát theo khoảng cấu hình riêng; DbContext là scoped nên mỗi tác vụ tạo một scope mới.
/// </summary>
public class BookingTimeoutJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingTimeoutJob> _logger;
    private readonly TimeProvider _clock;
    private readonly IOptions<SettlementOptions> _settlements;
    private DateTimeOffset? _lastSettlementGenerationSucceededAt;

    public BookingTimeoutJob(IServiceScopeFactory scopeFactory, ILogger<BookingTimeoutJob> logger,
        TimeProvider clock, IOptions<SettlementOptions> settlements)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
        _settlements = settlements;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Mỗi tác vụ có scope/try-catch riêng, không dùng context lỗi của tác vụ trước.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        (string Name, Func<IServiceProvider, Task> Run)[] jobs = [
            ("Hết hạn giữ phòng", async p => { await p.GetRequiredService<BookingService>().ExpireOverdueHoldsAsync(); }),
            ("Hết hạn yêu cầu đổi", async p => { await p.GetRequiredService<BookingChangeRequestService>().ExpireUnpaidAsync(); }),
            ("Quá hạn xác nhận", async p => { await p.GetRequiredService<BookingService>().AlertOverdueConfirmationsAsync(); }),
            ("Bill chậm duyệt", async p => { await p.GetRequiredService<ReceiptReviewService>().AlertSlowReviewsAsync(); }),
            ("Hết hạn voucher", async p => { await p.GetRequiredService<VoucherService>().ExpireAsync(); }),
            ("Hết hạn hợp đồng", async p => { await p.GetRequiredService<ContractService>().ExpireAsync(); }),
            ("Quá hạn hồ sơ chủ", async p => { await p.GetRequiredService<OwnerRegistrationService>().AlertOverdueAsync(); }),
            ("Nhắc khách nhận phòng", async p => { await p.GetRequiredService<ReminderService>().SendCustomersAsync(cancellationToken); }),
            ("Nhắc chủ đón khách", async p => { await p.GetRequiredService<ReminderService>().SendOwnersAsync(cancellationToken); }),
            ("Lập đối soát kỳ đã kết thúc", async p =>
            {
                if (_lastSettlementGenerationSucceededAt is { } last
                    && _clock.GetUtcNow() - last < TimeSpan.FromMinutes(_settlements.Value.GenerateEveryMinutes)) return;
                await p.GetRequiredService<SettlementService>().GenerateRecentAsync();
                // Chỉ giữ mốc sau thành công; lỗi thì được thử lại ở lượt kế tiếp.
                _lastSettlementGenerationSucceededAt = _clock.GetUtcNow();
            }),
            ("Từ chối khoản bù treo", async p => { await p.GetRequiredService<CompensationService>().RejectClosedAsync(); })
        ];
        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await job.Run(scope.ServiceProvider);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "BookingTimeoutJob: lỗi tác vụ {Job}; tiếp tục tác vụ kế tiếp.", job.Name);
            }
        }
    }

}
