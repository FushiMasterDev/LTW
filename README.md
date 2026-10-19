# Fuji Homestay — HomestayBooking

Hệ thống đặt phòng homestay cho Công ty Fuji (Hà Nội), bài tập lớn môn Lập trình Web. Ứng dụng ASP.NET Core MVC (.NET 10) có 4 lớp người dùng: khách, chủ homestay, nhân viên (tư vấn, kế toán, quản lý, quản trị) và hệ thống.

- **Khách:** tìm homestay theo khu vực, ngày, số khách, giá, tiện nghi; xem lịch trống và báo giá; đặt phòng không cần tài khoản; cọc 30% bằng QR; tra cứu, đổi/hủy đơn; viết đánh giá; đăng ký làm đối tác.
- **Chủ homestay:** sửa homestay (thông tin, phòng và giá 3 mức, ảnh, tiện nghi, chính sách hủy, giấy tờ), khóa/mở ngày, lịch nhận–trả, xác nhận đơn, đối soát, trả lời đánh giá.
- **Nhân viên:** quản lý và giao đơn, đặt hộ khách, phòng trống toàn hệ thống, xử lý trùng phòng, thẩm định hồ sơ đối tác.
- **Kế toán:** khớp giao dịch ngân hàng, duyệt bill, hoàn tiền, đối soát chủ homestay, khoản bù.
- **Quản lý / quản trị:** hợp đồng đối tác, duyệt nội dung homestay, báo cáo có biểu đồ, tài khoản nhân viên, danh mục.

Tài liệu chi tiết: [`QUY_TRINH_BTL.md`](QUY_TRINH_BTL.md) (yêu cầu, nghiệp vụ, API), [`fuji_homestay_database_v1.md`](fuji_homestay_database_v1.md) (thiết kế CSDL), [`DESIGN.md`](DESIGN.md) (hệ thiết kế giao diện), [`database/README.md`](database/README.md) (dựng và kiểm tra CSDL chi tiết), [`docs/ui/qa-report.md`](docs/ui/qa-report.md) (báo cáo kiểm thử giao diện).

## Công nghệ

| Phần | Dùng |
|---|---|
| Backend | ASP.NET Core MVC + Web API, .NET 10, EF Core 10 (SQL Server), ASP.NET Core Identity, BackgroundService cho job nền |
| Giao diện | Razor + Tag Helper + View Component, Bootstrap 5.3, Bootstrap Icons, jQuery Validation Unobtrusive, Flatpickr, Chart.js, TinyMCE 6 (tự host trong `wwwroot/lib`) |
| Bảo mật | Identity (mật khẩu băm), phân quyền theo vai trò/chức vụ + kiểm sở hữu ở service, antiforgery, HtmlSanitizer cho nội dung HTML |
| Đa ngôn ngữ | Tiếng Việt (mặc định) / English qua `SharedResource.en.resx` và cookie ngôn ngữ |
| Kiểm thử | xUnit (service + tích hợp trên pipeline thật với `WebApplicationFactory`), script SQL kiểm ràng buộc |

## 1. Yêu cầu môi trường

- **.NET SDK 10.0.400** trở lên (`global.json`). Kiểm tra: `dotnet --version`.
- **SQL Server 2022** (LocalDB/Express trên Windows, hoặc Docker trên macOS/Linux).
- **sqlcmd** (`mssql-tools18`) hoặc SSMS bật *SQLCMD Mode*, nếu dựng CSDL bằng script.
- Tùy chọn: `dotnet-ef` 10.0.12 (`dotnet tool install --global dotnet-ef --version 10.0.12`) nếu muốn dựng CSDL bằng migration thay vì script.

SQL Server bằng Docker (macOS/Linux):

```bash
docker run -d --name sql-homestay -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<mat-khau-manh>' \
  -v sql-homestay-data:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
```

## 2. Dựng cơ sở dữ liệu

Chọn **một** trong hai cách. Cả hai cho cùng schema (45 bảng, 9 migration).

### Cách A — chạy script SQL (khuyến nghị khi nộp bài / máy mới)

```bash
read -rs SQLCMDPASSWORD && export SQLCMDPASSWORD          # nhập mật khẩu sa, không lưu vào file
sqlcmd -S localhost,1433 -U sa -C -Q "CREATE DATABASE [HomestayBooking]"
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking -v TargetDb=HomestayBooking -i deploy.sql
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking -i verify.sql   # mọi dòng phải là OK
cd ..
```

- `-d` và `-v TargetDb` phải cùng tên; script dừng nếu chọn nhầm database hệ thống hoặc database có bảng lạ.
- SQL Server trong Docker mà máy không có sqlcmd: xem `database/README.md` mục 3 (chạy sqlcmd bên trong container).
- Windows + LocalDB: thay `-S localhost,1433 -U sa -C` bằng `-S "(localdb)\MSSQLLocalDB" -E`.

### Cách B — áp migration bằng EF Core

```bash
dotnet ef database update --configuration Release --project HomestayBooking.Web \
  --connection "Server=localhost,1433;Database=HomestayBooking;User Id=sa;Password=<mat-khau>;TrustServerCertificate=True"
```

### Dữ liệu mẫu

`schema.sql` chỉ có schema. **Dữ liệu mẫu do ứng dụng tự seed khi khởi động ở môi trường Development** (`Data/SeedData*.cs`), ngày của các đơn mẫu tính theo ngày chạy nên luôn có đơn sắp tới/đang ở/đã qua. Seed chạy lại không nhân đôi dữ liệu. Ở Production chỉ seed danh mục nền (vai trò, khu vực, loại phòng, tiện nghi, ngày lễ, chính sách hủy), không tạo tài khoản demo.

Sau lần chạy đầu ở Development, database có 35 homestay, 128 phòng, khoảng 60 đơn, 20 tài khoản, đánh giá, đối soát và voucher mẫu. Kiểm tra dữ liệu (chỉ đọc): `sqlcmd ... -d HomestayBooking -i database/tests/data_consistency.sql` (35 mục, mã thoát 0 là đạt).

Đã thử trọn quy trình trên một database mới tinh ngày 20/10/2026 (SQL Server 2022 trong Docker): `deploy.sql` → `verify.sql` toàn OK (45 bảng, 9 migration) → chạy app Development tự seed → `data_consistency.sql` 35/35 OK → `tools/ui-crawl.py` 840 trang, không có lỗi 500.

## 3. Cấu hình chuỗi kết nối

`appsettings.json` chỉ có chuỗi mẫu với mật khẩu giả. **Không ghi mật khẩu thật vào repo.** Chọn một cách:

```bash
# Biến môi trường (ưu tiên cao nhất)
export ConnectionStrings__Default="Server=localhost,1433;Database=HomestayBooking;User Id=sa;Password=<mat-khau>;TrustServerCertificate=True"

# hoặc user-secrets (lưu ngoài repo, chỉ máy phát triển)
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối>" --project HomestayBooking.Web
```

Windows + LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=HomestayBooking;Trusted_Connection=True;TrustServerCertificate=True`.

`Database:AutoMigrate` mặc định `false`: ứng dụng dừng nếu database còn migration chưa áp (để không tự sửa nhầm database). Đặt `Database__AutoMigrate=true` nếu muốn ứng dụng tự áp migration khi khởi động.

## 4. Chạy ứng dụng

```bash
dotnet restore
ASPNETCORE_ENVIRONMENT=Development dotnet run --project HomestayBooking.Web
```

Mở http://localhost:5011 (profile `https`: https://localhost:7286).

| Địa chỉ | Nội dung |
|---|---|
| `/` | Trang khách |
| `/Account/Login` | Đăng nhập khách / chủ homestay bằng số điện thoại |
| `/Account/EmployeeLogin` | Đăng nhập nhân viên bằng mã nhân viên |
| `/swagger` | Swagger UI của Web API (chỉ Development) |
| `/Dev/UiKit` | Thư viện component giao diện (Development, đăng nhập `QL001`) |
| `/Dev/SimulateBank` | Giả lập tiền vào tài khoản ngân hàng để thử khớp giao dịch (Development) |

### Tài khoản mẫu (chỉ Development)

Mật khẩu chung: **`Fuji@2026`**

| Vai trò | Đăng nhập | Trang |
|---|---|---|
| Khách | SĐT `0987000001` | `/Account/Login` |
| Chủ homestay | SĐT `0912000001` | `/Account/Login` → `/Owner` |
| Nhân viên tư vấn | `NV001`, `NV002` | `/Account/EmployeeLogin` → `/Staff` |
| Kế toán | `KT001` | `/Account/EmployeeLogin` → `/Accounting` |
| Quản lý | `QL001` | `/Account/EmployeeLogin` → `/Admin` |
| Quản trị viên | `AD001` | `/Account/EmployeeLogin` → `/Admin` |

## 5. Web API

Toàn bộ có trên `/swagger`. Trả JSON; lỗi trả `ProblemDetails` (400/404).

| Method | Endpoint | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/homestays?areaId=&checkIn=&checkOut=&guests=&minPrice=&maxPrice=&categoryId=&amenityIds=&sort=&page=` | Tìm kiếm, lọc, phân trang | Công khai |
| GET | `/api/rooms/{roomTypeId}/availability?from=&to=` | Các đêm đã kín (tối đa 92 đêm) | Công khai |
| GET | `/api/rooms/{roomTypeId}/quote?checkIn=&checkOut=` | Báo giá từng đêm, tổng tiền, cọc, số phòng còn trống | Công khai |
| GET | `/api/bookings/{code}?phone=` | Tra cứu đơn bằng mã + SĐT | Công khai |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc | Đã đăng nhập |
| POST | `/api/bank-webhook/sepay` | Nhận giao dịch tiền vào từ SePay (khóa API) | Máy chủ ngân hàng |

Các thao tác ghi (đặt phòng, xác nhận/từ chối, hủy, khóa ngày) đi qua form MVC có antiforgery.

## 6. Kiểm thử

```bash
dotnet build HomestayBooking.sln -c Release
dotnet test HomestayBooking.sln -c Release --no-build
```

Hầu hết test dùng CSDL InMemory/SQLite riêng, không cần SQL Server. Riêng hai lớp chạy trên SQL Server thật, database **`HomestayBooking_ConstraintTest` đã dựng schema và còn rỗng**, đọc chuỗi kết nối từ user-secrets của `HomestayBooking.Web` (mục 3; test tự đổi tên database, không chạm database chính):

- `ReportServiceSqlServerTests` (luôn chạy; thiếu database hoặc user-secrets thì test này báo lỗi).
- `AvailabilityPerformanceTests` (bỏ qua mặc định, bật bằng `RUN_PERF_TESTS=1`).

Dựng database cho hai test này một lần (như mục 2, không chạy app vào nó để không bị seed):

```bash
sqlcmd -S localhost,1433 -U sa -C -Q "CREATE DATABASE [HomestayBooking_ConstraintTest]"
cd database && sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_ConstraintTest -v TargetDb=HomestayBooking_ConstraintTest -i deploy.sql && cd ..
```

Kết quả gần nhất (20/10/2026): 1724 pass, 1 skip.

Kiểm tra liên kết mọi trang theo từng vai trò (khi app đang chạy): `python3 -I tools/ui-crawl.py http://localhost:5011`.

## 7. Cấu trúc thư mục

```
HomestayBooking.sln
HomestayBooking.Web/
  Controllers/            trang khách, tài khoản, đối tác; Controllers/Api: Web API
  Areas/Admin|Staff|Owner|Accounting/   màn nội bộ theo vai trò
  Services/               nghiệp vụ (đặt phòng, giá, phòng trống, đối soát, báo cáo…)
  Data/                   ApplicationDbContext, Migrations, SeedData
  Models/Entities, Models/ViewModels
  Views/, TagHelpers/, ViewComponents/   giao diện và component dùng chung
  Resources/              SharedResource.en.resx (bản dịch tiếng Anh)
  BackgroundJobs/         BookingTimeoutJob: hết hạn giữ phòng, cảnh báo 30 phút, nhắc lịch, lập đối soát định kỳ
  wwwroot/                css (tokens.css là nguồn màu), js, lib, images
HomestayBooking.Tests/    xUnit: Services/ và Integration/
database/                 schema.sql, deploy.sql, verify.sql, tests/*.sql
docs/                     tài liệu thiết kế, prompt và báo cáo từng bước giao diện
tools/ui-crawl.py         duyệt mọi trang theo vai trò, báo trang lỗi
```

## 8. Ghi chú

- Ảnh homestay mẫu nằm ở `wwwroot/images/demo`, ảnh trang trí ở `wwwroot/images/ai` (nguồn ghi ở `docs/ui/credits.md`). Ảnh người dùng tải lên lưu ở `wwwroot/uploads/` (không đưa vào Git).
- Thanh toán là chuyển khoản QR VietQR; khớp tự động qua webhook SePay hoặc kế toán duyệt bill. Ở Development dùng `/Dev/SimulateBank` để giả lập.
- Chưa làm: gửi tin Zalo/SMS (FR-04, FR-23, lời mời đối tác), cổng thanh toán, đồng bộ OTA.
