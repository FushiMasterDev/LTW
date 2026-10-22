using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Chốt sở hữu trước khi gọi nghiệp vụ: tài nguyên ngoài phạm vi được xem như không tồn tại.</summary>
public class OwnerResourceService(ApplicationDbContext db, HomestayContentService content)
{
    public async Task HomeAsync(int id, Actor actor)
    {
        if (!(await content.OwnerHomestaysAsync(actor)).Any(h => h.Id == id))
            throw new BookingException("Không tìm thấy homestay.");
    }

    public async Task TypeAsync(int homeId, int typeId, Actor actor)
    {
        await HomeAsync(homeId, actor);
        if (!await db.RoomTypes.AnyAsync(t => t.Id == typeId && t.HomestayId == homeId))
            throw new BookingException("Không tìm thấy loại phòng.");
    }

    public async Task RoomAsync(int homeId, int roomId, Actor actor)
    {
        await HomeAsync(homeId, actor);
        if (!await db.Rooms.AnyAsync(r => r.Id == roomId && r.RoomType.HomestayId == homeId))
            throw new BookingException("Không tìm thấy phòng.");
    }

    public async Task BlockAsync(int homeId, int roomId, int blockId, Actor actor)
    {
        await RoomAsync(homeId, roomId, actor);
        if (!await db.RoomBlocks.AnyAsync(b => b.Id == blockId && b.RoomId == roomId))
            throw new BookingException("Không tìm thấy khóa ngày.");
    }

    public async Task ImageAsync(int homeId, int imageId, Actor actor)
    {
        await HomeAsync(homeId, actor);
        if (!await db.HomestayImages.AnyAsync(i => i.Id == imageId && i.HomestayId == homeId))
            throw new BookingException("Không tìm thấy ảnh.");
    }

    public async Task DocumentAsync(int id, Actor actor, int? homeId = null)
    {
        var homes = (await content.OwnerHomestaysAsync(actor)).Select(h => h.Id).ToArray();
        if (!await db.VerificationDocuments.AnyAsync(d => d.Id == id
            && (homeId == null || d.HomestayId == homeId)
            && (d.HomestayId != null && homes.Contains(d.HomestayId.Value)
                || homeId == null && d.HomestayId == null && d.OwnerRegistration != null && d.OwnerRegistration.Owner.Person.Account != null
                    && d.OwnerRegistration.Owner.Person.Account.Id == actor.AccountId)))
            throw new BookingException("Không tìm thấy giấy tờ.");
    }

    public async Task ReviewAsync(int id, Actor actor)
    {
        var homes = (await content.OwnerHomestaysAsync(actor)).Select(h => h.Id).ToArray();
        if (!await db.Reviews.AnyAsync(r => r.Id == id && homes.Contains(r.HomestayId)))
            throw new BookingException("Không tìm thấy đánh giá.");
    }

    public async Task SettlementAsync(int id, Actor actor)
    {
        var homes = (await content.OwnerHomestaysAsync(actor)).Select(h => h.Id).ToArray();
        if (!await db.Settlements.AnyAsync(s => s.Id == id && homes.Contains(s.HomestayId) && s.Status != SettlementStatus.Draft))
            throw new BookingException("Không tìm thấy bảng đối soát.");
    }
}
