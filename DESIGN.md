---
version: 03y
name: Fuji-Homestay
description: Lưu trú Hà Nội, ảnh dẫn dắt, ivory ấm, xanh rừng và cam đất; hai chế độ màu.
typography:
  sans: Manrope 400–700
  display: Fraunces opsz 9–144, 400/500/600
rounded:
  input: 0.875rem
  card: 1.5rem
  button: 9999px
spacing:
  section: 96px
  max-width: 80rem
---

# Fuji Homestay

## Nguồn sự thật

`HomestayBooking.Web/wwwroot/css/tokens.css` là nguồn chuẩn cho màu, font, bo góc, bóng và chuyển động. Không viết màu ngoài file này. Tên `--fx-*` và `.tone-*` giữ nguyên để component và mọi view cũ tiếp tục hoạt động. Bootstrap ánh xạ theo token. Stack Razor, Bootstrap, Icons, Flatpickr, Chart.js, JavaScript thuần; không thêm thư viện.

Bước 03y ngày 18/10/2026 thay nhận diện cũ theo bản mẫu đã được người dùng chốt. Thương hiệu luôn là **Fuji Homestay**, có `translate="no"`; bản mẫu chỉ cung cấp hướng thị giác. Không sao chép ảnh hay số liệu của bản mẫu.

## Tinh thần

Ảnh thật của Hà Nội và homestay mở câu chuyện. Nền ivory và bề mặt giấy ấm giữ phần đọc yên tĩnh; xanh rừng cho thương hiệu, link, khối tối và lựa chọn; cam đất cho hành động chính và chữ nhấn. Không biến trạng thái nghiệp vụ thành màu trang trí. Dashboard ưu tiên đọc dữ liệu, chỉ đổi khung ở bước 03y.

Chế độ tối dùng nền xanh than ấm, surface sáng hơn nền, chữ ivory và cam sáng với chữ tối. Mọi cặp chữ/nền cần đạt 4.5:1. Cam nút ở chế độ sáng tối hơn bản mẫu để dùng chữ trắng; màu chữ nhấn trên ảnh dùng token riêng để đạt tương phản trên lớp phủ tối.

## Chữ

Manrope 400–700 cho giao diện. Fraunces 400/500/600, tracking -.02em cho h1–h3 trang khách, tiêu đề trang và số liệu StatCard. Tên thẻ homestay dùng sans đậm để đọc nhanh. Hai font có bộ dấu tiếng Việt, kiểm chuỗi `ỗ ữ ặ ậ ỡ` trong UI Kit ở mọi cỡ và hai chế độ.

Hero tối đa hai dòng ở mọi độ rộng. Đoạn văn tối đa khoảng 40rem. Tiền, ngày và số liệu dùng tabular-nums. Chữ mới đi qua `L[...]` và có resource EN; không dịch dữ liệu người dùng.

## Bề mặt

Thẻ/ảnh bo 1.5rem, input 0.75–1rem, nút/chip/điều hướng dạng viên. Viền mảnh theo token; bóng soft ở thẻ, lift ở hover, menu và modal. Glass blur 14px cho thanh tìm trong hero, nhãn ảnh và menu điện thoại. QR luôn trên `--fx-paper` trắng cố định.

Khung khách tối đa 80rem, lề 1.25rem điện thoại và 2rem từ md. Section khoảng 6rem. Lớp phủ trung tính trên ảnh giúp đọc chữ; gradient trang trí chỉ dùng cho vệt sáng tương tác trên thẻ khu vực.

## Component

Mọi mẫu dùng chung phải có partial/Tag Helper và xuất hiện trong `/Dev/UiKit` trước khi vào trang. Giữ nguyên model, form binding, antiforgery, route, validation và hook `data-*`.

- Header trang chủ trong suốt trên ảnh, chuyển sang kính khi qua mốc hero bằng IntersectionObserver; các trang khác dùng nền đặc. Footer xanh rừng chia cột, giữ link.
- SearchBar hero là viên kính trong ảnh, chỉ báo focus trượt theo ô; compact/điện thoại giữ cấu trúc thực dụng và ngày native khi thiếu JS.
- HomestayCard ảnh 4:3, nhãn kính trái, tiêu đề và sao cam cùng hàng, ghim khu vực và giá thật. Không nút yêu thích.
- Gallery desktop một ảnh lớn trái, hai ảnh phải; ảnh tiếp theo trong lightbox. Điện thoại vuốt và có bàn phím/nút mở.
- BookingPanel dính desktop, phòng chọn có viền xanh và dấu kiểm, tiện nghi chip, đánh giá có avatar chữ cái.
- Alert, Tabs, EmptyState, Steps, RatingInput, BookingCard, StatusPage, AuthAside, BookingStepper, PaymentQr, StatCard, PageHead, DataTable và FilterBar chung token bề mặt, viền và góc.
- Dashboard sidebar xanh đậm, lựa chọn viên cam, topbar sáng; auth hai cột ảnh/form. Không sửa nghiệp vụ hoặc làm bước 04.

## Chuyển động

Dùng easing `--fx-ease-out`, thời lượng `--fx-dur-1` … `--fx-dur-6` (150/250/350/500/800/1200ms), stagger 70ms, perspective 1000px. Phản hồi form/nút vẫn ngắn; vào trang 700ms, reveal 800ms, Ken Burns 20s. Chỉ animate transform/opacity, kính mờ khi cần; không animate bố cục, không `transition: all`.

Trang khách có chiều sâu: hero ba lớp nghiêng tối đa 3°, ảnh Ken Burns/parallax; thẻ khu vực 8°, thẻ homestay 4°, reveal một lần; bước đặt phòng tách từ chồng 3D, cam kết lật nhẹ, khối đối tác mở theo cuộn, FLIP lightbox. Pointermove gộp rAF, observer dùng chung, will-change chỉ khi tương tác; dừng vòng lặp khi ẩn tab/ra khỏi hero.

Dashboard chỉ stagger thẻ, count-up 800ms giữ giá trị cuối trong HTML, sidebar trượt viên chọn, Chart.js 800ms. Không 3D/parallax ở dashboard.

Không JS vẫn thấy nội dung. Ẩn reveal chỉ khi script head và lớp khởi tạo observer sẵn sàng. `prefers-reduced-motion` tắt mọi chuyển động không gian, reveal hiện ngay, chỉ giữ opacity/đổi màu ≤200ms. Touch không nghiêng, parallax và chồng thẻ nhẹ hơn. Các nhóm motion có fallback tương ứng trong `motion.css`.

## Không làm

Không bịa số liệu, đánh giá, phí, thuế, đối tác thanh toán hoặc cam kết dịch vụ. Không thêm ảnh stock vào thẻ homestay. Ảnh trang trí CC0/public domain tối đa bốn ảnh, lưu local và ghi credits. Không inline style trong markup, không màu ngoài tokens, không thư viện mới. Không đổi Services, Data, Entities, migration hoặc database. Controller chỉ được thêm ảnh khu vực vào truy vấn đã có. Không tắt app người dùng cổng 5011.
