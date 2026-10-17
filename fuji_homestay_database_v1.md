# Fuji Homestay — Thiết kế Database v1.10

> **Phiên bản:** 1.10 — 2026-10-16 (`EnumAndReviewChecks`: thêm 16 CHECK, schema code/database thử có 9 migration và 60 CHECK; CHƯA áp migration này lên v5)\
> 1.9 — 2026-10-16 (`ChangeRequestWorkflow` đã áp lên `HomestayBooking_v5`: 14 cột mới, 12 CHECK trong phạm vi, 5 FK, 6 index; toàn schema có 8 migration, 44 CHECK)\
> 1.8 — 2026-10-13 (nhất quán trạng thái: 4 CHECK — `ConfirmedAt` của đơn theo trạng thái; yêu cầu đổi/hủy: người gửi `Customer`/`Employee`, trường bắt buộc theo loại, `DecidedAt` theo trạng thái — migration `StatusConsistency`)\
> 1.7 — 2026-10-13 (siết ràng buộc: 7 CHECK — tỷ lệ cọc, hoa hồng của đơn và mục đối soát, bill tay phải có ảnh, đối soát đã gửi phải có `SentAt`, loại/trạng thái yêu cầu đổi/hủy; DEFAULT 0 cho `HomestayImages.SortOrder` — migration `DatabaseHardening`; tài liệu sửa `Notifications.AccountId` → `UserId` cho khớp schema sẵn có, không đổi database)  
> 1.6 — 2026-10-12 (màn kế toán: migration `AccountingGuards` thêm 2 index trên `Payments` — `IX_Payments_BookingId` và `IX_Payments_OneRefundPerBooking` (UNIQUE một khoản hoàn mỗi đơn); concurrency token `BankTransactions.MatchStatus` ở cấu hình EF Core)  
> 1.5 — 2026-10-12 (rà soát: thêm CHECK cho đơn, đối soát, giao dịch hoàn; mốc gửi/xác nhận/chuyển tiền đối soát; tiền hoàn giao dịch không khớp — migration `ReviewFixes`)  
> 1.4 — 2026-10-11 (thêm **voucher** — Q4, D14, migration `Vouchers`)  
> 1.3 — 2026-10-11 (giữ phòng 15 phút khi đặt; cọc qua QR **MB Bank** tự đối soát, ảnh bill làm dự phòng; **đã áp vào code**, migration `Init`)
> **Backend B6 — 17/10/2026:** ReportService, AccountAdminService, kiểm xóa danh mục và dữ liệu mẫu 2028; không thêm schema/migration, chưa thêm giao diện B6. Top-up đã thử CHỈ trên DeployTest, chưa chạy trên v5.\
> **Trạng thái:** Đã chốt các quyết định ở mục 1; dùng làm cơ sở viết ERD và sửa code (EF Core Code First).
> **Cập nhật triển khai 16/10/2026:** Nghiệp vụ đổi/hủy đã chốt 14–15/10 ở mục 5.E, Q11 và `docs/change-request-design.md`; migration `20261015092139_ChangeRequestWorkflow`, service và giao diện đã được triển khai trong code. Migration đã áp lên `HomestayBooking_v5` ngày **16/10/2026** bằng `deploy.sql`; `verify.sql` đạt 96 dòng OK, toàn schema có 45 bảng, 382 cột, 76 FK, 44 CHECK bật và được tin cậy, 93 index ngoài khóa chính.
> **Backup và restore thử:** `HomestayBooking_v5_20261016_pre-ChangeRequestWorkflow.bak` (`COPY_ONLY, CHECKSUM`), lưu tại `~/Backups/HomestayBooking/`, kèm `.sha256`; đã qua `RESTORE VERIFYONLY WITH CHECKSUM` và restore thử sang database mới **`HomestayBooking_v5_RestoreCheck3`** với file riêng, không `REPLACE`. CHECKDB không lỗi; bản restore có 7 migration, cuối `StatusConsistency`, và snapshot 45 bảng khớp v5 trước deploy. SHA-256: `9fe5e8a769229b19b2cddb010cd6d2cb2dcb292fabe90dc564c5ae5bd6fc87bc`.
> **Đối chiếu trước khi bật lại app:** 44 bảng nghiệp vụ giữ số dòng; checksum theo 368 cột cũ chỉ đổi ở `BookingChangeRequests`, đúng **Id 3: Approved → Applied**, mọi trường khác giữ nguyên; Pending **Id 1/4/5** giữ nguyên. `Bookings.ChangeFeeAmount` toàn 0, 13 cột mới khác toàn NULL. Riêng `__EFMigrationsHistory` giữ 7 dòng cũ và thêm migration thứ tám. Đây là kết quả trước khi app seed/job chạy lại; xem [biên bản triển khai](database/README.md#đã-triển-khai-changerequestworkflow-lên-v5--16102026).
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
| D14 | **Voucher xin lỗi khách** (Q4): chỉ quản lý cấp, gắn với một khách; giá trị = 5% tổng đơn gốc (50.000–200.000đ), hạn 6 tháng; có **số dư**, dùng nhiều lần. Mỗi đơn chỉ giảm tối đa **20% hoa hồng Fuji** của đơn đó (Fuji nhận 300k hoa hồng thì chịu tối đa 60k), trừ vào tiền cọc. Fuji chịu toàn bộ, homestay nhận đủ | Bảng `Vouchers`; `Booking.VoucherId`, `Booking.DiscountAmount` |

**10 quyết định chốt ngày 16/10/2026, Prompt 10 — backend phải tuân thủ, không tự mở rộng schema:**

| # | Quyết định | Thực hiện |
|---|---|---|
| D15 | Chủ xác nhận đối soát không bắt buộc; Paid đi từ Sent hoặc Confirmed | Giữ CHECK hiện tại; không thêm CHECK Settlements.ConfirmedAt |
| D16 | Người gửi/xác nhận đối soát ghi vào AuditLog | Không thêm cột actor trên Settlements |
| D17 | OwnerRegistration chỉ duyệt người chủ; từng Homestay duyệt riêng qua Status; giấy tờ pháp lý gắn Homestay | Không thêm cột liên kết hồ sơ–homestay; giữ liên kết giấy tờ cũ, backend áp quy tắc pháp lý cho hồ sơ mới |
| D18 | SubmittedAt là lúc nộp đủ hồ sơ | Tính SLA ba ngày làm việc từ mốc này, không thêm cột |
| D19 | Giá/phòng/ảnh/mô tả sửa trực tiếp và ghi AuditLog; sửa địa chỉ/giấy tờ pháp lý đưa Homestay về PendingReview | Backend kiểm quyền và chuyển trạng thái; không tạo bảng phiên bản nội dung |
| D20 | Hợp đồng phải Active và EndDate >= CheckOut của đơn | Backend kiểm khi báo giá/đặt/đổi; không chỉ kiểm ngày đặt |
| D21 | Chưa hỗ trợ hợp đồng tương lai | Giữ enum/index một hợp đồng Active, chưa thêm trạng thái chờ hiệu lực |
| D22 | Tắt phòng chặn bán mới, giữ các đơn đã nhận | Không xóa lịch hoặc hủy đơn khi đổi IsActive |
| D23 | Doanh số nhân viên theo CreatedByEmployeeId; công việc xử lý theo AssignedEmployeeId | Backend/báo cáo dùng đúng hai cột, giao lại không đổi người tạo; tỷ lệ chốt = số đơn mình tạo có ConfirmedAt / tổng số đơn mình tạo (chốt 17/10/2026), không theo lượt hỏi |
| D24 | Chủ từ chối lịch mới sau đổi không tự hoàn phí; Manager có thể cấp voucher | Dùng Vouchers hiện có, không thêm khoản hoàn phí tự động |

AuditLog được ghi **trực tiếp trong service**, cùng transaction với nghiệp vụ; không làm `SaveChangesInterceptor`. Các quyết định D15–D24 là đầu vào cho backend, không khẳng định toàn bộ backend đã được triển khai trong Prompt 10.

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
| `CreatedAt`, `UpdatedAt` | Theo từng entity; không thêm vào bảng nối/đêm phòng chỉ để đồng nhất tên cột |
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
| | `Vouchers` | ⭐ | **Mới** — voucher xin lỗi khách (Q4, D14) |
| | `Settlements` | ⭐ | Sửa: kỳ theo năm/tháng/số kỳ, trạng thái |
| | `SettlementItems` | ⭐ | **Mới** |
| **G. Khác** | `Reviews` | ⭐ | Sửa: thêm `HomestayId`, trả lời của chủ |
| | `Notifications` | ⭐ | Giữ |
| | `MessageLogs` | ○ | **Mới** |
| | `AuditLogs` | ○ | **Mới** |

**Tổng:** 40 bảng của ứng dụng (37 ⭐, 3 ○), tính cả `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` cộng 4 bảng phụ sẵn có của ASP.NET Identity (`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`) → **44 bảng** trong migration.

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
| SubmittedAt, ReviewedAt | datetime2 | NULL; SubmittedAt là lúc nhận đủ hồ sơ (D18) |
| | | **UNIQUE (OwnerId) WHERE Status IN (Draft … NeedsRevision)** — mỗi Owner chỉ một hồ sơ đang xử lý |

CHECK `CK_OwnerRegistrations_Status` lấy đủ enum. `CK_OwnerRegistrations_Decision`: NeedsRevision/Approved/Rejected phải có ReviewedByEmployeeId và ReviewedAt; NeedsRevision/Rejected phải có ReviewNote khác NULL, khác rỗng. Draft/WaitingOwnerActivation/PendingReview/UnderReview không buộc xóa dấu duyệt cũ khi gửi lại. Hồ sơ này duyệt người chủ; pháp lý và trạng thái kinh doanh được xét riêng theo từng Homestay (D17).

**Backend B1 (service + test; chưa có giao diện tiếp nhận/thẩm định):** `OwnerRegistrationService` tự đăng ký SĐT + mật khẩu tạo Person/Owner/Account role Owner, hồ sơ Draft và Homestay Draft. Không tự ghép SĐT đã thuộc Owner/Account khác. Nhân viên tạo thay không tạo mật khẩu; gửi mời tạo Account PendingActivation không mật khẩu, hồ sơ WaitingOwnerActivation. Chủ dùng token mời + SĐT để tự đặt mật khẩu, trở lại Draft rồi tự nộp. Đây là shortcut chưa xác minh OTP; token/mật khẩu không ghi vào AuditLog.

Sau review B1: có `ResendInviteAsync` cho WaitingOwnerActivation + Account PendingActivation chưa có mật khẩu; đổi security stamp để vô hiệu token cũ, không ghi token vào audit. Nhân viên chỉ sửa thông tin/ngân hàng và giấy tờ của hồ sơ EmployeeAssisted khi chủ chưa kích hoạt (chưa Account hoặc PendingActivation); sau kích hoạt chỉ chủ được sửa. Thông báo chủ dùng tiếng Việt, không chứa ReviewNote, tối đa 500 ký tự. Audit Insert của OwnerRegistrations/Homestays/Contracts/VerificationDocuments dùng Id thật, lưu entity rồi audit trong cùng transaction mặc định; unique index vẫn chặn trùng.

Bảng chuyển trạng thái của `CanTransition`:

| Từ | Đến | Người thực hiện |
|---|---|---|
| Draft | WaitingOwnerActivation | Consultant/Manager/Administrator gửi mời hồ sơ tạo thay |
| WaitingOwnerActivation | Draft | Chủ tự kích hoạt |
| Draft / NeedsRevision | PendingReview | Chủ nộp đủ hồ sơ; SubmittedAt là lúc nộp/nộp lại |
| PendingReview | UnderReview | Consultant/Manager nhận thẩm định |
| UnderReview | NeedsRevision | Consultant/Manager; ghi chú bắt buộc |
| UnderReview | Approved / Rejected | Manager/Administrator, khác người tạo thay; từ chối bắt buộc lý do |

Nộp mới cần họ tên, SĐT, đủ ba thông tin ngân hàng và ít nhất một Homestay Draft đủ thông tin, BusinessLicense + FireSafety + PropertyPhoto còn hạn. Chỉ các homestay đủ hồ sơ chuyển PendingReview; nháp chưa đủ giữ nguyên. Nộp lại sau NeedsRevision cũng xét các cơ sở PendingReview đang bổ sung. Thiếu điều kiện trả danh sách, không đổi trạng thái. Approve chỉ duyệt người chủ, không làm Homestay Active; cập nhật security stamp để claim OwnerApproved được tạo lại ở lần đăng nhập/làm mới. Reject đưa các cơ sở PendingReview của chủ về Draft (schema không liên kết trực tiếp hồ sơ–cơ sở, D17).

SLA: `ReviewDeadlineLocal` cộng ba ngày làm việc sau ngày nộp, giữ giờ VN, bỏ thứ Bảy/CN và Holidays. `BookingTimeoutJob` gọi `AlertOverdueAsync` cho PendingReview/UnderReview quá hạn; bỏ NeedsRevision. Thông báo tất cả Manager đang làm việc có Account Active, chống lặp bằng Notification.Url của hồ sơ + CreatedAt sau SubmittedAt. Nộp lại đổi SubmittedAt nên cảnh báo lại được. Audit trực tiếp cùng nghiệp vụ, có JSON cũ/mới và AccountId; job dùng actor System/AccountId NULL.

#### `VerificationDocuments` — giấy tờ xác minh (**mới**)

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| OwnerRegistrationId | int | FK, NULL | Giấy tờ nộp kèm hồ sơ đăng ký |
| HomestayId | int | FK, NULL | Giấy tờ của một homestay (khi Owner thêm homestay mới sau này) |
| DocumentType | nvarchar(30) | NOT NULL | `BusinessLicense`, `FireSafety`, `PropertyPhoto`, `Other` |
| FileUrl | nvarchar(300) | NOT NULL | Đường dẫn tệp riêng; backend phải bảo vệ quyền đọc, không công khai giấy tờ trong wwwroot |
| OriginalFileName | nvarchar(255) | NULL | |
| ExpiryDate | date | NULL | Ngày hết hạn giấy phép |
| | | CHECK **đúng một** trong `OwnerRegistrationId`, `HomestayId` khác NULL | |

Nguồn: v5 mục 4.6 ("bảng tài liệu xác minh riêng"); BR-12 (giấy phép kinh doanh lưu trú, PCCC, ảnh thực tế).

DocumentType có CHECK whitelist. Theo D17, BusinessLicense/FireSafety/PropertyPhoto mới gắn HomestayId, không gắn OwnerRegistrationId. Upload/xóa qua B1 chỉ khi hồ sơ Draft/WaitingOwnerActivation/NeedsRevision và cơ sở Draft, hoặc hồ sơ NeedsRevision và cơ sở PendingReview; hồ sơ PendingReview/UnderReview khóa sửa cả khi cơ sở còn Draft; Active/Suspended/Ended luôn bị chặn; ExpiryDate nếu có >= ngày VN hiện tại. B1 dùng ReceiptFileStore kiểm chữ ký JPG/PNG/WEBP và giới hạn 5 MB, lưu ngoài wwwroot; xóa bản ghi giấy tờ vẫn giữ file riêng tư phục vụ truy vết.

**Dữ liệu cũ (B1.H):** chuẩn đủ giấy tờ chỉ áp khi nộp mới qua service. Homestay Active và giấy tờ cũ gắn OwnerRegistrationId giữ nguyên, không chuyển liên kết, không bắt bổ sung PropertyPhoto. Đặt phòng, ký/gia hạn hợp đồng không kiểm lại giấy tờ cũ; service chỉ kiểm Owner đã Approved khi tạo hợp đồng. Không quét toàn bộ homestay để đổi trạng thái vì thiếu hồ sơ.

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
| Latitude, Longitude | decimal(9,6) | NULL | Thông tin bản đồ sau xác nhận (BR-13); tin nhắc FR-23 chỉ gửi link chi tiết |
| DescriptionHtml | nvarchar(max) | NULL | Đã lọc bằng HtmlSanitizer |
| CheckinGuideHtml | nvarchar(max) | NULL | Hướng dẫn trên chi tiết đơn sau xác nhận, không gửi qua tin ngoài |
| ContactPhone | nvarchar(16) | NULL | E.164; chỉ gửi khách sau khi xác nhận (BR-13) |
| CheckinTime, CheckoutTime | time | NOT NULL; 14:00 / 12:00 là mặc định **trong C#** (`Homestay`), database không có DEFAULT | |
| Status | nvarchar(20) | NOT NULL | `Draft`, `PendingReview`, `Active`, `Suspended`, `Ended` |
| ApprovedByEmployeeId | int | FK, NULL | FR-27 |
| ApprovedAt | datetime2 | NULL | |

- **Bỏ** `CommissionRate` (chuyển sang `Contracts`) và `IsActive` (thay bằng `Status`).
- Chỉ homestay `Active` **và** có hợp đồng đang hiệu lực mới hiện cho khách tìm kiếm. Backend phải kiểm thêm EndDate >= CheckOut khi đã chọn ngày (D20).
- Owner chỉ thêm/sửa homestay khi hồ sơ đăng ký đã `Approved` (policy `ApprovedOwner`).

**Backend B2 (service + test, chưa có giao diện quản lý mới):**

- Quyền chung tại `HomestayAccessService.AuthorizeAsync`: chủ có Account Active, không phải nhân viên, sửa cơ sở của mình chưa Ended. Consultant chỉ sửa Draft/PendingReview khi thỏa quy tắc B1: EmployeeAssisted, chủ chưa có Account hoặc PendingActivation. Manager/Administrator đang làm việc hỗ trợ cơ sở chưa Ended, nhưng không vượt khóa hồ sơ đang duyệt. Khi có hồ sơ thuộc OpenStatuses, mọi actor (kể cả Manager) chỉ sửa nếu hồ sơ Draft/WaitingOwnerActivation/NeedsRevision và homestay Draft, hoặc hồ sơ NeedsRevision và homestay PendingReview. B1 UpdateDraftAsync, EditableHomestayAsync và B2 dùng chung EnsureDraftEditable; PendingReview/UnderReview khóa nội dung, vị trí, ảnh, phòng, giá, chính sách và giấy tờ. Quyền nhân viên tạo thay trước kích hoạt của B1 vẫn áp dụng. Accountant/chủ khác bị chặn; lịch đọc được cả cơ sở đã Ended của đúng chủ hoặc quản lý.
- `HomestayContentService`: sửa tên, liên hệ E.164, giờ nhận/trả, mô tả/hướng dẫn (HtmlSanitizer), thay danh sách tiện nghi hợp lệ; không tự đổi trạng thái. Đổi Address/AreaId/Latitude/Longitude khi Active đưa về PendingReview; Suspended lưu và giữ Suspended, chỉ mở lại qua ResumeAsync sau khi kiểm đủ điều kiện; Draft/PendingReview chỉ lưu. Manager nhận thông báo theo nhóm mã đơn Confirmed/Deposited sắp tới để liên hệ khách (BR-13). Không đổi trạng thái/phòng/giá/lịch của các đơn đã nhận.
- `RoomService`: tạo loại phòng cần danh mục hợp lệ, sức chứa > 0 và đủ ba giá Weekday/Weekend/Holiday > 0 hiệu lực hôm nay. Tên loại unique trong cơ sở, mã phòng unique trong loại. Không xóa cứng phòng/loại, chỉ bật/tắt. Active không được tắt phòng/loại phòng cuối cùng khiến không còn loại bật có phòng bật; báo dùng khóa ngày hoặc đề nghị Manager tạm ngưng. Draft/PendingReview/Suspended vẫn tắt được. Tắt chỉ chặn bán/chọn mới, trả mã đơn sắp tới; đơn và RoomNights cũ giữ nguyên. FreeRoomIds, chọn phòng khi Confirm, chọn đích Quote/Submit/Manager Approve/Finance Confirm và tìm kiếm đều kiểm phòng/loại đang bật; xác nhận nguyên phòng cũ của đơn đã nhận vẫn được.
- Giá: thêm RoomTypePrice với EffectiveFrom >= ngày VN hiện tại. Cùng khóa (loại, loại ngày, ngày hiệu lực) chỉ cập nhật nếu ngày đó còn ở tương lai; dòng đã hiệu lực (<= hôm nay) không sửa/xóa. Đơn giữ BookingNights cũ, PricingService chọn phiên bản gần nhất <= từng đêm. Có thể thêm phiên bản hôm nay nếu chưa có dòng khóa đó, không ghi đè phiên bản đã có.
- `HomestayImageService`: ảnh công khai lưu `wwwroot/uploads/homestays/{id}/` bằng GUID, kiểm dung lượng <= 5 MB và magic bytes dùng chung ReceiptFileStore. Gắn RoomType phải cùng cơ sở; hỗ trợ thứ tự/ảnh bìa. Đổi bìa thả unique bằng SaveChanges trước khi bật bìa mới, cùng transaction. Active không xóa bìa duy nhất; xóa file quản lý sau commit DB, lỗi lưu thì dọn file vừa tạo; không xóa ảnh demo/URL ngoài.
- `HomestayDocumentService` dùng chung pipeline `StoreDocumentAsync` của B1, không chuyển liên kết cũ. Active/Suspended chỉ tải thêm pháp lý BusinessLicense/FireSafety: Active về PendingReview, Suspended giữ Suspended; cả hai báo Manager; không xóa bản cũ. PropertyPhoto/Other trên Active lưu thẳng, không tự đưa về PendingReview. B1 API tiếp nhận vẫn giữ giới hạn giấy tờ như mục B1; B2 không mở thao tác xóa trên Active/Suspended/Ended.
- `HomestayPolicyService`: thay chính sách riêng bằng tắt phiên bản cũ → SaveChanges → tạo mới và rules trong cùng transaction; không sửa rules cũ kể cả đã có đơn dùng. Bỏ chính sách riêng là tắt để quay về chung Fuji. Rules không trùng mốc, có mốc 0, ngày >= 0, % 0–100, hủy càng sớm thì % không giảm. RefundService của đơn cũ vẫn dùng CancellationPolicyId cũ.
- `HomestayReviewService`: chủ Approved thêm Draft và tự nộp Draft → PendingReview: cơ sở mới cần đủ ba giấy tờ còn hạn gắn cơ sở; cơ sở đã từng duyệt (ApprovedAt khác NULL) chỉ cần giấy phép/PCCC còn hạn, nhận liên kết hồ sơ Approved cũ cùng chủ, không bắt PropertyPhoto; không tạo OwnerRegistration mới. Danh sách chờ lấy mốc chuyển PendingReview gần nhất từ AuditLog, không lấy lần sửa nội dung muộn hơn; lịch sử thiếu audit để mốc NULL, xếp trước để xử lý. Không thêm SLA cho homestay.
- Manager/Administrator duyệt PendingReview hoặc mở lại Suspended khi đủ: chủ Approved; hợp đồng Covers(hôm nay, hôm nay); giấy phép/PCCC còn hạn (cơ sở cũ nhận giấy tờ gắn hồ sơ Approved cùng chủ, không yêu cầu PropertyPhoto lại); ít nhất một loại bật có phòng bật và đủ ba giá hôm nay; ảnh bìa; địa chỉ/liên hệ/mô tả không rỗng. Thiếu trả danh sách, không đổi trạng thái. Thành công đặt ApprovedByEmployeeId/ApprovedAt, audit và báo chủ.
- Từ chối PendingReview → Draft, tạm ngưng Active → Suspended, mở lại Suspended → Active đều do Manager/Administrator; lý do bắt buộc cho từ chối/tạm ngưng/mở lại, chỉ lưu audit, không ghép vào Message. Ended chặn hợp đồng Active và đơn HoldsRoom trừ Completed. Suspended/Ended không bán mới, đơn đã nhận giữ nguyên.

| Từ | Đến được phép qua `HomestayReviewService.CanTransition` |
|---|---|
| Draft | PendingReview, Ended |
| PendingReview | Active, Draft, Ended |
| Active | PendingReview, Suspended, Ended |
| Suspended | Active (chỉ ResumeAsync), Ended |
| Ended | Không có |

Chỉ sửa vị trí/pháp lý trên Active mới gọi chốt chuyển về PendingReview; Suspended không được tự sang PendingReview, ApproveAsync không mở lại được. Yêu cầu đổi mới (QuoteAsync/SubmitAsync) cần homestay Active; CanChangeAsync trả false khi cơ sở không Active. Với yêu cầu đã gửi, Manager ApproveAsync và Accountant ConfirmAsync cho tiếp tục khi homestay PendingReview qua tham số allowPendingReview của TargetAsync; vẫn kiểm hợp đồng D20 và phòng/loại phòng đang bật. Suspended/Ended chặn cả yêu cầu mới và xử lý yêu cầu đã gửi. Hủy đơn giữ luồng hiện tại.

Audit B2 dùng PartnerServiceSupport.Audit, RecordId là Id thật sau SaveChanges tạo mới, JSON cũ/mới và actor thật, cùng transaction mặc định. Thông báo tiếng Việt qua PartnerServiceSupport.Notify, <= 500 ký tự. Không thêm schema/migration; kiểm dữ liệu bằng SELECT ở data_consistency.sql, nay 19 mục.

**Backend B3 (service + test, chưa có giao diện quản trị mới):**

- `AvailabilityService.BookableHomestays` và `SellableRooms` là chốt bán chung: homestay Active, hợp đồng `Covering(hôm nay, CheckOut)` (không có ngày thì đến hôm nay), loại/phòng IsActive; không có RoomNights trong `[CheckIn, CheckOut)`. Đêm đơn, khóa ngày và giữ yêu cầu đổi đều chiếm phòng. HomestaysController Index/Details, HomeController phần nổi bật và BookingService.FreeRoomIdsAsync dùng lại; Details vẫn có thể hiện loại hết phòng với số trống bằng 0.
- `SearchAsync`: khu vực/từ khóa, sức chứa, danh mục, đủ tất cả tiện nghi chọn, giá min/max; mặc định tên như cũ, hoặc giá tăng/giảm, điểm trung bình Review không ẩn. Có ngày: FromPrice của thẻ, lọc MinPrice/MaxPrice và sắp xếp giá dùng trung bình mỗi đêm = RoundToThousand(tổng kỳ ở / số đêm) của loại rẻ nhất còn trống và đủ sức chứa; không ngày giữ giá thường hiện hành thấp nhất. AvailabilityOffer.TotalPrice vẫn là tổng kỳ ở cho nhân viên và đề xuất trùng phòng. Giá và Holidays nạp theo lô; `PricingService.QuoteFromPrices` dùng chung công thức từng đêm/EffectiveFrom với QuoteAsync, không gọi QuoteAsync trong vòng lặp. Thuộc tính lọc mới chưa có giao diện mới; giá thẻ và bộ lọc khớp nhãn `/đêm` của view sẵn có, không sửa view.
- `StaffAvailabilityAsync`: Consultant/Manager/Administrator đang hoạt động, tối đa 31 đêm, lọc khu vực/số khách; mỗi cơ sở × loại trả số trống/tổng phòng đang bán, tổng giá kỳ ở, sức chứa. Một truy vấn RoomNights cho toàn khoảng, không cache, vẫn trả loại đã kín với số trống 0.
- `StaffBookingService`: tìm mã, ContactName, tên khách, SĐT E.164 (so cả ContactPhone và số khách), trạng thái/cơ sở/ngày nhận/người phụ trách/chưa giao; phân trang. Shortcut tìm Contains chấp nhận quét, không thêm index. Chi tiết có lịch sử, yêu cầu đổi, Payments và audit đơn theo thời gian, kèm BookingNights.
- Giao đơn: Manager/Administrator giao mọi đơn; Consultant tự nhận đơn chưa giao hoặc giao lại đơn mình phụ trách. Người nhận chỉ Consultant/Manager đang làm việc, Account Active; không đổi CreatedByEmployeeId (D23). UPDATE có điều kiện người phụ trách cũ chặn hai người cùng nhận. Audit cũ/mới và thông báo người nhận.
- Nhân viên xác nhận (có thể đổi phòng), từ chối, CheckedIn/Completed/NoShow thay chủ sau khi gọi điện: bắt buộc ghi chú, dùng BookingService, history ghi actor Employee và tài khoản thật. Không có đường hủy trực tiếp hoặc đổi trạng thái tiền; hủy vẫn dùng yêu cầu hiện có. Luật ngày nhận/NoShow giữ nguyên.
- `CustomerService`: Consultant/Manager/Administrator tìm tên/SĐT/email, xem đơn và voucher còn dùng được. Note tối đa 1000 ký tự: null giữ ghi chú cũ, chuỗi rỗng xóa ghi chú (lưu NULL); không đổi số định danh. Chỉ sửa tên/email của khách chưa có Account; khách có Account tự sửa. DTO che CitizenId/StudentId cho Consultant, chỉ giữ bốn ký tự cuối (giá trị quá ngắn che hết); Manager/Administrator xem đủ. Không trả entity Profile thô.
- `OverbookingService`: báo sự cố chỉ khi Confirmed, chưa đến CheckOut, không có yêu cầu đổi/hủy mở. Chủ đúng cơ sở hoặc nhân viên báo; thông báo người phụ trách + Manager, ghi audit. Đề xuất dùng AvailabilityService, ưu tiên cùng homestay → cùng khu vực → nơi khác, có tổng giá/chênh lệch/điểm; không gửi địa chỉ/SĐT/hướng dẫn cơ sở mới trước xác nhận.

| Cách xử lý trùng phòng | Đơn/phòng | Cọc và voucher | Fuji / khoản tiền |
|---|---|---|---|
| 1. Chuyển phòng cùng loại, cùng cơ sở (Consultant/Manager/Administrator) | Giữ mã, trạng thái Confirmed, chuyển RoomNights trong transaction; history “Đổi phòng do trùng phòng” | Giữ nguyên tổng, cọc, giảm voucher, hoa hồng | Không tạo Payment hay khoản bù |
| 2. Đơn thay thế khác loại/cơ sở (Consultant/Manager/Administrator) | Tạo N bằng đường CreateInTransactionAsync chung; giá đêm, hoa hồng/hủy theo cơ sở mới. Đơn cũ Cancelled → ReplacedByBookingId=N, nhả đêm; N Deposited chờ chủ mới xác nhận BR-06 | Chuyển nguyên DepositRate/DepositAmount/VoucherId/DiscountAmount, không trừ voucher lần hai và không trả voucher ở đơn cũ. Chặn nếu tổng mới < cọc cũ. Giữ khách/người ở/ngày/kênh/người tạo; giao người xử lý | RefundAmount đơn cũ=0; Compensation trên N = max(0, tổng mới − tổng cũ), chỉ tạo khi >0, ManualTransfer/Submitted, B5 chi chuyển khoản riêng sau Completed, không cộng tổng đối soát; không trừ chủ cũ. SettlementCalculator loại toàn đơn có ReplacedByBookingId |
| 3. Không có phương án khách chấp nhận (Manager/Administrator) | Hủy, nhả phòng, không tạo N | Hoàn tiền khách thực chuyển = AmountDue; trả voucher 100% theo RestoreAsync hiện có (còn hiệu lực); cấp voucher xin lỗi theo D14 qua IssueAsync | Không tự chuyển tiền hoặc tạo Payment Refund; RefundAmount>0 vào hàng chờ kế toán hiện có |

Ở cách 2, nếu tổng mới cao hơn: phần khách còn trả = tổng mới − cọc chuyển − Compensation = tổng cũ − cọc cũ. Nếu tổng mới thấp hơn nhưng đủ cọc, phần còn trả giảm theo tổng mới. Voucher đã trừ ở cọc được giữ nguyên snapshot. Chủ mới từ chối N thì luồng Rejected hiện có hoàn 100% AmountDue và trả voucher; B3 tạo khoản bù Submitted; B5 chỉ chi khi N Completed, tự từ chối khi N kết thúc không ở/được thay tiếp. Không chuyển thông tin liên hệ mới trước khi N Confirmed. Transaction mặc định chứa đơn/counter/BookingNights/RoomNights/tiền/voucher/history/audit/thông báo; đụng PK đêm phòng thì rollback toàn bộ và trả lỗi tiếng Việt.

Dùng chung `BookingService.CreateInTransactionAsync`, `MoveRoomAsync`, `ApplyStatusCoreAsync`/`ApplyFujiCancellationAsync`; không mở đường Cancelled cho nhân viên ngoài service sự cố. Audit trực tiếp bằng PartnerServiceSupport, Id thật; không thêm schema/index/migration. `data_consistency.sql` giữ nguyên 19 mục B1/B2, thêm 4 mục: đơn được thay phải Cancelled; khách/ngày của N khớp; Compensation gắn N; người phụ trách đúng vị trí — tổng 23 mục, chỉ SELECT.

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

Gia hạn = lùi `EndDate` về sau trên hợp đồng `Active`, không đổi `CommissionRate`. Đổi tỷ lệ phải chấm dứt rồi tạo hợp đồng mới. Đơn đã đặt giữ snapshot `CommissionRate`.

Status có CHECK whitelist; chưa hỗ trợ hợp đồng tương lai (D21). B1 dùng `ContractEligibility.Covers/Covering` để kiểm `Active`, `StartDate <= hôm nay`, `EndDate >= CheckOut` khi đặt, báo giá/gửi/duyệt đổi và tìm kiếm có ngày trả; không có ngày thì kiểm đến hôm nay. `BookingsController` xem trước voucher cũng dùng ngày trả phòng.

`ContractService`: Manager/Administrator đang làm việc được tạo hợp đồng cho chủ Approved và homestay chưa Ended; số hợp đồng nhập tay, hoa hồng 10–15%, StartDate <= hôm nay, EndDate > hôm nay. Không có hợp đồng Active thứ hai. Gia hạn chỉ tăng EndDate. Chấm dứt cần lý do và chặn nếu còn đơn thuộc `HoldsRoom()` trừ `Completed`; lỗi trả mã đơn. Đơn Completed giữ đêm lịch sử nhưng không chặn chấm dứt (đã chốt sau review B1). Job chuyển Active có EndDate < ngày VN sang Expired, audit System, báo Manager; ngày EndDate vẫn hiệu lực đến hết ngày.

#### `Amenities`, `HomestayAmenities`

Giữ như code: `Amenities(Name UNIQUE, Icon)`, `HomestayAmenities(HomestayId, AmenityId)` khóa chính ghép, cascade khi xóa.

#### `HomestayImages` (gộp cả ảnh loại phòng)

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| HomestayId | int | FK, NOT NULL, cascade |
| RoomTypeId | int | FK, NULL — NULL = ảnh chung của homestay |
| Url | nvarchar(300) | NOT NULL |
| SortOrder | int | NOT NULL, **DEFAULT 0** ở database |
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

Giữ như code: `Date` (PK), `Name`. Fuji khai báo chung cho mọi homestay. Seed CSDL mới có lịch 2028: 01/01; 25–29/01 (Tết, mùng 1 = 26/01); 30/04; 01/05; 02/09. Giỗ Tổ để Admin bổ sung, không tự đoán lịch âm. Vẫn chỉ seed vào bảng ngày lễ rỗng; database cũ bổ sung qua script riêng, không tự top-up lúc khởi động.

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

**RoomBlockService B2 — [StartDate, EndDate):** chủ đúng cơ sở hoặc Manager/Administrator tạo khóa từ hôm nay, Reason OtherChannel/Maintenance, Note <= 255; sinh RoomNights với RoomBlockId. Trùng đơn/khóa/yêu cầu đổi trả danh sách mã + ngày; PK chặn tranh chấp lúc lưu, rollback toàn bộ và dịch lỗi tiếng Việt. Lịch đọc tối đa 62 ngày, một truy vấn RoomNights cho toàn khoảng, trả Trống/Đã đặt (mã, trạng thái)/Khóa (lý do)/Giữ cho yêu cầu đổi.

Mở khóa D3 đã duyệt: StartDate >= hôm nay xóa block (đêm cascade); StartDate < hôm nay < EndDate cắt EndDate về hôm nay và xóa đêm từ hôm nay, giữ lịch sử; EndDate <= hôm nay ném `BookingException("Khóa ngày đã kết thúc, không cần mở khóa.")`, không audit. Không tạo khoảng rỗng, giữ CK_RoomBlocks_Dates EndDate > StartDate. Tạo/mở khóa thành công có audit.

#### `RoomNights` — mỗi dòng là một phòng bị chiếm trong một đêm

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| RoomId | int | FK → Rooms, NOT NULL, Restrict |
| Date | date | NOT NULL |
| BookingId | int | FK → Bookings, NULL |
| RoomBlockId | int | FK → RoomBlocks, NULL, **cascade** |
| BookingChangeRequestId | int | FK → BookingChangeRequests, NULL, NO ACTION; giữ phòng mới chờ cọc bổ sung |
| | | **PRIMARY KEY (RoomId, Date)** |
| | | CHECK **đúng một** trong `BookingId`, `RoomBlockId`, `BookingChangeRequestId` khác NULL |

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
| | ConfirmedAt, CheckedInAt, CompletedAt | datetime2 | NULL. CHECK `CK_Bookings_ConfirmedAt`: `Confirmed`, `CheckedIn`, `Completed`, `NoShow` phải có `ConfirmedAt`; `AwaitingPayment`, `PaymentSubmitted`, `Deposited`, `Rejected`, `Expired` phải không có; `Cancelled` tùy (hủy trước hoặc sau xác nhận). `ConfirmedAt` chỉ được gán khi `Deposited → Confirmed` |
| | CancelledAt | datetime2 | NULL |
| | CancelReason | nvarchar(500) | NULL |
| Tiền (snapshot) | TotalAmount | decimal(18,0) | NOT NULL = Σ `BookingNights.Price` |
| | DepositRate | decimal(5,4) | NOT NULL, CHECK `0 < DepositRate ≤ 1` (`CK_Bookings_DepositRate`). Ứng dụng gán 0.3000 theo cấu hình `Booking:DepositRate` (BR-01); database không có DEFAULT để giá trị ứng dụng gửi không bị thay |
| | DepositAmount | decimal(18,0) | NOT NULL; CHECK `DepositAmount <= TotalAmount` |
| | CommissionRate | decimal(5,4) | NOT NULL — lấy từ hợp đồng đang hiệu lực; CHECK `0 < CommissionRate < 1` (`CK_Bookings_CommissionRate`, cùng khoảng với `Contracts`) |
| | CancellationPolicyId | int | FK, NOT NULL |
| | RefundAmount | decimal(18,0) | NULL — tính khi hủy (BR-04), trên số tiền khách **thực chuyển** (`DepositAmount − DiscountAmount`). CHECK `0 ≤ RefundAmount ≤ DepositAmount − DiscountAmount` |
| Voucher | VoucherId | int | FK → Vouchers, NULL (D14) |
| | DiscountAmount | decimal(18,0) | NOT NULL, mặc định 0 — số tiền voucher đã giảm (snapshot). CHECK `0 ≤ DiscountAmount < DepositAmount`; CHECK có `VoucherId` ⇔ `DiscountAmount > 0`. Số tiền trên QR / webhook so khớp = `DepositAmount − DiscountAmount` |
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
| RequestType | `nvarchar(32)`: `ChangeDates`, `ChangeRoomType`, `ChangeDatesAndRoomType`, `Cancel` — CHECK `CK_BookingChangeRequests_Type` (danh sách sinh từ enum `ChangeRequestType`) |
| RequestedByType | `Customer` / `Employee` — CHECK `CK_BookingChangeRequests_RequestedBy` (FR-05: khách gửi, hoặc nhân viên gửi thay) |
| NewCheckIn, NewCheckOut, NewRoomTypeId | CHECK `CK_BookingChangeRequests_Fields`: `Cancel` → cả ba NULL; `ChangeDates` → đủ hai ngày, ngày ra sau ngày vào và loại phòng NULL; `ChangeRoomType` → có loại phòng, hai ngày NULL; loại kết hợp → đủ hai ngày hợp lệ và loại phòng |
| PriceDifference | Chênh lệch tiền khi đổi (có thể âm) |
| RefundAmount | Số tiền hoàn dự kiến theo BR-04 — khách xem trước (FR-05) |
| Status | `Pending`, `AwaitingPayment`, `Applied`, `Rejected`, `Expired` — CHECK `CK_BookingChangeRequests_Status`; concurrency token ở EF Core |
| DecidedByAccountId, DecidedAt, DecisionNote | CHECK `CK_BookingChangeRequests_Decision`: `Pending` → người/thời điểm quyết định đều NULL; khác Pending → cả hai bắt buộc. CHECK Notes yêu cầu ghi chú khi đã quyết định |
| RequestedByAccountId, NewRoomId | FK nullable đến tài khoản gửi và phòng mới; NULL được giữ cho lịch sử cũ |
| ChangeFee, DepositCarriedOver, AdditionalDeposit, NewTotalAmount | `decimal(18,0)` nullable; CHECK Amounts kiểm không âm khi có giá trị, cùng RefundAmount |
| PaymentDeadlineAt | `datetime2` nullable; CHECK PaymentDeadline theo trạng thái và khoản cọc bổ sung; là hạn khách nộp, không phải hạn kế toán duyệt |
| FinanceConfirmedByAccountId, FinanceConfirmedAt, FinanceNote | Người/thời điểm/ghi chú kế toán; CHECK Finance bắt buộc khi Applied có AdditionalDeposit > 0; CHECK ActorSeparation tách người gửi, Manager và Accountant |
| RejectionReason | `nvarchar(500)` nullable; CHECK Notes cho lịch sử Rejected cũ dùng DecisionNote khi RejectionReason NULL |

Đổi ngày/đổi loại phòng được duyệt → service cập nhật đơn và `RoomNights` trong một transaction.

**Nghiệp vụ đã chốt 14/10/2026, bổ sung 15/10:**

- Cho phép đổi cả ngày lẫn loại phòng trong cùng một yêu cầu, khi còn phòng.
- Phí đổi ngày bằng phần cọc bị mất theo BR-04, lấy `RefundService.QuoteCancellationAsync` tại lúc gửi yêu cầu; cọc còn lại dùng cho **chính đơn sau khi sửa**, giữ nguyên mã. Không tính lại phí theo ngày duyệt.
- **Phí đổi ngày chia theo CommissionRate của đơn trên phần phí (chốt 15/10)**: cộng dồn `Bookings.ChangeFeeAmount`; `feeCommission = min(Round(Fsum × CommissionRate, 0, AwayFromZero), Fsum)`, chủ nhận `Fsum − feeCommission`. Đơn vẫn được ở và phần đặt phòng đã tính hoa hồng của cả đơn; không áp thêm công thức hủy `min(Round(TotalAmount × CommissionRate), cọc giữ lại)` lên phí để tránh tính hoa hồng cả đơn hai lần.
- Tiền cọc phải thêm = `max(0, RoundToThousand(DepositRate × tổng mới) − cọc chuyển sang)`; cọc sau sửa = cọc chuyển + đóng thêm. Xuống hạng không hoàn tiền mặt; nếu tổng mới nhỏ hơn cọc chuyển thì service **chặn**, không tạo bảng số dư cọc. **Chưa cho đổi đơn có DiscountAmount > 0**; báo “Đơn dùng voucher, vui lòng liên hệ nhân viên”. Hủy thật vẫn theo quy tắc hiện có; hỗ trợ đổi có voucher để sau nếu cần.
- Quản lý (`Manager`) duyệt nghiệp vụ; khi cần tiền thêm, kế toán (`Accountant`) xác nhận tiền trước khi áp dụng. Người gửi không được tự duyệt. Mỗi bước lưu người/thời điểm/ghi chú; từ chối bắt buộc có lý do.
- Loại phòng mới cùng homestay, đủ sức chứa, còn phòng, ngày hợp lệ: kiểm tra tại service. Giữ phòng mới trong lúc chờ tiền và áp dụng thay đổi/tiền/phòng/lịch sử trong một transaction; thông báo cho chủ homestay.
- Đơn Confirmed sau đổi ngày/phòng về Deposited, `ConfirmedAt = NULL`, chủ xác nhận lại theo BR-06; cặp trạng thái/mốc này thỏa CHECK hiện tại. Service đã có thao tác nội bộ có bảo vệ, không mở transition chuyển lùi cho mọi caller.
- `PaymentDeadlineAt` là hạn **khách nộp tiền/gửi bill**, không phải hạn kế toán. Nộp hợp lệ đúng hạn thì giữ phòng tới khi Accountant xử lý, dù `FinanceConfirmedAt` sau deadline. Hủy thật dùng `RefundAmount` chốt lúc gửi.

**Schema đã triển khai:** bản rút gọn tại [Thiết kế đổi/hủy](docs/change-request-design.md) đã được thực hiện bằng một migration `ChangeRequestWorkflow`, áp lên v5 ngày **16/10/2026**, cùng service và màn hình xử lý yêu cầu. Booking được sửa tại chỗ; `Applied` thay `Approved`; `PaymentType.AdditionalDeposit` có FK ChangeRequestId và unique riêng; RoomNights có chủ sở hữu yêu cầu. Dùng Status concurrency token và AuditLog sẵn có, không tạo bảng mới hoặc sổ chuyển cọc, không sửa index Deposit ban đầu hay làm FK ghép. Migration thêm 14 cột, 5 FK, 6 index; 12 CHECK trong phạm vi (5 sửa, 7 thêm), nâng tổng CHECK lên 44.

Ở `AwaitingPayment` chưa xác nhận kế toán; `Applied` có tiền thêm mới bắt buộc đủ xác nhận. Giới hạn cấu hình mặc định 3 ngày/2 lần đổi Applied trên chính Booking, ngày mới trong 90 ngày đã chốt cho bản rút gọn; options kiểm bằng Range + ValidateOnStart. Chỉ lưu NewTotalAmount, tính lại giá trước duyệt/áp dụng, khác giá thì từ chối có lý do. Migration đổi Approved → Applied nhưng giữ actor Owner của lịch sử đóng, không giả gán Manager. Pending thiếu RequestedByAccountId hoặc thiếu cột tiền là “yêu cầu cũ, cần gửi lại”: không sửa trong migration, hiện trên màn quản lý, Manager chỉ được từ chối với lý do thật, không được duyệt; vẫn chiếm unique yêu cầu mở tới lúc từ chối. Không có IsLegacy hoặc migration hai pha.

Các biểu thức CHECK mới dùng chuỗi thường và `IS NOT NULL AND <> ''`, không dùng hàm chuỗi riêng, dùng được trong model SQL Server/SQLite. Khác biệt đã biết: chuỗi chỉ có khoảng trắng bị SQL Server chặn, SQLite cho qua; service vẫn kiểm chuỗi trắng. Giữ Status concurrency token cho duyệt; job hết hạn dùng một UPDATE có điều kiện AwaitingPayment, quá hạn và NOT EXISTS khoản nộp hợp lệ, chỉ nhả phòng/ghi AuditLog khi cập nhật thành công trong cùng transaction. Đường nộp tiền kiểm trạng thái có điều kiện trong transaction để tuần tự với job, không dùng lock hint. **Đã triển khai database, service và giao diện; migration đã áp v5 ngày 16/10/2026.**

---

### F. Tiền

#### `Payments`

| Cột | Kiểu | Ràng buộc | Ghi chú |
|---|---|---|---|
| BookingId | int | FK, NOT NULL | Thanh toán **gắn với từng đơn** |
| Type | nvarchar(20) | NOT NULL | `Deposit`, `Refund`, `Compensation`, `AdditionalDeposit`; CHECK whitelist |
| Method | nvarchar(20) | NOT NULL | `BankAutoMatch` (tự khớp từ giao dịch ngân hàng, D13), `ManualReceipt` (ảnh bill, kế toán duyệt, D11), `ManualTransfer` (Fuji chuyển ra: hoàn cọc, bù chênh lệch) |
| BankTransactionId | int | FK → BankTransactions, NULL; **UNIQUE WHERE NOT NULL** | Một giao dịch ngân hàng chỉ thành **một** khoản thanh toán |
| Amount | decimal(18,0) | NOT NULL, CHECK > 0 | Số tiền thực nhận (tự khớp) hoặc số tiền theo bill |
| ReceiptImageUrl | nvarchar(300) | NULL — **bắt buộc khi `Method = ManualReceipt`**: CHECK `CK_Payments_Receipt` (`Method <> 'ManualReceipt' OR (ReceiptImageUrl IS NOT NULL AND ReceiptImageUrl <> '')`) | **Ảnh bill chuyển khoản** (D11). Với `Refund`: ảnh bill Fuji hoàn tiền (tùy chọn) |
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
- Mỗi đơn hoàn cọc **một lần**: **UNIQUE (BookingId) WHERE Type = 'Refund'** — hai kế toán cùng bấm "Đã hoàn" thì lần sau bị chặn.
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
| MatchStatus | nvarchar(20) | NOT NULL | Xem bảng dưới. **Concurrency token**: hai kế toán xử lý cùng giao dịch → chỉ một người thành công |
| MatchedBookingId | int | FK → Bookings, NULL | Đơn tìm được từ nội dung chuyển khoản |
| ProcessedByEmployeeId | int | FK, NULL | Người xử lý tay (khớp tay, hoàn tiền, bỏ qua) |
| ProcessedAt | datetime2 | NULL | |
| RefundedAt | datetime2 | NULL — CHECK bắt buộc khi `MatchStatus = Refunded` | Lúc Fuji chuyển trả khoản này. Ghi ở đây vì giao dịch `Unmatched` không có đơn để tạo `Payments(Refund)` |
| RefundBankRef | nvarchar(100) | NULL | Mã giao dịch chuyển trả |
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

#### `Vouchers` — voucher xin lỗi khách (**mới**, Q4, D14)

| Cột | Kiểu | Ràng buộc / ghi chú |
|---|---|---|
| Id | int | PK |
| Code | nvarchar(20) | NOT NULL, UNIQUE — `VC` + 8 ký tự (bỏ 0/O, 1/I) |
| CustomerId | int | FK, NOT NULL — chỉ khách này dùng được |
| Amount | decimal(18,0) | NOT NULL, CHECK > 0 — giá trị lúc cấp |
| Balance | decimal(18,0) | NOT NULL, CHECK `0 ≤ Balance ≤ Amount`. **Concurrency token**: hai đơn dùng cùng lúc → đơn sau phải thử lại |
| ExpiresOn | date | NOT NULL = ngày cấp + 6 tháng |
| Status | nvarchar(20) | `Active` / `UsedUp` / `Expired` / `Revoked` |
| Reason | nvarchar(500) | NOT NULL |
| SourceBookingId | int | FK → Bookings, NULL; UNIQUE (lọc khác NULL) — mỗi đơn bị ảnh hưởng chỉ được cấp một voucher |
| IssuedByEmployeeId | int | FK, NOT NULL — Manager / Administrator (service) |
| IssuedAt | datetime2 | NOT NULL |
| RevokedAt, RevokeReason | datetime2, nvarchar(500) | NULL |

Quy tắc (service `VoucherService`, cấu hình `appsettings: Voucher`):
- Giảm cho một đơn = `MIN(Balance, làm tròn xuống nghìn(TotalAmount × CommissionRate × 20%))`; giảm 0đ thì không cho dùng.
- Trừ số dư trong **cùng transaction** với việc tạo đơn: đặt phòng lỗi thì voucher không bị trừ.
- Đơn không thành → cộng trả vào số dư (nếu voucher chưa hết hạn / chưa bị thu hồi):
  - Homestay từ chối, hết hạn giữ phòng, hủy khi chưa có cọc → trả 100% số đã giảm.
  - Hủy sau khi có cọc → trả **theo cùng tỷ lệ hoàn tiền** (hoàn 50% thì voucher cũng trả 50%), để khách dùng voucher không mất ít hơn khách không dùng.
  - Không đến (`NoShow`) → không trả.
- Job mỗi phút chuyển voucher quá `ExpiresOn` sang `Expired`.

#### `Settlements` — bảng đối soát một homestay một kỳ

| Cột | Kiểu | Ràng buộc |
|---|---|---|
| HomestayId | int | FK, NOT NULL |
| Year, Month | int | NOT NULL; CHECK Year BETWEEN 2020 AND 2100, Month BETWEEN 1 AND 12 |
| PeriodNo | int | CHECK IN (1, 2) — kỳ 1: ngày 1–15; kỳ 2: 16 – cuối tháng |
| TotalRoomAmount, TotalRetained, TotalCommission, PayoutAmount | decimal(18,0) | NOT NULL; CHECK ≥ 0, `TotalCommission ≤ TotalRetained`, `PayoutAmount = TotalRetained − TotalCommission` |
| Status | nvarchar(20) | CHECK whitelist: Draft → Sent → Confirmed → Paid, hoặc Sent → Paid khi chủ không xác nhận (D15) |
| SentAt, ConfirmedAt | datetime2 | NULL — mốc gửi và mốc chủ homestay xác nhận. CHECK `CK_Settlements_Sent`: khác `Draft` thì phải có `SentAt` (`Sent`, `Confirmed`, `Paid` đều đã qua bước gửi). `ConfirmedAt` **chưa** có CHECK |
| PaidAt | datetime2 | NULL; CHECK bắt buộc khi `Status = Paid` |
| PaidBankRef | nvarchar(100) | NULL — mã giao dịch Fuji chuyển tiền cho chủ homestay |
| PaidByEmployeeId | int | FK, NULL |
| | | **UNIQUE (HomestayId, Year, Month, PeriodNo)** |

Chủ xác nhận số liệu là tùy chọn; giữ nguyên CHECK Sent/Paid, không thêm CHECK ConfirmedAt. Người gửi/xác nhận ghi trong AuditLog, không thêm cột (D15–D16).

**Backend B5 (service + test, chưa thêm giao diện):** `SettlementService` chỉ lập khi `period.End + Settlements:GraceDays < hôm nay` giờ VN, mặc định GraceDays = 2. Kỳ theo CheckOut dự kiến của đơn (kỳ 1: 1–15, kỳ 2: 16–cuối tháng), không theo ngày hủy. Chỉ lấy ItemFor != null và đơn chưa nằm ở bảng khác; không tạo bảng mới rỗng, vẫn lập cho homestay Ended còn tiền phải trả. Mọi số tiền lấy từ SettlementCalculator.Build, không có công thức thứ hai trong service. Bảng Draft tính lại/xóa dòng nháp cũ trong cùng transaction; không đổi gì (kể cả audit/UpdatedAt) nếu đầu vào/snapshot không đổi. Nháp đã có nhưng không còn dòng đủ điều kiện được giữ rỗng để lưu lịch sử, chặn gửi; không tự xóa bảng. Sent/Confirmed/Paid luôn giữ nguyên snapshot.

Job tự lập các kỳ đã hết grace gần nhất theo LookbackPeriods, mặc định 4; không quét mọi kỳ lịch sử. Settlements:GenerateEveryMinutes mặc định 60, Range 5–1440 và ValidateOnStart: job chỉ lập khi đã đủ khoảng này từ lần thành công trước, giữ mốc trong instance, khởi động lại chạy lượt đầu ngay; lỗi được thử lại ở phút sau. Đọc và Build so sánh trước, không transaction/khóa/ghi khi Draft không đổi hoặc bảng đã khác Draft; chỉ khi cần ghi mới khóa homestay rồi kiểm tra và Build lại. Lập tay: Accountant/Manager/Administrator; gửi/mở lại/ghi nhận đã trả chỉ Accountant. Đơn chưa chốt trong kỳ gồm Confirmed/CheckedIn/Deposited/PaymentSubmitted/AwaitingPayment; chi tiết Draft trả mã đơn này, gửi bị chặn và nêu các mã cần cập nhật. Gửi kiểm lại và build snapshot mới nhất trước khi Sent; chặn bảng rỗng.

| Từ trạng thái | Được chuyển tới | Ai/thao tác |
|---|---|---|
| Draft | Sent | Accountant gửi, SentAt |
| Sent | Confirmed | Chủ đúng homestay xác nhận tùy chọn, ConfirmedAt |
| Sent | Paid | Accountant, không buộc chủ xác nhận (D15) |
| Sent | Draft | Accountant mở lại, bắt buộc lý do |
| Confirmed | Paid / Draft | Accountant trả hoặc mở lại (cần lý do) |
| Paid | Không có | Không mở lại/chuyển tiếp |

Mở lại xóa SentAt/ConfirmedAt, audit giữ mốc cũ, rồi tính lại nháp; báo chủ. Chủ phản hồi sai số liệu bằng ghi chú 1–500 ký tự, audit và báo mọi Accountant đang làm việc; không đổi trạng thái, chỉ nhận bảng không Draft. Paid đặt PaidAt/PaidByEmployeeId; PayoutAmount > 0 bắt buộc PaidBankRef trim 1–100 ký tự, số tiền 0 được để NULL. Cả hai trường hợp phải đủ BankName/BankAccountNo/BankAccountName. Người gửi/chủ xác nhận/kế toán trả ghi AccountId thật trong AuditLog JSON cũ/mới theo D16; Insert có Id thật sau SaveChanges, cùng transaction mặc định. Khóa dòng homestay tuần tự hóa thao tác và unique có sẵn chặn cuối, không thêm concurrency token/index/schema.

Kế toán/Manager/Administrator đọc lọc kỳ/trạng thái/homestay, phân trang; Consultant bị chặn. Chủ chỉ đọc bảng không Draft của mình, không đọc/xác nhận bảng của chủ khác; Account phải Active và không phải nhân viên. Chi tiết có snapshot từng dòng, mã đơn, Fuji thực thu từ SettlementCalculator.FujiNetRevenue (= CommissionAmount − DiscountAmount), đơn chưa chốt nếu Draft, khoản bù liên quan các đơn có CheckOut trong kỳ (chỉ hiển thị) và lịch sử AuditLog. Đây là backend, chưa thêm màn hình.

**Khoản bù B5 (BR-11/Q3):** `CompensationService` chỉ đưa Payment Compensation/Submitted của đơn thay thế Completed, chưa bị thay tiếp, vào hàng chờ chi riêng. Accountant ghi nhận đã chuyển bằng BankRef 1–100, bắt buộc chủ homestay của đơn thay thế đủ BankName/BankAccountNo/BankAccountName (cùng hàm kiểm tra khi trả đối soát), Approved + ReviewedBy/At + audit và báo chủ. Dùng chung AccountingService.RecordTransferInTransactionAsync với RecordRefundAsync, không chép logic xác nhận chuyển tiền. Khoản bù KHÔNG cộng vào Settlements/SettlementItems vì CHECK Payout = Retained − Commission và không thêm cột; Fuji chuyển một lần riêng, không trừ homestay cũ. Khi N Rejected/Cancelled/Expired/NoShow hoặc có ReplacedByBookingId, job mỗi phút đổi khoản bù Submitted thành Rejected, lý do rõ, ReviewedAt và audit System, không xóa. Accountant có thể từ chối tay với lý do 1–500 trước Completed. B5 không thực hiện gọi ngân hàng/chuyển tiền thật, chỉ ghi nhận giao dịch kế toán đã làm.


#### `SettlementItems` — từng đơn trong bảng đối soát (**mới**)

| Cột | Ràng buộc / ghi chú |
|---|---|
| SettlementId | FK, NOT NULL, cascade |
| BookingId | FK, NOT NULL, **UNIQUE** — một đơn chỉ vào một kỳ |
| ItemType | `Completed`, `CancelledRetained`, `NoShow` |
| RoomAmount, DepositAmount, RefundAmount, RetainedAmount, CommissionRate, CommissionAmount, PayoutAmount | Sao chép từ đơn lúc lập bảng (công thức mục 7). CHECK: các khoản ≥ 0, `0 < RetainedAmount ≤ DepositAmount`, `CommissionAmount ≤ RetainedAmount`, `PayoutAmount = RetainedAmount − CommissionAmount`; `0 < CommissionRate < 1` (`CK_SettlementItems_CommissionRate`) |
| DiscountAmount | Phần voucher **Fuji chịu** cho đơn (đã trừ phần trả lại vào voucher khi hủy). Fuji thực thu = `CommissionAmount − DiscountAmount` — báo cáo lãi lấy thẳng từ bảng này |

- Đơn vào kỳ theo **ngày `CheckOut`** (D7). Công thức nằm ở `Services/SettlementCalculator.cs` (dùng chung cho seed và màn đối soát).
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

**Backend B4 (service + test, chưa có giao diện mới):** `ReviewService` chỉ nhận đánh giá khi đơn `Completed`, mỗi đơn một lần; cả kiểm tra trước và lỗi unique khi gửi đồng thời đều trả lỗi tiếng Việt. Xác thực bằng tài khoản khách sở hữu đơn đang Active, hoặc mã đơn + SĐT E.164 khớp SĐT người ở/khách như tra cứu FR-04. HomestayId lấy từ đơn; Rating 1–5; Comment trim, tối đa 1000, rỗng lưu NULL. Bình luận/trả lời là chuỗi văn bản nguyên bản, không render HTML; giao diện sử dụng sau này phải encode, không Html.Raw.

Chủ đúng homestay, tài khoản Active và không phải nhân viên được trả lời/sửa trả lời, trim và bắt buộc 1–1000 ký tự (chặn cả khoảng trắng ở service), cập nhật RepliedAt; không có thao tác xóa trả lời. Manager/Administrator ẩn/hiện với lý do bắt buộc trong AuditLog. Danh sách mới nhất trước, phân trang; công khai chỉ lấy đánh giá không ẩn, quản lý mới xem được cả dòng ẩn. `ReviewRatings.Summaries` dùng chung với AvailabilityService, luôn loại đánh giá ẩn khỏi điểm trung bình/số lượt. Audit tạo mới có Id thật, sửa có JSON cũ/mới và AccountId, trong transaction mặc định. Chủ nhận thông báo đánh giá mới, khách có tài khoản nhận thông báo trả lời; đơn Completed gửi lời mời đánh giá.

CHECK `CK_Reviews_Reply`: OwnerReply và RepliedAt cùng NULL, hoặc OwnerReply khác NULL/khác rỗng và RepliedAt khác NULL.

#### `Notifications` — thông báo trong hệ thống

Giữ như code: `UserId` (FK → AspNetUsers), `Message`, `Url`, `IsRead`, `CreatedAt`; chỉ mục `(UserId, IsRead)`.

**Thông báo chủ B4 (FR-15):** dùng luồng chung `BookingService.NotifyAsync` cho đơn đã cọc chờ xác nhận, nhân viên xác nhận/từ chối thay chủ và hủy (khách tự hủy, trùng phòng); `NotifyChangeRequestResult` dùng chung bước Manager/Accountant cho đổi đã áp dụng và hủy theo yêu cầu nhân viên. Đơn đổi về Deposited ghi rõ “cần xác nhận lại trong 30 phút” (theo Booking:ConfirmMinutes). Đơn thay thế mới dùng luồng Deposited hiện có; ReviewService báo đánh giá mới. Không đưa lý do/ghi chú dài hoặc liên hệ homestay vào thông báo khách trước Confirmed; mỗi thông báo tối đa 500 ký tự.

**Lịch chủ B4 (FR-16):** `OwnerScheduleService` đọc các homestay thuộc chủ, khoảng ngày gồm hai đầu tối đa 31 ngày: nhận phòng là Confirmed theo CheckIn, trả phòng là CheckedIn theo CheckOut. Trả mã đơn, người ở, SĐT, số khách, phòng, ghi chú đặc biệt. Chủ chỉ xem của mình; Manager/Administrator đang làm việc được hỗ trợ, Consultant/Accountant/khách bị chặn. Bảng đối soát chủ do SettlementService B5 cung cấp, chưa thêm giao diện.

#### `MessageLogs` — tin gửi ra ngoài (○, **mới**)

`Id bigint`, `Channel` (`Sms`, `Zalo`, `Email`), `RecipientPhone` / `RecipientEmail`, `TemplateCode` (`Otp`, `BookingConfirmed`, `DepositReminder`, `CheckinReminder`…), `BookingId` (NULL), `Status` (`Pending`, `Sent`, `Failed`), `ErrorMessage`, `SentAt`, `CreatedAt`. **Không** lưu nội dung OTP. Dùng để đối chiếu chi phí gửi tin (NFR-12).

**Backend gửi tin B4 (NFR-07/NFR-12):** `IMessageSender` mặc định `LoggingMessageSender` chỉ giả lập: “shortcut: chưa nối Zalo/SMS thật, thay khi Fuji duyệt dự toán (NFR-12)”. Gửi theo ba bước: (a) transaction ngắn khóa dòng đơn/chủ, kiểm chống lặp, tạo thông báo app và MessageLog Pending nếu số nhận hợp lệ rồi commit; (b) gọi sender NGOÀI transaction; (c) transaction ngắn cập nhật Sent + SentAt hoặc Failed + ErrorMessage ≤ 500. Zalo lỗi thì bước (c) đồng thời tạo Pending SMS, commit rồi mới gọi SMS, tránh khoảng trống mất dấu đang gửi. Không giữ khóa đơn/chủ trong lúc chờ kênh ngoài, không Serializable. Caller phải commit nghiệp vụ trước khi gọi SendAsync; lỗi kênh không rollback nghiệp vụ đã lưu. MessageLog tự là nhật ký gửi tin, KHÔNG thêm hai AuditLog cho mỗi lần thử; không lưu nội dung tin hoặc nguyên lỗi nhà cung cấp trong database/audit/log. Số nhận E.164. Sent mặc định chỉ là giả lập thành công.

**Nhắc lịch B4 (FR-23):** `ReminderService` chọn đúng Confirmed, CheckIn trong [hôm nay, hôm nay + Reminders:CustomerDaysBefore] (mặc định 1). Chỉ gửi ngoài trong [SendHour, QuietFromHour) giờ VN (mặc định [9, 21)), KỂ CẢ ngày nhận phòng hôm nay; 07:00 đợi 09:00, 23:30 đợi khung giờ hợp lệ tiếp theo. Thông báo trong app cũng theo cùng khung giờ, độc lập kết quả tin ngoài. Tin khách ≤ 160 ký tự, chỉ gồm mã đơn, tên homestay (rút gọn nếu cần), ngày/giờ nhận, SĐT cơ sở và link `/Bookings/Details/{code}` ghép Reminders:PublicBaseUrl (mặc định http://localhost:5011). URL phải HTTP/HTTPS tuyệt đối, không userinfo/query/fragment, tối đa 50 ký tự để chừa chỗ cho tin 160 ký tự; triển khai thật đặt URL công khai của ứng dụng. Địa chỉ, bản đồ, hướng dẫn không gửi ra ngoài, chỉ xem trong chi tiết sau chốt BR-13. Khách có tài khoản nhận thông báo app ngay transaction Reserve, không phụ thuộc kết quả Zalo/SMS; chống lặp bằng CustomerUrl (link chi tiết với query reminder=checkin) và CreatedAt >= ConfirmedAt, nên xác nhận lại được nhắc mới. Template CheckinReminder + BookingId, bỏ qua Sent/Pending từ ConfirmedAt (>= để chống lặp timestamp bằng nhau); tối đa 3 lượt Zalo→SMS/lần xác nhận (tối đa 6 dòng Failed). Đổi ngày rồi xác nhận lại mở chu kỳ mới.

Chủ được gộp lịch đơn Confirmed nhận ngày mai theo homestay, cũng chỉ gửi trong [SendHour, QuietFromHour). OwnerArrivals có BookingId NULL, gửi ContactPhone cơ sở; nhiều cơ sở cùng số nhận (kể cả khác chủ) gộp một tin chứa đủ lịch, thông báo app chỉ chứa lịch của đúng chủ. Thông báo app tạo ngay transaction Reserve, tách đoạn ≤ 500, chống lặp bằng Notification.Url theo (chủ, ngày nhận), không đợi Sent; homestay không có SĐT hợp lệ vẫn báo app, chỉ bỏ tin ngoài; metadata template/số nhận/ngày gửi chống lặp tin ngoài kể cả chủ chưa có Account. Pending đang gửi chặn lượt khác; Pending quá 10 phút đổi Failed với ErrorMessage “Quá thời gian chờ kết quả gửi”, rồi mới được thử lại trong giới hạn lượt. Nếu lưu bước (c) lỗi, rollback giữ Pending đã commit, lượt kế tiếp không gửi lại ngay; sau timeout kết quả bên ngoài có thể chưa xác định, khi nối nhà cung cấp cần khóa chống trùng phía nhà cung cấp để tránh giao lặp. Giữ cùng thứ tự khóa đơn/chủ → MessageLog trong hai transaction ngắn. Job vẫn scope/try-catch riêng từng tác vụ. Options validate DataAnnotations/IValidatableObject khi khởi động, SendHour < QuietFromHour. Khi nối Zalo ZNS hoặc SMS brandname thật cần đăng ký mẫu tin và đổi IMessageSender sang mã mẫu + tham số, chưa nối dịch vụ thật trong B4.

**Rà BR-13:** `BookingContactVisibility.CanView` chỉ nhận Confirmed/CheckedIn/Completed; BookingsController.ShowAddress và ReminderService cùng dùng. Details lược Address/ContactPhone/Latitude/Longitude/CheckinGuideHtml trên entity AsNoTracking trước khi đưa vào view model, không lưu ngược. Đã rà HomestaysController/HomeController và các view model công khai (chỉ khu vực, không liên hệ chính xác), AvailabilityOffer/đề xuất Overbooking (không trường liên hệ), thông báo của BookingService/OverbookingService/yêu cầu đổi (chỉ mã/trạng thái); thông báo từ chối cọc bổ sung không ghép lý do dài. Trang nhân viên/chủ có dữ liệu liên hệ theo quyền nghiệp vụ. Không đổi giao diện.

`data_consistency.sql` B4 giữ 23 mục cũ, thêm phản hồi chỉ khoảng trắng, log Sent/Failed thiếu mốc/lỗi, thiếu cả SĐT/email — tổng 26, chỉ SELECT. Không thêm bảng/cột/CHECK/index/migration.

#### `AuditLogs` — thay đổi dữ liệu nhạy cảm (○, **mới**)

`Id bigint`, `AccountId`, `TableName`, `RecordId`, `Action` (`Insert`/`Update`/`Delete`), `OldValues`, `NewValues` (JSON), `IpAddress`, `CreatedAt`. Service ghi trực tiếp cùng transaction; không dùng interceptor. Code đổi/hủy hiện ghi BookingChangeRequests và Bookings; backend tiếp theo phải bổ sung audit cho giá, phòng, ảnh, mô tả, địa chỉ, pháp lý, hợp đồng, thanh toán và các bước đối soát. Với tác vụ hệ thống, AccountId có thể NULL và nội dung phải nêu actor System.

**Schema Prompt 10:** `20261015200521_EnumAndReviewChecks` thêm 13 whitelist cho Settlements.Status, SettlementItems.ItemType, Contracts.Status, Homestays.Status, RoomTypePrices.DayType, BookingNights.DayType, RoomBlocks.Reason, OwnerRegistrations.Status, VerificationDocuments.DocumentType, CancellationPolicies.Scope, Payments.Type/Method/Status; thêm Reply, Year và OwnerRegistrations.Decision, tổng **16 CHECK**. Tất cả whitelist sinh từ `SqlList<TEnum>()`; không thêm bảng/cột/FK/index, không đổi dữ liệu. Schema database thử có 9 migration/60 CHECK; **v5 vẫn 8 migration/44 CHECK**, chưa triển khai migration này. Down chỉ gỡ 16 CHECK đã thêm. Các CHECK dùng chuỗi thường và `<> ''`, chạy được trên SQLite/SQL Server.

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
 (mở liên hệ trên    └─(khách không đến)───────────────────► NoShow     (không hoàn)
  chi tiết đơn — BR-13; tin nhắc ngoài chỉ gửi link)
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
5. **Homestay** xác nhận trong 30 phút → `Confirmed`, khách được xem SĐT, địa chỉ, hướng dẫn nhận phòng ở chi tiết đơn (BR-13); tin nhắc ngoài chỉ có tóm tắt và link chi tiết. Homestay từ chối → `Rejected`, kế toán chuyển hoàn và ghi `Payment(Type = Refund, Method = ManualTransfer, Amount = DepositAmount)`.

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
- Có voucher (D14): `DiscountAmount = MIN(Voucher.Balance, làm tròn xuống nghìn(TotalAmount × CommissionRate × 20%))`; khách chuyển `AmountDue = DepositAmount − DiscountAmount`.

### 7.2. Khi hủy

- `DaysBefore = CheckIn − ngày hủy` (theo giờ Việt Nam).
- `RefundPercent` = quy tắc có `MinDaysBefore` lớn nhất mà ≤ `DaysBefore` trong chính sách của đơn.
- `RefundAmount = (DepositAmount − DiscountAmount) × RefundPercent / 100` (tính trên tiền khách thực chuyển). Đơn chưa có cọc → 0. Voucher được trả lại `DiscountAmount × RefundPercent / 100`.
- `NoShow` → `RefundAmount = 0`.
- Homestay từ chối (`Rejected`) → `RefundAmount = DepositAmount − DiscountAmount` (100% tiền đã chuyển) và trả đủ voucher, không phụ thuộc chính sách hủy.
- Hủy khi đơn đang `AwaitingPayment` / `PaymentSubmitted` (chưa duyệt cọc): không có cọc được ghi nhận → 0; nếu khách đã chuyển tiền thì kế toán duyệt rồi hoàn theo chính sách.

### 7.3. Khi đối soát (D7, D8)

Công thức duy nhất trong code là `SettlementCalculator.ItemFor/Build` (seed và SettlementService dùng chung). Đơn có ReplacedByBookingId bị loại. Nhận Completed, NoShow, Cancelled đã cọc/có phí đổi; Rejected/Expired chỉ nhận phần phí đổi còn giữ. Đơn chưa chốt không vào dòng. Nếu tổng giữ lại sau cộng phí <= 0 thì không có dòng.

```text
refund = RefundAmount ?? 0
p = AmountDue > 0 ? Round(100 × refund / AmountDue) : 0  // đúng cách lấy % nguyên trong code
feeOnly = Status là Rejected hoặc Expired
baseRetained = feeOnly ? 0 : DepositAmount − RoundToThousand(DepositAmount × p / 100)
baseCommission = MIN(Round(TotalAmount × CommissionRate, 0, AwayFromZero), baseRetained)
feeCommission = MIN(Round(ChangeFeeAmount × CommissionRate, 0, AwayFromZero), ChangeFeeAmount)
RetainedAmount = baseRetained + ChangeFeeAmount
CommissionAmount = baseCommission + feeCommission
PayoutAmount = RetainedAmount − CommissionAmount
DiscountAmount (dòng) = feeOnly ? 0 : MAX(0, DiscountAmount (đơn) − RoundToThousand(DiscountAmount (đơn) × p / 100))
Fuji thực thu = CommissionAmount − DiscountAmount (dòng)
DepositAmount (dòng) = DepositAmount (đơn) + ChangeFeeAmount
```

Có voucher, phần cọc giữ lại tính như đơn không dùng voucher theo tỷ lệ hoàn đã chốt; voucher do Fuji chịu nên không giảm tiền chủ. Phí đổi chỉ chia CommissionRate trên phần phí, không tính hoa hồng cả đơn lần hai. Khoản bù trùng phòng chi riêng sau Completed và chỉ hiển thị cạnh đối soát, không cộng vào bất kỳ tổng nào.

Ví dụ voucher: tổng 2.700.000đ, hoa hồng 12% = 324.000đ → giảm tối đa 64.000đ; khách chuyển 810.000 − 64.000 = 746.000đ; Fuji chuyển homestay 486.000đ như bình thường; Fuji còn 260.000đ.

Ví dụ: tổng 2.000.000đ, cọc 600.000đ, hoa hồng 12%.

| Trường hợp | Hoàn | Giữ lại | Hoa hồng Fuji | Chuyển homestay |
|---|---|---|---|---|
| Hoàn thành | 0 | 600.000 | 240.000 | 360.000 |
| Hủy trước 5 ngày (hoàn 50%) | 300.000 | 300.000 | 240.000 | 60.000 |
| Hủy trước 1 ngày / không đến | 0 | 600.000 | 240.000 | 360.000 |
| Hủy trước 10 ngày (hoàn 100%) | 600.000 | 0 | 0 | 0 — không vào bảng đối soát |

---

### 7.4. Báo cáo và quản trị tài khoản B6 (FR-20/21/24, X6)

**ReportService** trả DTO đã tính sẵn, không có controller/view mới. Khoảng ngày gồm cả hai đầu theo giờ VN, tối đa 366 ngày; lọc thêm HomestayId/AreaId. Mọi đường đọc kiểm lại Actor + Employee.Position và Account/Employment Active: Manager/Administrator đọc toàn bộ; Accountant chỉ FinancialAsync; Consultant chỉ EmployeesAsync của mình, không được truyền Id người khác. Chủ/khách không vào báo cáo công ty.

| Chỉ tiêu | Ngày dùng / cách tính |
|---|---|
| Số đơn theo trạng thái, SourceChannel | CreatedAt UTC đổi biên ngày VN; GroupBy trong CSDL |
| Tiền phòng Completed | CheckOut trong khoảng, bỏ đơn bị thay thế; tổng TotalAmount |
| Giữ lại / hoa hồng / Fuji thực thu | Chỉ nạp đơn CheckOut trong khoảng, gọi SettlementCalculator.ItemFor/FujiNetRevenue; **tạm tính**, có thể khác bảng đã gửi |
| Khoản bù đã chi | Compensation Approved, ReviewedAt trong khoảng; tách riêng, không cộng đối soát |
| Tỷ lệ khách hủy | Cancelled / nhóm đơn tạo trong khoảng đã có DepositedAt; bỏ ReplacedByBookingId khác NULL |
| Tỷ lệ chủ từ chối / hết hạn | Rejected / tổng đơn tạo; Expired báo số lượng riêng |
| Hiệu suất tạo / tỷ lệ chốt | CreatedByEmployeeId; nhóm CreatedAt trong khoảng; ConfirmedAt khác NULL / tổng nhóm (chốt 17/10) |
| Doanh số nhân viên | Completed theo CheckOut, CreatedByEmployeeId, không đổi khi giao lại |
| Công việc nhân viên | AssignedEmployeeId trên nhóm đơn tạo trong khoảng; xong = Completed/Cancelled/Rejected/Expired/NoShow, còn lại đang phụ trách |
| Điểm homestay | ReviewRatings.Summaries của Review.CreatedAt trong khoảng, bỏ IsHidden |
| Tốc độ xác nhận | Nhóm DepositedAt trong khoảng có ConfirmedAt: trung bình phút và % <= 30 phút |
| Trùng phòng theo tháng | AuditLog Step OverbookingReported theo CreatedAt; đơn bị thay theo CancelledAt/ReplacedByBookingId; tháng giờ VN |

Trừ phần gọi ItemFor, các thống kê đơn/nhân viên/homestay được GroupBy tại CSDL, chỉ nạp kết quả gộp. Không tạo công thức tiền thứ hai. Mẫu số 0 trả 0; thời gian/điểm không có dữ liệu trả NULL. Nhân viên Consultant/Manager đang thuộc hai chức vụ này được liệt kê, kể cả ngừng việc để đọc số liệu lịch sử theo người tạo; tài khoản đọc báo cáo vẫn phải Active.

**AccountAdminService** dùng UserManager, không tự băm/đọc mật khẩu. Administrator tạo PersonProfile + Employee + Account Active, role Employee; SĐT E.164/mã nhân viên unique, không tự ghép SĐT đã có Person. Mật khẩu ban đầu tuân chính sách Identity; shortcut chưa bắt đổi lần đầu, chờ luồng kích hoạt nhân viên. AccountActivityLog AccountCreated/RoleAssigned, metadata cấp role đúng actor/thời gian.

Đổi chức vụ/trạng thái làm việc cập nhật security stamp. Suspended/Resigned -> Account Disabled; chuyển lại Active -> Account Active. Chỉ đổi chức vụ không tự mở Account Locked. Khóa/mở tài khoản bất kỳ Active <-> Locked phải có lý do; không dùng mở khóa để kích hoạt nhân viên ngừng việc. Reset mật khẩu chỉ nhân viên, qua UserManager, ghi PasswordReset. Manager chỉ đọc danh sách tìm theo tên/SĐT/mã, role/status, không được sửa/nâng quyền.

Không tự khóa/hạ chức vụ/cho nghỉ chính mình; luôn còn Administrator Employment Active có Account Active. Transaction mặc định khóa các dòng Administrator hiện có rồi kiểm lại actor/trạng thái trước thao tác; không Serializable, không thêm schema. AuditLog trực tiếp cùng transaction với Identity và AccountActivityLog, Id thật và Old/New JSON; không log mật khẩu/hash/stamp/token. CatalogDeletionService dùng chung bốn action xóa cũ: khu vực/loại phòng/tiện nghi đang dùng bị chặn, kể cả quan hệ tiện nghi cascade; FK do tranh chấp được dịch tiếng Việt. Holidays không có FK; BookingNights giữ giá/DayType snapshot.

**Dữ liệu mẫu:** Seed mới giữ guard bảng rỗng/Development như trước, hợp đồng mẫu HD-2026-* tới 2028-05-31; mỗi homestay có PropertyPhoto gắn HomestayId từ Data/SampleFiles/sample-document.png. Không đổi giấy tờ cũ gắn OwnerRegistrationId. Database cũ dùng database/data/seed_topup_2028.sql (guard TargetDb, không database hệ thống/RestoreCheck, XACT_ABORT/transaction). Chỉ gia hạn hợp đồng mẫu Active ngắn hơn mốc, không rút ngắn hợp đồng dài hơn; thêm ngày lễ/ảnh mẫu thiếu và AuditLog System. Mật khẩu demo chỉ ở Development; tài khoản Administrator đầu tiên Production vẫn cần cơ chế bootstrap riêng, không dùng DefaultPassword (việc đã hoãn từ Bước 7).

Đã thử top-up hai lần trên DeployTest ngày 17/10/2026: lần 1 35 hợp đồng/9 ngày lễ/35 ảnh, 79 audit mới; chỉ Contracts/Holidays/VerificationDocuments/AuditLogs đổi. Lần 2 0/0/0; COUNT_BIG + CHECKSUM_AGG(BINARY_CHECKSUM(*)) của đủ 45 bảng khớp trước/sau. **Chưa chạy script trên v5**, chờ duyệt/backup cùng triển khai migration 9. Đối chiếu dữ liệu nay có 35 mục: thêm nhân viên ngừng việc còn Account Active và thiếu Administrator chỉ khi có Employees (database rỗng như ConstraintTest = 0/OK). Không so hạn hợp đồng với ngày lễ cuối; giữ kiểm hợp đồng quá hạn ở mục 13 và D20 tại service.

Pkcs ghim 10.0.13 ở Web, transitive tới Tests; không nâng EF/SqlClient. [Advisory .NET chính thức](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx) xác nhận bản vá. Đối chiếu FR-01…27 và các phần còn thiếu xem QUY_TRINH_BTL.md mục 3.3; không suy từ entity/test thành đã có mọi giao diện hay connector thật.

## 8. Quy tắc nghiệp vụ được thực thi ở đâu

| Quy tắc | Database | Service (ứng dụng) |
|---|---|---|
| Không trùng phòng (BR-05) | **PK `RoomNights (RoomId, Date)`** | Bắt lỗi trùng khóa → báo "Phòng vừa được đặt" |
| Mã đơn duy nhất (BR-07) | UNIQUE `Bookings.Code` + `BookingCodeCounters` | Sinh mã trong transaction |
| Một người một tài khoản (v5) | UNIQUE `AspNetUsers.PersonId`, `PersonProfiles.PhoneNumber` | Chuẩn hóa SĐT |
| Một hồ sơ Owner đang xử lý | Filtered UNIQUE `OwnerRegistrations` | |
| Một hợp đồng đang hiệu lực / homestay | Filtered UNIQUE `Contracts` | |
| Hoa hồng theo hợp đồng (BR-02) | `Contracts.CommissionRate` → snapshot `Bookings.CommissionRate` → `SettlementItems.CommissionRate`; CHECK `0 < CommissionRate < 1` ở cả ba bảng | Lấy hợp đồng `Active` tại ngày đặt |
| Cọc 30% (BR-01) | `Bookings.DepositRate`, CHECK `0 < DepositRate ≤ 1`, CHECK `DepositAmount <= TotalAmount` | Lấy tỷ lệ từ cấu hình, tính và làm tròn |
| Giá 3 mức (BR-08) | `RoomTypePrices`, `Holidays` → `BookingNights` | Chọn mức giá cho từng đêm |
| Hoàn cọc, ưu tiên chính sách riêng (BR-04) | `CancellationPolicies/Rules`, `Bookings.CancellationPolicyId` | Chọn chính sách, tính `RefundAmount` |
| Homestay xác nhận trong 30 phút sau khi có cọc (BR-06, D12) | `Bookings.ConfirmDeadlineAt` | Job mỗi phút tạo cảnh báo |
| Mốc xác nhận đơn khớp trạng thái | CHECK `CK_Bookings_ConfirmedAt` | `ApplyStatusAsync` gán `ConfirmedAt` khi `Deposited → Confirmed`, hủy không xóa mốc |
| Giữ phòng 15 phút khi đặt (D4, D5) | `RoomNights` tạo ngay khi đặt, `Bookings.HoldExpiresAt` | Job mỗi phút chuyển `Expired` |
| Cọc khớp mã đơn và số tiền (BR-10, D13) | UNIQUE `BankTransactions (Provider, ProviderTransactionId)`, UNIQUE `Payments.BankTransactionId`, filtered UNIQUE khoản cọc | Tự khớp nội dung + số tiền trong transaction |
| Ảnh bill khi không tự khớp được (D11) | `Payments.Method = ManualReceipt`; CHECK `CK_Payments_Receipt`: bill tay phải có `ReceiptImageUrl` khác NULL, khác rỗng | Lưu ảnh (tên file do server sinh); kế toán xem bill, khớp tay / từ chối |
| Không bỏ sót tiền khách chuyển | `BankTransactions.MatchStatus` | Danh sách giao dịch chưa khớp / đến muộn cho kế toán |
| Chống giữ phòng ảo | — | Tối đa 3 đơn `AwaitingPayment` / khách |
| Đối soát 2 kỳ / tháng (BR-03) | UNIQUE `Settlements (HomestayId, Year, Month, PeriodNo)`, UNIQUE `SettlementItems.BookingId`, CHECK `Payout = Retained − Commission` ở cả hai bảng | Lập bảng theo công thức 7.3 (`SettlementCalculator`) |
| Đối soát đã gửi / đã trả có mốc thời gian | CHECK `CK_Settlements_Sent` (khác `Draft` ⇒ có `SentAt`), `CK_Settlements_Paid` (`Paid` ⇒ có `PaidAt`) | Màn đối soát gán mốc khi chuyển trạng thái. `Confirmed` ⇒ `ConfirmedAt` **chưa** có CHECK |
| Yêu cầu đổi/hủy hợp lệ | Type/Status/RequestedBy/Fields/Decision, Amounts/Finance/ActorSeparation/Notes/PaymentDeadline; CHECK liên kết AdditionalDeposit, chủ RoomNight và ChangeFeeAmount; `ChangeRequestWorkflow` đã áp v5 ngày 16/10/2026 | **Chốt 14/10 và 15/10**: sửa tại chỗ, đổi kết hợp, Manager → Accountant khi có tiền thêm; không tự duyệt; hạn khách nộp; phí đổi ngày chia theo CommissionRate của đơn trên phần phí (chốt 15/10). Service kiểm phòng, giá, 3 ngày/2 lần/90 ngày, chặn đổi khi DiscountAmount > 0; Pending cũ chỉ cho Manager từ chối; chia phí/đối soát và transaction (mục 5.E) |
| Không hoàn quá tiền khách đã chuyển (BR-04) | CHECK `Bookings.RefundAmount ≤ DepositAmount − DiscountAmount` | |
| Đánh giá một lần sau khi hoàn thành (BR-09) | UNIQUE `Reviews.BookingId`, CHECK 1–5 và Reply | ReviewService kiểm Completed, chủ đơn/tài khoản hoặc mã + SĐT, trim/độ dài, quyền trả lời/ẩn; ReviewRatings bỏ đánh giá ẩn |
| Homestay phải được duyệt mới hiện (BR-12) | `Homestays.Status`, `VerificationDocuments` | Lọc `Status = Active` |
| Ẩn SĐT, địa chỉ trước khi xác nhận (BR-13) | `Homestays.ContactPhone`, `CheckinGuideHtml` | BookingContactVisibility.CanView chỉ Confirmed/CheckedIn/Completed; lược dữ liệu Details trước xác nhận, ReminderService dùng chung |

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
| Báo cáo (X6) | — | Chỉ hiệu suất của mình, không tài chính công ty | Chỉ tài chính | Toàn bộ | — (đọc đối soát riêng) |
| Hồ sơ Owner, giấy tờ | — | Nhập hộ, thẩm định | — | Duyệt | Của mình |
| Tài khoản, nhân viên | — | — | — | Manager chỉ xem; Administrator tạo/sửa/khóa/reset | — |

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
| Notifications | (UserId, IsRead) | Đếm thông báo chưa đọc |
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

Database v5 đã có dữ liệu và migration `ChangeRequestWorkflow` đã áp ngày 16/10/2026. Các thay đổi schema tiếp theo cần migration mới và quy trình backup/triển khai tại [database/README.md](database/README.md#7-cập-nhật-database-đang-có-dữ-liệu-thật).

---

## 12. Câu hỏi còn mở

| # | Câu hỏi | Ảnh hưởng | Tạm thời |
|---|---|---|---|
| Q1 | ~~Kế toán chỉ làm giờ hành chính~~ — **đã giải quyết bằng D13**: chuyển khoản qua QR được tự xác nhận 24/7. Chỉ đơn đi đường ảnh bill vào ban đêm mới phải chờ tới giờ làm việc | Thời gian chờ | Giao diện khuyến khích quét QR; báo khách đi đường bill rằng Fuji duyệt trong giờ làm việc |
| Q2 | ~~Kế toán từ chối bill bao nhiêu lần thì hủy hẳn?~~ — **đã chốt (11/10)**: lần 1 khách được thêm 15'; lần 2 đơn `Expired`, mở lại phòng (`Booking:MaxReceiptRejections` = 2) | `Bookings`, `Payments` | — |
| Q3 | Khoản Fuji bù chênh lệch khi trùng phòng (BR-11) có trừ vào homestay gây ra không? — **đã chốt (11/10)**: Không trừ; ghi `Payments.Compensation` để thống kê | `Payments.Compensation` | B5 chi riêng sau Completed, không cộng đối soát |
| Q4 | ~~Voucher bồi thường: giá trị, thời hạn, điều kiện~~ — **đã chốt (11/10)**: xem D14 và bảng `Vouchers` | Bảng `Vouchers` | — |
| Q5 | Đơn bị hủy có giữ cọc: xếp kỳ đối soát theo ngày `CheckOut` dự kiến hay ngày hủy? — **đã chốt (11/10)**: Theo `CheckOut` dự kiến (thống nhất D7) | `SettlementItems` | — |
| Q6 | Một Owner có cần nhiều tài khoản ngân hàng (mỗi homestay một tài khoản) không? — **đã chốt (11/10)**: Một tài khoản / Owner | `Owners` | — |
| Q7 | Tiện nghi gắn với homestay hay với từng loại phòng? — **đã chốt (11/10)**: Theo homestay | `HomestayAmenities` | — |
| Q8 | Chủ homestay có được đổi sang phòng khác cùng loại khi xác nhận (phòng hệ thống gán đang có vấn đề) không? — **đã chốt (11/10)**: Cho phép, đổi trong một transaction; ghi lịch sử | `Bookings.RoomId`, `RoomNights` | — |
| Q9 | Fuji dùng dịch vụ nào (SePay / Casso / khác) khi triển khai thật? Phí dịch vụ? Ngân hàng: **MB Bank** (đã chốt) | Chỉ cấu hình, không đổi DB | BTL dùng giả lập `Simulator`; số tài khoản MB để trong `appsettings` |
| Q10 | Khách chuyển **dư** tiền cọc: hoàn ngay hay trừ vào số tiền trả tại homestay? — **đã chốt (11/10)**: Ghi chú lại, hoàn khi khách yêu cầu | `BankTransactions.Note`, `Payments` | — |
| Q11 | ~~Đổi kết hợp; phí/cọc; duyệt và lưu người duyệt~~ — **đã chốt 14/10, bổ sung 15/10**: sửa tại chỗ giữ mã; phí BR-04 chốt lúc gửi, phí đổi ngày chia theo CommissionRate của đơn trên phần phí (chốt 15/10); cộng phí riêng, chuyển cọc còn lại/thu thêm; chặn tổng mới dưới cọc chuyển, không hoàn tiền mặt; Manager → Accountant khi có tiền thêm, không tự duyệt, lưu người/thời điểm/ghi chú/lý do | Request, Booking.ChangeFeeAmount, Payment.AdditionalDeposit, RoomNights, AuditLog; không thêm bảng | [Bản rút gọn](docs/change-request-design.md): một migration, Status token, hạn khách nộp và giữ phòng chờ kế toán, 3 ngày/2 lần/90 ngày. Chặn đổi khi DiscountAmount > 0, hủy vẫn theo quy tắc cũ; Pending thiếu người gửi/tiền giữ nguyên và chỉ cho Manager từ chối có lý do, vẫn chiếm unique mở tới khi từ chối. Database/service/giao diện đã triển khai; `ChangeRequestWorkflow` đã áp v5 ngày 16/10/2026 |
