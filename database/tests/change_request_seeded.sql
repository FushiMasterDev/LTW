-- Thử workflow trên DeployTest: khóa thử riêng +84997…, WF-…, luôn ROLLBACK.
-- 12 CHECK, 2 unique filtered; mỗi ca có savepoint, không đọc/sửa dữ liệu seed.
-- Type/Status thử riêng đúng định nghĩa metadata vì enum sai cũng vi phạm Fields/Deadline.
-- ROLLBACK không hoàn lại bộ đếm IDENTITY. Chạy với sqlcmd -b -C.
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
   OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%[_]ChangeRequestWorkflow')
    THROW 50001, N'Chưa áp migration ChangeRequestWorkflow - chạy deploy.sql trước.', 1;
IF EXISTS (SELECT 1 FROM dbo.PersonProfiles WHERE PhoneNumber LIKE N'+84997%')
   OR EXISTS (SELECT 1 FROM dbo.Bookings WHERE Code LIKE N'WF-%')
    THROW 50002, N'Đã có dữ liệu mang khóa thử (+84997…, WF-…) - dừng để không đụng dữ liệu đó.', 1;

DECLARE @rowsBefore bigint = (SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id = p.object_id
                              WHERE p.index_id IN (0, 1) AND t.is_ms_shipped = 0);
PRINT CONCAT(N'Đích ', DB_NAME(), N', ', @rowsBefore, N' dòng hiện có. Bắt đầu transaction thử.');

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @pOwner int, @pCust int, @pUser int, @owner int, @cust int, @area int, @hs int, @cat int, @rt int, @room int, @pol int,
        @b1 int, @b2 int, @acct nvarchar(450) = N'wf-test-manager';
DECLARE @n int = 1, @max int, @name nvarchar(120), @sql nvarchar(max), @expect nvarchar(128),
        @pass int = 0, @fail int = 0, @err nvarchar(4000), @rowsAfter bigint;

BEGIN TRY
    -- Dữ liệu nền riêng: lỗi nào cũng hủy cả transaction
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84997000001', @now, @now); SET @pOwner = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84997000002', @now, @now); SET @pCust = SCOPE_IDENTITY();
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES (N'+84997000003', @now, @now); SET @pUser = SCOPE_IDENTITY();
    INSERT Owners (PersonId, CreatedAt, UpdatedAt) VALUES (@pOwner, @now, @now); SET @owner = SCOPE_IDENTITY();
    INSERT Customers (PersonId, CreatedAt, UpdatedAt) VALUES (@pCust, @now, @now); SET @cust = SCOPE_IDENTITY();
    -- Tài khoản làm người quyết định yêu cầu (FK DecidedByAccountId)
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@acct, @pUser, N'Active', @now, @now, 0, 0, 0, 0, 0);
    INSERT Areas (Name, Province, AreaGroup) VALUES (N'ChangeRequestWorkflowTest', N'ChangeRequestWorkflowTest', N'InnerCity'); SET @area = SCOPE_IDENTITY();
    INSERT Homestays (OwnerId, AreaId, Name, Address, CheckinTime, CheckoutTime, Status, CreatedAt, UpdatedAt)
        VALUES (@owner, @area, N'WF Homestay', N'WF', '14:00', '12:00', N'Active', @now, @now); SET @hs = SCOPE_IDENTITY();
    INSERT RoomCategories (Name) VALUES (N'ChangeRequestWorkflowTest'); SET @cat = SCOPE_IDENTITY();
    INSERT RoomTypes (HomestayId, CategoryId, Name, MaxGuests, IsActive, CreatedAt, UpdatedAt) VALUES (@hs, @cat, N'WF RT', 2, 1, @now, @now); SET @rt = SCOPE_IDENTITY();
    INSERT Rooms (RoomTypeId, RoomCode, IsActive) VALUES (@rt, N'WF1', 1); SET @room = SCOPE_IDENTITY();
    INSERT CancellationPolicies (HomestayId, Scope, Name, IsActive, CreatedAt) VALUES (@hs, N'All', N'WF', 1, @now); SET @pol = SCOPE_IDENTITY();
    -- @b1: đơn đã xác nhận, dùng làm đơn của các yêu cầu đổi/hủy
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, ConfirmedAt, CreatedAt, UpdatedAt)
        VALUES (N'WF-0001', @cust, N'WF', N'+84997000002', N'Web', @hs, @rt, @room, '2030-01-01', '2030-01-03', 2, N'Confirmed',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now, @now); SET @b1 = SCOPE_IDENTITY();
    -- @b2: đơn chờ thanh toán (chưa có ConfirmedAt), các ca đổi trạng thái chạy tuần tự trên đơn này
    INSERT Bookings (Code, CustomerId, ContactName, ContactPhone, SourceChannel, HomestayId, RoomTypeId, RoomId, CheckIn, CheckOut, Guests, Status,
                     HoldExpiresAt, TotalAmount, DepositRate, DepositAmount, CommissionRate, CancellationPolicyId, CreatedAt, UpdatedAt)
        VALUES (N'WF-0002', @cust, N'WF', N'+84997000002', N'Web', @hs, @rt, @room, '2030-02-01', '2030-02-03', 2, N'AwaitingPayment',
                @now, 2000000, 0.3, 600000, 0.1, @pol, @now, @now); SET @b2 = SCOPE_IDENTITY();


    -- Lấy đúng CHECK đang triển khai; tách enum sai khỏi Fields/Deadline cũng đang chặn nó.
    DECLARE @typeCheck nvarchar(max) = (SELECT definition FROM sys.check_constraints WHERE name = N'CK_BookingChangeRequests_Type');
    DECLARE @statusCheck nvarchar(max) = (SELECT definition FROM sys.check_constraints WHERE name = N'CK_BookingChangeRequests_Status');
    CREATE TABLE #WorkflowTypeProbe (RequestType nvarchar(32) NOT NULL);
    CREATE TABLE #WorkflowStatusProbe (Status nvarchar(20) NOT NULL);
    EXEC (N'ALTER TABLE #WorkflowTypeProbe ADD CONSTRAINT CK_BookingChangeRequests_Type CHECK (' + @typeCheck + N');'
        + N'ALTER TABLE #WorkflowStatusProbe ADD CONSTRAINT CK_BookingChangeRequests_Status CHECK (' + @statusCheck + N');');
    INSERT #WorkflowStatusProbe VALUES ('Pending');

    DECLARE @r int, @blk int, @person int, @sender nvarchar(450) = N'wf-test-sender', @finance nvarchar(450) = N'wf-test-finance';
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES ('+84997000004', @now, @now); SET @person = SCOPE_IDENTITY();
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@sender, @person, 'Active', @now, @now, 0, 0, 0, 0, 0);
    INSERT PersonProfiles (PhoneNumber, CreatedAt, UpdatedAt) VALUES ('+84997000005', @now, @now); SET @person = SCOPE_IDENTITY();
    INSERT AspNetUsers (Id, PersonId, Status, CreatedAt, UpdatedAt, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@finance, @person, 'Active', @now, @now, 0, 0, 0, 0, 0);
    INSERT BookingChangeRequests (BookingId, RequestType, RequestedByType, RequestedByAccountId, Status, ChangeFee, DepositCarriedOver, AdditionalDeposit, NewTotalAmount, CreatedAt)
        VALUES (@b1, 'Cancel', 'Customer', @sender, 'Pending', 0, 0, 0, 0, @now); SET @r = SCOPE_IDENTITY();
    INSERT RoomBlocks (RoomId, StartDate, EndDate, Reason, CreatedByAccountId, CreatedAt)
        VALUES (@room, '2030-04-01', '2030-04-02', 'Maintenance', @acct, @now); SET @blk = SCOPE_IDENTITY();
    SET XACT_ABORT OFF;
    DECLARE @cases TABLE (n int IDENTITY, Name nvarchar(120), Sql nvarchar(max), Expect nvarchar(128));
    INSERT @cases (Name, Sql, Expect) VALUES
     (N'Type sai', N'INSERT #WorkflowTypeProbe VALUES (''Unknown'')', N'CK_BookingChangeRequests_Type'),
     (N'Type kết hợp hợp lệ', N'INSERT #WorkflowTypeProbe VALUES (''ChangeDatesAndRoomType'')', N''),
     (N'Status sai', N'UPDATE #WorkflowStatusProbe SET Status = ''Unknown''', N'CK_BookingChangeRequests_Status'),
     (N'Status Applied hợp lệ', N'UPDATE #WorkflowStatusProbe SET Status = ''Applied''', N''),
     (N'Kết hợp đủ trường', N'UPDATE BookingChangeRequests SET RequestType=''ChangeDatesAndRoomType'', NewCheckIn=''2030-03-01'', NewCheckOut=''2030-03-02'', NewRoomTypeId=@rt WHERE Id = @r', N''),
     (N'Kết hợp thiếu loại phòng', N'UPDATE BookingChangeRequests SET RequestType=''ChangeDatesAndRoomType'', NewCheckIn=''2030-03-01'', NewCheckOut=''2030-03-02'' WHERE Id = @r', N'CK_BookingChangeRequests_Fields'),
     (N'Ngày bằng nhau', N'UPDATE BookingChangeRequests SET RequestType=''ChangeDates'', NewCheckIn=''2030-03-01'', NewCheckOut=''2030-03-01'' WHERE Id = @r', N'CK_BookingChangeRequests_Fields'),
     (N'Hủy có ngày', N'UPDATE BookingChangeRequests SET NewCheckIn=''2030-03-01'' WHERE Id = @r', N'CK_BookingChangeRequests_Fields'),
     (N'Pending không quyết định', N'UPDATE BookingChangeRequests SET Status=''Pending'' WHERE Id = @r', N''),
     (N'Pending có người quyết', N'UPDATE BookingChangeRequests SET DecidedByAccountId=@acct WHERE Id = @r', N'CK_BookingChangeRequests_Decision'),
     (N'Applied thiếu người', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedAt=@now, DecisionNote=''Đồng ý'' WHERE Id = @r', N'CK_BookingChangeRequests_Decision'),
     (N'Applied đủ quyết định', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'' WHERE Id = @r', N''),
     (N'ChangeFee âm', N'UPDATE BookingChangeRequests SET ChangeFee=-1 WHERE Id = @r', N'CK_BookingChangeRequests_Amounts'),
     (N'ChangeFee bằng 0', N'UPDATE BookingChangeRequests SET ChangeFee=0 WHERE Id = @r', N''),
     (N'ChangeFee NULL lịch sử', N'UPDATE BookingChangeRequests SET ChangeFee=NULL WHERE Id = @r', N''),
     (N'DepositCarriedOver âm', N'UPDATE BookingChangeRequests SET DepositCarriedOver=-1 WHERE Id = @r', N'CK_BookingChangeRequests_Amounts'),
     (N'DepositCarriedOver bằng 0', N'UPDATE BookingChangeRequests SET DepositCarriedOver=0 WHERE Id = @r', N''),
     (N'DepositCarriedOver NULL lịch sử', N'UPDATE BookingChangeRequests SET DepositCarriedOver=NULL WHERE Id = @r', N''),
     (N'AdditionalDeposit âm', N'UPDATE BookingChangeRequests SET AdditionalDeposit=-1 WHERE Id = @r', N'CK_BookingChangeRequests_Amounts'),
     (N'AdditionalDeposit bằng 0', N'UPDATE BookingChangeRequests SET AdditionalDeposit=0 WHERE Id = @r', N''),
     (N'AdditionalDeposit NULL lịch sử', N'UPDATE BookingChangeRequests SET AdditionalDeposit=NULL WHERE Id = @r', N''),
     (N'NewTotalAmount âm', N'UPDATE BookingChangeRequests SET NewTotalAmount=-1 WHERE Id = @r', N'CK_BookingChangeRequests_Amounts'),
     (N'NewTotalAmount bằng 0', N'UPDATE BookingChangeRequests SET NewTotalAmount=0 WHERE Id = @r', N''),
     (N'NewTotalAmount NULL lịch sử', N'UPDATE BookingChangeRequests SET NewTotalAmount=NULL WHERE Id = @r', N''),
     (N'RefundAmount âm', N'UPDATE BookingChangeRequests SET RefundAmount=-1 WHERE Id = @r', N'CK_BookingChangeRequests_Amounts'),
     (N'RefundAmount bằng 0', N'UPDATE BookingChangeRequests SET RefundAmount=0 WHERE Id = @r', N''),
     (N'RefundAmount NULL lịch sử', N'UPDATE BookingChangeRequests SET RefundAmount=NULL WHERE Id = @r', N''),
     (N'Applied có thêm tiền thiếu kế toán', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now) WHERE Id = @r', N'CK_BookingChangeRequests_Finance'),
     (N'Applied đủ kế toán', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@finance, FinanceConfirmedAt=@now, FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N''),
     (N'Kế toán xác nhận sau hạn khách nộp', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@finance, FinanceConfirmedAt=DATEADD(day,1,@now), FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N''),
     (N'FinanceNote rỗng', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@finance, FinanceConfirmedAt=@now, FinanceNote='''' WHERE Id = @r', N'CK_BookingChangeRequests_Finance'),
     (N'Kế toán xác nhận trước quản lý', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@finance, FinanceConfirmedAt=DATEADD(second,-1,@now), FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N'CK_BookingChangeRequests_Finance'),
     (N'Người gửi tự duyệt', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@sender, DecidedAt=@now, DecisionNote=''Đồng ý'' WHERE Id = @r', N'CK_BookingChangeRequests_ActorSeparation'),
     (N'Người gửi tự xác nhận tiền', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@sender, FinanceConfirmedAt=@now, FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N'CK_BookingChangeRequests_ActorSeparation'),
     (N'Quản lý tự xác nhận tiền', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@acct, FinanceConfirmedAt=@now, FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N'CK_BookingChangeRequests_ActorSeparation'),
     (N'Ba người khác nhau', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now), FinanceConfirmedByAccountId=@finance, FinanceConfirmedAt=@now, FinanceNote=''Đã nhận đủ'' WHERE Id = @r', N''),
     (N'Ghi chú quyết định rỗng', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote='''' WHERE Id = @r', N'CK_BookingChangeRequests_Notes'),
     (N'Lý do từ chối rỗng', N'UPDATE BookingChangeRequests SET Status=''Rejected'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', RejectionReason='''' WHERE Id = @r', N'CK_BookingChangeRequests_Notes'),
     (N'Từ chối có cả hai ghi chú', N'UPDATE BookingChangeRequests SET Status=''Rejected'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', RejectionReason=''Không còn phòng'' WHERE Id = @r', N''),
     (N'Lý do cũ ở DecisionNote', N'UPDATE BookingChangeRequests SET Status=''Rejected'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'' WHERE Id = @r', N''),
     (N'Chờ nộp thiếu deadline', N'UPDATE BookingChangeRequests SET Status=''AwaitingPayment'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000 WHERE Id = @r', N'CK_BookingChangeRequests_PaymentDeadline'),
     (N'Deadline bằng quyết định', N'UPDATE BookingChangeRequests SET Status=''AwaitingPayment'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=@now WHERE Id = @r', N'CK_BookingChangeRequests_PaymentDeadline'),
     (N'Deadline sau quyết định', N'UPDATE BookingChangeRequests SET Status=''AwaitingPayment'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(second,1,@now) WHERE Id = @r', N''),
     (N'Pending có deadline', N'UPDATE BookingChangeRequests SET PaymentDeadlineAt=DATEADD(minute,15,@now) WHERE Id = @r', N'CK_BookingChangeRequests_PaymentDeadline'),
     (N'Hết hạn sau duyệt', N'UPDATE BookingChangeRequests SET Status=''Expired'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'', AdditionalDeposit=1000, PaymentDeadlineAt=DATEADD(minute,15,@now) WHERE Id = @r', N''),
     (N'Phí cộng dồn âm', N'UPDATE Bookings SET ChangeFeeAmount=-1 WHERE Id=@b1', N'CK_Bookings_ChangeFeeAmount'),
     (N'Phí cộng dồn 0', N'UPDATE Bookings SET ChangeFeeAmount=0 WHERE Id=@b1', N''),
     (N'Phí cộng dồn dương', N'UPDATE Bookings SET ChangeFeeAmount=300000 WHERE Id=@b1', N''),
     (N'Cọc thêm thiếu Request', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',NULL,@now)', N'CK_Payments_ChangeRequest'),
     (N'Cọc đầu không có Request', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''Deposit'',''BankAutoMatch'',1000,''Submitted'',NULL,@now)', N''),
     (N'Cọc đầu kèm Request', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''Deposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now)', N'CK_Payments_ChangeRequest'),
     (N'Cọc thêm có Request', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now)', N''),
     (N'Một Request chỉ một khoản mở', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now); INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now)', N'IX_Payments_OneAdditionalDepositPerChangeRequest'),
     (N'Khoản bị từ chối không chiếm unique', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Rejected'',@r,@now); INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now)', N''),
     (N'Cọc đầu và cọc thêm cùng đơn', N'INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''Deposit'',''BankAutoMatch'',1000,''Submitted'',NULL,@now); INSERT Payments (BookingId,Type,Method,Amount,Status,ChangeRequestId,SubmittedAt) VALUES (@b1,''AdditionalDeposit'',''BankAutoMatch'',1000,''Submitted'',@r,@now)', N''),
     (N'Request mở thứ hai', N'INSERT BookingChangeRequests (BookingId,RequestType,RequestedByType,Status,CreatedAt) VALUES (@b1,''Cancel'',''Customer'',''Pending'',@now)', N'IX_BookingChangeRequests_OneActivePerBooking'),
     (N'Applied không chiếm unique', N'UPDATE BookingChangeRequests SET Status=''Applied'', DecidedByAccountId=@acct, DecidedAt=@now, DecisionNote=''Đồng ý'' WHERE Id=@r; INSERT BookingChangeRequests (BookingId,RequestType,RequestedByType,Status,CreatedAt) VALUES (@b1,''Cancel'',''Customer'',''Pending'',@now)', N''),
     (N'RoomNight mask 0', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',NULL,NULL,NULL)', N'CK_RoomNights_Owner'),
     (N'RoomNight mask 1', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',@b1,NULL,NULL)', N''),
     (N'RoomNight mask 2', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',NULL,@blk,NULL)', N''),
     (N'RoomNight mask 3', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',@b1,@blk,NULL)', N'CK_RoomNights_Owner'),
     (N'RoomNight mask 4', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',NULL,NULL,@r)', N''),
     (N'RoomNight mask 5', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',@b1,NULL,@r)', N'CK_RoomNights_Owner'),
     (N'RoomNight mask 6', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',NULL,@blk,@r)', N'CK_RoomNights_Owner'),
     (N'RoomNight mask 7', N'INSERT RoomNights (RoomId,Date,BookingId,RoomBlockId,BookingChangeRequestId) VALUES (@room,''2030-04-01'',@b1,@blk,@r)', N'CK_RoomNights_Owner');

    SET @max = (SELECT MAX(n) FROM @cases);
    WHILE @n <= @max
    BEGIN
        SELECT @name=Name, @sql=Sql, @expect=Expect FROM @cases WHERE n=@n;
        SET @err=NULL;
        SAVE TRANSACTION workflow_case;
        BEGIN TRY
            EXEC sp_executesql @sql, N'@r int,@b1 int,@b2 int,@rt int,@room int,@blk int,@acct nvarchar(450),@sender nvarchar(450),@finance nvarchar(450),@now datetime2',
                @r=@r,@b1=@b1,@b2=@b2,@rt=@rt,@room=@room,@blk=@blk,@acct=@acct,@sender=@sender,@finance=@finance,@now=@now;
        END TRY
        BEGIN CATCH
            SET @err=CONCAT(N'Msg ',ERROR_NUMBER(),N': ',ERROR_MESSAGE());
        END CATCH;
        IF XACT_STATE() <> 1 THROW 50200, N'Transaction hỏng - dừng.', 1;
        IF (@expect=N'' AND @err IS NULL) OR (@expect<>N'' AND @err LIKE N'%'+@expect+N'%')
        BEGIN
            SET @pass+=1; PRINT N'ĐẠT '+@name+ISNULL(N' ← '+@err,N'');
        END
        ELSE
        BEGIN
            SET @fail+=1; PRINT N'TRƯỢT '+@name+N' | mong đợi: '+@expect+N' | thực tế: '+ISNULL(@err,N'không bị chặn');
        END;
        ROLLBACK TRANSACTION workflow_case;
        SET @n+=1;
    END;
    ROLLBACK;
    PRINT CONCAT(N'Kết quả: ',@pass,N' đạt, ',@fail,N' trượt / ',@max,N' ca. Đã ROLLBACK.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
SET @rowsAfter=(SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.tables t ON t.object_id=p.object_id WHERE p.index_id IN(0,1) AND t.is_ms_shipped=0);
PRINT CONCAT(N'Số dòng sau ROLLBACK: ',@rowsAfter,N' (trước: ',@rowsBefore,N').');
IF @rowsAfter<>@rowsBefore THROW 50400,N'Số dòng sau ROLLBACK khác trước khi chạy.',1;
IF @fail>0 THROW 50300,N'Có ca trượt.',1;
GO
IF @@TRANCOUNT>0 ROLLBACK;
SELECT @@TRANCOUNT AS open_tran,
 (SELECT COUNT(*) FROM PersonProfiles WHERE PhoneNumber LIKE '+84997%')
 +(SELECT COUNT(*) FROM Bookings WHERE Code LIKE 'WF-%')
 +(SELECT COUNT(*) FROM Areas WHERE Name='ChangeRequestWorkflowTest')
 +(SELECT COUNT(*) FROM AspNetUsers WHERE Id LIKE 'wf-test-%') AS test_rows_left;
GO
