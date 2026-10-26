# Giao diện WinForms: toàn bộ form, API dùng và việc cần sửa

Tài liệu gốc để làm lại giao diện `HomestayBooking.Desktop`. Mỗi form ghi rõ: dành cho vai trò nào, gồm những gì, gọi API nào, hiện chạy đến đâu, lỗi đã thấy. Danh sách API đầy đủ theo vai trò ở [desktop-api-theo-vai-tro.md](desktop-api-theo-vai-tro.md). Mọi đường dẫn API dưới đây đều có tiền tố `/api/desktop`.

Rà ngày 26/10/2026 trên nhánh `chore/desktophost-launchagent` (code desktop giống `main` = `e30439f`).

## 1. Kết luận nhanh

| Phần | Tình trạng |
|---|---|
| Backend và API desktop | **Không thấy lỗi chặn.** Phân quyền đúng ma trận, chủ không đọc được homestay của chủ khác (404), sai/thiếu token trả 401/403 đúng. Có vài lỗi nhỏ ở mục 2.2 |
| API còn thiếu | Web có nhưng desktop chưa có API: hoàn tiền, khoản bù, đối soát (phía kế toán), duyệt đổi/hủy, hợp đồng, voucher, duyệt homestay, duyệt đăng ký chủ, overbooking, tài khoản nhân viên (mục 2.3). **Các form tương ứng hiện là vỏ rỗng** |
| Giao diện nhân viên | 16 form: 3 khung menu, 9 form gọi API thật, 4 form rỗng hoặc chỉ báo "chưa hỗ trợ" |
| Giao diện chủ homestay | **Chưa có form nào.** API đã đủ 42 endpoint |

## 2. Kiểm backend và API

### 2.1 Đã kiểm

- Bộ test toàn solution: xem mục 6.
- Gọi thật vào DesktopHost trên Mac (`http://100.75.198.67:5080`) bằng 5 tài khoản demo, chỉ dùng GET để không đổi dữ liệu:
  - Tư vấn gọi `/receipts`, `/owner/dashboard` → 403. Kế toán gọi `/bookings` → 403. Quản lý gọi `/additional-deposits` → 403. Chủ gọi `/bookings` → 403. Không token → 401. Đúng bảng quyền.
  - Chủ `0912000001` (7 homestay) gọi lịch, chi tiết homestay `2` của chủ `0912000002` → 404. Không lộ dữ liệu chéo.
  - `page=999`, `page=0`, `page=-1` → API tự kẹp về trang hợp lệ, không lỗi 500.
  - Ngày sai (`checkIn` sau `checkOut`), báo cáo quá 366 ngày, thiếu `from/to` → 400 có câu tiếng Việt.

### 2.2 Lỗi nhỏ (không chặn làm giao diện)

| # | Lỗi | Ảnh hưởng tới WinForms | Gợi ý |
|---|---|---|---|
| 1 | Giá trị enum hoặc ngày sai định dạng trong query (ví dụ `/bookings?status=Foo`) trả lỗi validation mặc định của ASP.NET: không có `detail`, `title` tiếng Anh "One or more validation errors occurred." | MessageBox hiện câu tiếng Anh | Giao diện dùng ComboBox cố định giá trị nên ít gặp; nếu muốn sửa thì đổi `InvalidModelStateResponseFactory` riêng cho nhóm `/api/desktop` |
| 2 | `/owner/schedule?period=foo` báo "Khoảng lịch nhận–trả phải từ 1 đến 31 ngày." thay vì "period không hợp lệ" | Chỉ gặp khi gửi sai | Đổi câu báo lỗi |
| 3 | `/owner/calendar?month=2026-13` không báo lỗi mà tự quay về tháng hiện tại (giống web) | Có thể hiện nhầm tháng nếu client ghép chuỗi tháng sai | Client luôn gửi `yyyy-MM` từ DateTimePicker |
| 4 | DesktopHost ở chế độ Development dùng khóa ký JWT ngẫu nhiên: mỗi lần restart host (kể cả `launchctl kickstart` sau khi build) **mọi máy Windows bị đăng xuất** | Người dùng bị đá ra giữa chừng | Đặt `DesktopApi__SigningKey` cố định trong LaunchAgent (xem README DesktopHost mục JWT) |
| 5 | Token 60 phút, không có refresh token | Làm lâu sẽ bị đăng xuất | Chấp nhận; client bắt 401 thì đưa về màn đăng nhập |

### 2.3 API còn thiếu so với web

Đây là lý do một số form desktop đang rỗng. Muốn các form này chạy thật thì phải **thêm API trước**, cùng cách làm các đợt trước (controller trong `HomestayBooking.Web/Controllers/Api/Desktop`, gọi lại service web có sẵn, không viết lại nghiệp vụ).

| Vai trò | Chức năng trên web | Controller web | Form desktop đang chờ |
|---|---|---|---|
| Kế toán | Hoàn tiền cọc (danh sách, ghi nhận đã hoàn) | `Accounting/Refunds` | `FrmRefundAndCompensation` |
| Kế toán | Khoản bù cho khách (ghi nhận chi, từ chối) | `Accounting/Compensations` | `FrmRefundAndCompensation` |
| Kế toán | Đối soát với chủ: lập nháp, gửi, xác nhận thay chủ, mở lại, ghi nhận đã trả | `Accounting/Settlements` | `FrmSettlementManagement` |
| Kế toán | Tổng quan (số việc cần làm) | `Accounting/Dashboard` | chưa có form |
| Quản lý | Duyệt/từ chối yêu cầu đổi/hủy | `Staff/ChangeApprovals` | `FrmApprovalAndContract` |
| Quản lý | Overbooking: báo sự cố, chuyển phòng, thay homestay, hoàn tiền | `Staff/Overbooking` | chưa có form |
| Quản lý | Duyệt hồ sơ đăng ký chủ homestay | `Staff/OwnerRegistrations` | chưa có form |
| Tư vấn | Tạo yêu cầu đổi/hủy hộ khách | `Staff/ChangeRequests` | chưa có form |
| Quản trị | Hợp đồng với chủ (tạo, gia hạn, chấm dứt) | `Admin/Contracts` | `FrmApprovalAndContract` |
| Quản trị | Duyệt homestay, xem giấy tờ | `Admin/Homestays` | chưa có form |
| Quản trị | Voucher (phát, thu hồi) | `Admin/Vouchers` | `FrmApprovalAndContract` |
| Quản trị | Tài khoản nhân viên (tạo, đổi chức vụ, khóa, đặt lại mật khẩu) | `Admin/Accounts` | `FrmEmployeeAccountAdmin` |

Cần chốt: làm hết các nhóm này hay bỏ khỏi desktop (ẩn nút, để làm trên web). Nếu thời gian BTL ngắn, nên làm trước **Đối soát** và **Hoàn tiền/Khoản bù** vì chủ homestay trên desktop cần thấy bảng đối soát do kế toán gửi.

## 3. Khung chung của app

### 3.1 Luồng mở màn hình

```
FrmLogin
 ├─ Nhân viên (mã NV) → POST /auth/login → employee.position
 │   ├─ Consultant             → FrmEmployeeHome
 │   ├─ Accountant             → FrmAccountantDashboard
 │   └─ Manager, Administrator → FrmAdminDashboard
 └─ Chủ homestay (SĐT) → POST /owner/auth/login → FrmOwnerHome   (CHƯA CÓ)
```

Mỗi "Home/Dashboard" là khung có menu trái, nội dung bên phải. Form con được nhúng vào panel (`TopLevel = false`, `Dock = Fill`).

### 3.2 Lỗi chung đã thấy

| # | Lỗi | Hướng sửa |
|---|---|---|
| C1 | Có **3 lớp gọi HTTP khác nhau** (`Models/DesktopApi`, `Controllers/AccountantController`, `Controllers/AdminController`), mỗi lớp tự xử lý 401, JSON và lỗi theo kiểu riêng | Gộp thành một `ApiClient` duy nhất, dùng chung cho mọi form, kể cả form chủ |
| C2 | Gặp 401 thì gọi `Application.Restart()` giữa chừng: app tắt và mở lại, mất dữ liệu đang nhập | Đóng các form, quay về `FrmLogin`, giữ địa chỉ máy chủ và mã đăng nhập |
| C3 | Phiên đăng nhập là `EmployeeSession`, chỉ hợp với nhân viên | Đổi thành phiên chung có `Kind` (Employee/Owner), token, hạn, tên hiển thị |
| C4 | Designer đặt vị trí và kích thước bằng số cứng (`Location`, `Size`), ít `Anchor`/`Dock` | Dùng `TableLayoutPanel`/`FlowLayoutPanel`, `Dock`, `Anchor`; kiểm ở 100%, 125%, 150% DPI và cửa sổ 1366×768 |
| C5 | Bảng bind thẳng JSON hoặc DTO: nhiều bảng **tiêu đề cột tiếng Anh**, hiện cả cột id, enum để nguyên chữ Anh (`Deposited`, `Submitted`…) | Mọi bảng gọi `ShowColumns` với tiêu đề tiếng Việt; một hàm dịch enum sang tiếng Việt dùng chung |
| C6 | ComboBox trạng thái chứa giá trị enum tiếng Anh | Item hiển thị tiếng Việt, `Value` là enum gửi API |
| C7 | Nhiều form không tự tải dữ liệu khi mở (phải bấm Tìm) | Tải trang 1 ở `Shown` |
| C8 | Khi đang gọi API, nút vẫn bấm được, có thể gửi hai lần | Mỗi thao tác khóa nút cho tới khi xong (như `FrmLogin.SetBusy`) |
| C9 | Còn các handler rỗng do Designer sinh (`FrmLogin_Load`, `guna2HtmlLabel1_Click`, `btnTransactionPrev_Click`…) | Xóa |

## 4. Form nhân viên (đã có)

Ký hiệu trạng thái: **Chạy** = gọi API thật · **Rỗng** = chỉ có giao diện, không gọi API · **Giả** = bấm nút chỉ báo chưa hỗ trợ.

### 4.1 `FrmLogin`: Đăng nhập

- **Vai trò:** tất cả.
- **Thành phần:** ô địa chỉ máy chủ (mặc định `http://localhost:5080`), mã nhân viên, mật khẩu, nút hiện mật khẩu, "Ghi nhớ mã nhân viên", "Quên mật khẩu?", "Đăng nhập".
- **API:** `POST /auth/login`.
- **Trạng thái:** Chạy, nhưng **chỉ đăng nhập được nhân viên**.
- **Lỗi:**
  - Không có lựa chọn Chủ homestay. Số điện thoại bị gửi như mã nhân viên nên luôn bị 401.
  - Layout nút "Quên mật khẩu" lệch (đã ghi nhận trước đây).
- **Cần làm:** thêm lựa chọn **Nhân viên / Chủ homestay** (RadioButton hoặc tab). Đổi nhãn ô theo lựa chọn ("Mã nhân viên" / "Số điện thoại") và gọi đúng API (`/auth/login` với `employeeCode` hoặc `/owner/auth/login` với `phoneNumber`). Nhớ lựa chọn cuối cùng.

### 4.2 `FrmEmployeeHome`: Khung nhân viên tư vấn

- **Vai trò:** Tư vấn.
- **Menu:** Đơn đặt phòng, Đặt hộ, Khách hàng, Đăng xuất.
- **Trạng thái:** Chạy.
- **Lỗi:** còn nhánh `RolePlaceholderForm` ("sẽ triển khai ở phase tiếp theo") cho vai trò khác, nhưng không bao giờ chạy tới vì Kế toán/Quản lý đã mở khung riêng. Xóa nhánh này.

### 4.3 `FrmBookingManagement`: Quản lý đơn

- **Vai trò:** Tư vấn, Quản lý, Quản trị.
- **Thành phần:** ô tìm (mã đơn/tên/SĐT), lọc trạng thái, bảng đơn phân trang; tab chi tiết: Giá từng đêm, Trạng thái, Thanh toán & Bill, Đổi/Hủy, Nhật ký; khu thao tác: giao đơn, đổi trạng thái (trạng thái mới, mã phòng mới, ghi chú).
- **API:** `GET /bookings`, `GET /bookings/{id}`, `GET /assignees`, `POST /bookings/{id}/assign`, `POST /bookings/{id}/status`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Ô "phòng mới" bắt người dùng **gõ id phòng (số)**, người dùng không biết id. Gõ chữ thì `int.Parse` ném lỗi tiếng Anh.
  - 5 bảng chi tiết hiện cột JSON thô, tiêu đề tiếng Anh.
  - Thiếu bộ lọc API có sẵn: homestay, khoảng ngày nhận phòng, người phụ trách, "chưa giao".
  - Danh sách trạng thái đích cố định 5 giá trị, không xét trạng thái hiện tại của đơn.
- **Cần làm:** ô chọn phòng đổ từ dữ liệu chi tiết đơn (nếu API chi tiết chưa trả danh sách phòng trống thì để ô này ẩn). Lọc trạng thái đích theo trạng thái hiện tại. Thêm các bộ lọc trên.

### 4.4 `FrmCreateBooking`: Đặt hộ khách

- **Vai trò:** Tư vấn, Quản lý, Quản trị.
- **Thành phần:** bên trái là tìm phòng trống (ngày nhận/trả, khu vực, số khách, bảng kết quả phân trang, bấm đúp để chọn); bên phải là thông tin đặt (SĐT, họ tên, email, homestay, loại phòng, yêu cầu đặc biệt, kênh), nút Tạo đặt phòng, Làm mới.
- **API:** `GET /areas`, `GET /booking-homestays`, `GET /booking-homestays/{id}/room-types`, `GET /availability`, `POST /bookings` (có `submissionId` chống tạo đơn đôi), `GET /bookings/{id}`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Nút "Sau" không chặn ở trang cuối (API tự kẹp nên không lỗi, nhưng bấm vẫn gọi API).
  - Không kiểm SĐT, họ tên, loại phòng trước khi gửi; lỗi chỉ hiện sau khi API trả 400.
  - `ToLocalTime()` của hạn giữ chỗ phụ thuộc múi giờ máy Windows; máy để sai múi giờ sẽ hiện sai giờ.
- **Cần làm:** kiểm ô bắt buộc trước khi gửi. Tắt nút "Sau" ở trang cuối. Hiện giờ theo `Asia/Ho_Chi_Minh`.

### 4.5 `FrmCustomerManagement`: Hồ sơ khách

- **Vai trò:** Tư vấn, Quản lý, Quản trị.
- **Thành phần:** ô tìm, bảng khách phân trang; form sửa (họ tên, SĐT, email, CCCD, thẻ SV, ghi chú), nhãn "khách có tài khoản / vãng lai"; tab Đơn đặt phòng, Voucher.
- **API:** `GET /customers`, `GET /customers/{id}`, `PUT /customers/{id}`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Mở form thì bảng trống, phải bấm "Tìm khách".
  - Ô CCCD/thẻ SV có thể gõ vào nhưng không được gửi khi lưu. Người dùng tưởng đã sửa.
  - MessageBox "Đã lưu" không có tiêu đề/icon, khác các form khác.
- **Cần làm:** tự tải khi mở. Ô CCCD/thẻ SV để `ReadOnly`. Thống nhất MessageBox.

### 4.6 `FrmAccountantDashboard`: Khung kế toán

- **Vai trò:** Kế toán.
- **Menu:** Duyệt Bill Cọc, Kiểm kê & Cọc bổ sung, Hoàn tiền & Khoản bù, Đối soát Chủ Homestay, Ngày lễ & Danh mục, Làm mới, Đăng xuất.
- **Trạng thái:** Chạy, nhưng 2/5 mục menu mở form rỗng.
- **Lỗi:** tên menu "Kiểm kê" sai nghĩa, nội dung là giao dịch ngân hàng. Đổi thành "Giao dịch & Cọc bổ sung".

### 4.7 `FrmReceiptReview`: Duyệt bill cọc

- **Vai trò:** Kế toán, Quản lý (duyệt được); Quản trị (chỉ xem).
- **Thành phần:** lọc trạng thái, bảng bill phân trang, khung ảnh bill (phóng to/thu nhỏ/xoay/đặt lại), mã đơn, số tiền phải cọc, nội dung chuyển khoản, số lần đã từ chối, ô lý do, nút Duyệt/Từ chối.
- **API:** `GET /receipts`, `GET /receipts/{id}/file`, `POST /receipts/{id}/approve`, `POST /receipts/{id}/reject`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Bill PDF chỉ lưu vào thư mục tạm, hiện đường dẫn; không mở được.
  - Bảng bind DTO, tiêu đề cột tiếng Anh, có cột id.
  - Lọc trạng thái là chữ Anh (`Submitted`…).
- **Cần làm:** nút "Mở PDF" (`Process.Start` với `UseShellExecute = true`). Đặt tiêu đề cột tiếng Việt.

### 4.8 `FrmBankAndAdditionalDeposit`: Giao dịch ngân hàng và cọc bổ sung

- **Vai trò:** Kế toán (tab cọc bổ sung chỉ Kế toán gọi được). Quản lý gọi được API giao dịch nhưng không có menu mở form này.
- **Tab Giao dịch:** lọc trạng thái khớp, bảng phân trang, ô mã đơn, ghi chú, mã chuyển hoàn; nút Khớp tay, Bỏ qua, Đã chuyển trả.
- **Tab Cọc bổ sung:** bảng phân trang, ô ghi chú tài chính, lý do từ chối; nút Xác nhận, Từ chối.
- **API:** `GET /bank-transactions`, `POST /bank-transactions/{id}/match|ignore|refund`, `GET /additional-deposits`, `POST /additional-deposits/{id}/confirm|reject`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Ô "Ghi chú" dùng chung cho cả ghi chú khớp tay, lý do bỏ qua và ghi chú chuyển trả. Dễ gửi nhầm nội dung.
  - Không xem được ảnh bill cọc bổ sung (`fileUrl` có trong API nhưng form không dùng).
  - Tiêu đề cột tiếng Anh.
  - Quản lý không có đường vào form này.
- **Cần làm:** tách ô theo từng thao tác, hoặc mỗi thao tác mở một hộp thoại nhỏ. Thêm khung xem ảnh dùng lại code của `FrmReceiptReview`. Cân nhắc thêm menu cho Quản lý.

### 4.9 `FrmRefundAndCompensation`: Hoàn tiền và khoản bù

- **Vai trò:** Kế toán.
- **Thành phần đã vẽ:** tab Hoàn tiền cọc (bảng, ô mã giao dịch, nút "Ghi nhận Đã hoàn tiền"); tab Khoản bù (bảng, ô mã giao dịch chi bù, lý do từ chối, nút Ghi nhận/Từ chối).
- **API:** **chưa có** (mục 2.3).
- **Trạng thái:** **Rỗng.** Code chỉ có `InitializeComponent()`, bấm nút không làm gì.

### 4.10 `FrmSettlementManagement`: Đối soát chủ homestay

- **Vai trò:** Kế toán.
- **Thành phần đã vẽ:** lọc năm/tháng/kỳ/trạng thái, bảng bảng đối soát, bảng các đơn trong bảng; ô lý do, mã giao dịch, ngày trả; nút Lập bảng nháp, Gửi cho chủ, Xác nhận thay chủ, Mở lại bản nháp, Ghi nhận đã thanh toán.
- **API:** **chưa có** (mục 2.3).
- **Trạng thái:** **Rỗng.** Các ComboBox không có item, nút không làm gì.

### 4.11 `FrmCatalogLookup`: Tra cứu danh mục

- **Vai trò:** Kế toán (chỉ xem).
- **Thành phần:** 4 bảng: Ngày lễ, Khu vực, Danh mục loại phòng, Tiện nghi; nút Làm mới.
- **API:** `GET /holidays`, `/areas`, `/room-categories`, `/amenities` (gom hết các trang).
- **Trạng thái:** Chạy.
- **Lỗi:** tiêu đề cột tiếng Anh; nhóm khu vực hiện `InnerCity`/`Suburb`. Layout khung danh mục lệch (đã ghi nhận trước đây).

### 4.12 `FrmAdminDashboard`: Khung quản lý và quản trị

- **Vai trò:** Quản lý, Quản trị.
- **Menu:** Báo cáo & Phân tích, Quản lý Danh mục, Giám sát Đơn đặt, Đặt hộ khách hàng, Hồ sơ Khách hàng, Kiểm tra Bill Cọc, Quản trị Nhân sự (chỉ Quản trị), Hợp đồng & Phê duyệt, Làm mới, Đăng xuất.
- **Trạng thái:** Chạy; 2 mục mở form giả.
- **Lỗi:** nút "Làm mới" chỉ chạy với form có `IAccountantRefreshable`; ở Đơn, Đặt hộ, Khách hàng bấm không có tác dụng.

### 4.13 `FrmAnalyticsReports`: Báo cáo

- **Vai trò:** Quản lý, Quản trị.
- **Thành phần:** từ ngày, đến ngày, khu vực, nút Xem; 4 thẻ số (tổng đơn, đơn đã cọc, doanh thu phòng hoàn tất, doanh thu ròng Fuji); bảng phân rã trạng thái × kênh, bảng nhân viên, bảng homestay.
- **API:** `GET /areas`, `GET /reports/management`.
- **Trạng thái:** Chạy. Tên trường khớp DTO của API.
- **Lỗi:**
  - Tiêu đề cột có dấu gạch dưới (`Trạng_thái`, `Mã_NV`) do dùng tên thuộc tính ẩn danh.
  - Thiếu số liệu API đã trả: tỷ lệ hủy/từ chối/hết hạn, tiền cọc giữ lại, hoa hồng, tiền bù, cờ "số liệu tạm tính" (`isProvisional`), thời gian xác nhận trung bình của homestay.
  - Không lọc theo homestay (API có `homestayId`).
- **Cần làm:** dùng `ShowColumns` để đặt tiêu đề. Thêm các thẻ số còn thiếu. Nếu cần biểu đồ thì dùng `System.Windows.Forms.DataVisualization` hoặc vẽ cột đơn giản.

### 4.14 `FrmCatalogManagement`: Quản lý danh mục

- **Vai trò:** Quản lý, Quản trị.
- **Thành phần:** 4 nút chuyển loại (Khu vực, Tiện nghi, Loại phòng, Ngày lễ), bảng, form nhập đổi theo loại, nút Thêm, Cập nhật, Xóa, Làm mới ô nhập.
- **API:** `GET/POST/PUT/DELETE /areas`, `/amenities`, `/room-categories`, `/holidays/{yyyy-MM-dd}`.
- **Trạng thái:** Chạy.
- **Lỗi:**
  - Ngày lễ: khi Cập nhật chỉ đổi tên. Đổi ngày trên DateTimePicker không có tác dụng nhưng ô vẫn cho chỉnh. Nên khóa ô ngày khi đang sửa.
  - Nút loại đang chọn không đổi màu.
  - `lblStatus` hiện mã kỹ thuật (`Danh mục: room-categories`).
  - Nhóm khu vực là chữ Anh.

### 4.15 `FrmApprovalAndContract`: Hợp đồng và phê duyệt

- **Vai trò:** Quản lý (duyệt đổi/hủy), Quản trị (hợp đồng, voucher).
- **Thành phần đã vẽ:** nút duyệt/từ chối đổi/hủy, tạo hợp đồng, phát voucher, thu hồi voucher.
- **API:** **chưa có**. Form cố ý gọi `POST /management/unsupported` để nhận lỗi rồi báo "không thể…".
- **Trạng thái:** **Giả.**
- **Cần làm:** chốt có làm API không (mục 2.3). Nếu không làm thì ẩn menu này thay vì để nút báo lỗi.

### 4.16 `FrmEmployeeAccountAdmin`: Quản trị nhân sự

- **Vai trò:** Quản trị.
- **Thành phần đã vẽ:** lọc chức vụ, trạng thái; bảng; nút Tạo, Cập nhật, Khóa/Mở, Đặt lại mật khẩu.
- **API:** **chưa có**.
- **Trạng thái:** **Giả.** Mọi nút báo "Backend hiện chưa cung cấp API…".

## 5. Form chủ homestay (chưa có, cần làm mới)

Toàn bộ API đã có sẵn (mục 7 của [desktop-api-theo-vai-tro.md](desktop-api-theo-vai-tro.md)). Chủ chỉ thấy homestay của mình; gọi bằng id của chủ khác API trả 404, nên client cứ hiện câu `detail`.

### 5.1 `FrmOwnerHome`: Khung chủ

- **Mở từ:** `FrmLogin` khi chọn Chủ homestay và đăng nhập thành công.
- **Đầu khung:** tên chủ, SĐT (`/owner/auth/me` hoặc dữ liệu login).
- **Menu:** Tổng quan, Lịch nhận/trả, Đơn, Lịch phòng, Homestay, Đánh giá, Đối soát, Làm mới, Đăng xuất.

### 5.2 `FrmOwnerDashboard`: Tổng quan

- **API:** `GET /owner/dashboard`.
- **Hiện:** trạng thái hồ sơ đăng ký và ghi chú duyệt; thẻ số: khách đến hôm nay, đơn chờ xác nhận, đơn quá hạn xác nhận; danh sách homestay (tên, khu vực, trạng thái). Bấm vào đơn chờ thì mở `FrmOwnerBookings` đã lọc sẵn.

### 5.3 `FrmOwnerSchedule`: Lịch nhận và trả phòng

- **API:** `GET /owner/schedule?period=today|tomorrow|week&date=&homestayId=`.
- **Hiện:** nút Hôm nay / Ngày mai / 7 ngày, ô chọn ngày, lọc homestay; hai bảng **Khách đến** và **Khách đi** (mã đơn, khách, SĐT, homestay, phòng, ngày).
- **Thao tác nhanh:** nút Nhận phòng / Hoàn thành gọi API của `FrmOwnerBookings`.

### 5.4 `FrmOwnerBookings`: Đơn của chủ

- **API:** `GET /owner/bookings?status=&q=&page=`, `GET /owner/bookings/{id}`, `POST /owner/bookings/{id}/confirm` (`{ roomId }` hoặc `{}`), `/reject` (`{ reason }`), `/check-in`, `/complete`, `/no-show`.
- **Hiện:** lọc trạng thái, ô tìm, bảng phân trang (đơn chờ xác nhận lên đầu); khung chi tiết: thông tin khách, ngày, phòng, tiền, lịch sử.
- **Nút theo trạng thái đơn:**

| Trạng thái đơn | Nút hiện |
|---|---|
| Đã cọc, chờ chủ xác nhận | Xác nhận (kèm ô chọn phòng trống cùng loại từ chi tiết đơn), Từ chối (bắt buộc lý do, báo trước "Fuji hoàn 100% cọc cho khách") |
| Đã xác nhận, tới ngày nhận | Nhận phòng; sau ngày nhận thêm Khách không đến |
| Đang ở | Hoàn thành |

### 5.5 `FrmOwnerCalendar`: Lịch phòng và khóa phòng

- **API:** `GET /owner/calendar?homestayId=&month=yyyy-MM&roomId=`, `POST /owner/homestays/{homeId}/rooms/{roomId}/blocks`, `DELETE …/blocks/{blockId}`.
- **Hiện:** chọn homestay, phòng, tháng (nút tháng trước/sau); lưới tháng 7 cột, mỗi ô ngày tô màu theo trạng thái (trống, có đơn, bị khóa, đã qua).
- **Khóa phòng:** chọn khoảng ngày `[bắt đầu, kết thúc)`, lý do (Kênh khác / Bảo trì), ghi chú. API trả 409 kèm danh sách ngày trùng đơn thì hiện danh sách đó.
- **Mở khóa:** chỉ các đêm từ hôm nay trở đi.
- **Gợi ý vẽ:** `TableLayoutPanel` 7×6 ô `Label`, không cần thư viện lịch.

### 5.6 `FrmOwnerHomestays`: Homestay của chủ

Màn lớn nhất. Nên làm dạng **danh sách bên trái + editor nhiều tab bên phải**.

- **Danh sách:** `GET /owner/homestays?q=&page=`.
- **Tải editor:** `GET /owner/homestays/{id}` (một lần cho mọi tab). Danh sách khu vực lấy từ `GET /owner/homestays/areas`.

| Tab | Nội dung | API ghi |
|---|---|---|
| Thông tin | Tên, mô tả, hướng dẫn nhận phòng (HTML, dùng TextBox nhiều dòng), SĐT liên hệ, giờ nhận/trả | `PUT /owner/homestays/{id}/content` |
| Vị trí | Địa chỉ, khu vực, vĩ độ, kinh độ. Báo trước: sửa có thể đưa homestay về chờ duyệt | `PUT …/location` |
| Tiện nghi | CheckedListBox tiện nghi | `PUT …/amenities` |
| Loại phòng & giá | Bảng loại phòng; thêm/sửa (tên, danh mục, số khách, diện tích, mô tả, 3 giá đầu); bật/tắt bán (API trả danh sách đơn sắp tới cần liên hệ, phải hiện ra); giá theo loại ngày áp dụng từ ngày | `POST …/room-types`, `PUT …/room-types/{typeId}`, `PUT …/active`, `PUT …/prices` |
| Phòng | Bảng phòng theo loại; thêm phòng, đổi mã, bật/tắt | `POST …/room-types/{typeId}/rooms`, `PUT …/rooms/{roomId}`, `PUT …/rooms/{roomId}/active` |
| Ảnh | Lưới ảnh; tải lên (OpenFileDialog, JPG/PNG/WEBP ≤ 5MB, multipart), gắn loại phòng, thứ tự, đặt bìa, xóa | `POST …/images`, `PUT …/images/{imageId}`, `POST …/images/{imageId}/cover`, `DELETE …/images/{imageId}` |
| Chính sách hủy | Chọn phạm vi (Tất cả / Ngày lễ), tên, bảng quy tắc (số ngày trước, % hoàn); gỡ để về mặc định | `PUT …/policies`, `DELETE …/policies/{scope}` |
| Giấy tờ | Bảng giấy tờ; tải lên (loại, ngày hết hạn, ảnh/PDF ≤ 5MB); xem (tải bằng token rồi mở); xóa khi còn nháp/chờ duyệt | `POST …/documents`, `DELETE …/documents/{documentId}`, `GET /owner/documents/{id}/file` |

Lưu ý: ảnh homestay công khai nằm ở `/images/...` và `/uploads/...`. **DesktopHost không phục vụ các đường dẫn này** nên trên host demo ảnh sẽ không hiện; với Web thật thì hiện được. Form phải xử lý ảnh lỗi (hiện khung xám) thay vì báo lỗi.

### 5.7 `FrmOwnerReviews`: Đánh giá

- **API:** `GET /owner/reviews?homestayId=&page=`, `POST /owner/reviews/{id}/reply` (`{ reply }`, 1–1000 ký tự).
- **Hiện:** chọn homestay, điểm trung bình và số lượt; bảng đánh giá (khách, điểm, nội dung, ngày, đã trả lời chưa); ô trả lời và nút Gửi.

### 5.8 `FrmOwnerSettlements`: Đối soát

- **API:** `GET /owner/settlements?status=&page=`, `GET /owner/settlements/{id}`, `POST /owner/settlements/{id}/confirm`, `POST /owner/settlements/{id}/discrepancy` (`{ note }`, 1–500 ký tự).
- **Hiện:** lọc trạng thái (Đã gửi / Đã xác nhận / Đã thanh toán), bảng kỳ đối soát (homestay, năm, tháng, kỳ, tổng tiền phòng, giữ lại, hoa hồng, thực nhận); chi tiết: từng đơn, khoản bù, lịch sử.
- **Nút:** Xác nhận đúng; Báo sai lệch (bắt buộc ghi chú). Chỉ bật khi bảng ở trạng thái Đã gửi.

## 6. Kết quả test

Chạy `dotnet test HomestayBooking.sln` trên Mac ngày 26/10/2026:

- `HomestayBooking.Tests` (backend, API web và API desktop, DesktopHost SQLite): **1905 đạt, 0 trượt, 1 bỏ qua** / 1906, khoảng 4 phút.
- `HomestayBooking.Desktop.Tests` (test giao diện WinForms): không chạy được trên Mac vì cần Windows. Bộ này chạy trên GitHub Actions `windows-latest` (workflow `desktop-ui.yml`); lần chạy gần nhất trên `main` (run 40307332746, 26/10) đạt.

## 7. Thứ tự làm đề xuất

1. **Khung chung (mục 3.2):** một `ApiClient`, phiên chung nhân viên/chủ, xử lý 401 về màn đăng nhập, hàm dịch enum, quy ước layout. Làm trước để mọi form sau dùng lại.
2. **`FrmLogin`** thêm lựa chọn Chủ homestay, rồi **`FrmOwnerHome` + Tổng quan + Đơn + Lịch nhận/trả**: đủ để chủ đăng nhập và xử lý đơn.
3. **Sửa 9 form nhân viên đang chạy** theo các mục "Lỗi" ở trên, chủ yếu là layout, tiêu đề tiếng Việt, ô chọn phòng.
4. **Chủ:** Lịch phòng, Homestay (editor), Đánh giá, Đối soát.
5. **Chốt API còn thiếu (mục 2.3):** làm API rồi mới nối `FrmRefundAndCompensation`, `FrmSettlementManagement`, `FrmApprovalAndContract`, `FrmEmployeeAccountAdmin`; hoặc ẩn các menu này.
