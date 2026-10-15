using System.Globalization;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HomestayBooking.Tests.Data;

/// <summary>
/// Kiểm tra các ràng buộc ở mức CSDL (khóa chính, unique index, filtered index, check constraint)
/// theo fuji_homestay_database_v1.md, bằng SQLite in-memory vì EF Core InMemory không thực thi ràng buộc.
/// </summary>
public sealed class DatabaseConstraintTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly Homestay _homestay;
    private readonly RoomType _roomType;
    private readonly Room _room;
    private readonly Customer _customer;
    private readonly CancellationPolicy _policy;

    public DatabaseConstraintTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new SqliteTestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        _homestay = new Homestay
        {
            Name = "Test Homestay",
            Address = "Hà Nội",
            Status = HomestayStatus.Active,
            Owner = new Owner { Person = new PersonProfile { FullName = "Owner", PhoneNumber = "+84912000001" } },
            Area = new Area { Name = "Tây Hồ", Province = "Hà Nội" },
        };
        _roomType = new RoomType
        {
            Homestay = _homestay,
            Name = "Phòng đôi",
            Category = new RoomCategory { Name = "Phòng đôi" },
            MaxGuests = 2,
        };
        _room = new Room { RoomType = _roomType, RoomCode = "101" };
        _customer = new Customer
        {
            Person = new PersonProfile { FullName = "Nguyễn Văn A", PhoneNumber = "+84987000001" },
            Profile = new CustomerProfile(),
        };
        _policy = new CancellationPolicy { Name = "Chung", Scope = PolicyScope.All };
        _db.AddRange(_room, _customer, _policy);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Booking NewBooking(string code, DateOnly checkIn, DateOnly checkOut, decimal total = 1_000_000) => new()
    {
        Code = code,
        CustomerId = _customer.Id,
        ContactName = "Nguyễn Văn A",
        ContactPhone = "+84987000001",
        HomestayId = _homestay.Id,
        RoomTypeId = _roomType.Id,
        RoomId = _room.Id,
        CheckIn = checkIn,
        CheckOut = checkOut,
        Guests = 2,
        HoldExpiresAt = DateTime.UtcNow.AddMinutes(15),
        TotalAmount = total,
        DepositAmount = total * 0.3m,
        CommissionRate = 0.12m,
        CancellationPolicyId = _policy.Id,
    };

    /// <summary>Bỏ theo dõi để lỗi đến từ CSDL chứ không phải từ change tracker của EF.</summary>
    private async Task AssertRejectedAsync(object entity)
    {
        _db.ChangeTracker.Clear();
        _db.Add(entity);
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // ---- D. Chống trùng phòng ----

    [Fact]
    public async Task RoomNight_SameRoomSameDate_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 21));
        booking.RoomNights.Add(new RoomNight { RoomId = _room.Id, Date = new(2026, 11, 20) });
        var other = NewBooking("FJ261120-002", new(2026, 11, 20), new(2026, 11, 21));
        _db.Bookings.AddRange(booking, other);
        await _db.SaveChangesAsync();

        // Đơn khác chiếm cùng phòng cùng đêm → vi phạm khóa chính (RoomId, Date)
        await AssertRejectedAsync(new RoomNight { RoomId = _room.Id, Date = new(2026, 11, 20), BookingId = other.Id });
    }

    [Fact]
    public async Task RoomNight_WithoutBookingOrBlock_IsRejected()
    {
        await AssertRejectedAsync(new RoomNight { RoomId = _room.Id, Date = new(2026, 11, 20) });
    }

    [Fact]
    public async Task RoomBlock_Deleted_ReleasesItsNights()
    {
        var block = new RoomBlock
        {
            RoomId = _room.Id,
            StartDate = new(2026, 11, 20),
            EndDate = new(2026, 11, 22),
            Reason = RoomBlockReason.Maintenance,
            CreatedByAccount = new ApplicationUser { PersonId = _homestay.Owner.PersonId },
        };
        block.Nights.Add(new RoomNight { RoomId = _room.Id, Date = new(2026, 11, 20) });
        block.Nights.Add(new RoomNight { RoomId = _room.Id, Date = new(2026, 11, 21) });
        _db.RoomBlocks.Add(block);
        await _db.SaveChangesAsync();

        // Mở khóa ngày: xóa RoomBlock, các đêm bị chiếm tự xóa theo (cascade)
        _db.ChangeTracker.Clear();
        _db.RoomBlocks.Remove(await _db.RoomBlocks.SingleAsync());
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _db.RoomNights.CountAsync());
    }

    // ---- E. Đơn đặt phòng ----

    [Fact]
    public async Task Booking_DuplicateCode_IsRejected()
    {
        _db.Bookings.Add(NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22)));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(NewBooking("FJ261120-001", new(2026, 12, 1), new(2026, 12, 2)));
    }

    [Fact]
    public async Task Booking_CheckOutNotAfterCheckIn_IsRejected()
    {
        await AssertRejectedAsync(NewBooking("FJ261122-001", new(2026, 11, 22), new(2026, 11, 20)));
    }

    [Fact]
    public async Task Booking_DepositGreaterThanTotal_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.DepositAmount = booking.TotalAmount + 1;

        await AssertRejectedAsync(booking);
    }

    // ---- Voucher (Q4, D14) ----

    private int _issuerId;

    /// <summary>Quản lý cấp voucher, tạo một lần để lỗi trong test chỉ đến từ ràng buộc của bảng Vouchers.</summary>
    private int IssuerId()
    {
        if (_issuerId == 0)
        {
            var manager = new Employee { EmployeeCode = "QL001", Person = new PersonProfile { PhoneNumber = "+84901000031" } };
            _db.Employees.Add(manager);
            _db.SaveChanges();
            _issuerId = manager.Id;
        }
        return _issuerId;
    }

    private Voucher NewVoucher(string code, decimal amount, decimal balance, int? sourceBookingId = null) => new()
    {
        Code = code,
        CustomerId = _customer.Id,
        Amount = amount,
        Balance = balance,
        ExpiresOn = new(2027, 5, 1),
        Reason = "Trùng phòng",
        SourceBookingId = sourceBookingId,
        IssuedByEmployeeId = IssuerId(),
    };

    [Fact]
    public async Task Voucher_BalanceAboveAmount_IsRejected()
    {
        await AssertRejectedAsync(NewVoucher("VC00010001", 100_000, 150_000));
    }

    [Fact]
    public async Task Voucher_DuplicateCode_IsRejected()
    {
        _db.Vouchers.Add(NewVoucher("VC00010001", 100_000, 100_000));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(NewVoucher("VC00010001", 50_000, 50_000));
    }

    [Fact]
    public async Task Voucher_SecondVoucherForSameSourceBooking_IsRejected()
    {
        var source = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(source);
        await _db.SaveChangesAsync();
        _db.Vouchers.Add(NewVoucher("VC00010001", 100_000, 100_000, source.Id));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(NewVoucher("VC00010002", 100_000, 100_000, source.Id));
    }

    [Fact]
    public async Task Booking_DiscountNotBelowDeposit_IsRejected()
    {
        var voucher = NewVoucher("VC00010001", 500_000, 500_000);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync();
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.VoucherId = voucher.Id;
        booking.DiscountAmount = booking.DepositAmount; // khách luôn phải chuyển một khoản > 0

        await AssertRejectedAsync(booking);
    }

    [Fact]
    public async Task Booking_DiscountWithoutVoucher_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.DiscountAmount = 50_000;

        await AssertRejectedAsync(booking);
    }

    [Fact]
    public async Task Booking_VoucherWithoutDiscount_IsRejected()
    {
        var voucher = NewVoucher("VC00010001", 100_000, 100_000);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync();
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.VoucherId = voucher.Id;

        await AssertRejectedAsync(booking);
    }

    [Fact]
    public async Task Booking_WithVoucherDiscount_IsAllowed()
    {
        var voucher = NewVoucher("VC00010001", 100_000, 100_000);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync();
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.VoucherId = voucher.Id;
        booking.DiscountAmount = 36_000;
        booking.RefundAmount = booking.DepositAmount - booking.DiscountAmount; // hoàn đủ số khách thực chuyển
        _db.Bookings.Add(booking);

        await _db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(300_001)] // cọc 300.000đ: không hoàn quá số khách đã chuyển
    public async Task Booking_RefundOutsideAmountPaid_IsRejected(int refund)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.RefundAmount = refund;

        await AssertRejectedAsync(booking);
    }

    [Fact]
    public async Task BookingNight_SameDateTwice_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.Nights.Add(new BookingNight { Date = new(2026, 11, 20), DayType = DayType.Weekday, Price = 500_000 });
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new BookingNight { BookingId = booking.Id, Date = new(2026, 11, 20), DayType = DayType.Weekday, Price = 500_000 });
    }

    /// <summary>Phải bị chặn bởi đúng CHECK này, không phải lỗi CSDL bất kỳ.</summary>
    private async Task AssertRejectedByAsync(object entity, string constraint)
    {
        _db.ChangeTracker.Clear();
        _db.Add(entity);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains(constraint, ex.InnerException?.Message);
    }

    private async Task AssertAcceptedAsync(object entity)
    {
        _db.ChangeTracker.Clear();
        _db.Add(entity);
        await _db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1.0001", false)]
    [InlineData("0.0001", true)]
    [InlineData("1", true)]
    public async Task Booking_DepositRateWithinBounds(string rate, bool allowed)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.DepositRate = decimal.Parse(rate, CultureInfo.InvariantCulture);

        if (allowed)
        {
            await AssertAcceptedAsync(booking);
        }
        else
        {
            await AssertRejectedByAsync(booking, "CK_Bookings_DepositRate");
        }
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", false)]
    [InlineData("0.0001", true)]
    [InlineData("0.9999", true)]
    public async Task Booking_CommissionRateWithinBounds(string rate, bool allowed)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.CommissionRate = decimal.Parse(rate, CultureInfo.InvariantCulture);

        if (allowed)
        {
            await AssertAcceptedAsync(booking);
        }
        else
        {
            await AssertRejectedByAsync(booking, "CK_Bookings_CommissionRate");
        }
    }

    // CK_Bookings_ConfirmedAt: sau xác nhận phải có, trước xác nhận phải trống, đã hủy thì tùy
    [Theory]
    [InlineData(BookingStatus.Confirmed, true, true)]
    [InlineData(BookingStatus.CheckedIn, true, true)]
    [InlineData(BookingStatus.Completed, true, true)]
    [InlineData(BookingStatus.NoShow, true, true)]
    [InlineData(BookingStatus.Confirmed, false, false)]
    [InlineData(BookingStatus.CheckedIn, false, false)]
    [InlineData(BookingStatus.Completed, false, false)]
    [InlineData(BookingStatus.NoShow, false, false)]
    [InlineData(BookingStatus.AwaitingPayment, false, true)]
    [InlineData(BookingStatus.PaymentSubmitted, false, true)]
    [InlineData(BookingStatus.Deposited, false, true)]
    [InlineData(BookingStatus.Rejected, false, true)]
    [InlineData(BookingStatus.Expired, false, true)]
    [InlineData(BookingStatus.AwaitingPayment, true, false)]
    [InlineData(BookingStatus.PaymentSubmitted, true, false)]
    [InlineData(BookingStatus.Deposited, true, false)]
    [InlineData(BookingStatus.Rejected, true, false)]
    [InlineData(BookingStatus.Expired, true, false)]
    [InlineData(BookingStatus.Cancelled, true, true)]
    [InlineData(BookingStatus.Cancelled, false, true)]
    public async Task Booking_ConfirmedAtMatchesStatus(BookingStatus status, bool hasConfirmedAt, bool allowed)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        booking.Status = status;
        booking.ConfirmedAt = hasConfirmedAt ? new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc) : null;

        if (allowed)
        {
            await AssertAcceptedAsync(booking);
        }
        else
        {
            await AssertRejectedByAsync(booking, "CK_Bookings_ConfirmedAt");
        }
    }

    [Fact]
    public void Booking_ConfirmedAtCheck_CoversEveryStatus()
    {
        // Trạng thái mới phải được xếp vào nhóm trong CHECK, nếu không mọi đơn ở trạng thái đó bị chặn
        var model = _db.GetService<IDesignTimeModel>().Model;
        var sql = model.FindEntityType(typeof(Booking))!.GetCheckConstraints().Single(c => c.Name == "CK_Bookings_ConfirmedAt").Sql;
        Assert.All(Enum.GetNames<BookingStatus>(), name => Assert.Contains($"'{name}'", sql));
    }

    // ---- Yêu cầu đổi/hủy ----

    private async Task<int> SavedBookingIdAsync()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking.Id;
    }

    private static BookingChangeRequest Request(int bookingId, ChangeRequestType type, ActorType by = ActorType.Customer) => new()
    {
        BookingId = bookingId,
        RequestType = type,
        RequestedByType = by,
        NewCheckIn = type == ChangeRequestType.ChangeDates ? new DateOnly(2026, 12, 1) : null,
        NewCheckOut = type == ChangeRequestType.ChangeDates ? new DateOnly(2026, 12, 3) : null,
    };

    [Theory]
    [InlineData("ChangeDates", true)]
    [InlineData("ChangeRoomType", true)]
    [InlineData("Cancel", true)]
    [InlineData("Unknown", false)]
    public async Task ChangeRequest_TypeIsKnown(string requestType, bool allowed)
    {
        var type = allowed ? Enum.Parse<ChangeRequestType>(requestType) : ChangeRequestType.Cancel;
        var request = Request(await SavedBookingIdAsync(), type);
        request.NewRoomTypeId = type == ChangeRequestType.ChangeRoomType ? _roomType.Id : null;
        await AssertAcceptedAsync(request);

        if (!allowed)
        {
            _db.ChangeTracker.Clear();
            var ex = await Assert.ThrowsAsync<SqliteException>(() => _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [BookingChangeRequests] SET [RequestType] = {requestType} WHERE [Id] = {request.Id}"));
            Assert.Contains("CK_BookingChangeRequests_Type", ex.Message);
            Assert.Equal(type, await _db.BookingChangeRequests.AsNoTracking()
                .Where(r => r.Id == request.Id).Select(r => r.RequestType).SingleAsync());
        }
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Approved", true)]
    [InlineData("Rejected", true)]
    [InlineData("Unknown", false)]
    public async Task ChangeRequest_StatusIsKnown(string statusValue, bool allowed)
    {
        var status = allowed ? Enum.Parse<ChangeRequestStatus>(statusValue) : ChangeRequestStatus.Approved;
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = status;
        // Có DecidedAt cho dòng Approved/Rejected: UPDATE Unknown chỉ vi phạm CHECK Status, không vi phạm Decision.
        request.DecidedAt = status == ChangeRequestStatus.Pending
            ? null : new DateTime(2026, 11, 20, 3, 0, 0, DateTimeKind.Utc);
        await AssertAcceptedAsync(request);

        if (!allowed)
        {
            _db.ChangeTracker.Clear();
            var ex = await Assert.ThrowsAsync<SqliteException>(() => _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [BookingChangeRequests] SET [Status] = {statusValue} WHERE [Id] = {request.Id}"));
            Assert.Contains("CK_BookingChangeRequests_Status", ex.Message);
            Assert.Equal(status, await _db.BookingChangeRequests.AsNoTracking()
                .Where(r => r.Id == request.Id).Select(r => r.Status).SingleAsync());
        }
    }

    // CK_BookingChangeRequests_RequestedBy: FR-05 — khách gửi, hoặc nhân viên gửi thay
    [Theory]
    [InlineData(ActorType.Customer, true)]
    [InlineData(ActorType.Employee, true)]
    [InlineData(ActorType.Owner, false)]
    [InlineData(ActorType.System, false)]
    public async Task ChangeRequest_RequestedByType(ActorType by, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel, by);

        if (allowed)
        {
            await AssertAcceptedAsync(request);
        }
        else
        {
            await AssertRejectedByAsync(request, "CK_BookingChangeRequests_RequestedBy");
        }
    }

    // CK_BookingChangeRequests_Fields: trường bắt buộc theo loại yêu cầu
    [Theory]
    [InlineData(ChangeRequestType.Cancel, null, null, false, true)]
    [InlineData(ChangeRequestType.Cancel, "2026-12-01", null, false, false)]
    [InlineData(ChangeRequestType.Cancel, null, "2026-12-03", false, false)]
    [InlineData(ChangeRequestType.Cancel, null, null, true, false)]
    [InlineData(ChangeRequestType.ChangeDates, "2026-12-01", "2026-12-03", false, true)]
    [InlineData(ChangeRequestType.ChangeDates, "2026-12-01", null, false, false)]
    [InlineData(ChangeRequestType.ChangeDates, null, "2026-12-03", false, false)]
    [InlineData(ChangeRequestType.ChangeDates, null, null, false, false)]
    [InlineData(ChangeRequestType.ChangeDates, "2026-12-03", "2026-12-03", false, false)]
    [InlineData(ChangeRequestType.ChangeDates, "2026-12-03", "2026-12-01", false, false)]
    [InlineData(ChangeRequestType.ChangeRoomType, null, null, true, true)]
    [InlineData(ChangeRequestType.ChangeRoomType, null, null, false, false)]
    // Chưa chốt đổi kết hợp: trường thừa đang được cho phép có chủ đích
    [InlineData(ChangeRequestType.ChangeDates, "2026-12-01", "2026-12-03", true, true)]
    [InlineData(ChangeRequestType.ChangeRoomType, "2026-12-01", "2026-12-03", true, true)]
    public async Task ChangeRequest_FieldsMatchType(ChangeRequestType type, string? newIn, string? newOut, bool newRoomType, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), type);
        request.NewCheckIn = newIn is null ? null : DateOnly.Parse(newIn);
        request.NewCheckOut = newOut is null ? null : DateOnly.Parse(newOut);
        request.NewRoomTypeId = newRoomType ? _roomType.Id : null;

        if (allowed)
        {
            await AssertAcceptedAsync(request);
        }
        else
        {
            await AssertRejectedByAsync(request, "CK_BookingChangeRequests_Fields");
        }
    }

    // CK_BookingChangeRequests_Decision: chờ duyệt thì chưa có quyết định; đã quyết thì có thời điểm, người quyết chưa bắt buộc
    [Theory]
    [InlineData(ChangeRequestStatus.Pending, false, false, true)]
    [InlineData(ChangeRequestStatus.Pending, true, false, false)]
    [InlineData(ChangeRequestStatus.Pending, false, true, false)]
    [InlineData(ChangeRequestStatus.Pending, true, true, false)]
    [InlineData(ChangeRequestStatus.Approved, true, true, true)]
    [InlineData(ChangeRequestStatus.Approved, true, false, true)]
    [InlineData(ChangeRequestStatus.Approved, false, false, false)]
    [InlineData(ChangeRequestStatus.Approved, false, true, false)]
    [InlineData(ChangeRequestStatus.Rejected, true, true, true)]
    [InlineData(ChangeRequestStatus.Rejected, true, false, true)]
    [InlineData(ChangeRequestStatus.Rejected, false, false, false)]
    [InlineData(ChangeRequestStatus.Rejected, false, true, false)]
    public async Task ChangeRequest_DecisionMatchesStatus(ChangeRequestStatus status, bool hasDecidedAt, bool hasDecider, bool allowed)
    {
        var decider = new ApplicationUser { Person = new PersonProfile { FullName = "Nhân viên", PhoneNumber = "+84901000099" } };
        _db.Users.Add(decider);
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = status;
        request.DecidedAt = hasDecidedAt ? new DateTime(2026, 11, 5, 3, 0, 0, DateTimeKind.Utc) : null;
        request.DecidedByAccountId = hasDecider ? decider.Id : null;

        if (allowed)
        {
            await AssertAcceptedAsync(request);
        }
        else
        {
            await AssertRejectedByAsync(request, "CK_BookingChangeRequests_Decision");
        }
    }

    // ---- F. Thanh toán và giao dịch ngân hàng ----

    /// <summary>Không có giao dịch ngân hàng = bill tay, luôn kèm ảnh như ReceiptReviewService (CK_Payments_Receipt).</summary>
    private static Payment Deposit(int bookingId, PaymentStatus status, int? bankTransactionId = null) => new()
    {
        BookingId = bookingId,
        Type = PaymentType.Deposit,
        Method = bankTransactionId is null ? PaymentMethod.ManualReceipt : PaymentMethod.BankAutoMatch,
        BankTransactionId = bankTransactionId,
        ReceiptImageUrl = bankTransactionId is null ? "receipts/test-receipt.png" : null,
        Amount = 300_000,
        Status = status,
    };

    private static BankTransaction BankTx(string providerId) => new()
    {
        Provider = BankProvider.SePay,
        ProviderTransactionId = providerId,
        AccountNumber = "0901000001",
        Amount = 300_000,
        TransferContent = "FJ261120001",
        TransactionTime = DateTime.UtcNow,
    };

    [Fact]
    public async Task Payment_SecondOpenDepositForBooking_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        _db.Payments.Add(Deposit(booking.Id, PaymentStatus.Submitted));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(Deposit(booking.Id, PaymentStatus.Submitted));
    }

    [Fact]
    public async Task Payment_RejectedBillThenNewDeposit_IsAllowed()
    {
        // Bill bị từ chối giữ làm lịch sử, khách được tải bill mới
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        _db.Payments.AddRange(Deposit(booking.Id, PaymentStatus.Rejected), Deposit(booking.Id, PaymentStatus.Submitted));

        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Payments.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Payment_ManualReceiptWithoutImage_IsRejectedByReceiptCheck(string? receiptImageUrl)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        var payment = Deposit(booking.Id, PaymentStatus.Submitted);
        payment.ReceiptImageUrl = receiptImageUrl;

        _db.ChangeTracker.Clear();
        _db.Add(payment);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());

        // Phải đúng CHECK này, không phải lỗi CSDL bất kỳ
        Assert.Contains("CK_Payments_Receipt", ex.InnerException?.Message);
    }

    [Fact]
    public async Task BankTransaction_SameProviderIdTwice_IsRejected()
    {
        // Webhook gửi lặp không được ghi nhận hai lần
        _db.BankTransactions.Add(BankTx("SP-1001"));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(BankTx("SP-1001"));
    }

    [Fact]
    public async Task Payment_SameBankTransactionTwice_IsRejected()
    {
        var first = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        var second = NewBooking("FJ261120-002", new(2026, 12, 20), new(2026, 12, 22));
        var tx = BankTx("SP-1001");
        _db.AddRange(first, second, tx);
        await _db.SaveChangesAsync();
        _db.Payments.Add(Deposit(first.Id, PaymentStatus.Approved, tx.Id));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(Deposit(second.Id, PaymentStatus.Approved, tx.Id));
    }

    [Fact]
    public async Task SettlementItem_BookingInTwoSettlements_IsRejected()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        var first = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2 };
        var second = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 12, PeriodNo = 1 };
        _db.AddRange(booking, first, second);
        await _db.SaveChangesAsync();
        _db.SettlementItems.Add(Item(first.Id, booking.Id));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(Item(second.Id, booking.Id));
    }

    /// <summary>Đơn hoàn thành: tổng 1.000.000đ, cọc 300.000đ, hoa hồng 12% (mục 7.3).</summary>
    private static SettlementItem Item(int settlementId, int bookingId) => new()
    {
        SettlementId = settlementId,
        BookingId = bookingId,
        ItemType = SettlementItemType.Completed,
        RoomAmount = 1_000_000,
        DepositAmount = 300_000,
        RetainedAmount = 300_000,
        CommissionRate = 0.12m,
        CommissionAmount = 120_000,
        PayoutAmount = 180_000,
    };

    [Fact]
    public async Task SettlementItem_ValidAmounts_IsAllowed()
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        var settlement = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2 };
        _db.AddRange(booking, settlement);
        await _db.SaveChangesAsync();
        _db.SettlementItems.Add(Item(settlement.Id, booking.Id));

        await _db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", false)]
    [InlineData("0.0001", true)]
    [InlineData("0.9999", true)]
    public async Task SettlementItem_CommissionRateWithinBounds(string rate, bool allowed)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        var settlement = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2 };
        _db.AddRange(booking, settlement);
        await _db.SaveChangesAsync();
        var item = Item(settlement.Id, booking.Id);
        item.CommissionRate = decimal.Parse(rate, CultureInfo.InvariantCulture);
        item.CommissionAmount = item.RetainedAmount * item.CommissionRate;
        item.PayoutAmount = item.RetainedAmount - item.CommissionAmount;

        if (allowed)
        {
            await AssertAcceptedAsync(item);
        }
        else
        {
            await AssertRejectedByAsync(item, "CK_SettlementItems_CommissionRate");
        }
    }

    [Theory]
    [InlineData(SettlementStatus.Draft, false, true)]
    [InlineData(SettlementStatus.Draft, true, true)]
    [InlineData(SettlementStatus.Sent, false, false)]
    [InlineData(SettlementStatus.Sent, true, true)]
    [InlineData(SettlementStatus.Confirmed, false, false)]
    [InlineData(SettlementStatus.Confirmed, true, true)]
    [InlineData(SettlementStatus.Paid, false, false)]
    [InlineData(SettlementStatus.Paid, true, true)]
    public async Task Settlement_SentAtMatchesStatus(SettlementStatus status, bool hasSentAt, bool allowed)
    {
        var now = new DateTime(2026, 11, 30, 3, 0, 0, DateTimeKind.Utc);
        var settlement = new Settlement
        {
            HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2,
            Status = status,
            SentAt = hasSentAt ? now : null,
            // Tránh vi phạm CK_Settlements_Paid khi đang kiểm tra CK_Settlements_Sent.
            PaidAt = status == SettlementStatus.Paid ? now : null,
        };

        if (allowed)
        {
            await AssertAcceptedAsync(settlement);
        }
        else
        {
            await AssertRejectedByAsync(settlement, "CK_Settlements_Sent");
        }
    }

    public static TheoryData<string> BadItems => ["payout", "commission", "retained", "zero"];

    [Theory]
    [MemberData(nameof(BadItems))]
    public async Task SettlementItem_InconsistentAmounts_IsRejected(string kind)
    {
        var booking = NewBooking("FJ261120-001", new(2026, 11, 20), new(2026, 11, 22));
        var settlement = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2 };
        _db.AddRange(booking, settlement);
        await _db.SaveChangesAsync();
        var item = Item(settlement.Id, booking.Id);
        switch (kind)
        {
            case "payout": item.PayoutAmount = 200_000; break;                                       // ≠ giữ lại − hoa hồng
            case "commission": item.CommissionAmount = 400_000; item.PayoutAmount = -100_000; break; // hoa hồng > tiền giữ lại
            case "retained": item.RetainedAmount = 400_000; item.PayoutAmount = 280_000; break;     // giữ lại > cọc
            case "zero": item.RetainedAmount = 0; item.CommissionAmount = 0; item.PayoutAmount = 0; break; // hoàn 100% không vào đối soát
        }

        await AssertRejectedAsync(item);
    }

    [Fact]
    public async Task Settlement_PayoutNotRetainedMinusCommission_IsRejected()
    {
        await AssertRejectedAsync(new Settlement
        {
            HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2,
            TotalRoomAmount = 1_000_000, TotalRetained = 300_000, TotalCommission = 120_000, PayoutAmount = 300_000,
        });
    }

    [Fact]
    public async Task Settlement_PaidWithoutPaidAt_IsRejected()
    {
        await AssertRejectedAsync(new Settlement
        {
            HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 2, Status = SettlementStatus.Paid,
        });
    }

    [Fact]
    public async Task BankTransaction_RefundedWithoutRefundedAt_IsRejected()
    {
        var tx = BankTx("SEPAY-REFUND");
        tx.MatchStatus = BankMatchStatus.Refunded;

        await AssertRejectedAsync(tx);
    }

    [Theory]
    [InlineData(0, 11)]
    [InlineData(3, 11)]
    [InlineData(1, 13)]
    public async Task Settlement_InvalidPeriod_IsRejected(int periodNo, int month)
    {
        await AssertRejectedAsync(new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = month, PeriodNo = periodNo });
    }

    // ---- B, C. Hợp đồng, giá, chính sách hủy, ảnh, giấy tờ ----

    private Contract NewContract(string no, ContractStatus status, decimal rate = 0.12m) => new()
    {
        HomestayId = _homestay.Id,
        ContractNo = no,
        StartDate = new(2026, 6, 1),
        EndDate = new(2027, 5, 31),
        CommissionRate = rate,
        Status = status,
    };

    [Fact]
    public async Task Contract_TwoActiveForSameHomestay_IsRejected()
    {
        _db.Contracts.Add(NewContract("HD-01", ContractStatus.Active));
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(NewContract("HD-02", ContractStatus.Active));
    }

    [Fact]
    public async Task Contract_ExpiredHistoryPlusOneActive_IsAllowed()
    {
        _db.Contracts.AddRange(NewContract("HD-01", ContractStatus.Expired), NewContract("HD-02", ContractStatus.Active));

        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Contracts.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Contract_CommissionRateOutOfRange_IsRejected(int rate)
    {
        await AssertRejectedAsync(NewContract("HD-01", ContractStatus.Active, rate));
    }

    [Fact]
    public async Task RoomTypePrice_SameDayTypeAndEffectiveDate_IsRejected()
    {
        _db.RoomTypePrices.Add(new RoomTypePrice { RoomTypeId = _roomType.Id, DayType = DayType.Weekday, Price = 500_000, EffectiveFrom = new(2026, 1, 1) });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new RoomTypePrice { RoomTypeId = _roomType.Id, DayType = DayType.Weekday, Price = 550_000, EffectiveFrom = new(2026, 1, 1) });
    }

    [Fact]
    public async Task Room_DuplicateCodeInRoomType_IsRejected()
    {
        await AssertRejectedAsync(new Room { RoomTypeId = _roomType.Id, RoomCode = "101" });
    }

    [Fact]
    public async Task CancellationPolicy_TwoActiveForSameHomestayAndScope_IsRejected()
    {
        _db.CancellationPolicies.Add(new CancellationPolicy { HomestayId = _homestay.Id, Name = "Lễ 1", Scope = PolicyScope.Holiday });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new CancellationPolicy { HomestayId = _homestay.Id, Name = "Lễ 2", Scope = PolicyScope.Holiday });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task CancellationRule_PercentOutOfRange_IsRejected(int percent)
    {
        await AssertRejectedAsync(new CancellationRule { PolicyId = _policy.Id, MinDaysBefore = 7, RefundPercent = percent });
    }

    [Fact]
    public async Task HomestayImage_TwoCoversForSameHomestay_IsRejected()
    {
        _db.HomestayImages.Add(new HomestayImage { HomestayId = _homestay.Id, Url = "/a.jpg", IsCover = true });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new HomestayImage { HomestayId = _homestay.Id, Url = "/b.jpg", IsCover = true });
    }

    [Fact]
    public async Task VerificationDocument_WithoutOwnerOrHomestay_IsRejected()
    {
        await AssertRejectedAsync(new VerificationDocument { DocumentType = DocumentType.BusinessLicense, FileUrl = "x.pdf" });
    }

    // ---- G. Đánh giá ----

    [Fact]
    public async Task Review_SecondReviewForSameBooking_IsRejected()
    {
        var booking = NewBooking("FJ261120-002", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        _db.Reviews.Add(new Review { Booking = booking, HomestayId = _homestay.Id, Rating = 5 });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new Review { BookingId = booking.Id, HomestayId = _homestay.Id, Rating = 4 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Review_RatingOutsideOneToFive_IsRejected(int rating)
    {
        var booking = NewBooking("FJ261120-003", new(2026, 11, 20), new(2026, 11, 22));
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new Review { BookingId = booking.Id, HomestayId = _homestay.Id, Rating = rating });
    }

    [Fact]
    public async Task Room_WithBookings_CannotBeDeleted()
    {
        _db.Bookings.Add(NewBooking("FJ261120-004", new(2026, 11, 20), new(2026, 11, 22)));
        await _db.SaveChangesAsync();

        // Không xóa cứng dữ liệu nghiệp vụ: khóa ngoại Restrict
        _db.ChangeTracker.Clear();
        _db.Rooms.Remove(await _db.Rooms.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // ---- A. Tài khoản dùng chung (v5) ----

    [Fact]
    public async Task PersonProfile_DuplicatePhone_IsRejected()
    {
        await AssertRejectedAsync(new PersonProfile { FullName = "Người khác", PhoneNumber = "+84987000001" });
    }

    [Fact]
    public async Task Account_SecondAccountForSamePerson_IsRejected()
    {
        var personId = _customer.PersonId;
        _db.Users.Add(new ApplicationUser { PersonId = personId });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new ApplicationUser { PersonId = personId });
    }

    [Fact]
    public async Task Customer_SecondCustomerForSamePerson_IsRejected()
    {
        await AssertRejectedAsync(new Customer { PersonId = _customer.PersonId });
    }

    [Fact]
    public async Task Employee_DuplicateCode_IsRejected()
    {
        _db.Employees.Add(new Employee { EmployeeCode = "NV001", Person = new PersonProfile { PhoneNumber = "+84901000011" } });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new Employee { EmployeeCode = "NV001", Person = new PersonProfile { PhoneNumber = "+84901000012" } });
    }

    [Fact]
    public async Task OwnerRegistration_TwoOpenForSameOwner_IsRejected()
    {
        var ownerId = _homestay.OwnerId;
        _db.OwnerRegistrations.Add(new OwnerRegistration { OwnerId = ownerId, Status = OwnerRegistrationStatus.PendingReview });
        await _db.SaveChangesAsync();

        await AssertRejectedAsync(new OwnerRegistration { OwnerId = ownerId, Status = OwnerRegistrationStatus.Draft });
    }

    [Fact]
    public async Task OwnerRegistration_ClosedHistoryPlusOneOpen_IsAllowed()
    {
        var ownerId = _homestay.OwnerId;
        _db.OwnerRegistrations.AddRange(
            new OwnerRegistration { OwnerId = ownerId, Status = OwnerRegistrationStatus.Rejected },
            new OwnerRegistration { OwnerId = ownerId, Status = OwnerRegistrationStatus.Approved },
            new OwnerRegistration { OwnerId = ownerId, Status = OwnerRegistrationStatus.PendingReview });

        await _db.SaveChangesAsync();

        Assert.Equal(3, await _db.OwnerRegistrations.CountAsync());
    }
}
