# Fuji Desktop API — SQLite độc lập

REST API cho WinForms chạy ngay trên Windows, không cần SQL Server, máy Mac hay kết nối mạng khi sử dụng. Host dùng controller, DTO, Identity và nghiệp vụ thật từ `HomestayBooking.Web`: **43 endpoint nhân viên + 42 endpoint chủ**, cùng đường dẫn `/api/desktop/...`, JWT, phân quyền và ProblemDetails.

## Chạy từ mã nguồn trên Windows

Hướng dẫn từng bước cho người mới, kèm test Swagger/PowerShell: [docs/api/chay-desktop-host-windows.md](../docs/api/chay-desktop-host-windows.md).

Cài **.NET 10 SDK x64, phiên bản 10.0.400 trở lên** theo `global.json`. Mở PowerShell tại thư mục chứa `HomestayBooking.sln`:

```powershell
dotnet --version
dotnet run --project HomestayBooking.DesktopHost
```

Lần build đầu cần tải các package NuGet; chuẩn bị bước này khi có mạng. Sau khi build, có thể chạy offline bằng `dotnet run --no-build --no-restore --project HomestayBooking.DesktopHost`.

- API: `http://localhost:5080/api/desktop/...`.
- Swagger UI: `http://localhost:5080/swagger`, chỉ liệt kê desktop; hai cổng login không cần Bearer.
- Lần đầu tạo schema SQLite và toàn bộ dữ liệu demo Development; các lần sau đọc lại file, **không seed lại**.
- `/`, `/Account/Login`, MVC/area, API cũ, webhook, health check và static files đều trả **404 ProblemDetails**.
- Mặc định môi trường `Development` để dùng khóa JWT ngẫu nhiên mỗi lần chạy. Dừng bằng Ctrl+C.

WinForms đặt base URL là `http://localhost:5080`. Đăng nhập nhân viên qua `POST /api/desktop/auth/login` với `employeeCode/password`, chủ qua `POST /api/desktop/owner/auth/login` với `phoneNumber/password`; gửi `Authorization: Bearer <accessToken>` ở các request tiếp theo. Không dùng cookie/antiforgery. Hợp đồng đầy đủ: [mục 8 API](../docs/api-va-backend.md#8-api-cho-ứng-dụng-desktop).

## Publish một thư mục chạy được

Từ thư mục solution, khi có mạng để tải runtime pack Windows:

```powershell
dotnet publish HomestayBooking.DesktopHost -c Release -r win-x64 --self-contained -o artifacts/desktop-win-x64
```

Hoặc dùng `dotnet publish -c Release -r win-x64 --self-contained` ngay trong thư mục project. Chép **toàn bộ thư mục publish** sang máy Windows x64, đặt vào thư mục người dùng có quyền ghi, rồi chạy:

```powershell
.\HomestayBooking.DesktopHost.exe
```

Máy nhận bản self-contained không cần SDK/runtime .NET, SQL Server hay mạng. Giữ cùng executable các DLL (gồm `e_sqlite3.dll`), `desktopsettings.json`, `Data/SampleFiles` và `App_Data/public/images/demo`. Không chép dữ liệu khách thật vào bản demo để phát cho người khác.

## Tài khoản demo

Mật khẩu chung **`Fuji@2026`**, đúng `SeedData.DefaultPassword`:

| Đăng nhập | Vai trò |
|---|---|
| `NV001` | Tư vấn |
| `KT001` | Kế toán |
| `QL001` | Quản lý |
| `AD001` | Quản trị |
| `0912000001` | Chủ homestay đã duyệt (login bằng SĐT) |

Seed còn có các tài khoản chủ/khách giống Web Development; khách không đăng nhập được API desktop. Bộ dữ liệu mẫu gồm danh mục, nhân sự, 35 homestay, phòng/giá/hợp đồng, 60 đơn mẫu, bill, đánh giá, giấy tờ, yêu cầu đổi, khóa phòng và dữ liệu vận hành/đối soát.

## Dữ liệu, cấu hình và reset

Đường dẫn mặc định tính từ **thư mục chứa executable/DLL**, không từ thư mục PowerShell:

- SQLite: `App_Data/fuji-desktop.db` (có thể xuất hiện thêm `-wal`, `-shm` khi đang chạy).
- Bill/giấy tờ riêng tư: `App_Data/receipts`, chỉ đọc qua endpoint kiểm quyền.
- Ảnh upload: `App_Data/public/uploads/homestays`; ảnh mẫu: `App_Data/public/images/demo`.
- Khóa Data Protection: `App_Data/keys`. Khóa JWT Development nằm trong bộ nhớ, không lưu trong thư mục này.

Cấu hình trong `desktopsettings.json`, file `desktopsettings.<Environment>.json`, biến môi trường hoặc tham số lệnh (tham số lệnh ưu tiên cao nhất). Khởi động lại sau khi đổi cấu hình. Host không dùng cấu hình SQL Server/user-secrets của Web.

```powershell
# Đổi file SQLite (đường dẫn tuyệt đối hoặc tương đối với thư mục chạy)
dotnet run --project HomestayBooking.DesktopHost -- --DesktopHost:DatabasePath "D:\FujiData\fuji.db"

# Đổi cổng
dotnet run --project HomestayBooking.DesktopHost -- --urls "http://localhost:5090"
```

Tương đương biến môi trường: `DesktopHost__DatabasePath`, `ASPNETCORE_URLS` hoặc `DOTNET_URLS`. `--contentRoot` có thể chọn thư mục runtime khác, khi đó cần chép `Data/SampleFiles` và `App_Data/public/images/demo` vào thư mục đó; mọi đường dẫn tương đối và file upload cũng theo thư mục này.

**Reset xóa mọi dữ liệu nghiệp vụ trong file SQLite đã chọn.** Dừng host và sao lưu trước, rồi chạy một lần:

```powershell
dotnet run --project HomestayBooking.DesktopHost -- --reset
# Hoặc trong thư mục publish:
.\HomestayBooking.DesktopHost.exe --reset
```

Sau reset bỏ `--reset` ở lần chạy tiếp theo. Cách khác: khi host đã dừng, xóa file SQLite đã chọn cùng các file `-wal`, `-shm` tương ứng rồi chạy lại. `--reset` chỉ tạo lại database, giữ file upload/keys; muốn dọn cả file cũ, sao lưu rồi xóa thư mục upload/receipts riêng. Sao lưu đầy đủ cần cả database và file trong `App_Data`; nên sao lưu khi host đã dừng. Không mở hai host cùng một file hoặc reset khi host khác đang dùng file.

Schema host dùng `EnsureCreated` và marker `__DesktopHost` trong chính file SQLite; không chạy migration SQL Server. Phiên bản này chưa có migration nâng cấp SQLite từ phiên bản model tương lai: sao lưu và lập kế hoạch chuyển dữ liệu trước khi nâng cấp.

## JWT và mở cho LAN

Development thiếu `DesktopApi:SigningKey` dùng khóa ngẫu nhiên; restart buộc đăng nhập lại. Để giữ khóa qua restart, đặt khóa bí mật tối thiểu 32 byte UTF-8 qua `DesktopApi__SigningKey` hoặc cấu hình riêng ngoài repo. Có thể sinh khóa trong PowerShell mà không in ra màn hình:

```powershell
$keyBytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
$env:DesktopApi__SigningKey = [Convert]::ToBase64String($keyBytes)
$rng.Dispose()
.\HomestayBooking.DesktopHost.exe
```

Đoạn trên chỉ giữ khóa cho phiên PowerShell hiện tại; muốn bền qua phiên đăng nhập cần lưu bằng cơ chế quản lý bí mật của máy. Không ghi khóa thật vào repo. `DOTNET_ENVIRONMENT=Production` yêu cầu khóa hợp lệ; thiếu/sai khóa thì login trả 503 giống API trong Web. Token mặc định 60 phút, giới hạn login chung hai nhóm mặc định 10 lần/IP/phút; cấu hình qua `DesktopApi__TokenMinutes`, `DesktopApi__LoginPermitLimit`, `DesktopApi__LoginWindowSeconds`.

Mặc định chỉ nghe loopback. Khi cần máy khác trong LAN gọi:

```powershell
dotnet run --project HomestayBooking.DesktopHost -- --urls "http://0.0.0.0:5080"
# Hoặc bản publish:
.\HomestayBooking.DesktopHost.exe --urls "http://0.0.0.0:5080"
```

Máy khác dùng `http://<IPv4-máy-chạy-host>:5080`. Cho phép cổng tương ứng trong Windows Firewall trên mạng riêng, giới hạn máy được truy cập. **HTTP không mã hóa mật khẩu, token hoặc dữ liệu; chỉ mở trên LAN tin cậy**, không chuyển tiếp cổng ra Internet. Swagger vẫn mở trên địa chỉ đang nghe.

## Phạm vi offline và SQLite

Job mỗi phút chỉ hết hạn giữ chỗ/yêu cầu đổi/voucher/hợp đồng và cảnh báo nội bộ về xác nhận, bill, hồ sơ chủ. Không gửi tin nhắc khách/chủ, không tự lập đối soát, không xử lý khoản bù treo, không webhook hay gọi dịch vụ ngân hàng. Các API đọc/xác nhận đối soát mẫu vẫn dùng nghiệp vụ thật.

Host cố ý không phục vụ static files. DTO ảnh giữ URL như Web, nhưng các URL `/images/...`, `/uploads/...` không tải qua host này; WinForms cần ảnh thay thế hoặc cơ chế hiển thị riêng. Bill và giấy tờ có endpoint desktop kiểm quyền và hoạt động offline. Không có bước thanh toán ngân hàng thật khi chạy offline.

SQLite lưu decimal bằng REAL/double: phù hợp bản demo nhưng có sai số nhị phân khi tính tổng/so sánh tiền. SQLite khóa ghi toàn database và phân biệt hoa thường khác collation SQL Server; không dùng làm hệ thống tài chính nhiều người dùng mà chưa đánh giá thêm. Chi tiết và kết quả kiểm tra: [báo cáo host](../docs/api/desktop-host-report.md).
