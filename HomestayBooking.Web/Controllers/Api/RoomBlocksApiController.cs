using System.Security.Claims;
using System.Text.Json.Serialization;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.Controllers.Api;

/// <summary>
/// Chủ homestay khóa/mở ngày của một phòng cho khách từ kênh khác (FR-14). RoomBlockService tự kiểm quyền sở hữu phòng
/// và chặn trùng đơn/khóa khác. POST/DELETE phải gửi token chống giả mạo qua header <c>RequestVerificationToken</c>.
/// </summary>
[ApiController]
[Authorize(Policy = AppPolicies.ApprovedOwner)]
[Route("api/rooms/{roomId:int}/blocks")]
[Produces("application/json")]
public class RoomBlocksApiController(ApplicationDbContext db, RoomBlockService blocks, IStringLocalizer<SharedResource> L) : ControllerBase
{
    /// <param name="End">Ngày kết thúc, không tính là đêm khóa (như ngày trả phòng).</param>
    /// <param name="Reason">OtherChannel (mặc định) hoặc Maintenance.</param>
    public record CreateBlockRequest(DateOnly Start, DateOnly End,
        [property: JsonConverter(typeof(JsonStringEnumConverter<RoomBlockReason>))] RoomBlockReason Reason = RoomBlockReason.OtherChannel,
        string? Note = null);

    /// <param name="Reason">OtherChannel (khách kênh khác) hoặc Maintenance (bảo trì).</param>
    public record BlockResult(int Id, int RoomId, DateOnly Start, DateOnly End, string Reason, string? Note);

    public record ConflictProblem(string Detail, IReadOnlyList<RoomNightConflict> Conflicts);

    private Actor CurrentActor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>Khóa các đêm [start, end) của phòng.</summary>
    /// <response code="201">Đã khóa.</response>
    /// <response code="400">Khoảng ngày, lý do hoặc ghi chú không hợp lệ (không khóa ngày quá khứ), hoặc thiếu token.</response>
    /// <response code="404">Phòng không thuộc homestay của bạn.</response>
    /// <response code="409">Trùng đêm đã có đơn hoặc đã khóa; trả danh sách đêm trùng.</response>
    [HttpPost]
    [ProducesResponseType<BlockResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ConflictProblem>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlockResult>> Create(int roomId, [FromBody] CreateBlockRequest request)
    {
        if (!await OwnsRoomAsync(roomId))
        {
            return Problem(detail: L["Không tìm thấy phòng."].Value, statusCode: StatusCodes.Status404NotFound);
        }
        try
        {
            var block = await blocks.CreateAsync(roomId, request.Start, request.End, request.Reason, request.Note, CurrentActor);
            return Created($"{Request.PathBase}/api/rooms/{roomId}/blocks/{block.Id}", ToResult(block));
        }
        catch (RoomBlockConflictException ex)
        {
            return Conflict(new ConflictProblem(L["Lịch phòng bị trùng: {0}. Vui lòng chọn khoảng ngày khác.",
                string.Join("; ", ex.Conflicts.Select(c => $"{c.Date:dd/MM/yyyy}: {c.Code}"))].Value, ex.Conflicts));
        }
        catch (BookingException ex)
        {
            return Problem(detail: L[ex.ResourceKey, ex.Arguments].Value, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Mở khóa một lần khóa ngày (phần đêm từ hôm nay trở đi).</summary>
    /// <response code="204">Đã mở khóa.</response>
    /// <response code="400">Khóa đã kết thúc, hoặc thiếu token.</response>
    /// <response code="404">Không có khóa này trên phòng thuộc homestay của bạn.</response>
    [HttpDelete("{blockId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int roomId, int blockId)
    {
        if (!await OwnsRoomAsync(roomId) || !await db.RoomBlocks.AnyAsync(b => b.Id == blockId && b.RoomId == roomId))
        {
            return Problem(detail: L["Không tìm thấy khóa ngày."].Value, statusCode: StatusCodes.Status404NotFound);
        }
        try
        {
            await blocks.UnlockAsync(blockId, CurrentActor);
            return NoContent();
        }
        catch (BookingException ex)
        {
            return Problem(detail: L[ex.ResourceKey, ex.Arguments].Value, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Phòng thuộc homestay của chủ đang đăng nhập; phòng của chủ khác trả 404 như Owner/Calendar.</summary>
    private Task<bool> OwnsRoomAsync(int roomId)
    {
        var personId = User.PersonId();
        return db.Rooms.AnyAsync(r => r.Id == roomId && r.RoomType.Homestay.Owner.PersonId == personId);
    }

    private static BlockResult ToResult(RoomBlock b) => new(b.Id, b.RoomId, b.StartDate, b.EndDate, b.Reason.ToString(), b.Note);
}
