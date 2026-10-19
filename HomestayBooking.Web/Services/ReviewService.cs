using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record HomestayRating(int Id, double Rating, int Count);
public record ReviewPage(PaginatedList<Review> Reviews, double? Average, int Count);

public static class ReviewRatings
{
    // Cùng một phép tính cho đánh giá công khai, tìm kiếm và homestay nổi bật; luôn bỏ đánh giá ẩn.
    public static IQueryable<HomestayRating> Summaries(IQueryable<Review> reviews) => reviews.Where(r => !r.IsHidden)
        .GroupBy(r => r.HomestayId).Select(g => new HomestayRating(g.Key, g.Average(r => (double)r.Rating), g.Count()));
}

/// <summary>BR-09: lưu bình luận/trả lời dưới dạng văn bản nguyên bản, bên hiển thị phải encode.</summary>
public class ReviewService(ApplicationDbContext db, NotificationService notifications, TimeProvider clock)
{
    private static string? Text(string? text, bool required = false)
    {
        text = text?.Trim();
        if (text?.Length > 1000 || required && string.IsNullOrWhiteSpace(text))
            throw new BookingException("Nội dung phải có từ 1 đến 1000 ký tự.");
        return string.IsNullOrEmpty(text) ? null : text;
    }
    private async Task<Booking> BookingAsync(int id) => await db.Bookings.Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
        .Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
        .SingleOrDefaultAsync(b => b.Id == id) ?? throw new BookingException("Không tìm thấy đơn đặt phòng.");

    public Task<Review> CreateAsync(int bookingId, int rating, string? comment, Actor actor) => WriteAsync(async () =>
    {
        var booking = await BookingAsync(bookingId);
        if (actor.Type != ActorType.Customer || actor.AccountId is null || booking.Customer.Person.Account?.Id != actor.AccountId
            || booking.Customer.Person.Account?.Status != AccountStatus.Active)
            throw new BookingException("Chỉ khách sở hữu đơn được đánh giá.");
        return await CreateCoreAsync(booking, rating, comment, actor);
    });

    public Task<Review> CreateByLookupAsync(string code, string phoneNumber, int rating, string? comment) => WriteAsync(async () =>
    {
        var phone = PhoneNumberNormalizer.ToE164(phoneNumber); code = code.Trim().ToUpperInvariant();
        var id = phone is null ? null : await db.Bookings.Where(b => b.Code == code
            && (b.ContactPhone == phone || b.Customer.Person.PhoneNumber == phone)).Select(b => (int?)b.Id).SingleOrDefaultAsync();
        if (id is null) throw new BookingException("Không tìm thấy đơn khớp với mã đơn và số điện thoại.");
        return await CreateCoreAsync(await BookingAsync(id.Value), rating, comment, new(ActorType.Customer));
    });

    private async Task<Review> CreateCoreAsync(Booking booking, int rating, string? comment, Actor actor)
    {
        if (booking.Status != BookingStatus.Completed) throw new BookingException("Chỉ đánh giá sau khi đơn đã hoàn thành.");
        if (rating is < 1 or > 5) throw new BookingException("Điểm đánh giá phải từ 1 đến 5.");
        if (await db.Reviews.AnyAsync(r => r.BookingId == booking.Id)) throw new BookingException("Đơn này đã được đánh giá.");
        var review = new Review { BookingId = booking.Id, HomestayId = booking.HomestayId, Rating = rating, Comment = Text(comment), CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.Reviews.Add(review); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Reviews", review.Id.ToString(), null, State(review), AuditAction.Insert);
        PartnerServiceSupport.Notify(db, notifications, clock, [booking.Homestay.Owner.Person.Account?.Id],
            $"Đơn {booking.Code} vừa có đánh giá {rating}/5. Vui lòng xem và phản hồi khách.", $"/Owner/Bookings/Details/{booking.Id}");
        return review;
    }

    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        try { return await HomestayWrite.RunAsync(db, action); }
        catch (DbUpdateException ex) when (ex.GetBaseException().Message.Contains("IX_Reviews_BookingId", StringComparison.Ordinal)
            || ex.GetBaseException().Message.Contains("Reviews.BookingId", StringComparison.Ordinal))
        { throw new BookingException("Đơn này đã được đánh giá."); }
    }
    private static object State(Review r) => new { r.Id, r.BookingId, r.HomestayId, r.Rating, r.Comment, r.OwnerReply, r.RepliedAt, r.IsHidden };

    public Task<Review> ReplyAsync(int id, string reply, Actor actor) => WriteAsync(async () =>
    {
        reply = Text(reply, required: true)!;
        var review = await db.Reviews.Include(r => r.Booking).ThenInclude(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(r => r.Id == id) ?? throw new BookingException("Không tìm thấy đánh giá.");
        if (actor.Type != ActorType.Owner || actor.AccountId is null || !await db.Homestays.AnyAsync(h => h.Id == review.HomestayId
            && h.Owner.Person.Account != null && h.Owner.Person.Account.Id == actor.AccountId && h.Owner.Person.Account.Status == AccountStatus.Active
            && h.Owner.Person.Employee == null)) throw new BookingException("Chỉ chủ homestay được trả lời đánh giá của mình.");
        var old = State(review); review.OwnerReply = reply; review.RepliedAt = clock.GetUtcNow().UtcDateTime;
        PartnerServiceSupport.Audit(db, clock, actor, "Reviews", id.ToString(), old, State(review));
        PartnerServiceSupport.Notify(db, notifications, clock, [review.Booking.Customer.Person.Account?.Id],
            $"Chủ homestay đã trả lời đánh giá của bạn cho đơn {review.Booking.Code}.", $"/Bookings/Details/{review.Booking.Code}");
        return review;
    });

    public Task<Review> SetHiddenAsync(int id, bool hidden, string reason, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        if (string.IsNullOrWhiteSpace(reason)) throw new BookingException("Vui lòng nhập lý do ẩn hoặc hiện đánh giá.");
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id) ?? throw new BookingException("Không tìm thấy đánh giá.");
        var old = State(review); review.IsHidden = hidden;
        PartnerServiceSupport.Audit(db, clock, actor, "Reviews", id.ToString(), old, new { Review = State(review), Reason = reason.Trim() });
        return review;
    });

    public async Task<ReviewPage> ListAsync(int homestayId, int? page = 1, bool includeHidden = false, Actor? actor = null)
    {
        if (includeHidden) await PartnerServiceSupport.EmployeeAsync(db, actor ?? Actor.System, EmployeePosition.Manager, EmployeePosition.Administrator);
        var rows = db.Reviews.AsNoTracking().Include(r => r.Booking).ThenInclude(b => b.Customer).ThenInclude(c => c.Person).Where(r => r.HomestayId == homestayId && (includeHidden || !r.IsHidden));
        var summary = await ReviewRatings.Summaries(db.Reviews.Where(r => r.HomestayId == homestayId)).SingleOrDefaultAsync();
        return new(await PaginatedList<Review>.CreateAsync(rows.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id), page), summary?.Rating, summary?.Count ?? 0);
    }
}
