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

Ảnh thật của Hà Nội và homestay mở câu chuyện. Nền ivory và bề mặt giấy ấm giữ phần đọc yên tĩnh; xanh rừng cho thương hiệu, link, khối tối và lựa chọn; cam đất cho hành động chính và chữ nhấn. Không biến trạng thái nghiệp vụ thành màu trang trí. Dashboard ưu tiên đọc dữ liệu; bước 04 dùng cùng thư viện component cho nội dung các màn nội bộ hiện có.

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
- Dashboard sidebar xanh đậm, lựa chọn viên cam, topbar sáng; auth hai cột ảnh/form. Nội dung nội bộ dùng PageHead, FilterBar, DataTable/Pager, TaskList, DescriptionList và Timeline. StatCard đặt nhãn cạnh icon sage, số Fraunces phía dưới. Bảng thành thẻ có nhãn trên điện thoại; form một cột tối đa 40rem, nút cuối form dính dưới trên điện thoại. Chi tiết Owner đặt thao tác trước lịch sử trên 375px, nút tối thiểu 44px. Accounting dùng Tabs và ReceiptReview; chỉ hiển thị dữ liệu thật đã được controller nạp. Không thay nghiệp vụ hoặc mở màn bước 05.

## Chuyển động

Dùng easing `--fx-ease-out`, thời lượng `--fx-dur-1` … `--fx-dur-6` (150/250/350/500/800/1200ms), stagger 70ms và `--fx-perspective`. Phản hồi form/nút vẫn ngắn; vào trang 700ms, reveal một lần 800ms. Chỉ animate transform/opacity; không animate bố cục hay dùng `transition: all`.

Ảnh tự chuyển động theo thời gian: hero và cột đăng nhập dùng máy quay 3D scale 1.05–1.15, rotateX/Y ±1°, vòng `--fx-loop-slow` 24s đảo chiều. Khu vực từ ba thẻ dùng vòng 3D, điện thoại coverflow vuốt được; bộ bài ba bước tự xào; gallery lật lập phương mỗi `--fx-dwell` 4s; chồng tối đa ba ảnh homestay thật trôi lệch pha; icon cam kết xoay lệch nhịp. Lightbox giữ FLIP khi mở/đóng và chỉ lật lập phương khi người dùng chuyển ảnh. Không có chuyển động theo chuột hay theo tiến độ cuộn. Hover thẻ homestay chỉ nâng 4px, bóng lift và zoom ảnh 1.06.

`autoplay.js` dùng observer của FxMotion, một đồng hồ rAF chung, tối đa hai khối chạy trong màn hình. Theo quyết định 03y3 ngày 19/10/2026, không có nút dừng/tiếp tục; khối chỉ dừng bằng hover/focus, khi khuất màn hình, ẩn tab, modal mở hoặc reduce. Lựa chọn này khác khuyến nghị WCAG 2.2.2 về điều khiển dừng nội dung tự chuyển động. Băng chuyền/bộ bài/gallery có trước–sau, phím mũi tên, aria-current và focus đưa thẻ ra trước. Không dùng aria-live cho tự chạy. Reduce hoặc không JS: lưới/danh sách phẳng đủ nội dung, không tự chạy hay 3D. will-change chỉ bật khi khối đang chạy.

Dashboard chỉ stagger thẻ, count-up 800ms giữ giá trị cuối trong HTML, sidebar trượt viên chọn, Chart.js 800ms. Không có 3D tự chạy ở dashboard.

Không JS vẫn thấy nội dung. Ẩn reveal chỉ khi script head và lớp khởi tạo observer sẵn sàng. `prefers-reduced-motion` tắt mọi chuyển động không gian, reveal hiện ngay, chỉ giữ opacity/đổi màu ≤200ms. Điện thoại dùng coverflow và vuốt; không theo chuột hoặc tiến độ cuộn. Các nhóm motion có fallback tương ứng trong `motion.css`.

## Không làm

Không bịa số liệu, đánh giá, phí, thuế, đối tác thanh toán hoặc cam kết dịch vụ. Không thêm ảnh stock vào thẻ homestay. Ảnh trang trí tạo bằng AI (ChatGPT), lưu images/ai và ghi nguyên văn prompt trong credits.md; hero có bản điện thoại. Không dùng AI làm ảnh phòng/homestay/gallery. Không inline style trong markup, không màu ngoài tokens, không thư viện mới. Không đổi Services, Data, Entities, migration hoặc database. Controller chỉ được thêm ảnh khu vực vào truy vấn đã có. Không tắt app người dùng cổng 5011.
