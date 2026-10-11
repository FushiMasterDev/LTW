using HomestayBooking.Web.Services;

namespace HomestayBooking.Web.BackgroundJobs;

/// <summary>
/// Chạy mỗi phút: đơn quá 15 phút chưa thanh toán → Hết hạn (D5); đơn đã cọc mà homestay quá 30 phút
/// chưa xác nhận → cảnh báo nhân viên (BR-06). DbContext là scoped nên mỗi lượt tạo một scope mới.
/// </summary>
public class BookingTimeoutJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingTimeoutJob> _logger;

    public BookingTimeoutJob(IServiceScopeFactory scopeFactory, ILogger<BookingTimeoutJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookings = scope.ServiceProvider.GetRequiredService<BookingService>();
                var expired = await bookings.ExpireOverdueHoldsAsync();
                var alerted = await bookings.AlertOverdueConfirmationsAsync();
                if (expired + alerted > 0)
                {
                    _logger.LogInformation("Hết hạn {Expired} đơn, cảnh báo {Alerted} đơn quá hạn xác nhận.", expired, alerted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Lỗi một lượt (ví dụ mất kết nối CSDL) không được làm dừng job
                _logger.LogError(ex, "BookingTimeoutJob lỗi.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
