# Fuji Homestay — Thiết kế Database v1.0

> **Phiên bản:** 1.3 — 2026-10-11 (giữ phòng 15 phút khi đặt; cọc qua QR **MB Bank** tự đối soát, ảnh bill làm dự phòng; **đã áp vào code**, migration `Init`)
> **Trạng thái:** Đã chốt các quyết định ở mục 1; dùng làm cơ sở viết ERD và sửa code (EF Core Code First).
> **Nguồn:**
> - `fuij_homestay_login_design_v5.md` → toàn bộ module **Tài khoản** (đã triển khai trong code).
> - `fuij_homestay_database_draft.md` v0.2 → phần lớn module **Nghiệp vụ** (hợp đồng, giá, chính sách hủy, đơn, đối soát).
> - Code hiện tại (`HomestayBooking.Web/Data/ApplicationDbContext.cs`) → cơ chế chống trùng phòng `RoomNight`, danh mục.
> - `phantichyeucaubanchinh.docx` → **chỉ tham khảo** (mã BR/FR ghi trong tài liệu này để truy vết).

---

## Mục lục

1. [Các quyết định đã chốt](#1-các-quyết-định-đã-chốt)
2. [Quy ước](#2-quy-ước)
3. [Sơ đồ quan hệ](#3-sơ-đồ-quan-hệ)
4. [Danh sách bảng](#4-danh-sách-bảng)
5. [Chi tiết từng bảng](#5-chi-tiết-từng-bảng)
6. [Vòng đời đơn đặt phòng và các mốc thời gian](#6-vòng-đời-đơn-đặt-phòng-và-các-mốc-thời-gian)
7. [Công thức tiền](#7-công-thức-tiền)
8. [Quy tắc nghiệp vụ được thực thi ở đâu](#8-quy-tắc-nghiệp-vụ-được-thực-thi-ở-đâu)
9. [Phân quyền dữ liệu](#9-phân-quyền-dữ-liệu)
10. [Chỉ mục](#10-chỉ-mục)
11. [Thay đổi so với code hiện tại](#11-thay-đổi-so-với-code-hiện-tại)
12. [Câu hỏi còn mở](#12-câu-hỏi-còn-mở)

---

## 1. Các quyết định đã chốt

| # | Quyết định | Ảnh hưởng tới database |
|---|---|---|
| D1 | Tài khoản theo **v5**: một `PersonProfile` – tối đa một `Account` – nhiều vai trò (Customer / Owner / Employee) | Module A |
| D2 | **Một đơn đặt đúng một phòng** | `Booking` có `RoomTypeId` + `RoomId`; **không** có bảng `BookingRoom` |
| D3 | **Giá theo loại phòng**, 3 mức (ngày thường / cuối tuần / lễ), có lịch sử | `RoomType`, `RoomTypePrice`; bỏ cột giá ở `Room` |
| D4 | **Giữ phòng ngay khi đặt**: hệ thống chọn một phòng trống của loại phòng khách chọn và chiếm lịch ngay. Hết phòng → không đặt được (không còn cảnh nhiều đơn chờ tranh một phòng) | `RoomNight` có từ lúc đơn được tạo; `Booking.RoomId` NOT NULL |
| D5 | **Giữ phòng 15 phút để thanh toán**: trong 15 phút khách phải chuyển khoản cọc và **tải ảnh bill** lên; quá hạn chưa có bill → tự hủy, mở lại phòng | `Booking.HoldExpiresAt` (mục 6.2) |
| D11 | **Ảnh bill là phương án dự phòng**: khi không tự khớp được (khách không dùng QR, ghi sai nội dung, chuyển thiếu), khách tải ảnh bill; kế toán (hoặc quản lý) xem ảnh và duyệt / từ chối. Có bill thì vẫn giữ phòng trong lúc chờ duyệt | `Payment.Method = ManualReceipt`, `Payment.ReceiptImageUrl` |
| D13 | **Cọc qua mã QR chuyển khoản (VietQR) + tự đối soát**: QR điền sẵn số tiền cọc và nội dung = mã đơn; dịch vụ theo dõi tài khoản (SePay / Casso) báo giao dịch về hệ thống (webhook); hệ thống tự khớp mã đơn + số tiền → **Đã đặt cọc ngay, 24/7**, không cần người duyệt. Tiền vào thẳng tài khoản Fuji — **không phải cổng thanh toán**. Trong BTL, việc ngân hàng báo về được **giả lập** | Bảng `BankTransactions`; `Payment.Method = BankAutoMatch` |
| D12 | **Homestay xác nhận sau khi đã có cọc**, trong 30 phút; homestay từ chối → hoàn 100% cọc | `Booking.ConfirmDeadlineAt = DepositedAt + 30'` |
| D6 | **Hoa hồng thuộc Hợp đồng**, không thuộc Homestay | Bảng `Contract`; đơn lưu snapshot `CommissionRate` |
| D7 | Đơn được xếp vào **kỳ đối soát theo ngày trả phòng** (`CheckOut`) | `Settlement`, `SettlementItem` |
| D8 | **Tiền cọc không được hoàn** (hủy muộn, không đến): Fuji giữ hoa hồng, homestay nhận phần còn lại — tính như đơn hoàn thành | Công thức mục 7.3 |
| D9 | Homestay có **30 phút** để xác nhận (BR-06), tính từ khi cọc được duyệt (D12); quá hạn → cảnh báo nhân viên, đơn không tự đổi trạng thái | `Booking.ConfirmDeadlineAt` |
| D10 | Chính sách hủy riêng của homestay **được ưu tiên** hơn chính sách chung của Fuji | `CancellationPolicy`, `CancellationRule` |

---

## 2. Quy ước

| Hạng mục | Quy ước |
|---|---|
| DBMS | **SQL Server** (chạy bằng Docker trên macOS), truy cập qua **EF Core Code First + Migrations** |
| Tên bảng / cột | Tiếng Anh, `PascalCase`, tên bảng số nhiều theo quy ước EF (`Bookings`, `RoomTypes`) |
| Khóa chính | `Id int IDENTITY`; riêng `AccountActivityLog`, `AuditLog`, `MessageLog` dùng `bigint`; `Account.Id` là chuỗi GUID của Identity |
| Mã nghiệp vụ | Cột riêng có `UNIQUE` (mã đơn, mã nhân viên, số hợp đồng) — không dùng làm khóa chính |
| Tiền | `decimal(18,0)`, đơn vị **đồng** |
| Tỷ lệ | `decimal(5,4)`, ví dụ `0.1200` = 12%; riêng `RefundPercent` là `int` 0–100 |
| Ngày ở | `date` (`DateOnly`) |
| Thời điểm | `datetime2`, lưu **UTC**, đổi sang giờ Việt Nam khi hiển thị |
| Giá trị liệt kê (enum) | Lưu dạng **chuỗi** (`nvarchar(20–40)`) để script SQL dễ đọc |
| `CreatedAt`, `UpdatedAt` | Có ở mọi bảng nghiệp vụ |
| Số điện thoại | Chuẩn hóa **E.164** (`+84912345678`) trước khi lưu và tra cứu |
| Xóa dữ liệu | **Không xóa cứng** đơn, thanh toán, đối soát, hợp đồng, hồ sơ người; đổi trạng thái. Chỉ xóa cứng: ảnh, liên kết tiện nghi, `RoomNight`, `RoomBlock` |
| Xóa dây chuyền (cascade) | Chỉ ở bảng con thuần túy (ảnh, bảng nối, `RoomBlock → RoomNight`, bảng Identity). Còn lại `Restrict` — SQL Server không cho nhiều đường cascade tới cùng một bảng |
| Cuối tuần | Đêm **thứ Sáu và thứ Bảy** tính giá cuối tuần (quy ước của nhóm) |
| Cấu hình không lưu DB | Tài khoản nhận tiền của Fuji (ngân hàng, số TK, tên), khóa API webhook, thời gian giữ phòng, tỷ lệ cọc mặc định → `appsettings.json` / user-secrets |

---

## 3. Sơ đồ quan hệ

```text
─────────────────────────── A. TÀI KHOẢN (v5) ───────────────────────────
                         ┌── 0..1 Customer ── 1 CustomerProfile
PersonProfile ───────────┼── 0..1 Owner ──< OwnerRegistration ──< VerificationDocument
   │ 0..1                └── 0..1 Employee                              (giấy phép, PCCC, ảnh)
Account ──< AccountRole >── Role
   ├──< AccountActivityLog
   └──< Notification

─────────────────────────── B–C. ĐỐI TÁC, PHÒNG, GIÁ ────────────────────
Area ──< Homestay >── Owner
           ├──< Contract                         (hoa hồng, thời hạn)
           ├──< HomestayImage
           ├──< HomestayAmenity >── Amenity
           ├──< CancellationPolicy ──< CancellationRule   (HomestayId NULL = chính sách chung)
           ├──< VerificationDocument             (giấy tờ của từng homestay)
           └──< RoomType >── RoomCategory        (danh mục chung để lọc)
                  ├──< RoomTypePrice             (3 mức giá × ngày hiệu lực)
                  └──< Room ──< RoomBlock
                        └──< RoomNight >──┐      (PK RoomId + Date → chống trùng)
Holiday                                    │
─────────────────────────── D–F. ĐƠN, TIỀN ─────┼─────────────────────────
Customer ──< Booking ─────────────────────┘
               ├──< BookingNight                 (giá từng đêm đã chốt)
               ├──< BookingStatusHistory
               ├──< BookingChangeRequest
               ├──< Payment ──o BankTransaction  (giao dịch ngân hàng báo về, tự khớp)
               ├──o Review
               └──o SettlementItem >── Settlement >── Homestay
BookingCodeCounter                               (sinh mã đơn)

─────────────────────────── G. NHẬT KÝ ───────────────────────────────────
MessageLog (SMS / Zalo / Email gửi ra ngoài)     AuditLog (thay đổi dữ liệu nhạy cảm)
BankTransaction ──o Booking (MatchedBookingId: kể cả giao dịch đến muộn, chưa thành Payment)
```

`──<` một – nhiều  `──o` một – không hoặc một  `>──` nhiều – một

---

## 4. Danh sách bảng

⭐ = bắt buộc cho BTL  ○ = nên có, làm nếu kịp

| Module | Bảng | | Trạng thái so với code |
|---|---|---|---|
| **A. Tài khoản** | `PersonProfiles` | ⭐ | Đã có |
| | `AspNetUsers` (Account) | ⭐ | Đã có |
| | `AspNetRoles`, `AspNetUserRoles` (AccountRole) | ⭐ | Đã có |
| | `Customers`, `CustomerProfiles` | ⭐ | Đã có, **thêm** `Customers.Note` |
| | `Owners` | ⭐ | Đã có, **thêm** thông tin ngân hàng |
| | `OwnerRegistrations` | ⭐ | Đã có |
| | `VerificationDocuments` | ⭐ | **Mới** |
| | `Employees` | ⭐ | Đã có |
| | `AccountActivityLogs` | ⭐ | Đã có |
| **B. Đối tác** | `Areas` | ⭐ | Sửa: thêm `Province`, `AreaGroup` |
| | `Homestays` | ⭐ | Sửa: `Status` thay `IsActive`; bỏ `CommissionRate`; thêm tọa độ, hướng dẫn nhận phòng, SĐT liên hệ |
| | `Contracts` | ⭐ | **Mới** |
| | `Amenities`, `HomestayAmenities` | ⭐ | Giữ |
| | `HomestayImages` | ⭐ | Sửa: thêm `RoomTypeId`, `SortOrder`; **gộp** `RoomImages` vào đây |
| **C. Phòng, giá** | `RoomCategories` | ⭐ | Đổi tên từ `RoomTypes` hiện tại |
| | `RoomTypes` | ⭐ | **Mới** (loại phòng của từng homestay) |
| | `Rooms` | ⭐ | Sửa: thuộc `RoomType`, bỏ cột giá |
| | `RoomTypePrices` | ⭐ | **Mới** |
| | `Holidays` | ⭐ | Giữ |
| | `CancellationPolicies`, `CancellationRules` | ⭐ | **Mới** |
| **D. Lịch phòng** | `RoomBlocks` | ⭐ | **Mới** |
| | `RoomNights` | ⭐ | Sửa: `BlockReason` → `RoomBlockId`; khóa chính `(RoomId, Date)` |
| **E. Đơn** | `Bookings` | ⭐ | Sửa nhiều (mục 5.E) |
| | `BookingNights` | ⭐ | **Mới** |
| | `BookingCodeCounters` | ⭐ | **Mới** |
| | `BookingStatusHistories` | ⭐ | Sửa: thêm `ActorType` |
| | `BookingChangeRequests` | ○ | **Mới** |
| **F. Tiền** | `Payments` | ⭐ | Sửa: thêm `Method` (tự khớp / bill), `BankTransactionId`, **ảnh bill** `ReceiptImageUrl`, `Status`, người duyệt |
| | `BankTransactions` | ⭐ | **Mới** — giao dịch ngân hàng báo về (D13) |
| | `Settlements` | ⭐ | Sửa: kỳ theo năm/tháng/số kỳ, trạng thái |
| | `SettlementItems` | ⭐ | **Mới** |
| **G. Khác** | `Reviews` | ⭐ | Sửa: thêm `HomestayId`, trả lời của chủ |
| | `Notifications` | ⭐ | Giữ |
| | `MessageLogs` | ○ | **Mới** |
| | `AuditLogs` | ○ | **Mới** |

**Tổng:** 39 bảng của ứng dụng (36 ⭐, 3 ○), tính cả `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` cộng 4 bảng phụ sẵn có của ASP.NET Identity (`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`) → **43 bảng** trong migration.

---

## 5. Chi tiết từng bảng

> Cột `Id`, `CreatedAt`, `UpdatedAt` có ở mọi bảng nghiệp vụ, không nhắc lại trừ khi có ràng buộc riêng.

### A. Tài khoản (theo v5 — đã triển khai)

#### `PersonProfiles` — thông tin cá nhân dùng chung

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| FullName | nvarchar(100) | NULL | Có thể trống khi là hồ sơ nháp |
| PhoneNumber | nvarchar(16) | NOT NULL, **UNIQUE** | E.164; định danh đăng nhập của Customer/Owner |
| Email | nvarchar(256) | NULL | |
| Address | nvarchar(300) | NULL | |

#### `AspNetUsers` — Account

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| Id | nvarchar(450) | PK | GUID; `UserName` = `Id` (định danh thật là SĐT / mã NV) |
| PersonId | int | FK → PersonProfiles, NOT NULL, **UNIQUE** | Một người tối đa một tài khoản |
| PasswordHash | nvarchar(max) | NULL | Identity băm (PBKDF2). Bắt buộc có khi kích hoạt |
| Status | nvarchar(30) | NOT NULL | `PendingActivation`, `Active`, `Locked`, `Disabled` |
| PasswordChangedAt | datetime2 | NULL | |
| AccessFailedCount, LockoutEnd, SecurityStamp… | | | Cột sẵn có của Identity: khóa tạm sau 5 lần sai |

Đăng nhập Google: lưu ở `AspNetUserLogins (LoginProvider = 'Google', ProviderKey = sub)` — khóa chính bảo đảm một `sub` chỉ gắn một tài khoản.

#### `AspNetUserRoles` — AccountRole

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| UserId, RoleId | | PK ghép |
| AssignedAt | datetime2 | NOT NULL |
| AssignedByAccountId | nvarchar(450) | FK → AspNetUsers, NULL (hệ thống cấp) |

Role: `Customer`, `Owner`, `Employee`. Quản lý / kế toán / quản trị **không phải role** mà là `Employee.Position`.

#### `Customers`, `CustomerProfiles`

| Bảng | Cột | Ràng buộc / ghi chú |
|---|---|---|
| Customers | PersonId | FK, NOT NULL, **UNIQUE** |
| | Note | nvarchar(1000), NULL — **mới**: ghi chú nội bộ của nhân viên về khách (FR-12) |
| CustomerProfiles | CustomerId | PK + FK (1–1), luôn tạo cùng Customer |
| | CitizenId, StudentId | NULL; dữ liệu nhạy cảm, che khi hiển thị |
| | StudentVerificationStatus | `NotSubmitted`, `Pending`, `Verified`, `Rejected` |

Khách có thể tồn tại **trước** khi có tài khoản (nhân viên tạo khi khách gọi điện). Đơn tham chiếu `CustomerId`, không tham chiếu Account.

#### `Owners`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| PersonId | int | FK, NOT NULL, **UNIQUE** | |
| BankName | nvarchar(100) | NULL | **Mới** — Fuji chuyển tiền đối soát (BR-03). Bắt buộc trước khi hồ sơ được duyệt (kiểm tra ở service) |
| BankAccountNo | nvarchar(30) | NULL | **Mới** |
| BankAccountName | nvarchar(100) | NULL | **Mới** |

#### `OwnerRegistrations` — hồ sơ đăng ký Owner

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| OwnerId | int | FK, NOT NULL |
| Source | nvarchar(20) | `SelfService`, `EmployeeAssisted` |
| Status | nvarchar(30) | `Draft`, `WaitingOwnerActivation`, `PendingReview`, `UnderReview`, `NeedsRevision`, `Approved`, `Rejected` |
| CreatedByEmployeeId, ReviewedByEmployeeId | int | FK → Employees, NULL |
| ReviewNote | nvarchar(1000) | NULL |
| SubmittedAt, ReviewedAt | datetime2 | NULL |
| | | **UNIQUE (OwnerId) WHERE Status IN (Draft … NeedsRevision)** — mỗi Owner chỉ một hồ sơ đang xử lý |

#### `VerificationDocuments` — giấy tờ xác minh (**mới**)

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| OwnerRegistrationId | int | FK, NULL | Giấy tờ nộp kèm hồ sơ đăng ký |
| HomestayId | int | FK, NULL | Giấy tờ của một homestay (khi Owner thêm homestay mới sau này) |
| DocumentType | nvarchar(30) | NOT NULL | `BusinessLicense`, `FireSafety`, `PropertyPhoto`, `Other` |
| FileUrl | nvarchar(300) | NOT NULL | Đường dẫn trong `wwwroot/uploads/docs/` (không công khai) |
| OriginalFileName | nvarchar(255) | NULL | |
| ExpiryDate | date | NULL | Ngày hết hạn giấy phép |
| | | CHECK **đúng một** trong `OwnerRegistrationId`, `HomestayId` khác NULL | |

Nguồn: v5 mục 4.6 ("bảng tài liệu xác minh riêng"); BR-12 (giấy phép kinh doanh lưu trú, PCCC, ảnh thực tế).

#### `Employees`

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| PersonId | int | FK, NOT NULL, **UNIQUE** |
| EmployeeCode | nvarchar(20) | NOT NULL, **UNIQUE** |
| Department | nvarchar(100) | NULL |
| Position | nvarchar(20) | `Consultant`, `Accountant`, `Manager`, `Administrator` |
| EmploymentStatus | nvarchar(20) | `Active`, `Suspended`, `Resigned` |

#### `AccountActivityLogs` — nhật ký tài khoản (chỉ thêm, không sửa/xóa)

`Id bigint`, `AccountId` (NULL khi sự kiện trước lúc có tài khoản), `EventType`, `ActorType` (`Customer`/`Owner`/`Employee`/`System`), `ActorId`, `Detail`, `IpAddress`, `CreatedAt`. Không bao giờ ghi mật khẩu hay mã OTP.

---

### B. Đối tác và hợp đồng

#### `Areas`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| Name | nvarchar(100) | NOT NULL | Hoàn Kiếm, Tây Hồ, Sa Pa… |
| Province | nvarchar(100) | NOT NULL | **Mới** — Hà Nội, Lào Cai… (Fuji mở rộng toàn quốc) |
| AreaGroup | nvarchar(20) | NOT NULL | **Mới** — `InnerCity`, `Suburb`, `OtherProvince` |
| | | **UNIQUE (Province, Name)** | |

#### `Homestays`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| OwnerId | int | FK → Owners, NOT NULL | |
| AreaId | int | FK → Areas, NOT NULL | |
| Name | nvarchar(200) | NOT NULL | |
| Address | nvarchar(300) | NOT NULL | Chỉ hiển thị chính xác sau khi đơn được xác nhận (BR-13); trước đó chỉ hiện khu vực |
| Latitude, Longitude | decimal(9,6) | NULL | Bản đồ trong tin nhắc trước ngày nhận phòng (FR-23) |
| DescriptionHtml | nvarchar(max) | NULL | Đã lọc bằng HtmlSanitizer |
| CheckinGuideHtml | nvarchar(max) | NULL | Hướng dẫn nhận phòng, chỉ gửi khách sau khi xác nhận |
| ContactPhone | nvarchar(16) | NULL | E.164; chỉ gửi khách sau khi xác nhận (BR-13) |
| CheckinTime, CheckoutTime | time | NOT NULL, mặc định 14:00 / 12:00 | |
| Status | nvarchar(20) | NOT NULL | `Draft`, `PendingReview`, `Active`, `Suspended`, `Ended` |
| ApprovedByEmployeeId | int | FK, NULL | FR-27 |
| ApprovedAt | datetime2 | NULL | |

- **Bỏ** `CommissionRate` (chuyển sang `Contracts`) và `IsActive` (thay bằng `Status`).
- Chỉ homestay `Active` **và** có hợp đồng đang hiệu lực mới hiện cho khách tìm kiếm.
- Owner chỉ thêm/sửa homestay khi hồ sơ đăng ký đã `Approved` (policy `ApprovedOwner`).

#### `Contracts` — hợp đồng hợp tác (**mới**)

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| HomestayId | int | FK, NOT NULL | |
| ContractNo | nvarchar(30) | NOT NULL, **UNIQUE** | Ví dụ `HD-2026-0035` |
| StartDate, EndDate | date | NOT NULL, CHECK `EndDate > StartDate` | Thường 1 năm |
| CommissionRate | decimal(5,4) | NOT NULL, CHECK `0 < x < 1` | BR-02; thực tế 0.10–0.15 nhưng không khóa cứng để Fuji đàm phán |
| Status | nvarchar(20) | NOT NULL | `Active`, `Expired`, `Terminated` |
| SignedByEmployeeId | int | FK, NULL | |
| | | **UNIQUE (HomestayId) WHERE Status = 'Active'** | Mỗi homestay tối đa một hợp đồng đang hiệu lực |

Gia hạn = kết thúc hợp đồng cũ, tạo hợp đồng mới. Đơn lưu snapshot `CommissionRate` tại thời điểm đặt.

#### `Amenities`, `HomestayAmenities`

Giữ như code: `Amenities(Name UNIQUE, Icon)`, `HomestayAmenities(HomestayId, AmenityId)` khóa chính ghép, cascade khi xóa.

#### `HomestayImages` (gộp cả ảnh loại phòng)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| HomestayId | int | FK, NOT NULL, cascade |
| RoomTypeId | int | FK, NULL — NULL = ảnh chung của homestay |
| Url | nvarchar(300) | NOT NULL |
| SortOrder | int | NOT NULL, mặc định 0 |
| IsCover | bit | Mỗi homestay một ảnh bìa: **UNIQUE (HomestayId) WHERE IsCover = 1** |

---

### C. Phòng và giá

#### `RoomCategories` — danh mục loại phòng chung

`Name UNIQUE` (Phòng đơn, Phòng đôi, Phòng gia đình, Nguyên căn, Phòng tập thể). Admin quản lý. Dùng cho **bộ lọc tìm kiếm** (FR-01) — đây là bảng `RoomTypes` hiện tại đổi tên.

#### `RoomTypes` — loại phòng của từng homestay (**mới**)

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| HomestayId | int | FK, NOT NULL | |
| CategoryId | int | FK → RoomCategories, NOT NULL | |
| Name | nvarchar(100) | NOT NULL | "Phòng đôi hướng hồ" |
| MaxGuests | int | NOT NULL, CHECK > 0 | |
| AreaM2 | int | NULL | |
| Description | nvarchar(1000) | NULL | |
| IsActive | bit | NOT NULL | |
| | | **UNIQUE (HomestayId, Name)** | |

#### `Rooms` — từng phòng cụ thể

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| RoomTypeId | int | FK, NOT NULL |
| RoomCode | nvarchar(20) | NOT NULL — "101", "Căn A" |
| IsActive | bit | NOT NULL — còn kinh doanh hay không |
| | | **UNIQUE (RoomTypeId, RoomCode)** |

Trạng thái theo ngày (trống / đã đặt / bảo trì) **không lưu ở đây** mà suy ra từ `RoomNights`.

#### `RoomTypePrices` — giá 3 mức có lịch sử (**mới**)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| RoomTypeId | int | FK, NOT NULL |
| DayType | nvarchar(10) | `Weekday`, `Weekend`, `Holiday` |
| Price | decimal(18,0) | NOT NULL, CHECK > 0 — giá **một đêm** |
| EffectiveFrom | date | NOT NULL |
| | | **UNIQUE (RoomTypeId, DayType, EffectiveFrom)** |

- Giá của một đêm = dòng có `EffectiveFrom` lớn nhất mà ≤ ngày đó, theo `DayType` của đêm đó.
- Đổi giá = thêm dòng mới; không sửa dòng cũ đã dùng cho đơn.
- Loại phòng chỉ được bán khi có đủ cả 3 mức giá (kiểm tra ở service).

#### `Holidays`

Giữ như code: `Date` (PK), `Name`. Fuji khai báo chung cho mọi homestay.

#### `CancellationPolicies`, `CancellationRules` — chính sách hủy (**mới**)

| Bảng | Cột | Ràng buộc / ghi chú |
|---|---|---|
| CancellationPolicies | HomestayId | FK, **NULL = chính sách chung của Fuji** |
| | Scope | `All` (mọi ngày) hoặc `Holiday` (đơn có ít nhất một đêm lễ) |
| | Name | nvarchar(100) |
| | IsActive | bit |
| | | **UNIQUE (HomestayId, Scope) WHERE IsActive = 1** (SQL Server coi các NULL là bằng nhau → chỉ một chính sách chung đang dùng cho mỗi phạm vi) |
| CancellationRules | PolicyId | FK, NOT NULL, cascade |
| | MinDaysBefore | int, CHECK ≥ 0 |
| | RefundPercent | int, CHECK 0–100 |
| | | **UNIQUE (PolicyId, MinDaysBefore)** |

**Dữ liệu chính sách chung:** (7 ngày → 100%), (3 ngày → 50%), (0 ngày → 0%).

**Chọn chính sách cho một đơn** (lưu `CancellationPolicyId` vào đơn lúc đặt):
1. Đơn có đêm lễ và homestay có chính sách `Holiday` đang dùng → dùng nó.
2. Nếu không: homestay có chính sách `All` đang dùng → dùng nó.
3. Nếu không: chính sách chung `Holiday` (nếu đơn có đêm lễ), cuối cùng là chính sách chung `All`.

Chính sách đã có đơn dùng thì **không sửa**; muốn đổi thì tạo chính sách mới và tắt chính sách cũ.

---

### D. Lịch phòng và chống trùng phòng

#### `RoomBlocks` — chủ homestay khóa ngày (**mới**)

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| RoomId | int | FK, NOT NULL | |
| StartDate, EndDate | date | NOT NULL, CHECK `EndDate > StartDate` | `EndDate` là ngày trả, không tính đêm đó |
| Reason | nvarchar(20) | NOT NULL | `OtherChannel` (khách tự đến, Facebook…), `Maintenance` |
| Note | nvarchar(255) | NULL | |
| CreatedByAccountId | nvarchar(450) | FK, NOT NULL | |

#### `RoomNights` — mỗi dòng là một phòng bị chiếm trong một đêm

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| RoomId | int | FK → Rooms, NOT NULL, Restrict |
| Date | date | NOT NULL |
| BookingId | int | FK → Bookings, NULL |
| RoomBlockId | int | FK → RoomBlocks, NULL, **cascade** |
| | | **PRIMARY KEY (RoomId, Date)** |
| | | CHECK **đúng một** trong `BookingId`, `RoomBlockId` khác NULL |

**Cách hoạt động:**
- Khách **bấm đặt** → trong một transaction: chọn một phòng `IsActive` của loại phòng đã chọn mà không có `RoomNight` trong khoảng ngày, gán `Booking.RoomId`, thêm một dòng cho mỗi đêm `[CheckIn, CheckOut)`. Nếu hai khách đặt cùng lúc vào đúng phòng cuối cùng → người sau vi phạm khóa chính → service thử phòng trống khác, hết phòng thì báo "Đã hết phòng". **Không thể trùng phòng** (BR-05, NFR-01).
- Chủ **khóa ngày** → tạo `RoomBlock` + các dòng `RoomNight` có `RoomBlockId`. Đơn và ngày khóa bị chặn bởi **cùng một khóa chính**.
- Đơn chuyển sang `Expired`, `Cancelled`, `Rejected`, `NoShow` → xóa các dòng có `BookingId` đó, phòng mở lại. **Mở khóa** → xóa `RoomBlock`, các dòng tự xóa theo (cascade).
- Đơn `Completed` → **giữ** các dòng để thống kê công suất phòng.
- **Phòng trống** của một loại phòng trong khoảng `[CheckIn, CheckOut)` = phòng `IsActive` không có dòng `RoomNight` nào trong khoảng đó.

---

### E. Đơn đặt phòng

#### `Bookings` (một đơn = một phòng)

| Nhóm | Cột | Kiểu | Ràng buộc / ghi chú |
|---|---|---|---|
| Định danh | Code | nvarchar(20) | NOT NULL, **UNIQUE**; `FJ` + `yyMMdd` + `-` + 3 số, ví dụ `FJ261011-007` (BR-07) |
| Ai đặt | CustomerId | int | FK → Customers, NOT NULL |
| | ContactName | nvarchar(100) | NOT NULL — người **ở thật** (có thể khác người đặt) |
| | ContactPhone | nvarchar(16) | NOT NULL — E.164 |
| | SourceChannel | nvarchar(20) | `Web`, `Phone`, `Zalo`, `Messenger`, `Other` |
| | CreatedByEmployeeId | int | FK, NULL — NULL = khách tự đặt trên web |
| | AssignedEmployeeId | int | FK, NULL — nhân viên phụ trách (giao lại được, FR-09) |
| Đặt gì | HomestayId | int | FK, NOT NULL — lưu thừa có chủ đích để lọc theo chủ homestay và lập đối soát nhanh |
| | RoomTypeId | int | FK, NOT NULL — khách chọn loại phòng |
| | RoomId | int | FK, **NOT NULL** — hệ thống gán phòng trống ngay khi đặt (D4) |
| | CheckIn, CheckOut | date | NOT NULL, CHECK `CheckOut > CheckIn` |
| | Guests | int | NOT NULL, CHECK > 0; ≤ `RoomType.MaxGuests` (service) |
| | SpecialRequests | nvarchar(1000) | NULL |
| Trạng thái | Status | nvarchar(20) | Xem mục 6. **Concurrency token**: hai thao tác đổi trạng thái cùng lúc (job hết hạn và tiền về) → chỉ một bên thành công |
| | HoldExpiresAt | datetime2 | NOT NULL = `CreatedAt + 15 phút` (D5); gia hạn thêm 15 phút khi kế toán từ chối bill (mục 6.2) |
| | PaymentSubmittedAt | datetime2 | NULL — lúc khách tải bill |
| | DepositedAt | datetime2 | NULL — lúc kế toán duyệt cọc |
| | ConfirmDeadlineAt | datetime2 | NULL = `DepositedAt + 30 phút` (D12, BR-06) |
| | ConfirmAlertSentAt | datetime2 | NULL — đã cảnh báo nhân viên vì quá hạn xác nhận; job không gửi lặp |
| | ConfirmedAt, CheckedInAt, CompletedAt | datetime2 | NULL |
| | CancelledAt | datetime2 | NULL |
| | CancelReason | nvarchar(500) | NULL |
| Tiền (snapshot) | TotalAmount | decimal(18,0) | NOT NULL = Σ `BookingNights.Price` |
| | DepositRate | decimal(5,4) | NOT NULL, mặc định 0.3000 (BR-01) |
| | DepositAmount | decimal(18,0) | NOT NULL; CHECK `DepositAmount <= TotalAmount` |
| | CommissionRate | decimal(5,4) | NOT NULL — lấy từ hợp đồng đang hiệu lực |
| | CancellationPolicyId | int | FK, NOT NULL |
| | RefundAmount | decimal(18,0) | NULL — tính khi hủy (BR-04) |
| Trùng phòng | ReplacedByBookingId | int | FK → Bookings, NULL — đơn thay thế khi Fuji chuyển khách sang phòng khác (BR-11) |

**Vì sao lưu snapshot:** giá, tỷ lệ cọc, hoa hồng và chính sách hủy có thể thay đổi sau khi đặt. Đơn cũ và bảng đối soát cũ phải giữ đúng số liệu tại thời điểm đặt.

**Ràng buộc chéo (kiểm tra ở service, không làm được bằng FK):** `RoomType.HomestayId = Booking.HomestayId`; `Room.RoomTypeId = Booking.RoomTypeId`.

**Chống giữ phòng ảo:** vì đặt là giữ phòng ngay, một khách (cùng `CustomerId`) tối đa **3 đơn** `AwaitingPayment` cùng lúc (service). Con số 3 cho phép gia đình đặt 2–3 phòng (mỗi đơn một phòng — D2) nhưng chặn việc giữ hàng loạt.

#### `BookingNights` — giá từng đêm đã chốt (**mới**)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| BookingId | int | FK, NOT NULL |
| Date | date | NOT NULL |
| DayType | nvarchar(10) | `Weekday` / `Weekend` / `Holiday` |
| Price | decimal(18,0) | NOT NULL, CHECK > 0 |
| | | **PRIMARY KEY (BookingId, Date)** |

Hiển thị chi tiết giá cho khách trước khi gửi yêu cầu (FR-02).

#### `BookingCodeCounters` — sinh mã đơn (**mới**)

`Date date PK`, `LastSeq int NOT NULL`. Khi tạo đơn: `UPDATE … SET LastSeq = LastSeq + 1 OUTPUT inserted.LastSeq WHERE Date = @today` (chưa có dòng thì `INSERT`), trong cùng transaction với `INSERT Bookings`. Kết hợp `UNIQUE (Code)` → không thể trùng mã như khi đặt tay.

#### `BookingStatusHistories`

| Cột | Ghi chú |
|---|---|
| BookingId | FK |
| FromStatus | NULL khi đơn vừa tạo |
| ToStatus | |
| ActorType | `Customer` / `Owner` / `Employee` / `System` — **mới** |
| ActorAccountId | FK, NULL (khách chưa đăng nhập, hệ thống) |
| Note | lý do từ chối, hủy… |
| ChangedAt | |

#### `BookingChangeRequests` — yêu cầu đổi/hủy (○, **mới**)

| Cột | Ghi chú |
|---|---|
| BookingId | FK |
| RequestType | `ChangeDates`, `ChangeRoomType`, `Cancel` |
| RequestedByType | `Customer` / `Employee` |
| NewCheckIn, NewCheckOut, NewRoomTypeId | NULL nếu là hủy |
| PriceDifference | Chênh lệch tiền khi đổi (có thể âm) |
| RefundAmount | Số tiền hoàn dự kiến theo BR-04 — khách xem trước (FR-05) |
| Status | `Pending`, `Approved`, `Rejected` |
| DecidedByAccountId, DecidedAt, DecisionNote | |

Đổi ngày/đổi loại phòng được duyệt → service cập nhật đơn và `RoomNights` trong một transaction.

---

### F. Tiền

#### `Payments`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| BookingId | int | FK, NOT NULL | Thanh toán **gắn với từng đơn** |
| Type | nvarchar(20) | NOT NULL | `Deposit` (cọc), `Refund` (hoàn cọc), `Compensation` (Fuji bù chênh lệch, BR-11) |
| Method | nvarchar(20) | NOT NULL | `BankAutoMatch` (tự khớp từ giao dịch ngân hàng, D13), `ManualReceipt` (ảnh bill, kế toán duyệt, D11), `ManualTransfer` (Fuji chuyển ra: hoàn cọc, bù chênh lệch) |
| BankTransactionId | int | FK → BankTransactions, NULL; **UNIQUE WHERE NOT NULL** | Một giao dịch ngân hàng chỉ thành **một** khoản thanh toán |
| Amount | decimal(18,0) | NOT NULL, CHECK > 0 | Số tiền thực nhận (tự khớp) hoặc số tiền theo bill |
| ReceiptImageUrl | nvarchar(300) | NULL — **bắt buộc khi `Method = ManualReceipt`** (service) | **Ảnh bill chuyển khoản** (D11). Với `Refund`: ảnh bill Fuji hoàn tiền (tùy chọn) |
| TransferContent | nvarchar(255) | NULL | Nội dung chuyển khoản trên bill, phải chứa mã đơn (BR-10) |
| BankRef | nvarchar(100) | NULL | Mã giao dịch kế toán ghi khi đối chiếu |
| Status | nvarchar(20) | NOT NULL | `Submitted` (đã tải bill, chờ duyệt), `Approved` (kế toán duyệt), `Rejected` (bill không hợp lệ) |
| SubmittedByAccountId | nvarchar(450) | FK, NULL | Khách (đã đăng nhập) hoặc nhân viên tải hộ; NULL = khách tra cứu bằng mã đơn + SĐT |
| SubmittedAt | datetime2 | NOT NULL | |
| ReviewedByEmployeeId | int | FK, NULL | Kế toán; **NULL khi hệ thống tự duyệt** (`BankAutoMatch`) |
| ReviewedAt | datetime2 | NULL | |
| RejectReason | nvarchar(500) | NULL | "Ảnh mờ", "Sai số tiền", "Không thấy giao dịch"… — gửi lại cho khách |
| ReviewAlertSentAt | datetime2 | NULL | Bill chờ duyệt quá 2 giờ hành chính (T2–T6, 8h–17h) đã báo quản lý; job không gửi lặp. Bill gửi ngoài giờ chờ đến sáng hôm sau |

- 70% còn lại khách trả **trực tiếp cho homestay** → không ghi ở đây.
- Mỗi đơn tối đa **một** khoản cọc đang chờ duyệt hoặc đã duyệt: **UNIQUE (BookingId) WHERE Type = 'Deposit' AND Status <> 'Rejected'**. Bill bị từ chối được giữ lại làm lịch sử.
- Khách đã tải bill (`ManualReceipt`, `Submitted`) rồi giao dịch ngân hàng mới về và khớp → hệ thống **duyệt luôn khoản đó** (gắn `BankTransactionId`, `Status = Approved`), không tạo khoản thứ hai.
- **Ảnh bill là dữ liệu nhạy cảm** (có số tài khoản, tên người chuyển): lưu **ngoài** `wwwroot` (ví dụ `App_Data/receipts/`), tên file GUID, chỉ phục vụ qua controller có kiểm tra quyền (khách của đơn, nhân viên, kế toán). Kiểm tra định dạng jpg/png/webp/pdf, ≤ 5 MB.

#### `BankTransactions` — giao dịch ngân hàng báo về (**mới**, D13)

Lưu **mọi** khoản tiền vào tài khoản nhận cọc của Fuji mà dịch vụ theo dõi tài khoản báo về, kể cả khoản không khớp được đơn nào — để kế toán đối chiếu sao kê và không bỏ sót tiền của khách.

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| Provider | nvarchar(20) | NOT NULL | `SePay`, `Casso`, `Simulator` (giả lập khi demo BTL) |
| ProviderTransactionId | nvarchar(100) | NOT NULL | Mã giao dịch phía dịch vụ |
| | | **UNIQUE (Provider, ProviderTransactionId)** | Webhook gửi lặp → không ghi nhận hai lần (idempotent) |
| BankReference | nvarchar(100) | NULL | Mã tham chiếu trên sao kê ngân hàng |
| AccountNumber | nvarchar(30) | NOT NULL | Tài khoản Fuji nhận tiền |
| Amount | decimal(18,0) | NOT NULL, CHECK > 0 | Chỉ lưu tiền **vào** |
| TransferContent | nvarchar(500) | NULL | Nội dung chuyển khoản gốc |
| TransactionTime | datetime2 | NOT NULL | Thời điểm ngân hàng ghi nhận |
| ReceivedAt | datetime2 | NOT NULL | Thời điểm hệ thống nhận webhook |
| RawPayload | nvarchar(max) | NULL | JSON gốc, để tra soát khi có tranh chấp |
| MatchStatus | nvarchar(20) | NOT NULL | Xem bảng dưới |
| MatchedBookingId | int | FK → Bookings, NULL | Đơn tìm được từ nội dung chuyển khoản |
| ProcessedByEmployeeId | int | FK, NULL | Người xử lý tay (khớp tay, hoàn tiền, bỏ qua) |
| ProcessedAt | datetime2 | NULL | |
| Note | nvarchar(500) | NULL | |

| MatchStatus | Ý nghĩa | Xử lý |
|---|---|---|
| `Matched` | Tìm thấy mã đơn, đơn đang chờ thanh toán, đủ tiền → đã tạo / duyệt `Payment` | Tự động |
| `Underpaid` | Có mã đơn nhưng chuyển **thiếu** | Vào danh sách kế toán: liên hệ khách chuyển bù hoặc hoàn |
| `LatePayment` | Có mã đơn nhưng đơn đã `Expired` / `Cancelled` | Vào danh sách: còn phòng → nhân viên mở lại đơn (giữ phòng mới); hết phòng → hoàn tiền |
| `Unmatched` | Không đọc được mã đơn | Vào danh sách: kế toán **khớp tay** với đơn, hoặc hoàn tiền |
| `ManuallyMatched` | Kế toán đã khớp tay | — |
| `Refunded`, `Ignored` | Đã hoàn lại / không liên quan đặt phòng (ví dụ tiền khác vào tài khoản) | — |

**Quy tắc tự khớp:**
1. Chuẩn hóa nội dung: chữ hoa, bỏ dấu cách và ký tự đặc biệt (ngân hàng hay bỏ dấu `-`), rồi tìm mẫu `FJ` + 9 chữ số → `FJ261011007` ứng với mã đơn `FJ261011-007`.
2. Đơn ở `AwaitingPayment` hoặc `PaymentSubmitted` **và** `Amount ≥ DepositAmount` → `Matched`: tạo (hoặc duyệt khoản bill đang chờ) `Payment(Method = BankAutoMatch, Status = Approved)`, đơn → `Deposited`. Chuyển **dư** → ghi `Note`, phần dư hoàn khi đối soát với khách.
3. Các trường hợp còn lại → `Underpaid` / `LatePayment` / `Unmatched`.
4. Toàn bộ chạy trong **một transaction**; webhook trả `200` ngay cả khi giao dịch đã có (trùng `ProviderTransactionId`).

**Bảo mật webhook:** chỉ nhận qua HTTPS, kiểm tra khóa API trong header (`Authorization: Apikey …`) do SePay/Casso gửi; khóa để trong user-secrets. Endpoint giả lập (`Simulator`) **chỉ bật ở môi trường Development** và chỉ nhân viên quản lý dùng được.

#### `Settlements` — bảng đối soát một homestay một kỳ

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| HomestayId | int | FK, NOT NULL |
| Year, Month | int | NOT NULL |
| PeriodNo | int | CHECK IN (1, 2) — kỳ 1: ngày 1–15; kỳ 2: 16 – cuối tháng |
| TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount | decimal(18,0) | NOT NULL |
| Status | nvarchar(20) | `Draft`, `Sent`, `Confirmed`, `Paid` |
| PaidAt | datetime2 | NULL |
| PaidByEmployeeId | int | FK, NULL |
| | | **UNIQUE (HomestayId, Year, Month, PeriodNo)** |

#### `SettlementItems` — từng đơn trong bảng đối soát (**mới**)

| Cột | Ràng buộc / ghi chú |
|---|---|
| SettlementId | FK, NOT NULL, cascade |
| BookingId | FK, NOT NULL, **UNIQUE** — một đơn chỉ vào một kỳ |
| ItemType | `Completed`, `CancelledRetained`, `NoShow` |
| RoomAmount, DepositAmount, RefundAmount, RetainedAmount, CommissionRate, CommissionAmount, PayoutAmount | Sao chép từ đơn lúc lập bảng (công thức mục 7) |

- Đơn vào kỳ theo **ngày `CheckOut`** (D7).
- Khi `Settlement.Status` khác `Draft`, các đơn trong kỳ không được sửa số tiền.

---

### G. Đánh giá, thông báo, nhật ký

#### `Reviews`

| Cột | Ràng buộc / ghi chú |
|---|---|
| BookingId | FK, NOT NULL, **UNIQUE** — mỗi đơn một đánh giá (BR-09) |
| HomestayId | FK, NOT NULL — **mới**, lưu thừa để tính điểm trung bình nhanh |
| Rating | int, CHECK 1–5 |
| Comment | nvarchar(1000) |
| OwnerReply | nvarchar(1000), NULL — **mới** (FR-06) |
| RepliedAt | datetime2, NULL |
| IsHidden | bit — Fuji ẩn đánh giá vi phạm |

Chỉ cho đánh giá khi đơn `Completed` (service).

#### `Notifications` — thông báo trong hệ thống

Giữ như code: `AccountId`, `Message`, `Url`, `IsRead`, `CreatedAt`; chỉ mục `(AccountId, IsRead)`.

#### `MessageLogs` — tin gửi ra ngoài (○, **mới**)

`Id bigint`, `Channel` (`Sms`, `Zalo`, `Email`), `RecipientPhone` / `RecipientEmail`, `TemplateCode` (`Otp`, `BookingConfirmed`, `DepositReminder`, `CheckinReminder`…), `BookingId` (NULL), `Status` (`Pending`, `Sent`, `Failed`), `ErrorMessage`, `SentAt`, `CreatedAt`. **Không** lưu nội dung OTP. Dùng để đối chiếu chi phí gửi tin (NFR-12).

#### `AuditLogs` — thay đổi dữ liệu nhạy cảm (○, **mới**)

`Id bigint`, `AccountId`, `TableName`, `RecordId`, `Action` (`Insert`/`Update`/`Delete`), `OldValues`, `NewValues` (JSON), `IpAddress`, `CreatedAt`. Chỉ ghi các bảng: `RoomTypePrices`, `Contracts`, `Payments`, `Settlements`, `Homestays.Status`. Hiện thực bằng `SaveChangesInterceptor` của EF Core.

---

## 6. Vòng đời đơn đặt phòng và các mốc thời gian

### 6.1. Sơ đồ trạng thái

```text
 Khách / nhân viên bấm đặt  (hệ thống gán phòng trống, GIỮ PHÒNG — D4)
        │
        ▼
 AwaitingPayment ─(quá 15' chưa nhận tiền, chưa có bill)─► Expired    (mở lại phòng)
 (chờ thanh toán,  ──(khách hủy)───────────────────────────► Cancelled  (mở lại phòng)
  giữ phòng 15')
        ├─ khách quét QR, tiền về, hệ thống TỰ KHỚP (D13) ─────────────┐   ← đường chính, vài giây, 24/7
        │                                                              │
        │ dự phòng: khách tải ảnh bill (D11)                           │
        ▼                                                              │
 PaymentSubmitted ───(kế toán từ chối bill)──► quay lại AwaitingPayment, giữ thêm 15'
 (chờ duyệt cọc,    ──(giao dịch ngân hàng về và khớp)──────────────────┤   tự duyệt luôn
  vẫn giữ phòng)                                                       │
        │ kế toán xem bill → duyệt                                     │
        ▼                                                              │
        ◄──────────────────────────────────────────────────────────────┘
 Deposited ──────────(homestay từ chối)──────────────────► Rejected   (hoàn 100% cọc, mở lại phòng)
 (đã cọc, chờ       ──(khách / NV hủy)──────────────────────► Cancelled  (hoàn theo chính sách hủy)
  homestay ≤ 30')
        │ homestay xác nhận (D12)
        ▼
 Confirmed ──────────(khách / NV hủy)──────────────────────► Cancelled  (hoàn theo chính sách hủy)
 (gửi SĐT, địa chỉ,  └─(khách không đến)───────────────────► NoShow     (không hoàn)
  hướng dẫn cho khách — BR-13)
        │ đến ngày nhận phòng
        ▼
 CheckedIn ──► Completed ──► được đánh giá (BR-09), vào kỳ đối soát (D7)
```

| Trạng thái | Tiếng Việt | Giữ phòng (`RoomNight`)? | Ai chuyển sang |
|---|---|---|---|
| `AwaitingPayment` | Chờ thanh toán | **Có**, tối đa đến `HoldExpiresAt` | Khách (web) / nhân viên (đơn qua điện thoại, Zalo) |
| `PaymentSubmitted` | Chờ duyệt cọc | **Có**, không hết hạn trong lúc chờ duyệt | Khách / nhân viên tải bill (đường dự phòng) |
| `Deposited` | Đã đặt cọc | **Có** | **Hệ thống** tự khớp giao dịch, hoặc kế toán duyệt bill |
| `Confirmed` | Đã xác nhận | **Có** | Chủ homestay |
| `CheckedIn` | Đã nhận phòng | **Có** | Chủ homestay / nhân viên |
| `Completed` | Hoàn thành | Có (giữ để thống kê) | Chủ homestay / nhân viên |
| `Expired` | Hết hạn giữ phòng | Không — xóa | Hệ thống (job) |
| `Rejected` | Homestay từ chối | Không — xóa | Chủ homestay |
| `Cancelled` | Đã hủy | Không — xóa | Khách / nhân viên |
| `NoShow` | Không đến | Không — xóa | Chủ homestay / nhân viên |

Mọi lần đổi trạng thái đi qua một hàm duy nhất (`BookingService.ChangeStatus`): kiểm tra chuyển trạng thái hợp lệ, cập nhật `RoomNights`, ghi `BookingStatusHistories`, tạo `Notifications`.

### 6.2. Giữ phòng 15 phút và thanh toán cọc (D4, D5, D11, D13)

1. **Đặt phòng** → tạo đơn `AwaitingPayment`, gán phòng, thêm `RoomNights`, `HoldExpiresAt = now + 15'`. Màn hình thanh toán hiện:
   - **Mã QR VietQR của MB Bank** (mã ngân hàng `MB`, BIN 970422) điền sẵn tài khoản Fuji, số tiền = `DepositAmount`, nội dung = **mã đơn** (ảnh QR sinh theo URL, không lưu DB);
   - số tài khoản và nội dung để chuyển tay;
   - đồng hồ đếm ngược 15 phút; trang tự kiểm tra trạng thái đơn mỗi vài giây;
   - liên kết "Tôi đã chuyển nhưng chưa được xác nhận → tải ảnh bill".
2. **Đường chính — tự khớp:** khách quét QR và chuyển → ngân hàng báo SePay/Casso → webhook gửi về → ghi `BankTransactions` → khớp mã đơn + đủ tiền → `Payment(BankAutoMatch, Approved)`, đơn → `Deposited`, `ConfirmDeadlineAt = now + 30'`, thông báo khách và chủ homestay. Thường **vài giây**, chạy cả ban đêm.
3. **Đường dự phòng — ảnh bill:** khách tải bill (trước `HoldExpiresAt`) → `Payment(ManualReceipt, Submitted)`, đơn → `PaymentSubmitted`, phòng được giữ tới khi xử lý xong. Sau đó:
   - giao dịch ngân hàng về muộn và khớp → hệ thống **tự duyệt** khoản bill đó;
   - không khớp được → kế toán xem bill + danh sách `BankTransactions` chưa khớp, **khớp tay** (`ManuallyMatched`) hoặc từ chối (`Rejected` + lý do → đơn quay lại `AwaitingPayment`, giữ thêm 15').
4. **Job mỗi phút:** đơn `AwaitingPayment` có `HoldExpiresAt < now` → `Expired`, xóa `RoomNights`. Tiền về **sau** khi đơn đã hết hạn → `BankTransactions.LatePayment`: nhân viên mở lại đơn nếu phòng còn trống, nếu không thì hoàn tiền.
5. **Homestay** xác nhận trong 30 phút → `Confirmed`, hệ thống gửi khách SĐT, địa chỉ, hướng dẫn nhận phòng (BR-13). Homestay từ chối → `Rejected`, kế toán chuyển hoàn và ghi `Payment(Type = Refund, Method = ManualTransfer, Amount = DepositAmount)`.

**Trong BTL (không đăng ký SePay/Casso thật):** endpoint `POST /api/bank-webhook/sepay` nhận đúng định dạng của SePay; trang **"Giả lập tiền vào"** (chỉ Development, chỉ quản lý) cho nhập mã đơn + số tiền rồi gọi chính endpoint đó với `Provider = Simulator`. Khi Fuji đăng ký thật chỉ cần cấu hình khóa API, không đổi database.

### 6.3. Các mốc thời gian

| Mốc | Giá trị | Xử lý khi quá hạn |
|---|---|---|
| Giữ phòng chờ thanh toán | `CreatedAt + 15'` (gia hạn 15' khi bill bị từ chối) | Job tự chuyển `Expired`, mở lại phòng |
| Tự khớp giao dịch QR | Vài giây (phụ thuộc ngân hàng / dịch vụ) | — |
| Kế toán duyệt bill (dự phòng) | Không giới hạn cứng, trong giờ làm việc | Đơn vẫn giữ phòng; hiện ở đầu danh sách chờ duyệt (Q1, mục 12) |
| Homestay xác nhận | `DepositedAt + 30'` (BR-06) | Cảnh báo nhân viên phụ trách; đơn **không** tự đổi trạng thái |
| Hồ sơ Owner chờ duyệt | 3 ngày làm việc | Cảnh báo quản lý (FR-27) |
| OTP | 5 phút | Lưu trong bộ nhớ đệm, không có bảng |

---

## 7. Công thức tiền

### 7.1. Khi đặt

- Giá mỗi đêm: đêm có trong `Holidays` → `Holiday`; đêm thứ Sáu, thứ Bảy → `Weekend`; còn lại → `Weekday`. Lấy `RoomTypePrices` hiệu lực tại đêm đó.
- `TotalAmount = Σ BookingNights.Price`
- `DepositAmount = làm tròn đến 1.000đ (TotalAmount × DepositRate)` (BR-01)
- `CommissionRate` = `Contracts.CommissionRate` của hợp đồng `Active` tại ngày đặt.

### 7.2. Khi hủy

- `DaysBefore = CheckIn − ngày hủy` (theo giờ Việt Nam).
- `RefundPercent` = quy tắc có `MinDaysBefore` lớn nhất mà ≤ `DaysBefore` trong chính sách của đơn.
- `RefundAmount = DepositAmount × RefundPercent / 100`. Đơn chưa có cọc → 0.
- `NoShow` → `RefundAmount = 0`.
- Homestay từ chối (`Rejected`) → `RefundAmount = DepositAmount` (100%), không phụ thuộc chính sách hủy.
- Hủy khi đơn đang `AwaitingPayment` / `PaymentSubmitted` (chưa duyệt cọc): không có cọc được ghi nhận → 0; nếu khách đã chuyển tiền thì kế toán duyệt rồi hoàn theo chính sách.

### 7.3. Khi đối soát (D7, D8)

Áp cho các đơn `Completed`, `Cancelled` có giữ lại tiền cọc, và `NoShow`, có `CheckOut` thuộc kỳ:

```text
RetainedAmount   = DepositAmount − RefundAmount            (đơn hoàn thành: RefundAmount = 0)
CommissionAmount = MIN(TotalAmount × CommissionRate, RetainedAmount)
PayoutAmount     = RetainedAmount − CommissionAmount        (Fuji chuyển cho homestay)
```

Ví dụ: tổng 2.000.000đ, cọc 600.000đ, hoa hồng 12%.

| Trường hợp | Hoàn | Giữ lại | Hoa hồng Fuji | Chuyển homestay |
|---|---|---|---|---|
| Hoàn thành | 0 | 600.000 | 240.000 | 360.000 |
| Hủy trước 5 ngày (hoàn 50%) | 300.000 | 300.000 | 240.000 | 60.000 |
| Hủy trước 1 ngày / không đến | 0 | 600.000 | 240.000 | 360.000 |
| Hủy trước 10 ngày (hoàn 100%) | 600.000 | 0 | 0 | 0 — không vào bảng đối soát |

---

## 8. Quy tắc nghiệp vụ được thực thi ở đâu

| Quy tắc | Database | Service (ứng dụng) |
|---|---|---|
| Không trùng phòng (BR-05) | **PK `RoomNights (RoomId, Date)`** | Bắt lỗi trùng khóa → báo "Phòng vừa được đặt" |
| Mã đơn duy nhất (BR-07) | UNIQUE `Bookings.Code` + `BookingCodeCounters` | Sinh mã trong transaction |
| Một người một tài khoản (v5) | UNIQUE `AspNetUsers.PersonId`, `PersonProfiles.PhoneNumber` | Chuẩn hóa SĐT |
| Một hồ sơ Owner đang xử lý | Filtered UNIQUE `OwnerRegistrations` | |
| Một hợp đồng đang hiệu lực / homestay | Filtered UNIQUE `Contracts` | |
| Hoa hồng theo hợp đồng (BR-02) | `Contracts.CommissionRate` → snapshot `Bookings.CommissionRate` | |
| Cọc 30% (BR-01) | `Bookings.DepositRate`, CHECK `DepositAmount <= TotalAmount` | Tính và làm tròn |
| Giá 3 mức (BR-08) | `RoomTypePrices`, `Holidays` → `BookingNights` | Chọn mức giá cho từng đêm |
| Hoàn cọc, ưu tiên chính sách riêng (BR-04) | `CancellationPolicies/Rules`, `Bookings.CancellationPolicyId` | Chọn chính sách, tính `RefundAmount` |
| Homestay xác nhận trong 30 phút sau khi có cọc (BR-06, D12) | `Bookings.ConfirmDeadlineAt` | Job mỗi phút tạo cảnh báo |
| Giữ phòng 15 phút khi đặt (D4, D5) | `RoomNights` tạo ngay khi đặt, `Bookings.HoldExpiresAt` | Job mỗi phút chuyển `Expired` |
| Cọc khớp mã đơn và số tiền (BR-10, D13) | UNIQUE `BankTransactions (Provider, ProviderTransactionId)`, UNIQUE `Payments.BankTransactionId`, filtered UNIQUE khoản cọc | Tự khớp nội dung + số tiền trong transaction |
| Ảnh bill khi không tự khớp được (D11) | `Payments.Method = ManualReceipt`, `ReceiptImageUrl` | Kế toán xem bill, khớp tay / từ chối |
| Không bỏ sót tiền khách chuyển | `BankTransactions.MatchStatus` | Danh sách giao dịch chưa khớp / đến muộn cho kế toán |
| Chống giữ phòng ảo | — | Tối đa 3 đơn `AwaitingPayment` / khách |
| Đối soát 2 kỳ / tháng (BR-03) | UNIQUE `Settlements (HomestayId, Year, Month, PeriodNo)`, UNIQUE `SettlementItems.BookingId` | Lập bảng theo công thức 7.3 |
| Đánh giá một lần sau khi hoàn thành (BR-09) | UNIQUE `Reviews.BookingId`, CHECK 1–5 | Kiểm tra `Status = Completed` |
| Homestay phải được duyệt mới hiện (BR-12) | `Homestays.Status`, `VerificationDocuments` | Lọc `Status = Active` |
| Ẩn SĐT, địa chỉ trước khi xác nhận (BR-13) | `Homestays.ContactPhone`, `CheckinGuideHtml` | Chỉ trả về khi đơn từ `Confirmed` |

---

## 9. Phân quyền dữ liệu

| Dữ liệu | Khách | Tư vấn | Kế toán | Quản lý / Quản trị | Chủ homestay |
|---|---|---|---|---|---|
| Homestay, loại phòng, giá, ảnh | Xem (chỉ `Active`) | Xem | Xem | Xem, duyệt | **Của mình**: thêm, sửa |
| Lịch phòng, khóa ngày | Chỉ thấy còn/hết | Xem tất cả | — | Xem | **Của mình**: xem, khóa |
| Đơn đặt phòng | **Đơn của mình** (đăng nhập, hoặc mã đơn + SĐT) | Xem, tạo, xử lý, giao lại | Xem | Xem | **Đơn của homestay mình** (từ `Deposited` trở đi) |
| Thông tin khách | Của mình | Xem, sửa, ghi chú | Xem | Xem | Tên, SĐT người ở trong đơn của mình |
| CCCD, thẻ sinh viên | Của mình | Xem (che một phần) | — | Xem | — |
| Thanh toán, ảnh bill | Quét QR, tải bill, xem trạng thái cọc của đơn mình | Xem, tải bill thay khách | **Xem bill, duyệt / từ chối**, ghi hoàn tiền | Xem | — |
| Giao dịch ngân hàng (`BankTransactions`) | — | Xem giao dịch của đơn mình phụ trách | **Xem tất cả, khớp tay, đánh dấu hoàn / bỏ qua** | Xem; giả lập tiền vào (Development) | — |
| Đối soát | — | — | Lập, đánh dấu đã trả | Xem | **Của mình**: xem |
| Hợp đồng, hoa hồng | — | — | Xem | Thêm, sửa | Của mình: xem |
| Báo cáo doanh thu | — | Chỉ thống kê đơn của mình | Xem | Xem | Của mình |
| Hồ sơ Owner, giấy tờ | — | Nhập hộ, thẩm định | — | Duyệt | Của mình |
| Tài khoản, nhân viên | — | — | — | Quản trị | — |

Chủ homestay chỉ truy vấn dữ liệu có `Homestay.OwnerId` = chính mình; lọc ở service, kiểm tra cả ở API.

---

## 10. Chỉ mục

| Bảng | Chỉ mục | Phục vụ |
|---|---|---|
| RoomNights | PK (RoomId, Date); (BookingId); (RoomBlockId) | Chống trùng, tìm phòng trống |
| Bookings | UNIQUE (Code); (CustomerId); (HomestayId, CheckIn); (Status, HoldExpiresAt); (Status, ConfirmDeadlineAt); (AssignedEmployeeId, Status); (CheckOut) | Tra cứu, job, đối soát |
| RoomTypePrices | UNIQUE (RoomTypeId, DayType, EffectiveFrom) | Lấy giá hiệu lực |
| Homestays | (AreaId, Status); (OwnerId) | Tìm kiếm, khu vực chủ homestay |
| PersonProfiles | UNIQUE (PhoneNumber) | Đăng nhập, tìm khách |
| Payments | (BookingId); filtered UNIQUE khoản cọc; (Status, SubmittedAt) | Danh sách bill chờ duyệt |
| SettlementItems | UNIQUE (BookingId) | |
| BankTransactions | UNIQUE (Provider, ProviderTransactionId); (MatchStatus, TransactionTime); (MatchedBookingId) | Chống ghi trùng webhook; danh sách chờ xử lý |
| Notifications | (AccountId, IsRead) | Đếm thông báo chưa đọc |
| AccountActivityLogs | (AccountId, CreatedAt) | |

Quy mô dự kiến (khoảng 150 homestay, vài chục nghìn đơn mỗi năm) là **nhỏ** với SQL Server: không cần phân vùng bảng.

---

## 11. Thay đổi so với code hiện tại

| Code hiện tại | Thiết kế v1.0 | Ghi chú khi sửa |
|---|---|---|
| `Homestays.CommissionRate` | `Contracts.CommissionRate` | Dữ liệu mẫu: tạo một hợp đồng `Active` cho mỗi homestay |
| `Homestays.IsActive` | `Homestays.Status` | |
| `RoomTypes` (danh mục chung) | `RoomCategories` | Đổi tên controller/view Admin tương ứng |
| — | `RoomTypes` thuộc homestay | |
| `Rooms.PriceWeekday/Weekend/Holiday` | `RoomTypePrices` | |
| `RoomImages` | Gộp vào `HomestayImages.RoomTypeId` | |
| `RoomNights.BlockReason` | `RoomBlocks` + `RoomNights.RoomBlockId`; PK (RoomId, Date) | |
| `Bookings.RoomId` | Giữ `RoomId` (gán ngay khi đặt) + thêm `RoomTypeId`, các cột snapshot, mốc thời gian (`HoldExpiresAt`…) | Trạng thái mới: `AwaitingPayment`, `PaymentSubmitted`, `Expired` |
| `Payments` không có trạng thái | Thêm `Method`, `BankTransactionId`, `ReceiptImageUrl`, `Status`, `TransferContent`, người tải / người duyệt, `RejectReason` | |
| Chưa có | `BankTransactions` + endpoint webhook + trang giả lập tiền vào | |
| `Settlements` theo `PeriodStart/End` | `Year`, `Month`, `PeriodNo`, `Status` + `SettlementItems` | |
| `Reviews` | Thêm `HomestayId`, `OwnerReply`, `IsHidden` | |
| Chưa có | `VerificationDocuments`, `CancellationPolicies/Rules`, `BookingNights`, `BookingCodeCounters`, `BookingChangeRequests`, `MessageLogs`, `AuditLogs` | |

Vì chưa triển khai thật: sửa entity xong thì **tạo lại migration `Init`** và database.

---

## 12. Câu hỏi còn mở

| # | Câu hỏi | Ảnh hưởng | Tạm thời |
|---|---|---|---|
| Q1 | ~~Kế toán chỉ làm giờ hành chính~~ — **đã giải quyết bằng D13**: chuyển khoản qua QR được tự xác nhận 24/7. Chỉ đơn đi đường ảnh bill vào ban đêm mới phải chờ tới giờ làm việc | Thời gian chờ | Giao diện khuyến khích quét QR; báo khách đi đường bill rằng Fuji duyệt trong giờ làm việc |
| Q2 | ~~Kế toán từ chối bill bao nhiêu lần thì hủy hẳn?~~ — **đã chốt (11/10)**: lần 1 khách được thêm 15'; lần 2 đơn `Expired`, mở lại phòng (`Booking:MaxReceiptRejections` = 2) | `Bookings`, `Payments` | — |
| Q3 | Khoản Fuji bù chênh lệch khi trùng phòng (BR-11) có trừ vào homestay gây ra không? | `SettlementItems` | Không trừ; ghi `Payments.Compensation` để thống kê |
| Q4 | Voucher bồi thường: giá trị, thời hạn, điều kiện | Bảng `Vouchers` | Chưa thiết kế |
| Q5 | Đơn bị hủy có giữ cọc: xếp kỳ đối soát theo ngày `CheckOut` dự kiến hay ngày hủy? | `SettlementItems` | Theo `CheckOut` dự kiến (thống nhất D7) |
| Q6 | Một Owner có cần nhiều tài khoản ngân hàng (mỗi homestay một tài khoản) không? | `Owners` | Một tài khoản / Owner |
| Q7 | Tiện nghi gắn với homestay hay với từng loại phòng? | `HomestayAmenities` | Theo homestay |
| Q8 | Chủ homestay có được đổi sang phòng khác cùng loại khi xác nhận (phòng hệ thống gán đang có vấn đề) không? | `Bookings.RoomId`, `RoomNights` | Cho phép, đổi trong một transaction; ghi lịch sử |
| Q9 | Fuji dùng dịch vụ nào (SePay / Casso / khác) khi triển khai thật? Phí dịch vụ? Ngân hàng: **MB Bank** (đã chốt) | Chỉ cấu hình, không đổi DB | BTL dùng giả lập `Simulator`; số tài khoản MB để trong `appsettings` |
| Q10 | Khách chuyển **dư** tiền cọc: hoàn ngay hay trừ vào số tiền trả tại homestay? | `BankTransactions.Note`, `Payments` | Ghi chú lại, hoàn khi khách yêu cầu |
