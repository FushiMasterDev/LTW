using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Identity;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Services;

public record CustomerInfo(int Id, string? FullName, string PhoneNumber, string? Email, string? Note, string? CitizenId, string? StudentId, bool HasAccount);
public record CustomerBooking(int Id, string Code, BookingStatus Status, DateOnly CheckIn, DateOnly CheckOut, decimal TotalAmount);
public record CustomerDetails(CustomerInfo Customer, IReadOnlyList<CustomerBooking> Bookings, IReadOnlyList<Voucher> UsableVouchers);
public record CustomerUpdate(string? Note, string? FullName = null, string? Email = null, string? PhoneNumber = null);

/// <summary>FR-12: DTO che định danh cho Tư vấn, không trả entity Profile để lộ dữ liệu gốc.</summary>
public class CustomerService(ApplicationDbContext db, VoucherService vouchers, TimeProvider clock)
{
    private static CustomerInfo Info(Customer c, Employee employee) => new(c.Id, c.Person.FullName, c.Person.PhoneNumber, c.Person.Email,
        c.Note, Visible(c.Profile?.CitizenId, employee), Visible(c.Profile?.StudentId, employee), c.Person.Account != null);
    private static string? Visible(string? value, Employee employee)
    {
        if (value is null || employee.Position != EmployeePosition.Consultant) return value;
        return value.Length <= 4 ? new string('*', value.Length) : new string('*', value.Length - 4) + value[^4..];
    }
    private IQueryable<Customer> Profiles() => db.Customers.Include(c => c.Person).ThenInclude(p => p.Account).Include(c => c.Profile);

    public async Task<PaginatedList<CustomerInfo>> SearchAsync(string? query, int? page, Actor actor)
    {
        var employee = await StaffBookingService.EmployeeAsync(db, actor); var rows = Profiles().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim(); var phone = PhoneNumberNormalizer.ToE164(term);
            rows = rows.Where(c => (c.Person.FullName != null && c.Person.FullName.Contains(term))
                || (c.Person.Email != null && c.Person.Email.Contains(term)) || (phone != null && c.Person.PhoneNumber == phone));
        }
        var result = await PaginatedList<Customer>.CreateAsync(rows.OrderBy(c => c.Person.FullName).ThenBy(c => c.Id), page);
        return new(result.Select(c => Info(c, employee)), result.TotalCount, result.PageIndex, result.PageSize);
    }

    public async Task<CustomerDetails> DetailsAsync(int id, Actor actor)
    {
        var employee = await StaffBookingService.EmployeeAsync(db, actor);
        var customer = await Profiles().AsNoTracking().SingleOrDefaultAsync(c => c.Id == id) ?? throw new BookingException("Không tìm thấy khách hàng.");
        return new(Info(customer, employee), await db.Bookings.AsNoTracking().Where(b => b.CustomerId == id)
            .OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
            .Select(b => new CustomerBooking(b.Id, b.Code, b.Status, b.CheckIn, b.CheckOut, b.TotalAmount)).ToListAsync(), await vouchers.UsableAsync(id));
    }

    public Task<CustomerInfo> UpdateAsync(int id, CustomerUpdate input, Actor actor) => HomestayWrite.RunAsync(db, async () =>
    {
        var employee = await StaffBookingService.EmployeeAsync(db, actor);
        var customer = await Profiles().SingleOrDefaultAsync(c => c.Id == id) ?? throw new BookingException("Không tìm thấy khách hàng.");
        if (input.Note?.Length > 1000) throw new BookingException("Ghi chú khách hàng không được quá 1000 ký tự.");
        if (input.PhoneNumber is not null && PhoneNumberNormalizer.ToE164(input.PhoneNumber) != customer.Person.PhoneNumber)
            throw new BookingException("Không được sửa số điện thoại định danh của khách.");
        var name = input.FullName?.Trim(); var email = input.Email?.Trim();
        if (input.FullName is not null && (string.IsNullOrWhiteSpace(name) || name.Length > 100)) throw new BookingException("Họ tên khách không hợp lệ.");
        if (email is { Length: > 256 } || (!string.IsNullOrEmpty(email) && !new EmailAddressAttribute().IsValid(email)))
            throw new BookingException("Email khách không hợp lệ.");
        if (customer.Person.Account != null && ((name != null && name != customer.Person.FullName) || (email != null && email != customer.Person.Email)))
            throw new BookingException("Khách đã có tài khoản phải tự sửa họ tên/email.");
        var old = new { customer.Note, customer.Person.FullName, customer.Person.Email };
        if (input.Note is not null)
            customer.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        if (name != null) customer.Person.FullName = name;
        if (email != null) customer.Person.Email = email;
        customer.UpdatedAt = clock.GetUtcNow().UtcDateTime; customer.Person.UpdatedAt = customer.UpdatedAt;
        PartnerServiceSupport.Audit(db, clock, actor, "Customers", id.ToString(), old,
            new { customer.Note, customer.Person.FullName, customer.Person.Email });
        return Info(customer, employee);
    });
}
