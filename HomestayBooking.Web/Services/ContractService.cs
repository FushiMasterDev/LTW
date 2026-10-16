using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record CreateContractRequest(int HomestayId, string ContractNo, DateOnly StartDate,
    DateOnly EndDate, decimal CommissionRate);

/// <summary>FR-19, BR-02: quản lý hợp đồng; không thay snapshot hoa hồng của đơn đã nhận.</summary>
public class ContractService(ApplicationDbContext db, NotificationService notifications, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static object State(Contract c) => new { c.ContractNo, c.HomestayId, c.StartDate, c.EndDate,
        c.CommissionRate, Status = c.Status.ToString(), c.SignedByEmployeeId, c.UpdatedAt };

    public Task<Contract> CreateAsync(CreateContractRequest input, Actor actor) => WriteAsync(async () =>
    {
        var employee = await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        var today = VietnamTime.Today(clock);
        var homestay = await db.Homestays.SingleOrDefaultAsync(h => h.Id == input.HomestayId)
            ?? throw new BookingException("Không tìm thấy homestay.");
        if (homestay.Status == HomestayStatus.Ended) throw new BookingException("Homestay đã chấm dứt hợp tác.");
        if (!await db.OwnerRegistrations.AnyAsync(r => r.OwnerId == homestay.OwnerId && r.Status == OwnerRegistrationStatus.Approved))
            throw new BookingException("Chủ homestay chưa được duyệt.");
        if (input.CommissionRate is < 0.10m or > 0.15m) throw new BookingException("Hoa hồng phải từ 10% đến 15%.");
        if (input.StartDate > today || input.EndDate <= today || input.EndDate <= input.StartDate)
            throw new BookingException("Ngày hợp đồng không hợp lệ; chưa hỗ trợ hợp đồng tương lai.");
        var number = input.ContractNo.Trim();
        if (number.Length is 0 or > 30) throw new BookingException("Số hợp đồng phải có từ 1 đến 30 ký tự.");
        if (await db.Contracts.AnyAsync(c => c.ContractNo == number)) throw new BookingException("Số hợp đồng đã tồn tại.");
        if (await db.Contracts.AnyAsync(c => c.HomestayId == homestay.Id && c.Status == ContractStatus.Active))
            throw new BookingException("Homestay đã có hợp đồng Active.");
        var contract = new Contract { HomestayId = homestay.Id, ContractNo = number,
            StartDate = input.StartDate, EndDate = input.EndDate, CommissionRate = input.CommissionRate,
            SignedByEmployeeId = employee.Id, CreatedAt = Now, UpdatedAt = Now };
        db.Contracts.Add(contract);
        // Id thật được sinh trước audit; hai lần lưu vẫn nằm trong transaction của WriteAsync.
        await db.SaveChangesAsync();
        PartnerServiceSupport.Audit(db, clock, actor, "Contracts", contract.Id.ToString(), null, State(contract), AuditAction.Insert);
        return contract;
    });

    public Task<Contract> ExtendAsync(int id, DateOnly endDate, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        var contract = await ActiveAsync(id);
        if (endDate <= contract.EndDate) throw new BookingException("Ngày gia hạn phải sau ngày kết thúc hiện tại.");
        var old = State(contract);
        contract.EndDate = endDate; contract.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "Contracts", id.ToString(), old, State(contract));
        return contract;
    });

    public Task<Contract> TerminateAsync(int id, string reason, Actor actor) => WriteAsync(async () =>
    {
        await PartnerServiceSupport.EmployeeAsync(db, actor, EmployeePosition.Manager, EmployeePosition.Administrator);
        if (string.IsNullOrWhiteSpace(reason)) throw new BookingException("Vui lòng nhập lý do chấm dứt hợp đồng.");
        var contract = await ActiveAsync(id);
        var holds = Enum.GetValues<BookingStatus>().Where(s => s.HoldsRoom() && s != BookingStatus.Completed).ToArray();
        var codes = await db.Bookings.Where(b => b.HomestayId == contract.HomestayId && holds.Contains(b.Status))
            .OrderBy(b => b.Code).Select(b => b.Code).ToListAsync();
        if (codes.Count > 0) throw new BookingException("Chưa thể chấm dứt vì còn đơn giữ lịch phòng: {0}.", string.Join(", ", codes));
        var old = State(contract);
        contract.Status = ContractStatus.Terminated; contract.UpdatedAt = Now;
        PartnerServiceSupport.Audit(db, clock, actor, "Contracts", id.ToString(), old, new { Contract = State(contract), Reason = reason.Trim() });
        return contract;
    });

    private async Task<Contract> ActiveAsync(int id)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw new BookingException("Không tìm thấy hợp đồng.");
        if (contract.Status != ContractStatus.Active) throw new BookingException("Chỉ xử lý hợp đồng Active.");
        return contract;
    }

    public Task<int> ExpireAsync() => WriteAsync(async () =>
    {
        var today = VietnamTime.Today(clock);
        var contracts = await db.Contracts.Where(c => c.Status == ContractStatus.Active && c.EndDate < today).ToListAsync();
        var managers = await PartnerServiceSupport.Managers(db).ToListAsync();
        foreach (var contract in contracts)
        {
            var old = State(contract);
            contract.Status = ContractStatus.Expired; contract.UpdatedAt = Now;
            PartnerServiceSupport.Audit(db, clock, Actor.System, "Contracts", contract.Id.ToString(), old, State(contract));
            PartnerServiceSupport.Notify(db, notifications, clock, managers, $"Hợp đồng {contract.ContractNo} đã hết hạn.",
                $"/Staff/Contracts/{contract.Id}");
        }
        return contracts.Count;
    });

    private async Task<T> WriteAsync<T>(Func<Task<T>> action)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        try
        {
            var result = await action(); await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            var message = ex.InnerException?.Message ?? ex.Message;
            if (ex is DbUpdateException && (message.Contains("IX_Contracts_HomestayId_Active") || message.Contains("UNIQUE constraint failed: Contracts.HomestayId")))
                throw new BookingException("Homestay đã có hợp đồng Active.");
            if (ex is DbUpdateException && (message.Contains("IX_Contracts_ContractNo") || message.Contains("UNIQUE constraint failed: Contracts.ContractNo")))
                throw new BookingException("Số hợp đồng đã tồn tại.");
            throw;
        }
    }
}
