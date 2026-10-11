# Fuij Homestay — Thiết kế đăng nhập và tài khoản dùng chung

> **Phiên bản:** v5 — 2026-10-09  
> **Trạng thái:** Draft — dùng làm cơ sở thiết kế; các quy tắc nghiệp vụ chưa khảo sát cần được xác nhận trước khi chốt database.  
> **Phạm vi:** Account dùng chung cho Customer, Owner và Employee; đăng ký, đăng nhập, OTP, Google Login, quy trình hồ sơ Owner và cấp tài khoản nhân viên.

---

## 1. Các quyết định đã thống nhất

1. Dùng **một bảng `ACCOUNT` chung** thay vì `CUSTOMER_ACCOUNT`, `OWNER_ACCOUNT` và `EMPLOYEE_ACCOUNT` riêng.
2. Một Account có thể có nhiều vai trò thông qua `ACCOUNT_ROLE`; ví dụ, cùng một người có thể vừa là Customer vừa là Owner.
3. Hồ sơ cá nhân dùng chung của Customer và Owner được đặt trong `PERSON_PROFILE`, tránh lặp lại họ tên, email, số điện thoại liên hệ và địa chỉ.
4. Các hồ sơ nghiệp vụ vẫn tách riêng: `CUSTOMER`, `OWNER`, `EMPLOYEE`. Các dữ liệu đặc thù như giấy tờ xác minh/ưu đãi Customer thuộc phần mở rộng riêng, không nhét tất cả vào `PERSON_PROFILE`.
5. **Customer có thể tồn tại trước Account.** Nhân viên vẫn có thể tra cứu Customer bằng số điện thoại; Booking liên kết bằng `customer_id`, không phụ thuộc `account_id`.
6. Customer và Owner đăng nhập bằng **số điện thoại + mật khẩu**. Employee đăng nhập bằng **mã nhân viên + mật khẩu**.
7. Nhân viên Fuij có hồ sơ công việc riêng. Nhân viên thường và quản lý vẫn thuộc `EMPLOYEE`; quyền quản lý được cấp theo cơ chế phân quyền, không bắt buộc có bảng `MANAGER` riêng.
8. Owner có thể tự đăng ký hoặc nhờ nhân viên nhập hồ sơ. Với trường hợp nhân viên hỗ trợ, hồ sơ được lưu nháp trước; Owner tự xác minh OTP và tự thiết lập mật khẩu sau.
9. Owner chỉ được sử dụng các chức năng quản lý homestay theo trạng thái tài khoản, kết quả duyệt hồ sơ và quyền được cấp.
10. OTP hiện dùng cho xác minh số điện thoại; chưa bắt buộc tạo bảng OTP nếu ứng dụng vẫn lưu OTP an toàn trong cache có TTL.

## 2. Nguyên tắc thiết kế

- `ACCOUNT` chịu trách nhiệm xác thực và trạng thái tài khoản; không lưu thông tin nghiệp vụ riêng của Customer/Owner/Employee.
- `PERSON_PROFILE` lưu thông tin cá nhân dùng chung. `CUSTOMER`, `OWNER` và `EMPLOYEE` mô tả vai trò/nghiệp vụ tương ứng.
- `ACCOUNT_ROLE` biểu diễn nhiều-nhiều giữa Account và các vai trò. Vai trò trả lời “tài khoản thuộc nhóm nào”; trạng thái duyệt Owner trả lời “Owner đã được phép hoạt động chưa”. Hai khái niệm này không thay thế nhau.
- Mỗi người chỉ có tối đa một Account trong phạm vi thiết kế hiện tại. Có thể có `PERSON_PROFILE` và hồ sơ Customer/Owner trước khi tạo Account.
- Các thao tác tạo hồ sơ, liên kết Account và gán role phải thực hiện trong transaction khi phù hợp để tránh dữ liệu nửa chừng.
- Không lưu mật khẩu dạng rõ. Sử dụng BCrypt hoặc Argon2; giới hạn thử đăng nhập và trả thông báo lỗi chung.

## 3. Sơ đồ quan hệ khái niệm

```text
PERSON_PROFILE
  ├── 0..1 ACCOUNT
  │      └── 1..n ACCOUNT_ROLE
  ├── 0..1 CUSTOMER
  │      └── 0..1 CUSTOMER_PROFILE
  ├── 0..1 OWNER
  │      └── 0..n OWNER_REGISTRATION (lưu hồ sơ/lịch sử đăng ký)
  │              └── employee xử lý/duyệt (nếu có)
  └── 0..1 EMPLOYEE

OWNER 1 ──── 0..n HOMESTAY
CUSTOMER 1 ──── 0..n BOOKING
ACCOUNT 1 ──── 0..n ACCOUNT_ACTIVITY_LOG (có thể có log trước khi Account tồn tại)
```

**Ý nghĩa:** `PERSON_PROFILE` có thể tồn tại trước Account. Một người có thể có nhiều hồ sơ nghiệp vụ (ví dụ Customer và Owner) nhưng vẫn dùng chung một Account. Employee có hồ sơ công việc riêng và cũng tham chiếu Person Profile để không lặp dữ liệu cá nhân cơ bản.

> Sơ đồ này là mô hình khái niệm. Cardinality và constraint cuối cùng cần được thể hiện chính xác trong ERD/DDL của hệ quản trị database được chọn.

## 4. Thiết kế các bảng

### 4.1. `PERSON_PROFILE` — thông tin cá nhân chung

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `person_id` | PK |
| `full_name` | Tên đầy đủ; có thể chưa hoàn chỉnh trong hồ sơ nháp nếu nghiệp vụ cho phép |
| `phone_number` | Số liên hệ; chuẩn hóa E.164. Nên duy nhất nếu được dùng để đăng nhập Customer/Owner |
| `email` | Có thể NULL nếu chưa cung cấp |
| `address` | Có thể NULL nếu chưa cung cấp |
| `created_at` | Thời điểm tạo |
| `updated_at` | Thời điểm cập nhật |

Quy tắc:

- Số điện thoại trong `PERSON_PROFILE` vừa phục vụ liên hệ/tra cứu, vừa là định danh đăng nhập Customer/Owner sau khi Account được tạo và số đã được xác minh.
- Không cho phép hai Person Profile có cùng số điện thoại nếu luồng đăng nhập tìm Account theo số điện thoại. Nếu khảo sát yêu cầu nhiều người dùng chung một số, cần thiết kế thêm cơ chế định danh đăng nhập đã xác minh thay vì tra cứu trực tiếp theo phone.
- Khi nhân viên nhập hồ sơ Owner nháp, số điện thoại phải được thu thập để Owner tiếp tục kích hoạt; nhân viên không xác minh thay Owner.
- Thông tin giấy tờ nhạy cảm không nên đặt trong bảng hồ sơ chung nếu chỉ phục vụ một nhóm nghiệp vụ.

### 4.2. `ACCOUNT` — tài khoản xác thực dùng chung

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `account_id` | PK |
| `person_id` | FK → `PERSON_PROFILE.person_id`, UNIQUE, NOT NULL |
| `password_hash` | Hash mật khẩu; cho phép NULL chỉ khi chính sách cho phép Account đăng nhập hoàn toàn bằng Google. Cần chốt chính sách Google trước khi DDL cuối cùng |
| `google_sub` | UNIQUE, NULL; định danh ổn định (`sub`) từ Google, không dùng email/tên để tự ghép tài khoản |
| `status` | `PENDING_ACTIVATION`, `ACTIVE`, `LOCKED`, `DISABLED` (danh sách đề xuất) |
| `password_changed_at` | Thời điểm đổi mật khẩu, có thể NULL trước lần đặt mật khẩu đầu tiên |
| `created_at` | Thời điểm tạo |
| `updated_at` | Thời điểm cập nhật |

Quy tắc:

- `person_id UNIQUE` đảm bảo một Person Profile chỉ liên kết tối đa một Account; nhiều vai trò được gắn trong `ACCOUNT_ROLE` thay vì tạo nhiều Account.
- Thiết kế này không cần lưu `phone_number` lần nữa trong `ACCOUNT`: số điện thoại đăng nhập Customer/Owner lấy từ `PERSON_PROFILE` sau khi xác minh.
- Employee đăng nhập bằng `employee_code`, nhưng mật khẩu và trạng thái tài khoản vẫn được quản lý trong `ACCOUNT`.
- Nếu tất cả phương thức đăng nhập đều bắt buộc có mật khẩu, hãy đặt `password_hash NOT NULL`. Nếu cho phép Google-only, giữ nullable và kiểm tra kỹ các nhánh đăng nhập/khôi phục tài khoản.

### 4.3. `ACCOUNT_ROLE` — vai trò của Account

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `account_id` | PK ghép một phần; FK → `ACCOUNT.account_id` |
| `role` | PK ghép một phần; giá trị hiện tại: `CUSTOMER`, `OWNER`, `EMPLOYEE` |
| `assigned_at` | Thời điểm cấp vai trò |
| `assigned_by_account_id` | Người cấp vai trò, NULL nếu do quy trình hệ thống cấp; FK tùy triển khai |

Khóa chính ghép: **(`account_id`, `role`)**.

- Một Account không thể được gán trùng cùng một role.
- Một Account có thể có nhiều role, ví dụ `CUSTOMER` và `OWNER`.
- Không dùng `MANAGER` làm role đăng nhập độc lập trong thiết kế ban đầu. Quản lý là nhân viên có quyền mở rộng dựa trên chức vụ/quyền được cấp.
- Chỉ gán `OWNER` sau khi hệ thống xác minh được yêu cầu sở hữu/đăng ký theo quy trình; dù đã có role `OWNER`, vẫn phải kiểm tra trạng thái duyệt trước khi mở chức năng quản lý homestay.
- Có thể tạo bảng danh mục `ROLE` ở tương lai nếu role cần tên hiển thị, mô tả hoặc cấu hình phức tạp. Hiện tại các mã role cố định có thể được quản lý bằng enum/check constraint.

### 4.4. `CUSTOMER` — hồ sơ nghiệp vụ khách hàng

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `customer_id` | PK |
| `person_id` | FK → `PERSON_PROFILE.person_id`, UNIQUE, NOT NULL |
| `created_at` | Thời điểm tạo hồ sơ Customer |
| `updated_at` | Thời điểm cập nhật |

- Customer có thể tồn tại khi chưa có Account.
- Booking phải tham chiếu `customer_id`, không tham chiếu `account_id`, để giữ lịch sử đặt phòng kể cả khi khách chưa từng đăng nhập.
- Khi khách đã có `PERSON_PROFILE` và Customer record nhưng chưa có Account, sau khi OTP đúng thì tạo Account liên kết cùng `person_id`; không tạo Customer trùng.
- Nếu một người vốn là Owner rồi đăng ký làm Customer, dùng cùng Person Profile/Account nếu xác minh được quyền sở hữu tài khoản, rồi bổ sung hồ sơ Customer và role `CUSTOMER`.

### 4.5. `CUSTOMER_PROFILE` — dữ liệu đặc thù Customer

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `customer_id` | PK + FK → `CUSTOMER.customer_id` (1–1) |
| `citizen_id` | Có thể NULL; dữ liệu nhạy cảm dùng cho xác minh tuổi/danh tính khi cần |
| `student_id` | Có thể NULL; thông tin thẻ sinh viên nếu chính sách ưu đãi yêu cầu |
| `student_verification_status` | Trạng thái xác minh, ví dụ `NOT_SUBMITTED`, `PENDING`, `VERIFIED`, `REJECTED` |
| `created_at` | Thời điểm tạo |
| `updated_at` | Thời điểm cập nhật |

- Tạo `CUSTOMER_PROFILE` cùng `CUSTOMER` trong transaction nếu hệ thống muốn bảo đảm mọi khách đều có hàng hồ sơ mở rộng, kể cả khi các trường đặc thù còn NULL.
- Giới hạn quyền xem `citizen_id`/`student_id`; che một phần khi hiển thị và cân nhắc mã hóa khi triển khai thật.
- Chỉ thu thập giấy tờ khi nghiệp vụ thực sự cần; cần khảo sát xem CCCD/thẻ sinh viên được yêu cầu lúc đăng ký, lúc đặt phòng hay khi xin ưu đãi.

### 4.6. `OWNER` — hồ sơ nghiệp vụ chủ homestay

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `owner_id` | PK |
| `person_id` | FK → `PERSON_PROFILE.person_id`, UNIQUE, NOT NULL |
| `created_at` | Thời điểm tạo hồ sơ Owner |
| `updated_at` | Thời điểm cập nhật |

- `OWNER` có thể được tạo trước khi Owner có Account, để hỗ trợ nhân viên nhập hồ sơ nháp.
- Thông tin hợp tác, trạng thái xét duyệt và lịch sử yêu cầu thuộc `OWNER_REGISTRATION`, không nên đặt chung vào `ACCOUNT.status`.
- `HOMESTAY` tham chiếu `owner_id`. Một Owner có thể quản lý nhiều homestay nếu chính sách Fuij cho phép.
- Các giấy tờ pháp lý riêng của Owner/homestay chưa được chốt; nếu khảo sát yêu cầu, xem xét bảng tài liệu xác minh riêng thay vì thêm nhiều cột không dùng vào `PERSON_PROFILE`.

### 4.7. `OWNER_REGISTRATION` — hồ sơ đăng ký và xét duyệt Owner

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `owner_registration_id` | PK |
| `owner_id` | FK → `OWNER.owner_id` |
| `registration_source` | `SELF_SERVICE` hoặc `EMPLOYEE_ASSISTED` |
| `status` | `DRAFT`, `WAITING_OWNER_ACTIVATION`, `PENDING_REVIEW`, `UNDER_REVIEW`, `NEEDS_REVISION`, `APPROVED`, `REJECTED` (đề xuất) |
| `created_by_employee_id` | FK → `EMPLOYEE.employee_id`, NULL khi Owner tự đăng ký |
| `reviewed_by_employee_id` | FK → `EMPLOYEE.employee_id`, NULL trước khi duyệt |
| `review_note` | Ghi chú/lý do yêu cầu sửa hoặc từ chối |
| `submitted_at` | Thời điểm Owner hoàn tất và gửi hồ sơ |
| `reviewed_at` | Thời điểm xử lý duyệt gần nhất |
| `created_at` | Thời điểm tạo hồ sơ |
| `updated_at` | Thời điểm cập nhật |

Quy tắc:

- Hồ sơ nháp không đồng nghĩa với một tài khoản có thể đăng nhập.
- Trường hợp nhân viên hỗ trợ: tạo/cập nhật Person Profile, Owner và hồ sơ `OWNER_REGISTRATION` ở trạng thái `DRAFT`; chưa tạo mật khẩu cho Owner.
- Owner tự xác minh số điện thoại bằng OTP, tự thiết lập mật khẩu và hoàn tất gửi hồ sơ. Khi đó hệ thống tạo/liên kết Account và gán role phù hợp.
- Nhân viên không được biết hoặc tự đặt mật khẩu Owner.
- Cả hồ sơ tự đăng ký và hồ sơ do nhân viên hỗ trợ đều đi qua cùng quy trình xét duyệt.
- Nên chỉ cho phép một hồ sơ đăng ký đang chờ xử lý cho cùng Owner tại một thời điểm; lưu các hồ sơ cũ nếu cần lịch sử.
- `ACCOUNT.status = ACTIVE` không tự động có nghĩa Owner được duyệt. Kiểm tra `OWNER_REGISTRATION.status`/trạng thái phê duyệt trong authorization trước khi cho phép đăng phòng, quản lý giá hay nhận booking.
- Nếu hồ sơ đã tồn tại theo số điện thoại, không tự động cho nhân viên ghép hồ sơ vào Account đó chỉ dựa trên số điện thoại. Owner phải xác minh quyền kiểm soát; tránh lộ thông tin của người khác.

### 4.8. `EMPLOYEE` — hồ sơ nhân viên Fuij

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `employee_id` | PK |
| `person_id` | FK → `PERSON_PROFILE.person_id`, UNIQUE, NOT NULL |
| `employee_code` | UNIQUE, NOT NULL; mã dùng để đăng nhập nội bộ |
| `department` | Bộ phận, có thể NULL |
| `position` | Chức vụ, ví dụ nhân viên/quản lý |
| `employment_status` | `ACTIVE`, `SUSPENDED`, `RESIGNED` hoặc danh sách do Fuij thống nhất |
| `created_at` | Thời điểm tạo |
| `updated_at` | Thời điểm cập nhật |

- `PERSON_PROFILE` lưu thông tin cá nhân cơ bản; `EMPLOYEE` lưu thông tin công việc. Cách này cho phép cùng một người có các role khác nhau mà không phải sao chép họ tên, email và địa chỉ.
- Mã `employee_code` phải duy nhất; không dùng số điện thoại để đăng nhập vào cổng nội bộ.
- Quản lý vẫn là Employee. Quyền quản lý nhân sự, duyệt Owner và xem dashboard cần được cấp cụ thể; không nên chỉ dựa vào việc người dùng đăng nhập được với role `EMPLOYEE`.
- Khi nhân viên nghỉ việc, khóa/vô hiệu hóa Account và cập nhật trạng thái công việc; không xóa hồ sơ vì có thể cần truy vết thao tác trước đây.

### 4.9. `HOMESTAY` và `BOOKING`

- `HOMESTAY.owner_id` → `OWNER.owner_id`.
- `BOOKING.customer_id` → `CUSTOMER.customer_id`.
- Không thay các khóa ngoại nghiệp vụ này bằng `account_id`.
- Nếu cần lưu thông tin khách/chủ tại thời điểm giao dịch để làm bằng chứng lịch sử, cân nhắc snapshot nghiệp vụ riêng sau khi phỏng vấn; không tự động đọc dữ liệu hồ sơ hiện tại để đại diện cho toàn bộ lịch sử.

### 4.10. `ACCOUNT_ACTIVITY_LOG` — nhật ký an ninh/nghiệp vụ tài khoản

| Cột | Ràng buộc / ý nghĩa |
|---|---|
| `log_id` | PK |
| `account_id` | FK → `ACCOUNT.account_id`, NULL khi ghi sự kiện trước khi Account tồn tại |
| `event_type` | Ví dụ `ACCOUNT_CREATED`, `ROLE_ASSIGNED`, `GOOGLE_LINKED`, `PASSWORD_CHANGED`, `PASSWORD_RESET`, `OWNER_REGISTRATION_SUBMITTED`, `OWNER_APPROVED`, `OWNER_REJECTED` |
| `actor_type` | `CUSTOMER`, `OWNER`, `EMPLOYEE`, `SYSTEM` |
| `actor_id` | ID của người thực hiện; có thể là trường đa hình không đặt FK cho tới khi thống nhất mô hình |
| `detail` | Thông tin bổ sung, không ghi mật khẩu/OTP thô hoặc dữ liệu nhạy cảm không cần thiết |
| `ip_address` | Có thể NULL |
| `created_at` | Thời điểm ghi log |

- Log là append-only nếu cần phục vụ kiểm toán: thêm bản ghi, không sửa/xóa tùy tiện.
- Chưa cần ghi tất cả lần đăng nhập vào log nghiệp vụ. Nếu cần phát hiện tấn công, thiết kế security log/rate limit riêng và có chính sách lưu trữ rõ ràng.

## 5. Quy trình đăng ký và liên kết Account cho Customer

### 5.1. Customer mới, chưa có hồ sơ

```text
Nhập số điện thoại
    ↓
Chuẩn hóa số theo E.164
    ↓
Yêu cầu OTP (không tiết lộ số điện thoại có tồn tại hay không)
    ↓
Xác minh OTP thành công
    ↓
Tra PERSON_PROFILE theo số điện thoại
    ↓
Không tìm thấy hồ sơ
    ↓
Nhập thông tin cá nhân và đặt mật khẩu
    ↓
Tạo PERSON_PROFILE + CUSTOMER + CUSTOMER_PROFILE + ACCOUNT
+ ACCOUNT_ROLE(CUSTOMER) trong transaction
    ↓
Ghi ACCOUNT_ACTIVITY_LOG
```

Nếu quy trình Google-first được cho phép, `google_sub` được lưu sau bước Google xác thực và liên kết an toàn; không tự ghép chỉ theo tên hoặc email Google.

### 5.2. Customer đã có hồ sơ nhưng chưa có Account

```text
Nhập số điện thoại → OTP → xác minh thành công
    ↓
Tìm PERSON_PROFILE và CUSTOMER theo số điện thoại
    ↓
Tìm thấy hồ sơ, chưa có ACCOUNT
    ↓
Khách đặt mật khẩu
    ↓
Tạo ACCOUNT(person_id) + ACCOUNT_ROLE(CUSTOMER)
    ↓
Booking cũ vẫn tham chiếu cùng customer_id
```

### 5.3. Người đã có Account muốn thêm role Customer

Sau khi xác minh và chứng minh được quyền sử dụng Account hiện có, nếu người đó chưa có hồ sơ Customer thì tạo `CUSTOMER`/`CUSTOMER_PROFILE` và thêm role `CUSTOMER` vào `ACCOUNT_ROLE`. Không tạo Account mới chỉ vì người đó đăng ký thêm vai trò.

### 5.4. Quy tắc bảo mật số điện thoại

- Xác minh OTP trước khi tiết lộ/ghép hồ sơ theo số điện thoại; trả thông báo chung khi cần để tránh dò số.
- Số điện thoại được nhà mạng cấp lại có thể thuộc về người mới. Nếu hồ sơ cũ có lịch sử Booking hoặc dữ liệu nhạy cảm, không cho tự động nhận hồ sơ chỉ nhờ OTP; cần bằng chứng phụ hoặc nhân viên kiểm tra theo chính sách Fuij.
- Giới hạn số lần OTP sai và số lần gửi OTP; không xem “mã hết hạn” là biện pháp chống đoán mã đầy đủ.

## 6. Quy trình đăng ký Owner

### 6.1. Hai điểm bắt đầu, một quy trình xét duyệt

**Tự đăng ký:**

```text
Owner nhập thông tin trên website
    ↓
Xác minh số điện thoại bằng OTP
    ↓
Tạo mật khẩu / hoàn tất thông tin
    ↓
Tạo hoặc liên kết PERSON_PROFILE + OWNER + ACCOUNT
+ ACCOUNT_ROLE(OWNER) + OWNER_REGISTRATION
    ↓
Gửi hồ sơ → PENDING_REVIEW
    ↓
Nhân viên Fuij kiểm duyệt
    ↓
APPROVED / NEEDS_REVISION / REJECTED
```

**Nhân viên hỗ trợ nhập hồ sơ:**

```text
Nhân viên nhập thông tin Owner
    ↓
Lưu PERSON_PROFILE + OWNER + OWNER_REGISTRATION(DRAFT)
    ↓
Hệ thống gửi hướng dẫn để Owner hoàn tất
    ↓
Owner tự xác minh OTP và tự thiết lập mật khẩu
    ↓
Tạo hoặc liên kết ACCOUNT + ACCOUNT_ROLE(OWNER)
    ↓
Owner hoàn tất/gửi hồ sơ → PENDING_REVIEW
    ↓
Nhân viên Fuij kiểm duyệt
```

Quy tắc bảo mật:

- Nhân viên không được đặt/biết mật khẩu Owner.
- Không cấp quyền quản lý homestay đầy đủ khi hồ sơ còn `DRAFT`, `WAITING_OWNER_ACTIVATION`, `PENDING_REVIEW`, `NEEDS_REVISION` hoặc `REJECTED`.
- Tài khoản có thể đăng nhập để xem trạng thái/yêu cầu bổ sung nếu chính sách cho phép, nhưng endpoint quản lý homestay phải kiểm tra trạng thái duyệt.
- Ghi nhận ai tạo hồ sơ, ai sửa, ai duyệt và lý do từ chối/yêu cầu bổ sung. Cân nhắc yêu cầu người duyệt khác người hỗ trợ nhập trong nghiệp vụ cần kiểm soát cao.

## 7. Quy trình cấp tài khoản Employee

```text
Người có thẩm quyền tạo hồ sơ nhân viên
    ↓
Tạo PERSON_PROFILE + EMPLOYEE(employee_code)
    ↓
Tạo ACCOUNT + ACCOUNT_ROLE(EMPLOYEE)
    ↓
Gửi lời mời kích hoạt hoặc yêu cầu đặt mật khẩu lần đầu
    ↓
Nhân viên tự thiết lập mật khẩu
    ↓
Đăng nhập bằng employee_code + mật khẩu
```

- `employee_code` là duy nhất và là định danh đăng nhập nội bộ.
- Tài khoản chỉ được kích hoạt bởi quy trình có thẩm quyền; không cho nhân viên tự nhận role `EMPLOYEE` từ cổng đăng ký Customer.
- Khi đăng nhập, tìm Employee theo `employee_code`, lấy `person_id`, tìm Account liên kết, kiểm tra role `EMPLOYEE`, trạng thái Account và `employment_status`, rồi mới kiểm tra mật khẩu.
- Nếu nhân viên đồng thời là Customer/Owner, dùng chung Account và Person Profile khi đúng là cùng một người; thêm các role tương ứng. Các quyền nội bộ vẫn chỉ được mở ở khu vực Employee sau khi kiểm tra role/trạng thái.
- Quản lý là một chức vụ/phạm vi quyền trong EMPLOYEE. Cần khảo sát xem quản lý được duyệt hồ sơ Owner của chính nhân viên đang hỗ trợ hay cần cơ chế tách người xử lý/người duyệt.

## 8. Đăng nhập thường

### 8.1. Customer/Owner bằng số điện thoại + mật khẩu

```text
Nhập SĐT + Password
    ↓
Chuẩn hóa SĐT E.164
    ↓
Tìm PERSON_PROFILE theo phone_number → ACCOUNT theo person_id
    ↓
Kiểm tra trạng thái ACCOUNT và role phù hợp với khu vực đăng nhập
    ↓
So sánh password với password_hash
    ↓
Đăng nhập thành công
```

- Cổng Customer yêu cầu role `CUSTOMER`; cổng Owner yêu cầu role `OWNER`.
- Nếu một Account có cả hai role, cùng thông tin đăng nhập có thể vào từng khu vực nhưng mỗi khu vực vẫn kiểm tra role và các điều kiện nghiệp vụ riêng.
- Nếu role `OWNER` đã có nhưng hồ sơ chưa `APPROVED`, không cấp quyền quản lý homestay đầy đủ.
- Rate limit và khóa tạm khi nhập sai nhiều lần. Thông báo lỗi không phân biệt số điện thoại không tồn tại, sai mật khẩu hay không đủ điều kiện đăng nhập.

### 8.2. Employee bằng mã nhân viên + mật khẩu

```text
Nhập employee_code + Password
    ↓
Tìm EMPLOYEE theo employee_code
    ↓
Lấy person_id → tìm ACCOUNT
    ↓
Kiểm tra role EMPLOYEE, ACCOUNT.status, EMPLOYEE.employment_status
    ↓
Kiểm tra password_hash
    ↓
Đăng nhập vào cổng nội bộ
```

- Không dùng số điện thoại để tra cứu đăng nhập Employee.
- Không trả thông báo chi tiết tiết lộ mã nhân viên có tồn tại hay không.
- Quyền truy cập từng chức năng cần được kiểm tra ở backend, không chỉ ẩn/hiện nút ở giao diện.

## 9. Google Login

Google là phương thức bổ sung chủ yếu cho Customer trong phạm vi hiện tại. Có thể mở rộng cho Owner sau khi xác nhận yêu cầu.

**Account đã liên kết Google:**

```text
Google xác thực → nhận claim `sub` → tìm ACCOUNT.google_sub → kiểm tra status/role → đăng nhập
```

**Customer có Account và muốn liên kết Google:**

```text
Đăng nhập Account hiện có → yêu cầu liên kết Google
    ↓
Google xác thực → lưu google_sub duy nhất → ghi log GOOGLE_LINKED
```

**Customer mới bắt đầu từ Google:**

```text
Google xác thực → nhập SĐT → OTP
    ↓
Tra Person Profile/Customer/Account
    ↓
Nếu hồ sơ đã gắn Account: yêu cầu đăng nhập Account hiện hữu hoặc quy trình phục hồi đủ an toàn
Nếu hồ sơ chưa có Account: hoàn tất hồ sơ, tạo/liên kết Account và lưu google_sub
```

- Không tự hợp nhất Account chỉ theo tên hoặc email Google.
- Không cho một `google_sub` liên kết với nhiều Account.
- V4 đề xuất khách dùng Google vẫn đặt mật khẩu; đây chưa phải quyết định nghiệp vụ cuối cùng. Cần chốt trước khi đặt `password_hash NOT NULL` hay cho phép nullable.

## 10. OTP và SMS Gateway

Mục đích: xác minh số điện thoại khi đăng ký Customer/Owner, hoàn tất hồ sơ nháp, hoặc các thao tác nhạy cảm sau này như đổi số điện thoại/khôi phục mật khẩu.

Quy tắc ban đầu đề xuất:

| Quy tắc | Mục đích |
|---|---|
| OTP 6 chữ số, sinh bằng `SecureRandom` | Khó dự đoán |
| Hết hạn khoảng 3–5 phút | Giới hạn thời gian sử dụng |
| Mỗi OTP chỉ dùng một lần | Tránh tái sử dụng |
| Tối đa khoảng 5 lần nhập sai/mã | Chặn brute force |
| Gửi lại có cooldown khoảng 60 giây | Giảm spam |
| Mã mới vô hiệu mã cũ | Chỉ có một mã hợp lệ cho cùng mục đích |
| Rate limit theo số điện thoại và IP | Chặn spam và lạm dụng gateway |
| Lưu hash OTP thay vì OTP thô | Hạn chế hậu quả nếu dữ liệu bị lộ |

Lưu trữ:

- Chưa tạo bảng OTP trong bản thiết kế ban đầu; dùng cache có TTL và bộ đếm thử cùng bản ghi OTP.
- Cache trong bộ nhớ phù hợp prototype một instance nhưng mất dữ liệu khi restart. Nếu triển khai nhiều instance, dùng Redis hoặc cơ chế lưu trữ dùng chung.
- Nếu yêu cầu nghiệp vụ cần truy vết OTP, khôi phục sau restart, hoặc giao dịch cần bền vững, đánh giá lại việc dùng bảng riêng.

Kiến trúc gửi SMS:

```text
OtpService → SmsSender (interface) → PersonalGatewaySender
                                    → ProviderSender (nếu đổi nhà cung cấp sau này)
```

Không để nghiệp vụ xác minh OTP phụ thuộc trực tiếp vào một gateway cụ thể. Đường dự phòng khi SMS không gửi được cần quy trình xác minh thủ công có thẩm quyền và có audit log; không được bỏ qua xác minh một cách âm thầm.

## 11. Đổi số điện thoại, quên mật khẩu và khóa tài khoản

Các chức năng này chưa phải phần đã triển khai, nhưng schema cần cho phép mở rộng:

- **Đổi SĐT:** xác minh số mới bằng OTP; cập nhật `PERSON_PROFILE.phone_number` trong transaction; bảo đảm số mới duy nhất; ghi log. Cân nhắc xác minh số hiện tại/đăng nhập gần đây trước khi đổi.
- **Quên mật khẩu:** OTP chỉ xác minh số; sau khi xác minh, tạo mật khẩu mới và cập nhật `password_hash`, `password_changed_at`; ghi log; cân nhắc vô hiệu phiên đăng nhập cũ.
- **Khóa tài khoản:** cập nhật `ACCOUNT.status`; backend kiểm tra trạng thái ở mọi luồng đăng nhập. `EMPLOYEE.employment_status`/trạng thái duyệt Owner vẫn được kiểm tra riêng.
- **Nhân viên hỗ trợ khôi phục:** phải có quy trình xác minh danh tính, quyền hạn và audit log; không đặt mật khẩu tùy ý rồi gửi plaintext cho người dùng.

## 12. Skeleton SQL ở mức thiết kế

> Đây là skeleton để thể hiện quan hệ, chưa phải DDL chạy ngay. Cú pháp kiểu dữ liệu, index, check constraint và chiến lược xóa cần chốt theo DBMS.

```sql
PERSON_PROFILE (
    person_id       PK,
    full_name       NULLABLE,
    phone_number    NOT NULL UNIQUE, -- chuẩn hóa E.164
    email           NULLABLE,
    address         NULLABLE,
    created_at,
    updated_at
)

ACCOUNT (
    account_id       PK,
    person_id        NOT NULL UNIQUE FK -> PERSON_PROFILE.person_id,
    password_hash    NULLABLE, -- chỉ NULL nếu chính sách Google-only cho phép
    google_sub       NULLABLE UNIQUE,
    status           NOT NULL,
    password_changed_at NULLABLE,
    created_at,
    updated_at
)

ACCOUNT_ROLE (
    account_id       FK -> ACCOUNT.account_id,
    role             -- CUSTOMER / OWNER / EMPLOYEE
    assigned_at,
    assigned_by_account_id NULLABLE,
    PRIMARY KEY (account_id, role)
)

CUSTOMER (
    customer_id      PK,
    person_id        NOT NULL UNIQUE FK -> PERSON_PROFILE.person_id,
    created_at,
    updated_at
)

CUSTOMER_PROFILE (
    customer_id      PK + FK -> CUSTOMER.customer_id,
    citizen_id       NULLABLE,
    student_id       NULLABLE,
    student_verification_status,
    created_at,
    updated_at
)

OWNER (
    owner_id         PK,
    person_id        NOT NULL UNIQUE FK -> PERSON_PROFILE.person_id,
    created_at,
    updated_at
)

OWNER_REGISTRATION (
    owner_registration_id PK,
    owner_id              NOT NULL FK -> OWNER.owner_id,
    registration_source   NOT NULL,
    status                NOT NULL,
    created_by_employee_id NULLABLE FK -> EMPLOYEE.employee_id,
    reviewed_by_employee_id NULLABLE FK -> EMPLOYEE.employee_id,
    review_note           NULLABLE,
    submitted_at          NULLABLE,
    reviewed_at           NULLABLE,
    created_at,
    updated_at
)

EMPLOYEE (
    employee_id       PK,
    person_id         NOT NULL UNIQUE FK -> PERSON_PROFILE.person_id,
    employee_code     NOT NULL UNIQUE,
    department        NULLABLE,
    position          NULLABLE,
    employment_status NOT NULL,
    created_at,
    updated_at
)

-- BOOKING.customer_id -> CUSTOMER.customer_id
-- HOMESTAY.owner_id -> OWNER.owner_id
-- ACCOUNT_ACTIVITY_LOG lưu sự kiện liên quan Account/đăng ký/phê duyệt
```

### Lưu ý về DDL

- Vì `OWNER_REGISTRATION` tham chiếu `EMPLOYEE` trong khi các bảng có phụ thuộc qua hồ sơ, thứ tự tạo bảng SQL thực tế cần xử lý phù hợp (tạo bảng trước rồi thêm FK bằng `ALTER TABLE` nếu cần).
- `PERSON_PROFILE.phone_number UNIQUE` chỉ phù hợp khi mỗi số điện thoại chỉ xác định một Person Profile. Nếu có người dùng chung số, cần thay đổi thiết kế định danh đăng nhập.
- `ACCOUNT.person_id UNIQUE` và các `person_id UNIQUE` trong bảng hồ sơ biểu diễn quan hệ một-người-một-hồ-sơ-mỗi-loại. Một Person Profile có thể có Customer và Owner đồng thời, nhưng không có hai Customer record hoặc hai Owner record cùng loại.
- Cần đảm bảo role trong `ACCOUNT_ROLE` khớp hồ sơ tương ứng; ví dụ có role `CUSTOMER` thì phải có Customer record. Có thể kiểm tra trong service/transaction hoặc bổ sung cơ chế constraint phù hợp DBMS.

## 13. Checklist trước khi triển khai

- [ ] Chốt việc `password_hash` có bắt buộc cho tài khoản liên kết Google hay không.
- [ ] Xác nhận một số điện thoại chỉ được gắn với một Person Profile hay có trường hợp dùng chung.
- [ ] Chốt thời điểm yêu cầu CCCD/thẻ sinh viên và quy tắc xét ưu đãi.
- [ ] Xác nhận dữ liệu nào bắt buộc khi lưu Owner draft, hồ sơ nào cần giấy tờ xác minh.
- [ ] Chốt Owner có được đăng nhập/xem trạng thái hồ sơ khi chờ duyệt hay không.
- [ ] Chốt nhân viên nào được tạo tài khoản, hỗ trợ hồ sơ, duyệt Owner và cấp role.
- [ ] Xác nhận quản lý có được duyệt hồ sơ do chính mình nhập hay cần phân tách người nhập/người duyệt.
- [ ] Chốt việc ghi log đăng nhập thành công/thất bại và thời hạn lưu log.
- [ ] Chốt cache OTP: in-memory khi prototype hay Redis nếu chạy nhiều instance.
- [ ] Chốt luồng số điện thoại bị nhà mạng cấp lại và hồ sơ có lịch sử Booking.
- [ ] Thể hiện ERD cuối cùng và kiểm tra ràng buộc khóa ngoại/UNIQUE trước khi viết DDL triển khai.

## 14. Những thay đổi so với bản v4

- Thay `CUSTOMER_ACCOUNT` bằng `ACCOUNT` dùng chung.
- Thêm `ACCOUNT_ROLE` để một Account có nhiều vai trò.
- Đưa thông tin cá nhân chung vào `PERSON_PROFILE`.
- Giữ `CUSTOMER` và `CUSTOMER_PROFILE` cho hồ sơ nghiệp vụ và thông tin xác minh/ưu đãi riêng của Customer.
- Thêm `OWNER`, `OWNER_REGISTRATION` và quy trình tự đăng ký/nhân viên hỗ trợ nhập hồ sơ nháp.
- Thêm `EMPLOYEE` với `employee_code` là định danh đăng nhập nội bộ; mật khẩu vẫn được kiểm tra qua `ACCOUNT`.
- Đổi `CUSTOMER_ACTIVITY_LOG` thành `ACCOUNT_ACTIVITY_LOG` để theo dõi sự kiện của cơ chế Account dùng chung.
- Giữ nguyên nguyên tắc Booking tham chiếu `customer_id`, OTP có giới hạn số lần thử, SMS gửi qua interface và không lưu OTP thô.

> Tài liệu này là bản thiết kế để tiếp tục phỏng vấn/khảo sát và làm ERD. Không coi các trạng thái, trường tùy chọn hay chính sách chưa xác nhận là yêu cầu cuối cùng.
