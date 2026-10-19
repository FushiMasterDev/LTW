using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

/// <summary>Tổng quan việc của kế toán: bill chờ duyệt, giao dịch cần xử lý, tiền chờ hoàn.</summary>
public class DashboardController : AccountingController
{
    private readonly AccountingService _accounting;

    public DashboardController(ApplicationDbContext db, AccountingService accounting) : base(db)
    {
        _accounting = accounting;
    }

    public async Task<IActionResult> Index([FromServices] SettlementService settlements)
    {
        await LoadCountsAsync(_accounting);
        var actor = await CurrentActorAsync();
        var drafts = await ReadAll(SettlementStatus.Draft);
        var sent = await ReadAll(SettlementStatus.Sent);
        var confirmed = await ReadAll(SettlementStatus.Confirmed);
        return View(new AccountingSettlementSummary(drafts.Count, drafts.Sum(s => s.PayoutAmount),
            sent.Count + confirmed.Count, sent.Sum(s => s.PayoutAmount) + confirmed.Sum(s => s.PayoutAmount)));

        async Task<List<Settlement>> ReadAll(SettlementStatus status)
        {
            var result = new List<Settlement>();
            var page = 1;
            do
            {
                var rows = await settlements.ListAsync(new(Status: status, Page: page), actor);
                result.AddRange(rows);
                if (!rows.HasNextPage) break;
                page++;
            } while (true);
            return result;
        }
    }
}
