-- Kiểm tra schema sau khi chạy deploy.sql. Chỉ đọc (SELECT), không thay đổi dữ liệu.
--   sqlcmd -S <server> -U <user> -C -d <TenDb> -i verify.sql
-- Số kỳ vọng ứng với migration cuối 20261013032617_StatusConsistency; thêm migration mới thì cập nhật lại.
SET NOCOUNT ON;

WITH Expected (Item, Expected) AS (
    SELECT N'Bảng (gồm __EFMigrationsHistory)', 45 UNION ALL
    SELECT N'Cột', 368 UNION ALL
    SELECT N'Khóa chính', 45 UNION ALL
    SELECT N'Khóa ngoại', 71 UNION ALL
    SELECT N'Khóa ngoại ON DELETE CASCADE', 13 UNION ALL
    SELECT N'CHECK', 37 UNION ALL
    SELECT N'Default', 3 UNION ALL
    SELECT N'Index (không tính khóa chính)', 87 UNION ALL
    SELECT N'Index có điều kiện (filtered)', 10 UNION ALL
    SELECT N'Migration đã áp', 7
), Actual (Item, Actual) AS (
    SELECT N'Bảng (gồm __EFMigrationsHistory)', COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 UNION ALL
    SELECT N'Cột', COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.is_ms_shipped = 0 UNION ALL
    SELECT N'Khóa chính', COUNT(*) FROM sys.key_constraints WHERE type = 'PK' AND OBJECTPROPERTY(parent_object_id, 'IsMSShipped') = 0 UNION ALL
    SELECT N'Khóa ngoại', COUNT(*) FROM sys.foreign_keys UNION ALL
    SELECT N'Khóa ngoại ON DELETE CASCADE', COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action_desc = 'CASCADE' UNION ALL
    SELECT N'CHECK', COUNT(*) FROM sys.check_constraints UNION ALL
    SELECT N'Default', COUNT(*) FROM sys.default_constraints UNION ALL
    SELECT N'Index (không tính khóa chính)', COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
        WHERE t.is_ms_shipped = 0 AND i.is_primary_key = 0 AND i.type > 0 UNION ALL
    SELECT N'Index có điều kiện (filtered)', COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
        WHERE t.is_ms_shipped = 0 AND i.has_filter = 1 UNION ALL
    -- Đếm qua sys.partitions: tham chiếu thẳng bảng chưa tồn tại sẽ lỗi ngay cả trong nhánh CASE không chạy
    SELECT N'Migration đã áp', CASE WHEN OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL THEN 0
        ELSE (SELECT SUM(rows) FROM sys.partitions WHERE object_id = OBJECT_ID(N'[__EFMigrationsHistory]') AND index_id IN (0, 1)) END
)
SELECT e.Item, e.Expected, a.Actual, CASE WHEN e.Expected = a.Actual THEN 'OK' ELSE 'SAI' END AS Result
FROM Expected e JOIN Actual a ON a.Item = e.Item;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
    SELECT MigrationId, ProductVersion FROM [__EFMigrationsHistory] ORDER BY MigrationId;
GO

-- Các ràng buộc chống đặt trùng phòng và giữ tiền đúng (thiếu dòng nào là thiếu ràng buộc)
SELECT v.ObjName, CASE WHEN OBJECT_ID(v.ObjName) IS NOT NULL OR EXISTS (SELECT 1 FROM sys.indexes i WHERE i.name = v.ObjName) THEN N'OK' ELSE N'THIẾU' END AS Result
FROM (VALUES (N'PK_RoomNights'), (N'CK_RoomNights_Owner'), (N'IX_Payments_OneDepositPerBooking'), (N'IX_Payments_OneRefundPerBooking'),
             (N'IX_Contracts_HomestayId_Active'), (N'IX_HomestayImages_Cover'), (N'IX_Bookings_Code'), (N'CK_Bookings_Refund'),
             (N'CK_Payments_Receipt'), (N'CK_Settlements_Sent'), (N'CK_Bookings_ConfirmedAt'), (N'CK_BookingChangeRequests_Decision')) v(ObjName);
GO
