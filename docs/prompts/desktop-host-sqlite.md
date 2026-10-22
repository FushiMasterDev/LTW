# Prompt: host API desktop chạy độc lập bằng SQLite (cho WinForms trên Windows)

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh hiện tại **`feature/desktop-api-owner`**. Trên nhánh này có thay đổi **chưa commit** của đợt 2 (API chủ, đã duyệt): giữ nguyên, không sửa trừ khi bắt buộc cho việc dưới đây. Đọc trước: `README.md`, `docs/api-va-backend.md` mục 8, `HomestayBooking.Web/Program.cs`, `HomestayBooking.Web/DesktopApi/*`, `HomestayBooking.Web/Controllers/Api/Desktop/**`, `HomestayBooking.Tests/Integration/DesktopSqliteHost.cs`, `HomestayBooking.Tests/Data/SqliteTestDbContext.cs`, `HomestayBooking.Tests/Data/SeedTestServices.cs`, `HomestayBooking.Web/Data/SeedData.cs`.

## Mục tiêu

Người dùng cần chạy ứng dụng WinForms trên **máy Windows khác**, gọi một REST API chạy **ngay trên máy đó**, **không kết nối SQL Server, không cần máy Mac, không cần mạng**. Tạo project mới **`HomestayBooking.DesktopHost`** (thêm vào `HomestayBooking.sln`):

- Chỉ phục vụ nhóm `/api/desktop/...` (nhân viên + chủ, toàn bộ endpoint hiện có: 43 nhân viên + 42 chủ), cùng đường dẫn, request/response, JWT, phân quyền, ProblemDetails, rate limit login như bản trong Web. **Dùng lại controller, DTO, service, Identity thật** của `HomestayBooking.Web` qua `ProjectReference`; không chép hay viết lại nghiệp vụ, không tạo API giả.
- Không lộ trang MVC, area, `/api/...` cũ, webhook, static file của web: lọc application part/controller feature để chỉ nhận controller trong namespace `HomestayBooking.Web.Controllers.Api.Desktop` (gồm `Owner`). Route khác → 404 ProblemDetails.
- Dữ liệu: **một file SQLite** (mặc định `App_Data/fuji-desktop.db` cạnh file chạy, cấu hình được). Lần đầu chạy: tạo schema và nạp **dữ liệu mẫu đầy đủ như seed Development** (cùng tài khoản demo NV001/KT001/QL001/AD001, chủ `0912000001`…, mật khẩu `SeedData.DefaultPassword`). Lần sau dùng lại file, không seed lại. Có cách reset rõ ràng (xóa file hoặc tham số `--reset`), ghi trong README.
- SQLite khác SQL Server: tham khảo `SqliteTestDbContext` (decimal → double) và cách `DesktopSqliteHost` tạo schema. Nếu cần một DbContext SQLite dùng chung cho host và test thì đặt ở chỗ hợp lý, không để code host phụ thuộc project test. Ghi trong báo cáo mọi khác biệt hành vi so với SQL Server (độ chính xác tiền, khóa đồng thời, collation/không phân biệt hoa thường…).
- File upload/bill/giấy tờ lưu trong thư mục `App_Data` của host; nếu seed cần ảnh mẫu (`Data/SampleFiles`) thì chép sang.
- JWT: Development dùng khóa ngẫu nhiên lúc khởi động như hiện nay; cho phép cấu hình `DesktopApi:SigningKey`. Không ghi khóa thật vào repo.
- Cổng mặc định `http://localhost:5080`; cấu hình được để nghe `0.0.0.0` nếu muốn máy khác trong LAN gọi tới (ghi chú bảo mật: HTTP không mã hóa). Bật Swagger UI tại `/swagger` chỉ với nhóm desktop.
- Background job: chỉ chạy những job cần để nghiệp vụ đúng (ví dụ hết hạn giữ chỗ) nếu chúng chạy được trên SQLite; job gửi tin/đối soát ngoài phạm vi thì bỏ. Ghi rõ trong báo cáo.

## Không ảnh hưởng web

- Để dùng chung đăng ký service, được phép **chuyển nguyên khối** đăng ký service/options/Identity/JWT trong `HomestayBooking.Web/Program.cs` ra extension method (ví dụ `AddHomestayCore(...)`) và gọi lại từ cả Web lẫn host. Giữ nguyên thứ tự, giá trị, provider SQL Server và mọi middleware của Web. Không đổi hành vi bất kỳ trang/endpoint nào của Web.
- Toàn bộ test hiện có phải pass. Liệt kê trong báo cáo mọi file của `HomestayBooking.Web` đã sửa và lý do.
- Phải build và chạy được trên **Windows** (không dùng đường dẫn/lệnh riêng macOS, dùng `Path.Combine`, `AppContext.BaseDirectory`).

## Test

Thêm test cho host (project test hiện có hoặc project test mới): khởi động host với file SQLite tạm; login NV001, QL001, KT001 và chủ `0912000001`; gọi vài endpoint đọc/ghi mỗi nhóm (ví dụ tạo đơn đặt hộ, duyệt bill, khóa phòng của chủ); token chéo vai trò 403; route MVC như `/`, `/Account/Login`, `/api/homestays` → 404; khởi động lần hai không seed lại và dữ liệu đã ghi vẫn còn.

## Tài liệu

- `HomestayBooking.DesktopHost/README.md`: cài .NET 10 SDK trên Windows, `dotnet run --project HomestayBooking.DesktopHost`, publish một thư mục chạy được (`dotnet publish -c Release -r win-x64 --self-contained`), tài khoản demo, địa chỉ API, reset dữ liệu, mở cho LAN.
- Thêm mục ngắn trong README chính và `docs/api-va-backend.md` mục 8 trỏ tới host.
- Báo cáo `docs/api/desktop-host-report.md`: cấu trúc, file sửa, khác biệt SQLite, test, kết quả, điểm chưa chắc.

## Giới hạn môi trường

Sandbox không ghi được `.git`: **không commit**. SQLite không cần mạng nên hãy **chạy thật** host trong sandbox (`dotnet run`, curl vài endpoint) và chạy test liên quan; không dùng Docker, không đụng SQL Server. Không đụng `docs/bao-cao/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file.
