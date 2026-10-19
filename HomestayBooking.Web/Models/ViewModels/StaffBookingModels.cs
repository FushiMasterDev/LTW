using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
namespace HomestayBooking.Web.Models.ViewModels;
public record StaffBookingsModel(StaffBookingQuery Query, PaginatedList<Booking> Bookings, IReadOnlyList<BookingAssignee> Assignees)
{ public IReadOnlyList<StaffHomestayOption> Homestays { get; init; } = []; }
public record StaffBookingModel(StaffBookingDetails Details, IReadOnlyList<BookingAssignee> Assignees, IReadOnlyList<AvailableRoom> FreeRooms, int? EmployeeId, DateOnly Today);
public class StaffCreateForm
{
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    [Required, Display(Name="Số điện thoại")] public string Phone { get; set; } = "";
    [Required, StringLength(100), Display(Name="Họ tên khách")] public string FullName { get; set; } = "";
    [EmailAddress, StringLength(256), Display(Name="Email")] public string? Email { get; set; }
    [Display(Name="Kênh liên hệ")] public SourceChannel Channel { get; set; } = SourceChannel.Phone;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    [Range(1,20), Display(Name="Số khách")] public int Guests { get; set; } = 2;
    [Display(Name="Khu vực")] public int? AreaId { get; set; }
    [Display(Name="Loại phòng")] public int? RoomTypeId { get; set; }
    [StringLength(1000), Display(Name="Yêu cầu đặc biệt")] public string? SpecialRequests { get; set; }
    public IReadOnlyList<CustomerInfo> Customers { get; set; } = [];
    public IReadOnlyList<AvailabilityOffer> Offers { get; set; } = [];
    public IReadOnlyList<AreaOption> Areas { get; set; } = [];
    public PriceQuote? Quote { get; set; }
}
