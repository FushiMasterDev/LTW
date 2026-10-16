using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

/// <summary>Kiểm tra lại tác nhân trong CSDL và ghi audit trong transaction nghiệp vụ đối tác.</summary>
internal static class PartnerServiceSupport
{
    internal static async Task<Employee> EmployeeAsync(ApplicationDbContext db, Actor actor,
        params EmployeePosition[] positions)
    {
        var employee = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account)
            .SingleOrDefaultAsync(e => e.Id == actor.EmployeeId);
        if (actor.Type != ActorType.Employee || actor.AccountId is null
            || employee?.EmploymentStatus != EmploymentStatus.Active
            || employee.Person.Account?.Id != actor.AccountId || employee.Person.Account.Status != AccountStatus.Active
            || (positions.Length > 0 && !positions.Contains(employee.Position)))
            throw new BookingException("Bạn không có quyền thực hiện thao tác này.");
        return employee;
    }

    internal static void Audit(ApplicationDbContext db, TimeProvider clock, Actor actor, string table,
        string id, object? oldValues, object newValues, AuditAction action = AuditAction.Update) =>
        db.AuditLogs.Add(new AuditLog
        {
            AccountId = actor.AccountId, TableName = table, RecordId = id, Action = action,
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(new { Actor = actor.Type.ToString(), Values = newValues }),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });

    internal static IQueryable<string> Managers(ApplicationDbContext db) => db.Employees
        .Where(e => e.Position == EmployeePosition.Manager && e.EmploymentStatus == EmploymentStatus.Active
            && e.Person.Account != null && e.Person.Account.Status == AccountStatus.Active)
        .Select(e => e.Person.Account!.Id);

    internal static void Notify(ApplicationDbContext db, NotificationService notifications, TimeProvider clock,
        IEnumerable<string?> accounts, string message, string url)
    {
        if (message.Length > 500) throw new BookingException("Thông báo không được quá 500 ký tự.");
        notifications.Add(accounts, message, url);
        // NotificationService chỉ thêm entity; đồng bộ thời điểm với đồng hồ nghiệp vụ của service.
        foreach (var entry in db.ChangeTracker.Entries<Notification>().Where(e => e.State == EntityState.Added))
            entry.Entity.CreatedAt = clock.GetUtcNow().UtcDateTime;
    }
}
