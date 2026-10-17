-- Đối chiếu dữ liệu nghiệp vụ, chỉ đọc; chạy bằng sqlcmd -C -b -i tests/data_consistency.sql.
-- Không sửa dữ liệu, không mở transaction, không giới hạn tên database.
-- Tên trạng thái theo Enums.cs và BookingStatusExtensions.HoldsRoom(); sửa enum thì sửa script.
-- Completed vẫn giữ lịch phòng theo HoldsRoom(); Cancelled/Rejected/Expired phải nhả phòng.
-- Không kiểm giấy tờ mới trên toàn bộ homestay cũ (D17, mục H của B1).
-- Ba loại giá Weekday/Weekend/Holiday theo DayType; sửa enum thì sửa script.
-- Ngày hợp đồng tính theo giờ Việt Nam; mỗi mục đếm dòng vi phạm trong bảng được nêu.
-- Mục 35 không áp dụng cho database rỗng như ConstraintTest (không có nhân viên).
;WITH KiemTra AS (
    SELECT 1 AS STT, N'Đơn khác homestay của loại phòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN RoomTypes t ON t.Id = b.RoomTypeId WHERE b.HomestayId <> t.HomestayId
    UNION ALL
    SELECT 2 AS STT, N'Đơn khác loại phòng của phòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Rooms r ON r.Id = b.RoomId WHERE b.RoomTypeId <> r.RoomTypeId
    UNION ALL
    SELECT 3 AS STT, N'Đêm giữ khác phòng của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN Bookings b ON b.Id = n.BookingId WHERE n.RoomId <> b.RoomId
    UNION ALL
    SELECT 4 AS STT, N'Đêm giữ ngoài khoảng lưu trú' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN Bookings b ON b.Id = n.BookingId WHERE n.[Date] < b.CheckIn OR n.[Date] >= b.CheckOut
    UNION ALL
    SELECT 5 AS STT, N'Đơn giữ phòng thiếu hoặc thừa đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.Status IN ('AwaitingPayment','PaymentSubmitted','Deposited','Confirmed','CheckedIn','Completed') AND (SELECT COUNT_BIG(*) FROM RoomNights n WHERE n.BookingId = b.Id) <> DATEDIFF(day, b.CheckIn, b.CheckOut)
    UNION ALL
    SELECT 6 AS STT, N'Đơn hủy hoặc kết thúc vẫn giữ đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.Status IN ('Cancelled','Rejected','Expired') AND EXISTS (SELECT 1 FROM RoomNights n WHERE n.BookingId = b.Id)
    UNION ALL
    SELECT 7 AS STT, N'Số đêm giá khác số đêm của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE (SELECT COUNT_BIG(*) FROM BookingNights n WHERE n.BookingId = b.Id) <> DATEDIFF(day, b.CheckIn, b.CheckOut)
    UNION ALL
    SELECT 8 AS STT, N'Tổng tiền khác tổng giá từng đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.TotalAmount <> COALESCE((SELECT SUM(n.Price) FROM BookingNights n WHERE n.BookingId = b.Id), 0)
    UNION ALL
    SELECT 9 AS STT, N'Đánh giá khác homestay của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r JOIN Bookings b ON b.Id = r.BookingId WHERE r.HomestayId <> b.HomestayId
    UNION ALL
    SELECT 10 AS STT, N'Đánh giá đơn chưa hoàn thành' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r JOIN Bookings b ON b.Id = r.BookingId WHERE b.Status <> 'Completed'
    UNION ALL
    SELECT 11 AS STT, N'Homestay Active thiếu hợp đồng Active' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM Contracts c WHERE c.HomestayId = h.Id AND c.Status = 'Active')
    UNION ALL
    SELECT 12 AS STT, N'Homestay Active có chủ chưa được duyệt' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM OwnerRegistrations r WHERE r.OwnerId = h.OwnerId AND r.Status = 'Approved')
    UNION ALL
    SELECT 13 AS STT, N'Hợp đồng Active đã hết hạn (job B4 chưa chạy)' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Contracts c WHERE c.Status = 'Active' AND c.EndDate < CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time')
    UNION ALL
    SELECT 14 AS STT, N'Tiền trả đối soát khác tổng chi tiết' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.PayoutAmount <> COALESCE((SELECT SUM(i.PayoutAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
    UNION ALL
    SELECT 15 AS STT, N'Homestay Active thiếu ảnh bìa' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM HomestayImages i WHERE i.HomestayId = h.Id AND i.IsCover = 1)
    UNION ALL
    SELECT 16 AS STT, N'Loại phòng Active thiếu giá hiệu lực hôm nay' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomTypes t JOIN Homestays h ON h.Id = t.HomestayId
    WHERE t.IsActive = 1 AND h.Status = 'Active' AND (
        NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Weekday' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time'))
        OR NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Weekend' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time'))
        OR NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Holiday' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time')))
    UNION ALL
    SELECT 17 AS STT, N'Homestay Active thiếu loại phòng có phòng đang bán' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (
        SELECT 1 FROM RoomTypes t WHERE t.HomestayId = h.Id AND t.IsActive = 1
        AND EXISTS (SELECT 1 FROM Rooms r WHERE r.RoomTypeId = t.Id AND r.IsActive = 1))
    UNION ALL
    SELECT 18 AS STT, N'Đêm khóa ngoài khoảng khóa ngày' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN RoomBlocks b ON b.Id = n.RoomBlockId WHERE n.[Date] < b.StartDate OR n.[Date] >= b.EndDate
    UNION ALL
    SELECT 19 AS STT, N'Ảnh gắn loại phòng của homestay khác' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM HomestayImages i JOIN RoomTypes t ON t.Id = i.RoomTypeId WHERE i.HomestayId <> t.HomestayId
    UNION ALL
    SELECT 20 AS STT, N'Đơn có đơn thay thế nhưng chưa hủy' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.ReplacedByBookingId IS NOT NULL AND b.Status <> 'Cancelled'
    UNION ALL
    SELECT 21 AS STT, N'Đơn thay thế khác khách hoặc ngày lưu trú' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Bookings n ON n.Id = b.ReplacedByBookingId
    WHERE b.CustomerId <> n.CustomerId OR b.CheckIn <> n.CheckIn OR b.CheckOut <> n.CheckOut
    UNION ALL
    SELECT 22 AS STT, N'Khoản bù không gắn đơn thay thế' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Payments p WHERE p.Type = 'Compensation' AND NOT EXISTS (SELECT 1 FROM Bookings b WHERE b.ReplacedByBookingId = p.BookingId)
    UNION ALL
    SELECT 23 AS STT, N'Đơn giao nhân viên không đúng vị trí' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Employees e ON e.Id = b.AssignedEmployeeId
    WHERE e.Position NOT IN ('Consultant','Manager','Administrator')
    UNION ALL
    SELECT 24 AS STT, N'Trả lời đánh giá chỉ có khoảng trắng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r WHERE r.OwnerReply IS NOT NULL AND LEN(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(r.OwnerReply, CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ')))) = 0
    UNION ALL
    SELECT 25 AS STT, N'Tin Sent thiếu giờ gửi hoặc Failed thiếu lỗi' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM MessageLogs m WHERE (m.Status = 'Sent' AND m.SentAt IS NULL)
        OR (m.Status = 'Failed' AND (m.ErrorMessage IS NULL OR LTRIM(RTRIM(m.ErrorMessage)) = ''))
    UNION ALL
    SELECT 26 AS STT, N'Tin thiếu cả số điện thoại và email người nhận' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM MessageLogs m WHERE (m.RecipientPhone IS NULL OR LTRIM(RTRIM(m.RecipientPhone)) = '')
        AND (m.RecipientEmail IS NULL OR LTRIM(RTRIM(m.RecipientEmail)) = '')
    UNION ALL
    SELECT 27 AS STT, N'Dòng đối soát khác homestay của bảng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Settlements s ON s.Id = i.SettlementId JOIN Bookings b ON b.Id = i.BookingId
    WHERE b.HomestayId <> s.HomestayId
    UNION ALL
    SELECT 28 AS STT, N'Ngày trả phòng ngoài kỳ đối soát' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Settlements s ON s.Id = i.SettlementId JOIN Bookings b ON b.Id = i.BookingId
    WHERE b.CheckOut < DATEFROMPARTS(s.[Year], s.[Month], CASE WHEN s.PeriodNo = 1 THEN 1 ELSE 16 END)
       OR b.CheckOut > CASE WHEN s.PeriodNo = 1 THEN DATEFROMPARTS(s.[Year], s.[Month], 15) ELSE EOMONTH(DATEFROMPARTS(s.[Year], s.[Month], 1)) END
    UNION ALL
    SELECT 29 AS STT, N'Đơn đã thay thế vẫn có dòng đối soát' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Bookings b ON b.Id = i.BookingId WHERE b.ReplacedByBookingId IS NOT NULL
    UNION ALL
    SELECT 30 AS STT, N'Tổng tiền phòng, giữ, hoa hồng khác tổng dòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.TotalRoomAmount <> COALESCE((SELECT SUM(i.RoomAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
       OR s.TotalRetained <> COALESCE((SELECT SUM(i.RetainedAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
       OR s.TotalCommission <> COALESCE((SELECT SUM(i.CommissionAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
    UNION ALL
    SELECT 31 AS STT, N'Đối soát Paid thiếu mã chuyển hoặc người trả' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.Status = 'Paid' AND s.PayoutAmount > 0
       AND (s.PaidBankRef IS NULL OR LTRIM(RTRIM(s.PaidBankRef)) = '' OR s.PaidByEmployeeId IS NULL)
    UNION ALL
    SELECT 32 AS STT, N'Đối soát Confirmed thiếu giờ xác nhận' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.Status = 'Confirmed' AND s.ConfirmedAt IS NULL
    UNION ALL
    SELECT 33 AS STT, N'Khoản bù Approved sai trạng thái hoặc thiếu mã' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Payments p JOIN Bookings b ON b.Id = p.BookingId WHERE p.Type = 'Compensation' AND p.Status = 'Approved'
       AND (b.Status <> 'Completed' OR p.BankRef IS NULL OR LTRIM(RTRIM(p.BankRef)) = '')
    UNION ALL
    SELECT 34 AS STT, N'Nhân viên ngừng việc còn tài khoản Active' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Employees e JOIN AspNetUsers a ON a.PersonId = e.PersonId
    WHERE e.EmploymentStatus IN ('Suspended','Resigned') AND a.Status = 'Active'
    UNION ALL
    SELECT 35 AS STT, N'Không còn Administrator Active' AS Muc,
       CASE WHEN EXISTS (SELECT 1 FROM Employees) AND NOT EXISTS (
           SELECT 1 FROM Employees e JOIN AspNetUsers a ON a.PersonId = e.PersonId
           WHERE e.Position = 'Administrator' AND e.EmploymentStatus = 'Active' AND a.Status = 'Active'
       ) THEN CAST(1 AS bigint) ELSE CAST(0 AS bigint) END AS SoDongLech

)
SELECT CASE WHEN GROUPING(STT) = 1 THEN 99 ELSE STT END AS STT,
       CASE WHEN GROUPING(STT) = 1 THEN N'TỔNG (35 mục)' ELSE Muc END AS Muc,
       SUM(SoDongLech) AS SoDongLech,
       CASE WHEN SUM(SoDongLech) = 0 THEN N'OK' ELSE N'LỆCH' END AS KetQua
FROM KiemTra
GROUP BY GROUPING SETS ((STT, Muc), ())
ORDER BY STT;

-- Lặp lại truy vấn chỉ đọc để phát tín hiệu thất bại cho sqlcmd -b, không tạo bảng tạm.
-- Chạy khi ứng dụng dừng nếu cần kết quả ổn định giữa hai lần đọc.
IF EXISTS (
    SELECT 1 FROM (
    SELECT 1 AS STT, N'Đơn khác homestay của loại phòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN RoomTypes t ON t.Id = b.RoomTypeId WHERE b.HomestayId <> t.HomestayId
    UNION ALL
    SELECT 2 AS STT, N'Đơn khác loại phòng của phòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Rooms r ON r.Id = b.RoomId WHERE b.RoomTypeId <> r.RoomTypeId
    UNION ALL
    SELECT 3 AS STT, N'Đêm giữ khác phòng của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN Bookings b ON b.Id = n.BookingId WHERE n.RoomId <> b.RoomId
    UNION ALL
    SELECT 4 AS STT, N'Đêm giữ ngoài khoảng lưu trú' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN Bookings b ON b.Id = n.BookingId WHERE n.[Date] < b.CheckIn OR n.[Date] >= b.CheckOut
    UNION ALL
    SELECT 5 AS STT, N'Đơn giữ phòng thiếu hoặc thừa đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.Status IN ('AwaitingPayment','PaymentSubmitted','Deposited','Confirmed','CheckedIn','Completed') AND (SELECT COUNT_BIG(*) FROM RoomNights n WHERE n.BookingId = b.Id) <> DATEDIFF(day, b.CheckIn, b.CheckOut)
    UNION ALL
    SELECT 6 AS STT, N'Đơn hủy hoặc kết thúc vẫn giữ đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.Status IN ('Cancelled','Rejected','Expired') AND EXISTS (SELECT 1 FROM RoomNights n WHERE n.BookingId = b.Id)
    UNION ALL
    SELECT 7 AS STT, N'Số đêm giá khác số đêm của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE (SELECT COUNT_BIG(*) FROM BookingNights n WHERE n.BookingId = b.Id) <> DATEDIFF(day, b.CheckIn, b.CheckOut)
    UNION ALL
    SELECT 8 AS STT, N'Tổng tiền khác tổng giá từng đêm' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.TotalAmount <> COALESCE((SELECT SUM(n.Price) FROM BookingNights n WHERE n.BookingId = b.Id), 0)
    UNION ALL
    SELECT 9 AS STT, N'Đánh giá khác homestay của đơn' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r JOIN Bookings b ON b.Id = r.BookingId WHERE r.HomestayId <> b.HomestayId
    UNION ALL
    SELECT 10 AS STT, N'Đánh giá đơn chưa hoàn thành' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r JOIN Bookings b ON b.Id = r.BookingId WHERE b.Status <> 'Completed'
    UNION ALL
    SELECT 11 AS STT, N'Homestay Active thiếu hợp đồng Active' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM Contracts c WHERE c.HomestayId = h.Id AND c.Status = 'Active')
    UNION ALL
    SELECT 12 AS STT, N'Homestay Active có chủ chưa được duyệt' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM OwnerRegistrations r WHERE r.OwnerId = h.OwnerId AND r.Status = 'Approved')
    UNION ALL
    SELECT 13 AS STT, N'Hợp đồng Active đã hết hạn (job B4 chưa chạy)' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Contracts c WHERE c.Status = 'Active' AND c.EndDate < CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time')
    UNION ALL
    SELECT 14 AS STT, N'Tiền trả đối soát khác tổng chi tiết' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.PayoutAmount <> COALESCE((SELECT SUM(i.PayoutAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
    UNION ALL
    SELECT 15 AS STT, N'Homestay Active thiếu ảnh bìa' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM HomestayImages i WHERE i.HomestayId = h.Id AND i.IsCover = 1)
    UNION ALL
    SELECT 16 AS STT, N'Loại phòng Active thiếu giá hiệu lực hôm nay' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomTypes t JOIN Homestays h ON h.Id = t.HomestayId
    WHERE t.IsActive = 1 AND h.Status = 'Active' AND (
        NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Weekday' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time'))
        OR NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Weekend' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time'))
        OR NOT EXISTS (SELECT 1 FROM RoomTypePrices p WHERE p.RoomTypeId = t.Id AND p.DayType = 'Holiday' AND p.EffectiveFrom <= CONVERT(date, SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time')))
    UNION ALL
    SELECT 17 AS STT, N'Homestay Active thiếu loại phòng có phòng đang bán' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Homestays h WHERE h.Status = 'Active' AND NOT EXISTS (
        SELECT 1 FROM RoomTypes t WHERE t.HomestayId = h.Id AND t.IsActive = 1
        AND EXISTS (SELECT 1 FROM Rooms r WHERE r.RoomTypeId = t.Id AND r.IsActive = 1))
    UNION ALL
    SELECT 18 AS STT, N'Đêm khóa ngoài khoảng khóa ngày' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM RoomNights n JOIN RoomBlocks b ON b.Id = n.RoomBlockId WHERE n.[Date] < b.StartDate OR n.[Date] >= b.EndDate
    UNION ALL
    SELECT 19 AS STT, N'Ảnh gắn loại phòng của homestay khác' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM HomestayImages i JOIN RoomTypes t ON t.Id = i.RoomTypeId WHERE i.HomestayId <> t.HomestayId
    UNION ALL
    SELECT 20 AS STT, N'Đơn có đơn thay thế nhưng chưa hủy' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b WHERE b.ReplacedByBookingId IS NOT NULL AND b.Status <> 'Cancelled'
    UNION ALL
    SELECT 21 AS STT, N'Đơn thay thế khác khách hoặc ngày lưu trú' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Bookings n ON n.Id = b.ReplacedByBookingId
    WHERE b.CustomerId <> n.CustomerId OR b.CheckIn <> n.CheckIn OR b.CheckOut <> n.CheckOut
    UNION ALL
    SELECT 22 AS STT, N'Khoản bù không gắn đơn thay thế' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Payments p WHERE p.Type = 'Compensation' AND NOT EXISTS (SELECT 1 FROM Bookings b WHERE b.ReplacedByBookingId = p.BookingId)
    UNION ALL
    SELECT 23 AS STT, N'Đơn giao nhân viên không đúng vị trí' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Bookings b JOIN Employees e ON e.Id = b.AssignedEmployeeId
    WHERE e.Position NOT IN ('Consultant','Manager','Administrator')
    UNION ALL
    SELECT 24 AS STT, N'Trả lời đánh giá chỉ có khoảng trắng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Reviews r WHERE r.OwnerReply IS NOT NULL AND LEN(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(r.OwnerReply, CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ')))) = 0
    UNION ALL
    SELECT 25 AS STT, N'Tin Sent thiếu giờ gửi hoặc Failed thiếu lỗi' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM MessageLogs m WHERE (m.Status = 'Sent' AND m.SentAt IS NULL)
        OR (m.Status = 'Failed' AND (m.ErrorMessage IS NULL OR LTRIM(RTRIM(m.ErrorMessage)) = ''))
    UNION ALL
    SELECT 26 AS STT, N'Tin thiếu cả số điện thoại và email người nhận' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM MessageLogs m WHERE (m.RecipientPhone IS NULL OR LTRIM(RTRIM(m.RecipientPhone)) = '')
        AND (m.RecipientEmail IS NULL OR LTRIM(RTRIM(m.RecipientEmail)) = '')
    UNION ALL
    SELECT 27 AS STT, N'Dòng đối soát khác homestay của bảng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Settlements s ON s.Id = i.SettlementId JOIN Bookings b ON b.Id = i.BookingId
    WHERE b.HomestayId <> s.HomestayId
    UNION ALL
    SELECT 28 AS STT, N'Ngày trả phòng ngoài kỳ đối soát' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Settlements s ON s.Id = i.SettlementId JOIN Bookings b ON b.Id = i.BookingId
    WHERE b.CheckOut < DATEFROMPARTS(s.[Year], s.[Month], CASE WHEN s.PeriodNo = 1 THEN 1 ELSE 16 END)
       OR b.CheckOut > CASE WHEN s.PeriodNo = 1 THEN DATEFROMPARTS(s.[Year], s.[Month], 15) ELSE EOMONTH(DATEFROMPARTS(s.[Year], s.[Month], 1)) END
    UNION ALL
    SELECT 29 AS STT, N'Đơn đã thay thế vẫn có dòng đối soát' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM SettlementItems i JOIN Bookings b ON b.Id = i.BookingId WHERE b.ReplacedByBookingId IS NOT NULL
    UNION ALL
    SELECT 30 AS STT, N'Tổng tiền phòng, giữ, hoa hồng khác tổng dòng' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.TotalRoomAmount <> COALESCE((SELECT SUM(i.RoomAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
       OR s.TotalRetained <> COALESCE((SELECT SUM(i.RetainedAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
       OR s.TotalCommission <> COALESCE((SELECT SUM(i.CommissionAmount) FROM SettlementItems i WHERE i.SettlementId = s.Id), 0)
    UNION ALL
    SELECT 31 AS STT, N'Đối soát Paid thiếu mã chuyển hoặc người trả' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.Status = 'Paid' AND s.PayoutAmount > 0
       AND (s.PaidBankRef IS NULL OR LTRIM(RTRIM(s.PaidBankRef)) = '' OR s.PaidByEmployeeId IS NULL)
    UNION ALL
    SELECT 32 AS STT, N'Đối soát Confirmed thiếu giờ xác nhận' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Settlements s WHERE s.Status = 'Confirmed' AND s.ConfirmedAt IS NULL
    UNION ALL
    SELECT 33 AS STT, N'Khoản bù Approved sai trạng thái hoặc thiếu mã' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Payments p JOIN Bookings b ON b.Id = p.BookingId WHERE p.Type = 'Compensation' AND p.Status = 'Approved'
       AND (b.Status <> 'Completed' OR p.BankRef IS NULL OR LTRIM(RTRIM(p.BankRef)) = '')
    UNION ALL
    SELECT 34 AS STT, N'Nhân viên ngừng việc còn tài khoản Active' AS Muc, COUNT_BIG(*) AS SoDongLech
    FROM Employees e JOIN AspNetUsers a ON a.PersonId = e.PersonId
    WHERE e.EmploymentStatus IN ('Suspended','Resigned') AND a.Status = 'Active'
    UNION ALL
    SELECT 35 AS STT, N'Không còn Administrator Active' AS Muc,
       CASE WHEN EXISTS (SELECT 1 FROM Employees) AND NOT EXISTS (
           SELECT 1 FROM Employees e JOIN AspNetUsers a ON a.PersonId = e.PersonId
           WHERE e.Position = 'Administrator' AND e.EmploymentStatus = 'Active' AND a.Status = 'Active'
       ) THEN CAST(1 AS bigint) ELSE CAST(0 AS bigint) END AS SoDongLech

    ) AS KiemTra WHERE SoDongLech > 0
)
    THROW 50020, N'Đối chiếu dữ liệu có mục LỆCH; không tự sửa dữ liệu.', 1;
