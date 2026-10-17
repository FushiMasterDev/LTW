# Bước 2 – Thư viện component

Đọc `docs/prompts/ui-rebuild.md` (mục "Danh mục component cần có"), `DESIGN.md`, `docs/ui/inventory.md` (mục mẫu UI lặp lại), `docs/ui/decisions.md`.

Skill: `minimalist-ui`, `design-taste-frontend` (cho component trang khách), `ui-animation` (theo mức thời lượng trong `motion.css`), `full-output-enforcement`.

## Quy tắc chung cho mỗi component

- **Model:** record trong `Models/ViewModels/Ui/`, có XML doc cho từng tham số.
- **Vị trí:** partial ở `Views/Shared/Components/_Ten.cshtml`, CSS trong `components.css` (một khối, có comment tiêu đề), JS (nếu có) ở `wwwroot/js/components/ten.js`.
- **UI Kit:** mỗi component có mục riêng với các trạng thái mặc định, rỗng, lỗi, đang tải, chữ rất dài, số rất lớn, sáng/tối.
- **Chữ:** qua `L[...]`, thêm bản dịch vào `SharedResource.en.resx`.

## Thư viện cần thêm trước

Bước 0 ghi nhận Flatpickr và Chart.js **chưa có** trong `wwwroot/lib`. Tải về `wwwroot/lib/flatpickr/` (kèm locale `vn`) và `wwwroot/lib/chart.js/`, mỗi thư viện một phiên bản cố định. Chỉ nạp ở trang cần dùng qua `@section Scripts`. Ghi phiên bản vào `decisions.md`.

## Danh sách cần làm

**Hiển thị dữ liệu**
- `<rating value="4.8" count="12" />`: màu `ink`, không dùng sao vàng.
- `_StatCard`: icon → số → nhãn, có link tùy chọn.
- `_DataTable`: đầu bảng, cột sắp xếp qua query string, cột số căn phải, trên điện thoại chuyển thành danh sách thẻ. Dùng kèm `_RowActions` và `_Pager`.
- `_DescriptionList` (key–value).
- `_Timeline`: kế thừa và thay `_ChangeHistory`.
- `_PriceBreakdown`: kế thừa `_ChangeQuote` và `_PolicyRules`, gồm giá từng đêm, giảm giá, cọc, còn lại.
- `_EmptyState`: viết lại, có icon + tiêu đề + mô tả + hành động.
- Skeleton dạng dòng và dạng thẻ.

**Form**
- `_FilterBar`: đọc và ghi query string, có nút xóa lọc, trên điện thoại thu vào offcanvas.
- `_DateRangePicker`: Flatpickr, tính theo đêm, chặn ngày quá khứ, ngôn ngữ vi/en, ghi vào 2 input `CheckIn`/`CheckOut` để giữ model binding.
- `_GuestPicker`: số người lớn/trẻ em, có giới hạn tối đa.
- `_UploadZone`: kéo thả hoặc chọn file, preview ảnh, kiểm tra jpg/png/webp/pdf và ≤ 5 MB phía client, có `multiple`. Server vẫn là nơi kiểm tra chính.
- Hộp xác nhận: nâng cấp `data-confirm` thành modal Bootstrap, có `data-confirm-title` và `data-confirm-variant="danger"`.

**Điều hướng**
- `_Tabs`: tổng quát hóa từ `_AccountingTabs`, rồi thay `_AccountingTabs` bằng component này.
- `_Breadcrumb`.
- View Component `NotificationBell`: dùng `NotificationService.UnreadCountAsync/LatestAsync` (B8), hiện số chưa đọc và 5 thông báo mới nhất, có link "Xem tất cả".

**Đặt phòng**
- `_SearchBar`: viết lại dạng viên thuốc Khu vực | Ngày | Khách | nút tìm, có biến thể `hero` và `compact`. Giữ tên tham số query mà `HomestaysController.Index` đang đọc.
- `_HomestayCard`: ảnh tỉ lệ cố định + "Chưa có ảnh", tên, khu vực, giá/đêm từ, rating (`HomestayCard.Rating/ReviewCount` từ B8), badge tùy chọn.
- `_Gallery`: lưới trên desktop, dải vuốt trên điện thoại, mở lightbox (giữ `data-lightbox-*`).
- `_BookingPanel`: ô báo giá sticky, dùng `_DateRangePicker`, `_GuestPicker`, `_PriceBreakdown`.
- `_BookingStepper`: tiến trình đơn theo `BookingStatus`, chỉ để đọc.
- `_PaymentQr`: QR từ `VietQrService`, nội dung chuyển khoản, nút sao chép, luôn trên nền trắng.
- `_RoomCalendar`: lưới tháng, mỗi ô là một đêm × phòng, trạng thái trống/đã đặt/bảo trì/khóa. Có chú thích màu theo `tone-*`, điều hướng tháng, bấm ô để chọn. Dùng cho chủ homestay và Quản lý/Quản trị (Tư vấn không xem lịch tháng).

**Phản hồi**
- `_Alerts`: viết lại.
- Toast cho thao tác AJAX.
- Trang `Error`, `StatusCode`, `AccessDenied` dùng `_AuthLayout` + `_EmptyState`.

## Test

Thêm test cho component có logic: `rating` (làm tròn, 0 đánh giá), `_RoomCalendar` model (dựng tuần, ngày đầu tháng), `_DataTable` (sắp xếp và query string), nếu logic nằm trong C#.

## Kết quả bàn giao

- Commit: `feat(ui): thư viện component dùng chung`.
- Ảnh `playwright-cli` của `/Dev/UiKit` (toàn trang) ở 375/1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Mọi component trong danh sách đều có mục trên UI Kit.
- Không có mã hex mới ngoài `tokens.css`. Không có `style="`.
- Dùng bàn phím (Tab, Enter, Esc) với date picker, modal xác nhận, tabs, lightbox, ngăn kéo sidebar: tất cả dùng được.

## Báo cáo rồi dừng

Báo cáo bảng component × trạng thái đã có, và component nào lệch danh mục kèm lý do. **Không bắt đầu bước 3.**
