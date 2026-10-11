using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Data;

/// <summary>
/// Đơn đặt phòng mẫu để demo màn hình chủ homestay, kế toán, thống kê: khoảng 60 đơn trải từ 4 tháng trước
/// đến hết 2 tháng sau tháng hiện tại, đủ các trạng thái, kèm thanh toán, giao dịch ngân hàng, lịch sử và đánh giá.
/// Mốc thời gian tính theo ngày chạy seed nên đơn "đang ở", "chờ duyệt bill"... luôn khớp với hôm nay.
/// Dữ liệu sinh theo Random cố định nên mỗi lần tạo lại CSDL đều giống nhau (trừ mốc thời gian).
/// </summary>
public static partial class SeedData
{
    public const int SampleBookingCount = 60;
    public const int SampleReviewCount = 20;

    /// <summary>Ảnh bill mẫu (ghi rõ "ảnh mẫu"), chép vào App_Data/receipts khi seed chạy trong ứng dụng.</summary>
    public const string SampleReceiptFile = "sample-receipt.png";

    private static readonly string[] CancelReasons =
    [
        "Thay đổi lịch công tác.",
        "Gia đình có việc đột xuất.",
        "Đặt nhầm ngày.",
        "Chuyển sang đi tỉnh khác.",
        "Nhóm bạn không đủ người.",
    ];

    private static readonly string[] RejectReasons =
    [
        "Phòng đang sửa chữa đột xuất (hỏng điều hòa).",
        "Chủ nhà bận việc gia đình, không đón khách được.",
    ];

    private static readonly (int Rating, string Comment)[] ReviewData =
    [
        (5, "Phòng sạch sẽ, chủ nhà nhiệt tình, vị trí rất thuận tiện đi lại. Sẽ quay lại!"),
        (5, "Không gian đẹp y như ảnh, giường êm, check-in nhanh gọn."),
        (5, "Cả nhà đều thích, trẻ con được chơi thoải mái. Giá hợp lý so với chất lượng."),
        (5, "Chủ nhà chỉ quán ăn ngon quanh khu, buổi tối rất yên tĩnh."),
        (5, "Đặt cọc bằng QR nhanh, vài phút đã có xác nhận. Trải nghiệm rất tốt."),
        (5, "View đẹp, sáng ra uống cà phê ngoài ban công rất chill."),
        (5, "Nhóm mình tổ chức sinh nhật ở đây, khu BBQ rộng và đầy đủ dụng cụ."),
        (4, "Phòng ổn, sạch, chỉ có wifi hơi chập chờn buổi tối."),
        (4, "Vị trí tốt nhưng chỗ để xe hơi chật. Nhìn chung hài lòng."),
        (4, "Phòng đẹp, tuy nhiên nước nóng hơi yếu. Chủ nhà hỗ trợ nhanh."),
        (4, "Đáng tiền, chỉ mong có thêm ấm siêu tốc trong phòng."),
        (4, "Không khí trong lành, đường vào hơi khó tìm lúc trời tối."),
        (4, "Nhân viên Fuji tư vấn nhiệt tình qua điện thoại, phòng đúng như mô tả."),
        (3, "Phòng hơi nhỏ so với ảnh, cách âm chưa tốt."),
        (3, "Tạm ổn, dọn phòng chưa kỹ lắm khi nhận phòng."),
        (3, "Giá cuối tuần hơi cao, tiện nghi ở mức cơ bản."),
        (2, "Điều hòa kêu to, phải đổi phòng giữa đêm. Chủ nhà có xin lỗi."),
        (5, "Lần thứ hai ở đây, vẫn tuyệt vời như lần trước."),
        (4, "Trẻ con rất thích sân vườn. Sẽ giới thiệu cho bạn bè."),
        (1, "Inbox zalo 09xx để nhận mã giảm giá homestay khác rẻ hơn!!!"), // spam → ẩn
    ];

    private static readonly string[] OwnerReplies =
    [
        "Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!",
        "Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.",
        "Rất vui vì gia đình bạn có kỳ nghỉ vui vẻ!",
    ];

    private sealed record SeedActor(ActorType Type, string? AccountId, int? EmployeeId = null);

    private static async Task SeedSampleBookingsAsync(ApplicationDbContext db, IServiceProvider services)
    {
        if (await db.Bookings.AnyAsync())
        {
            return;
        }
        CopySampleReceipt(services);

        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(VietnamTime.ToLocal(nowUtc));
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var rangeStart = firstOfMonth.AddMonths(-4);
        var rangeEnd = firstOfMonth.AddMonths(3).AddDays(-1);
        if (rangeStart < ContractStart)
        {
            rangeStart = ContractStart;
        }
        var rng = new Random(2026);

        var homestays = await db.Homestays
            .Include(h => h.Contracts)
            .Include(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Rooms)
            .Include(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .AsSplitQuery()
            .OrderBy(h => h.Id)
            .ToListAsync();
        var holidays = (await db.Holidays.Select(h => h.Date).ToListAsync()).ToHashSet();
        var policies = await db.CancellationPolicies.Include(p => p.Rules).Where(p => p.IsActive).ToListAsync();
        var customers = await db.Customers
            .Include(c => c.Person).ThenInclude(p => p.Account)
            .OrderBy(c => c.Id)
            .ToListAsync();
        var employees = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account).ToListAsync();
        var consultants = employees.Where(e => e.Position == EmployeePosition.Consultant).ToList();
        var accountant = employees.Single(e => e.Position == EmployeePosition.Accountant);
        var accountantActor = new SeedActor(ActorType.Employee, accountant.Person.Account?.Id, accountant.Id);

        var occupied = new HashSet<(int RoomId, DateOnly Date)>();
        var codeSeq = new Dictionary<DateOnly, int>();
        var bankSeq = 0;
        var bookings = new List<Booking>();
        var looseTransactions = new List<BankTransaction>();

        // Đơn "đặc biệt" bảo đảm đủ trạng thái cho demo; phần còn lại chọn trạng thái theo ngày ở
        var plans = new List<(BookingStatus? Status, DateOnly? CheckIn, double MinutesAgo)>
        {
            (BookingStatus.CheckedIn, today.AddDays(-1), 0),
            (BookingStatus.CheckedIn, today, 0),
            (BookingStatus.Deposited, today.AddDays(9), 12),     // còn trong hạn 30' xác nhận
            (BookingStatus.Deposited, today.AddDays(16), 50),    // quá hạn 30' → job cảnh báo nhân viên
            (BookingStatus.PaymentSubmitted, today.AddDays(12), 25),
            (BookingStatus.PaymentSubmitted, today.AddDays(20), 180),
            (BookingStatus.Rejected, null, 0),
            (BookingStatus.NoShow, null, 0),
        };
        while (plans.Count < SampleBookingCount)
        {
            plans.Add((null, null, 0));
        }

        var expiredCount = 0;
        foreach (var plan in plans)
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var homestay = homestays[rng.Next(homestays.Count)];
                var roomType = homestay.RoomTypes.ElementAt(rng.Next(homestay.RoomTypes.Count));
                var nights = roomType.MaxGuests >= 10 ? rng.Next(1, 3) : rng.Next(1, 4);
                var checkIn = plan.CheckIn ?? RandomDate(rng, rangeStart, rangeEnd.AddDays(-nights));
                if (plan.Status is BookingStatus.NoShow or BookingStatus.Rejected && plan.CheckIn is null)
                {
                    // Đơn đã qua: chọn ngày trong quá khứ
                    checkIn = RandomDate(rng, rangeStart, today.AddDays(-nights - 1));
                }
                var checkOut = checkIn.AddDays(nights);
                var status = plan.Status ?? PickStatus(rng, checkIn, checkOut, today);
                var holds = status.HoldsRoom();

                var room = roomType.Rooms
                    .OrderBy(_ => rng.Next())
                    .FirstOrDefault(r => !holds || Enumerable.Range(0, nights).All(d => !occupied.Contains((r.Id, checkIn.AddDays(d)))));
                if (room is null)
                {
                    continue;
                }

                var customer = customers[rng.Next(customers.Count)];
                var booking = BuildBooking(rng, homestay, roomType, room, customer, consultants, accountantActor, holidays, policies,
                    checkIn, checkOut, status, plan.MinutesAgo, nowUtc, codeSeq, looseTransactions, ref bankSeq, ref expiredCount);
                if (booking is null)
                {
                    continue;
                }
                if (holds)
                {
                    foreach (var night in booking.Nights)
                    {
                        occupied.Add((room.Id, night.Date));
                        booking.RoomNights.Add(new RoomNight { RoomId = room.Id, Date = night.Date });
                    }
                }
                bookings.Add(booking);
                break;
            }
        }

        db.Bookings.AddRange(bookings);
        db.BankTransactions.AddRange(looseTransactions);

        // Bộ đếm mã đơn khớp với mã đã sinh, để đơn thật đặt cùng ngày không trùng mã
        foreach (var (date, seq) in codeSeq)
        {
            db.BookingCodeCounters.Add(new BookingCodeCounter { Date = date, LastSeq = seq });
        }

        // Giao dịch không có mã đơn: kế toán phải tự tìm đơn hoặc hoàn lại (màn hình kế toán)
        db.BankTransactions.Add(new BankTransaction
        {
            Provider = BankProvider.SePay,
            ProviderTransactionId = $"SEED-{++bankSeq:0000}",
            AccountNumber = "0338405548",
            Amount = 450_000,
            TransferContent = "NGUYEN VAN A chuyen tien homestay",
            TransactionTime = nowUtc.AddHours(-20),
            ReceivedAt = nowUtc.AddHours(-20),
            MatchStatus = BankMatchStatus.Unmatched,
            Note = "Không tìm thấy mã đơn trong nội dung chuyển khoản.",
        });

        AddSampleReviews(rng, bookings, nowUtc);
        await db.SaveChangesAsync();
    }

    private static DateOnly RandomDate(Random rng, DateOnly from, DateOnly to) =>
        to <= from ? from : from.AddDays(rng.Next(to.DayNumber - from.DayNumber + 1));

    /// <summary>Đơn đã qua chủ yếu hoàn thành; đơn tương lai chủ yếu đã xác nhận; xen kẽ hủy, hết hạn, bị từ chối.</summary>
    private static BookingStatus PickStatus(Random rng, DateOnly checkIn, DateOnly checkOut, DateOnly today)
    {
        var r = rng.NextDouble();
        if (checkOut < today)
        {
            return r switch
            {
                < 0.70 => BookingStatus.Completed,
                < 0.82 => BookingStatus.Cancelled,
                < 0.92 => BookingStatus.Expired,
                < 0.96 => BookingStatus.NoShow,
                _ => BookingStatus.Rejected,
            };
        }
        if (checkIn <= today)
        {
            return BookingStatus.CheckedIn;
        }
        return r switch
        {
            < 0.68 => BookingStatus.Confirmed,
            < 0.84 => BookingStatus.Cancelled,
            < 0.95 => BookingStatus.Expired,
            _ => BookingStatus.Rejected,
        };
    }

    private static DateTime VnToUtc(DateOnly date, int hour, int minute = 0) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(new TimeOnly(hour, minute)), TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));

    private static Booking? BuildBooking(
        Random rng, Homestay homestay, RoomType roomType, Room room, Customer customer,
        IReadOnlyList<Employee> consultants, SeedActor accountantActor, IReadOnlySet<DateOnly> holidays, IReadOnlyList<CancellationPolicy> policies,
        DateOnly checkIn, DateOnly checkOut, BookingStatus status, double minutesAgo, DateTime nowUtc,
        Dictionary<DateOnly, int> codeSeq, List<BankTransaction> looseTransactions, ref int bankSeq, ref int expiredCount)
    {
        var live = status is BookingStatus.Deposited or BookingStatus.PaymentSubmitted;
        var checkInUtc = VnToUtc(checkIn, 14);

        // Thời điểm đặt: 0–30 ngày trước ngày nhận phòng, trong giờ 8–22h; không trước ngày hợp đồng, không sau hiện tại
        DateTime created;
        if (live)
        {
            created = nowUtc.AddMinutes(-minutesAgo - 10);
        }
        else
        {
            var createdDate = checkIn.AddDays(-rng.Next(0, 31));
            if (createdDate < ContractStart)
            {
                createdDate = ContractStart;
            }
            created = VnToUtc(createdDate, rng.Next(8, 13), rng.Next(60));
            var latest = checkInUtc.AddHours(-3) < nowUtc.AddHours(-2) ? checkInUtc.AddHours(-3) : nowUtc.AddHours(-2);
            if (created > latest)
            {
                created = latest.AddHours(-rng.Next(1, 72));
            }
        }

        // Giá từng đêm theo loại ngày (giống PricingService), cọc 30% làm tròn nghìn
        var bookingNights = new List<BookingNight>();
        for (var night = checkIn; night < checkOut; night = night.AddDays(1))
        {
            var dayType = PricingService.DayTypeOf(night, holidays);
            var price = roomType.Prices.Where(p => p.DayType == dayType && p.EffectiveFrom <= night).MaxBy(p => p.EffectiveFrom)?.Price;
            if (price is null)
            {
                return null;
            }
            bookingNights.Add(new BookingNight { Date = night, DayType = dayType, Price = price.Value });
        }
        var total = bookingNights.Sum(n => n.Price);
        var deposit = PricingService.RoundToThousand(total * 0.30m);
        var hasHoliday = bookingNights.Any(n => n.DayType == DayType.Holiday);
        var policy = SelectSeedPolicy(policies, homestay.Id, hasHoliday);
        var contract = homestay.Contracts.Single(c => c.Status == ContractStatus.Active);

        // Kênh đặt: khách có tài khoản phần lớn tự đặt web; còn lại nhân viên tư vấn đặt qua điện thoại/Zalo
        var customerAccount = customer.Person.Account?.Id;
        var web = customerAccount is not null && rng.NextDouble() < 0.75;
        var consultant = web ? null : consultants[rng.Next(consultants.Count)];
        var creator = web
            ? new SeedActor(ActorType.Customer, customerAccount)
            : new SeedActor(ActorType.Employee, consultant!.Person.Account?.Id);
        var customerSide = creator; // người hủy phía khách: khách tự hủy hoặc nhân viên hủy giúp
        var owner = new SeedActor(ActorType.Owner, homestay.Owner.Person.Account?.Id);

        var createdDay = DateOnly.FromDateTime(VietnamTime.ToLocal(created));
        codeSeq[createdDay] = codeSeq.GetValueOrDefault(createdDay) + 1;

        var booking = new Booking
        {
            Code = BookingCodeGenerator.Format(createdDay, codeSeq[createdDay]),
            Customer = customer,
            ContactName = customer.Person.FullName ?? "Khách hàng",
            ContactPhone = customer.Person.PhoneNumber,
            SourceChannel = web ? SourceChannel.Web : (rng.NextDouble() < 0.7 ? SourceChannel.Phone : SourceChannel.Zalo),
            CreatedByEmployeeId = consultant?.Id,
            AssignedEmployeeId = consultant?.Id,
            HomestayId = homestay.Id,
            RoomTypeId = roomType.Id,
            RoomId = room.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = Math.Max(1, roomType.MaxGuests - rng.Next(0, 2)),
            Status = status,
            HoldExpiresAt = created.AddMinutes(15),
            TotalAmount = total,
            DepositRate = 0.30m,
            DepositAmount = deposit,
            CommissionRate = contract.CommissionRate,
            CancellationPolicyId = policy.Id,
            CreatedAt = created,
            UpdatedAt = created,
        };
        foreach (var n in bookingNights)
        {
            booking.Nights.Add(n);
        }

        var at = created;
        var current = BookingStatus.AwaitingPayment;
        void Move(BookingStatus to, DateTime when, SeedActor actor, string? note = null)
        {
            booking.Histories.Add(new BookingStatusHistory
            {
                FromStatus = current, ToStatus = to, ActorType = actor.Type, ActorAccountId = actor.AccountId, Note = note, ChangedAt = when,
            });
            current = to;
            at = when;
            booking.UpdatedAt = when;
        }
        booking.Histories.Add(new BookingStatusHistory
        {
            ToStatus = BookingStatus.AwaitingPayment, ActorType = creator.Type, ActorAccountId = creator.AccountId, ChangedAt = created,
        });

        // Khách hủy hoặc hết hạn khi chưa trả tiền
        if (status == BookingStatus.Expired || (status == BookingStatus.Cancelled && rng.NextDouble() < 0.3))
        {
            if (status == BookingStatus.Expired)
            {
                var expiredAt = booking.HoldExpiresAt.AddMinutes(1);
                expiredCount++;
                // Hai đơn hết hạn có giao dịch cần kế toán xử lý: chuyển thiếu, và chuyển sau khi đã hết hạn
                if (expiredCount is 1 or 2)
                {
                    var underpaid = expiredCount == 1;
                    looseTransactions.Add(new BankTransaction
                    {
                        MatchedBooking = booking,
                        Provider = BankProvider.SePay,
                        ProviderTransactionId = $"SEED-{++bankSeq:0000}",
                        AccountNumber = "0338405548",
                        Amount = underpaid ? deposit - 50_000 : deposit,
                        TransferContent = $"{booking.Code} dat coc",
                        TransactionTime = underpaid ? created.AddMinutes(8) : expiredAt.AddMinutes(40),
                        ReceivedAt = underpaid ? created.AddMinutes(8) : expiredAt.AddMinutes(40),
                        MatchStatus = underpaid ? BankMatchStatus.Underpaid : BankMatchStatus.LatePayment,
                        Note = underpaid ? "Thiếu 50.000đ so với tiền cọc." : null,
                    });
                }
                Move(BookingStatus.Expired, expiredAt, new SeedActor(ActorType.System, null), "Quá hạn giữ phòng chờ thanh toán.");
                booking.CancelledAt = expiredAt;
            }
            else
            {
                var cancelledAt = created.AddMinutes(rng.Next(3, 12));
                booking.CancelReason = CancelReasons[rng.Next(CancelReasons.Length)];
                Move(BookingStatus.Cancelled, cancelledAt, customerSide, $"Khách tự hủy: {booking.CancelReason}");
                booking.CancelledAt = cancelledAt;
                booking.RefundAmount = 0;
            }
            return booking;
        }

        // Thanh toán cọc: 80% tự khớp qua QR MB, 20% ảnh bill kế toán duyệt
        var paidAt = created.AddMinutes(live ? 5 : rng.Next(2, 13));
        var manual = status == BookingStatus.PaymentSubmitted || (status != BookingStatus.Deposited && rng.NextDouble() < 0.2);
        DateTime depositedAt;
        if (manual)
        {
            var payment = new Payment
            {
                Type = PaymentType.Deposit,
                Method = PaymentMethod.ManualReceipt,
                Amount = deposit,
                ReceiptImageUrl = SampleReceiptFile,
                TransferContent = booking.Code,
                SubmittedByAccountId = customerAccount,
                SubmittedAt = paidAt,
            };
            booking.Payments.Add(payment);
            booking.PaymentSubmittedAt = paidAt;
            Move(BookingStatus.PaymentSubmitted, paidAt, new SeedActor(ActorType.Customer, customerAccount), "Khách gửi ảnh bill.");
            if (status == BookingStatus.PaymentSubmitted)
            {
                return booking;
            }
            depositedAt = paidAt.AddMinutes(rng.Next(20, 181));
            payment.Status = PaymentStatus.Approved;
            payment.ReviewedByEmployeeId = accountantActor.EmployeeId;
            payment.ReviewedAt = depositedAt;
            Move(BookingStatus.Deposited, depositedAt, accountantActor, "Kế toán duyệt ảnh bill.");
        }
        else
        {
            depositedAt = paidAt;
            var tx = new BankTransaction
            {
                Provider = BankProvider.SePay,
                ProviderTransactionId = $"SEED-{++bankSeq:0000}",
                AccountNumber = "0338405548",
                Amount = deposit,
                // Ngân hàng hay bỏ dấu "-" hoặc chèn tiền tố; bộ đọc mã đơn vẫn nhận ra
                TransferContent = rng.Next(3) switch
                {
                    0 => $"{booking.Code} dat coc",
                    1 => $"MBVCB.{rng.Next(100000, 999999)}.{booking.Code.Replace("-", "")}.CT tu {customer.Person.FullName}",
                    _ => booking.Code,
                },
                TransactionTime = paidAt,
                ReceivedAt = paidAt,
                MatchStatus = BankMatchStatus.Matched,
                MatchedBooking = booking,
            };
            booking.Payments.Add(new Payment
            {
                Type = PaymentType.Deposit,
                Method = PaymentMethod.BankAutoMatch,
                BankTransaction = tx,
                Amount = deposit,
                TransferContent = tx.TransferContent,
                Status = PaymentStatus.Approved,
                SubmittedAt = paidAt,
                ReviewedAt = paidAt,
            });
            Move(BookingStatus.Deposited, depositedAt, new SeedActor(ActorType.System, null), $"Tự khớp giao dịch {tx.ProviderTransactionId}.");
        }
        booking.DepositedAt = depositedAt;
        booking.ConfirmDeadlineAt = depositedAt.AddMinutes(30);
        if (status == BookingStatus.Deposited)
        {
            return booking;
        }

        if (status == BookingStatus.Rejected)
        {
            var rejectedAt = depositedAt.AddMinutes(rng.Next(10, 41));
            booking.CancelReason = RejectReasons[rng.Next(RejectReasons.Length)];
            booking.RefundAmount = deposit;
            booking.CancelledAt = rejectedAt;
            Move(BookingStatus.Rejected, rejectedAt, owner, booking.CancelReason);
            AddRefund(booking, deposit, rejectedAt, nowUtc, accountantActor);
            return booking;
        }

        var confirmedAt = depositedAt.AddMinutes(rng.Next(5, 29));
        booking.ConfirmedAt = confirmedAt;
        Move(BookingStatus.Confirmed, confirmedAt, owner);

        switch (status)
        {
            case BookingStatus.Cancelled:
            {
                // Hủy sau khi đã xác nhận: hoàn theo chính sách, tính theo số ngày còn lại trước ngày nhận phòng
                var latest = checkInUtc.AddHours(-12) < nowUtc ? checkInUtc.AddHours(-12) : nowUtc.AddMinutes(-30);
                if (latest <= confirmedAt)
                {
                    latest = confirmedAt.AddMinutes(30);
                }
                var cancelledAt = confirmedAt.AddMinutes(rng.NextDouble() * (latest - confirmedAt).TotalMinutes);
                var daysBefore = checkIn.DayNumber - DateOnly.FromDateTime(VietnamTime.ToLocal(cancelledAt)).DayNumber;
                var percent = policy.Rules.Where(r => r.MinDaysBefore <= daysBefore).MaxBy(r => r.MinDaysBefore)?.RefundPercent ?? 0;
                var refund = PricingService.RoundToThousand(deposit * percent / 100m);
                booking.CancelReason = CancelReasons[rng.Next(CancelReasons.Length)];
                booking.RefundAmount = refund;
                booking.CancelledAt = cancelledAt;
                Move(BookingStatus.Cancelled, cancelledAt, customerSide, $"Khách hủy trước {daysBefore} ngày: {booking.CancelReason}");
                AddRefund(booking, refund, cancelledAt, nowUtc, accountantActor);
                break;
            }
            case BookingStatus.NoShow:
                booking.RefundAmount = 0;
                Move(BookingStatus.NoShow, VnToUtc(checkIn, 23), owner, "Khách không đến nhận phòng.");
                break;
            case BookingStatus.CheckedIn or BookingStatus.Completed:
            {
                var checkedInAt = checkInUtc.AddMinutes(rng.Next(0, 240));
                if (checkedInAt > nowUtc.AddMinutes(-10))
                {
                    checkedInAt = nowUtc.AddMinutes(-10); // nhận phòng sớm hôm nay
                }
                if (checkedInAt <= confirmedAt)
                {
                    checkedInAt = confirmedAt.AddMinutes(5);
                }
                booking.CheckedInAt = checkedInAt;
                Move(BookingStatus.CheckedIn, checkedInAt, owner);
                if (status == BookingStatus.Completed)
                {
                    var completedAt = VnToUtc(checkOut, 11, rng.Next(60));
                    booking.CompletedAt = completedAt;
                    Move(BookingStatus.Completed, completedAt, owner);
                }
                break;
            }
        }
        return booking;
    }

    /// <summary>Hoàn tiền đã chuyển nếu hủy từ 3 ngày trước; gần đây thì để kế toán xử lý (chưa có khoản Refund).</summary>
    private static void AddRefund(Booking booking, decimal amount, DateTime cancelledAt, DateTime nowUtc, SeedActor accountant)
    {
        if (amount <= 0 || cancelledAt.AddDays(3) > nowUtc)
        {
            return;
        }
        booking.Payments.Add(new Payment
        {
            Type = PaymentType.Refund,
            Method = PaymentMethod.ManualTransfer,
            Amount = amount,
            TransferContent = $"Fuji hoan coc {booking.Code}",
            Status = PaymentStatus.Approved,
            SubmittedByAccountId = accountant.AccountId,
            ReviewedByEmployeeId = accountant.EmployeeId,
            SubmittedAt = cancelledAt.AddDays(2),
            ReviewedAt = cancelledAt.AddDays(2),
        });
    }

    private static CancellationPolicy SelectSeedPolicy(IReadOnlyList<CancellationPolicy> policies, int homestayId, bool hasHoliday)
    {
        (int? HomestayId, PolicyScope Scope)[] order =
            [(homestayId, PolicyScope.Holiday), (homestayId, PolicyScope.All), (null, PolicyScope.Holiday), (null, PolicyScope.All)];
        return order
            .Where(o => hasHoliday || o.Scope != PolicyScope.Holiday)
            .Select(o => policies.FirstOrDefault(p => p.HomestayId == o.HomestayId && p.Scope == o.Scope))
            .First(p => p is not null)!;
    }

    /// <summary>Đánh giá cho các đơn đã hoàn thành: viết sau khi trả phòng 2–72 giờ; một phần có chủ nhà trả lời; đánh giá spam bị ẩn.</summary>
    private static void AddSampleReviews(Random rng, IReadOnlyList<Booking> bookings, DateTime nowUtc)
    {
        var completed = bookings
            .Where(b => b.Status == BookingStatus.Completed && b.CompletedAt < nowUtc.AddHours(-3))
            .OrderBy(b => b.CompletedAt)
            .ToList();
        var count = Math.Min(SampleReviewCount, completed.Count);
        for (var i = 0; i < count; i++)
        {
            var booking = completed[i];
            var (rating, comment) = ReviewData[i % ReviewData.Length];
            var createdAt = booking.CompletedAt!.Value.AddHours(rng.Next(2, 73));
            if (createdAt > nowUtc)
            {
                createdAt = nowUtc.AddMinutes(-30);
            }
            var spam = rating == 1;
            var reply = !spam && (rating <= 3 || i % 3 == 0);
            booking.Review = new Review
            {
                HomestayId = booking.HomestayId,
                Rating = rating,
                Comment = comment,
                IsHidden = spam,
                OwnerReply = reply ? OwnerReplies[rating <= 3 ? 1 : i % 2 == 0 ? 0 : 2] : null,
                RepliedAt = reply ? createdAt.AddHours(rng.Next(1, 25)) : null,
                CreatedAt = createdAt,
            };
        }
    }

    /// <summary>Chép ảnh bill mẫu vào nơi ReceiptFileStore đọc (chỉ khi chạy trong ứng dụng có IWebHostEnvironment).</summary>
    private static void CopySampleReceipt(IServiceProvider services)
    {
        if (services.GetService(typeof(IWebHostEnvironment)) is not IWebHostEnvironment env)
        {
            return;
        }
        var source = Path.Combine(env.ContentRootPath, "Data", "SampleFiles", SampleReceiptFile);
        var targetDir = Path.Combine(env.ContentRootPath, "App_Data", "receipts");
        var target = Path.Combine(targetDir, SampleReceiptFile);
        if (File.Exists(source) && !File.Exists(target))
        {
            Directory.CreateDirectory(targetDir);
            File.Copy(source, target);
        }
    }
}
