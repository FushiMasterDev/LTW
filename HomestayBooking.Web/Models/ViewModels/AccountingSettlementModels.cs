using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Read models for the accounting workspace; all monetary values come from services.</summary>
public record AccountingSettlementList(PaginatedList<Settlement> Rows, SettlementPeriod LatestPeriod);
public record AccountingSettlementDetails(SettlementDetails Details, IReadOnlyList<TimelineItem> History);
public record AccountingSettlementSummary(int DraftCount, decimal DraftAmount, int AwaitingPaymentCount, decimal AwaitingPaymentAmount);
