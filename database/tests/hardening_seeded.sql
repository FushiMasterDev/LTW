-- Thử các ràng buộc của migration DatabaseHardening trên HomestayBooking_DeployTest ĐÃ CÓ dữ liệu seed.
-- Không đọc hay sửa dữ liệu seed: tự tạo dữ liệu nền riêng (SĐT +84999…, mã HT-…, khu vực HardeningTest).
--   cd database && sqlcmd -S <server> -U <user> -C -d HomestayBooking_DeployTest -i tests/hardening_seeded.sql
-- Mọi dữ liệu thử nằm trong một transaction và luôn ROLLBACK. Có ca trượt hoặc lỗi → thoát với mã khác 0.
-- Lưu ý: ROLLBACK không trả lại bộ đếm IDENTITY.
:on error exit
SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Kiểm tra đích và toàn bộ phần ghi nằm chung MỘT batch: THROW dừng cả batch trước BEGIN TRANSACTION,
-- kể cả khi công cụ (SSMS) vẫn chạy tiếp các batch sau.
IF DB_NAME() <> N'HomestayBooking_DeployTest'
    THROW 50000, N'Chỉ chạy trên HomestayBooking_DeployTest - dừng, không ghi gì.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%[_]DatabaseHardening')
    THROW 50001, N'Chưa áp migration DatabaseHardening - chạy deploy.sql trước.', 1;
IF EXISTS (SELECT 1 FROM dbo.PersonProfiles WHERE PhoneNumber LIKE N'+84999%')
   OR EXISTS (SELECT 1 FROM dbo.Bookings WHERE Code LIKE N'HT-%')
    THROW 50002, N'Đã có dữ liệu mang khóa thử (+84999…, HT-…) - dừng để không đụng dữ liệu đó.', 1;

DECLARE @rowsBefore bigint = (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
                              WHERE p.index_id IN (0, 1) AND t.is_ms_shipped = 0);
PRINT CONCAT(N'Đích ', DB_NAME(), N', ', @rowsBefore, N' dòng hiện có. Bắt đầu transaction thử.');

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @pOwner int, @pCust int, @owner int, @cust int, @area int, @hs int, @cat int, @rt int, @room int, @pol int,
        @b1 int, @b2 int, @set int, @pUser int, @acct nvarchar(450) = N'ht-test-decider';
DECLARE @n int = 1, @max int, @name nvarchar(120), @sql nvarchar(max), @expect nvarchar(128),
        @pass int = 0, @fail int = 0, @err nvarchar(4000), @rowsAfter bigint;

BEGIN TRY
    -- Dữ liệu nền riêng: lỗi nào cũng hủy cả transaction
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84999000001', @now, @now); SET @pOwner = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84999000002', @now, @now); SET @pCust = SCOPE_IDENTITY();
    INSERT Owners (PersonId, CreatedAt, UpdatedAt) VALUES (@pOwner, @now, @now); SET @owner = SCOPE_IDENTITY();
    INSERT Customers (PersonId, CreatedAt, UpdatedAt) VALUES (@pCust, @now, @now); SET @cust = SCOPE_IDENTITY();
    INSERT Areas (Name, Province, AreaGroup) VALUES (N'HardeningTest', N'HardeningTest', N'InnerCity'); SET @area = SCOPE_IDENTITY();
    INSERT Homestays (OwnerId, AreaId, Name, Address, CheckinTime, CheckoutTime, Status, CreatedAt, UpdatedAt)
        VALUES (@owner, @area, N'HT Homestay', N'HT', '14:00', '12:00', N'Active', @now, @now); SET @hs = SCOPE_IDENTITY();
    INSERT RoomCategories (Name) VALUES (N'HardeningTest'); SET @cat = SCOPE_IDENTITY();
    INSERT RoomTypes (HomestayId, CategoryId, Name, MaxGuests, IsActive, CreatedAt, UpdatedAt) VALUES (@hs, @cat, N'HT RT', 2, 1, @now, @now); SET @rt = SCOPE_IDENTITY();
    INSERT Rooms (RoomTypeId, RoomCode, IsActive) VALUES (@rt, N'HT1', 1); SET @room = SCOPE_IDENTITY();
    -- Chính sách riêng của homestay thử (không đụng chính sách chung đang dùng)
    INSERT CancellationPolicies (HomestayId, Scope, Name, IsActive, CreatedAt) VALUES (@hs, N'All', N'HT', 1, @now); SET @pol = SCOPE_IDENTITY();
    -- Đơn Completed phải có ConfirmedAt (CK_Bookings_ConfirmedAt, migration StatusConsistency)
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, ConfirmedAt, CreatedAt, UpdatedAt)
        VALUES (N'HT-0001', @cust, N'HT', N'+84999000002', N'Web', @hs, @rt, @room, '2030-01-01', '2030-01-03', 2, N'Completed',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now, @now); SET @b1 = SCOPE_IDENTITY();
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, CreatedAt, UpdatedAt)
        VALUES (N'HT-0002', @cust, N'HT', N'+84999000002', N'Web', @hs, @rt, @room, '2030-02-01', '2030-02-03', 2, N'AwaitingPayment',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now); SET @b2 = SCOPE_IDENTITY();

    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84999000003', @now, @now); SET @pUser = SCOPE_IDENTITY();
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@acct, @pUser, N'Active', @now, @now, 0, 0, 0, 0, 0);

    -- Lấy đúng CHECK đang triển khai; tách enum sai khỏi Fields/Deadline cũng đang chặn nó.
    DECLARE @typeCheck nvarchar(max) = (SELECT definition FROM sys.check_constraints WHERE name = N'CK_BookingChangeRequests_Type');
    DECLARE @statusCheck nvarchar(max) = (SELECT definition FROM sys.check_constraints WHERE name = N'CK_BookingChangeRequests_Status');
    CREATE TABLE #WorkflowTypeProbe (RequestType nvarchar(32) NOT NULL);
    CREATE TABLE #WorkflowStatusProbe (Status nvarchar(20) NOT NULL);
    EXEC (N'ALTER TABLE #WorkflowTypeProbe ADD CONSTRAINT CK_BookingChangeRequests_Type CHECK (' + @typeCheck + N');'
        + N'ALTER TABLE #WorkflowStatusProbe ADD CONSTRAINT CK_BookingChangeRequests_Status CHECK (' + @statusCheck + N');');
    INSERT #WorkflowStatusProbe VALUES ('Pending');
    -- Các ca: lỗi ràng buộc chỉ kết thúc câu lệnh, transaction vẫn dùng tiếp được
    SET XACT_ABORT OFF;
    PRINT N'Dữ liệu thử đã tạo trong transaction.';

    -- Expect = tên ràng buộc phải xuất hiện trong thông báo lỗi; rỗng = câu lệnh hợp lệ, phải chạy được
    DECLARE @cases TABLE (n int IDENTITY, Name nvarchar(120), Sql nvarchar(max), Expect nvarchar(128));
    INSERT @cases (Name, Sql, Expect) VALUES
     (N'Tỷ lệ cọc 0', N'UPDATE Bookings SET DepositRate = 0 WHERE Id = @b1', N'CK_Bookings_DepositRate'),
     (N'Tỷ lệ cọc âm', N'UPDATE Bookings SET DepositRate = -0.1 WHERE Id = @b1', N'CK_Bookings_DepositRate'),
     (N'Tỷ lệ cọc 1.0001', N'UPDATE Bookings SET DepositRate = 1.0001 WHERE Id = @b1', N'CK_Bookings_DepositRate'),
     (N'Tỷ lệ cọc 1 (hợp lệ)', N'UPDATE Bookings SET DepositRate = 1 WHERE Id = @b1', N''),
     (N'Tỷ lệ cọc 0.3 (hợp lệ)', N'UPDATE Bookings SET DepositRate = 0.3 WHERE Id = @b1', N''),
     (N'Hoa hồng đơn 0', N'UPDATE Bookings SET CommissionRate = 0 WHERE Id = @b1', N'CK_Bookings_CommissionRate'),
     (N'Hoa hồng đơn 1', N'UPDATE Bookings SET CommissionRate = 1 WHERE Id = @b1', N'CK_Bookings_CommissionRate'),
     (N'Hoa hồng đơn 0.9999 (hợp lệ)', N'UPDATE Bookings SET CommissionRate = 0.9999 WHERE Id = @b1', N''),
     (N'Hoa hồng đơn 0.1 (hợp lệ)', N'UPDATE Bookings SET CommissionRate = 0.1 WHERE Id = @b1', N''),
     (N'Bill tay thiếu ảnh (NULL)', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b2, N''Deposit'', N''ManualReceipt'', 600000, N''Submitted'', SYSUTCDATETIME())', N'CK_Payments_Receipt'),
     (N'Bill tay ảnh rỗng', N'INSERT Payments (BookingId, Type, Method, Amount, ReceiptImageUrl, Status, SubmittedAt) VALUES (@b2, N''Deposit'', N''ManualReceipt'', 600000, N'''', N''Submitted'', SYSUTCDATETIME())', N'CK_Payments_Receipt'),
     (N'Bill tay ảnh chỉ có khoảng trắng', N'INSERT Payments (BookingId, Type, Method, Amount, ReceiptImageUrl, Status, SubmittedAt) VALUES (@b2, N''Deposit'', N''ManualReceipt'', 600000, N''   '', N''Submitted'', SYSUTCDATETIME())', N'CK_Payments_Receipt'),
     (N'Bill tay có ảnh (hợp lệ)', N'INSERT Payments (BookingId, Type, Method, Amount, ReceiptImageUrl, Status, SubmittedAt) VALUES (@b2, N''Deposit'', N''ManualReceipt'', 600000, N''ht-receipt.png'', N''Submitted'', SYSUTCDATETIME())', N''),
     (N'Gỡ ảnh khỏi bill tay đã có', N'UPDATE Payments SET ReceiptImageUrl = NULL WHERE BookingId = @b2 AND Method = N''ManualReceipt''', N'CK_Payments_Receipt'),
     (N'Hoàn cọc chuyển khoản không ảnh (hợp lệ)', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N''Refund'', N''ManualTransfer'', 100000, N''Approved'', SYSUTCDATETIME())', N''),
     (N'Đối soát Sent thiếu SentAt', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, CreatedAt, UpdatedAt) VALUES (@hs, 2030, 1, 1, 2000000, 600000, 60000, 540000, N''Sent'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Settlements_Sent'),
     (N'Đối soát Confirmed thiếu SentAt', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, ConfirmedAt, CreatedAt, UpdatedAt) VALUES (@hs, 2030, 1, 1, 2000000, 600000, 60000, 540000, N''Confirmed'', SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Settlements_Sent'),
     (N'Đối soát Draft chưa gửi (hợp lệ)', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, CreatedAt, UpdatedAt) VALUES (@hs, 2030, 1, 1, 2000000, 600000, 60000, 540000, N''Draft'', SYSUTCDATETIME(), SYSUTCDATETIME())', N''),
     (N'Chuyển Draft → Sent không ghi SentAt', N'UPDATE Settlements SET Status = N''Sent'' WHERE HomestayId = @hs', N'CK_Settlements_Sent'),
     (N'Chuyển Draft → Sent có SentAt (hợp lệ)', N'UPDATE Settlements SET Status = N''Sent'', SentAt = SYSUTCDATETIME() WHERE HomestayId = @hs', N''),
     (N'Mục đối soát hoa hồng 0', N'INSERT SettlementItems (SettlementId, BookingId, ItemType, RoomAmount, DepositAmount, RefundAmount, RetainedAmount, CommissionRate, CommissionAmount, PayoutAmount) SELECT Id, @b1, N''Completed'', 2000000, 600000, 0, 600000, 0, 60000, 540000 FROM Settlements WHERE HomestayId = @hs', N'CK_SettlementItems_CommissionRate'),
     (N'Mục đối soát hoa hồng 1', N'INSERT SettlementItems (SettlementId, BookingId, ItemType, RoomAmount, DepositAmount, RefundAmount, RetainedAmount, CommissionRate, CommissionAmount, PayoutAmount) SELECT Id, @b1, N''Completed'', 2000000, 600000, 0, 600000, 1, 60000, 540000 FROM Settlements WHERE HomestayId = @hs', N'CK_SettlementItems_CommissionRate'),
     (N'Mục đối soát hoa hồng 0.1 (hợp lệ)', N'INSERT SettlementItems (SettlementId, BookingId, ItemType, RoomAmount, DepositAmount, RefundAmount, RetainedAmount, CommissionRate, CommissionAmount, PayoutAmount) SELECT Id, @b1, N''Completed'', 2000000, 600000, 0, 600000, 0.1, 60000, 540000 FROM Settlements WHERE HomestayId = @hs', N''),
     (N'Yêu cầu loại không có trong enum', N'INSERT #WorkflowTypeProbe VALUES (''Upgrade'')', N'CK_BookingChangeRequests_Type'),
     (N'Yêu cầu trạng thái không có trong enum', N'UPDATE #WorkflowStatusProbe SET Status = ''Unknown''', N'CK_BookingChangeRequests_Status'),
     (N'Yêu cầu hủy chờ duyệt (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b2, N''Cancel'', N''Customer'', N''Pending'', SYSUTCDATETIME())', N''),
     (N'Đổi trạng thái yêu cầu sang giá trị lạ', N'UPDATE #WorkflowStatusProbe SET Status = ''Unknown''', N'CK_BookingChangeRequests_Status'),
     (N'Yêu cầu đổi loại phòng đã duyệt (hợp lệ)', N'INSERT BookingChangeRequests (DecisionNote, DecidedByAccountId, BookingId, RequestType, RequestedByType, NewRoomTypeId, Status, DecidedAt, CreatedAt) VALUES (N''Đồng ý đổi phòng'', @acct, @b2, N''ChangeRoomType'', N''Employee'', @rt, N''Applied'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'');

    SET @max = (SELECT MAX(n) FROM @cases);
    WHILE @n <= @max
    BEGIN
        SELECT @name = Name, @sql = Sql, @expect = Expect FROM @cases WHERE n = @n;
        SET @err = NULL;
        BEGIN TRY
            EXEC sp_executesql @sql, N'@b1 int, @b2 int, @hs int, @rt int, @acct nvarchar(450)', @b1 = @b1, @b2 = @b2, @hs = @hs, @rt = @rt, @acct = @acct;
        END TRY
        BEGIN CATCH
            SET @err = CONCAT(N'Msg ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
        END CATCH;
        IF XACT_STATE() <> 1
        BEGIN
            PRINT N'Transaction không còn dùng được sau ca: ' + @name + ISNULL(N' | ' + @err, N'');
            THROW 50200, N'Transaction hỏng - dừng.', 1;
        END;
        IF (@expect = N'' AND @err IS NULL) OR (@expect <> N'' AND @err LIKE N'%' + @expect + N'%')
            SELECT @pass += 1, @err = N'ĐẠT   ' + @name + ISNULL(N'  ←  ' + @err, N'  (chạy được)');
        ELSE
            SELECT @fail += 1, @err = N'TRƯỢT ' + @name + N'  | mong đợi: ' + IIF(@expect = N'', N'chạy được', @expect) + N' | thực tế: ' + ISNULL(@err, N'không bị chặn');
        PRINT @err;
        SET @n += 1;
    END;

    ROLLBACK;
    PRINT CONCAT(N'Kết quả: ', @pass, N' đạt, ', @fail, N' trượt / ', @max, N' ca. Đã ROLLBACK.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    PRINT N'Lỗi, đã ROLLBACK: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

SET @rowsAfter = (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
                  WHERE p.index_id IN (0, 1) AND t.is_ms_shipped = 0);
PRINT CONCAT(N'Số dòng sau ROLLBACK: ', @rowsAfter, N' (trước: ', @rowsBefore, N').');
IF @rowsAfter <> @rowsBefore
    THROW 50400, N'Số dòng sau ROLLBACK khác trước khi chạy.', 1;
IF @fail > 0
    THROW 50300, N'Có ca trượt.', 1;
GO

-- Dự phòng khi công cụ không dừng sau lỗi: không bao giờ để lại transaction mở
IF @@TRANCOUNT > 0 ROLLBACK;
SELECT @@TRANCOUNT AS open_tran,
       (SELECT COUNT(*) FROM dbo.PersonProfiles WHERE PhoneNumber LIKE N'+84999%')
     + (SELECT COUNT(*) FROM dbo.Bookings WHERE Code LIKE N'HT-%')
     + (SELECT COUNT(*) FROM dbo.Areas WHERE Name = N'HardeningTest') AS test_rows_left;
GO
