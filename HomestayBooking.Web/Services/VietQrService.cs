using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Services;

/// <summary>
/// Ảnh QR chuyển khoản MB Bank theo chuẩn VietQR: điền sẵn số tiền và nội dung = mã đơn (D13).
/// Ảnh sinh theo URL của img.vietqr.io nên không lưu gì vào CSDL.
/// </summary>
public class VietQrService
{
    private readonly BankAccountOptions _account;

    public VietQrService(IOptions<BankAccountOptions> account)
    {
        _account = account.Value;
    }

    public BankAccountOptions Account => _account;

    /// <summary>
    /// Ảnh QR tĩnh của tài khoản (không kèm số tiền/nội dung) lưu sẵn trong wwwroot,
    /// dùng làm dự phòng khi img.vietqr.io không tải được (onerror của thẻ img).
    /// Khách quét ảnh này phải tự nhập số tiền và mã đơn.
    /// </summary>
    public const string StaticImagePath = "/images/qr-mb.png";

    public string BuildImageUrl(decimal amount, string transferContent) =>
        $"{_account.QrImageBaseUrl.TrimEnd('/')}/{_account.BankCode}-{_account.AccountNumber}-compact2.png" +
        $"?amount={decimal.ToInt64(amount)}" +
        $"&addInfo={Uri.EscapeDataString(transferContent)}" +
        $"&accountName={Uri.EscapeDataString(_account.AccountName)}";
}
