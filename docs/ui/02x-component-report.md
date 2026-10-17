# Rà thư viện component bước 02x

Ngày: 17/10/2026. Phạm vi theo `docs/prompts/ui/02x-ra-skill.md`; dừng trước bước 03.

## Sửa review bước 02

| Chỗ sửa | Lỗi | Cách sửa |
|---|---|---|
| `wwwroot/js/site.js`, `Views/Dev/UiKit.cshtml` | Chỉ bắt form có data-confirm, bỏ qua nút đổi lịch | Bắt mọi form; message/title/variant ưu tiên submitter rồi form; bỏ qua khi không có message; giữ requestSubmit(submitter). Thêm mẫu nút vào UI Kit |
| `TagHelpers/RatingTagHelper.cs`, `Tests/Ui/DataComponentTests.cs` | Nhãn của span không có role bị trình đọc bỏ qua | Khi có điểm thêm role=img; assert trong test rating hiện có |
| `Views/Shared/_Timeline.cshtml` | Directive nằm sau markup; badge viết cứng | Đưa using/model lên đầu; dùng status-badge; giữ hook và chuỗi tích hợp |
| `Views/Shared/_PaymentQr.cshtml` | Chỉ hiện MB | Hiện MB Bank (Ngân hàng Quân đội), dùng khóa dịch Ngân hàng Quân đội có sẵn |
| `Views/Bookings/Details.cshtml` | Wrapper/dòng trống sau khi chuyển countdown | Gộp thành tiêu đề h2 có margin dưới |
| `Views/Shared/_Gallery.cshtml` | Thiếu hướng dẫn khi rỗng | Truyền lại Description đầy đủ với hotline 1900 0000; dùng khóa dịch cũ |
| `Views/Shared/_DataTable.cshtml` | Sort thiếu tiền tố ứng dụng | Dùng Request.PathBase + Request.Path |

Kiểm chứng trước commit fix: `dotnet test HomestayBooking.sln --configuration Release --no-restore -p:UseSharedCompilation=false`: **1589 pass, 1 skip cũ, 0 fail** (1590 tổng); TRX `/private/tmp/LTW-ui02x/review.trx`. Lượt đầu trong sandbox lỗi named pipe; lượt chạy lại ngoài sandbox exit 0. `node --check site.js`, `git diff --check` đạt.

Playwright trên Release cổng 5012 / DeployTest / AutoMigrate=false: nút submit mở modal; Esc/Hủy không điều hướng; Đồng ý tới `?confirmed=button` đúng name/value; không có pageerror. Không thêm bản dịch, không sửa bank options/appsettings.
