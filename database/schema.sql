IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Amenities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Icon] nvarchar(50) NULL,
        CONSTRAINT [PK_Amenities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Areas] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Province] nvarchar(100) NOT NULL,
        [AreaGroup] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [BookingCodeCounters] (
        [Date] date NOT NULL,
        [LastSeq] int NOT NULL,
        CONSTRAINT [PK_BookingCodeCounters] PRIMARY KEY ([Date])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Holidays] (
        [Date] date NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Date])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [PersonProfiles] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(16) NOT NULL,
        [Email] nvarchar(256) NULL,
        [Address] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PersonProfiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [RoomCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_RoomCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [PersonId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [PasswordChangedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_PersonProfiles_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [PersonProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [PersonId] int NOT NULL,
        [Note] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_PersonProfiles_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [PersonProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [PersonId] int NOT NULL,
        [EmployeeCode] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NULL,
        [Position] nvarchar(20) NOT NULL,
        [EmploymentStatus] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_PersonProfiles_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [PersonProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Owners] (
        [Id] int NOT NULL IDENTITY,
        [PersonId] int NOT NULL,
        [BankName] nvarchar(100) NULL,
        [BankAccountNo] nvarchar(30) NULL,
        [BankAccountName] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Owners] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Owners_PersonProfiles_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [PersonProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AccountActivityLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [AccountId] nvarchar(450) NULL,
        [EventType] nvarchar(40) NOT NULL,
        [ActorType] nvarchar(20) NOT NULL,
        [ActorId] nvarchar(450) NULL,
        [Detail] nvarchar(1000) NULL,
        [IpAddress] nvarchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AccountActivityLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountActivityLogs_AspNetUsers_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [AssignedByAccountId] nvarchar(450) NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_AssignedByAccountId] FOREIGN KEY ([AssignedByAccountId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [AccountId] nvarchar(450) NULL,
        [TableName] nvarchar(100) NOT NULL,
        [RecordId] nvarchar(100) NOT NULL,
        [Action] nvarchar(10) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [IpAddress] nvarchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [Url] nvarchar(300) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [CustomerProfiles] (
        [CustomerId] int NOT NULL,
        [CitizenId] nvarchar(20) NULL,
        [StudentId] nvarchar(30) NULL,
        [StudentVerificationStatus] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomerProfiles] PRIMARY KEY ([CustomerId]),
        CONSTRAINT [FK_CustomerProfiles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Homestays] (
        [Id] int NOT NULL IDENTITY,
        [OwnerId] int NOT NULL,
        [AreaId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [DescriptionHtml] nvarchar(max) NULL,
        [CheckinGuideHtml] nvarchar(max) NULL,
        [ContactPhone] nvarchar(16) NULL,
        [CheckinTime] time NOT NULL,
        [CheckoutTime] time NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ApprovedByEmployeeId] int NULL,
        [ApprovedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Homestays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Homestays_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Homestays_Employees_ApprovedByEmployeeId] FOREIGN KEY ([ApprovedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Homestays_Owners_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Owners] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [OwnerRegistrations] (
        [Id] int NOT NULL IDENTITY,
        [OwnerId] int NOT NULL,
        [Source] nvarchar(20) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedByEmployeeId] int NULL,
        [ReviewedByEmployeeId] int NULL,
        [ReviewNote] nvarchar(1000) NULL,
        [SubmittedAt] datetime2 NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OwnerRegistrations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OwnerRegistrations_Employees_CreatedByEmployeeId] FOREIGN KEY ([CreatedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_OwnerRegistrations_Employees_ReviewedByEmployeeId] FOREIGN KEY ([ReviewedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_OwnerRegistrations_Owners_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Owners] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [CancellationPolicies] (
        [Id] int NOT NULL IDENTITY,
        [HomestayId] int NULL,
        [Scope] nvarchar(10) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CancellationPolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CancellationPolicies_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Contracts] (
        [Id] int NOT NULL IDENTITY,
        [HomestayId] int NOT NULL,
        [ContractNo] nvarchar(30) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [CommissionRate] decimal(5,4) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [SignedByEmployeeId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Contracts_CommissionRate] CHECK ([CommissionRate] > 0 AND [CommissionRate] < 1),
        CONSTRAINT [CK_Contracts_Dates] CHECK ([EndDate] > [StartDate]),
        CONSTRAINT [FK_Contracts_Employees_SignedByEmployeeId] FOREIGN KEY ([SignedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Contracts_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [HomestayAmenities] (
        [HomestayId] int NOT NULL,
        [AmenityId] int NOT NULL,
        CONSTRAINT [PK_HomestayAmenities] PRIMARY KEY ([HomestayId], [AmenityId]),
        CONSTRAINT [FK_HomestayAmenities_Amenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [Amenities] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HomestayAmenities_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [RoomTypes] (
        [Id] int NOT NULL IDENTITY,
        [HomestayId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [MaxGuests] int NOT NULL,
        [AreaM2] int NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RoomTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RoomTypes_MaxGuests] CHECK ([MaxGuests] > 0),
        CONSTRAINT [FK_RoomTypes_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoomTypes_RoomCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [RoomCategories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Settlements] (
        [Id] int NOT NULL IDENTITY,
        [HomestayId] int NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        [PeriodNo] int NOT NULL,
        [TotalRoomAmount] decimal(18,0) NOT NULL,
        [TotalRetained] decimal(18,0) NOT NULL,
        [TotalCommission] decimal(18,0) NOT NULL,
        [PayoutAmount] decimal(18,0) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PaidAt] datetime2 NULL,
        [PaidByEmployeeId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Settlements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Settlements_Period] CHECK ([PeriodNo] IN (1, 2) AND [Month] BETWEEN 1 AND 12),
        CONSTRAINT [FK_Settlements_Employees_PaidByEmployeeId] FOREIGN KEY ([PaidByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Settlements_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [VerificationDocuments] (
        [Id] int NOT NULL IDENTITY,
        [OwnerRegistrationId] int NULL,
        [HomestayId] int NULL,
        [DocumentType] nvarchar(30) NOT NULL,
        [FileUrl] nvarchar(300) NOT NULL,
        [OriginalFileName] nvarchar(255) NULL,
        [ExpiryDate] date NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VerificationDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VerificationDocuments_Owner] CHECK (([OwnerRegistrationId] IS NOT NULL AND [HomestayId] IS NULL) OR ([OwnerRegistrationId] IS NULL AND [HomestayId] IS NOT NULL)),
        CONSTRAINT [FK_VerificationDocuments_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]),
        CONSTRAINT [FK_VerificationDocuments_OwnerRegistrations_OwnerRegistrationId] FOREIGN KEY ([OwnerRegistrationId]) REFERENCES [OwnerRegistrations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [CancellationRules] (
        [Id] int NOT NULL IDENTITY,
        [PolicyId] int NOT NULL,
        [MinDaysBefore] int NOT NULL,
        [RefundPercent] int NOT NULL,
        CONSTRAINT [PK_CancellationRules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CancellationRules_Days] CHECK ([MinDaysBefore] >= 0),
        CONSTRAINT [CK_CancellationRules_Percent] CHECK ([RefundPercent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_CancellationRules_CancellationPolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [CancellationPolicies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [HomestayImages] (
        [Id] int NOT NULL IDENTITY,
        [HomestayId] int NOT NULL,
        [RoomTypeId] int NULL,
        [Url] nvarchar(300) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsCover] bit NOT NULL,
        CONSTRAINT [PK_HomestayImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HomestayImages_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HomestayImages_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [RoomTypeId] int NOT NULL,
        [RoomCode] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rooms_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [RoomTypePrices] (
        [Id] int NOT NULL IDENTITY,
        [RoomTypeId] int NOT NULL,
        [DayType] nvarchar(10) NOT NULL,
        [Price] decimal(18,0) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RoomTypePrices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RoomTypePrices_Price] CHECK ([Price] > 0),
        CONSTRAINT [FK_RoomTypePrices_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [CustomerId] int NOT NULL,
        [ContactName] nvarchar(100) NOT NULL,
        [ContactPhone] nvarchar(16) NOT NULL,
        [SourceChannel] nvarchar(20) NOT NULL,
        [CreatedByEmployeeId] int NULL,
        [AssignedEmployeeId] int NULL,
        [HomestayId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomId] int NOT NULL,
        [CheckIn] date NOT NULL,
        [CheckOut] date NOT NULL,
        [Guests] int NOT NULL,
        [SpecialRequests] nvarchar(1000) NULL,
        [Status] nvarchar(20) NOT NULL,
        [HoldExpiresAt] datetime2 NOT NULL,
        [PaymentSubmittedAt] datetime2 NULL,
        [DepositedAt] datetime2 NULL,
        [ConfirmDeadlineAt] datetime2 NULL,
        [ConfirmAlertSentAt] datetime2 NULL,
        [ConfirmedAt] datetime2 NULL,
        [CheckedInAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancelReason] nvarchar(500) NULL,
        [TotalAmount] decimal(18,0) NOT NULL,
        [DepositRate] decimal(5,4) NOT NULL,
        [DepositAmount] decimal(18,0) NOT NULL,
        [CommissionRate] decimal(5,4) NOT NULL,
        [CancellationPolicyId] int NOT NULL,
        [RefundAmount] decimal(18,0) NULL,
        [ReplacedByBookingId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Bookings_Amounts] CHECK ([TotalAmount] > 0 AND [DepositAmount] >= 0 AND [DepositAmount] <= [TotalAmount]),
        CONSTRAINT [CK_Bookings_Dates] CHECK ([CheckOut] > [CheckIn]),
        CONSTRAINT [CK_Bookings_Guests] CHECK ([Guests] > 0),
        CONSTRAINT [FK_Bookings_Bookings_ReplacedByBookingId] FOREIGN KEY ([ReplacedByBookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_Bookings_CancellationPolicies_CancellationPolicyId] FOREIGN KEY ([CancellationPolicyId]) REFERENCES [CancellationPolicies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Employees_AssignedEmployeeId] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Bookings_Employees_CreatedByEmployeeId] FOREIGN KEY ([CreatedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Bookings_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [RoomBlocks] (
        [Id] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Reason] nvarchar(20) NOT NULL,
        [Note] nvarchar(255) NULL,
        [CreatedByAccountId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RoomBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RoomBlocks_Dates] CHECK ([EndDate] > [StartDate]),
        CONSTRAINT [FK_RoomBlocks_AspNetUsers_CreatedByAccountId] FOREIGN KEY ([CreatedByAccountId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoomBlocks_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [BankTransactions] (
        [Id] int NOT NULL IDENTITY,
        [Provider] nvarchar(20) NOT NULL,
        [ProviderTransactionId] nvarchar(100) NOT NULL,
        [BankReference] nvarchar(100) NULL,
        [AccountNumber] nvarchar(30) NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [TransferContent] nvarchar(500) NULL,
        [TransactionTime] datetime2 NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [RawPayload] nvarchar(max) NULL,
        [MatchStatus] nvarchar(20) NOT NULL,
        [MatchedBookingId] int NULL,
        [ProcessedByEmployeeId] int NULL,
        [ProcessedAt] datetime2 NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_BankTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BankTransactions_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_BankTransactions_Bookings_MatchedBookingId] FOREIGN KEY ([MatchedBookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_BankTransactions_Employees_ProcessedByEmployeeId] FOREIGN KEY ([ProcessedByEmployeeId]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [BookingChangeRequests] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [RequestType] nvarchar(20) NOT NULL,
        [RequestedByType] nvarchar(20) NOT NULL,
        [NewCheckIn] date NULL,
        [NewCheckOut] date NULL,
        [NewRoomTypeId] int NULL,
        [PriceDifference] decimal(18,0) NULL,
        [RefundAmount] decimal(18,0) NULL,
        [Status] nvarchar(20) NOT NULL,
        [DecidedByAccountId] nvarchar(450) NULL,
        [DecidedAt] datetime2 NULL,
        [DecisionNote] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BookingChangeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingChangeRequests_AspNetUsers_DecidedByAccountId] FOREIGN KEY ([DecidedByAccountId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_BookingChangeRequests_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BookingChangeRequests_RoomTypes_NewRoomTypeId] FOREIGN KEY ([NewRoomTypeId]) REFERENCES [RoomTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [BookingNights] (
        [BookingId] int NOT NULL,
        [Date] date NOT NULL,
        [DayType] nvarchar(10) NOT NULL,
        [Price] decimal(18,0) NOT NULL,
        CONSTRAINT [PK_BookingNights] PRIMARY KEY ([BookingId], [Date]),
        CONSTRAINT [CK_BookingNights_Price] CHECK ([Price] > 0),
        CONSTRAINT [FK_BookingNights_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [BookingStatusHistories] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [FromStatus] nvarchar(20) NULL,
        [ToStatus] nvarchar(20) NOT NULL,
        [ActorType] nvarchar(20) NOT NULL,
        [ActorAccountId] nvarchar(450) NULL,
        [Note] nvarchar(500) NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BookingStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingStatusHistories_AspNetUsers_ActorAccountId] FOREIGN KEY ([ActorAccountId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_BookingStatusHistories_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [MessageLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [Channel] nvarchar(10) NOT NULL,
        [RecipientPhone] nvarchar(16) NULL,
        [RecipientEmail] nvarchar(256) NULL,
        [TemplateCode] nvarchar(50) NOT NULL,
        [BookingId] int NULL,
        [Status] nvarchar(10) NOT NULL,
        [ErrorMessage] nvarchar(500) NULL,
        [SentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MessageLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MessageLogs_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [HomestayId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [OwnerReply] nvarchar(1000) NULL,
        [RepliedAt] datetime2 NULL,
        [IsHidden] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Reviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Homestays_HomestayId] FOREIGN KEY ([HomestayId]) REFERENCES [Homestays] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [SettlementItems] (
        [Id] int NOT NULL IDENTITY,
        [SettlementId] int NOT NULL,
        [BookingId] int NOT NULL,
        [ItemType] nvarchar(20) NOT NULL,
        [RoomAmount] decimal(18,0) NOT NULL,
        [DepositAmount] decimal(18,0) NOT NULL,
        [RefundAmount] decimal(18,0) NOT NULL,
        [RetainedAmount] decimal(18,0) NOT NULL,
        [CommissionRate] decimal(5,4) NOT NULL,
        [CommissionAmount] decimal(18,0) NOT NULL,
        [PayoutAmount] decimal(18,0) NOT NULL,
        CONSTRAINT [PK_SettlementItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SettlementItems_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SettlementItems_Settlements_SettlementId] FOREIGN KEY ([SettlementId]) REFERENCES [Settlements] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [RoomNights] (
        [RoomId] int NOT NULL,
        [Date] date NOT NULL,
        [BookingId] int NULL,
        [RoomBlockId] int NULL,
        CONSTRAINT [PK_RoomNights] PRIMARY KEY ([RoomId], [Date]),
        CONSTRAINT [CK_RoomNights_Owner] CHECK (([BookingId] IS NOT NULL AND [RoomBlockId] IS NULL) OR ([BookingId] IS NULL AND [RoomBlockId] IS NOT NULL)),
        CONSTRAINT [FK_RoomNights_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_RoomNights_RoomBlocks_RoomBlockId] FOREIGN KEY ([RoomBlockId]) REFERENCES [RoomBlocks] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoomNights_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Method] nvarchar(20) NOT NULL,
        [BankTransactionId] int NULL,
        [Amount] decimal(18,0) NOT NULL,
        [ReceiptImageUrl] nvarchar(300) NULL,
        [TransferContent] nvarchar(255) NULL,
        [BankRef] nvarchar(100) NULL,
        [Status] nvarchar(20) NOT NULL,
        [SubmittedByAccountId] nvarchar(450) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [ReviewedByEmployeeId] int NULL,
        [ReviewedAt] datetime2 NULL,
        [RejectReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Payments_AspNetUsers_SubmittedByAccountId] FOREIGN KEY ([SubmittedByAccountId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Payments_BankTransactions_BankTransactionId] FOREIGN KEY ([BankTransactionId]) REFERENCES [BankTransactions] ([Id]),
        CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Employees_ReviewedByEmployeeId] FOREIGN KEY ([ReviewedByEmployeeId]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AccountActivityLogs_AccountId_CreatedAt] ON [AccountActivityLogs] ([AccountId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Amenities_Name] ON [Amenities] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Areas_Province_Name] ON [Areas] ([Province], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_AssignedByAccountId] ON [AspNetUserRoles] ([AssignedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AspNetUsers_PersonId] ON [AspNetUsers] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_AccountId] ON [AuditLogs] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TableName_RecordId] ON [AuditLogs] ([TableName], [RecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BankTransactions_MatchedBookingId] ON [BankTransactions] ([MatchedBookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BankTransactions_MatchStatus_TransactionTime] ON [BankTransactions] ([MatchStatus], [TransactionTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BankTransactions_ProcessedByEmployeeId] ON [BankTransactions] ([ProcessedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BankTransactions_Provider_ProviderTransactionId] ON [BankTransactions] ([Provider], [ProviderTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BookingChangeRequests_BookingId] ON [BookingChangeRequests] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BookingChangeRequests_DecidedByAccountId] ON [BookingChangeRequests] ([DecidedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BookingChangeRequests_NewRoomTypeId] ON [BookingChangeRequests] ([NewRoomTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_AssignedEmployeeId_Status] ON [Bookings] ([AssignedEmployeeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_CancellationPolicyId] ON [Bookings] ([CancellationPolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_CheckOut] ON [Bookings] ([CheckOut]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bookings_Code] ON [Bookings] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_CreatedByEmployeeId] ON [Bookings] ([CreatedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_CustomerId] ON [Bookings] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_HomestayId_CheckIn] ON [Bookings] ([HomestayId], [CheckIn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_ReplacedByBookingId] ON [Bookings] ([ReplacedByBookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_RoomId] ON [Bookings] ([RoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_RoomTypeId] ON [Bookings] ([RoomTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_Status_ConfirmDeadlineAt] ON [Bookings] ([Status], [ConfirmDeadlineAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_Status_HoldExpiresAt] ON [Bookings] ([Status], [HoldExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BookingStatusHistories_ActorAccountId] ON [BookingStatusHistories] ([ActorAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_BookingStatusHistories_BookingId] ON [BookingStatusHistories] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CancellationPolicies_Active] ON [CancellationPolicies] ([HomestayId], [Scope]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CancellationRules_PolicyId_MinDaysBefore] ON [CancellationRules] ([PolicyId], [MinDaysBefore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contracts_ContractNo] ON [Contracts] ([ContractNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Contracts_HomestayId_Active] ON [Contracts] ([HomestayId]) WHERE [Status] = ''Active''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Contracts_SignedByEmployeeId] ON [Contracts] ([SignedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_PersonId] ON [Customers] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_PersonId] ON [Employees] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_HomestayAmenities_AmenityId] ON [HomestayAmenities] ([AmenityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HomestayImages_Cover] ON [HomestayImages] ([HomestayId]) WHERE [IsCover] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_HomestayImages_HomestayId_SortOrder] ON [HomestayImages] ([HomestayId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_HomestayImages_RoomTypeId] ON [HomestayImages] ([RoomTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Homestays_ApprovedByEmployeeId] ON [Homestays] ([ApprovedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Homestays_AreaId_Status] ON [Homestays] ([AreaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Homestays_OwnerId] ON [Homestays] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_MessageLogs_BookingId] ON [MessageLogs] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_MessageLogs_Status_CreatedAt] ON [MessageLogs] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_OwnerRegistrations_CreatedByEmployeeId] ON [OwnerRegistrations] ([CreatedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OwnerRegistrations_OwnerId_Open] ON [OwnerRegistrations] ([OwnerId]) WHERE [Status] IN (''Draft'', ''WaitingOwnerActivation'', ''PendingReview'', ''UnderReview'', ''NeedsRevision'')');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_OwnerRegistrations_ReviewedByEmployeeId] ON [OwnerRegistrations] ([ReviewedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Owners_PersonId] ON [Owners] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_BankTransactionId] ON [Payments] ([BankTransactionId]) WHERE [BankTransactionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_OneDepositPerBooking] ON [Payments] ([BookingId]) WHERE [Type] = ''Deposit'' AND [Status] <> ''Rejected''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Payments_ReviewedByEmployeeId] ON [Payments] ([ReviewedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Payments_Status_SubmittedAt] ON [Payments] ([Status], [SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Payments_SubmittedByAccountId] ON [Payments] ([SubmittedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PersonProfiles_PhoneNumber] ON [PersonProfiles] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Reviews_HomestayId] ON [Reviews] ([HomestayId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_RoomBlocks_CreatedByAccountId] ON [RoomBlocks] ([CreatedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_RoomBlocks_RoomId] ON [RoomBlocks] ([RoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoomCategories_Name] ON [RoomCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_RoomNights_BookingId] ON [RoomNights] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_RoomNights_RoomBlockId] ON [RoomNights] ([RoomBlockId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_RoomTypeId_RoomCode] ON [Rooms] ([RoomTypeId], [RoomCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoomTypePrices_RoomTypeId_DayType_EffectiveFrom] ON [RoomTypePrices] ([RoomTypeId], [DayType], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_RoomTypes_CategoryId] ON [RoomTypes] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoomTypes_HomestayId_Name] ON [RoomTypes] ([HomestayId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SettlementItems_BookingId] ON [SettlementItems] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_SettlementItems_SettlementId] ON [SettlementItems] ([SettlementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Settlements_HomestayId_Year_Month_PeriodNo] ON [Settlements] ([HomestayId], [Year], [Month], [PeriodNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_Settlements_PaidByEmployeeId] ON [Settlements] ([PaidByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_VerificationDocuments_HomestayId] ON [VerificationDocuments] ([HomestayId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    CREATE INDEX [IX_VerificationDocuments_OwnerRegistrationId] ON [VerificationDocuments] ([OwnerRegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011022005_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011022005_Init', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011044611_ReceiptReviewAlert'
)
BEGIN
    ALTER TABLE [Payments] ADD [ReviewAlertSentAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011044611_ReceiptReviewAlert'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011044611_ReceiptReviewAlert', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    ALTER TABLE [Bookings] ADD [DiscountAmount] decimal(18,0) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    ALTER TABLE [Bookings] ADD [VoucherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    CREATE TABLE [Vouchers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [CustomerId] int NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [Balance] decimal(18,0) NOT NULL,
        [ExpiresOn] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [SourceBookingId] int NULL,
        [IssuedByEmployeeId] int NOT NULL,
        [IssuedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [RevokeReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Vouchers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Vouchers_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Vouchers_Balance] CHECK ([Balance] >= 0 AND [Balance] <= [Amount]),
        CONSTRAINT [FK_Vouchers_Bookings_SourceBookingId] FOREIGN KEY ([SourceBookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_Vouchers_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vouchers_Employees_IssuedByEmployeeId] FOREIGN KEY ([IssuedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    CREATE INDEX [IX_Bookings_VoucherId] ON [Bookings] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_Discount] CHECK ([DiscountAmount] >= 0 AND [DiscountAmount] < [DepositAmount])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vouchers_Code] ON [Vouchers] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    CREATE INDEX [IX_Vouchers_CustomerId_Status] ON [Vouchers] ([CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    CREATE INDEX [IX_Vouchers_IssuedByEmployeeId] ON [Vouchers] ([IssuedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vouchers_SourceBookingId] ON [Vouchers] ([SourceBookingId]) WHERE [SourceBookingId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011062711_Vouchers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011062711_Vouchers', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [Settlements] ADD [ConfirmedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [Settlements] ADD [PaidBankRef] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [Settlements] ADD [SentAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [SettlementItems] ADD [DiscountAmount] decimal(18,0) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [BankTransactions] ADD [RefundBankRef] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    ALTER TABLE [BankTransactions] ADD [RefundedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [Settlements] ADD CONSTRAINT [CK_Settlements_Amounts] CHECK ([TotalRoomAmount] >= 0 AND [TotalRetained] >= 0 AND [TotalCommission] >= 0 AND [TotalCommission] <= [TotalRetained] AND [PayoutAmount] = [TotalRetained] - [TotalCommission])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [Settlements] ADD CONSTRAINT [CK_Settlements_Paid] CHECK ([Status] <> ''Paid'' OR [PaidAt] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [SettlementItems] ADD CONSTRAINT [CK_SettlementItems_Amounts] CHECK ([RoomAmount] >= 0 AND [DepositAmount] >= 0 AND [RefundAmount] >= 0 AND [DiscountAmount] >= 0 AND [RetainedAmount] > 0 AND [RetainedAmount] <= [DepositAmount] AND [CommissionAmount] >= 0 AND [CommissionAmount] <= [RetainedAmount] AND [PayoutAmount] = [RetainedAmount] - [CommissionAmount])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_Refund] CHECK ([RefundAmount] IS NULL OR ([RefundAmount] >= 0 AND [RefundAmount] <= [DepositAmount] - [DiscountAmount]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_Voucher] CHECK (([VoucherId] IS NULL AND [DiscountAmount] = 0) OR ([VoucherId] IS NOT NULL AND [DiscountAmount] > 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    EXEC(N'ALTER TABLE [BankTransactions] ADD CONSTRAINT [CK_BankTransactions_Refunded] CHECK ([MatchStatus] <> ''Refunded'' OR [RefundedAt] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012032051_ReviewFixes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012032051_ReviewFixes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012033411_AccountingGuards'
)
BEGIN
    CREATE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012033411_AccountingGuards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_OneRefundPerBooking] ON [Payments] ([BookingId]) WHERE [Type] = ''Refund''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012033411_AccountingGuards'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012033411_AccountingGuards', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HomestayImages]') AND [c].[name] = N'SortOrder');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [HomestayImages] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [HomestayImages] ADD DEFAULT 0 FOR [SortOrder];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [Settlements] ADD CONSTRAINT [CK_Settlements_Sent] CHECK ([Status] = ''Draft'' OR [SentAt] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [SettlementItems] ADD CONSTRAINT [CK_SettlementItems_CommissionRate] CHECK ([CommissionRate] > 0 AND [CommissionRate] < 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [Payments] ADD CONSTRAINT [CK_Payments_Receipt] CHECK ([Method] <> ''ManualReceipt'' OR ([ReceiptImageUrl] IS NOT NULL AND [ReceiptImageUrl] <> ''''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_CommissionRate] CHECK ([CommissionRate] > 0 AND [CommissionRate] < 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_DepositRate] CHECK ([DepositRate] > 0 AND [DepositRate] <= 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [BookingChangeRequests] ADD CONSTRAINT [CK_BookingChangeRequests_Status] CHECK ([Status] IN (''Pending'', ''Approved'', ''Rejected''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    EXEC(N'ALTER TABLE [BookingChangeRequests] ADD CONSTRAINT [CK_BookingChangeRequests_Type] CHECK ([RequestType] IN (''ChangeDates'', ''ChangeRoomType'', ''Cancel''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012181745_DatabaseHardening'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012181745_DatabaseHardening', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013032617_StatusConsistency'
)
BEGIN
    EXEC(N'ALTER TABLE [Bookings] ADD CONSTRAINT [CK_Bookings_ConfirmedAt] CHECK (([Status] IN (''Confirmed'', ''CheckedIn'', ''Completed'', ''NoShow'') AND [ConfirmedAt] IS NOT NULL) OR ([Status] IN (''AwaitingPayment'', ''PaymentSubmitted'', ''Deposited'', ''Rejected'', ''Expired'') AND [ConfirmedAt] IS NULL) OR [Status] = ''Cancelled'')');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013032617_StatusConsistency'
)
BEGIN
    EXEC(N'ALTER TABLE [BookingChangeRequests] ADD CONSTRAINT [CK_BookingChangeRequests_Decision] CHECK (([Status] = ''Pending'' AND [DecidedAt] IS NULL AND [DecidedByAccountId] IS NULL) OR ([Status] <> ''Pending'' AND [DecidedAt] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013032617_StatusConsistency'
)
BEGIN
    EXEC(N'ALTER TABLE [BookingChangeRequests] ADD CONSTRAINT [CK_BookingChangeRequests_Fields] CHECK (([RequestType] <> ''Cancel'' OR ([NewCheckIn] IS NULL AND [NewCheckOut] IS NULL AND [NewRoomTypeId] IS NULL)) AND ([RequestType] <> ''ChangeDates'' OR ([NewCheckIn] IS NOT NULL AND [NewCheckOut] IS NOT NULL AND [NewCheckOut] > [NewCheckIn])) AND ([RequestType] <> ''ChangeRoomType'' OR [NewRoomTypeId] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013032617_StatusConsistency'
)
BEGIN
    EXEC(N'ALTER TABLE [BookingChangeRequests] ADD CONSTRAINT [CK_BookingChangeRequests_RequestedBy] CHECK ([RequestedByType] IN (''Customer'', ''Employee''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013032617_StatusConsistency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013032617_StatusConsistency', N'10.0.12');
END;

COMMIT;
GO

