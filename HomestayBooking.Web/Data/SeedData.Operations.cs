using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Data;

/// <summary>
/// Dữ liệu mẫu cho các màn hình vận hành: bảng đối soát các kỳ đã qua, khóa ngày của chủ homestay,
/// giấy tờ xác minh, yêu cầu đổi/hủy đơn và một giao dịch đã hoàn lại.
/// Mỗi phần tự kiểm tra trước nên thêm được vào CSDL đã có đơn mẫu mà không cần tạo lại.
/// </summary>
public static partial class SeedData
{
    /// <summary>Ảnh giấy tờ mẫu (ghi rõ "giấy tờ mẫu"), chép vào App_Data/documents khi seed chạy trong ứng dụng.</summary>
    public const string SampleDocumentFile = "sample-document.png";

    public const int SampleRoomBlockCount = 6;

    private static async Task SeedOperationsAsync(ApplicationDbContext db, IServiceProvider services)
    {
        CopySampleFile(services, SampleDocumentFile, "documents");
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(VietnamTime.ToLocal(nowUtc));
        var accountant = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account)
            .SingleAsync(e => e.EmployeeCode == "KT001");

        await SeedSettlementsAsync(db, accountant, nowUtc, today);
        await SeedRoomBlocksAsync(db, today);
        await SeedVerificationDocumentsAsync(db, today);
        await SeedChangeRequestsAsync(db, nowUtc, today);
        await SeedRefundedTransactionAsync(db, accountant, nowUtc);
    }

    /// <summary>
    /// Đối soát mọi kỳ đã kết thúc (D7). Kỳ cũ đã trả xong: gửi chủ homestay sau 2 ngày, chủ xác nhận sau 3 ngày, Fuji chuyển sau 5 ngày.
    /// Kỳ gần nhất xoay vòng Nháp / Đã gửi / Chủ đã xác nhận để demo đủ bước, bất kể ngày chạy seed.
    /// </summary>
    private static async Task SeedSettlementsAsync(ApplicationDbContext db, Employee accountant, DateTime nowUtc, DateOnly today)
    {
        if (await db.Settlements.AnyAsync())
        {
            return;
        }
        var bookings = await db.Bookings
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.NoShow || b.Status == BookingStatus.Cancelled)
            .ToListAsync();
        var groups = bookings
            .Where(b => SettlementCalculator.PeriodOf(b.CheckOut).End < today)
            .GroupBy(b => (b.HomestayId, Period: SettlementCalculator.PeriodOf(b.CheckOut)))
            .OrderBy(g => g.Key.Period.Start).ThenBy(g => g.Key.HomestayId)
            .ToList();
        var latest = groups.Count == 0 ? (DateOnly?)null : groups.Max(g => g.Key.Period.Start);

        var latestIndex = 0;
        foreach (var group in groups)
        {
            var settlement = SettlementCalculator.Build(group.Key.HomestayId, group.Key.Period, group);
            if (settlement.Items.Count == 0)
            {
                continue;
            }
            var end = group.Key.Period.End;
            DateTime Step(int days, int hour) => Min(VnToUtc(end.AddDays(days), hour), nowUtc.AddHours(-(6 - days)));
            settlement.CreatedAt = Step(1, 8);
            settlement.UpdatedAt = settlement.CreatedAt;
            var stage = group.Key.Period.Start == latest ? latestIndex++ % 3 : 3;
            if (stage >= 1)
            {
                settlement.Status = SettlementStatus.Sent;
                settlement.SentAt = settlement.UpdatedAt = Step(2, 9);
            }
            if (stage >= 2)
            {
                settlement.Status = SettlementStatus.Confirmed;
                settlement.ConfirmedAt = settlement.UpdatedAt = Step(3, 10);
            }
            if (stage >= 3)
            {
                settlement.Status = SettlementStatus.Paid;
                settlement.PaidAt = settlement.UpdatedAt = Step(5, 15);
                settlement.PaidByEmployeeId = accountant.Id;
                settlement.PaidBankRef = $"FT{end:yyMMdd}{settlement.HomestayId:000}";
            }
            db.Settlements.Add(settlement);
        }
        await db.SaveChangesAsync();
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    /// <summary>Chủ homestay khóa ngày: khách đặt qua kênh khác, bảo trì. Chỉ khóa đêm còn trống.</summary>
    private static async Task SeedRoomBlocksAsync(ApplicationDbContext db, DateOnly today)
    {
        if (await db.RoomBlocks.AnyAsync())
        {
            return;
        }
        var rooms = await db.Rooms
            .Include(r => r.RoomType).ThenInclude(t => t.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .OrderBy(r => r.Id)
            .ToListAsync();
        var occupied = (await db.RoomNights.Select(n => new { n.RoomId, n.Date }).ToListAsync())
            .Select(n => (n.RoomId, n.Date)).ToHashSet();
        (int StartOffset, int Nights, RoomBlockReason Reason, string Note)[] plans =
        [
            (-20, 3, RoomBlockReason.Maintenance, "Sơn lại tường, thay rèm cửa."),
            (2, 2, RoomBlockReason.OtherChannel, "Khách quen đặt qua Zalo của chủ nhà."),
            (5, 1, RoomBlockReason.OtherChannel, "Khách đặt qua Facebook."),
            (11, 4, RoomBlockReason.Maintenance, "Bảo dưỡng điều hòa và bình nóng lạnh."),
            (18, 2, RoomBlockReason.OtherChannel, "Đoàn khách công ty đặt trực tiếp."),
            (30, 3, RoomBlockReason.OtherChannel, "Khách đặt qua Facebook."),
        ];
        var rng = new Random(2027);
        foreach (var plan in plans)
        {
            var start = today.AddDays(plan.StartOffset);
            var dates = Enumerable.Range(0, plan.Nights).Select(start.AddDays).ToList();
            var room = rooms.OrderBy(_ => rng.Next())
                .FirstOrDefault(r => r.RoomType.Homestay.Owner.Person.Account is not null && dates.All(d => !occupied.Contains((r.Id, d))));
            if (room is null)
            {
                continue;
            }
            var block = new RoomBlock
            {
                RoomId = room.Id,
                StartDate = start,
                EndDate = start.AddDays(plan.Nights),
                Reason = plan.Reason,
                Note = plan.Note,
                CreatedByAccountId = room.RoomType.Homestay.Owner.Person.Account!.Id,
                CreatedAt = VnToUtc(start.AddDays(-7), 20),
            };
            foreach (var date in dates)
            {
                occupied.Add((room.Id, date));
                block.Nights.Add(new RoomNight { RoomId = room.Id, Date = date });
            }
            db.RoomBlocks.Add(block);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Giấy tờ (BR-12): mỗi homestay có giấy phép kinh doanh lưu trú và giấy chứng nhận PCCC (một giấy sắp hết hạn để demo cảnh báo);
    /// hồ sơ Owner có bản chụp CCCD, hồ sơ đang chờ duyệt có đủ bộ giấy tờ để nhân viên thẩm định.
    /// </summary>
    private static async Task SeedVerificationDocumentsAsync(ApplicationDbContext db, DateOnly today)
    {
        if (await db.VerificationDocuments.AnyAsync())
        {
            return;
        }
        var homestays = await db.Homestays.OrderBy(h => h.Id).ToListAsync();
        for (var i = 0; i < homestays.Count; i++)
        {
            var homestay = homestays[i];
            db.VerificationDocuments.Add(new VerificationDocument
            {
                HomestayId = homestay.Id,
                DocumentType = DocumentType.BusinessLicense,
                FileUrl = SampleDocumentFile,
                OriginalFileName = $"giay-phep-kinh-doanh-{i + 1:00}.png",
                ExpiryDate = ContractStart.AddYears(3),
                CreatedAt = homestay.CreatedAt,
            });
            db.VerificationDocuments.Add(new VerificationDocument
            {
                HomestayId = homestay.Id,
                DocumentType = DocumentType.FireSafety,
                FileUrl = SampleDocumentFile,
                OriginalFileName = $"chung-nhan-pccc-{i + 1:00}.png",
                ExpiryDate = i == 0 ? today.AddDays(20) : ContractStart.AddYears(2),
                CreatedAt = homestay.CreatedAt,
            });
        }

        var registrations = await db.OwnerRegistrations.OrderBy(r => r.Id).ToListAsync();
        foreach (var registration in registrations)
        {
            var created = registration.SubmittedAt ?? registration.CreatedAt;
            db.VerificationDocuments.Add(new VerificationDocument
            {
                OwnerRegistrationId = registration.Id,
                DocumentType = DocumentType.Other,
                FileUrl = SampleDocumentFile,
                OriginalFileName = "cccd-mat-truoc.png",
                CreatedAt = created,
            });
            if (registration.Status == OwnerRegistrationStatus.PendingReview)
            {
                foreach (var (type, name) in new[]
                {
                    (DocumentType.BusinessLicense, "giay-phep-kinh-doanh.png"),
                    (DocumentType.FireSafety, "chung-nhan-pccc.png"),
                    (DocumentType.PropertyPhoto, "anh-thuc-te-mat-tien.png"),
                })
                {
                    db.VerificationDocuments.Add(new VerificationDocument
                    {
                        OwnerRegistrationId = registration.Id,
                        DocumentType = type,
                        FileUrl = SampleDocumentFile,
                        OriginalFileName = name,
                        ExpiryDate = type == DocumentType.PropertyPhoto ? null : today.AddYears(2),
                        CreatedAt = created,
                    });
                }
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Yêu cầu đổi/hủy trên các đơn đã xác nhận sắp tới: đổi ngày (chờ duyệt, bị từ chối, đã duyệt), đổi loại phòng, hủy (xem trước tiền hoàn).
    /// </summary>
    private static async Task SeedChangeRequestsAsync(ApplicationDbContext db, DateTime nowUtc, DateOnly today)
    {
        if (await db.BookingChangeRequests.AnyAsync())
        {
            return;
        }
        var bookings = await db.Bookings
            .Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .Include(b => b.Homestay).ThenInclude(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .Include(b => b.Nights)
            .Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
            .Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn > today.AddDays(2))
            .OrderBy(b => b.CheckIn)
            .AsSplitQuery()
            .ToListAsync();
        if (bookings.Count == 0)
        {
            return;
        }
        var holidays = (await db.Holidays.Select(h => h.Date).ToListAsync()).ToHashSet();
        var rules = await db.CancellationRules.ToListAsync();

        decimal PriceOf(RoomType roomType, DateOnly checkIn, DateOnly checkOut)
        {
            var total = 0m;
            for (var night = checkIn; night < checkOut; night = night.AddDays(1))
            {
                var dayType = PricingService.DayTypeOf(night, holidays);
                total += roomType.Prices.Where(p => p.DayType == dayType && p.EffectiveFrom <= night).MaxBy(p => p.EffectiveFrom)?.Price ?? 0;
            }
            return total;
        }

        BookingChangeRequest ChangeDates(Booking b, int shiftDays, double hoursAgo)
        {
            var newIn = b.CheckIn.AddDays(shiftDays);
            var newOut = b.CheckOut.AddDays(shiftDays);
            var roomType = b.Homestay.RoomTypes.Single(t => t.Id == b.RoomTypeId);
            return new BookingChangeRequest
            {
                Booking = b,
                RequestType = ChangeRequestType.ChangeDates,
                RequestedByType = ActorType.Customer,
                NewCheckIn = newIn,
                NewCheckOut = newOut,
                PriceDifference = PriceOf(roomType, newIn, newOut) - b.TotalAmount,
                CreatedAt = nowUtc.AddHours(-hoursAgo),
            };
        }

        var requests = new List<BookingChangeRequest>();
        var owner = (Booking b) => b.Homestay.Owner.Person.Account?.Id;

        // 1. Khách xin lùi lịch 7 ngày — chờ chủ homestay duyệt
        requests.Add(ChangeDates(bookings[0], 7, 3));

        // 2. Nhân viên tư vấn gửi giúp khách xin lùi 1 ngày — chủ homestay từ chối vì kín phòng
        if (bookings.Count > 1)
        {
            var rejected = ChangeDates(bookings[1], 1, 30);
            rejected.RequestedByType = ActorType.Employee;
            rejected.Status = ChangeRequestStatus.Rejected;
            rejected.DecidedByAccountId = owner(bookings[1]);
            rejected.DecidedAt = rejected.CreatedAt.AddHours(2);
            rejected.DecisionNote = "Ngày mới homestay đã kín phòng, mong khách thông cảm.";
            requests.Add(rejected);
        }

        // 3. Đã duyệt: đơn đã mang ngày mới, yêu cầu giữ lại làm lịch sử
        if (bookings.Count > 2)
        {
            var b = bookings[2];
            var roomType = b.Homestay.RoomTypes.Single(t => t.Id == b.RoomTypeId);
            var oldIn = b.CheckIn.AddDays(-3);
            requests.Add(new BookingChangeRequest
            {
                Booking = b,
                RequestType = ChangeRequestType.ChangeDates,
                RequestedByType = ActorType.Customer,
                NewCheckIn = b.CheckIn,
                NewCheckOut = b.CheckOut,
                PriceDifference = b.TotalAmount - PriceOf(roomType, oldIn, oldIn.AddDays(b.CheckOut.DayNumber - b.CheckIn.DayNumber)),
                Status = ChangeRequestStatus.Approved,
                DecidedByAccountId = owner(b),
                DecidedAt = nowUtc.AddDays(-2).AddHours(1),
                DecisionNote = "Đồng ý đổi ngày.",
                CreatedAt = nowUtc.AddDays(-2),
            });
        }

        // 4. Đổi sang loại phòng khác cùng homestay — chờ duyệt
        var upgrade = bookings.Skip(3).FirstOrDefault(b => b.Homestay.RoomTypes.Any(t => t.Id != b.RoomTypeId && t.MaxGuests >= b.Guests));
        if (upgrade is not null)
        {
            var newType = upgrade.Homestay.RoomTypes.Where(t => t.Id != upgrade.RoomTypeId && t.MaxGuests >= upgrade.Guests).OrderBy(t => t.Id).First();
            requests.Add(new BookingChangeRequest
            {
                Booking = upgrade,
                RequestType = ChangeRequestType.ChangeRoomType,
                RequestedByType = ActorType.Customer,
                NewRoomTypeId = newType.Id,
                PriceDifference = PriceOf(newType, upgrade.CheckIn, upgrade.CheckOut) - upgrade.TotalAmount,
                CreatedAt = nowUtc.AddHours(-5),
            });
        }

        // 5. Nhân viên gửi yêu cầu hủy giúp khách, kèm số tiền hoàn dự kiến theo chính sách (FR-05)
        var cancel = bookings.Skip(3).FirstOrDefault(b => b != upgrade);
        if (cancel is not null)
        {
            var daysBefore = cancel.CheckIn.DayNumber - today.DayNumber;
            var percent = rules.Where(r => r.PolicyId == cancel.CancellationPolicyId && r.MinDaysBefore <= daysBefore)
                .MaxBy(r => r.MinDaysBefore)?.RefundPercent ?? 0;
            requests.Add(new BookingChangeRequest
            {
                Booking = cancel,
                RequestType = ChangeRequestType.Cancel,
                RequestedByType = ActorType.Employee,
                RefundAmount = PricingService.RoundToThousand(cancel.AmountDue * percent / 100m),
                CreatedAt = nowUtc.AddHours(-1),
            });
        }

        db.BookingChangeRequests.AddRange(requests);
        await db.SaveChangesAsync();
    }

    /// <summary>Khoản tiền chuyển nhầm không thuộc đơn nào, kế toán đã chuyển trả (D13: Refunded).</summary>
    private static async Task SeedRefundedTransactionAsync(ApplicationDbContext db, Employee accountant, DateTime nowUtc)
    {
        if (await db.BankTransactions.AnyAsync(t => t.MatchStatus == BankMatchStatus.Refunded))
        {
            return;
        }
        var received = nowUtc.AddDays(-15);
        db.BankTransactions.Add(new BankTransaction
        {
            Provider = BankProvider.SePay,
            ProviderTransactionId = "SEED-R001",
            AccountNumber = "0338405548",
            Amount = 300_000,
            TransferContent = "TRAN THI HOA chuyen tien",
            TransactionTime = received,
            ReceivedAt = received,
            MatchStatus = BankMatchStatus.Refunded,
            ProcessedByEmployeeId = accountant.Id,
            ProcessedAt = received.AddDays(1),
            RefundedAt = received.AddDays(1),
            RefundBankRef = $"FT{VietnamTime.ToLocal(received.AddDays(1)):yyMMdd}0001",
            Note = "Khách chuyển nhầm tài khoản, đã gọi xác nhận và chuyển trả.",
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Chép file mẫu vào App_Data/{folder} (chỉ khi chạy trong ứng dụng có IWebHostEnvironment).</summary>
    private static void CopySampleFile(IServiceProvider services, string fileName, string folder)
    {
        if (services.GetService(typeof(IWebHostEnvironment)) is not IWebHostEnvironment env)
        {
            return;
        }
        var source = Path.Combine(env.ContentRootPath, "Data", "SampleFiles", fileName);
        var targetDir = Path.Combine(env.ContentRootPath, "App_Data", folder);
        var target = Path.Combine(targetDir, fileName);
        if (File.Exists(source) && !File.Exists(target))
        {
            Directory.CreateDirectory(targetDir);
            File.Copy(source, target);
        }
    }
}
