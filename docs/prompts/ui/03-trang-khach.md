# Bước 3 – Trang dành cho khách

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/design/airbnb-reference.md` (chỉ lấy bố cục), `docs/ui/inventory.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit` để dùng lại component đã có.

Skill: `design-taste-frontend` (chạy đủ vòng chụp → sửa), `ui-animation`, `full-output-enforcement`. Nếu chạy trong Codex có thể dùng thêm `image-to-code`, nhưng ảnh thiết kế phải theo `DESIGN.md` và `tokens.css`.

Giới hạn khi dùng skill (như bước 02x): chỉ dùng house style, `scripts/inspect.mjs` và vòng chụp → sửa của `design-taste-frontend`. **Không** chạy `draw.mjs`, không lấy hướng/bảng màu/font/hero mà skill "chia bài": nhận diện đã chốt ở `DESIGN.md` + `tokens.css`. Chuyển động chỉ dùng thời lượng có trong `motion.css` (0.06–0.45s) và `--fx-ease`.

## Hiện trạng sau bước 02 và 02x (commit 8a48445) – dùng lại, không làm lại

- **Component** (xem `/Dev/UiKit`, báo cáo ở `docs/ui/02-component-report.md` và `docs/ui/02x-component-report.md`):
  - Hiển thị: `<rating>`, `_StatCard`, `_DataTable`, `_DescriptionList`, `_Timeline`, `_PriceBreakdown`, `_EmptyState`, `_Skeleton`.
  - Form và phản hồi: `_FilterBar`, `_DateRangePicker`, `_GuestPicker`, `_UploadZone`, modal `data-confirm` (trên form hoặc nút submit), `_Alerts` (`Success`/`Error`/`Info`), `window.fxToast`.
  - Điều hướng: `_Tabs`, `_Breadcrumb`, `_PageHead`, `NotificationBell`, trang `/Notifications`.
  - Đặt phòng: `_SearchBar` (`hero`/`compact`), `_HomestayCard`, `_Gallery`, `_BookingPanel`, `_BookingStepper`, `_PaymentQr`, `_RoomCalendar`.
  - Model ở `Models/ViewModels/Ui/`.
- **Partial đã xóa:** `_ChangeHistory`, `_ChangeQuote`, `_PolicyRules`, `_AccountingTabs`. Chính sách hủy dùng `<partial name="_PriceBreakdown" model="new PriceBreakdownModel(Rules: ...)" />`.
- **Đã gắn vào trang thật:** `_HomestayCard` (Home, Homestays), `_Gallery` (Homestays/Details), `_PaymentQr` + `_Timeline` (Bookings/Details, `_CustomerChanges`), `_PriceBreakdown` (Change, Homestays/Details).
- **Chưa gắn, bước này gắn:** `_SearchBar`, `_BookingPanel`, `_BookingStepper`, `_DateRangePicker`, `_GuestPicker`, `_UploadZone`, `_FilterBar`, `_DescriptionList`.
- **Script theo trang:** hiện Flatpickr chỉ nạp ở UI Kit. Trang nào dùng `_DateRangePicker` thì thêm vào `@section Head` dòng `~/lib/flatpickr/flatpickr.min.css`, và vào `@section Scripts` (đúng thứ tự): `~/lib/flatpickr/flatpickr.min.js`, `~/lib/flatpickr/l10n/vn.min.js`, `~/js/components/date-range-picker.js`. `guest-picker.js`, `upload-zone.js`, `payment-qr.js` cũng chỉ nạp ở trang cần. Xem cách UI Kit nạp ở cuối `Views/Dev/UiKit.cshtml`.
- **Số liệu nền:**
  - Test: 1589 pass + 1 skip.
  - Mã hex ngoài `tokens.css`: 18, đều ở `layout-public.css`. Bước này phải đưa về 0.
  - `style="` hiện là 0.

## Việc còn treo từ review 02x (làm trong bước này)

1. **Ô ngày của `_SearchBar` trống trơn khi chưa chọn:** ô không viền nên khách không biết bấm vào đâu. Thêm placeholder (ví dụ "Chọn ngày", có bản dịch EN) cho input Flatpickr trong `_DateRangePicker`. Sửa ở component, cập nhật mục UI Kit.
2. **Ở khoảng 1000px, chữ "Tất cả khu vực" trong `_SearchBar` bị mũi tên của select che:** sửa padding phải của select trong `.fx-search`.
3. **Giảm chuyển động:** `base.css` không còn luật chung tắt transition/animation. Mọi chuyển động mới (ví dụ hero, ảnh nền) phải tự có khối `@media (prefers-reduced-motion: reduce)` trong `motion.css`.
4. **`inspect.mjs` còn báo trên Home cũ:** `headline-lines` (tiêu đề Home trên điện thoại quá nhiều dòng), `eyebrow` (đoạn hướng dẫn trước số 2 bị nhận nhầm là eyebrow), `shadow`/`outlined-box`/`hairline`. Làm lại Home thì xử lý hết. Ngoại lệ nào giữ lại phải ghi vào `decisions.md`.
5. **Ngày "hôm nay" trong view:** `Home/Index`, `Homestays/Index`, `Homestays/Details`, `Bookings/Create` đang dùng `DateTime.Today`. Đổi sang `VietnamTime.Today(TimeProvider)` như `_DateRangePicker` (inject `TimeProvider`).

## Nguyên tắc

- View chỉ ghép component. Nếu cần mẫu UI mới, thêm vào thư viện component + UI Kit trước, rồi mới dùng.
- Giữ nguyên `@model`, tên trường form, `asp-action`/`asp-route-*`, antiforgery, validation.
- Ảnh homestay lấy từ dữ liệu. Ảnh trang trí tối đa 4 ảnh CC0/public domain, lưu `wwwroot/images/`, ghi nguồn vào `docs/ui/credits.md`.
- Tiêu đề lớn dùng serif (Lora). Còn lại dùng sans.

## Các trang

1. **`Home/Index`:** banner ảnh + `_SearchBar` biến thể `hero` đè lên mép dưới banner. Tiếp theo là khu vực nổi bật (link theo `AreaGroupExtensions`), homestay nổi bật (`_HomestayCard`), 3 bước đặt phòng có đánh số, cam kết ngắn, lời mời "Trở thành đối tác".
2. **`Homestays/Index`:** `_SearchBar` compact + `_FilterBar` (giá, loại phòng, số người, tiện nghi, sắp xếp `HomestaySort`). Danh mục loại phòng/tiện nghi lấy từ hàm lọc B8 thêm (G-06). Lưới `_HomestayCard`, `_Pager`, `_EmptyState` khi không có kết quả.
3. **`Homestays/Details`:** `_Gallery`, tên + khu vực + rating, tiện nghi, danh sách phòng/loại phòng còn trống theo ngày đã chọn. Bên phải là `_BookingPanel` sticky.
   - **Đêm hết phòng:** thêm action GET trả JSON trong `HomestaysController`, ví dụ `BlockedDates(int roomTypeId, DateOnly from, DateOnly to)`. Action gọi `AvailabilityService.BlockedDatesAsync(roomTypeId, from, to)` (tối đa 92 đêm, service tự kiểm), trả mảng `"yyyy-MM-dd"`. Truyền URL vào `DateRangePickerModel.BlockedUrl` để Flatpickr khóa các đêm này. Tham số sai hoặc quá 92 đêm thì trả 400, không để lỗi 500. Có test.
   - Chính sách hủy dùng `_PriceBreakdown` (`Rules:`). Đánh giá lấy từ `ReviewService.ListAsync(homestayId, page)`: danh sách + điểm tổng. Địa chỉ/bản đồ chỉ hiện theo đúng quy tắc `BookingContactVisibility`.
4. **`Bookings/Create`:** form đặt phòng bằng `form-field`, có `_PriceBreakdown`.
   - **Phải chạy khi chưa đăng nhập (FR-03):** khách nhập SĐT, không thấy ô voucher. Sau khi đặt, mã đơn vào session lookup.
   - Đã đăng nhập thì điền sẵn thông tin và cho dùng voucher.
5. **`Bookings/Details`:** `_BookingStepper`, `_DescriptionList`, `_PriceBreakdown`, `_PaymentQr` khi chờ cọc, tải ảnh bill (`_UploadZone`), `_Timeline`. Có nút đổi/hủy khi được phép.
6. **`Bookings/Lookup`:** form mã đơn + SĐT, gọn ở giữa trang.
7. **`Bookings/Mine`:** tabs Sắp tới / Đã qua / Đã hủy, mỗi đơn là một thẻ.
8. **`Bookings/Change`, `_CustomerChanges`:** chọn đổi ngày/loại phòng hoặc hủy, xem trước số tiền hoàn/phí bằng `_PriceBreakdown` trước khi gửi.
9. **Viết đánh giá (FR-06):** trong `Bookings/Details`, khi đơn đã trả phòng và `HasReview == false` (B8, G-09), hiện form điểm + nội dung. Đã đánh giá thì hiện lại đánh giá của khách. Gọi `ReviewService.CreateAsync` (đã đăng nhập) hoặc `CreateByLookupAsync` (tra cứu). Đọc service để biết điều kiện BR-09.
10. **Đăng ký đối tác (FR-25):** trang công khai "Trở thành đối tác". Chưa có controller công khai cho việc này. Tạo controller mỏng (không area), gọi `OwnerRegistrationService`:
   - `RegisterAsync(input, password)` tạo hồ sơ.
   - `UploadDocumentAsync(...)` nằm ở `OwnerRegistrationService.DocumentsAndSla.cs`.
   - `SubmitAsync(id, actor)` gửi hồ sơ.
   
   Đọc service để biết đúng thứ tự, quyền và tham số. Không sửa service. Form nhiều bước: thông tin cơ sở → số phòng/khu vực → liên hệ → ảnh/giấy tờ → gửi. Sau khi gửi, hiện trang trạng thái hồ sơ bằng `OwnerRegistrationQueryService.MineAsync`, có thể xem lại giấy tờ qua `OpenDocumentAsync`.
11. **`Account/Login`, `Register`, `EmployeeLogin`, `ChangePassword`, `AccessDenied`:** dùng `_AuthLayout`. Khách/chủ đăng nhập bằng SĐT, nhân viên bằng mã nhân viên (xem `fuij_homestay_login_design_v5.md`). Có link qua lại giữa hai trang đăng nhập, `data-toggle-password`.
12. **`Home/StatusCode`, `Shared/Error`.**

Sau khi chuyển xong, xóa class legacy mà các trang này không còn dùng.

## Kết quả bàn giao

- Commit theo nhóm:
  - `feat(ui): trang chủ, tìm kiếm và chi tiết homestay`
  - `feat(ui): đặt phòng, tra cứu và đổi/hủy đơn`
  - `feat(ui): đánh giá, đăng ký đối tác và tài khoản`
- `docs/ui/credits.md`.
- Ảnh `playwright-cli` của luồng: trang chủ → tìm → chi tiết → đặt (**chưa đăng nhập**) → chi tiết đơn có QR → tra cứu → gửi đổi/hủy. Chụp ở 375/768/1280 × sáng/tối, thêm 1 bộ tiếng Anh ở 1280.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đi trọn luồng trên bằng `playwright-cli` với khách chưa đăng nhập, rồi với `0987000001` / `Fuji@2026`.
- Chạy `web-design-guidelines` trên các view đã sửa và sửa hết lỗi mức cao.
- Không có `style="`. Mã hex ngoài `tokens.css` về **0** (báo trước/sau).
- Chạy `inspect.mjs` trên `/`, `/Homestays`, `/Homestays/Details/{id}`, `/Bookings/Create`, `/Bookings/Lookup` ở 375/768/1280. Không còn FAIL; ngoại lệ phải có lý do trong `decisions.md`.
- Chạy app riêng ở cổng 5012 như các bước trước, không tắt app 5011 của người dùng.
- Đăng nhập lần lượt `QL001`, `NV001`, `KT001`, `0912000001`, `0987000001`: không trang nào lỗi 500. Các trang nội bộ chưa chuyển vẫn phải hiển thị được sau khi xóa class legacy.

## Báo cáo rồi dừng

Báo cáo:
- Trang đã làm, và trạng thái 5 việc ở mục "Việc còn treo từ review 02x".
- Ảnh đã dùng.
- Kết quả `inspect.mjs`.
- Class legacy đã xóa.
- Chỗ nào lệch `DESIGN.md`.

**Không bắt đầu bước 4.**
