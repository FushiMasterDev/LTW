using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HomestayBooking.Web.Models.ViewModels;

public class ChangeRequestForm
{
    public ChangeRequestType RequestType { get; set; } = ChangeRequestType.ChangeDates;
    public DateOnly? NewCheckIn { get; set; }
    public DateOnly? NewCheckOut { get; set; }
    public int? NewRoomTypeId { get; set; }
    public string? PreviewToken { get; set; }
    [BindNever, ValidateNever] public Booking Booking { get; set; } = null!;
    [BindNever, ValidateNever] public List<ChangeRoomOption> RoomTypes { get; set; } = [];
    [BindNever, ValidateNever] public BookingChangeQuote? Quote { get; set; }
    [BindNever, ValidateNever] public bool IsStaff { get; set; }

    public ChangeBookingRequest Input(int bookingId) => new(bookingId, RequestType,
        RequestType is ChangeRequestType.ChangeDates or ChangeRequestType.ChangeDatesAndRoomType ? NewCheckIn : null,
        RequestType is ChangeRequestType.ChangeDates or ChangeRequestType.ChangeDatesAndRoomType ? NewCheckOut : null,
        RequestType is ChangeRequestType.ChangeRoomType or ChangeRequestType.ChangeDatesAndRoomType ? NewRoomTypeId : null);
}

public record ChangeRoomOption(int Id, string Name, int MaxGuests);

public record ChangeRequestHistoryRow(BookingChangeRequest Request, DateOnly? OldCheckIn, DateOnly? OldCheckOut,
    string? OldRoomType, string? NewRoomType)
{
    public bool IsLegacy => Request.RequestedByAccountId is null || Request.ChangeFee is null
        || Request.DepositCarriedOver is null || Request.AdditionalDeposit is null || Request.NewTotalAmount is null
        || (Request.RequestType == ChangeRequestType.Cancel && Request.RefundAmount is null);
}

public record CustomerChangeRequests(List<ChangeRequestHistoryRow> History, bool CanChange,
    int? PaymentRequestId, string? QrUrl, DateTime? PaymentDeadline, bool HasSubmittedPayment);

public class ChangeRequestDecisionForm
{
    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [StringLength(500, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Ghi chú / lý do")]
    public string Note { get; set; } = string.Empty;
}
