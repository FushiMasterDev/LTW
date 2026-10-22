using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api.Desktop.Owner;

[Route("api/desktop/owner/homestays")]
public sealed class DesktopOwnerHomestaysController(HomestayContentService content, RoomService rooms, HomestayImageService images,
    HomestayPolicyService policies, HomestayDocumentService documents, OwnerResourceService access, AvailabilityService availability) : DesktopOwnerController
{
    /// <summary>Homestay của chủ hiện tại ở mọi trạng thái, lọc q và phân trang như web.</summary>
    /// <response code="200">Trang homestay.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<OwnerHomestayDto>>(200)]
    public async Task<ActionResult<DesktopPage<OwnerHomestayDto>>> List([FromQuery] string? q, [FromQuery] int? page) =>
        Ok(DesktopPage<OwnerHomestayDto>.Slice((await content.OwnerHomestaysAsync(Actor))
            .Where(h => string.IsNullOrWhiteSpace(q) || h.Name.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)).Select(DesktopOwnerMapping.Home), page));

    /// <summary>Toàn bộ dữ liệu editor: thông tin, phòng/giá, ảnh, tiện nghi, chính sách và link giấy tờ có kiểm quyền.</summary>
    /// <response code="200">Dữ liệu editor với enum chữ, timestamp UTC.</response>
    /// <response code="404">Homestay ngoài phạm vi.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<OwnerEditorDto>(200)]
    public async Task<ActionResult<OwnerEditorDto>> Details(int id) => Ok(DesktopOwnerMapping.Editor(await Editor(id), Request.PathBase));

    /// <summary>Danh sách khu vực cho form vị trí của chủ, cùng nguồn dữ liệu như web; phân trang.</summary>
    /// <response code="200">Trang khu vực.</response>
    [HttpGet("areas")]
    [ProducesResponseType<DesktopPage<OwnerAreaDto>>(200)]
    public async Task<ActionResult<DesktopPage<OwnerAreaDto>>> Areas([FromQuery] int? page) =>
        Ok(DesktopPage<OwnerAreaDto>.Slice((await availability.SearchAsync(new())).Areas.Select(a => new OwnerAreaDto(a.Id, a.Name, a.Group.ToString())), page));

    /// <summary>Sửa thông tin/HTML và giờ nhận trả; service chuẩn hóa SĐT, làm sạch HTML, giữ tiện nghi.</summary>
    /// <response code="200">Id homestay đã cập nhật.</response>
    /// <response code="400">Thông tin không hợp lệ hoặc không được sửa trạng thái hiện tại.</response>
    [HttpPut("{id:int}/content")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Content(int id, OwnerContentRequest request)
    {
        var h = await Editor(id);
        var saved = await content.UpdateAsync(id, new(request.Name, request.DescriptionHtml, request.CheckinGuideHtml, request.ContactPhone,
            request.CheckinTime, request.CheckoutTime, h.AmenityIds), Actor);
        return Ok(new OwnerWriteResult(saved.Id, saved.Status.ToString()));
    }

    /// <summary>Sửa vị trí qua service, trả trạng thái chờ duyệt và các đơn sắp tới cần liên hệ như web.</summary>
    /// <response code="200">Trạng thái và danh sách mã đơn bị ảnh hưởng.</response>
    /// <response code="400">Địa chỉ/khu vực/tọa độ không hợp lệ.</response>
    [HttpPut("{id:int}/location")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Location(int id, OwnerLocationRequest request)
    {
        await access.HomeAsync(id, Actor);
        var saved = await content.UpdateLocationAsync(id, new(request.Address, request.AreaId, request.Latitude, request.Longitude), Actor);
        return Ok(new OwnerWriteResult(saved.Homestay.Id, saved.Homestay.Status.ToString(), saved.UpcomingBookingCodes));
    }

    /// <summary>Thay tiện nghi, giữ nguyên thông tin homestay như form web.</summary>
    /// <response code="200">Id homestay.</response>
    /// <response code="400">Tiện nghi không tồn tại.</response>
    [HttpPut("{id:int}/amenities")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Amenities(int id, OwnerAmenitiesRequest request)
    {
        var h = await Editor(id);
        await content.UpdateAsync(id, new(h.Name, h.DescriptionHtml, h.CheckinGuideHtml, h.ContactPhone ?? "", h.CheckinTime, h.CheckoutTime, request.AmenityIds), Actor);
        return Ok(new OwnerWriteResult(id));
    }

    /// <summary>Tạo loại phòng cùng đủ ba giá đầu tiên qua RoomService.</summary>
    /// <response code="201">Id loại phòng mới.</response>
    /// <response code="400">Tên trùng, thông tin hoặc giá sai.</response>
    [HttpPost("{id:int}/room-types")]
    [ProducesResponseType<OwnerWriteResult>(201)]
    public async Task<ActionResult<OwnerWriteResult>> CreateType(int id, OwnerTypeCreateRequest request)
    {
        await access.HomeAsync(id, Actor);
        var t = await rooms.CreateTypeAsync(id, new(request.Name, request.CategoryId, request.MaxGuests, request.AreaM2, request.Description),
            new Dictionary<DayType, decimal> { [DayType.Weekday] = request.WeekdayPrice, [DayType.Weekend] = request.WeekendPrice, [DayType.Holiday] = request.HolidayPrice }, Actor);
        return CreatedAtAction(nameof(Details), new { id }, new OwnerWriteResult(t.Id));
    }

    /// <summary>Sửa loại phòng, chỉ cho loại thuộc đúng homestay trong URL.</summary>
    /// <response code="200">Id loại phòng.</response>
    /// <response code="400">Thông tin loại phòng sai.</response>
    [HttpPut("{id:int}/room-types/{typeId:int}")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> UpdateType(int id, int typeId, OwnerTypeRequest request)
    {
        await access.TypeAsync(id, typeId, Actor);
        await rooms.UpdateTypeAsync(typeId, new(request.Name, request.CategoryId, request.MaxGuests, request.AreaM2, request.Description), Actor);
        return Ok(new OwnerWriteResult(typeId));
    }

    /// <summary>Bật/tắt bán loại phòng, giữ kiểm phòng bán cuối cùng và cảnh báo đơn sắp tới.</summary>
    /// <response code="200">Các đơn sắp tới cần kiểm tra.</response>
    /// <response code="400">Không được tắt loại phòng cuối cùng.</response>
    [HttpPut("{id:int}/room-types/{typeId:int}/active")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> TypeActive(int id, int typeId, OwnerActiveRequest request)
    {
        await access.TypeAsync(id, typeId, Actor);
        var change = await rooms.SetTypeActiveAsync(typeId, request.Active, Actor);
        return Ok(new OwnerWriteResult(typeId, UpcomingBookingCodes: change.UpcomingBookingCodes));
    }

    /// <summary>Thêm giá hiệu lực tương lai hoặc sửa giá chưa hiệu lực qua RoomService.</summary>
    /// <response code="200">Giá đã lưu với dayType dạng chữ.</response>
    /// <response code="400">Ngày quá khứ, giá không dương hoặc đã hiệu lực.</response>
    [HttpPut("{id:int}/room-types/{typeId:int}/prices")]
    [ProducesResponseType<OwnerPriceDto>(200)]
    public async Task<ActionResult<OwnerPriceDto>> Price(int id, int typeId, OwnerPriceRequest request)
    {
        await access.TypeAsync(id, typeId, Actor);
        var p = await rooms.SetPriceAsync(typeId, request.DayType, request.EffectiveFrom, request.Price, Actor);
        return Ok(new OwnerPriceDto(p.Id, p.DayType.ToString(), p.Price, p.EffectiveFrom));
    }

    /// <summary>Thêm phòng vật lý vào loại phòng thuộc homestay của chủ.</summary>
    /// <response code="201">Phòng mới.</response>
    /// <response code="400">Mã phòng trùng hoặc không hợp lệ.</response>
    [HttpPost("{id:int}/room-types/{typeId:int}/rooms")]
    [ProducesResponseType<OwnerRoomDto>(201)]
    public async Task<ActionResult<OwnerRoomDto>> AddRoom(int id, int typeId, OwnerCodeRequest request)
    {
        await access.TypeAsync(id, typeId, Actor);
        var r = await rooms.AddRoomAsync(typeId, request.Code, Actor);
        return CreatedAtAction(nameof(Details), new { id }, new OwnerRoomDto(r.Id, r.RoomCode, r.IsActive));
    }

    /// <summary>Đổi mã phòng qua RoomService; giữ kiểm trùng.</summary>
    /// <response code="200">Phòng sau sửa.</response>
    /// <response code="400">Mã sai/trùng.</response>
    [HttpPut("{id:int}/rooms/{roomId:int}")]
    [ProducesResponseType<OwnerRoomDto>(200)]
    public async Task<ActionResult<OwnerRoomDto>> RenameRoom(int id, int roomId, OwnerCodeRequest request)
    {
        await access.RoomAsync(id, roomId, Actor);
        var r = await rooms.RenameRoomAsync(roomId, request.Code, Actor);
        return Ok(new OwnerRoomDto(r.Id, r.RoomCode, r.IsActive));
    }

    /// <summary>Bật/tắt phòng, giữ kiểm phòng bán cuối cùng và danh sách đơn cần kiểm tra.</summary>
    /// <response code="200">Các đơn sắp tới.</response>
    /// <response code="400">Không thể tắt phòng cuối cùng.</response>
    [HttpPut("{id:int}/rooms/{roomId:int}/active")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> RoomActive(int id, int roomId, OwnerActiveRequest request)
    {
        await access.RoomAsync(id, roomId, Actor);
        var change = await rooms.SetRoomActiveAsync(roomId, request.Active, Actor);
        return Ok(new OwnerWriteResult(roomId, UpcomingBookingCodes: change.UpcomingBookingCodes));
    }

    /// <summary>Upload ảnh JPG/PNG/WEBP tối đa 5MB, kiểm nội dung bằng validator dùng chung web.</summary>
    /// <response code="201">Ảnh mới, URL công khai.</response>
    /// <response code="400">File sai định dạng/dung lượng.</response>
    [HttpPost("{id:int}/images"), Consumes("multipart/form-data")]
    [ProducesResponseType<OwnerImageDto>(201)]
    public async Task<ActionResult<OwnerImageDto>> UploadImage(int id, [FromForm] OwnerImageUpload request)
    {
        await access.HomeAsync(id, Actor);
        if (request.TypeId is int t) await access.TypeAsync(id, t, Actor);
        var i = await images.UploadAsync(id, request.TypeId, request.File, request.Cover, request.SortOrder, Actor);
        return CreatedAtAction(nameof(Details), new { id }, new OwnerImageDto(i.Id, i.Url, i.RoomTypeId, i.SortOrder, i.IsCover));
    }

    /// <summary>Đổi loại phòng liên kết và thứ tự ảnh.</summary>
    /// <response code="200">Ảnh sau sửa.</response>
    /// <response code="400">Thứ tự không hợp lệ.</response>
    [HttpPut("{id:int}/images/{imageId:int}")]
    [ProducesResponseType<OwnerImageDto>(200)]
    public async Task<ActionResult<OwnerImageDto>> Image(int id, int imageId, OwnerImageRequest request)
    {
        await access.ImageAsync(id, imageId, Actor);
        if (request.TypeId is int t) await access.TypeAsync(id, t, Actor);
        var i = await images.UpdateAsync(imageId, request.TypeId, request.SortOrder, Actor);
        return Ok(new OwnerImageDto(i.Id, i.Url, i.RoomTypeId, i.SortOrder, i.IsCover));
    }

    /// <summary>Chọn ảnh bìa, dùng transaction và audit hiện có.</summary>
    /// <response code="200">Ảnh bìa.</response>
    [HttpPost("{id:int}/images/{imageId:int}/cover")]
    [ProducesResponseType<OwnerImageDto>(200)]
    public async Task<ActionResult<OwnerImageDto>> Cover(int id, int imageId)
    {
        await access.ImageAsync(id, imageId, Actor);
        var i = await images.SetCoverAsync(imageId, Actor);
        return Ok(new OwnerImageDto(i.Id, i.Url, i.RoomTypeId, i.SortOrder, i.IsCover));
    }

    /// <summary>Xóa ảnh qua service; không được xóa bìa duy nhất của homestay đang bán.</summary>
    /// <response code="204">Đã xóa.</response>
    /// <response code="400">Ảnh bìa đang được sử dụng.</response>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        await access.ImageAsync(id, imageId, Actor);
        await images.DeleteAsync(imageId, Actor);
        return NoContent();
    }

    /// <summary>Thay phiên bản chính sách hủy, giữ snapshot chính sách của đơn cũ.</summary>
    /// <response code="200">Id phiên bản chính sách mới.</response>
    /// <response code="400">Quy tắc hoặc scope không hợp lệ.</response>
    [HttpPut("{id:int}/policies")]
    [ProducesResponseType<OwnerWriteResult>(200)]
    public async Task<ActionResult<OwnerWriteResult>> Policy(int id, OwnerPolicyRequest request)
    {
        await access.HomeAsync(id, Actor);
        var p = await policies.ReplaceAsync(id, request.Scope, request.Name,
            request.Rules.Select(r => new CancellationRuleInput(r.MinDaysBefore, r.RefundPercent)).ToList(), Actor);
        return Ok(new OwnerWriteResult(p.Id));
    }

    /// <summary>Gỡ chính sách riêng theo scope qua service, không xóa lịch sử.</summary>
    /// <response code="204">Đã gỡ hoặc không có chính sách riêng.</response>
    /// <response code="400">Scope không hợp lệ.</response>
    [HttpDelete("{id:int}/policies/{scope}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemovePolicy(int id, PolicyScope scope)
    {
        await access.HomeAsync(id, Actor);
        await policies.RemoveAsync(id, scope, Actor);
        return NoContent();
    }

    /// <summary>Upload giấy tờ ảnh/PDF tối đa 5MB qua ReceiptFileStore; trả link riêng tư, có thể đưa về chờ duyệt.</summary>
    /// <response code="201">Metadata và link tải riêng tư; gọi lại editor để xem trạng thái homestay.</response>
    /// <response code="400">File, loại giấy tờ hoặc hạn không hợp lệ.</response>
    [HttpPost("{id:int}/documents"), Consumes("multipart/form-data")]
    [ProducesResponseType<OwnerDocumentDto>(201)]
    public async Task<ActionResult<OwnerDocumentDto>> UploadDocument(int id, [FromForm] OwnerDocumentUpload request)
    {
        await access.HomeAsync(id, Actor);
        var d = await documents.UploadAsync(id, request.Type, request.File, request.Expiry, Actor);
        var dto = DesktopOwnerMapping.Document(new(d.Id, d.DocumentType, d.OriginalFileName, d.ExpiryDate, d.CreatedAt), Request.PathBase);
        return Created(dto.FileUrl, dto);
    }

    /// <summary>Xóa giấy tờ nháp/chờ duyệt, giữ file lịch sử như web.</summary>
    /// <response code="204">Đã xóa.</response>
    /// <response code="400">Homestay không còn cho phép xóa.</response>
    [HttpDelete("{id:int}/documents/{documentId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteDocument(int id, int documentId)
    {
        await access.HomeAsync(id, Actor);
        await access.DocumentAsync(documentId, Actor, id);
        await documents.DeleteAsync(documentId, Actor);
        return NoContent();
    }

    private async Task<HomestayEditor> Editor(int id)
    {
        await access.HomeAsync(id, Actor);
        return await content.EditorAsync(id, Actor);
    }
}
