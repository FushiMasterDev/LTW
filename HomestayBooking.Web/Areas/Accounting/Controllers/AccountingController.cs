using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Areas.Accounting.Controllers;

/// <summary>Lớp cơ sở khu Kế toán: Kế toán, Quản lý, Quản trị hệ thống (AppPolicies.Accounting).</summary>
[Area("Accounting")]
[Authorize(Policy = AppPolicies.Accounting)]
public abstract class AccountingController : Controller
{
    protected AccountingController(ApplicationDbContext db)
    {
        Db = db;
    }

    protected ApplicationDbContext Db { get; }

    protected Task<int> CurrentEmployeeIdAsync()
    {
        var personId = User.PersonId();
        return Db.Employees.Where(e => e.PersonId == personId).Select(e => e.Id).SingleAsync();
    }

    /// <summary>Số việc đang chờ cho thanh tab — mỗi trang kế toán đều hiện.</summary>
    protected async Task LoadCountsAsync(AccountingService accounting)
    {
        var refunds = accounting.PendingRefunds();
        ViewData["Counts"] = new AccountingCounts(
            await accounting.InitialDepositReceipts().CountAsync(),
            await Db.BankTransactions.CountAsync(t => AccountingService.OpenStatuses.Contains(t.MatchStatus)),
            await refunds.CountAsync(),
            await refunds.SumAsync(b => b.RefundAmount ?? 0),
            await accounting.PendingAdditionalDeposits().CountAsync());
    }
}
