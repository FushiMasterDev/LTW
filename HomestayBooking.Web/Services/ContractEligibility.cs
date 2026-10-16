using System.Linq.Expressions;
using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>D20: hợp đồng đang hiệu lực và bao phủ cả ngày trả phòng, không chỉ ngày đặt.</summary>
public static class ContractEligibility
{
    public static Expression<Func<Contract, bool>> Covers(DateOnly today, DateOnly checkOut) =>
        c => c.Status == ContractStatus.Active && c.StartDate <= today && c.EndDate >= checkOut;

    public static IQueryable<Contract> Covering(this IQueryable<Contract> contracts, DateOnly today, DateOnly checkOut) =>
        contracts.Where(Covers(today, checkOut));
}
