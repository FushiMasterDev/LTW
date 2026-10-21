using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomestayBooking.Web.Controllers.Api.Desktop;

[Route("api/desktop/customers")]
[Authorize(Policy = DesktopApiRegistration.StaffPolicy)]
public sealed class DesktopCustomersController(ApplicationDbContext db, CustomerService customers) : DesktopController(db)
{
    /// <summary>Tìm khách theo q, phân trang; CustomerService che giấy tờ theo quyền Tư vấn.</summary>
    /// <response code="200">Trang khách hàng.</response>
    /// <response code="400">Tham số không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType<DesktopPage<CustomerDto>>(200)]
    public async Task<ActionResult<DesktopPage<CustomerDto>>> Search([FromQuery] string? q, [FromQuery] int? page) =>
        Ok(DesktopPage<CustomerDto>.From(await customers.SearchAsync(q, page, await CurrentActorAsync()), DesktopMapping.Customer));

    /// <summary>Chi tiết khách, đơn và voucher dùng được qua CustomerService; không trả entity hồ sơ.</summary>
    /// <response code="200">Chi tiết khách.</response>
    /// <response code="404">Không có khách.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CustomerDetailsDto>(200)]
    public async Task<ActionResult<CustomerDetailsDto>> Details(int id)
    {
        var d = await customers.DetailsAsync(id, await CurrentActorAsync());
        return Ok(new CustomerDetailsDto(DesktopMapping.Customer(d.Customer),
            d.Bookings.Select(b => new CustomerBookingDto(b.Id, b.Code, b.Status.ToString(), b.CheckIn, b.CheckOut, b.TotalAmount)).ToList(),
            d.UsableVouchers.Select(v => new VoucherDto(v.Id, v.Code, v.Amount, v.Balance, v.ExpiresOn, v.Status.ToString())).ToList()));
    }

    /// <summary>Cập nhật khách qua CustomerService; giữ nguyên luật định danh và khách có tài khoản tự sửa tên/email.</summary>
    /// <response code="200">Thông tin sau sửa, giấy tờ vẫn che theo quyền.</response>
    /// <response code="400">Sửa trường không được phép hoặc dữ liệu sai.</response>
    /// <response code="404">Không có khách.</response>
    /// <response code="409">Xung đột cập nhật.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CustomerDto>(200)]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerUpdateRequest request) =>
        Ok(DesktopMapping.Customer(await customers.UpdateAsync(id, new(request.Note, request.FullName, request.Email, request.PhoneNumber), await CurrentActorAsync())));
}
