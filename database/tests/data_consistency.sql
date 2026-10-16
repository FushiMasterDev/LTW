-- Đối chiếu dữ liệu nghiệp vụ, chỉ đọc; chạy bằng sqlcmd -C -b -i tests/data_consistency.sql.
-- Không sửa dữ liệu, không mở transaction, không giới hạn tên database.
-- Tên trạng thái theo Enums.cs và BookingStatusExtensions.HoldsRoom(); sửa enum thì sửa script.
-- Completed vẫn giữ lịch phòng theo HoldsRoom(); Cancelled/Rejected/Expired phải nhả phòng.
-- Không kiểm giấy tờ mới trên toàn bộ homestay cũ (D17, mục H của B1).
-- Ba loại giá Weekday/Weekend/Holiday theo DayType; sửa enum thì sửa script.
-- Ngày hợp đồng tính theo giờ Việt Nam; mỗi mục đếm dòng vi phạm trong bảng được nêu.
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
)
SELECT CASE WHEN GROUPING(STT) = 1 THEN 99 ELSE STT END AS STT,
       CASE WHEN GROUPING(STT) = 1 THEN N'TỔNG (19 mục)' ELSE Muc END AS Muc,
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
    ) AS KiemTra WHERE SoDongLech > 0
)
    THROW 50020, N'Đối chiếu dữ liệu có mục LỆCH; không tự sửa dữ liệu.', 1;
