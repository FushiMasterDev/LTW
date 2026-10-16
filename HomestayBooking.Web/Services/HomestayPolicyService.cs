using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record CancellationRuleInput(int MinDaysBefore, int RefundPercent);

/// <summary>Thay phiên bản chính sách, không sửa quy tắc mà đơn cũ đang tham chiếu.</summary>
public class HomestayPolicyService(ApplicationDbContext db, HomestayAccessService access, TimeProvider clock)
{
    private static object State(CancellationPolicy p) => new { p.Id, p.HomestayId, p.Name, Scope = p.Scope.ToString(), p.IsActive,
        Rules = p.Rules.Select(r => new { r.Id, r.MinDaysBefore, r.RefundPercent }).OrderBy(r => r.MinDaysBefore).ToArray() };
    public Task<CancellationPolicy> ReplaceAsync(int homeId, PolicyScope scope, string name, IReadOnlyList<CancellationRuleInput> rules, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await access.AuthorizeAsync(homeId, actor);
        if (!Enum.IsDefined(scope) || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) throw new BookingException("Tên hoặc phạm vi chính sách không hợp lệ.");
        var ordered = rules.OrderBy(r => r.MinDaysBefore).ToArray();
        if (ordered.Length == 0 || ordered[0].MinDaysBefore != 0 || ordered.Any(r => r.MinDaysBefore < 0 || r.RefundPercent is < 0 or > 100)
            || ordered.Select(r => r.MinDaysBefore).Distinct().Count() != ordered.Length
            || ordered.Zip(ordered.Skip(1)).Any(pair => pair.First.RefundPercent > pair.Second.RefundPercent))
            throw new BookingException("Quy tắc hủy cần mốc 0, không trùng ngày, tỷ lệ 0–100 và không giảm khi hủy sớm hơn.");
        await DisableAsync(homeId, scope, actor); await db.SaveChangesAsync();
        var policy = new CancellationPolicy { HomestayId = homeId, Name = name.Trim(), Scope = scope, CreatedAt = clock.GetUtcNow().UtcDateTime };
        foreach (var r in ordered) policy.Rules.Add(new() { MinDaysBefore = r.MinDaysBefore, RefundPercent = r.RefundPercent });
        db.CancellationPolicies.Add(policy); await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "CancellationPolicies", policy.Id.ToString(), null, State(policy), AuditAction.Insert);
        foreach (var r in policy.Rules) PartnerServiceSupport.Audit(db, clock, actor, "CancellationRules", r.Id.ToString(), null,
            new { r.Id, r.PolicyId, r.MinDaysBefore, r.RefundPercent }, AuditAction.Insert);
        return policy;
    });
    public Task<int> RemoveAsync(int homeId, PolicyScope scope, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        await access.AuthorizeAsync(homeId, actor);
        if (!Enum.IsDefined(scope)) throw new BookingException("Phạm vi chính sách không hợp lệ.");
        return await DisableAsync(homeId, scope, actor);
    });
    private async Task<int> DisableAsync(int homeId, PolicyScope scope, Actor actor)
    {
        var policy = await db.CancellationPolicies.Include(p => p.Rules).SingleOrDefaultAsync(p => p.HomestayId == homeId && p.Scope == scope && p.IsActive);
        if (policy is null) return 0;
        var old = State(policy); policy.IsActive = false;
        PartnerServiceSupport.Audit(db, clock, actor, "CancellationPolicies", policy.Id.ToString(), old, State(policy)); return policy.Id;
    }
}
