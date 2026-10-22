# API desktop (WinForms) theo vai trò

Tài liệu tra cứu nhanh cho người làm giao diện WinForms: mỗi vai trò đăng nhập thế nào, gọi được những API nào và mỗi API làm gì. Chi tiết kỹ thuật đầy đủ (mã lỗi, ví dụ curl) ở [`docs/api-va-backend.md` mục 8](../api-va-backend.md#8-api-cho-ứng-dụng-desktop).

## 1. Thông tin chung

### Địa chỉ API

| Môi trường | Địa chỉ gốc | Ghi chú |
|---|---|---|
| Host SQLite chạy trên Mac (làm và demo WinForms) | `http://<IP Mac>:5080/api/desktop` | Chạy `dotnet run --project HomestayBooking.DesktopHost -- --urls http://0.0.0.0:5080`. Swagger: `http://<IP Mac>:5080/swagger` |
| Production | `https://fuji-homestay.tail3e48c6.ts.net/api/desktop` | Dữ liệu thật, không có Swagger, không có tài khoản demo |

WinForms chỉ gọi API qua HTTP, **không kết nối thẳng database**. Nên để địa chỉ gốc trong file cấu hình vì IP của Mac đổi theo mạng.

### Tài khoản demo (chỉ có trên host SQLite)

Mật khẩu chung: `Fuji@2026`.

| Vai trò | Đăng nhập bằng |
|---|---|
| Nhân viên tư vấn | `NV001`, `NV002` |
| Kế toán | `KT001` |
| Quản lý | `QL001` |
| Quản trị viên | `AD001` |
| Chủ homestay | SĐT `0912000001`, `0912000002` |

### Đăng nhập và token

Có **hai cửa đăng nhập**: nhân viên (mã nhân viên) và chủ homestay (số điện thoại). Cả hai trả về `accessToken` (JWT).

- Gửi token trong mọi request sau đó: header `Authorization: Bearer <accessToken>`.
- Token hết hạn sau **60 phút** (`expiresAt`, giờ UTC). Khi API trả **401** thì cho người dùng đăng nhập lại.
- Tài khoản bị khóa, bị đổi mật khẩu hoặc nhân viên nghỉ việc thì token cũ bị từ chối ngay ở request kế tiếp.
- Đăng nhập sai quá **10 lần/phút** từ một máy → **429**, chờ rồi thử lại.
- Token nhân viên không dùng được cho API chủ và ngược lại (**403**).

### Lỗi trả về

Mọi lỗi là JSON dạng ProblemDetails, câu tiếng Việt nằm ở trường `detail`, chỉ cần hiện lên `MessageBox`:

```json
{ "title": "Conflict", "status": 409, "detail": "Tên này đã tồn tại." }
```

| Mã | Ý nghĩa |
|---|---|
| 400 | Dữ liệu sai hoặc vi phạm luật nghiệp vụ |
| 401 | Chưa đăng nhập, token sai hoặc hết hạn |
| 403 | Đăng nhập rồi nhưng vai trò không được dùng chức năng này |
| 404 | Không tìm thấy (kể cả dữ liệu của chủ khác: trả 404 để không lộ là có tồn tại) |
| 409 | Trùng dữ liệu hoặc vừa bị người khác sửa |
| 429 | Đăng nhập quá nhiều lần |

### Quy ước dữ liệu

- Ngày: `yyyy-MM-dd`. Thời điểm: UTC, có hậu tố `Z`.
- Danh sách có phân trang trả `{ items, page, totalPages, totalCount }` (10 mục/trang), truyền `?page=2` để lấy trang sau.
- Giá trị enum gửi và nhận dạng **chữ**:

| Enum | Giá trị |
|---|---|
| Trạng thái đơn | `AwaitingPayment`, `PaymentSubmitted`, `Deposited`, `Confirmed`, `CheckedIn`, `Completed`, `Expired`, `Rejected`, `Cancelled`, `NoShow` |
| Kênh đặt hộ | `Phone`, `Zalo`, `Messenger`, `Other` (không dùng `Web` khi nhân viên đặt hộ) |
| Trạng thái bill | `Submitted`, `Approved`, `Rejected` |
| Nhóm khu vực | `InnerCity`, `Suburb`, `OtherProvince` |
| Lý do khóa phòng | `OtherChannel`, `Maintenance` |
| Loại ngày (giá) | `Weekday`, `Weekend`, `Holiday` |
| Phạm vi chính sách hủy | `All`, `Holiday` |
| Loại giấy tờ | `BusinessLicense`, `FireSafety`, `PropertyPhoto`, `Other` |
| Trạng thái đối soát | `Sent`, `Confirmed`, `Paid` |

## 2. Bảng quyền tổng hợp

✅ được dùng · ❌ bị chặn (403)

| Nhóm chức năng | Tư vấn | Kế toán | Quản lý | Quản trị viên | Chủ homestay |
|---|:-:|:-:|:-:|:-:|:-:|
| Đăng nhập nhân viên, xem hồ sơ (`/auth`) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Đơn đặt phòng, đặt hộ, phòng trống | ✅ | ❌ | ✅ | ✅ | ❌ |
| Khách hàng | ✅ | ❌ | ✅ | ✅ | ❌ |
| Đọc danh mục (khu vực, tiện nghi, loại phòng, ngày lễ) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Thêm/sửa/xóa danh mục | ❌ | ❌ | ✅ | ✅ | ❌ |
| Xem bill cọc | ❌ | ✅ | ✅ | ✅ | ❌ |
| Duyệt/từ chối bill cọc | ❌ | ✅ | ✅ | ⚠️ | ❌ |
| Báo cáo | ❌ | ❌ | ✅ | ✅ | ❌ |
| Toàn bộ API chủ (`/owner/...`) | ❌ | ❌ | ❌ | ❌ | ✅ |

⚠️ Quản trị viên xem được bill nhưng nghiệp vụ chỉ cho **Kế toán và Quản lý** duyệt/từ chối; quản trị viên gọi sẽ nhận 400.

## 3. Nhân viên tư vấn

### Đăng nhập

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| POST | `/auth/login` | Đăng nhập bằng mã nhân viên + mật khẩu, nhận token |
| GET | `/auth/me` | Thông tin nhân viên đang đăng nhập |

```json
// POST /api/desktop/auth/login
{ "employeeCode": "NV001", "password": "Fuji@2026" }

// Trả về
{ "accessToken": "eyJ...", "expiresAt": "2026-10-22T11:00:00Z",
  "employee": { "id": 4, "code": "NV001", "fullName": "Nguyễn Thị Lan", "position": "Consultant" } }
```

Dùng `employee.position` (`Consultant`, `Accountant`, `Manager`, `Administrator`) để WinForms quyết định mở màn hình nào.

### Đơn đặt phòng

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/bookings` | Tìm đơn. Lọc: `q` (mã đơn/tên/SĐT), `status`, `homestayId`, `checkInFrom`, `checkInTo`, `assignedEmployeeId`, `unassigned=true`, `page` |
| GET | `/bookings/{id}` | Chi tiết đơn: giá từng đêm, lịch sử trạng thái, yêu cầu đổi/hủy, thanh toán, nhật ký |
| POST | `/bookings` | Đặt phòng hộ khách gọi điện/Zalo (khách chưa có hồ sơ thì tự tạo) |
| POST | `/bookings/{id}/assign` | Giao đơn cho nhân viên khác `{ "employeeId": 5 }` |
| POST | `/bookings/{id}/status` | Đổi trạng thái đơn, bắt buộc ghi chú (không đổi được trạng thái tiền, không hủy trực tiếp) |
| GET | `/assignees` | Danh sách nhân viên có thể nhận đơn (để đổ ô chọn) |

```json
// POST /api/desktop/bookings: đặt hộ
{ "phone": "0987000123", "fullName": "Nguyễn Văn A", "email": null,
  "roomTypeId": 3, "checkIn": "2026-11-01", "checkOut": "2026-11-03", "guests": 2,
  "specialRequests": "Phòng tầng thấp", "channel": "Phone",
  "submissionId": "6f1c2a5e-0b1d-4a7e-9c3e-2f5d8a1b7c40" }
```

`submissionId` là một GUID mới **cho mỗi lần bấm "Đặt"**. Nếu mạng lỗi và gửi lại cùng GUID, API trả lại đơn cũ chứ không tạo đơn thứ hai.

```json
// POST /api/desktop/bookings/{id}/status
{ "status": "CheckedIn", "note": "Khách đã nhận phòng", "newRoomId": null }
```

### Chọn phòng khi đặt hộ

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/booking-homestays` | Danh sách homestay (ô chọn thứ nhất) |
| GET | `/booking-homestays/{homestayId}/room-types` | Loại phòng đặt được của homestay đó (ô chọn thứ hai), kèm số khách tối đa và số phòng đang kinh doanh |
| GET | `/availability` | Phòng trống và giá theo ngày: `checkIn`, `checkOut` (bắt buộc), `areaId`, `guests` (mặc định 1), `page` |

### Khách hàng

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/customers` | Tìm khách theo `q` (tên, email hoặc SĐT), `page`. Tư vấn thấy giấy tờ (CCCD, thẻ SV) đã được che |
| GET | `/customers/{id}` | Chi tiết khách, các đơn đã đặt, voucher còn dùng được |
| PUT | `/customers/{id}` | Sửa ghi chú, họ tên, email, SĐT `{ "note": "...", "fullName": "...", "email": "...", "phoneNumber": "..." }`. Khách có tài khoản thì tự sửa tên/email của mình |

### Danh mục (chỉ đọc, để đổ ô chọn)

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/areas`, `/areas/{id}` | Khu vực |
| GET | `/amenities`, `/amenities/{id}` | Tiện nghi |
| GET | `/room-categories`, `/room-categories/{id}` | Danh mục loại phòng |
| GET | `/holidays`, `/holidays/{yyyy-MM-dd}` | Ngày lễ |

## 4. Kế toán

### Đăng nhập

Giống nhân viên tư vấn: `POST /auth/login` với `{ "employeeCode": "KT001", "password": "..." }`, `GET /auth/me`.

### Duyệt bill cọc

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/receipts` | Danh sách bill cọc đầu, lọc `status` (mặc định `Submitted` = chờ duyệt), `page`. Có mã đơn, khách, homestay, số tiền phải cọc, nội dung chuyển khoản, hạn giữ chỗ, số lần đã từ chối |
| GET | `/receipts/{paymentId}/file` | Tải ảnh/PDF bill (dùng link `fileUrl` trong danh sách, nhớ gửi kèm token) |
| POST | `/receipts/{paymentId}/approve` | Duyệt bill → đơn chuyển sang đã cọc |
| POST | `/receipts/{paymentId}/reject` | Từ chối bill, bắt buộc lý do `{ "reason": "Số tiền không khớp" }` |

Ảnh bill là file riêng tư: WinForms phải tải bằng `HttpClient` có header Bearer rồi hiện trong `PictureBox`, không mở link trực tiếp bằng trình duyệt.

### Danh mục (chỉ đọc)

Như mục 3: `/areas`, `/amenities`, `/room-categories`, `/holidays`.

Kế toán **không** dùng được API đơn, khách hàng, phòng trống (403).

## 5. Quản lý

### Đăng nhập

`POST /auth/login` với `{ "employeeCode": "QL001", "password": "..." }`, `GET /auth/me`.

### Được dùng

- **Mọi API của nhân viên tư vấn** (mục 3): đơn, đặt hộ, giao đơn, khách hàng, phòng trống.
- **Mọi API của kế toán** (mục 4): xem, duyệt, từ chối bill cọc.
- Thêm/sửa/xóa danh mục và báo cáo (dưới đây).

### Quản lý danh mục

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| POST / PUT / DELETE | `/areas`, `/areas/{id}` | Thêm/sửa/xóa khu vực `{ "name": "Hoàn Kiếm", "province": "Hà Nội", "areaGroup": "InnerCity" }` |
| POST / PUT / DELETE | `/amenities`, `/amenities/{id}` | Thêm/sửa/xóa tiện nghi `{ "name": "Wifi", "icon": "bi-wifi" }` |
| POST / PUT / DELETE | `/room-categories`, `/room-categories/{id}` | Thêm/sửa/xóa danh mục loại phòng `{ "name": "Phòng đôi" }` |
| POST | `/holidays` | Thêm ngày lễ `{ "date": "2027-01-01", "name": "Tết Dương lịch" }` |
| PUT / DELETE | `/holidays/{yyyy-MM-dd}` | Sửa tên `{ "name": "..." }` / xóa ngày lễ |

Tên trùng → 409. Xóa mục đang được dùng (ví dụ khu vực còn homestay) → 400. Xóa ngày lễ không làm đổi giá các đơn đã đặt.

### Báo cáo

Tham số chung: `from`, `to` (bắt buộc, `yyyy-MM-dd`, tính cả hai đầu, tối đa 366 ngày), `homestayId`, `areaId` (tùy chọn).

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/reports/management` | Báo cáo tổng hợp: đặt phòng, tài chính, nhân viên, homestay trong một lần gọi |
| GET | `/reports/bookings` | Số đơn theo trạng thái và kênh, tỷ lệ hủy, từ chối, hết hạn |
| GET | `/reports/financial` | Tiền phòng của đơn hoàn thành, tiền cọc giữ lại, hoa hồng, Fuji thực nhận, tiền bù đã trả |
| GET | `/reports/employees` | Hiệu suất nhân viên (số đơn tạo, tỷ lệ chốt), có `page` |
| GET | `/reports/homestays` | Hiệu suất từng homestay, có `page` |

## 6. Quản trị viên

Đăng nhập như các nhân viên khác (`AD001` trên host demo). Quyền **giống Quản lý**, trừ việc **duyệt/từ chối bill cọc** (chỉ xem được, gọi duyệt sẽ nhận 400). Quản lý tài khoản nhân viên chưa có trên API desktop, làm ở trang Admin của web.

## 7. Chủ homestay

Mọi đường dẫn bắt đầu bằng `/owner`. Chủ chỉ thấy và sửa dữ liệu **homestay của chính mình**; gọi bằng id của chủ khác sẽ nhận 404.

### Đăng nhập

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| POST | `/owner/auth/login` | Đăng nhập bằng SĐT + mật khẩu. Chỉ chủ đã được duyệt mới nhận token; khách thường, nhân viên → 401 |
| GET | `/owner/auth/me` | Thông tin chủ đang đăng nhập |

```json
// POST /api/desktop/owner/auth/login
{ "phoneNumber": "0912000001", "password": "Fuji@2026" }

// Trả về
{ "accessToken": "eyJ...", "expiresAt": "2026-10-22T11:00:00Z",
  "owner": { "id": 1, "fullName": "...", "phoneNumber": "+84912000001" } }
```

### Tổng quan và lịch làm việc

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/dashboard` | Trạng thái hồ sơ, danh sách homestay, số khách đến hôm nay, đơn chờ xác nhận, đơn quá hạn xác nhận |
| GET | `/owner/schedule` | Lịch nhận/trả phòng: `period` = `today` (mặc định), `tomorrow`, `week`; hoặc `date=yyyy-MM-dd`; lọc `homestayId` |

### Đơn

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/bookings` | Đơn đã cọc của mình, lọc `status`, `q`, `page`; đơn chờ xác nhận lên đầu |
| GET | `/owner/bookings/{id}` | Chi tiết đơn, phòng còn trống để đổi khi xác nhận, lịch sử |
| POST | `/owner/bookings/{id}/confirm` | Xác nhận đơn, có thể đổi sang phòng trống cùng loại `{ "roomId": 5 }` hoặc `{}` |
| POST | `/owner/bookings/{id}/reject` | Từ chối có lý do `{ "reason": "..." }`, Fuji hoàn 100% cọc cho khách |
| POST | `/owner/bookings/{id}/check-in` | Ghi nhận khách đã nhận phòng |
| POST | `/owner/bookings/{id}/complete` | Hoàn thành đơn |
| POST | `/owner/bookings/{id}/no-show` | Khách không đến (sau ngày nhận phòng) |

### Lịch phòng và khóa phòng

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/calendar` | Lịch tháng: `homestayId`, `month=yyyy-MM`, `roomId` (tùy chọn) |
| POST | `/owner/homestays/{homeId}/rooms/{roomId}/blocks` | Khóa phòng các đêm `[start, end)` `{ "start": "2026-12-24", "end": "2026-12-26", "reason": "Maintenance", "note": "Sửa điều hòa" }`. Trùng đơn → 409 kèm danh sách ngày trùng |
| DELETE | `/owner/homestays/{homeId}/rooms/{roomId}/blocks/{blockId}` | Mở khóa (chỉ các đêm từ hôm nay trở đi) |

### Homestay

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/homestays` | Homestay của mình, lọc `q`, `page` |
| GET | `/owner/homestays/{id}` | Toàn bộ dữ liệu để sửa: thông tin, loại phòng, phòng, giá, ảnh, tiện nghi, chính sách hủy, giấy tờ |
| GET | `/owner/homestays/areas` | Danh sách khu vực cho form vị trí |
| PUT | `/owner/homestays/{id}/content` | Sửa tên, mô tả, hướng dẫn nhận phòng, SĐT liên hệ, giờ nhận/trả `{ "name", "descriptionHtml", "checkinGuideHtml", "contactPhone", "checkinTime": "14:00:00", "checkoutTime": "12:00:00" }` |
| PUT | `/owner/homestays/{id}/location` | Sửa địa chỉ, khu vực, tọa độ `{ "address", "areaId", "latitude", "longitude" }`; có thể chuyển homestay về chờ duyệt |
| PUT | `/owner/homestays/{id}/amenities` | Chọn tiện nghi `{ "amenityIds": [1, 2, 5] }` |
| POST | `/owner/homestays/{id}/room-types` | Thêm loại phòng kèm 3 giá đầu `{ "name", "categoryId", "maxGuests", "areaM2", "description", "weekdayPrice", "weekendPrice", "holidayPrice" }` |
| PUT | `/owner/homestays/{id}/room-types/{typeId}` | Sửa loại phòng |
| PUT | `/owner/homestays/{id}/room-types/{typeId}/active` | Bật/tắt bán loại phòng `{ "active": false }`; trả danh sách đơn sắp tới cần liên hệ |
| PUT | `/owner/homestays/{id}/room-types/{typeId}/prices` | Thêm/sửa giá áp dụng từ ngày tới `{ "dayType": "Weekend", "effectiveFrom": "2026-12-01", "price": 900000 }` |
| POST | `/owner/homestays/{id}/room-types/{typeId}/rooms` | Thêm phòng `{ "code": "105" }` |
| PUT | `/owner/homestays/{id}/rooms/{roomId}` | Đổi mã phòng `{ "code": "105A" }` |
| PUT | `/owner/homestays/{id}/rooms/{roomId}/active` | Bật/tắt phòng `{ "active": false }` |
| POST | `/owner/homestays/{id}/images` | Tải ảnh lên (multipart: `file`, `typeId`, `cover`, `sortOrder`). JPG/PNG/WEBP, tối đa 5MB |
| PUT | `/owner/homestays/{id}/images/{imageId}` | Đổi loại phòng gắn ảnh và thứ tự `{ "typeId": 3, "sortOrder": 2 }` |
| POST | `/owner/homestays/{id}/images/{imageId}/cover` | Đặt làm ảnh bìa |
| DELETE | `/owner/homestays/{id}/images/{imageId}` | Xóa ảnh (không xóa được ảnh bìa duy nhất của homestay đang bán) |
| PUT | `/owner/homestays/{id}/policies` | Đặt chính sách hủy `{ "scope": "All", "name": "Linh hoạt", "rules": [{ "minDaysBefore": 7, "refundPercent": 100 }, { "minDaysBefore": 2, "refundPercent": 50 }] }` |
| DELETE | `/owner/homestays/{id}/policies/{scope}` | Gỡ chính sách riêng, quay về mặc định (`scope` = `All` hoặc `Holiday`, cũng nhận `0`/`1`) |
| POST | `/owner/homestays/{id}/documents` | Tải giấy tờ (multipart: `file`, `type`, `expiry`). Ảnh hoặc PDF, tối đa 5MB |
| DELETE | `/owner/homestays/{id}/documents/{documentId}` | Xóa giấy tờ còn ở trạng thái nháp/chờ duyệt |
| GET | `/owner/documents/{id}/file` | Tải file giấy tờ (riêng tư, gửi kèm token) |

### Đánh giá

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/reviews` | Đánh giá của khách và điểm trung bình, lọc `homestayId`, `page` |
| POST | `/owner/reviews/{id}/reply` | Trả lời đánh giá `{ "reply": "Cảm ơn anh/chị..." }` (1–1000 ký tự) |

### Đối soát

| Phương thức | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/owner/settlements` | Bảng đối soát Fuji đã gửi, lọc `status`, `page` |
| GET | `/owner/settlements/{id}` | Chi tiết: từng đơn, khoản bù, lịch sử |
| POST | `/owner/settlements/{id}/confirm` | Chủ xác nhận số liệu đúng |
| POST | `/owner/settlements/{id}/discrepancy` | Báo sai lệch `{ "note": "Thiếu đơn FJ261020-004" }` (1–500 ký tự), kế toán nhận thông báo |

## 8. Gợi ý luồng màn hình WinForms

1. **Màn đăng nhập:** chọn "Nhân viên" (mã NV) hoặc "Chủ homestay" (SĐT) → gọi đúng cửa đăng nhập → lưu `accessToken`.
2. **Mở màn hình theo vai trò:** nhân viên dựa vào `employee.position`; chủ mở thẳng màn chủ.
   - `Consultant`: Đơn, Đặt hộ, Khách hàng, Phòng trống.
   - `Accountant`: Duyệt bill.
   - `Manager`: tất cả màn nhân viên + Danh mục + Báo cáo.
   - `Administrator`: như Manager, nút duyệt bill ẩn.
3. **Mọi request:** gắn header Bearer; gặp 401 thì về màn đăng nhập; gặp lỗi khác thì hiện `detail`.

Có thể sinh sẵn code C# gọi API từ `http://<IP Mac>:5080/swagger/v1/swagger.json` bằng NSwag thay vì tự viết từng request.
