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
    /// <summary>Ảnh giấy tờ mẫu (ghi rõ "giấy tờ mẫu"), chép vào App_Data/receipts — thư mục ReceiptFileStore đọc khi mở giấy tờ.</summary>
    public const string SampleDocumentFile = "sample-document.png";

    public const int SampleRoomBlockCount = 6;

    private static async Task SeedOperationsAsync(ApplicationDbContext db, IServiceProvider services)
    {
        CopySampleFile(services, SampleDocumentFile, "receipts");
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
    /// Giấy tờ (BR-12): mỗi homestay có giấy phép kinh doanh, PCCC và ảnh thực tế (một giấy sắp hết hạn để demo cảnh báo);
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
            db.VerificationDocuments.Add(new VerificationDocument
            {
                HomestayId = homestay.Id,
                DocumentType = DocumentType.PropertyPhoto,
                FileUrl = SampleDocumentFile,
                OriginalFileName = $"anh-thuc-te-{i + 1:00}.png",
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

    /// <summary>Yêu cầu mẫu theo luồng Manager duyệt; lịch sử Applied đã phản ánh vào chính đơn.</summary>
    private static async Task SeedChangeRequestsAsync(ApplicationDbContext db, DateTime nowUtc, DateOnly today)
    {
        if (await db.BookingChangeRequests.AnyAsync()) return;

        var bookings = await db.Bookings
            .Include(b => b.Homestay).ThenInclude(h => h.Owner).ThenInclude(o => o.Person).ThenInclude(p => p.Account)
            .Include(b => b.Homestay).ThenInclude(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .Include(b => b.Nights).Include(b => b.RoomNights)
            .Include(b => b.Customer).ThenInclude(c => c.Person).ThenInclude(p => p.Account)
            .Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn > today.AddDays(2) && b.DiscountAmount == 0)
            .OrderBy(b => b.CheckIn).AsSplitQuery().ToListAsync();
        if (bookings.Count == 0) return;

        var manager = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account)
            .SingleAsync(e => e.EmployeeCode == "QL001");
        var consultant = await db.Employees.Include(e => e.Person).ThenInclude(p => p.Account)
            .SingleAsync(e => e.EmployeeCode == "NV001");
        var managerId = manager.Person.Account!.Id;
        var consultantId = consultant.Person.Account!.Id;
        var holidays = (await db.Holidays.Select(h => h.Date).ToListAsync()).ToHashSet();
        var rules = await db.CancellationRules.ToListAsync();
        var occupied = await db.RoomNights.AsNoTracking().ToListAsync();

        List<BookingNight> NightsOf(RoomType type, DateOnly start, DateOnly end)
        {
            var nights = new List<BookingNight>();
            for (var date = start; date < end; date = date.AddDays(1))
            {
                var dayType = PricingService.DayTypeOf(date, holidays);
                var price = type.Prices.Where(p => p.DayType == dayType && p.EffectiveFrom <= date)
                    .MaxBy(p => p.EffectiveFrom)?.Price ?? throw new InvalidOperationException("Seed thiếu giá đêm.");
                nights.Add(new BookingNight { Date = date, DayType = dayType, Price = price });
            }
            return nights;
        }

        // Cùng công thức BR-04, nhưng lấy ngày gửi mẫu thay cho đồng hồ lúc chạy seed.
        decimal RefundAt(Booking b, DateTime createdAt)
        {
            var sentOn = DateOnly.FromDateTime(VietnamTime.ToLocal(createdAt));
            var daysBefore = b.CheckIn.DayNumber - sentOn.DayNumber;
            var percent = rules.Where(r => r.PolicyId == b.CancellationPolicyId && r.MinDaysBefore <= daysBefore)
                .MaxBy(r => r.MinDaysBefore)?.RefundPercent ?? 0;
            return PricingService.RoundToThousand(b.AmountDue * percent / 100m);
        }

        BookingChangeRequest Quote(Booking b, ChangeRequestType type, DateOnly? start, DateOnly? end, RoomType? roomType, DateTime createdAt)
        {
            var cancel = type == ChangeRequestType.Cancel;
            var total = cancel ? 0 : NightsOf(roomType!, start ?? b.CheckIn, end ?? b.CheckOut).Sum(n => n.Price);
            var fee = type is ChangeRequestType.ChangeDates or ChangeRequestType.ChangeDatesAndRoomType
                ? b.AmountDue - RefundAt(b, createdAt) : 0;
            var carried = cancel ? 0 : b.AmountDue - fee;
            var customerId = b.Customer.Person.Account?.Id;
            return new BookingChangeRequest
            {
                Booking = b, RequestType = type,
                RequestedByType = customerId is null ? ActorType.Employee : ActorType.Customer,
                RequestedByAccountId = customerId ?? consultantId,
                NewCheckIn = start, NewCheckOut = end,
                NewRoomTypeId = type is ChangeRequestType.ChangeRoomType or ChangeRequestType.ChangeDatesAndRoomType ? roomType!.Id : null,
                NewTotalAmount = total, ChangeFee = fee, DepositCarriedOver = carried,
                AdditionalDeposit = cancel ? 0 : Math.Max(0, PricingService.RoundToThousand(b.DepositRate * total) - carried),
                PriceDifference = cancel ? null : total - b.TotalAmount,
                RefundAmount = cancel ? RefundAt(b, createdAt) : null, CreatedAt = createdAt,
            };
        }

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        var requests = new List<BookingChangeRequest>();
        var first = bookings[0];
        requests.Add(Quote(first, ChangeRequestType.ChangeDates, first.CheckIn.AddDays(7), first.CheckOut.AddDays(7),
            first.Homestay.RoomTypes.Single(t => t.Id == first.RoomTypeId), nowUtc.AddHours(-3)));

        if (bookings.Count > 1)
        {
            var b = bookings[1];
            var rejected = Quote(b, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(1), b.CheckOut.AddDays(1),
                b.Homestay.RoomTypes.Single(t => t.Id == b.RoomTypeId), nowUtc.AddHours(-30));
            rejected.RequestedByType = ActorType.Employee;
            rejected.RequestedByAccountId = consultantId;
            rejected.Status = ChangeRequestStatus.Rejected;
            rejected.DecidedByAccountId = managerId;
            rejected.DecidedAt = rejected.CreatedAt.AddHours(2);
            rejected.DecisionNote = rejected.RejectionReason = "Ngày mới homestay đã kín phòng, mong khách thông cảm.";
            requests.Add(rejected);
        }

        Booking? appliedBooking = null;
        foreach (var b in bookings.Skip(2))
        {
            var type = b.Homestay.RoomTypes.Single(t => t.Id == b.RoomTypeId);
            if (b.ConfirmedAt is null || b.ConfirmedAt > nowUtc.AddHours(-2)) continue;
            var createdAt = b.ConfirmedAt.Value.AddMinutes(1);
            if (RefundAt(b, createdAt) != b.AmountDue) continue;
            foreach (var shift in new[] { 7, 14, -7 })
            {
                var start = b.CheckIn.AddDays(shift);
                var end = b.CheckOut.AddDays(shift);
                if (start < today.AddDays(3) || end > today.AddDays(90)
                    || occupied.Any(n => n.RoomId == b.RoomId && n.Date >= start && n.Date < end && n.BookingId != b.Id)) continue;
                var request = Quote(b, ChangeRequestType.ChangeDates, start, end, type, createdAt);
                if (request.AdditionalDeposit != 0 || request.NewTotalAmount < request.DepositCarriedOver) continue;

                var oldValues = System.Text.Json.JsonSerializer.Serialize(new { b.CheckIn, b.CheckOut, b.TotalAmount, b.Status, b.ConfirmedAt });
                request.Status = ChangeRequestStatus.Applied;
                request.DecidedByAccountId = managerId;
                request.DecidedAt = createdAt.AddHours(1);
                request.DecisionNote = "Đồng ý đổi ngày, không cần nộp thêm cọc.";
                request.NewRoomId = b.RoomId;
                db.BookingNights.RemoveRange(b.Nights);
                db.RoomNights.RemoveRange(b.RoomNights);
                // Lưu phần xóa trước khi thêm lại các khóa (BookingId, Date) có thể giao nhau.
                await db.SaveChangesAsync();
                b.Nights = NightsOf(type, start, end);
                b.RoomNights = b.Nights.Select(n => new RoomNight { RoomId = b.RoomId, Date = n.Date }).ToList();
                b.CheckIn = start; b.CheckOut = end; b.TotalAmount = request.NewTotalAmount!.Value;
                b.DepositAmount = request.DepositCarriedOver!.Value;
                b.ChangeFeeAmount += request.ChangeFee!.Value;
                b.Status = BookingStatus.Deposited; b.ConfirmedAt = null;
                b.ConfirmDeadlineAt = request.DecidedAt.Value.AddMinutes(30); b.ConfirmAlertSentAt = null;
                b.UpdatedAt = request.DecidedAt.Value;
                b.Histories.Add(new BookingStatusHistory { FromStatus = BookingStatus.Confirmed, ToStatus = BookingStatus.Deposited,
                    ActorType = ActorType.Employee, ActorAccountId = managerId, ChangedAt = b.UpdatedAt, Note = request.DecisionNote });
                db.AuditLogs.Add(new AuditLog { AccountId = managerId, TableName = "Bookings", RecordId = b.Id.ToString(),
                    Action = AuditAction.Update, OldValues = oldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new { b.CheckIn, b.CheckOut, b.TotalAmount, b.Status, b.ConfirmedAt }), CreatedAt = b.UpdatedAt });
                if (b.Homestay.Owner.Person.Account is { } owner)
                    db.Notifications.Add(new Notification { UserId = owner.Id, Message = $"Đơn {b.Code} đã đổi ngày, vui lòng xác nhận lại.",
                        Url = $"/Owner/Bookings/Details/{b.Id}", CreatedAt = b.UpdatedAt });
                requests.Add(request); appliedBooking = b;
                break;
            }
            if (appliedBooking is not null) break;
        }
        if (bookings.Count > 2 && appliedBooking is null)
            throw new InvalidOperationException("Không tìm được mẫu đổi ngày miễn phí, không cần thêm cọc và còn phòng.");

        var upgrade = bookings.Skip(2).FirstOrDefault(b => b != appliedBooking
            && b.Homestay.RoomTypes.Any(t => t.Id != b.RoomTypeId && t.MaxGuests >= b.Guests));
        if (upgrade is not null)
        {
            var type = upgrade.Homestay.RoomTypes.Where(t => t.Id != upgrade.RoomTypeId && t.MaxGuests >= upgrade.Guests).OrderBy(t => t.Id).First();
            requests.Add(Quote(upgrade, ChangeRequestType.ChangeRoomType, null, null, type, nowUtc.AddHours(-5)));
        }
        var cancelBooking = bookings.Skip(2).FirstOrDefault(b => b != appliedBooking && b != upgrade);
        if (cancelBooking is not null)
        {
            var cancel = Quote(cancelBooking, ChangeRequestType.Cancel, null, null, null, nowUtc.AddHours(-1));
            cancel.RequestedByType = ActorType.Employee; cancel.RequestedByAccountId = consultantId;
            requests.Add(cancel);
        }
        db.BookingChangeRequests.AddRange(requests);
        await db.SaveChangesAsync();
        foreach (var request in requests)
            db.AuditLogs.Add(new AuditLog { AccountId = request.DecidedByAccountId ?? request.RequestedByAccountId,
                TableName = "BookingChangeRequests", RecordId = request.Id.ToString(), Action = AuditAction.Insert,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { request.Status, request.RequestType, request.NewTotalAmount,
                    request.ChangeFee, request.DepositCarriedOver, request.AdditionalDeposit, request.DecisionNote, request.RejectionReason }), CreatedAt = request.DecidedAt ?? request.CreatedAt });
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
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
