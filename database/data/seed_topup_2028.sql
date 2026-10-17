-- B6/FR-24: bổ sung dữ liệu mẫu cũ, KHÔNG phải migration hay script seed tài khoản.
-- Chỉ chạy sau khi duyệt/backup đúng database; không chạy tự động lúc khởi động.
-- sqlcmd -C -b -d <DB> -v TargetDb=<DB> -i data/seed_topup_2028.sql
:on error exit
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_NAME() <> N'$(TargetDb)' OR DB_NAME() IN (N'master',N'model',N'msdb',N'tempdb')
    OR DB_NAME() LIKE N'%RestoreCheck%'
    THROW 50030, N'Sai database đích hoặc là bản restore được bảo vệ.', 1;

DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Contracts TABLE (Id int, ContractNo nvarchar(50), OldEndDate date, EndDate date);
DECLARE @Holidays TABLE ([Date] date, Name nvarchar(100));
DECLARE @Photos TABLE (Id int, HomestayId int, FileUrl nvarchar(300));

BEGIN TRY
    BEGIN TRANSACTION;

    -- Không rút ngắn hợp đồng đã được gia hạn xa hơn, không động hợp đồng thật khác mã mẫu.
    UPDATE c SET EndDate = '20280531', UpdatedAt = @Now
    OUTPUT inserted.Id, inserted.ContractNo, deleted.EndDate, inserted.EndDate INTO @Contracts
    FROM Contracts c
    WHERE c.ContractNo LIKE 'HD-2026-[0-9][0-9][0-9][0-9]' AND c.Status = 'Active' AND c.EndDate < '20280531';

    INSERT INTO AuditLogs (AccountId, TableName, RecordId, Action, OldValues, NewValues, CreatedAt)
    SELECT NULL, 'Contracts', CONVERT(nvarchar(50), c.Id), 'Update',
        (SELECT c.OldEndDate AS EndDate FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        (SELECT 'System' AS Actor, 'SeedTopup2028' AS Step, c.ContractNo, c.EndDate FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), @Now
    FROM @Contracts c;

    -- Lịch đã chốt trong prompt; Giỗ Tổ 2028 để Admin bổ sung, không tự đoán lịch âm.
    INSERT INTO Holidays ([Date], Name)
    OUTPUT inserted.[Date], inserted.Name INTO @Holidays
    SELECT v.[Date], v.Name FROM (VALUES
        (CONVERT(date,'20280101'),N'Tết Dương lịch'),
        (CONVERT(date,'20280125'),N'Tết Nguyên đán'),
        (CONVERT(date,'20280126'),N'Tết Nguyên đán'),
        (CONVERT(date,'20280127'),N'Tết Nguyên đán'),
        (CONVERT(date,'20280128'),N'Tết Nguyên đán'),
        (CONVERT(date,'20280129'),N'Tết Nguyên đán'),
        (CONVERT(date,'20280430'),N'Ngày Giải phóng miền Nam'),
        (CONVERT(date,'20280501'),N'Quốc tế Lao động'),
        (CONVERT(date,'20280902'),N'Quốc khánh')
    ) v([Date],Name) WHERE NOT EXISTS (SELECT 1 FROM Holidays h WHERE h.[Date] = v.[Date]);

    INSERT INTO AuditLogs (AccountId, TableName, RecordId, Action, OldValues, NewValues, CreatedAt)
    SELECT NULL, 'Holidays', CONVERT(nvarchar(10), h.[Date], 23), 'Insert', NULL,
        (SELECT 'System' AS Actor, 'SeedTopup2028' AS Step, h.[Date], h.Name FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), @Now FROM @Holidays h;

    -- Giữ nguyên giấy tờ gắn hồ sơ Owner; chỉ thêm ảnh thực tế vào cơ sở mẫu chưa có ảnh gắn Homestay.
    -- File có sẵn trong Data/SampleFiles; cần có bản sao App_Data/receipts/sample-document.png (thư mục ReceiptFileStore đọc) ở môi trường chạy thử.
    INSERT INTO VerificationDocuments (OwnerRegistrationId, HomestayId, DocumentType, FileUrl, OriginalFileName, ExpiryDate, CreatedAt)
    OUTPUT inserted.Id, inserted.HomestayId, inserted.FileUrl INTO @Photos
    SELECT NULL, h.Id, 'PropertyPhoto', 'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, @Now
    FROM Homestays h WHERE EXISTS (
        SELECT 1 FROM Contracts c WHERE c.HomestayId = h.Id AND c.ContractNo LIKE 'HD-2026-[0-9][0-9][0-9][0-9]'
    ) AND NOT EXISTS (SELECT 1 FROM VerificationDocuments d WHERE d.HomestayId = h.Id AND d.DocumentType = 'PropertyPhoto');

    INSERT INTO AuditLogs (AccountId, TableName, RecordId, Action, OldValues, NewValues, CreatedAt)
    SELECT NULL, 'VerificationDocuments', CONVERT(nvarchar(50), p.Id), 'Insert', NULL,
        (SELECT 'System' AS Actor, 'SeedTopup2028' AS Step, p.HomestayId, 'PropertyPhoto' AS DocumentType, p.FileUrl FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), @Now FROM @Photos p;

    COMMIT TRANSACTION;
    SELECT (SELECT COUNT_BIG(*) FROM @Contracts) AS ContractsExtended,
        (SELECT COUNT_BIG(*) FROM @Holidays) AS HolidaysAdded,
        (SELECT COUNT_BIG(*) FROM @Photos) AS PhotosAdded;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
