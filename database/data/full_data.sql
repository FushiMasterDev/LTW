-- Dữ liệu đầy đủ Fuji Homestay, xuất từ database dev HomestayBooking_v5 (seed Development + dữ liệu thao tác thử/QA).
-- Ngày xuất: 2026-10-21T12:12:37+07:00.
-- Thứ tự: tạo database trắng → deploy.sql → data/full_data.sql (sqlcmd, trong database/). Không chạy cùng sample_data.sql.
-- Dùng -d <TenDb> -v TargetDb=<TenDb>; chỉ nạp vào schema đúng bản và chưa có dữ liệu.
-- Ngày đơn cố định theo lúc tạo trong database nguồn, không tự dời theo ngày chạy.
-- Có mã băm Identity của tài khoản demo; chỉ dùng ở môi trường thử. Mật khẩu xem SeedData.DefaultPassword.
-- Ảnh homestay tham chiếu wwwroot/images/demo; ảnh bill/giấy tờ lấy từ Data/SampleFiles,
-- chép sample-receipt.png và sample-document.png vào App_Data/receipts nếu chưa có.
-- Không chứa khóa Data Protection; ứng dụng tự tạo khóa ở nơi cấu hình khi chạy.
-- Số dòng từng bảng nguồn (kể cả bảng rỗng; không chèn __EFMigrationsHistory):
-- __EFMigrationsHistory: 9
-- AccountActivityLogs: 9
-- Amenities: 10
-- Areas: 7
-- AspNetRoleClaims: 0
-- AspNetRoles: 3
-- AspNetUserClaims: 0
-- AspNetUserLogins: 0
-- AspNetUserRoles: 26
-- AspNetUsers: 25
-- AspNetUserTokens: 0
-- AuditLogs: 169
-- BankTransactions: 50
-- BookingChangeRequests: 6
-- BookingCodeCounters: 42
-- BookingNights: 153
-- Bookings: 81
-- BookingStatusHistories: 288
-- CancellationPolicies: 6
-- CancellationRules: 18
-- Contracts: 36
-- CustomerProfiles: 23
-- Customers: 23
-- Employees: 6
-- Holidays: 29
-- HomestayAmenities: 183
-- HomestayImages: 179
-- Homestays: 37
-- MessageLogs: 3
-- Notifications: 90
-- OwnerRegistrations: 8
-- Owners: 8
-- Payments: 66
-- PersonProfiles: 36
-- Reviews: 20
-- RoomBlocks: 6
-- RoomCategories: 5
-- RoomNights: 103
-- Rooms: 128
-- RoomTypePrices: 281
-- RoomTypes: 93
-- SettlementItems: 28
-- Settlements: 27
-- VerificationDocuments: 120
-- Vouchers: 4
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
        THROW 50102, N'Schema không khớp phiên bản xuất dữ liệu.', 1;
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
    DBCC CHECKIDENT (N'[dbo].[Amenities]', RESEED, 1001) WITH NO_INFOMSGS;

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
    DBCC CHECKIDENT (N'[dbo].[Areas]', RESEED, 1001) WITH NO_INFOMSGS;

    -- AspNetRoles: 3 dòng.
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES
        (N'675aab8e-8ff3-440a-9449-3117b830b249', N'Owner', N'OWNER', N'a82cf3c4-f51b-456b-83db-b3a66636df4b'),
        (N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'Employee', N'EMPLOYEE', N'996bfe20-24a5-47bd-90b0-e8e2c5149e45'),
        (N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'Customer', N'CUSTOMER', N'b0f45203-6a88-4ab2-9259-e3e1f0c15e84');

    -- BookingCodeCounters: 42 dòng.
    INSERT INTO [dbo].[BookingCodeCounters] ([Date], [LastSeq]) VALUES
        (N'2026-06-01', 6),
        (N'2026-06-05', 1),
        (N'2026-06-07', 1),
        (N'2026-06-09', 1),
        (N'2026-06-21', 1),
        (N'2026-06-25', 1),
        (N'2026-06-29', 1),
        (N'2026-07-04', 1),
        (N'2026-07-05', 1),
        (N'2026-07-06', 1),
        (N'2026-07-13', 1),
        (N'2026-07-15', 2),
        (N'2026-07-17', 1),
        (N'2026-07-18', 1),
        (N'2026-07-24', 1),
        (N'2026-07-29', 1),
        (N'2026-07-30', 1),
        (N'2026-08-01', 1),
        (N'2026-08-04', 1),
        (N'2026-08-07', 1),
        (N'2026-08-08', 1),
        (N'2026-08-09', 1),
        (N'2026-08-16', 1),
        (N'2026-08-17', 1),
        (N'2026-08-18', 1),
        (N'2026-08-21', 1),
        (N'2026-09-02', 1),
        (N'2026-09-10', 1),
        (N'2026-09-13', 1),
        (N'2026-09-20', 1),
        (N'2026-09-29', 1),
        (N'2026-10-01', 1),
        (N'2026-10-08', 2),
        (N'2026-10-09', 4),
        (N'2026-10-10', 6),
        (N'2026-10-11', 10),
        (N'2026-10-12', 3),
        (N'2026-10-13', 1),
        (N'2026-10-15', 3),
        (N'2026-10-18', 6),
        (N'2026-10-19', 7),
        (N'2026-10-20', 1);

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

    -- PersonProfiles: 36 dòng.
    SET IDENTITY_INSERT [dbo].[PersonProfiles] ON;
    INSERT INTO [dbo].[PersonProfiles] ([Id], [FullName], [PhoneNumber], [Email], [Address], [CreatedAt], [UpdatedAt]) VALUES
        (1, N'Quản trị viên Fuji', N'+84901000001', N'admin@fuji.vn', NULL, N'2026-10-11T06:38:15.4633550', N'2026-10-11T06:38:15.4633550'),
        (2, N'Nguyễn Thị Hương', N'+84901000002', N'huong.nt@fuji.vn', NULL, N'2026-10-11T06:38:15.9386370', N'2026-10-11T06:38:15.9386370'),
        (3, N'Phạm Thu Trang', N'+84901000003', N'ketoan@fuji.vn', NULL, N'2026-10-11T06:38:16.0599010', N'2026-10-11T06:38:16.0599010'),
        (4, N'Nguyễn Thị Lan', N'+84901000011', N'lan.nt@fuji.vn', NULL, N'2026-10-11T06:38:16.1979860', N'2026-10-11T06:38:16.1979870'),
        (5, N'Trần Văn Minh', N'+84901000012', N'minh.tv@fuji.vn', NULL, N'2026-10-11T06:38:16.3133490', N'2026-10-11T06:38:16.3133490'),
        (6, N'Lê Văn Hùng', N'+84912000001', NULL, NULL, N'2026-10-11T06:38:16.4442400', N'2026-10-11T06:38:16.4442400'),
        (7, N'Phạm Thu Hà', N'+84912000002', NULL, NULL, N'2026-10-11T06:38:16.6513520', N'2026-10-11T06:38:16.6513520'),
        (8, N'Đỗ Quang Huy', N'+84912000003', NULL, NULL, N'2026-10-11T06:38:16.7749500', N'2026-10-11T06:38:16.7749500'),
        (9, N'Vũ Minh Anh', N'+84912000004', NULL, NULL, N'2026-10-11T06:38:16.8961610', N'2026-10-11T06:38:16.8961610'),
        (10, N'Hoàng Thị Mai', N'+84912000005', NULL, NULL, N'2026-10-11T06:38:17.0158850', N'2026-10-11T06:38:17.0158850'),
        (11, N'Ngô Đức Thắng', N'+84912000006', NULL, NULL, N'2026-10-11T06:38:17.1414100', N'2026-10-11T06:38:17.1414100'),
        (12, N'Nguyễn Văn Minh', N'+84987000001', NULL, NULL, N'2026-10-11T06:38:17.2601710', N'2026-10-11T06:38:17.2601710'),
        (13, N'Trần Thị Bích', N'+84987000002', NULL, NULL, N'2026-10-11T06:38:17.4316900', N'2026-10-11T06:38:17.4316900'),
        (14, N'Đặng Thị Thu', N'+84987000003', NULL, NULL, N'2026-10-11T06:38:17.4712300', N'2026-10-11T06:38:17.4712300'),
        (15, N'Bùi Quốc Bảo', N'+84987000004', NULL, NULL, N'2026-10-11T06:38:17.5868040', N'2026-10-11T06:38:17.5868040'),
        (16, N'Phan Ngọc Ánh', N'+84987000005', NULL, NULL, N'2026-10-11T06:38:17.7054160', N'2026-10-11T06:38:17.7054160'),
        (17, N'Trịnh Văn Long', N'+84987000006', NULL, NULL, N'2026-10-11T06:38:17.8250550', N'2026-10-11T06:38:17.8250550'),
        (18, N'Võ Thị Hồng', N'+84987000007', NULL, NULL, N'2026-10-11T06:38:17.8370860', N'2026-10-11T06:38:17.8370870'),
        (19, N'Hồ Minh Khang', N'+84987000008', NULL, NULL, N'2026-10-11T06:38:17.9521600', N'2026-10-11T06:38:17.9521600'),
        (20, N'Lý Thanh Tâm', N'+84987000009', NULL, NULL, N'2026-10-11T06:38:17.9648560', N'2026-10-11T06:38:17.9648560'),
        (21, N'Mai Đức Anh', N'+84987000010', NULL, NULL, N'2026-10-11T06:38:18.0861380', N'2026-10-11T06:38:18.0861380'),
        (22, N'Chu Thị Ngân', N'+84987000011', NULL, NULL, N'2026-10-11T06:38:18.2045390', N'2026-10-11T06:38:18.2045390'),
        (23, N'Tạ Quang Vinh', N'+84987000012', NULL, NULL, N'2026-10-11T06:38:18.2169680', N'2026-10-11T06:38:18.2169680'),
        (24, N'Đinh Thị Yến', N'+84987000013', NULL, NULL, N'2026-10-11T06:38:18.3335990', N'2026-10-11T06:38:18.3335990'),
        (25, N'Kiều Văn Phúc', N'+84987000014', NULL, NULL, N'2026-10-11T06:38:18.4542820', N'2026-10-11T06:38:18.4542820'),
        (1003, N'PHU', N'+84984562321', NULL, NULL, N'2026-10-12T06:38:15.3705730', N'2026-10-12T06:38:15.3705720'),
        (1004, N'Đỗ Văn Toàn', N'+84384919501', NULL, NULL, N'2026-10-15T08:23:17.1045830', N'2026-10-15T08:23:17.1045820'),
        (1005, N'Khách Thử Giao Diện', N'+84988123456', NULL, NULL, N'2026-10-17T17:43:06.0041300', N'2026-10-17T17:43:06.0041300'),
        (2005, N'Chủ Thử Giao Diện', N'+84977123456', NULL, NULL, N'2026-10-17T18:08:50.5592480', N'2026-10-17T18:08:50.5593290'),
        (2006, N'Phạm Ngọc Phú', N'+84338405548', NULL, NULL, N'2026-10-18T14:08:48.0319400', N'2026-10-18T14:08:48.0319400'),
        (2007, N'Kiểm thử giao diện 03y', N'+84988123403', NULL, NULL, N'2026-10-18T15:50:23.7707700', N'2026-10-18T15:50:23.7707700'),
        (2008, N'Khách kiểm thử 05b', N'+84977050505', NULL, NULL, N'2026-10-19T07:26:02.2814560', N'2026-10-19T07:26:02.2814560'),
        (2009, N'Khách thử 05b tiếp tục', N'+84977050519', NULL, NULL, N'2026-10-19T08:33:41.8147590', N'2026-10-19T08:36:51.8512650'),
        (2010, N'Nguyễn An Kiểm Thử', N'+84905050519', N'ui05d001@example.test', NULL, N'2026-10-19T13:27:04.4273250', N'2026-10-19T13:27:04.4273570'),
        (2011, N'Chủ kiểm thử B9', N'+84977091919', NULL, NULL, N'2026-10-19T14:07:50.4349590', N'2026-10-19T14:07:50.4350160'),
        (2012, N'Khách QA 06', N'+84977060606', NULL, NULL, N'2026-10-19T15:05:15.1906630', N'2026-10-19T15:05:15.1906630');
    SET IDENTITY_INSERT [dbo].[PersonProfiles] OFF;
    DBCC CHECKIDENT (N'[dbo].[PersonProfiles]', RESEED, 3004) WITH NO_INFOMSGS;

    -- RoomCategories: 5 dòng.
    SET IDENTITY_INSERT [dbo].[RoomCategories] ON;
    INSERT INTO [dbo].[RoomCategories] ([Id], [Name]) VALUES
        (1, N'Phòng đơn'),
        (2, N'Phòng đôi'),
        (3, N'Phòng gia đình'),
        (4, N'Nguyên căn'),
        (5, N'Phòng tập thể');
    SET IDENTITY_INSERT [dbo].[RoomCategories] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomCategories]', RESEED, 2001) WITH NO_INFOMSGS;

    -- AspNetUsers: 25 dòng.
    INSERT INTO [dbo].[AspNetUsers] ([Id], [PersonId], [Status], [PasswordChangedAt], [CreatedAt], [UpdatedAt], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]) VALUES
        (N'11248390-0317-4fe5-b11d-ffd0f950f0e1', 6, N'Active', N'2026-10-11T06:38:16.5353360', N'2026-10-11T06:38:16.5353270', N'2026-10-11T06:38:16.5353270', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'11248390-0317-4FE5-B11D-FFD0F950F0E1', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEDy+Sw9cT814AbMJPeLGL/HhoNmzF7Gn6Fwbr5naqoRhIzBdEUsp+Q1s0o7LGOuVEw==', N'MD7PEGSZLRCYGJONLAMKNRNJ3PVALBWL', N'82d20cd8-3b9a-4131-b7d5-f6c0dbe40656', NULL, 0, 0, NULL, 1, 0),
        (N'18b241d3-6441-42f5-8bde-d39acb462728', 1004, N'Active', N'2026-10-15T08:23:17.1045820', N'2026-10-15T08:23:17.1476130', N'2026-10-15T08:23:17.1476130', N'18b241d3-6441-42f5-8bde-d39acb462728', N'18B241D3-6441-42F5-8BDE-D39ACB462728', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEFNNmSGQRWzmMKsGIRzm9RhgCxLozJwepyYnwr6+jpgRJQ9kOktOCKD6WxGYR/vL7Q==', N'S5NYT3J7MXAR24GSHK2O2EU4FGRXDEAN', N'ab34455f-79e5-49cc-b51a-ffc41656bbfa', NULL, 0, 0, NULL, 1, 0),
        (N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', 12, N'Active', N'2026-10-11T06:38:17.3180110', N'2026-10-11T06:38:17.3180030', N'2026-10-11T06:38:17.3180030', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'241FE00B-0D8E-4E8E-9133-9648407CCC6E', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEFrF+1Sf1XEwVcpVzkq2VzNW3ZdoRM+TkRfbE1cTNrfJtA4hnRE57dZUlmUVF/m8RA==', N'OOT4W3O3JDPC5N3J4SEPVMQ7MRSISIY3', N'ca76ca20-ab70-4bc0-923c-f8046e33e1eb', NULL, 0, 0, NULL, 1, 0),
        (N'2d3ea584-7f5a-4a2a-a6d4-306856ba3933', 11, N'Active', N'2026-10-11T06:38:17.1551860', N'2026-10-11T06:38:17.1551760', N'2026-10-11T06:38:17.1551760', N'2d3ea584-7f5a-4a2a-a6d4-306856ba3933', N'2D3EA584-7F5A-4A2A-A6D4-306856BA3933', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEL7n2a2QrjcOltWUiCh7HvnxFMKp6W0Qcz4OsqmcnYaRHl4an0hxBAz7Scm1G/6SDw==', N'VKT433S7RU2ZPIHC4IN3CR7CVHCRGFHA', N'e197ffab-6e1f-4462-9a89-6c79f1562b18', NULL, 0, 0, NULL, 1, 0),
        (N'3027af7f-546b-49bc-8598-af0d9a0034e2', 1003, N'Active', N'2026-10-12T06:38:15.3705720', N'2026-10-12T06:38:15.3803000', N'2026-10-12T06:38:15.3803000', N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'3027AF7F-546B-49BC-8598-AF0D9A0034E2', NULL, NULL, 0, N'AQAAAAIAAYagAAAAELzCWb3CLzQ2HesQe6nQVBvf1W9iu+wnL5dGdsStpLH7EGTypsbpAsZhto900akqDQ==', N'3NNEPYFWXVEREI4HJSHYXTBHJR573DII', N'7b20facf-be5d-46c2-b966-e1f331ce66a7', NULL, 0, 0, NULL, 1, 0),
        (N'38d5efb7-61dd-45f1-83fc-436930a66583', 21, N'Active', N'2026-10-11T06:38:18.1002570', N'2026-10-11T06:38:18.1002490', N'2026-10-11T06:38:18.1002490', N'38d5efb7-61dd-45f1-83fc-436930a66583', N'38D5EFB7-61DD-45F1-83FC-436930A66583', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEKkqjy+EZ2pPsu1TNEtl6GdzCK/08TyQE+9Jm19YOZqySyDhtFCGzWZlKzt8VWMYTA==', N'5ORCAXMIRL6V5G4ZOZUXQPY6OIDWRRE2', N'2d5b136c-a699-40e1-81c1-f769b359b264', NULL, 0, 0, NULL, 1, 0),
        (N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', 5, N'Active', N'2026-10-11T06:38:16.3221180', N'2026-10-11T06:38:16.3221090', N'2026-10-11T06:38:16.3221090', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'3DF96A6A-8196-4777-9712-B6E9F5ED34CD', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEHbuTj+OjGh+lan1HBVRthpv8eV5P40wEwQWpRXPE1llNbqvNiMDxemn9Ngx9zpBKg==', N'24ZROW6OFDUJ3MKT4AFRGNY3OA2K4LOZ', N'e45d2287-3c35-4371-a3b5-4017eae8bae4', NULL, 0, 0, NULL, 1, 0),
        (N'3e33295d-a909-4dfb-b209-cdfe617661a2', 15, N'Active', N'2026-10-11T06:38:17.5982220', N'2026-10-11T06:38:17.5982150', N'2026-10-11T06:38:17.5982150', N'3e33295d-a909-4dfb-b209-cdfe617661a2', N'3E33295D-A909-4DFB-B209-CDFE617661A2', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEFCBw3BolEcpEtNkYnbhuUogzLk9Y5Rqn2n2fZTSkJFW86dJ00moP+2+IFFSfHH6BA==', N'7L3ETGP4G3PD2SCHRC34H2OYZCKGG2SN', N'cbe8b6bd-2b51-43ba-8dae-a687deb5e831', NULL, 0, 0, NULL, 1, 0),
        (N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', 23, N'Active', N'2026-10-11T06:38:18.2294450', N'2026-10-11T06:38:18.2294380', N'2026-10-11T06:38:18.2294380', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', N'44D9FDCB-84B2-4B36-99D4-CA22ABBCDDA8', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEMY0HfKuv1ZE9DE50nkTkfYvTuSdsGNo/TaphnO5kBnJiNZqNsn+4tLzp+IWnxhLVA==', N'AO2JR3XOEMVREBIW7Q2FFTBNHDTBPHN4', N'8fd9307e-eeef-44b2-a0cb-a51de9004915', NULL, 0, 0, NULL, 1, 0),
        (N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', 8, N'Active', N'2026-10-11T06:38:16.7887530', N'2026-10-11T06:38:16.7887420', N'2026-10-11T06:38:16.7887420', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', N'6940757C-E23D-4C9D-A8E9-8CB911F650B5', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEBO4hLZf2S+B+06Gw+Ny32RqAYKinHaXX21ZSLYiZ/NNNAgUogD7jRoZAsG1WJ3q+g==', N'QZXYCYE462JRPZRWUQTGQWV5XEIW3U5I', N'57c8d248-cfb2-4f7b-922d-719ad779b441', NULL, 0, 0, NULL, 1, 0),
        (N'93317b46-82fc-486f-993a-d8d786f63b89', 3, N'Active', N'2026-10-11T06:38:16.0694450', N'2026-10-11T06:38:16.0694350', N'2026-10-11T06:38:16.0694350', N'93317b46-82fc-486f-993a-d8d786f63b89', N'93317B46-82FC-486F-993A-D8D786F63B89', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEBUOii0C56JF3TZLl/VPc7tywUUecnYKMYzg26FLr4+ifeBkQ1pQOpGasDmQx0Wyqw==', N'UZGS273ABTO5IZDS4HJLLFIGUDFR2Q4Y', N'd8f812e2-10f7-4ae7-ad79-e05460929a21', NULL, 0, 0, NULL, 1, 0),
        (N'a758bd81-b043-4dc1-a347-a469c8546bf1', 7, N'Active', N'2026-10-11T06:38:16.6637310', N'2026-10-11T06:38:16.6637240', N'2026-10-11T06:38:16.6637240', N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'A758BD81-B043-4DC1-A347-A469C8546BF1', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEMdZ+Albs6EDNggHNxg2ARPPOnYklS4sKMK4BFQL76D6nZbXV/UUUUZVuYoPm43hKA==', N'NECNJQCHF4J6SXRKZIQHBBV4CRZJBO7R', N'4e9d71fb-02a8-43a3-8c08-adfd9c5dc4d1', NULL, 0, 0, NULL, 1, 0),
        (N'abdc6396-ee31-41cf-b74e-ee58209136b8', 10, N'Active', N'2026-10-11T06:38:17.0295900', N'2026-10-11T06:38:17.0295800', N'2026-10-11T06:38:17.0295810', N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'ABDC6396-EE31-41CF-B74E-EE58209136B8', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEJepBlStuUJ2VnNAubnDVecBVbABxaZWeR3kJ5e0M1iImWd24jA07mrrZDNIJTyRfQ==', N'5VYJS5VH576WCXGGD2J5UJKBJTCZDT2D', N'5eafb180-01b4-4d27-a7b8-d7e86b256a65', NULL, 0, 0, NULL, 1, 0),
        (N'b2ce2e23-513a-417f-a972-5251641e4e8f', 20, N'Active', N'2026-10-11T06:38:17.9781910', N'2026-10-11T06:38:17.9781820', N'2026-10-11T06:38:17.9781830', N'b2ce2e23-513a-417f-a972-5251641e4e8f', N'B2CE2E23-513A-417F-A972-5251641E4E8F', NULL, NULL, 0, N'AQAAAAIAAYagAAAAENFfcKoxpCeiqgCSwC5ILkwmseqIvJvN6Mj/XDkrjMe28880KB5fLxkZiZ8k/S7J9w==', N'TLUJVY6CCAXQPIURDJG7YEZ5SFM2RULL', N'5b513ea0-28fc-4c96-8ca2-22539650b956', NULL, 0, 0, NULL, 1, 0),
        (N'b4623385-f904-483d-9040-fbb25af3e7f1', 9, N'Active', N'2026-10-11T06:38:16.9080910', N'2026-10-11T06:38:16.9080770', N'2026-10-11T06:38:16.9080780', N'b4623385-f904-483d-9040-fbb25af3e7f1', N'B4623385-F904-483D-9040-FBB25AF3E7F1', NULL, NULL, 0, N'AQAAAAIAAYagAAAAENblr7xSaPpaRKow9ThOppMJS9iLfI7wCsyVnDkiijmYkxdQrkNDROFBJG3Pa8YQ+A==', N'LFU7DHOPBTP6W3NSAPSKTHWHBEJ7WSPP', N'd840b2c8-d160-4da5-b9d1-4ec5119a7035', NULL, 0, 0, NULL, 1, 0),
        (N'b96a201f-88c5-4fa4-9aae-4111d62e5427', 14, N'Active', N'2026-10-11T06:38:17.4829340', N'2026-10-11T06:38:17.4829260', N'2026-10-11T06:38:17.4829260', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'B96A201F-88C5-4FA4-9AAE-4111D62E5427', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEBo3ntvUVhj6Gh1Klqd90S0xlPp62yLCqs6bJIcIyIPNEWOwvqlPJlQUXh++xQdjGw==', N'YYU34GEPZNHPNTP42UMX2ZSAMI46LDZY', N'4ad1e776-2dfc-486f-81dc-55473dd0ac34', NULL, 0, 0, NULL, 1, 0),
        (N'ba6bf082-50aa-4026-befe-c64773f49df3', 4, N'Active', N'2026-10-11T06:38:16.2079170', N'2026-10-11T06:38:16.2078950', N'2026-10-11T06:38:16.2078950', N'ba6bf082-50aa-4026-befe-c64773f49df3', N'BA6BF082-50AA-4026-BEFE-C64773F49DF3', NULL, NULL, 0, N'AQAAAAIAAYagAAAAELkInJxJX1g5/8hvWR8WSc/YOljTxZNvoCwcJLXbb42H9zObxOsfhnS5KMaO4QgMYw==', N'6GCQZH5AB6PWWZ35DXHPOLNO7QSHFJE2', N'c6dc9242-08f3-4661-9083-aeb4e3d3bb0a', NULL, 0, 0, NULL, 1, 0),
        (N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', 2, N'Active', N'2026-10-11T06:38:15.9493250', N'2026-10-11T06:38:15.9493080', N'2026-10-11T06:38:15.9493080', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'C30F83A0-0B47-47EB-B167-6910AB47DD9B', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEB2j8/0GbaqXcNPOOGI1LQuM/cMK0++s+D/TWOjP1g2sKUh52gHStckODRn/ksL8+w==', N'BX6LJ3QES4AE7ONR7GZNJ6R5MGBI25QH', N'7caf5d1a-b5e5-4dee-9ae9-b945b3804516', NULL, 0, 0, NULL, 1, 0),
        (N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', 2005, N'Active', N'2026-10-17T18:08:50.6749570', N'2026-10-17T18:08:50.6750100', N'2026-10-17T18:08:50.6750580', N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'D17A3C8F-8D18-4AAA-9804-EED0125DA9E1', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEEiC/dXstAVId8dCqrRZuTSVAaBWOovSyn3trYEfU4WWColw1z7o7pwu0wgKRYGQyQ==', N'6RXREAIB7M7E5VF2P66MQY6U6N3GIV4W', N'757de024-3037-405d-b266-2009ada9c61a', NULL, 0, 0, NULL, 1, 0),
        (N'd2507b59-e06a-4422-b6ec-cc260760ceed', 2010, N'Active', N'2026-10-19T13:28:40.0699180', N'2026-10-19T13:27:04.4276090', N'2026-10-19T13:28:40.0699180', N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'D2507B59-E06A-4422-B6EC-CC260760CEED', N'ui05d001@example.test', N'UI05D001@EXAMPLE.TEST', 0, N'AQAAAAIAAYagAAAAEEUc6yIqYTj9usFm0+BQhkdnwzPzfGlSU6164nX+R4a3KA1YdRvaVjnQO+Zngq9TtQ==', N'Z3BGHRL2FNSLBX3WR7ABWY4N7GMDJV6W', N'12b62520-3e76-44a0-b0e7-4badc4c2ea1f', NULL, 0, 0, NULL, 1, 1),
        (N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', 24, N'Active', N'2026-10-11T06:38:18.3460000', N'2026-10-11T06:38:18.3459910', N'2026-10-11T06:38:18.3459910', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'DD940858-5F5E-4CF9-B2B8-41C711D60F07', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEGo68UKlUWPnFS17nBZXC5c/QZNTHAnrISpqZlVVt66rja7JIIUEd17Qahn4R63sog==', N'SU33APSPF56IQ4QJK5PRTOV5W6CREXCE', N'385b16a1-4a2b-42f0-a9c1-1592741fc438', NULL, 0, 0, NULL, 1, 0),
        (N'e17c9e83-1cec-4fba-b30c-3e685deca18e', 2011, N'Active', N'2026-10-19T14:07:50.4593000', N'2026-10-19T14:07:50.4593360', N'2026-10-19T14:07:50.4593580', N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'E17C9E83-1CEC-4FBA-B30C-3E685DECA18E', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEGDdEPOtoVCzhqHeaWlErpI/JF+kS16z8FYoAV4v/uBlIN4BKvwjCjbXsaShACWWnA==', N'HLGQ6MRH5UUCDAUEFDGUAPFWAVA762ZI', N'7b9258e1-bf5b-4de1-b33a-fcc34cbdf1a5', NULL, 0, 0, NULL, 1, 0),
        (N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', 1, N'Active', N'2026-10-11T06:38:15.6267350', N'2026-10-11T06:38:15.6266000', N'2026-10-11T06:38:15.6266000', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'E9FDC3AE-62A4-4CAD-AE80-B9A9292BC8DD', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEOMyvE+Q0cb5ei+83+HVMkIr5XKaNSReksDFnHEpymS3oe4KqvttLcFUN+c4qjrLBA==', N'QGYUCJQUQBFRIRGRWAJ26JZLLF4NC23R', N'940c5040-7d23-4dc5-a798-1e7ed551a1c0', NULL, 0, 0, NULL, 1, 0),
        (N'f017ffe1-ef1a-45fd-89ec-072e14350174', 16, N'Active', N'2026-10-11T06:38:17.7190120', N'2026-10-11T06:38:17.7190030', N'2026-10-11T06:38:17.7190030', N'f017ffe1-ef1a-45fd-89ec-072e14350174', N'F017FFE1-EF1A-45FD-89EC-072E14350174', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEPMF1m98yrdloMtkdr0lMo/GW6WCS6xXOoi1YjHta8ci+WkZZTAwN6ko4oK/uOpLQQ==', N'JO7V4T24LIWQS7TENAADF27IFR7UQL7Y', N'5f88c3bd-b711-4102-84f4-e8c257e3ca41', NULL, 0, 0, NULL, 1, 0),
        (N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', 18, N'Active', N'2026-10-11T06:38:17.8493340', N'2026-10-11T06:38:17.8493270', N'2026-10-11T06:38:17.8493270', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', N'FA2D35BB-CC18-4145-9AAC-8CF9A3997D23', NULL, NULL, 0, N'AQAAAAIAAYagAAAAEFurfqY7DxzvYb7Hd6U2si/e2ik4E6GuDfqNudwNf6AyS8N7wawLYXeTpBF3W8P4tA==', N'A5A4ZGBKJ3HRXJG73ZDSYTX7HKCBIILS', N'1071fb51-02f2-467b-a302-eff0f3137068', NULL, 0, 0, NULL, 1, 0);

    -- Customers: 23 dòng.
    SET IDENTITY_INSERT [dbo].[Customers] ON;
    INSERT INTO [dbo].[Customers] ([Id], [PersonId], [Note], [CreatedAt], [UpdatedAt]) VALUES
        (1, 12, NULL, N'2026-10-11T06:38:17.2642310', N'2026-10-11T06:38:17.2642310'),
        (2, 13, NULL, N'2026-10-11T06:38:17.4357700', N'2026-10-11T06:38:17.4357700'),
        (3, 6, NULL, N'2026-10-11T06:38:17.4454510', N'2026-10-11T06:38:17.4454510'),
        (4, 14, NULL, N'2026-10-11T06:38:17.4750840', N'2026-10-11T06:38:17.4750840'),
        (5, 15, NULL, N'2026-10-11T06:38:17.5906440', N'2026-10-11T06:38:17.5906440'),
        (6, 16, NULL, N'2026-10-11T06:38:17.7095910', N'2026-10-11T06:38:17.7095910'),
        (7, 17, NULL, N'2026-10-11T06:38:17.8290030', N'2026-10-11T06:38:17.8290030'),
        (8, 18, NULL, N'2026-10-11T06:38:17.8411980', N'2026-10-11T06:38:17.8411980'),
        (9, 19, NULL, N'2026-10-11T06:38:17.9562530', N'2026-10-11T06:38:17.9562530'),
        (10, 20, NULL, N'2026-10-11T06:38:17.9688290', N'2026-10-11T06:38:17.9688290'),
        (11, 21, NULL, N'2026-10-11T06:38:18.0906730', N'2026-10-11T06:38:18.0906740'),
        (12, 22, NULL, N'2026-10-11T06:38:18.2084920', N'2026-10-11T06:38:18.2084920'),
        (13, 23, NULL, N'2026-10-11T06:38:18.2211440', N'2026-10-11T06:38:18.2211440'),
        (14, 24, NULL, N'2026-10-11T06:38:18.3376330', N'2026-10-11T06:38:18.3376330'),
        (15, 25, NULL, N'2026-10-11T06:38:18.4598170', N'2026-10-11T06:38:18.4598170'),
        (1003, 1003, NULL, N'2026-10-12T06:38:15.3714440', N'2026-10-12T06:38:15.3714440'),
        (1004, 1004, NULL, N'2026-10-15T08:23:17.1053850', N'2026-10-15T08:23:17.1053850'),
        (1005, 1005, NULL, N'2026-10-17T17:43:06.0041300', N'2026-10-17T17:43:06.0041300'),
        (2005, 2006, NULL, N'2026-10-18T14:08:48.0319400', N'2026-10-18T14:08:48.0319400'),
        (2006, 2007, NULL, N'2026-10-18T15:50:23.7707700', N'2026-10-18T15:50:23.7707700'),
        (2007, 2008, NULL, N'2026-10-19T07:26:02.2814560', N'2026-10-19T07:26:02.2814560'),
        (2008, 2009, N'Kiểm thử 05b tiếp tục: khách liên hệ qua Zalo, kiểm tra ghi chú hồ sơ.', N'2026-10-19T08:33:41.8147590', N'2026-10-19T08:36:51.8512650'),
        (2009, 2012, NULL, N'2026-10-19T15:05:15.1906630', N'2026-10-19T15:05:15.1906630');
    SET IDENTITY_INSERT [dbo].[Customers] OFF;
    DBCC CHECKIDENT (N'[dbo].[Customers]', RESEED, 3004) WITH NO_INFOMSGS;

    -- Employees: 6 dòng.
    SET IDENTITY_INSERT [dbo].[Employees] ON;
    INSERT INTO [dbo].[Employees] ([Id], [PersonId], [EmployeeCode], [Department], [Position], [EmploymentStatus], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'AD001', N'Kỹ thuật', N'Administrator', N'Active', N'2026-10-11T06:38:15.5396260', N'2026-10-11T06:38:15.5396260'),
        (2, 2, N'QL001', N'Ban giám đốc', N'Manager', N'Active', N'2026-10-11T06:38:15.9433260', N'2026-10-11T06:38:15.9433260'),
        (3, 3, N'KT001', N'Kế toán', N'Accountant', N'Active', N'2026-10-11T06:38:16.0635990', N'2026-10-11T06:38:16.0635990'),
        (4, 4, N'NV001', N'Tư vấn đặt phòng', N'Consultant', N'Active', N'2026-10-11T06:38:16.2019000', N'2026-10-11T06:38:16.2019000'),
        (5, 5, N'NV002', N'Tư vấn đặt phòng', N'Consultant', N'Active', N'2026-10-11T06:38:16.3169020', N'2026-10-11T06:38:16.3169020'),
        (1002, 2010, N'UI05D001', N'Vận hành thử nghiệm', N'Consultant', N'Active', N'2026-10-19T13:27:04.4274800', N'2026-10-19T13:27:04.4275000');
    SET IDENTITY_INSERT [dbo].[Employees] OFF;
    DBCC CHECKIDENT (N'[dbo].[Employees]', RESEED, 2001) WITH NO_INFOMSGS;

    -- Owners: 8 dòng.
    SET IDENTITY_INSERT [dbo].[Owners] ON;
    INSERT INTO [dbo].[Owners] ([Id], [PersonId], [BankName], [BankAccountNo], [BankAccountName], [CreatedAt], [UpdatedAt]) VALUES
        (1, 6, N'MB Bank', N'0912000001', N'LÊ VĂN HÙNG', N'2026-10-11T06:38:16.4482810', N'2026-10-11T06:38:16.4482810'),
        (2, 7, N'MB Bank', N'0912000002', N'PHẠM THU HÀ', N'2026-10-11T06:38:16.6551610', N'2026-10-11T06:38:16.6551610'),
        (3, 8, N'MB Bank', N'0912000003', N'ĐỖ QUANG HUY', N'2026-10-11T06:38:16.7788750', N'2026-10-11T06:38:16.7788760'),
        (4, 9, N'MB Bank', N'0912000004', N'VŨ MINH ANH', N'2026-10-11T06:38:16.8998790', N'2026-10-11T06:38:16.8998790'),
        (5, 10, N'MB Bank', N'0912000005', N'HOÀNG THỊ MAI', N'2026-10-11T06:38:17.0199710', N'2026-10-11T06:38:17.0199710'),
        (6, 11, NULL, NULL, NULL, N'2026-10-11T06:38:17.1456360', N'2026-10-11T06:38:17.1456360'),
        (1002, 2005, N'MB Bank', N'0123456789', N'CHU THU GIAO DIEN', N'2026-10-17T18:08:50.5593670', N'2026-10-17T18:08:50.5595160'),
        (1003, 2011, N'MB - minh họa B9', N'0000091919', N'CHU KIEM THU B9', N'2026-10-19T14:07:50.4350410', N'2026-10-19T14:07:50.4351420');
    SET IDENTITY_INSERT [dbo].[Owners] OFF;
    DBCC CHECKIDENT (N'[dbo].[Owners]', RESEED, 2001) WITH NO_INFOMSGS;

    -- AccountActivityLogs: 9 dòng.
    SET IDENTITY_INSERT [dbo].[AccountActivityLogs] ON;
    INSERT INTO [dbo].[AccountActivityLogs] ([Id], [AccountId], [EventType], [ActorType], [ActorId], [Detail], [IpAddress], [CreatedAt]) VALUES
        (1, N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'AccountCreated', N'Customer', N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'Tự đăng ký (SĐT + mật khẩu)', N'::1', N'2026-10-12T06:38:15.5575790'),
        (2, N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'RoleAssigned', N'System', NULL, N'Customer', N'::1', N'2026-10-12T06:38:15.5850300'),
        (3, N'18b241d3-6441-42f5-8bde-d39acb462728', N'AccountCreated', N'Customer', N'18b241d3-6441-42f5-8bde-d39acb462728', N'Tự đăng ký (SĐT + mật khẩu)', N'::1', N'2026-10-15T08:23:17.3132200'),
        (4, N'18b241d3-6441-42f5-8bde-d39acb462728', N'RoleAssigned', N'System', NULL, N'Customer', N'::1', N'2026-10-15T08:23:17.3392280'),
        (5, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'AccountCreated', N'Employee', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'Quản trị viên tạo tài khoản nhân viên.', N'::1', N'2026-10-19T13:27:04.7958430'),
        (6, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'RoleAssigned', N'Employee', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'Employee', N'::1', N'2026-10-19T13:27:04.7958440'),
        (7, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'StatusChanged', N'Employee', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'UI05d: kiểm thử khóa nhân viên mới.', N'::1', N'2026-10-19T13:28:39.1301490'),
        (8, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'StatusChanged', N'Employee', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'UI05d: mở lại sau kiểm thử.', N'::1', N'2026-10-19T13:28:39.5484050'),
        (9, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'PasswordReset', N'Employee', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'Quản trị viên đặt lại mật khẩu nhân viên.', N'::1', N'2026-10-19T13:28:40.0702390');
    SET IDENTITY_INSERT [dbo].[AccountActivityLogs] OFF;
    DBCC CHECKIDENT (N'[dbo].[AccountActivityLogs]', RESEED, 10004) WITH NO_INFOMSGS;

    -- AspNetUserRoles: 26 dòng.
    INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId], [AssignedAt], [AssignedByAccountId]) VALUES
        (N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:16.6361380', NULL),
        (N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.4621540', NULL),
        (N'18b241d3-6441-42f5-8bde-d39acb462728', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-15T08:23:17.2870350', NULL),
        (N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.4175040', NULL),
        (N'2d3ea584-7f5a-4a2a-a6d4-306856ba3933', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:17.2512050', NULL),
        (N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-12T06:38:15.5246400', NULL),
        (N'38d5efb7-61dd-45f1-83fc-436930a66583', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:18.1953710', NULL),
        (N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-11T06:38:16.4176980', NULL),
        (N'3e33295d-a909-4dfb-b209-cdfe617661a2', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.6950080', NULL),
        (N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:18.3247540', NULL),
        (N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:16.8875100', NULL),
        (N'93317b46-82fc-486f-993a-d8d786f63b89', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-11T06:38:16.1832470', NULL),
        (N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:16.7645720', NULL),
        (N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:17.1301590', NULL),
        (N'b2ce2e23-513a-417f-a972-5251641e4e8f', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:18.0757440', NULL),
        (N'b4623385-f904-483d-9040-fbb25af3e7f1', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-11T06:38:17.0040670', NULL),
        (N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.5775610', NULL),
        (N'ba6bf082-50aa-4026-befe-c64773f49df3', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-11T06:38:16.3043640', NULL),
        (N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-11T06:38:16.0486660', NULL),
        (N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-17T18:08:50.8108150', NULL),
        (N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-19T13:27:04.7113070', N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd'),
        (N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:18.4420130', NULL),
        (N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'675aab8e-8ff3-440a-9449-3117b830b249', N'2026-10-19T14:07:50.5828100', NULL),
        (N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'a1e7353b-c02b-48ff-af9b-dbfd5db251ce', N'2026-10-11T06:38:15.8529400', NULL),
        (N'f017ffe1-ef1a-45fd-89ec-072e14350174', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.8164900', NULL),
        (N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', N'e84f6eb8-23e5-442b-b067-20faaf3568c1', N'2026-10-11T06:38:17.9434400', NULL);

    -- AuditLogs: 169 dòng.
    SET IDENTITY_INSERT [dbo].[AuditLogs] ON;
    INSERT INTO [dbo].[AuditLogs] ([Id], [AccountId], [TableName], [RecordId], [Action], [OldValues], [NewValues], [IpAddress], [CreatedAt]) VALUES
        (1, NULL, N'Contracts', N'1', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0001","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (2, NULL, N'Contracts', N'2', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0002","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (3, NULL, N'Contracts', N'3', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0003","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (4, NULL, N'Contracts', N'4', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0004","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (5, NULL, N'Contracts', N'5', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0005","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (6, NULL, N'Contracts', N'6', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0006","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (7, NULL, N'Contracts', N'7', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0007","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (8, NULL, N'Contracts', N'8', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0008","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (9, NULL, N'Contracts', N'9', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0009","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (10, NULL, N'Contracts', N'10', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0010","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (11, NULL, N'Contracts', N'11', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0011","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (12, NULL, N'Contracts', N'12', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0012","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (13, NULL, N'Contracts', N'13', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0013","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (14, NULL, N'Contracts', N'14', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0014","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (15, NULL, N'Contracts', N'15', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0015","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (16, NULL, N'Contracts', N'16', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0016","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (17, NULL, N'Contracts', N'17', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0017","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (18, NULL, N'Contracts', N'18', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0018","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (19, NULL, N'Contracts', N'19', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0019","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (20, NULL, N'Contracts', N'20', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0020","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (21, NULL, N'Contracts', N'21', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0021","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (22, NULL, N'Contracts', N'22', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0022","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (23, NULL, N'Contracts', N'23', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0023","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (24, NULL, N'Contracts', N'24', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0024","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (25, NULL, N'Contracts', N'25', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0025","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (26, NULL, N'Contracts', N'26', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0026","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (27, NULL, N'Contracts', N'27', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0027","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (28, NULL, N'Contracts', N'28', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0028","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (29, NULL, N'Contracts', N'29', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0029","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (30, NULL, N'Contracts', N'30', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0030","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (31, NULL, N'Contracts', N'31', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0031","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (32, NULL, N'Contracts', N'32', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0032","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (33, NULL, N'Contracts', N'33', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0033","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (34, NULL, N'Contracts', N'34', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0034","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (35, NULL, N'Contracts', N'35', N'Update', N'{"EndDate":"2027-05-31"}', N'{"Actor":"System","Step":"SeedTopup2028","ContractNo":"HD-2026-0035","EndDate":"2028-05-31"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (36, NULL, N'Holidays', N'2028-01-01', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-01","Name":"Tết Dương lịch"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (37, NULL, N'Holidays', N'2028-01-25', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-25","Name":"Tết Nguyên đán"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (38, NULL, N'Holidays', N'2028-01-26', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-26","Name":"Tết Nguyên đán"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (39, NULL, N'Holidays', N'2028-01-27', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-27","Name":"Tết Nguyên đán"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (40, NULL, N'Holidays', N'2028-01-28', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-28","Name":"Tết Nguyên đán"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (41, NULL, N'Holidays', N'2028-01-29', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-01-29","Name":"Tết Nguyên đán"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (42, NULL, N'Holidays', N'2028-04-30', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-04-30","Name":"Ngày Giải phóng miền Nam"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (43, NULL, N'Holidays', N'2028-05-01', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-05-01","Name":"Quốc tế Lao động"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (44, NULL, N'Holidays', N'2028-09-02', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","Date":"2028-09-02","Name":"Quốc khánh"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (45, NULL, N'VerificationDocuments', N'80', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":1,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (46, NULL, N'VerificationDocuments', N'81', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":6,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (47, NULL, N'VerificationDocuments', N'82', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":11,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (48, NULL, N'VerificationDocuments', N'83', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":16,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (49, NULL, N'VerificationDocuments', N'84', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":21,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (50, NULL, N'VerificationDocuments', N'85', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":26,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (51, NULL, N'VerificationDocuments', N'86', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":31,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (52, NULL, N'VerificationDocuments', N'87', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":2,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (53, NULL, N'VerificationDocuments', N'88', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":7,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (54, NULL, N'VerificationDocuments', N'89', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":12,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (55, NULL, N'VerificationDocuments', N'90', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":17,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (56, NULL, N'VerificationDocuments', N'91', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":22,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (57, NULL, N'VerificationDocuments', N'92', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":27,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (58, NULL, N'VerificationDocuments', N'93', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":32,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (59, NULL, N'VerificationDocuments', N'94', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":3,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (60, NULL, N'VerificationDocuments', N'95', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":8,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (61, NULL, N'VerificationDocuments', N'96', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":13,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (62, NULL, N'VerificationDocuments', N'97', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":18,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (63, NULL, N'VerificationDocuments', N'98', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":23,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (64, NULL, N'VerificationDocuments', N'99', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":28,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (65, NULL, N'VerificationDocuments', N'100', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":33,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (66, NULL, N'VerificationDocuments', N'101', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":4,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (67, NULL, N'VerificationDocuments', N'102', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":9,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (68, NULL, N'VerificationDocuments', N'103', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":14,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (69, NULL, N'VerificationDocuments', N'104', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":19,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (70, NULL, N'VerificationDocuments', N'105', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":24,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (71, NULL, N'VerificationDocuments', N'106', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":29,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (72, NULL, N'VerificationDocuments', N'107', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":34,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (73, NULL, N'VerificationDocuments', N'108', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":5,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (74, NULL, N'VerificationDocuments', N'109', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":10,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (75, NULL, N'VerificationDocuments', N'110', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":15,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (76, NULL, N'VerificationDocuments', N'111', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":20,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (77, NULL, N'VerificationDocuments', N'112', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":25,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (78, NULL, N'VerificationDocuments', N'113', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":30,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (79, NULL, N'VerificationDocuments', N'114', N'Insert', NULL, N'{"Actor":"System","Step":"SeedTopup2028","HomestayId":35,"DocumentType":"PropertyPhoto","FileUrl":"sample-document.png"}', NULL, N'2026-10-17T11:04:05.9041355'),
        (80, NULL, N'OwnerRegistrations', N'6', N'Update', N'{"SubmittedAt":"2026-10-11T06:38:17.145622"}', N'{"Actor":"System","Values":{"Step":"SlaAlert","SubmittedAt":"2026-10-11T06:38:17.145622","AlertedAt":"2026-10-17T11:50:05.130187Z"}}', NULL, N'2026-10-17T11:50:05.1549620'),
        (81, NULL, N'Settlements', N'53', N'Insert', N'null', N'{"Actor":"System","Values":{"Settlement":{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Draft","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":null,"ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Items":[{"BookingId":51,"ItemType":0,"RoomAmount":380000,"DepositAmount":114000,"RefundAmount":0,"RetainedAmount":114000,"CommissionRate":0.1500,"CommissionAmount":57000,"PayoutAmount":57000,"DiscountAmount":0}]}}', NULL, N'2026-10-17T17:17:04.2183580'),
        (82, NULL, N'PersonProfiles', N'1005', N'Insert', N'null', N'{"Actor":"Customer","Values":{"FullName":"Kh\u00E1ch Th\u1EED Giao Di\u1EC7n","PhoneNumber":"\u002B84988123456","Email":null}}', NULL, N'2026-10-17T17:43:06.1418000'),
        (83, NULL, N'Customers', N'1005', N'Insert', N'null', N'{"Actor":"Customer","Values":{"PersonId":1005}}', NULL, N'2026-10-17T17:43:06.2433930'),
        (10002, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'OwnerRegistrations', N'1002', N'Insert', N'null', N'{"Actor":"Owner","Values":{"OwnerId":1002,"Profile":{"FullName":"Ch\u1EE7 Th\u1EED Giao Di\u1EC7n","PhoneNumber":"\u002B84977123456","BankName":"MB Bank","BankAccountNo":"0123456789","BankAccountName":"CHU THU GIAO DIEN"},"Registration":{"OwnerId":1002,"Source":"SelfService","Status":"Draft","CreatedByEmployeeId":null,"SubmittedAt":null,"ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-17T18:08:50.843251Z"}}}', NULL, N'2026-10-17T18:08:50.9554940'),
        (10003, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'Homestays', N'1002', N'Insert', N'null', N'{"Actor":"Owner","Values":{"OwnerId":1002,"Name":"Nh\u00E0 G\u1ED7 Th\u1EED Giao Di\u1EC7n","Address":"12 Ng\u00F5 Th\u1EED, T\u00E2y H\u1ED3, H\u00E0 N\u1ED9i","AreaId":4,"ContactPhone":"\u002B84977123457","DescriptionHtml":"Ba ph\u0026#242;ng nh\u0026#236;n ra v\u01B0\u1EDDn, c\u0026#225;ch h\u1ED3 T\u0026#226;y 5 ph\u0026#250;t \u0111i b\u1ED9.","Status":"Draft","UpdatedAt":"2026-10-17T18:08:50.843689Z"}}', NULL, N'2026-10-17T18:08:50.9797080'),
        (10004, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'VerificationDocuments', N'1080', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1002,"Type":"BusinessLicense","FileUrl":"HS1002-e6fa447e14b748aeb7fc7033e2053dbd.png","ExpiryDate":null}}', NULL, N'2026-10-17T18:08:52.1123820'),
        (10005, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'VerificationDocuments', N'1081', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1002,"Type":"FireSafety","FileUrl":"HS1002-5d23e601705241acb8993b3c96d0713b.png","ExpiryDate":null}}', NULL, N'2026-10-17T18:08:52.8003250'),
        (10006, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'VerificationDocuments', N'1082', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1002,"Type":"PropertyPhoto","FileUrl":"HS1002-e2936ba030384919889a84149e94551a.png","ExpiryDate":null}}', NULL, N'2026-10-17T18:08:53.4693240'),
        (10007, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'Homestays', N'1002', N'Update', N'{"OwnerId":1002,"Name":"Nh\u00E0 G\u1ED7 Th\u1EED Giao Di\u1EC7n","Address":"12 Ng\u00F5 Th\u1EED, T\u00E2y H\u1ED3, H\u00E0 N\u1ED9i","AreaId":4,"ContactPhone":"\u002B84977123457","DescriptionHtml":"Ba ph\u0026#242;ng nh\u0026#236;n ra v\u01B0\u1EDDn, c\u0026#225;ch h\u1ED3 T\u0026#226;y 5 ph\u0026#250;t \u0111i b\u1ED9.","Status":"Draft","UpdatedAt":"2026-10-17T18:08:50.843689"}', N'{"Actor":"Owner","Values":{"OwnerId":1002,"Name":"Nh\u00E0 G\u1ED7 Th\u1EED Giao Di\u1EC7n","Address":"12 Ng\u00F5 Th\u1EED, T\u00E2y H\u1ED3, H\u00E0 N\u1ED9i","AreaId":4,"ContactPhone":"\u002B84977123457","DescriptionHtml":"Ba ph\u0026#242;ng nh\u0026#236;n ra v\u01B0\u1EDDn, c\u0026#225;ch h\u1ED3 T\u0026#226;y 5 ph\u0026#250;t \u0111i b\u1ED9.","Status":"PendingReview","UpdatedAt":"2026-10-17T18:08:55.230284Z"}}', NULL, N'2026-10-17T18:08:55.2305020'),
        (10008, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'OwnerRegistrations', N'1002', N'Update', N'{"OwnerId":1002,"Source":"SelfService","Status":"Draft","CreatedByEmployeeId":null,"SubmittedAt":null,"ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-17T18:08:50.843251"}', N'{"Actor":"Owner","Values":{"OwnerId":1002,"Source":"SelfService","Status":"PendingReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-17T18:08:55.230778Z","ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-17T18:08:55.230821Z"}}', NULL, N'2026-10-17T18:08:55.2313520'),
        (10009, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'BookingChangeRequests', N'6', N'Insert', NULL, N'{"Step":"Submitted","RefundPercent":null,"Request":{"Id":6,"BookingId":55,"Type":"ChangeDates","Status":"Pending","RequestedByAccountId":"241fe00b-0d8e-4e8e-9133-9648407ccc6e","NewCheckIn":"2026-12-01","NewCheckOut":"2026-12-02","NewRoomTypeId":null,"NewRoomId":null,"NewTotalAmount":800000,"ChangeFee":0,"DepositCarriedOver":240000,"AdditionalDeposit":0,"RefundAmount":null,"DecidedByAccountId":null,"DecidedAt":null,"DecisionNote":null,"RejectionReason":null,"PaymentDeadlineAt":null,"FinanceConfirmedByAccountId":null,"FinanceConfirmedAt":null,"FinanceNote":null},"Booking":{"Id":55,"Code":"FJ261011-010","CheckIn":"2026-11-29","CheckOut":"2026-11-30","RoomTypeId":77,"RoomId":107,"TotalAmount":800000,"DepositAmount":240000,"ChangeFeeAmount":0,"DiscountAmount":0,"RefundAmount":null,"Status":"Confirmed","ConfirmedAt":"2026-10-10T23:04:19.308424","ConfirmDeadlineAt":"2026-10-10T23:16:19.308424","ConfirmAlertSentAt":null,"CancellationPolicyId":1}}', NULL, N'2026-10-17T18:21:19.1390690'),
        (10010, NULL, N'PersonProfiles', N'2006', N'Insert', N'null', N'{"Actor":"Customer","Values":{"FullName":"Ph\u1EA1m Ng\u1ECDc Ph\u00FA","PhoneNumber":"\u002B84338405548","Email":null}}', NULL, N'2026-10-18T14:08:48.1572880'),
        (10011, NULL, N'Customers', N'2005', N'Insert', N'null', N'{"Actor":"Customer","Values":{"PersonId":2006}}', NULL, N'2026-10-18T14:08:48.2529750'),
        (10012, NULL, N'PersonProfiles', N'2007', N'Insert', N'null', N'{"Actor":"Customer","Values":{"FullName":"Ki\u1EC3m th\u1EED giao di\u1EC7n 03y","PhoneNumber":"\u002B84988123403","Email":null}}', NULL, N'2026-10-18T15:50:23.8963500'),
        (10013, NULL, N'Customers', N'2006', N'Insert', N'null', N'{"Actor":"Customer","Values":{"PersonId":2007}}', NULL, N'2026-10-18T15:50:23.9801550'),
        (10014, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'RoomCategories', N'1002', N'Delete', N'{"Id":1002,"Name":"Ki\u1EC3m tra UI04 \u0111\u00E3 s\u1EEDa 20261019"}', N'{"Actor":"Employee","Values":{"Deleted":true}}', NULL, N'2026-10-19T01:48:06.5242670'),
        (10015, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomTypePrices', N'1002', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":1002,"RoomTypeId":14,"DayType":"Weekday","Price":905000,"EffectiveFrom":"2026-10-19"}}', NULL, N'2026-10-19T02:37:44.2106990'),
        (10016, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'HomestayImages', N'348', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":348,"HomestayId":6,"RoomTypeId":null,"Url":"/uploads/homestays/6/a3042354f3d64daeb1732422ada67b34.webp","SortOrder":0,"IsCover":false}}', NULL, N'2026-10-19T02:38:01.4250710'),
        (10017, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'7', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":7,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}}', NULL, N'2026-10-19T02:40:00.6478400'),
        (10018, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'7', N'Delete', N'{"Id":7,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T02:40:24.0292890');
    INSERT INTO [dbo].[AuditLogs] ([Id], [AccountId], [TableName], [RecordId], [Action], [OldValues], [NewValues], [IpAddress], [CreatedAt]) VALUES
        (10019, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Reviews', N'7', N'Update', N'{"Id":7,"BookingId":33,"HomestayId":16,"Rating":4,"Comment":"\u0110\u00E1ng ti\u1EC1n, ch\u1EC9 mong c\u00F3 th\u00EAm \u1EA5m si\u00EAu t\u1ED1c trong ph\u00F2ng.","OwnerReply":null,"RepliedAt":null,"IsHidden":false}', N'{"Actor":"Owner","Values":{"Id":7,"BookingId":33,"HomestayId":16,"Rating":4,"Comment":"\u0110\u00E1ng ti\u1EC1n, ch\u1EC9 mong c\u00F3 th\u00EAm \u1EA5m si\u00EAu t\u1ED1c trong ph\u00F2ng.","OwnerReply":"C\u1EA3m \u01A1n b\u1EA1n \u0111\u00E3 chia s\u1EBB tr\u1EA3i nghi\u1EC7m. Fuji \u0111\u00E3 ghi nh\u1EADn g\u00F3p \u00FD \u0111\u1EC3 chu\u1EA9n b\u1ECB ph\u00F2ng chu \u0111\u00E1o h\u01A1n cho l\u1EA7n l\u01B0u tr\u00FA t\u1EDBi.","RepliedAt":"2026-10-19T02:42:42.473044Z","IsHidden":false}}', NULL, N'2026-10-19T02:42:42.4801240'),
        (10020, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'8', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":8,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}}', NULL, N'2026-10-19T02:44:02.3337480'),
        (10021, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'8', N'Delete', N'{"Id":8,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T02:44:15.9558710'),
        (10022, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'HomestayImages', N'348', N'Delete', N'{"Id":348,"HomestayId":6,"RoomTypeId":null,"Url":"/uploads/homestays/6/a3042354f3d64daeb1732422ada67b34.webp","SortOrder":0,"IsCover":false}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T02:54:22.0950710'),
        (10023, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomTypePrices', N'1003', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":1003,"RoomTypeId":14,"DayType":"Weekday","Price":905000,"EffectiveFrom":"2026-10-20"}}', NULL, N'2026-10-19T07:00:49.9387080'),
        (10024, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'HomestayImages', N'349', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":349,"HomestayId":6,"RoomTypeId":null,"Url":"/uploads/homestays/6/332d08ee9dca44b09a20704e57e4fe95.webp","SortOrder":0,"IsCover":false}}', NULL, N'2026-10-19T07:00:51.0590620'),
        (10025, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'HomestayImages', N'349', N'Delete', N'{"Id":349,"HomestayId":6,"RoomTypeId":null,"Url":"/uploads/homestays/6/332d08ee9dca44b09a20704e57e4fe95.webp","SortOrder":0,"IsCover":false}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T07:00:52.3994090'),
        (10026, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'9', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":9,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}}', NULL, N'2026-10-19T07:00:54.0295340'),
        (10027, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'9', N'Delete', N'{"Id":9,"RoomId":20,"StartDate":"2026-10-27","EndDate":"2026-10-29","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T07:00:54.6495240'),
        (10028, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Reviews', N'7', N'Update', N'{"Id":7,"BookingId":33,"HomestayId":16,"Rating":4,"Comment":"\u0110\u00E1ng ti\u1EC1n, ch\u1EC9 mong c\u00F3 th\u00EAm \u1EA5m si\u00EAu t\u1ED1c trong ph\u00F2ng.","OwnerReply":"C\u1EA3m \u01A1n b\u1EA1n \u0111\u00E3 chia s\u1EBB tr\u1EA3i nghi\u1EC7m. Fuji \u0111\u00E3 ghi nh\u1EADn g\u00F3p \u00FD \u0111\u1EC3 chu\u1EA9n b\u1ECB ph\u00F2ng chu \u0111\u00E1o h\u01A1n cho l\u1EA7n l\u01B0u tr\u00FA t\u1EDBi.","RepliedAt":"2026-10-19T02:42:42.473044","IsHidden":false}', N'{"Actor":"Owner","Values":{"Id":7,"BookingId":33,"HomestayId":16,"Rating":4,"Comment":"\u0110\u00E1ng ti\u1EC1n, ch\u1EC9 mong c\u00F3 th\u00EAm \u1EA5m si\u00EAu t\u1ED1c trong ph\u00F2ng.","OwnerReply":"C\u1EA3m \u01A1n b\u1EA1n \u0111\u00E3 chia s\u1EBB tr\u1EA3i nghi\u1EC7m. Fuji \u0111\u00E3 ghi nh\u1EADn g\u00F3p \u00FD \u0111\u1EC3 chu\u1EA9n b\u1ECB ph\u00F2ng chu \u0111\u00E1o h\u01A1n cho l\u1EA7n l\u01B0u tr\u00FA t\u1EDBi.","RepliedAt":"2026-10-19T07:00:55.840082Z","IsHidden":false}}', NULL, N'2026-10-19T07:00:55.8524080'),
        (10029, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'PersonProfiles', N'2008', N'Insert', N'null', N'{"Actor":"Employee","Values":{"FullName":"Kh\u00E1ch ki\u1EC3m th\u1EED 05b","PhoneNumber":"\u002B84977050505","Email":null}}', NULL, N'2026-10-19T07:26:02.4137900'),
        (10030, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Customers', N'2007', N'Insert', N'null', N'{"Actor":"Employee","Values":{"PersonId":2008}}', NULL, N'2026-10-19T07:26:02.5060310'),
        (10031, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Bookings', N'3013', N'Update', N'{"AssignedEmployeeId":null,"CreatedByEmployeeId":null}', N'{"Actor":"Employee","Values":{"AssignedEmployeeId":4,"CreatedByEmployeeId":null}}', NULL, N'2026-10-19T07:27:46.4187760'),
        (10032, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"AssignedEmployeeId":4,"CreatedByEmployeeId":4}', N'{"Actor":"Employee","Values":{"AssignedEmployeeId":5,"CreatedByEmployeeId":4}}', NULL, N'2026-10-19T07:30:40.5423000'),
        (10033, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"Status":"Deposited","RoomId":19,"RefundAmount":null}', N'{"Actor":"Employee","Values":{"Status":"Confirmed","RoomId":19,"RefundAmount":null,"Note":"Ki\u1EC3m th\u1EED 05b: x\u00E1c nh\u1EADn \u0111\u01A1n th\u1EED tr\u01B0\u1EDBc khi di\u1EC5n t\u1EADp chuy\u1EC3n ph\u00F2ng.","OnBehalfOfOwner":true}}', NULL, N'2026-10-19T07:31:35.0623030'),
        (10034, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":19,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}', N'{"Actor":"Employee","Values":{"Step":"OverbookingReported","Note":"Ki\u1EC3m th\u1EED 05b: di\u1EC5n t\u1EADp s\u1EF1 c\u1ED1 tr\u00EAn \u0111\u01A1n th\u1EED ri\u00EAng, kh\u00E1ch \u0111\u1ED3ng \u00FD \u0111\u1ED5i ph\u00F2ng c\u00F9ng lo\u1EA1i.","Booking":{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":19,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}}}', NULL, N'2026-10-19T07:31:36.1543830'),
        (10035, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":19,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}', N'{"Actor":"Employee","Values":{"Step":"OverbookingRoomMoved","Note":"Ki\u1EC3m th\u1EED 05b: kh\u00E1ch \u0111\u1ED3ng \u00FD chuy\u1EC3n sang ph\u00F2ng tr\u1ED1ng c\u00F9ng lo\u1EA1i, kh\u00F4ng \u0111\u1ED5i gi\u00E1/c\u1ECDc.","Booking":{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":20,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}}}', NULL, N'2026-10-19T07:32:34.2106390'),
        (10036, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'1002', N'Update', N'{"OwnerId":1002,"Source":"SelfService","Status":"PendingReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-17T18:08:55.230778","ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-17T18:08:55.230821"}', N'{"Actor":"Employee","Values":{"OwnerId":1002,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-17T18:08:55.230778","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T07:34:09.979663Z","ReviewNote":null,"UpdatedAt":"2026-10-19T07:34:09.979687Z"}}', NULL, N'2026-10-19T07:34:09.9881920'),
        (10037, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'1002', N'Update', N'{"OwnerId":1002,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-17T18:08:55.230778","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T07:34:09.979663","ReviewNote":null,"UpdatedAt":"2026-10-19T07:34:09.979687"}', N'{"Actor":"Employee","Values":{"OwnerId":1002,"Source":"SelfService","Status":"NeedsRevision","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-17T18:08:55.230778","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T07:40:23.214608Z","ReviewNote":"Ki\u1EC3m th\u1EED 05b: vui l\u00F2ng b\u1ED5 sung gi\u1EA5y t\u1EDD th\u1EF1c t\u1EBF thay c\u00E1c t\u1EC7p m\u1EABu tr\u01B0\u1EDBc khi th\u1EA9m \u0111\u1ECBnh ch\u00EDnh th\u1EE9c.","UpdatedAt":"2026-10-19T07:40:23.214632Z"}}', NULL, N'2026-10-19T07:40:23.2266280'),
        (10038, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Bookings', N'3014', N'Update', N'{"AssignedEmployeeId":null,"CreatedByEmployeeId":null}', N'{"Actor":"Employee","Values":{"AssignedEmployeeId":4,"CreatedByEmployeeId":null}}', NULL, N'2026-10-19T08:33:09.8116440'),
        (10039, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'PersonProfiles', N'2009', N'Insert', N'null', N'{"Actor":"Employee","Values":{"FullName":"Kh\u00E1ch th\u1EED 05b ti\u1EBFp t\u1EE5c","PhoneNumber":"\u002B84977050519","Email":null}}', NULL, N'2026-10-19T08:33:41.8473980'),
        (10040, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Customers', N'2008', N'Insert', N'null', N'{"Actor":"Employee","Values":{"PersonId":2009}}', NULL, N'2026-10-19T08:33:41.8937060'),
        (10041, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"AssignedEmployeeId":5,"CreatedByEmployeeId":4}', N'{"Actor":"Employee","Values":{"AssignedEmployeeId":5,"CreatedByEmployeeId":4}}', NULL, N'2026-10-19T08:34:05.6474790'),
        (10042, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'6', N'Update', N'{"OwnerId":6,"Source":"SelfService","Status":"PendingReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-11T06:38:17.145622","ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-11T06:38:17.145637"}', N'{"Actor":"Employee","Values":{"OwnerId":6,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-11T06:38:17.145622","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T08:34:19.735397Z","ReviewNote":null,"UpdatedAt":"2026-10-19T08:34:19.73542Z"}}', NULL, N'2026-10-19T08:34:19.7463850'),
        (10043, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'6', N'Update', N'{"OwnerId":6,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-11T06:38:17.145622","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T08:34:19.735397","ReviewNote":null,"UpdatedAt":"2026-10-19T08:34:19.73542"}', N'{"Actor":"Employee","Values":{"OwnerId":6,"Source":"SelfService","Status":"NeedsRevision","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-11T06:38:17.145622","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T08:34:20.8827Z","ReviewNote":"Ki\u1EC3m th\u1EED 05b ti\u1EBFp t\u1EE5c: b\u1ED5 sung th\u00F4ng tin c\u01A1 s\u1EDF l\u01B0u tr\u00FA g\u1EAFn v\u1EDBi h\u1ED3 s\u01A1 tr\u01B0\u1EDBc khi duy\u1EC7t.","UpdatedAt":"2026-10-19T08:34:20.882701Z"}}', NULL, N'2026-10-19T08:34:20.8827530'),
        (10044, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":20,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}', N'{"Actor":"Employee","Values":{"Step":"OverbookingReported","Note":"Ki\u1EC3m th\u1EED 05b ti\u1EBFp t\u1EE5c: ki\u1EC3m tra x\u1EED l\u00FD s\u1EF1 c\u1ED1 tr\u00EAn \u0111\u00FAng \u0111\u01A1n th\u1EED, kh\u00E1ch \u0111\u1ED3ng \u00FD chuy\u1EC3n ph\u00F2ng c\u00F9ng lo\u1EA1i.","Booking":{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":20,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}}}', NULL, N'2026-10-19T08:34:36.5620750'),
        (10045, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bookings', N'3015', N'Update', N'{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":20,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}', N'{"Actor":"Employee","Values":{"Step":"OverbookingRoomMoved","Note":"Ki\u1EC3m th\u1EED 05b ti\u1EBFp t\u1EE5c: kh\u00E1ch \u0111\u1ED3ng \u00FD chuy\u1EC3n l\u1EA1i ph\u00F2ng tr\u1ED1ng c\u00F9ng lo\u1EA1i; gi\u1EEF nguy\u00EAn gi\u00E1 v\u00E0 c\u1ECDc.","Booking":{"Id":3015,"Code":"FJ261019-003","Status":"Confirmed","RoomId":19,"RoomTypeId":14,"HomestayId":6,"CustomerId":2007,"CheckIn":"2026-12-05","CheckOut":"2026-12-07","TotalAmount":1985000,"DepositRate":0.3000,"DepositAmount":596000,"DiscountAmount":0,"VoucherId":null,"RefundAmount":null,"ReplacedByBookingId":null,"CreatedByEmployeeId":4,"AssignedEmployeeId":5,"CommissionRate":0.1500,"CancellationPolicyId":1}}}', NULL, N'2026-10-19T08:36:19.9880260'),
        (10046, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Customers', N'2008', N'Update', N'{"Note":null,"FullName":"Kh\u00E1ch th\u1EED 05b ti\u1EBFp t\u1EE5c","Email":null}', N'{"Actor":"Employee","Values":{"Note":"Ki\u1EC3m th\u1EED 05b ti\u1EBFp t\u1EE5c: kh\u00E1ch li\u00EAn h\u1EC7 qua Zalo, ki\u1EC3m tra ghi ch\u00FA h\u1ED3 s\u01A1.","FullName":"Kh\u00E1ch th\u1EED 05b ti\u1EBFp t\u1EE5c","Email":null}}', NULL, N'2026-10-19T08:36:51.8524410'),
        (10047, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'53', N'Update', N'{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Draft","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":null,"ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"Sent","Settlement":{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Sent","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":"2026-10-19T09:13:32.133959Z","ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Note":null}}', NULL, N'2026-10-19T09:13:32.1660720'),
        (10048, N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'Settlements', N'53', N'Update', N'{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Sent","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":"2026-10-19T09:13:32.133959","ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Owner","Values":{"Step":"OwnerConfirmed","Settlement":{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Confirmed","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":"2026-10-19T09:13:32.133959","ConfirmedAt":"2026-10-19T09:14:41.419505Z","PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Note":null}}', NULL, N'2026-10-19T09:14:41.4448420'),
        (10049, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'53', N'Update', N'{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Confirmed","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":"2026-10-19T09:13:32.133959","ConfirmedAt":"2026-10-19T09:14:41.419505","PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"Paid","Settlement":{"HomestayId":15,"Year":2026,"Month":10,"PeriodNo":1,"Status":"Paid","TotalRoomAmount":380000,"TotalRetained":114000,"TotalCommission":57000,"PayoutAmount":57000,"SentAt":"2026-10-19T09:13:32.133959","ConfirmedAt":"2026-10-19T09:14:41.419505","PaidAt":"2026-10-19T09:14:47.461757Z","PaidByEmployeeId":3,"PaidBankRef":"UI05C-DEMO-SETTLEMENT-53"},"Note":null}}', NULL, N'2026-10-19T09:14:47.4636500'),
        (10050, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'52', N'Update', N'{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Sent","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-02T02:00:00","ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"Reopened","Settlement":{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Draft","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":null,"ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Note":"UI05c: m\u1EDF l\u1EA1i b\u1EA3ng demo \u0111\u1EC3 r\u00E0 s\u1ED1 li\u1EC7u tr\u01B0\u1EDBc khi chi."}}', NULL, N'2026-10-19T09:15:10.9886170'),
        (10051, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'PersonProfiles', N'2010', N'Insert', N'null', N'{"Actor":"Employee","Values":{"FullName":"Nguy\u1EC5n An Ki\u1EC3m Th\u1EED","PhoneNumber":"\u002B84905050519","Email":"ui05d001@example.test"}}', NULL, N'2026-10-19T13:27:04.7226990'),
        (10052, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'Employees', N'1002', N'Insert', N'null', N'{"Actor":"Employee","Values":{"PersonId":2010,"EmployeeCode":"UI05D001","Department":"V\u1EADn h\u00E0nh th\u1EED nghi\u1EC7m","Position":"Consultant","EmploymentStatus":"Active"}}', NULL, N'2026-10-19T13:27:04.7487380'),
        (10053, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'AspNetUsers', N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Insert', N'null', N'{"Actor":"Employee","Values":{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Active","PasswordChangedAt":"2026-10-19T13:27:04.42758Z"}}', NULL, N'2026-10-19T13:27:04.7558800'),
        (10054, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'AspNetUsers', N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Update', N'{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Active","PasswordChangedAt":"2026-10-19T13:27:04.42758"}', N'{"Actor":"Employee","Values":{"Account":{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Locked","PasswordChangedAt":"2026-10-19T13:27:04.42758"},"Reason":"UI05d: ki\u1EC3m th\u1EED kh\u00F3a nh\u00E2n vi\u00EAn m\u1EDBi."}}', NULL, N'2026-10-19T13:28:39.1297860'),
        (10055, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'AspNetUsers', N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Update', N'{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Locked","PasswordChangedAt":"2026-10-19T13:27:04.42758"}', N'{"Actor":"Employee","Values":{"Account":{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Active","PasswordChangedAt":"2026-10-19T13:27:04.42758"},"Reason":"UI05d: m\u1EDF l\u1EA1i sau ki\u1EC3m th\u1EED."}}', NULL, N'2026-10-19T13:28:39.5481740'),
        (10056, N'e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd', N'AspNetUsers', N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Update', N'{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Active","PasswordChangedAt":"2026-10-19T13:27:04.42758"}', N'{"Actor":"Employee","Values":{"Id":"d2507b59-e06a-4422-b6ec-cc260760ceed","PersonId":2010,"Status":"Active","PasswordChangedAt":"2026-10-19T13:28:40.069918Z"}}', NULL, N'2026-10-19T13:28:40.0699660'),
        (10057, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Contracts', N'31', N'Update', N'{"ContractNo":"HD-2026-0031","HomestayId":31,"StartDate":"2026-06-01","EndDate":"2028-05-31","CommissionRate":0.1000,"Status":"Active","SignedByEmployeeId":2,"UpdatedAt":"2026-10-17T11:04:05.9041355"}', N'{"Actor":"Employee","Values":{"ContractNo":"HD-2026-0031","HomestayId":31,"StartDate":"2026-06-01","EndDate":"2028-06-01","CommissionRate":0.1000,"Status":"Active","SignedByEmployeeId":2,"UpdatedAt":"2026-10-19T13:28:43.301163Z"}}', NULL, N'2026-10-19T13:28:43.3102550'),
        (10058, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-11T06:38:18.880571"},"AmenityIds":[1,2,5,6,8,9]}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:07:23.307519Z"},"AmenityIds":[5,9,3,6,8,2,1]}}', NULL, N'2026-10-19T14:07:23.3464850'),
        (10059, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:07:23.307519"},"AmenityIds":[1,2,3,5,6,8,9]}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:07:23.830241Z"},"AmenityIds":[5,9,6,8,2,1]}}', NULL, N'2026-10-19T14:07:23.8315280'),
        (10060, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'OwnerRegistrations', N'1003', N'Insert', N'null', N'{"Actor":"Owner","Values":{"OwnerId":1003,"Profile":{"FullName":"Ch\u1EE7 ki\u1EC3m th\u1EED B9","PhoneNumber":"\u002B84977091919","BankName":"MB - minh h\u1ECDa B9","BankAccountNo":"0000091919","BankAccountName":"CHU KIEM THU B9"},"Registration":{"OwnerId":1003,"Source":"SelfService","Status":"Draft","CreatedByEmployeeId":null,"SubmittedAt":null,"ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-19T14:07:50.598823Z"}}}', NULL, N'2026-10-19T14:07:50.6542020'),
        (10061, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'Homestays', N'1003', N'Insert', N'null', N'{"Actor":"Owner","Values":{"OwnerId":1003,"Name":"C\u01A1 s\u1EDF th\u1EED B9 - Kh\u00F4ng m\u1EDF b\u00E1n","Address":"\u0110\u1ECBa ch\u1EC9 minh h\u1ECDa B9, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"ContactPhone":"\u002B84977091919","DescriptionHtml":"H\u1ED3 s\u01A1 th\u1EED Development B9 \u0111\u1EC3 nghi\u1EC7m thu h\u1EE3p \u0111\u1ED3ng, kh\u0026#244;ng d\u0026#249;ng \u0111\u1EC3 nh\u1EADn kh\u0026#225;ch.","Status":"Draft","UpdatedAt":"2026-10-19T14:07:50.599069Z"}}', NULL, N'2026-10-19T14:07:50.6560450'),
        (10062, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'VerificationDocuments', N'1083', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1003,"Type":"BusinessLicense","FileUrl":"HS1003-f1aec47a0969481b8aed5c1f5a188cd4.webp","ExpiryDate":"2028-12-31"}}', NULL, N'2026-10-19T14:08:28.3708480'),
        (10063, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'VerificationDocuments', N'1084', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1003,"Type":"FireSafety","FileUrl":"HS1003-b42f1c8a386842b38c210effe20d445e.webp","ExpiryDate":"2028-12-31"}}', NULL, N'2026-10-19T14:08:29.4910560'),
        (10064, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'VerificationDocuments', N'1085', N'Insert', N'null', N'{"Actor":"Owner","Values":{"HomestayId":1003,"Type":"PropertyPhoto","FileUrl":"HS1003-329bd07da4e9401dbd764eee3159ea80.webp","ExpiryDate":"2028-12-31"}}', NULL, N'2026-10-19T14:08:30.6172550'),
        (10065, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'Homestays', N'1003', N'Update', N'{"OwnerId":1003,"Name":"C\u01A1 s\u1EDF th\u1EED B9 - Kh\u00F4ng m\u1EDF b\u00E1n","Address":"\u0110\u1ECBa ch\u1EC9 minh h\u1ECDa B9, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"ContactPhone":"\u002B84977091919","DescriptionHtml":"H\u1ED3 s\u01A1 th\u1EED Development B9 \u0111\u1EC3 nghi\u1EC7m thu h\u1EE3p \u0111\u1ED3ng, kh\u0026#244;ng d\u0026#249;ng \u0111\u1EC3 nh\u1EADn kh\u0026#225;ch.","Status":"Draft","UpdatedAt":"2026-10-19T14:07:50.599069"}', N'{"Actor":"Owner","Values":{"OwnerId":1003,"Name":"C\u01A1 s\u1EDF th\u1EED B9 - Kh\u00F4ng m\u1EDF b\u00E1n","Address":"\u0110\u1ECBa ch\u1EC9 minh h\u1ECDa B9, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"ContactPhone":"\u002B84977091919","DescriptionHtml":"H\u1ED3 s\u01A1 th\u1EED Development B9 \u0111\u1EC3 nghi\u1EC7m thu h\u1EE3p \u0111\u1ED3ng, kh\u0026#244;ng d\u0026#249;ng \u0111\u1EC3 nh\u1EADn kh\u0026#225;ch.","Status":"PendingReview","UpdatedAt":"2026-10-19T14:08:58.769918Z"}}', NULL, N'2026-10-19T14:08:58.7700510'),
        (10066, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'OwnerRegistrations', N'1003', N'Update', N'{"OwnerId":1003,"Source":"SelfService","Status":"Draft","CreatedByEmployeeId":null,"SubmittedAt":null,"ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-19T14:07:50.598823"}', N'{"Actor":"Owner","Values":{"OwnerId":1003,"Source":"SelfService","Status":"PendingReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-19T14:08:58.770367Z","ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-19T14:08:58.770423Z"}}', NULL, N'2026-10-19T14:08:58.7706680'),
        (10067, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:07:23.830241"}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (ki\u1EC3m th\u1EED B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:09:33.921379Z"},"ChangeReason":"Thay \u0111\u1ED5i \u0111\u1ECBa ch\u1EC9 / v\u1ECB tr\u00ED"}}', NULL, N'2026-10-19T14:09:33.9226570'),
        (10068, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'52', N'Update', N'{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Draft","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":null,"ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"Sent","Settlement":{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Sent","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-19T14:10:11.414467Z","ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Note":null}}', NULL, N'2026-10-19T14:10:11.4193470'),
        (10069, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'52', N'Update', N'{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Sent","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-19T14:10:11.414467","ConfirmedAt":null,"PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"AccountantConfirmedOnBehalf","Settlement":{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Confirmed","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-19T14:10:11.414467","ConfirmedAt":"2026-10-19T14:10:27.397637Z","PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null},"Note":"K\u1EBF to\u00E1n x\u00E1c nh\u1EADn thay ch\u1EE7: B9: k\u1EBF to\u00E1n x\u00E1c nh\u1EADn thay ch\u1EE7 sau khi \u0111\u1ED1i chi\u1EBFu s\u1ED1 li\u1EC7u demo."}}', NULL, N'2026-10-19T14:10:27.3979300'),
        (10070, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Settlements', N'52', N'Update', N'{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Confirmed","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-19T14:10:11.414467","ConfirmedAt":"2026-10-19T14:10:27.397637","PaidAt":null,"PaidByEmployeeId":null,"PaidBankRef":null}', N'{"Actor":"Employee","Values":{"Step":"Paid","Settlement":{"HomestayId":31,"Year":2026,"Month":9,"PeriodNo":2,"Status":"Paid","TotalRoomAmount":600000,"TotalRetained":180000,"TotalCommission":60000,"PayoutAmount":120000,"SentAt":"2026-10-19T14:10:11.414467","ConfirmedAt":"2026-10-19T14:10:27.397637","PaidAt":"2026-10-19T14:10:49Z","PaidByEmployeeId":3,"PaidBankRef":"B9-DEV-SETTLEMENT-52"},"Note":"S\u1ED1 ti\u1EC1n: 120000; ng\u00E0y chuy\u1EC3n UTC: 2026-10-19T14:10:49.0000000Z"}}', NULL, N'2026-10-19T14:10:51.7052630'),
        (10071, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'1003', N'Update', N'{"OwnerId":1003,"Source":"SelfService","Status":"PendingReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-19T14:08:58.770367","ReviewedByEmployeeId":null,"ReviewedAt":null,"ReviewNote":null,"UpdatedAt":"2026-10-19T14:08:58.770423"}', N'{"Actor":"Employee","Values":{"OwnerId":1003,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-19T14:08:58.770367","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T14:10:59.339645Z","ReviewNote":null,"UpdatedAt":"2026-10-19T14:10:59.339676Z"}}', NULL, N'2026-10-19T14:10:59.3397430'),
        (10072, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'OwnerRegistrations', N'1003', N'Update', N'{"OwnerId":1003,"Source":"SelfService","Status":"UnderReview","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-19T14:08:58.770367","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T14:10:59.339645","ReviewNote":null,"UpdatedAt":"2026-10-19T14:10:59.339676"}', N'{"Actor":"Employee","Values":{"OwnerId":1003,"Source":"SelfService","Status":"Approved","CreatedByEmployeeId":null,"SubmittedAt":"2026-10-19T14:08:58.770367","ReviewedByEmployeeId":2,"ReviewedAt":"2026-10-19T14:11:00.441649Z","ReviewNote":null,"UpdatedAt":"2026-10-19T14:11:00.441651Z"}}', NULL, N'2026-10-19T14:11:00.4417340'),
        (10073, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestays', N'6', N'Update', N'{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (ki\u1EC3m th\u1EED B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-11T06:38:18.738979","UpdatedAt":"2026-10-19T14:09:33.921379"}', N'{"Actor":"Employee","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (ki\u1EC3m th\u1EED B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:11:47.420068Z","UpdatedAt":"2026-10-19T14:11:47.41994Z"},"Reason":null}}', NULL, N'2026-10-19T14:11:47.4208920'),
        (10074, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (ki\u1EC3m th\u1EED B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:11:47.420068","UpdatedAt":"2026-10-19T14:11:47.41994"}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:11:47.420068","UpdatedAt":"2026-10-19T14:12:46.899771Z"},"ChangeReason":"Thay \u0111\u1ED5i \u0111\u1ECBa ch\u1EC9 / v\u1ECB tr\u00ED"}}', NULL, N'2026-10-19T14:12:46.8999040'),
        (10075, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestays', N'6', N'Update', N'{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:11:47.420068","UpdatedAt":"2026-10-19T14:12:46.899771"}', N'{"Actor":"Employee","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239Z","UpdatedAt":"2026-10-19T14:13:07.732387Z"},"Reason":null}}', NULL, N'2026-10-19T14:13:07.7325230'),
        (10076, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Contracts', N'1002', N'Insert', N'null', N'{"Actor":"Employee","Values":{"ContractNo":"B9-DEV-20261019-1003","HomestayId":1003,"StartDate":"2026-10-19","EndDate":"2027-10-19","CommissionRate":0.1,"Status":"Active","SignedByEmployeeId":2,"UpdatedAt":"2026-10-19T14:13:08.358685Z"}}', NULL, N'2026-10-19T14:13:08.4209560'),
        (10077, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Contracts', N'1002', N'Update', N'{"ContractNo":"B9-DEV-20261019-1003","HomestayId":1003,"StartDate":"2026-10-19","EndDate":"2027-10-19","CommissionRate":0.1000,"Status":"Active","SignedByEmployeeId":2,"UpdatedAt":"2026-10-19T14:13:08.358685"}', N'{"Actor":"Employee","Values":{"ContractNo":"B9-DEV-20261019-1003","HomestayId":1003,"StartDate":"2026-10-19","EndDate":"2027-10-20","CommissionRate":0.1000,"Status":"Active","SignedByEmployeeId":2,"UpdatedAt":"2026-10-19T14:13:57.193939Z"}}', NULL, N'2026-10-19T14:13:57.1940230'),
        (10078, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239","UpdatedAt":"2026-10-19T14:13:07.732387"},"Snapshot":{"Id":6,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"AreaName":"Ba \u0110\u00ECnh","Latitude":null,"Longitude":null,"DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","ContactPhone":"\u002B84912000001","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":2,"AmenityIds":[1,2,5,6,8,9],"RoomTypes":[{"Id":14,"Name":"Ph\u00F2ng \u0111\u00F4i","CategoryId":2,"CategoryName":"Ph\u00F2ng \u0111\u00F4i","MaxGuests":2,"AreaM2":24,"Description":"Gi\u01B0\u1EDDng \u0111\u00F4i 1m6, \u0111i\u1EC1u h\u00F2a, t\u1EE7 qu\u1EA7n \u00E1o v\u00E0 ph\u00F2ng t\u1EAFm ri\u00EAng c\u00F3 v\u00F2i sen n\u00F3ng l\u1EA1nh.","IsActive":true,"Rooms":[{"Id":19,"RoomCode":"101","IsActive":true},{"Id":20,"RoomCode":"102","IsActive":true}],"CurrentPrices":[{"Id":1002,"DayType":0,"Price":905000,"EffectiveFrom":"2026-10-19"},{"Id":41,"DayType":1,"Price":1080000,"EffectiveFrom":"2026-01-01"},{"Id":42,"DayType":2,"Price":1350000,"EffectiveFrom":"2026-01-01"}],"UpcomingPrices":[{"Id":1003,"DayType":0,"Price":905000,"EffectiveFrom":"2026-10-20"}]},{"Id":15,"Name":"Ph\u00F2ng gia \u0111\u00ECnh","CategoryId":3,"CategoryName":"Ph\u00F2ng gia \u0111\u00ECnh","MaxGuests":4,"AreaM2":36,"Description":"M\u1ED9t gi\u01B0\u1EDDng \u0111\u00F4i v\u00E0 hai gi\u01B0\u1EDDng \u0111\u01A1n (ho\u1EB7c hai gi\u01B0\u1EDDng \u0111\u00F4i), sofa, ph\u00F9 h\u1EE3p gia \u0111\u00ECnh c\u00F3 tr\u1EBB nh\u1ECF.","IsActive":true,"Rooms":[{"Id":21,"RoomCode":"201","IsActive":true}],"CurrentPrices":[{"Id":43,"DayType":0,"Price":1440000,"EffectiveFrom":"2026-01-01"},{"Id":44,"DayType":1,"Price":1730000,"EffectiveFrom":"2026-01-01"},{"Id":45,"DayType":2,"Price":2160000,"EffectiveFrom":"2026-01-01"}],"UpcomingPrices":[]}],"Images":[{"Id":199,"Url":"/images/demo/loft-living.webp","RoomTypeId":null,"SortOrder":0,"IsCover":true},{"Id":200,"Url":"/images/demo/room-desk.webp","RoomTypeId":null,"SortOrder":1,"IsCover":false},{"Id":201,"Url":"/images/demo/room-classic.webp","RoomTypeId":null,"SortOrder":2,"IsCover":false},{"Id":202,"Url":"/images/demo/breakfast-table.webp","RoomTypeId":null,"SortOrder":3,"IsCover":false},{"Id":203,"Url":"/images/demo/bathroom-white.webp","RoomTypeId":null,"SortOrder":4,"IsCover":false}],"Policies":[],"Documents":[{"Id":11,"Type":0,"OriginalFileName":"giay-phep-kinh-doanh-06.png","ExpiryDate":"2029-06-01","CreatedAt":"2026-10-11T06:38:18.880571"},{"Id":12,"Type":1,"OriginalFileName":"chung-nhan-pccc-06.png","ExpiryDate":"2028-06-01","CreatedAt":"2026-10-11T06:38:18.880571"},{"Id":81,"Type":2,"OriginalFileName":"anh-thuc-te-mau-2028.png","ExpiryDate":null,"CreatedAt":"2026-10-17T11:04:05.9041355"}],"Amenities":[{"Id":5,"Name":"B\u00E3i \u0111\u1ED7 xe","Icon":"bi-p-circle","Selected":true},{"Id":9,"Name":"Ban c\u00F4ng","Icon":"bi-door-open","Selected":true},{"Id":3,"Name":"B\u1EC3 b\u01A1i","Icon":"bi-water","Selected":false},{"Id":7,"Name":"B\u1EBFp ri\u00EAng","Icon":"bi-egg-fried","Selected":false},{"Id":6,"Name":"B\u1EEFa s\u00E1ng","Icon":"bi-cup-hot","Selected":true},{"Id":8,"Name":"Cho ph\u00E9p th\u00FA c\u01B0ng","Icon":"bi-heart","Selected":true},{"Id":2,"Name":"\u0110i\u1EC1u h\u00F2a","Icon":"bi-snow","Selected":true},{"Id":4,"Name":"Khu BBQ","Icon":"bi-fire","Selected":false},{"Id":10,"Name":"M\u00E1y gi\u1EB7t","Icon":"bi-droplet","Selected":false},{"Id":1,"Name":"Wifi","Icon":"bi-wifi","Selected":true}],"Categories":[{"Id":4,"Name":"Nguy\u00EAn c\u0103n"},{"Id":2,"Name":"Ph\u00F2ng \u0111\u00F4i"},{"Id":1,"Name":"Ph\u00F2ng \u0111\u01A1n"},{"Id":3,"Name":"Ph\u00F2ng gia \u0111\u00ECnh"},{"Id":5,"Name":"Ph\u00F2ng t\u1EADp th\u1EC3"}]}}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (snapshot B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239","UpdatedAt":"2026-10-19T14:30:21.053257Z"},"ChangeReason":"Thay \u0111\u1ED5i \u0111\u1ECBa ch\u1EC9 / v\u1ECB tr\u00ED"}}', NULL, N'2026-10-19T14:30:21.2058340'),
        (10079, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i (snapshot B9)","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239","UpdatedAt":"2026-10-19T14:30:21.053257"},"Snapshot":null}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239","UpdatedAt":"2026-10-19T14:31:05.867144Z"},"ChangeReason":"Thay \u0111\u1ED5i \u0111\u1ECBa ch\u1EC9 / v\u1ECB tr\u00ED"}}', NULL, N'2026-10-19T14:31:05.8674030'),
        (10080, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestays', N'6', N'Update', N'{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"PendingReview","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:13:07.73239","UpdatedAt":"2026-10-19T14:31:05.867144"}', N'{"Actor":"Employee","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882Z","UpdatedAt":"2026-10-19T14:31:32.283703Z"},"Reason":null}}', NULL, N'2026-10-19T14:31:32.2858140'),
        (10081, NULL, N'PersonProfiles', N'2012', N'Insert', N'null', N'{"Actor":"Customer","Values":{"FullName":"Kh\u00E1ch QA 06","PhoneNumber":"\u002B84977060606","Email":null}}', NULL, N'2026-10-19T15:05:15.3377930'),
        (10082, NULL, N'Customers', N'2009', N'Insert', N'null', N'{"Actor":"Customer","Values":{"PersonId":2012}}', NULL, N'2026-10-19T15:05:15.4683590'),
        (10083, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T14:31:32.283703"},"AmenityIds":[1,2,5,6,8,9]}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EKi\u1EC3m th\u1EED \u003Cstrong\u003E\u0111\u1EADm\u003C/strong\u003E \u003Cem\u003Enghi\u00EAng\u003C/em\u003E\u003C/p\u003E\n\u003Cul\u003E\n\u003Cli\u003EM\u1EE5c 1\u003C/li\u003E\n\u003C/ul\u003E\n\u003Cp\u003E\u003Ca\u003Ex\u003C/a\u003E\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T15:43:20.444442Z"},"AmenityIds":[1,2,5,6,8,9]}}', NULL, N'2026-10-19T15:43:20.4754790'),
        (10084, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EKi\u1EC3m th\u1EED \u003Cstrong\u003E\u0111\u1EADm\u003C/strong\u003E \u003Cem\u003Enghi\u00EAng\u003C/em\u003E\u003C/p\u003E\n\u003Cul\u003E\n\u003Cli\u003EM\u1EE5c 1\u003C/li\u003E\n\u003C/ul\u003E\n\u003Cp\u003E\u003Ca\u003Ex\u003C/a\u003E\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T15:43:20.444442"},"AmenityIds":[1,2,5,6,8,9]}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T15:43:24.511205Z"},"AmenityIds":[1,2,5,6,8,9]}}', NULL, N'2026-10-19T15:43:24.5115650'),
        (10085, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestays', N'6', N'Update', N'{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T15:43:24.511205"},"AmenityIds":[1,2,5,6,8,9]}', N'{"Actor":"Owner","Values":{"Homestay":{"Id":6,"OwnerId":1,"Name":"Ba \u0110\u00ECnh Green Villa","Address":"15 \u0110\u1ED9i C\u1EA5n, Ba \u0110\u00ECnh, H\u00E0 N\u1ED9i","AreaId":2,"Latitude":null,"Longitude":null,"ContactPhone":"\u002B84912000001","DescriptionHtml":"\u003Cp\u003EBi\u1EC7t th\u1EF1 nh\u1ECF ph\u1EE7 c\u00E2y xanh tr\u00EAn ph\u1ED1 \u0110\u1ED9i C\u1EA5n, g\u1EA7n L\u0103ng B\u00E1c v\u00E0 Ho\u00E0ng th\u00E0nh Th\u0103ng Long. Kh\u00F4ng gian r\u1ED9ng, s\u00E2n tr\u01B0\u1EDBc \u0111\u1EC3 \u0111\u01B0\u1EE3c xe m\u00E1y, th\u00EDch h\u1EE3p gia \u0111\u00ECnh c\u00F3 ng\u01B0\u1EDDi l\u1EDBn tu\u1ED5i.\u003C/p\u003E","CheckinGuideHtml":"\u003Cp\u003ENh\u1EADn ph\u00F2ng t\u1EEB 14:00. G\u1ECDi ch\u1EE7 nh\u00E0 khi \u0111\u1EBFn n\u01A1i \u0111\u1EC3 \u0111\u01B0\u1EE3c h\u01B0\u1EDBng d\u1EABn ch\u1ED7 \u0111\u1EC3 xe v\u00E0 l\u1EA5y ch\u00ECa kh\u00F3a.\u003C/p\u003E","CheckinTime":"14:00:00","CheckoutTime":"12:00:00","Status":"Active","ApprovedByEmployeeId":2,"ApprovedAt":"2026-10-19T14:31:32.283882","UpdatedAt":"2026-10-19T15:43:46.367175Z"},"AmenityIds":[1,2,5,6,8,9]}}', NULL, N'2026-10-19T15:43:46.3673280'),
        (10086, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'10', N'Insert', N'null', N'{"Actor":"Owner","Values":{"Id":10,"RoomId":19,"StartDate":"2026-10-30","EndDate":"2026-11-01","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}}', NULL, N'2026-10-19T16:26:44.6473750'),
        (10087, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'RoomBlocks', N'10', N'Delete', N'{"Id":10,"RoomId":19,"StartDate":"2026-10-30","EndDate":"2026-11-01","Reason":"OtherChannel","Note":null,"CreatedByAccountId":"11248390-0317-4fe5-b11d-ffd0f950f0e1"}', N'{"Actor":"Owner","Values":{"Deleted":true}}', NULL, N'2026-10-19T16:26:44.9768400');
    SET IDENTITY_INSERT [dbo].[AuditLogs] OFF;
    DBCC CHECKIDENT (N'[dbo].[AuditLogs]', RESEED, 20001) WITH NO_INFOMSGS;

    -- CustomerProfiles: 23 dòng.
    INSERT INTO [dbo].[CustomerProfiles] ([CustomerId], [CitizenId], [StudentId], [StudentVerificationStatus], [CreatedAt], [UpdatedAt]) VALUES
        (1, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.2643350', N'2026-10-11T06:38:17.2643350'),
        (2, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.4357710', N'2026-10-11T06:38:17.4357710'),
        (3, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.4454510', N'2026-10-11T06:38:17.4454510'),
        (4, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.4750850', N'2026-10-11T06:38:17.4750850'),
        (5, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.5906450', N'2026-10-11T06:38:17.5906450'),
        (6, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.7095920', N'2026-10-11T06:38:17.7095920'),
        (7, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.8290030', N'2026-10-11T06:38:17.8290030'),
        (8, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.8411990', N'2026-10-11T06:38:17.8411990'),
        (9, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.9562540', N'2026-10-11T06:38:17.9562540'),
        (10, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:17.9688300', N'2026-10-11T06:38:17.9688300'),
        (11, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:18.0906740', N'2026-10-11T06:38:18.0906740'),
        (12, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:18.2084920', N'2026-10-11T06:38:18.2084930'),
        (13, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:18.2211450', N'2026-10-11T06:38:18.2211450'),
        (14, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:18.3376340', N'2026-10-11T06:38:18.3376340'),
        (15, NULL, NULL, N'NotSubmitted', N'2026-10-11T06:38:18.4598180', N'2026-10-11T06:38:18.4598180'),
        (1003, NULL, NULL, N'NotSubmitted', N'2026-10-12T06:38:15.3714450', N'2026-10-12T06:38:15.3714450'),
        (1004, NULL, NULL, N'NotSubmitted', N'2026-10-15T08:23:17.1054560', N'2026-10-15T08:23:17.1054560'),
        (1005, NULL, NULL, N'NotSubmitted', N'2026-10-17T17:43:06.1704450', N'2026-10-17T17:43:06.1704450'),
        (2005, NULL, NULL, N'NotSubmitted', N'2026-10-18T14:08:48.1815640', N'2026-10-18T14:08:48.1815640'),
        (2006, NULL, NULL, N'NotSubmitted', N'2026-10-18T15:50:23.9199900', N'2026-10-18T15:50:23.9199900'),
        (2007, NULL, NULL, N'NotSubmitted', N'2026-10-19T07:26:02.4380610', N'2026-10-19T07:26:02.4380610'),
        (2008, NULL, NULL, N'NotSubmitted', N'2026-10-19T08:33:41.8483760', N'2026-10-19T08:33:41.8483760'),
        (2009, NULL, NULL, N'NotSubmitted', N'2026-10-19T15:05:15.3657140', N'2026-10-19T15:05:15.3657140');

    -- Homestays: 37 dòng.
    SET IDENTITY_INSERT [dbo].[Homestays] ON;
    INSERT INTO [dbo].[Homestays] ([Id], [OwnerId], [AreaId], [Name], [Address], [Latitude], [Longitude], [DescriptionHtml], [CheckinGuideHtml], [ContactPhone], [CheckinTime], [CheckoutTime], [Status], [ApprovedByEmployeeId], [ApprovedAt], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, 1, N'Phố Cổ Corner House', N'12 Hàng Bạc, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Căn nhà ống ba tầng ngay góc phố Hàng Bạc, bước chân ra là chợ đêm cuối tuần và phố đi bộ Hồ Gươm. Phòng cách âm tốt, có cửa sổ nhìn xuống phố cổ, rất hợp với khách lần đầu đến Hà Nội.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.7393600', N'2026-10-11T06:38:18.7393610'),
        (2, 2, 1, N'Hồ Gươm Little Home', N'35 Lò Sũ, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Ngôi nhà nhỏ trong ngõ yên tĩnh phố Lò Sũ, đi bộ 3 phút ra hồ Hoàn Kiếm. Chủ nhà là người Hà Nội gốc, sẵn lòng chỉ quán bún chả, phở gánh ngon quanh khu phố.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8768950', N'2026-10-11T06:38:18.8768950'),
        (3, 3, 1, N'Tạ Hiện Night Stay', N'8 Tạ Hiện, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Nằm giữa phố Tạ Hiện sôi động, phù hợp với nhóm bạn trẻ thích khám phá phố bia và ẩm thực đêm. Phòng ngủ đặt ở tầng trên, lắp kính hai lớp để vẫn ngủ ngon khi phố còn đông.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8780940', N'2026-10-11T06:38:18.8780940'),
        (4, 4, 1, N'Hàng Gai Heritage', N'61 Hàng Gai, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Nhà phố được giữ lại mặt tiền kiểu Pháp cũ, nội thất gỗ và gạch bông truyền thống. Phố Hàng Gai nổi tiếng lụa tơ tằm, cách Nhà thờ Lớn chỉ 5 phút đi bộ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8790330', N'2026-10-11T06:38:18.8790330'),
        (5, 5, 1, N'Old Quarter Loft', N'20 Mã Mây, Hoàn Kiếm, Hà Nội', NULL, NULL, N'<p>Căn gác xép thiết kế mở với trần cao, nhiều ánh sáng tự nhiên, nằm trên phố Mã Mây. Có góc làm việc riêng và wifi mạnh, phù hợp khách công tác kết hợp du lịch.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8796900', N'2026-10-11T06:38:18.8796900'),
        (6, 1, 2, N'Ba Đình Green Villa', N'15 Đội Cấn, Ba Đình, Hà Nội', NULL, NULL, N'<p>Biệt thự nhỏ phủ cây xanh trên phố Đội Cấn, gần Lăng Bác và Hoàng thành Thăng Long. Không gian rộng, sân trước để được xe máy, thích hợp gia đình có người lớn tuổi.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-19T14:31:32.2838820', N'2026-10-11T06:38:18.8805710', N'2026-10-19T15:43:46.3671750'),
        (7, 2, 2, N'Liễu Giai Sky Home', N'102 Liễu Giai, Ba Đình, Hà Nội', NULL, NULL, N'<p>Căn hộ tầng cao gần Lotte Center, nhìn thẳng ra đường chân trời Hà Nội về đêm. Tòa nhà có bảo vệ 24/7 và thang máy, tiện cho khách đi công tác.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8815140', N'2026-10-11T06:38:18.8815150'),
        (8, 3, 2, N'Trúc Bạch Lakeside', N'7 Trúc Bạch, Ba Đình, Hà Nội', NULL, NULL, N'<p>Ngôi nhà sát hồ Trúc Bạch, sáng sớm có thể chạy bộ quanh hồ hoặc đạp vịt cùng trẻ nhỏ. Gần phố ẩm thực Ngũ Xã với món phở cuốn nổi tiếng.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8823810', N'2026-10-11T06:38:18.8823810'),
        (9, 4, 2, N'Kim Mã Cozy Nest', N'45 Kim Mã, Ba Đình, Hà Nội', NULL, NULL, N'<p>Homestay ấm cúng trên đường Kim Mã, gần bến xe buýt và các tuyến tàu điện. Giá hợp lý, phòng gọn gàng, phù hợp sinh viên và khách đi một mình.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8830670', N'2026-10-11T06:38:18.8830670'),
        (10, 5, 2, N'Ngọc Hà Garden', N'28 Ngọc Hà, Ba Đình, Hà Nội', NULL, NULL, N'<p>Nhà vườn trong làng hoa Ngọc Hà xưa, có khoảng sân nhỏ trồng cây và bàn trà ngoài trời. Không khí yên bình hiếm có giữa trung tâm thành phố.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8839650', N'2026-10-11T06:38:18.8839650'),
        (11, 1, 3, N'Tây Hồ Sunset Homestay', N'52 Từ Hoa, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Nằm trên phố Từ Hoa, cách mép Hồ Tây vài bước chân, nổi tiếng với cảnh hoàng hôn từ ban công tầng ba. Xung quanh có nhiều quán cà phê ven hồ và đường đạp xe.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8846200', N'2026-10-11T06:38:18.8846200'),
        (12, 2, 3, N'Quảng An Lotus House', N'18 Quảng An, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Ngôi nhà mang phong cách tối giản Nhật Bản ở khu Quảng An, gần đầm sen Quảng Bá. Mùa hè có thể dậy sớm ngắm sen nở và chụp ảnh áo dài.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8854750', N'2026-10-11T06:38:18.8854750'),
        (13, 3, 3, N'Xuân Diệu Westlake Studio', N'33 Xuân Diệu, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Studio hiện đại trên phố Xuân Diệu, khu vực đông khách nước ngoài với nhiều nhà hàng quốc tế. Phù hợp cặp đôi muốn kỳ nghỉ ngắn thư thả bên Hồ Tây.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8862070', N'2026-10-11T06:38:18.8862070'),
        (14, 4, 3, N'Nhật Chiêu Hideaway', N'9 Nhật Chiêu, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Căn nhà nép trong ngõ phố Nhật Chiêu, tách biệt khỏi tiếng ồn, có sân thượng trồng cây. Chỉ 5 phút đi bộ đến phủ Tây Hồ và đường ven hồ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8870410', N'2026-10-11T06:38:18.8870410'),
        (15, 5, 3, N'Yên Phụ Riverside', N'140 Yên Phụ, Tây Hồ, Hà Nội', NULL, NULL, N'<p>Ngay mặt đường Yên Phụ, nhìn ra sông Hồng và cầu Long Biên. Thuận tiện di chuyển cả về phố cổ lẫn khu Hồ Tây, có chỗ để ô tô.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8877250', N'2026-10-11T06:38:18.8877250'),
        (16, 1, 4, N'Văn Miếu Scholar Home', N'25 Quốc Tử Giám, Đống Đa, Hà Nội', NULL, NULL, N'<p>Nhà phố đối diện Văn Miếu – Quốc Tử Giám, trang trí bằng thư pháp và đồ gỗ truyền thống. Lựa chọn quen thuộc của phụ huynh đưa con đi thi hoặc khách yêu văn hóa.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8886350', N'2026-10-11T06:38:18.8886350'),
        (17, 2, 4, N'Hoàng Cầu Lake View', N'68 Hoàng Cầu, Đống Đa, Hà Nội', NULL, NULL, N'<p>Căn hộ nhìn ra hồ Hoàng Cầu, buổi tối có đường dạo bộ và khu ẩm thực quanh hồ. Gần các trung tâm thương mại và bệnh viện lớn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8892790', N'2026-10-11T06:38:18.8892790'),
        (18, 3, 4, N'Thái Hà Urban Nest', N'120 Thái Hà, Đống Đa, Hà Nội', NULL, NULL, N'<p>Homestay trên phố Thái Hà sầm uất, gần nhiều trường đại học và nhà hát. Phòng mới sửa, có bếp chung đầy đủ dụng cụ để tự nấu ăn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8901440', N'2026-10-11T06:38:18.8901440'),
        (19, 4, 4, N'Khâm Thiên Retro House', N'77 Khâm Thiên, Đống Đa, Hà Nội', NULL, NULL, N'<p>Ngôi nhà mang phong cách thập niên 80 với đồ cổ, radio và tranh cổ động, trên phố Khâm Thiên. Trải nghiệm hoài niệm lý thú cho khách thích chụp ảnh.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8908910', N'2026-10-11T06:38:18.8908910'),
        (20, 5, 4, N'Láng Hạ Family Stay', N'40 Láng Hạ, Đống Đa, Hà Nội', NULL, NULL, N'<p>Nhà rộng trên phố Láng Hạ, nhiều phòng ngủ thông nhau, phù hợp đại gia đình hoặc nhóm đông người. Gần công viên Thủ Lệ và Bảo tàng Dân tộc học.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8922480', N'2026-10-11T06:38:18.8922480'),
        (21, 1, 5, N'Long Biên Bridge House', N'6 Ngọc Thụy, Long Biên, Hà Nội', NULL, NULL, N'<p>Ngôi nhà ở Ngọc Thụy, ngay chân cầu Long Biên, có thể đi bộ lên cầu ngắm bình minh trên sông Hồng. Bãi giữa sông có vườn hoa và đường đạp xe cuối tuần.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8929950', N'2026-10-11T06:38:18.8929950'),
        (22, 2, 5, N'Bồ Đề Garden Home', N'88 Bồ Đề, Long Biên, Hà Nội', NULL, NULL, N'<p>Nhà có vườn rộng ở phường Bồ Đề, không khí thoáng đãng, cách phố cổ chỉ 10 phút lái xe qua cầu. Trẻ em có chỗ chạy nhảy thoải mái.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8939170', N'2026-10-11T06:38:18.8939170'),
        (23, 3, 5, N'Vinhomes Riverside Suite', N'Hoa Lan 3, Phúc Lợi, Long Biên, Hà Nội', NULL, NULL, N'<p>Căn biệt thự trong khu đô thị Vinhomes Riverside với hồ nước, cây xanh và an ninh tốt. Thích hợp gia đình muốn nghỉ dưỡng yên tĩnh mà vẫn gần trung tâm.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8945730', N'2026-10-11T06:38:18.8945730'),
        (24, 4, 5, N'Gia Thụy Quiet Corner', N'15 Gia Thụy, Long Biên, Hà Nội', NULL, NULL, N'<p>Góc nhỏ yên bình ở Gia Thụy, gần công viên và chợ dân sinh. Giá mềm, chủ nhà thân thiện, phù hợp khách ở dài ngày.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8954320', N'2026-10-11T06:38:18.8954320'),
        (25, 5, 5, N'Sài Đồng Family Villa', N'52 Sài Đồng, Long Biên, Hà Nội', NULL, NULL, N'<p>Biệt thự gia đình ở Sài Đồng, có sân nướng ngoài trời và phòng khách lớn. Gần Aeon Mall Long Biên để mua sắm, vui chơi cùng trẻ nhỏ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8963220', N'2026-10-11T06:38:18.8963220'),
        (26, 1, 6, N'Sóc Sơn Pine Hill', N'Thôn Đền Gióng, Phù Linh, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Nằm giữa đồi thông gần đền Gióng, không khí mát lành quanh năm. Buổi tối có thể đốt lửa trại và nướng BBQ cùng bạn bè, chỉ cách sân bay Nội Bài 20 phút.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8971780', N'2026-10-11T06:38:18.8971780'),
        (27, 2, 6, N'Hàm Lợn Lake Camp', N'Hồ Hàm Lợn, Nam Sơn, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Khu cắm trại ven hồ Hàm Lợn với nhà gỗ và lều glamping. Hoạt động chèo SUP, câu cá và ngắm hoàng hôn trên mặt hồ là điểm nhấn.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8984920', N'2026-10-11T06:38:18.8984920'),
        (28, 3, 6, N'Đồng Đò Forest Villa', N'Hồ Đồng Đò, Minh Trí, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Biệt thự ẩn trong rừng ven hồ Đồng Đò, có bể bơi và khu BBQ riêng. Lựa chọn lý tưởng cho nhóm công ty tổ chức team building cuối tuần.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.8994810', N'2026-10-11T06:38:18.8994820'),
        (29, 4, 6, N'Minh Phú Garden Retreat', N'Xã Minh Phú, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Khu vườn rộng trồng cây ăn quả ở xã Minh Phú, khách được tự tay hái rau và nấu bữa quê. Nhà thoáng mát, nhiều góc chụp ảnh đồng quê.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9003560', N'2026-10-11T06:38:18.9003560'),
        (30, 5, 6, N'Sóc Sơn Hilltop House', N'Xã Bắc Sơn, Sóc Sơn, Hà Nội', NULL, NULL, N'<p>Ngôi nhà trên đỉnh đồi ở Bắc Sơn, nhìn toàn cảnh thung lũng và hồ nước phía xa. Đêm trong trời có thể ngắm sao, sáng sớm thường có sương mờ.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9014170', N'2026-10-11T06:38:18.9014170'),
        (31, 1, 7, N'Ba Vì Mountain Lodge', N'Xã Ba Vì, Ba Vì, Hà Nội', NULL, NULL, N'<p>Nhà gỗ dưới chân núi Ba Vì, gần Vườn quốc gia và đền Thượng. Thích hợp khách thích leo núi, đi bộ đường rừng và tận hưởng không khí trong lành.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000001', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9026060', N'2026-10-11T06:38:18.9026060'),
        (32, 2, 7, N'Tản Viên Cloud Villa', N'Xã Tản Lĩnh, Ba Vì, Hà Nội', NULL, NULL, N'<p>Biệt thự ở Tản Lĩnh, nhiều buổi sáng có biển mây bao quanh. Có bể bơi nước ấm và phòng xông hơi lá thuốc của người Dao.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000002', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9082240', N'2026-10-11T06:38:18.9082240'),
        (33, 3, 7, N'Suối Ngọc Bamboo House', N'Xã Vân Hòa, Ba Vì, Hà Nội', NULL, NULL, N'<p>Nhà tre nằm cạnh suối Ngọc, mùa hè có thể tắm suối mát lạnh. Thiết kế mở, gần gũi thiên nhiên, có võng và chòi nghỉ bên dòng nước.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000003', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9093150', N'2026-10-11T06:38:18.9093150'),
        (34, 4, 7, N'Đá Chông Stone Villa', N'Xã Minh Quang, Ba Vì, Hà Nội', NULL, NULL, N'<p>Biệt thự xây bằng đá ong và đá núi ở Minh Quang, mát vào mùa hè và ấm vào mùa đông. Có sân vườn rộng, phù hợp nhóm đông tổ chức tiệc.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000004', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9104000', N'2026-10-11T06:38:18.9104000'),
        (35, 5, 7, N'Khoang Xanh Eco Stay', N'Xã Vân Hòa, Ba Vì, Hà Nội', NULL, NULL, N'<p>Khu lưu trú sinh thái gần thác Khoang Xanh, chủ trương hạn chế đồ nhựa và dùng thực phẩm địa phương. Hợp với gia đình muốn trẻ nhỏ trải nghiệm thiên nhiên.</p>', N'<p>Nhận phòng từ 14:00. Gọi chủ nhà khi đến nơi để được hướng dẫn chỗ để xe và lấy chìa khóa.</p>', N'+84912000005', N'14:00:00', N'12:00:00', N'Active', 2, N'2026-10-11T06:38:18.7389790', N'2026-10-11T06:38:18.9116190', N'2026-10-11T06:38:18.9116190'),
        (1002, 1002, 4, N'Nhà Gỗ Thử Giao Diện', N'12 Ngõ Thử, Tây Hồ, Hà Nội', NULL, NULL, N'Ba ph&#242;ng nh&#236;n ra vườn, c&#225;ch hồ T&#226;y 5 ph&#250;t đi bộ.', NULL, N'+84977123457', N'14:00:00', N'12:00:00', N'PendingReview', NULL, NULL, N'2026-10-17T18:08:50.8436550', N'2026-10-17T18:08:55.2302840'),
        (1003, 1003, 2, N'Cơ sở thử B9 - Không mở bán', N'Địa chỉ minh họa B9, Ba Đình, Hà Nội', NULL, NULL, N'Hồ sơ thử Development B9 để nghiệm thu hợp đồng, kh&#244;ng d&#249;ng để nhận kh&#225;ch.', NULL, N'+84977091919', N'14:00:00', N'12:00:00', N'PendingReview', NULL, NULL, N'2026-10-19T14:07:50.5990360', N'2026-10-19T14:08:58.7699180');
    SET IDENTITY_INSERT [dbo].[Homestays] OFF;
    DBCC CHECKIDENT (N'[dbo].[Homestays]', RESEED, 2001) WITH NO_INFOMSGS;

    -- Notifications: 90 dòng.
    SET IDENTITY_INSERT [dbo].[Notifications] ON;
    INSERT INTO [dbo].[Notifications] ([Id], [UserId], [Message], [Url], [IsRead], [CreatedAt]) VALUES
        (1, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261011-002 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-11T06:38:20.9193810'),
        (2, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261011-001 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-11T06:58:11.9062590'),
        (3, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261011-001 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-11T06:58:11.9388070'),
        (1002, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bill đơn FJ261011-003 đã chờ duyệt quá 2 giờ làm việc.', NULL, 0, N'2026-10-12T03:22:19.9968460'),
        (1003, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bill đơn FJ261011-004 đã chờ duyệt quá 2 giờ làm việc.', NULL, 0, N'2026-10-12T03:22:20.0073540'),
        (1004, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261012-001 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-12T06:40:57.7182300'),
        (1005, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261012-001 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-12T06:40:57.7269840'),
        (1006, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261012-001 đã đặt cọc, vui lòng xác nhận trong 30 phút.', NULL, 1, N'2026-10-12T06:43:29.4990830'),
        (1007, N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'Fuji đã nhận tiền cọc đơn FJ261012-001. Đang chờ homestay xác nhận.', NULL, 0, N'2026-10-12T06:43:29.4993300'),
        (1008, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261012-001 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-12T07:20:23.3024020'),
        (1009, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261012-001 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-12T07:20:23.3025450'),
        (1010, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261012-002 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 1, N'2026-10-12T08:27:39.9106650'),
        (1011, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261012-003 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-12T08:49:39.4692030'),
        (1012, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261012-003 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-12T08:49:39.4793060'),
        (1013, N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'Đơn FJ261012-003 đã đặt cọc, vui lòng xác nhận trong 30 phút.', NULL, 0, N'2026-10-12T08:50:41.7899200'),
        (1014, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Fuji đã nhận tiền cọc đơn FJ261012-003. Đang chờ homestay xác nhận.', NULL, 0, N'2026-10-12T08:50:41.7901640'),
        (1015, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261012-003 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-12T09:21:23.1122750'),
        (1016, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261012-003 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-12T09:21:23.1127320'),
        (1017, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261013-001 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-13T09:01:10.1742150'),
        (2017, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261015-001 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-14T18:58:17.9005260'),
        (2018, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261015-002 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-15T05:50:42.8468670'),
        (2019, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261015-002 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-15T05:50:42.8471170'),
        (2020, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Bill đơn FJ261015-002 đã chờ duyệt quá 2 giờ làm việc.', NULL, 0, N'2026-10-15T07:51:28.4228940'),
        (2021, N'18b241d3-6441-42f5-8bde-d39acb462728', N'Đơn FJ261015-003 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-15T08:39:28.2119380'),
        (2022, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Hồ sơ đối tác #6 đã quá hạn ba ngày làm việc, cần thẩm định.', N'/Staff/OwnerRegistrations/6', 0, N'2026-10-17T11:50:05.1298860'),
        (3022, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261018-002 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-18T14:09:11.1238960'),
        (3023, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261018-002 có ảnh bill chờ duyệt.', NULL, 0, N'2026-10-18T14:09:11.1333440'),
        (3024, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261018-002 đã đặt cọc, vui lòng xác nhận trong 30 phút.', NULL, 0, N'2026-10-18T14:11:41.7077120'),
        (3025, N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'Đơn FJ261012-001 đã được homestay xác nhận.', NULL, 0, N'2026-10-18T14:12:06.7239380'),
        (3026, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261018-002 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-18T14:41:57.0331840'),
        (3027, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261018-002 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', NULL, 0, N'2026-10-18T14:41:57.0334350'),
        (3028, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261018-004 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-18T16:08:57.0093600'),
        (3029, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261018-005 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-18T17:06:17.1188060'),
        (3030, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261018-006 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-18T17:06:17.3240850'),
        (3031, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261019-001 đã đặt cọc, vui lòng xác nhận trong 30 phút.', NULL, 0, N'2026-10-19T02:01:54.5636310'),
        (3032, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Fuji đã nhận tiền cọc đơn FJ261019-001. Đang chờ homestay xác nhận.', NULL, 0, N'2026-10-19T02:01:54.5719950'),
        (3033, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261019-001 đã được homestay xác nhận.', NULL, 0, N'2026-10-19T02:01:56.6226030'),
        (3034, N'f017ffe1-ef1a-45fd-89ec-072e14350174', N'Chủ homestay đã trả lời đánh giá của bạn cho đơn FJ260704-001.', N'/Bookings/Details/FJ260704-001', 0, N'2026-10-19T02:42:42.5338820'),
        (3035, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261019-002 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-19T06:56:24.6216920'),
        (3036, N'f017ffe1-ef1a-45fd-89ec-072e14350174', N'Chủ homestay đã trả lời đánh giá của bạn cho đơn FJ260704-001.', N'/Bookings/Details/FJ260704-001', 0, N'2026-10-19T07:00:55.8799840'),
        (3037, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Bạn được giao phụ trách đơn FJ261019-001.', N'/Staff/Bookings/3013', 0, N'2026-10-19T07:27:46.5974230'),
        (3038, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Bạn được giao phụ trách đơn FJ261019-003.', N'/Staff/Bookings/3015', 0, N'2026-10-19T07:30:40.5476040'),
        (3039, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261019-003 đã đặt cọc, vui lòng xác nhận trong 30 phút.', NULL, 0, N'2026-10-19T07:31:33.5675480'),
        (3040, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261019-003 đã được homestay xác nhận.', NULL, 0, N'2026-10-19T07:31:35.0128920'),
        (3041, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Nhân viên đã xác nhận thay chủ đơn FJ261019-003 sau khi liên hệ.', N'/Owner/Bookings/Details/3015', 0, N'2026-10-19T07:31:35.0128950'),
        (3042, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261019-003 được báo trùng phòng, cần liên hệ khách và tìm phương án thay thế.', N'/Staff/Bookings/3015', 0, N'2026-10-19T07:31:36.1937710'),
        (3043, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261019-003 được báo trùng phòng, cần liên hệ khách và tìm phương án thay thế.', N'/Staff/Bookings/3015', 0, N'2026-10-19T07:31:36.1937720'),
        (3044, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261019-003 đã được chuyển sang phòng khác cùng loại do trùng phòng; giá và tiền cọc giữ nguyên.', N'/Owner/Bookings/Details/3015', 0, N'2026-10-19T07:32:34.2125480'),
        (3045, N'd17a3c8f-8d18-4aaa-9804-eed0125da9e1', N'Hồ sơ đối tác #1002 cần bổ sung. Vui lòng xem chi tiết hồ sơ.', N'/Staff/OwnerRegistrations/1002', 0, N'2026-10-19T07:40:23.2903970'),
        (3046, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Bạn được giao phụ trách đơn FJ261019-002.', N'/Staff/Bookings/3014', 0, N'2026-10-19T08:33:10.0027020'),
        (3047, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Bạn được giao phụ trách đơn FJ261019-003.', N'/Staff/Bookings/3015', 0, N'2026-10-19T08:34:05.6496350'),
        (3048, N'2d3ea584-7f5a-4a2a-a6d4-306856ba3933', N'Hồ sơ đối tác #6 cần bổ sung. Vui lòng xem chi tiết hồ sơ.', N'/Staff/OwnerRegistrations/6', 0, N'2026-10-19T08:34:20.8840750'),
        (3049, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261019-003 được báo trùng phòng, cần liên hệ khách và tìm phương án thay thế.', N'/Staff/Bookings/3015', 0, N'2026-10-19T08:34:36.5650190'),
        (3050, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261019-003 được báo trùng phòng, cần liên hệ khách và tìm phương án thay thế.', N'/Staff/Bookings/3015', 0, N'2026-10-19T08:34:36.5650200'),
        (3051, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261019-003 đã được chuyển sang phòng khác cùng loại do trùng phòng; giá và tiền cọc giữ nguyên.', N'/Owner/Bookings/Details/3015', 0, N'2026-10-19T08:36:19.9908870'),
        (3052, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261019-004 đã hết hạn giữ phòng vì chưa thanh toán.', NULL, 0, N'2026-10-19T08:49:19.2793910'),
        (3053, N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'Bảng đối soát #53 kỳ 1/10/2026 đã được gửi, vui lòng kiểm tra số liệu.', N'/Owner/Settlements/53', 0, N'2026-10-19T09:13:32.3232040'),
        (3054, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Bảng đối soát #53 đã được chủ xác nhận.', N'/Accounting/Settlements/53', 0, N'2026-10-19T09:14:41.6096890'),
        (3055, N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'Bảng đối soát #53 kỳ 1/10/2026 đã được Fuji chuyển tiền.', N'/Owner/Settlements/53', 0, N'2026-10-19T09:14:47.5045500'),
        (3056, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Bảng đối soát #52 kỳ 2/9/2026 đã được mở lại để kế toán điều chỉnh.', N'/Owner/Settlements/52', 0, N'2026-10-19T09:15:11.0016630'),
        (3057, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestay #6: Địa chỉ/vị trí đã thay đổi, cần duyệt lại và liên hệ khách của các đơn sau. FJ261019-001, FJ261019-003', N'/Staff/Homestays/6', 0, N'2026-10-19T14:09:33.9890690'),
        (3058, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Bảng đối soát #52 kỳ 2/9/2026 đã được gửi, vui lòng kiểm tra số liệu.', N'/Owner/Settlements/52', 0, N'2026-10-19T14:10:11.4384040'),
        (3059, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Bảng đối soát #52 kỳ 2/9/2026 đã được kế toán xác nhận thay chủ.', N'/Owner/Settlements/52', 0, N'2026-10-19T14:10:27.4001430'),
        (3060, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Bảng đối soát #52 kỳ 2/9/2026 đã được Fuji chuyển tiền.', N'/Owner/Settlements/52', 0, N'2026-10-19T14:10:51.7101490'),
        (3061, N'e17c9e83-1cec-4fba-b30c-3e685deca18e', N'Hồ sơ đối tác #1003 đã được duyệt. Vui lòng xem chi tiết hồ sơ.', N'/Partner/Status', 1, N'2026-10-19T14:11:00.4595910'),
        (3062, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestay #6 đã được duyệt hoạt động. Vui lòng xem chi tiết.', N'/Owner/Homestays/6', 0, N'2026-10-19T14:11:47.4512400'),
        (3063, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestay #6: Địa chỉ/vị trí đã thay đổi, cần duyệt lại và liên hệ khách của các đơn sau. FJ261019-001, FJ261019-003', N'/Staff/Homestays/6', 0, N'2026-10-19T14:12:46.9044340'),
        (3064, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestay #6 đã được duyệt hoạt động. Vui lòng xem chi tiết.', N'/Owner/Homestays/6', 0, N'2026-10-19T14:13:07.7438620'),
        (3065, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261019-005 đã đặt cọc, vui lòng xác nhận trong 30 phút.', N'/Owner/Bookings/Details/3017', 1, N'2026-10-19T14:14:46.0248920'),
        (3066, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Fuji đã nhận tiền cọc đơn FJ261019-005. Đang chờ homestay xác nhận.', N'/Bookings/Details/FJ261019-005', 0, N'2026-10-19T14:14:46.0250570'),
        (3067, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Homestay #6: Địa chỉ/vị trí đã thay đổi, cần duyệt lại và liên hệ khách của các đơn sau. FJ261019-001, FJ261019-003, FJ261019-005', N'/Staff/Homestays/6', 0, N'2026-10-19T14:30:21.3751670'),
        (3068, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Homestay #6 đã được duyệt hoạt động. Vui lòng xem chi tiết.', N'/Owner/Homestays/6', 1, N'2026-10-19T14:31:32.3374880'),
        (3069, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261019-005 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3017', 0, N'2026-10-19T14:48:46.5171180'),
        (3070, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261019-005 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3017', 0, N'2026-10-19T14:48:46.5504270'),
        (3071, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Đơn FJ261019-005 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3017', 0, N'2026-10-19T14:48:46.5512150'),
        (3072, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261019-007 có ảnh bill chờ duyệt.', N'/Accounting/Receipts', 0, N'2026-10-19T16:55:31.5228200'),
        (3073, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261019-007 có ảnh bill chờ duyệt.', N'/Accounting/Receipts', 0, N'2026-10-19T16:55:31.5333870'),
        (3074, N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'Đơn FJ261019-007 đã đặt cọc, vui lòng xác nhận trong 30 phút.', N'/Owner/Bookings/Details/3019', 0, N'2026-10-19T16:57:04.5274160'),
        (3075, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Fuji đã nhận tiền cọc đơn FJ261019-007. Đang chờ homestay xác nhận.', N'/Bookings/Details/FJ261019-007', 1, N'2026-10-19T16:57:04.5276470'),
        (3076, N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Đơn FJ261019-007 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3019', 0, N'2026-10-19T17:27:18.6789500'),
        (3077, N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Đơn FJ261019-007 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3019', 0, N'2026-10-19T17:27:18.6793210'),
        (3078, N'd2507b59-e06a-4422-b6ec-cc260760ceed', N'Đơn FJ261019-007 đã quá 30 phút homestay chưa xác nhận. Vui lòng gọi nhắc.', N'/Staff/Bookings/3019', 0, N'2026-10-19T17:27:18.6793520'),
        (4022, N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đơn FJ261020-001 có ảnh bill chờ duyệt.', N'/Accounting/Receipts', 0, N'2026-10-19T19:11:39.3450210'),
        (4023, N'93317b46-82fc-486f-993a-d8d786f63b89', N'Đơn FJ261020-001 có ảnh bill chờ duyệt.', N'/Accounting/Receipts', 0, N'2026-10-19T19:11:39.3452100'),
        (4024, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Đơn FJ261020-001 đã đặt cọc, vui lòng xác nhận trong 30 phút.', N'/Owner/Bookings/Details/4008', 0, N'2026-10-19T19:13:50.8121020'),
        (4025, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Fuji đã nhận tiền cọc đơn FJ261020-001. Đang chờ homestay xác nhận.', N'/Bookings/Details/FJ261020-001', 0, N'2026-10-19T19:13:50.8122290'),
        (4026, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Đơn FJ261020-001 đã được homestay xác nhận.', N'/Bookings/Details/FJ261020-001', 1, N'2026-10-19T19:15:14.2704400'),
        (4027, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Sắp đến lịch nhận phòng đơn FJ261020-001 ngày 20/10/2026. Xem chi tiết đơn để biết thông tin nhận phòng.', N'/Bookings/Details/FJ261020-001?reminder=checkin', 1, N'2026-10-20T02:46:03.6851530'),
        (4028, N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Sắp đến lịch nhận phòng đơn FJ261010-004 ngày 22/10/2026. Xem chi tiết đơn để biết thông tin nhận phòng.', N'/Bookings/Details/FJ261010-004?reminder=checkin', 0, N'2026-10-21T02:00:41.0224760'),
        (4029, N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'Lịch đón khách ngày 22/10/2026 tại Old Quarter Loft: FJ261010-004: Lê Văn Hùng, 1 khách, nhận 14:00.', N'/Owner/Bookings?ownerId=5&arrivals=2026-10-22', 0, N'2026-10-21T02:00:41.1630480');
    SET IDENTITY_INSERT [dbo].[Notifications] OFF;
    DBCC CHECKIDENT (N'[dbo].[Notifications]', RESEED, 4029) WITH NO_INFOMSGS;

    -- OwnerRegistrations: 8 dòng.
    SET IDENTITY_INSERT [dbo].[OwnerRegistrations] ON;
    INSERT INTO [dbo].[OwnerRegistrations] ([Id], [OwnerId], [Source], [Status], [CreatedByEmployeeId], [ReviewedByEmployeeId], [ReviewNote], [SubmittedAt], [ReviewedAt], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-11T06:38:16.4481140', N'2026-10-11T06:38:16.4481140', N'2026-10-11T06:38:16.4486150', N'2026-10-11T06:38:16.4486150'),
        (2, 2, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-11T06:38:16.6551610', N'2026-10-11T06:38:16.6551610', N'2026-10-11T06:38:16.6551700', N'2026-10-11T06:38:16.6551700'),
        (3, 3, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-11T06:38:16.7788750', N'2026-10-11T06:38:16.7788750', N'2026-10-11T06:38:16.7788810', N'2026-10-11T06:38:16.7788810'),
        (4, 4, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-11T06:38:16.8998790', N'2026-10-11T06:38:16.8998790', N'2026-10-11T06:38:16.8998860', N'2026-10-11T06:38:16.8998860'),
        (5, 5, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-11T06:38:17.0199710', N'2026-10-11T06:38:17.0199710', N'2026-10-11T06:38:17.0199770', N'2026-10-11T06:38:17.0199770'),
        (6, 6, N'SelfService', N'NeedsRevision', NULL, 2, N'Kiểm thử 05b tiếp tục: bổ sung thông tin cơ sở lưu trú gắn với hồ sơ trước khi duyệt.', N'2026-10-11T06:38:17.1456220', N'2026-10-19T08:34:20.8827000', N'2026-10-11T06:38:17.1456370', N'2026-10-19T08:34:20.8827010'),
        (1002, 1002, N'SelfService', N'NeedsRevision', NULL, 2, N'Kiểm thử 05b: vui lòng bổ sung giấy tờ thực tế thay các tệp mẫu trước khi thẩm định chính thức.', N'2026-10-17T18:08:55.2307780', N'2026-10-19T07:40:23.2146080', N'2026-10-17T18:08:50.8432190', N'2026-10-19T07:40:23.2146320'),
        (1003, 1003, N'SelfService', N'Approved', NULL, 2, NULL, N'2026-10-19T14:08:58.7703670', N'2026-10-19T14:11:00.4416490', N'2026-10-19T14:07:50.5988010', N'2026-10-19T14:11:00.4416510');
    SET IDENTITY_INSERT [dbo].[OwnerRegistrations] OFF;
    DBCC CHECKIDENT (N'[dbo].[OwnerRegistrations]', RESEED, 2001) WITH NO_INFOMSGS;

    -- Vouchers: 4 dòng.
    SET IDENTITY_INSERT [dbo].[Vouchers] ON;
    INSERT INTO [dbo].[Vouchers] ([Id], [Code], [CustomerId], [Amount], [Balance], [ExpiresOn], [Status], [Reason], [SourceBookingId], [IssuedByEmployeeId], [IssuedAt], [RevokedAt], [RevokeReason]) VALUES
        (1, N'VCDEMODD2K', 10, 50000, 50000, N'2027-04-09', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 14, 2, N'2026-10-08T22:56:19.3084240', NULL, NULL),
        (2, N'VCDEMOAA2K', 13, 67000, 67000, N'2026-12-01', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 45, 2, N'2026-06-01T04:11:00', NULL, NULL),
        (3, N'VCDEMOBB2K', 13, 176000, 176000, N'2026-12-01', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 7, 2, N'2026-06-01T07:03:00', NULL, NULL),
        (4, N'VCDEMOCC2K', 15, 50000, 50000, N'2027-01-29', N'Active', N'Homestay từ chối đơn sau khi khách đã đặt cọc.', 53, 2, N'2026-07-29T04:12:00', NULL, NULL);
    SET IDENTITY_INSERT [dbo].[Vouchers] OFF;
    DBCC CHECKIDENT (N'[dbo].[Vouchers]', RESEED, 1001) WITH NO_INFOMSGS;

    -- CancellationPolicies: 6 dòng.
    SET IDENTITY_INSERT [dbo].[CancellationPolicies] ON;
    INSERT INTO [dbo].[CancellationPolicies] ([Id], [HomestayId], [Scope], [Name], [IsActive], [CreatedAt]) VALUES
        (1, NULL, N'All', N'Chính sách hủy chung của Fuji', 1, N'2026-10-11T06:38:18.5589890'),
        (2, 31, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-11T06:38:18.9029740'),
        (3, 32, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-11T06:38:18.9082710'),
        (4, 33, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-11T06:38:18.9093340'),
        (5, 34, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-11T06:38:18.9104210'),
        (6, 35, N'Holiday', N'Chính sách hủy dịp lễ Tết', 1, N'2026-10-11T06:38:18.9116430');
    SET IDENTITY_INSERT [dbo].[CancellationPolicies] OFF;
    DBCC CHECKIDENT (N'[dbo].[CancellationPolicies]', RESEED, 1001) WITH NO_INFOMSGS;

    -- Contracts: 36 dòng.
    SET IDENTITY_INSERT [dbo].[Contracts] ON;
    INSERT INTO [dbo].[Contracts] ([Id], [HomestayId], [ContractNo], [StartDate], [EndDate], [CommissionRate], [Status], [SignedByEmployeeId], [CreatedAt], [UpdatedAt]) VALUES
        (1, 1, N'HD-2026-0001', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.7399560', N'2026-10-17T11:04:05.9041355'),
        (2, 2, N'HD-2026-0002', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8769000', N'2026-10-17T11:04:05.9041355'),
        (3, 3, N'HD-2026-0003', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8780960', N'2026-10-17T11:04:05.9041355'),
        (4, 4, N'HD-2026-0004', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8790350', N'2026-10-17T11:04:05.9041355'),
        (5, 5, N'HD-2026-0005', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8796920', N'2026-10-17T11:04:05.9041355'),
        (6, 6, N'HD-2026-0006', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8805790', N'2026-10-17T11:04:05.9041355'),
        (7, 7, N'HD-2026-0007', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8815160', N'2026-10-17T11:04:05.9041355'),
        (8, 8, N'HD-2026-0008', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8823830', N'2026-10-17T11:04:05.9041355'),
        (9, 9, N'HD-2026-0009', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8830780', N'2026-10-17T11:04:05.9041355'),
        (10, 10, N'HD-2026-0010', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8839660', N'2026-10-17T11:04:05.9041355'),
        (11, 11, N'HD-2026-0011', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8846220', N'2026-10-17T11:04:05.9041355'),
        (12, 12, N'HD-2026-0012', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8854760', N'2026-10-17T11:04:05.9041355'),
        (13, 13, N'HD-2026-0013', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8862290', N'2026-10-17T11:04:05.9041355'),
        (14, 14, N'HD-2026-0014', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8870430', N'2026-10-17T11:04:05.9041355'),
        (15, 15, N'HD-2026-0015', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8877320', N'2026-10-17T11:04:05.9041355'),
        (16, 16, N'HD-2026-0016', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8886370', N'2026-10-17T11:04:05.9041355'),
        (17, 17, N'HD-2026-0017', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8892810', N'2026-10-17T11:04:05.9041355'),
        (18, 18, N'HD-2026-0018', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8901450', N'2026-10-17T11:04:05.9041355'),
        (19, 19, N'HD-2026-0019', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8908920', N'2026-10-17T11:04:05.9041355'),
        (20, 20, N'HD-2026-0020', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8922510', N'2026-10-17T11:04:05.9041355'),
        (21, 21, N'HD-2026-0021', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8929960', N'2026-10-17T11:04:05.9041355'),
        (22, 22, N'HD-2026-0022', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8939190', N'2026-10-17T11:04:05.9041355'),
        (23, 23, N'HD-2026-0023', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8945740', N'2026-10-17T11:04:05.9041355'),
        (24, 24, N'HD-2026-0024', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8954340', N'2026-10-17T11:04:05.9041355'),
        (25, 25, N'HD-2026-0025', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8963240', N'2026-10-17T11:04:05.9041355'),
        (26, 26, N'HD-2026-0026', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.8971800', N'2026-10-17T11:04:05.9041355'),
        (27, 27, N'HD-2026-0027', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.8984930', N'2026-10-17T11:04:05.9041355'),
        (28, 28, N'HD-2026-0028', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.8994880', N'2026-10-17T11:04:05.9041355'),
        (29, 29, N'HD-2026-0029', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.9003570', N'2026-10-17T11:04:05.9041355'),
        (30, 30, N'HD-2026-0030', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.9014190', N'2026-10-17T11:04:05.9041355'),
        (31, 31, N'HD-2026-0031', N'2026-06-01', N'2028-06-01', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.9026500', N'2026-10-19T13:28:43.3011630'),
        (32, 32, N'HD-2026-0032', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.9082280', N'2026-10-17T11:04:05.9041355'),
        (33, 33, N'HD-2026-0033', N'2026-06-01', N'2028-05-31', 0.1500, N'Active', 2, N'2026-10-11T06:38:18.9093170', N'2026-10-17T11:04:05.9041355'),
        (34, 34, N'HD-2026-0034', N'2026-06-01', N'2028-05-31', 0.1000, N'Active', 2, N'2026-10-11T06:38:18.9104060', N'2026-10-17T11:04:05.9041355'),
        (35, 35, N'HD-2026-0035', N'2026-06-01', N'2028-05-31', 0.1200, N'Active', 2, N'2026-10-11T06:38:18.9116210', N'2026-10-17T11:04:05.9041355'),
        (1002, 1003, N'B9-DEV-20261019-1003', N'2026-10-19', N'2027-10-20', 0.1000, N'Active', 2, N'2026-10-19T14:13:08.3586490', N'2026-10-19T14:13:57.1939390');
    SET IDENTITY_INSERT [dbo].[Contracts] OFF;
    DBCC CHECKIDENT (N'[dbo].[Contracts]', RESEED, 2001) WITH NO_INFOMSGS;

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
        (1, 1, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.7444590', N'2026-10-11T06:38:18.7444600'),
        (2, 1, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.7452490', N'2026-10-11T06:38:18.7452490'),
        (3, 1, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.7452600', N'2026-10-11T06:38:18.7452600'),
        (4, 2, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8770290', N'2026-10-11T06:38:18.8770290'),
        (5, 2, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8770320', N'2026-10-11T06:38:18.8770320'),
        (6, 3, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8781080', N'2026-10-11T06:38:18.8781080'),
        (7, 3, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8781100', N'2026-10-11T06:38:18.8781100'),
        (8, 3, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8781180', N'2026-10-11T06:38:18.8781180'),
        (9, 4, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8790420', N'2026-10-11T06:38:18.8790420'),
        (10, 4, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8790440', N'2026-10-11T06:38:18.8790440'),
        (11, 5, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8796980', N'2026-10-11T06:38:18.8796990'),
        (12, 5, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8797000', N'2026-10-11T06:38:18.8797000'),
        (13, 5, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8797010', N'2026-10-11T06:38:18.8797010'),
        (14, 6, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8805860', N'2026-10-11T06:38:18.8805860'),
        (15, 6, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8805890', N'2026-10-11T06:38:18.8805890'),
        (16, 7, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8815260', N'2026-10-11T06:38:18.8815260'),
        (17, 7, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8815280', N'2026-10-11T06:38:18.8815280'),
        (18, 7, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8815300', N'2026-10-11T06:38:18.8815300'),
        (19, 8, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8823900', N'2026-10-11T06:38:18.8823900'),
        (20, 8, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8823920', N'2026-10-11T06:38:18.8823920'),
        (21, 9, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8830850', N'2026-10-11T06:38:18.8830850'),
        (22, 9, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8830870', N'2026-10-11T06:38:18.8830870'),
        (23, 9, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8830880', N'2026-10-11T06:38:18.8830880'),
        (24, 10, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8839730', N'2026-10-11T06:38:18.8839730'),
        (25, 10, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8839750', N'2026-10-11T06:38:18.8839750'),
        (26, 11, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8846280', N'2026-10-11T06:38:18.8846280'),
        (27, 11, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8846300', N'2026-10-11T06:38:18.8846300'),
        (28, 11, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8846310', N'2026-10-11T06:38:18.8846310'),
        (29, 12, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8854830', N'2026-10-11T06:38:18.8854830'),
        (30, 12, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8854850', N'2026-10-11T06:38:18.8854850'),
        (31, 13, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8862350', N'2026-10-11T06:38:18.8862350'),
        (32, 13, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8862370', N'2026-10-11T06:38:18.8862370'),
        (33, 13, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8862380', N'2026-10-11T06:38:18.8862380'),
        (34, 14, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8870530', N'2026-10-11T06:38:18.8870530'),
        (35, 14, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8870550', N'2026-10-11T06:38:18.8870550'),
        (36, 15, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8877400', N'2026-10-11T06:38:18.8877400'),
        (37, 15, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8877420', N'2026-10-11T06:38:18.8877420'),
        (38, 15, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8877430', N'2026-10-11T06:38:18.8877430'),
        (39, 16, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8886430', N'2026-10-11T06:38:18.8886430'),
        (40, 16, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8886450', N'2026-10-11T06:38:18.8886450'),
        (41, 17, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8892940', N'2026-10-11T06:38:18.8892940'),
        (42, 17, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8892960', N'2026-10-11T06:38:18.8892960'),
        (43, 17, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8892970', N'2026-10-11T06:38:18.8892970'),
        (44, 18, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8901520', N'2026-10-11T06:38:18.8901520'),
        (45, 18, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8901610', N'2026-10-11T06:38:18.8901610'),
        (46, 19, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8908990', N'2026-10-11T06:38:18.8908990'),
        (47, 19, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8909060', N'2026-10-11T06:38:18.8909060'),
        (48, 19, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8909070', N'2026-10-11T06:38:18.8909070'),
        (49, 20, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8922630', N'2026-10-11T06:38:18.8922630'),
        (50, 20, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8922670', N'2026-10-11T06:38:18.8922670'),
        (51, 21, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8930040', N'2026-10-11T06:38:18.8930040'),
        (52, 21, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8930060', N'2026-10-11T06:38:18.8930060'),
        (53, 21, 1, N'Phòng đơn', 1, 18, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8930070', N'2026-10-11T06:38:18.8930070'),
        (54, 22, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8939250', N'2026-10-11T06:38:18.8939260'),
        (55, 22, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8939270', N'2026-10-11T06:38:18.8939270'),
        (56, 23, 2, N'Phòng đôi', 2, 22, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8945810', N'2026-10-11T06:38:18.8945810'),
        (57, 23, 3, N'Phòng gia đình', 4, 34, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8945830', N'2026-10-11T06:38:18.8945830'),
        (58, 23, 1, N'Phòng đơn', 1, 16, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8945840', N'2026-10-11T06:38:18.8945840'),
        (59, 24, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8954410', N'2026-10-11T06:38:18.8954410'),
        (60, 24, 3, N'Phòng gia đình', 4, 36, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8954460', N'2026-10-11T06:38:18.8954460'),
        (61, 25, 2, N'Phòng đôi', 2, 20, N'Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.', 1, N'2026-10-11T06:38:18.8963310', N'2026-10-11T06:38:18.8963310'),
        (62, 25, 3, N'Phòng gia đình', 4, 32, N'Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.', 1, N'2026-10-11T06:38:18.8963330', N'2026-10-11T06:38:18.8963330'),
        (63, 25, 1, N'Phòng đơn', 1, 14, N'Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.', 1, N'2026-10-11T06:38:18.8963340', N'2026-10-11T06:38:18.8963340'),
        (64, 26, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.8975360', N'2026-10-11T06:38:18.8975360'),
        (65, 26, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.8975380', N'2026-10-11T06:38:18.8975380'),
        (66, 26, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.8975400', N'2026-10-11T06:38:18.8975400'),
        (67, 27, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.8985180', N'2026-10-11T06:38:18.8985180'),
        (68, 27, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.8985200', N'2026-10-11T06:38:18.8985200'),
        (69, 27, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.8985210', N'2026-10-11T06:38:18.8985210'),
        (70, 28, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.8994980', N'2026-10-11T06:38:18.8994980'),
        (71, 28, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.8995000', N'2026-10-11T06:38:18.8995000'),
        (72, 28, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.8995010', N'2026-10-11T06:38:18.8995010'),
        (73, 29, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9003700', N'2026-10-11T06:38:18.9003700'),
        (74, 29, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9003720', N'2026-10-11T06:38:18.9003720'),
        (75, 29, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9003800', N'2026-10-11T06:38:18.9003800'),
        (76, 30, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9014360', N'2026-10-11T06:38:18.9014360'),
        (77, 30, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9014440', N'2026-10-11T06:38:18.9014440'),
        (78, 30, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9014450', N'2026-10-11T06:38:18.9014450'),
        (79, 31, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9027060', N'2026-10-11T06:38:18.9027060'),
        (80, 31, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9027080', N'2026-10-11T06:38:18.9027080'),
        (81, 31, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9027090', N'2026-10-11T06:38:18.9027090'),
        (82, 32, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9082570', N'2026-10-11T06:38:18.9082570'),
        (83, 32, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9082690', N'2026-10-11T06:38:18.9082690'),
        (84, 32, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9082700', N'2026-10-11T06:38:18.9082700'),
        (85, 33, 2, N'Phòng đôi', 2, 28, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9093300', N'2026-10-11T06:38:18.9093300'),
        (86, 33, 3, N'Phòng gia đình', 4, 42, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9093320', N'2026-10-11T06:38:18.9093320'),
        (87, 33, 4, N'Nguyên căn', 10, 124, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9093330', N'2026-10-11T06:38:18.9093340'),
        (88, 34, 2, N'Phòng đôi', 2, 24, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9104170', N'2026-10-11T06:38:18.9104170'),
        (89, 34, 3, N'Phòng gia đình', 4, 38, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9104190', N'2026-10-11T06:38:18.9104190'),
        (90, 34, 4, N'Nguyên căn', 10, 120, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9104200', N'2026-10-11T06:38:18.9104200'),
        (91, 35, 2, N'Phòng đôi', 2, 26, N'Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.', 1, N'2026-10-11T06:38:18.9116380', N'2026-10-11T06:38:18.9116380'),
        (92, 35, 3, N'Phòng gia đình', 4, 40, N'Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.', 1, N'2026-10-11T06:38:18.9116410', N'2026-10-11T06:38:18.9116410'),
        (93, 35, 4, N'Nguyên căn', 10, 122, N'Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.', 1, N'2026-10-11T06:38:18.9116420', N'2026-10-11T06:38:18.9116420');
    SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomTypes]', RESEED, 1001) WITH NO_INFOMSGS;

    -- Settlements: 27 dòng.
    SET IDENTITY_INSERT [dbo].[Settlements] ON;
    INSERT INTO [dbo].[Settlements] ([Id], [HomestayId], [Year], [Month], [PeriodNo], [TotalRoomAmount], [TotalRetained], [TotalCommission], [PayoutAmount], [Status], [PaidAt], [PaidByEmployeeId], [CreatedAt], [UpdatedAt], [ConfirmedAt], [PaidBankRef], [SentAt]) VALUES
        (27, 10, 2026, 6, 1, 960000, 288000, 96000, 192000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615010', N'2026-06-17T02:00:00'),
        (28, 12, 2026, 6, 1, 2560000, 768000, 384000, 384000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615012', N'2026-06-17T02:00:00'),
        (29, 15, 2026, 6, 1, 960000, 288000, 144000, 144000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615015', N'2026-06-17T02:00:00'),
        (30, 31, 2026, 6, 1, 2040000, 306000, 204000, 102000, N'Paid', N'2026-06-20T08:00:00', 3, N'2026-06-16T01:00:00', N'2026-06-20T08:00:00', N'2026-06-18T03:00:00', N'FT260615031', N'2026-06-17T02:00:00'),
        (31, 8, 2026, 6, 2, 480000, 144000, 57600, 86400, N'Paid', N'2026-07-05T08:00:00', 3, N'2026-07-01T01:00:00', N'2026-07-05T08:00:00', N'2026-07-03T03:00:00', N'FT260630008', N'2026-07-02T02:00:00'),
        (32, 27, 2026, 6, 2, 3600000, 1080000, 540000, 540000, N'Paid', N'2026-07-05T08:00:00', 3, N'2026-07-01T01:00:00', N'2026-07-05T08:00:00', N'2026-07-03T03:00:00', N'FT260630027', N'2026-07-02T02:00:00'),
        (33, 4, 2026, 7, 2, 3580000, 1074000, 358000, 716000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731004', N'2026-08-02T02:00:00'),
        (34, 7, 2026, 7, 2, 960000, 288000, 96000, 192000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731007', N'2026-08-02T02:00:00'),
        (35, 8, 2026, 7, 2, 770000, 231000, 92400, 138600, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731008', N'2026-08-02T02:00:00'),
        (36, 16, 2026, 7, 2, 1600000, 480000, 160000, 320000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731016', N'2026-08-02T02:00:00'),
        (37, 31, 2026, 7, 2, 3070000, 921000, 307000, 614000, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731031', N'2026-08-02T02:00:00'),
        (38, 32, 2026, 7, 2, 6720000, 2016000, 806400, 1209600, N'Paid', N'2026-08-05T08:00:00', 3, N'2026-08-01T01:00:00', N'2026-08-05T08:00:00', N'2026-08-03T03:00:00', N'FT260731032', N'2026-08-02T02:00:00'),
        (39, 5, 2026, 8, 1, 2560000, 768000, 307200, 460800, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815005', N'2026-08-17T02:00:00'),
        (40, 6, 2026, 8, 1, 1440000, 432000, 216000, 216000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815006', N'2026-08-17T02:00:00'),
        (41, 21, 2026, 8, 1, 5120000, 1536000, 768000, 768000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815021', N'2026-08-17T02:00:00'),
        (42, 31, 2026, 8, 1, 4800000, 1440000, 480000, 960000, N'Paid', N'2026-08-20T08:00:00', 3, N'2026-08-16T01:00:00', N'2026-08-20T08:00:00', N'2026-08-18T03:00:00', N'FT260815031', N'2026-08-17T02:00:00'),
        (43, 10, 2026, 8, 2, 2300000, 690000, 230000, 460000, N'Paid', N'2026-09-05T08:00:00', 3, N'2026-09-01T01:00:00', N'2026-09-05T08:00:00', N'2026-09-03T03:00:00', N'FT260831010', N'2026-09-02T02:00:00'),
        (44, 33, 2026, 8, 2, 1600000, 480000, 240000, 240000, N'Paid', N'2026-09-05T08:00:00', 3, N'2026-09-01T01:00:00', N'2026-09-05T08:00:00', N'2026-09-03T03:00:00', N'FT260831033', N'2026-09-02T02:00:00'),
        (45, 3, 2026, 9, 1, 1920000, 576000, 288000, 288000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915003', N'2026-09-17T02:00:00'),
        (46, 6, 2026, 9, 1, 1440000, 432000, 216000, 216000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915006', N'2026-09-17T02:00:00'),
        (47, 9, 2026, 9, 1, 1850000, 555000, 277500, 277500, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915009', N'2026-09-17T02:00:00'),
        (48, 12, 2026, 9, 1, 1280000, 384000, 192000, 192000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915012', N'2026-09-17T02:00:00'),
        (49, 22, 2026, 9, 1, 1000000, 300000, 100000, 200000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915022', N'2026-09-17T02:00:00'),
        (50, 33, 2026, 9, 1, 8000000, 2400000, 1200000, 1200000, N'Paid', N'2026-09-20T08:00:00', 3, N'2026-09-16T01:00:00', N'2026-09-20T08:00:00', N'2026-09-18T03:00:00', N'FT260915033', N'2026-09-17T02:00:00'),
        (51, 17, 2026, 9, 2, 2620000, 786000, 314400, 471600, N'Draft', NULL, NULL, N'2026-10-01T01:00:00', N'2026-10-01T01:00:00', NULL, NULL, NULL),
        (52, 31, 2026, 9, 2, 600000, 180000, 60000, 120000, N'Paid', N'2026-10-19T14:10:49', 3, N'2026-10-01T01:00:00', N'2026-10-19T14:10:51.7046640', N'2026-10-19T14:10:27.3976370', N'B9-DEV-SETTLEMENT-52', N'2026-10-19T14:10:11.4144670'),
        (53, 15, 2026, 10, 1, 380000, 114000, 57000, 57000, N'Paid', N'2026-10-19T09:14:47.4617570', 3, N'2026-10-17T17:17:03.9571730', N'2026-10-19T09:14:47.4617570', N'2026-10-19T09:14:41.4195050', N'UI05C-DEMO-SETTLEMENT-53', N'2026-10-19T09:13:32.1339590');
    SET IDENTITY_INSERT [dbo].[Settlements] OFF;
    DBCC CHECKIDENT (N'[dbo].[Settlements]', RESEED, 1052) WITH NO_INFOMSGS;

    -- VerificationDocuments: 120 dòng.
    SET IDENTITY_INSERT [dbo].[VerificationDocuments] ON;
    INSERT INTO [dbo].[VerificationDocuments] ([Id], [OwnerRegistrationId], [HomestayId], [DocumentType], [FileUrl], [OriginalFileName], [ExpiryDate], [CreatedAt]) VALUES
        (1, NULL, 1, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-01.png', N'2029-06-01', N'2026-10-11T06:38:18.7393600'),
        (2, NULL, 1, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-01.png', N'2026-11-01', N'2026-10-11T06:38:18.7393600'),
        (3, NULL, 2, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-02.png', N'2029-06-01', N'2026-10-11T06:38:18.8768950'),
        (4, NULL, 2, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-02.png', N'2028-06-01', N'2026-10-11T06:38:18.8768950'),
        (5, NULL, 3, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-03.png', N'2029-06-01', N'2026-10-11T06:38:18.8780940'),
        (6, NULL, 3, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-03.png', N'2028-06-01', N'2026-10-11T06:38:18.8780940'),
        (7, NULL, 4, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-04.png', N'2029-06-01', N'2026-10-11T06:38:18.8790330'),
        (8, NULL, 4, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-04.png', N'2028-06-01', N'2026-10-11T06:38:18.8790330'),
        (9, NULL, 5, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-05.png', N'2029-06-01', N'2026-10-11T06:38:18.8796900'),
        (10, NULL, 5, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-05.png', N'2028-06-01', N'2026-10-11T06:38:18.8796900'),
        (11, NULL, 6, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-06.png', N'2029-06-01', N'2026-10-11T06:38:18.8805710'),
        (12, NULL, 6, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-06.png', N'2028-06-01', N'2026-10-11T06:38:18.8805710'),
        (13, NULL, 7, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-07.png', N'2029-06-01', N'2026-10-11T06:38:18.8815140'),
        (14, NULL, 7, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-07.png', N'2028-06-01', N'2026-10-11T06:38:18.8815140'),
        (15, NULL, 8, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-08.png', N'2029-06-01', N'2026-10-11T06:38:18.8823810'),
        (16, NULL, 8, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-08.png', N'2028-06-01', N'2026-10-11T06:38:18.8823810'),
        (17, NULL, 9, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-09.png', N'2029-06-01', N'2026-10-11T06:38:18.8830670'),
        (18, NULL, 9, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-09.png', N'2028-06-01', N'2026-10-11T06:38:18.8830670'),
        (19, NULL, 10, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-10.png', N'2029-06-01', N'2026-10-11T06:38:18.8839650'),
        (20, NULL, 10, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-10.png', N'2028-06-01', N'2026-10-11T06:38:18.8839650'),
        (21, NULL, 11, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-11.png', N'2029-06-01', N'2026-10-11T06:38:18.8846200'),
        (22, NULL, 11, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-11.png', N'2028-06-01', N'2026-10-11T06:38:18.8846200'),
        (23, NULL, 12, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-12.png', N'2029-06-01', N'2026-10-11T06:38:18.8854750'),
        (24, NULL, 12, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-12.png', N'2028-06-01', N'2026-10-11T06:38:18.8854750'),
        (25, NULL, 13, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-13.png', N'2029-06-01', N'2026-10-11T06:38:18.8862070'),
        (26, NULL, 13, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-13.png', N'2028-06-01', N'2026-10-11T06:38:18.8862070'),
        (27, NULL, 14, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-14.png', N'2029-06-01', N'2026-10-11T06:38:18.8870410'),
        (28, NULL, 14, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-14.png', N'2028-06-01', N'2026-10-11T06:38:18.8870410'),
        (29, NULL, 15, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-15.png', N'2029-06-01', N'2026-10-11T06:38:18.8877250'),
        (30, NULL, 15, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-15.png', N'2028-06-01', N'2026-10-11T06:38:18.8877250'),
        (31, NULL, 16, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-16.png', N'2029-06-01', N'2026-10-11T06:38:18.8886350'),
        (32, NULL, 16, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-16.png', N'2028-06-01', N'2026-10-11T06:38:18.8886350'),
        (33, NULL, 17, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-17.png', N'2029-06-01', N'2026-10-11T06:38:18.8892790'),
        (34, NULL, 17, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-17.png', N'2028-06-01', N'2026-10-11T06:38:18.8892790'),
        (35, NULL, 18, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-18.png', N'2029-06-01', N'2026-10-11T06:38:18.8901440'),
        (36, NULL, 18, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-18.png', N'2028-06-01', N'2026-10-11T06:38:18.8901440'),
        (37, NULL, 19, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-19.png', N'2029-06-01', N'2026-10-11T06:38:18.8908910'),
        (38, NULL, 19, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-19.png', N'2028-06-01', N'2026-10-11T06:38:18.8908910'),
        (39, NULL, 20, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-20.png', N'2029-06-01', N'2026-10-11T06:38:18.8922480'),
        (40, NULL, 20, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-20.png', N'2028-06-01', N'2026-10-11T06:38:18.8922480'),
        (41, NULL, 21, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-21.png', N'2029-06-01', N'2026-10-11T06:38:18.8929950'),
        (42, NULL, 21, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-21.png', N'2028-06-01', N'2026-10-11T06:38:18.8929950'),
        (43, NULL, 22, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-22.png', N'2029-06-01', N'2026-10-11T06:38:18.8939170'),
        (44, NULL, 22, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-22.png', N'2028-06-01', N'2026-10-11T06:38:18.8939170'),
        (45, NULL, 23, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-23.png', N'2029-06-01', N'2026-10-11T06:38:18.8945730'),
        (46, NULL, 23, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-23.png', N'2028-06-01', N'2026-10-11T06:38:18.8945730'),
        (47, NULL, 24, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-24.png', N'2029-06-01', N'2026-10-11T06:38:18.8954320'),
        (48, NULL, 24, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-24.png', N'2028-06-01', N'2026-10-11T06:38:18.8954320'),
        (49, NULL, 25, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-25.png', N'2029-06-01', N'2026-10-11T06:38:18.8963220'),
        (50, NULL, 25, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-25.png', N'2028-06-01', N'2026-10-11T06:38:18.8963220'),
        (51, NULL, 26, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-26.png', N'2029-06-01', N'2026-10-11T06:38:18.8971780'),
        (52, NULL, 26, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-26.png', N'2028-06-01', N'2026-10-11T06:38:18.8971780'),
        (53, NULL, 27, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-27.png', N'2029-06-01', N'2026-10-11T06:38:18.8984920'),
        (54, NULL, 27, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-27.png', N'2028-06-01', N'2026-10-11T06:38:18.8984920'),
        (55, NULL, 28, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-28.png', N'2029-06-01', N'2026-10-11T06:38:18.8994810'),
        (56, NULL, 28, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-28.png', N'2028-06-01', N'2026-10-11T06:38:18.8994810'),
        (57, NULL, 29, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-29.png', N'2029-06-01', N'2026-10-11T06:38:18.9003560'),
        (58, NULL, 29, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-29.png', N'2028-06-01', N'2026-10-11T06:38:18.9003560'),
        (59, NULL, 30, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-30.png', N'2029-06-01', N'2026-10-11T06:38:18.9014170'),
        (60, NULL, 30, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-30.png', N'2028-06-01', N'2026-10-11T06:38:18.9014170'),
        (61, NULL, 31, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-31.png', N'2029-06-01', N'2026-10-11T06:38:18.9026060'),
        (62, NULL, 31, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-31.png', N'2028-06-01', N'2026-10-11T06:38:18.9026060'),
        (63, NULL, 32, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-32.png', N'2029-06-01', N'2026-10-11T06:38:18.9082240'),
        (64, NULL, 32, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-32.png', N'2028-06-01', N'2026-10-11T06:38:18.9082240'),
        (65, NULL, 33, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-33.png', N'2029-06-01', N'2026-10-11T06:38:18.9093150'),
        (66, NULL, 33, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-33.png', N'2028-06-01', N'2026-10-11T06:38:18.9093150'),
        (67, NULL, 34, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-34.png', N'2029-06-01', N'2026-10-11T06:38:18.9104000'),
        (68, NULL, 34, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-34.png', N'2028-06-01', N'2026-10-11T06:38:18.9104000'),
        (69, NULL, 35, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh-35.png', N'2029-06-01', N'2026-10-11T06:38:18.9116190'),
        (70, NULL, 35, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc-35.png', N'2028-06-01', N'2026-10-11T06:38:18.9116190'),
        (71, 1, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:16.4481140'),
        (72, 2, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:16.6551610'),
        (73, 3, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:16.7788750'),
        (74, 4, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:16.8998790'),
        (75, 5, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:17.0199710'),
        (76, 6, NULL, N'Other', N'sample-document.png', N'cccd-mat-truoc.png', NULL, N'2026-10-11T06:38:17.1456220'),
        (77, 6, NULL, N'BusinessLicense', N'sample-document.png', N'giay-phep-kinh-doanh.png', N'2028-10-12', N'2026-10-11T06:38:17.1456220'),
        (78, 6, NULL, N'FireSafety', N'sample-document.png', N'chung-nhan-pccc.png', N'2028-10-12', N'2026-10-11T06:38:17.1456220'),
        (79, 6, NULL, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mat-tien.png', NULL, N'2026-10-11T06:38:17.1456220'),
        (80, NULL, 1, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (81, NULL, 6, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (82, NULL, 11, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (83, NULL, 16, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (84, NULL, 21, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (85, NULL, 26, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (86, NULL, 31, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (87, NULL, 2, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (88, NULL, 7, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (89, NULL, 12, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (90, NULL, 17, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (91, NULL, 22, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (92, NULL, 27, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (93, NULL, 32, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (94, NULL, 3, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (95, NULL, 8, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (96, NULL, 13, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (97, NULL, 18, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (98, NULL, 23, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (99, NULL, 28, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (100, NULL, 33, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355');
    INSERT INTO [dbo].[VerificationDocuments] ([Id], [OwnerRegistrationId], [HomestayId], [DocumentType], [FileUrl], [OriginalFileName], [ExpiryDate], [CreatedAt]) VALUES
        (101, NULL, 4, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (102, NULL, 9, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (103, NULL, 14, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (104, NULL, 19, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (105, NULL, 24, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (106, NULL, 29, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (107, NULL, 34, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (108, NULL, 5, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (109, NULL, 10, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (110, NULL, 15, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (111, NULL, 20, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (112, NULL, 25, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (113, NULL, 30, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (114, NULL, 35, N'PropertyPhoto', N'sample-document.png', N'anh-thuc-te-mau-2028.png', NULL, N'2026-10-17T11:04:05.9041355'),
        (1080, NULL, 1002, N'BusinessLicense', N'HS1002-e6fa447e14b748aeb7fc7033e2053dbd.png', N'doc.png', NULL, N'2026-10-17T18:08:52.0694380'),
        (1081, NULL, 1002, N'FireSafety', N'HS1002-5d23e601705241acb8993b3c96d0713b.png', N'doc.png', NULL, N'2026-10-17T18:08:52.7968960'),
        (1082, NULL, 1002, N'PropertyPhoto', N'HS1002-e2936ba030384919889a84149e94551a.png', N'doc.png', NULL, N'2026-10-17T18:08:53.4660580'),
        (1083, NULL, 1003, N'BusinessLicense', N'HS1003-f1aec47a0969481b8aed5c1f5a188cd4.webp', N'sofa-corner-sm.webp', N'2028-12-31', N'2026-10-19T14:08:28.2946940'),
        (1084, NULL, 1003, N'FireSafety', N'HS1003-b42f1c8a386842b38c210effe20d445e.webp', N'sofa-corner-sm.webp', N'2028-12-31', N'2026-10-19T14:08:29.4871020'),
        (1085, NULL, 1003, N'PropertyPhoto', N'HS1003-329bd07da4e9401dbd764eee3159ea80.webp', N'sofa-corner-sm.webp', N'2028-12-31', N'2026-10-19T14:08:30.6079960');
    SET IDENTITY_INSERT [dbo].[VerificationDocuments] OFF;
    DBCC CHECKIDENT (N'[dbo].[VerificationDocuments]', RESEED, 2079) WITH NO_INFOMSGS;

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
    DBCC CHECKIDENT (N'[dbo].[CancellationRules]', RESEED, 1001) WITH NO_INFOMSGS;

    -- HomestayImages: 179 dòng.
    SET IDENTITY_INSERT [dbo].[HomestayImages] ON;
    INSERT INTO [dbo].[HomestayImages] ([Id], [HomestayId], [RoomTypeId], [Url], [SortOrder], [IsCover]) VALUES
        (174, 1, NULL, N'/images/demo/veranda-garden.webp', 0, 1),
        (175, 1, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (176, 1, NULL, N'/images/demo/room-city.webp', 2, 0),
        (177, 1, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (178, 1, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (179, 2, NULL, N'/images/demo/garden-door.webp', 0, 1),
        (180, 2, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (181, 2, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (182, 2, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (183, 2, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (184, 3, NULL, N'/images/demo/bright-living.webp', 0, 1),
        (185, 3, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (186, 3, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (187, 3, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (188, 3, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (189, 4, NULL, N'/images/demo/living-plants.webp', 0, 1),
        (190, 4, NULL, N'/images/demo/room-city.webp', 1, 0),
        (191, 4, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (192, 4, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (193, 4, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (194, 5, NULL, N'/images/demo/wooden-dining.webp', 0, 1),
        (195, 5, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (196, 5, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (197, 5, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (198, 5, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (199, 6, NULL, N'/images/demo/loft-living.webp', 0, 1),
        (200, 6, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (201, 6, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (202, 6, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (203, 6, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (204, 7, NULL, N'/images/demo/window-nook.webp', 0, 1),
        (205, 7, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (206, 7, NULL, N'/images/demo/room-city.webp', 2, 0),
        (207, 7, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (208, 7, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (209, 8, NULL, N'/images/demo/sofa-corner.webp', 0, 1),
        (210, 8, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (211, 8, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (212, 8, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (213, 8, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (214, 9, NULL, N'/images/demo/breakfast-table.webp', 0, 1),
        (215, 9, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (216, 9, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (217, 9, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (218, 9, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (219, 10, NULL, N'/images/demo/room-warm-wood.webp', 0, 1),
        (220, 10, NULL, N'/images/demo/room-city.webp', 1, 0),
        (221, 10, NULL, N'/images/demo/sofa-corner.webp', 2, 0),
        (222, 10, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (223, 11, NULL, N'/images/demo/room-studio.webp', 0, 1),
        (224, 11, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (225, 11, NULL, N'/images/demo/breakfast-table.webp', 2, 0),
        (226, 11, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (227, 12, NULL, N'/images/demo/room-classic.webp', 0, 1),
        (228, 12, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (229, 12, NULL, N'/images/demo/living-plants.webp', 2, 0),
        (230, 12, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (231, 13, NULL, N'/images/demo/room-city.webp', 0, 1),
        (232, 13, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (233, 13, NULL, N'/images/demo/window-nook.webp', 2, 0),
        (234, 13, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (235, 14, NULL, N'/images/demo/room-timber.webp', 0, 1),
        (236, 14, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (237, 14, NULL, N'/images/demo/wooden-dining.webp', 2, 0),
        (238, 14, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (239, 15, NULL, N'/images/demo/room-desk.webp', 0, 1),
        (240, 15, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (241, 15, NULL, N'/images/demo/sofa-corner.webp', 2, 0),
        (242, 15, NULL, N'/images/demo/bathroom-white.webp', 3, 0),
        (243, 16, NULL, N'/images/demo/room-bright.webp', 0, 1),
        (244, 16, NULL, N'/images/demo/room-city.webp', 1, 0),
        (245, 16, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (246, 16, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (247, 16, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (248, 17, NULL, N'/images/demo/room-rose.webp', 0, 1),
        (249, 17, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (250, 17, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (251, 17, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (252, 17, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (253, 18, NULL, N'/images/demo/room-navy.webp', 0, 1),
        (254, 18, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (255, 18, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (256, 18, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (257, 18, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (258, 19, NULL, N'/images/demo/room-heritage.webp', 0, 1),
        (259, 19, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (260, 19, NULL, N'/images/demo/room-city.webp', 2, 0),
        (261, 19, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (262, 19, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (263, 20, NULL, N'/images/demo/living-white.webp', 0, 1),
        (264, 20, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (265, 20, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (266, 20, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (267, 20, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (268, 21, NULL, N'/images/demo/dining-tulips.webp', 0, 1),
        (269, 21, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (270, 21, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (271, 21, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (272, 21, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (273, 22, NULL, N'/images/demo/dining-window.webp', 0, 1);
    INSERT INTO [dbo].[HomestayImages] ([Id], [HomestayId], [RoomTypeId], [Url], [SortOrder], [IsCover]) VALUES
        (274, 22, NULL, N'/images/demo/room-city.webp', 1, 0),
        (275, 22, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (276, 22, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (277, 22, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (278, 23, NULL, N'/images/demo/dining-sunlit.webp', 0, 1),
        (279, 23, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (280, 23, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (281, 23, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (282, 23, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (283, 24, NULL, N'/images/demo/living-cane.webp', 0, 1),
        (284, 24, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (285, 24, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (286, 24, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (287, 24, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (288, 25, NULL, N'/images/demo/living-lounge.webp', 0, 1),
        (289, 25, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (290, 25, NULL, N'/images/demo/room-city.webp', 2, 0),
        (291, 25, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (292, 25, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (293, 26, NULL, N'/images/demo/valley-village.webp', 0, 1),
        (294, 26, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (295, 26, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (296, 26, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (297, 26, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (298, 27, NULL, N'/images/demo/garden-pool.webp', 0, 1),
        (299, 27, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (300, 27, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (301, 27, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (302, 27, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (303, 28, NULL, N'/images/demo/tea-hills.webp', 0, 1),
        (304, 28, NULL, N'/images/demo/room-city.webp', 1, 0),
        (305, 28, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (306, 28, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (307, 28, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (308, 29, NULL, N'/images/demo/rice-terraces.webp', 0, 1),
        (309, 29, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (310, 29, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (311, 29, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (312, 29, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (313, 30, NULL, N'/images/demo/terraces-golden.webp', 0, 1),
        (314, 30, NULL, N'/images/demo/room-desk.webp', 1, 0),
        (315, 30, NULL, N'/images/demo/room-classic.webp', 2, 0),
        (316, 30, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (317, 30, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (318, 31, NULL, N'/images/demo/green-hills.webp', 0, 1),
        (319, 31, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (320, 31, NULL, N'/images/demo/room-city.webp', 2, 0),
        (321, 31, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (322, 31, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (323, 32, NULL, N'/images/demo/terraces-green.webp', 0, 1),
        (324, 32, NULL, N'/images/demo/room-studio.webp', 1, 0),
        (325, 32, NULL, N'/images/demo/room-timber.webp', 2, 0),
        (326, 32, NULL, N'/images/demo/living-plants.webp', 3, 0),
        (327, 32, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (328, 33, NULL, N'/images/demo/misty-hills.webp', 0, 1),
        (329, 33, NULL, N'/images/demo/room-classic.webp', 1, 0),
        (330, 33, NULL, N'/images/demo/room-desk.webp', 2, 0),
        (331, 33, NULL, N'/images/demo/window-nook.webp', 3, 0),
        (332, 33, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (333, 34, NULL, N'/images/demo/mountain-sunrise.webp', 0, 1),
        (334, 34, NULL, N'/images/demo/room-city.webp', 1, 0),
        (335, 34, NULL, N'/images/demo/room-warm-wood.webp', 2, 0),
        (336, 34, NULL, N'/images/demo/wooden-dining.webp', 3, 0),
        (337, 34, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (338, 35, NULL, N'/images/demo/sapa-terraces.webp', 0, 1),
        (339, 35, NULL, N'/images/demo/room-timber.webp', 1, 0),
        (340, 35, NULL, N'/images/demo/room-studio.webp', 2, 0),
        (341, 35, NULL, N'/images/demo/sofa-corner.webp', 3, 0),
        (342, 35, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (343, 1002, NULL, N'/images/demo/veranda-garden.webp', 0, 1),
        (344, 1002, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (345, 1002, NULL, N'/images/demo/room-city.webp', 2, 0),
        (346, 1002, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (347, 1002, NULL, N'/images/demo/bathroom-white.webp', 4, 0),
        (350, 1003, NULL, N'/images/demo/veranda-garden.webp', 0, 1),
        (351, 1003, NULL, N'/images/demo/room-warm-wood.webp', 1, 0),
        (352, 1003, NULL, N'/images/demo/room-city.webp', 2, 0),
        (353, 1003, NULL, N'/images/demo/breakfast-table.webp', 3, 0),
        (354, 1003, NULL, N'/images/demo/bathroom-white.webp', 4, 0);
    SET IDENTITY_INSERT [dbo].[HomestayImages] OFF;
    DBCC CHECKIDENT (N'[dbo].[HomestayImages]', RESEED, 1342) WITH NO_INFOMSGS;

    -- RoomTypePrices: 281 dòng.
    SET IDENTITY_INSERT [dbo].[RoomTypePrices] ON;
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (1, 1, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.7449440'),
        (2, 1, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.7451520'),
        (3, 1, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.7451560'),
        (4, 2, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.7452500'),
        (5, 2, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.7452500'),
        (6, 2, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.7452500'),
        (7, 3, N'Weekday', 320000, N'2026-01-01', N'2026-10-11T06:38:18.7452600'),
        (8, 3, N'Weekend', 380000, N'2026-01-01', N'2026-10-11T06:38:18.7452600'),
        (9, 3, N'Holiday', 480000, N'2026-01-01', N'2026-10-11T06:38:18.7452610'),
        (10, 4, N'Weekday', 500000, N'2026-01-01', N'2026-10-11T06:38:18.8770300'),
        (11, 4, N'Weekend', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8770310'),
        (12, 4, N'Holiday', 750000, N'2026-01-01', N'2026-10-11T06:38:18.8770310'),
        (13, 5, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8770320'),
        (14, 5, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8770330'),
        (15, 5, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8770330'),
        (16, 6, N'Weekday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8781090'),
        (17, 6, N'Weekend', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8781090'),
        (18, 6, N'Holiday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8781090'),
        (19, 7, N'Weekday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8781160'),
        (20, 7, N'Weekend', 1150000, N'2026-01-01', N'2026-10-11T06:38:18.8781170'),
        (21, 7, N'Holiday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8781170'),
        (22, 8, N'Weekday', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8781180'),
        (23, 8, N'Weekend', 580000, N'2026-01-01', N'2026-10-11T06:38:18.8781180'),
        (24, 8, N'Holiday', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8781180'),
        (25, 9, N'Weekday', 700000, N'2026-01-01', N'2026-10-11T06:38:18.8790430'),
        (26, 9, N'Weekend', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8790440'),
        (27, 9, N'Holiday', 1050000, N'2026-01-01', N'2026-10-11T06:38:18.8790440'),
        (28, 10, N'Weekday', 1120000, N'2026-01-01', N'2026-10-11T06:38:18.8790450'),
        (29, 10, N'Weekend', 1340000, N'2026-01-01', N'2026-10-11T06:38:18.8790450'),
        (30, 10, N'Holiday', 1680000, N'2026-01-01', N'2026-10-11T06:38:18.8790450'),
        (31, 11, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8796990'),
        (32, 11, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8797000'),
        (33, 11, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8797000'),
        (34, 12, N'Weekday', 1280000, N'2026-01-01', N'2026-10-11T06:38:18.8797010'),
        (35, 12, N'Weekend', 1540000, N'2026-01-01', N'2026-10-11T06:38:18.8797010'),
        (36, 12, N'Holiday', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8797010'),
        (37, 13, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.8797020'),
        (38, 13, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.8797020'),
        (39, 13, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8797020'),
        (40, 14, N'Weekday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8805870'),
        (41, 14, N'Weekend', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.8805870'),
        (42, 14, N'Holiday', 1350000, N'2026-01-01', N'2026-10-11T06:38:18.8805880'),
        (43, 15, N'Weekday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8805890'),
        (44, 15, N'Weekend', 1730000, N'2026-01-01', N'2026-10-11T06:38:18.8805890'),
        (45, 15, N'Holiday', 2160000, N'2026-01-01', N'2026-10-11T06:38:18.8805900'),
        (46, 16, N'Weekday', 1000000, N'2026-01-01', N'2026-10-11T06:38:18.8815270'),
        (47, 16, N'Weekend', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8815270'),
        (48, 16, N'Holiday', 1500000, N'2026-01-01', N'2026-10-11T06:38:18.8815280'),
        (49, 17, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.8815290'),
        (50, 17, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8815290'),
        (51, 17, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.8815290'),
        (52, 18, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8815300'),
        (53, 18, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8815300'),
        (54, 18, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8815300'),
        (55, 19, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.8823910'),
        (56, 19, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8823910'),
        (57, 19, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8823910'),
        (58, 20, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.8823920'),
        (59, 20, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.8823920'),
        (60, 20, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8823930'),
        (61, 21, N'Weekday', 500000, N'2026-01-01', N'2026-10-11T06:38:18.8830860'),
        (62, 21, N'Weekend', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8830860'),
        (63, 21, N'Holiday', 750000, N'2026-01-01', N'2026-10-11T06:38:18.8830860'),
        (64, 22, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8830870'),
        (65, 22, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8830870'),
        (66, 22, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8830870'),
        (67, 23, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.8830880'),
        (68, 23, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8830880'),
        (69, 23, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8830890'),
        (70, 24, N'Weekday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8839740'),
        (71, 24, N'Weekend', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8839740'),
        (72, 24, N'Holiday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8839740'),
        (73, 25, N'Weekday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8839750'),
        (74, 25, N'Weekend', 1150000, N'2026-01-01', N'2026-10-11T06:38:18.8839750'),
        (75, 25, N'Holiday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8839750'),
        (76, 26, N'Weekday', 700000, N'2026-01-01', N'2026-10-11T06:38:18.8846290'),
        (77, 26, N'Weekend', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8846300'),
        (78, 26, N'Holiday', 1050000, N'2026-01-01', N'2026-10-11T06:38:18.8846300'),
        (79, 27, N'Weekday', 1120000, N'2026-01-01', N'2026-10-11T06:38:18.8846310'),
        (80, 27, N'Weekend', 1340000, N'2026-01-01', N'2026-10-11T06:38:18.8846310'),
        (81, 27, N'Holiday', 1680000, N'2026-01-01', N'2026-10-11T06:38:18.8846310'),
        (82, 28, N'Weekday', 560000, N'2026-01-01', N'2026-10-11T06:38:18.8846320'),
        (83, 28, N'Weekend', 670000, N'2026-01-01', N'2026-10-11T06:38:18.8846320'),
        (84, 28, N'Holiday', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8846320'),
        (85, 29, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8854840'),
        (86, 29, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8854850'),
        (87, 29, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8854850'),
        (88, 30, N'Weekday', 1280000, N'2026-01-01', N'2026-10-11T06:38:18.8854860'),
        (89, 30, N'Weekend', 1540000, N'2026-01-01', N'2026-10-11T06:38:18.8854860'),
        (90, 30, N'Holiday', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8854860'),
        (91, 31, N'Weekday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8862360'),
        (92, 31, N'Weekend', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.8862360'),
        (93, 31, N'Holiday', 1350000, N'2026-01-01', N'2026-10-11T06:38:18.8862360'),
        (94, 32, N'Weekday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8862370'),
        (95, 32, N'Weekend', 1730000, N'2026-01-01', N'2026-10-11T06:38:18.8862370'),
        (96, 32, N'Holiday', 2160000, N'2026-01-01', N'2026-10-11T06:38:18.8862380'),
        (97, 33, N'Weekday', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8862460'),
        (98, 33, N'Weekend', 860000, N'2026-01-01', N'2026-10-11T06:38:18.8862460'),
        (99, 33, N'Holiday', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.8862470'),
        (100, 34, N'Weekday', 1000000, N'2026-01-01', N'2026-10-11T06:38:18.8870540');
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (101, 34, N'Weekend', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8870540'),
        (102, 34, N'Holiday', 1500000, N'2026-01-01', N'2026-10-11T06:38:18.8870540'),
        (103, 35, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.8870550'),
        (104, 35, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8870550'),
        (105, 35, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.8870550'),
        (106, 36, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.8877400'),
        (107, 36, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8877410'),
        (108, 36, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8877410'),
        (109, 37, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.8877420'),
        (110, 37, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.8877420'),
        (111, 37, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8877420'),
        (112, 38, N'Weekday', 320000, N'2026-01-01', N'2026-10-11T06:38:18.8877430'),
        (113, 38, N'Weekend', 380000, N'2026-01-01', N'2026-10-11T06:38:18.8877430'),
        (114, 38, N'Holiday', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8877430'),
        (115, 39, N'Weekday', 500000, N'2026-01-01', N'2026-10-11T06:38:18.8886440'),
        (116, 39, N'Weekend', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8886450'),
        (117, 39, N'Holiday', 750000, N'2026-01-01', N'2026-10-11T06:38:18.8886450'),
        (118, 40, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8886450'),
        (119, 40, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8886460'),
        (120, 40, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8886460'),
        (121, 41, N'Weekday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8892950'),
        (122, 41, N'Weekend', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8892950'),
        (123, 41, N'Holiday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8892950'),
        (124, 42, N'Weekday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8892960'),
        (125, 42, N'Weekend', 1150000, N'2026-01-01', N'2026-10-11T06:38:18.8892960'),
        (126, 42, N'Holiday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8892960'),
        (127, 43, N'Weekday', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8892970'),
        (128, 43, N'Weekend', 580000, N'2026-01-01', N'2026-10-11T06:38:18.8892970'),
        (129, 43, N'Holiday', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8892970'),
        (130, 44, N'Weekday', 700000, N'2026-01-01', N'2026-10-11T06:38:18.8901530'),
        (131, 44, N'Weekend', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8901600'),
        (132, 44, N'Holiday', 1050000, N'2026-01-01', N'2026-10-11T06:38:18.8901600'),
        (133, 45, N'Weekday', 1120000, N'2026-01-01', N'2026-10-11T06:38:18.8901610'),
        (134, 45, N'Weekend', 1340000, N'2026-01-01', N'2026-10-11T06:38:18.8901610'),
        (135, 45, N'Holiday', 1680000, N'2026-01-01', N'2026-10-11T06:38:18.8901610'),
        (136, 46, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8909000'),
        (137, 46, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8909000'),
        (138, 46, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8909010'),
        (139, 47, N'Weekday', 1280000, N'2026-01-01', N'2026-10-11T06:38:18.8909060'),
        (140, 47, N'Weekend', 1540000, N'2026-01-01', N'2026-10-11T06:38:18.8909060'),
        (141, 47, N'Holiday', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8909070'),
        (142, 48, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.8909070'),
        (143, 48, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.8909080'),
        (144, 48, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8909080'),
        (145, 49, N'Weekday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8922650'),
        (146, 49, N'Weekend', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.8922660'),
        (147, 49, N'Holiday', 1350000, N'2026-01-01', N'2026-10-11T06:38:18.8922660'),
        (148, 50, N'Weekday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8922670'),
        (149, 50, N'Weekend', 1730000, N'2026-01-01', N'2026-10-11T06:38:18.8922680'),
        (150, 50, N'Holiday', 2160000, N'2026-01-01', N'2026-10-11T06:38:18.8922680'),
        (151, 51, N'Weekday', 1000000, N'2026-01-01', N'2026-10-11T06:38:18.8930050'),
        (152, 51, N'Weekend', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8930050'),
        (153, 51, N'Holiday', 1500000, N'2026-01-01', N'2026-10-11T06:38:18.8930050'),
        (154, 52, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.8930060'),
        (155, 52, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8930060'),
        (156, 52, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.8930060'),
        (157, 53, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8930070'),
        (158, 53, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8930070'),
        (159, 53, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8930070'),
        (160, 54, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.8939260'),
        (161, 54, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8939270'),
        (162, 54, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8939270'),
        (163, 55, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.8939330'),
        (164, 55, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.8939330'),
        (165, 55, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8939330'),
        (166, 56, N'Weekday', 500000, N'2026-01-01', N'2026-10-11T06:38:18.8945820'),
        (167, 56, N'Weekend', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8945820'),
        (168, 56, N'Holiday', 750000, N'2026-01-01', N'2026-10-11T06:38:18.8945830'),
        (169, 57, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8945830'),
        (170, 57, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8945840'),
        (171, 57, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8945840'),
        (172, 58, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.8945840'),
        (173, 58, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.8945850'),
        (174, 58, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8945850'),
        (175, 59, N'Weekday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.8954420'),
        (176, 59, N'Weekend', 720000, N'2026-01-01', N'2026-10-11T06:38:18.8954460'),
        (177, 59, N'Holiday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8954460'),
        (178, 60, N'Weekday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8954470'),
        (179, 60, N'Weekend', 1150000, N'2026-01-01', N'2026-10-11T06:38:18.8954470'),
        (180, 60, N'Holiday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8954470'),
        (181, 61, N'Weekday', 700000, N'2026-01-01', N'2026-10-11T06:38:18.8963320'),
        (182, 61, N'Weekend', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8963320'),
        (183, 61, N'Holiday', 1050000, N'2026-01-01', N'2026-10-11T06:38:18.8963320'),
        (184, 62, N'Weekday', 1120000, N'2026-01-01', N'2026-10-11T06:38:18.8963330'),
        (185, 62, N'Weekend', 1340000, N'2026-01-01', N'2026-10-11T06:38:18.8963330'),
        (186, 62, N'Holiday', 1680000, N'2026-01-01', N'2026-10-11T06:38:18.8963340'),
        (187, 63, N'Weekday', 560000, N'2026-01-01', N'2026-10-11T06:38:18.8963340'),
        (188, 63, N'Weekend', 670000, N'2026-01-01', N'2026-10-11T06:38:18.8963340'),
        (189, 63, N'Holiday', 840000, N'2026-01-01', N'2026-10-11T06:38:18.8963340'),
        (190, 64, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.8975370'),
        (191, 64, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.8975370'),
        (192, 64, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8975370'),
        (193, 65, N'Weekday', 1280000, N'2026-01-01', N'2026-10-11T06:38:18.8975380'),
        (194, 65, N'Weekend', 1540000, N'2026-01-01', N'2026-10-11T06:38:18.8975380'),
        (195, 65, N'Holiday', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8975380'),
        (196, 66, N'Weekday', 3200000, N'2026-01-01', N'2026-10-11T06:38:18.8975400'),
        (197, 66, N'Weekend', 3840000, N'2026-01-01', N'2026-10-11T06:38:18.8975400'),
        (198, 66, N'Holiday', 4800000, N'2026-01-01', N'2026-10-11T06:38:18.8975400'),
        (199, 67, N'Weekday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.8985190'),
        (200, 67, N'Weekend', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.8985190');
    INSERT INTO [dbo].[RoomTypePrices] ([Id], [RoomTypeId], [DayType], [Price], [EffectiveFrom], [CreatedAt]) VALUES
        (201, 67, N'Holiday', 1350000, N'2026-01-01', N'2026-10-11T06:38:18.8985200'),
        (202, 68, N'Weekday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.8985200'),
        (203, 68, N'Weekend', 1730000, N'2026-01-01', N'2026-10-11T06:38:18.8985200'),
        (204, 68, N'Holiday', 2160000, N'2026-01-01', N'2026-10-11T06:38:18.8985210'),
        (205, 69, N'Weekday', 3600000, N'2026-01-01', N'2026-10-11T06:38:18.8985210'),
        (206, 69, N'Weekend', 4320000, N'2026-01-01', N'2026-10-11T06:38:18.8985210'),
        (207, 69, N'Holiday', 5400000, N'2026-01-01', N'2026-10-11T06:38:18.8985220'),
        (208, 70, N'Weekday', 1000000, N'2026-01-01', N'2026-10-11T06:38:18.8994990'),
        (209, 70, N'Weekend', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.8994990'),
        (210, 70, N'Holiday', 1500000, N'2026-01-01', N'2026-10-11T06:38:18.8995000'),
        (211, 71, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.8995000'),
        (212, 71, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.8995010'),
        (213, 71, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.8995010'),
        (214, 72, N'Weekday', 4000000, N'2026-01-01', N'2026-10-11T06:38:18.8995010'),
        (215, 72, N'Weekend', 4800000, N'2026-01-01', N'2026-10-11T06:38:18.8995020'),
        (216, 72, N'Holiday', 6000000, N'2026-01-01', N'2026-10-11T06:38:18.8995020'),
        (217, 73, N'Weekday', 400000, N'2026-01-01', N'2026-10-11T06:38:18.9003710'),
        (218, 73, N'Weekend', 480000, N'2026-01-01', N'2026-10-11T06:38:18.9003710'),
        (219, 73, N'Holiday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.9003720'),
        (220, 74, N'Weekday', 640000, N'2026-01-01', N'2026-10-11T06:38:18.9003790'),
        (221, 74, N'Weekend', 770000, N'2026-01-01', N'2026-10-11T06:38:18.9003790'),
        (222, 74, N'Holiday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.9003790'),
        (223, 75, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.9003800'),
        (224, 75, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.9003800'),
        (225, 75, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.9003800'),
        (226, 76, N'Weekday', 500000, N'2026-01-01', N'2026-10-11T06:38:18.9014370'),
        (227, 76, N'Weekend', 600000, N'2026-01-01', N'2026-10-11T06:38:18.9014430'),
        (228, 76, N'Holiday', 750000, N'2026-01-01', N'2026-10-11T06:38:18.9014430'),
        (229, 77, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.9014440'),
        (230, 77, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.9014440'),
        (231, 77, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.9014440'),
        (232, 78, N'Weekday', 2000000, N'2026-01-01', N'2026-10-11T06:38:18.9014450'),
        (233, 78, N'Weekend', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.9014450'),
        (234, 78, N'Holiday', 3000000, N'2026-01-01', N'2026-10-11T06:38:18.9014450'),
        (235, 79, N'Weekday', 600000, N'2026-01-01', N'2026-10-11T06:38:18.9027070'),
        (236, 79, N'Weekend', 720000, N'2026-01-01', N'2026-10-11T06:38:18.9027070'),
        (237, 79, N'Holiday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.9027080'),
        (238, 80, N'Weekday', 960000, N'2026-01-01', N'2026-10-11T06:38:18.9027090'),
        (239, 80, N'Weekend', 1150000, N'2026-01-01', N'2026-10-11T06:38:18.9027090'),
        (240, 80, N'Holiday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.9027090'),
        (241, 81, N'Weekday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.9027100'),
        (242, 81, N'Weekend', 2880000, N'2026-01-01', N'2026-10-11T06:38:18.9027100'),
        (243, 81, N'Holiday', 3600000, N'2026-01-01', N'2026-10-11T06:38:18.9027100'),
        (244, 82, N'Weekday', 700000, N'2026-01-01', N'2026-10-11T06:38:18.9082590'),
        (245, 82, N'Weekend', 840000, N'2026-01-01', N'2026-10-11T06:38:18.9082600'),
        (246, 82, N'Holiday', 1050000, N'2026-01-01', N'2026-10-11T06:38:18.9082600'),
        (247, 83, N'Weekday', 1120000, N'2026-01-01', N'2026-10-11T06:38:18.9082690'),
        (248, 83, N'Weekend', 1340000, N'2026-01-01', N'2026-10-11T06:38:18.9082690'),
        (249, 83, N'Holiday', 1680000, N'2026-01-01', N'2026-10-11T06:38:18.9082690'),
        (250, 84, N'Weekday', 2800000, N'2026-01-01', N'2026-10-11T06:38:18.9082700'),
        (251, 84, N'Weekend', 3360000, N'2026-01-01', N'2026-10-11T06:38:18.9082700'),
        (252, 84, N'Holiday', 4200000, N'2026-01-01', N'2026-10-11T06:38:18.9082710'),
        (253, 85, N'Weekday', 800000, N'2026-01-01', N'2026-10-11T06:38:18.9093310'),
        (254, 85, N'Weekend', 960000, N'2026-01-01', N'2026-10-11T06:38:18.9093320'),
        (255, 85, N'Holiday', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.9093320'),
        (256, 86, N'Weekday', 1280000, N'2026-01-01', N'2026-10-11T06:38:18.9093330'),
        (257, 86, N'Weekend', 1540000, N'2026-01-01', N'2026-10-11T06:38:18.9093330'),
        (258, 86, N'Holiday', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.9093330'),
        (259, 87, N'Weekday', 3200000, N'2026-01-01', N'2026-10-11T06:38:18.9093340'),
        (260, 87, N'Weekend', 3840000, N'2026-01-01', N'2026-10-11T06:38:18.9093340'),
        (261, 87, N'Holiday', 4800000, N'2026-01-01', N'2026-10-11T06:38:18.9093340'),
        (262, 88, N'Weekday', 900000, N'2026-01-01', N'2026-10-11T06:38:18.9104170'),
        (263, 88, N'Weekend', 1080000, N'2026-01-01', N'2026-10-11T06:38:18.9104180'),
        (264, 88, N'Holiday', 1350000, N'2026-01-01', N'2026-10-11T06:38:18.9104180'),
        (265, 89, N'Weekday', 1440000, N'2026-01-01', N'2026-10-11T06:38:18.9104190'),
        (266, 89, N'Weekend', 1730000, N'2026-01-01', N'2026-10-11T06:38:18.9104190'),
        (267, 89, N'Holiday', 2160000, N'2026-01-01', N'2026-10-11T06:38:18.9104190'),
        (268, 90, N'Weekday', 3600000, N'2026-01-01', N'2026-10-11T06:38:18.9104200'),
        (269, 90, N'Weekend', 4320000, N'2026-01-01', N'2026-10-11T06:38:18.9104200'),
        (270, 90, N'Holiday', 5400000, N'2026-01-01', N'2026-10-11T06:38:18.9104200'),
        (271, 91, N'Weekday', 1000000, N'2026-01-01', N'2026-10-11T06:38:18.9116390'),
        (272, 91, N'Weekend', 1200000, N'2026-01-01', N'2026-10-11T06:38:18.9116400'),
        (273, 91, N'Holiday', 1500000, N'2026-01-01', N'2026-10-11T06:38:18.9116400'),
        (274, 92, N'Weekday', 1600000, N'2026-01-01', N'2026-10-11T06:38:18.9116410'),
        (275, 92, N'Weekend', 1920000, N'2026-01-01', N'2026-10-11T06:38:18.9116410'),
        (276, 92, N'Holiday', 2400000, N'2026-01-01', N'2026-10-11T06:38:18.9116410'),
        (277, 93, N'Weekday', 4000000, N'2026-01-01', N'2026-10-11T06:38:18.9116420'),
        (278, 93, N'Weekend', 4800000, N'2026-01-01', N'2026-10-11T06:38:18.9116420'),
        (279, 93, N'Holiday', 6000000, N'2026-01-01', N'2026-10-11T06:38:18.9116420'),
        (1002, 14, N'Weekday', 905000, N'2026-10-19', N'2026-10-19T02:37:44.0431300'),
        (1003, 14, N'Weekday', 905000, N'2026-10-20', N'2026-10-19T07:00:49.7765240');
    SET IDENTITY_INSERT [dbo].[RoomTypePrices] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomTypePrices]', RESEED, 2001) WITH NO_INFOMSGS;

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
    DBCC CHECKIDENT (N'[dbo].[Rooms]', RESEED, 1001) WITH NO_INFOMSGS;

    -- Bookings: 81 dòng.
    SET IDENTITY_INSERT [dbo].[Bookings] ON;
    INSERT INTO [dbo].[Bookings] ([Id], [Code], [CustomerId], [ContactName], [ContactPhone], [SourceChannel], [CreatedByEmployeeId], [AssignedEmployeeId], [HomestayId], [RoomTypeId], [RoomId], [CheckIn], [CheckOut], [Guests], [SpecialRequests], [Status], [HoldExpiresAt], [PaymentSubmittedAt], [DepositedAt], [ConfirmDeadlineAt], [ConfirmAlertSentAt], [ConfirmedAt], [CheckedInAt], [CompletedAt], [CancelledAt], [CancelReason], [TotalAmount], [DepositRate], [DepositAmount], [CommissionRate], [CancellationPolicyId], [RefundAmount], [ReplacedByBookingId], [CreatedAt], [UpdatedAt], [DiscountAmount], [VoucherId], [ChangeFeeAmount]) VALUES
        (1, N'FJ260920-001', 2, N'Trần Thị Bích', N'+84987000002', N'Zalo', 4, 4, 6, 14, 20, N'2026-10-10', N'2026-10-13', 2, NULL, N'CheckedIn', N'2026-09-20T05:52:00', NULL, N'2026-09-20T05:43:00', N'2026-09-20T06:13:00', NULL, N'2026-09-20T05:55:00', N'2026-10-10T09:02:00', NULL, NULL, NULL, 2880000, 0.3000, 864000, 0.1500, 1, NULL, NULL, N'2026-09-20T05:37:00', N'2026-10-10T09:02:00', 0, NULL, 0),
        (2, N'FJ260929-001', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 4, 10, 14, N'2026-10-11', N'2026-10-14', 3, NULL, N'CheckedIn', N'2026-09-29T05:30:00', N'2026-09-29T05:17:00', N'2026-09-29T07:36:00', N'2026-09-29T08:06:00', NULL, N'2026-09-29T07:44:00', N'2026-10-11T06:28:19.3084240', NULL, NULL, NULL, 3360000, 0.3000, 1008000, 0.1000, 1, NULL, NULL, N'2026-09-29T05:15:00', N'2026-10-11T06:28:19.3084240', 0, NULL, 0),
        (3, N'FJ261011-001', 4, N'Đặng Thị Thu', N'+84987000003', N'Web', NULL, NULL, 1, 3, 4, N'2026-10-20', N'2026-10-21', 1, NULL, N'Deposited', N'2026-10-11T06:31:19.3084240', NULL, N'2026-10-11T06:21:19.3084240', N'2026-10-11T06:51:19.3084240', N'2026-10-11T06:58:11.6886710', NULL, NULL, NULL, NULL, NULL, 320000, 0.3000, 96000, 0.1000, 1, NULL, NULL, N'2026-10-11T06:16:19.3084240', N'2026-10-11T06:21:19.3084240', 0, NULL, 0),
        (4, N'FJ261011-002', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 5, 5, 30, 77, 107, N'2026-10-27', N'2026-10-28', 4, NULL, N'Deposited', N'2026-10-11T05:53:19.3084240', NULL, N'2026-10-11T05:43:19.3084240', N'2026-10-11T06:13:19.3084240', N'2026-10-11T06:38:20.8612970', NULL, NULL, NULL, NULL, NULL, 800000, 0.3000, 240000, 0.1500, 1, NULL, NULL, N'2026-10-11T05:38:19.3084240', N'2026-10-11T05:43:19.3084240', 0, NULL, 0),
        (5, N'FJ261011-003', 4, N'Đặng Thị Thu', N'+84987000003', N'Zalo', 4, 4, 35, 93, 128, N'2026-10-23', N'2026-10-25', 10, NULL, N'PaymentSubmitted', N'2026-10-11T06:18:19.3084240', N'2026-10-11T06:08:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 9600000, 0.3000, 2880000, 0.1200, 1, NULL, NULL, N'2026-10-11T06:03:19.3084240', N'2026-10-11T06:08:19.3084240', 0, NULL, 0),
        (6, N'FJ261011-004', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 26, 64, 89, N'2026-10-31', N'2026-11-01', 1, NULL, N'PaymentSubmitted', N'2026-10-11T03:43:19.3084240', N'2026-10-11T03:33:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 960000, 0.3000, 288000, 0.1200, 1, NULL, NULL, N'2026-10-11T03:28:19.3084240', N'2026-10-11T03:33:19.3084240', 0, NULL, 0),
        (7, N'FJ260601-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 7, 17, 24, N'2026-06-06', N'2026-06-08', 3, NULL, N'Rejected', N'2026-06-01T05:40:00', NULL, N'2026-06-01T05:34:00', N'2026-06-01T06:04:00', NULL, NULL, NULL, NULL, N'2026-06-01T06:03:00', N'Chủ nhà bận việc gia đình, không đón khách được.', 3520000, 0.3000, 1056000, 0.1000, 1, 1056000, NULL, N'2026-06-01T05:25:00', N'2026-06-01T06:03:00', 0, NULL, 0),
        (8, N'FJ260818-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 6, 15, 21, N'2026-09-08', N'2026-09-09', 3, NULL, N'NoShow', N'2026-08-18T01:36:00', NULL, N'2026-08-18T01:27:00', N'2026-08-18T01:57:00', NULL, N'2026-08-18T01:50:00', NULL, NULL, NULL, NULL, 1440000, 0.3000, 432000, 0.1500, 1, 0, NULL, N'2026-08-18T01:21:00', N'2026-09-08T16:00:00', 0, NULL, 0),
        (9, N'FJ261011-005', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 35, 93, 128, N'2026-12-20', N'2026-12-22', 10, NULL, N'Confirmed', N'2026-10-11T01:53:19.3084240', NULL, N'2026-10-11T01:43:19.3084240', N'2026-10-11T02:13:19.3084240', NULL, N'2026-10-11T02:01:19.3084240', NULL, NULL, NULL, NULL, 8000000, 0.3000, 2400000, 0.1200, 1, NULL, NULL, N'2026-10-11T01:38:19.3084240', N'2026-10-11T02:01:19.3084240', 0, NULL, 0),
        (10, N'FJ260804-001', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 33, 87, 120, N'2026-08-31', N'2026-09-02', 9, NULL, N'Cancelled', N'2026-08-04T02:03:00', NULL, N'2026-08-04T01:52:00', N'2026-08-04T02:22:00', NULL, N'2026-08-04T02:20:00', NULL, NULL, N'2026-08-26T21:01:15.0358745', N'Đặt nhầm ngày.', 8000000, 0.3000, 2400000, 0.1500, 4, 0, NULL, N'2026-08-04T01:48:00', N'2026-08-26T21:01:15.0358745', 0, NULL, 0),
        (11, N'FJ260705-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 8, 20, 28, N'2026-07-24', N'2026-07-25', 4, NULL, N'Completed', N'2026-07-05T05:10:00', NULL, N'2026-07-05T05:02:00', N'2026-07-05T05:32:00', NULL, N'2026-07-05T05:21:00', N'2026-07-24T08:22:00', N'2026-07-25T04:17:00', NULL, NULL, 770000, 0.3000, 231000, 0.1200, 1, NULL, NULL, N'2026-07-05T04:55:00', N'2026-07-25T04:17:00', 0, NULL, 0),
        (12, N'FJ260713-001', 9, N'Hồ Minh Khang', N'+84987000008', N'Zalo', 5, 5, 6, 15, 21, N'2026-08-09', N'2026-08-10', 4, NULL, N'Completed', N'2026-07-13T03:45:00', NULL, N'2026-07-13T03:40:00', N'2026-07-13T04:10:00', NULL, N'2026-07-13T04:00:00', N'2026-08-09T08:49:00', N'2026-08-10T04:58:00', NULL, NULL, 1440000, 0.3000, 432000, 0.1500, 1, NULL, NULL, N'2026-07-13T03:30:00', N'2026-08-10T04:58:00', 0, NULL, 0),
        (13, N'FJ260621-001', 4, N'Đặng Thị Thu', N'+84987000003', N'Phone', 4, 4, 8, 19, 26, N'2026-06-27', N'2026-06-28', 1, NULL, N'Completed', N'2026-06-21T04:22:00', N'2026-06-21T04:17:00', N'2026-06-21T05:16:00', N'2026-06-21T05:46:00', NULL, N'2026-06-21T05:22:00', N'2026-06-27T09:09:00', N'2026-06-28T04:53:00', NULL, NULL, 480000, 0.3000, 144000, 0.1200, 1, NULL, NULL, N'2026-06-21T04:07:00', N'2026-06-28T04:53:00', 0, NULL, 0),
        (14, N'FJ261009-001', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 21, 53, 74, N'2026-11-18', N'2026-11-19', 1, NULL, N'Rejected', N'2026-10-08T21:53:19.3084240', NULL, N'2026-10-08T21:40:19.3084240', N'2026-10-08T22:10:19.3084240', NULL, NULL, NULL, NULL, N'2026-10-08T21:56:19.3084240', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', 800000, 0.3000, 240000, 0.1500, 1, 240000, NULL, N'2026-10-08T21:38:19.3084240', N'2026-10-08T21:56:19.3084240', 0, NULL, 0),
        (15, N'FJ261011-006', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 1, 1, 1, N'2026-10-23', N'2026-10-26', 1, NULL, N'Confirmed', N'2026-10-10T19:53:19.3084240', NULL, N'2026-10-10T19:40:19.3084240', N'2026-10-10T20:10:19.3084240', NULL, N'2026-10-10T19:47:19.3084240', NULL, NULL, NULL, NULL, 1360000, 0.3000, 408000, 0.1000, 1, NULL, NULL, N'2026-10-10T19:38:19.3084240', N'2026-10-10T19:47:19.3084240', 0, NULL, 0),
        (16, N'FJ260601-002', 6, N'Phan Ngọc Ánh', N'+84987000005', N'Phone', 4, 4, 12, 29, 40, N'2026-06-10', N'2026-06-13', 2, NULL, N'Completed', N'2026-06-01T04:52:00', NULL, N'2026-06-01T04:49:00', N'2026-06-01T05:19:00', NULL, N'2026-06-01T04:55:00', N'2026-06-10T10:03:00', N'2026-06-13T04:40:00', NULL, NULL, 2560000, 0.3000, 768000, 0.1500, 1, NULL, NULL, N'2026-06-01T04:37:00', N'2026-06-13T04:40:00', 0, NULL, 0),
        (17, N'FJ261011-007', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 8, 20, 28, N'2026-10-25', N'2026-10-27', 3, NULL, N'Cancelled', N'2026-10-11T02:53:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-11T02:43:19.3084240', N'Nhóm bạn không đủ người.', 1280000, 0.3000, 384000, 0.1200, 1, 0, NULL, N'2026-10-11T02:38:19.3084240', N'2026-10-11T02:43:19.3084240', 0, NULL, 0),
        (18, N'FJ260601-003', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 10, 25, 35, N'2026-06-03', N'2026-06-04', 3, NULL, N'Completed', N'2026-06-01T01:15:00', NULL, N'2026-06-01T01:06:00', N'2026-06-01T01:36:00', NULL, N'2026-06-01T01:26:00', N'2026-06-03T08:20:00', N'2026-06-04T04:38:00', NULL, NULL, 960000, 0.3000, 288000, 0.1000, 1, NULL, NULL, N'2026-06-01T01:00:00', N'2026-06-04T04:38:00', 0, NULL, 0),
        (19, N'FJ260801-001', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 33, 85, 118, N'2026-08-17', N'2026-08-19', 1, NULL, N'Completed', N'2026-08-01T02:12:00', NULL, N'2026-08-01T02:09:00', N'2026-08-01T02:39:00', NULL, N'2026-08-01T02:23:00', N'2026-08-17T07:03:00', N'2026-08-19T04:45:00', NULL, NULL, 1600000, 0.3000, 480000, 0.1500, 1, NULL, NULL, N'2026-08-01T01:57:00', N'2026-08-19T04:45:00', 0, NULL, 0),
        (20, N'FJ260808-001', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 9, 21, 29, N'2026-09-02', N'2026-09-05', 1, NULL, N'Completed', N'2026-08-08T04:57:00', N'2026-08-08T04:54:00', N'2026-08-08T06:01:00', N'2026-08-08T06:31:00', NULL, N'2026-08-08T06:17:00', N'2026-09-02T10:36:00', N'2026-09-05T04:08:00', NULL, NULL, 1850000, 0.3000, 555000, 0.1500, 1, NULL, NULL, N'2026-08-08T04:42:00', N'2026-09-05T04:08:00', 0, NULL, 0),
        (21, N'FJ260821-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Phone', 4, 4, 13, 33, 46, N'2026-09-03', N'2026-09-06', 1, NULL, N'Expired', N'2026-08-21T02:08:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-08-21T02:09:00', NULL, 2440000, 0.3000, 732000, 0.1000, 1, NULL, NULL, N'2026-08-21T01:53:00', N'2026-08-21T02:09:00', 0, NULL, 0),
        (22, N'FJ260706-001', 9, N'Hồ Minh Khang', N'+84987000008', N'Phone', 5, 5, 21, 52, 73, N'2026-07-29', N'2026-08-01', 3, NULL, N'Completed', N'2026-07-06T03:55:00', NULL, N'2026-07-06T03:43:00', N'2026-07-06T04:13:00', NULL, N'2026-07-06T03:57:00', N'2026-07-29T07:18:00', N'2026-08-01T04:23:00', NULL, NULL, 5120000, 0.3000, 1536000, 0.1500, 1, NULL, NULL, N'2026-07-06T03:40:00', N'2026-08-01T04:23:00', 0, NULL, 0),
        (23, N'FJ260724-001', 5, N'Bùi Quốc Bảo', N'+84987000004', N'Web', NULL, NULL, 9, 22, 31, N'2026-08-10', N'2026-08-13', 3, NULL, N'Cancelled', N'2026-07-24T03:04:00', NULL, N'2026-07-24T02:54:00', N'2026-07-24T03:24:00', NULL, N'2026-07-24T03:09:00', NULL, NULL, N'2026-07-24T09:40:14.1792389', N'Đặt nhầm ngày.', 2400000, 0.3000, 720000, 0.1500, 1, 720000, NULL, N'2026-07-24T02:49:00', N'2026-07-24T09:40:14.1792389', 0, NULL, 0),
        (24, N'FJ260718-001', 6, N'Phan Ngọc Ánh', N'+84987000005', N'Web', NULL, NULL, 5, 12, 17, N'2026-08-04', N'2026-08-06', 3, NULL, N'Completed', N'2026-07-18T06:04:00', NULL, N'2026-07-18T05:59:00', N'2026-07-18T06:29:00', NULL, N'2026-07-18T06:27:00', N'2026-08-04T09:42:00', N'2026-08-06T04:03:00', NULL, NULL, 2560000, 0.3000, 768000, 0.1200, 1, NULL, NULL, N'2026-07-18T05:49:00', N'2026-08-06T04:03:00', 0, NULL, 0),
        (25, N'FJ260625-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 5, 5, 31, 80, 111, N'2026-07-15', N'2026-07-18', 4, NULL, N'Completed', N'2026-06-25T02:22:00', NULL, N'2026-06-25T02:13:00', N'2026-06-25T02:43:00', NULL, N'2026-06-25T02:34:00', N'2026-07-15T07:15:00', N'2026-07-18T04:36:00', NULL, NULL, 3070000, 0.3000, 921000, 0.1000, 1, NULL, NULL, N'2026-06-25T02:07:00', N'2026-07-18T04:36:00', 0, NULL, 0),
        (26, N'FJ261009-002', 4, N'Đặng Thị Thu', N'+84987000003', N'Web', NULL, NULL, 3, 6, 8, N'2026-10-30', N'2026-11-01', 1, NULL, N'Cancelled', N'2026-10-08T23:53:19.3084240', NULL, N'2026-10-08T23:47:19.3084240', N'2026-10-09T00:17:19.3084240', NULL, N'2026-10-09T00:01:19.3084240', NULL, NULL, N'2026-10-10T10:49:26.7675156', N'Đặt nhầm ngày.', 1440000, 0.3000, 432000, 0.1500, 1, 432000, NULL, N'2026-10-08T23:38:19.3084240', N'2026-10-10T10:49:26.7675156', 0, NULL, 0),
        (27, N'FJ261010-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Phone', 5, 5, 21, 52, 73, N'2026-10-28', N'2026-10-29', 3, NULL, N'Confirmed', N'2026-10-09T21:53:19.3084240', NULL, N'2026-10-09T21:48:19.3084240', N'2026-10-09T22:18:19.3084240', NULL, N'2026-10-09T22:05:19.3084240', NULL, NULL, NULL, NULL, 1600000, 0.3000, 480000, 0.1500, 1, NULL, NULL, N'2026-10-09T21:38:19.3084240', N'2026-10-09T22:05:19.3084240', 0, NULL, 0),
        (28, N'FJ260816-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Phone', 4, 4, 22, 54, 76, N'2026-08-31', N'2026-09-02', 2, NULL, N'Completed', N'2026-08-16T05:57:00', NULL, N'2026-08-16T05:45:00', N'2026-08-16T06:15:00', NULL, N'2026-08-16T05:57:00', N'2026-08-31T07:03:00', N'2026-09-02T04:34:00', NULL, NULL, 1000000, 0.3000, 300000, 0.1000, 1, NULL, NULL, N'2026-08-16T05:42:00', N'2026-09-02T04:34:00', 0, NULL, 0),
        (29, N'FJ260609-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 5, 5, 27, 69, 96, N'2026-06-28', N'2026-06-29', 9, NULL, N'Completed', N'2026-06-09T04:02:00', NULL, N'2026-06-09T03:52:00', N'2026-06-09T04:22:00', NULL, N'2026-06-09T04:18:00', N'2026-06-28T08:25:00', N'2026-06-29T04:34:00', NULL, NULL, 3600000, 0.3000, 1080000, 0.1500, 1, NULL, NULL, N'2026-06-09T03:47:00', N'2026-06-29T04:34:00', 0, NULL, 0),
        (30, N'FJ260730-001', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 3, 6, 9, N'2026-08-29', N'2026-09-01', 2, NULL, N'Completed', N'2026-07-30T03:20:00', NULL, N'2026-07-30T03:17:00', N'2026-07-30T03:47:00', NULL, N'2026-07-30T03:29:00', N'2026-08-29T08:45:00', N'2026-09-01T04:05:00', NULL, NULL, 1920000, 0.3000, 576000, 0.1500, 1, NULL, NULL, N'2026-07-30T03:05:00', N'2026-09-01T04:05:00', 0, NULL, 0),
        (31, N'FJ260605-001', 15, N'Kiều Văn Phúc', N'+84987000014', N'Zalo', 4, 4, 11, 27, 38, N'2026-06-22', N'2026-06-25', 3, NULL, N'Expired', N'2026-06-05T01:25:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-06-05T01:26:00', NULL, 3360000, 0.3000, 1008000, 0.1200, 1, NULL, NULL, N'2026-06-05T01:10:00', N'2026-06-05T01:26:00', 0, NULL, 0),
        (32, N'FJ261011-008', 14, N'Đinh Thị Yến', N'+84987000013', N'Web', NULL, NULL, 16, 40, 56, N'2026-12-27', N'2026-12-29', 4, NULL, N'Confirmed', N'2026-10-10T21:53:19.3084240', NULL, N'2026-10-10T21:46:19.3084240', N'2026-10-10T22:16:19.3084240', NULL, N'2026-10-10T22:11:19.3084240', NULL, NULL, NULL, NULL, 1600000, 0.3000, 480000, 0.1000, 1, NULL, NULL, N'2026-10-10T21:38:19.3084240', N'2026-10-10T22:11:19.3084240', 0, NULL, 0),
        (33, N'FJ260704-001', 6, N'Phan Ngọc Ánh', N'+84987000005', N'Web', NULL, NULL, 16, 40, 56, N'2026-07-27', N'2026-07-29', 3, NULL, N'Completed', N'2026-07-04T04:05:00', NULL, N'2026-07-04T03:56:00', N'2026-07-04T04:26:00', NULL, N'2026-07-04T04:08:00', N'2026-07-27T09:02:00', N'2026-07-29T04:35:00', NULL, NULL, 1600000, 0.3000, 480000, 0.1000, 1, NULL, NULL, N'2026-07-04T03:50:00', N'2026-07-29T04:35:00', 0, NULL, 0),
        (34, N'FJ261010-002', 15, N'Kiều Văn Phúc', N'+84987000014', N'Phone', 5, 5, 16, 39, 55, N'2026-11-22', N'2026-11-24', 1, NULL, N'Confirmed', N'2026-10-09T18:53:19.3084240', NULL, N'2026-10-09T18:42:19.3084240', N'2026-10-09T19:12:19.3084240', NULL, N'2026-10-09T19:03:19.3084240', NULL, NULL, NULL, NULL, 1000000, 0.3000, 300000, 0.1000, 1, NULL, NULL, N'2026-10-09T18:38:19.3084240', N'2026-10-09T19:03:19.3084240', 0, NULL, 0),
        (35, N'FJ260715-001', 11, N'Mai Đức Anh', N'+84987000010', N'Zalo', 4, 4, 7, 18, 25, N'2026-07-17', N'2026-07-18', 1, NULL, N'Completed', N'2026-07-15T06:05:00', NULL, N'2026-07-15T06:02:00', N'2026-07-15T06:32:00', NULL, N'2026-07-15T06:19:00', N'2026-07-17T10:39:00', N'2026-07-18T04:50:00', NULL, NULL, 960000, 0.3000, 288000, 0.1000, 1, NULL, NULL, N'2026-07-15T05:50:00', N'2026-07-18T04:50:00', 0, NULL, 0),
        (36, N'FJ260717-001', 11, N'Mai Đức Anh', N'+84987000010', N'Web', NULL, NULL, 10, 25, 35, N'2026-08-14', N'2026-08-16', 3, NULL, N'Completed', N'2026-07-17T01:17:00', NULL, N'2026-07-17T01:12:00', N'2026-07-17T01:42:00', NULL, N'2026-07-17T01:22:00', N'2026-08-14T09:55:00', N'2026-08-16T04:12:00', NULL, NULL, 2300000, 0.3000, 690000, 0.1000, 1, NULL, NULL, N'2026-07-17T01:02:00', N'2026-08-16T04:12:00', 0, NULL, 0),
        (37, N'FJ261008-001', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 31, 80, 111, N'2026-12-19', N'2026-12-20', 4, NULL, N'Expired', N'2026-10-08T16:53:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-08T16:54:19.3084240', NULL, 1150000, 0.3000, 345000, 0.1000, 1, NULL, NULL, N'2026-10-08T16:38:19.3084240', N'2026-10-08T16:54:19.3084240', 0, NULL, 0),
        (38, N'FJ260601-004', 15, N'Kiều Văn Phúc', N'+84987000014', N'Zalo', 5, 5, 15, 38, 53, N'2026-06-09', N'2026-06-12', 1, NULL, N'Completed', N'2026-06-01T05:25:00', N'2026-06-01T05:16:00', N'2026-06-01T07:18:00', N'2026-06-01T07:48:00', NULL, N'2026-06-01T07:33:00', N'2026-06-09T07:20:00', N'2026-06-12T04:09:00', NULL, NULL, 960000, 0.3000, 288000, 0.1500, 1, NULL, NULL, N'2026-06-01T05:10:00', N'2026-06-12T04:09:00', 0, NULL, 0),
        (39, N'FJ260902-001', 5, N'Bùi Quốc Bảo', N'+84987000004', N'Web', NULL, NULL, 31, 79, 110, N'2026-09-17', N'2026-09-18', 2, NULL, N'Completed', N'2026-09-02T03:25:00', NULL, N'2026-09-02T03:22:00', N'2026-09-02T03:52:00', NULL, N'2026-09-02T03:38:00', N'2026-09-17T09:06:00', N'2026-09-18T04:49:00', NULL, NULL, 600000, 0.3000, 180000, 0.1000, 1, NULL, NULL, N'2026-09-02T03:10:00', N'2026-09-18T04:49:00', 0, NULL, 0),
        (40, N'FJ261011-009', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 14, 35, 49, N'2026-12-08', N'2026-12-09', 4, NULL, N'Confirmed', N'2026-10-11T01:53:19.3084240', NULL, N'2026-10-11T01:47:19.3084240', N'2026-10-11T02:17:19.3084240', NULL, N'2026-10-11T02:00:19.3084240', NULL, NULL, NULL, NULL, 1600000, 0.3000, 480000, 0.1200, 1, NULL, NULL, N'2026-10-11T01:38:19.3084240', N'2026-10-11T02:00:19.3084240', 0, NULL, 0),
        (41, N'FJ261008-002', 8, N'Võ Thị Hồng', N'+84987000007', N'Phone', 5, 5, 13, 33, 46, N'2026-12-08', N'2026-12-09', 1, NULL, N'Confirmed', N'2026-10-08T12:53:19.3084240', NULL, N'2026-10-08T12:49:19.3084240', N'2026-10-08T13:19:19.3084240', NULL, N'2026-10-08T12:57:19.3084240', NULL, NULL, NULL, NULL, 720000, 0.3000, 216000, 0.1000, 1, NULL, NULL, N'2026-10-08T12:38:19.3084240', N'2026-10-08T12:57:19.3084240', 0, NULL, 0),
        (42, N'FJ261009-003', 15, N'Kiều Văn Phúc', N'+84987000014', N'Zalo', 5, 5, 2, 4, 5, N'2026-11-28', N'2026-11-30', 2, NULL, N'Cancelled', N'2026-10-09T06:53:19.3084240', NULL, N'2026-10-09T06:48:19.3084240', N'2026-10-09T07:18:19.3084240', NULL, N'2026-10-09T07:12:19.3084240', NULL, NULL, N'2026-10-09T17:31:49.8131972', N'Chuyển sang đi tỉnh khác.', 1100000, 0.3000, 330000, 0.1200, 1, 330000, NULL, N'2026-10-09T06:38:19.3084240', N'2026-10-09T17:31:49.8131972', 0, NULL, 0),
        (43, N'FJ260629-001', 7, N'Trịnh Văn Long', N'+84987000006', N'Phone', 5, 5, 4, 10, 14, N'2026-07-25', N'2026-07-28', 3, NULL, N'Completed', N'2026-06-29T03:07:00', N'2026-06-29T02:56:00', N'2026-06-29T05:39:00', N'2026-06-29T06:09:00', NULL, N'2026-06-29T05:53:00', N'2026-07-25T09:47:00', N'2026-07-28T04:53:00', NULL, NULL, 3580000, 0.3000, 1074000, 0.1000, 1, NULL, NULL, N'2026-06-29T02:52:00', N'2026-07-28T04:53:00', 0, NULL, 0),
        (44, N'FJ261010-003', 11, N'Mai Đức Anh', N'+84987000010', N'Phone', 5, 5, 20, 49, 69, N'2026-12-13', N'2026-12-16', 2, NULL, N'Cancelled', N'2026-10-10T03:53:19.3084240', NULL, N'2026-10-10T03:50:19.3084240', N'2026-10-10T04:20:19.3084240', NULL, N'2026-10-10T04:06:19.3084240', NULL, NULL, N'2026-10-10T19:24:49.5866868', N'Nhóm bạn không đủ người.', 2700000, 0.3000, 810000, 0.1200, 1, 810000, NULL, N'2026-10-10T03:38:19.3084240', N'2026-10-10T19:24:49.5866868', 0, NULL, 0),
        (45, N'FJ260601-005', 13, N'Tạ Quang Vinh', N'+84987000012', N'Web', NULL, NULL, 25, 63, 88, N'2026-06-26', N'2026-06-28', 1, NULL, N'Rejected', N'2026-06-01T03:10:00', NULL, N'2026-06-01T03:01:00', N'2026-06-01T03:31:00', NULL, NULL, NULL, NULL, N'2026-06-01T03:11:00', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', 1340000, 0.3000, 402000, 0.1000, 1, 402000, NULL, N'2026-06-01T02:55:00', N'2026-06-01T03:11:00', 0, NULL, 0),
        (46, N'FJ261010-004', 3, N'Lê Văn Hùng', N'+84912000001', N'Phone', 5, 5, 5, 13, 18, N'2026-10-22', N'2026-10-24', 1, NULL, N'Confirmed', N'2026-10-10T01:35:00', NULL, N'2026-10-10T01:22:00', N'2026-10-10T01:52:00', NULL, N'2026-10-10T01:32:00', NULL, NULL, NULL, NULL, 1410000, 0.3000, 423000, 0.1200, 1, NULL, NULL, N'2026-10-10T01:20:00', N'2026-10-10T01:32:00', 0, NULL, 0),
        (47, N'FJ261009-004', 9, N'Hồ Minh Khang', N'+84987000008', N'Zalo', 5, 5, 2, 5, 7, N'2026-10-29', N'2026-11-01', 3, NULL, N'Expired', N'2026-10-09T12:53:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-09T12:54:19.3084240', NULL, 2720000, 0.3000, 816000, 0.1200, 1, NULL, NULL, N'2026-10-09T12:38:19.3084240', N'2026-10-09T12:54:19.3084240', 0, NULL, 0),
        (48, N'FJ260913-001', 12, N'Chu Thị Ngân', N'+84987000011', N'Zalo', 4, 4, 17, 41, 58, N'2026-09-24', N'2026-09-27', 2, NULL, N'Completed', N'2026-09-13T05:02:00', NULL, N'2026-09-13T04:50:00', N'2026-09-13T05:20:00', NULL, N'2026-09-13T05:07:00', N'2026-09-24T09:02:00', N'2026-09-27T04:04:00', NULL, NULL, 2040000, 0.3000, 612000, 0.1200, 1, NULL, NULL, N'2026-09-13T04:47:00', N'2026-09-27T04:04:00', 0, NULL, 0),
        (49, N'FJ260601-006', 7, N'Trịnh Văn Long', N'+84987000006', N'Phone', 4, 4, 12, 30, 42, N'2026-06-13', N'2026-06-15', 4, NULL, N'Expired', N'2026-06-01T06:04:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-06-01T06:05:00', NULL, 2820000, 0.3000, 846000, 0.1500, 1, NULL, NULL, N'2026-06-01T05:49:00', N'2026-06-01T06:05:00', 0, NULL, 0),
        (50, N'FJ260910-001', 2, N'Trần Thị Bích', N'+84987000002', N'Phone', 5, 5, 17, 43, 60, N'2026-09-19', N'2026-09-20', 1, NULL, N'Completed', N'2026-09-10T02:28:00', NULL, N'2026-09-10T02:20:00', N'2026-09-10T02:50:00', NULL, N'2026-09-10T02:41:00', N'2026-09-19T08:02:00', N'2026-09-20T04:10:00', NULL, NULL, 580000, 0.3000, 174000, 0.1200, 1, NULL, NULL, N'2026-09-10T02:13:00', N'2026-09-20T04:10:00', 0, NULL, 0),
        (51, N'FJ261001-001', 5, N'Bùi Quốc Bảo', N'+84987000004', N'Web', NULL, NULL, 15, 38, 53, N'2026-10-03', N'2026-10-04', 1, NULL, N'Completed', N'2026-10-01T05:03:00', NULL, N'2026-10-01T04:53:00', N'2026-10-01T05:23:00', NULL, N'2026-10-01T05:11:00', N'2026-10-03T07:50:00', N'2026-10-04T04:35:00', NULL, NULL, 380000, 0.3000, 114000, 0.1500, 1, NULL, NULL, N'2026-10-01T04:48:00', N'2026-10-04T04:35:00', 0, NULL, 0),
        (52, N'FJ260817-001', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 3, 6, 9, N'2026-08-30', N'2026-09-02', 1, NULL, N'Cancelled', N'2026-08-17T01:49:00', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-08-17T01:42:00', N'Gia đình có việc đột xuất.', 2100000, 0.3000, 630000, 0.1500, 1, 0, NULL, N'2026-08-17T01:34:00', N'2026-08-17T01:42:00', 0, NULL, 0),
        (53, N'FJ260729-001', 15, N'Kiều Văn Phúc', N'+84987000014', N'Phone', 5, 5, 1, 2, 3, N'2026-08-16', N'2026-08-17', 3, NULL, N'Rejected', N'2026-07-29T02:44:00', NULL, N'2026-07-29T02:39:00', N'2026-07-29T03:09:00', NULL, NULL, NULL, NULL, N'2026-07-29T03:12:00', N'Chủ nhà bận việc gia đình, không đón khách được.', 640000, 0.3000, 192000, 0.1000, 1, 192000, NULL, N'2026-07-29T02:29:00', N'2026-07-29T03:12:00', 0, NULL, 0),
        (54, N'FJ261010-005', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 23, 56, 78, N'2026-12-25', N'2026-12-27', 1, NULL, N'Expired', N'2026-10-10T07:53:19.3084240', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-10T07:54:19.3084240', NULL, 1200000, 0.3000, 360000, 0.1200, 1, NULL, NULL, N'2026-10-10T07:38:19.3084240', N'2026-10-10T07:54:19.3084240', 0, NULL, 0),
        (55, N'FJ261011-010', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Phone', 5, 5, 30, 77, 107, N'2026-11-29', N'2026-11-30', 4, NULL, N'Confirmed', N'2026-10-10T22:53:19.3084240', NULL, N'2026-10-10T22:46:19.3084240', N'2026-10-10T23:16:19.3084240', NULL, N'2026-10-10T23:04:19.3084240', NULL, NULL, NULL, NULL, 800000, 0.3000, 240000, 0.1500, 1, NULL, NULL, N'2026-10-10T22:38:19.3084240', N'2026-10-10T23:04:19.3084240', 0, NULL, 0),
        (56, N'FJ260807-001', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 12, 30, 42, N'2026-09-06', N'2026-09-07', 3, NULL, N'Completed', N'2026-08-07T05:20:00', N'2026-08-07T05:14:00', N'2026-08-07T07:13:00', N'2026-08-07T07:43:00', NULL, N'2026-08-07T07:40:00', N'2026-09-06T09:30:00', N'2026-09-07T04:44:00', NULL, NULL, 1280000, 0.3000, 384000, 0.1500, 1, NULL, NULL, N'2026-08-07T05:05:00', N'2026-09-07T04:44:00', 0, NULL, 0),
        (57, N'FJ260715-002', 10, N'Lý Thanh Tâm', N'+84987000009', N'Web', NULL, NULL, 32, 84, 116, N'2026-07-17', N'2026-07-19', 10, NULL, N'Completed', N'2026-07-15T03:10:00', NULL, N'2026-07-15T03:07:00', N'2026-07-15T03:37:00', NULL, N'2026-07-15T03:27:00', N'2026-07-17T10:59:00', N'2026-07-19T04:00:00', NULL, NULL, 6720000, 0.3000, 2016000, 0.1200, 1, NULL, NULL, N'2026-07-15T02:55:00', N'2026-07-19T04:00:00', 0, NULL, 0),
        (58, N'FJ261010-006', 4, N'Đặng Thị Thu', N'+84987000003', N'Zalo', 4, 4, 28, 72, 100, N'2026-11-11', N'2026-11-12', 9, NULL, N'Confirmed', N'2026-10-09T21:53:19.3084240', NULL, N'2026-10-09T21:49:19.3084240', N'2026-10-09T22:19:19.3084240', NULL, N'2026-10-09T21:56:19.3084240', NULL, NULL, NULL, NULL, 4000000, 0.3000, 1200000, 0.1000, 1, NULL, NULL, N'2026-10-09T21:38:19.3084240', N'2026-10-09T21:56:19.3084240', 0, NULL, 0),
        (59, N'FJ260607-001', 13, N'Tạ Quang Vinh', N'+84987000012', N'Phone', 4, 4, 31, 79, 109, N'2026-06-12', N'2026-06-15', 2, NULL, N'Cancelled', N'2026-06-07T04:03:00', N'2026-06-07T03:57:00', N'2026-06-07T05:32:00', N'2026-06-07T06:02:00', NULL, N'2026-06-07T05:50:00', NULL, NULL, N'2026-06-07T06:07:27.4097985', N'Nhóm bạn không đủ người.', 2040000, 0.3000, 612000, 0.1000, 1, 306000, NULL, N'2026-06-07T03:48:00', N'2026-06-07T06:07:27.4097985', 0, NULL, 0),
        (60, N'FJ260809-001', 8, N'Võ Thị Hồng', N'+84987000007', N'Web', NULL, NULL, 31, 81, 112, N'2026-08-10', N'2026-08-12', 9, NULL, N'Completed', N'2026-08-09T04:35:00', NULL, N'2026-08-09T04:23:00', N'2026-08-09T04:53:00', NULL, N'2026-08-09T04:28:00', N'2026-08-10T10:54:00', N'2026-08-12T04:13:00', NULL, NULL, 4800000, 0.3000, 1440000, 0.1000, 1, NULL, NULL, N'2026-08-09T04:20:00', N'2026-08-12T04:13:00', 0, NULL, 0),
        (1002, N'FJ261012-001', 1003, N'PHU', N'+84984562321', N'Web', NULL, NULL, 6, 14, 19, N'2026-10-12', N'2026-10-13', 2, NULL, N'Confirmed', N'2026-10-12T06:53:54.4090760', N'2026-10-12T06:40:57.6791440', N'2026-10-12T06:43:29.4961970', N'2026-10-12T07:13:29.4961970', N'2026-10-12T07:20:23.2855080', N'2026-10-18T14:12:06.7208870', NULL, NULL, NULL, NULL, 900000, 0.3000, 270000, 0.1500, 1, NULL, NULL, N'2026-10-12T06:38:54.4090760', N'2026-10-18T14:12:06.7208870', 0, NULL, 0),
        (1003, N'FJ261012-002', 3, N'Lê Văn Hùng', N'+84912000001', N'Web', NULL, NULL, 1, 1, 1, N'2026-12-08', N'2026-12-10', 2, NULL, N'Expired', N'2026-10-12T08:27:38.3018470', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-12T08:27:39.8517500', NULL, 800000, 0.3000, 240000, 0.1000, 1, NULL, NULL, N'2026-10-12T08:12:38.3018470', N'2026-10-12T08:27:39.8517500', 0, NULL, 0),
        (1004, N'FJ261012-003', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 27, 67, 93, N'2026-10-12', N'2026-10-13', 2, NULL, N'Deposited', N'2026-10-12T08:58:11.8493900', N'2026-10-12T08:49:39.4398490', N'2026-10-12T08:50:41.7864170', N'2026-10-12T09:20:41.7864170', N'2026-10-12T09:21:23.1089800', NULL, NULL, NULL, NULL, NULL, 900000, 0.3000, 270000, 0.1500, 1, NULL, NULL, N'2026-10-12T08:43:11.8493900', N'2026-10-12T08:50:41.7864170', 0, NULL, 0),
        (1005, N'FJ261013-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-10-15', N'2026-10-20', 2, NULL, N'Expired', N'2026-10-13T09:00:25.2441720', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-13T09:01:10.1374710', NULL, 4860000, 0.3000, 1458000, 0.1500, 1, NULL, NULL, N'2026-10-13T08:45:25.2441720', N'2026-10-13T09:01:10.1374710', 0, NULL, 0),
        (2005, N'FJ261015-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-10-16', N'2026-10-17', 2, NULL, N'Expired', N'2026-10-14T18:57:51.5509430', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-14T18:58:17.8616950', NULL, 1080000, 0.3000, 324000, 0.1500, 1, NULL, NULL, N'2026-10-14T18:42:51.5509430', N'2026-10-14T18:58:17.8616950', 0, NULL, 0),
        (2006, N'FJ261015-002', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 7, 16, 22, N'2026-10-16', N'2026-10-17', 2, NULL, N'PaymentSubmitted', N'2026-10-15T06:05:17.2051610', N'2026-10-15T05:50:42.8281500', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1200000, 0.3000, 360000, 0.1000, 1, NULL, NULL, N'2026-10-15T05:50:17.2051610', N'2026-10-15T05:50:42.8281500', 0, NULL, 0),
        (2007, N'FJ261015-003', 1004, N'Đỗ Văn Toàn', N'+84384919501', N'Web', NULL, NULL, 11, 26, 36, N'2026-10-15', N'2026-10-16', 2, N'chogay', N'Expired', N'2026-10-15T08:39:05.5437350', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-15T08:39:28.2058970', NULL, 700000, 0.3000, 210000, 0.1200, 1, NULL, NULL, N'2026-10-15T08:24:05.5437350', N'2026-10-15T08:39:28.2058970', 0, NULL, 0),
        (2008, N'FJ261018-001', 1005, N'Khách Thử Giao Diện', N'+84988123456', N'Web', NULL, NULL, 6, 14, 19, N'2026-11-03', N'2026-11-05', 2, NULL, N'Expired', N'2026-10-17T17:58:06.4411050', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-17T17:58:42.4077070', NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-17T17:43:06.4411050', N'2026-10-17T17:58:42.4077070', 0, NULL, 0),
        (3008, N'FJ261018-002', 2005, N'Phạm Ngọc Phú', N'+84338405548', N'Web', NULL, NULL, 6, 14, 19, N'2026-10-18', N'2026-10-19', 2, NULL, N'Confirmed', N'2026-10-18T14:23:48.4177850', N'2026-10-18T14:09:11.0850330', N'2026-10-18T14:11:41.7048330', N'2026-10-18T14:41:41.7048330', N'2026-10-18T14:41:57.0207360', N'2026-10-19T16:53:35.5451720', NULL, NULL, NULL, NULL, 900000, 0.3000, 270000, 0.1500, 1, NULL, NULL, N'2026-10-18T14:08:48.4177850', N'2026-10-19T16:53:35.5451720', 0, NULL, 0),
        (3009, N'FJ261018-003', 2006, N'Kiểm thử giao diện 03y', N'+84988123403', N'Web', NULL, NULL, 6, 14, 19, N'2026-11-23', N'2026-11-25', 2, N'Kiểm thử giao diện bước 03y ngày 18/10/2026. Không thanh toán.', N'Expired', N'2026-10-18T16:05:24.0693020', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-18T16:05:47.4247760', NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-18T15:50:24.0693020', N'2026-10-18T16:05:47.4247760', 0, NULL, 0),
        (3010, N'FJ261018-004', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-11-30', N'2026-12-02', 2, N'Kiểm thử giao diện bước 03y, khách seed, không thanh toán.', N'Expired', N'2026-10-18T16:08:53.8828150', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-18T16:08:56.9890200', NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-18T15:53:53.8828150', N'2026-10-18T16:08:56.9890200', 0, NULL, 0),
        (3011, N'FJ261018-005', 1, N'Khách thử giao diện 03y2', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-12-07', N'2026-12-09', 2, NULL, N'Expired', N'2026-10-18T17:05:02.4506700', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-18T17:06:17.0276480', NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-18T16:50:02.4506700', N'2026-10-18T17:06:17.0276480', 0, NULL, 0),
        (3012, N'FJ261018-006', 1, N'Khách kiểm tra 03y2', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-12-14', N'2026-12-16', 2, NULL, N'Expired', N'2026-10-18T17:05:13.0509500', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-18T17:06:17.3178910', NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-18T16:50:13.0509500', N'2026-10-18T17:06:17.3178910', 0, NULL, 0),
        (3013, N'FJ261019-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, 4, 6, 14, 19, N'2026-12-15', N'2026-12-17', 2, N'Kiểm thử giao diện bước 04 – đối chiếu giao dịch và xác nhận chủ', N'Confirmed', N'2026-10-19T02:15:57.3134650', NULL, N'2026-10-19T02:01:54.5502580', N'2026-10-19T02:31:54.5502580', NULL, N'2026-10-19T02:01:56.6198080', NULL, NULL, NULL, NULL, 1800000, 0.3000, 540000, 0.1500, 1, NULL, NULL, N'2026-10-19T02:00:57.3134650', N'2026-10-19T07:27:46.4069870', 0, NULL, 0),
        (3014, N'FJ261019-002', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, 4, 27, 68, 95, N'2026-10-19', N'2026-10-20', 2, NULL, N'Expired', N'2026-10-19T03:15:19.3186420', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-19T06:56:24.4807020', NULL, 1440000, 0.3000, 432000, 0.1500, 1, NULL, NULL, N'2026-10-19T03:00:19.3186420', N'2026-10-19T08:33:09.8015580', 0, NULL, 0),
        (3015, N'FJ261019-003', 2007, N'Khách kiểm thử 05b', N'+84977050505', N'Phone', 4, 5, 6, 14, 19, N'2026-12-05', N'2026-12-07', 2, NULL, N'Confirmed', N'2026-10-19T07:41:02.6853570', NULL, N'2026-10-19T07:31:33.5061180', N'2026-10-19T08:01:33.5061180', NULL, N'2026-10-19T07:31:35.0057400', NULL, NULL, NULL, NULL, 1985000, 0.3000, 596000, 0.1500, 1, NULL, NULL, N'2026-10-19T07:26:02.6853570', N'2026-10-19T08:36:19.9716120', 0, NULL, 0),
        (3016, N'FJ261019-004', 2008, N'Khách thử 05b tiếp tục', N'+84977050519', N'Zalo', 4, 4, 6, 14, 19, N'2026-12-10', N'2026-12-12', 2, NULL, N'Expired', N'2026-10-19T08:48:41.9903700', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-19T08:49:19.2078730', NULL, 1985000, 0.3000, 596000, 0.1500, 1, NULL, NULL, N'2026-10-19T08:33:41.9903700', N'2026-10-19T08:49:19.2078730', 0, NULL, 0),
        (3017, N'FJ261019-005', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-12-20', N'2026-12-21', 2, N'B9: đơn thử kiểm link thông báo đặt cọc, giữ riêng với FJ261019-001.', N'Deposited', N'2026-10-19T14:29:16.8569200', NULL, N'2026-10-19T14:14:46.0080920', N'2026-10-19T14:44:46.0080920', N'2026-10-19T14:48:46.3771760', NULL, NULL, NULL, NULL, NULL, 905000, 0.3000, 272000, 0.1500, 1, NULL, NULL, N'2026-10-19T14:14:16.8569200', N'2026-10-19T14:14:46.0080920', 0, NULL, 0),
        (3018, N'FJ261019-006', 2009, N'Khách QA 06', N'+84977060606', N'Web', NULL, NULL, 6, 14, 19, N'2026-12-27', N'2026-12-29', 2, NULL, N'Expired', N'2026-10-19T15:20:15.5981980', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-19T15:40:22.2335870', NULL, 1810000, 0.3000, 543000, 0.1500, 1, NULL, NULL, N'2026-10-19T15:05:15.5981980', N'2026-10-19T15:40:22.2335870', 0, NULL, 0),
        (3019, N'FJ261019-007', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 27, 68, 95, N'2026-10-19', N'2026-10-21', 2, NULL, N'Deposited', N'2026-10-19T17:09:56.5997020', N'2026-10-19T16:55:31.5086720', N'2026-10-19T16:57:04.5238510', N'2026-10-19T17:27:04.5238510', N'2026-10-19T17:27:18.6716030', NULL, NULL, NULL, NULL, NULL, 2880000, 0.3000, 864000, 0.1500, 1, NULL, NULL, N'2026-10-19T16:54:56.5997020', N'2026-10-19T16:57:04.5238510', 0, NULL, 0),
        (4008, N'FJ261020-001', 1, N'Nguyễn Văn Minh', N'+84987000001', N'Web', NULL, NULL, 6, 14, 19, N'2026-10-20', N'2026-10-22', 2, NULL, N'Confirmed', N'2026-10-19T19:26:03.0259660', N'2026-10-19T19:11:39.3282090', N'2026-10-19T19:13:50.8100110', N'2026-10-19T19:43:50.8100110', NULL, N'2026-10-19T19:15:14.2673750', NULL, NULL, NULL, NULL, 1810000, 0.3000, 543000, 0.1500, 1, NULL, NULL, N'2026-10-19T19:11:03.0259660', N'2026-10-19T19:15:14.2673750', 0, NULL, 0);
    SET IDENTITY_INSERT [dbo].[Bookings] OFF;
    DBCC CHECKIDENT (N'[dbo].[Bookings]', RESEED, 4008) WITH NO_INFOMSGS;

    -- RoomBlocks: 6 dòng.
    SET IDENTITY_INSERT [dbo].[RoomBlocks] ON;
    INSERT INTO [dbo].[RoomBlocks] ([Id], [RoomId], [StartDate], [EndDate], [Reason], [Note], [CreatedByAccountId], [CreatedAt]) VALUES
        (1, 57, N'2026-09-22', N'2026-09-25', N'Maintenance', N'Sơn lại tường, thay rèm cửa.', N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'2026-09-15T13:00:00'),
        (2, 98, N'2026-10-14', N'2026-10-16', N'OtherChannel', N'Khách quen đặt qua Zalo của chủ nhà.', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', N'2026-10-07T13:00:00'),
        (3, 72, N'2026-10-17', N'2026-10-18', N'OtherChannel', N'Khách đặt qua Facebook.', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'2026-10-10T13:00:00'),
        (4, 127, N'2026-10-23', N'2026-10-27', N'Maintenance', N'Bảo dưỡng điều hòa và bình nóng lạnh.', N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'2026-10-16T13:00:00'),
        (5, 62, N'2026-10-30', N'2026-11-01', N'OtherChannel', N'Đoàn khách công ty đặt trực tiếp.', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', N'2026-10-23T13:00:00'),
        (6, 96, N'2026-11-11', N'2026-11-14', N'OtherChannel', N'Khách đặt qua Facebook.', N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'2026-11-04T13:00:00');
    SET IDENTITY_INSERT [dbo].[RoomBlocks] OFF;
    DBCC CHECKIDENT (N'[dbo].[RoomBlocks]', RESEED, 1006) WITH NO_INFOMSGS;

    -- BankTransactions: 50 dòng.
    SET IDENTITY_INSERT [dbo].[BankTransactions] ON;
    INSERT INTO [dbo].[BankTransactions] ([Id], [Provider], [ProviderTransactionId], [BankReference], [AccountNumber], [Amount], [TransferContent], [TransactionTime], [ReceivedAt], [RawPayload], [MatchStatus], [MatchedBookingId], [ProcessedByEmployeeId], [ProcessedAt], [Note], [RefundBankRef], [RefundedAt]) VALUES
        (1, N'SePay', N'SEED-0046', NULL, N'0338405548', 450000, N'NGUYEN VAN A chuyen tien homestay', N'2026-10-10T10:38:19.3084240', N'2026-10-10T10:38:19.3084240', NULL, N'Unmatched', NULL, NULL, NULL, N'Không tìm thấy mã đơn trong nội dung chuyển khoản.', NULL, NULL),
        (2, N'SePay', N'SEED-0001', NULL, N'0338405548', 864000, N'FJ260920-001 dat coc', N'2026-09-20T05:43:00', N'2026-09-20T05:43:00', NULL, N'Matched', 1, NULL, NULL, NULL, NULL, NULL),
        (3, N'SePay', N'SEED-0002', NULL, N'0338405548', 96000, N'FJ261011-001 dat coc', N'2026-10-11T06:21:19.3084240', N'2026-10-11T06:21:19.3084240', NULL, N'Matched', 3, NULL, NULL, NULL, NULL, NULL),
        (4, N'SePay', N'SEED-0003', NULL, N'0338405548', 240000, N'FJ261011-002 dat coc', N'2026-10-11T05:43:19.3084240', N'2026-10-11T05:43:19.3084240', NULL, N'Matched', 4, NULL, NULL, NULL, NULL, NULL),
        (5, N'SePay', N'SEED-0004', NULL, N'0338405548', 1056000, N'FJ260601-001', N'2026-06-01T05:34:00', N'2026-06-01T05:34:00', NULL, N'Matched', 7, NULL, NULL, NULL, NULL, NULL),
        (6, N'SePay', N'SEED-0005', NULL, N'0338405548', 432000, N'FJ260818-001 dat coc', N'2026-08-18T01:27:00', N'2026-08-18T01:27:00', NULL, N'Matched', 8, NULL, NULL, NULL, NULL, NULL),
        (7, N'SePay', N'SEED-0006', NULL, N'0338405548', 2400000, N'FJ261011-005', N'2026-10-11T01:43:19.3084240', N'2026-10-11T01:43:19.3084240', NULL, N'Matched', 9, NULL, NULL, NULL, NULL, NULL),
        (8, N'SePay', N'SEED-0007', NULL, N'0338405548', 2400000, N'MBVCB.560275.FJ260804001.CT tu Đinh Thị Yến', N'2026-08-04T01:52:00', N'2026-08-04T01:52:00', NULL, N'Matched', 10, NULL, NULL, NULL, NULL, NULL),
        (9, N'SePay', N'SEED-0008', NULL, N'0338405548', 231000, N'MBVCB.462430.FJ260705001.CT tu Lê Văn Hùng', N'2026-07-05T05:02:00', N'2026-07-05T05:02:00', NULL, N'Matched', 11, NULL, NULL, NULL, NULL, NULL),
        (10, N'SePay', N'SEED-0009', NULL, N'0338405548', 432000, N'MBVCB.428352.FJ260713001.CT tu Hồ Minh Khang', N'2026-07-13T03:40:00', N'2026-07-13T03:40:00', NULL, N'Matched', 12, NULL, NULL, NULL, NULL, NULL),
        (11, N'SePay', N'SEED-0010', NULL, N'0338405548', 240000, N'MBVCB.948530.FJ261009001.CT tu Lý Thanh Tâm', N'2026-10-08T21:40:19.3084240', N'2026-10-08T21:40:19.3084240', NULL, N'Matched', 14, NULL, NULL, NULL, NULL, NULL),
        (12, N'SePay', N'SEED-0011', NULL, N'0338405548', 408000, N'FJ261011-006', N'2026-10-10T19:40:19.3084240', N'2026-10-10T19:40:19.3084240', NULL, N'Matched', 15, NULL, NULL, NULL, NULL, NULL),
        (13, N'SePay', N'SEED-0012', NULL, N'0338405548', 768000, N'MBVCB.901086.FJ260601002.CT tu Phan Ngọc Ánh', N'2026-06-01T04:49:00', N'2026-06-01T04:49:00', NULL, N'Matched', 16, NULL, NULL, NULL, NULL, NULL),
        (14, N'SePay', N'SEED-0013', NULL, N'0338405548', 288000, N'FJ260601-003', N'2026-06-01T01:06:00', N'2026-06-01T01:06:00', NULL, N'Matched', 18, NULL, NULL, NULL, NULL, NULL),
        (15, N'SePay', N'SEED-0014', NULL, N'0338405548', 480000, N'MBVCB.365648.FJ260801001.CT tu Võ Thị Hồng', N'2026-08-01T02:09:00', N'2026-08-01T02:09:00', NULL, N'Matched', 19, NULL, NULL, NULL, NULL, NULL),
        (16, N'SePay', N'SEED-0016', NULL, N'0338405548', 1536000, N'FJ260706-001', N'2026-07-06T03:43:00', N'2026-07-06T03:43:00', NULL, N'Matched', 22, NULL, NULL, NULL, NULL, NULL),
        (17, N'SePay', N'SEED-0017', NULL, N'0338405548', 720000, N'MBVCB.625312.FJ260724001.CT tu Bùi Quốc Bảo', N'2026-07-24T02:54:00', N'2026-07-24T02:54:00', NULL, N'Matched', 23, NULL, NULL, NULL, NULL, NULL),
        (18, N'SePay', N'SEED-0018', NULL, N'0338405548', 768000, N'FJ260718-001 dat coc', N'2026-07-18T05:59:00', N'2026-07-18T05:59:00', NULL, N'Matched', 24, NULL, NULL, NULL, NULL, NULL),
        (19, N'SePay', N'SEED-0019', NULL, N'0338405548', 921000, N'FJ260625-001 dat coc', N'2026-06-25T02:13:00', N'2026-06-25T02:13:00', NULL, N'Matched', 25, NULL, NULL, NULL, NULL, NULL),
        (20, N'SePay', N'SEED-0020', NULL, N'0338405548', 432000, N'FJ261009-002', N'2026-10-08T23:47:19.3084240', N'2026-10-08T23:47:19.3084240', NULL, N'Matched', 26, NULL, NULL, NULL, NULL, NULL),
        (21, N'SePay', N'SEED-0021', NULL, N'0338405548', 480000, N'FJ261010-001 dat coc', N'2026-10-09T21:48:19.3084240', N'2026-10-09T21:48:19.3084240', NULL, N'Matched', 27, NULL, NULL, NULL, NULL, NULL),
        (22, N'SePay', N'SEED-0022', NULL, N'0338405548', 300000, N'FJ260816-001', N'2026-08-16T05:45:00', N'2026-08-16T05:45:00', NULL, N'Matched', 28, NULL, NULL, NULL, NULL, NULL),
        (23, N'SePay', N'SEED-0023', NULL, N'0338405548', 1080000, N'FJ260609-001 dat coc', N'2026-06-09T03:52:00', N'2026-06-09T03:52:00', NULL, N'Matched', 29, NULL, NULL, NULL, NULL, NULL),
        (24, N'SePay', N'SEED-0024', NULL, N'0338405548', 576000, N'MBVCB.539625.FJ260730001.CT tu Võ Thị Hồng', N'2026-07-30T03:17:00', N'2026-07-30T03:17:00', NULL, N'Matched', 30, NULL, NULL, NULL, NULL, NULL),
        (25, N'SePay', N'SEED-0026', NULL, N'0338405548', 480000, N'FJ261011-008', N'2026-10-10T21:46:19.3084240', N'2026-10-10T21:46:19.3084240', NULL, N'Matched', 32, NULL, NULL, NULL, NULL, NULL),
        (26, N'SePay', N'SEED-0027', NULL, N'0338405548', 480000, N'FJ260704-001 dat coc', N'2026-07-04T03:56:00', N'2026-07-04T03:56:00', NULL, N'Matched', 33, NULL, NULL, NULL, NULL, NULL),
        (27, N'SePay', N'SEED-0028', NULL, N'0338405548', 300000, N'MBVCB.404921.FJ261010002.CT tu Kiều Văn Phúc', N'2026-10-09T18:42:19.3084240', N'2026-10-09T18:42:19.3084240', NULL, N'Matched', 34, NULL, NULL, NULL, NULL, NULL),
        (28, N'SePay', N'SEED-0029', NULL, N'0338405548', 288000, N'MBVCB.647706.FJ260715001.CT tu Mai Đức Anh', N'2026-07-15T06:02:00', N'2026-07-15T06:02:00', NULL, N'Matched', 35, NULL, NULL, NULL, NULL, NULL),
        (29, N'SePay', N'SEED-0030', NULL, N'0338405548', 690000, N'MBVCB.429082.FJ260717001.CT tu Mai Đức Anh', N'2026-07-17T01:12:00', N'2026-07-17T01:12:00', NULL, N'Matched', 36, NULL, NULL, NULL, NULL, NULL),
        (30, N'SePay', N'SEED-0031', NULL, N'0338405548', 180000, N'MBVCB.959950.FJ260902001.CT tu Bùi Quốc Bảo', N'2026-09-02T03:22:00', N'2026-09-02T03:22:00', NULL, N'Matched', 39, NULL, NULL, NULL, NULL, NULL),
        (31, N'SePay', N'SEED-0032', NULL, N'0338405548', 480000, N'FJ261011-009', N'2026-10-11T01:47:19.3084240', N'2026-10-11T01:47:19.3084240', NULL, N'Matched', 40, NULL, NULL, NULL, NULL, NULL),
        (32, N'SePay', N'SEED-0033', NULL, N'0338405548', 216000, N'FJ261008-002', N'2026-10-08T12:49:19.3084240', N'2026-10-08T12:49:19.3084240', NULL, N'Matched', 41, NULL, NULL, NULL, NULL, NULL),
        (33, N'SePay', N'SEED-0034', NULL, N'0338405548', 330000, N'FJ261009-003', N'2026-10-09T06:48:19.3084240', N'2026-10-09T06:48:19.3084240', NULL, N'Matched', 42, NULL, NULL, NULL, NULL, NULL),
        (34, N'SePay', N'SEED-0035', NULL, N'0338405548', 810000, N'MBVCB.875883.FJ261010003.CT tu Mai Đức Anh', N'2026-10-10T03:50:19.3084240', N'2026-10-10T03:50:19.3084240', NULL, N'Matched', 44, NULL, NULL, NULL, NULL, NULL),
        (35, N'SePay', N'SEED-0036', NULL, N'0338405548', 402000, N'FJ260601-005 dat coc', N'2026-06-01T03:01:00', N'2026-06-01T03:01:00', NULL, N'Matched', 45, NULL, NULL, NULL, NULL, NULL),
        (36, N'SePay', N'SEED-0037', NULL, N'0338405548', 423000, N'FJ261010-004', N'2026-10-10T01:22:00', N'2026-10-10T01:22:00', NULL, N'Matched', 46, NULL, NULL, NULL, NULL, NULL),
        (37, N'SePay', N'SEED-0038', NULL, N'0338405548', 612000, N'MBVCB.368766.FJ260913001.CT tu Chu Thị Ngân', N'2026-09-13T04:50:00', N'2026-09-13T04:50:00', NULL, N'Matched', 48, NULL, NULL, NULL, NULL, NULL),
        (38, N'SePay', N'SEED-0039', NULL, N'0338405548', 174000, N'FJ260910-001 dat coc', N'2026-09-10T02:20:00', N'2026-09-10T02:20:00', NULL, N'Matched', 50, NULL, NULL, NULL, NULL, NULL),
        (39, N'SePay', N'SEED-0040', NULL, N'0338405548', 114000, N'FJ261001-001', N'2026-10-01T04:53:00', N'2026-10-01T04:53:00', NULL, N'Matched', 51, NULL, NULL, NULL, NULL, NULL),
        (40, N'SePay', N'SEED-0041', NULL, N'0338405548', 192000, N'FJ260729-001', N'2026-07-29T02:39:00', N'2026-07-29T02:39:00', NULL, N'Matched', 53, NULL, NULL, NULL, NULL, NULL),
        (41, N'SePay', N'SEED-0042', NULL, N'0338405548', 240000, N'FJ261011-010 dat coc', N'2026-10-10T22:46:19.3084240', N'2026-10-10T22:46:19.3084240', NULL, N'Matched', 55, NULL, NULL, NULL, NULL, NULL),
        (42, N'SePay', N'SEED-0043', NULL, N'0338405548', 2016000, N'FJ260715-002', N'2026-07-15T03:07:00', N'2026-07-15T03:07:00', NULL, N'Matched', 57, NULL, NULL, NULL, NULL, NULL),
        (43, N'SePay', N'SEED-0044', NULL, N'0338405548', 1200000, N'MBVCB.974092.FJ261010006.CT tu Đặng Thị Thu', N'2026-10-09T21:49:19.3084240', N'2026-10-09T21:49:19.3084240', NULL, N'Matched', 58, NULL, NULL, NULL, NULL, NULL),
        (44, N'SePay', N'SEED-0045', NULL, N'0338405548', 1440000, N'FJ260809-001 dat coc', N'2026-08-09T04:23:00', N'2026-08-09T04:23:00', NULL, N'Matched', 60, NULL, NULL, NULL, NULL, NULL),
        (45, N'SePay', N'SEED-0015', NULL, N'0338405548', 682000, N'FJ260821-001 dat coc', N'2026-08-21T02:01:00', N'2026-08-21T02:01:00', NULL, N'Underpaid', 21, NULL, NULL, N'Thiếu 50.000đ so với tiền cọc.', NULL, NULL),
        (46, N'SePay', N'SEED-0025', NULL, N'0338405548', 1008000, N'FJ260605-001 dat coc', N'2026-06-05T02:06:00', N'2026-06-05T02:06:00', NULL, N'LatePayment', 31, NULL, NULL, NULL, NULL, NULL),
        (1002, N'SePay', N'SEED-R001', NULL, N'0338405548', 300000, N'TRAN THI HOA chuyen tien', N'2026-09-27T03:22:18.1687740', N'2026-09-27T03:22:18.1687740', NULL, N'Refunded', NULL, 3, N'2026-09-28T03:22:18.1687740', N'Khách chuyển nhầm tài khoản, đã gọi xác nhận và chuyển trả.', N'FT2609280001', N'2026-09-28T03:22:18.1687740'),
        (1003, N'Simulator', N'SIM-bdbe4caf3bb24f55b7ad074d4ab0bbc0', NULL, N'0338405548', 540000, N'UI04 doi chieu 20261019', N'2026-10-19T02:01:23.5302110', N'2026-10-19T02:01:23.5583240', NULL, N'ManuallyMatched', 3013, 3, N'2026-10-19T02:01:54.5476790', N'Kiểm thử giao diện bước 04', NULL, NULL),
        (1004, N'Simulator', N'SIM-b451f3f23f30495793f5285517839722', NULL, N'0338405548', 596000, N'FJ261019-003 dat coc', N'2026-10-19T07:31:33.3856770', N'2026-10-19T07:31:33.4217360', NULL, N'Matched', 3015, NULL, NULL, NULL, NULL, NULL),
        (1005, N'Simulator', N'SIM-7ac22a09d28f49a8a29907fe41195b47', NULL, N'0338405548', 272000, N'FJ261019-005 dat coc B9', N'2026-10-19T14:14:45.9449900', N'2026-10-19T14:14:45.9729760', NULL, N'Matched', 3017, NULL, NULL, NULL, NULL, NULL);
    SET IDENTITY_INSERT [dbo].[BankTransactions] OFF;
    DBCC CHECKIDENT (N'[dbo].[BankTransactions]', RESEED, 2002) WITH NO_INFOMSGS;

    -- BookingChangeRequests: 6 dòng.
    SET IDENTITY_INSERT [dbo].[BookingChangeRequests] ON;
    INSERT INTO [dbo].[BookingChangeRequests] ([Id], [BookingId], [RequestType], [RequestedByType], [NewCheckIn], [NewCheckOut], [NewRoomTypeId], [PriceDifference], [RefundAmount], [Status], [DecidedByAccountId], [DecidedAt], [DecisionNote], [CreatedAt], [AdditionalDeposit], [ChangeFee], [DepositCarriedOver], [FinanceConfirmedAt], [FinanceConfirmedByAccountId], [FinanceNote], [NewRoomId], [NewTotalAmount], [PaymentDeadlineAt], [RejectionReason], [RequestedByAccountId]) VALUES
        (1, 46, N'ChangeDates', N'Customer', N'2026-10-29', N'2026-10-31', NULL, 0, NULL, N'Pending', NULL, NULL, NULL, N'2026-10-12T00:22:18.1687740', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (2, 15, N'ChangeDates', N'Employee', N'2026-10-24', N'2026-10-27', NULL, -80000, NULL, N'Rejected', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'2026-10-10T23:22:18.1687740', N'Ngày mới homestay đã kín phòng, mong khách thông cảm.', N'2026-10-10T21:22:18.1687740', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (3, 27, N'ChangeDates', N'Customer', N'2026-10-28', N'2026-10-29', NULL, 0, NULL, N'Applied', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'2026-10-10T04:22:18.1687740', N'Đồng ý đổi ngày.', N'2026-10-10T03:22:18.1687740', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (4, 34, N'ChangeRoomType', N'Customer', NULL, NULL, 40, 600000, NULL, N'Pending', NULL, NULL, NULL, N'2026-10-11T22:22:18.1687740', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (5, 58, N'Cancel', N'Employee', NULL, NULL, NULL, NULL, 1200000, N'Pending', NULL, NULL, NULL, N'2026-10-12T02:22:18.1687740', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (6, 55, N'ChangeDates', N'Customer', N'2026-12-01', N'2026-12-02', NULL, 0, NULL, N'Pending', NULL, NULL, NULL, N'2026-10-17T18:21:18.9218270', 0, 0, 240000, NULL, NULL, NULL, NULL, 800000, NULL, NULL, N'241fe00b-0d8e-4e8e-9133-9648407ccc6e');
    SET IDENTITY_INSERT [dbo].[BookingChangeRequests] OFF;
    DBCC CHECKIDENT (N'[dbo].[BookingChangeRequests]', RESEED, 1005) WITH NO_INFOMSGS;

    -- BookingNights: 153 dòng.
    INSERT INTO [dbo].[BookingNights] ([BookingId], [Date], [DayType], [Price]) VALUES
        (1, N'2026-10-10', N'Weekend', 1080000),
        (1, N'2026-10-11', N'Weekday', 900000),
        (1, N'2026-10-12', N'Weekday', 900000),
        (2, N'2026-10-11', N'Weekday', 1120000),
        (2, N'2026-10-12', N'Weekday', 1120000),
        (2, N'2026-10-13', N'Weekday', 1120000),
        (3, N'2026-10-20', N'Weekday', 320000),
        (4, N'2026-10-27', N'Weekday', 800000),
        (5, N'2026-10-23', N'Weekend', 4800000),
        (5, N'2026-10-24', N'Weekend', 4800000),
        (6, N'2026-10-31', N'Weekend', 960000),
        (7, N'2026-06-06', N'Weekend', 1920000),
        (7, N'2026-06-07', N'Weekday', 1600000),
        (8, N'2026-09-08', N'Weekday', 1440000),
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
        (18, N'2026-06-03', N'Weekday', 960000),
        (19, N'2026-08-17', N'Weekday', 800000),
        (19, N'2026-08-18', N'Weekday', 800000),
        (20, N'2026-09-02', N'Holiday', 750000),
        (20, N'2026-09-03', N'Weekday', 500000),
        (20, N'2026-09-04', N'Weekend', 600000),
        (21, N'2026-09-03', N'Weekday', 720000),
        (21, N'2026-09-04', N'Weekend', 860000),
        (21, N'2026-09-05', N'Weekend', 860000),
        (22, N'2026-07-29', N'Weekday', 1600000),
        (22, N'2026-07-30', N'Weekday', 1600000),
        (22, N'2026-07-31', N'Weekend', 1920000),
        (23, N'2026-08-10', N'Weekday', 800000),
        (23, N'2026-08-11', N'Weekday', 800000),
        (23, N'2026-08-12', N'Weekday', 800000),
        (24, N'2026-08-04', N'Weekday', 1280000),
        (24, N'2026-08-05', N'Weekday', 1280000),
        (25, N'2026-07-15', N'Weekday', 960000),
        (25, N'2026-07-16', N'Weekday', 960000),
        (25, N'2026-07-17', N'Weekend', 1150000),
        (26, N'2026-10-30', N'Weekend', 720000),
        (26, N'2026-10-31', N'Weekend', 720000),
        (27, N'2026-10-28', N'Weekday', 1600000),
        (28, N'2026-08-31', N'Weekday', 400000),
        (28, N'2026-09-01', N'Holiday', 600000),
        (29, N'2026-06-28', N'Weekday', 3600000),
        (30, N'2026-08-29', N'Weekend', 720000),
        (30, N'2026-08-30', N'Weekday', 600000),
        (30, N'2026-08-31', N'Weekday', 600000),
        (31, N'2026-06-22', N'Weekday', 1120000),
        (31, N'2026-06-23', N'Weekday', 1120000),
        (31, N'2026-06-24', N'Weekday', 1120000),
        (32, N'2026-12-27', N'Weekday', 800000),
        (32, N'2026-12-28', N'Weekday', 800000),
        (33, N'2026-07-27', N'Weekday', 800000),
        (33, N'2026-07-28', N'Weekday', 800000),
        (34, N'2026-11-22', N'Weekday', 500000),
        (34, N'2026-11-23', N'Weekday', 500000),
        (35, N'2026-07-17', N'Weekend', 960000),
        (36, N'2026-08-14', N'Weekend', 1150000),
        (36, N'2026-08-15', N'Weekend', 1150000),
        (37, N'2026-12-19', N'Weekend', 1150000),
        (38, N'2026-06-09', N'Weekday', 320000),
        (38, N'2026-06-10', N'Weekday', 320000),
        (38, N'2026-06-11', N'Weekday', 320000),
        (39, N'2026-09-17', N'Weekday', 600000),
        (40, N'2026-12-08', N'Weekday', 1600000),
        (41, N'2026-12-08', N'Weekday', 720000),
        (42, N'2026-11-28', N'Weekend', 600000),
        (42, N'2026-11-29', N'Weekday', 500000),
        (43, N'2026-07-25', N'Weekend', 1340000),
        (43, N'2026-07-26', N'Weekday', 1120000),
        (43, N'2026-07-27', N'Weekday', 1120000),
        (44, N'2026-12-13', N'Weekday', 900000),
        (44, N'2026-12-14', N'Weekday', 900000),
        (44, N'2026-12-15', N'Weekday', 900000),
        (45, N'2026-06-26', N'Weekend', 670000),
        (45, N'2026-06-27', N'Weekend', 670000),
        (46, N'2026-10-22', N'Weekday', 640000),
        (46, N'2026-10-23', N'Weekend', 770000),
        (47, N'2026-10-29', N'Weekday', 800000),
        (47, N'2026-10-30', N'Weekend', 960000),
        (47, N'2026-10-31', N'Weekend', 960000),
        (48, N'2026-09-24', N'Weekday', 600000),
        (48, N'2026-09-25', N'Weekend', 720000),
        (48, N'2026-09-26', N'Weekend', 720000),
        (49, N'2026-06-13', N'Weekend', 1540000),
        (49, N'2026-06-14', N'Weekday', 1280000),
        (50, N'2026-09-19', N'Weekend', 580000),
        (51, N'2026-10-03', N'Weekend', 380000);
    INSERT INTO [dbo].[BookingNights] ([BookingId], [Date], [DayType], [Price]) VALUES
        (52, N'2026-08-30', N'Weekday', 600000),
        (52, N'2026-08-31', N'Weekday', 600000),
        (52, N'2026-09-01', N'Holiday', 900000),
        (53, N'2026-08-16', N'Weekday', 640000),
        (54, N'2026-12-25', N'Weekend', 600000),
        (54, N'2026-12-26', N'Weekend', 600000),
        (55, N'2026-11-29', N'Weekday', 800000),
        (56, N'2026-09-06', N'Weekday', 1280000),
        (57, N'2026-07-17', N'Weekend', 3360000),
        (57, N'2026-07-18', N'Weekend', 3360000),
        (58, N'2026-11-11', N'Weekday', 4000000),
        (59, N'2026-06-12', N'Weekend', 720000),
        (59, N'2026-06-13', N'Weekend', 720000),
        (59, N'2026-06-14', N'Weekday', 600000),
        (60, N'2026-08-10', N'Weekday', 2400000),
        (60, N'2026-08-11', N'Weekday', 2400000),
        (1002, N'2026-10-12', N'Weekday', 900000),
        (1003, N'2026-12-08', N'Weekday', 400000),
        (1003, N'2026-12-09', N'Weekday', 400000),
        (1004, N'2026-10-12', N'Weekday', 900000),
        (1005, N'2026-10-15', N'Weekday', 900000),
        (1005, N'2026-10-16', N'Weekend', 1080000),
        (1005, N'2026-10-17', N'Weekend', 1080000),
        (1005, N'2026-10-18', N'Weekday', 900000),
        (1005, N'2026-10-19', N'Weekday', 900000),
        (2005, N'2026-10-16', N'Weekend', 1080000),
        (2006, N'2026-10-16', N'Weekend', 1200000),
        (2007, N'2026-10-15', N'Weekday', 700000),
        (2008, N'2026-11-03', N'Weekday', 900000),
        (2008, N'2026-11-04', N'Weekday', 900000),
        (3008, N'2026-10-18', N'Weekday', 900000),
        (3009, N'2026-11-23', N'Weekday', 900000),
        (3009, N'2026-11-24', N'Weekday', 900000),
        (3010, N'2026-11-30', N'Weekday', 900000),
        (3010, N'2026-12-01', N'Weekday', 900000),
        (3011, N'2026-12-07', N'Weekday', 900000),
        (3011, N'2026-12-08', N'Weekday', 900000),
        (3012, N'2026-12-14', N'Weekday', 900000),
        (3012, N'2026-12-15', N'Weekday', 900000),
        (3013, N'2026-12-15', N'Weekday', 900000),
        (3013, N'2026-12-16', N'Weekday', 900000),
        (3014, N'2026-10-19', N'Weekday', 1440000),
        (3015, N'2026-12-05', N'Weekend', 1080000),
        (3015, N'2026-12-06', N'Weekday', 905000),
        (3016, N'2026-12-10', N'Weekday', 905000),
        (3016, N'2026-12-11', N'Weekend', 1080000),
        (3017, N'2026-12-20', N'Weekday', 905000),
        (3018, N'2026-12-27', N'Weekday', 905000),
        (3018, N'2026-12-28', N'Weekday', 905000),
        (3019, N'2026-10-19', N'Weekday', 1440000),
        (3019, N'2026-10-20', N'Weekday', 1440000),
        (4008, N'2026-10-20', N'Weekday', 905000),
        (4008, N'2026-10-21', N'Weekday', 905000);

    -- BookingStatusHistories: 288 dòng.
    SET IDENTITY_INSERT [dbo].[BookingStatusHistories] ON;
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (1, 1, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-09-20T05:37:00'),
        (2, 1, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0001.', N'2026-09-20T05:43:00'),
        (3, 1, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-09-20T05:55:00'),
        (4, 1, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-10T09:02:00'),
        (5, 2, NULL, N'AwaitingPayment', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', NULL, N'2026-09-29T05:15:00'),
        (6, 2, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'Khách gửi ảnh bill.', N'2026-09-29T05:17:00'),
        (7, 2, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-09-29T07:36:00'),
        (8, 2, N'Deposited', N'Confirmed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-09-29T07:44:00'),
        (9, 2, N'Confirmed', N'CheckedIn', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-10-11T06:28:19.3084240'),
        (10, 3, NULL, N'AwaitingPayment', N'Customer', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', NULL, N'2026-10-11T06:16:19.3084240'),
        (11, 3, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0002.', N'2026-10-11T06:21:19.3084240'),
        (12, 4, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-11T05:38:19.3084240'),
        (13, 4, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0003.', N'2026-10-11T05:43:19.3084240'),
        (14, 5, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-10-11T06:03:19.3084240'),
        (15, 5, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'Khách gửi ảnh bill.', N'2026-10-11T06:08:19.3084240'),
        (16, 6, NULL, N'AwaitingPayment', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', NULL, N'2026-10-11T03:28:19.3084240'),
        (17, 6, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'Khách gửi ảnh bill.', N'2026-10-11T03:33:19.3084240'),
        (18, 7, NULL, N'AwaitingPayment', N'Customer', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', NULL, N'2026-06-01T05:25:00'),
        (19, 7, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0004.', N'2026-06-01T05:34:00'),
        (20, 7, N'Deposited', N'Rejected', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', N'Chủ nhà bận việc gia đình, không đón khách được.', N'2026-06-01T06:03:00'),
        (21, 8, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-08-18T01:21:00'),
        (22, 8, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0005.', N'2026-08-18T01:27:00'),
        (23, 8, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-18T01:50:00'),
        (24, 8, N'Confirmed', N'NoShow', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Khách không đến nhận phòng.', N'2026-09-08T16:00:00'),
        (25, 9, NULL, N'AwaitingPayment', N'Customer', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', NULL, N'2026-10-11T01:38:19.3084240'),
        (26, 9, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0006.', N'2026-10-11T01:43:19.3084240'),
        (27, 9, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-11T02:01:19.3084240'),
        (28, 10, NULL, N'AwaitingPayment', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', NULL, N'2026-08-04T01:48:00'),
        (29, 10, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0007.', N'2026-08-04T01:52:00'),
        (30, 10, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-08-04T02:20:00'),
        (31, 10, N'Confirmed', N'Cancelled', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'Khách hủy trước 4 ngày: Đặt nhầm ngày.', N'2026-08-26T21:01:15.0358745'),
        (32, 11, NULL, N'AwaitingPayment', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-05T04:55:00'),
        (33, 11, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0008.', N'2026-07-05T05:02:00'),
        (34, 11, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-07-05T05:21:00'),
        (35, 11, N'Confirmed', N'CheckedIn', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-07-24T08:22:00'),
        (36, 11, N'CheckedIn', N'Completed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-07-25T04:17:00'),
        (37, 12, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-07-13T03:30:00'),
        (38, 12, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0009.', N'2026-07-13T03:40:00'),
        (39, 12, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-13T04:00:00'),
        (40, 12, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-09T08:49:00'),
        (41, 12, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-10T04:58:00'),
        (42, 13, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-06-21T04:07:00'),
        (43, 13, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'Khách gửi ảnh bill.', N'2026-06-21T04:17:00'),
        (44, 13, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-06-21T05:16:00'),
        (45, 13, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-06-21T05:22:00'),
        (46, 13, N'Confirmed', N'CheckedIn', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-06-27T09:09:00'),
        (47, 13, N'CheckedIn', N'Completed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-06-28T04:53:00'),
        (48, 14, NULL, N'AwaitingPayment', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', NULL, N'2026-10-08T21:38:19.3084240'),
        (49, 14, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0010.', N'2026-10-08T21:40:19.3084240'),
        (50, 14, N'Deposited', N'Rejected', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', N'2026-10-08T21:56:19.3084240'),
        (51, 15, NULL, N'AwaitingPayment', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', NULL, N'2026-10-10T19:38:19.3084240'),
        (52, 15, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0011.', N'2026-10-10T19:40:19.3084240'),
        (53, 15, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-10T19:47:19.3084240'),
        (54, 16, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-06-01T04:37:00'),
        (55, 16, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0012.', N'2026-06-01T04:49:00'),
        (56, 16, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-01T04:55:00'),
        (57, 16, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-10T10:03:00'),
        (58, 16, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-13T04:40:00'),
        (59, 17, NULL, N'AwaitingPayment', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-11T02:38:19.3084240'),
        (60, 17, N'AwaitingPayment', N'Cancelled', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Khách tự hủy: Nhóm bạn không đủ người.', N'2026-10-11T02:43:19.3084240'),
        (61, 18, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-06-01T01:00:00'),
        (62, 18, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0013.', N'2026-06-01T01:06:00'),
        (63, 18, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-01T01:26:00'),
        (64, 18, N'Confirmed', N'CheckedIn', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-03T08:20:00'),
        (65, 18, N'CheckedIn', N'Completed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-04T04:38:00'),
        (66, 19, NULL, N'AwaitingPayment', N'Customer', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', NULL, N'2026-08-01T01:57:00'),
        (67, 19, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0014.', N'2026-08-01T02:09:00'),
        (68, 19, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-08-01T02:23:00'),
        (69, 19, N'Confirmed', N'CheckedIn', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-08-17T07:03:00'),
        (70, 19, N'CheckedIn', N'Completed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-08-19T04:45:00'),
        (71, 20, NULL, N'AwaitingPayment', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', NULL, N'2026-08-08T04:42:00'),
        (72, 20, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', N'Khách gửi ảnh bill.', N'2026-08-08T04:54:00'),
        (73, 20, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-08-08T06:01:00'),
        (74, 20, N'Deposited', N'Confirmed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-08-08T06:17:00'),
        (75, 20, N'Confirmed', N'CheckedIn', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-09-02T10:36:00'),
        (76, 20, N'CheckedIn', N'Completed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-09-05T04:08:00'),
        (77, 21, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-08-21T01:53:00'),
        (78, 21, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-08-21T02:09:00'),
        (79, 22, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-07-06T03:40:00'),
        (80, 22, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0016.', N'2026-07-06T03:43:00'),
        (81, 22, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-06T03:57:00'),
        (82, 22, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-29T07:18:00'),
        (83, 22, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-01T04:23:00'),
        (84, 23, NULL, N'AwaitingPayment', N'Customer', N'3e33295d-a909-4dfb-b209-cdfe617661a2', NULL, N'2026-07-24T02:49:00'),
        (85, 23, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0017.', N'2026-07-24T02:54:00'),
        (86, 23, N'Deposited', N'Confirmed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-07-24T03:09:00'),
        (87, 23, N'Confirmed', N'Cancelled', N'Customer', N'3e33295d-a909-4dfb-b209-cdfe617661a2', N'Khách hủy trước 17 ngày: Đặt nhầm ngày.', N'2026-07-24T09:40:14.1792389'),
        (88, 24, NULL, N'AwaitingPayment', N'Customer', N'f017ffe1-ef1a-45fd-89ec-072e14350174', NULL, N'2026-07-18T05:49:00'),
        (89, 24, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0018.', N'2026-07-18T05:59:00'),
        (90, 24, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-07-18T06:27:00'),
        (91, 24, N'Confirmed', N'CheckedIn', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-08-04T09:42:00'),
        (92, 24, N'CheckedIn', N'Completed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-08-06T04:03:00'),
        (93, 25, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-06-25T02:07:00'),
        (94, 25, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0019.', N'2026-06-25T02:13:00'),
        (95, 25, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-06-25T02:34:00'),
        (96, 25, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-15T07:15:00'),
        (97, 25, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-18T04:36:00'),
        (98, 26, NULL, N'AwaitingPayment', N'Customer', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', NULL, N'2026-10-08T23:38:19.3084240'),
        (99, 26, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0020.', N'2026-10-08T23:47:19.3084240'),
        (100, 26, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-10-09T00:01:19.3084240');
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (101, 26, N'Confirmed', N'Cancelled', N'Customer', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'Khách hủy trước 20 ngày: Đặt nhầm ngày.', N'2026-10-10T10:49:26.7675156'),
        (102, 27, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-09T21:38:19.3084240'),
        (103, 27, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0021.', N'2026-10-09T21:48:19.3084240'),
        (104, 27, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-09T22:05:19.3084240'),
        (105, 28, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-08-16T05:42:00'),
        (106, 28, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0022.', N'2026-08-16T05:45:00'),
        (107, 28, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-08-16T05:57:00'),
        (108, 28, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-08-31T07:03:00'),
        (109, 28, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-02T04:34:00'),
        (110, 29, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-06-09T03:47:00'),
        (111, 29, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0023.', N'2026-06-09T03:52:00'),
        (112, 29, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-09T04:18:00'),
        (113, 29, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-28T08:25:00'),
        (114, 29, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-06-29T04:34:00'),
        (115, 30, NULL, N'AwaitingPayment', N'Customer', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', NULL, N'2026-07-30T03:05:00'),
        (116, 30, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0024.', N'2026-07-30T03:17:00'),
        (117, 30, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-07-30T03:29:00'),
        (118, 30, N'Confirmed', N'CheckedIn', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-08-29T08:45:00'),
        (119, 30, N'CheckedIn', N'Completed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-09-01T04:05:00'),
        (120, 31, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-06-05T01:10:00'),
        (121, 31, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-06-05T01:26:00'),
        (122, 32, NULL, N'AwaitingPayment', N'Customer', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', NULL, N'2026-10-10T21:38:19.3084240'),
        (123, 32, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0026.', N'2026-10-10T21:46:19.3084240'),
        (124, 32, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-10T22:11:19.3084240'),
        (125, 33, NULL, N'AwaitingPayment', N'Customer', N'f017ffe1-ef1a-45fd-89ec-072e14350174', NULL, N'2026-07-04T03:50:00'),
        (126, 33, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0027.', N'2026-07-04T03:56:00'),
        (127, 33, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-04T04:08:00'),
        (128, 33, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-27T09:02:00'),
        (129, 33, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-07-29T04:35:00'),
        (130, 34, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-09T18:38:19.3084240'),
        (131, 34, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0028.', N'2026-10-09T18:42:19.3084240'),
        (132, 34, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-09T19:03:19.3084240'),
        (133, 35, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-07-15T05:50:00'),
        (134, 35, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0029.', N'2026-07-15T06:02:00'),
        (135, 35, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-15T06:19:00'),
        (136, 35, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-17T10:39:00'),
        (137, 35, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-18T04:50:00'),
        (138, 36, NULL, N'AwaitingPayment', N'Customer', N'38d5efb7-61dd-45f1-83fc-436930a66583', NULL, N'2026-07-17T01:02:00'),
        (139, 36, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0030.', N'2026-07-17T01:12:00'),
        (140, 36, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-07-17T01:22:00'),
        (141, 36, N'Confirmed', N'CheckedIn', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-08-14T09:55:00'),
        (142, 36, N'CheckedIn', N'Completed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-08-16T04:12:00'),
        (143, 37, NULL, N'AwaitingPayment', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', NULL, N'2026-10-08T16:38:19.3084240'),
        (144, 37, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-08T16:54:19.3084240'),
        (145, 38, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-06-01T05:10:00'),
        (146, 38, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-06-01T05:16:00'),
        (147, 38, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-06-01T07:18:00'),
        (148, 38, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-01T07:33:00'),
        (149, 38, N'Confirmed', N'CheckedIn', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-09T07:20:00'),
        (150, 38, N'CheckedIn', N'Completed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-06-12T04:09:00'),
        (151, 39, NULL, N'AwaitingPayment', N'Customer', N'3e33295d-a909-4dfb-b209-cdfe617661a2', NULL, N'2026-09-02T03:10:00'),
        (152, 39, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0031.', N'2026-09-02T03:22:00'),
        (153, 39, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-09-02T03:38:00'),
        (154, 39, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-09-17T09:06:00'),
        (155, 39, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-09-18T04:49:00'),
        (156, 40, NULL, N'AwaitingPayment', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-11T01:38:19.3084240'),
        (157, 40, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0032.', N'2026-10-11T01:47:19.3084240'),
        (158, 40, N'Deposited', N'Confirmed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-10-11T02:00:19.3084240'),
        (159, 41, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-08T12:38:19.3084240'),
        (160, 41, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0033.', N'2026-10-08T12:49:19.3084240'),
        (161, 41, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-10-08T12:57:19.3084240'),
        (162, 42, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-09T06:38:19.3084240'),
        (163, 42, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0034.', N'2026-10-09T06:48:19.3084240'),
        (164, 42, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-10-09T07:12:19.3084240'),
        (165, 42, N'Confirmed', N'Cancelled', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Khách hủy trước 49 ngày: Chuyển sang đi tỉnh khác.', N'2026-10-09T17:31:49.8131972'),
        (166, 43, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-06-29T02:52:00'),
        (167, 43, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-06-29T02:56:00'),
        (168, 43, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-06-29T05:39:00'),
        (169, 43, N'Deposited', N'Confirmed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-06-29T05:53:00'),
        (170, 43, N'Confirmed', N'CheckedIn', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-07-25T09:47:00'),
        (171, 43, N'CheckedIn', N'Completed', N'Owner', N'b4623385-f904-483d-9040-fbb25af3e7f1', NULL, N'2026-07-28T04:53:00'),
        (172, 44, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-10T03:38:19.3084240'),
        (173, 44, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0035.', N'2026-10-10T03:50:19.3084240'),
        (174, 44, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-10T04:06:19.3084240'),
        (175, 44, N'Confirmed', N'Cancelled', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', N'Khách hủy trước 63 ngày: Nhóm bạn không đủ người.', N'2026-10-10T19:24:49.5866868'),
        (176, 45, NULL, N'AwaitingPayment', N'Customer', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', NULL, N'2026-06-01T02:55:00'),
        (177, 45, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0036.', N'2026-06-01T03:01:00'),
        (178, 45, N'Deposited', N'Rejected', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', N'Phòng đang sửa chữa đột xuất (hỏng điều hòa).', N'2026-06-01T03:11:00'),
        (179, 46, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-10T01:20:00'),
        (180, 46, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0037.', N'2026-10-10T01:22:00'),
        (181, 46, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-10T01:32:00'),
        (182, 47, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-09T12:38:19.3084240'),
        (183, 47, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-09T12:54:19.3084240'),
        (184, 48, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-09-13T04:47:00'),
        (185, 48, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0038.', N'2026-09-13T04:50:00'),
        (186, 48, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-13T05:07:00'),
        (187, 48, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-24T09:02:00'),
        (188, 48, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-27T04:04:00'),
        (189, 49, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-06-01T05:49:00'),
        (190, 49, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-06-01T06:05:00'),
        (191, 50, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-09-10T02:13:00'),
        (192, 50, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0039.', N'2026-09-10T02:20:00'),
        (193, 50, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-10T02:41:00'),
        (194, 50, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-19T08:02:00'),
        (195, 50, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-20T04:10:00'),
        (196, 51, NULL, N'AwaitingPayment', N'Customer', N'3e33295d-a909-4dfb-b209-cdfe617661a2', NULL, N'2026-10-01T04:48:00'),
        (197, 51, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0040.', N'2026-10-01T04:53:00'),
        (198, 51, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-01T05:11:00'),
        (199, 51, N'Confirmed', N'CheckedIn', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-03T07:50:00'),
        (200, 51, N'CheckedIn', N'Completed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-04T04:35:00');
    INSERT INTO [dbo].[BookingStatusHistories] ([Id], [BookingId], [FromStatus], [ToStatus], [ActorType], [ActorAccountId], [Note], [ChangedAt]) VALUES
        (201, 52, NULL, N'AwaitingPayment', N'Customer', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', NULL, N'2026-08-17T01:34:00'),
        (202, 52, N'AwaitingPayment', N'Cancelled', N'Customer', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', N'Khách tự hủy: Gia đình có việc đột xuất.', N'2026-08-17T01:42:00'),
        (203, 53, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-07-29T02:29:00'),
        (204, 53, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0041.', N'2026-07-29T02:39:00'),
        (205, 53, N'Deposited', N'Rejected', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Chủ nhà bận việc gia đình, không đón khách được.', N'2026-07-29T03:12:00'),
        (206, 54, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-10-10T07:38:19.3084240'),
        (207, 54, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-10T07:54:19.3084240'),
        (208, 55, NULL, N'AwaitingPayment', N'Employee', N'3df96a6a-8196-4777-9712-b6e9f5ed34cd', NULL, N'2026-10-10T22:38:19.3084240'),
        (209, 55, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0042.', N'2026-10-10T22:46:19.3084240'),
        (210, 55, N'Deposited', N'Confirmed', N'Owner', N'abdc6396-ee31-41cf-b74e-ee58209136b8', NULL, N'2026-10-10T23:04:19.3084240'),
        (211, 56, NULL, N'AwaitingPayment', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-07T05:05:00'),
        (212, 56, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'Khách gửi ảnh bill.', N'2026-08-07T05:14:00'),
        (213, 56, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-08-07T07:13:00'),
        (214, 56, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-08-07T07:40:00'),
        (215, 56, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-06T09:30:00'),
        (216, 56, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-09-07T04:44:00'),
        (217, 57, NULL, N'AwaitingPayment', N'Customer', N'b2ce2e23-513a-417f-a972-5251641e4e8f', NULL, N'2026-07-15T02:55:00'),
        (218, 57, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0043.', N'2026-07-15T03:07:00'),
        (219, 57, N'Deposited', N'Confirmed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-15T03:27:00'),
        (220, 57, N'Confirmed', N'CheckedIn', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-17T10:59:00'),
        (221, 57, N'CheckedIn', N'Completed', N'Owner', N'a758bd81-b043-4dc1-a347-a469c8546bf1', NULL, N'2026-07-19T04:00:00'),
        (222, 58, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-10-09T21:38:19.3084240'),
        (223, 58, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0044.', N'2026-10-09T21:49:19.3084240'),
        (224, 58, N'Deposited', N'Confirmed', N'Owner', N'6940757c-e23d-4c9d-a8e9-8cb911f650b5', NULL, N'2026-10-09T21:56:19.3084240'),
        (225, 59, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-06-07T03:48:00'),
        (226, 59, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', N'Khách gửi ảnh bill.', N'2026-06-07T03:57:00'),
        (227, 59, N'PaymentSubmitted', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán duyệt ảnh bill.', N'2026-06-07T05:32:00'),
        (228, 59, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-06-07T05:50:00'),
        (229, 59, N'Confirmed', N'Cancelled', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', N'Khách hủy trước 5 ngày: Nhóm bạn không đủ người.', N'2026-06-07T06:07:27.4097985'),
        (230, 60, NULL, N'AwaitingPayment', N'Customer', N'fa2d35bb-cc18-4145-9aac-8cf9a3997d23', NULL, N'2026-08-09T04:20:00'),
        (231, 60, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SEED-0045.', N'2026-08-09T04:23:00'),
        (232, 60, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-09T04:28:00'),
        (233, 60, N'Confirmed', N'CheckedIn', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-10T10:54:00'),
        (234, 60, N'CheckedIn', N'Completed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-08-12T04:13:00'),
        (1002, 1002, NULL, N'AwaitingPayment', N'Customer', N'3027af7f-546b-49bc-8598-af0d9a0034e2', NULL, N'2026-10-12T06:38:54.4090760'),
        (1003, 1002, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'Khách gửi ảnh bill.', N'2026-10-12T06:40:57.6791440'),
        (1004, 1002, N'PaymentSubmitted', N'Deposited', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kế toán duyệt ảnh bill.', N'2026-10-12T06:43:29.4961970'),
        (1005, 1003, NULL, N'AwaitingPayment', N'Customer', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-12T08:12:38.3018470'),
        (1006, 1003, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-12T08:27:39.8517500'),
        (1007, 1004, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-12T08:43:11.8493900'),
        (1008, 1004, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Khách gửi ảnh bill.', N'2026-10-12T08:49:39.4398490'),
        (1009, 1004, N'PaymentSubmitted', N'Deposited', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kế toán duyệt ảnh bill.', N'2026-10-12T08:50:41.7864170'),
        (1010, 1005, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-13T08:45:25.2441720'),
        (1011, 1005, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-13T09:01:10.1374710'),
        (2010, 2005, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-14T18:42:51.5509430'),
        (2011, 2005, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-14T18:58:17.8616950'),
        (2012, 2006, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-15T05:50:17.2051610'),
        (2013, 2006, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Khách gửi ảnh bill.', N'2026-10-15T05:50:42.8281500'),
        (2014, 2007, NULL, N'AwaitingPayment', N'Customer', N'18b241d3-6441-42f5-8bde-d39acb462728', NULL, N'2026-10-15T08:24:05.5437350'),
        (2015, 2007, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-15T08:39:28.2058970'),
        (2016, 2008, NULL, N'AwaitingPayment', N'Customer', NULL, NULL, N'2026-10-17T17:43:06.4411050'),
        (2017, 2008, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-17T17:58:42.4077070'),
        (3016, 3008, NULL, N'AwaitingPayment', N'Customer', NULL, NULL, N'2026-10-18T14:08:48.4177850'),
        (3017, 3008, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', NULL, N'Khách gửi ảnh bill.', N'2026-10-18T14:09:11.0850330'),
        (3018, 3008, N'PaymentSubmitted', N'Deposited', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kế toán duyệt ảnh bill.', N'2026-10-18T14:11:41.7048330'),
        (3019, 1002, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-18T14:12:06.7208870'),
        (3020, 3009, NULL, N'AwaitingPayment', N'Customer', NULL, NULL, N'2026-10-18T15:50:24.0693020'),
        (3021, 3010, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-18T15:53:53.8828150'),
        (3022, 3009, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-18T16:05:47.4247760'),
        (3023, 3010, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-18T16:08:56.9890200'),
        (3024, 3011, NULL, N'AwaitingPayment', N'Customer', NULL, NULL, N'2026-10-18T16:50:02.4506700'),
        (3025, 3012, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-18T16:50:13.0509500'),
        (3026, 3011, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-18T17:06:17.0276480'),
        (3027, 3012, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-18T17:06:17.3178910'),
        (3028, 3013, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-19T02:00:57.3134650'),
        (3029, 3013, N'AwaitingPayment', N'Deposited', N'Employee', N'93317b46-82fc-486f-993a-d8d786f63b89', N'Kế toán khớp tay giao dịch SIM-bdbe4caf3bb24f55b7ad074d4ab0bbc0.', N'2026-10-19T02:01:54.5502580'),
        (3030, 3013, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-19T02:01:56.6198080'),
        (3031, 3014, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-19T03:00:19.3186420'),
        (3032, 3014, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-19T06:56:24.4807020'),
        (3033, 3015, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-10-19T07:26:02.6853570'),
        (3034, 3015, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SIM-b451f3f23f30495793f5285517839722.', N'2026-10-19T07:31:33.5061180'),
        (3035, 3015, N'Deposited', N'Confirmed', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kiểm thử 05b: xác nhận đơn thử trước khi diễn tập chuyển phòng.', N'2026-10-19T07:31:35.0057400'),
        (3036, 3015, N'Confirmed', N'Confirmed', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đổi phòng do trùng phòng; Đổi phòng 101 → 102; Kiểm thử 05b: khách đồng ý chuyển sang phòng trống cùng loại, không đổi giá/cọc.', N'2026-10-19T07:32:34.2103150'),
        (3037, 3016, NULL, N'AwaitingPayment', N'Employee', N'ba6bf082-50aa-4026-befe-c64773f49df3', NULL, N'2026-10-19T08:33:41.9903700'),
        (3038, 3015, N'Confirmed', N'Confirmed', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Đổi phòng do trùng phòng; Đổi phòng 102 → 101; Kiểm thử 05b tiếp tục: khách đồng ý chuyển lại phòng trống cùng loại; giữ nguyên giá và cọc.', N'2026-10-19T08:36:19.9862940'),
        (3039, 3016, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-19T08:49:19.2078730'),
        (3040, 3017, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-19T14:14:16.8569200'),
        (3041, 3017, N'AwaitingPayment', N'Deposited', N'System', NULL, N'Tự khớp giao dịch SIM-7ac22a09d28f49a8a29907fe41195b47.', N'2026-10-19T14:14:46.0080920'),
        (3042, 3018, NULL, N'AwaitingPayment', N'Customer', NULL, NULL, N'2026-10-19T15:05:15.5981980'),
        (3043, 3018, N'AwaitingPayment', N'Expired', N'System', NULL, N'Quá hạn giữ phòng chờ thanh toán.', N'2026-10-19T15:40:22.2335870'),
        (3044, 3008, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-19T16:53:35.5451720'),
        (3045, 3019, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-19T16:54:56.5997020'),
        (3046, 3019, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Khách gửi ảnh bill.', N'2026-10-19T16:55:31.5086720'),
        (3047, 3019, N'PaymentSubmitted', N'Deposited', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kế toán duyệt ảnh bill.', N'2026-10-19T16:57:04.5238510'),
        (4016, 4008, NULL, N'AwaitingPayment', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', NULL, N'2026-10-19T19:11:03.0259660'),
        (4017, 4008, N'AwaitingPayment', N'PaymentSubmitted', N'Customer', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'Khách gửi ảnh bill.', N'2026-10-19T19:11:39.3282090'),
        (4018, 4008, N'PaymentSubmitted', N'Deposited', N'Employee', N'c30f83a0-0b47-47eb-b167-6910ab47dd9b', N'Kế toán duyệt ảnh bill.', N'2026-10-19T19:13:50.8100110'),
        (4019, 4008, N'Deposited', N'Confirmed', N'Owner', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', NULL, N'2026-10-19T19:15:14.2673750');
    SET IDENTITY_INSERT [dbo].[BookingStatusHistories] OFF;
    DBCC CHECKIDENT (N'[dbo].[BookingStatusHistories]', RESEED, 4019) WITH NO_INFOMSGS;

    -- MessageLogs: 3 dòng.
    SET IDENTITY_INSERT [dbo].[MessageLogs] ON;
    INSERT INTO [dbo].[MessageLogs] ([Id], [Channel], [RecipientPhone], [RecipientEmail], [TemplateCode], [BookingId], [Status], [ErrorMessage], [SentAt], [CreatedAt]) VALUES
        (1, N'Zalo', N'+84987000001', NULL, N'CheckinReminder', 4008, N'Sent', NULL, N'2026-10-20T02:46:04.2156900', N'2026-10-20T02:46:03.8513510'),
        (2, N'Zalo', N'+84912000001', NULL, N'CheckinReminder', 46, N'Sent', NULL, N'2026-10-21T02:00:41.0653610', N'2026-10-21T02:00:41.0370210'),
        (3, N'Zalo', N'+84912000005', NULL, N'OwnerArrivals', NULL, N'Sent', NULL, N'2026-10-21T02:00:41.2067420', N'2026-10-21T02:00:41.1931390');
    SET IDENTITY_INSERT [dbo].[MessageLogs] OFF;
    DBCC CHECKIDENT (N'[dbo].[MessageLogs]', RESEED, 3) WITH NO_INFOMSGS;

    -- Reviews: 20 dòng.
    SET IDENTITY_INSERT [dbo].[Reviews] ON;
    INSERT INTO [dbo].[Reviews] ([Id], [BookingId], [HomestayId], [Rating], [Comment], [OwnerReply], [RepliedAt], [IsHidden], [CreatedAt]) VALUES
        (1, 60, 31, 3, N'Tạm ổn, dọn phòng chưa kỹ lắm khi nhận phòng.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-14T22:13:00', 0, N'2026-08-13T23:13:00'),
        (2, 57, 32, 4, N'Phòng ổn, sạch, chỉ có wifi hơi chập chờn buổi tối.', NULL, NULL, 0, N'2026-07-19T21:00:00'),
        (3, 43, 4, 4, N'Phòng đẹp, tuy nhiên nước nóng hơi yếu. Chủ nhà hỗ trợ nhanh.', N'Rất vui vì gia đình bạn có kỳ nghỉ vui vẻ!', N'2026-07-31T08:53:00', 0, N'2026-07-30T21:53:00'),
        (4, 38, 15, 5, N'Không gian đẹp y như ảnh, giường êm, check-in nhanh gọn.', NULL, NULL, 0, N'2026-06-15T02:09:00'),
        (5, 36, 10, 3, N'Giá cuối tuần hơi cao, tiện nghi ở mức cơ bản.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-17T09:12:00', 0, N'2026-08-17T05:12:00'),
        (6, 35, 7, 5, N'Nhóm mình tổ chức sinh nhật ở đây, khu BBQ rộng và đầy đủ dụng cụ.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-07-20T19:50:00', 0, N'2026-07-19T23:50:00'),
        (7, 33, 16, 4, N'Đáng tiền, chỉ mong có thêm ấm siêu tốc trong phòng.', N'Cảm ơn bạn đã chia sẻ trải nghiệm. Fuji đã ghi nhận góp ý để chuẩn bị phòng chu đáo hơn cho lần lưu trú tới.', N'2026-10-19T07:00:55.8400820', 0, N'2026-07-29T13:35:00'),
        (8, 30, 3, 5, N'Lần thứ hai ở đây, vẫn tuyệt vời như lần trước.', NULL, NULL, 0, N'2026-09-02T02:05:00'),
        (9, 29, 27, 5, N'Đặt cọc bằng QR nhanh, vài phút đã có xác nhận. Trải nghiệm rất tốt.', NULL, NULL, 0, N'2026-07-02T01:34:00'),
        (10, 28, 22, 4, N'Trẻ con rất thích sân vườn. Sẽ giới thiệu cho bạn bè.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-09-03T02:34:00', 0, N'2026-09-02T14:34:00'),
        (11, 25, 31, 5, N'View đẹp, sáng ra uống cà phê ngoài ban công rất chill.', NULL, NULL, 0, N'2026-07-20T05:36:00'),
        (12, 24, 5, 4, N'Nhân viên Fuji tư vấn nhiệt tình qua điện thoại, phòng đúng như mô tả.', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-08-08T02:03:00', 0, N'2026-08-07T02:03:00'),
        (13, 22, 21, 4, N'Không khí trong lành, đường vào hơi khó tìm lúc trời tối.', NULL, NULL, 0, N'2026-08-03T23:23:00'),
        (14, 20, 9, 1, N'Inbox zalo 09xx để nhận mã giảm giá homestay khác rẻ hơn!!!', NULL, NULL, 1, N'2026-09-06T16:08:00'),
        (15, 19, 33, 2, N'Điều hòa kêu to, phải đổi phòng giữa đêm. Chủ nhà có xin lỗi.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-21T21:45:00', 0, N'2026-08-21T09:45:00'),
        (16, 18, 10, 5, N'Phòng sạch sẽ, chủ nhà nhiệt tình, vị trí rất thuận tiện đi lại. Sẽ quay lại!', N'Cảm ơn bạn đã lựa chọn homestay, hẹn gặp lại bạn lần sau!', N'2026-06-06T02:38:00', 0, N'2026-06-05T15:38:00'),
        (17, 16, 12, 5, N'Cả nhà đều thích, trẻ con được chơi thoải mái. Giá hợp lý so với chất lượng.', NULL, NULL, 0, N'2026-06-14T20:40:00'),
        (18, 13, 8, 5, N'Chủ nhà chỉ quán ăn ngon quanh khu, buổi tối rất yên tĩnh.', N'Rất vui vì gia đình bạn có kỳ nghỉ vui vẻ!', N'2026-07-01T15:53:00', 0, N'2026-07-01T04:53:00'),
        (19, 12, 6, 3, N'Phòng hơi nhỏ so với ảnh, cách âm chưa tốt.', N'Cảm ơn góp ý của bạn, nhà mình đã khắc phục ngay.', N'2026-08-11T20:58:00', 0, N'2026-08-11T09:58:00'),
        (20, 11, 8, 4, N'Vị trí tốt nhưng chỗ để xe hơi chật. Nhìn chung hài lòng.', NULL, NULL, 0, N'2026-07-27T06:17:00');
    SET IDENTITY_INSERT [dbo].[Reviews] OFF;
    DBCC CHECKIDENT (N'[dbo].[Reviews]', RESEED, 1001) WITH NO_INFOMSGS;

    -- SettlementItems: 28 dòng.
    SET IDENTITY_INSERT [dbo].[SettlementItems] ON;
    INSERT INTO [dbo].[SettlementItems] ([Id], [SettlementId], [BookingId], [ItemType], [RoomAmount], [DepositAmount], [RefundAmount], [RetainedAmount], [CommissionRate], [CommissionAmount], [PayoutAmount], [DiscountAmount]) VALUES
        (28, 27, 18, N'Completed', 960000, 288000, 0, 288000, 0.1000, 96000, 192000, 0),
        (29, 28, 16, N'Completed', 2560000, 768000, 0, 768000, 0.1500, 384000, 384000, 0),
        (30, 29, 38, N'Completed', 960000, 288000, 0, 288000, 0.1500, 144000, 144000, 0),
        (31, 30, 59, N'CancelledRetained', 2040000, 612000, 306000, 306000, 0.1000, 204000, 102000, 0),
        (32, 31, 13, N'Completed', 480000, 144000, 0, 144000, 0.1200, 57600, 86400, 0),
        (33, 32, 29, N'Completed', 3600000, 1080000, 0, 1080000, 0.1500, 540000, 540000, 0),
        (34, 33, 43, N'Completed', 3580000, 1074000, 0, 1074000, 0.1000, 358000, 716000, 0),
        (35, 34, 35, N'Completed', 960000, 288000, 0, 288000, 0.1000, 96000, 192000, 0),
        (36, 35, 11, N'Completed', 770000, 231000, 0, 231000, 0.1200, 92400, 138600, 0),
        (37, 36, 33, N'Completed', 1600000, 480000, 0, 480000, 0.1000, 160000, 320000, 0),
        (38, 37, 25, N'Completed', 3070000, 921000, 0, 921000, 0.1000, 307000, 614000, 0),
        (39, 38, 57, N'Completed', 6720000, 2016000, 0, 2016000, 0.1200, 806400, 1209600, 0),
        (40, 39, 24, N'Completed', 2560000, 768000, 0, 768000, 0.1200, 307200, 460800, 0),
        (41, 40, 12, N'Completed', 1440000, 432000, 0, 432000, 0.1500, 216000, 216000, 0),
        (42, 41, 22, N'Completed', 5120000, 1536000, 0, 1536000, 0.1500, 768000, 768000, 0),
        (43, 42, 60, N'Completed', 4800000, 1440000, 0, 1440000, 0.1000, 480000, 960000, 0),
        (44, 43, 36, N'Completed', 2300000, 690000, 0, 690000, 0.1000, 230000, 460000, 0),
        (45, 44, 19, N'Completed', 1600000, 480000, 0, 480000, 0.1500, 240000, 240000, 0),
        (46, 45, 30, N'Completed', 1920000, 576000, 0, 576000, 0.1500, 288000, 288000, 0),
        (47, 46, 8, N'NoShow', 1440000, 432000, 0, 432000, 0.1500, 216000, 216000, 0),
        (48, 47, 20, N'Completed', 1850000, 555000, 0, 555000, 0.1500, 277500, 277500, 0),
        (49, 48, 56, N'Completed', 1280000, 384000, 0, 384000, 0.1500, 192000, 192000, 0),
        (50, 49, 28, N'Completed', 1000000, 300000, 0, 300000, 0.1000, 100000, 200000, 0),
        (51, 50, 10, N'CancelledRetained', 8000000, 2400000, 0, 2400000, 0.1500, 1200000, 1200000, 0),
        (52, 51, 48, N'Completed', 2040000, 612000, 0, 612000, 0.1200, 244800, 367200, 0),
        (53, 51, 50, N'Completed', 580000, 174000, 0, 174000, 0.1200, 69600, 104400, 0),
        (54, 52, 39, N'Completed', 600000, 180000, 0, 180000, 0.1000, 60000, 120000, 0),
        (55, 53, 51, N'Completed', 380000, 114000, 0, 114000, 0.1500, 57000, 57000, 0);
    SET IDENTITY_INSERT [dbo].[SettlementItems] OFF;
    DBCC CHECKIDENT (N'[dbo].[SettlementItems]', RESEED, 1054) WITH NO_INFOMSGS;

    -- Payments: 66 dòng.
    SET IDENTITY_INSERT [dbo].[Payments] ON;
    INSERT INTO [dbo].[Payments] ([Id], [BookingId], [Type], [Method], [BankTransactionId], [Amount], [ReceiptImageUrl], [TransferContent], [BankRef], [Status], [SubmittedByAccountId], [SubmittedAt], [ReviewedByEmployeeId], [ReviewedAt], [RejectReason], [ReviewAlertSentAt], [ChangeRequestId]) VALUES
        (1, 2, N'Deposit', N'ManualReceipt', NULL, 1008000, N'sample-receipt.png', N'FJ260929-001', NULL, N'Approved', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'2026-09-29T05:17:00', 3, N'2026-09-29T07:36:00', NULL, NULL, NULL),
        (2, 5, N'Deposit', N'ManualReceipt', NULL, 2880000, N'sample-receipt.png', N'FJ261011-003', NULL, N'Submitted', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'2026-10-11T06:08:19.3084240', NULL, NULL, NULL, N'2026-10-12T03:22:19.8940300', NULL),
        (3, 6, N'Deposit', N'ManualReceipt', NULL, 288000, N'sample-receipt.png', N'FJ261011-004', NULL, N'Submitted', N'dd940858-5f5e-4cf9-b2b8-41c711d60f07', N'2026-10-11T03:33:19.3084240', NULL, NULL, NULL, N'2026-10-12T03:22:19.8940300', NULL),
        (4, 7, N'Refund', N'ManualTransfer', NULL, 1056000, NULL, N'Fuji hoan coc FJ260601-001', NULL, N'Approved', N'93317b46-82fc-486f-993a-d8d786f63b89', N'2026-06-03T06:03:00', 3, N'2026-06-03T06:03:00', NULL, NULL, NULL),
        (5, 13, N'Deposit', N'ManualReceipt', NULL, 144000, N'sample-receipt.png', N'FJ260621-001', NULL, N'Approved', N'b96a201f-88c5-4fa4-9aae-4111d62e5427', N'2026-06-21T04:17:00', 3, N'2026-06-21T05:16:00', NULL, NULL, NULL),
        (6, 20, N'Deposit', N'ManualReceipt', NULL, 555000, N'sample-receipt.png', N'FJ260808-001', NULL, N'Approved', N'b2ce2e23-513a-417f-a972-5251641e4e8f', N'2026-08-08T04:54:00', 3, N'2026-08-08T06:01:00', NULL, NULL, NULL),
        (7, 23, N'Refund', N'ManualTransfer', NULL, 720000, NULL, N'Fuji hoan coc FJ260724-001', NULL, N'Approved', N'93317b46-82fc-486f-993a-d8d786f63b89', N'2026-07-26T09:40:14.1792389', 3, N'2026-07-26T09:40:14.1792389', NULL, NULL, NULL),
        (8, 38, N'Deposit', N'ManualReceipt', NULL, 288000, N'sample-receipt.png', N'FJ260601-004', NULL, N'Approved', NULL, N'2026-06-01T05:16:00', 3, N'2026-06-01T07:18:00', NULL, NULL, NULL),
        (9, 43, N'Deposit', N'ManualReceipt', NULL, 1074000, N'sample-receipt.png', N'FJ260629-001', NULL, N'Approved', NULL, N'2026-06-29T02:56:00', 3, N'2026-06-29T05:39:00', NULL, NULL, NULL),
        (10, 45, N'Refund', N'ManualTransfer', NULL, 402000, NULL, N'Fuji hoan coc FJ260601-005', NULL, N'Approved', N'93317b46-82fc-486f-993a-d8d786f63b89', N'2026-06-03T03:11:00', 3, N'2026-06-03T03:11:00', NULL, NULL, NULL),
        (11, 53, N'Refund', N'ManualTransfer', NULL, 192000, NULL, N'Fuji hoan coc FJ260729-001', NULL, N'Approved', N'93317b46-82fc-486f-993a-d8d786f63b89', N'2026-07-31T03:12:00', 3, N'2026-07-31T03:12:00', NULL, NULL, NULL),
        (12, 56, N'Deposit', N'ManualReceipt', NULL, 384000, N'sample-receipt.png', N'FJ260807-001', NULL, N'Approved', N'11248390-0317-4fe5-b11d-ffd0f950f0e1', N'2026-08-07T05:14:00', 3, N'2026-08-07T07:13:00', NULL, NULL, NULL),
        (13, 59, N'Deposit', N'ManualReceipt', NULL, 612000, N'sample-receipt.png', N'FJ260607-001', NULL, N'Approved', N'44d9fdcb-84b2-4b36-99d4-ca22abbcdda8', N'2026-06-07T03:57:00', 3, N'2026-06-07T05:32:00', NULL, NULL, NULL),
        (14, 59, N'Refund', N'ManualTransfer', NULL, 306000, NULL, N'Fuji hoan coc FJ260607-001', NULL, N'Approved', N'93317b46-82fc-486f-993a-d8d786f63b89', N'2026-06-09T06:07:27.4097985', 3, N'2026-06-09T06:07:27.4097985', NULL, NULL, NULL),
        (15, 1, N'Deposit', N'BankAutoMatch', 2, 864000, NULL, N'FJ260920-001 dat coc', NULL, N'Approved', NULL, N'2026-09-20T05:43:00', NULL, N'2026-09-20T05:43:00', NULL, NULL, NULL),
        (16, 3, N'Deposit', N'BankAutoMatch', 3, 96000, NULL, N'FJ261011-001 dat coc', NULL, N'Approved', NULL, N'2026-10-11T06:21:19.3084240', NULL, N'2026-10-11T06:21:19.3084240', NULL, NULL, NULL),
        (17, 4, N'Deposit', N'BankAutoMatch', 4, 240000, NULL, N'FJ261011-002 dat coc', NULL, N'Approved', NULL, N'2026-10-11T05:43:19.3084240', NULL, N'2026-10-11T05:43:19.3084240', NULL, NULL, NULL),
        (18, 7, N'Deposit', N'BankAutoMatch', 5, 1056000, NULL, N'FJ260601-001', NULL, N'Approved', NULL, N'2026-06-01T05:34:00', NULL, N'2026-06-01T05:34:00', NULL, NULL, NULL),
        (19, 8, N'Deposit', N'BankAutoMatch', 6, 432000, NULL, N'FJ260818-001 dat coc', NULL, N'Approved', NULL, N'2026-08-18T01:27:00', NULL, N'2026-08-18T01:27:00', NULL, NULL, NULL),
        (20, 9, N'Deposit', N'BankAutoMatch', 7, 2400000, NULL, N'FJ261011-005', NULL, N'Approved', NULL, N'2026-10-11T01:43:19.3084240', NULL, N'2026-10-11T01:43:19.3084240', NULL, NULL, NULL),
        (21, 10, N'Deposit', N'BankAutoMatch', 8, 2400000, NULL, N'MBVCB.560275.FJ260804001.CT tu Đinh Thị Yến', NULL, N'Approved', NULL, N'2026-08-04T01:52:00', NULL, N'2026-08-04T01:52:00', NULL, NULL, NULL),
        (22, 11, N'Deposit', N'BankAutoMatch', 9, 231000, NULL, N'MBVCB.462430.FJ260705001.CT tu Lê Văn Hùng', NULL, N'Approved', NULL, N'2026-07-05T05:02:00', NULL, N'2026-07-05T05:02:00', NULL, NULL, NULL),
        (23, 12, N'Deposit', N'BankAutoMatch', 10, 432000, NULL, N'MBVCB.428352.FJ260713001.CT tu Hồ Minh Khang', NULL, N'Approved', NULL, N'2026-07-13T03:40:00', NULL, N'2026-07-13T03:40:00', NULL, NULL, NULL),
        (24, 14, N'Deposit', N'BankAutoMatch', 11, 240000, NULL, N'MBVCB.948530.FJ261009001.CT tu Lý Thanh Tâm', NULL, N'Approved', NULL, N'2026-10-08T21:40:19.3084240', NULL, N'2026-10-08T21:40:19.3084240', NULL, NULL, NULL),
        (25, 15, N'Deposit', N'BankAutoMatch', 12, 408000, NULL, N'FJ261011-006', NULL, N'Approved', NULL, N'2026-10-10T19:40:19.3084240', NULL, N'2026-10-10T19:40:19.3084240', NULL, NULL, NULL),
        (26, 16, N'Deposit', N'BankAutoMatch', 13, 768000, NULL, N'MBVCB.901086.FJ260601002.CT tu Phan Ngọc Ánh', NULL, N'Approved', NULL, N'2026-06-01T04:49:00', NULL, N'2026-06-01T04:49:00', NULL, NULL, NULL),
        (27, 18, N'Deposit', N'BankAutoMatch', 14, 288000, NULL, N'FJ260601-003', NULL, N'Approved', NULL, N'2026-06-01T01:06:00', NULL, N'2026-06-01T01:06:00', NULL, NULL, NULL),
        (28, 19, N'Deposit', N'BankAutoMatch', 15, 480000, NULL, N'MBVCB.365648.FJ260801001.CT tu Võ Thị Hồng', NULL, N'Approved', NULL, N'2026-08-01T02:09:00', NULL, N'2026-08-01T02:09:00', NULL, NULL, NULL),
        (29, 22, N'Deposit', N'BankAutoMatch', 16, 1536000, NULL, N'FJ260706-001', NULL, N'Approved', NULL, N'2026-07-06T03:43:00', NULL, N'2026-07-06T03:43:00', NULL, NULL, NULL),
        (30, 23, N'Deposit', N'BankAutoMatch', 17, 720000, NULL, N'MBVCB.625312.FJ260724001.CT tu Bùi Quốc Bảo', NULL, N'Approved', NULL, N'2026-07-24T02:54:00', NULL, N'2026-07-24T02:54:00', NULL, NULL, NULL),
        (31, 24, N'Deposit', N'BankAutoMatch', 18, 768000, NULL, N'FJ260718-001 dat coc', NULL, N'Approved', NULL, N'2026-07-18T05:59:00', NULL, N'2026-07-18T05:59:00', NULL, NULL, NULL),
        (32, 25, N'Deposit', N'BankAutoMatch', 19, 921000, NULL, N'FJ260625-001 dat coc', NULL, N'Approved', NULL, N'2026-06-25T02:13:00', NULL, N'2026-06-25T02:13:00', NULL, NULL, NULL),
        (33, 26, N'Deposit', N'BankAutoMatch', 20, 432000, NULL, N'FJ261009-002', NULL, N'Approved', NULL, N'2026-10-08T23:47:19.3084240', NULL, N'2026-10-08T23:47:19.3084240', NULL, NULL, NULL),
        (34, 27, N'Deposit', N'BankAutoMatch', 21, 480000, NULL, N'FJ261010-001 dat coc', NULL, N'Approved', NULL, N'2026-10-09T21:48:19.3084240', NULL, N'2026-10-09T21:48:19.3084240', NULL, NULL, NULL),
        (35, 28, N'Deposit', N'BankAutoMatch', 22, 300000, NULL, N'FJ260816-001', NULL, N'Approved', NULL, N'2026-08-16T05:45:00', NULL, N'2026-08-16T05:45:00', NULL, NULL, NULL),
        (36, 29, N'Deposit', N'BankAutoMatch', 23, 1080000, NULL, N'FJ260609-001 dat coc', NULL, N'Approved', NULL, N'2026-06-09T03:52:00', NULL, N'2026-06-09T03:52:00', NULL, NULL, NULL),
        (37, 30, N'Deposit', N'BankAutoMatch', 24, 576000, NULL, N'MBVCB.539625.FJ260730001.CT tu Võ Thị Hồng', NULL, N'Approved', NULL, N'2026-07-30T03:17:00', NULL, N'2026-07-30T03:17:00', NULL, NULL, NULL),
        (38, 32, N'Deposit', N'BankAutoMatch', 25, 480000, NULL, N'FJ261011-008', NULL, N'Approved', NULL, N'2026-10-10T21:46:19.3084240', NULL, N'2026-10-10T21:46:19.3084240', NULL, NULL, NULL),
        (39, 33, N'Deposit', N'BankAutoMatch', 26, 480000, NULL, N'FJ260704-001 dat coc', NULL, N'Approved', NULL, N'2026-07-04T03:56:00', NULL, N'2026-07-04T03:56:00', NULL, NULL, NULL),
        (40, 34, N'Deposit', N'BankAutoMatch', 27, 300000, NULL, N'MBVCB.404921.FJ261010002.CT tu Kiều Văn Phúc', NULL, N'Approved', NULL, N'2026-10-09T18:42:19.3084240', NULL, N'2026-10-09T18:42:19.3084240', NULL, NULL, NULL),
        (41, 35, N'Deposit', N'BankAutoMatch', 28, 288000, NULL, N'MBVCB.647706.FJ260715001.CT tu Mai Đức Anh', NULL, N'Approved', NULL, N'2026-07-15T06:02:00', NULL, N'2026-07-15T06:02:00', NULL, NULL, NULL),
        (42, 36, N'Deposit', N'BankAutoMatch', 29, 690000, NULL, N'MBVCB.429082.FJ260717001.CT tu Mai Đức Anh', NULL, N'Approved', NULL, N'2026-07-17T01:12:00', NULL, N'2026-07-17T01:12:00', NULL, NULL, NULL),
        (43, 39, N'Deposit', N'BankAutoMatch', 30, 180000, NULL, N'MBVCB.959950.FJ260902001.CT tu Bùi Quốc Bảo', NULL, N'Approved', NULL, N'2026-09-02T03:22:00', NULL, N'2026-09-02T03:22:00', NULL, NULL, NULL),
        (44, 40, N'Deposit', N'BankAutoMatch', 31, 480000, NULL, N'FJ261011-009', NULL, N'Approved', NULL, N'2026-10-11T01:47:19.3084240', NULL, N'2026-10-11T01:47:19.3084240', NULL, NULL, NULL),
        (45, 41, N'Deposit', N'BankAutoMatch', 32, 216000, NULL, N'FJ261008-002', NULL, N'Approved', NULL, N'2026-10-08T12:49:19.3084240', NULL, N'2026-10-08T12:49:19.3084240', NULL, NULL, NULL),
        (46, 42, N'Deposit', N'BankAutoMatch', 33, 330000, NULL, N'FJ261009-003', NULL, N'Approved', NULL, N'2026-10-09T06:48:19.3084240', NULL, N'2026-10-09T06:48:19.3084240', NULL, NULL, NULL),
        (47, 44, N'Deposit', N'BankAutoMatch', 34, 810000, NULL, N'MBVCB.875883.FJ261010003.CT tu Mai Đức Anh', NULL, N'Approved', NULL, N'2026-10-10T03:50:19.3084240', NULL, N'2026-10-10T03:50:19.3084240', NULL, NULL, NULL),
        (48, 45, N'Deposit', N'BankAutoMatch', 35, 402000, NULL, N'FJ260601-005 dat coc', NULL, N'Approved', NULL, N'2026-06-01T03:01:00', NULL, N'2026-06-01T03:01:00', NULL, NULL, NULL),
        (49, 46, N'Deposit', N'BankAutoMatch', 36, 423000, NULL, N'FJ261010-004', NULL, N'Approved', NULL, N'2026-10-10T01:22:00', NULL, N'2026-10-10T01:22:00', NULL, NULL, NULL),
        (50, 48, N'Deposit', N'BankAutoMatch', 37, 612000, NULL, N'MBVCB.368766.FJ260913001.CT tu Chu Thị Ngân', NULL, N'Approved', NULL, N'2026-09-13T04:50:00', NULL, N'2026-09-13T04:50:00', NULL, NULL, NULL),
        (51, 50, N'Deposit', N'BankAutoMatch', 38, 174000, NULL, N'FJ260910-001 dat coc', NULL, N'Approved', NULL, N'2026-09-10T02:20:00', NULL, N'2026-09-10T02:20:00', NULL, NULL, NULL),
        (52, 51, N'Deposit', N'BankAutoMatch', 39, 114000, NULL, N'FJ261001-001', NULL, N'Approved', NULL, N'2026-10-01T04:53:00', NULL, N'2026-10-01T04:53:00', NULL, NULL, NULL),
        (53, 53, N'Deposit', N'BankAutoMatch', 40, 192000, NULL, N'FJ260729-001', NULL, N'Approved', NULL, N'2026-07-29T02:39:00', NULL, N'2026-07-29T02:39:00', NULL, NULL, NULL),
        (54, 55, N'Deposit', N'BankAutoMatch', 41, 240000, NULL, N'FJ261011-010 dat coc', NULL, N'Approved', NULL, N'2026-10-10T22:46:19.3084240', NULL, N'2026-10-10T22:46:19.3084240', NULL, NULL, NULL),
        (55, 57, N'Deposit', N'BankAutoMatch', 42, 2016000, NULL, N'FJ260715-002', NULL, N'Approved', NULL, N'2026-07-15T03:07:00', NULL, N'2026-07-15T03:07:00', NULL, NULL, NULL),
        (56, 58, N'Deposit', N'BankAutoMatch', 43, 1200000, NULL, N'MBVCB.974092.FJ261010006.CT tu Đặng Thị Thu', NULL, N'Approved', NULL, N'2026-10-09T21:49:19.3084240', NULL, N'2026-10-09T21:49:19.3084240', NULL, NULL, NULL),
        (57, 60, N'Deposit', N'BankAutoMatch', 44, 1440000, NULL, N'FJ260809-001 dat coc', NULL, N'Approved', NULL, N'2026-08-09T04:23:00', NULL, N'2026-08-09T04:23:00', NULL, NULL, NULL),
        (1002, 1002, N'Deposit', N'ManualReceipt', NULL, 270000, N'FJ261012-001-0e3f53a07de04338b0477fa34a10090f.png', NULL, NULL, N'Approved', N'3027af7f-546b-49bc-8598-af0d9a0034e2', N'2026-10-12T06:40:57.6776510', 2, N'2026-10-12T06:43:29.4961510', NULL, NULL, NULL),
        (1003, 1004, N'Deposit', N'ManualReceipt', NULL, 270000, N'FJ261012-003-ef5c83ea1fd6419e964792008dc0dd1d.png', NULL, NULL, N'Approved', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'2026-10-12T08:49:39.4382640', 2, N'2026-10-12T08:50:41.7863680', NULL, NULL, NULL),
        (1004, 2006, N'Deposit', N'ManualReceipt', NULL, 360000, N'FJ261015-002-37c39bc9ea7a404da243c354609bed0b.png', NULL, NULL, N'Submitted', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'2026-10-15T05:50:42.8280550', NULL, NULL, NULL, N'2026-10-15T07:51:28.4082720', NULL),
        (1005, 3008, N'Deposit', N'ManualReceipt', NULL, 270000, N'FJ261018-002-7497ccab8a4744adb6c093bdcd4b85f1.png', NULL, NULL, N'Approved', NULL, N'2026-10-18T14:09:11.0827250', 2, N'2026-10-18T14:11:41.7047510', NULL, NULL, NULL),
        (1006, 3013, N'Deposit', N'BankAutoMatch', 1003, 540000, NULL, N'UI04 doi chieu 20261019', NULL, N'Approved', NULL, N'2026-10-19T02:01:54.5476790', 3, N'2026-10-19T02:01:54.5476790', NULL, NULL, NULL),
        (1007, 3015, N'Deposit', N'BankAutoMatch', 1004, 596000, NULL, N'FJ261019-003 dat coc', NULL, N'Approved', NULL, N'2026-10-19T07:31:33.4954280', NULL, N'2026-10-19T07:31:33.4954280', NULL, NULL, NULL),
        (1008, 3017, N'Deposit', N'BankAutoMatch', 1005, 272000, NULL, N'FJ261019-005 dat coc B9', NULL, N'Approved', NULL, N'2026-10-19T14:14:46.0055700', NULL, N'2026-10-19T14:14:46.0055700', NULL, NULL, NULL),
        (1009, 3019, N'Deposit', N'ManualReceipt', NULL, 864000, N'FJ261019-007-f1d4d27509e843dd8e746b4f5d9ff10e.png', NULL, NULL, N'Approved', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'2026-10-19T16:55:31.5085770', 2, N'2026-10-19T16:57:04.5237670', NULL, NULL, NULL),
        (2005, 4008, N'Deposit', N'ManualReceipt', NULL, 543000, N'FJ261020-001-0859b569a6cb40759e1adf8b7a9b3d41.png', NULL, NULL, N'Approved', N'241fe00b-0d8e-4e8e-9133-9648407ccc6e', N'2026-10-19T19:11:39.3281710', 2, N'2026-10-19T19:13:50.8100050', NULL, NULL, NULL);
    SET IDENTITY_INSERT [dbo].[Payments] OFF;
    DBCC CHECKIDENT (N'[dbo].[Payments]', RESEED, 2005) WITH NO_INFOMSGS;

    -- RoomNights: 103 dòng.
    INSERT INTO [dbo].[RoomNights] ([RoomId], [Date], [BookingId], [RoomBlockId], [BookingChangeRequestId]) VALUES
        (1, N'2026-10-23', 15, NULL, NULL),
        (1, N'2026-10-24', 15, NULL, NULL),
        (1, N'2026-10-25', 15, NULL, NULL),
        (4, N'2026-10-20', 3, NULL, NULL),
        (9, N'2026-08-29', 30, NULL, NULL),
        (9, N'2026-08-30', 30, NULL, NULL),
        (9, N'2026-08-31', 30, NULL, NULL),
        (14, N'2026-07-25', 43, NULL, NULL),
        (14, N'2026-07-26', 43, NULL, NULL),
        (14, N'2026-07-27', 43, NULL, NULL),
        (14, N'2026-10-11', 2, NULL, NULL),
        (14, N'2026-10-12', 2, NULL, NULL),
        (14, N'2026-10-13', 2, NULL, NULL),
        (17, N'2026-08-04', 24, NULL, NULL),
        (17, N'2026-08-05', 24, NULL, NULL),
        (18, N'2026-10-22', 46, NULL, NULL),
        (18, N'2026-10-23', 46, NULL, NULL),
        (19, N'2026-10-12', 1002, NULL, NULL),
        (19, N'2026-10-18', 3008, NULL, NULL),
        (19, N'2026-10-20', 4008, NULL, NULL),
        (19, N'2026-10-21', 4008, NULL, NULL),
        (19, N'2026-12-05', 3015, NULL, NULL),
        (19, N'2026-12-06', 3015, NULL, NULL),
        (19, N'2026-12-15', 3013, NULL, NULL),
        (19, N'2026-12-16', 3013, NULL, NULL),
        (19, N'2026-12-20', 3017, NULL, NULL),
        (20, N'2026-10-10', 1, NULL, NULL),
        (20, N'2026-10-11', 1, NULL, NULL),
        (20, N'2026-10-12', 1, NULL, NULL),
        (21, N'2026-08-09', 12, NULL, NULL),
        (22, N'2026-10-16', 2006, NULL, NULL),
        (25, N'2026-07-17', 35, NULL, NULL),
        (26, N'2026-06-27', 13, NULL, NULL),
        (28, N'2026-07-24', 11, NULL, NULL),
        (29, N'2026-09-02', 20, NULL, NULL),
        (29, N'2026-09-03', 20, NULL, NULL),
        (29, N'2026-09-04', 20, NULL, NULL),
        (35, N'2026-06-03', 18, NULL, NULL),
        (35, N'2026-08-14', 36, NULL, NULL),
        (35, N'2026-08-15', 36, NULL, NULL),
        (40, N'2026-06-10', 16, NULL, NULL),
        (40, N'2026-06-11', 16, NULL, NULL),
        (40, N'2026-06-12', 16, NULL, NULL),
        (42, N'2026-09-06', 56, NULL, NULL),
        (46, N'2026-12-08', 41, NULL, NULL),
        (49, N'2026-12-08', 40, NULL, NULL),
        (53, N'2026-06-09', 38, NULL, NULL),
        (53, N'2026-06-10', 38, NULL, NULL),
        (53, N'2026-06-11', 38, NULL, NULL),
        (53, N'2026-10-03', 51, NULL, NULL),
        (55, N'2026-11-22', 34, NULL, NULL),
        (55, N'2026-11-23', 34, NULL, NULL),
        (56, N'2026-07-27', 33, NULL, NULL),
        (56, N'2026-07-28', 33, NULL, NULL),
        (56, N'2026-12-27', 32, NULL, NULL),
        (56, N'2026-12-28', 32, NULL, NULL),
        (57, N'2026-09-22', NULL, 1, NULL),
        (57, N'2026-09-23', NULL, 1, NULL),
        (57, N'2026-09-24', NULL, 1, NULL),
        (58, N'2026-09-24', 48, NULL, NULL),
        (58, N'2026-09-25', 48, NULL, NULL),
        (58, N'2026-09-26', 48, NULL, NULL),
        (60, N'2026-09-19', 50, NULL, NULL),
        (62, N'2026-10-30', NULL, 5, NULL),
        (62, N'2026-10-31', NULL, 5, NULL),
        (72, N'2026-10-17', NULL, 3, NULL),
        (73, N'2026-07-29', 22, NULL, NULL),
        (73, N'2026-07-30', 22, NULL, NULL),
        (73, N'2026-07-31', 22, NULL, NULL),
        (73, N'2026-10-28', 27, NULL, NULL),
        (76, N'2026-08-31', 28, NULL, NULL),
        (76, N'2026-09-01', 28, NULL, NULL),
        (89, N'2026-10-31', 6, NULL, NULL),
        (93, N'2026-10-12', 1004, NULL, NULL),
        (95, N'2026-10-19', 3019, NULL, NULL),
        (95, N'2026-10-20', 3019, NULL, NULL),
        (96, N'2026-06-28', 29, NULL, NULL),
        (96, N'2026-11-11', NULL, 6, NULL),
        (96, N'2026-11-12', NULL, 6, NULL),
        (96, N'2026-11-13', NULL, 6, NULL),
        (98, N'2026-10-14', NULL, 2, NULL),
        (98, N'2026-10-15', NULL, 2, NULL),
        (100, N'2026-11-11', 58, NULL, NULL),
        (107, N'2026-10-27', 4, NULL, NULL),
        (107, N'2026-11-29', 55, NULL, NULL),
        (110, N'2026-09-17', 39, NULL, NULL),
        (111, N'2026-07-15', 25, NULL, NULL),
        (111, N'2026-07-16', 25, NULL, NULL),
        (111, N'2026-07-17', 25, NULL, NULL),
        (112, N'2026-08-10', 60, NULL, NULL),
        (112, N'2026-08-11', 60, NULL, NULL),
        (116, N'2026-07-17', 57, NULL, NULL),
        (116, N'2026-07-18', 57, NULL, NULL),
        (118, N'2026-08-17', 19, NULL, NULL),
        (118, N'2026-08-18', 19, NULL, NULL),
        (127, N'2026-10-23', NULL, 4, NULL),
        (127, N'2026-10-24', NULL, 4, NULL),
        (127, N'2026-10-25', NULL, 4, NULL),
        (127, N'2026-10-26', NULL, 4, NULL),
        (128, N'2026-10-23', 5, NULL, NULL);
    INSERT INTO [dbo].[RoomNights] ([RoomId], [Date], [BookingId], [RoomBlockId], [BookingChangeRequestId]) VALUES
        (128, N'2026-10-24', 5, NULL, NULL),
        (128, N'2026-12-20', 9, NULL, NULL),
        (128, N'2026-12-21', 9, NULL, NULL);

    -- Kiểm tra lại dữ liệu cũ/mới và khôi phục FK trusted trước khi COMMIT.
    ALTER TABLE [dbo].[Bookings] WITH CHECK CHECK CONSTRAINT [FK_Bookings_Bookings_ReplacedByBookingId];
    ALTER TABLE [dbo].[Vouchers] WITH CHECK CHECK CONSTRAINT [FK_Vouchers_Bookings_SourceBookingId];
    ALTER TABLE [dbo].[Bookings] WITH CHECK CHECK CONSTRAINT [FK_Bookings_Vouchers_VoucherId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
        THROW 50105, N'Khóa ngoại chưa bật hoặc chưa trusted.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[__EFMigrationsHistory]) <> 9
        THROW 50106, N'Số dòng không khớp: __EFMigrationsHistory.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AccountActivityLogs]) <> 9
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
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserRoles]) <> 26
        THROW 50106, N'Số dòng không khớp: AspNetUserRoles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUsers]) <> 25
        THROW 50106, N'Số dòng không khớp: AspNetUsers.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AspNetUserTokens]) <> 0
        THROW 50106, N'Số dòng không khớp: AspNetUserTokens.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[AuditLogs]) <> 169
        THROW 50106, N'Số dòng không khớp: AuditLogs.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BankTransactions]) <> 50
        THROW 50106, N'Số dòng không khớp: BankTransactions.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingChangeRequests]) <> 6
        THROW 50106, N'Số dòng không khớp: BookingChangeRequests.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingCodeCounters]) <> 42
        THROW 50106, N'Số dòng không khớp: BookingCodeCounters.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingNights]) <> 153
        THROW 50106, N'Số dòng không khớp: BookingNights.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Bookings]) <> 81
        THROW 50106, N'Số dòng không khớp: Bookings.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[BookingStatusHistories]) <> 288
        THROW 50106, N'Số dòng không khớp: BookingStatusHistories.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CancellationPolicies]) <> 6
        THROW 50106, N'Số dòng không khớp: CancellationPolicies.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CancellationRules]) <> 18
        THROW 50106, N'Số dòng không khớp: CancellationRules.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Contracts]) <> 36
        THROW 50106, N'Số dòng không khớp: Contracts.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[CustomerProfiles]) <> 23
        THROW 50106, N'Số dòng không khớp: CustomerProfiles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Customers]) <> 23
        THROW 50106, N'Số dòng không khớp: Customers.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Employees]) <> 6
        THROW 50106, N'Số dòng không khớp: Employees.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Holidays]) <> 29
        THROW 50106, N'Số dòng không khớp: Holidays.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[HomestayAmenities]) <> 183
        THROW 50106, N'Số dòng không khớp: HomestayAmenities.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[HomestayImages]) <> 179
        THROW 50106, N'Số dòng không khớp: HomestayImages.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Homestays]) <> 37
        THROW 50106, N'Số dòng không khớp: Homestays.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[MessageLogs]) <> 3
        THROW 50106, N'Số dòng không khớp: MessageLogs.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Notifications]) <> 90
        THROW 50106, N'Số dòng không khớp: Notifications.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[OwnerRegistrations]) <> 8
        THROW 50106, N'Số dòng không khớp: OwnerRegistrations.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Owners]) <> 8
        THROW 50106, N'Số dòng không khớp: Owners.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Payments]) <> 66
        THROW 50106, N'Số dòng không khớp: Payments.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[PersonProfiles]) <> 36
        THROW 50106, N'Số dòng không khớp: PersonProfiles.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Reviews]) <> 20
        THROW 50106, N'Số dòng không khớp: Reviews.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomBlocks]) <> 6
        THROW 50106, N'Số dòng không khớp: RoomBlocks.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomCategories]) <> 5
        THROW 50106, N'Số dòng không khớp: RoomCategories.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomNights]) <> 103
        THROW 50106, N'Số dòng không khớp: RoomNights.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Rooms]) <> 128
        THROW 50106, N'Số dòng không khớp: Rooms.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomTypePrices]) <> 281
        THROW 50106, N'Số dòng không khớp: RoomTypePrices.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[RoomTypes]) <> 93
        THROW 50106, N'Số dòng không khớp: RoomTypes.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[SettlementItems]) <> 28
        THROW 50106, N'Số dòng không khớp: SettlementItems.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Settlements]) <> 27
        THROW 50106, N'Số dòng không khớp: Settlements.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[VerificationDocuments]) <> 120
        THROW 50106, N'Số dòng không khớp: VerificationDocuments.', 1;
    IF (SELECT COUNT_BIG(*) FROM [dbo].[Vouchers]) <> 4
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
PRINT N'Đã nạp dữ liệu đầy đủ; chạy verify.sql và tests/data_consistency.sql để kiểm tra.';
GO
