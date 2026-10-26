# Chạy và test API desktop (SQLite) trên Windows từ mã nguồn

Hướng dẫn từng bước để chạy [`HomestayBooking.DesktopHost`](../../HomestayBooking.DesktopHost/README.md) trên máy Windows: một REST API dùng SQLite, không cần SQL Server, máy Mac hay mạng (trừ lần build đầu). Cách publish bản self-contained, cấu hình JWT và mở cho LAN xem ở README của host.

## 1. Cài .NET 10 SDK

1. Vào https://dotnet.microsoft.com/download/dotnet/10.0.
2. Ở mục **SDK**, tải bản **Windows x64 Installer**, phiên bản **10.0.400 trở lên**. `global.json` yêu cầu tối thiểu 10.0.400; bản 10.0.4xx hoặc 10.0.5xx mới hơn đều chạy được.
3. Cài xong thì **đóng hết PowerShell đang mở**, mở PowerShell mới rồi kiểm tra:
   ```powershell
   dotnet --version
   ```
   Kết quả phải là `10.0.4xx` hoặc cao hơn. Nếu báo không có lệnh `dotnet`, khởi động lại máy.

Không cần cài SQL Server, SQLite hay Visual Studio. SQLite đi kèm package NuGet `Microsoft.EntityFrameworkCore.Sqlite` (có sẵn `e_sqlite3.dll`).

## 2. Lấy mã nguồn

Có Git:

```powershell
cd D:\
git clone https://github.com/FushiMasterDev/LTW.git
cd LTW
```

Không có Git: mở https://github.com/FushiMasterDev/LTW, bấm **Code → Download ZIP**, giải nén ra `D:\LTW`, rồi chạy `cd D:\LTW`.

Chạy `dir` để kiểm tra: phải thấy `HomestayBooking.sln`, `HomestayBooking.DesktopHost` và `HomestayBooking.Web`. Nên đặt repo ở `D:\` hoặc trong thư mục người dùng, tránh `C:\Program Files` vì host cần quyền ghi file database.

## 3. Build lần đầu (cần mạng)

```powershell
dotnet build HomestayBooking.DesktopHost
```

Lần đầu sẽ tải package NuGet, mất khoảng 1–3 phút. Build xong sẽ báo `Build succeeded`. Dòng warning bỏ qua được, chỉ dòng `error` mới cần xử lý.

## 4. Chạy host

```powershell
dotnet run --project HomestayBooking.DesktopHost
```

Chạy thành công sẽ thấy dòng:

```
Now listening on: http://localhost:5080
```

- Lần đầu host tự tạo database SQLite và nạp dữ liệu mẫu (35 homestay, 60 đơn…), có thể mất vài giây. Các lần sau dùng lại file đó, không seed lại.
- File database: `HomestayBooking.DesktopHost\bin\Debug\net10.0\App_Data\fuji-desktop.db`.
- **Giữ cửa sổ PowerShell này mở.** Đóng cửa sổ là API dừng. Muốn dừng thì bấm `Ctrl+C`.
- Nếu Windows Firewall hỏi quyền, chọn **Private network**.

Muốn chạy không cần mạng thì từ lần sau dùng:

```powershell
dotnet run --no-build --no-restore --project HomestayBooking.DesktopHost
```

## 5. Test bằng Swagger

1. Mở trình duyệt vào **http://localhost:5080/swagger**.
2. Tìm `POST /api/desktop/auth/login`, bấm **Try it out**, nhập đoạn dưới rồi bấm **Execute**:
   ```json
   { "employeeCode": "NV001", "password": "Fuji@2026" }
   ```
3. Copy giá trị `accessToken` trong response (chuỗi dài bắt đầu bằng `eyJ`).
4. Bấm **Authorize** ở góc trên, dán token, rồi bấm **Authorize → Close**.
5. Gọi thử `GET /api/desktop/auth/me` hoặc `GET /api/desktop/bookings`. Trả `200` là chạy đúng.

Tài khoản demo, mật khẩu chung `Fuji@2026`:

| Đăng nhập | Endpoint login | Vai trò |
|---|---|---|
| `NV001`, `NV002` | `POST /api/desktop/auth/login` (`employeeCode`) | Tư vấn |
| `KT001` | như trên | Kế toán |
| `QL001` | như trên | Quản lý |
| `AD001` | như trên | Quản trị |
| `0912000001`, `0912000002` | `POST /api/desktop/owner/auth/login` (`phoneNumber`) | Chủ homestay |

Token hết hạn sau 60 phút. Ở môi trường Development, mỗi lần restart host cũng phải đăng nhập lại.

## 6. Test bằng PowerShell

Mở **một PowerShell khác** và để cửa sổ host vẫn chạy:

```powershell
$login = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/desktop/auth/login `
  -ContentType "application/json" -Body '{"employeeCode":"NV001","password":"Fuji@2026"}'
$h = @{ Authorization = "Bearer $($login.accessToken)" }

Invoke-RestMethod -Uri http://localhost:5080/api/desktop/auth/me -Headers $h
Invoke-RestMethod -Uri "http://localhost:5080/api/desktop/bookings?page=1" -Headers $h
```

Chủ homestay đăng nhập bằng `POST /api/desktop/owner/auth/login` với body `{"phoneNumber":"0912000001","password":"Fuji@2026"}`.

## 7. Nối với WinForms

- Base URL: `http://localhost:5080`. Nên để trong file cấu hình, không hard-code.
- Gọi login để lấy `accessToken`, rồi gắn header `Authorization: Bearer <token>` vào mọi request sau đó.
- Không cần cookie hay antiforgery. JSON dùng camelCase. Lỗi trả về dạng ProblemDetails.
- WinForms chỉ gọi API qua HTTP, không kết nối thẳng file SQLite.

## 8. Host SQLite và REST API của Web

DesktopHost **chính là một REST API**: 85 endpoint dưới `/api/desktop/...` (43 cho nhân viên, 42 cho chủ homestay), xác thực bằng JWT Bearer. Danh sách endpoint theo vai trò ở [desktop-api-theo-vai-tro.md](desktop-api-theo-vai-tro.md); mã lỗi và ví dụ curl ở [api-va-backend.md mục 8](../api-va-backend.md#8-api-cho-ứng-dụng-desktop).

Web còn **10 endpoint REST** riêng ([api-va-backend.md mục 3](../api-va-backend.md#3-danh-sách-10-endpoint-web-api)): `/api/homestays`, `/api/rooms/...` (lịch trống, báo giá, khóa lịch), `/api/bookings/...` (tra cứu đơn, chủ xác nhận/từ chối), `/api/notifications/unread-count` và webhook `/api/bank-webhook/sepay`. Nhóm này **không chạy trên DesktopHost**: gọi vào host sẽ nhận `404`. Chúng chỉ có khi chạy `HomestayBooking.Web` với SQL Server, đăng nhập bằng cookie qua form web, và phục vụ giao diện web chứ không dành cho WinForms.

| | API desktop trên DesktopHost | API desktop trên Web | 10 REST API của Web |
|---|---|---|---|
| Đường dẫn | `/api/desktop/...` | `/api/desktop/...` | `/api/...` (không có `desktop`) |
| Database | SQLite, dữ liệu demo riêng | SQL Server, dữ liệu thật | SQL Server |
| Xác thực | JWT Bearer | JWT Bearer | Cookie Identity (webhook dùng khóa riêng) |
| Swagger | Có (`/swagger`) | Production không có | Development có |
| Dùng cho | Làm và test WinForms offline | WinForms dùng dữ liệu thật | Giao diện web |

Muốn WinForms dùng chung dữ liệu với web, chỉ cần đổi base URL sang địa chỉ Web. Đường dẫn và DTO giữ nguyên, nhưng ở đó không có tài khoản demo.

## 9. Reset dữ liệu

Bước này **xóa toàn bộ dữ liệu trong file SQLite** rồi nạp lại dữ liệu mẫu. Dừng host bằng `Ctrl+C`, rồi chạy:

```powershell
dotnet run --project HomestayBooking.DesktopHost -- --reset
```

Lần chạy sau bỏ `--reset` đi. Reset không xóa file upload hay key trong `App_Data`.

## 10. Lỗi hay gặp

| Triệu chứng | Cách xử lý |
|---|---|
| `A compatible .NET SDK was not found` | SDK cũ hơn 10.0.400. Cài bản mới hơn (mục 1) |
| `Failed to bind to address ... 5080` | Cổng 5080 đang bị chiếm (có thể host cũ chưa tắt). Tắt nó, hoặc thêm `-- --urls "http://localhost:5090"` vào cuối lệnh chạy |
| Login trả `401` | Sai mã hoặc mật khẩu. Mật khẩu có chữ F hoa: `Fuji@2026` |
| Endpoint trả `401` sau khi đã login | Chưa bấm Authorize, token hết hạn, hoặc host vừa restart. Đăng nhập lại |
| Endpoint trả `403` | Vai trò không có quyền (ví dụ NV001 gọi API kế toán). Đổi sang tài khoản đúng vai trò |
| Gọi `/api/homestays` hoặc `/api/bookings/...` trả `404` | Đó là REST API của Web, không có trên host (mục 8). Dùng `/api/desktop/...` |
| Ảnh homestay không hiện | Bình thường. Host không phục vụ ảnh `/images/...`, `/uploads/...` |
| `SQLite schema không được hỗ trợ` khi khởi động | File database tạo từ phiên bản khác. Sao lưu `App_Data`, rồi chạy `--reset` (mục 9) |
