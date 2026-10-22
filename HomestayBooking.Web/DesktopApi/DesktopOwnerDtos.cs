using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using static HomestayBooking.Web.DesktopApi.DesktopMapping;

namespace HomestayBooking.Web.DesktopApi;

public record OwnerLoginRequest([Required, StringLength(50)] string PhoneNumber, [Required, StringLength(256)] string Password);
public record OwnerAreaDto(int Id, string Name, string AreaGroup);
public record OwnerDto(int Id, string? FullName, string PhoneNumber);
public record OwnerLoginResponse(string AccessToken, DateTime ExpiresAt, OwnerDto Owner);
public record OwnerHomestayDto(int Id, string Name, string AreaName, string Status, string? CoverUrl, int RoomCount);
public record OwnerDashboardDto(string? RegistrationStatus, string? ReviewNote, DesktopPage<OwnerHomestayDto> Homestays,
    int? ArrivalsToday, int? PendingConfirmation, int? OverdueConfirmation);
public record OwnerScheduleItemDto(int BookingId, string Code, int HomestayId, string HomestayName, DateOnly CheckIn, DateOnly CheckOut,
    string ContactName, string ContactPhone, int Guests, string RoomCode, string? SpecialRequests);
public record OwnerScheduleDto(string Period, DateOnly From, DateOnly Through, IReadOnlyList<OwnerScheduleItemDto> Arrivals,
    IReadOnlyList<OwnerScheduleItemDto> Departures);
public record OwnerBookingRowDto(int Id, string Code, string Status, string ContactName, string ContactPhone, string HomestayName,
    string RoomTypeName, string RoomCode, DateOnly CheckIn, DateOnly CheckOut, int Guests, decimal TotalAmount, DateTime? ConfirmDeadlineAt);
public record OwnerBookingDto(int Id, string Code, string Status, string ContactName, string ContactPhone, int HomestayId, string HomestayName,
    int RoomTypeId, string RoomTypeName, int RoomId, string RoomCode, DateOnly CheckIn, DateOnly CheckOut, int Guests,
    decimal TotalAmount, decimal DepositAmount, DateTime? ConfirmDeadlineAt, string? SpecialRequests, string? CancelReason);
public record OwnerRoomOptionDto(int Id, string RoomCode);
public record OwnerChangeDto(int Id, string RequestType, string Status, DateOnly? OldCheckIn, DateOnly? OldCheckOut, string? OldRoomType,
    DateOnly? NewCheckIn, DateOnly? NewCheckOut, string? NewRoomType, decimal? PriceDifference, decimal? RefundAmount,
    decimal? ChangeFee, decimal? DepositCarriedOver, decimal? AdditionalDeposit, decimal? NewTotalAmount,
    DateTime CreatedAt, DateTime? DecidedAt, string? DecisionNote, string? RejectionReason, bool IsLegacy);
public record OwnerBookingDetailsDto(OwnerBookingDto Booking, IReadOnlyList<OwnerRoomOptionDto> FreeRooms, DateOnly Today,
    IReadOnlyList<BookingNightDto> Nights, IReadOnlyList<BookingHistoryDto> History, IReadOnlyList<OwnerChangeDto> Changes);
public record OwnerConfirmRequest(int? RoomId = null);
public record OwnerCalendarNightDto(int RoomId, string RoomCode, DateOnly Date, string State, string? Code, string? BookingStatus, string? BlockReason);
public record OwnerCalendarDto(string Month, int? HomestayId, int? RoomId, DateOnly Today, IReadOnlyList<DateOnly> Days,
    IReadOnlyList<OwnerRoomOptionDto> Rooms, IReadOnlyList<OwnerCalendarNightDto> Nights);
public record OwnerBlockRequest(DateOnly Start, DateOnly End,
    [property: JsonConverter(typeof(JsonStringEnumConverter<RoomBlockReason>))] [EnumDataType(typeof(RoomBlockReason))] RoomBlockReason Reason = RoomBlockReason.OtherChannel,
    [StringLength(255)] string? Note = null);
public record OwnerBlockDto(int Id, int RoomId, DateOnly Start, DateOnly End, string Reason, string? Note);
public record OwnerContentRequest([Required, StringLength(200)] string Name, string? DescriptionHtml, string? CheckinGuideHtml,
    [Required] string ContactPhone, TimeOnly CheckinTime, TimeOnly CheckoutTime);
public record OwnerLocationRequest([Required, StringLength(300)] string Address, int AreaId, decimal? Latitude, decimal? Longitude);
public record OwnerAmenitiesRequest([Required] int[] AmenityIds);
public record OwnerTypeRequest([Required, StringLength(100)] string Name, int CategoryId, int MaxGuests, int? AreaM2, string? Description);
public record OwnerTypeCreateRequest([Required, StringLength(100)] string Name, int CategoryId, int MaxGuests, int? AreaM2, string? Description,
    decimal WeekdayPrice, decimal WeekendPrice, decimal HolidayPrice);
public record OwnerCodeRequest([Required, StringLength(20)] string Code);
public record OwnerActiveRequest(bool Active);
public record OwnerPriceRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter<DayType>))] [EnumDataType(typeof(DayType))] DayType DayType,
    DateOnly EffectiveFrom, decimal Price);
public record OwnerPriceDto(int Id, string DayType, decimal Price, DateOnly EffectiveFrom);
public record OwnerRoomDto(int Id, string RoomCode, bool IsActive);
public record OwnerRoomTypeDto(int Id, string Name, int CategoryId, string CategoryName, int MaxGuests, int? AreaM2, string? Description,
    bool IsActive, IReadOnlyList<OwnerRoomDto> Rooms, IReadOnlyList<OwnerPriceDto> CurrentPrices, IReadOnlyList<OwnerPriceDto> UpcomingPrices);
public record OwnerImageDto(int Id, string Url, int? RoomTypeId, int SortOrder, bool IsCover);
public record OwnerImageRequest(int? TypeId, int SortOrder);
public record OwnerRuleDto(int MinDaysBefore, int RefundPercent);
public record OwnerPolicyDto(int Id, string Scope, string Name, IReadOnlyList<OwnerRuleDto> Rules);
public record OwnerPolicyRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter<PolicyScope>))] [EnumDataType(typeof(PolicyScope))] PolicyScope Scope,
    [Required, StringLength(100)] string Name, [Required] List<OwnerRuleDto> Rules);
public record OwnerDocumentDto(int Id, string Type, string? OriginalFileName, DateOnly? ExpiryDate, DateTime CreatedAt, string FileUrl);
public record OwnerAmenityDto(int Id, string Name, string? Icon, bool Selected);
public record OwnerEditorDto(int Id, string Name, string Address, int AreaId, string AreaName, decimal? Latitude, decimal? Longitude,
    string? DescriptionHtml, string? CheckinGuideHtml, string? ContactPhone, TimeOnly CheckinTime, TimeOnly CheckoutTime, string Status,
    IReadOnlyList<int> AmenityIds, IReadOnlyList<OwnerRoomTypeDto> RoomTypes, IReadOnlyList<OwnerImageDto> Images,
    IReadOnlyList<OwnerPolicyDto> Policies, IReadOnlyList<OwnerDocumentDto> Documents, IReadOnlyList<OwnerAmenityDto> Amenities,
    IReadOnlyList<CategoryDto> Categories);
public record OwnerWriteResult(int Id, string? Status = null, IReadOnlyList<string>? UpcomingBookingCodes = null);
public record OwnerReplyRequest([Required, StringLength(1000)] string Reply);
public record OwnerReviewDto(int Id, int BookingId, string BookingCode, int HomestayId, string? CustomerName, int Rating, string? Comment,
    string? OwnerReply, DateTime CreatedAt, DateTime? RepliedAt);
public record OwnerReviewsDto(int? HomestayId, DesktopPage<OwnerReviewDto> Reviews, double? Average, int Count);
public record OwnerSettlementDto(int Id, int HomestayId, string? HomestayName, int Year, int Month, int PeriodNo, string Status,
    decimal TotalRoomAmount, decimal TotalRetained, decimal TotalCommission, decimal PayoutAmount,
    DateTime? SentAt, DateTime? ConfirmedAt, DateTime? PaidAt, string? PaidBankRef, DateTime CreatedAt, DateTime UpdatedAt);
public record OwnerSettlementLineDto(int Id, int BookingId, string BookingCode, string ItemType, decimal RoomAmount, decimal DepositAmount,
    decimal RefundAmount, decimal RetainedAmount, decimal CommissionRate, decimal CommissionAmount, decimal PayoutAmount,
    decimal DiscountAmount, decimal FujiNetRevenue);
public record OwnerCompensationDto(int Id, string BookingCode, decimal Amount, string Status);
public record OwnerSettlementDetailsDto(OwnerSettlementDto Settlement, IReadOnlyList<OwnerSettlementLineDto> Lines,
    IReadOnlyList<string> UnclosedBookingCodes, IReadOnlyList<OwnerCompensationDto> Compensations, IReadOnlyList<AuditDto> History);
public record OwnerNoteRequest([Required, StringLength(500)] string Note);

public static class DesktopOwnerMapping
{
    public static OwnerHomestayDto Home(OwnerHomestaySummary h) => new(h.Id, h.Name, h.AreaName, h.Status.ToString(), h.CoverUrl, h.RoomCount);
    public static OwnerScheduleItemDto Schedule(OwnerScheduleItem s) => new(s.BookingId, s.Code, s.HomestayId, s.HomestayName, s.CheckIn, s.CheckOut,
        s.ContactName, s.ContactPhone, s.Guests, s.RoomCode, s.SpecialRequests);
    public static OwnerPriceDto Price(HomestayEditorPrice p) => new(p.Id, p.DayType.ToString(), p.Price, p.EffectiveFrom);
    public static OwnerDocumentDto Document(HomestayEditorDocument d, string pathBase) => new(d.Id, d.Type.ToString(), d.OriginalFileName,
        d.ExpiryDate, Utc(d.CreatedAt), $"{pathBase}/api/desktop/owner/documents/{d.Id}/file");
    public static OwnerEditorDto Editor(HomestayEditor h, string pathBase) => new(h.Id, h.Name, h.Address, h.AreaId, h.AreaName, h.Latitude, h.Longitude,
        h.DescriptionHtml, h.CheckinGuideHtml, h.ContactPhone, h.CheckinTime, h.CheckoutTime, h.Status.ToString(), h.AmenityIds,
        h.RoomTypes.Select(t => new OwnerRoomTypeDto(t.Id, t.Name, t.CategoryId, t.CategoryName, t.MaxGuests, t.AreaM2, t.Description, t.IsActive,
            t.Rooms.Select(r => new OwnerRoomDto(r.Id, r.RoomCode, r.IsActive)).ToList(), t.CurrentPrices.Select(Price).ToList(), t.UpcomingPrices.Select(Price).ToList())).ToList(),
        h.Images.Select(i => new OwnerImageDto(i.Id, i.Url, i.RoomTypeId, i.SortOrder, i.IsCover)).ToList(),
        h.Policies.Select(p => new OwnerPolicyDto(p.Id, p.Scope.ToString(), p.Name, p.Rules.Select(r => new OwnerRuleDto(r.MinDaysBefore, r.RefundPercent)).ToList())).ToList(),
        h.Documents.Select(d => Document(d, pathBase)).ToList(), h.Amenities.Select(a => new OwnerAmenityDto(a.Id, a.Name, a.Icon, a.Selected)).ToList(),
        h.Categories.Select(c => new CategoryDto(c.Id, c.Name)).ToList());
    public static OwnerReviewDto Review(Review r) => new(r.Id, r.BookingId, r.Booking?.Code ?? "", r.HomestayId, r.Booking?.Customer.Person.FullName,
        r.Rating, r.Comment, r.OwnerReply, Utc(r.CreatedAt), Utc(r.RepliedAt));
    public static OwnerSettlementDto Settlement(Settlement s) => new(s.Id, s.HomestayId, s.Homestay?.Name, s.Year, s.Month, s.PeriodNo, s.Status.ToString(),
        s.TotalRoomAmount, s.TotalRetained, s.TotalCommission, s.PayoutAmount, Utc(s.SentAt), Utc(s.ConfirmedAt), Utc(s.PaidAt), s.PaidBankRef,
        Utc(s.CreatedAt), Utc(s.UpdatedAt));
}

public sealed class OwnerImageUpload
{
    [Required] public IFormFile File { get; set; } = null!;
    public int? TypeId { get; set; }
    public bool Cover { get; set; }
    public int SortOrder { get; set; }
}
public sealed class OwnerDocumentUpload
{
    [Required] public IFormFile File { get; set; } = null!;
    [EnumDataType(typeof(DocumentType))] public DocumentType Type { get; set; }
    public DateOnly? Expiry { get; set; }
}
