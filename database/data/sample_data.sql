-- Dữ liệu mẫu Fuji Homestay, xuất từ database trắng chạy seed Development.
-- Ngày xuất: 2026-10-20T22:21:16+07:00.
-- Thứ tự: tạo database trắng → deploy.sql → data/sample_data.sql (sqlcmd, trong database/).
-- Dùng -d <TenDb> -v TargetDb=<TenDb>; chỉ nạp vào schema đúng bản và chưa có dữ liệu.
-- Ngày đơn tính theo ngày seed khi xuất; file này cố định các mốc đó, không tự dời theo ngày chạy.
-- Có mã băm Identity của tài khoản demo; chỉ dùng ở môi trường thử. Mật khẩu xem SeedData.DefaultPassword.
-- Ảnh homestay tham chiếu wwwroot/images/demo; ảnh bill/giấy tờ lấy từ Data/SampleFiles,
-- chép sample-receipt.png và sample-document.png vào App_Data/receipts nếu chưa có.
-- Không chứa khóa Data Protection; ứng dụng tự tạo khóa ở nơi cấu hình khi chạy.
-- Số dòng từng bảng nguồn (kể cả bảng rỗng; không chèn __EFMigrationsHistory):
-- __EFMigrationsHistory: 9
-- AccountActivityLogs: 0
-- Amenities: 10
-- Areas: 7
-- AspNetRoleClaims: 0
-- AspNetRoles: 3
-- AspNetUserClaims: 0
-- AspNetUserLogins: 0
-- AspNetUserRoles: 21
-- AspNetUsers: 20
-- AspNetUserTokens: 0
-- AuditLogs: 6
-- BankTransactions: 43
-- BookingChangeRequests: 5
-- BookingCodeCounters: 34
-- BookingNights: 122
-- Bookings: 60
-- BookingStatusHistories: 238
-- CancellationPolicies: 6
-- CancellationRules: 18
-- Contracts: 35
-- CustomerProfiles: 15
-- Customers: 15
-- Employees: 5
-- Holidays: 29
-- HomestayAmenities: 183
-- HomestayImages: 169
-- Homestays: 35
-- MessageLogs: 0
-- Notifications: 3
-- OwnerRegistrations: 6
-- Owners: 6
-- Payments: 53
-- PersonProfiles: 25
-- Reviews: 20
-- RoomBlocks: 6
-- RoomCategories: 5
-- RoomNights: 101
-- Rooms: 128
-- RoomTypePrices: 279
-- RoomTypes: 93
-- SettlementItems: 31
-- Settlements: 31
-- VerificationDocuments: 114
-- Vouchers: 3
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
GO

-- Toàn bộ kiểm tra và ghi nằm trong một batch/transaction; lỗi thì ROLLBACK.
BEGIN TRY
    IF DB_NAME() <> N'$(TargetDb)' OR DB_NAME() IN (N'master',N'model',N'msdb',N'tempdb')
        THROW 50100, N'Sai database đích hoặc là database hệ thống.', 1;
    IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL
        THROW 50101, N'Chưa có schema; hãy chạy deploy.sql trước.', 1;
    IF (SELECT MAX(MigrationId) FROM dbo.__EFMigrationsHistory) <> N'20261015200521_EnumAndReviewChecks'
       OR (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> 9
       OR EXISTS (SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory EXCEPT SELECT * FROM (VALUES
            (N'20261011022005_Init', N'10.0.12'),
            (N'20261011044611_ReceiptReviewAlert', N'10.0.12'),
            (N'20261011062711_Vouchers', N'10.0.12'),
            (N'20261012032051_ReviewFixes', N'10.0.12'),
            (N'20261012033411_AccountingGuards', N'10.0.12'),
            (N'20261012181745_DatabaseHardening', N'10.0.12'),
            (N'20261013032617_StatusConsistency', N'10.0.12'),
            (N'20261015092139_ChangeRequestWorkflow', N'10.0.12'),
            (N'20261015200521_EnumAndReviewChecks', N'10.0.12')
       ) expected(MigrationId, ProductVersion))
        THROW 50102, N'Schema không khớp phiên bản xuất dữ liệu mẫu.', 1;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
        THROW 50103, N'Khóa ngoại ban đầu phải bật và trusted.', 1;
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [dbo].[AccountActivityLogs] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Amenities] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Areas] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoleClaims] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserClaims] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserLogins] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserTokens] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[BankTransactions] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[BookingChangeRequests] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[BookingCodeCounters] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[BookingNights] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Bookings] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[BookingStatusHistories] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[CancellationPolicies] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[CancellationRules] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Contracts] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[CustomerProfiles] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Customers] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Employees] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Holidays] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[HomestayAmenities] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[HomestayImages] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Homestays] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[MessageLogs] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Notifications] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[OwnerRegistrations] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Owners] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Payments] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[PersonProfiles] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Reviews] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[RoomBlocks] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[RoomCategories] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[RoomNights] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Rooms] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[RoomTypePrices] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[RoomTypes] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[SettlementItems] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Settlements] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[VerificationDocuments] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;
    IF EXISTS (SELECT 1 FROM [dbo].[Vouchers] WITH (TABLOCKX, HOLDLOCK))
        THROW 50104, N'Database đã có dữ liệu; chỉ nạp vào database trắng sau deploy.sql.', 1;

    -- Vòng tham chiếu: chỉ tạm tắt các FK trong cùng thành phần có vòng.
    ALTER TABLE [dbo].[Bookings] NOCHECK CONSTRAINT [FK_Bookings_Bookings_ReplacedByBookingId];
    ALTER TABLE [dbo].[Vouchers] NOCHECK CONSTRAINT [FK_Vouchers_Bookings_SourceBookingId];
    ALTER TABLE [dbo].[Bookings] NOCHECK CONSTRAINT [FK_Bookings_Vouchers_VoucherId];

    -- Amenities: 10 dòng.
    SET IDENTITY_INSERT [dbo].[Amenities] ON;
    INSERT INTO [dbo].[Amenities] ([Id], [Name], [Icon]) VALUES
        (1, N'Wifi', N'bi-wifi'),
        (2, N'Điều hòa', N'bi-snow'),
        (3, N'Bể bơi', N'bi-water'),
        (4, N'Khu BBQ', N'bi-fire'),
        (5, N'Bãi đỗ xe', N'bi-p-circle'),
        (6, N'Bữa sáng', N'bi-cup-hot'),
        (7, N'Bếp riêng', N'bi-egg-fried'),
        (8, N'Cho phép thú cưng', N'bi-heart'),
        (9, N'Ban công', N'bi-door-open'),
        (10, N'Máy giặt', N'bi-droplet');
    SET IDENTITY_INSERT [dbo].[Amenities] OFF;
    DBCC CHECKIDENT (N'[dbo].[Amenities]', RESEED, 10) WITH NO_INFOMSGS;

    -- Areas: 7 dòng.
    SET IDENTITY_INSERT [dbo].[Areas] ON;
    INSERT INTO [dbo].[Areas] ([Id], [Name], [Province], [AreaGroup]) VALUES
        (1, N'Hoàn Kiếm', N'Hà Nội', N'InnerCity'),
        (2, N'Ba Đình', N'Hà Nội', N'InnerCity'),
        (3, N'Tây Hồ', N'Hà Nội', N'InnerCity'),
        (4, N'Đống Đa', N'Hà Nội', N'InnerCity'),
        (5, N'Long Biên', N'Hà Nội', N'InnerCity'),
        (6, N'Sóc Sơn', N'Hà Nội', N'Suburb'),
        (7, N'Ba Vì', N'Hà Nội', N'Suburb');
    SET IDENTITY_INSERT [dbo].[Areas] OFF;
    DBCC CHECKIDENT (N'[dbo].[Areas]', RESEED, 7) WITH NO_INFOMSGS;

    -- AspNetRoles: 3 dòng.
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES
        (N'38e7b375-e54d-4f85-a464-285b806e71ee', N'Customer', N'CUSTOMER', N'b00c27b5-1250-4b62-8b24-e013dc15b569'),
        (N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'Owner', N'OWNER', N'276de2c2-7641-4cd0-b79a-db2aeeea3010'),
        (N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'Employee', N'EMPLOYEE', N'37ccc5a2-8d5b-407d-8c9c-184bf4b73b9d');

    -- BookingCodeCounters: 34 dòng.
    INSERT INTO [dbo].[BookingCodeCounters] ([Date], [LastSeq]) VALUES
        (N'2026-06-01', 4),
        (N'2026-06-07', 2),
        (N'2026-06-21', 2),
        (N'2026-06-27', 1),
        (N'2026-06-29', 2),
        (N'2026-07-04', 2),
        (N'2026-07-05', 1),
        (N'2026-07-07', 1),
        (N'2026-07-10', 1),
        (N'2026-07-12', 1),
        (N'2026-07-13', 1),
        (N'2026-07-15', 2),
        (N'2026-07-29', 1),
        (N'2026-08-01', 1),
        (N'2026-08-04', 1),
        (N'2026-08-07', 1),
        (N'2026-08-09', 1),
        (N'2026-08-16', 1),
        (N'2026-08-17', 1),
        (N'2026-08-25', 1),
        (N'2026-08-27', 1),
        (N'2026-09-04', 2),
        (N'2026-09-08', 1),
        (N'2026-09-12', 2),
        (N'2026-09-15', 1),
        (N'2026-09-22', 1),
        (N'2026-09-23', 1),
        (N'2026-09-25', 2),
        (N'2026-09-29', 1),
        (N'2026-10-08', 1),
        (N'2026-10-17', 1),
        (N'2026-10-18', 4),
        (N'2026-10-19', 4),
        (N'2026-10-20', 10);

    -- Holidays: 29 dòng.
    INSERT INTO [dbo].[Holidays] ([Date], [Name]) VALUES
        (N'2026-01-01', N'Tết Dương lịch'),
        (N'2026-02-16', N'Tết Nguyên đán'),
        (N'2026-02-17', N'Tết Nguyên đán'),
        (N'2026-02-18', N'Tết Nguyên đán'),
        (N'2026-02-19', N'Tết Nguyên đán'),
        (N'2026-02-20', N'Tết Nguyên đán'),
        (N'2026-04-26', N'Giỗ Tổ Hùng Vương'),
        (N'2026-04-30', N'Ngày Giải phóng miền Nam'),
        (N'2026-05-01', N'Quốc tế Lao động'),
        (N'2026-09-01', N'Quốc khánh'),
        (N'2026-09-02', N'Quốc khánh'),
        (N'2027-01-01', N'Tết Dương lịch'),
        (N'2027-02-05', N'Tết Nguyên đán'),
        (N'2027-02-06', N'Tết Nguyên đán'),
        (N'2027-02-07', N'Tết Nguyên đán'),
        (N'2027-02-08', N'Tết Nguyên đán'),
        (N'2027-02-09', N'Tết Nguyên đán'),
        (N'2027-04-30', N'Ngày Giải phóng miền Nam'),
        (N'2027-05-01', N'Quốc tế Lao động'),
        (N'2027-09-02', N'Quốc khánh'),
        (N'2028-01-01', N'Tết Dương lịch'),
        (N'2028-01-25', N'Tết Nguyên đán'),
        (N'2028-01-26', N'Tết Nguyên đán'),
        (N'2028-01-27', N'Tết Nguyên đán'),
        (N'2028-01-28', N'Tết Nguyên đán'),
        (N'2028-01-29', N'Tết Nguyên đán'),
        (N'2028-04-30', N'Ngày Giải phóng miền Nam'),
        (N'2028-05-01', N'Quốc tế Lao động'),
        (N'2028-09-02', N'Quốc khánh');

    -- PersonProfiles: 25 dòng.
    SET IDENTITY_INSERT [dbo].[PersonProfiles] ON;
    INSERT INTO [dbo].[PersonProfiles] ([Id], [FullName], [PhoneNumber], [Email], [Address], [CreatedAt], [UpdatedAt]) VALUES
        (1, N'Quản trị viên Fuji', N'+84901000001', N'admin@fuji.vn', NULL, N'2026-10-20T15:19:43.4073760', N'2026-10-20T15:19:43.4073760'),
        (2, N'Nguyễn Thị Hương', N'+84901000002', N'huong.nt@fuji.vn', NULL, N'2026-10-20T15:19:43.9548560', N'2026-10-20T15:19:43.9548560'),
        (3, N'Phạm Thu Trang', N'+84901000003', N'ketoan@fuji.vn', NULL, N'2026-10-20T15:19:44.1146380', N'2026-10-20T15:19:44.1146380'),
        (4, N'Nguyễn Thị Lan', N'+84901000011', N'lan.nt@fuji.vn', NULL, N'2026-10-20T15:19:44.3060010', N'2026-10-20T15:19:44.3060010'),
        (5, N'Trần Văn Minh', N'+84901000012', N'minh.tv@fuji.vn', NULL, N'2026-10-20T15:19:44.4795240', N'2026-10-20T15:19:44.4795240'),
        (6, N'Lê Văn Hùng', N'+84912000001', NULL, NULL, N'2026-10-20T15:19:44.7809820', N'2026-10-20T15:19:44.7809820'),
        (7, N'Phạm Thu Hà', N'+84912000002', NULL, NULL, N'2026-10-20T15:19:45.5979140', N'2026-10-20T15:19:45.5979140'),
        (8, N'Đỗ Quang Huy', N'+84912000003', NULL, NULL, N'2026-10-20T15:19:45.8821820', N'2026-10-20T15:19:45.8821820'),
        (9, N'Vũ Minh Anh', N'+84912000004', NULL, NULL, N'2026-10-20T15:19:46.0651200', N'2026-10-20T15:19:46.0651200'),
        (10, N'Hoàng Thị Mai', N'+84912000005', NULL, NULL, N'2026-10-20T15:19:46.2355810', N'2026-10-20T15:19:46.2355810'),
        (11, N'Ngô Đức Thắng', N'+84912000006', NULL, NULL, N'2026-10-20T15:19:46.4016200', N'2026-10-20T15:19:46.4016200'),
        (12, N'Nguyễn Văn Minh', N'+84987000001', NULL, NULL, N'2026-10-20T15:19:46.5674540', N'2026-10-20T15:19:46.5674540'),
        (13, N'Trần Thị Bích', N'+84987000002', NULL, NULL, N'2026-10-20T15:19:46.9189910', N'2026-10-20T15:19:46.9189910'),
        (14, N'Đặng Thị Thu', N'+84987000003', NULL, NULL, N'2026-10-20T15:19:47.0056190', N'2026-10-20T15:19:47.0056190'),
        (15, N'Bùi Quốc Bảo', N'+84987000004', NULL, NULL, N'2026-10-20T15:19:47.1715720', N'2026-10-20T15:19:47.1715720'),
        (16, N'Phan Ngọc Ánh', N'+84987000005', NULL, NULL, N'2026-10-20T15:19:47.3277730', N'2026-10-20T15:19:47.3277730'),
        (17, N'Trịnh Văn Long', N'+84987000006', NULL, NULL, N'2026-10-20T15:19:47.4919040', N'2026-10-20T15:19:47.4919040'),
        (18, N'Võ Thị Hồng', N'+84987000007', NULL, NULL, N'2026-10-20T15:19:47.5160220', N'2026-10-20T15:19:47.5160220'),
        (19, N'Hồ Minh Khang', N'+84987000008', NULL, NULL, N'2026-10-20T15:19:47.6791580', N'2026-10-20T15:19:47.6791580'),
        (20, N'Lý Thanh Tâm', N'+84987000009', NULL, NULL, N'2026-10-20T15:19:47.7058580', N'2026-10-20T15:19:47.7058580'),
        (21, N'Mai Đức Anh', N'+84987000010', NULL, NULL, N'2026-10-20T15:19:47.8968310', N'2026-10-20T15:19:47.8968310'),
        (22, N'Chu Thị Ngân', N'+84987000011', NULL, NULL, N'2026-10-20T15:19:48.0675620', N'2026-10-20T15:19:48.0675620'),
        (23, N'Tạ Quang Vinh', N'+84987000012', NULL, NULL, N'2026-10-20T15:19:48.0918520', N'2026-10-20T15:19:48.0918520'),
        (24, N'Đinh Thị Yến', N'+84987000013', NULL, NULL, N'2026-10-20T15:19:48.2578350', N'2026-10-20T15:19:48.2578350'),
        (25, N'Kiều Văn Phúc', N'+84987000014', NULL, NULL, N'2026-10-20T15:19:48.4246430', N'2026-10-20T15:19:48.4246430');
    SET IDENTITY_INSERT [dbo].[PersonProfiles] OFF;
    DBCC CHECKIDENT (N'[dbo].[PersonProfiles]', RESEED, 25) WITH NO_INFOMSGS;

    -- RoomCategories: 5 dòng.
    SET IDENTITY_INSERT [dbo].[RoomCategories] ON;
    INSERT INTO [dbo].[RoomCategories] ([Id], [Name]) VALUES
        (1, N'Phòng đơn'),
        (2, N'Phòng đôi'),
        (3, N'Phòng gia đình'),
        (4, N'Nguyên căn'),
        (5, N'Phòng tập thể');
    SET IDENTITY_INSERT [dbo].[RoomCategories] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomCategories]', RESEED, 5) WITH NO_INFOMSGS;

    -- AspNetUsers: 20 dòng.
    INSERT INTO [dbo].[AspNetUsers] ([Id], [PersonId], [Status], [PasswordChangedAt], [CreatedAt], [UpdatedAt], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]) VALUES
        (N'0110b42d-3b94-4d4d-8c46-19893793d8ac', 8, N'Active', N'2026-10-20T15:19:45.9193100', N'2026-10-20T15:19:45.9192980', N'2026-10-20T15:19:45.9192980', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', N'0110B42D-3B94-4D4D-8C46-19893793D8AC', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEJDf+pDhVslP3WYkHkPsOu06coxXoDV8vmro5gnYC3RzV/zryqbysqns4MGk11CuSg==', N'XPP66MMP5BQITRGB74ZXU3GUD5RHUPWQ', N'ad41dd1f-fd9e-4111-95b1-f05dd4c09daa', NULL, 0, 0, NULL, 1, 0),
        (N'0f21ecd9-e567-4aa2-9971-f1049ecec930', 4, N'Active', N'2026-10-20T15:19:44.3285900', N'2026-10-20T15:19:44.3285790', N'2026-10-20T15:19:44.3285790', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', N'0F21ECD9-E567-4AA2-9971-F1049ECEC930', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEB6bjL0bh3reejmLB6D34ZcLPmnr6BC5TiLoiocjkJR/+oAjuVe8GgpZ6x8h07WnGQ==', N'MLPTCPI7DST64IFIA6TEDQ434K3AOORR', N'03884750-b4a4-4d3e-9967-c7bfa7a2a730', NULL, 0, 0, NULL, 1, 0),
        (N'244b0e39-485b-4bef-9136-7a59df1fdde7', 15, N'Active', N'2026-10-20T15:19:47.1960660', N'2026-10-20T15:19:47.1960550', N'2026-10-20T15:19:47.1960550', N'244b0e39-485b-4bef-9136-7a59df1fdde7', N'244B0E39-485B-4BEF-9136-7A59DF1FDDE7', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEI74qGO4S5yxBLPz7IHkOCUDTia4/MBrSqnj/nmE4RiROrppTqcLTycBlTUiYJ4q9g==', N'DYC5MC35JUZGAVNAOMCZTWFMF4HSBLF5', N'2dc5788c-fc0a-40f1-928b-88594fd2bb0b', NULL, 0, 0, NULL, 1, 0),
        (N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', 23, N'Active', N'2026-10-20T15:19:48.1158610', N'2026-10-20T15:19:48.1158500', N'2026-10-20T15:19:48.1158500', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'256F9D4A-ECB9-430E-B090-BC42B05CA6A5', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEJPm/r0wVl8TSe+oE3NH/wP4zokHxpljlUuC1IvgJaR00UvY/zaEhEoTFlWQSv1eGQ==', N'NHIWDRNGRKEG2N7L3W3QKC5MSPWD5M5O', N'8b090e39-8e28-4b42-b5b6-822eeeaecab2', NULL, 0, 0, NULL, 1, 0),
        (N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', 24, N'Active', N'2026-10-20T15:19:48.2849530', N'2026-10-20T15:19:48.2849420', N'2026-10-20T15:19:48.2849420', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'29AEAF6C-C20B-479D-BDDD-14B5A3E8174D', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEAwoNMnJ7/6O8zW/Zz5yWoIvDWDwAHRBizwUvlS3cAc8TVB8aHnnWpwuocahSjK0RQ==', N'XKBMTLTFSYRM5KUMJABRGWYAZHBHHGTI', N'219ac96a-8d64-44cc-b693-98d00d73c12f', NULL, 0, 0, NULL, 1, 0),
        (N'33365d91-e3a7-4120-95de-cc8894ea7fc8', 6, N'Active', N'2026-10-20T15:19:45.3109840', N'2026-10-20T15:19:45.3109720', N'2026-10-20T15:19:45.3109720', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'33365D91-E3A7-4120-95DE-CC8894EA7FC8', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEIfGnjdEHK63hZ5iOtCj/enWvFhMsUipZo3hg0cXAXzDpfKTiwD+zs/gRJGa2dQ9lA==', N'PPHRHA47QFM63KU25Q5DBPBXIJNXWJYF', N'de6fe70e-bff7-4fc3-bd76-7816025174d7', NULL, 0, 0, NULL, 1, 0),
        (N'40c01ab2-f1f5-4f22-826a-45abd55ca486', 2, N'Active', N'2026-10-20T15:19:43.9697290', N'2026-10-20T15:19:43.9697080', N'2026-10-20T15:19:43.9697080', N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'40C01AB2-F1F5-4F22-826A-45ABD55CA486', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEGsKf7I8fwHJZI2fXUeZGUWOlI3sI2RDDWrxegLoIZmIbrLl29eo3KAxhCqeZkP3bA==', N'TYG3B2EF3OQZPRHQTGMO5UFLXDJADICL', N'e58ebc71-1cdc-46c8-aa93-63c26c79af0e', NULL, 0, 0, NULL, 1, 0),
        (N'44c73fdc-7312-4954-8342-047c13457bc4', 3, N'Active', N'2026-10-20T15:19:44.1366360', N'2026-10-20T15:19:44.1366130', N'2026-10-20T15:19:44.1366130', N'44c73fdc-7312-4954-8342-047c13457bc4', N'44C73FDC-7312-4954-8342-047C13457BC4', NULL, NULL, 0, N'AQAAAAIAAYagAAAAECOw+3diJ+E+dvRUnIFG7Ak7esMk/pKwj1iX5cowMZLI4lk5XRaeYgEGYRzwOaMcgg==', N'PZHVQCKAYCCAIXRUWHPRRG6OZY722DHV', N'b7a00e8e-3e20-42ad-b1fd-2608c4efebdb', NULL, 0, 0, NULL, 1, 0),
        (N'59293239-825f-4f9f-88cd-aa308164fba4', 21, N'Active', N'2026-10-20T15:19:47.9313010', N'2026-10-20T15:19:47.9312780', N'2026-10-20T15:19:47.9312780', N'59293239-825f-4f9f-88cd-aa308164fba4', N'59293239-825F-4F9F-88CD-AA308164FBA4', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEGgke3UXbJWzvXYtFly2R8x2IEFlIE40QVJI8sawm1h5m+B5GikI1MX5lDyWXZY3vw==', N'JFJJF7Q3QY5MDLEBGANI2ZQJUSWJXU2C', N'58938ea9-15f8-4a6d-a895-f3714b16570c', NULL, 0, 0, NULL, 1, 0),
        (N'62b20678-2da9-465f-b9cc-595ed1710f0d', 14, N'Active', N'2026-10-20T15:19:47.0310810', N'2026-10-20T15:19:47.0310570', N'2026-10-20T15:19:47.0310570', N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'62B20678-2DA9-465F-B9CC-595ED1710F0D', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEApPSGBDoPGeYe30vmZbSGwBGo+59PY/8Km3sTxgLJKIFkDX3tkyktGwv/lQv74lEg==', N'LHEHAELLZWE4SFI54GYIQTFSKCZDJ4MB', N'078971c2-d1c3-43c8-a3a2-04857e752394', NULL, 0, 0, NULL, 1, 0),
        (N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', 12, N'Active', N'2026-10-20T15:19:46.6885080', N'2026-10-20T15:19:46.6884940', N'2026-10-20T15:19:46.6884950', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', N'6BBB3BE5-00D9-48F4-8839-8F7E03ACED5E', NULL, NULL, 0, N'AQAAAAIAAYagAAAAELbFnEtdBtuarYUeDYHZTtRBQ8rHwEf19bykogJ7/EfHi+QzOe8IQdgu0TSVdBlBPg==', N'K7LT4OQMET5GAXWAPWFK7B5TZCNVXKGG', N'51c51869-bfe8-448f-96f0-4966f7a6a119', NULL, 0, 0, NULL, 1, 0),
        (N'6fba46ec-84e5-41fb-86b1-ce1fb055d8f4', 1, N'Active', N'2026-10-20T15:19:43.5317630', N'2026-10-20T15:19:43.5316570', N'2026-10-20T15:19:43.5316570', N'6fba46ec-84e5-41fb-86b1-ce1fb055d8f4', N'6FBA46EC-84E5-41FB-86B1-CE1FB055D8F4', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEDBr1ZlkvaVpn1xxF+bX/pwDylLD01+eIX6AlYBPeIFOQovI3kPxBdTiGo2ZJglpSg==', N'2V7VL32XKDOKLQXMEJL4EJZYVEVT6NF2', N'c105baac-2b07-4d4d-bd84-df0020b26631', NULL, 0, 0, NULL, 1, 0),
        (N'89010c48-12f1-4c0d-b829-c1679b02e399', 20, N'Active', N'2026-10-20T15:19:47.7344100', N'2026-10-20T15:19:47.7344000', N'2026-10-20T15:19:47.7344000', N'89010c48-12f1-4c0d-b829-c1679b02e399', N'89010C48-12F1-4C0D-B829-C1679B02E399', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEFOHXOnHTWmzWYFSbEtfpPSYT/VmAhwueB9PnI8Wf/U31T1CY+0N/f1BaX1tpJr7bQ==', N'LAL2RQ3BURRKDQCBCK3XSS2DCIZH5NUZ', N'e7239abd-06b9-4be8-8d6f-e4e4d47f0aed', NULL, 0, 0, NULL, 1, 0),
        (N'9a24dade-ea07-4f85-aa55-c70d964a8024', 5, N'Active', N'2026-10-20T15:19:44.5049660', N'2026-10-20T15:19:44.5049100', N'2026-10-20T15:19:44.5049100', N'9a24dade-ea07-4f85-aa55-c70d964a8024', N'9A24DADE-EA07-4F85-AA55-C70D964A8024', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEMg+DOKDU3pYyG4t3f+9dB1YUxzEI4QQOTiBZFto9gawHGUN4v+SSFLhTuopi/d6lg==', N'U3JVOIPLYOZACQXGYWOYZOR4LYPZD7LS', N'b993ae60-f9eb-4ee3-b293-5058f5a658db', NULL, 0, 0, NULL, 1, 0),
        (N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', 7, N'Active', N'2026-10-20T15:19:45.6771700', N'2026-10-20T15:19:45.6771320', N'2026-10-20T15:19:45.6771320', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'9D0C1AC4-F49D-4E4E-B42C-F19C5C63FF2A', NULL, NULL, 0, N'AQAAAAIAAYagAAAAECve8WsmOkn7OeFfR7FH4EMJiA6ZsoSJ+n7kXHhgtjVoB3caXlkS7XDbtmIgxgEtAA==', N'QZRLIOY5QSN6WJECZZ52SRZCZHUOCNRQ', N'68301bf8-438f-454f-aaaa-1bb6a7077af9', NULL, 0, 0, NULL, 1, 0),
        (N'a9099925-9567-4dda-8121-d17926c1675e', 16, N'Active', N'2026-10-20T15:19:47.3569440', N'2026-10-20T15:19:47.3569350', N'2026-10-20T15:19:47.3569350', N'a9099925-9567-4dda-8121-d17926c1675e', N'A9099925-9567-4DDA-8121-D17926C1675E', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEDpT36y1Hlxg76y4YexjsbK/uqDgDEljy3Ge0YGaITz0A5j02ZUtq2rc6p84uW7XhQ==', N'T7FQJOP2XU27OZS44SWBL2HTJK6FIRO5', N'b34ca0f7-9ae1-454a-a81d-d2175c43883d', NULL, 0, 0, NULL, 1, 0),
        (N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', 18, N'Active', N'2026-10-20T15:19:47.5422930', N'2026-10-20T15:19:47.5422760', N'2026-10-20T15:19:47.5422770', N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', N'AD7D6E89-F20C-4EF1-BC81-B7E30F4D8A30', NULL, NULL, 0, N'AQAAAAIAAYagAAAAECtDNbHhou9jKnf/5I8MWlQizJ6YvchV+oXqUYp7hEHVFPew4aFUXjRFQaXzZQQBAA==', N'3HONDSMLXGFOFYXHFUBD4ZYOP6YU553W', N'ac90103a-ff23-4d07-83b0-f17f60a1fd00', NULL, 0, 0, NULL, 1, 0),
        (N'b0266ef0-61ff-42de-9be9-c2102618e46e', 9, N'Active', N'2026-10-20T15:19:46.0960430', N'2026-10-20T15:19:46.0960310', N'2026-10-20T15:19:46.0960310', N'b0266ef0-61ff-42de-9be9-c2102618e46e', N'B0266EF0-61FF-42DE-9BE9-C2102618E46E', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEHkew5uvyIoVDjlWAO6HbWy7ujC/n+cMJVYH4DcjrkKWPbkAh+fsvwAJ1b+5WQimQA==', N'SX7UYIRCEXWW4TSKA2XAUZZAVNGRLIHH', N'c92705de-23af-4715-967f-9eb0b9a0c555', NULL, 0, 0, NULL, 1, 0),
        (N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', 10, N'Active', N'2026-10-20T15:19:46.2655620', N'2026-10-20T15:19:46.2655510', N'2026-10-20T15:19:46.2655510', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', N'B12B5C52-B60A-4356-AC02-EBF3B56D0295', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEJKa89Fr4cS6KmDdL1woD37KUPVgEQ6531TdTBTCJda6vykKAhwDDK/CZdT2fgyyYQ==', N'3JDHJ6LLDGPZNT654FIVKUVLYXNFHGV6', N'a1233248-ed6b-4b62-ad3f-14971ddcb71f', NULL, 0, 0, NULL, 1, 0),
        (N'ca9baa4c-8332-4079-a375-be369eb728fb', 11, N'Active', N'2026-10-20T15:19:46.4269340', N'2026-10-20T15:19:46.4269170', N'2026-10-20T15:19:46.4269170', N'ca9baa4c-8332-4079-a375-be369eb728fb', N'CA9BAA4C-8332-4079-A375-BE369EB728FB', NULL, NULL, 0, N'AQAAAAIAAYagAAAAECtwSw6baSwuYVWbFOph1tzVxNK/sLBqmosl5Vr6QbDKfyczLIQvQXH0UsPPywvq1A==', N'J7JIX6FISGWWYYWHKTXVVMALFGZDH2B7', N'd62dcdb8-f030-4ced-8b90-e3b96f6b39a8', NULL, 0, 0, NULL, 1, 0);

    -- Customers: 15 dòng.
    SET IDENTITY_INSERT [dbo].[Customers] ON;
    INSERT INTO [dbo].[Customers] ([Id], [PersonId], [Note], [CreatedAt], [UpdatedAt]) VALUES
        (1, 12, NULL, N'2026-10-20T15:19:46.5764360', N'2026-10-20T15:19:46.5764360'),
        (2, 13, NULL, N'2026-10-20T15:19:46.9277000', N'2026-10-20T15:19:46.9277000'),
        (3, 6, NULL, N'2026-10-20T15:19:46.9479890', N'2026-10-20T15:19:46.9479890'),
        (4, 14, NULL, N'2026-10-20T15:19:47.0133170', N'2026-10-20T15:19:47.0133170'),
        (5, 15, NULL, N'2026-10-20T15:19:47.1784890', N'2026-10-20T15:19:47.1784900'),
        (6, 16, NULL, N'2026-10-20T15:19:47.3373580', N'2026-10-20T15:19:47.3373580'),
        (7, 17, NULL, N'2026-10-20T15:19:47.4995300', N'2026-10-20T15:19:47.4995300'),
        (8, 18, NULL, N'2026-10-20T15:19:47.5244500', N'2026-10-20T15:19:47.5244500'),
        (9, 19, NULL, N'2026-10-20T15:19:47.6873610', N'2026-10-20T15:19:47.6873610'),
        (10, 20, NULL, N'2026-10-20T15:19:47.7146600', N'2026-10-20T15:19:47.7146600'),
        (11, 21, NULL, N'2026-10-20T15:19:47.9096180', N'2026-10-20T15:19:47.9096180'),
        (12, 22, NULL, N'2026-10-20T15:19:48.0743210', N'2026-10-20T15:19:48.0743210'),
        (13, 23, NULL, N'2026-10-20T15:19:48.0986190', N'2026-10-20T15:19:48.0986190'),
        (14, 24, NULL, N'2026-10-20T15:19:48.2655910', N'2026-10-20T15:19:48.2655910'),
        (15, 25, NULL, N'2026-10-20T15:19:48.4318740', N'2026-10-20T15:19:48.4318740');
    SET IDENTITY_INSERT [dbo].[Customers] OFF;
    DBCC CHECKIDENT (N'[dbo].[Customers]', RESEED, 15) WITH NO_INFOMSGS;

    -- Employees: 5 dòng.
    SET IDENTITY_INSERT [dbo].[Employees] ON;
    INSERT INTO [dbo].[Employees] ([Id], [PersonId], [EmployeeCode], [Department], [Position], [EmploymentStatus], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'AD001', N'Kỹ thuật', N'Administrator', N'Active', N'2026-10-20T15:19:43.4491150', N'2026-10-20T15:19:43.4491150'),
        (2, 2, N'QL001', N'Ban giám đốc', N'Manager', N'Active', N'2026-10-20T15:19:43.9603380', N'2026-10-20T15:19:43.9603380'),
        (3, 3, N'KT001', N'Kế toán', N'Accountant', N'Active', N'2026-10-20T15:19:44.1247890', N'2026-10-20T15:19:44.1247890'),
        (4, 4, N'NV001', N'Tư vấn đặt phòng', N'Consultant', N'Active', N'2026-10-20T15:19:44.3154250', N'2026-10-20T15:19:44.3154250'),
        (5, 5, N'NV002', N'Tư vấn đặt phòng', N'Consultant', N'Active', N'2026-10-20T15:19:44.4893110', N'2026-10-20T15:19:44.4893110');
    SET IDENTITY_INSERT [dbo].[Employees] OFF;
    DBCC CHECKIDENT (N'[dbo].[Employees]', RESEED, 5) WITH NO_INFOMSGS;

    -- Owners: 6 dòng.
    SET IDENTITY_INSERT [dbo].[Owners] ON;
    INSERT INTO [dbo].[Owners] ([Id], [PersonId], [BankName], [BankAccountNo], [BankAccountName], [CreatedAt], [UpdatedAt]) VALUES
        (1, 6, N'MB Bank', N'0912000001', N'LÊ VĂN HÙNG', N'2026-10-20T15:19:44.7893070', N'2026-10-20T15:19:44.7893070'),
        (2, 7, N'MB Bank', N'0912000002', N'PHẠM THU HÀ', N'2026-10-20T15:19:45.6427140', N'2026-10-20T15:19:45.6427140'),
        (3, 8, N'MB Bank', N'0912000003', N'ĐỖ QUANG HUY', N'2026-10-20T15:19:45.8932440', N'2026-10-20T15:19:45.8932440'),
        (4, 9, N'MB Bank', N'0912000004', N'VŨ MINH ANH', N'2026-10-20T15:19:46.0735540', N'2026-10-20T15:19:46.0735540'),
        (5, 10, N'MB Bank', N'0912000005', N'HOÀNG THỊ MAI', N'2026-10-20T15:19:46.2457740', N'2026-10-20T15:19:46.2457740'),
        (6, 11, NULL, NULL, NULL, N'2026-10-20T15:19:46.4085190', N'2026-10-20T15:19:46.4085190');
    SET IDENTITY_INSERT [dbo].[Owners] OFF;
    DBCC CHECKIDENT (N'[dbo].[Owners]', RESEED, 6) WITH NO_INFOMSGS;

    -- AspNetUserRoles: 21 dòng.
    INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId], [AssignedAt], [AssignedByAccountId]) VALUES
        (N'0110b42d-3b94-4d4d-8c46-19893793d8ac', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:46.0376820', NULL),
        (N'0f21ecd9-e567-4aa2-9971-f1049ecec930', N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'2026-10-20T15:19:44.4571950', NULL),
        (N'244b0e39-485b-4bef-9136-7a59df1fdde7', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:47.3107400', NULL),
        (N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:48.2365690', NULL),
        (N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:48.4066860', NULL),
        (N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:46.9845910', NULL),
        (N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:45.5431260', NULL),
        (N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'2026-10-20T15:19:44.0947210', NULL),
        (N'44c73fdc-7312-4954-8342-047c13457bc4', N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'2026-10-20T15:19:44.2562410', NULL),
        (N'59293239-825f-4f9f-88cd-aa308164fba4', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:48.0491050', NULL),
        (N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:47.1533830', NULL),
        (N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:46.8892200', NULL),
        (N'6fba46ec-84e5-41fb-86b1-ce1fb055d8f4', N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'2026-10-20T15:19:43.8419960', NULL),
        (N'89010c48-12f1-4c0d-b829-c1679b02e399', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:47.8598250', NULL),
        (N'9a24dade-ea07-4f85-aa55-c70d964a8024', N'fe48af93-f0da-47b2-a1ea-477332c95d59', N'2026-10-20T15:19:44.7267510', NULL),
        (N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:45.8450820', NULL),
        (N'a9099925-9567-4dda-8121-d17926c1675e', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:47.4712550', NULL),
        (N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', N'38e7b375-e54d-4f85-a464-285b806e71ee', N'2026-10-20T15:19:47.6594690', NULL),
        (N'b0266ef0-61ff-42de-9be9-c2102618e46e', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:46.2165920', NULL),
        (N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:46.3827360', NULL),
        (N'ca9baa4c-8332-4079-a375-be369eb728fb', N'be7a19e6-d569-4e20-a460-9c61cf25a4a4', N'2026-10-20T15:19:46.5465920', NULL);

    -- AuditLogs: 6 dòng.
    SET IDENTITY_INSERT [dbo].[AuditLogs] ON;
    INSERT INTO [dbo].[AuditLogs] ([Id], [AccountId], [TableName], [RecordId], [Action], [OldValues], [NewValues], [IpAddress], [CreatedAt]) VALUES
        (1, N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'Bookings', N'59', N'Update', N'{"CheckIn":"2026-11-11","CheckOut":"2026-11-12","TotalAmount":4000000,"Status":3,"ConfirmedAt":"2026-10-19T06:37:49.441178Z"}', N'{"CheckIn":"2026-11-18","CheckOut":"2026-11-19","TotalAmount":4000000,"Status":2,"ConfirmedAt":null}', NULL, N'2026-10-19T07:38:49.4411780'),
        (2, N'89010c48-12f1-4c0d-b829-c1679b02e399', N'BookingChangeRequests', N'1', N'Insert', NULL, N'{"Status":0,"RequestType":0,"NewTotalAmount":1360000,"ChangeFee":204000,"DepositCarriedOver":204000,"AdditionalDeposit":204000,"DecisionNote":null,"RejectionReason":null}', NULL, N'2026-10-20T12:19:52.6361690'),
        (3, N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'BookingChangeRequests', N'2', N'Insert', NULL, N'{"Status":3,"RequestType":0,"NewTotalAmount":4320000,"ChangeFee":0,"DepositCarriedOver":1296000,"AdditionalDeposit":0,"DecisionNote":"Ng\u00E0y m\u1EDBi homestay \u0111\u00E3 k\u00EDn ph\u00F2ng, mong kh\u00E1ch th\u00F4ng c\u1EA3m.","RejectionReason":"Ng\u00E0y m\u1EDBi homestay \u0111\u00E3 k\u00EDn ph\u00F2ng, mong kh\u00E1ch th\u00F4ng c\u1EA3m."}', NULL, N'2026-10-19T11:19:52.6361690'),
        (4, N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'BookingChangeRequests', N'3', N'Insert', NULL, N'{"Status":2,"RequestType":0,"NewTotalAmount":4000000,"ChangeFee":0,"DepositCarriedOver":1200000,"AdditionalDeposit":0,"DecisionNote":"\u0110\u1ED3ng \u00FD \u0111\u1ED5i ng\u00E0y, kh\u00F4ng c\u1EA7n n\u1ED9p th\u00EAm c\u1ECDc.","RejectionReason":null}', NULL, N'2026-10-19T07:38:49.4411780'),
        (5, N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', N'BookingChangeRequests', N'4', N'Insert', NULL, N'{"Status":0,"RequestType":1,"NewTotalAmount":2000000,"ChangeFee":0,"DepositCarriedOver":240000,"AdditionalDeposit":360000,"DecisionNote":null,"RejectionReason":null}', NULL, N'2026-10-20T10:19:52.6361690'),
        (6, N'0f21ecd9-e567-4aa2-9971-f1049ecec930', N'BookingChangeRequests', N'5', N'Insert', NULL, N'{"Status":0,"RequestType":3,"NewTotalAmount":0,"ChangeFee":0,"DepositCarriedOver":0,"AdditionalDeposit":0,"DecisionNote":null,"RejectionReason":null}', NULL, N'2026-10-20T14:19:52.6361690');
    SET IDENTITY_INSERT [dbo].[AuditLogs] OFF;
    DBCC CHECKIDENT (N'[dbo].[AuditLogs]', RESEED, 6) WITH NO_INFOMSGS;

    -- CustomerProfiles: 15 dòng.
    INSERT INTO [dbo].[CustomerProfiles] ([CustomerId], [CitizenId], [StudentId], [StudentVerificationStatus], [CreatedAt], [UpdatedAt]) VALUES
        (1, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:46.5764980', N'2026-10-20T15:19:46.5764980'),
        (2, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:46.9277000', N'2026-10-20T15:19:46.9277000'),
        (3, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:46.9479890', N'2026-10-20T15:19:46.9479890'),
        (4, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.0133170', N'2026-10-20T15:19:47.0133170'),
        (5, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.1784900', N'2026-10-20T15:19:47.1784900'),
        (6, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.3373580', N'2026-10-20T15:19:47.3373580'),
        (7, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.4995310', N'2026-10-20T15:19:47.4995310'),
        (8, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.5244660', N'2026-10-20T15:19:47.5244660'),
        (9, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.6873620', N'2026-10-20T15:19:47.6873620'),
        (10, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.7146610', N'2026-10-20T15:19:47.7146610'),
        (11, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:47.9096190', N'2026-10-20T15:19:47.9096190'),
        (12, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:48.0743220', N'2026-10-20T15:19:48.0743220'),
        (13, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:48.0986200', N'2026-10-20T15:19:48.0986200'),
        (14, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:48.2655920', N'2026-10-20T15:19:48.2655920'),
        (15, NULL, NULL, N'NotSubmitted', N'2026-10-20T15:19:48.4318750', N'2026-10-20T15:19:48.4318750');

    -- Homestays: 35 dòng.
    SET IDENTITY_INSERT [dbo].[Homestays] ON;
    INSERT INTO [dbo].[Homestays] ([Id], [OwnerId], [AreaId], [Name], [Address], [Latitude], [Longitude], [DescriptionHtml], [CheckinGuideHtml], [ContactPhone], [CheckinTime], [CheckoutTime], [Status], [ApprovedByEmployeeId], [ApprovedAt], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, 1, N'Phố Cổ Corner House', N'12 Hàng Bạc, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Căn nhà ống ba tầng ngay góc phố Hàng Bạc, bước chân ra là chợ đêm cuối tuần và phố đi bộ Hồ Gươm. Phòng cách âm tốt, có cửa sổ nhìn xuống phố cổ, rất hợp với khách lần đầu đến Hà Nội.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.5995550', N'2026-10-20T15:19:48.5995550'),
        (2, 2, 1, N'Hồ Gươm Little Home', N'35 Lò Sũ, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Ngôi nhà nhỏ trong ngõ yên tĩnh phố Lò Sũ, đi bộ 3 phút ra hồ Hoàn Kiếm. Chủ nhà là người Hà Nội gốc, sẵn lòng chỉ quán bún chả, phở gánh ngon quanh khu phố.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8022680', N'2026-10-20T15:19:48.8022680'),
        (3, 3, 1, N'Tạ Hiện Night Stay', N'8 Tạ Hiện, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Nằm giữa phố Tạ Hiện sôi động, phù hợp với nhóm bạn trẻ thích khám phá phố bia và ẩm thực đêm. Phòng ngủ đặt ở tầng trên, lắp kính hai lớp để vẫn ngủ ngon khi phố còn đông.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8045570', N'2026-10-20T15:19:48.8045570'),
        (4, 4, 1, N'Hàng Gai Heritage', N'61 Hàng Gai, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Nhà phố được giữ lại mặt tiền kiểu Pháp cũ, nội thất gỗ và gạch bông truyền thống. Phố Hàng Gai nổi tiếng lụa tơ tằm, cách Nhà thờ Lớn chỉ 5 phút đi bộ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8065130', N'2026-10-20T15:19:48.8065130'),
        (5, 5, 1, N'Old Quarter Loft', N'20 Mã Mây, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Căn gác xép thiết kế mở với trần cao, nhiều ánh sáng tự nhiên, nằm trên phố Mã Mây. Có góc làm việc riêng và wifi mạnh, phù hợp khách công tác kết hợp du lịch.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8078210', N'2026-10-20T15:19:48.8078210'),
        (6, 1, 2, N'Ba Đình Green Villa', N'15 Đội Cấn, Ba Đình, Hà Nội', NULL, NULL, N'<p>Biệt thự nhỏ phủ cây xanh trên phố Đội Cấn, gần Lăng Bác và Hoàng thành Thăng Long. Không gian rộng, sân trước để được xe máy, thích hợp gia đình có người lớn tuổi.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8089690', N'2026-10-20T15:19:48.8089690'),
        (7, 2, 2, N'Liễu Giai Sky Home', N'102 Liễu Giai, Ba Đình, Hà Nội', NULL, NULL, N'<p>Căn hộ tầng cao gần Lotte Center, nhìn thẳng ra đường chân trời Hà Nội về đêm. Tòa nhà có bảo vệ 24/7 và thang máy, tiện cho khách đi công tác.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8097740', N'2026-10-20T15:19:48.8097740'),
        (8, 3, 2, N'Trúc Bạch Lakeside', N'7 Trúc Bạch, Ba Đình, Hà Nội', NULL, NULL, N'<p>Ngôi nhà sát hồ Trúc Bạch, sáng sớm có thể chạy bộ quanh hồ hoặc đạp vịt cùng trẻ nhỏ. Gần phố ẩm thực Ngũ Xã với món phở cuốn nổi tiếng.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8106910', N'2026-10-20T15:19:48.8106920'),
        (9, 4, 2, N'Kim Mã Cozy Nest', N'45 Kim Mã, Ba Đình, Hà Nội', NULL, NULL, N'<p>Homestay ấm cúng trên đường Kim Mã, gần bến xe buýt và các tuyến tàu điện. Giá hợp lý, phòng gọn gàng, phù hợp sinh viên và khách đi một mình.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8114530', N'2026-10-20T15:19:48.8114540'),
        (10, 5, 2, N'Ngọc Hà Garden', N'28 Ngọc Hà, Ba Đình, Hà Nội', NULL, NULL, N'<p>Nhà vườn trong làng hoa Ngọc Hà xưa, có khoảng sân nhỏ trồng cây và bàn trà ngoài trời. Không khí yên bình hiếm có giữa trung tâm thành phố.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8125080', N'2026-10-20T15:19:48.8125080'),
        (11, 1, 3, N'Tây Hồ Sunset Homestay', N'52 Từ Hoa, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Nằm trên phố Từ Hoa, cách mép Hồ Tây vài bước chân, nổi tiếng với cảnh hoàng hôn từ ban công tầng ba. Xung quanh có nhiều quán cà phê ven hồ và đường đạp xe.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8133890', N'2026-10-20T15:19:48.8133890'),
        (12, 2, 3, N'Quảng An Lotus House', N'18 Quảng An, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Ngôi nhà mang phong cách tối giản Nhật Bản ở khu Quảng An, gần đầm sen Quảng Bá. Mùa hè có thể dậy sớm ngắm sen nở và chụp ảnh áo dài.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8146080', N'2026-10-20T15:19:48.8146080'),
        (13, 3, 3, N'Xuân Diệu Westlake Studio', N'33 Xuân Diệu, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Studio hiện đại trên phố Xuân Diệu, khu vực đông khách nước ngoài với nhiều nhà hàng quốc tế. Phù hợp cặp đôi muốn kỳ nghỉ ngắn thư thả bên Hồ Tây.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8164810', N'2026-10-20T15:19:48.8164810'),
        (14, 4, 3, N'Nhật Chiêu Hideaway', N'9 Nhật Chiêu, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Căn nhà nép trong ngõ phố Nhật Chiêu, tách biệt khỏi tiếng ồn, có sân thượng trồng cây. Chỉ 5 phút đi bộ đến phủ Tây Hồ và đường ven hồ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8176420', N'2026-10-20T15:19:48.8176420'),
        (15, 5, 3, N'Yên Phụ Riverside', N'140 Yên Phụ, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Ngay mặt đường Yên Phụ, nhìn ra sông Hồng và cầu Long Biên. Thuận tiện di chuyển cả về phố cổ lẫn khu Hồ Tây, có chỗ để ô tô.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8186430', N'2026-10-20T15:19:48.8186430'),
        (16, 1, 4, N'Văn Miếu Scholar Home', N'25 Quốc Tử Giám, Đống Đa, Hà Nội', NULL, NULL, N'<p>Nhà phố đối diện Văn Miếu – Quốc Tử Giám, trang trí bằng thư pháp và đồ gỗ truyền thống. Lựa chọn quen thuộc của phụ huynh đưa con đi thi hoặc khách yêu văn hóa.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8201640', N'2026-10-20T15:19:48.8201640'),
        (17, 2, 4, N'Hoàng Cầu Lake View', N'68 Hoàng Cầu, Đống Đa, Hà Nội', NULL, NULL, N'<p>Căn hộ nhìn ra hồ Hoàng Cầu, buổi tối có đường dạo bộ và khu ẩm thực quanh hồ. Gần các trung tâm thương mại và bệnh viện lớn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8211090', N'2026-10-20T15:19:48.8211100'),
        (18, 3, 4, N'Thái Hà Urban Nest', N'120 Thái Hà, Đống Đa, Hà Nội', NULL, NULL, N'<p>Homestay trên phố Thái Hà sầm uất, gần nhiều trường đại học và nhà hát. Phòng mới sửa, có bếp chung đầy đủ dụng cụ để tự nấu ăn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8222010', N'2026-10-20T15:19:48.8222010'),
        (19, 4, 4, N'Khâm Thiên Retro House', N'77 Khâm Thiên, Đống Đa, Hà Nội', NULL, NULL, N'<p>Ngôi nhà mang phong cách thập niên 80 với đồ cổ, radio và tranh cổ động, trên phố Khâm Thiên. Trải nghiệm hoài niệm lý thú cho khách thích chụp ảnh.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8241280', N'2026-10-20T15:19:48.8241280'),
        (20, 5, 4, N'Láng Hạ Family Stay', N'40 Láng Hạ, Đống Đa, Hà Nội', NULL, NULL, N'<p>Nhà rộng trên phố Láng Hạ, nhiều phòng ngủ thông nhau, phù hợp đại gia đình hoặc nhóm đông người. Gần công viên Thủ Lệ và Bảo tàng Dân tộc học.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8261550', N'2026-10-20T15:19:48.8261550'),
        (21, 1, 5, N'Long Biên Bridge House', N'6 Ngọc Thụy, Long Biên, Hà Nội', NULL, NULL, N'<p>Ngôi nhà ở Ngọc Thụy, ngay chân cầu Long Biên, có thể đi bộ lên cầu ngắm bình minh trên sông Hồng. Bãi giữa sông có vườn hoa và đường đạp xe cuối tuần.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8277820', N'2026-10-20T15:19:48.8277820'),
        (22, 2, 5, N'Bồ Đề Garden Home', N'88 Bồ Đề, Long Biên, Hà Nội', NULL, NULL, N'<p>Nhà có vườn rộng ở phường Bồ Đề, không khí thoáng đãng, cách phố cổ chỉ 10 phút lái xe qua cầu. Trẻ em có chỗ chạy nhảy thoải mái.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8290420', N'2026-10-20T15:19:48.8290420'),
        (23, 3, 5, N'Vinhomes Riverside Suite', N'Hoa Lan 3, Phúc Lợi, Long Biên, Hà Nội', NULL, NULL, N'<p>Căn biệt thự trong khu đô thị Vinhomes Riverside với hồ nước, cây xanh và an ninh tốt. Thích hợp gia đình muốn nghỉ dưỡng yên tĩnh mà vẫn gần trung tâm.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8299680', N'2026-10-20T15:19:48.8299680'),
        (24, 4, 5, N'Gia Thụy Quiet Corner', N'15 Gia Thụy, Long Biên, Hà Nội', NULL, NULL, N'<p>Góc nhỏ yên bình ở Gia Thụy, gần công viên và chợ dân sinh. Giá mềm, chủ nhà thân thiện, phù hợp khách ở dài ngày.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8310020', N'2026-10-20T15:19:48.8310020'),
        (25, 5, 5, N'Sài Đồng Family Villa', N'52 Sài Đồng, Long Biên, Hà Nội', NULL, NULL, N'<p>Biệt thự gia đình ở Sài Đồng, có sân nướng ngoài trời và phòng khách lớn. Gần Aeon Mall Long Biên để mua sắm, vui chơi cùng trẻ nhỏ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8317510', N'2026-10-20T15:19:48.8317520'),
        (26, 1, 6, N'Sóc Sơn Pine Hill', N'Thôn Đền Gióng, Phù Linh, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Nằm giữa đồi thông gần đền Gióng, không khí mát lành quanh năm. Buổi tối có thể đốt lửa trại và nướng BBQ cùng bạn bè, chỉ cách sân bay Nội Bài 20 phút.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8325650', N'2026-10-20T15:19:48.8325650'),
        (27, 2, 6, N'Hàm Lợn Lake Camp', N'Hồ Hàm Lợn, Nam Sơn, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Khu cắm trại ven hồ Hàm Lợn với nhà gỗ và lều glamping. Hoạt động chèo SUP, câu cá và ngắm hoàng hôn trên mặt hồ là điểm nhấn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8342520', N'2026-10-20T15:19:48.8342520'),
        (28, 3, 6, N'Đồng Đò Forest Villa', N'Hồ Đồng Đò, Minh Trí, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Biệt thự ẩn trong rừng ven hồ Đồng Đò, có bể bơi và khu BBQ riêng. Lựa chọn lý tưởng cho nhóm công ty tổ chức team building cuối tuần.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8353810', N'2026-10-20T15:19:48.8353810'),
        (29, 4, 6, N'Minh Phú Garden Retreat', N'Xã Minh Phú, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Khu vườn rộng trồng cây ăn quả ở xã Minh Phú, khách được tự tay hái rau và nấu bữa quê. Nhà thoáng mát, nhiều góc chụp ảnh đồng quê.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8366570', N'2026-10-20T15:19:48.8366570'),
        (30, 5, 6, N'Sóc Sơn Hilltop House', N'Xã Bắc Sơn, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Ngôi nhà trên đỉnh đồi ở Bắc Sơn, nhìn toàn cảnh thung lũng và hồ nước phía xa. Đêm trong trời có thể ngắm sao, sáng sớm thường có sương mờ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8388570', N'2026-10-20T15:19:48.8388570'),
        (31, 1, 7, N'Ba Vì Mountain Lodge', N'Xã Ba Vì, Ba Vì, Hà Nội', NULL, NULL, N'<p>Nhà gỗ dưới chân núi Ba Vì, gần Vườn quốc gia và đền Thượng. Thích hợp khách thích leo núi, đi bộ đường rừng và tận hưởng không khí trong lành.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8405590', N'2026-10-20T15:19:48.8405590'),
        (32, 2, 7, N'Tản Viên Cloud Villa', N'Xã Tản Lĩnh, Ba Vì, Hà Nội', NULL, NULL, N'<p>Biệt thự ở Tản Lĩnh, nhiều buổi sáng có biển mây bao quanh. Có bể bơi nước ấm và phòng xông hơi lá thuốc của người Dao.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8477260', N'2026-10-20T15:19:48.8477260'),
        (33, 3, 7, N'Suối Ngọc Bamboo House', N'Xã Vân Hòa, Ba Vì, Hà Nội', NULL, NULL, N'<p>Nhà tre nằm cạnh suối Ngọc, mùa hè có thể tắm suối mát lạnh. Thiết kế mở, gần gũi thiên nhiên, có võng và chòi nghỉ bên dòng nước.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8489830', N'2026-10-20T15:19:48.8489830'),
        (34, 4, 7, N'Đá Chông Stone Villa', N'Xã Minh Quang, Ba Vì, Hà Nội', NULL, NULL, N'<p>Biệt thự xây bằng đá ong và đá núi ở Minh Quang, mát vào mùa hè và ấm vào mùa đông. Có sân vườn rộng, phù hợp nhóm đông tổ chức tiệc.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8506900', N'2026-10-20T15:19:48.8506900'),
        (35, 5, 7, N'Khoang Xanh Eco Stay', N'Xã Vân Hòa, Ba Vì, Hà Nội', NULL, NULL, N'<p>Khu lưu trú sinh thái gần thác Khoang Xanh, chủ trương hạn chế đồ nhựa và dùng thực phẩm địa phương. Hợp với gia đình muốn trẻ nhỏ trải nghiệm thiên nhiên.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-20T15:19:48.5993060', N'2026-10-20T15:19:48.8519270', N'2026-10-20T15:19:48.8519270');
    SET IDENTITY_INSERT [dbo].[Homestays] OFF;
    DBCC CHECKIDENT (N'[dbo].[Homestays]', RESEED, 35) WITH NO_INFOMSGS;

    -- Notifications: 3 dòng.
    SET IDENTITY_INSERT [dbo].[Notifications] ON;
    INSERT INTO [dbo].[Notifications] ([Id], [UserId], [Message], [Url], [IsRead], [CreatedAt]) VALUES
        (1, N'0110b42d-3b94-4d4d-8c46-19893793d8ac', N'Đơn FJ261019-004 đã đổi ngày, vui lòng xác nhận lại.', N'/Owner/Bookings/Details/59', 0, N'2026-10-19T07:38:49.4411780'),
        (2, N'9a24dade-ea07-4f85-aa55-c70d964a8024', N'Đơn FJ261020-002 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/4', 0, N'2026-10-20T15:19:55.1863390'),
        (3, N'0f21ecd9-e567-4aa2-9971-f1049ecec930', N'Đơn FJ261019-004 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/59', 0, N'2026-10-20T15:19:55.1864880');
    SET IDENTITY_INSERT [dbo].[Notifications] OFF;
    DBCC CHECKIDENT (N'[dbo].[Notifications]', RESEED, 3) WITH NO_INFOMSGS;

    -- OwnerRegistrations: 6 dòng.
    SET IDENTITY_INSERT [dbo].[OwnerRegistrations] ON;
    INSERT INTO [dbo].[OwnerRegistrations] ([Id], [OwnerId], [Source], [Status], [CreatedByEmployeeId], [ReviewedByEmployeeId], [ReviewNote], [SubmittedAt], [ReviewedAt], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-20T15:19:44.7891950', N'2026-10-20T15:19:44.7891950', N'2026-10-20T15:19:44.7895320', N'2026-10-20T15:19:44.7895320'),
        (2, 2, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-20T15:19:45.6427130', N'2026-10-20T15:19:45.6427130', N'2026-10-20T15:19:45.6427350', N'2026-10-20T15:19:45.6427350'),
        (3, 3, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-20T15:19:45.8932440', N'2026-10-20T15:19:45.8932440', N'2026-10-20T15:19:45.8932570', N'2026-10-20T15:19:45.8932570'),
        (4, 4, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-20T15:19:46.0735540', N'2026-10-20T15:19:46.0735540', N'2026-10-20T15:19:46.0735600', N'2026-10-20T15:19:46.0735600'),
        (5, 5, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-20T15:19:46.2457740', N'2026-10-20T15:19:46.2457740', N'2026-10-20T15:19:46.2457810', N'2026-10-20T15:19:46.2457810'),
        (6, 6, N'SelfService', N'PendingReview', NULL, NULL, NULL, N'2026-10-20T15:19:46.4085180', NULL, N'2026-10-20T15:19:46.4085200', N'2026-10-20T15:19:46.4085200');
    SET IDENTITY_INSERT [dbo].[OwnerRegistrations] OFF;
    DBCC CHECKIDENT (N'[dbo].[OwnerRegistrations]', RESEED, 6) WITH NO_INFOMSGS;

    -- Vouchers: 3 dòng.
    SET IDENTITY_INSERT [dbo].[Vouchers] ON;
    INSERT INTO [dbo].[Vouchers] ([Id], [Code], [CustomerId], [Amount], [Balance], [ExpiresOn], [Status], [Reason], [SourceBookingId], [IssuedByEmployeeId], [IssuedAt], [RevokedAt], [RevokeReason]) VALUES
        (1, N'VCDEMOCC2K', 10, 50000, 50000, N'2027-04-18', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 14, 2, N'2026-10-18T07:37:49.4411780', NULL, NULL),
        (2, N'VCDEMOAA2K', 13, 160000, 160000, N'2026-12-01', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 7, 2, N'2026-06-01T07:03:00', NULL, NULL),
        (3, N'VCDEMOBB2K', 15, 50000, 50000, N'2027-01-29', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 54, 2, N'2026-07-29T04:12:00', NULL, NULL);
    SET IDENTITY_INSERT [dbo].[Vouchers] OFF;
    DBCC CHECKIDENT (N'[dbo].[Vouchers]', RESEED, 3) WITH NO_INFOMSGS;

    -- CancellationPolicies: 6 dòng.
    SET IDENTITY_INSERT [dbo].[CancellationPolicies] ON;
    INSERT INTO [dbo].[CancellationPolicies] ([Id], [HomestayId], [Scope], [Name], [IsActive], [CreatedAt]) VALUES
        (1, NULL, N'All', N'Chính sách hủy chung của Fuji', 1, N'2026-10-20T15:19:43.0169360'),
        (2, 31, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-20T15:19:48.8407720'),
        (3, 32, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-20T15:19:48.8478040'),
        (4, 33, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-20T15:19:48.8490250'),
        (5, 34, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-20T15:19:48.8507580'),
        (6, 35, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-20T15:19:48.8519620');
    SET IDENTITY_INSERT [dbo].[CancellationPolicies] OFF;
    DBCC CHECKIDENT (N'[dbo].[CancellationPolicies]', RESEED, 6) WITH NO_INFOMSGS;

    -- Contracts: 35 dòng.
    SET IDENTITY_INSERT [dbo].[Contracts] ON;
    INSERT INTO [dbo].[Contracts] ([Id], [HomestayId], [ContractNo], [StartDate], [EndDate], [CommissionRate], [Status], [SignedByEmployeeId], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'HD-2026-0001', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.6002650', N'2026-10-20T15:19:48.6002670'),
        (2, 2, N'HD-2026-0002', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8022750', N'2026-10-20T15:19:48.8022750'),
        (3, 3, N'HD-2026-0003', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8045750', N'2026-10-20T15:19:48.8045750'),
        (4, 4, N'HD-2026-0004', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8065180', N'2026-10-20T15:19:48.8065180'),
        (5, 5, N'HD-2026-0005', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8078250', N'2026-10-20T15:19:48.8078250'),
        (6, 6, N'HD-2026-0006', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8089720', N'2026-10-20T15:19:48.8089720'),
        (7, 7, N'HD-2026-0007', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8097800', N'2026-10-20T15:19:48.8097800'),
        (8, 8, N'HD-2026-0008', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8106930', N'2026-10-20T15:19:48.8106930'),
        (9, 9, N'HD-2026-0009', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8114550', N'2026-10-20T15:19:48.8114550'),
        (10, 10, N'HD-2026-0010', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8125100', N'2026-10-20T15:19:48.8125100'),
        (11, 11, N'HD-2026-0011', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8133910', N'2026-10-20T15:19:48.8133910'),
        (12, 12, N'HD-2026-0012', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8146110', N'2026-10-20T15:19:48.8146110'),
        (13, 13, N'HD-2026-0013', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8164920', N'2026-10-20T15:19:48.8164920'),
        (14, 14, N'HD-2026-0014', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8176450', N'2026-10-20T15:19:48.8176450'),
        (15, 15, N'HD-2026-0015', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8186470', N'2026-10-20T15:19:48.8186470'),
        (16, 16, N'HD-2026-0016', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8201700', N'2026-10-20T15:19:48.8201700'),
        (17, 17, N'HD-2026-0017', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8211130', N'2026-10-20T15:19:48.8211130'),
        (18, 18, N'HD-2026-0018', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8222030', N'2026-10-20T15:19:48.8222030'),
        (19, 19, N'HD-2026-0019', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8241330', N'2026-10-20T15:19:48.8241330'),
        (20, 20, N'HD-2026-0020', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8261600', N'2026-10-20T15:19:48.8261600'),
        (21, 21, N'HD-2026-0021', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8277880', N'2026-10-20T15:19:48.8277880'),
        (22, 22, N'HD-2026-0022', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8290520', N'2026-10-20T15:19:48.8290520'),
        (23, 23, N'HD-2026-0023', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8299700', N'2026-10-20T15:19:48.8299700'),
        (24, 24, N'HD-2026-0024', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8310040', N'2026-10-20T15:19:48.8310040'),
        (25, 25, N'HD-2026-0025', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8317530', N'2026-10-20T15:19:48.8317530'),
        (26, 26, N'HD-2026-0026', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8325660', N'2026-10-20T15:19:48.8325660'),
        (27, 27, N'HD-2026-0027', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8342550', N'2026-10-20T15:19:48.8342550'),
        (28, 28, N'HD-2026-0028', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8354450', N'2026-10-20T15:19:48.8354450'),
        (29, 29, N'HD-2026-0029', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8366600', N'2026-10-20T15:19:48.8366600'),
        (30, 30, N'HD-2026-0030', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8388610', N'2026-10-20T15:19:48.8388610'),
        (31, 31, N'HD-2026-0031', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8405630', N'2026-10-20T15:19:48.8405630'),
        (32, 32, N'HD-2026-0032', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8477300', N'2026-10-20T15:19:48.8477300'),
        (33, 33, N'HD-2026-0033', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-20T15:19:48.8489860', N'2026-10-20T15:19:48.8489860'),
        (34, 34, N'HD-2026-0034', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-20T15:19:48.8506950', N'2026-10-20T15:19:48.8506950'),
        (35, 35, N'HD-2026-0035', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-20T15:19:48.8519310', N'2026-10-20T15:19:48.8519310');
    SET IDENTITY_INSERT [dbo].[Contracts] OFF;
    DBCC CHECKIDENT (N'[dbo].[Contracts]', RESEED, 35) WITH NO_INFOMSGS;

    -- HomestayAmenities: 183 dòng.
    INSERT INTO [dbo].[HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
        (1, 1),
        (1, 4),
        (1, 7),
        (1, 10),
        (2, 1),
        (2, 2),
        (2, 4),
        (2, 5),
        (2, 8),
        (3, 2),
        (3, 3),
        (3, 5),
        (3, 6),
        (3, 8),
        (3, 9),
        (4, 3),
        (4, 4),
        (4, 7),
        (4, 10),
        (5, 1),
        (5, 4),
        (5, 5),
        (5, 7),
        (5, 8),
        (6, 1),
        (6, 2),
        (6, 5),
        (6, 6),
        (6, 8),
        (6, 9),
        (7, 3),
        (7, 6),
        (7, 7),
        (7, 10),
        (8, 1),
        (8, 4),
        (8, 7),
        (8, 8),
        (8, 10),
        (9, 1),
        (9, 2),
        (9, 4),
        (9, 5),
        (9, 8),
        (9, 9),
        (10, 3),
        (10, 6),
        (10, 9),
        (10, 10),
        (11, 1),
        (11, 3),
        (11, 4),
        (11, 7),
        (11, 10),
        (12, 1),
        (12, 2),
        (12, 4),
        (12, 5),
        (12, 7),
        (12, 8),
        (13, 2),
        (13, 3),
        (13, 6),
        (13, 9),
        (14, 3),
        (14, 4),
        (14, 6),
        (14, 7),
        (14, 10),
        (15, 1),
        (15, 4),
        (15, 5),
        (15, 7),
        (15, 8),
        (15, 10),
        (16, 2),
        (16, 5),
        (16, 6),
        (16, 9),
        (17, 3),
        (17, 6),
        (17, 7),
        (17, 9),
        (17, 10),
        (18, 1),
        (18, 3),
        (18, 4),
        (18, 7),
        (18, 8),
        (18, 10),
        (19, 2),
        (19, 5),
        (19, 8),
        (19, 9),
        (20, 2),
        (20, 3),
        (20, 6),
        (20, 9),
        (20, 10),
        (21, 1);
    INSERT INTO [dbo].[HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
        (21, 3),
        (21, 4),
        (21, 6),
        (21, 7),
        (21, 10),
        (22, 1),
        (22, 2),
        (22, 5),
        (22, 8),
        (23, 2),
        (23, 3),
        (23, 5),
        (23, 6),
        (23, 9),
        (24, 3),
        (24, 4),
        (24, 6),
        (24, 7),
        (24, 9),
        (24, 10),
        (25, 1),
        (25, 4),
        (25, 5),
        (25, 8),
        (26, 2),
        (26, 4),
        (26, 5),
        (26, 6),
        (26, 8),
        (26, 9),
        (27, 2),
        (27, 3),
        (27, 4),
        (27, 5),
        (27, 6),
        (27, 7),
        (27, 9),
        (27, 10),
        (28, 1),
        (28, 4),
        (28, 5),
        (28, 7),
        (28, 8),
        (29, 1),
        (29, 2),
        (29, 4),
        (29, 5),
        (29, 8),
        (29, 9),
        (30, 2),
        (30, 3),
        (30, 4),
        (30, 5),
        (30, 6),
        (30, 9),
        (30, 10),
        (31, 1),
        (31, 4),
        (31, 5),
        (31, 7),
        (31, 10),
        (32, 1),
        (32, 2),
        (32, 4),
        (32, 5),
        (32, 8),
        (33, 2),
        (33, 3),
        (33, 4),
        (33, 5),
        (33, 6),
        (33, 8),
        (33, 9),
        (34, 3),
        (34, 4),
        (34, 5),
        (34, 7),
        (34, 10),
        (35, 1),
        (35, 4),
        (35, 5),
        (35, 7),
        (35, 8);

    -- RoomTypes: 93 dòng.
    SET IDENTITY_INSERT [dbo].[RoomTypes] ON;
    INSERT INTO [dbo].[RoomTypes] ([Id], [HomestayId], [CategoryId], [Name], [MaxGuests], [AreaM2], [Description], [IsActive], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.6065060', N'2026-10-20T15:19:48.6065060'),
        (2, 1, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.6076430', N'2026-10-20T15:19:48.6076430'),
        (3, 1, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.6076670', N'2026-10-20T15:19:48.6076670'),
        (4, 2, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8025870', N'2026-10-20T15:19:48.8025870'),
        (5, 2, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8025930', N'2026-10-20T15:19:48.8025930'),
        (6, 3, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8046140', N'2026-10-20T15:19:48.8046140'),
        (7, 3, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8046170', N'2026-10-20T15:19:48.8046170'),
        (8, 3, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8046180', N'2026-10-20T15:19:48.8046180'),
        (9, 4, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8065700', N'2026-10-20T15:19:48.8065700'),
        (10, 4, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8065730', N'2026-10-20T15:19:48.8065740'),
        (11, 5, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8078500', N'2026-10-20T15:19:48.8078500'),
        (12, 5, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8078690', N'2026-10-20T15:19:48.8078690'),
        (13, 5, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8078700', N'2026-10-20T15:19:48.8078700'),
        (14, 6, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8089870', N'2026-10-20T15:19:48.8089870'),
        (15, 6, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8089910', N'2026-10-20T15:19:48.8089910'),
        (16, 7, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8097880', N'2026-10-20T15:19:48.8097880'),
        (17, 7, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8097900', N'2026-10-20T15:19:48.8097900'),
        (18, 7, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8097910', N'2026-10-20T15:19:48.8097910'),
        (19, 8, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8107010', N'2026-10-20T15:19:48.8107010'),
        (20, 8, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8107080', N'2026-10-20T15:19:48.8107080'),
        (21, 9, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8114620', N'2026-10-20T15:19:48.8114620'),
        (22, 9, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8114640', N'2026-10-20T15:19:48.8114640'),
        (23, 9, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8114720', N'2026-10-20T15:19:48.8114720'),
        (24, 10, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8125170', N'2026-10-20T15:19:48.8125170'),
        (25, 10, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8125190', N'2026-10-20T15:19:48.8125190'),
        (26, 11, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8134000', N'2026-10-20T15:19:48.8134000'),
        (27, 11, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8134020', N'2026-10-20T15:19:48.8134020'),
        (28, 11, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8134090', N'2026-10-20T15:19:48.8134090'),
        (29, 12, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8146530', N'2026-10-20T15:19:48.8146530'),
        (30, 12, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8146940', N'2026-10-20T15:19:48.8146940'),
        (31, 13, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8165210', N'2026-10-20T15:19:48.8165210'),
        (32, 13, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8165240', N'2026-10-20T15:19:48.8165240'),
        (33, 13, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8165250', N'2026-10-20T15:19:48.8165250'),
        (34, 14, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8176640', N'2026-10-20T15:19:48.8176640'),
        (35, 14, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8176670', N'2026-10-20T15:19:48.8176670'),
        (36, 15, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8186690', N'2026-10-20T15:19:48.8186690'),
        (37, 15, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8186720', N'2026-10-20T15:19:48.8186720'),
        (38, 15, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8186730', N'2026-10-20T15:19:48.8186730'),
        (39, 16, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8202270', N'2026-10-20T15:19:48.8202270'),
        (40, 16, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8202310', N'2026-10-20T15:19:48.8202310'),
        (41, 17, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8211390', N'2026-10-20T15:19:48.8211390'),
        (42, 17, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8211420', N'2026-10-20T15:19:48.8211420'),
        (43, 17, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8211430', N'2026-10-20T15:19:48.8211430'),
        (44, 18, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8222180', N'2026-10-20T15:19:48.8222180'),
        (45, 18, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8222200', N'2026-10-20T15:19:48.8222210'),
        (46, 19, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8241640', N'2026-10-20T15:19:48.8241640'),
        (47, 19, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8242030', N'2026-10-20T15:19:48.8242030'),
        (48, 19, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8242050', N'2026-10-20T15:19:48.8242050'),
        (49, 20, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8262400', N'2026-10-20T15:19:48.8262400'),
        (50, 20, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8262450', N'2026-10-20T15:19:48.8262450'),
        (51, 21, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8278230', N'2026-10-20T15:19:48.8278230'),
        (52, 21, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8278270', N'2026-10-20T15:19:48.8278270'),
        (53, 21, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8278380', N'2026-10-20T15:19:48.8278380'),
        (54, 22, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8290720', N'2026-10-20T15:19:48.8290720'),
        (55, 22, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8290750', N'2026-10-20T15:19:48.8290750'),
        (56, 23, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8299850', N'2026-10-20T15:19:48.8299850'),
        (57, 23, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8299930', N'2026-10-20T15:19:48.8299930'),
        (58, 23, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8299940', N'2026-10-20T15:19:48.8299940'),
        (59, 24, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8310190', N'2026-10-20T15:19:48.8310190'),
        (60, 24, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8310210', N'2026-10-20T15:19:48.8310210'),
        (61, 25, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-20T15:19:48.8317620', N'2026-10-20T15:19:48.8317620'),
        (62, 25, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-20T15:19:48.8317640', N'2026-10-20T15:19:48.8317640'),
        (63, 25, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-20T15:19:48.8317650', N'2026-10-20T15:19:48.8317650'),
        (64, 26, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8329820', N'2026-10-20T15:19:48.8329820'),
        (65, 26, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8329920', N'2026-10-20T15:19:48.8329920'),
        (66, 26, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8329940', N'2026-10-20T15:19:48.8329940'),
        (67, 27, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8343040', N'2026-10-20T15:19:48.8343040'),
        (68, 27, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8343070', N'2026-10-20T15:19:48.8343070'),
        (69, 27, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8343160', N'2026-10-20T15:19:48.8343160'),
        (70, 28, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8354970', N'2026-10-20T15:19:48.8354970'),
        (71, 28, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8355000', N'2026-10-20T15:19:48.8355000'),
        (72, 28, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8355010', N'2026-10-20T15:19:48.8355010'),
        (73, 29, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8366880', N'2026-10-20T15:19:48.8366880'),
        (74, 29, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8366910', N'2026-10-20T15:19:48.8366910'),
        (75, 29, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8366920', N'2026-10-20T15:19:48.8366920'),
        (76, 30, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8389050', N'2026-10-20T15:19:48.8389050'),
        (77, 30, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8389080', N'2026-10-20T15:19:48.8389080'),
        (78, 30, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8389090', N'2026-10-20T15:19:48.8389090'),
        (79, 31, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8406020', N'2026-10-20T15:19:48.8406030'),
        (80, 31, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8406060', N'2026-10-20T15:19:48.8406060'),
        (81, 31, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8406070', N'2026-10-20T15:19:48.8406070'),
        (82, 32, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8477620', N'2026-10-20T15:19:48.8477620'),
        (83, 32, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8478020', N'2026-10-20T15:19:48.8478020'),
        (84, 32, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8478020', N'2026-10-20T15:19:48.8478030'),
        (85, 33, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8490200', N'2026-10-20T15:19:48.8490200'),
        (86, 33, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8490230', N'2026-10-20T15:19:48.8490230'),
        (87, 33, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8490240', N'2026-10-20T15:19:48.8490240'),
        (88, 34, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8507410', N'2026-10-20T15:19:48.8507410'),
        (89, 34, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8507450', N'2026-10-20T15:19:48.8507450'),
        (90, 34, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8507460', N'2026-10-20T15:19:48.8507460'),
        (91, 35, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-20T15:19:48.8519580', N'2026-10-20T15:19:48.8519580'),
        (92, 35, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-20T15:19:48.8519600', N'2026-10-20T15:19:48.8519610'),
        (93, 35, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-20T15:19:48.8519620', N'2026-10-20T15:19:48.8519620');
    SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomTypes]', RESEED, 93) WITH NO_INFOMSGS;

    -- Settlements: 31 dòng.
    SET IDENTITY_INSERT [dbo].[Settlements] ON;
    INSERT INTO [dbo].[Settlements] ([Id], [HomestayId], [Year], [Month], [PeriodNo], [TotalRoomAmount], [TotalRetained], [TotalCommission], [PayoutAmount], [Status], [PaidAt], [PaidByEmployeeId], [CreatedAt], [UpdatedAt], [ConfirmedAt], [PaidBankRef], [SentAt]) VALUES
        (1, 1, 2026, 6, 1, 1920000, 576000, 192000, 384000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615001', N'2026-06-17T02:00:00'),
        (2, 12, 2026, 6, 1, 2560000, 768000, 384000, 384000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615012', N'2026-06-17T02:00:00'),
        (3, 25, 2026, 6, 1, 560000, 168000, 56000, 112000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615025', N'2026-06-17T02:00:00'),
        (4, 31, 2026, 6, 1, 2040000, 306000, 204000, 102000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615031', N'2026-06-17T02:00:00'),
        (5, 8, 2026, 6, 2, 480000, 144000, 57600, 86400, N'Paid', N'2026-07-05T08:00:00', 3, N'2026-07-01T01:00:00', N'2026-07-05T08:00:00', N'2026-07-03T03:00:00', N'FT260630008', N'2026-07-02T02:00:00'),
        (6, 6, 2026, 7, 1, 2880000, 864000, 432000, 432000, N'Paid', N'2026-07-20T08:00:00', 3, N'2026-07-16T01:00:00', N'2026-07-20T08:00:00', N'2026-07-18T03:00:00', N'FT260715006', N'2026-07-17T02:00:00'),
        (7, 32, 2026, 7, 1, 700000, 210000, 84000, 126000, N'Paid', N'2026-07-20T08:00:00', 3, N'2026-07-16T01:00:00', N'2026-07-20T08:00:00', N'2026-07-18T03:00:00', N'FT260715032', N'2026-07-17T02:00:00'),
        (8, 33, 2026, 7, 1, 4100000, 1230000, 615000, 615000, N'Paid', N'2026-07-20T08:00:00', 3, N'2026-07-16T01:00:00', N'2026-07-20T08:00:00', N'2026-07-18T03:00:00', N'FT260715033', N'2026-07-17T02:00:00'),
        (9, 34, 2026, 7, 1, 1800000, 540000, 180000, 360000, N'Paid', N'2026-07-20T08:00:00', 3, N'2026-07-16T01:00:00', N'2026-07-20T08:00:00', N'2026-07-18T03:00:00', N'FT260715034', N'2026-07-17T02:00:00'),
        (10, 8, 2026, 7, 2, 770000, 231000, 92400, 138600, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731008', N'2026-08-02T02:00:00'),
        (11, 20, 2026, 7, 2, 4320000, 1296000, 518400, 777600, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731020', N'2026-08-02T02:00:00'),
        (12, 22, 2026, 7, 2, 2050000, 615000, 205000, 410000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731022', N'2026-08-02T02:00:00'),
        (13, 25, 2026, 7, 2, 2240000, 672000, 224000, 448000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731025', N'2026-08-02T02:00:00'),
        (14, 32, 2026, 7, 2, 6720000, 2016000, 806400, 1209600, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731032', N'2026-08-02T02:00:00'),
        (15, 4, 2026, 8, 1, 3800000, 1140000, 380000, 760000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815004', N'2026-08-17T02:00:00'),
        (16, 5, 2026, 8, 1, 3840000, 1152000, 460800, 691200, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815005', N'2026-08-17T02:00:00'),
        (17, 6, 2026, 8, 1, 1440000, 432000, 216000, 216000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815006', N'2026-08-17T02:00:00'),
        (18, 7, 2026, 8, 1, 1600000, 480000, 160000, 320000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815007', N'2026-08-17T02:00:00'),
        (19, 5, 2026, 9, 1, 3840000, 1152000, 460800, 691200, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915005', N'2026-09-17T02:00:00'),
        (20, 7, 2026, 9, 1, 5120000, 1536000, 512000, 1024000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915007', N'2026-09-17T02:00:00'),
        (21, 12, 2026, 9, 1, 1280000, 384000, 192000, 192000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915012', N'2026-09-17T02:00:00'),
        (22, 14, 2026, 9, 1, 1600000, 480000, 192000, 288000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915014', N'2026-09-17T02:00:00'),
        (23, 18, 2026, 9, 1, 2380000, 714000, 357000, 357000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915018', N'2026-09-17T02:00:00'),
        (24, 33, 2026, 9, 1, 8000000, 2400000, 1200000, 1200000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915033', N'2026-09-17T02:00:00'),
        (25, 4, 2026, 9, 2, 700000, 210000, 70000, 140000, N'Paid', N'2026-10-05T08:00:00', 3, N'2026-10-01T01:00:00', N'2026-10-05T08:00:00', N'2026-10-03T03:00:00', N'FT260930004', N'2026-10-02T02:00:00'),
        (26, 6, 2026, 9, 2, 1440000, 432000, 216000, 216000, N'Paid', N'2026-10-05T08:00:00', 3, N'2026-10-01T01:00:00', N'2026-10-05T08:00:00', N'2026-10-03T03:00:00', N'FT260930006', N'2026-10-02T02:00:00'),
        (27, 15, 2026, 9, 2, 1920000, 576000, 288000, 288000, N'Paid', N'2026-10-05T08:00:00', 3, N'2026-10-01T01:00:00', N'2026-10-05T08:00:00', N'2026-10-03T03:00:00', N'FT260930015', N'2026-10-02T02:00:00'),
        (28, 30, 2026, 9, 2, 1700000, 510000, 255000, 255000, N'Paid', N'2026-10-05T08:00:00', 3, N'2026-10-01T01:00:00', N'2026-10-05T08:00:00', N'2026-10-03T03:00:00', N'FT260930030', N'2026-10-02T02:00:00'),
        (29, 2, 2026, 10, 1, 1700000, 510000, 204000, 306000, N'Draft', NULL, NULL, N'2026-10-16T01:00:00', N'2026-10-16T01:00:00', NULL, NULL, NULL),
        (30, 15, 2026, 10, 1, 960000, 288000, 144000, 144000, N'Sent', NULL, NULL, N'2026-10-16T01:00:00', N'2026-10-17T02:00:00', NULL, NULL, N'2026-10-17T02:00:00'),
        (31, 20, 2026, 10, 1, 1730000, 519000, 207600, 311400, N'Confirmed', NULL, NULL, N'2026-10-16T01:00:00', N'2026-10-18T03:00:00', N'2026-10-18T03:00:00', NULL, N'2026-10-17T02:00:00');
    SET IDENTITY_INSERT [dbo].[Settlements] OFF;
    DBCC CHECKIDENT (N'[dbo].[Settlements]', RESEED, 31) WITH NO_INFOMSGS;

    -- VerificationDocuments: 114 dòng.
    SET IDENTITY_INSERT [dbo].[VerificationDocuments] ON;
    INSERT INTO [dbo].[VerificationDocuments] ([Id], [OwnerRegistrationId], [HomestayId], [DocumentType], [FileUrl], [OriginalFileName], [ExpiryDate], [CreatedAt]) VALUES
        (1, NULL, 1, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-01.png', N'2029-06-01', N'2026-10-20T15:19:48.5995550'),
        (2, NULL, 1, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-01.png', N'2026-11-09', N'2026-10-20T15:19:48.5995550'),
        (3, NULL, 1, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-01.png', NULL, N'2026-10-20T15:19:48.5995550'),
        (4, NULL, 2, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-02.png', N'2029-06-01', N'2026-10-20T15:19:48.8022680'),
        (5, NULL, 2, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-02.png', N'2028-06-01', N'2026-10-20T15:19:48.8022680'),
        (6, NULL, 2, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-02.png', NULL, N'2026-10-20T15:19:48.8022680'),
        (7, NULL, 3, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-03.png', N'2029-06-01', N'2026-10-20T15:19:48.8045570'),
        (8, NULL, 3, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-03.png', N'2028-06-01', N'2026-10-20T15:19:48.8045570'),
        (9, NULL, 3, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-03.png', NULL, N'2026-10-20T15:19:48.8045570'),
        (10, NULL, 4, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-04.png', N'2029-06-01', N'2026-10-20T15:19:48.8065130'),
        (11, NULL, 4, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-04.png', N'2028-06-01', N'2026-10-20T15:19:48.8065130'),
        (12, NULL, 4, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-04.png', NULL, N'2026-10-20T15:19:48.8065130'),
        (13, NULL, 5, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-05.png', N'2029-06-01', N'2026-10-20T15:19:48.8078210'),
        (14, NULL, 5, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-05.png', N'2028-06-01', N'2026-10-20T15:19:48.8078210'),
        (15, NULL, 5, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-05.png', NULL, N'2026-10-20T15:19:48.8078210'),
        (16, NULL, 6, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-06.png', N'2029-06-01', N'2026-10-20T15:19:48.8089690'),
        (17, NULL, 6, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-06.png', N'2028-06-01', N'2026-10-20T15:19:48.8089690'),
        (18, NULL, 6, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-06.png', NULL, N'2026-10-20T15:19:48.8089690'),
        (19, NULL, 7, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-07.png', N'2029-06-01', N'2026-10-20T15:19:48.8097740'),
        (20, NULL, 7, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-07.png', N'2028-06-01', N'2026-10-20T15:19:48.8097740'),
        (21, NULL, 7, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-07.png', NULL, N'2026-10-20T15:19:48.8097740'),
        (22, NULL, 8, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-08.png', N'2029-06-01', N'2026-10-20T15:19:48.8106910'),
        (23, NULL, 8, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-08.png', N'2028-06-01', N'2026-10-20T15:19:48.8106910'),
        (24, NULL, 8, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-08.png', NULL, N'2026-10-20T15:19:48.8106910'),
        (25, NULL, 9, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-09.png', N'2029-06-01', N'2026-10-20T15:19:48.8114530'),
        (26, NULL, 9, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-09.png', N'2028-06-01', N'2026-10-20T15:19:48.8114530'),
        (27, NULL, 9, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-09.png', NULL, N'2026-10-20T15:19:48.8114530'),
        (28, NULL, 10, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-10.png', N'2029-06-01', N'2026-10-20T15:19:48.8125080'),
        (29, NULL, 10, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-10.png', N'2028-06-01', N'2026-10-20T15:19:48.8125080'),
        (30, NULL, 10, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-10.png', NULL, N'2026-10-20T15:19:48.8125080'),
        (31, NULL, 11, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-11.png', N'2029-06-01', N'2026-10-20T15:19:48.8133890'),
        (32, NULL, 11, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-11.png', N'2028-06-01', N'2026-10-20T15:19:48.8133890'),
        (33, NULL, 11, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-11.png', NULL, N'2026-10-20T15:19:48.8133890'),
        (34, NULL, 12, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-12.png', N'2029-06-01', N'2026-10-20T15:19:48.8146080'),
        (35, NULL, 12, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-12.png', N'2028-06-01', N'2026-10-20T15:19:48.8146080'),
        (36, NULL, 12, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-12.png', NULL, N'2026-10-20T15:19:48.8146080'),
        (37, NULL, 13, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-13.png', N'2029-06-01', N'2026-10-20T15:19:48.8164810'),
        (38, NULL, 13, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-13.png', N'2028-06-01', N'2026-10-20T15:19:48.8164810'),
        (39, NULL, 13, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-13.png', NULL, N'2026-10-20T15:19:48.8164810'),
        (40, NULL, 14, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-14.png', N'2029-06-01', N'2026-10-20T15:19:48.8176420'),
        (41, NULL, 14, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-14.png', N'2028-06-01', N'2026-10-20T15:19:48.8176420'),
        (42, NULL, 14, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-14.png', NULL, N'2026-10-20T15:19:48.8176420'),
        (43, NULL, 15, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-15.png', N'2029-06-01', N'2026-10-20T15:19:48.8186430'),
        (44, NULL, 15, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-15.png', N'2028-06-01', N'2026-10-20T15:19:48.8186430'),
        (45, NULL, 15, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-15.png', NULL, N'2026-10-20T15:19:48.8186430'),
        (46, NULL, 16, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-16.png', N'2029-06-01', N'2026-10-20T15:19:48.8201640'),
        (47, NULL, 16, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-16.png', N'2028-06-01', N'2026-10-20T15:19:48.8201640'),
        (48, NULL, 16, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-16.png', NULL, N'2026-10-20T15:19:48.8201640'),
        (49, NULL, 17, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-17.png', N'2029-06-01', N'2026-10-20T15:19:48.8211090'),
        (50, NULL, 17, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-17.png', N'2028-06-01', N'2026-10-20T15:19:48.8211090'),
        (51, NULL, 17, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-17.png', NULL, N'2026-10-20T15:19:48.8211090'),
        (52, NULL, 18, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-18.png', N'2029-06-01', N'2026-10-20T15:19:48.8222010'),
        (53, NULL, 18, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-18.png', N'2028-06-01', N'2026-10-20T15:19:48.8222010'),
        (54, NULL, 18, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-18.png', NULL, N'2026-10-20T15:19:48.8222010'),
        (55, NULL, 19, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-19.png', N'2029-06-01', N'2026-10-20T15:19:48.8241280'),
        (56, NULL, 19, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-19.png', N'2028-06-01', N'2026-10-20T15:19:48.8241280'),
        (57, NULL, 19, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-19.png', NULL, N'2026-10-20T15:19:48.8241280'),
        (58, NULL, 20, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-20.png', N'2029-06-01', N'2026-10-20T15:19:48.8261550'),
        (59, NULL, 20, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-20.png', N'2028-06-01', N'2026-10-20T15:19:48.8261550'),
        (60, NULL, 20, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-20.png', NULL, N'2026-10-20T15:19:48.8261550'),
        (61, NULL, 21, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-21.png', N'2029-06-01', N'2026-10-20T15:19:48.8277820'),
        (62, NULL, 21, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-21.png', N'2028-06-01', N'2026-10-20T15:19:48.8277820'),
        (63, NULL, 21, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-21.png', NULL, N'2026-10-20T15:19:48.8277820'),
        (64, NULL, 22, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-22.png', N'2029-06-01', N'2026-10-20T15:19:48.8290420'),
        (65, NULL, 22, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-22.png', N'2028-06-01', N'2026-10-20T15:19:48.8290420'),
        (66, NULL, 22, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-22.png', NULL, N'2026-10-20T15:19:48.8290420'),
        (67, NULL, 23, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-23.png', N'2029-06-01', N'2026-10-20T15:19:48.8299680'),
        (68, NULL, 23, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-23.png', N'2028-06-01', N'2026-10-20T15:19:48.8299680'),
        (69, NULL, 23, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-23.png', NULL, N'2026-10-20T15:19:48.8299680'),
        (70, NULL, 24, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-24.png', N'2029-06-01', N'2026-10-20T15:19:48.8310020'),
        (71, NULL, 24, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-24.png', N'2028-06-01', N'2026-10-20T15:19:48.8310020'),
        (72, NULL, 24, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-24.png', NULL, N'2026-10-20T15:19:48.8310020'),
        (73, NULL, 25, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-25.png', N'2029-06-01', N'2026-10-20T15:19:48.8317510'),
        (74, NULL, 25, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-25.png', N'2028-06-01', N'2026-10-20T15:19:48.8317510'),
        (75, NULL, 25, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-25.png', NULL, N'2026-10-20T15:19:48.8317510'),
        (76, NULL, 26, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-26.png', N'2029-06-01', N'2026-10-20T15:19:48.8325650'),
        (77, NULL, 26, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-26.png', N'2028-06-01', N'2026-10-20T15:19:48.8325650'),
        (78, NULL, 26, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-26.png', NULL, N'2026-10-20T15:19:48.8325650'),
        (79, NULL, 27, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-27.png', N'2029-06-01', N'2026-10-20T15:19:48.8342520'),
        (80, NULL, 27, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-27.png', N'2028-06-01', N'2026-10-20T15:19:48.8342520'),
        (81, NULL, 27, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-27.png', NULL, N'2026-10-20T15:19:48.8342520'),
        (82, NULL, 28, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-28.png', N'2029-06-01', N'2026-10-20T15:19:48.8353810'),
        (83, NULL, 28, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-28.png', N'2028-06-01', N'2026-10-20T15:19:48.8353810'),
        (84, NULL, 28, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-28.png', NULL, N'2026-10-20T15:19:48.8353810'),
        (85, NULL, 29, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-29.png', N'2029-06-01', N'2026-10-20T15:19:48.8366570'),
        (86, NULL, 29, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-29.png', N'2028-06-01', N'2026-10-20T15:19:48.8366570'),
        (87, NULL, 29, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-29.png', NULL, N'2026-10-20T15:19:48.8366570'),
        (88, NULL, 30, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-30.png', N'2029-06-01', N'2026-10-20T15:19:48.8388570'),
        (89, NULL, 30, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-30.png', N'2028-06-01', N'2026-10-20T15:19:48.8388570'),
        (90, NULL, 30, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-30.png', NULL, N'2026-10-20T15:19:48.8388570'),
        (91, NULL, 31, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-31.png', N'2029-06-01', N'2026-10-20T15:19:48.8405590'),
        (92, NULL, 31, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-31.png', N'2028-06-01', N'2026-10-20T15:19:48.8405590'),
        (93, NULL, 31, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-31.png', NULL, N'2026-10-20T15:19:48.8405590'),
        (94, NULL, 32, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-32.png', N'2029-06-01', N'2026-10-20T15:19:48.8477260'),
        (95, NULL, 32, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-32.png', N'2028-06-01', N'2026-10-20T15:19:48.8477260'),
        (96, NULL, 32, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-32.png', NULL, N'2026-10-20T15:19:48.8477260'),
        (97, NULL, 33, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-33.png', N'2029-06-01', N'2026-10-20T15:19:48.8489830'),
        (98, NULL, 33, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-33.png', N'2028-06-01', N'2026-10-20T15:19:48.8489830'),
        (99, NULL, 33, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-33.png', NULL, N'2026-10-20T15:19:48.8489830'),
        (100, NULL, 34, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-34.png', N'2029-06-01', N'2026-10-20T15:19:48.8506900');
    INSERT INTO [dbo].[VerificationDocuments] ([Id], [OwnerRegistrationId], [HomestayId], [DocumentType], [FileUrl], [OriginalFileName], [ExpiryDate], [CreatedAt]) VALUES
        (101, NULL, 34, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-34.png', N'2028-06-01', N'2026-10-20T15:19:48.8506900'),
        (102, NULL, 34, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-34.png', NULL, N'2026-10-20T15:19:48.8506900'),
        (103, NULL, 35, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-35.png', N'2029-06-01', N'2026-10-20T15:19:48.8519270'),
        (104, NULL, 35, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-35.png', N'2028-06-01', N'2026-10-20T15:19:48.8519270'),
        (105, NULL, 35, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-35.png', NULL, N'2026-10-20T15:19:48.8519270'),
        (106, 1, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:44.7891950'),
        (107, 2, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:45.6427130'),
        (108, 3, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:45.8932440'),
        (109, 4, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:46.0735540'),
        (110, 5, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:46.2457740'),
        (111, 6, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-20T15:19:46.4085180'),
        (112, 6, NULL, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh.png', N'2028-10-20', N'2026-10-20T15:19:46.4085180'),
        (113, 6, NULL, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc.png', N'2028-10-20', N'2026-10-20T15:19:46.4085180'),
        (114, 6, NULL, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mat-tien.png', NULL, N'2026-10-20T15:19:46.4085180');
    SET IDENTITY_INSERT [dbo].[VerificationDocuments] OFF;
    DBCC CHECKIDENT (N'[dbo].[VerificationDocuments]', RESEED, 114) WITH NO_INFOMSGS;

    -- CancellationRules: 18 dòng.
    SET IDENTITY_INSERT [dbo].[CancellationRules] ON;
    INSERT INTO [dbo].[CancellationRules] ([Id], [PolicyId], [MinDaysBefore], [RefundPercent]) VALUES
        (1, 1, 7, 100),
        (2, 1, 3, 50),
        (3, 1, 0, 0),
        (4, 2, 14, 100),
        (5, 2, 7, 50),
        (6, 2, 0, 0),
        (7, 3, 14, 100),
        (8, 3, 7, 50),
        (9, 3, 0, 0),
        (10, 4, 14, 100),
        (11, 4, 7, 50),
        (12, 4, 0, 0),
        (13, 5, 14, 100),
        (14, 5, 7, 50),
        (15, 5, 0, 0),
        (16, 6, 14, 100),
        (17, 6, 7, 50),
        (18, 6, 0, 0);
    SET IDENTITY_INSERT [dbo].[CancellationRules] OFF;
    DBCC CHECKIDENT (N'[dbo].[CancellationRules]', RESEED, 18) WITH NO_INFOMSGS;

    -- HomestayImages: 169 dòng.
    SET IDENTITY_INSERT [dbo].[HomestayImages] ON;
    INSERT INTO [dbo].[HomestayImages] ([Id], [HomestayId], [RoomTypeId], [Url], [SortOrder], [IsCover]) VALUES
        (1, 1, NULL, N'/images/demo/veranda-garden.webp', 0, 1),
        (2, 1, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (3, 1, NULL, N'/images/demo/room-city.webp', 2, 0),
        (4, 1, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (5, 1, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (6, 2, NULL, N'/images/demo/garden-door.webp', 0, 1),
        (7, 2, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (8, 2, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (9, 2, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (10, 2, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (11, 3, NULL, N'/images/demo/bright-living.webp', 0, 1),
        (12, 3, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (13, 3, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (14, 3, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (15, 3, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (16, 4, NULL, N'/images/demo/living-plants.webp', 0, 1),
        (17, 4, NULL, N'/images/demo/room-city.webp', 1, 0),
        (18, 4, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (19, 4, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (20, 4, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (21, 5, NULL, N'/images/demo/wooden-dining.webp', 0, 1),
        (22, 5, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (23, 5, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (24, 5, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (25, 5, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (26, 6, NULL, N'/images/demo/loft-living.webp', 0, 1),
        (27, 6, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (28, 6, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (29, 6, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (30, 6, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (31, 7, NULL, N'/images/demo/window-nook.webp', 0, 1),
        (32, 7, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (33, 7, NULL, N'/images/demo/room-city.webp', 2, 0),
        (34, 7, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (35, 7, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (36, 8, NULL, N'/images/demo/sofa-corner.webp', 0, 1),
        (37, 8, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (38, 8, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (39, 8, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (40, 8, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (41, 9, NULL, N'/images/demo/breakfast-table.webp', 0, 1),
        (42, 9, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (43, 9, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (44, 9, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (45, 9, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (46, 10, NULL, N'/images/demo/room-warm-wood.webp', 0, 1),
        (47, 10, NULL, N'/images/demo/room-city.webp', 1, 0),
        (48, 10, NULL, N'/images/demo/sofa-corner.webp', 2, 0),
        (49, 10, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (50, 11, NULL, N'/images/demo/room-studio.webp', 0, 1),
        (51, 11, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (52, 11, NULL, N'/images/demo/breakfast-table.webp', 2, 0),
        (53, 11, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (54, 12, NULL, N'/images/demo/room-classic.webp', 0, 1),
        (55, 12, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (56, 12, NULL, N'/images/demo/living-plants.webp', 2, 0),
        (57, 12, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (58, 13, NULL, N'/images/demo/room-city.webp', 0, 1),
        (59, 13, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (60, 13, NULL, N'/images/demo/window-nook.webp', 2, 0),
        (61, 13, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (62, 14, NULL, N'/images/demo/room-timber.webp', 0, 1),
        (63, 14, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (64, 14, NULL, N'/images/demo/wooden-dining.webp', 2, 0),
        (65, 14, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (66, 15, NULL, N'/images/demo/room-desk.webp', 0, 1),
        (67, 15, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (68, 15, NULL, N'/images/demo/sofa-corner.webp', 2, 0),
        (69, 15, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (70, 16, NULL, N'/images/demo/room-bright.webp', 0, 1),
        (71, 16, NULL, N'/images/demo/room-city.webp', 1, 0),
        (72, 16, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (73, 16, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (74, 16, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (75, 17, NULL, N'/images/demo/room-rose.webp', 0, 1),
        (76, 17, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (77, 17, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (78, 17, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (79, 17, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (80, 18, NULL, N'/images/demo/room-navy.webp', 0, 1),
        (81, 18, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (82, 18, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (83, 18, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (84, 18, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (85, 19, NULL, N'/images/demo/room-heritage.webp', 0, 1),
        (86, 19, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (87, 19, NULL, N'/images/demo/room-city.webp', 2, 0),
        (88, 19, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (89, 19, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (90, 20, NULL, N'/images/demo/living-white.webp', 0, 1),
        (91, 20, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (92, 20, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (93, 20, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (94, 20, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (95, 21, NULL, N'/images/demo/dining-tulips.webp', 0, 1),
        (96, 21, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (97, 21, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (98, 21, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (99, 21, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (100, 22, NULL, N'/images/demo/dining-window.webp', 0, 1);
    INSERT INTO [dbo].[HomestayImages] ([Id], [HomestayId], [RoomTypeId], [Url], [SortOrder], [IsCover]) VALUES
        (101, 22, NULL, N'/images/demo/room-city.webp', 1, 0),
        (102, 22, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (103, 22, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (104, 22, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (105, 23, NULL, N'/images/demo/dining-sunlit.webp', 0, 1),
        (106, 23, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (107, 23, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (108, 23, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (109, 23, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (110, 24, NULL, N'/images/demo/living-cane.webp', 0, 1),
        (111, 24, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (112, 24, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (113, 24, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (114, 24, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (115, 25, NULL, N'/images/demo/living-lounge.webp', 0, 1),
        (116, 25, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (117, 25, NULL, N'/images/demo/room-city.webp', 2, 0),
        (118, 25, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (119, 25, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (120, 26, NULL, N'/images/demo/valley-village.webp', 0, 1),
        (121, 26, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (122, 26, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (123, 26, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (124, 26, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (125, 27, NULL, N'/images/demo/garden-pool.webp', 0, 1),
        (126, 27, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (127, 27, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (128, 27, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (129, 27, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (130, 28, NULL, N'/images/demo/tea-hills.webp', 0, 1),
        (131, 28, NULL, N'/images/demo/room-city.webp', 1, 0),
        (132, 28, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (133, 28, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (134, 28, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (135, 29, NULL, N'/images/demo/rice-terraces.webp', 0, 1),
        (136, 29, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (137, 29, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (138, 29, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (139, 29, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (140, 30, NULL, N'/images/demo/terraces-golden.webp', 0, 1),
        (141, 30, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (142, 30, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (143, 30, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (144, 30, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (145, 31, NULL, N'/images/demo/green-hills.webp', 0, 1),
        (146, 31, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (147, 31, NULL, N'/images/demo/room-city.webp', 2, 0),
        (148, 31, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (149, 31, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (150, 32, NULL, N'/images/demo/terraces-green.webp', 0, 1),
        (151, 32, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (152, 32, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (153, 32, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (154, 32, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (155, 33, NULL, N'/images/demo/misty-hills.webp', 0, 1),
        (156, 33, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (157, 33, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (158, 33, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (159, 33, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (160, 34, NULL, N'/images/demo/mountain-sunrise.webp', 0, 1),
        (161, 34, NULL, N'/images/demo/room-city.webp', 1, 0),
        (162, 34, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (163, 34, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (164, 34, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (165, 35, NULL, N'/images/demo/sapa-terraces.webp', 0, 1),
        (166, 35, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (167, 35, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (168, 35, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (169, 35, NULL, N'/images/demo/bathroom-white.webp', 4, 0);
    SET IDENTITY_INSERT [dbo].[HomestayImages] OFF;
    DBCC CHECKIDENT (N'[dbo].[HomestayImages]', RESEED, 169) WITH NO_INFOMSGS;

    -- RoomTypePrices: 279 dòng.
    SET IDENTITY_INSERT [dbo].[RoomTypePrices] ON;
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (1, 1, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.6070900'),
        (2, 1, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.6074190'),
        (3, 1, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.6074300'),
        (4, 2, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.6076450'),
        (5, 2, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.6076460'),
        (6, 2, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.6076460'),
        (7, 3, N'Weekday', 320000, N'2026-01-01', N'2026-10-20T15:19:48.6076680'),
        (8, 3, N'Weekend', 380000, N'2026-01-01', N'2026-10-20T15:19:48.6076680'),
        (9, 3, N'Holiday', 480000, N'2026-01-01', N'2026-10-20T15:19:48.6076680'),
        (10, 4, N'Weekday', 500000, N'2026-01-01', N'2026-10-20T15:19:48.8025890'),
        (11, 4, N'Weekend', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8025910'),
        (12, 4, N'Holiday', 750000, N'2026-01-01', N'2026-10-20T15:19:48.8025920'),
        (13, 5, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8026080'),
        (14, 5, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8026100'),
        (15, 5, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8026100'),
        (16, 6, N'Weekday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8046160'),
        (17, 6, N'Weekend', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8046160'),
        (18, 6, N'Holiday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8046170'),
        (19, 7, N'Weekday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8046180'),
        (20, 7, N'Weekend', 1150000, N'2026-01-01', N'2026-10-20T15:19:48.8046180'),
        (21, 7, N'Holiday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8046180'),
        (22, 8, N'Weekday', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8046190'),
        (23, 8, N'Weekend', 580000, N'2026-01-01', N'2026-10-20T15:19:48.8046190'),
        (24, 8, N'Holiday', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8046190'),
        (25, 9, N'Weekday', 700000, N'2026-01-01', N'2026-10-20T15:19:48.8065710'),
        (26, 9, N'Weekend', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8065720'),
        (27, 9, N'Holiday', 1050000, N'2026-01-01', N'2026-10-20T15:19:48.8065720'),
        (28, 10, N'Weekday', 1120000, N'2026-01-01', N'2026-10-20T15:19:48.8065880'),
        (29, 10, N'Weekend', 1340000, N'2026-01-01', N'2026-10-20T15:19:48.8065880'),
        (30, 10, N'Holiday', 1680000, N'2026-01-01', N'2026-10-20T15:19:48.8065880'),
        (31, 11, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8078670'),
        (32, 11, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8078680'),
        (33, 11, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8078680'),
        (34, 12, N'Weekday', 1280000, N'2026-01-01', N'2026-10-20T15:19:48.8078690'),
        (35, 12, N'Weekend', 1540000, N'2026-01-01', N'2026-10-20T15:19:48.8078690'),
        (36, 12, N'Holiday', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8078700'),
        (37, 13, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8078700'),
        (38, 13, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8078710'),
        (39, 13, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8078710'),
        (40, 14, N'Weekday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8089880'),
        (41, 14, N'Weekend', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8089880'),
        (42, 14, N'Holiday', 1350000, N'2026-01-01', N'2026-10-20T15:19:48.8089890'),
        (43, 15, N'Weekday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8089910'),
        (44, 15, N'Weekend', 1730000, N'2026-01-01', N'2026-10-20T15:19:48.8089910'),
        (45, 15, N'Holiday', 2160000, N'2026-01-01', N'2026-10-20T15:19:48.8089910'),
        (46, 16, N'Weekday', 1000000, N'2026-01-01', N'2026-10-20T15:19:48.8097890'),
        (47, 16, N'Weekend', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8097900'),
        (48, 16, N'Holiday', 1500000, N'2026-01-01', N'2026-10-20T15:19:48.8097900'),
        (49, 17, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8097900'),
        (50, 17, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8097910'),
        (51, 17, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8097910'),
        (52, 18, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8097920'),
        (53, 18, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8097920'),
        (54, 18, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8097920'),
        (55, 19, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8107020'),
        (56, 19, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8107070'),
        (57, 19, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8107080'),
        (58, 20, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8107080'),
        (59, 20, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8107090'),
        (60, 20, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8107090'),
        (61, 21, N'Weekday', 500000, N'2026-01-01', N'2026-10-20T15:19:48.8114630'),
        (62, 21, N'Weekend', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8114640'),
        (63, 21, N'Holiday', 750000, N'2026-01-01', N'2026-10-20T15:19:48.8114640'),
        (64, 22, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8114640'),
        (65, 22, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8114650'),
        (66, 22, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8114650'),
        (67, 23, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8114720'),
        (68, 23, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8114730'),
        (69, 23, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8114730'),
        (70, 24, N'Weekday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8125180'),
        (71, 24, N'Weekend', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8125190'),
        (72, 24, N'Holiday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8125190'),
        (73, 25, N'Weekday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8125200'),
        (74, 25, N'Weekend', 1150000, N'2026-01-01', N'2026-10-20T15:19:48.8125200'),
        (75, 25, N'Holiday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8125200'),
        (76, 26, N'Weekday', 700000, N'2026-01-01', N'2026-10-20T15:19:48.8134010'),
        (77, 26, N'Weekend', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8134010'),
        (78, 26, N'Holiday', 1050000, N'2026-01-01', N'2026-10-20T15:19:48.8134020'),
        (79, 27, N'Weekday', 1120000, N'2026-01-01', N'2026-10-20T15:19:48.8134080'),
        (80, 27, N'Weekend', 1340000, N'2026-01-01', N'2026-10-20T15:19:48.8134080'),
        (81, 27, N'Holiday', 1680000, N'2026-01-01', N'2026-10-20T15:19:48.8134080'),
        (82, 28, N'Weekday', 560000, N'2026-01-01', N'2026-10-20T15:19:48.8134090'),
        (83, 28, N'Weekend', 670000, N'2026-01-01', N'2026-10-20T15:19:48.8134090'),
        (84, 28, N'Holiday', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8134090'),
        (85, 29, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8146920'),
        (86, 29, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8146930'),
        (87, 29, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8146930'),
        (88, 30, N'Weekday', 1280000, N'2026-01-01', N'2026-10-20T15:19:48.8146950'),
        (89, 30, N'Weekend', 1540000, N'2026-01-01', N'2026-10-20T15:19:48.8146950'),
        (90, 30, N'Holiday', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8147110'),
        (91, 31, N'Weekday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8165220'),
        (92, 31, N'Weekend', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8165230'),
        (93, 31, N'Holiday', 1350000, N'2026-01-01', N'2026-10-20T15:19:48.8165230'),
        (94, 32, N'Weekday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8165240'),
        (95, 32, N'Weekend', 1730000, N'2026-01-01', N'2026-10-20T15:19:48.8165250'),
        (96, 32, N'Holiday', 2160000, N'2026-01-01', N'2026-10-20T15:19:48.8165250'),
        (97, 33, N'Weekday', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8165260'),
        (98, 33, N'Weekend', 860000, N'2026-01-01', N'2026-10-20T15:19:48.8165260'),
        (99, 33, N'Holiday', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8165260'),
        (100, 34, N'Weekday', 1000000, N'2026-01-01', N'2026-10-20T15:19:48.8176650');
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (101, 34, N'Weekend', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8176660'),
        (102, 34, N'Holiday', 1500000, N'2026-01-01', N'2026-10-20T15:19:48.8176660'),
        (103, 35, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8176670'),
        (104, 35, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8176670'),
        (105, 35, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8176670'),
        (106, 36, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8186700'),
        (107, 36, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8186710'),
        (108, 36, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8186710'),
        (109, 37, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8186720'),
        (110, 37, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8186720'),
        (111, 37, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8186720'),
        (112, 38, N'Weekday', 320000, N'2026-01-01', N'2026-10-20T15:19:48.8186730'),
        (113, 38, N'Weekend', 380000, N'2026-01-01', N'2026-10-20T15:19:48.8186730'),
        (114, 38, N'Holiday', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8186730'),
        (115, 39, N'Weekday', 500000, N'2026-01-01', N'2026-10-20T15:19:48.8202280'),
        (116, 39, N'Weekend', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8202300'),
        (117, 39, N'Holiday', 750000, N'2026-01-01', N'2026-10-20T15:19:48.8202300'),
        (118, 40, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8202310'),
        (119, 40, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8202310'),
        (120, 40, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8202310'),
        (121, 41, N'Weekday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8211400'),
        (122, 41, N'Weekend', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8211410'),
        (123, 41, N'Holiday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8211410'),
        (124, 42, N'Weekday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8211420'),
        (125, 42, N'Weekend', 1150000, N'2026-01-01', N'2026-10-20T15:19:48.8211430'),
        (126, 42, N'Holiday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8211430'),
        (127, 43, N'Weekday', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8211430'),
        (128, 43, N'Weekend', 580000, N'2026-01-01', N'2026-10-20T15:19:48.8211440'),
        (129, 43, N'Holiday', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8211440'),
        (130, 44, N'Weekday', 700000, N'2026-01-01', N'2026-10-20T15:19:48.8222190'),
        (131, 44, N'Weekend', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8222200'),
        (132, 44, N'Holiday', 1050000, N'2026-01-01', N'2026-10-20T15:19:48.8222200'),
        (133, 45, N'Weekday', 1120000, N'2026-01-01', N'2026-10-20T15:19:48.8222210'),
        (134, 45, N'Weekend', 1340000, N'2026-01-01', N'2026-10-20T15:19:48.8222210'),
        (135, 45, N'Holiday', 1680000, N'2026-01-01', N'2026-10-20T15:19:48.8222210'),
        (136, 46, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8242010'),
        (137, 46, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8242020'),
        (138, 46, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8242030'),
        (139, 47, N'Weekday', 1280000, N'2026-01-01', N'2026-10-20T15:19:48.8242040'),
        (140, 47, N'Weekend', 1540000, N'2026-01-01', N'2026-10-20T15:19:48.8242040'),
        (141, 47, N'Holiday', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8242040'),
        (142, 48, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8242050'),
        (143, 48, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8242050'),
        (144, 48, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8242050'),
        (145, 49, N'Weekday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8262420'),
        (146, 49, N'Weekend', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8262430'),
        (147, 49, N'Holiday', 1350000, N'2026-01-01', N'2026-10-20T15:19:48.8262440'),
        (148, 50, N'Weekday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8262450'),
        (149, 50, N'Weekend', 1730000, N'2026-01-01', N'2026-10-20T15:19:48.8262450'),
        (150, 50, N'Holiday', 2160000, N'2026-01-01', N'2026-10-20T15:19:48.8262450'),
        (151, 51, N'Weekday', 1000000, N'2026-01-01', N'2026-10-20T15:19:48.8278240'),
        (152, 51, N'Weekend', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8278250'),
        (153, 51, N'Holiday', 1500000, N'2026-01-01', N'2026-10-20T15:19:48.8278260'),
        (154, 52, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8278370'),
        (155, 52, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8278370'),
        (156, 52, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8278380'),
        (157, 53, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8278390'),
        (158, 53, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8278390'),
        (159, 53, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8278390'),
        (160, 54, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8290730'),
        (161, 54, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8290740'),
        (162, 54, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8290740'),
        (163, 55, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8290750'),
        (164, 55, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8290750'),
        (165, 55, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8290750'),
        (166, 56, N'Weekday', 500000, N'2026-01-01', N'2026-10-20T15:19:48.8299860'),
        (167, 56, N'Weekend', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8299920'),
        (168, 56, N'Holiday', 750000, N'2026-01-01', N'2026-10-20T15:19:48.8299930'),
        (169, 57, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8299930'),
        (170, 57, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8299940'),
        (171, 57, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8299940'),
        (172, 58, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8299940'),
        (173, 58, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8299950'),
        (174, 58, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8299950'),
        (175, 59, N'Weekday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8310200'),
        (176, 59, N'Weekend', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8310200'),
        (177, 59, N'Holiday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8310200'),
        (178, 60, N'Weekday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8310210'),
        (179, 60, N'Weekend', 1150000, N'2026-01-01', N'2026-10-20T15:19:48.8310210'),
        (180, 60, N'Holiday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8310220'),
        (181, 61, N'Weekday', 700000, N'2026-01-01', N'2026-10-20T15:19:48.8317630'),
        (182, 61, N'Weekend', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8317630'),
        (183, 61, N'Holiday', 1050000, N'2026-01-01', N'2026-10-20T15:19:48.8317630'),
        (184, 62, N'Weekday', 1120000, N'2026-01-01', N'2026-10-20T15:19:48.8317640'),
        (185, 62, N'Weekend', 1340000, N'2026-01-01', N'2026-10-20T15:19:48.8317640'),
        (186, 62, N'Holiday', 1680000, N'2026-01-01', N'2026-10-20T15:19:48.8317640'),
        (187, 63, N'Weekday', 560000, N'2026-01-01', N'2026-10-20T15:19:48.8317650'),
        (188, 63, N'Weekend', 670000, N'2026-01-01', N'2026-10-20T15:19:48.8317650'),
        (189, 63, N'Holiday', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8317650'),
        (190, 64, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8329900'),
        (191, 64, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8329910'),
        (192, 64, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8329910'),
        (193, 65, N'Weekday', 1280000, N'2026-01-01', N'2026-10-20T15:19:48.8329920'),
        (194, 65, N'Weekend', 1540000, N'2026-01-01', N'2026-10-20T15:19:48.8329920'),
        (195, 65, N'Holiday', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8329920'),
        (196, 66, N'Weekday', 3200000, N'2026-01-01', N'2026-10-20T15:19:48.8329950'),
        (197, 66, N'Weekend', 3840000, N'2026-01-01', N'2026-10-20T15:19:48.8329950'),
        (198, 66, N'Holiday', 4800000, N'2026-01-01', N'2026-10-20T15:19:48.8329950'),
        (199, 67, N'Weekday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8343050'),
        (200, 67, N'Weekend', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8343060');
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (201, 67, N'Holiday', 1350000, N'2026-01-01', N'2026-10-20T15:19:48.8343060'),
        (202, 68, N'Weekday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8343150'),
        (203, 68, N'Weekend', 1730000, N'2026-01-01', N'2026-10-20T15:19:48.8343150'),
        (204, 68, N'Holiday', 2160000, N'2026-01-01', N'2026-10-20T15:19:48.8343150'),
        (205, 69, N'Weekday', 3600000, N'2026-01-01', N'2026-10-20T15:19:48.8343160'),
        (206, 69, N'Weekend', 4320000, N'2026-01-01', N'2026-10-20T15:19:48.8343170'),
        (207, 69, N'Holiday', 5400000, N'2026-01-01', N'2026-10-20T15:19:48.8343170'),
        (208, 70, N'Weekday', 1000000, N'2026-01-01', N'2026-10-20T15:19:48.8354980'),
        (209, 70, N'Weekend', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8354990'),
        (210, 70, N'Holiday', 1500000, N'2026-01-01', N'2026-10-20T15:19:48.8354990'),
        (211, 71, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8355000'),
        (212, 71, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8355000'),
        (213, 71, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8355010'),
        (214, 72, N'Weekday', 4000000, N'2026-01-01', N'2026-10-20T15:19:48.8355080'),
        (215, 72, N'Weekend', 4800000, N'2026-01-01', N'2026-10-20T15:19:48.8355080'),
        (216, 72, N'Holiday', 6000000, N'2026-01-01', N'2026-10-20T15:19:48.8355080'),
        (217, 73, N'Weekday', 400000, N'2026-01-01', N'2026-10-20T15:19:48.8366890'),
        (218, 73, N'Weekend', 480000, N'2026-01-01', N'2026-10-20T15:19:48.8366900'),
        (219, 73, N'Holiday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8366900'),
        (220, 74, N'Weekday', 640000, N'2026-01-01', N'2026-10-20T15:19:48.8366910'),
        (221, 74, N'Weekend', 770000, N'2026-01-01', N'2026-10-20T15:19:48.8366910'),
        (222, 74, N'Holiday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8366910'),
        (223, 75, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8366920'),
        (224, 75, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8366920'),
        (225, 75, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8366920'),
        (226, 76, N'Weekday', 500000, N'2026-01-01', N'2026-10-20T15:19:48.8389060'),
        (227, 76, N'Weekend', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8389070'),
        (228, 76, N'Holiday', 750000, N'2026-01-01', N'2026-10-20T15:19:48.8389070'),
        (229, 77, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8389080'),
        (230, 77, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8389080'),
        (231, 77, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8389080'),
        (232, 78, N'Weekday', 2000000, N'2026-01-01', N'2026-10-20T15:19:48.8389090'),
        (233, 78, N'Weekend', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8389090'),
        (234, 78, N'Holiday', 3000000, N'2026-01-01', N'2026-10-20T15:19:48.8389090'),
        (235, 79, N'Weekday', 600000, N'2026-01-01', N'2026-10-20T15:19:48.8406040'),
        (236, 79, N'Weekend', 720000, N'2026-01-01', N'2026-10-20T15:19:48.8406050'),
        (237, 79, N'Holiday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8406050'),
        (238, 80, N'Weekday', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8406060'),
        (239, 80, N'Weekend', 1150000, N'2026-01-01', N'2026-10-20T15:19:48.8406060'),
        (240, 80, N'Holiday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8406060'),
        (241, 81, N'Weekday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8406070'),
        (242, 81, N'Weekend', 2880000, N'2026-01-01', N'2026-10-20T15:19:48.8406120'),
        (243, 81, N'Holiday', 3600000, N'2026-01-01', N'2026-10-20T15:19:48.8406120'),
        (244, 82, N'Weekday', 700000, N'2026-01-01', N'2026-10-20T15:19:48.8477630'),
        (245, 82, N'Weekend', 840000, N'2026-01-01', N'2026-10-20T15:19:48.8477640'),
        (246, 82, N'Holiday', 1050000, N'2026-01-01', N'2026-10-20T15:19:48.8477640'),
        (247, 83, N'Weekday', 1120000, N'2026-01-01', N'2026-10-20T15:19:48.8478020'),
        (248, 83, N'Weekend', 1340000, N'2026-01-01', N'2026-10-20T15:19:48.8478020'),
        (249, 83, N'Holiday', 1680000, N'2026-01-01', N'2026-10-20T15:19:48.8478020'),
        (250, 84, N'Weekday', 2800000, N'2026-01-01', N'2026-10-20T15:19:48.8478030'),
        (251, 84, N'Weekend', 3360000, N'2026-01-01', N'2026-10-20T15:19:48.8478030'),
        (252, 84, N'Holiday', 4200000, N'2026-01-01', N'2026-10-20T15:19:48.8478030'),
        (253, 85, N'Weekday', 800000, N'2026-01-01', N'2026-10-20T15:19:48.8490210'),
        (254, 85, N'Weekend', 960000, N'2026-01-01', N'2026-10-20T15:19:48.8490220'),
        (255, 85, N'Holiday', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8490220'),
        (256, 86, N'Weekday', 1280000, N'2026-01-01', N'2026-10-20T15:19:48.8490230'),
        (257, 86, N'Weekend', 1540000, N'2026-01-01', N'2026-10-20T15:19:48.8490230'),
        (258, 86, N'Holiday', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8490230'),
        (259, 87, N'Weekday', 3200000, N'2026-01-01', N'2026-10-20T15:19:48.8490240'),
        (260, 87, N'Weekend', 3840000, N'2026-01-01', N'2026-10-20T15:19:48.8490240'),
        (261, 87, N'Holiday', 4800000, N'2026-01-01', N'2026-10-20T15:19:48.8490240'),
        (262, 88, N'Weekday', 900000, N'2026-01-01', N'2026-10-20T15:19:48.8507430'),
        (263, 88, N'Weekend', 1080000, N'2026-01-01', N'2026-10-20T15:19:48.8507440'),
        (264, 88, N'Holiday', 1350000, N'2026-01-01', N'2026-10-20T15:19:48.8507440'),
        (265, 89, N'Weekday', 1440000, N'2026-01-01', N'2026-10-20T15:19:48.8507450'),
        (266, 89, N'Weekend', 1730000, N'2026-01-01', N'2026-10-20T15:19:48.8507460'),
        (267, 89, N'Holiday', 2160000, N'2026-01-01', N'2026-10-20T15:19:48.8507460'),
        (268, 90, N'Weekday', 3600000, N'2026-01-01', N'2026-10-20T15:19:48.8507460'),
        (269, 90, N'Weekend', 4320000, N'2026-01-01', N'2026-10-20T15:19:48.8507570'),
        (270, 90, N'Holiday', 5400000, N'2026-01-01', N'2026-10-20T15:19:48.8507570'),
        (271, 91, N'Weekday', 1000000, N'2026-01-01', N'2026-10-20T15:19:48.8519590'),
        (272, 91, N'Weekend', 1200000, N'2026-01-01', N'2026-10-20T15:19:48.8519600'),
        (273, 91, N'Holiday', 1500000, N'2026-01-01', N'2026-10-20T15:19:48.8519600'),
        (274, 92, N'Weekday', 1600000, N'2026-01-01', N'2026-10-20T15:19:48.8519610'),
        (275, 92, N'Weekend', 1920000, N'2026-01-01', N'2026-10-20T15:19:48.8519610'),
        (276, 92, N'Holiday', 2400000, N'2026-01-01', N'2026-10-20T15:19:48.8519610'),
        (277, 93, N'Weekday', 4000000, N'2026-01-01', N'2026-10-20T15:19:48.8519620'),
        (278, 93, N'Weekend', 4800000, N'2026-01-01', N'2026-10-20T15:19:48.8519620'),
        (279, 93, N'Holiday', 6000000, N'2026-01-01', N'2026-10-20T15:19:48.8519620');
    SET IDENTITY_INSERT [dbo].[RoomTypePrices] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomTypePrices]', RESEED, 279) WITH NO_INFOMSGS;

    -- Rooms: 128 dòng.
    SET IDENTITY_INSERT [dbo].[Rooms] ON;
    INSERT INTO [dbo].[Rooms] ([Id], [RoomTypeId], [RoomCode], [IsActive]) VALUES
        (1, 1, N'101', 1),
        (2, 1, N'102', 1),
        (3, 2, N'201', 1),
        (4, 3, N'103', 1),
        (5, 4, N'101', 1),
        (6, 4, N'102', 1),
        (7, 5, N'201', 1),
        (8, 6, N'101', 1),
        (9, 6, N'102', 1),
        (10, 7, N'201', 1),
        (11, 8, N'103', 1),
        (12, 9, N'101', 1),
        (13, 9, N'102', 1),
        (14, 10, N'201', 1),
        (15, 11, N'101', 1),
        (16, 11, N'102', 1),
        (17, 12, N'201', 1),
        (18, 13, N'103', 1),
        (19, 14, N'101', 1),
        (20, 14, N'102', 1),
        (21, 15, N'201', 1),
        (22, 16, N'101', 1),
        (23, 16, N'102', 1),
        (24, 17, N'201', 1),
        (25, 18, N'103', 1),
        (26, 19, N'101', 1),
        (27, 19, N'102', 1),
        (28, 20, N'201', 1),
        (29, 21, N'101', 1),
        (30, 21, N'102', 1),
        (31, 22, N'201', 1),
        (32, 23, N'103', 1),
        (33, 24, N'101', 1),
        (34, 24, N'102', 1),
        (35, 25, N'201', 1),
        (36, 26, N'101', 1),
        (37, 26, N'102', 1),
        (38, 27, N'201', 1),
        (39, 28, N'103', 1),
        (40, 29, N'101', 1),
        (41, 29, N'102', 1),
        (42, 30, N'201', 1),
        (43, 31, N'101', 1),
        (44, 31, N'102', 1),
        (45, 32, N'201', 1),
        (46, 33, N'103', 1),
        (47, 34, N'101', 1),
        (48, 34, N'102', 1),
        (49, 35, N'201', 1),
        (50, 36, N'101', 1),
        (51, 36, N'102', 1),
        (52, 37, N'201', 1),
        (53, 38, N'103', 1),
        (54, 39, N'101', 1),
        (55, 39, N'102', 1),
        (56, 40, N'201', 1),
        (57, 41, N'101', 1),
        (58, 41, N'102', 1),
        (59, 42, N'201', 1),
        (60, 43, N'103', 1),
        (61, 44, N'101', 1),
        (62, 44, N'102', 1),
        (63, 45, N'201', 1),
        (64, 46, N'101', 1),
        (65, 46, N'102', 1),
        (66, 47, N'201', 1),
        (67, 48, N'103', 1),
        (68, 49, N'101', 1),
        (69, 49, N'102', 1),
        (70, 50, N'201', 1),
        (71, 51, N'101', 1),
        (72, 51, N'102', 1),
        (73, 52, N'201', 1),
        (74, 53, N'103', 1),
        (75, 54, N'101', 1),
        (76, 54, N'102', 1),
        (77, 55, N'201', 1),
        (78, 56, N'101', 1),
        (79, 56, N'102', 1),
        (80, 57, N'201', 1),
        (81, 58, N'103', 1),
        (82, 59, N'101', 1),
        (83, 59, N'102', 1),
        (84, 60, N'201', 1),
        (85, 61, N'101', 1),
        (86, 61, N'102', 1),
        (87, 62, N'201', 1),
        (88, 63, N'103', 1),
        (89, 64, N'101', 1),
        (90, 64, N'102', 1),
        (91, 65, N'201', 1),
        (92, 66, N'Căn A', 1),
        (93, 67, N'101', 1),
        (94, 67, N'102', 1),
        (95, 68, N'201', 1),
        (96, 69, N'Căn A', 1),
        (97, 70, N'101', 1),
        (98, 70, N'102', 1),
        (99, 71, N'201', 1),
        (100, 72, N'Căn A', 1);
    INSERT INTO [dbo].[Rooms] ([Id], [RoomTypeId], [RoomCode], [IsActive]) VALUES
        (101, 73, N'101', 1),
        (102, 73, N'102', 1),
        (103, 74, N'201', 1),
        (104, 75, N'Căn A', 1),
        (105, 76, N'101', 1),
        (106, 76, N'102', 1),
        (107, 77, N'201', 1),
        (108, 78, N'Căn A', 1),
        (109, 79, N'101', 1),
        (110, 79, N'102', 1),
        (111, 80, N'201', 1),
        (112, 81, N'Căn A', 1),
        (113, 82, N'101', 1),
        (114, 82, N'102', 1),
        (115, 83, N'201', 1),
        (116, 84, N'Căn A', 1),
        (117, 85, N'101', 1),
        (118, 85, N'102', 1),
        (119, 86, N'201', 1),
        (120, 87, N'Căn A', 1),
        (121, 88, N'101', 1),
        (122, 88, N'102', 1),
        (123, 89, N'201', 1),
        (124, 90, N'Căn A', 1),
        (125, 91, N'101', 1),
        (126, 91, N'102', 1),
        (127, 92, N'201', 1),
        (128, 93, N'Căn A', 1);
    SET IDENTITY_INSERT [dbo].[Rooms] OFF;
    DBCC CHECKIDENT (N'[dbo].[Rooms]', RESEED, 128) WITH NO_INFOMSGS;

    -- Bookings: 60 dòng.
    SET IDENTITY_INSERT [dbo].[Bookings] ON;
    INSERT INTO [dbo].[Bookings] ([Id], [Code], [CustomerId], [ContactName], [ContactPhone], [SourceChannel], [CreatedByEmployeeId], [AssignedEmployeeId], [HomestayId], [RoomTypeId], [RoomId], [CheckIn], [CheckOut], [Guests], [SpecialRequests], [Status], [HoldExpiresAt], [PaymentSubmittedAt], [DepositedAt], [ConfirmDeadlineAt], [ConfirmAlertSentAt], [ConfirmedAt], [CheckedInAt], [CompletedAt], [CancelledAt], [CancelReason], [TotalAmount], [DepositRate], [DepositAmount], [CommissionRate], [CancellationPolicyId], [RefundAmount], [ReplacedByBookingId], [CreatedAt], [UpdatedAt], [DiscountAmount], [VoucherId], [ChangeFeeAmount]) VALUES
        (1, N'FJ260929-001', 2, N'Trần Thị Bích', N'+84987000002', N'Zalo', 4, 4, 6, 14, 20, N'2026-10-19', N'2026-10-22', 2, NULL, N'CheckedIn', N'2026-09-29T05:52:00', NULL, N'2026-09-29T05:43:00', N'2026-09-29T06:13:00', NULL, N'2026-09-29T05:55:00', N'2026-10-19T09:02:00', NULL, NULL, NULL, 2700000, 0.3000, 810000, 0.1500, 1, NULL, NULL, N'2026-09-29T05:37:00', N'2026-10-19T09:02:00', 0, NULL, 0),
        (2, N'FJ261008-001', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 4, 10, 14, N'2026-10-20', N'2026-10-23', 3, NULL, N'CheckedIn', N'2026-10-08T05:30:00', N'2026-10-08T05:17:00', N'2026-10-08T07:36:00', N'2026-10-08T08:06:00', NULL, N'2026-10-08T07:44:00', N'2026-10-20T07:27:00', NULL, NULL, NULL, 3360000, 0.3000, 1008000, 0.1000, 1, NULL, NULL, N'2026-10-08T05:15:00', N'2026-10-20T07:27:00', 0, NULL, 0),
        (3, N'FJ261020-001', 4, N'Đặng Thị Thu', N'+84987000003', N'Web', NULL, NULL, 1, 3, 4, N'2026-10-29', N'2026-10-30', 1, NULL, N'Deposited', N'2026-10-20T15:12:49.4411780', NULL, N'2026-10-20T15:02:49.4411780', N'2026-10-20T15:32:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, 320000, 0.3000, 96000, 0.1000, 1, NULL, NULL, N'2026-10-20T14:57:49.4411780', N'2026-10-20T15:02:49.4411780', 0, NULL, 0),
        (4, N'FJ261020-002', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 5, 5, 30, 77, 107, N'2026-11-05', N'2026-11-06', 4, NULL, N'Deposited', N'2026-10-20T14:34:49.4411780', NULL, N'2026-10-20T14:24:49.4411780', N'2026-10-20T14:54:49.4411780', N'2026-10-20T15:19:55.1029310', NULL, NULL, NULL, NULL, NULL, 800000, 0.3000, 240000, 0.1500, 1, NULL, NULL, N'2026-10-20T14:19:49.4411780', N'2026-10-20T14:24:49.4411780', 0, NULL, 0),
        (5, N'FJ261020-003', 4, N'Đặng Thị Thu', N'+84987000003', N'Zalo', 4, 4, 35, 93, 128, N'2026-11-01', N'2026-11-03', 10, NULL, N'PaymentSubmitted', N'2026-10-20T14:59:49.4411780', N'2026-10-20T14:49:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 8000000, 0.3000, 2400000, 0.1200, 1, NULL, NULL, N'2026-10-20T14:44:49.4411780', N'2026-10-20T14:49:49.4411780', 0, NULL, 0),
        (6, N'FJ261020-004', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 26, 64, 89, N'2026-11-09', N'2026-11-10', 1, NULL, N'PaymentSubmitted', N'2026-10-20T12:24:49.4411780', N'2026-10-20T12:14:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 800000, 0.3000, 240000, 0.1200, 1, NULL, NULL, N'2026-10-20T12:09:49.4411780', N'2026-10-20T12:14:49.4411780', 0, NULL, 0),
        (7, N'FJ260601-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 7, 17, 24, N'2026-06-07', N'2026-06-09', 3, NULL, N'Rejected', N'2026-06-01T05:40:00', NULL, N'2026-06-01T05:34:00', N'2026-06-01T06:04:00', NULL, NULL, NULL, NULL, N'2026-06-01T06:03:00', N'Chủ nhà bận việc gia đình, không đón khách được.', 3200000, 0.3000, 960000, 0.1000, 1, 960000, NULL, N'2026-06-01T05:25:00', N'2026-06-01T06:03:00', 0, NULL, 0),
        (8, N'FJ260825-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 6, 15, 21, N'2026-09-15', N'2026-09-16', 3, NULL, N'NoShow', N'2026-08-25T01:36:00', NULL, N'2026-08-25T01:27:00', N'2026-08-25T01:57:00', NULL, N'2026-08-25T01:50:00', NULL, NULL, NULL, NULL, 1440000, 0.3000, 432000, 0.1500, 1, 0, NULL, N'2026-08-25T01:21:00', N'2026-09-15T16:00:00', 0, NULL, 0),
        (9, N'FJ261020-005', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 35, 93, 128, N'2026-12-20', N'2026-12-22', 10, NULL, N'Confirmed', N'2026-10-20T10:34:49.4411780', NULL, N'2026-10-20T10:24:49.4411780', N'2026-10-20T10:54:49.4411780', NULL, N'2026-10-20T10:42:49.4411780', NULL, NULL, NULL, NULL, 8000000, 0.3000, 2400000, 0.1200, 1, NULL, NULL, N'2026-10-20T10:19:49.4411780', N'2026-10-20T10:42:49.4411780', 0, NULL, 0),
        (10, N'FJ260804-001', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 33, 87, 120, N'2026-08-31', N'2026-09-02', 9, NULL, N'Cancelled', N'2026-08-04T02:03:00', NULL, N'2026-08-04T01:52:00', N'2026-08-04T02:22:00', NULL, N'2026-08-04T02:20:00', NULL, NULL, N'2026-08-26T21:01:15.0358745', N'Đặt nhầm ngày.', 8000000, 0.3000, 2400000, 0.1500, 4, 0, NULL, N'2026-08-04T01:48:00', N'2026-08-26T21:01:15.0358745', 0, NULL, 0),
        (11, N'FJ260705-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 8, 20, 28, N'2026-07-24', N'2026-07-25', 4, NULL, N'Completed', N'2026-07-05T05:10:00', NULL, N'2026-07-05T05:02:00', N'2026-07-05T05:32:00', NULL, N'2026-07-05T05:21:00', N'2026-07-24T08:22:00', N'2026-07-25T04:17:00', NULL, NULL, 770000, 0.3000, 231000, 0.1200, 1, NULL, NULL, N'2026-07-05T04:55:00', N'2026-07-25T04:17:00', 0, NULL, 0),
        (12, N'FJ260713-001', 9, N'Hồ Minh Khang', N'+84987000008', N'Zalo', 5, 5, 6, 15, 21, N'2026-08-09', N'2026-08-10', 4, NULL, N'Completed', N'2026-07-13T03:45:00', NULL, N'2026-07-13T03:40:00', N'2026-07-13T04:10:00', NULL, N'2026-07-13T04:00:00', N'2026-08-09T08:49:00', N'2026-08-10T04:58:00', NULL, NULL, 1440000, 0.3000, 432000, 0.1500, 1, NULL, NULL, N'2026-07-13T03:30:00', N'2026-08-10T04:58:00', 0, NULL, 0),
        (13, N'FJ260621-001', 4, N'Đặng Thị Thu', N'+84987000003', N'Phone', 4, 4, 8, 19, 26, N'2026-06-27', N'2026-06-28', 1, NULL, N'Completed', N'2026-06-21T04:22:00', N'2026-06-21T04:17:00', N'2026-06-21T05:16:00', N'2026-06-21T05:46:00', NULL, N'2026-06-21T05:22:00', N'2026-06-27T09:09:00', N'2026-06-28T04:53:00', NULL, NULL, 480000, 0.3000, 144000, 0.1200, 1, NULL, NULL, N'2026-06-21T04:07:00', N'2026-06-28T04:53:00', 0, NULL, 0),
        (14, N'FJ261018-001', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 21, 53, 74, N'2026-11-18', N'2026-11-19', 1, NULL, N'Rejected', N'2026-10-18T06:34:49.4411780', NULL, N'2026-10-18T06:21:49.4411780', N'2026-10-18T06:51:49.4411780', NULL, NULL, NULL, NULL, N'2026-10-18T06:37:49.4411780', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', 800000, 0.3000, 240000, 0.1500, 1, 240000, NULL, N'2026-10-18T06:19:49.4411780', N'2026-10-18T06:37:49.4411780', 0, NULL, 0),
        (15, N'FJ261020-006', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 1, 1, 1, N'2026-10-23', N'2026-10-26', 1, NULL, N'Confirmed', N'2026-10-20T04:34:49.4411780', NULL, N'2026-10-20T04:21:49.4411780', N'2026-10-20T04:51:49.4411780', NULL, N'2026-10-20T04:28:49.4411780', NULL, NULL, NULL, NULL, 1360000, 0.3000, 408000, 0.1000, 1, NULL, NULL, N'2026-10-20T04:19:49.4411780', N'2026-10-20T04:28:49.4411780', 0, NULL, 0),
        (16, N'FJ260601-002', 6, N'Phan Ngọc Ánh', N'+84987000005', N'Phone', 4, 4, 12, 29, 40, N'2026-06-10', N'2026-06-13', 2, NULL, N'Completed', N'2026-06-01T04:52:00', NULL, N'2026-06-01T04:49:00', N'2026-06-01T05:19:00', NULL, N'2026-06-01T04:55:00', N'2026-06-10T10:03:00', N'2026-06-13T04:40:00', NULL, NULL, 2560000, 0.3000, 768000, 0.1500, 1, NULL, NULL, N'2026-06-01T04:37:00', N'2026-06-13T04:40:00', 0, NULL, 0),
        (17, N'FJ261019-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 8, 20, 28, N'2026-10-25', N'2026-10-27', 4, NULL, N'Cancelled', N'2026-10-19T02:41:00', NULL, N'2026-10-19T02:28:00', N'2026-10-19T02:58:00', NULL, N'2026-10-19T02:39:00', NULL, NULL, N'2026-10-19T21:11:54.1590931', N'Thay đổi lịch công tác.', 1280000, 0.3000, 384000, 0.1200, 1, 192000, NULL, N'2026-10-19T02:26:00', N'2026-10-19T21:11:54.1590931', 0, NULL, 0),
        (18, N'FJ260601-003', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 1, 2, 3, N'2026-06-01', N'2026-06-04', 3, NULL, N'Completed', N'2026-06-01T03:38:00', NULL, N'2026-06-01T03:32:00', N'2026-06-01T04:02:00', NULL, N'2026-06-01T03:37:00', N'2026-06-01T08:56:00', N'2026-06-04T04:21:00', NULL, NULL, 1920000, 0.3000, 576000, 0.1000, 1, NULL, NULL, N'2026-06-01T03:23:00', N'2026-06-04T04:21:00', 0, NULL, 0),
        (19, N'FJ260912-001', 15, N'Kiều Văn Phúc', N'+84987000014', N'Phone', 5, 5, 14, 35, 49, N'2026-09-14', N'2026-09-15', 4, NULL, N'Completed', N'2026-09-12T04:14:00', N'2026-09-12T04:05:00', N'2026-09-12T06:25:00', N'2026-09-12T06:55:00', NULL, N'2026-09-12T06:36:00', N'2026-09-14T07:59:00', N'2026-09-15T04:44:00', NULL, NULL, 1600000, 0.3000, 480000, 0.1200, 1, NULL, NULL, N'2026-09-12T03:59:00', N'2026-09-15T04:44:00', 0, NULL, 0),
        (20, N'FJ261017-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Phone', 4, 4, 16, 40, 56, N'2026-12-08', N'2026-12-11', 4, NULL, N'Confirmed', N'2026-10-17T15:34:49.4411780', N'2026-10-17T15:30:49.4411780', N'2026-10-17T16:47:49.4411780', N'2026-10-17T17:17:49.4411780', NULL, N'2026-10-17T17:12:49.4411780', NULL, NULL, NULL, NULL, 2400000, 0.3000, 720000, 0.1000, 1, NULL, NULL, N'2026-10-17T15:19:49.4411780', N'2026-10-17T17:12:49.4411780', 0, NULL, 0),
        (21, N'FJ260607-001', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 4, 4, 33, 86, 119, N'2026-07-04', N'2026-07-07', 3, NULL, N'Completed', N'2026-06-07T05:28:00', NULL, N'2026-06-07T05:19:00', N'2026-06-07T05:49:00', NULL, N'2026-06-07T05:24:00', N'2026-07-04T10:07:00', N'2026-07-07T04:34:00', NULL, NULL, 4100000, 0.3000, 1230000, 0.1500, 1, NULL, NULL, N'2026-06-07T05:13:00', N'2026-07-07T04:34:00', 0, NULL, 0),
        (22, N'FJ261020-007', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 27, 68, 95, N'2026-11-09', N'2026-11-12', 4, NULL, N'Confirmed', N'2026-10-20T07:34:49.4411780', NULL, N'2026-10-20T07:28:49.4411780', N'2026-10-20T07:58:49.4411780', NULL, N'2026-10-20T07:55:49.4411780', NULL, NULL, NULL, NULL, 4320000, 0.3000, 1296000, 0.1500, 1, NULL, NULL, N'2026-10-20T07:19:49.4411780', N'2026-10-20T07:55:49.4411780', 0, NULL, 0),
        (23, N'FJ261019-002', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 10, 25, 35, N'2026-11-23', N'2026-11-25', 4, NULL, N'Cancelled', N'2026-10-18T19:34:49.4411780', NULL, N'2026-10-18T19:22:49.4411780', N'2026-10-18T19:52:49.4411780', NULL, N'2026-10-18T19:33:49.4411780', NULL, NULL, N'2026-10-20T01:10:45.9581452', N'Gia đình có việc đột xuất.', 1920000, 0.3000, 576000, 0.1000, 1, 576000, NULL, N'2026-10-18T19:19:49.4411780', N'2026-10-20T01:10:45.9581452', 0, NULL, 0),
        (24, N'FJ260710-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 20, 50, 70, N'2026-07-13', N'2026-07-16', 3, NULL, N'Completed', N'2026-07-10T05:55:00', NULL, N'2026-07-10T05:45:00', N'2026-07-10T06:15:00', NULL, N'2026-07-10T05:54:00', N'2026-07-13T10:46:00', N'2026-07-16T04:48:00', NULL, NULL, 4320000, 0.3000, 1296000, 0.1200, 1, NULL, NULL, N'2026-07-10T05:40:00', N'2026-07-16T04:48:00', 0, NULL, 0),
        (25, N'FJ261020-008', 6, N'Phan Ngọc Ánh', N'+84987000005', N'Web', NULL, NULL, 24, 59, 82, N'2026-12-02', N'2026-12-03', 2, NULL, N'Cancelled', N'2026-10-20T08:34:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-20T08:27:49.4411780', N'Chuyển sang đi tỉnh khác.', 600000, 0.3000, 180000, 0.1500, 1, 0, NULL, N'2026-10-20T08:19:49.4411780', N'2026-10-20T08:27:49.4411780', 0, NULL, 0),
        (26, N'FJ260627-001', 4, N'Đặng Thị Thu', N'+84987000003', N'Web', NULL, NULL, 25, 62, 87, N'2026-07-19', N'2026-07-21', 3, NULL, N'Completed', N'2026-06-27T04:46:00', NULL, N'2026-06-27T04:40:00', N'2026-06-27T05:10:00', NULL, N'2026-06-27T04:56:00', N'2026-07-19T09:17:00', N'2026-07-21T04:26:00', NULL, NULL, 2240000, 0.3000, 672000, 0.1000, 1, NULL, NULL, N'2026-06-27T04:31:00', N'2026-07-21T04:26:00', 0, NULL, 0),
        (27, N'FJ261020-009', 2, N'Trần Thị Bích', N'+84987000002', N'Zalo', 5, 5, 9, 23, 32, N'2026-11-19', N'2026-11-20', 1, NULL, N'Cancelled', N'2026-10-20T07:34:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-20T07:26:49.4411780', N'Chuyển sang đi tỉnh khác.', 400000, 0.3000, 120000, 0.1500, 1, 0, NULL, N'2026-10-20T07:19:49.4411780', N'2026-10-20T07:26:49.4411780', 0, NULL, 0),
        (28, N'FJ260912-002', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Phone', 4, 4, 12, 30, 42, N'2026-09-27', N'2026-09-29', 4, NULL, N'Expired', N'2026-09-12T05:57:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-09-12T05:58:00', NULL, 2560000, 0.3000, 768000, 0.1500, 1, NULL, NULL, N'2026-09-12T05:42:00', N'2026-09-12T05:58:00', 0, NULL, 0),
        (29, N'FJ260712-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Zalo', 4, 4, 4, 10, 14, N'2026-08-07', N'2026-08-10', 3, NULL, N'Completed', N'2026-07-12T01:22:00', NULL, N'2026-07-12T01:15:00', N'2026-07-12T01:45:00', NULL, N'2026-07-12T01:40:00', N'2026-08-07T09:34:00', N'2026-08-10T04:23:00', NULL, NULL, 3800000, 0.3000, 1140000, 0.1000, 1, NULL, NULL, N'2026-07-12T01:07:00', N'2026-08-10T04:23:00', 0, NULL, 0),
        (30, N'FJ260621-002', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 27, 67, 93, N'2026-06-30', N'2026-07-03', 2, NULL, N'Expired', N'2026-06-21T05:40:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-06-21T05:41:00', NULL, 2700000, 0.3000, 810000, 0.1500, 1, NULL, NULL, N'2026-06-21T05:25:00', N'2026-06-21T05:41:00', 0, NULL, 0),
        (31, N'FJ260809-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 5, 12, 17, N'2026-09-07', N'2026-09-10', 4, NULL, N'Completed', N'2026-08-09T05:42:00', N'2026-08-09T05:33:00', N'2026-08-09T06:43:00', N'2026-08-09T07:13:00', NULL, N'2026-08-09T06:58:00', N'2026-09-07T10:45:00', N'2026-09-10T04:06:00', NULL, NULL, 3840000, 0.3000, 1152000, 0.1200, 1, NULL, NULL, N'2026-08-09T05:27:00', N'2026-09-10T04:06:00', 0, NULL, 0),
        (32, N'FJ260827-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Zalo', 4, 4, 30, 76, 105, N'2026-09-25', N'2026-09-28', 1, NULL, N'Completed', N'2026-08-27T03:45:00', NULL, N'2026-08-27T03:42:00', N'2026-08-27T04:12:00', NULL, N'2026-08-27T04:02:00', N'2026-09-25T07:20:00', N'2026-09-28T04:41:00', NULL, NULL, 1700000, 0.3000, 510000, 0.1500, 1, NULL, NULL, N'2026-08-27T03:30:00', N'2026-09-28T04:41:00', 0, NULL, 0),
        (33, N'FJ261018-002', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 14, 35, 49, N'2026-12-04', N'2026-12-06', 3, NULL, N'Expired', N'2026-10-18T16:34:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-18T16:35:49.4411780', NULL, 3840000, 0.3000, 1152000, 0.1200, 1, NULL, NULL, N'2026-10-18T16:19:49.4411780', N'2026-10-18T16:35:49.4411780', 0, NULL, 0),
        (34, N'FJ260923-001', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 4, 4, 20, 50, 70, N'2026-10-02', N'2026-10-03', 4, NULL, N'Completed', N'2026-09-23T03:50:00', NULL, N'2026-09-23T03:46:00', N'2026-09-23T04:16:00', NULL, N'2026-09-23T04:14:00', N'2026-10-02T10:10:00', N'2026-10-03T04:51:00', NULL, NULL, 1730000, 0.3000, 519000, 0.1200, 1, NULL, NULL, N'2026-09-23T03:35:00', N'2026-10-03T04:51:00', 0, NULL, 0),
        (35, N'FJ260904-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Zalo', 4, 4, 15, 37, 52, N'2026-09-20', N'2026-09-23', 4, NULL, N'Completed', N'2026-09-04T02:56:00', N'2026-09-04T02:45:00', N'2026-09-04T03:24:00', N'2026-09-04T03:54:00', NULL, N'2026-09-04T03:45:00', N'2026-09-20T07:20:00', N'2026-09-23T04:54:00', NULL, NULL, 1920000, 0.3000, 576000, 0.1500, 1, NULL, NULL, N'2026-09-04T02:41:00', N'2026-09-23T04:54:00', 0, NULL, 0),
        (36, N'FJ261018-003', 7, N'Trịnh Văn Long', N'+84987000006', N'Phone', 5, 5, 30, 78, 108, N'2026-12-03', N'2026-12-04', 9, NULL, N'Confirmed', N'2026-10-17T20:34:49.4411780', NULL, N'2026-10-17T20:26:49.4411780', N'2026-10-17T20:56:49.4411780', NULL, N'2026-10-17T20:34:49.4411780', NULL, NULL, NULL, NULL, 2000000, 0.3000, 600000, 0.1500, 1, NULL, NULL, N'2026-10-17T20:19:49.4411780', N'2026-10-17T20:34:49.4411780', 0, NULL, 0),
        (37, N'FJ260601-004', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 25, 63, 88, N'2026-06-11', N'2026-06-12', 1, NULL, N'Completed', N'2026-06-01T03:36:00', NULL, N'2026-06-01T03:25:00', N'2026-06-01T03:55:00', NULL, N'2026-06-01T03:52:00', N'2026-06-11T10:28:00', N'2026-06-12T04:02:00', NULL, NULL, 560000, 0.3000, 168000, 0.1000, 1, NULL, NULL, N'2026-06-01T03:21:00', N'2026-06-12T04:02:00', 0, NULL, 0),
        (38, N'FJ260704-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 22, 55, 77, N'2026-07-17', N'2026-07-19', 3, NULL, N'Expired', N'2026-07-04T06:10:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-07-04T06:11:00', NULL, 1540000, 0.3000, 462000, 0.1000, 1, NULL, NULL, N'2026-07-04T05:55:00', N'2026-07-04T06:11:00', 0, NULL, 0),
        (39, N'FJ261018-004', 12, N'Chu Thị Ngân', N'+84987000011', N'Phone', 4, 4, 31, 81, 112, N'2026-12-22', N'2026-12-24', 10, NULL, N'Confirmed', N'2026-10-18T16:34:49.4411780', NULL, N'2026-10-18T16:30:49.4411780', N'2026-10-18T17:00:49.4411780', NULL, N'2026-10-18T16:47:49.4411780', NULL, NULL, NULL, NULL, 4800000, 0.3000, 1440000, 0.1000, 1, NULL, NULL, N'2026-10-18T16:19:49.4411780', N'2026-10-18T16:47:49.4411780', 0, NULL, 0),
        (40, N'FJ260715-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 7, 18, 25, N'2026-08-05', N'2026-08-07', 1, NULL, N'Completed', N'2026-07-15T03:09:00', NULL, N'2026-07-15T03:06:00', N'2026-07-15T03:36:00', NULL, N'2026-07-15T03:20:00', N'2026-08-05T09:55:00', N'2026-08-07T04:14:00', NULL, NULL, 1600000, 0.3000, 480000, 0.1000, 1, NULL, NULL, N'2026-07-15T02:54:00', N'2026-08-07T04:14:00', 0, NULL, 0),
        (41, N'FJ260629-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 32, 82, 114, N'2026-07-02', N'2026-07-03', 1, NULL, N'Completed', N'2026-06-29T01:56:00', NULL, N'2026-06-29T01:47:00', N'2026-06-29T02:17:00', NULL, N'2026-06-29T01:55:00', N'2026-07-02T10:34:00', N'2026-07-03T04:33:00', NULL, NULL, 700000, 0.3000, 210000, 0.1200, 1, NULL, NULL, N'2026-06-29T01:41:00', N'2026-07-03T04:33:00', 0, NULL, 0),
        (42, N'FJ260908-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 18, 44, 62, N'2026-09-11', N'2026-09-14', 1, NULL, N'Completed', N'2026-09-08T02:09:00', N'2026-09-08T01:57:00', N'2026-09-08T02:42:00', N'2026-09-08T03:12:00', NULL, N'2026-09-08T02:55:00', N'2026-09-11T10:24:00', N'2026-09-14T04:44:00', NULL, NULL, 2380000, 0.3000, 714000, 0.1500, 1, NULL, NULL, N'2026-09-08T01:54:00', N'2026-09-14T04:44:00', 0, NULL, 0),
        (43, N'FJ260816-001', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 7, 17, 24, N'2026-09-05', N'2026-09-08', 3, NULL, N'Completed', N'2026-08-16T05:25:00', NULL, N'2026-08-16T05:16:00', N'2026-08-16T05:46:00', NULL, N'2026-08-16T05:26:00', N'2026-09-05T10:01:00', N'2026-09-08T04:06:00', NULL, NULL, 5120000, 0.3000, 1536000, 0.1000, 1, NULL, NULL, N'2026-08-16T05:10:00', N'2026-09-08T04:06:00', 0, NULL, 0),
        (44, N'FJ260925-001', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 24, 60, 84, N'2026-10-01', N'2026-10-02', 3, NULL, N'Cancelled', N'2026-09-25T06:09:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-09-25T05:57:00', N'Nhóm bạn không đủ người.', 960000, 0.3000, 288000, 0.1500, 1, 0, NULL, N'2026-09-25T05:54:00', N'2026-09-25T05:57:00', 0, NULL, 0),
        (45, N'FJ260904-002', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 15, 38, 53, N'2026-09-28', N'2026-10-01', 1, NULL, N'Completed', N'2026-09-04T02:19:00', NULL, N'2026-09-04T02:10:00', N'2026-09-04T02:40:00', NULL, N'2026-09-04T02:37:00', N'2026-09-28T08:10:00', N'2026-10-01T04:03:00', NULL, NULL, 960000, 0.3000, 288000, 0.1500, 1, NULL, NULL, N'2026-09-04T02:04:00', N'2026-10-01T04:03:00', 0, NULL, 0),
        (46, N'FJ260915-001', 9, N'Hồ Minh Khang', N'+84987000008', N'Phone', 5, 5, 24, 60, 84, N'2026-10-11', N'2026-10-13', 3, NULL, N'Expired', N'2026-09-15T04:58:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-09-15T04:59:00', NULL, 1920000, 0.3000, 576000, 0.1500, 1, NULL, NULL, N'2026-09-15T04:43:00', N'2026-09-15T04:59:00', 0, NULL, 0),
        (47, N'FJ260707-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Phone', 4, 4, 22, 55, 77, N'2026-07-18', N'2026-07-21', 4, NULL, N'NoShow', N'2026-07-07T04:22:00', NULL, N'2026-07-07T04:19:00', N'2026-07-07T04:49:00', NULL, N'2026-07-07T04:36:00', NULL, NULL, NULL, NULL, 2050000, 0.3000, 615000, 0.1000, 1, 0, NULL, N'2026-07-07T04:07:00', N'2026-07-18T16:00:00', 0, NULL, 0),
        (48, N'FJ260704-002', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 5, 5, 34, 88, 122, N'2026-07-05', N'2026-07-07', 2, NULL, N'Completed', N'2026-07-04T01:41:00', NULL, N'2026-07-04T01:35:00', N'2026-07-04T02:05:00', NULL, N'2026-07-04T02:01:00', N'2026-07-05T09:22:00', N'2026-07-07T04:34:00', NULL, NULL, 1800000, 0.3000, 540000, 0.1000, 1, NULL, NULL, N'2026-07-04T01:26:00', N'2026-07-07T04:34:00', 0, NULL, 0),
        (49, N'FJ260925-002', 7, N'Trịnh Văn Long', N'+84987000006', N'Phone', 4, 4, 4, 9, 13, N'2026-09-28', N'2026-09-29', 2, NULL, N'Completed', N'2026-09-25T04:47:00', NULL, N'2026-09-25T04:42:00', N'2026-09-25T05:12:00', NULL, N'2026-09-25T05:06:00', N'2026-09-28T08:04:00', N'2026-09-29T04:52:00', NULL, NULL, 700000, 0.3000, 210000, 0.1000, 1, NULL, NULL, N'2026-09-25T04:32:00', N'2026-09-29T04:52:00', 0, NULL, 0),
        (50, N'FJ260922-001', 2, N'Trần Thị Bích', N'+84987000002', N'Zalo', 4, 4, 2, 4, 6, N'2026-10-01', N'2026-10-04', 2, NULL, N'Completed', N'2026-09-22T03:39:00', NULL, N'2026-09-22T03:30:00', N'2026-09-22T04:00:00', NULL, N'2026-09-22T03:54:00', N'2026-10-01T08:56:00', N'2026-10-04T04:25:00', NULL, NULL, 1700000, 0.3000, 510000, 0.1200, 1, NULL, NULL, N'2026-09-22T03:24:00', N'2026-10-04T04:25:00', 0, NULL, 0),
        (51, N'FJ260801-001', 11, N'Mai Đức Anh', N'+84987000010', N'Web', NULL, NULL, 5, 12, 17, N'2026-08-03', N'2026-08-06', 3, NULL, N'Completed', N'2026-08-01T02:35:00', NULL, N'2026-08-01T02:28:00', N'2026-08-01T02:58:00', NULL, N'2026-08-01T02:36:00', N'2026-08-03T09:46:00', N'2026-08-06T04:15:00', NULL, NULL, 3840000, 0.3000, 1152000, 0.1200, 1, NULL, NULL, N'2026-08-01T02:20:00', N'2026-08-06T04:15:00', 0, NULL, 0),
        (52, N'FJ260629-002', 5, N'Bùi Quốc Bảo', N'+84987000004', N'Web', NULL, NULL, 6, 14, 19, N'2026-07-01', N'2026-07-04', 1, NULL, N'Completed', N'2026-06-29T05:03:00', NULL, N'2026-06-29T04:53:00', N'2026-06-29T05:23:00', NULL, N'2026-06-29T05:11:00', N'2026-07-01T07:50:00', N'2026-07-04T04:35:00', NULL, NULL, 2880000, 0.3000, 864000, 0.1500, 1, NULL, NULL, N'2026-06-29T04:48:00', N'2026-07-04T04:35:00', 0, NULL, 0),
        (53, N'FJ260817-001', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 3, 6, 9, N'2026-08-30', N'2026-09-02', 1, NULL, N'Cancelled', N'2026-08-17T01:49:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-08-17T01:42:00', N'Gia đình có việc đột xuất.', 2100000, 0.3000, 630000, 0.1500, 1, 0, NULL, N'2026-08-17T01:34:00', N'2026-08-17T01:42:00', 0, NULL, 0),
        (54, N'FJ260729-001', 15, N'Kiều Văn Phúc', N'+84987000014', N'Phone', 5, 5, 1, 2, 3, N'2026-08-16', N'2026-08-17', 3, NULL, N'Rejected', N'2026-07-29T02:44:00', NULL, N'2026-07-29T02:39:00', N'2026-07-29T03:09:00', NULL, NULL, NULL, NULL, N'2026-07-29T03:12:00', N'Chủ nhà bận việc gia đình, không đón khách được.', 640000, 0.3000, 192000, 0.1000, 1, 192000, NULL, N'2026-07-29T02:29:00', N'2026-07-29T03:12:00', 0, NULL, 0),
        (55, N'FJ261019-003', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 23, 56, 78, N'2026-12-25', N'2026-12-27', 1, NULL, N'Expired', N'2026-10-19T16:34:49.4411780', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-19T16:35:49.4411780', NULL, 1200000, 0.3000, 360000, 0.1200, 1, NULL, NULL, N'2026-10-19T16:19:49.4411780', N'2026-10-19T16:35:49.4411780', 0, NULL, 0),
        (56, N'FJ261020-010', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Phone', 5, 5, 30, 77, 107, N'2026-11-29', N'2026-11-30', 4, NULL, N'Confirmed', N'2026-10-20T07:34:49.4411780', NULL, N'2026-10-20T07:27:49.4411780', N'2026-10-20T07:57:49.4411780', NULL, N'2026-10-20T07:45:49.4411780', NULL, NULL, NULL, NULL, 800000, 0.3000, 240000, 0.1500, 1, NULL, NULL, N'2026-10-20T07:19:49.4411780', N'2026-10-20T07:45:49.4411780', 0, NULL, 0),
        (57, N'FJ260807-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 12, 30, 42, N'2026-09-06', N'2026-09-07', 3, NULL, N'Completed', N'2026-08-07T05:20:00', N'2026-08-07T05:14:00', N'2026-08-07T07:13:00', N'2026-08-07T07:43:00', NULL, N'2026-08-07T07:40:00', N'2026-09-06T09:30:00', N'2026-09-07T04:44:00', NULL, NULL, 1280000, 0.3000, 384000, 0.1500, 1, NULL, NULL, N'2026-08-07T05:05:00', N'2026-09-07T04:44:00', 0, NULL, 0),
        (58, N'FJ260715-002', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 32, 84, 116, N'2026-07-17', N'2026-07-19', 10, NULL, N'Completed', N'2026-07-15T03:10:00', NULL, N'2026-07-15T03:07:00', N'2026-07-15T03:37:00', NULL, N'2026-07-15T03:27:00', N'2026-07-17T10:59:00', N'2026-07-19T04:00:00', NULL, NULL, 6720000, 0.3000, 2016000, 0.1200, 1, NULL, NULL, N'2026-07-15T02:55:00', N'2026-07-19T04:00:00', 0, NULL, 0),
        (59, N'FJ261019-004', 4, N'Đặng Thị Thu', N'+84987000003', N'Zalo', 4, 4, 28, 72, 100, N'2026-11-18', N'2026-11-19', 9, NULL, N'Deposited', N'2026-10-19T06:34:49.4411780', NULL, N'2026-10-19T06:30:49.4411780', N'2026-10-19T08:08:49.4411780', N'2026-10-20T15:19:55.1029310', NULL, NULL, NULL, NULL, NULL, 4000000, 0.3000, 1200000, 0.1000, 1, NULL, NULL, N'2026-10-19T06:19:49.4411780', N'2026-10-19T07:38:49.4411780', 0, NULL, 0),
        (60, N'FJ260607-002', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 31, 79, 109, N'2026-06-12', N'2026-06-15', 2, NULL, N'Cancelled', N'2026-06-07T04:03:00', N'2026-06-07T03:57:00', N'2026-06-07T05:32:00', N'2026-06-07T06:02:00', NULL, N'2026-06-07T05:50:00', NULL, NULL, N'2026-06-07T06:07:27.4097985', N'Nhóm bạn không đủ người.', 2040000, 0.3000, 612000, 0.1000, 1, 306000, NULL, N'2026-06-07T03:48:00', N'2026-06-07T06:07:27.4097985', 0, NULL, 0);
    SET IDENTITY_INSERT [dbo].[Bookings] OFF;
    DBCC CHECKIDENT (N'[dbo].[Bookings]', RESEED, 60) WITH NO_INFOMSGS;

    -- RoomBlocks: 6 dòng.
    SET IDENTITY_INSERT [dbo].[RoomBlocks] ON;
    INSERT INTO [dbo].[RoomBlocks] ([Id], [RoomId], [StartDate], [EndDate], [Reason], [Note], [CreatedByAccountId], [CreatedAt]) VALUES
        (1, 57, N'2026-09-30', N'2026-10-03', N'Maintenance', N'Sơn lại tường, thay rèm cửa.', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'2026-09-23T13:00:00'),
        (2, 98, N'2026-10-22', N'2026-10-24', N'OtherChannel', N'Khách quen đặt qua Zalo của chủ nhà.', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', N'2026-10-15T13:00:00'),
        (3, 72, N'2026-10-25', N'2026-10-26', N'OtherChannel', N'Khách đặt qua Facebook.', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'2026-10-18T13:00:00'),
        (4, 127, N'2026-10-31', N'2026-11-04', N'Maintenance', N'Bảo dưỡng điều hòa và bình nóng lạnh.', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', N'2026-10-24T13:00:00'),
        (5, 62, N'2026-11-07', N'2026-11-09', N'OtherChannel', N'Đoàn khách công ty đặt trực tiếp.', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', N'2026-10-31T13:00:00'),
        (6, 96, N'2026-11-19', N'2026-11-22', N'OtherChannel', N'Khách đặt qua Facebook.', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'2026-11-12T13:00:00');
    SET IDENTITY_INSERT [dbo].[RoomBlocks] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomBlocks]', RESEED, 6) WITH NO_INFOMSGS;

    -- BankTransactions: 43 dòng.
    SET IDENTITY_INSERT [dbo].[BankTransactions] ON;
    INSERT INTO [dbo].[BankTransactions] ([Id], [Provider], [ProviderTransactionId], [BankReference], [AccountNumber], [Amount], [TransferContent], [TransactionTime], [ReceivedAt], [RawPayload], [MatchStatus], [MatchedBookingId], [ProcessedByEmployeeId], [ProcessedAt], [Note], [RefundBankRef], [RefundedAt]) VALUES
        (1, N'SePay', N'SEED-0042', NULL, N'0338405548', 450000, N'NGUYEN VAN A chuyen tien homestay', N'2026-10-19T19:19:49.4411780', N'2026-10-19T19:19:49.4411780', NULL, N'Unmatched', NULL, NULL, NULL, N'Không tìm thấy mã đơn trong nội dung chuyển khoản.', NULL, NULL),
        (2, N'SePay', N'SEED-0001', NULL, N'0338405548', 810000, N'FJ260929-001 dat coc', N'2026-09-29T05:43:00', N'2026-09-29T05:43:00', NULL, N'Matched', 1, NULL, NULL, NULL, NULL, NULL),
        (3, N'SePay', N'SEED-0002', NULL, N'0338405548', 96000, N'FJ261020-001 dat coc', N'2026-10-20T15:02:49.4411780', N'2026-10-20T15:02:49.4411780', NULL, N'Matched', 3, NULL, NULL, NULL, NULL, NULL),
        (4, N'SePay', N'SEED-0003', NULL, N'0338405548', 240000, N'FJ261020-002 dat coc', N'2026-10-20T14:24:49.4411780', N'2026-10-20T14:24:49.4411780', NULL, N'Matched', 4, NULL, NULL, NULL, NULL, NULL),
        (5, N'SePay', N'SEED-0004', NULL, N'0338405548', 960000, N'FJ260601-001', N'2026-06-01T05:34:00', N'2026-06-01T05:34:00', NULL, N'Matched', 7, NULL, NULL, NULL, NULL, NULL),
        (6, N'SePay', N'SEED-0005', NULL, N'0338405548', 432000, N'FJ260825-001 dat coc', N'2026-08-25T01:27:00', N'2026-08-25T01:27:00', NULL, N'Matched', 8, NULL, NULL, NULL, NULL, NULL),
        (7, N'SePay', N'SEED-0006', NULL, N'0338405548', 2400000, N'FJ261020-005', N'2026-10-20T10:24:49.4411780', N'2026-10-20T10:24:49.4411780', NULL, N'Matched', 9, NULL, NULL, NULL, NULL, NULL),
        (8, N'SePay', N'SEED-0007', NULL, N'0338405548', 2400000, N'MBVCB.560275.FJ260804001.CT tu Đinh Thị Yến', N'2026-08-04T01:52:00', N'2026-08-04T01:52:00', NULL, N'Matched', 10, NULL, NULL, NULL, NULL, NULL),
        (9, N'SePay', N'SEED-0008', NULL, N'0338405548', 231000, N'MBVCB.462430.FJ260705001.CT tu Lê Văn Hùng', N'2026-07-05T05:02:00', N'2026-07-05T05:02:00', NULL, N'Matched', 11, NULL, NULL, NULL, NULL, NULL),
        (10, N'SePay', N'SEED-0009', NULL, N'0338405548', 432000, N'MBVCB.428352.FJ260713001.CT tu Hồ Minh Khang', N'2026-07-13T03:40:00', N'2026-07-13T03:40:00', NULL, N'Matched', 12, NULL, NULL, NULL, NULL, NULL),
        (11, N'SePay', N'SEED-0010', NULL, N'0338405548', 240000, N'MBVCB.948530.FJ261018001.CT tu Lý Thanh Tâm', N'2026-10-18T06:21:49.4411780', N'2026-10-18T06:21:49.4411780', NULL, N'Matched', 14, NULL, NULL, NULL, NULL, NULL),
        (12, N'SePay', N'SEED-0011', NULL, N'0338405548', 408000, N'FJ261020-006', N'2026-10-20T04:21:49.4411780', N'2026-10-20T04:21:49.4411780', NULL, N'Matched', 15, NULL, NULL, NULL, NULL, NULL),
        (13, N'SePay', N'SEED-0012', NULL, N'0338405548', 768000, N'MBVCB.901086.FJ260601002.CT tu Phan Ngọc Ánh', N'2026-06-01T04:49:00', N'2026-06-01T04:49:00', NULL, N'Matched', 16, NULL, NULL, NULL, NULL, NULL),
        (14, N'SePay', N'SEED-0013', NULL, N'0338405548', 384000, N'FJ261019-001', N'2026-10-19T02:28:00', N'2026-10-19T02:28:00', NULL, N'Matched', 17, NULL, NULL, NULL, NULL, NULL),
        (15, N'SePay', N'SEED-0014', NULL, N'0338405548', 576000, N'MBVCB.937809.FJ260601003.CT tu Nguyễn Văn Minh', N'2026-06-01T03:32:00', N'2026-06-01T03:32:00', NULL, N'Matched', 18, NULL, NULL, NULL, NULL, NULL),
        (16, N'SePay', N'SEED-0015', NULL, N'0338405548', 1230000, N'FJ260607-001 dat coc', N'2026-06-07T05:19:00', N'2026-06-07T05:19:00', NULL, N'Matched', 21, NULL, NULL, NULL, NULL, NULL),
        (17, N'SePay', N'SEED-0016', NULL, N'0338405548', 1296000, N'MBVCB.819025.FJ261020007.CT tu Lê Văn Hùng', N'2026-10-20T07:28:49.4411780', N'2026-10-20T07:28:49.4411780', NULL, N'Matched', 22, NULL, NULL, NULL, NULL, NULL),
        (18, N'SePay', N'SEED-0017', NULL, N'0338405548', 576000, N'MBVCB.374663.FJ261019002.CT tu Võ Thị Hồng', N'2026-10-18T19:22:49.4411780', N'2026-10-18T19:22:49.4411780', NULL, N'Matched', 23, NULL, NULL, NULL, NULL, NULL),
        (19, N'SePay', N'SEED-0018', NULL, N'0338405548', 1296000, N'FJ260710-001 dat coc', N'2026-07-10T05:45:00', N'2026-07-10T05:45:00', NULL, N'Matched', 24, NULL, NULL, NULL, NULL, NULL),
        (20, N'SePay', N'SEED-0019', NULL, N'0338405548', 672000, N'MBVCB.678783.FJ260627001.CT tu Đặng Thị Thu', N'2026-06-27T04:40:00', N'2026-06-27T04:40:00', NULL, N'Matched', 26, NULL, NULL, NULL, NULL, NULL),
        (21, N'SePay', N'SEED-0021', NULL, N'0338405548', 1140000, N'FJ260712-001', N'2026-07-12T01:15:00', N'2026-07-12T01:15:00', NULL, N'Matched', 29, NULL, NULL, NULL, NULL, NULL),
        (22, N'SePay', N'SEED-0023', NULL, N'0338405548', 510000, N'MBVCB.384859.FJ260827001.CT tu Chu Thị Ngân', N'2026-08-27T03:42:00', N'2026-08-27T03:42:00', NULL, N'Matched', 32, NULL, NULL, NULL, NULL, NULL),
        (23, N'SePay', N'SEED-0024', NULL, N'0338405548', 519000, N'MBVCB.480041.FJ260923001.CT tu Trần Thị Bích', N'2026-09-23T03:46:00', N'2026-09-23T03:46:00', NULL, N'Matched', 34, NULL, NULL, NULL, NULL, NULL),
        (24, N'SePay', N'SEED-0025', NULL, N'0338405548', 600000, N'FJ261018-003 dat coc', N'2026-10-17T20:26:49.4411780', N'2026-10-17T20:26:49.4411780', NULL, N'Matched', 36, NULL, NULL, NULL, NULL, NULL),
        (25, N'SePay', N'SEED-0026', NULL, N'0338405548', 168000, N'MBVCB.172576.FJ260601004.CT tu Tạ Quang Vinh', N'2026-06-01T03:25:00', N'2026-06-01T03:25:00', NULL, N'Matched', 37, NULL, NULL, NULL, NULL, NULL),
        (26, N'SePay', N'SEED-0027', NULL, N'0338405548', 1440000, N'FJ261018-004 dat coc', N'2026-10-18T16:30:49.4411780', N'2026-10-18T16:30:49.4411780', NULL, N'Matched', 39, NULL, NULL, NULL, NULL, NULL),
        (27, N'SePay', N'SEED-0028', NULL, N'0338405548', 480000, N'MBVCB.838236.FJ260715001.CT tu Lê Văn Hùng', N'2026-07-15T03:06:00', N'2026-07-15T03:06:00', NULL, N'Matched', 40, NULL, NULL, NULL, NULL, NULL),
        (28, N'SePay', N'SEED-0029', NULL, N'0338405548', 210000, N'FJ260629-001', N'2026-06-29T01:47:00', N'2026-06-29T01:47:00', NULL, N'Matched', 41, NULL, NULL, NULL, NULL, NULL),
        (29, N'SePay', N'SEED-0030', NULL, N'0338405548', 1536000, N'FJ260816-001', N'2026-08-16T05:16:00', N'2026-08-16T05:16:00', NULL, N'Matched', 43, NULL, NULL, NULL, NULL, NULL),
        (30, N'SePay', N'SEED-0031', NULL, N'0338405548', 288000, N'MBVCB.951313.FJ260904002.CT tu Đinh Thị Yến', N'2026-09-04T02:10:00', N'2026-09-04T02:10:00', NULL, N'Matched', 45, NULL, NULL, NULL, NULL, NULL),
        (31, N'SePay', N'SEED-0032', NULL, N'0338405548', 615000, N'FJ260707-001', N'2026-07-07T04:19:00', N'2026-07-07T04:19:00', NULL, N'Matched', 47, NULL, NULL, NULL, NULL, NULL),
        (32, N'SePay', N'SEED-0033', NULL, N'0338405548', 540000, N'FJ260704-002', N'2026-07-04T01:35:00', N'2026-07-04T01:35:00', NULL, N'Matched', 48, NULL, NULL, NULL, NULL, NULL),
        (33, N'SePay', N'SEED-0034', NULL, N'0338405548', 210000, N'FJ260925-002', N'2026-09-25T04:42:00', N'2026-09-25T04:42:00', NULL, N'Matched', 49, NULL, NULL, NULL, NULL, NULL),
        (34, N'SePay', N'SEED-0035', NULL, N'0338405548', 510000, N'FJ260922-001', N'2026-09-22T03:30:00', N'2026-09-22T03:30:00', NULL, N'Matched', 50, NULL, NULL, NULL, NULL, NULL),
        (35, N'SePay', N'SEED-0036', NULL, N'0338405548', 1152000, N'MBVCB.961891.FJ260801001.CT tu Mai Đức Anh', N'2026-08-01T02:28:00', N'2026-08-01T02:28:00', NULL, N'Matched', 51, NULL, NULL, NULL, NULL, NULL),
        (36, N'SePay', N'SEED-0037', NULL, N'0338405548', 864000, N'FJ260629-002', N'2026-06-29T04:53:00', N'2026-06-29T04:53:00', NULL, N'Matched', 52, NULL, NULL, NULL, NULL, NULL),
        (37, N'SePay', N'SEED-0038', NULL, N'0338405548', 192000, N'FJ260729-001', N'2026-07-29T02:39:00', N'2026-07-29T02:39:00', NULL, N'Matched', 54, NULL, NULL, NULL, NULL, NULL),
        (38, N'SePay', N'SEED-0039', NULL, N'0338405548', 240000, N'FJ261020-010 dat coc', N'2026-10-20T07:27:49.4411780', N'2026-10-20T07:27:49.4411780', NULL, N'Matched', 56, NULL, NULL, NULL, NULL, NULL),
        (39, N'SePay', N'SEED-0040', NULL, N'0338405548', 2016000, N'FJ260715-002', N'2026-07-15T03:07:00', N'2026-07-15T03:07:00', NULL, N'Matched', 58, NULL, NULL, NULL, NULL, NULL),
        (40, N'SePay', N'SEED-0041', NULL, N'0338405548', 1200000, N'MBVCB.974092.FJ261019004.CT tu Đặng Thị Thu', N'2026-10-19T06:30:49.4411780', N'2026-10-19T06:30:49.4411780', NULL, N'Matched', 59, NULL, NULL, NULL, NULL, NULL),
        (41, N'SePay', N'SEED-0020', NULL, N'0338405548', 718000, N'FJ260912-002 dat coc', N'2026-09-12T05:50:00', N'2026-09-12T05:50:00', NULL, N'Underpaid', 28, NULL, NULL, N'Thiếu 50.000đ so với tiền cọc.', NULL, NULL),
        (42, N'SePay', N'SEED-0022', NULL, N'0338405548', 810000, N'FJ260621-002 dat coc', N'2026-06-21T06:21:00', N'2026-06-21T06:21:00', NULL, N'LatePayment', 30, NULL, NULL, NULL, NULL, NULL),
        (43, N'SePay', N'SEED-R001', NULL, N'0338405548', 300000, N'TRAN THI HOA chuyen tien', N'2026-10-05T15:19:52.6361690', N'2026-10-05T15:19:52.6361690', NULL, N'Refunded', NULL, 3, N'2026-10-06T15:19:52.6361690', N'Khách chuyển nhầm tài khoản, đã gọi xác nhận và chuyển trả.', N'FT2610060001', N'2026-10-06T15:19:52.6361690');
    SET IDENTITY_INSERT [dbo].[BankTransactions] OFF;
    DBCC CHECKIDENT (N'[dbo].[BankTransactions]', RESEED, 43) WITH NO_INFOMSGS;

    -- BookingChangeRequests: 5 dòng.
    SET IDENTITY_INSERT [dbo].[BookingChangeRequests] ON;
    INSERT INTO [dbo].[BookingChangeRequests] ([Id], [BookingId], [RequestType], [RequestedByType], [NewCheckIn], [NewCheckOut], [NewRoomTypeId], [PriceDifference], [RefundAmount], [Status], [DecidedByAccountId], [DecidedAt], [DecisionNote], [CreatedAt], [AdditionalDeposit], [ChangeFee], [DepositCarriedOver], [FinanceConfirmedAt], [FinanceConfirmedByAccountId], [FinanceNote], [NewRoomId], [NewTotalAmount], [PaymentDeadlineAt], [RejectionReason], [RequestedByAccountId]) VALUES
        (1, 15, N'ChangeDates', N'Customer', N'2026-10-30', N'2026-11-02', NULL, 0, NULL, N'Pending', NULL, NULL, NULL, N'2026-10-20T12:19:52.6361690', 204000, 204000, 204000, NULL, NULL, NULL, NULL, 1360000, NULL, NULL, N'89010c48-12f1-4c0d-b829-c1679b02e399'),
        (2, 22, N'ChangeDates', N'Employee', N'2026-11-10', N'2026-11-13', NULL, 0, NULL, N'Rejected', N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'2026-10-19T11:19:52.6361690', N'Ngày mới homestay đã kín phòng, mong khách thông cảm.', N'2026-10-19T09:19:52.6361690', 0, 0, 1296000, NULL, NULL, NULL, NULL, 4320000, NULL, N'Ngày mới homestay đã kín phòng, mong khách thông cảm.', N'0f21ecd9-e567-4aa2-9971-f1049ecec930'),
        (3, 59, N'ChangeDates', N'Customer', N'2026-11-18', N'2026-11-19', NULL, 0, NULL, N'Applied', N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'2026-10-19T07:38:49.4411780', N'Đồng ý đổi ngày, không cần nộp thêm cọc.', N'2026-10-19T06:38:49.4411780', 0, 0, 1200000, NULL, NULL, NULL, 100, 4000000, NULL, NULL, N'62b20678-2da9-465f-b9cc-595ed1710f0d'),
        (4, 56, N'ChangeRoomType', N'Customer', NULL, NULL, 78, 1200000, NULL, N'Pending', NULL, NULL, NULL, N'2026-10-20T10:19:52.6361690', 360000, 0, 240000, NULL, NULL, NULL, NULL, 2000000, NULL, NULL, N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e'),
        (5, 36, N'Cancel', N'Employee', NULL, NULL, NULL, NULL, 600000, N'Pending', NULL, NULL, NULL, N'2026-10-20T14:19:52.6361690', 0, 0, 0, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'0f21ecd9-e567-4aa2-9971-f1049ecec930');
    SET IDENTITY_INSERT [dbo].[BookingChangeRequests] OFF;
    DBCC CHECKIDENT (N'[dbo].[BookingChangeRequests]', RESEED, 5) WITH NO_INFOMSGS;

    -- BookingNights: 122 dòng.
    INSERT INTO [dbo].[BookingNights] ([BookingId], [Date], [DayType], [Price]) VALUES
        (1, N'2026-10-19', N'Weekday', 900000),
        (1, N'2026-10-20', N'Weekday', 900000),
        (1, N'2026-10-21', N'Weekday', 900000),
        (2, N'2026-10-20', N'Weekday', 1120000),
        (2, N'2026-10-21', N'Weekday', 1120000),
        (2, N'2026-10-22', N'Weekday', 1120000),
        (3, N'2026-10-29', N'Weekday', 320000),
        (4, N'2026-11-05', N'Weekday', 800000),
        (5, N'2026-11-01', N'Weekday', 4000000),
        (5, N'2026-11-02', N'Weekday', 4000000),
        (6, N'2026-11-09', N'Weekday', 800000),
        (7, N'2026-06-07', N'Weekday', 1600000),
        (7, N'2026-06-08', N'Weekday', 1600000),
        (8, N'2026-09-15', N'Weekday', 1440000),
        (9, N'2026-12-20', N'Weekday', 4000000),
        (9, N'2026-12-21', N'Weekday', 4000000),
        (10, N'2026-08-31', N'Weekday', 3200000),
        (10, N'2026-09-01', N'Holiday', 4800000),
        (11, N'2026-07-24', N'Weekend', 770000),
        (12, N'2026-08-09', N'Weekday', 1440000),
        (13, N'2026-06-27', N'Weekend', 480000),
        (14, N'2026-11-18', N'Weekday', 800000),
        (15, N'2026-10-23', N'Weekend', 480000),
        (15, N'2026-10-24', N'Weekend', 480000),
        (15, N'2026-10-25', N'Weekday', 400000),
        (16, N'2026-06-10', N'Weekday', 800000),
        (16, N'2026-06-11', N'Weekday', 800000),
        (16, N'2026-06-12', N'Weekend', 960000),
        (17, N'2026-10-25', N'Weekday', 640000),
        (17, N'2026-10-26', N'Weekday', 640000),
        (18, N'2026-06-01', N'Weekday', 640000),
        (18, N'2026-06-02', N'Weekday', 640000),
        (18, N'2026-06-03', N'Weekday', 640000),
        (19, N'2026-09-14', N'Weekday', 1600000),
        (20, N'2026-12-08', N'Weekday', 800000),
        (20, N'2026-12-09', N'Weekday', 800000),
        (20, N'2026-12-10', N'Weekday', 800000),
        (21, N'2026-07-04', N'Weekend', 1540000),
        (21, N'2026-07-05', N'Weekday', 1280000),
        (21, N'2026-07-06', N'Weekday', 1280000),
        (22, N'2026-11-09', N'Weekday', 1440000),
        (22, N'2026-11-10', N'Weekday', 1440000),
        (22, N'2026-11-11', N'Weekday', 1440000),
        (23, N'2026-11-23', N'Weekday', 960000),
        (23, N'2026-11-24', N'Weekday', 960000),
        (24, N'2026-07-13', N'Weekday', 1440000),
        (24, N'2026-07-14', N'Weekday', 1440000),
        (24, N'2026-07-15', N'Weekday', 1440000),
        (25, N'2026-12-02', N'Weekday', 600000),
        (26, N'2026-07-19', N'Weekday', 1120000),
        (26, N'2026-07-20', N'Weekday', 1120000),
        (27, N'2026-11-19', N'Weekday', 400000),
        (28, N'2026-09-27', N'Weekday', 1280000),
        (28, N'2026-09-28', N'Weekday', 1280000),
        (29, N'2026-08-07', N'Weekend', 1340000),
        (29, N'2026-08-08', N'Weekend', 1340000),
        (29, N'2026-08-09', N'Weekday', 1120000),
        (30, N'2026-06-30', N'Weekday', 900000),
        (30, N'2026-07-01', N'Weekday', 900000),
        (30, N'2026-07-02', N'Weekday', 900000),
        (31, N'2026-09-07', N'Weekday', 1280000),
        (31, N'2026-09-08', N'Weekday', 1280000),
        (31, N'2026-09-09', N'Weekday', 1280000),
        (32, N'2026-09-25', N'Weekend', 600000),
        (32, N'2026-09-26', N'Weekend', 600000),
        (32, N'2026-09-27', N'Weekday', 500000),
        (33, N'2026-12-04', N'Weekend', 1920000),
        (33, N'2026-12-05', N'Weekend', 1920000),
        (34, N'2026-10-02', N'Weekend', 1730000),
        (35, N'2026-09-20', N'Weekday', 640000),
        (35, N'2026-09-21', N'Weekday', 640000),
        (35, N'2026-09-22', N'Weekday', 640000),
        (36, N'2026-12-03', N'Weekday', 2000000),
        (37, N'2026-06-11', N'Weekday', 560000),
        (38, N'2026-07-17', N'Weekend', 770000),
        (38, N'2026-07-18', N'Weekend', 770000),
        (39, N'2026-12-22', N'Weekday', 2400000),
        (39, N'2026-12-23', N'Weekday', 2400000),
        (40, N'2026-08-05', N'Weekday', 800000),
        (40, N'2026-08-06', N'Weekday', 800000),
        (41, N'2026-07-02', N'Weekday', 700000),
        (42, N'2026-09-11', N'Weekend', 840000),
        (42, N'2026-09-12', N'Weekend', 840000),
        (42, N'2026-09-13', N'Weekday', 700000),
        (43, N'2026-09-05', N'Weekend', 1920000),
        (43, N'2026-09-06', N'Weekday', 1600000),
        (43, N'2026-09-07', N'Weekday', 1600000),
        (44, N'2026-10-01', N'Weekday', 960000),
        (45, N'2026-09-28', N'Weekday', 320000),
        (45, N'2026-09-29', N'Weekday', 320000),
        (45, N'2026-09-30', N'Weekday', 320000),
        (46, N'2026-10-11', N'Weekday', 960000),
        (46, N'2026-10-12', N'Weekday', 960000),
        (47, N'2026-07-18', N'Weekend', 770000),
        (47, N'2026-07-19', N'Weekday', 640000),
        (47, N'2026-07-20', N'Weekday', 640000),
        (48, N'2026-07-05', N'Weekday', 900000),
        (48, N'2026-07-06', N'Weekday', 900000),
        (49, N'2026-09-28', N'Weekday', 700000),
        (50, N'2026-10-01', N'Weekday', 500000);
    INSERT INTO [dbo].[BookingNights] ([BookingId], [Date], [DayType], [Price]) VALUES
        (50, N'2026-10-02', N'Weekend', 600000),
        (50, N'2026-10-03', N'Weekend', 600000),
        (51, N'2026-08-03', N'Weekday', 1280000),
        (51, N'2026-08-04', N'Weekday', 1280000),
        (51, N'2026-08-05', N'Weekday', 1280000),
        (52, N'2026-07-01', N'Weekday', 900000),
        (52, N'2026-07-02', N'Weekday', 900000),
        (52, N'2026-07-03', N'Weekend', 1080000),
        (53, N'2026-08-30', N'Weekday', 600000),
        (53, N'2026-08-31', N'Weekday', 600000),
        (53, N'2026-09-01', N'Holiday', 900000),
        (54, N'2026-08-16', N'Weekday', 640000),
        (55, N'2026-12-25', N'Weekend', 600000),
        (55, N'2026-12-26', N'Weekend', 600000),
        (56, N'2026-11-29', N'Weekday', 800000),
        (57, N'2026-09-06', N'Weekday', 1280000),
        (58, N'2026-07-17', N'Weekend', 3360000),
        (58, N'2026-07-18', N'Weekend', 3360000),
        (59, N'2026-11-18', N'Weekday', 4000000),
        (60, N'2026-06-12', N'Weekend', 720000),
        (60, N'2026-06-13', N'Weekend', 720000),
        (60, N'2026-06-14', N'Weekday', 600000);

    -- BookingStatusHistories: 238 dòng.
    SET IDENTITY_INSERT [dbo].[BookingStatusHistories] ON;
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (1, 1, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-29T05:37:00'),
        (2, 1, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0001.', N'2026-09-29T05:43:00'),
        (3, 1, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-09-29T05:55:00'),
        (4, 1, N'Confirmed', N'CheckedIn', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-19T09:02:00'),
        (5, 2, NULL, N'AwaitingPayment', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', NULL, N'2026-10-08T05:15:00'),
        (6, 2, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'Khách gửi ảnh bill.', N'2026-10-08T05:17:00'),
        (7, 2, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-10-08T07:36:00'),
        (8, 2, N'Deposited', N'Confirmed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-10-08T07:44:00'),
        (9, 2, N'Confirmed', N'CheckedIn', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-10-20T07:27:00'),
        (10, 3, NULL, N'AwaitingPayment', N'Customer', N'62b20678-2da9-465f-b9cc-595ed1710f0d', NULL, N'2026-10-20T14:57:49.4411780'),
        (11, 3, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0002.', N'2026-10-20T15:02:49.4411780'),
        (12, 4, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-10-20T14:19:49.4411780'),
        (13, 4, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0003.', N'2026-10-20T14:24:49.4411780'),
        (14, 5, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-10-20T14:44:49.4411780'),
        (15, 5, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'Khách gửi ảnh bill.', N'2026-10-20T14:49:49.4411780'),
        (16, 6, NULL, N'AwaitingPayment', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', NULL, N'2026-10-20T12:09:49.4411780'),
        (17, 6, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'Khách gửi ảnh bill.', N'2026-10-20T12:14:49.4411780'),
        (18, 7, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-06-01T05:25:00'),
        (19, 7, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0004.', N'2026-06-01T05:34:00'),
        (20, 7, N'Deposited', N'Rejected', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'Chủ nhà bận việc gia đình, không đón khách được.', N'2026-06-01T06:03:00'),
        (21, 8, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-08-25T01:21:00'),
        (22, 8, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0005.', N'2026-08-25T01:27:00'),
        (23, 8, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-08-25T01:50:00'),
        (24, 8, N'Confirmed', N'NoShow', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'Khách không đến nhận phòng.', N'2026-09-15T16:00:00'),
        (25, 9, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-10-20T10:19:49.4411780'),
        (26, 9, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0006.', N'2026-10-20T10:24:49.4411780'),
        (27, 9, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-20T10:42:49.4411780'),
        (28, 10, NULL, N'AwaitingPayment', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', NULL, N'2026-08-04T01:48:00'),
        (29, 10, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0007.', N'2026-08-04T01:52:00'),
        (30, 10, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-08-04T02:20:00'),
        (31, 10, N'Confirmed', N'Cancelled', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'Khách hủy trước 4 ngày: Đặt nhầm ngày.', N'2026-08-26T21:01:15.0358745'),
        (32, 11, NULL, N'AwaitingPayment', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-07-05T04:55:00'),
        (33, 11, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0008.', N'2026-07-05T05:02:00'),
        (34, 11, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-07-05T05:21:00'),
        (35, 11, N'Confirmed', N'CheckedIn', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-07-24T08:22:00'),
        (36, 11, N'CheckedIn', N'Completed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-07-25T04:17:00'),
        (37, 12, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-07-13T03:30:00'),
        (38, 12, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0009.', N'2026-07-13T03:40:00'),
        (39, 12, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-07-13T04:00:00'),
        (40, 12, N'Confirmed', N'CheckedIn', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-08-09T08:49:00'),
        (41, 12, N'CheckedIn', N'Completed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-08-10T04:58:00'),
        (42, 13, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-06-21T04:07:00'),
        (43, 13, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'Khách gửi ảnh bill.', N'2026-06-21T04:17:00'),
        (44, 13, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-06-21T05:16:00'),
        (45, 13, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-06-21T05:22:00'),
        (46, 13, N'Confirmed', N'CheckedIn', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-06-27T09:09:00'),
        (47, 13, N'CheckedIn', N'Completed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-06-28T04:53:00'),
        (48, 14, NULL, N'AwaitingPayment', N'Customer', N'89010c48-12f1-4c0d-b829-c1679b02e399', NULL, N'2026-10-18T06:19:49.4411780'),
        (49, 14, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0010.', N'2026-10-18T06:21:49.4411780'),
        (50, 14, N'Deposited', N'Rejected', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', N'2026-10-18T06:37:49.4411780'),
        (51, 15, NULL, N'AwaitingPayment', N'Customer', N'89010c48-12f1-4c0d-b829-c1679b02e399', NULL, N'2026-10-20T04:19:49.4411780'),
        (52, 15, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0011.', N'2026-10-20T04:21:49.4411780'),
        (53, 15, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-20T04:28:49.4411780'),
        (54, 16, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-06-01T04:37:00'),
        (55, 16, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0012.', N'2026-06-01T04:49:00'),
        (56, 16, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-06-01T04:55:00'),
        (57, 16, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-06-10T10:03:00'),
        (58, 16, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-06-13T04:40:00'),
        (59, 17, NULL, N'AwaitingPayment', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-19T02:26:00'),
        (60, 17, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0013.', N'2026-10-19T02:28:00'),
        (61, 17, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-10-19T02:39:00'),
        (62, 17, N'Confirmed', N'Cancelled', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'Khách hủy trước 5 ngày: Thay đổi lịch công tác.', N'2026-10-19T21:11:54.1590931'),
        (63, 18, NULL, N'AwaitingPayment', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', NULL, N'2026-06-01T03:23:00'),
        (64, 18, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0014.', N'2026-06-01T03:32:00'),
        (65, 18, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-06-01T03:37:00'),
        (66, 18, N'Confirmed', N'CheckedIn', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-06-01T08:56:00'),
        (67, 18, N'CheckedIn', N'Completed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-06-04T04:21:00'),
        (68, 19, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-09-12T03:59:00'),
        (69, 19, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-09-12T04:05:00'),
        (70, 19, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-09-12T06:25:00'),
        (71, 19, N'Deposited', N'Confirmed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-12T06:36:00'),
        (72, 19, N'Confirmed', N'CheckedIn', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-14T07:59:00'),
        (73, 19, N'CheckedIn', N'Completed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-15T04:44:00'),
        (74, 20, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-10-17T15:19:49.4411780'),
        (75, 20, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-10-17T15:30:49.4411780'),
        (76, 20, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-10-17T16:47:49.4411780'),
        (77, 20, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-17T17:12:49.4411780'),
        (78, 21, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-06-07T05:13:00'),
        (79, 21, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0015.', N'2026-06-07T05:19:00'),
        (80, 21, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-06-07T05:24:00'),
        (81, 21, N'Confirmed', N'CheckedIn', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-07-04T10:07:00'),
        (82, 21, N'CheckedIn', N'Completed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-07-07T04:34:00'),
        (83, 22, NULL, N'AwaitingPayment', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-20T07:19:49.4411780'),
        (84, 22, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0016.', N'2026-10-20T07:28:49.4411780'),
        (85, 22, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-10-20T07:55:49.4411780'),
        (86, 23, NULL, N'AwaitingPayment', N'Customer', N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', NULL, N'2026-10-18T19:19:49.4411780'),
        (87, 23, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0017.', N'2026-10-18T19:22:49.4411780'),
        (88, 23, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-18T19:33:49.4411780'),
        (89, 23, N'Confirmed', N'Cancelled', N'Customer', N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', N'Khách hủy trước 34 ngày: Gia đình có việc đột xuất.', N'2026-10-20T01:10:45.9581452'),
        (90, 24, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-07-10T05:40:00'),
        (91, 24, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0018.', N'2026-07-10T05:45:00'),
        (92, 24, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-07-10T05:54:00'),
        (93, 24, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-07-13T10:46:00'),
        (94, 24, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-07-16T04:48:00'),
        (95, 25, NULL, N'AwaitingPayment', N'Customer', N'a9099925-9567-4dda-8121-d17926c1675e', NULL, N'2026-10-20T08:19:49.4411780'),
        (96, 25, N'AwaitingPayment', N'Cancelled', N'Customer', N'a9099925-9567-4dda-8121-d17926c1675e', N'Khách tự hủy: Chuyển sang đi tỉnh khác.', N'2026-10-20T08:27:49.4411780'),
        (97, 26, NULL, N'AwaitingPayment', N'Customer', N'62b20678-2da9-465f-b9cc-595ed1710f0d', NULL, N'2026-06-27T04:31:00'),
        (98, 26, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0019.', N'2026-06-27T04:40:00'),
        (99, 26, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-06-27T04:56:00'),
        (100, 26, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-07-19T09:17:00');
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (101, 26, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-07-21T04:26:00'),
        (102, 27, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-10-20T07:19:49.4411780'),
        (103, 27, N'AwaitingPayment', N'Cancelled', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', N'Khách tự hủy: Chuyển sang đi tỉnh khác.', N'2026-10-20T07:26:49.4411780'),
        (104, 28, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-12T05:42:00'),
        (105, 28, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-09-12T05:58:00'),
        (106, 29, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-07-12T01:07:00'),
        (107, 29, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0021.', N'2026-07-12T01:15:00'),
        (108, 29, N'Deposited', N'Confirmed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-07-12T01:40:00'),
        (109, 29, N'Confirmed', N'CheckedIn', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-08-07T09:34:00'),
        (110, 29, N'CheckedIn', N'Completed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-08-10T04:23:00'),
        (111, 30, NULL, N'AwaitingPayment', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', NULL, N'2026-06-21T05:25:00'),
        (112, 30, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-06-21T05:41:00'),
        (113, 31, NULL, N'AwaitingPayment', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', NULL, N'2026-08-09T05:27:00'),
        (114, 31, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', N'Khách gửi ảnh bill.', N'2026-08-09T05:33:00'),
        (115, 31, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-08-09T06:43:00'),
        (116, 31, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-08-09T06:58:00'),
        (117, 31, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-07T10:45:00'),
        (118, 31, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-10T04:06:00'),
        (119, 32, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-08-27T03:30:00'),
        (120, 32, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0023.', N'2026-08-27T03:42:00'),
        (121, 32, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-08-27T04:02:00'),
        (122, 32, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-25T07:20:00'),
        (123, 32, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-28T04:41:00'),
        (124, 33, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-10-18T16:19:49.4411780'),
        (125, 33, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-18T16:35:49.4411780'),
        (126, 34, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-23T03:35:00'),
        (127, 34, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0024.', N'2026-09-23T03:46:00'),
        (128, 34, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-23T04:14:00'),
        (129, 34, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-02T10:10:00'),
        (130, 34, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-03T04:51:00'),
        (131, 35, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-04T02:41:00'),
        (132, 35, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-09-04T02:45:00'),
        (133, 35, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-09-04T03:24:00'),
        (134, 35, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-04T03:45:00'),
        (135, 35, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-20T07:20:00'),
        (136, 35, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-23T04:54:00'),
        (137, 36, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-10-17T20:19:49.4411780'),
        (138, 36, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0025.', N'2026-10-17T20:26:49.4411780'),
        (139, 36, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-17T20:34:49.4411780'),
        (140, 37, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-06-01T03:21:00'),
        (141, 37, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0026.', N'2026-06-01T03:25:00'),
        (142, 37, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-06-01T03:52:00'),
        (143, 37, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-06-11T10:28:00'),
        (144, 37, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-06-12T04:02:00'),
        (145, 38, NULL, N'AwaitingPayment', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', NULL, N'2026-07-04T05:55:00'),
        (146, 38, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-07-04T06:11:00'),
        (147, 39, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-10-18T16:19:49.4411780'),
        (148, 39, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0027.', N'2026-10-18T16:30:49.4411780'),
        (149, 39, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-10-18T16:47:49.4411780'),
        (150, 40, NULL, N'AwaitingPayment', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-07-15T02:54:00'),
        (151, 40, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0028.', N'2026-07-15T03:06:00'),
        (152, 40, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-15T03:20:00'),
        (153, 40, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-08-05T09:55:00'),
        (154, 40, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-08-07T04:14:00'),
        (155, 41, NULL, N'AwaitingPayment', N'Customer', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', NULL, N'2026-06-29T01:41:00'),
        (156, 41, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0029.', N'2026-06-29T01:47:00'),
        (157, 41, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-06-29T01:55:00'),
        (158, 41, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-02T10:34:00'),
        (159, 41, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-03T04:33:00'),
        (160, 42, NULL, N'AwaitingPayment', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', NULL, N'2026-09-08T01:54:00'),
        (161, 42, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'Khách gửi ảnh bill.', N'2026-09-08T01:57:00'),
        (162, 42, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-09-08T02:42:00'),
        (163, 42, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-09-08T02:55:00'),
        (164, 42, N'Confirmed', N'CheckedIn', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-09-11T10:24:00'),
        (165, 42, N'CheckedIn', N'Completed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-09-14T04:44:00'),
        (166, 43, NULL, N'AwaitingPayment', N'Customer', N'89010c48-12f1-4c0d-b829-c1679b02e399', NULL, N'2026-08-16T05:10:00'),
        (167, 43, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0030.', N'2026-08-16T05:16:00'),
        (168, 43, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-08-16T05:26:00'),
        (169, 43, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-09-05T10:01:00'),
        (170, 43, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-09-08T04:06:00'),
        (171, 44, NULL, N'AwaitingPayment', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', NULL, N'2026-09-25T05:54:00'),
        (172, 44, N'AwaitingPayment', N'Cancelled', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'Khách tự hủy: Nhóm bạn không đủ người.', N'2026-09-25T05:57:00'),
        (173, 45, NULL, N'AwaitingPayment', N'Customer', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', NULL, N'2026-09-04T02:04:00'),
        (174, 45, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0031.', N'2026-09-04T02:10:00'),
        (175, 45, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-04T02:37:00'),
        (176, 45, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-09-28T08:10:00'),
        (177, 45, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-01T04:03:00'),
        (178, 46, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-09-15T04:43:00'),
        (179, 46, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-09-15T04:59:00'),
        (180, 47, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-07-07T04:07:00'),
        (181, 47, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0032.', N'2026-07-07T04:19:00'),
        (182, 47, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-07T04:36:00'),
        (183, 47, N'Confirmed', N'NoShow', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', N'Khách không đến nhận phòng.', N'2026-07-18T16:00:00'),
        (184, 48, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-07-04T01:26:00'),
        (185, 48, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0033.', N'2026-07-04T01:35:00'),
        (186, 48, N'Deposited', N'Confirmed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-07-04T02:01:00'),
        (187, 48, N'Confirmed', N'CheckedIn', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-07-05T09:22:00'),
        (188, 48, N'CheckedIn', N'Completed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-07-07T04:34:00'),
        (189, 49, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-25T04:32:00'),
        (190, 49, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0034.', N'2026-09-25T04:42:00'),
        (191, 49, N'Deposited', N'Confirmed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-25T05:06:00'),
        (192, 49, N'Confirmed', N'CheckedIn', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-28T08:04:00'),
        (193, 49, N'CheckedIn', N'Completed', N'Owner', N'b0266ef0-61ff-42de-9be9-c2102618e46e', NULL, N'2026-09-29T04:52:00'),
        (194, 50, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-09-22T03:24:00'),
        (195, 50, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0035.', N'2026-09-22T03:30:00'),
        (196, 50, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-09-22T03:54:00'),
        (197, 50, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-10-01T08:56:00'),
        (198, 50, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-10-04T04:25:00'),
        (199, 51, NULL, N'AwaitingPayment', N'Customer', N'59293239-825f-4f9f-88cd-aa308164fba4', NULL, N'2026-08-01T02:20:00'),
        (200, 51, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0036.', N'2026-08-01T02:28:00');
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (201, 51, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-08-01T02:36:00'),
        (202, 51, N'Confirmed', N'CheckedIn', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-08-03T09:46:00'),
        (203, 51, N'CheckedIn', N'Completed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-08-06T04:15:00'),
        (204, 52, NULL, N'AwaitingPayment', N'Customer', N'244b0e39-485b-4bef-9136-7a59df1fdde7', NULL, N'2026-06-29T04:48:00'),
        (205, 52, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0037.', N'2026-06-29T04:53:00'),
        (206, 52, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-06-29T05:11:00'),
        (207, 52, N'Confirmed', N'CheckedIn', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-07-01T07:50:00'),
        (208, 52, N'CheckedIn', N'Completed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-07-04T04:35:00'),
        (209, 53, NULL, N'AwaitingPayment', N'Customer', N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', NULL, N'2026-08-17T01:34:00'),
        (210, 53, N'AwaitingPayment', N'Cancelled', N'Customer', N'ad7d6e89-f20c-4ef1-bc81-b7e30f4d8a30', N'Khách tự hủy: Gia đình có việc đột xuất.', N'2026-08-17T01:42:00'),
        (211, 54, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-07-29T02:29:00'),
        (212, 54, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0038.', N'2026-07-29T02:39:00'),
        (213, 54, N'Deposited', N'Rejected', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'Chủ nhà bận việc gia đình, không đón khách được.', N'2026-07-29T03:12:00'),
        (214, 55, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-10-19T16:19:49.4411780'),
        (215, 55, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-19T16:35:49.4411780'),
        (216, 56, NULL, N'AwaitingPayment', N'Employee', N'9a24dade-ea07-4f85-aa55-c70d964a8024', NULL, N'2026-10-20T07:19:49.4411780'),
        (217, 56, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0039.', N'2026-10-20T07:27:49.4411780'),
        (218, 56, N'Deposited', N'Confirmed', N'Owner', N'b12b5c52-b60a-4356-ac02-ebf3b56d0295', NULL, N'2026-10-20T07:45:49.4411780'),
        (219, 57, NULL, N'AwaitingPayment', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-08-07T05:05:00'),
        (220, 57, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'Khách gửi ảnh bill.', N'2026-08-07T05:14:00'),
        (221, 57, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-08-07T07:13:00'),
        (222, 57, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-08-07T07:40:00'),
        (223, 57, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-09-06T09:30:00'),
        (224, 57, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-09-07T04:44:00'),
        (225, 58, NULL, N'AwaitingPayment', N'Customer', N'89010c48-12f1-4c0d-b829-c1679b02e399', NULL, N'2026-07-15T02:55:00'),
        (226, 58, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0040.', N'2026-07-15T03:07:00'),
        (227, 58, N'Deposited', N'Confirmed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-15T03:27:00'),
        (228, 58, N'Confirmed', N'CheckedIn', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-17T10:59:00'),
        (229, 58, N'CheckedIn', N'Completed', N'Owner', N'9d0c1ac4-f49d-4e4e-b42c-f19c5c63ff2a', NULL, N'2026-07-19T04:00:00'),
        (230, 59, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-10-19T06:19:49.4411780'),
        (231, 59, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0041.', N'2026-10-19T06:30:49.4411780'),
        (232, 59, N'Deposited', N'Confirmed', N'Owner', N'0110b42d-3b94-4d4d-8c46-19893793d8ac', NULL, N'2026-10-19T06:37:49.4411780'),
        (233, 60, NULL, N'AwaitingPayment', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', NULL, N'2026-06-07T03:48:00'),
        (234, 60, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'Khách gửi ảnh bill.', N'2026-06-07T03:57:00'),
        (235, 60, N'PaymentSubmitted', N'Deposited', N'Employee', N'44c73fdc-7312-4954-8342-047c13457bc4', N'Kế toán duyệt ảnh bill.', N'2026-06-07T05:32:00'),
        (236, 60, N'Deposited', N'Confirmed', N'Owner', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', NULL, N'2026-06-07T05:50:00'),
        (237, 60, N'Confirmed', N'Cancelled', N'Employee', N'0f21ecd9-e567-4aa2-9971-f1049ecec930', N'Khách hủy trước 5 ngày: Nhóm bạn không đủ người.', N'2026-06-07T06:07:27.4097985'),
        (238, 59, N'Confirmed', N'Deposited', N'Employee', N'40c01ab2-f1f5-4f22-826a-45abd55ca486', N'Đồng ý đổi ngày, không cần nộp thêm cọc.', N'2026-10-19T07:38:49.4411780');
    SET IDENTITY_INSERT [dbo].[BookingStatusHistories] OFF;
    DBCC CHECKIDENT (N'[dbo].[BookingStatusHistories]', RESEED, 238) WITH NO_INFOMSGS;

    -- Reviews: 20 dòng.
    SET IDENTITY_INSERT [dbo].[Reviews] ON;
    INSERT INTO [dbo].[Reviews] ([Id], [BookingId], [HomestayId], [Rating], [Comment], [OwnerReply], [RepliedAt], [IsHidden], [CreatedAt]) VALUES
        (1, 58, 32, 4, N'Phòng đẹp, tuy nhiên nước nóng hơi yếu. Chủ nhà hỗ trợ nhanh.', N'Rất vui vì gia đình bạn có kỳ nghỉ vui vẻ!', N'2026-07-20T06:00:00', 0, N'2026-07-19T14:00:00'),
        (2, 57, 12, 2, N'Điều hòa kêu to, phải đổi phòng giữa đêm. Chủ nhà có xin lỗi.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-09-10T17:44:00', 0, N'2026-09-10T01:44:00'),
        (3, 52, 6, 5, N'View đẹp, sáng ra uống cà phê ngoài ban công rất chill.', NULL, NULL, 0, N'2026-07-04T09:35:00'),
        (4, 51, 5, 4, N'Nhân viên Fuji tư vấn nhiệt tình qua điện thoại, phòng đúng như mô tả.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-08-09T09:15:00', 0, N'2026-08-09T03:15:00'),
        (5, 48, 34, 4, N'Phòng ổn, sạch, chỉ có wifi hơi chập chờn buổi tối.', NULL, NULL, 0, N'2026-07-09T07:34:00'),
        (6, 43, 7, 5, N'Lần thứ hai ở đây, vẫn tuyệt vời như lần trước.', NULL, NULL, 0, N'2026-09-09T23:06:00'),
        (7, 42, 18, 1, N'Inbox zalo 09xx để nhận mã giảm giá homestay khác rẻ hơn!!!', NULL, NULL, 1, N'2026-09-16T06:44:00'),
        (8, 41, 32, 5, N'Đặt cọc bằng QR nhanh, vài phút đã có xác nhận. Trải nghiệm rất tốt.', NULL, NULL, 0, N'2026-07-04T15:33:00'),
        (9, 40, 7, 3, N'Phòng hơi nhỏ so với ảnh, cách âm chưa tốt.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-09T02:14:00', 0, N'2026-08-08T15:14:00'),
        (10, 37, 25, 5, N'Không gian đẹp y như ảnh, giường êm, check-in nhanh gọn.', NULL, NULL, 0, N'2026-06-14T05:02:00'),
        (11, 31, 5, 4, N'Trẻ con rất thích sân vườn. Sẽ giới thiệu cho bạn bè.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-09-12T20:06:00', 0, N'2026-09-12T14:06:00'),
        (12, 29, 4, 3, N'Tạm ổn, dọn phòng chưa kỹ lắm khi nhận phòng.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-13T16:23:00', 0, N'2026-08-13T02:23:00'),
        (13, 26, 25, 4, N'Đáng tiền, chỉ mong có thêm ấm siêu tốc trong phòng.', NULL, NULL, 0, N'2026-07-21T11:26:00'),
        (14, 24, 20, 4, N'Vị trí tốt nhưng chỗ để xe hơi chật. Nhìn chung hài lòng.', NULL, NULL, 0, N'2026-07-18T21:48:00'),
        (15, 21, 33, 5, N'Nhóm mình tổ chức sinh nhật ở đây, khu BBQ rộng và đầy đủ dụng cụ.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-07-09T20:34:00', 0, N'2026-07-09T11:34:00'),
        (16, 18, 1, 5, N'Phòng sạch sẽ, chủ nhà nhiệt tình, vị trí rất thuận tiện đi lại. Sẽ quay lại!', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-06-07T16:21:00', 0, N'2026-06-06T19:21:00'),
        (17, 16, 12, 5, N'Cả nhà đều thích, trẻ con được chơi thoải mái. Giá hợp lý so với chất lượng.', NULL, NULL, 0, N'2026-06-14T05:40:00'),
        (18, 13, 8, 5, N'Chủ nhà chỉ quán ăn ngon quanh khu, buổi tối rất yên tĩnh.', N'Rất vui vì gia đình bạn có kỳ nghỉ vui vẻ!', N'2026-06-30T20:53:00', 0, N'2026-06-30T04:53:00'),
        (19, 12, 6, 3, N'Giá cuối tuần hơi cao, tiện nghi ở mức cơ bản.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-13T15:58:00', 0, N'2026-08-13T04:58:00'),
        (20, 11, 8, 4, N'Không khí trong lành, đường vào hơi khó tìm lúc trời tối.', NULL, NULL, 0, N'2026-07-25T08:17:00');
    SET IDENTITY_INSERT [dbo].[Reviews] OFF;
    DBCC CHECKIDENT (N'[dbo].[Reviews]', RESEED, 20) WITH NO_INFOMSGS;

    -- SettlementItems: 31 dòng.
    SET IDENTITY_INSERT [dbo].[SettlementItems] ON;
    INSERT INTO [dbo].[SettlementItems] ([Id], [SettlementId], [BookingId], [ItemType], [RoomAmount], [DepositAmount], [RefundAmount], [RetainedAmount], [CommissionRate], [CommissionAmount], [PayoutAmount], [DiscountAmount]) VALUES
        (1, 1, 18, N'Completed', 1920000, 576000, 0, 576000, 0.1000, 192000, 384000, 0),
        (2, 2, 16, N'Completed', 2560000, 768000, 0, 768000, 0.1500, 384000, 384000, 0),
        (3, 3, 37, N'Completed', 560000, 168000, 0, 168000, 0.1000, 56000, 112000, 0),
        (4, 4, 60, N'CancelledRetained', 2040000, 612000, 306000, 306000, 0.1000, 204000, 102000, 0),
        (5, 5, 13, N'Completed', 480000, 144000, 0, 144000, 0.1200, 57600, 86400, 0),
        (6, 6, 52, N'Completed', 2880000, 864000, 0, 864000, 0.1500, 432000, 432000, 0),
        (7, 7, 41, N'Completed', 700000, 210000, 0, 210000, 0.1200, 84000, 126000, 0),
        (8, 8, 21, N'Completed', 4100000, 1230000, 0, 1230000, 0.1500, 615000, 615000, 0),
        (9, 9, 48, N'Completed', 1800000, 540000, 0, 540000, 0.1000, 180000, 360000, 0),
        (10, 10, 11, N'Completed', 770000, 231000, 0, 231000, 0.1200, 92400, 138600, 0),
        (11, 11, 24, N'Completed', 4320000, 1296000, 0, 1296000, 0.1200, 518400, 777600, 0),
        (12, 12, 47, N'NoShow', 2050000, 615000, 0, 615000, 0.1000, 205000, 410000, 0),
        (13, 13, 26, N'Completed', 2240000, 672000, 0, 672000, 0.1000, 224000, 448000, 0),
        (14, 14, 58, N'Completed', 6720000, 2016000, 0, 2016000, 0.1200, 806400, 1209600, 0),
        (15, 15, 29, N'Completed', 3800000, 1140000, 0, 1140000, 0.1000, 380000, 760000, 0),
        (16, 16, 51, N'Completed', 3840000, 1152000, 0, 1152000, 0.1200, 460800, 691200, 0),
        (17, 17, 12, N'Completed', 1440000, 432000, 0, 432000, 0.1500, 216000, 216000, 0),
        (18, 18, 40, N'Completed', 1600000, 480000, 0, 480000, 0.1000, 160000, 320000, 0),
        (19, 19, 31, N'Completed', 3840000, 1152000, 0, 1152000, 0.1200, 460800, 691200, 0),
        (20, 20, 43, N'Completed', 5120000, 1536000, 0, 1536000, 0.1000, 512000, 1024000, 0),
        (21, 21, 57, N'Completed', 1280000, 384000, 0, 384000, 0.1500, 192000, 192000, 0),
        (22, 22, 19, N'Completed', 1600000, 480000, 0, 480000, 0.1200, 192000, 288000, 0),
        (23, 23, 42, N'Completed', 2380000, 714000, 0, 714000, 0.1500, 357000, 357000, 0),
        (24, 24, 10, N'CancelledRetained', 8000000, 2400000, 0, 2400000, 0.1500, 1200000, 1200000, 0),
        (25, 25, 49, N'Completed', 700000, 210000, 0, 210000, 0.1000, 70000, 140000, 0),
        (26, 26, 8, N'NoShow', 1440000, 432000, 0, 432000, 0.1500, 216000, 216000, 0),
        (27, 27, 35, N'Completed', 1920000, 576000, 0, 576000, 0.1500, 288000, 288000, 0),
        (28, 28, 32, N'Completed', 1700000, 510000, 0, 510000, 0.1500, 255000, 255000, 0),
        (29, 29, 50, N'Completed', 1700000, 510000, 0, 510000, 0.1200, 204000, 306000, 0),
        (30, 30, 45, N'Completed', 960000, 288000, 0, 288000, 0.1500, 144000, 144000, 0),
        (31, 31, 34, N'Completed', 1730000, 519000, 0, 519000, 0.1200, 207600, 311400, 0);
    SET IDENTITY_INSERT [dbo].[SettlementItems] OFF;
    DBCC CHECKIDENT (N'[dbo].[SettlementItems]', RESEED, 31) WITH NO_INFOMSGS;

    -- Payments: 53 dòng.
    SET IDENTITY_INSERT [dbo].[Payments] ON;
    INSERT INTO [dbo].[Payments] ([Id], [BookingId], [Type], [Method], [BankTransactionId], [Amount], [ReceiptImageUrl], [TransferContent], [BankRef], [Status], [SubmittedByAccountId], [SubmittedAt], [ReviewedByEmployeeId], [ReviewedAt], [RejectReason], [ReviewAlertSentAt], [ChangeRequestId]) VALUES
        (1, 2, N'Deposit', N'ManualReceipt', NULL, 1008000, N'sample-receipt.png', N'FJ261008-001', NULL, N'Approved', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'2026-10-08T05:17:00', 3, N'2026-10-08T07:36:00', NULL, NULL, NULL),
        (2, 5, N'Deposit', N'ManualReceipt', NULL, 2400000, N'sample-receipt.png', N'FJ261020-003', NULL, N'Submitted', N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'2026-10-20T14:49:49.4411780', NULL, NULL, NULL, NULL, NULL),
        (3, 6, N'Deposit', N'ManualReceipt', NULL, 240000, N'sample-receipt.png', N'FJ261020-004', NULL, N'Submitted', N'29aeaf6c-c20b-479d-bddd-14b5a3e8174d', N'2026-10-20T12:14:49.4411780', NULL, NULL, NULL, NULL, NULL),
        (4, 7, N'Refund', N'ManualTransfer', NULL, 960000, NULL, N'Fuji hoan coc FJ260601-001', NULL, N'Approved', N'44c73fdc-7312-4954-8342-047c13457bc4', N'2026-06-03T06:03:00', 3, N'2026-06-03T06:03:00', NULL, NULL, NULL),
        (5, 13, N'Deposit', N'ManualReceipt', NULL, 144000, N'sample-receipt.png', N'FJ260621-001', NULL, N'Approved', N'62b20678-2da9-465f-b9cc-595ed1710f0d', N'2026-06-21T04:17:00', 3, N'2026-06-21T05:16:00', NULL, NULL, NULL),
        (6, 19, N'Deposit', N'ManualReceipt', NULL, 480000, N'sample-receipt.png', N'FJ260912-001', NULL, N'Approved', NULL, N'2026-09-12T04:05:00', 3, N'2026-09-12T06:25:00', NULL, NULL, NULL),
        (7, 20, N'Deposit', N'ManualReceipt', NULL, 720000, N'sample-receipt.png', N'FJ261017-001', NULL, N'Approved', NULL, N'2026-10-17T15:30:49.4411780', 3, N'2026-10-17T16:47:49.4411780', NULL, NULL, NULL),
        (8, 31, N'Deposit', N'ManualReceipt', NULL, 1152000, N'sample-receipt.png', N'FJ260809-001', NULL, N'Approved', N'6bbb3be5-00d9-48f4-8839-8f7e03aced5e', N'2026-08-09T05:33:00', 3, N'2026-08-09T06:43:00', NULL, NULL, NULL),
        (9, 35, N'Deposit', N'ManualReceipt', NULL, 576000, N'sample-receipt.png', N'FJ260904-001', NULL, N'Approved', NULL, N'2026-09-04T02:45:00', 3, N'2026-09-04T03:24:00', NULL, NULL, NULL),
        (10, 42, N'Deposit', N'ManualReceipt', NULL, 714000, N'sample-receipt.png', N'FJ260908-001', NULL, N'Approved', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'2026-09-08T01:57:00', 3, N'2026-09-08T02:42:00', NULL, NULL, NULL),
        (11, 54, N'Refund', N'ManualTransfer', NULL, 192000, NULL, N'Fuji hoan coc FJ260729-001', NULL, N'Approved', N'44c73fdc-7312-4954-8342-047c13457bc4', N'2026-07-31T03:12:00', 3, N'2026-07-31T03:12:00', NULL, NULL, NULL),
        (12, 57, N'Deposit', N'ManualReceipt', NULL, 384000, N'sample-receipt.png', N'FJ260807-001', NULL, N'Approved', N'33365d91-e3a7-4120-95de-cc8894ea7fc8', N'2026-08-07T05:14:00', 3, N'2026-08-07T07:13:00', NULL, NULL, NULL),
        (13, 60, N'Deposit', N'ManualReceipt', NULL, 612000, N'sample-receipt.png', N'FJ260607-002', NULL, N'Approved', N'256f9d4a-ecb9-430e-b090-bc42b05ca6a5', N'2026-06-07T03:57:00', 3, N'2026-06-07T05:32:00', NULL, NULL, NULL),
        (14, 60, N'Refund', N'ManualTransfer', NULL, 306000, NULL, N'Fuji hoan coc FJ260607-002', NULL, N'Approved', N'44c73fdc-7312-4954-8342-047c13457bc4', N'2026-06-09T06:07:27.4097985', 3, N'2026-06-09T06:07:27.4097985', NULL, NULL, NULL),
        (15, 1, N'Deposit', N'BankAutoMatch', 2, 810000, NULL, N'FJ260929-001 dat coc', NULL, N'Approved', NULL, N'2026-09-29T05:43:00', NULL, N'2026-09-29T05:43:00', NULL, NULL, NULL),
        (16, 3, N'Deposit', N'BankAutoMatch', 3, 96000, NULL, N'FJ261020-001 dat coc', NULL, N'Approved', NULL, N'2026-10-20T15:02:49.4411780', NULL, N'2026-10-20T15:02:49.4411780', NULL, NULL, NULL),
        (17, 4, N'Deposit', N'BankAutoMatch', 4, 240000, NULL, N'FJ261020-002 dat coc', NULL, N'Approved', NULL, N'2026-10-20T14:24:49.4411780', NULL, N'2026-10-20T14:24:49.4411780', NULL, NULL, NULL),
        (18, 7, N'Deposit', N'BankAutoMatch', 5, 960000, NULL, N'FJ260601-001', NULL, N'Approved', NULL, N'2026-06-01T05:34:00', NULL, N'2026-06-01T05:34:00', NULL, NULL, NULL),
        (19, 8, N'Deposit', N'BankAutoMatch', 6, 432000, NULL, N'FJ260825-001 dat coc', NULL, N'Approved', NULL, N'2026-08-25T01:27:00', NULL, N'2026-08-25T01:27:00', NULL, NULL, NULL),
        (20, 9, N'Deposit', N'BankAutoMatch', 7, 2400000, NULL, N'FJ261020-005', NULL, N'Approved', NULL, N'2026-10-20T10:24:49.4411780', NULL, N'2026-10-20T10:24:49.4411780', NULL, NULL, NULL),
        (21, 10, N'Deposit', N'BankAutoMatch', 8, 2400000, NULL, N'MBVCB.560275.FJ260804001.CT tu Đinh Thị Yến', NULL, N'Approved', NULL, N'2026-08-04T01:52:00', NULL, N'2026-08-04T01:52:00', NULL, NULL, NULL),
        (22, 11, N'Deposit', N'BankAutoMatch', 9, 231000, NULL, N'MBVCB.462430.FJ260705001.CT tu Lê Văn Hùng', NULL, N'Approved', NULL, N'2026-07-05T05:02:00', NULL, N'2026-07-05T05:02:00', NULL, NULL, NULL),
        (23, 12, N'Deposit', N'BankAutoMatch', 10, 432000, NULL, N'MBVCB.428352.FJ260713001.CT tu Hồ Minh Khang', NULL, N'Approved', NULL, N'2026-07-13T03:40:00', NULL, N'2026-07-13T03:40:00', NULL, NULL, NULL),
        (24, 14, N'Deposit', N'BankAutoMatch', 11, 240000, NULL, N'MBVCB.948530.FJ261018001.CT tu Lý Thanh Tâm', NULL, N'Approved', NULL, N'2026-10-18T06:21:49.4411780', NULL, N'2026-10-18T06:21:49.4411780', NULL, NULL, NULL),
        (25, 15, N'Deposit', N'BankAutoMatch', 12, 408000, NULL, N'FJ261020-006', NULL, N'Approved', NULL, N'2026-10-20T04:21:49.4411780', NULL, N'2026-10-20T04:21:49.4411780', NULL, NULL, NULL),
        (26, 16, N'Deposit', N'BankAutoMatch', 13, 768000, NULL, N'MBVCB.901086.FJ260601002.CT tu Phan Ngọc Ánh', NULL, N'Approved', NULL, N'2026-06-01T04:49:00', NULL, N'2026-06-01T04:49:00', NULL, NULL, NULL),
        (27, 17, N'Deposit', N'BankAutoMatch', 14, 384000, NULL, N'FJ261019-001', NULL, N'Approved', NULL, N'2026-10-19T02:28:00', NULL, N'2026-10-19T02:28:00', NULL, NULL, NULL),
        (28, 18, N'Deposit', N'BankAutoMatch', 15, 576000, NULL, N'MBVCB.937809.FJ260601003.CT tu Nguyễn Văn Minh', NULL, N'Approved', NULL, N'2026-06-01T03:32:00', NULL, N'2026-06-01T03:32:00', NULL, NULL, NULL),
        (29, 21, N'Deposit', N'BankAutoMatch', 16, 1230000, NULL, N'FJ260607-001 dat coc', NULL, N'Approved', NULL, N'2026-06-07T05:19:00', NULL, N'2026-06-07T05:19:00', NULL, NULL, NULL),
        (30, 22, N'Deposit', N'BankAutoMatch', 17, 1296000, NULL, N'MBVCB.819025.FJ261020007.CT tu Lê Văn Hùng', NULL, N'Approved', NULL, N'2026-10-20T07:28:49.4411780', NULL, N'2026-10-20T07:28:49.4411780', NULL, NULL, NULL),
        (31, 23, N'Deposit', N'BankAutoMatch', 18, 576000, NULL, N'MBVCB.374663.FJ261019002.CT tu Võ Thị Hồng', NULL, N'Approved', NULL, N'2026-10-18T19:22:49.4411780', NULL, N'2026-10-18T19:22:49.4411780', NULL, NULL, NULL),
        (32, 24, N'Deposit', N'BankAutoMatch', 19, 1296000, NULL, N'FJ260710-001 dat coc', NULL, N'Approved', NULL, N'2026-07-10T05:45:00', NULL, N'2026-07-10T05:45:00', NULL, NULL, NULL),
        (33, 26, N'Deposit', N'BankAutoMatch', 20, 672000, NULL, N'MBVCB.678783.FJ260627001.CT tu Đặng Thị Thu', NULL, N'Approved', NULL, N'2026-06-27T04:40:00', NULL, N'2026-06-27T04:40:00', NULL, NULL, NULL),
        (34, 29, N'Deposit', N'BankAutoMatch', 21, 1140000, NULL, N'FJ260712-001', NULL, N'Approved', NULL, N'2026-07-12T01:15:00', NULL, N'2026-07-12T01:15:00', NULL, NULL, NULL),
        (35, 32, N'Deposit', N'BankAutoMatch', 22, 510000, NULL, N'MBVCB.384859.FJ260827001.CT tu Chu Thị Ngân', NULL, N'Approved', NULL, N'2026-08-27T03:42:00', NULL, N'2026-08-27T03:42:00', NULL, NULL, NULL),
        (36, 34, N'Deposit', N'BankAutoMatch', 23, 519000, NULL, N'MBVCB.480041.FJ260923001.CT tu Trần Thị Bích', NULL, N'Approved', NULL, N'2026-09-23T03:46:00', NULL, N'2026-09-23T03:46:00', NULL, NULL, NULL),
        (37, 36, N'Deposit', N'BankAutoMatch', 24, 600000, NULL, N'FJ261018-003 dat coc', NULL, N'Approved', NULL, N'2026-10-17T20:26:49.4411780', NULL, N'2026-10-17T20:26:49.4411780', NULL, NULL, NULL),
        (38, 37, N'Deposit', N'BankAutoMatch', 25, 168000, NULL, N'MBVCB.172576.FJ260601004.CT tu Tạ Quang Vinh', NULL, N'Approved', NULL, N'2026-06-01T03:25:00', NULL, N'2026-06-01T03:25:00', NULL, NULL, NULL),
        (39, 39, N'Deposit', N'BankAutoMatch', 26, 1440000, NULL, N'FJ261018-004 dat coc', NULL, N'Approved', NULL, N'2026-10-18T16:30:49.4411780', NULL, N'2026-10-18T16:30:49.4411780', NULL, NULL, NULL),
        (40, 40, N'Deposit', N'BankAutoMatch', 27, 480000, NULL, N'MBVCB.838236.FJ260715001.CT tu Lê Văn Hùng', NULL, N'Approved', NULL, N'2026-07-15T03:06:00', NULL, N'2026-07-15T03:06:00', NULL, NULL, NULL),
        (41, 41, N'Deposit', N'BankAutoMatch', 28, 210000, NULL, N'FJ260629-001', NULL, N'Approved', NULL, N'2026-06-29T01:47:00', NULL, N'2026-06-29T01:47:00', NULL, NULL, NULL),
        (42, 43, N'Deposit', N'BankAutoMatch', 29, 1536000, NULL, N'FJ260816-001', NULL, N'Approved', NULL, N'2026-08-16T05:16:00', NULL, N'2026-08-16T05:16:00', NULL, NULL, NULL),
        (43, 45, N'Deposit', N'BankAutoMatch', 30, 288000, NULL, N'MBVCB.951313.FJ260904002.CT tu Đinh Thị Yến', NULL, N'Approved', NULL, N'2026-09-04T02:10:00', NULL, N'2026-09-04T02:10:00', NULL, NULL, NULL),
        (44, 47, N'Deposit', N'BankAutoMatch', 31, 615000, NULL, N'FJ260707-001', NULL, N'Approved', NULL, N'2026-07-07T04:19:00', NULL, N'2026-07-07T04:19:00', NULL, NULL, NULL),
        (45, 48, N'Deposit', N'BankAutoMatch', 32, 540000, NULL, N'FJ260704-002', NULL, N'Approved', NULL, N'2026-07-04T01:35:00', NULL, N'2026-07-04T01:35:00', NULL, NULL, NULL),
        (46, 49, N'Deposit', N'BankAutoMatch', 33, 210000, NULL, N'FJ260925-002', NULL, N'Approved', NULL, N'2026-09-25T04:42:00', NULL, N'2026-09-25T04:42:00', NULL, NULL, NULL),
        (47, 50, N'Deposit', N'BankAutoMatch', 34, 510000, NULL, N'FJ260922-001', NULL, N'Approved', NULL, N'2026-09-22T03:30:00', NULL, N'2026-09-22T03:30:00', NULL, NULL, NULL),
        (48, 51, N'Deposit', N'BankAutoMatch', 35, 1152000, NULL, N'MBVCB.961891.FJ260801001.CT tu Mai Đức Anh', NULL, N'Approved', NULL, N'2026-08-01T02:28:00', NULL, N'2026-08-01T02:28:00', NULL, NULL, NULL),
        (49, 52, N'Deposit', N'BankAutoMatch', 36, 864000, NULL, N'FJ260629-002', NULL, N'Approved', NULL, N'2026-06-29T04:53:00', NULL, N'2026-06-29T04:53:00', NULL, NULL, NULL),
        (50, 54, N'Deposit', N'BankAutoMatch', 37, 192000, NULL, N'FJ260729-001', NULL, N'Approved', NULL, N'2026-07-29T02:39:00', NULL, N'2026-07-29T02:39:00', NULL, NULL, NULL),
        (51, 56, N'Deposit', N'BankAutoMatch', 38, 240000, NULL, N'FJ261020-010 dat coc', NULL, N'Approved', NULL, N'2026-10-20T07:27:49.4411780', NULL, N'2026-10-20T07:27:49.4411780', NULL, NULL, NULL),
        (52, 58, N'Deposit', N'BankAutoMatch', 39, 2016000, NULL, N'FJ260715-002', NULL, N'Approved', NULL, N'2026-07-15T03:07:00', NULL, N'2026-07-15T03:07:00', NULL, NULL, NULL),
        (53, 59, N'Deposit', N'BankAutoMatch', 40, 1200000, NULL, N'MBVCB.974092.FJ261019004.CT tu Đặng Thị Thu', NULL, N'Approved', NULL, N'2026-10-19T06:30:49.4411780', NULL, N'2026-10-19T06:30:49.4411780', NULL, NULL, NULL);
    SET IDENTITY_INSERT [dbo].[Payments] OFF;
    DBCC CHECKIDENT (N'[dbo].[Payments]', RESEED, 53) WITH NO_INFOMSGS;

    -- RoomNights: 101 dòng.
    INSERT INTO [dbo].[RoomNights] ([RoomId], [Date], [BookingId], [RoomBlockId], [BookingChangeRequestId]) VALUES
        (1, N'2026-10-23', 15, NULL, NULL),
        (1, N'2026-10-24', 15, NULL, NULL),
        (1, N'2026-10-25', 15, NULL, NULL),
        (3, N'2026-06-01', 18, NULL, NULL),
        (3, N'2026-06-02', 18, NULL, NULL),
        (3, N'2026-06-03', 18, NULL, NULL),
        (4, N'2026-10-29', 3, NULL, NULL),
        (6, N'2026-10-01', 50, NULL, NULL),
        (6, N'2026-10-02', 50, NULL, NULL),
        (6, N'2026-10-03', 50, NULL, NULL),
        (13, N'2026-09-28', 49, NULL, NULL),
        (14, N'2026-08-07', 29, NULL, NULL),
        (14, N'2026-08-08', 29, NULL, NULL),
        (14, N'2026-08-09', 29, NULL, NULL),
        (14, N'2026-10-20', 2, NULL, NULL),
        (14, N'2026-10-21', 2, NULL, NULL),
        (14, N'2026-10-22', 2, NULL, NULL),
        (17, N'2026-08-03', 51, NULL, NULL),
        (17, N'2026-08-04', 51, NULL, NULL),
        (17, N'2026-08-05', 51, NULL, NULL),
        (17, N'2026-09-07', 31, NULL, NULL),
        (17, N'2026-09-08', 31, NULL, NULL),
        (17, N'2026-09-09', 31, NULL, NULL),
        (19, N'2026-07-01', 52, NULL, NULL),
        (19, N'2026-07-02', 52, NULL, NULL),
        (19, N'2026-07-03', 52, NULL, NULL),
        (20, N'2026-10-19', 1, NULL, NULL),
        (20, N'2026-10-20', 1, NULL, NULL),
        (20, N'2026-10-21', 1, NULL, NULL),
        (21, N'2026-08-09', 12, NULL, NULL),
        (24, N'2026-09-05', 43, NULL, NULL),
        (24, N'2026-09-06', 43, NULL, NULL),
        (24, N'2026-09-07', 43, NULL, NULL),
        (25, N'2026-08-05', 40, NULL, NULL),
        (25, N'2026-08-06', 40, NULL, NULL),
        (26, N'2026-06-27', 13, NULL, NULL),
        (28, N'2026-07-24', 11, NULL, NULL),
        (40, N'2026-06-10', 16, NULL, NULL),
        (40, N'2026-06-11', 16, NULL, NULL),
        (40, N'2026-06-12', 16, NULL, NULL),
        (42, N'2026-09-06', 57, NULL, NULL),
        (49, N'2026-09-14', 19, NULL, NULL),
        (52, N'2026-09-20', 35, NULL, NULL),
        (52, N'2026-09-21', 35, NULL, NULL),
        (52, N'2026-09-22', 35, NULL, NULL),
        (53, N'2026-09-28', 45, NULL, NULL),
        (53, N'2026-09-29', 45, NULL, NULL),
        (53, N'2026-09-30', 45, NULL, NULL),
        (56, N'2026-12-08', 20, NULL, NULL),
        (56, N'2026-12-09', 20, NULL, NULL),
        (56, N'2026-12-10', 20, NULL, NULL),
        (57, N'2026-09-30', NULL, 1, NULL),
        (57, N'2026-10-01', NULL, 1, NULL),
        (57, N'2026-10-02', NULL, 1, NULL),
        (62, N'2026-09-11', 42, NULL, NULL),
        (62, N'2026-09-12', 42, NULL, NULL),
        (62, N'2026-09-13', 42, NULL, NULL),
        (62, N'2026-11-07', NULL, 5, NULL),
        (62, N'2026-11-08', NULL, 5, NULL),
        (70, N'2026-07-13', 24, NULL, NULL),
        (70, N'2026-07-14', 24, NULL, NULL),
        (70, N'2026-07-15', 24, NULL, NULL),
        (70, N'2026-10-02', 34, NULL, NULL),
        (72, N'2026-10-25', NULL, 3, NULL),
        (87, N'2026-07-19', 26, NULL, NULL),
        (87, N'2026-07-20', 26, NULL, NULL),
        (88, N'2026-06-11', 37, NULL, NULL),
        (89, N'2026-11-09', 6, NULL, NULL),
        (95, N'2026-11-09', 22, NULL, NULL),
        (95, N'2026-11-10', 22, NULL, NULL),
        (95, N'2026-11-11', 22, NULL, NULL),
        (96, N'2026-11-19', NULL, 6, NULL),
        (96, N'2026-11-20', NULL, 6, NULL),
        (96, N'2026-11-21', NULL, 6, NULL),
        (98, N'2026-10-22', NULL, 2, NULL),
        (98, N'2026-10-23', NULL, 2, NULL),
        (100, N'2026-11-18', 59, NULL, NULL),
        (105, N'2026-09-25', 32, NULL, NULL),
        (105, N'2026-09-26', 32, NULL, NULL),
        (105, N'2026-09-27', 32, NULL, NULL),
        (107, N'2026-11-05', 4, NULL, NULL),
        (107, N'2026-11-29', 56, NULL, NULL),
        (108, N'2026-12-03', 36, NULL, NULL),
        (112, N'2026-12-22', 39, NULL, NULL),
        (112, N'2026-12-23', 39, NULL, NULL),
        (114, N'2026-07-02', 41, NULL, NULL),
        (116, N'2026-07-17', 58, NULL, NULL),
        (116, N'2026-07-18', 58, NULL, NULL),
        (119, N'2026-07-04', 21, NULL, NULL),
        (119, N'2026-07-05', 21, NULL, NULL),
        (119, N'2026-07-06', 21, NULL, NULL),
        (122, N'2026-07-05', 48, NULL, NULL),
        (122, N'2026-07-06', 48, NULL, NULL),
        (127, N'2026-10-31', NULL, 4, NULL),
        (127, N'2026-11-01', NULL, 4, NULL),
        (127, N'2026-11-02', NULL, 4, NULL),
        (127, N'2026-11-03', NULL, 4, NULL),
        (128, N'2026-11-01', 5, NULL, NULL),
        (128, N'2026-11-02', 5, NULL, NULL),
        (128, N'2026-12-20', 9, NULL, NULL);
    INSERT INTO [dbo].[RoomNights] ([RoomId], [Date], [BookingId], [RoomBlockId], [BookingChangeRequestId]) VALUES
        (128, N'2026-12-21', 9, NULL, NULL);

    -- Kiểm tra lại dữ liệu cũ/mới và khôi phục FK trusted trước khi COMMIT.
    ALTER TABLE [dbo].[Bookings] WITH CHECK CHECK CONSTRAINT [FK_Bookings_Bookings_ReplacedByBookingId];
    ALTER TABLE [dbo].[Vouchers] WITH CHECK CHECK CONSTRAINT [FK_Vouchers_Bookings_SourceBookingId];
    ALTER TABLE [dbo].[Bookings] WITH CHECK CHECK CONSTRAINT [FK_Bookings_Vouchers_VoucherId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
        THROW 50105, N'Khóa ngoại chưa bật hoặc chưa trusted.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[__EFMigrationsHistory]) <> 9
        THROW 50106, N'Số dòng không khớp: __EFMigrationsHistory.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AccountActivityLogs]) <> 0
        THROW 50106, N'Số dòng không khớp: AccountActivityLogs.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Amenities]) <> 10
        THROW 50106, N'Số dòng không khớp: Amenities.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Areas]) <> 7
        THROW 50106, N'Số dòng không khớp: Areas.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetRoleClaims]) <> 0
        THROW 50106, N'Số dòng không khớp: AspNetRoleClaims.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetRoles]) <> 3
        THROW 50106, N'Số dòng không khớp: AspNetRoles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserClaims]) <> 0
        THROW 50106, N'Số dòng không khớp: AspNetUserClaims.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserLogins]) <> 0
        THROW 50106, N'Số dòng không khớp: AspNetUserLogins.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserRoles]) <> 21
        THROW 50106, N'Số dòng không khớp: AspNetUserRoles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUsers]) <> 20
        THROW 50106, N'Số dòng không khớp: AspNetUsers.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserTokens]) <> 0
        THROW 50106, N'Số dòng không khớp: AspNetUserTokens.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AuditLogs]) <> 6
        THROW 50106, N'Số dòng không khớp: AuditLogs.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BankTransactions]) <> 43
        THROW 50106, N'Số dòng không khớp: BankTransactions.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingChangeRequests]) <> 5
        THROW 50106, N'Số dòng không khớp: BookingChangeRequests.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingCodeCounters]) <> 34
        THROW 50106, N'Số dòng không khớp: BookingCodeCounters.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingNights]) <> 122
        THROW 50106, N'Số dòng không khớp: BookingNights.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Bookings]) <> 60
        THROW 50106, N'Số dòng không khớp: Bookings.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingStatusHistories]) <> 238
        THROW 50106, N'Số dòng không khớp: BookingStatusHistories.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CancellationPolicies]) <> 6
        THROW 50106, N'Số dòng không khớp: CancellationPolicies.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CancellationRules]) <> 18
        THROW 50106, N'Số dòng không khớp: CancellationRules.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Contracts]) <> 35
        THROW 50106, N'Số dòng không khớp: Contracts.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CustomerProfiles]) <> 15
        THROW 50106, N'Số dòng không khớp: CustomerProfiles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Customers]) <> 15
        THROW 50106, N'Số dòng không khớp: Customers.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Employees]) <> 5
        THROW 50106, N'Số dòng không khớp: Employees.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Holidays]) <> 29
        THROW 50106, N'Số dòng không khớp: Holidays.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[HomestayAmenities]) <> 183
        THROW 50106, N'Số dòng không khớp: HomestayAmenities.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[HomestayImages]) <> 169
        THROW 50106, N'Số dòng không khớp: HomestayImages.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Homestays]) <> 35
        THROW 50106, N'Số dòng không khớp: Homestays.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[MessageLogs]) <> 0
        THROW 50106, N'Số dòng không khớp: MessageLogs.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Notifications]) <> 3
        THROW 50106, N'Số dòng không khớp: Notifications.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[OwnerRegistrations]) <> 6
        THROW 50106, N'Số dòng không khớp: OwnerRegistrations.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Owners]) <> 6
        THROW 50106, N'Số dòng không khớp: Owners.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Payments]) <> 53
        THROW 50106, N'Số dòng không khớp: Payments.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[PersonProfiles]) <> 25
        THROW 50106, N'Số dòng không khớp: PersonProfiles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Reviews]) <> 20
        THROW 50106, N'Số dòng không khớp: Reviews.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomBlocks]) <> 6
        THROW 50106, N'Số dòng không khớp: RoomBlocks.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomCategories]) <> 5
        THROW 50106, N'Số dòng không khớp: RoomCategories.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomNights]) <> 101
        THROW 50106, N'Số dòng không khớp: RoomNights.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Rooms]) <> 128
        THROW 50106, N'Số dòng không khớp: Rooms.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomTypePrices]) <> 279
        THROW 50106, N'Số dòng không khớp: RoomTypePrices.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomTypes]) <> 93
        THROW 50106, N'Số dòng không khớp: RoomTypes.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[SettlementItems]) <> 31
        THROW 50106, N'Số dòng không khớp: SettlementItems.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Settlements]) <> 31
        THROW 50106, N'Số dòng không khớp: Settlements.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[VerificationDocuments]) <> 114
        THROW 50106, N'Số dòng không khớp: VerificationDocuments.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Vouchers]) <> 3
        THROW 50106, N'Số dòng không khớp: Vouchers.', 1;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Số dòng thực tế sau khi nạp (bao gồm migration do deploy.sql ghi).
SELECT N'__EFMigrationsHistory' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[__EFMigrationsHistory]
UNION ALL SELECT N'AccountActivityLogs' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AccountActivityLogs]
UNION ALL SELECT N'Amenities' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Amenities]
UNION ALL SELECT N'Areas' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Areas]
UNION ALL SELECT N'AspNetRoleClaims' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetRoleClaims]
UNION ALL SELECT N'AspNetRoles' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetRoles]
UNION ALL SELECT N'AspNetUserClaims' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetUserClaims]
UNION ALL SELECT N'AspNetUserLogins' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetUserLogins]
UNION ALL SELECT N'AspNetUserRoles' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetUserRoles]
UNION ALL SELECT N'AspNetUsers' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetUsers]
UNION ALL SELECT N'AspNetUserTokens' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AspNetUserTokens]
UNION ALL SELECT N'AuditLogs' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[AuditLogs]
UNION ALL SELECT N'BankTransactions' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[BankTransactions]
UNION ALL SELECT N'BookingChangeRequests' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[BookingChangeRequests]
UNION ALL SELECT N'BookingCodeCounters' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[BookingCodeCounters]
UNION ALL SELECT N'BookingNights' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[BookingNights]
UNION ALL SELECT N'Bookings' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Bookings]
UNION ALL SELECT N'BookingStatusHistories' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[BookingStatusHistories]
UNION ALL SELECT N'CancellationPolicies' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[CancellationPolicies]
UNION ALL SELECT N'CancellationRules' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[CancellationRules]
UNION ALL SELECT N'Contracts' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Contracts]
UNION ALL SELECT N'CustomerProfiles' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[CustomerProfiles]
UNION ALL SELECT N'Customers' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Customers]
UNION ALL SELECT N'Employees' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Employees]
UNION ALL SELECT N'Holidays' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Holidays]
UNION ALL SELECT N'HomestayAmenities' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[HomestayAmenities]
UNION ALL SELECT N'HomestayImages' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[HomestayImages]
UNION ALL SELECT N'Homestays' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Homestays]
UNION ALL SELECT N'MessageLogs' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[MessageLogs]
UNION ALL SELECT N'Notifications' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Notifications]
UNION ALL SELECT N'OwnerRegistrations' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[OwnerRegistrations]
UNION ALL SELECT N'Owners' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Owners]
UNION ALL SELECT N'Payments' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Payments]
UNION ALL SELECT N'PersonProfiles' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[PersonProfiles]
UNION ALL SELECT N'Reviews' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Reviews]
UNION ALL SELECT N'RoomBlocks' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[RoomBlocks]
UNION ALL SELECT N'RoomCategories' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[RoomCategories]
UNION ALL SELECT N'RoomNights' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[RoomNights]
UNION ALL SELECT N'Rooms' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Rooms]
UNION ALL SELECT N'RoomTypePrices' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[RoomTypePrices]
UNION ALL SELECT N'RoomTypes' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[RoomTypes]
UNION ALL SELECT N'SettlementItems' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[SettlementItems]
UNION ALL SELECT N'Settlements' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Settlements]
UNION ALL SELECT N'VerificationDocuments' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[VerificationDocuments]
UNION ALL SELECT N'Vouchers' AS [Bảng], COUNT_BIG(*) AS [Số dòng] FROM [dbo].[Vouchers]
ORDER BY [Bảng];
PRINT N'Đã nạp dữ liệu mẫu; chạy verify.sql và tests/data_consistency.sql để kiểm tra.';
GO
