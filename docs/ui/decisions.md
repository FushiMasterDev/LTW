# Quyết định giao diện

Mỗi quyết định ghi một dòng: ngày, bước, nội dung, lý do. Dùng cho các chỗ `ui-rebuild.md` để ngỏ, các chỗ lệch `DESIGN.md`, cách làm tạm cho gap trong `backend-gaps.md`, hoặc khi thêm thư viện JS/CSS.

| Ngày | Bước | Quyết định | Lý do |
|---|---|---|---|
| 17/10/2026 | 01 | Tách CSS: `site.css` → `base.css` (nền, utility ghi đè, reduced-motion) + `components.css`; `public.css` → `layout-public.css`; `dashboard.css` + `accounting.css` → `layout-dashboard.css` (phần kế toán đặt cuối file) | Giữ đúng thứ tự cascade cũ (accounting.css từng nạp sau dashboard.css, chỉ ở dashboard); tránh vỡ giao diện khi chưa viết lại view |
| 17/10/2026 | 01 | Chỉ xóa 5 class chết ở `public.css`; giữ `form-select-sm`, `text-body-secondary` | Hai class này là ghi đè lên utility của Bootstrap, view mới có thể dùng; không phải CSS chết của dự án |
| 17/10/2026 | 01 | Class không có tiền tố `fx-` trong `components.css`/`layout-*.css` đều là class cũ, còn 57 mã hex nằm trong chúng; bước 02–04 thay bằng component mới rồi xóa | Không đánh dấu `/* legacy */` từng class vì hầu hết sẽ bị thay; ghi một lần ở đầu file |
| 17/10/2026 | 01 | Đổi tên `_SearchBar` cũ (ô tìm theo tên ở 3 trang danh mục Admin) thành `_CatalogSearch`; để dành `_SearchBar` cho thanh tìm phòng dạng viên thuốc | Tránh trùng tên với component của bước 02 (đã ghi ở inventory mục 1.6) |
| 17/10/2026 | 01 | Chưa đặt `_SearchBar` compact vào header trang khách | Component thanh tìm phòng làm ở bước 02; gắn vào header ở bước 03 |
| 17/10/2026 | 01 | Sidebar là View Component `Sidebar`, số đếm qua `BookingChangeRequestService.PendingCountAsync` và `AccountingService.PendingAdditionalDeposits`; bỏ `@inject ApplicationDbContext` khỏi `_DashboardLayout` | Layout không tự truy vấn; giữ nguyên markup và quyền hiện link (test `SidebarShowsOnlyPermittedChangeActions`) |
| 17/10/2026 | 01 | `WorkspaceSwitcher` chỉ dùng ở header trang khách | Sidebar dashboard đã liệt kê mọi khu làm việc theo nhóm |
| 17/10/2026 | 01 | Đăng nhập, đăng ký, đổi mật khẩu, AccessDenied, StatusCode, Error dùng `_AuthLayout` | Khung gọn theo `ui-rebuild.md`; các view vốn đã tự dựng thẻ 2 cột |
| 17/10/2026 | 01 | `<money>` luôn định dạng VND theo vi-VN (`1.500.000đ`) kể cả khi giao diện tiếng Anh | Theo `ui-rebuild.md`; đơn vị là đồng, dấu nhóm kiểu Việt Nam tránh đọc nhầm |
| 17/10/2026 | 01 | Chữ mô tả trên `Dev/UiKit` không đưa vào resource; trang dùng `_DashboardLayout` | Trang công cụ cho nhóm phát triển như `SimulateBank`; component bên trong vẫn tự dịch |
| 17/10/2026 | 02-A | Component hiển thị dùng record trong Models/ViewModels/Ui; Timeline/PriceBreakdown thay toàn bộ lời gọi ChangeHistory/ChangeQuote/PolicyRules, giữ hook tích hợp | Một mẫu hiển thị, không đổi dữ liệu hoặc nghiệp vụ |
| 17/10/2026 | 02-A | EmptyState nhận object ở ranh giới tương thích, chỉ đọc EmptyStateModel; model trang cũ bị bỏ qua | Partial không truyền model sẽ kế thừa model trang; giữ lời gọi cũ mà không dùng dynamic |
| 17/10/2026 | 02-A | Flatpickr 4.6.13 (JS/CSS, vi), Chart.js 4.4.1 từ cdnjs, kèm LICENSE.md từ tag chính thức; Chart.js chưa nạp ở bước 02 | Phiên bản cố định theo prompt, không dùng npm/bundler; CSS thư viện không sửa, chỉ theme qua token |
| 17/10/2026 | 02-A | Kiểm tra trình duyệt bằng app Release cổng 5012 trên DeployTest, AutoMigrate=false, snapshot từng bảng trước/sau | Không tác động app 5011 hoặc ghi vào v5 |
| 17/10/2026 | 02-B | DateRangePicker giữ hai input date thật, chỉ ẩn sau khi Flatpickr khởi tạo; checkout không tính đêm, cho trả đúng ngày bắt đầu bị khóa | Giữ model binding/no-JS và mô hình đặt phòng theo đêm |
| 17/10/2026 | 02-B | Hộp xác nhận và toast nằm ở FeedbackHost trong ba layout; site.js dùng requestSubmit với submitter gốc | Giữ antiforgery, giá trị nút và data-confirm cũ; Esc/Hủy không submit |
| 17/10/2026 | 02-B | Alerts đọc Success/Error/Info và ghép Alert kiểu rõ; UI Kit dùng cùng partial Alert | Không lặp mẫu HTML hoặc chèn HTML vào thông báo |
| 17/10/2026 | 02-B | Thêm token paper/on-paper cho nền trắng cố định; chuyển màu nút cũ sang token hiện có | QR bước D giữ trắng cả dark mode; giảm mã hex ngoài tokens mà giữ nhận diện |
| 17/10/2026 | 02-C | Tabs dùng nav/aria-current cho liên kết sang trang, không giả role=tab của widget đổi panel | Tab/Enter theo hành vi trình duyệt; giữ nguyên quyền Cọc bổ sung chỉ Kế toán |
| 17/10/2026 | 02-C | MarkRead dùng returnUrl từ form sau khi service xác nhận thông báo thuộc tài khoản, chỉ LocalRedirect khi Url.IsLocalUrl | Dùng service B8 hiện có, không tự truy vấn DbContext; chặn open redirect và thao tác người khác |
| 17/10/2026 | 02-C | Giữ Icon/Tone/ShowCreate của PageHead để tương thích; Tone không còn tô màu module, thêm Action/Controller/Area/RouteValues/ActionLabel | Các lời gọi cũ vẫn hoạt động; bỏ page-banner ở cả bốn dashboard |
| 17/10/2026 | 02-C | Nội dung Notification do service lưu bằng tiếng Việt được hiển thị nguyên văn và encode; nhãn điều khiển được dịch | Không dịch dữ liệu người dùng hoặc sửa Services trong giai đoạn giao diện |
| 17/10/2026 | 02-D | Thẻ/galleries nhận ảnh từ view model, fallback qua .is-broken; UI Kit chỉ dùng đường dẫn ảnh demo đã có | Không thêm stock hoặc ảnh ngoài dữ liệu vào thẻ thật |
| 17/10/2026 | 02-D | SearchBar và BookingPanel chỉ dựng ở UI Kit; thẻ, gallery và QR thay mẫu lặp tại trang thật | Giữ phạm vi thư viện; chưa bắt đầu bước 03 hoặc gắn header tìm kiếm |
| 17/10/2026 | 02-D | Lịch tháng bắt đầu thứ Hai, C# dựng đủ tuần, dữ liệu nhận từ CalendarNight; chỉ chọn ô/phát sự kiện, không tự ghi lịch | Dùng cho Chủ/Quản lý ở bước 05, Tư vấn vẫn xem bảng phòng trống |
| 17/10/2026 | 02-D | PaymentQr dùng URL VietQrService, luôn nền paper trắng; fallback/clipboard/timer chuyển sang payment-qr.js, giữ data-change-countdown | Bỏ inline onerror, dùng chung cọc đầu và bổ sung; QR demo hết hạn không tự reload |
| 17/10/2026 | 02-D | Áp dụng house style và vòng chụp → sửa của design-taste-frontend; không chạy draw.mjs, không đổi DESIGN.md/tokens.css so với cuối C; chuyển động D chỉ dùng thời lượng cũ và --fx-ease | Theo chỉ dẫn bổ sung của người dùng; không đưa bảng màu/font hoặc animation marketing vào ứng dụng |
