-- Thử các ràng buộc quan trọng (trùng phòng, tiền, unique có điều kiện, FK) trên SQL Server thật.
-- Chỉ chạy trên HomestayBooking_ConstraintTest vừa dựng bằng deploy.sql và CHƯA seed (không có dòng dữ liệu nào):
--   cd database && sqlcmd -S <server> -U <user> -C -d HomestayBooking_ConstraintTest -i tests/constraints.sql
-- Mọi dữ liệu thử nằm trong một transaction và luôn ROLLBACK. Có ca trượt hoặc lỗi → thoát với mã khác 0.
-- Lưu ý: ROLLBACK không trả lại bộ đếm IDENTITY (Id sau này sẽ không bắt đầu từ 1).
:on error exit
SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Kiểm tra đích và toàn bộ phần ghi nằm chung MỘT batch: THROW dừng cả batch trước BEGIN TRANSACTION,
-- kể cả khi công cụ (SSMS) vẫn chạy tiếp các batch sau.
IF DB_NAME() <> N'HomestayBooking_ConstraintTest'
    THROW 50000, N'Chỉ chạy trên HomestayBooking_ConstraintTest - dừng, không ghi gì.', 1;
IF OBJECT_ID(N'dbo.RoomNights') IS NULL OR OBJECT_ID(N'dbo.Vouchers') IS NULL OR OBJECT_ID(N'dbo.Settlements') IS NULL
    THROW 50001, N'Chưa có schema - chạy deploy.sql trước.', 1;
IF (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
    WHERE p.index_id IN (0, 1) AND t.name <> N'__EFMigrationsHistory') > 0
    THROW 50002, N'DB đã có dữ liệu - không chạy để tránh đụng dữ liệu hiện có.', 1;
PRINT N'Đích ' + DB_NAME() + N', không có dữ liệu. Bắt đầu transaction thử.';

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @pOwner int, @pCust int, @pUser int, @pEmp int, @owner int, @area int, @hs int, @cat int, @rt int,
        @room int, @room2 int, @cust int, @emp int, @pol int, @b1 int, @b2 int, @blk int, @v int,
        @acct nvarchar(450) = N'test-constraint-acct';
DECLARE @n int = 1, @max int, @name nvarchar(120), @sql nvarchar(max), @expect nvarchar(128),
        @pass int = 0, @fail int = 0, @err nvarchar(4000);

BEGIN TRY
    -- Dữ liệu nền: lỗi nào cũng hủy cả transaction
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84900000001', @now, @now); SET @pOwner = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84900000002', @now, @now); SET @pCust = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84900000003', @now, @now); SET @pUser = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84900000004', @now, @now); SET @pEmp = SCOPE_IDENTITY();
    INSERT Owners (PersonId, CreatedAt, UpdatedAt) VALUES (@pOwner, @now, @now); SET @owner = SCOPE_IDENTITY();
    INSERT Customers (PersonId, CreatedAt, UpdatedAt) VALUES (@pCust, @now, @now); SET @cust = SCOPE_IDENTITY();
    INSERT Employees (PersonId, EmployeeCode, Position, EmploymentStatus, CreatedAt, UpdatedAt) VALUES (@pEmp, N'TEST01', N'Manager', N'Active', @now, @now); SET @emp = SCOPE_IDENTITY();
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@acct, @pUser, N'Active', @now, @now, 0, 0, 0, 0, 0);
    INSERT Areas (Name, Province, AreaGroup) VALUES (N'Test', N'Test', N'InnerCity'); SET @area = SCOPE_IDENTITY();
    INSERT Homestays (OwnerId, AreaId, Name, Address, CheckinTime, CheckoutTime, Status, CreatedAt, UpdatedAt)
        VALUES (@owner, @area, N'Test HS', N'Test', '14:00', '12:00', N'Active', @now, @now); SET @hs = SCOPE_IDENTITY();
    INSERT RoomCategories (Name) VALUES (N'Test'); SET @cat = SCOPE_IDENTITY();
    INSERT RoomTypes (HomestayId, CategoryId, Name, MaxGuests, IsActive, CreatedAt, UpdatedAt) VALUES (@hs, @cat, N'Test RT', 2, 1, @now, @now); SET @rt = SCOPE_IDENTITY();
    INSERT Rooms (RoomTypeId, RoomCode, IsActive) VALUES (@rt, N'R1', 1); SET @room = SCOPE_IDENTITY();
    INSERT Rooms (RoomTypeId, RoomCode, IsActive) VALUES (@rt, N'R2', 1); SET @room2 = SCOPE_IDENTITY();
    INSERT CancellationPolicies (HomestayId, Scope, Name, IsActive, CreatedAt) VALUES (NULL, N'All', N'Test', 1, @now); SET @pol = SCOPE_IDENTITY();
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, CreatedAt, UpdatedAt)
        VALUES (N'TEST-0001', @cust, N'Test', N'+84900000002', N'Web', @hs, @rt, @room, '2027-01-01', '2027-01-03', 2, N'Deposited',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now); SET @b1 = SCOPE_IDENTITY();
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, CreatedAt, UpdatedAt)
        VALUES (N'TEST-0002', @cust, N'Test', N'+84900000002', N'Web', @hs, @rt, @room, '2027-03-01', '2027-03-03', 2, N'Deposited',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now); SET @b2 = SCOPE_IDENTITY();
    INSERT RoomNights (RoomId, Date, BookingId) VALUES (@room, '2027-01-01', @b1);
    INSERT RoomBlocks (RoomId, StartDate, EndDate, Reason, CreatedByAccountId, CreatedAt) VALUES (@room2, '2027-02-01', '2027-02-03', N'Maintenance', @acct, @now); SET @blk = SCOPE_IDENTITY();
    INSERT RoomNights (RoomId, Date, RoomBlockId) VALUES (@room2, '2027-02-01', @blk);
    INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N'Deposit', N'BankAutoMatch', 600000, N'Submitted', @now);
    INSERT Vouchers (Code, CustomerId, Amount, Balance, ExpiresOn, Status, Reason, IssuedByEmployeeId, IssuedAt)
        VALUES (N'TESTV1', @cust, 100000, 100000, '2027-12-31', N'Active', N'Test', @emp, @now); SET @v = SCOPE_IDENTITY();
    INSERT Contracts (HomestayId, ContractNo, StartDate, EndDate, CommissionRate, Status, CreatedAt, UpdatedAt)
        VALUES (@hs, N'TEST-C1', '2026-01-01', '2027-12-31', 0.1, N'Active', @now, @now);
    INSERT HomestayImages (HomestayId, Url, SortOrder, IsCover) VALUES (@hs, N'/t.jpg', 0, 1);
    INSERT BankTransactions (Provider, ProviderTransactionId, AccountNumber, Amount, TransactionTime, ReceivedAt, MatchStatus)
        VALUES (N'SePay', N'TX1', N'000', 600000, @now, @now, N'Unmatched');

    -- Các ca: lỗi ràng buộc chỉ kết thúc câu lệnh, transaction vẫn dùng tiếp được
    SET XACT_ABORT OFF;
    PRINT N'Dữ liệu thử đã tạo trong transaction.';

    -- Expect = tên ràng buộc phải xuất hiện trong thông báo lỗi; rỗng = câu lệnh hợp lệ, phải chạy được
    DECLARE @cases TABLE (n int IDENTITY, Name nvarchar(120), Sql nvarchar(max), Expect nvarchar(128));
    INSERT @cases (Name, Sql, Expect) VALUES
     (N'Trùng phòng cùng đêm', N'INSERT RoomNights (RoomId, Date, BookingId) VALUES (@room, ''2027-01-01'', @b2)', N'PK_RoomNights'),
     (N'Cùng phòng đêm khác (hợp lệ)', N'INSERT RoomNights (RoomId, Date, BookingId) VALUES (@room, ''2027-01-02'', @b1)', N''),
     (N'Đêm không thuộc đơn hay khóa', N'INSERT RoomNights (RoomId, Date) VALUES (@room, ''2027-05-01'')', N'CK_RoomNights_Owner'),
     (N'Đêm thuộc cả đơn và khóa', N'INSERT RoomNights (RoomId, Date, BookingId, RoomBlockId) VALUES (@room, ''2027-05-02'', @b1, @blk)', N'CK_RoomNights_Owner'),
     (N'Khóa ngày thêm đêm (hợp lệ)', N'INSERT RoomNights (RoomId, Date, RoomBlockId) VALUES (@room2, ''2027-02-02'', @blk)', N''),
     (N'Ngày khóa kết thúc trước bắt đầu', N'INSERT RoomBlocks (RoomId, StartDate, EndDate, Reason, CreatedByAccountId, CreatedAt) VALUES (@room, ''2027-06-02'', ''2027-06-01'', N''Maintenance'', @acct, SYSUTCDATETIME())', N'CK_RoomBlocks_Dates'),
     (N'Khoản cọc đang mở thứ hai', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N''Deposit'', N''BankAutoMatch'', 600000, N''Submitted'', SYSUTCDATETIME())', N'IX_Payments_OneDepositPerBooking'),
     (N'Thêm bill cọc bị từ chối (hợp lệ)', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N''Deposit'', N''BankAutoMatch'', 600000, N''Rejected'', SYSUTCDATETIME())', N''),
     (N'Hoàn cọc lần 1 (hợp lệ)', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N''Refund'', N''ManualTransfer'', 100000, N''Approved'', SYSUTCDATETIME())', N''),
     (N'Hoàn cọc lần 2', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b1, N''Refund'', N''ManualTransfer'', 100000, N''Approved'', SYSUTCDATETIME())', N'IX_Payments_OneRefundPerBooking'),
     (N'Thanh toán số tiền 0', N'INSERT Payments (BookingId, Type, Method, Amount, Status, SubmittedAt) VALUES (@b2, N''Deposit'', N''BankAutoMatch'', 0, N''Submitted'', SYSUTCDATETIME())', N'CK_Payments_Amount'),
     (N'Trùng mã đơn', N'UPDATE Bookings SET Code = N''TEST-0001'' WHERE Id = @b2', N'IX_Bookings_Code'),
     (N'Trả phòng không sau nhận phòng', N'UPDATE Bookings SET CheckOut = CheckIn WHERE Id = @b2', N'CK_Bookings_Dates'),
     (N'Số khách 0', N'UPDATE Bookings SET Guests = 0 WHERE Id = @b2', N'CK_Bookings_Guests'),
     (N'Cọc lớn hơn tổng tiền', N'UPDATE Bookings SET DepositAmount = TotalAmount + 1 WHERE Id = @b2', N'CK_Bookings_Amounts'),
     (N'Hoàn lớn hơn cọc', N'UPDATE Bookings SET RefundAmount = DepositAmount + 1 WHERE Id = @b2', N'CK_Bookings_Refund'),
     (N'Hoàn đúng bằng cọc (hợp lệ)', N'UPDATE Bookings SET RefundAmount = DepositAmount WHERE Id = @b2', N''),
     (N'Giảm giá không có voucher', N'UPDATE Bookings SET DiscountAmount = 1000 WHERE Id = @b1', N'CK_Bookings_Voucher'),
     (N'Giảm giá bằng cả tiền cọc', N'UPDATE Bookings SET VoucherId = @v, DiscountAmount = DepositAmount WHERE Id = @b1', N'CK_Bookings_Discount'),
     (N'Voucher dùng hợp lệ', N'UPDATE Bookings SET VoucherId = @v, DiscountAmount = 50000 WHERE Id = @b1', N''),
     (N'Voucher số dư lớn hơn mệnh giá', N'UPDATE Vouchers SET Balance = Amount + 1 WHERE Id = @v', N'CK_Vouchers_Balance'),
     (N'Đơn trỏ tới phòng không tồn tại', N'UPDATE Bookings SET RoomId = 999999 WHERE Id = @b2', N'FK_Bookings_Rooms_RoomId'),
     (N'Hợp đồng hiệu lực thứ hai', N'INSERT Contracts (HomestayId, ContractNo, StartDate, EndDate, CommissionRate, Status, CreatedAt, UpdatedAt) VALUES (@hs, N''TEST-C2'', ''2026-01-01'', ''2027-12-31'', 0.1, N''Active'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'IX_Contracts_HomestayId_Active'),
     (N'Hợp đồng hết hạn thêm (hợp lệ)', N'INSERT Contracts (HomestayId, ContractNo, StartDate, EndDate, CommissionRate, Status, CreatedAt, UpdatedAt) VALUES (@hs, N''TEST-C3'', ''2025-01-01'', ''2025-12-31'', 0.1, N''Expired'', SYSUTCDATETIME(), SYSUTCDATETIME())', N''),
     (N'Hoa hồng 100%', N'INSERT Contracts (HomestayId, ContractNo, StartDate, EndDate, CommissionRate, Status, CreatedAt, UpdatedAt) VALUES (@hs, N''TEST-C4'', ''2024-01-01'', ''2024-12-31'', 1, N''Expired'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Contracts_CommissionRate'),
     (N'Ảnh bìa thứ hai', N'INSERT HomestayImages (HomestayId, Url, SortOrder, IsCover) VALUES (@hs, N''/t2.jpg'', 1, 1)', N'IX_HomestayImages_Cover'),
     (N'Đánh giá 6 sao', N'INSERT Reviews (BookingId, HomestayId, Rating, IsHidden, CreatedAt) VALUES (@b1, @hs, 6, 0, SYSUTCDATETIME())', N'CK_Reviews_Rating'),
     (N'Trùng giao dịch ngân hàng', N'INSERT BankTransactions (Provider, ProviderTransactionId, AccountNumber, Amount, TransactionTime, ReceivedAt, MatchStatus) VALUES (N''SePay'', N''TX1'', N''000'', 1000, SYSUTCDATETIME(), SYSUTCDATETIME(), N''Unmatched'')', N'IX_BankTransactions_Provider_ProviderTransactionId'),
     (N'Giao dịch Refunded thiếu RefundedAt', N'UPDATE BankTransactions SET MatchStatus = N''Refunded'' WHERE ProviderTransactionId = N''TX1''', N'CK_BankTransactions_Refunded'),
     -- Có SentAt để chỉ vi phạm CK_Settlements_Paid (thiếu SentAt thì vi phạm cả CK_Settlements_Sent, SQL Server chỉ nêu một tên)
     (N'Đối soát Paid thiếu PaidAt', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, SentAt, CreatedAt, UpdatedAt) VALUES (@hs, 2027, 1, 1, 0, 1000, 100, 900, N''Paid'', SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Settlements_Paid'),
     (N'Đối soát cộng sai', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, CreatedAt, UpdatedAt) VALUES (@hs, 2027, 1, 1, 0, 1000, 100, 950, N''Draft'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Settlements_Amounts'),
     (N'Đối soát kỳ 3', N'INSERT Settlements (HomestayId, Year, Month, PeriodNo, TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount, Status, CreatedAt, UpdatedAt) VALUES (@hs, 2027, 1, 3, 0, 1000, 100, 900, N''Draft'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_Settlements_Period'),
     (N'Trùng số điện thoại', N'INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N''+84900000001'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'IX_PersonProfiles_PhoneNumber'),
     (N'Xóa homestay đang có đơn (NO ACTION)', N'DELETE Homestays WHERE Id = @hs', N'_Homestays_HomestayId'),
     (N'Xóa khóa ngày → đêm khóa tự xóa (CASCADE)', N'DELETE RoomBlocks WHERE Id = @blk; IF EXISTS (SELECT 1 FROM RoomNights WHERE RoomBlockId = @blk) THROW 50100, N''Cascade không xóa RoomNights'', 1;', N'');

    SET @max = (SELECT MAX(n) FROM @cases);
    WHILE @n <= @max
    BEGIN
        SELECT @name = Name, @sql = Sql, @expect = Expect FROM @cases WHERE n = @n;
        SET @err = NULL;
        BEGIN TRY
            EXEC sp_executesql @sql, N'@room int, @room2 int, @b1 int, @b2 int, @blk int, @hs int, @v int, @acct nvarchar(450)',
                 @room = @room, @room2 = @room2, @b1 = @b1, @b2 = @b2, @blk = @blk, @hs = @hs, @v = @v, @acct = @acct;
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

IF @fail > 0
    THROW 50300, N'Có ca trượt.', 1;
GO

-- Dự phòng khi công cụ không dừng sau lỗi: không bao giờ để lại transaction mở
IF @@TRANCOUNT > 0 ROLLBACK;
SELECT @@TRANCOUNT AS open_tran,
       (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
        WHERE p.index_id IN (0, 1) AND t.name <> N'__EFMigrationsHistory') AS rows_left;
GO
