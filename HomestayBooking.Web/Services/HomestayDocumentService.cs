using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Giấy tờ sau tiếp nhận: dùng chung lưu file B1; pháp lý mới đưa cơ sở về chờ duyệt.</summary>
public class HomestayDocumentService(ApplicationDbContext db, HomestayAccessService access, OwnerRegistrationService registrations,
    NotificationService notifications, TimeProvider clock)
{
    public async Task<VerificationDocument> UploadAsync(int homeId, DocumentType type, IFormFile file, DateOnly? expiry, Actor actor)
    {
        VerificationDocument? created = null;
        try
        {
            return await HomestayWrite.RunAsync(db, async () =>
            {
                var home = await access.AuthorizeAsync(homeId, actor); var old = HomestayReviewService.State(home);
                created = await registrations.StoreDocumentAsync(home, type, file, expiry, actor);
                if (type is DocumentType.BusinessLicense or DocumentType.FireSafety && home.Status is HomestayStatus.Active or HomestayStatus.Suspended)
                {
                    if (home.Status == HomestayStatus.Active) HomestayReviewService.Move(home, HomestayStatus.PendingReview, clock);
                    PartnerServiceSupport.Audit(db, clock, actor, "Homestays", homeId.ToString(), old, HomestayReviewService.State(home));
                    await HomestayManagerMessages.SendAsync(db, notifications, clock, homeId, "Có giấy tờ pháp lý mới, cần duyệt lại");
                }
                return created;
            });
        }
        catch { if (created is not null) registrations.DeleteNewDocumentFile(created.FileUrl); throw; }
    }
    public Task<int> DeleteAsync(int id, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var doc = await db.VerificationDocuments.SingleOrDefaultAsync(d => d.Id == id) ?? throw new BookingException("Không tìm thấy giấy tờ.");
        if (doc.HomestayId is not int homeId) throw new BookingException("Không xóa hoặc chuyển liên kết giấy tờ lịch sử.");
        var home = await access.AuthorizeAsync(homeId, actor);
        if (home.Status is not (HomestayStatus.Draft or HomestayStatus.PendingReview)) throw new BookingException("Không xóa giấy tờ của cơ sở đang hoạt động, tạm ngưng hoặc đã kết thúc.");
        db.VerificationDocuments.Remove(doc);
        PartnerServiceSupport.Audit(db, clock, actor, "VerificationDocuments", id.ToString(), new { doc.HomestayId, doc.FileUrl, doc.DocumentType, doc.ExpiryDate }, new { Deleted = true }, AuditAction.Delete);
        return id;
    });
}
