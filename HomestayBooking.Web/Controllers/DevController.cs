using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers;

/// <summary>
/// Giả lập "tiền vào tài khoản MB" để demo luồng tự khớp mà không cần SePay thật.
/// Chỉ chạy ở môi trường Development và chỉ quản lý dùng được; môi trường khác trả 404.
/// Đi qua đúng BankMatchingService như webhook thật.
/// </summary>
[Authorize(Policy = AppPolicies.Management)]
public class DevController : Controller
{
    private readonly BankMatchingService _matching;
    private readonly VietQrService _qr;
    private readonly IWebHostEnvironment _env;
    private readonly TimeProvider _clock;

    public DevController(BankMatchingService matching, VietQrService qr, IWebHostEnvironment env, TimeProvider clock)
    {
        _matching = matching;
        _qr = qr;
        _env = env;
        _clock = clock;
    }

    /// <summary>Danh mục component giao diện để xem và chụp so sánh; ?errors=true hiện trạng thái lỗi validation của form mẫu.</summary>
    [HttpGet]
    public IActionResult UiKit(bool errors = false)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        var model = new UiKitViewModel();
        if (errors)
        {
            TryValidateModel(model);
        }
        else
        {
            ModelState.Clear();
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult SimulateBank(string? code, decimal? amount)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        return View(new SimulateBankViewModel
        {
            Content = code is null ? string.Empty : $"{code} dat coc",
            Amount = amount ?? 0,
            AccountNumber = _qr.Account.AccountNumber,
        });
    }

    [HttpPost]
    public async Task<IActionResult> SimulateBank(SimulateBankViewModel model)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _matching.ProcessAsync(new IncomingBankTransaction(
            BankProvider.Simulator, $"SIM-{Guid.NewGuid():N}", model.AccountNumber, model.Amount, model.Content,
            _clock.GetUtcNow().UtcDateTime));
        TempData["Success"] = $"Kết quả khớp: {result.Status}" + (result.BookingCode is null ? "" : $" — đơn {result.BookingCode}");
        return RedirectToAction(nameof(SimulateBank));
    }
}
