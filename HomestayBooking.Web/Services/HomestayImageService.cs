using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public class HomestayImageService(ApplicationDbContext db, HomestayAccessService access, IWebHostEnvironment environment, TimeProvider clock)
{
    private string Root => string.IsNullOrWhiteSpace(environment.WebRootPath) ? Path.Combine(environment.ContentRootPath, "wwwroot") : environment.WebRootPath;
    private static object State(HomestayImage i) => new { i.Id, i.HomestayId, i.RoomTypeId, i.Url, i.SortOrder, i.IsCover };
    private async Task ValidateTypeAsync(int homeId, int? typeId)
    {
        if (typeId is not null && !await db.RoomTypes.AnyAsync(t => t.Id == typeId && t.HomestayId == homeId))
            throw new BookingException("Ảnh phải gắn loại phòng thuộc cùng homestay.");
    }
    public async Task<HomestayImage> UploadAsync(int homeId, int? typeId, IFormFile file, bool cover, int sortOrder, Actor actor)
    {
        string? created = null;
        try
        {
            return await HomestayWrite.RunAsync(db, async () =>
            {
                await access.AuthorizeAsync(homeId, actor); await ValidateTypeAsync(homeId, typeId);
                if (sortOrder < 0) throw new BookingException("Thứ tự ảnh không được âm.");
                var extension = await ReceiptFileStore.ValidateImageAsync(file);
                var directory = Path.Combine(Root, "uploads", "homestays", homeId.ToString()); Directory.CreateDirectory(directory);
                var name = Guid.NewGuid().ToString("N") + extension; created = Path.Combine(directory, name);
                await using (var output = File.Create(created)) await file.CopyToAsync(output);
                if (cover) await ClearCoverAsync(homeId, null, actor);
                var image = new HomestayImage { HomestayId = homeId, RoomTypeId = typeId, Url = $"/uploads/homestays/{homeId}/{name}", SortOrder = sortOrder, IsCover = cover };
                db.HomestayImages.Add(image); await db.SaveChangesAsync();
                PartnerServiceSupport.Audit(db, clock, actor, "HomestayImages", image.Id.ToString(), null, State(image), AuditAction.Insert);
                return image;
            });
        }
        catch { if (created is not null && File.Exists(created)) File.Delete(created); throw; }
    }
    private async Task<HomestayImage> ImageAsync(int id, Actor actor)
    {
        var image = await db.HomestayImages.SingleOrDefaultAsync(i => i.Id == id) ?? throw new BookingException("Không tìm thấy ảnh.");
        await access.AuthorizeAsync(image.HomestayId, actor); return image;
    }
    private async Task ClearCoverAsync(int homeId, int? except, Actor actor)
    {
        foreach (var image in await db.HomestayImages.Where(i => i.HomestayId == homeId && i.IsCover && i.Id != except).ToListAsync())
        {
            var old = State(image); image.IsCover = false;
            PartnerServiceSupport.Audit(db, clock, actor, "HomestayImages", image.Id.ToString(), old, State(image));
        }
        // Thả unique filtered trước khi bật ảnh bìa khác, vẫn trong transaction của người gọi.
        await db.SaveChangesAsync();
    }
    public Task<HomestayImage> SetCoverAsync(int id, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var image = await ImageAsync(id, actor);
        if (image.IsCover) return image;
        await ClearCoverAsync(image.HomestayId, id, actor); var old = State(image); image.IsCover = true;
        PartnerServiceSupport.Audit(db, clock, actor, "HomestayImages", id.ToString(), old, State(image)); return image;
    });
    public Task<HomestayImage> UpdateAsync(int id, int? typeId, int sortOrder, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var image = await ImageAsync(id, actor); await ValidateTypeAsync(image.HomestayId, typeId);
        if (sortOrder < 0) throw new BookingException("Thứ tự ảnh không được âm.");
        var old = State(image); image.RoomTypeId = typeId; image.SortOrder = sortOrder;
        PartnerServiceSupport.Audit(db, clock, actor, "HomestayImages", id.ToString(), old, State(image)); return image;
    });
    public async Task DeleteAsync(int id, Actor actor)
    {
        var image = await HomestayWrite.RunAsync(db, async () =>
        {
            var row = await ImageAsync(id, actor); var home = await db.Homestays.SingleAsync(h => h.Id == row.HomestayId);
            if (home.Status == HomestayStatus.Active && row.IsCover) throw new BookingException("Không xóa ảnh bìa duy nhất của homestay đang hoạt động; hãy chọn ảnh bìa khác trước.");
            db.HomestayImages.Remove(row);
            PartnerServiceSupport.Audit(db, clock, actor, "HomestayImages", id.ToString(), State(row), new { Deleted = true }, AuditAction.Delete);
            return row;
        });
        // Chỉ xóa file do service này quản lý, sau khi DB đã commit; không đụng ảnh demo hay URL ngoài.
        var prefix = $"/uploads/homestays/{image.HomestayId}/";
        if (image.Url.StartsWith(prefix, StringComparison.Ordinal))
        {
            var name = image.Url[prefix.Length..];
            if (name == Path.GetFileName(name) && !name.Contains('\\') && name is not ("." or ".."))
            {
                var path = Path.Combine(Root, "uploads", "homestays", image.HomestayId.ToString(), name);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
