namespace HomestayBooking.Web.Models.Entities;

/// <summary>Vòng đời đơn đặt phòng (fuji_homestay_database_v1 mục 6).</summary>
public enum BookingStatus
{
    AwaitingPayment,  // Chờ thanh toán — giữ phòng 15 phút
    PaymentSubmitted, // Chờ duyệt cọc — khách đã tải ảnh bill
    Deposited,        // Đã đặt cọc — chờ homestay xác nhận (≤ 30 phút)
    Confirmed,        // Đã xác nhận
    CheckedIn,        // Đã nhận phòng
    Completed,        // Hoàn thành
    Expired,          // Hết hạn giữ phòng
    Rejected,         // Homestay từ chối
    Cancelled,        // Đã hủy
    NoShow            // Không đến
}

/// <summary>Kênh khách liên hệ đặt phòng.</summary>
public enum SourceChannel
{
    Web,
    Phone,
    Zalo,
    Messenger,
    Other
}

public enum PaymentType
{
    Deposit,      // Cọc
    Refund,       // Hoàn cọc
    Compensation  // Fuji bù chênh lệch khi trùng phòng (BR-11)
}

public enum PaymentMethod
{
    BankAutoMatch,  // Tự khớp từ giao dịch ngân hàng (QR MB)
    ManualReceipt,  // Ảnh bill, kế toán duyệt
    ManualTransfer  // Fuji chuyển ra: hoàn cọc, bù chênh lệch
}

public enum PaymentStatus
{
    Submitted,
    Approved,
    Rejected
}

public enum BankProvider
{
    SePay,
    Casso,
    Simulator // Giả lập khi demo
}

public enum BankMatchStatus
{
    Matched,
    Underpaid,
    LatePayment,
    Unmatched,
    ManuallyMatched,
    Refunded,
    Ignored
}

public enum RoomBlockReason
{
    OtherChannel, // Khách đặt qua kênh khác
    Maintenance   // Bảo trì
}

public enum DayType
{
    Weekday,
    Weekend,
    Holiday
}

public enum AreaGroup
{
    InnerCity,
    Suburb,
    OtherProvince
}

public enum HomestayStatus
{
    Draft,
    PendingReview,
    Active,
    Suspended,
    Ended
}

public enum ContractStatus
{
    Active,
    Expired,
    Terminated
}

/// <summary>Phạm vi chính sách hủy: mọi ngày, hoặc đơn có đêm lễ.</summary>
public enum PolicyScope
{
    All,
    Holiday
}

public enum DocumentType
{
    BusinessLicense, // Giấy phép kinh doanh lưu trú
    FireSafety,      // Giấy chứng nhận PCCC
    PropertyPhoto,   // Ảnh thực tế
    Other
}

public enum ChangeRequestType
{
    ChangeDates,
    ChangeRoomType,
    Cancel
}

public enum ChangeRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public enum SettlementStatus
{
    Draft,
    Sent,
    Confirmed,
    Paid
}

public enum SettlementItemType
{
    Completed,
    CancelledRetained,
    NoShow
}

public enum MessageChannel
{
    Sms,
    Zalo,
    Email
}

public enum MessageStatus
{
    Pending,
    Sent,
    Failed
}

public enum AuditAction
{
    Insert,
    Update,
    Delete
}

// ---- Tài khoản dùng chung (fuij_homestay_login_design_v5) ----

/// <summary>Trạng thái ACCOUNT (v5 mục 4.2).</summary>
public enum AccountStatus
{
    PendingActivation, // Đã tạo nhưng chủ tài khoản chưa tự đặt mật khẩu
    Active,
    Locked,            // Khóa do vi phạm / nghi ngờ bị chiếm
    Disabled           // Ngừng sử dụng (ví dụ nhân viên nghỉ việc)
}

/// <summary>Trạng thái xác minh thẻ sinh viên (v5 mục 4.5).</summary>
public enum StudentVerificationStatus
{
    NotSubmitted,
    Pending,
    Verified,
    Rejected
}

/// <summary>Nguồn tạo hồ sơ đăng ký Owner (v5 mục 4.7).</summary>
public enum OwnerRegistrationSource
{
    SelfService,
    EmployeeAssisted
}

/// <summary>Trạng thái hồ sơ đăng ký Owner (v5 mục 4.7).</summary>
public enum OwnerRegistrationStatus
{
    Draft,
    WaitingOwnerActivation,
    PendingReview,
    UnderReview,
    NeedsRevision,
    Approved,
    Rejected
}

/// <summary>Chức vụ nhân viên. Quản lý vẫn là Employee, quyền mở rộng theo chức vụ (v5 mục 1.7, 4.8).</summary>
public enum EmployeePosition
{
    Consultant,    // Nhân viên tư vấn đặt phòng
    Accountant,    // Kế toán
    Manager,       // Quản lý
    Administrator  // Quản trị hệ thống
}

public enum EmploymentStatus
{
    Active,
    Suspended,
    Resigned
}

/// <summary>Loại sự kiện trong ACCOUNT_ACTIVITY_LOG (v5 mục 4.10).</summary>
public enum AccountEventType
{
    AccountCreated,
    AccountActivated,
    RoleAssigned,
    GoogleLinked,
    PasswordChanged,
    PasswordReset,
    PhoneChanged,
    StatusChanged,
    OwnerRegistrationSubmitted,
    OwnerApproved,
    OwnerRejected
}

public enum ActorType
{
    Customer,
    Owner,
    Employee,
    System
}

public enum VoucherStatus
{
    Active,   // Còn số dư và còn hạn
    UsedUp,   // Số dư = 0 (có thể Active lại nếu đơn dùng voucher bị hủy)
    Expired,
    Revoked   // Quản lý thu hồi
}
