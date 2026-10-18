# Bàn giao bước 03y — Phong cách mới và chuyển động 3D

Ngày 18/10/2026. Phạm vi: `docs/prompts/ui/03y-phong-cach-moi.md`. Giữ thương hiệu Fuji Homestay, dữ liệu thật, các hợp đồng form/route/validation. Không bắt đầu bước 04.

## Nhận diện và token

Đã đọc bản mẫu qua trình duyệt ở `/`, `/search`, `/homestay/may-sapa`, `/owner`, `/staff`, `/accounting`, `/admin`; cuộn toàn trang trước khi chụp. Ảnh tham chiếu nằm ở `/private/tmp/fuji-03y-reference*.png`. Không chạy draw.mjs, không tạo hướng thiết kế hoặc ảnh thay thế.

| Token | Cũ (sáng) | Mới (sáng) |
|---|---|---|
| `--fx-bg` | `#f8f6f1` | `#faf7f0` |
| `--fx-surface` | `#ffffff` | `#fffdf8` |
| `--fx-surface-2` | `#f2efe8` | `#f3eee3` |
| `--fx-surface-3` | `#e9e5dc` | `#ece5d8` |
| `--fx-ink` | `#1e2320` | `#29251f` |
| `--fx-muted` | `#59615c` | `#6b6257` |
| `--fx-primary` | `#2f5d50` | `#234c38` |
| `--fx-primary-hover` | `#244a40` | `#193c2b` |
| `--fx-accent` | `var(--fx-primary)` | `#a94d2c` |
| `--fx-accent-hover` | `var(--fx-primary-hover)` | `#893b20` |
| `--fx-font-sans` | `"Be Vietnam Pro", system-ui, -apple-system, "Segoe UI", Roboto, sans-serif` | `"Manrope", system-ui, -apple-system, "Segoe UI", Roboto, sans-serif` |
| `--fx-font-serif` | `"Lora", Georgia, "Times New Roman", serif` | `"Fraunces", Georgia, "Times New Roman", serif` |
| `--fx-radius` | `0.75rem` | `1.5rem` |
| `--fx-radius-sm` | `0.5rem` | `0.875rem` |
| `--fx-radius-lg` | `1rem` | `1.5rem` |

Token mới: on-accent, accent-text, accent-photo, font-display, radius-xl, glass-bg/glass-blur, sidebar-*, ease-out, dur-1…dur-6, dur-page, dur-dialog, dur-kenburns, stagger, perspective/perspective-area. Tên biến cũ và tone-* giữ nguyên. Bootstrap ánh xạ theo bộ mới.

Chế độ tối dùng nền `#121d18`, thẻ `#1b2922`, chữ `#f2eee5`, chữ phụ `#bdc6ba`, xanh `#a5d0b4`, cam `#f0ab83` với chữ `#2a1b13`. Nút sáng dùng cam tối hơn bản mẫu. Fraunces nạp cả normal và italic thật, optical size 9–144; Manrope 400–700. Đã kiểm/chụp chuỗi `ỗ ữ ặ ậ ỡ` ở nhiều cỡ, hai chế độ trong UI Kit; Google Fonts tải được các subset tiếng Việt.

### Tương phản

Script tạm: `/private/tmp/fuji-03y/contrast.py`, `extra_contrast.py`; tính luminance sRGB theo WCAG, assert ≥4.5. Kết quả chi tiết `/private/tmp/fuji-03y/contrast.json`.

| Chữ / nền | Sáng | Tối |
|---|---:|---:|
| `ink` / `bg` | 14.24:1 | 14.93:1 |
| `ink` / `surface` | 14.98:1 | 13.08:1 |
| `muted` / `bg` | 5.59:1 | 9.84:1 |
| `muted` / `surface` | 5.89:1 | 8.62:1 |
| `muted` / `surface-2` | 5.17:1 | 7.46:1 |
| `primary` / `bg` | 9.08:1 | 10.11:1 |
| `on-primary` / `primary` | 9.55:1 | 9.00:1 |
| `on-primary-soft` / `primary-soft` | 8.82:1 | 8.09:1 |
| `on-accent` / `accent` | 5.55:1 | 8.59:1 |
| `accent-text` / `bg` | 5.19:1 | 8.94:1 |
| `footer-ink` / `footer-bg` | 12.16:1 | 15.69:1 |
| `footer-muted` / `footer-bg` | 7.20:1 | 6.66:1 |

Tone trạng thái: nhỏ nhất **6.24:1** ở sáng, **8.14:1** ở tối. Các màu success/warning/danger/info trên nền mới nhỏ nhất **4.72:1** ở sáng, **7.56:1** ở tối. Sidebar chữ phụ/nền, chữ thường/nền và chữ trắng/viên cam giữ ≥4.5:1. QR dùng trắng cố định.

Trên ảnh: xét trường hợp nền ảnh trắng hoàn toàn, lớp phủ scrim 74% cho vùng chữ tạo nền `#4d514f`: chữ cam nhạt `#ffc09b` đạt **5.09:1**, chữ trắng **8.06:1**. Phía trên hero dùng ít nhất 64% (chữ trắng **5.57:1**); credit dùng lớp phủ 78% (chữ trắng **9.33:1**). Auth aside cũng tăng lớp phủ để chữ trắng đọc được.

## Component, layout và trang

- Nút chính cam, dạng viên, input bo 0.875rem, thẻ/ảnh 1.5rem, bóng soft; Tabs/chip theo ngôn ngữ mới. StatCard dùng số Fraunces, icon sage tròn.
- SearchBar hero kính mờ trong ảnh, chỉ báo focus trượt bằng transform; giữ tên trường GET, picker ngày native khi thiếu JS, compact/mobile responsive.
- HomestayCard ảnh 4:3, nhãn kính, tên + sao cam, ghim khu vực và giá từ dữ liệu. Không có nút yêu thích.
- Gallery desktop một lớn trái/hai nhỏ phải, ảnh tiếp theo trong lightbox; mobile giữ dải vuốt và bàn phím.
- BookingPanel/PriceBreakdown/Alert/Tabs/EmptyState/Steps/RatingInput/BookingCard/StatusPage/AuthAside/BookingStepper/PaymentQr/PageHead/DataTable/FilterBar cùng token. Nền QR đo được `rgb(255,255,255)` ở đủ sáu cấu hình.
- Thêm `_AreaCard`, `_RoomOption`, `_ReviewCard` với model kiểu rõ, đưa vào UI Kit trước khi ghép trang. CSS thẻ phòng và phản hồi đánh giá nằm ở components.css để UI Kit/trang thật dùng cùng mẫu.
- Header Home trong suốt → kính mờ qua mốc hero bằng observer, không dùng scroll listener cho header; menu mobile fade-up. Header khác nền đặc; footer xanh đậm chia cột, giữ link cũ. Dashboard chỉ đổi khung sidebar/topbar; auth giữ hai cột ảnh/form.
- Home: tiêu đề hai dòng, số homestay thật, ảnh Hoàn Kiếm, khu vực ảnh dọc, featured bốn cột ở 1280, ba cam kết, ba bước, lời mời đối tác xanh rừng. Không thêm nhận xét khách toàn hệ thống.
- Details: tiêu đề/điểm/khu vực trên gallery, phòng chọn có viền và dấu kiểm, tiện nghi chip, review có avatar, BookingPanel sticky desktop. Các trang Bookings/Account/Partner/Status/Error kế thừa component/khung mới, giữ bố cục bước 03.
- Controller chỉ thay truy vấn Areas sẵn có để lấy CoverUrl của ảnh IsCover thuộc homestay Active. Không sửa Services/Data/Entities/migration/database, không thêm thư viện.

ReviewService chưa nạp tên khách: ReviewCard hiển thị “Khách đã lưu trú”, avatar K; chỉ dùng tên khi model thực sự có. Gap G-15 đã ghi vào backend-gaps.md. Không tạo tên hay đánh giá giả.

## Chuyển động

Tất cả easing và thời lượng mới lấy từ token; chỉ tween transform/opacity (kính mờ tĩnh trên lớp opacity). Không tween kích thước/bố cục, không transition: all.

| Chỗ | Chuyển động / thời lượng | Reduce |
|---|---|---|
| Main khách/auth | Fade-up 18px / 700ms | Hiện ngay |
| Hero chữ | Nhãn → dòng 1 → dòng 2 → câu phụ, stagger 70ms, search trễ 150ms | Hiện ngay |
| Hero ảnh | Ken Burns 1.05→1.12 / 20s đảo chiều; parallax translate3d qua rAF | Tắt |
| Hero chiều sâu | Ảnh/text/search ở ba lớp, nghiêng tối đa 3° | Tắt |
| Section | Reveal 24px + rotateX(8°) / 800ms, một lần | Không ẩn/di chuyển |
| Khu vực | Nghiêng 8°, perspective 900px, tên Z40, sheen; ảnh zoom 1.06 / 800ms; reset 500ms | Không nghiêng/zoom |
| Homestay | Nghiêng 4°, nâng 4px / 350ms, ảnh zoom 1.06 / 800ms | Không di chuyển |
| Ba bước | Deck translateZ/rotateX tách ra theo view timeline; fallback observer | Trạng thái cuối |
| Cam kết | Reveal rotateY(-12°); icon xoay 360° khi hover / 800ms | Tắt |
| Đối tác | RotateY 10°→0 theo tiến độ cuộn | Tắt |
| Header | Glass layer opacity / 350ms | ≤150ms |
| Thanh tìm | Focus pill translate / 350ms, kích thước đích đặt tức thì | Đổi vị trí ngay, opacity ≤150ms |
| Lightbox | FLIP từ/về thumbnail / 350ms, giữ focus/keyboard | Không FLIP |
| Chọn phòng | Dấu kiểm scale + opacity / 250ms | Opacity 150ms |
| Steps/BookingStepper | Đường tiến trình scaleX / 800ms | Đường đầy đủ |
| Modal/toast | scale(.96), rotateX(6°) / 220ms + backdrop 150ms | Không xoay/phóng |
| Link/nút/chip | Underline transform, nâng 1px, nhấn .97 / 150–250ms | Đổi màu/opacity ngắn |
| Dashboard | Stagger StatCard, counter 800ms, sidebar pill 350ms | Số cuối, không trượt |

JS tách tại `wwwroot/js/motion/`: reveal, tilt, parallax, controls, flip-lightbox, dashboard. Một IntersectionObserver dùng chung; reveal/counter unobserve sau lần đầu, visibility subscriptions giữ cho hero/header. Pointermove và parallax gộp requestAnimationFrame. will-change chỉ khi tương tác/FLIP, gỡ sau đó. Hero dừng khi khuất/ẩn tab; có nút dừng/bật. Không nghiêng/parallax/Ken Burns ở dashboard. Chart.js chưa có biểu đồ trên các view hiện tại; module đặt animation 800ms khi Chart đã được trang nạp, không thêm chart trước bước 04.

## Inspector và vòng nhìn ảnh

Đo ở 375/768/1280; báo cáo JSON và ảnh tại `/private/tmp/fuji-03y/inspect-*-raw/` và `inspect-*-final/`. Không sửa inspector. Ngoại lệ từng loại đã ghi trong decisions.md, dùng --allow đúng các cờ tương ứng, không miễn covered-text/overflow/headline-lines. Home không miễn still-page.

| Route | Lượt thô FAIL/WARN | Cuối FAIL/WARN |
|---|---:|---:|
| `/` | 6/8 | 0/2 |
| `/Homestays` | 3/6 | 0/3 |
| `/Homestays/Details/6` | 5/6 | 0/5 |
| `/Bookings/Create?roomTypeId=14` | 5/4 | 0/3 |
| `/Bookings/Lookup` | 5/3 | 0/2 |
| `/Partner` | 5/4 | 0/3 |

Home đo **43–45 phần tử chuyển động**, vượt mức tối thiểu 8. Các WARN còn lại chủ yếu số cỡ chữ, thiếu video, nút ở dưới form, gallery vuốt/ngăn kéo lọc; lý do giữ ở decisions.md. Đây là kết quả có ngoại lệ công khai, không tuyên bố inspector thô không còn cờ.

ROUND 1
- Seen in one second: ảnh hồ Hoàn Kiếm và tiêu đề Fuji.
- Idea: hồ Hoàn Kiếm mở cánh cửa tới các chốn nghỉ thật của Hà Nội; số phòng/ngày/giá là tương tác chính.
- Live thing: tìm phòng theo ngày/khách từ dữ liệu thực.
- Layout: hero ảnh tràn / khu vực ảnh dọc / homestay lưới / cam kết thẻ / bước deck / đối tác khối màu.
- Scores: design 7.0 · usability 7.0 · creativity 6.8 · content 8.0 → 7.1; cap 6.0 do FAIL lúc đầu.
- Ba lỗi đã sửa: sheen bị hit-test đè tên khu vực; deck mobile lệch ngang; nguồn ảnh quá nhỏ và note dài dòng. Ngoài ra sửa Bootstrap xóa nền sidebar desktop.

ROUND 2
- Các khối có nội dung đủ, hero hai dòng ở cả ba cỡ, form đọc rõ, không tràn ngang, deck nằm trong màn hình, tên khu vực nổi trên ảnh.
- Scores: design 7.8 · usability 8.2 · creativity 7.0 · content 8.3 → 7.8 theo brief. Nếu giữ nguyên cap house style của skill thì 6.5 vì brief chủ đích yêu cầu badge/glass/viền/icon-card; đây là ngoại lệ nhận diện đã được chỉ định.
- Khác bản mẫu còn lại: ảnh Hà Nội và dữ liệu thật, không đủ tám featured vì service trả sáu, không nhận xét toàn hệ thống, không nút yêu thích, thêm ba bước/partner và chế độ tối, màu cam tối hơn ở sáng để đạt tương phản. Không đề xuất thư viện mới.

## Ảnh và artifact

Giữ hai ảnh trang trí hiện có: `hero-hoan-kiem.jpg` (+ bản sm), Prof MK, public domain, Wikimedia Commons; `auth-veranda.webp`, Joshua Ness, CC0, StockSnap. Nguồn đầy đủ trong credits.md, credit hero hiện ngay trên trang. Không lấy ảnh Lovable, không thêm ảnh trang trí (2/4). Ảnh khu vực/thẻ/gallery từ dữ liệu qua ImageUrlExtensions; ảnh UI Kit có nhãn minh họa.

Các commit đầu: `470e14e` (token/font), `f5f47a8` (component/layout), `2a98201` (motion); commit trang khách cuối chứa báo cáo này.

Artifact không commit tại `/private/tmp/fuji-03y/`: bộ `a-*`, `b-*`, `c-*`, `d-*`; `final-{home,search,detail,booking,lookup,partner,login,employee-login,register,status}-{light,dark}-{375,768,1280}.png`, ảnh QR `d-order-*`, fallback `reduce-*` và `nojs-*`. Video `home-motion.webm` (khoảng 1.1MB). Trace `.playwright-cli/traces/trace-1792338576328.trace` cùng network/resources.

## Kiểm tra và dữ liệu QA

- Build `dotnet build -c Release -p:UseSharedCompilation=false`: các nhóm đạt 0 lỗi/0 cảnh báo. `dotnet test HomestayBooking.sln -c Release --no-build`: trước **1597 pass + 1 skip**, các nhóm A/B/C/D và lượt cuối **1597 pass + 1 skip**, không fail. Mốc prompt 1595 + 1 thấp hơn HEAD lúc bắt đầu hai test có sẵn; không thêm test nghiệp vụ ở 03y.
- Chụp 10 trang × 3 cỡ × sáng/tối; không tràn ngang, không phần reveal còn ẩn. UI Kit chụp cùng sáu cấu hình; chọn phòng/lightbox mở-đóng/Esc hoạt động, không pageerror JS. QR đo nền trắng cả sáu cấu hình.
- PerformanceObserver trên Home 1280: **CLS=0**, không layout-shift; cuộn rAF 24px/frame: **165 frame, max 16.8ms, 0 frame >50ms**. Đây là đo local trong Chrome, không phải cam kết mọi thiết bị. Trace/video lưu như trên.
- Reduce Home ở đủ sáu cấu hình: **0 animation đang chạy, 0 nội dung ẩn, 0 tràn ngang**. JavaScript disabled: html không có class bật JS/motion, nội dung hiện, picker native hiện, không tràn ngang. Thuộc tính theme được test harness đặt để chụp CSS dark khi app không chạy script.
- Mô phỏng cảm ứng: fine-pointer=false, **0 thẻ bị nghiêng**. Nút dừng: bỏ fx-motion, ảnh animation=none, bật lại được. Chưa thử thiết bị cảm ứng vật lý.
- Đã chụp thêm sáu trang chính bằng tiếng Anh ở 375/768/1280 (bộ `en-*`), kiểm nội dung dịch và bố cục.
- Quét Views/Areas/CSS/JS riêng của app: **0 inline markup style, 0 mã màu ngoài tokens.css**. Khóa mới EN đủ, resource trùng=0. Rà theo web-design-guidelines: giữ label/focus/aria, icon trang trí, form và antiforgery, không tween layout; CSS menu collapse bỏ tween height của Bootstrap. Chữ hoa đầu câu theo tiếng Việt.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: **840 trang, 7 vai trò, 0 lỗi 500**. Bốn 404 đã biết: Create thiếu roomTypeId; ba mã demo FJ2611-0012/0013/0014 trên UI Kit.
- Chưa đăng nhập: Home chọn 23–25/11/2026 → tìm → Details → Create → **FJ261018-003**, QR → Lookup bằng mã và 0988123403 → đúng đơn, có QR.
- Khách seed 0987000001: đăng nhập → Home chọn 30/11–02/12/2026 → tìm → Details → Create → **FJ261018-004**, QR → Lookup bằng mã/SĐT → đúng đơn, có QR.
- Hai đơn QA trên được ghi vào database Development, có ghi chú “Kiểm thử giao diện bước 03y”, không chuyển tiền/xác nhận thanh toán; sẽ hết hạn giữ theo job hiện có. Không tự xóa dữ liệu nghiệp vụ. Không gửi yêu cầu đổi/hủy hoặc đăng ký đối tác mới trong lượt này.

App kiểm thử riêng chạy cổng 5012; không tắt app người dùng ở 5011. Thay đổi được chia đúng bốn commit theo prompt; báo cáo nằm trong commit trang khách cuối. Không triển khai view nội bộ của bước 04.
