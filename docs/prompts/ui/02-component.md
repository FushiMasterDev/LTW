# Bước 2 – Thư viện component

Đọc `docs/prompts/ui-rebuild.md` (luật chung, mục "Danh mục component cần có"), `DESIGN.md`, `docs/ui/inventory.md` (mục 2 "Mẫu UI lặp lại", mục 5–6 "Ràng buộc" và "Test phụ thuộc view"), `docs/ui/decisions.md`, `docs/ui/backend-gaps.md`. Mở `/Dev/UiKit` để xem những gì đã có.

Skill: `minimalist-ui`, `design-taste-frontend` (cho component trang khách), `ui-animation` (chỉ dùng các mức thời lượng có trong `motion.css`), `dataviz` (không dùng ở bước này), `full-output-enforcement`.

## Hiện trạng sau bước 01 (commit 7f7177a) – dùng lại, không làm lại

- **CSS:**
  - Thứ tự nạp: `tokens.css` → `base.css` → `components.css` → `layout-public.css` | `layout-dashboard.css` → `motion.css`.
  - Class mới dùng tiền tố `fx-`. Class không có `fx-` là class cũ. Chỗ nào bước này thay bằng component thì xóa class cũ tương ứng.
- **Layout:**
  - `_Layout` gồm `_SiteHeader` + `_SiteFooter`.
  - `_DashboardLayout` dùng View Component `Sidebar`.
  - `_AuthLayout` dùng cho Account/*, `Home/StatusCode`, `Shared/Error`.
- **Tag Helper** trong `HomestayBooking.Web/TagHelpers/`, đã đăng ký ở mọi `_ViewImports`: `<status-badge>`, `<money>`, `<date-range>`, `<icon>`, `<form-field>`.
- **View Component:** `Sidebar`, `WorkspaceSwitcher`. View nằm ở `Views/Shared/Components/{Tên}/Default.cshtml`.
- **UI Kit:** `/Dev/UiKit`, chỉ chạy ở Development, đăng nhập `QL001` / `Fuji@2026`. Form mẫu `UiKitViewModel`. `?errors=true` để xem trạng thái lỗi.
- **Mẫu test:** `HomestayBooking.Tests/TagHelpers/TagHelperTests.cs` có helper `Mvc()`, `RenderAsync()`, `Localizer()` để render Tag Helper với `IHtmlGenerator` thật. Dùng lại cho test mới.
- **Đổi tên:** `_SearchBar` cũ (ô tìm theo tên ở 3 trang danh mục Admin) đã đổi thành `_CatalogSearch`. Tên `_SearchBar` dành cho thanh tìm phòng ở bước này.
- **Backend B8 đã có:**
  - `NotificationService.LatestAsync/UnreadCountAsync/ListAsync/MarkReadAsync/MarkAllReadAsync`
  - `HomestayCard.Rating/ReviewCount`
  - `AvailabilityService.BlockedDatesAsync`
  - `BookingDetailsViewModel.HasReview`

## Bẫy đã gặp ở bước 01

- Tag Helper ghép lại các Tag Helper có sẵn: khi đặt `InputTypeName` thì phải đưa thuộc tính `type` vào `TagHelperContext`, nếu không sẽ ném `ArgumentException` lúc chạy. Xem `FormFieldTagHelper`.
- `AddClass` nằm trong namespace `Microsoft.AspNetCore.Mvc.TagHelpers`.
- Test dựng MVC bằng DI phải đăng ký `DiagnosticListener`/`DiagnosticSource`. Helper `Mvc()` đã làm sẵn.
- Môi trường test tích hợp là `Testing`, nên `/Dev/UiKit` trả 404 ở đó (đúng thiết kế). Test component bằng render Tag Helper/partial, không gọi UI Kit.
- Nếu cổng 5011 đang bận (có thể người dùng đang chạy app): **không tắt tiến trình đó**. Chạy bản của bạn bằng `--urls http://localhost:5012`.

## Quy tắc chung cho mỗi component

- **Model:** record trong `Models/ViewModels/Ui/` (namespace `HomestayBooking.Web.Models.ViewModels.Ui`, thêm `@using` vào các `_ViewImports`). XML doc cho từng tham số.
- **Vị trí:**
  - Partial đặt ở `Views/Shared/_Tên.cshtml` (cùng chỗ các partial hiện có), gọi bằng `<partial name="_Tên" model="..." />`.
  - **Không** đặt partial trong `Views/Shared/Components/`, vì thư mục đó dành cho View Component.
  - Tag Helper đặt ở `TagHelpers/`, View Component ở `ViewComponents/`.
- **CSS:** thêm vào `components.css`, mỗi component một khối có comment tiêu đề, class tiền tố `fx-`. Chỉ dùng biến `--fx-*` và `.tone-*`.
- **JS:**
  - Đặt ở `wwwroot/js/components/tên.js`, hành vi gắn qua `data-*`, không dùng inline handler.
  - Chỉ nạp ở trang cần, qua `@section Scripts`.
  - Hành vi dùng ở mọi trang (hộp xác nhận, toast) thì để trong `site.js`.
- **UI Kit:** mỗi component có mục riêng trong `/Dev/UiKit`, đủ các trạng thái: mặc định, rỗng, lỗi, đang tải, chữ rất dài, số rất lớn. Kiểm ở cả sáng/tối và VI/EN.
- **Chữ:** qua `L[...]`, thêm bản dịch vào `SharedResource.en.resx`.
- **Thay thế:** component nào "kế thừa/thay" partial cũ thì phải sửa **mọi** chỗ gọi trong cùng commit rồi xóa partial cũ.

## Ràng buộc test (không được làm gãy)

`HomestayBooking.Tests/Integration/*` đọc HTML thật. Khi viết lại các partial dưới đây phải giữ nguyên:

| Partial cũ → mới | Phải giữ |
|---|---|
| `_ChangeQuote` → `_PriceBreakdown` | thuộc tính `data-quote-total` trên ô tổng tiền mới; `data-additional-payment` |
| `_ChangeHistory` → `_Timeline` | mỗi yêu cầu bọc trong `article` có `data-change-request="{id}"` và `data-status="{status}"`; chuỗi "Yêu cầu đổi", "Lịch cũ", "Lịch mới", "Chờ quản lý duyệt", "Yêu cầu cũ, cần gửi lại"; `data-change-countdown` |
| `Staff/ChangeApprovals/Index` | khối `<section ... data-approval="{id}">…</section>` |
| `_AccountingTabs` → `_Tabs` | link sidebar/tabs `/Accounting/AdditionalDeposits` chỉ hiện với Kế toán |
| Trang lỗi | `class="status-code"`, chuỗi "Không tìm thấy trang" |
| Mọi form | token antiforgery do form tag helper sinh ra (test tìm bằng regex) |

Danh sách đầy đủ ở `inventory.md` mục 5–6. Chạy `dotnet test` sau mỗi nhóm.

## Thư viện cần thêm trước

Tải bản cố định từ cdnjs (đã kiểm URL trả 200), lưu vào `wwwroot/lib`, không dùng npm/bundler:
- Flatpickr 4.6.13:
  - `https://cdnjs.cloudflare.com/ajax/libs/flatpickr/4.6.13/flatpickr.min.js`
  - `.../flatpickr.min.css`
  - `.../l10n/vn.min.js`
- Chart.js 4.4.1: `https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js` (chưa dùng ở bước này, tải sẵn cho bước 05d).

Ghi phiên bản và nguồn vào `decisions.md`. Theme Flatpickr theo token bằng CSS trong `components.css`, không sửa file thư viện.

## Danh sách cần làm (commit theo nhóm)

### Nhóm A – Hiển thị dữ liệu → commit `feat(ui): component hiển thị dữ liệu`
- `<rating value="4.8" count="12" />`:
  - Màu `ink`, không dùng sao vàng.
  - Làm tròn 1 chữ số thập phân. 0 đánh giá thì hiện "Chưa có đánh giá".
  - Có `aria-label` đọc đủ câu.
  - Thay `bi-star-fill` đang dùng ở `Home/Index`.
- `_StatCard` (`StatCardModel`): icon → số → nhãn, link tùy chọn, tone chỉ dùng khi có ý nghĩa trạng thái. Thay `stat-card` ở các dashboard.
- `_DataTable`:
  - Khung bảng gồm đầu bảng, cột sắp xếp qua query string (`sort`, `dir`, giữ các tham số khác), cột số căn phải.
  - Dưới 768px chuyển thành danh sách thẻ bằng CSS (dùng `data-label` cho từng ô).
  - Dùng kèm `_RowActions`, `_Pager`, `_EmptyState`.
  - Logic dựng link sắp xếp viết trong C#, có test.
- `_DescriptionList` (`IReadOnlyList<(string Label, IHtmlContent Value)>` hoặc record tương đương).
- `_Timeline`: kế thừa và thay `_ChangeHistory` (giữ ràng buộc test ở trên). Dùng được cho lịch sử trạng thái đơn.
- `_PriceBreakdown`: kế thừa và thay `_ChangeQuote` + `_PolicyRules`. Gồm giá từng đêm, giảm giá/voucher, cọc, còn lại, phí đổi, tiền hoàn. Số tiền dùng `<money>`.
- `_EmptyState` (`EmptyStateModel`):
  - Viết lại: icon + tiêu đề + mô tả + hành động tùy chọn.
  - Không truyền model thì vẫn hiện "Không có dữ liệu." để 12 chỗ gọi cũ không gãy.
  - Dùng cho `Shared/Error.cshtml` (bỏ chữ tiếng Anh viết cứng).
- Skeleton dạng dòng và dạng thẻ (`fx-skeleton`), tôn trọng `prefers-reduced-motion`.

### Nhóm B – Form và phản hồi → commit `feat(ui): component form và phản hồi`
- `_FilterBar`: form GET đọc/ghi query string, nút "Xóa lọc", trên điện thoại thu vào offcanvas. Nhận danh sách trường qua model.
- `_DateRangePicker`:
  - Flatpickr chế độ khoảng ngày, tính theo đêm, chặn ngày quá khứ (theo `VietnamTime.Today`, không dùng `DateTime.Today`).
  - Ngôn ngữ vi/en theo culture.
  - Ghi vào 2 input `CheckIn`/`CheckOut` để giữ model binding.
  - Tùy chọn `data-blocked-url` để tải các đêm hết phòng (endpoint làm ở bước 03).
  - Không có JS vẫn còn 2 ô `type="date"` dùng được.
- `_GuestPicker`: số khách với nút +/−, giới hạn tối đa, vẫn là input `Guests` thật.
- `_UploadZone`:
  - Kéo thả hoặc chọn file, preview ảnh, `multiple`.
  - Kiểm tra jpg/png/webp/pdf và ≤ 5 MB phía client. Server vẫn là nơi kiểm tra chính.
  - Vẫn là `<input type="file">` thật để form submit bình thường.
- Hộp xác nhận: nâng `data-confirm` thành modal Bootstrap.
  - Thêm `data-confirm-title` và `data-confirm-variant="danger"`.
  - Giữ tương thích mọi chỗ đang dùng `data-confirm`.
  - Esc/Hủy thì không submit.
- `_Alerts`: viết lại, giữ khóa `TempData["Success"]`/`TempData["Error"]`, thêm `TempData["Info"]`.
- Toast cho thao tác AJAX (`window.fxToast(message, tone)` trong `site.js`, vùng `aria-live`).

### Nhóm C – Điều hướng và thông báo → commit `feat(ui): điều hướng, tabs và thông báo`
- `_Tabs` (`TabsModel`): tổng quát hóa từ `_AccountingTabs` (có số đếm, ẩn tab theo quyền), rồi thay `_AccountingTabs` ở 6 trang Accounting và xóa file cũ.
- `_Breadcrumb`: dùng thay breadcrumb viết tay trong `_DashboardLayout`.
- `_PageHead`:
  - Viết lại, bỏ `icon-chip` tô màu theo module (DESIGN.md: dashboard yên tĩnh).
  - Nút chính nhận action/route qua model thay vì luôn `asp-action="Create"`.
  - Giữ tương thích 13 chỗ gọi.
- Gỡ `page-banner banner-*` (gradient) khỏi các dashboard, thay bằng `_PageHead`.
- View Component `NotificationBell`:
  - Dùng `NotificationService.UnreadCountAsync/LatestAsync`, hiện số chưa đọc và 5 tin mới nhất.
  - Đặt trên topbar `_DashboardLayout`, và trên `_SiteHeader` khi đã đăng nhập.
- **Trang thông báo dùng chung** (prompt `05a` chỉ rà lại):
  - `NotificationsController` (không area, `[Authorize]`): `Index` (phân trang, `ListAsync`); `MarkRead(id)` POST, chuyển tới `Url` của thông báo nếu là đường dẫn nội bộ; `MarkAllRead` POST.
  - Dùng `User` id hiện tại, PRG + `_Alerts`.
  - Test controller: đánh dấu thông báo của người khác thì không đổi gì; URL ngoài site không được redirect tới (chống open redirect).
  - Chọn layout theo vai trò: nhân viên/chủ dùng `_DashboardLayout`, khách dùng `_Layout`.

### Nhóm D – Đặt phòng → commit `feat(ui): component đặt phòng`
- `_SearchBar` (`SearchBarModel`):
  - Dạng viên thuốc: Khu vực | Ngày (`_DateRangePicker`) | Khách (`_GuestPicker`) | nút tìm.
  - Biến thể `hero` và `compact`.
  - Giữ đúng tên tham số query mà `HomestaysController.Index` đang đọc (`Q`, `AreaId`, `CheckIn`, `CheckOut`, `Guests`…).
  - Trên điện thoại xếp dọc.
- `_HomestayCard`:
  - Ảnh tỉ lệ cố định, có khung "Chưa có ảnh" khi ảnh lỗi (giữ cơ chế `.is-broken` của `site.js`).
  - Gồm tên, khu vực, giá/đêm từ (`<money>`), `<rating>` từ `HomestayCard.Rating/ReviewCount`, badge tùy chọn.
  - Thay `stay-card` ở `Home/Index` và `Homestays/Index`.
- `_Gallery`: lưới trên desktop, dải vuốt trên điện thoại, mở lightbox (giữ `data-lightbox-*` và `#lightbox`).
- `_BookingPanel`: ô báo giá sticky, dùng `_DateRangePicker`, `_GuestPicker`, `_PriceBreakdown`. Chỉ dựng component và mục UI Kit, gắn vào trang ở bước 03.
- `_BookingStepper`: tiến trình đơn theo `BookingStatus`, chỉ để đọc (không phải nút), có `aria-current="step"`.
- `_PaymentQr`:
  - QR từ `VietQrService`, số tiền (`<money>`), nội dung chuyển khoản, nút sao chép (`data-copy`), đếm ngược hạn giữ chỗ.
  - Luôn trên nền trắng.
  - Bỏ inline `onerror` của `img.qr-image`.
- `_RoomCalendar` (`RoomCalendarModel`):
  - Lưới tháng, mỗi ô là một đêm × phòng. Trạng thái trống/đã đặt/bảo trì/khóa có chú thích màu theo `tone-*` và có chữ/biểu tượng (không chỉ dựa vào màu).
  - Điều hướng tháng, bấm ô để chọn, dùng được bằng bàn phím.
  - Dựng tuần/ngày đầu tháng bằng C#, có test.
  - Dùng cho Chủ + Quản lý/Quản trị (Tư vấn không xem lịch tháng, quyết định 17/10).

## Test

Thêm vào `HomestayBooking.Tests/TagHelpers/` (hoặc thư mục `Ui/` cùng cấp), dùng lại helper của `TagHelperTests`:
- `rating`: làm tròn, 0 đánh giá, `aria-label`.
- Link sắp xếp của `_DataTable`: giữ tham số khác, đảo chiều.
- `RoomCalendarModel`: tháng bắt đầu giữa tuần, tháng 2 năm nhuận, đêm ở hai tháng.
- `NotificationsController`: đánh dấu đúng chủ, chống open redirect.
- Một test cho mỗi partial thay thế (`_Timeline`, `_PriceBreakdown`), render và kiểm các thuộc tính ở bảng "Ràng buộc test", nếu test tích hợp hiện có chưa bao.

## Kết quả bàn giao

- 4 commit theo nhóm A–D. Nếu hết phiên giữa chừng, dừng ở cuối một nhóm đã commit.
- Cập nhật `docs/ui/decisions.md`: quyết định mới, phiên bản thư viện.
- Ảnh `playwright-cli` của `/Dev/UiKit` (toàn trang) ở 375/1280 × sáng/tối. Ảnh `/`, `/Homestays`, một dashboard mỗi khu, `/Notifications` ở 1280 × sáng.

## Kiểm tra trước khi báo xong

- `dotnet build` không có cảnh báo mới. `dotnet test HomestayBooking.sln` pass toàn bộ, báo số test trước/sau (trước: 1568 gồm 1 skip).
- Mọi component trong danh sách có mục trên UI Kit.
- `grep -rn 'style="' HomestayBooking.Web/Views HomestayBooking.Web/Areas` trả về rỗng. Không thêm mã hex ngoài `tokens.css`. Số mã hex cũ (57) phải giảm, báo số còn lại.
- `_ChangeHistory`, `_ChangeQuote`, `_PolicyRules`, `_AccountingTabs` đã xóa, và không còn chỗ gọi.
- Dùng bàn phím (Tab, Enter, Esc) được với date picker, guest picker, modal xác nhận, tabs, lightbox, ngăn kéo sidebar, chuông thông báo, lịch phòng.
- Đăng nhập lần lượt `QL001`, `NV001`, `KT001`, `0912000001`, `0987000001` (mật khẩu `Fuji@2026`), mở dashboard và vài trang mỗi vai trò: không trang nào lỗi 500.

## Báo cáo rồi dừng

Báo cáo:
- Bảng component × trạng thái đã có.
- Partial cũ đã thay/xóa.
- Số mã hex trước/sau.
- Component lệch danh mục, kèm lý do.

**Không bắt đầu bước 3.**
