-- Thử 4 CHECK của migration StatusConsistency trên HomestayBooking_DeployTest ĐÃ CÓ dữ liệu seed:
-- CK_Bookings_ConfirmedAt, CK_BookingChangeRequests_RequestedBy, CK_BookingChangeRequests_Fields, CK_BookingChangeRequests_Decision.
-- Không đọc hay sửa dữ liệu seed: tự tạo dữ liệu nền riêng (SĐT +84998…, mã SC-…, khu vực StatusConsistencyTest).
--   cd database && sqlcmd -S <server> -U <user> -C -d HomestayBooking_DeployTest -i tests/status_consistency_seeded.sql
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
   OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%[_]StatusConsistency')
    THROW 50001, N'Chưa áp migration StatusConsistency - chạy deploy.sql trước.', 1;
IF EXISTS (SELECT 1 FROM dbo.PersonProfiles WHERE PhoneNumber LIKE N'+84998%')
   OR EXISTS (SELECT 1 FROM dbo.Bookings WHERE Code LIKE N'SC-%')
    THROW 50002, N'Đã có dữ liệu mang khóa thử (+84998…, SC-…) - dừng để không đụng dữ liệu đó.', 1;

DECLARE @rowsBefore bigint = (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
                              WHERE p.index_id IN (0, 1) AND t.is_ms_shipped = 0);
PRINT CONCAT(N'Đích ', DB_NAME(), N', ', @rowsBefore, N' dòng hiện có. Bắt đầu transaction thử.');

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @pOwner int, @pCust int, @pUser int, @owner int, @cust int, @area int, @hs int, @cat int, @rt int, @room int, @pol int,
        @b1 int, @b2 int, @acct nvarchar(450) = N'sc-test-decider';
DECLARE @n int = 1, @max int, @name nvarchar(120), @sql nvarchar(max), @expect nvarchar(128),
        @pass int = 0, @fail int = 0, @err nvarchar(4000), @rowsAfter bigint;

BEGIN TRY
    -- Dữ liệu nền riêng: lỗi nào cũng hủy cả transaction
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84998000001', @now, @now); SET @pOwner = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84998000002', @now, @now); SET @pCust = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84998000003', @now, @now); SET @pUser = SCOPE_IDENTITY();
    INSERT Owners (PersonId, CreatedAt, UpdatedAt) VALUES (@pOwner, @now, @now); SET @owner = SCOPE_IDENTITY();
    INSERT Customers (PersonId, CreatedAt, UpdatedAt) VALUES (@pCust, @now, @now); SET @cust = SCOPE_IDENTITY();
    -- Tài khoản làm người quyết định yêu cầu (FK DecidedByAccountId)
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@acct, @pUser, N'Active', @now, @now, 0, 0, 0, 0, 0);
    INSERT Areas (Name, Province, AreaGroup) VALUES (N'StatusConsistencyTest', N'StatusConsistencyTest', N'InnerCity'); SET @area = SCOPE_IDENTITY();
    INSERT Homestays (OwnerId, AreaId, Name, Address, CheckinTime, CheckoutTime, Status, CreatedAt, UpdatedAt)
        VALUES (@owner, @area, N'SC Homestay', N'SC', '14:00', '12:00', N'Active', @now, @now); SET @hs = SCOPE_IDENTITY();
    INSERT RoomCategories (Name) VALUES (N'StatusConsistencyTest'); SET @cat = SCOPE_IDENTITY();
    INSERT RoomTypes (HomestayId, CategoryId, Name, MaxGuests, IsActive, CreatedAt, UpdatedAt) VALUES (@hs, @cat, N'SC RT', 2, 1, @now, @now); SET @rt = SCOPE_IDENTITY();
    INSERT Rooms (RoomTypeId, RoomCode, IsActive) VALUES (@rt, N'SC1', 1); SET @room = SCOPE_IDENTITY();
    INSERT CancellationPolicies (HomestayId, Scope, Name, IsActive, CreatedAt) VALUES (@hs, N'All', N'SC', 1, @now); SET @pol = SCOPE_IDENTITY();
    -- @b1: đơn đã xác nhận, dùng làm đơn của các yêu cầu đổi/hủy
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, ConfirmedAt, CreatedAt, UpdatedAt)
        VALUES (N'SC-0001', @cust, N'SC', N'+84998000002', N'Web', @hs, @rt, @room, '2030-01-01', '2030-01-03', 2, N'Confirmed',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now, @now); SET @b1 = SCOPE_IDENTITY();
    -- @b2: đơn chờ thanh toán (chưa có ConfirmedAt), các ca đổi trạng thái chạy tuần tự trên đơn này
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, CreatedAt, UpdatedAt)
        VALUES (N'SC-0002', @cust, N'SC', N'+84998000002', N'Web', @hs, @rt, @room, '2030-02-01', '2030-02-03', 2, N'AwaitingPayment',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now); SET @b2 = SCOPE_IDENTITY();

    -- Các ca: lỗi ràng buộc chỉ kết thúc câu lệnh, transaction vẫn dùng tiếp được
    SET XACT_ABORT OFF;
    PRINT N'Dữ liệu thử đã tạo trong transaction.';

    -- Expect = tên ràng buộc phải xuất hiện trong thông báo lỗi; rỗng = câu lệnh hợp lệ, phải chạy được.
    -- Mỗi ca chặn chỉ vi phạm đúng một CHECK. Thứ tự có ý nghĩa: các ca hợp lệ của @b2 nối tiếp nhau.
    DECLARE @cases TABLE (n int IDENTITY, Name nvarchar(120), Sql nvarchar(max), Expect nvarchar(128));
    INSERT @cases (Name, Sql, Expect) VALUES
     -- CK_Bookings_ConfirmedAt: @b2 đang AwaitingPayment, chưa có ConfirmedAt
     (N'Chờ thanh toán mà có ConfirmedAt', N'UPDATE Bookings SET ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'PaymentSubmitted mà có ConfirmedAt', N'UPDATE Bookings SET Status = N''PaymentSubmitted'', ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Deposited mà có ConfirmedAt', N'UPDATE Bookings SET Status = N''Deposited'', ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Rejected mà có ConfirmedAt', N'UPDATE Bookings SET Status = N''Rejected'', ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Expired mà có ConfirmedAt', N'UPDATE Bookings SET Status = N''Expired'', ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Confirmed thiếu ConfirmedAt', N'UPDATE Bookings SET Status = N''Confirmed'' WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'CheckedIn thiếu ConfirmedAt', N'UPDATE Bookings SET Status = N''CheckedIn'' WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Completed thiếu ConfirmedAt', N'UPDATE Bookings SET Status = N''Completed'' WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'NoShow thiếu ConfirmedAt', N'UPDATE Bookings SET Status = N''NoShow'' WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     (N'Deposited không ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''Deposited'' WHERE Id = @b2', N''),
     (N'Hủy trước xác nhận, không ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''Cancelled'' WHERE Id = @b2', N''),
     (N'Confirmed có ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''Confirmed'', ConfirmedAt = SYSUTCDATETIME() WHERE Id = @b2', N''),
     (N'CheckedIn giữ ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''CheckedIn'' WHERE Id = @b2', N''),
     (N'Completed giữ ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''Completed'' WHERE Id = @b2', N''),
     (N'NoShow giữ ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''NoShow'' WHERE Id = @b2', N''),
     (N'Hủy sau xác nhận, giữ ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET Status = N''Cancelled'' WHERE Id = @b2', N''),
     (N'Đơn đã hủy bỏ ConfirmedAt (hợp lệ)', N'UPDATE Bookings SET ConfirmedAt = NULL WHERE Id = @b2', N''),
     (N'Xác nhận lại mà không ghi ConfirmedAt', N'UPDATE Bookings SET Status = N''Confirmed'' WHERE Id = @b2', N'CK_Bookings_ConfirmedAt'),
     -- CK_BookingChangeRequests_RequestedBy
     (N'Chủ homestay gửi yêu cầu', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Owner'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_RequestedBy'),
     (N'Hệ thống gửi yêu cầu', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''System'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_RequestedBy'),
     (N'Khách gửi yêu cầu (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Pending'', SYSUTCDATETIME())', N''),
     (N'Nhân viên gửi thay khách (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Employee'', N''Pending'', SYSUTCDATETIME())', N''),
     -- Ghi nhận hành vi: collation CI nên SQL Server nhận chữ thường (EF đọc vẫn ra Customer); SQLite trong test thì chặn
     (N'Chữ thường ''customer'' (collation CI, được nhận)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''customer'', N''Pending'', SYSUTCDATETIME())', N''),
     -- CK_BookingChangeRequests_Fields
     (N'Hủy kèm ngày mới', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', ''2030-03-01'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Hủy kèm loại phòng mới', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewRoomTypeId, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', @rt, N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi ngày thiếu ngày trả', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-01'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi ngày thiếu ngày nhận', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckOut, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-03'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi ngày trả = ngày nhận', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, NewCheckOut, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-03'', ''2030-03-03'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi ngày trả trước ngày nhận', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, NewCheckOut, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-03'', ''2030-03-01'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi loại phòng thiếu loại phòng', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''ChangeRoomType'', N''Customer'', N''Pending'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Fields'),
     (N'Đổi ngày đủ, trả sau nhận (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, NewCheckOut, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-01'', ''2030-03-03'', N''Pending'', SYSUTCDATETIME())', N''),
     (N'Đổi loại phòng có loại phòng (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewRoomTypeId, Status, CreatedAt) VALUES (@b1, N''ChangeRoomType'', N''Customer'', @rt, N''Pending'', SYSUTCDATETIME())', N''),
     (N'Đổi ngày kèm loại phòng (chưa chốt, đang cho phép)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, NewCheckOut, NewRoomTypeId, Status, CreatedAt) VALUES (@b1, N''ChangeDates'', N''Customer'', ''2030-03-01'', ''2030-03-03'', @rt, N''Pending'', SYSUTCDATETIME())', N''),
     (N'Đổi loại phòng kèm ngày (chưa chốt, đang cho phép)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, NewCheckIn, NewCheckOut, NewRoomTypeId, Status, CreatedAt) VALUES (@b1, N''ChangeRoomType'', N''Customer'', ''2030-03-01'', ''2030-03-03'', @rt, N''Pending'', SYSUTCDATETIME())', N''),
     -- CK_BookingChangeRequests_Decision
     (N'Chờ duyệt mà có DecidedAt', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, DecidedAt, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Pending'', SYSUTCDATETIME(), SYSUTCDATETIME())', N'CK_BookingChangeRequests_Decision'),
     (N'Chờ duyệt mà có người quyết', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, DecidedByAccountId, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Pending'', @acct, SYSUTCDATETIME())', N'CK_BookingChangeRequests_Decision'),
     (N'Đã duyệt thiếu DecidedAt', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, DecidedByAccountId, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Approved'', @acct, SYSUTCDATETIME())', N'CK_BookingChangeRequests_Decision'),
     (N'Từ chối thiếu DecidedAt', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Rejected'', SYSUTCDATETIME())', N'CK_BookingChangeRequests_Decision'),
     (N'Đã duyệt có DecidedAt, không người quyết (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, DecidedAt, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Approved'', SYSUTCDATETIME(), SYSUTCDATETIME())', N''),
     (N'Từ chối có DecidedAt và người quyết (hợp lệ)', N'INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, Status, DecidedAt, DecidedByAccountId, CreatedAt) VALUES (@b1, N''Cancel'', N''Customer'', N''Rejected'', SYSUTCDATETIME(), @acct, SYSUTCDATETIME())', N''),
     (N'Duyệt yêu cầu chờ mà không ghi DecidedAt', N'UPDATE BookingChangeRequests SET Status = N''Approved'' WHERE BookingId = @b1 AND Status = N''Pending''', N'CK_BookingChangeRequests_Decision'),
     (N'Duyệt yêu cầu chờ có DecidedAt và người quyết (hợp lệ)', N'UPDATE BookingChangeRequests SET Status = N''Approved'', DecidedAt = SYSUTCDATETIME(), DecidedByAccountId = @acct WHERE BookingId = @b1 AND Status = N''Pending''', N'');

    SET @max = (SELECT MAX(n) FROM @cases);
    WHILE @n <= @max
    BEGIN
        SELECT @name = Name, @sql = Sql, @expect = Expect FROM @cases WHERE n = @n;
        SET @err = NULL;
        BEGIN TRY
            EXEC sp_executesql @sql, N'@b1 int, @b2 int, @rt int, @acct nvarchar(450)', @b1 = @b1, @b2 = @b2, @rt = @rt, @acct = @acct;
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
       (SELECT COUNT(*) FROM dbo.PersonProfiles WHERE PhoneNumber LIKE N'+84998%')
     + (SELECT COUNT(*) FROM dbo.Bookings WHERE Code LIKE N'SC-%')
     + (SELECT COUNT(*) FROM dbo.Areas WHERE Name = N'StatusConsistencyTest')
     + (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Id = N'sc-test-decider') AS test_rows_left;
GO
