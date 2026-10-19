using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Areas.Admin.Controllers;

public class HomestaysController(ApplicationDbContext db, HomestayReviewService reviews, HomestayContentService content,
    OwnerRegistrationQueryService documents, IStringLocalizer<SharedResource> localizer) : AdminServiceController(db, localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await reviews.PendingAsync(await CurrentActorAsync()));
    [HttpGet("/Admin/Homestays/{id:int}")]
    [HttpGet("/Admin/Homestays/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var actor = await CurrentActorAsync();
            var home = await content.EditorAsync(id, actor, allowConsultant: false);
            var pending = home.Status == HomestayStatus.PendingReview ? await reviews.PendingDetailsAsync(id, actor) : null;
            return View(new AdminReviewDetails(home, pending));
        }
        catch (BookingException) { return NotFound(); }
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Document(int id)
    {
        try
        {
            var file = await documents.OpenDocumentAsync(id, await CurrentActorAsync());
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(file.Path, file.ContentType, file.DownloadName);
        }
        catch (BookingException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(int id) => Activate(id, false, null);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Resume(int id, string reason) => Activate(id, true, reason);
    private async Task<IActionResult> Activate(int id, bool resume, string? reason)
    {
        await SaveAsync(async () =>
        {
            var actor = await CurrentActorAsync();
            var result = resume ? await reviews.ResumeAsync(id, reason ?? "", actor) : await reviews.ApproveAsync(id, actor);
            if (!result.Succeeded) throw new BookingException("Chưa thể duyệt. Cần bổ sung: {0}", string.Join(", ", result.Missing.Select(m => L[m].Value)));
        });
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        await SaveAsync(async () => await reviews.RejectAsync(id, reason, await CurrentActorAsync()));
        return RedirectToAction(nameof(Details), new { id });
    }
}
