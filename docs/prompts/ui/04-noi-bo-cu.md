# Bước 4 – Chuyển các màn nội bộ đang có

> Cập nhật 19/10/2026 sau bước 03, 03y, 03y2, 03y3 (HEAD `b12d011`). Nhận diện đã đổi theo bản mẫu Lovable "Lúa"; khung dashboard (sidebar, topbar, `_StatCard`, `_PageHead`, `_DataTable`, `_FilterBar`) đã đổi ở 03y. Bước này chuyển **nội dung** các màn nội bộ sang thư viện component và phong cách mới.

Đọc:
- `docs/prompts/ui-rebuild.md`, `DESIGN.md` (bản mới sau 03y), `docs/ui/inventory.md`, `docs/ui/decisions.md` (nhất là các dòng 03y–03y3), `docs/ui/03y-phong-cach-report.md`.
- Xem `/Dev/UiKit` (đăng nhập `QL001`).
- Mở bản mẫu bằng `playwright-cli` để lấy tinh thần dashboard: https://lua-ivory-forest-capture.lovable.app/admin, `/owner`, `/staff`, `/accounting`. Chỉ lấy bố cục và cách trình bày. Không lấy số liệu, biểu đồ hay chức năng mà app chưa có.

Skill:
- `minimalist-ui`: mục cấm, thông số component, icon, chuyển động nhẹ. **Không** dùng bảng màu hay font riêng của skill.
- `design-taste-frontend`: chỉ dùng house style và vòng chụp → sửa; không `draw.mjs`, không `inspect.mjs` cho dashboard. Inspector đo trang marketing, dashboard không cần hero, ảnh hay đủ số phần tử chuyển động.
- `ui-animation`, `full-output-enforcement`, `web-design-guidelines`.

## Hiện trạng – dùng lại, không làm lại

- **Token, font, bo góc, bóng:** theo `tokens.css` mới.
  - Xanh rừng cho thương hiệu, link, mục chọn; cam đất cho hành động chính.
  - Manrope cho chữ giao diện. Fraunces cho tiêu đề trang và số liệu `_StatCard`.
  - Thẻ bo 1.5rem, nút dạng viên.
  - Có chế độ sáng và tối, tương phản ≥ 4.5:1.
- **Khung dashboard (03y):**
  - Sidebar xanh đậm có viên cam trượt sang mục đang chọn; topbar sáng.
  - Chuyển động: `wwwroot/js/motion/dashboard.js` (thẻ số liệu hiện lần lượt, số đếm lên 800ms và giữ giá trị cuối trong HTML, Chart.js 800ms nếu trang có biểu đồ).
- **Component có sẵn:**
  - Hiển thị: `_StatCard`, `_DataTable`, `_DescriptionList`, `_Timeline`, `_PriceBreakdown`, `_EmptyState`, `_Skeleton`.
  - Trạng thái và số: `<status-badge>`, `<money>`, `<rating>`, `<date-range>`.
  - Điều hướng: `_FilterBar`, `_Tabs`, `_Breadcrumb`, `_PageHead`, `_Pager`, `_RowActions`.
  - Phản hồi: `_Alert`/`_Alerts`, modal `data-confirm` (đặt trên form hoặc nút submit), `window.fxToast`.
  - Form và lịch: `_UploadZone`, `_RoomCalendar`, `_CatalogSearch`, `form-field`.
  - Model nằm ở `Models/ViewModels/Ui/`. Cần mẫu mới thì thêm vào thư viện + `/Dev/UiKit` trước rồi mới dùng.
- **Không có 3D tự chạy, băng chuyền, Ken Burns hay nút tạm dừng ở dashboard** (quyết định 03y3).
- **CSS:**
  - `accounting.css` đã gộp vào cuối `layout-dashboard.css` từ bước 01.
  - Class **không có tiền tố `fx-`** trong `layout-dashboard.css`/`components.css` là class cũ, sẽ xóa sau khi chuyển xong.
  - Mã màu ngoài `tokens.css`: 0. `style="`: 0. Giữ nguyên hai con số này.
- **Test nền:** 1597 pass + 1 skip.

## Phạm vi: chỉ các view đang có

- **Admin:** `Dashboard`, `Amenities` (Index/Form), `Areas` (Index/Form), `Holidays` (Index/Form), `RoomCategories` (Index/Form), `Vouchers` (Index/Create).
- **Staff:** `Dashboard`, `ChangeRequests/Index`, `ChangeApprovals/Index`.
- **Owner:** `Dashboard`, `Bookings/Index`, `Bookings/Details`.
- **Accounting:** `Dashboard`, `Transactions` (Index/Process), `Receipts/Index`, `Refunds/Index`, `AdditionalDeposits/Index`.
- **Dev:** `SimulateBank`.

Màn mới (sửa homestay, lịch phòng của chủ, báo cáo, hợp đồng…) là bước 05a–05d. **Không làm ở đây.**

## Mẫu chung

- **Trang danh sách:**
  - `_PageHead` (tiêu đề Fraunces + nút chính màu cam) → `_FilterBar` → `_DataTable` → `_Pager`. Có `_EmptyState`.
  - Trên điện thoại, bảng thành danh sách thẻ. Nếu `_DataTable` chưa có chế độ này thì thêm vào component (và UI Kit), không viết riêng từng view.
- **Trang form:**
  - `_PageHead` + `_Breadcrumb` → một thẻ một cột tối đa khoảng 40rem, dùng `form-field`.
  - Nút Lưu (cam) / Hủy nằm ở cuối, dính dưới trên điện thoại. Có `data-loading-text`.
- **Xóa danh mục:**
  - Gọi qua `CatalogDeletionService` nếu controller đã dùng.
  - Không xóa được (đang được dùng) thì báo qua `_Alerts`.
  - Nút xóa dùng hộp xác nhận `danger`.
- **Dashboard mỗi khu:**
  - Hàng `_StatCard` (kiểu bản mẫu: nhãn, số Fraunces lớn, icon tròn sage) + danh sách "Việc cần xử lý" (đơn chờ xác nhận, yêu cầu đổi/hủy chờ duyệt, giao dịch chưa khớp…).
  - Số liệu lấy từ dữ liệu controller đang truyền. Muốn thêm số liệu thì chỉ gọi service đã có.
  - **Không bịa** phần trăm tăng/giảm "so với tháng trước" hay biểu đồ nếu không có dữ liệu thật.
- **Owner:**
  - Ưu tiên điện thoại: nút xác nhận/từ chối đơn to (cao ≥ 44px), nằm trong tầm ngón cái.
  - Chi tiết đơn đọc được trên 375px.
- **Accounting:**
  - Các trang gom bằng `_Tabs`.
  - Số tiền dùng `<money>`, trạng thái khớp ngân hàng dùng `<status-badge>`.
  - Số căn phải, dùng `tabular-nums`.
- **Chuyển động:** chỉ dùng những gì `dashboard.js`/`motion.css` đã có. Nếu thêm thì chỉ thêm phản hồi nhẹ (hover/nhấn nút, hàng bảng), dùng token thời lượng, có khối `prefers-reduced-motion`.

Sau khi chuyển xong:
- Xóa class cũ (không có tiền tố `fx-`) trong `layout-dashboard.css`/`components.css` mà không còn ai dùng. Kiểm bằng `rg` trên toàn bộ `Views/`, `Areas/`, `wwwroot/js/`.
- Xóa partial cũ đã có component thay thế, sửa mọi chỗ gọi.
- Cập nhật mục Dashboard của `DESIGN.md` (bỏ câu "Không sửa nghiệp vụ hoặc làm bước 04") và ghi quyết định mới vào `decisions.md`.

## Không được đổi

- `@model`, tên trường form, `asp-*`, route, antiforgery, validation, quyền hiện link ở sidebar.
- Mọi hook `data-*` và chuỗi mà test tích hợp đọc. Đọc các test trong `HomestayBooking.Tests/Integration` liên quan Admin/Staff/Owner/Accounting trước khi sửa view.
- Không sửa `Services/`, `Data/`, `Models/Entities/`, migration. Controller không thêm truy vấn mới.
- Không `style="`, không màu ngoài `tokens.css`, không thư viện mới. Chữ mới qua `L[...]` và có bản dịch trong `SharedResource.en.resx`.

## Kết quả bàn giao

- Commit theo area, mỗi commit đi qua vòng: build → test → chụp → sửa.
  1. `feat(ui): chuyển màn Admin sang thư viện component`
  2. `feat(ui): chuyển màn Staff sang thư viện component`
  3. `feat(ui): chuyển màn Owner sang thư viện component`
  4. `feat(ui): chuyển màn Accounting sang thư viện component`
  5. Cuối cùng: `refactor(ui): xóa CSS và partial cũ của dashboard`
- Ảnh `playwright-cli` mỗi màn ở 1280 × sáng/tối, riêng Owner thêm 375. Ảnh để ngoài repo (thư mục tạm hoặc `.playwright-cli/`), không commit.

## Kiểm tra trước khi báo xong

- `dotnet build -c Release -p:UseSharedCompilation=false` không cảnh báo mới. `dotnet test HomestayBooking.sln -c Release --no-build` pass, báo số test.
- Chạy app riêng cổng 5012 (`ASPNETCORE_ENVIRONMENT=Development dotnet run --project <đường dẫn tuyệt đối>/HomestayBooking.Web --no-launch-profile --urls http://localhost:5012`).
  - Không tắt app cổng 5011.
  - Dừng app của mình bằng đúng PID. Không dùng `pkill -f`/`killall` với mẫu ngắn.
  - SQL Server ở container Docker `sql-homestay`.
- Đăng nhập lần lượt `AD001`, `QL001`, `NV001`, `KT001` (ở `/Account/EmployeeLogin`) và `0912000001` (mật khẩu `Fuji@2026`): mỗi tài khoản mở hết màn của mình.
  - Tạo, sửa, xóa một mục danh mục.
  - Xử lý một giao dịch: `SimulateBank` → `Transactions/Process`.
  - Chủ homestay xác nhận hoặc từ chối một đơn trên 375px.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: không có trang 500. Đã biết: `/Bookings/Create` thiếu `roomTypeId` trả 404, 3 thẻ mẫu ở UI Kit trả 404.
- `rg`: không còn class cũ đã xóa, không `style="`, không mã màu ngoài `tokens.css`.
- Chạy `web-design-guidelines` trên `Areas/**/*.cshtml` và sửa lỗi mức cao.

## Báo cáo rồi dừng

Viết `docs/ui/04-noi-bo-report.md`:
- màn đã chuyển;
- component thêm/sửa;
- CSS/partial đã xóa, số dòng CSS trước/sau;
- dữ liệu thử đã ghi vào database Development;
- chỗ lệch `DESIGN.md`;
- mục "Cần người dùng quyết định" (nếu có).

**Không bắt đầu bước 5.**
