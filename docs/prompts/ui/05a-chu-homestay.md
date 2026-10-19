# Bước 5a – Màn mới cho chủ homestay

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/ui/inventory.md` (mục màn còn thiếu), `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui` (chỉ mục cấm, thông số component, icon; không dùng bảng màu/font riêng của skill), `ui-ux-pro-max` (UX form và lịch trên điện thoại), `ui-animation`, `full-output-enforcement`, `web-design-guidelines`.

## Hiện trạng và luật chung (cập nhật 19/10/2026, áp dụng mọi bước 5)

Sau bước 03, 03y–03y3 và 04 (HEAD `8ee6861`):
- **Nhận diện:** theo `DESIGN.md` mới (bản mẫu Lovable "Lúa"). Xanh rừng cho thương hiệu/link/mục chọn, cam đất cho hành động chính. Manrope cho chữ, Fraunces cho tiêu đề trang và số liệu `_StatCard`. Thẻ bo 1.5rem, nút dạng viên. Sáng/tối, tương phản ≥ 4.5:1. Mọi màu lấy từ `tokens.css`.
- **Tham khảo bố cục dashboard:** https://lua-ivory-forest-capture.lovable.app/admin, `/owner`, `/staff`, `/accounting` (mở bằng `playwright-cli`). Chỉ lấy bố cục và cách trình bày. Không lấy số liệu, phần trăm "so với tháng trước" hay biểu đồ khi app không có dữ liệu thật.
- **Component có sẵn** (xem `/Dev/UiKit`, đăng nhập `QL001`):
  - khung: `_PageHead`, `_Breadcrumb`, `_Tabs`, `_FilterBar`, `_Pager`, `_RowActions`;
  - dữ liệu: `_DataTable` (tự thành thẻ dưới 992px), `_DescriptionList`, `_Timeline`, `_PriceBreakdown`, `_StatCard`, `_TaskList`, `_ReceiptReview`, `_EmptyState`, `_Skeleton`;
  - form: `_UploadZone`, `_RoomCalendar`, `_DateRangePicker`, `_GuestPicker`, `form-field`;
  - Tag Helper: `<money>`, `<status-badge>`, `<rating>`, `<date-range>`;
  - phản hồi: `_Alerts`, modal `data-confirm` (trên form hoặc nút submit), `window.fxToast`.
  
  Cần mẫu mới thì thêm vào thư viện component + `/Dev/UiKit` trước rồi mới dùng.
- **Mẫu trang:** như mục "Mẫu chung" của `docs/prompts/ui/04-noi-bo-cu.md`. Danh sách: PageHead → FilterBar → DataTable → Pager. Form: một thẻ một cột ≤ 40rem, nút Lưu cam dính dưới trên điện thoại.
- **Chuyển động dashboard:** chỉ dùng những gì `wwwroot/js/motion/dashboard.js` và `motion.css` đã có (thẻ hiện lần lượt, số đếm lên giữ giá trị cuối trong HTML, Chart.js 800ms). **Không** 3D tự chạy, băng chuyền, Ken Burns hay nút tạm dừng ở dashboard (quyết định 03y3).
- **Biểu đồ:** Chart.js 4.4.1 đã có sẵn trong `wwwroot/lib` (quyết định 02-A). Chỉ nạp ở trang cần. Màu đọc từ biến CSS (`--fx-primary` xanh rừng, `--fx-accent` cam đất, `tone-*`) qua `getComputedStyle`, vẽ lại khi đổi sáng/tối. Có `aria-label` và bảng số liệu thay thế.
- **Luật cứng:**
  - Không `style="`, không màu ngoài `tokens.css`, không thư viện mới.
  - Chữ mới qua `L[...]` và có bản dịch trong `SharedResource.en.resx`.
  - Giữ hook `data-*`/chuỗi mà test tích hợp đọc.
- **Chạy app:**
  - Chạy app riêng cổng 5012 (`ASPNETCORE_ENVIRONMENT=Development dotnet run --project <đường dẫn tuyệt đối>/HomestayBooking.Web --no-launch-profile --urls http://localhost:5012`). Không tắt app cổng 5011.
  - Dừng app của mình bằng đúng PID. Không dùng `pkill -f`/`killall` với mẫu ngắn.
  - SQL Server ở container Docker `sql-homestay`.
- **Tài khoản thử** (mật khẩu `Fuji@2026`): khách `0987000001`, chủ `0912000001`, `NV001`, `NV002`, `KT001`, `QL001`, `AD001`. Nhân viên đăng nhập ở `/Account/EmployeeLogin`.
- **Test nền:** 1597 pass + 1 skip.
- **Kiểm tra chung trước khi báo xong:**
  - `dotnet build -c Release -p:UseSharedCompilation=false` không cảnh báo mới;
  - `dotnet test HomestayBooking.sln -c Release --no-build` pass;
  - `python3 -I tools/ui-crawl.py http://localhost:5012` không có trang 500 (4 trang 404 đã biết: `/Bookings/Create` thiếu `roomTypeId`, 3 thẻ mẫu ở UI Kit);
  - `rg` không có `style="`, không có mã màu ngoài `tokens.css`;
  - chạy `web-design-guidelines` trên view mới.
- **Dữ liệu thử:** đơn `FJ261019-001` (bước 04) **giữ lại để demo**, không hủy. Dữ liệu thử mới tạo phải ghi vào báo cáo.
- **Báo cáo:** viết `docs/ui/<bước>-report.md` (ví dụ `05a-chu-homestay-report.md`), có mục "Cần người dùng quyết định".

## Luật controller (áp dụng cho mọi bước 5)

- Controller mới đặt trong area tương ứng, mỏng: `[Authorize]` đúng policy (`AppPolicies`/`AppRoles`) → gọi service → map ra ViewModel → View.
- POST dùng mẫu PRG + `TempData` cho `_Alerts`, có `[ValidateAntiForgeryToken]`.
- **Đọc method public của service trước khi nối.** Không sửa `Services/`, `Data/`, `Models/Entities/`. Thiếu dữ liệu thì ghi vào `backend-gaps.md` rồi bỏ qua màn đó.
- Kiểm tra quyền sở hữu theo đúng cách service/`HomestayAccessService` đang làm: chủ chỉ thấy homestay của mình.
- Thêm mục vào sidebar (`SidebarLink`) cho màn mới.
- Mỗi controller mới có test cho đường thành công + đường bị chặn quyền, theo style test hiện có.

## Các màn (area `Owner`, ưu tiên điện thoại)

1. **Homestay của tôi (FR-13):** danh sách từ `HomestayContentService.OwnerHomestaysAsync`, có trạng thái duyệt (`<status-badge>`). Trang sửa đổ dữ liệu từ `HomestayContentService.EditorAsync` (B8), chia `_Tabs`:
   - Thông tin, mô tả: `HomestayContentService`.
   - Phòng, loại phòng, giá 3 mức ngày thường/cuối tuần/lễ: `RoomService`.
   - Ảnh: `HomestayImageService`, `_UploadZone`, chọn ảnh bìa, xóa ảnh có xác nhận.
   - Tiện nghi.
   - Chính sách hủy: `HomestayPolicyService`.
   - Giấy tờ: `HomestayDocumentService`.
   - Theo quyết định 16/10: giá/phòng/ảnh/mô tả sửa là áp dụng ngay. Riêng địa chỉ và giấy tờ pháp lý đưa homestay về chờ duyệt. Form phải báo trước điều này ở hai tab đó, và sau khi lưu hiện "Đang chờ Quản lý duyệt".
2. **Lịch phòng (FR-14):** `_RoomCalendar` + `RoomBlockService.CalendarAsync/CreateAsync/UnlockAsync`. **Khóa ngày cho khách từ kênh khác tối đa 3 thao tác:** chọn phòng → chọn khoảng ngày → xác nhận. Mở khóa tương tự.
3. **Lịch nhận–trả (FR-16):** `OwnerScheduleService.ReadAsync(actor)` (overload B8). Danh sách theo ngày (Hôm nay / Ngày mai / 7 ngày tới), mỗi dòng có tên khách, SĐT (theo `BookingContactVisibility`), phòng, số đêm.
4. **Đối soát (FR-16):** `SettlementService.ListAsync/DetailsAsync` phía chủ. Danh sách kỳ + trạng thái; chi tiết gồm các đơn, hoa hồng, khoản bù, số được nhận (`<money>`).
5. **Đánh giá (FR-06):** danh sách đánh giá homestay của mình, trả lời bằng `ReviewService.ReplyAsync`.
6. **Thông báo (FR-15):** chuông và trang `/Notifications` dùng chung đã làm ở bước 02. Chỉ rà lại trên điện thoại cho chủ và bảo đảm link trong thông báo của chủ mở đúng màn mới của bước này.
7. **Dashboard Owner:** bổ sung lối tắt tới các màn trên, đơn chờ xác nhận, khách đến hôm nay. Bước 04 ghi lại là dashboard chưa có **số đơn chờ xác nhận** vì model chưa có: lấy số này bằng service đã có (ví dụ `OwnerScheduleService` hoặc service danh sách đơn của chủ), không tự truy vấn DbContext trong controller.

## Kết quả bàn giao

- Commit theo màn: `feat(owner): ...`. Mỗi commit đi qua vòng build → test → chụp → sửa.
- Ảnh `playwright-cli` mỗi màn ở **375** và 1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập `0912000001` / `Fuji@2026` trên viewport 375: sửa giá một phòng, tải 1 ảnh, khóa rồi mở 2 đêm, xem đối soát, trả lời 1 đánh giá.
- Thử mở URL homestay của chủ khác: phải bị chặn.

## Báo cáo rồi dừng

Báo cáo màn đã làm, service đã nối, gap đã ghi, số thao tác khóa ngày đo được.
