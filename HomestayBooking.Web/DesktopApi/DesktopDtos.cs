using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.DesktopApi;

public record DesktopPage<T>(IReadOnlyList<T> Items, int Page, int TotalPages, int TotalCount)
{
    public static DesktopPage<T> From<TSource>(PaginatedList<TSource> rows, Func<TSource, T> map) =>
        new(rows.Select(map).ToList(), rows.PageIndex, rows.TotalPages, rows.TotalCount);
    public static DesktopPage<T> Slice(IEnumerable<T> rows, int? requestedPage)
    {
        var all = rows.ToList();
        var page = Math.Clamp(requestedPage ?? 1, 1, Math.Max(1, (int)Math.Ceiling(all.Count / 10d)));
        var paginated = new PaginatedList<T>(all.Skip((page - 1) * 10).Take(10), all.Count, page, 10);
        return From(paginated, x => x);
    }
}

public record EmployeeDto(int Id, string Code, string? FullName, string Position);
public record LoginRequest([Required, StringLength(50)] string EmployeeCode, [Required, StringLength(256)] string Password);
/// <param name="ExpiresAt">Thời điểm UTC, hậu tố Z.</param>
public record LoginResponse(string AccessToken, DateTime ExpiresAt, EmployeeDto Employee);
public record AreaDto(int Id, string Name, string Province, string AreaGroup);
public record AmenityDto(int Id, string Name, string? Icon);
public record CategoryDto(int Id, string Name);
public record HolidayDto(DateOnly Date, string Name);
public record AreaRequest([Required, StringLength(100)] string Name, [Required, StringLength(100)] string Province,
    [property: JsonConverter(typeof(JsonStringEnumConverter<AreaGroup>))] [EnumDataType(typeof(AreaGroup))] AreaGroup AreaGroup = AreaGroup.InnerCity);
public record AmenityRequest([Required, StringLength(100)] string Name,
    [StringLength(50), RegularExpression(@"^bi-[a-z0-9-]+$")] string? Icon = null);
public record CategoryRequest([Required, StringLength(100)] string Name);
public record HolidayRequest([Required] DateOnly? Date, [Required, StringLength(100)] string Name);
public record HolidayUpdateRequest([Required, StringLength(100)] string Name);
public record BookingCreateRequest([Required] string Phone, [Required, StringLength(100)] string FullName, string? Email,
    [Range(1, int.MaxValue)] int RoomTypeId, DateOnly CheckIn, DateOnly CheckOut, [Range(1, int.MaxValue)] int Guests,
    string? SpecialRequests, [property: JsonConverter(typeof(JsonStringEnumConverter<SourceChannel>))] SourceChannel Channel,
    [Required] Guid? SubmissionId);
public record AssignRequest([Range(1, int.MaxValue)] int EmployeeId);
public record StatusRequest([property: JsonConverter(typeof(JsonStringEnumConverter<BookingStatus>))] BookingStatus Status,
    [Required, StringLength(500)] string Note, int? NewRoomId = null);
public record ReasonRequest([Required, StringLength(500)] string Reason);
public record BookingStatusDto(int Id, string Code, string Status);
/// <summary>Timestamp UTC; các ngày lưu trú là yyyy-MM-dd, không chuyển múi giờ.</summary>
public record BookingDto(int Id, string Code, int CustomerId, string ContactName, string ContactPhone, string SourceChannel,
    int HomestayId, string? HomestayName, int RoomTypeId, string? RoomTypeName, int RoomId, string? RoomCode,
    DateOnly CheckIn, DateOnly CheckOut, int Guests, string Status, decimal TotalAmount, decimal DepositAmount,
    decimal DiscountAmount, decimal AmountDue, int? AssignedEmployeeId, string? AssignedEmployeeName,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime HoldExpiresAt, string? SpecialRequests);
public record BookingNightDto(DateOnly Date, string DayType, decimal Price);
public record BookingHistoryDto(int Id, string? FromStatus, string ToStatus, string ActorType, string? ActorName, string? Note, DateTime ChangedAt);
public record BookingChangeDto(int Id, string RequestType, string Status, DateOnly? NewCheckIn, DateOnly? NewCheckOut,
    int? NewRoomTypeId, decimal? PriceDifference, decimal? RefundAmount, DateTime CreatedAt);
public record PaymentDto(int Id, string Type, string Method, decimal Amount, string Status, DateTime SubmittedAt,
    DateTime? ReviewedAt, string? RejectReason);
public record AuditDto(long Id, string Action, string? AccountName, string? OldValues, string? NewValues, DateTime CreatedAt);
public record BookingDetailsDto(BookingDto Booking, IReadOnlyList<BookingNightDto> Nights, IReadOnlyList<BookingHistoryDto> History,
    IReadOnlyList<BookingChangeDto> Changes, IReadOnlyList<PaymentDto> Payments, IReadOnlyList<AuditDto> Audit);
public record AssigneeDto(int EmployeeId, string EmployeeCode, string? FullName, string Position);
public record BookingHomestayDto(int Id, string Name);
/// <summary>ActiveRoomCount là số phòng đang kinh doanh, không phải tồn phòng theo ngày lưu trú.</summary>
public record BookingRoomTypeDto(int Id, string Name, int CategoryId, string CategoryName, int MaxGuests, int ActiveRoomCount);
public record AvailabilityDto(int HomestayId, string HomestayName, int AreaId, string AreaName, int RoomTypeId,
    string RoomTypeName, int CategoryId, int MaxGuests, int FreeRooms, int TotalRooms, decimal? TotalPrice, double? Rating, int ReviewCount);
public record CustomerDto(int Id, string? FullName, string PhoneNumber, string? Email, string? Note, string? CitizenId, string? StudentId, bool HasAccount);
public record CustomerUpdateRequest([StringLength(1000)] string? Note, string? FullName = null, string? Email = null, string? PhoneNumber = null);
public record CustomerBookingDto(int Id, string Code, string Status, DateOnly CheckIn, DateOnly CheckOut, decimal TotalAmount);
public record VoucherDto(int Id, string Code, decimal Amount, decimal Balance, DateOnly ExpiresOn, string Status);
public record CustomerDetailsDto(CustomerDto Customer, IReadOnlyList<CustomerBookingDto> Bookings, IReadOnlyList<VoucherDto> UsableVouchers);
public record ReceiptDto(int PaymentId, string BookingCode, string BookingStatus, string ContactName, string ContactPhone,
    string HomestayName, decimal Amount, decimal AmountDue, string? TransferContent, string Status, DateTime SubmittedAt,
    DateTime HoldExpiresAt, int PreviousRejections, string? ReviewedBy, DateTime? ReviewedAt, string? RejectReason,
    int? SettlementId, string FileUrl);

public static class DesktopMapping
{
    // SQL Server datetime2 materializes as Unspecified; stored values are UTC. Do not use ToUniversalTime (host-dependent).
    public static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
    public static BookingDto Booking(Booking b) => new(b.Id, b.Code, b.CustomerId, b.ContactName, b.ContactPhone, b.SourceChannel.ToString(),
        b.HomestayId, b.Homestay?.Name, b.RoomTypeId, b.RoomType?.Name, b.RoomId, b.Room?.RoomCode, b.CheckIn, b.CheckOut, b.Guests,
        b.Status.ToString(), b.TotalAmount, b.DepositAmount, b.DiscountAmount, b.AmountDue, b.AssignedEmployeeId,
        b.AssignedEmployee?.Person.FullName, Utc(b.CreatedAt), Utc(b.UpdatedAt), Utc(b.HoldExpiresAt), b.SpecialRequests);
    public static CustomerDto Customer(Services.CustomerInfo c) => new(c.Id, c.FullName, c.PhoneNumber, c.Email, c.Note, c.CitizenId, c.StudentId, c.HasAccount);
}
