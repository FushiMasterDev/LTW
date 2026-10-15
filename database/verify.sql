-- Kiểm tra schema sau khi chạy deploy.sql. Chỉ đọc (SELECT), không thay đổi dữ liệu.
--   sqlcmd -S <server> -U <user> -C -d <TenDb> -i verify.sql
-- Số kỳ vọng ứng với migration cuối 20261015092139_ChangeRequestWorkflow; thêm migration mới thì cập nhật lại.
SET NOCOUNT ON;

WITH Expected (Item, Expected) AS (
    SELECT N'Bảng (gồm __EFMigrationsHistory)', 45 UNION ALL
    SELECT N'Cột', 382 UNION ALL
    SELECT N'Khóa chính', 45 UNION ALL
    SELECT N'Khóa ngoại', 76 UNION ALL
    SELECT N'Khóa ngoại ON DELETE CASCADE', 13 UNION ALL
    SELECT N'CHECK', 44 UNION ALL
    SELECT N'Default', 4 UNION ALL
    SELECT N'Index (không tính khóa chính)', 93 UNION ALL
    SELECT N'Index có điều kiện (filtered)', 12 UNION ALL
    SELECT N'Migration đã áp', 8
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
             (N'CK_Payments_Receipt'), (N'CK_Settlements_Sent'), (N'CK_Bookings_ConfirmedAt'), (N'CK_BookingChangeRequests_Decision'),
             (N'CK_Bookings_ChangeFeeAmount'), (N'CK_Payments_ChangeRequest'), (N'CK_BookingChangeRequests_Amounts'),
             (N'CK_BookingChangeRequests_Finance'), (N'CK_BookingChangeRequests_ActorSeparation'),
             (N'CK_BookingChangeRequests_Notes'), (N'CK_BookingChangeRequests_PaymentDeadline'),
             (N'IX_BookingChangeRequests_OneActivePerBooking'), (N'IX_Payments_OneAdditionalDepositPerChangeRequest')) v(ObjName);
GO

-- 14 cột workflow: loại/độ dài và nullability phải đúng, không chỉ đủ tổng số cột.
WITH Expected(TableName, ColumnName, TypeName, MaxLength, Nullable) AS (
    SELECT * FROM (VALUES
    ('BookingChangeRequests','FinanceConfirmedByAccountId','nvarchar',900,1),
    ('BookingChangeRequests','FinanceConfirmedAt','datetime2',8,1),
    ('BookingChangeRequests','FinanceNote','nvarchar',1000,1),
    ('BookingChangeRequests','RequestedByAccountId','nvarchar',900,1),
    ('BookingChangeRequests','NewRoomId','int',4,1),
    ('BookingChangeRequests','RejectionReason','nvarchar',1000,1),
    ('BookingChangeRequests','ChangeFee','decimal',9,1),
    ('BookingChangeRequests','DepositCarriedOver','decimal',9,1),
    ('BookingChangeRequests','AdditionalDeposit','decimal',9,1),
    ('BookingChangeRequests','NewTotalAmount','decimal',9,1),
    ('BookingChangeRequests','PaymentDeadlineAt','datetime2',8,1),
    ('Bookings','ChangeFeeAmount','decimal',9,0),
    ('Payments','ChangeRequestId','int',4,1),
    ('RoomNights','BookingChangeRequestId','int',4,1)) v(TableName,ColumnName,TypeName,MaxLength,Nullable)
)
SELECT e.TableName,e.ColumnName,
    CASE WHEN TYPE_NAME(c.user_type_id)=e.TypeName AND c.max_length=e.MaxLength AND c.is_nullable=e.Nullable
        AND (e.TypeName<>'decimal' OR (c.precision=18 AND c.scale=0)) THEN 'OK' ELSE 'SAI' END AS Result
FROM Expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName;
GO

-- Năm FK mới đều NO ACTION, đang bật và đã kiểm tra dữ liệu cũ.
SELECT f.name, CASE WHEN f.delete_referential_action=0 AND f.update_referential_action=0
    AND f.is_disabled=0 AND f.is_not_trusted=0 THEN 'OK' ELSE 'SAI' END AS Result
FROM sys.foreign_keys f WHERE f.name IN (
    'FK_BookingChangeRequests_AspNetUsers_FinanceConfirmedByAccountId',
    'FK_BookingChangeRequests_AspNetUsers_RequestedByAccountId',
    'FK_BookingChangeRequests_Rooms_NewRoomId',
    'FK_Payments_BookingChangeRequests_ChangeRequestId',
    'FK_RoomNights_BookingChangeRequests_BookingChangeRequestId');
SELECT c.name, CASE WHEN c.max_length=64 THEN 'OK' ELSE 'SAI' END AS Result
FROM sys.columns c WHERE c.object_id=OBJECT_ID('dbo.BookingChangeRequests') AND c.name='RequestType';
SELECT c.name, CASE WHEN c.max_length>=34 THEN 'OK' ELSE 'SAI' END AS Result
FROM sys.columns c WHERE c.object_id=OBJECT_ID('dbo.Payments') AND c.name='Type';
SELECT name, CASE WHEN is_disabled=0 AND is_not_trusted=0 THEN 'OK' ELSE 'SAI' END AS Result
FROM sys.check_constraints ORDER BY name;
GO
