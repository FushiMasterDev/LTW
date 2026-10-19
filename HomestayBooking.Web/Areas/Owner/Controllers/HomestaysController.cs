using System.Security.Claims;
using HomestayBooking.Web.Extensions;
using System.Text.Json;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Owner.Controllers;

[Area(AppRoles.Owner), Authorize(Policy = AppPolicies.ApprovedOwner)]
public class HomestaysController(HomestayContentService content, RoomService rooms, HomestayImageService images,
    HomestayPolicyService policies, HomestayDocumentService documents, OwnerRegistrationQueryService registrations, AvailabilityService availability,
    TimeProvider clock, IStringLocalizer<SharedResource> L) : Controller
{
    private string DraftKey(int id, string tab) => $"owner-editor:{Actor.AccountId}:{id}:{tab}";
    private Actor Actor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));
    public async Task<IActionResult> Index(string? q, int? page)
    {
        var homes = await content.OwnerHomestaysAsync(Actor);
        var filtered = homes.Where(h => string.IsNullOrWhiteSpace(q) || h.Name.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        const int size = 10;
        var index = Math.Clamp(page ?? 1,1,Math.Max(1,(int)Math.Ceiling(filtered.Count/(double)size)));
        return View(new OwnerHomestaysModel(new PaginatedList<OwnerHomestaySummary>(filtered.Skip((index-1)*size).Take(size),filtered.Count,index,size),q));
    }
    [HttpGet("/Owner/Homestays/{id:int}")]
    [HttpGet("/Owner/Homestays/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, string tab = "info")
    {
        try
        {
            var h = await content.EditorAsync(id, Actor);
            if (!new[] { "info", "location", "rooms", "images", "amenities", "policy", "documents" }.Contains(tab)) return NotFound();
            var contentDraft = HttpContext.Session.GetString(DraftKey(id,"info"));
            var locationDraft = HttpContext.Session.GetString(DraftKey(id,"location"));
            HttpContext.Session.Remove(DraftKey(id,tab));
            return View(new OwnerEditorModel(h, tab, VietnamTime.Today(clock)) {
                Areas = tab == "location" ? (await availability.SearchAsync(new())).Areas : [],
                Content = (tab == "info" && contentDraft is not null ? JsonSerializer.Deserialize<OwnerContentForm>(contentDraft) : null) ?? new() { Name=h.Name, DescriptionHtml=h.DescriptionHtml, CheckinGuideHtml=h.CheckinGuideHtml,
                    ContactPhone=h.ContactPhone ?? "", CheckinTime=h.CheckinTime, CheckoutTime=h.CheckoutTime },
                Location = (tab == "location" && locationDraft is not null ? JsonSerializer.Deserialize<OwnerLocationForm>(locationDraft) : null) ?? new() { Address=h.Address, AreaId=h.AreaId, Latitude=h.Latitude, Longitude=h.Longitude } });
        }
        catch (BookingException) { return NotFound(); }
    }
    private async Task<IActionResult> Save(int id, string tab, Func<HomestayEditor, Task> action)
    {
        HomestayEditor h;
        try { h = await content.EditorAsync(id, Actor); }
        catch (BookingException) { return NotFound(); }
        try
        {
            if (!ModelState.IsValid) throw new BookingException("Thông tin không hợp lệ. Vui lòng kiểm tra các trường và thử lại.");
            await action(h);
            TempData["Success"] = L["Đã lưu thay đổi."].Value;
        }
        catch (BookingException ex) { TempData["Error"] = L[ex.ResourceKey, ex.Arguments].Value; }
        return RedirectToAction(nameof(Edit), new { id, tab });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Content(int id, [Bind(Prefix = "Content")] OwnerContentForm contentForm)
    {
        var result = await Save(id, "info", async h => {
        await content.UpdateAsync(id, new(contentForm.Name, contentForm.DescriptionHtml, contentForm.CheckinGuideHtml,
            contentForm.ContactPhone, contentForm.CheckinTime, contentForm.CheckoutTime, h.AmenityIds), Actor);
        });
        if (TempData.ContainsKey("Error")) HttpContext.Session.SetString(DraftKey(id,"info"),JsonSerializer.Serialize(contentForm));
        return result;
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Location(int id, [Bind(Prefix = "Location")] OwnerLocationForm locationForm)
    {
        var result = await Save(id, "location", async h => {
            var saved = await content.UpdateLocationAsync(id, new(locationForm.Address, locationForm.AreaId, locationForm.Latitude, locationForm.Longitude), Actor);
            if (saved.Homestay.Status == HomestayStatus.PendingReview) TempData["Info"] = L["Đang chờ Quản lý duyệt"].Value;
        });
        if (TempData.ContainsKey("Error")) HttpContext.Session.SetString(DraftKey(id,"location"),JsonSerializer.Serialize(locationForm));
        return result;
    }
    private static HomestayEditorRoomType Type(HomestayEditor h, int typeId) => h.RoomTypes.SingleOrDefault(t => t.Id == typeId)
        ?? throw new BookingException("Không tìm thấy loại phòng.");
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Price(int id, int typeId, DayType dayType, DateOnly effectiveFrom, decimal price) => Save(id, "rooms", async h => {
        Type(h, typeId); await rooms.SetPriceAsync(typeId, dayType, effectiveFrom, price, Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RoomType(int id, int typeId, string name, int maxGuests, int? areaM2, string? description) => Save(id, "rooms", async h => {
        var t = Type(h, typeId); await rooms.UpdateTypeAsync(typeId, new(name, t.CategoryId, maxGuests, areaM2, description), Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> AddRoom(int id, int typeId, string code) => Save(id, "rooms", async h => {
        Type(h, typeId); await rooms.AddRoomAsync(typeId, code, Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Room(int id, int roomId, string code) => Save(id, "rooms", async h => {
        if (!h.RoomTypes.Any(t => t.Rooms.Any(r => r.Id == roomId))) throw new BookingException("Không tìm thấy phòng.");
        await rooms.RenameRoomAsync(roomId, code, Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Active(int id, int typeId, int? roomId, bool active) => Save(id, "rooms", async h => {
        var t = Type(h, typeId);
        RoomAvailabilityChange change;
        if (roomId is int r) {
            if (!t.Rooms.Any(x => x.Id == r)) throw new BookingException("Không tìm thấy phòng.");
            change = await rooms.SetRoomActiveAsync(r, active, Actor);
        } else change = await rooms.SetTypeActiveAsync(typeId, active, Actor);
        if (change.UpcomingBookingCodes.Count > 0) TempData["Info"] = L["Đơn sắp tới cần kiểm tra: {0}", string.Join(", ", change.UpcomingBookingCodes)].Value;
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Upload(int id, IFormFile? file, int? typeId, bool cover, int sortOrder) => Save(id, "images", async h => {
        if (typeId is int t) Type(h,t);
        if (file is null) throw new BookingException("Vui lòng chọn file.");
        await images.UploadAsync(id, typeId, file, cover, sortOrder, Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Image(int id, int imageId, string operation, int? typeId, int sortOrder) => Save(id, "images", async h => {
        if (!h.Images.Any(i => i.Id == imageId)) throw new BookingException("Không tìm thấy ảnh.");
        if (operation == "cover") await images.SetCoverAsync(imageId, Actor);
        else if (operation == "delete") await images.DeleteAsync(imageId, Actor);
        else if (operation == "update") await images.UpdateAsync(imageId, typeId, sortOrder, Actor);
        else throw new BookingException("Thông tin không hợp lệ. Vui lòng kiểm tra các trường và thử lại.");
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Policy(int id, PolicyScope scope, string name, List<int> minDaysBefore, List<int> refundPercent) => Save(id, "policy", async h => {
        if (minDaysBefore.Count != refundPercent.Count) throw new BookingException("Quy tắc hủy không hợp lệ.");
        await policies.ReplaceAsync(id, scope, name, minDaysBefore.Zip(refundPercent, (d,p) => new CancellationRuleInput(d,p)).ToList(), Actor);
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RemovePolicy(int id, PolicyScope scope) => Save(id, "policy", async h => { await policies.RemoveAsync(id, scope, Actor); });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Document(int id, DocumentType type, IFormFile? file, DateOnly? expiry) => Save(id, "documents", async h => {
        if (file is null) throw new BookingException("Vui lòng chọn file.");
        await documents.UploadAsync(id, type, file, expiry, Actor);
        var saved = await content.EditorAsync(id, Actor);
        if (saved.Status == HomestayStatus.PendingReview) TempData["Info"] = L["Đang chờ Quản lý duyệt"].Value;
    });
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteDocument(int id, int documentId) => Save(id, "documents", async h => {
        if (!h.Documents.Any(d => d.Id == documentId)) throw new BookingException("Không tìm thấy giấy tờ.");
        await documents.DeleteAsync(documentId, Actor);
    });
    public async Task<IActionResult> OpenDocument(int id)
    {
        try { var f = await registrations.OpenDocumentAsync(id, Actor); return PhysicalFile(f.Path, f.ContentType, f.DownloadName); }
        catch (BookingException) { return NotFound(); }
    }
}
