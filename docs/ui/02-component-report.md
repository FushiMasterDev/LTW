# Bàn giao bước 02: thư viện component

Ngày kiểm tra: 17/10/2026. Phạm vi: nhóm A → D của `docs/prompts/ui/02-component.md`; dừng trước bước 03.

## Component

Mỗi dòng dưới đây có mục dùng thật trong `/Dev/UiKit`. Mẫu công cụ phát triển giữ mô tả tiếng Việt theo quyết định 01; nhãn tương tác của component có VI/EN.

| Nhóm | Component | Kết quả và nơi dùng |
|---|---|---|
| A | rating | Làm tròn 1 số, không có đánh giá, aria-label; thay sao ở Home |
| A | StatCard | Số lớn/chữ dài, link tùy chọn; dashboard dùng chung |
| A | DataTable | Link sort giữ query/lặp bộ lọc, đảo chiều; mobile dạng thẻ có data-label |
| A | DescriptionList | Nhãn/giá trị HTML có kiểu rõ, nội dung dài được xuống dòng |
| A | Timeline | Lịch sử trạng thái và yêu cầu đổi; giữ article/data-change-request/data-status |
| A | PriceBreakdown | Đêm, cọc, giảm giá, còn lại, đổi/hủy, chính sách; giữ data-quote-total |
| A | EmptyState | Rỗng/lỗi/hành động; tương thích lời gọi không truyền model |
| A | Skeleton | Dòng/thẻ; reduced-motion không nhấp nháy |
| B | FilterBar | GET giữ query, xóa lọc; mobile offcanvas |
| B | DateRangePicker | Flatpickr vi/en, ngày VN, hai input date thật, checkout không tính đêm |
| B | GuestPicker | Input Guests thật, nút tăng/giảm, biên và disabled |
| B | UploadZone | Nhiều file/ảnh/PDF, preview, kéo thả, 1 byte–5 MB; server vẫn kiểm chính |
| B | Hộp xác nhận | Bootstrap modal qua data-confirm; Esc/Hủy không gửi form |
| B | Alerts | Giữ Success/Error, thêm Info; dùng Alert kiểu rõ |
| B | Toast | window.fxToast, aria-live, textContent; dùng cho phản hồi AJAX/background |
| C | Tabs | Liên kết trang có aria-current; Cọc bổ sung chỉ hiện Kế toán |
| C | Breadcrumb | Dùng trong DashboardLayout |
| C | PageHead | Bỏ tô màu theo module; action/route nhận từ model, lời gọi cũ còn dùng được |
| C | NotificationBell | Số chưa đọc và 5 tin mới nhất, dropdown dùng bàn phím |
| C | Trang Notifications | Phân trang, POST antiforgery, đúng chủ; URL ngoài không được redirect |
| D | SearchBar | Hero/compact, query đúng tên; mobile xếp dọc; mẫu UI Kit, gắn trang ở 03 |
| D | HomestayCard | Ảnh 3:2/fallback, tên tối đa 2 dòng, giá/đêm, rating; Home và Homestays |
| D | Gallery | Desktop grid, mobile dải vuốt, lightbox; giữ hook cũ và trả focus khi đóng |
| D | BookingPanel | Sticky, DateRangePicker/GuestPicker/PriceBreakdown; chỉ UI Kit theo prompt |
| D | BookingStepper | Chỉ đọc, aria-current; hiển thị mọi trạng thái và nhánh kết thúc |
| D | PaymentQr | VietQrService, nền trắng cả dark, clipboard, fallback và timer dùng chung |
| D | RoomCalendar | C# dựng tuần bắt đầu thứ Hai, chọn ô/đổi tháng bằng bàn phím; không ghi dữ liệu |

## Partial được thay

- `_ChangeHistory` → `_Timeline`: sửa tất cả lời gọi, xóa file cũ.
- `_ChangeQuote` và `_PolicyRules` → `_PriceBreakdown`: sửa tất cả lời gọi, xóa file cũ.
- `_AccountingTabs` → `_Tabs`: thay ở sáu trang Accounting, xóa file cũ.
- Thẻ homestay, gallery, QR được trích khỏi markup lặp ở trang thật. CSS cũ của các mẫu này đã gỡ.

Các test tích hợp hiện có tiếp tục kiểm hook lịch sử/báo giá/cọc bổ sung; không đổi tên trường form hay luồng nghiệp vụ.

## Build và test

Tất cả dùng Release, không ghi đè bin/Debug.

| Mốc | Pass | Skip cũ | Fail | Mã thoát build/test |
|---|---:|---:|---:|---|
| Trước bước 02, 5e4de8e | 1567 | 1 | 0 | 0 / 0 |
| A, 0cde961 | 1573 | 1 | 0 | 0 / 0 |
| B, 6f6db35 | 1573 | 1 | 0 | 0 / 0 |
| C, 109bdcf | 1586 | 1 | 0 | 0 / 0 |
| D, feat(ui): component đặt phòng | 1589 | 1 | 0 | 0 / 0 |

Lệnh: `dotnet build --configuration Release --no-restore -p:UseSharedCompilation=false`; `dotnet test HomestayBooking.sln --configuration Release` (lượt cuối dùng `--no-build --no-restore` sau build). Build cuối 0 cảnh báo/0 lỗi. Thêm 22 test tổng cộng: 6 A, 13 C, 3 D. Test hiệu năng StaffAvailabilityAndDatedSearchStayWithinTargets bỏ qua đã tồn tại trước bước này, không thêm skip.

`node --check`: 7 file JS ứng dụng đạt. `git diff --check`: đạt. Không trùng khóa resx; không thiếu khóa dịch literal trong 7 partial D.

## Kiểm trình duyệt

Bản QA Release chạy riêng trên **5012 / HomestayBooking_DeployTest / AutoMigrate=false**; app 5011 của người dùng được giữ nguyên. Không chạy app trên v5.

- UI Kit: 375/768/1280 × sáng/tối đều HTTP 200, không tràn ngang, không có lỗi JavaScript. 34 section, gồm cả section lồng.
- Màn 375: ô lịch rộng 44,70 px; ngày Flatpickr rộng 45,42 px. Trạng thái lịch có chữ/icon, không chỉ màu.
- QR: nền rgb(255,255,255) ở cả hai theme; nội dung clipboard khớp data-copy và nút đổi thành “Đã chép”.
- Tab/Enter/Esc: date picker, guest picker, xác nhận, tabs, lightbox, sidebar, chuông thông báo và lịch hoạt động. Lightbox trả focus về ảnh đã mở. Đổi tháng 10 → 11 đúng query.
- Reduced-motion: phần ảnh thẻ không translate/scale. No-JS: hai ô CheckIn/CheckOut vẫn hiện và dùng được.
- English: nhãn tìm phòng/lịch, rating và nút được dịch; tên khu vực, dữ liệu thông báo lưu bằng tiếng Việt giữ nguyên.
- Đăng nhập lần lượt QL001, NV001, KT001, 0912000001, 0987000001 trên database thử. 17 trang theo vai trò + `/`, `/Homestays`, `/Notifications`: 20 lượt đều 200, không có 500 hoặc JS exception.

Ảnh cục bộ (không commit): `.playwright-cli/ui02-D-{375|1280}-{light|dark}.png` toàn UI Kit; thêm ảnh 768 và ảnh từng component. Các trang bàn giao: `.playwright-cli/ui02-final-{home|homestays|admin|staff|accounting|owner|notifications}-1280-light.png`. Ảnh trạng thái tĩnh được chụp sau khi animation hoàn tất; không dùng ảnh đang chuyển động để kết luận vỡ layout.

## Vòng chụp → sửa nhóm D

| Trước | Sau | Lý do |
|---|---|---|
| Tên homestay thử rất dài chiếm 3 dòng | Giới hạn 2 dòng; giữ tên đầy đủ trong DOM/title | Thẻ dễ quét, không mất tên cho công cụ hỗ trợ đọc |
| Tiêu đề mẫu lịch dài 3 dòng trên điện thoại | “Lịch phòng theo đêm”, mô tả quyền ở đoạn riêng | Tiêu đề rõ, không biến thành đoạn văn |
| SearchBar/BookingPanel có bóng khi đứng yên | Bề mặt phẳng, phân biệt bằng nền/viền hiện có | House style; giữ viền chức năng của form theo DESIGN.md |
| Bản nháp D có token màu nền lightbox mới | Dùng on-paper đã có; tokens.css không đổi so với cuối C | Giữ hệ màu đã chốt |
| Hover của thẻ vừa trích còn dùng đường cong riêng cũ | Phần D dùng --fx-ease và các mức thời lượng sẵn có | Không thêm phong cách chuyển động khác |

Không chạy `draw.mjs`, không lấy palette/font/direction của design-taste-frontend. Không thêm chuyển động marketing/scroll reveal theo skill. DESIGN.md có ưu tiên: giữ viền form, thanh tìm dạng pill, font Be Vietnam Pro/Lora và tone trạng thái. Kiểm house style bằng ảnh thật; lint 2 JS D: 0 FAIL, 1 WARN padStart cho timer (đúng mục đích đồng hồ).

## Phạm vi và chỗ khác danh mục

- EmptyState có adapter object để lời gọi không truyền model vẫn tương thích; chỉ đọc EmptyStateModel, không dùng dynamic.
- NotificationsController mới là ngoại lệ được prompt 02-C cho phép. Controller gọi NotificationService đã có; không sửa service/schema/migration.
- SearchBar, BookingPanel, BookingStepper và RoomCalendar đã dựng để các bước trang sau dùng. Không triển khai màn nghiệp vụ bước 03/05 tại đây.
- Chart.js 4.4.1 đã tải cố định kèm giấy phép, chưa nạp/chưa dùng. Flatpickr 4.6.13 được theme bằng CSS token, không sửa vendor.
- UI Kit có một URL ảnh lỗi **có chủ ý** để kiểm fallback (404); đây không phải lỗi 500 hoặc lỗi JS.
- Mã hex trong CSS ứng dụng ngoài tokens: **57 → 20** (đếm số lần xuất hiện, loại vendor). Các mã còn lại ở phần trang cũ dành bước 03–04; không thêm mã hex mới ngoài tokens.
- Không còn `style=""` trong Views/Areas; không còn lời gọi hoặc file của bốn partial đã thay.

## Bằng chứng database QA

Snapshot từng bảng COUNT_BIG + CHECKSUM_AGG được lưu trước/sau từng lần mở app QA trên DeployTest. So đầu bước 02 với sau khi dừng app QA cuối: **43/45 bảng khớp cả số dòng và checksum**. Hai bảng khác: AuditLogs 83 → 84 (checksum -1897617035 → -1791328023); Notifications 42 → 43 (647910206 → 1584411493). Đây là database thử có app/job nền hoạt động trong QA; không kết luận toàn database không đổi. Lịch sử migration vẫn 9 dòng với checksum -395365947. Test SQL Server trong bộ test dùng ConstraintTest, fixture đối chiếu lại sau rollback.

Log build/test, TRX, manifest SHA-256 và snapshot nằm cục bộ tại `/private/tmp/LTW-ui02-b05n473r/`. Không lưu user-secrets, mật khẩu SQL hoặc connection string vào repo.
