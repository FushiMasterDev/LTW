using HomestayBooking.Web.Extensions;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

public class SettlementsController(ApplicationDbContext db, SettlementService settlements, AccountingService accounting,
    TimeProvider clock, IOptions<SettlementOptions> options, IStringLocalizer<SharedResource> L) : AccountingController(db)
{
    private SettlementPeriod LatestPeriod()
    {
        var period = SettlementCalculator.PeriodOf(VietnamTime.Today(clock));
        while (period.End.AddDays(options.Value.GraceDays) >= VietnamTime.Today(clock))
            period = SettlementCalculator.PeriodOf(period.Start.AddDays(-1));
        return period;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, int? periodNo, int? homestayId, SettlementStatus? status, int? page)
    {
        await LoadCountsAsync(accounting);
        try
        {
            if (!ModelState.IsValid || homestayId is <= 0 || status is { } st && !Enum.IsDefined(st))
                throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            SettlementPeriod? period = null;
            if (year.HasValue || month.HasValue || periodNo.HasValue)
            {
                if (!year.HasValue || !month.HasValue || !periodNo.HasValue)
                    throw new BookingException("Vui lòng chọn đủ năm, tháng và kỳ đối soát.");
                period = new(year.Value, month.Value, periodNo.Value);
            }
            var rows = await settlements.ListAsync(new(period, status, homestayId, page), await CurrentActorAsync());
            return View(new AccountingSettlementList(rows, LatestPeriod()));
        }
        catch (BookingException ex)
        {
            TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value;
            return View(new AccountingSettlementList(new([], 0, 1, 20), LatestPeriod()));
        }
    }

    [HttpGet("/Accounting/Settlements/{id:int}")]
    [HttpGet("/Accounting/Settlements/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var details = await settlements.DetailsAsync(id, await CurrentActorAsync());
            await LoadCountsAsync(accounting);
            return View(new AccountingSettlementDetails(details, details.History.Select(HistoryItem).ToArray()));
        }
        catch (BookingException) { return NotFound(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecent()
    {
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            var result = await settlements.GeneratePeriodAsync(LatestPeriod(), await CurrentActorAsync());
            GenerationNotice(result);
        }
        catch (BookingException ex) { Error(ex); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int year, int month, int periodNo, int? homestayId)
    {
        try
        {
            if (!ModelState.IsValid || homestayId is <= 0)
                throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            var actor = await CurrentActorAsync();
            var period = new SettlementPeriod(year, month, periodNo);
            if (homestayId is { } home)
            {
                var result = await settlements.GenerateAsync(home, period, actor);
                GenerationNotice([result]);
                if (result.Settlement is { } row) return RedirectToAction(nameof(Details), new { id = row.Id });
            }
            else GenerationNotice(await settlements.GeneratePeriodAsync(period, actor));
        }
        catch (BookingException ex) { Error(ex); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Send(int id) => Save(id, actor => settlements.SendAsync(id, actor));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RecordPaid(int id, string? bankRef) => Save(id, actor => settlements.RecordPaidAsync(id, bankRef, actor));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reopen(int id, string reason) => Save(id, actor => settlements.ReopenAsync(id, reason, actor));

    private async Task<IActionResult> Save(int id, Func<Actor, Task<Settlement>> action)
    {
        // Only accountants can send, pay or reopen. Confirm remains an owner action.
        if (User.EmployeePosition() != EmployeePosition.Accountant) return Forbid();
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.");
            await action(await CurrentActorAsync());
            TempData["Success"] = L["Đã lưu thay đổi."].Value;
        }
        catch (BookingException ex) { Error(ex); }
        return RedirectToAction(nameof(Details), new { id });
    }
    private void Error(BookingException ex) => TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value;
    private void GenerationNotice(IReadOnlyList<SettlementDraftResult> result)
    {
        TempData["Success"] = L["Đã kiểm tra kỳ đối soát: {0} bảng có dữ liệu.", result.Count(r => r.Settlement is not null)].Value;
        var codes = result.SelectMany(r => r.UnclosedBookingCodes).Distinct().ToArray();
        if (codes.Length > 0) TempData["Info"] = L["Còn đơn chưa chốt trong kỳ: {0}.", string.Join(", ", codes)].Value;
    }
    private static TimelineItem HistoryItem(AuditLog audit)
    {
        var title = "Cập nhật đối soát";
        string? note = null;
        if (!string.IsNullOrWhiteSpace(audit.NewValues))
        {
            try
            {
                using var json = JsonDocument.Parse(audit.NewValues);
                var values = json.RootElement.TryGetProperty("Values", out var wrapped) ? wrapped : json.RootElement;
                if (values.TryGetProperty("Step", out var step)) title = step.GetString() switch
                {
                    "Sent" => "Đã gửi chủ", "OwnerConfirmed" => "Chủ đã xác nhận", "Paid" => "Đã chuyển tiền",
                    "Reopened" => "Đã mở lại", "OwnerDiscrepancy" => "Chủ phản hồi sai lệch", _ => title,
                };
                if (values.TryGetProperty("Note", out var value) && value.ValueKind == JsonValueKind.String) note = value.GetString();
            }
            catch (JsonException) { }
        }
        return new(audit.CreatedAt, title, note);
    }
}
