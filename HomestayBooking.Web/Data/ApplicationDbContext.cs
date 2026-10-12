using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Data;

/// <summary>
/// Dùng ApplicationUserRole (ACCOUNT_ROLE, v5 mục 4.3) thay cho IdentityUserRole mặc định,
/// nên phải khai báo đủ tham số kiểu của IdentityDbContext.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string,
    IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
    IdentityRoleClaim<string>, IdentityUserToken<string>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<PersonProfile> PersonProfiles => Set<PersonProfile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<OwnerRegistration> OwnerRegistrations => Set<OwnerRegistration>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AccountActivityLog> AccountActivityLogs => Set<AccountActivityLog>();
    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();

    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Homestay> Homestays => Set<Homestay>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HomestayAmenity> HomestayAmenities => Set<HomestayAmenity>();
    public DbSet<HomestayImage> HomestayImages => Set<HomestayImage>();

    public DbSet<RoomCategory> RoomCategories => Set<RoomCategory>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomTypePrice> RoomTypePrices => Set<RoomTypePrice>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();
    public DbSet<CancellationRule> CancellationRules => Set<CancellationRule>();

    public DbSet<RoomBlock> RoomBlocks => Set<RoomBlock>();
    public DbSet<RoomNight> RoomNights => Set<RoomNight>();

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingNight> BookingNights => Set<BookingNight>();
    public DbSet<BookingCodeCounter> BookingCodeCounters => Set<BookingCodeCounter>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();
    public DbSet<BookingChangeRequest> BookingChangeRequests => Set<BookingChangeRequest>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<SettlementItem> SettlementItems => Set<SettlementItem>();

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MessageLog> MessageLogs => Set<MessageLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Tiền VND không có phần lẻ; tỷ lệ cấu hình riêng (5,4) ở từng cột
        configurationBuilder.Properties<decimal>().HavePrecision(18, 0);

        // Enum lưu dạng chuỗi để script .sql và filtered index dễ đọc
        void AsString<TEnum>(int maxLength) where TEnum : struct, Enum =>
            configurationBuilder.Properties<TEnum>().HaveConversion<string>().HaveMaxLength(maxLength);

        AsString<BookingStatus>(20);
        AsString<SourceChannel>(20);
        AsString<PaymentType>(20);
        AsString<PaymentMethod>(20);
        AsString<PaymentStatus>(20);
        AsString<BankProvider>(20);
        AsString<BankMatchStatus>(20);
        AsString<VoucherStatus>(20);
        AsString<RoomBlockReason>(20);
        AsString<DayType>(10);
        AsString<AreaGroup>(20);
        AsString<HomestayStatus>(20);
        AsString<ContractStatus>(20);
        AsString<PolicyScope>(10);
        AsString<DocumentType>(30);
        AsString<ChangeRequestType>(20);
        AsString<ChangeRequestStatus>(20);
        AsString<SettlementStatus>(20);
        AsString<SettlementItemType>(20);
        AsString<MessageChannel>(10);
        AsString<MessageStatus>(10);
        AsString<AuditAction>(10);
        AsString<AccountStatus>(30);
        AsString<StudentVerificationStatus>(20);
        AsString<OwnerRegistrationSource>(20);
        AsString<OwnerRegistrationStatus>(30);
        AsString<EmployeePosition>(20);
        AsString<EmploymentStatus>(20);
        AsString<AccountEventType>(40);
        AsString<ActorType>(20);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- Tài khoản dùng chung (fuij_homestay_login_design_v5) ----

        builder.Entity<PersonProfile>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
            // E.164: tối đa 15 chữ số + dấu "+"
            e.Property(x => x.PhoneNumber).HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.PhoneNumber).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Address).HasMaxLength(300);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            // Một Person Profile có tối đa một Account (v5 mục 4.2)
            e.HasIndex(x => x.PersonId).IsUnique();
            e.HasOne(x => x.Person).WithOne(p => p.Account).HasForeignKey<ApplicationUser>(x => x.PersonId);
        });

        builder.Entity<ApplicationUserRole>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).IsRequired();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).IsRequired();
            e.HasOne(x => x.AssignedByAccount).WithMany().HasForeignKey(x => x.AssignedByAccountId);
        });

        builder.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.PersonId).IsUnique();
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasOne(x => x.Person).WithOne(p => p.Customer).HasForeignKey<Customer>(x => x.PersonId);
        });

        builder.Entity<CustomerProfile>(e =>
        {
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.CitizenId).HasMaxLength(20);
            e.Property(x => x.StudentId).HasMaxLength(30);
            e.HasOne(x => x.Customer).WithOne(c => c.Profile).HasForeignKey<CustomerProfile>(x => x.CustomerId);
        });

        builder.Entity<Owner>(e =>
        {
            e.HasIndex(x => x.PersonId).IsUnique();
            e.Property(x => x.BankName).HasMaxLength(100);
            e.Property(x => x.BankAccountNo).HasMaxLength(30);
            e.Property(x => x.BankAccountName).HasMaxLength(100);
            e.HasOne(x => x.Person).WithOne(p => p.Owner).HasForeignKey<Owner>(x => x.PersonId);
        });

        builder.Entity<OwnerRegistration>(e =>
        {
            e.Property(x => x.ReviewNote).HasMaxLength(1000);
            // Chỉ một hồ sơ đang xử lý cho mỗi Owner (v5 mục 4.7); hồ sơ đã duyệt/từ chối giữ làm lịch sử
            var open = string.Join(", ", OwnerRegistration.OpenStatuses.Select(s => $"'{s}'"));
            e.HasIndex(x => x.OwnerId).IsUnique().HasFilter($"[Status] IN ({open})")
                .HasDatabaseName("IX_OwnerRegistrations_OwnerId_Open");
            e.HasOne(x => x.Owner).WithMany(o => o.Registrations).HasForeignKey(x => x.OwnerId);
            e.HasOne(x => x.CreatedByEmployee).WithMany().HasForeignKey(x => x.CreatedByEmployeeId);
            e.HasOne(x => x.ReviewedByEmployee).WithMany().HasForeignKey(x => x.ReviewedByEmployeeId);
        });

        builder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.PersonId).IsUnique();
            e.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.Property(x => x.Department).HasMaxLength(100);
            e.HasOne(x => x.Person).WithOne(p => p.Employee).HasForeignKey<Employee>(x => x.PersonId);
        });

        builder.Entity<AccountActivityLog>(e =>
        {
            e.Property(x => x.ActorId).HasMaxLength(450);
            e.Property(x => x.Detail).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasIndex(x => new { x.AccountId, x.CreatedAt });
            e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId);
        });

        builder.Entity<VerificationDocument>(e =>
        {
            e.Property(x => x.FileUrl).HasMaxLength(300).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.ToTable(t => t.HasCheckConstraint("CK_VerificationDocuments_Owner",
                "([OwnerRegistrationId] IS NOT NULL AND [HomestayId] IS NULL) OR ([OwnerRegistrationId] IS NULL AND [HomestayId] IS NOT NULL)"));
            e.HasOne(x => x.OwnerRegistration).WithMany(r => r.Documents).HasForeignKey(x => x.OwnerRegistrationId);
            e.HasOne(x => x.Homestay).WithMany().HasForeignKey(x => x.HomestayId);
        });

        // ---- B. Đối tác và hợp đồng ----

        builder.Entity<Area>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Province, x.Name }).IsUnique();
        });

        builder.Entity<Homestay>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(300).IsRequired();
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.Property(x => x.ContactPhone).HasMaxLength(16);
            e.HasIndex(x => new { x.AreaId, x.Status });
            e.HasOne(x => x.Owner).WithMany(o => o.Homestays).HasForeignKey(x => x.OwnerId);
            e.HasOne(x => x.Area).WithMany(a => a.Homestays).HasForeignKey(x => x.AreaId);
            e.HasOne(x => x.ApprovedByEmployee).WithMany().HasForeignKey(x => x.ApprovedByEmployeeId);
        });

        builder.Entity<Contract>(e =>
        {
            e.Property(x => x.ContractNo).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.ContractNo).IsUnique();
            e.Property(x => x.CommissionRate).HasPrecision(5, 4);
            // Mỗi homestay tối đa một hợp đồng đang hiệu lực
            e.HasIndex(x => x.HomestayId).IsUnique()
                .HasFilter($"[Status] = '{ContractStatus.Active}'")
                .HasDatabaseName("IX_Contracts_HomestayId_Active");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Contracts_Dates", "[EndDate] > [StartDate]");
                t.HasCheckConstraint("CK_Contracts_CommissionRate", "[CommissionRate] > 0 AND [CommissionRate] < 1");
            });
            e.HasOne(x => x.Homestay).WithMany(h => h.Contracts).HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.SignedByEmployee).WithMany().HasForeignKey(x => x.SignedByEmployeeId);
        });

        builder.Entity<Amenity>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<HomestayAmenity>(e =>
        {
            e.HasKey(x => new { x.HomestayId, x.AmenityId });
            e.HasOne(x => x.Homestay).WithMany(h => h.HomestayAmenities).HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.Amenity).WithMany(a => a.HomestayAmenities).HasForeignKey(x => x.AmenityId);
        });

        builder.Entity<HomestayImage>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(300).IsRequired();
            // Mỗi homestay một ảnh bìa
            e.HasIndex(x => x.HomestayId).IsUnique().HasFilter("[IsCover] = 1").HasDatabaseName("IX_HomestayImages_Cover");
            e.HasIndex(x => new { x.HomestayId, x.SortOrder });
            e.HasOne(x => x.Homestay).WithMany(h => h.Images).HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.RoomType).WithMany(t => t.Images).HasForeignKey(x => x.RoomTypeId);
        });

        // ---- C. Phòng và giá ----

        builder.Entity<RoomCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<RoomType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => new { x.HomestayId, x.Name }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_RoomTypes_MaxGuests", "[MaxGuests] > 0"));
            e.HasOne(x => x.Homestay).WithMany(h => h.RoomTypes).HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.Category).WithMany(c => c.RoomTypes).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<Room>(e =>
        {
            e.Property(x => x.RoomCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.RoomTypeId, x.RoomCode }).IsUnique();
            e.HasOne(x => x.RoomType).WithMany(t => t.Rooms).HasForeignKey(x => x.RoomTypeId);
        });

        builder.Entity<RoomTypePrice>(e =>
        {
            e.HasIndex(x => new { x.RoomTypeId, x.DayType, x.EffectiveFrom }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_RoomTypePrices_Price", "[Price] > 0"));
            e.HasOne(x => x.RoomType).WithMany(t => t.Prices).HasForeignKey(x => x.RoomTypeId);
        });

        builder.Entity<Holiday>(e =>
        {
            e.HasKey(x => x.Date);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        builder.Entity<CancellationPolicy>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            // Một chính sách đang dùng cho mỗi (homestay, phạm vi). SQL Server coi các NULL là trùng nhau
            // nên cũng chỉ có một chính sách chung (HomestayId NULL) đang dùng cho mỗi phạm vi.
            e.HasIndex(x => new { x.HomestayId, x.Scope }).IsUnique()
                .HasFilter("[IsActive] = 1").HasDatabaseName("IX_CancellationPolicies_Active");
            e.HasOne(x => x.Homestay).WithMany(h => h.CancellationPolicies).HasForeignKey(x => x.HomestayId);
        });

        builder.Entity<CancellationRule>(e =>
        {
            e.HasIndex(x => new { x.PolicyId, x.MinDaysBefore }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CancellationRules_Days", "[MinDaysBefore] >= 0");
                t.HasCheckConstraint("CK_CancellationRules_Percent", "[RefundPercent] BETWEEN 0 AND 100");
            });
        });

        // ---- D. Lịch phòng và chống trùng phòng ----

        builder.Entity<RoomBlock>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(255);
            e.ToTable(t => t.HasCheckConstraint("CK_RoomBlocks_Dates", "[EndDate] > [StartDate]"));
            e.HasOne(x => x.Room).WithMany(r => r.Blocks).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId);
        });

        builder.Entity<RoomNight>(e =>
        {
            // Cốt lõi chống trùng phòng (BR-05, NFR-01): mỗi phòng mỗi đêm chỉ một dòng
            e.HasKey(x => new { x.RoomId, x.Date });
            e.HasIndex(x => x.BookingId);
            e.HasIndex(x => x.RoomBlockId);
            e.ToTable(t => t.HasCheckConstraint("CK_RoomNights_Owner",
                "([BookingId] IS NOT NULL AND [RoomBlockId] IS NULL) OR ([BookingId] IS NULL AND [RoomBlockId] IS NOT NULL)"));
            e.HasOne(x => x.Room).WithMany(r => r.RoomNights).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.Booking).WithMany(b => b.RoomNights).HasForeignKey(x => x.BookingId);
        });

        builder.Entity<Voucher>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.RevokeReason).HasMaxLength(500);
            e.Property(x => x.Balance).IsConcurrencyToken();
            e.HasIndex(x => new { x.CustomerId, x.Status });
            // Mỗi đơn bị làm hỏng chỉ được cấp một voucher
            e.HasIndex(x => x.SourceBookingId).IsUnique().HasFilter("[SourceBookingId] IS NOT NULL");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Vouchers_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_Vouchers_Balance", "[Balance] >= 0 AND [Balance] <= [Amount]");
            });
            e.HasOne(x => x.Customer).WithMany(c => c.Vouchers).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.SourceBooking).WithMany().HasForeignKey(x => x.SourceBookingId);
            e.HasOne(x => x.IssuedByEmployee).WithMany().HasForeignKey(x => x.IssuedByEmployeeId);
        });

        // ---- E. Đơn đặt phòng ----

        builder.Entity<Booking>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.ContactName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(16).IsRequired();
            e.Property(x => x.SpecialRequests).HasMaxLength(1000);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.Property(x => x.DepositRate).HasPrecision(5, 4);
            e.Property(x => x.CommissionRate).HasPrecision(5, 4);
            // Hai thao tác đổi trạng thái cùng lúc (job hết hạn và tiền về) → chỉ một bên thành công
            e.Property(x => x.Status).IsConcurrencyToken();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => new { x.HomestayId, x.CheckIn });
            e.HasIndex(x => new { x.Status, x.HoldExpiresAt });
            e.HasIndex(x => new { x.Status, x.ConfirmDeadlineAt });
            e.HasIndex(x => new { x.AssignedEmployeeId, x.Status });
            e.HasIndex(x => x.CheckOut);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_Dates", "[CheckOut] > [CheckIn]");
                t.HasCheckConstraint("CK_Bookings_Guests", "[Guests] > 0");
                t.HasCheckConstraint("CK_Bookings_Amounts", "[TotalAmount] > 0 AND [DepositAmount] >= 0 AND [DepositAmount] <= [TotalAmount]");
                // Voucher chỉ trừ vào cọc và khách luôn phải chuyển một khoản > 0
                t.HasCheckConstraint("CK_Bookings_Discount", "[DiscountAmount] >= 0 AND [DiscountAmount] < [DepositAmount]");
                // Có giảm giá thì phải biết voucher nào; gắn voucher thì phải giảm được > 0
                t.HasCheckConstraint("CK_Bookings_Voucher",
                    "([VoucherId] IS NULL AND [DiscountAmount] = 0) OR ([VoucherId] IS NOT NULL AND [DiscountAmount] > 0)");
                // Hoàn tối đa số tiền khách thực chuyển (BR-04, mục 7.2)
                t.HasCheckConstraint("CK_Bookings_Refund",
                    "[RefundAmount] IS NULL OR ([RefundAmount] >= 0 AND [RefundAmount] <= [DepositAmount] - [DiscountAmount])");
            });
            e.HasOne(x => x.Voucher).WithMany(v => v.UsedByBookings).HasForeignKey(x => x.VoucherId);
            e.HasOne(x => x.Customer).WithMany(c => c.Bookings).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.CreatedByEmployee).WithMany().HasForeignKey(x => x.CreatedByEmployeeId);
            e.HasOne(x => x.AssignedEmployee).WithMany().HasForeignKey(x => x.AssignedEmployeeId);
            e.HasOne(x => x.Homestay).WithMany().HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.RoomType).WithMany().HasForeignKey(x => x.RoomTypeId);
            e.HasOne(x => x.Room).WithMany(r => r.Bookings).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.CancellationPolicy).WithMany().HasForeignKey(x => x.CancellationPolicyId);
            e.HasOne(x => x.ReplacedByBooking).WithMany().HasForeignKey(x => x.ReplacedByBookingId);
        });

        builder.Entity<BookingNight>(e =>
        {
            e.HasKey(x => new { x.BookingId, x.Date });
            e.ToTable(t => t.HasCheckConstraint("CK_BookingNights_Price", "[Price] > 0"));
            e.HasOne(x => x.Booking).WithMany(b => b.Nights).HasForeignKey(x => x.BookingId);
        });

        builder.Entity<BookingCodeCounter>(e => e.HasKey(x => x.Date));

        builder.Entity<BookingStatusHistory>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.Booking).WithMany(b => b.Histories).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.ActorAccount).WithMany().HasForeignKey(x => x.ActorAccountId);
        });

        builder.Entity<BookingChangeRequest>(e =>
        {
            e.Property(x => x.DecisionNote).HasMaxLength(500);
            e.HasOne(x => x.Booking).WithMany(b => b.ChangeRequests).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.NewRoomType).WithMany().HasForeignKey(x => x.NewRoomTypeId);
            e.HasOne(x => x.DecidedByAccount).WithMany().HasForeignKey(x => x.DecidedByAccountId);
        });

        // ---- F. Tiền ----

        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.ReceiptImageUrl).HasMaxLength(300);
            e.Property(x => x.TransferContent).HasMaxLength(255);
            e.Property(x => x.BankRef).HasMaxLength(100);
            e.Property(x => x.RejectReason).HasMaxLength(500);
            e.HasIndex(x => x.BookingId);
            e.HasIndex(x => new { x.Status, x.SubmittedAt });
            // Một giao dịch ngân hàng chỉ thành một khoản thanh toán
            e.HasIndex(x => x.BankTransactionId).IsUnique().HasFilter("[BankTransactionId] IS NOT NULL");
            // Mỗi đơn tối đa một khoản cọc đang chờ duyệt hoặc đã duyệt; bill bị từ chối giữ làm lịch sử
            e.HasIndex(x => x.BookingId).IsUnique()
                .HasFilter($"[Type] = '{PaymentType.Deposit}' AND [Status] <> '{PaymentStatus.Rejected}'")
                .HasDatabaseName("IX_Payments_OneDepositPerBooking");
            e.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
            e.HasOne(x => x.Booking).WithMany(b => b.Payments).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.BankTransaction).WithOne(t => t.Payment).HasForeignKey<Payment>(x => x.BankTransactionId);
            e.HasOne(x => x.SubmittedByAccount).WithMany().HasForeignKey(x => x.SubmittedByAccountId);
            e.HasOne(x => x.ReviewedByEmployee).WithMany().HasForeignKey(x => x.ReviewedByEmployeeId);
        });

        builder.Entity<BankTransaction>(e =>
        {
            e.Property(x => x.ProviderTransactionId).HasMaxLength(100).IsRequired();
            e.Property(x => x.BankReference).HasMaxLength(100);
            e.Property(x => x.AccountNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.TransferContent).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            // Webhook gửi lặp không bị ghi nhận hai lần
            e.HasIndex(x => new { x.Provider, x.ProviderTransactionId }).IsUnique();
            e.HasIndex(x => new { x.MatchStatus, x.TransactionTime });
            e.HasIndex(x => x.MatchedBookingId);
            e.Property(x => x.RefundBankRef).HasMaxLength(100);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_BankTransactions_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_BankTransactions_Refunded",
                    $"[MatchStatus] <> '{BankMatchStatus.Refunded}' OR [RefundedAt] IS NOT NULL");
            });
            e.HasOne(x => x.MatchedBooking).WithMany().HasForeignKey(x => x.MatchedBookingId);
            e.HasOne(x => x.ProcessedByEmployee).WithMany().HasForeignKey(x => x.ProcessedByEmployeeId);
        });

        builder.Entity<Settlement>(e =>
        {
            e.HasIndex(x => new { x.HomestayId, x.Year, x.Month, x.PeriodNo }).IsUnique();
            e.Property(x => x.PaidBankRef).HasMaxLength(100);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Settlements_Period", "[PeriodNo] IN (1, 2) AND [Month] BETWEEN 1 AND 12");
                t.HasCheckConstraint("CK_Settlements_Amounts",
                    "[TotalRoomAmount] >= 0 AND [TotalRetained] >= 0 AND [TotalCommission] >= 0 AND [TotalCommission] <= [TotalRetained]"
                    + " AND [PayoutAmount] = [TotalRetained] - [TotalCommission]");
                t.HasCheckConstraint("CK_Settlements_Paid", $"[Status] <> '{SettlementStatus.Paid}' OR [PaidAt] IS NOT NULL");
            });
            e.HasOne(x => x.Homestay).WithMany(h => h.Settlements).HasForeignKey(x => x.HomestayId);
            e.HasOne(x => x.PaidByEmployee).WithMany().HasForeignKey(x => x.PaidByEmployeeId);
        });

        builder.Entity<SettlementItem>(e =>
        {
            e.Property(x => x.CommissionRate).HasPrecision(5, 4);
            e.ToTable(t => t.HasCheckConstraint("CK_SettlementItems_Amounts",
                "[RoomAmount] >= 0 AND [DepositAmount] >= 0 AND [RefundAmount] >= 0 AND [DiscountAmount] >= 0"
                + " AND [RetainedAmount] > 0 AND [RetainedAmount] <= [DepositAmount]"
                + " AND [CommissionAmount] >= 0 AND [CommissionAmount] <= [RetainedAmount]"
                + " AND [PayoutAmount] = [RetainedAmount] - [CommissionAmount]"));
            // Một đơn chỉ vào một kỳ đối soát
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasOne(x => x.Booking).WithOne(b => b.SettlementItem).HasForeignKey<SettlementItem>(x => x.BookingId);
        });

        // ---- G. Đánh giá, thông báo, nhật ký ----

        builder.Entity<Review>(e =>
        {
            // Mỗi đơn chỉ một đánh giá (BR-09)
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasIndex(x => x.HomestayId);
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.Property(x => x.OwnerReply).HasMaxLength(1000);
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
            e.HasOne(x => x.Booking).WithOne(b => b.Review).HasForeignKey<Review>(x => x.BookingId);
            e.HasOne(x => x.Homestay).WithMany().HasForeignKey(x => x.HomestayId);
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.Url).HasMaxLength(300);
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        builder.Entity<MessageLog>(e =>
        {
            e.Property(x => x.RecipientPhone).HasMaxLength(16);
            e.Property(x => x.RecipientEmail).HasMaxLength(256);
            e.Property(x => x.TemplateCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.TableName).HasMaxLength(100).IsRequired();
            e.Property(x => x.RecordId).HasMaxLength(100).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasIndex(x => new { x.TableName, x.RecordId });
            e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId);
        });

        // Không xóa cứng dữ liệu nghiệp vụ, và SQL Server không cho nhiều đường cascade tới cùng một bảng:
        // mặc định chặn xóa (Restrict) cho mọi khóa ngoại, trừ bảng của Identity.
        foreach (var fk in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var declaringType = fk.DeclaringEntityType.ClrType;
            if (fk.DeleteBehavior == DeleteBehavior.Cascade
                && declaringType != typeof(ApplicationUserRole)
                && declaringType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") != true)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        // Chỉ cascade ở bảng con thuần túy (mỗi bảng chỉ một đường cascade):
        // ảnh, bảng nối tiện nghi, quy tắc hủy, đêm của đơn, các đêm khóa của một RoomBlock, dòng đối soát.
        builder.Entity<HomestayImage>().HasOne(x => x.Homestay).WithMany(h => h.Images).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<HomestayAmenity>().HasOne(x => x.Homestay).WithMany(h => h.HomestayAmenities).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<HomestayAmenity>().HasOne(x => x.Amenity).WithMany(a => a.HomestayAmenities).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CancellationRule>().HasOne(x => x.Policy).WithMany(p => p.Rules).HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<BookingNight>().HasOne(x => x.Booking).WithMany(b => b.Nights).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<RoomNight>().HasOne(x => x.RoomBlock).WithMany(b => b.Nights).HasForeignKey(x => x.RoomBlockId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SettlementItem>().HasOne(x => x.Settlement).WithMany(s => s.Items).HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Cascade);
    }
}
