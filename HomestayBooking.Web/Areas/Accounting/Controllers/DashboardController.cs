using HomestayBooking.Web.Data;
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

    public async Task<IActionResult> Index()
    {
        await LoadCountsAsync(_accounting);
        return View();
    }
}
