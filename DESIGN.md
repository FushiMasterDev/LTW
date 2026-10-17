---
version: alpha
name: Fuji-Homestay
description: Hệ thiết kế của Fuji Homestay. Nền ivory ấm, xanh rêu là màu chính duy nhất, chữ charcoal, có chế độ sáng/tối. Trang khách đặt phòng theo tinh thần marketplace lưu trú (ảnh dẫn dắt, thanh tìm kiếm ngày/khách, thẻ homestay ảnh lớn); trang Admin/Nhân viên/Chủ/Kế toán là dashboard yên tĩnh, đọc dữ liệu nhanh.

colors:
  bg: "#f8f6f1"
  surface: "#ffffff"
  surface-2: "#f2efe8"
  surface-3: "#e9e5dc"
  border: "#e4dfd5"
  border-strong: "#cfc8ba"
  ink: "#1e2320"
  muted: "#59615c"
  primary: "#2f5d50"
  primary-hover: "#244a40"
  on-primary: "#ffffff"
  primary-soft: "#e3ece5"
  on-primary-soft: "#24473d"
  focus: "#b7791f"
  success: "#2e7d4f"
  warning: "#a15c00"
  danger: "#b42318"
  info: "#2f6690"

colors-dark:
  bg: "#121614"
  surface: "#1a1f1c"
  surface-2: "#222824"
  border: "#2e3631"
  ink: "#e7ebe8"
  muted: "#a2aba5"
  primary: "#8dbfa8"
  on-primary: "#10241c"
  focus: "#e0b45c"

typography:
  sans: "'Be Vietnam Pro', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
  serif: "'Lora', Georgia, 'Times New Roman', serif"
  weights: [400, 500, 600, 700]

rounded:
  sm: 0.5rem   # nút, ô nhập
  md: 0.75rem  # thẻ, bảng
  lg: 1rem     # banner, ảnh bìa
  full: 9999px # thanh tìm kiếm, chip, nút icon tròn

spacing:
  base: 8px
  section: 64px
---

# Fuji Homestay – DESIGN.md

## Nguồn sự thật

- Token nằm ở `HomestayBooking.Web/wwwroot/css/tokens.css`. Front matter trên chỉ là bản chép để đọc. Khi hai bên lệch nhau, **`tokens.css` đúng**, sửa lại file này.
- Luôn dùng biến `--fx-*` (hoặc biến `--bs-*` đã ánh xạ). Không viết mã màu hex trong view hay CSS mới.
- Màu trạng thái (đơn, giao dịch, hồ sơ…) dùng class `.tone-*` do `Extensions/*StatusExtensions.cs` trả về. Không tự đặt thêm màu cho trạng thái.
- Stack: Razor + Tag Helpers, Bootstrap 5.3, Bootstrap Icons, jQuery Validation Unobtrusive, Flatpickr, Chart.js. Không thêm framework CSS/JS khác.
- `docs/design/airbnb-reference.md` chỉ là **tài liệu tham khảo** về cách bố trí component. Không lấy màu, font hay tên thương hiệu từ file đó.

## Tinh thần

- **Một màu nhấn:** xanh rêu chỉ dùng cho nút chính, link, mục đang chọn. Khoảng 90% màn hình là ivory/trắng + chữ charcoal.
- **Ảnh dẫn dắt ở trang khách:** ảnh homestay gánh phần thị giác. Tiêu đề vừa phải, không to và đậm kiểu SaaS.
- **Yên tĩnh ở dashboard:** không gradient, không tô mỗi module một màu. Icon không mang nghĩa trạng thái thì dùng chung tông sage (`primary-soft`).
- **Mềm, không góc nhọn:** mọi phần tử tương tác đều bo theo thang `rounded`.

## Chữ

- Sans (Be Vietnam Pro) cho mọi chữ giao diện. Font này hiển thị dấu tiếng Việt chuẩn, nên **không đổi sang Inter hay Circular**.
- Serif (Lora 500/600) chỉ dùng cho tiêu đề lớn ở trang khách (`.site-public` h1/h2, banner, tên homestay). Dashboard dùng sans hoàn toàn.
- Đoạn văn dài: tối đa khoảng 40rem mỗi dòng.
- Số tiền, ngày, mã đơn: dùng `font-variant-numeric: tabular-nums` trong bảng và ô tóm tắt.
- Điểm đánh giá hiển thị bằng màu chữ `ink`, không dùng sao vàng. Riêng trang chi tiết được phép cho con số điểm to nhất trang.

## Bề mặt và độ nổi

- Chỉ có hai mức bóng: `--fx-shadow` (thẻ, dropdown) và `--fx-shadow-raised` (modal, thẻ khi hover). Không thêm mức bóng khác.
- Thứ tự nền: `bg` < `surface` (thẻ) < `surface-2` (đầu bảng, ô phụ). Viền 1px bằng `border`.
- Lớp phủ modal dùng `--fx-overlay`.

## Component – trang khách (`_Layout`, `layout-public.css`)

- **Header:** mục đang xem có một gạch dưới xanh rêu, không đổi màu nền.
- **Thanh tìm kiếm** (`_SearchBar`): bo tròn hoàn toàn, chia ô Khu vực / Ngày nhận–trả / Số khách bằng viền mảnh, kết thúc bằng nút tìm màu chính. Ở trang chủ, thanh này đè lên mép dưới banner. Chọn ngày bằng Flatpickr, tính theo đêm.
- **Thẻ homestay:** ảnh ở trên, khung tỉ lệ cố định (bo `md`). Dưới ảnh là 3–4 dòng: tên, khu vực, giá/đêm, điểm đánh giá. Hover thì nâng nhẹ và phóng ảnh trong khung. Không có ảnh hoặc ảnh lỗi thì hiện "Chưa có ảnh".
- **Chi tiết homestay:** desktop dùng lưới ảnh, điện thoại dùng dải ảnh vuốt ngang. Có modal xem ảnh lớn. Ô báo giá/đặt phòng dính bên phải trên desktop.
- **Đặt phòng và đơn:** tiến trình đơn chỉ để đọc, không trông giống nút. Mã QR thanh toán luôn nằm trên nền trắng, kể cả ở chế độ tối.

## Component – dashboard (`_DashboardLayout`, `layout-dashboard.css`)

- Sidebar rộng 16rem, thu gọn còn 4.5rem. Topbar cao 4rem. Dưới 992px, sidebar thành ngăn kéo có lớp phủ.
- Đầu trang: tiêu đề yên tĩnh trên nền trang (`_PageHead`), không dùng dải gradient.
- Thẻ số liệu xếp dọc theo thứ tự icon → số → nhãn, để 6 thẻ chung một hàng vẫn đọc được.
- Bảng: đầu bảng nền `surface-2`, số căn phải, thao tác dòng dùng `_RowActions`, phân trang dùng `_Pager`. Danh sách rỗng dùng `_EmptyState`.
- Thông báo kết quả thao tác: dùng `_Alerts` (TempData).
- **Giao diện chủ homestay ưu tiên điện thoại:** khóa ngày phòng tối đa 3 thao tác, nút bấm cao tối thiểu 44px.
- Biểu đồ (Chart.js, trang Báo cáo/Kế toán): dùng màu chính và các màu `tone-*`, không dùng bảng màu mặc định của thư viện.

## Chuyển động (`motion.css`)

- Đường cong chung là `--fx-ease`. Thời lượng theo các mức đã có trong `motion.css` (0.06–0.45s), không đặt mức mới. Modal mở tổng khoảng 370ms.
- Ảnh mờ dần khi tải xong, khung đặt tỉ lệ sẵn nên không xô bố cục.
- `prefers-reduced-motion`: bỏ di chuyển và phóng to, chỉ giữ đổi màu, độ mờ, bóng.

## Khả năng truy cập

- Tương phản chữ ≥ 4.5:1 ở cả hai chế độ. Token hiện có đã đạt, màu mới phải kiểm tra lại.
- Viền focus dùng `--fx-focus` (màu hổ phách), hiện rõ khi dùng bàn phím. Không xóa outline.
- Mọi ô nhập có `<label>`. Lỗi validation hiện ngay dưới ô bằng màu `danger`.
- Ảnh có `alt`. Nút chỉ có icon thì có `aria-label`.
- Hai ngôn ngữ (vi-VN mặc định, en-US): chữ giao diện lấy từ resource, không viết cứng trong view.

## Không làm

- Không thêm màu nhấn thứ hai, không dùng gradient trang trí, không dùng emoji làm icon (dùng Bootstrap Icons).
- Không lồng thẻ trong thẻ trong thẻ.
- Không thêm chế độ chỉ sáng hoặc chỉ tối. Mọi màn phải chạy được cả hai.
- Không dùng tên, logo hay màu Rausch của Airbnb.
