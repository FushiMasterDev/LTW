# Bàn giao bước 03: trang dành cho khách

Ngày: 18/10/2026. Phạm vi: `docs/prompts/ui/03-trang-khach.md`, chạy bằng Claude Code. Dừng trước bước 04.

## Commit

| Commit | Nội dung |
|---|---|
| `2cacb68` feat(ui): trang chủ, tìm kiếm và chi tiết homestay | Home, Homestays/Index, Homestays/Details, `BlockedDates`, banner mới |
| `20aca7f` feat(ui): đặt phòng, tra cứu và đổi/hủy đơn | Bookings/Create, Details, Lookup, Mine, Change, `_CustomerChanges`; sửa `_PriceBreakdown` |
| `f82086d` feat(ui): đánh giá, đăng ký đối tác và tài khoản | Đánh giá FR-06, `PartnerController` FR-25, trang tài khoản và trang lỗi |
| (commit cuối) docs(ui): báo cáo bước 03 | Báo cáo, quyết định, chỉnh nhỏ sau kiểm tra cuối |

## Trang đã làm

| Trang | Component ghép | Ghi chú |
|---|---|---|
| `Home/Index` | Banner ảnh + `_SearchBar` hero đè mép, khu vực theo `AreaGroupExtensions`, `_HomestayCard`, 3 bước (`<ol>` có số), cam kết, lời mời đối tác | `AreaSummary` thêm `Group` |
| `Homestays/Index` | `_SearchBar` compact, `_FilterBar` (tên, giá từ/đến, loại phòng, sắp xếp `HomestaySort`, chip tiện nghi), lưới `_HomestayCard`, `_Pager`, `_EmptyState`, `_Alert` khi bộ lọc sai | Bộ lọc sai không còn 500 |
| `Homestays/Details` | `_Breadcrumb`, `_Gallery`, `_BookingPanel` sticky, danh sách loại phòng còn trống, tiện nghi, đánh giá (`ReviewService.ListAsync`, điểm tổng lớn nhất trang), `_PriceBreakdown` chính sách hủy | Không hiện địa chỉ (BR-13); `BlockedDates` khi homestay có một loại phòng |
| `Bookings/Create` | `_DateRangePicker` (khóa đêm hết phòng), `_GuestPicker`, `form-field`, `_PriceBreakdown`, `_Alert` | Chạy khi chưa đăng nhập; voucher chỉ khi đăng nhập; script báo giá → `booking-quote.js` |
| `Bookings/Details` | `_BookingStepper`, `_Alert`, `_PaymentQr`, `_UploadZone`, `_DescriptionList`, `_PriceBreakdown`, `_Timeline`, `_RatingInput` | Hủy đơn có xem trước tiền hoàn; đánh giá khi đơn hoàn thành |
| `Bookings/Lookup` | `form-field`, khung `form-panel` | |
| `Bookings/Mine` | `_Tabs` (Sắp tới / Đã qua / Đã hủy), `_BookingCard` mới, `_EmptyState`, `_Pager` | `Mine(page, tab)` |
| `Bookings/Change`, `_CustomerChanges` | `_PageHead`, `_DescriptionList`, `_PriceBreakdown`, `_Timeline`, `_PaymentQr`, `_UploadZone` | Giữ ô ngày native và hook test; dùng chung layout nhân viên |
| Đánh giá (FR-06) | `_RatingInput` mới | `Bookings/Review/{code}`: `CreateAsync` (đăng nhập) hoặc `CreateByLookupAsync` (tra cứu, nhập lại SĐT) |
| Đăng ký đối tác (FR-25) | `_Steps` mới, `form-field`, `_UploadZone`, `status-badge`, `_EmptyState` | `/Partner` → tạo hồ sơ + đăng nhập → `/Partner/Status` tải giấy tờ, gửi hồ sơ, xem lại giấy tờ |
| `Account/Login`, `EmployeeLogin`, `Register`, `ChangePassword`, `AccessDenied` | `_AuthAside` mới, `form-field`, `data-toggle-password` | Link qua lại hai cổng đăng nhập giữ nguyên |
| `Home/StatusCode`, `Shared/Error` | `_StatusPage` mới | Giữ `class="status-code"`, "Không tìm thấy trang" |

Component mới có mục trên `/Dev/UiKit`: `_BookingCard`, `_RatingInput`, `_Steps`, `_AuthAside`, `_StatusPage`; biến thể mới: `_FilterBar` kiểu chip, `_BookingPanel` có tiêu đề/gợi ý/ghi chú, `<rating single="true">`.

## Năm việc còn treo từ review 02x

| Việc | Trạng thái |
|---|---|
| 1. Ô ngày của `_SearchBar` trống | Xong: placeholder "Chọn ngày" / "Select dates" trong `_DateRangePicker`, hiện ở cả UI Kit |
| 2. "Tất cả khu vực" bị mũi tên select che ở ~1000px | Xong: `.fx-search .form-select` chừa `padding-inline-end: 1.75rem` |
| 3. Giảm chuyển động | Xong: không thêm chuyển động không gian mới; intro trang chủ đổi selector và vẫn nằm trong khối `prefers-reduced-motion` của `motion.css`; hover mới chỉ đổi màu nền |
| 4. `inspect.mjs` trên Home cũ | Xong: `headline-lines`, `eyebrow`, `shadow`, `outlined-box` của Home cũ hết; cờ còn lại có lý do ở `decisions.md` (xem bên dưới) |
| 5. `DateTime.Today` trong view | Xong: `Home`, `Homestays/Index`, `Homestays/Details`, `Bookings/Create` không còn `DateTime.Today`; ngày lấy qua `VietnamTime.Today(TimeProvider)` |

## Ảnh

- Banner trang chủ: `images/hero-hoan-kiem.jpg` (+ `-sm`), hồ Hoàn Kiếm, public domain, Wikimedia Commons, tác giả vi:User:Prof MK. Thay ảnh cũ `hero-wooden-cabin` (CC BY 2.0, trái luật CC0/public domain) và đã xóa ảnh cũ.
- Cột ảnh đăng nhập: `images/auth-veranda.webp`, CC0, Joshua Ness (StockSnap), chép từ bộ ảnh mẫu.
- Tổng 2/4 ảnh trang trí. Nguồn ghi ở `docs/ui/credits.md`. Ảnh homestay vẫn lấy từ dữ liệu.

## Kết quả `inspect.mjs` (1280/768/375)

| Trang | Lượt đầu | Sau sửa | Cờ còn lại (lý do ở `decisions.md`) |
|---|---|---|---|
| `/` | 1 FAIL, 7 WARN | 1 FAIL, 5 WARN | `hairline` (chỉ còn viền header dính và viền trên footer ở chế độ tối); WARN `outlined-box` (pill tìm kiếm), `size-count`, `no-footage`, `still-page`, `first-action` 768 |
| `/Homestays` | 2 FAIL, 4 WARN | 2 FAIL, 4 WARN | `hairline` (header/footer), `focal-point` (trang tra cứu không có hero); WARN `outlined-box`, `no-footage`, `still-page`, `side-scroll` 375 (ngăn kéo lọc) |
| `/Homestays/Details/6` | 3 FAIL, 6 WARN | 3 FAIL, 6 WARN | `hairline` (header, dòng `_PriceBreakdown`), `focal-point` (dẫn bằng gallery), `still-page`; WARN `measure` đã sửa, còn `size-count`, `no-footage`, `first-action`, `side-scroll` (dải ảnh vuốt) |
| `/Bookings/Create` | 4 FAIL, 3 WARN | như cũ | `hairline`, `few-pictures`, `focal-point`, `still-page`: trang form giao dịch |
| `/Bookings/Lookup` | 5 FAIL, 1 WARN | 4 FAIL, 1 WARN | `shadow` hết sau khi bỏ autofocus; còn `hairline`, `few-pictures`, `focal-point`, `still-page`; WARN `no-footage` |

Đã sửa theo inspector: bỏ đường kẻ ở danh sách khu vực, danh sách loại phòng và footer; chữ nguồn ảnh ≥ 13px và có nền tối; giới hạn độ dài dòng; bỏ khung quanh nút "Bộ lọc" trên điện thoại; bỏ autofocus ở tra cứu. Không tuyên bố inspector thô sạch: các FAIL còn lại là ngoại lệ đã ghi.

Vòng nhìn ảnh: lượt 1 thấy chữ nguồn ảnh trắng trên trời sáng, 6 homestay nổi bật lẻ 2 thẻ, "Không giới hạn" bị cắt trong ô giá, chú thích "Tiện nghi" quá to, điểm của từng đánh giá hiện "Chưa có đánh giá"; lượt 2 sửa xong cả năm, thêm cột tiền co theo nội dung trong ô báo giá, ẩn khối "Yêu cầu đổi" rỗng.

## Class legacy đã xóa

Từ `layout-public.css`: `hero-photo`, `hero-copy`, `hero-credit`, `hero-search`, `hero-search-note`, `home-section` (viết lại), `section-head`, `promise-list`, `area-grid`, `area-tile`, `steps`, `step-number`, `search-summary`, `stay-heading`, `stay-facts`, `room-offer`, `room-quote`, `price-table`, `amenity-list`, `booking-panel`, `form-section`, `night-list`, `summary-row`, `summary-total`, `order-head`, `order-progress`, `pay-card`, `pay-steps`, `receipt-box`, `dev-box`, `auth-side`, `narrow-card`, `site-footer-rule`. `auth-card`, `auth-form`, `status-page`, `status-code` viết lại (CSS của `_AuthAside`/`_StatusPage` chuyển sang `components.css`). Quét lại: không còn class nào trong `layout-public.css`/`components.css` mà view, JS hay tag helper không dùng (trừ class do Flatpickr, validation MVC, Bootstrap sinh ra).

## Kiểm tra

- `dotnet build -c Release -p:UseSharedCompilation=false`: 0 lỗi, 0 cảnh báo.
- `dotnet test HomestayBooking.sln -c Release --no-build`: **1595 pass + 1 skip cũ, 0 fail** (mốc đầu 1589 + 1). Test mới: `BlockedDates` (200/400), bộ lọc sai không 500, `rating single`, đánh giá qua trang đơn (`GuestReviewPageTests`), đăng ký đối tác và chặn nhân viên (`PartnerRegistrationPageTests`).
- Mã hex ngoài `tokens.css`: **18 → 0** (thêm token `--fx-footer-*`, `--fx-photo-fallback`, `--fx-scrim`). `style="` trong Views/Areas: 0.
- Khóa dịch: script kiểm `L["..."]` trong các view đã sửa: 0 thiếu, 0 trùng (thêm khoảng 150 khóa EN, gồm cả 2 khóa thiếu từ trước: "Áp dụng", "Thông báo").
- `tools/ui-crawl.py`: 840 trang theo 7 vai trò, không có 500. 4 lỗi 404: `/Bookings/Create` thiếu `roomTypeId` (đã biết) và 3 link thẻ đơn mẫu trên UI Kit (mã demo, cố ý).
- web-design-guidelines: rà các view đã sửa; sửa autofocus ở tra cứu, thêm `translate="no"` cho tên thương hiệu, nhãn cho ô lý do hủy. Giữ chữ hoa đầu câu kiểu tiếng Việt thay cho Title Case.
- Luồng trên app Development cổng 5012:
  - Chưa đăng nhập: trang chủ → chọn ngày bằng Flatpickr → tìm → chi tiết → đặt (`FJ261018-001`, SĐT 0988123456) → đơn có QR → tra cứu lại bằng mã + SĐT. Đơn này tự hết hạn giữ phòng sau 15 phút nên bước hủy chỉ kiểm trên trang (nút hủy có hộp xác nhận); không bấm hủy thật.
  - `0987000001`: Đơn của tôi (3 tab) → `FJ261011-010` → đổi ngày, xem báo giá → gửi yêu cầu (tạo CR6, chờ quản lý duyệt).
  - Đối tác: SĐT mới 0977123456 tạo hồ sơ #1002, tải 3 giấy tờ, gửi hồ sơ.
- Ảnh (không commit) ở `/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/d5b5bcfe-95ef-4718-b0eb-3298b33edca2/scratchpad/shots/final/`: 7 trang của luồng × 375/768/1280 × sáng/tối, cộng bộ tiếng Anh 1280 (`final-en-*`), ảnh gửi yêu cầu đổi. Không trang nào tràn ngang ở 375/768.

## Dữ liệu đã ghi vào database Development (v5)

Luồng kiểm tra chạy trên database Development như lệnh trong prompt, nên đã tạo: đơn `FJ261018-001` (khách 0988123456, đã hết hạn giữ phòng), yêu cầu đổi CR6 của `FJ261011-010` (Pending), hồ sơ đối tác #1002 (chủ 0977123456, PendingReview, 3 file giấy tờ). Xóa hoặc giữ tùy bạn.

## Lệch DESIGN.md

- Không lệch màu, font, bo góc; không thêm thư viện. Token mới chỉ thay mã hex cũ (footer, nền ảnh, lớp phủ).
- Lớp phủ dải chuyển trung tính dưới chữ trên ảnh (banner, cột ảnh đăng nhập): ngoại lệ duy nhất của house style, đã có từ trước.
- Ba bước có số thứ tự: theo prompt, trái house rule "no index numbering".
- Thẻ loại phòng, ô tóm tắt, thẻ đơn dùng nền `surface` không viền thay cho `card-soft` có viền; viền vẫn giữ ở form, bảng giá, header (DESIGN.md).

## Còn treo / cần bạn quyết

- Chủ chưa có màn sửa hồ sơ nháp (service có `UpdateDraftAsync`); form chỉ tạo mới. Có thể làm ở bước 05a.
- Form đối tác không hỏi số phòng vì `OwnerRegistrationInput` không có trường này.
