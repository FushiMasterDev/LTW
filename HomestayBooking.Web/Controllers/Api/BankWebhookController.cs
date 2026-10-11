using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>
/// Nhận giao dịch tiền vào tài khoản MB của Fuji từ SePay (D13). SePay gửi header "Authorization: Apikey {khóa}".
/// Không dùng antiforgery (lời gọi từ máy chủ SePay, không từ trình duyệt); xác thực bằng khóa API.
/// </summary>
[ApiController]
[Route("api/bank-webhook")]
[IgnoreAntiforgeryToken]
public class BankWebhookController : ControllerBase
{
    private readonly BankMatchingService _matching;
    private readonly BankAccountOptions _account;

    public BankWebhookController(BankMatchingService matching, IOptions<BankAccountOptions> account)
    {
        _matching = matching;
        _account = account.Value;
    }

    /// <summary>Định dạng dữ liệu SePay gửi (transactionDate là giờ Việt Nam).</summary>
    public record SePayPayload(
        long Id,
        string? Gateway,
        string TransactionDate,
        string AccountNumber,
        string? Code,
        string? Content,
        string TransferType,
        decimal TransferAmount,
        string? ReferenceCode,
        string? Description);

    [HttpPost("sepay")]
    public async Task<IActionResult> SePay(SePayPayload payload)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        // Chỉ quan tâm tiền vào
        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase) || payload.TransferAmount <= 0)
        {
            return Ok(new { success = true });
        }

        var local = DateTime.ParseExact(payload.TransactionDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var result = await _matching.ProcessAsync(new IncomingBankTransaction(
            BankProvider.SePay,
            payload.Id.ToString(CultureInfo.InvariantCulture),
            payload.AccountNumber,
            payload.TransferAmount,
            payload.Content,
            TimeZoneInfo.ConvertTimeToUtc(local, TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")),
            payload.ReferenceCode,
            JsonSerializer.Serialize(payload)));

        // SePay coi 200/201 kèm success = true là đã nhận; trả 200 cả khi trùng để SePay không gửi lại
        return Ok(new { success = true, status = result.Status.ToString(), booking = result.BookingCode, duplicate = result.Duplicate });
    }

    /// <summary>So khớp khóa API trong thời gian cố định để không lộ khóa qua thời gian phản hồi.</summary>
    private bool IsAuthorized()
    {
        if (string.IsNullOrEmpty(_account.WebhookApiKey))
        {
            return false;
        }
        var header = Request.Headers.Authorization.ToString();
        const string scheme = "Apikey ";
        if (!header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(header[scheme.Length..].Trim()),
            Encoding.UTF8.GetBytes(_account.WebhookApiKey));
    }
}
