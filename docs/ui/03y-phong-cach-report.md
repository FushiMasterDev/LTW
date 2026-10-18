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


## 03y2 – 3D tự chạy và ảnh trang trí AI (18/10/2026)

Thực hiện trên `feature/foundation`, bắt đầu từ `b78007d` (commit tài liệu 03y2 sau 03y). Không đổi nhánh, không push, không bắt đầu bước 04. Giữ font, nhận diện, các form/route/hợp đồng dữ liệu và nghiệp vụ của 03y; không sửa Services, Data, Entities, migration hoặc controller.

### Đã gỡ và thay

- Xóa `tilt.js`, `parallax.js` và thẻ script tương ứng; xóa data điều khiển theo con trỏ, class trạng thái, sheen của thẻ khu vực và nghiêng 4° của thẻ homestay.
- Bỏ hero dịch/nghiêng theo chuột hoặc cuộn, khối đối tác xoay theo tiến độ cuộn, bộ bước dùng view timeline và icon xoay khi hover. Giữ reveal một lần, thứ tự chữ hero, nâng 4px/bóng/zoom 1.06 của thẻ homestay.
- Component mới `_AreaCarousel`, `_CardDeck`, `_PhotoStack`, `_MotionControls`; `_Gallery` dùng module lập phương. Mẫu được thêm vào thư viện/UI Kit trước khi ghép vào trang thật. Dashboard không có tự chạy; UI Kit chỉ nạp các module này trong khu trưng bày mẫu.
- Sửa hai lỗi tìm được khi kiểm tra: đưa modal lightbox ra body để backdrop không bị stacking context của main che nút; ô ngày native dùng minmax/min-width để chi tiết 375px không JS không tràn.

### Chuyển động và điều khiển

| Chỗ | Nhịp và chuyển động | Cách dừng | Reduce / không JS |
|---|---|---|---|
| Hero | Máy quay scale 1.05→1.15, rotateX/Y −1°→1°, 24s đảo chiều | Nút riêng; hover/focus/khuất/ẩn tab | Ảnh tĩnh, chữ hiện đủ |
| Khu vực ≥3 thẻ | Vòng CSS 3D, 4s mỗi thẻ, đổi vị trí 1.2s; điện thoại coverflow ±35°, vuốt được | Nút riêng; trước/sau, mũi tên, Tab đưa thẻ đang focus ra trước; aria-current | Lưới đủ mọi link; <3 thẻ luôn dùng lưới |
| Ba bước | Bộ bài xoay X ra sau mỗi 4s, chuyển 800ms; danh sách bước cạnh bên chọn trực tiếp | Nút riêng; trước/sau/mũi tên, focus; aria-current=step | Danh sách ba thẻ đầy đủ |
| Đối tác | Tối đa ba ảnh thật từ Model.Featured trôi translateY/Z và rotate; 24s lệch pha | Nút riêng; hover/focus/khuất/ẩn tab | Dải ba ảnh thật phẳng |
| Cam kết | Icon xoay Y trong phần cuối chu kỳ 24s; lệch pha 4s/8s | Nút riêng; hover/focus/khuất/ẩn tab | Icon tĩnh, đủ nội dung |
| Gallery | Lật lập phương Y 90° mỗi 4s, 800ms; ảnh lớn Ken Burns nhẹ, hai thumbnail kế tiếp đổi cùng | Nút riêng; trước/sau/mũi tên/focus; aria-current | Lưới đủ mọi ảnh, kể cả ảnh thứ tư trở đi |
| Lightbox | Giữ FLIP mở/đóng; chuyển bằng hai mặt lập phương, 800ms | Chỉ người dùng chuyển; không tự chạy | Đổi ảnh tức thì; không JS xem đủ gallery phẳng |
| AuthAside | Máy quay như hero, 24s | Nút riêng; hover/focus/khuất/ẩn tab | Ảnh tĩnh; điện thoại vẫn giữ bố cục auth cũ |

`autoplay.js` đăng ký các khối, dùng chung IntersectionObserver của FxMotion và một đồng hồ rAF cho nhịp đổi thẻ. Không setInterval từng thẻ; tối đa hai khối đang chạy. Khuất, ẩn tab, hover chuột, focus hoặc modal mở đều dừng; trạng thái nút độc lập từng khối. Nút Tiếp tục vẫn chờ người dùng rời chuột/focus khỏi khối. Thiếu IntersectionObserver/module thì giữ nội dung phẳng. will-change chỉ khi chạy; hai mặt cube có backface hidden. Chỉ animate transform/opacity, không thêm thư viện hoặc inline style trong markup.

### Ảnh AI

Tám ảnh đã tạo bằng công cụ image generation ChatGPT, ngày 18/10/2026; đã xem ảnh gốc, nén WebP bằng Pillow và xem lại trong trình duyệt. Phong cách chung được đặt ở đầu từng lệnh. **Nguyên văn đầy đủ mọi lệnh tạo ảnh:** [credits.md](credits.md), từng mục theo đúng tên file dưới đây. Ảnh AI chỉ trang trí; ảnh phòng, thẻ homestay, gallery và chồng ảnh đối tác giữ dữ liệu thật.

| File trong `wwwroot/images/ai/` | Kích thước | Bytes | Dùng ở / lệnh tạo |
|---|---|---|---|
| `hero-hanoi.webp` | 2400×1350 | 279,536 | Hero desktop/tablet; [lệnh](credits.md#hero-hanoiwebp) |
| `hero-hanoi-mobile.webp` | 1080×1350 | 179,964 | Hero điện thoại; [lệnh](credits.md#hero-hanoi-mobilewebp) |
| `auth-veranda.webp` | 1200×1500 | 192,026 | Cột auth; [lệnh](credits.md#auth-verandawebp) |
| `partner-host.webp` | 1600×1000 | 187,518 | Ảnh nền trang trí khối đối tác, đi cùng ba ảnh thật; [lệnh](credits.md#partner-hostwebp) |
| `area-inner-city.webp` | 1200×1600 | 192,836 | Fallback AreaGroup.InnerCity; [lệnh](credits.md#area-inner-citywebp) |
| `area-suburb.webp` | 1200×1600 | 199,304 | Fallback AreaGroup.Suburb; [lệnh](credits.md#area-suburbwebp) |
| `area-other-province.webp` | 1200×1600 | 196,230 | Fallback AreaGroup.OtherProvince; [lệnh](credits.md#area-other-provincewebp) |
| `status-lost.webp` | 1600×1000 | 193,300 | Trang 404/lỗi máy chủ; [lệnh](credits.md#status-lostwebp) |

Hero preload đúng ảnh theo media desktop/điện thoại; có kích thước và decoding. Ảnh khác dưới fold dùng lazy khi phù hợp; khu vực/gallery có kích thước sẵn. Đã xóa `hero-hoan-kiem*.jpg`, `images/auth-veranda.webp`, cập nhật credit hero sang “Ảnh trang trí tạo bằng AI (ChatGPT)”. Giữ bản ảnh seed veranda trong demo vì vẫn được dữ liệu/UI Kit sử dụng. Không còn tham chiếu các ảnh trang trí cũ trong mã app.

### Kiểm tra và vòng chụp → sửa

- Build Release với UseSharedCompilation=false: cả ba nhóm và lượt cuối **0 lỗi, 0 cảnh báo**. Nhóm 2 có một warning nullable ở PhotoStack trong lượt đầu, đã sửa trước commit và build lại sạch.
- Mốc trước: **1597 pass + 1 skip** của HEAD 03y; nhóm 1/2/3 đều **1597 pass + 1 skip, 0 fail**. Lượt nhóm 3 mất 4m22s. Không thêm test nghiệp vụ. Skip là performance SQL test chỉ bật khi RUN_PERF_TESTS=1. Lượt chạy trùng trong sandbox lúc đầu gặp SSL handshake; đã dừng đúng các PID chạy trùng và chạy thành công ngoài sandbox, không sửa SQL/container.
- Mỗi commit có chụp 375/768/1280 × sáng/tối. Vòng cuối chụp tám trang Home/Search/Details/Create/Lookup/Partner/Auth/404 (48 ảnh), UI Kit bằng QL001 (6 ảnh), cùng ảnh sửa auth/đối tác. Không tràn ngang toàn trang, không reveal bị ẩn, không pageerror JS. Ảnh lazy ở mặt sau/ảnh gallery ẩn chưa tải và img lightbox chưa có src được loại khỏi lỗi ảnh thực tế.
- Reduce và không JS: Home + Details × ba cỡ × hai theme; **0 animation chạy, 0 nội dung bị ẩn, 0 tràn ngang**, đủ bảy khu vực, ba bước và năm ảnh gallery mẫu. Vòng đầu phát hiện ô ngày native ép chiều rộng ở no-JS 375px; đã sửa và chạy lại toàn bộ ma trận.
- Đã thử autoplay khi đứng yên; Tạm dừng/Tiếp tục; hover dừng và rời hover chạy tiếp; focus dừng; mũi tên; Tab qua đủ bảy link khu vực đều nhận aria-current và ra trước; chọn bước thứ ba; gallery đổi ảnh và lightbox trước/sau, đủ 5 ảnh. Root hero/băng chuyền dừng khi cuộn qua; auth có fx-camera; mobile có coverflow.
- **Giới hạn phép thử ẩn tab:** Chrome headless vẫn báo document.hidden=false khi đổi tab, kể cả khi tắt focus emulation. Đã kiểm tra riêng nhánh visibilitychange bằng mô phỏng document.hidden: số khối chạy 1→0→1. Không trình bày phép mô phỏng này như kiểm tra tab thật. Touch dùng browser emulation, chưa kiểm trên điện thoại vật lý.
- `rg` trong Views và js/motion: không còn tilt, pointermove, data-hero-parallax, animation-timeline. `style="` trong Views = 0; JavaScript qua node --check; git diff --check sạch. Không thêm mã màu ngoài tokens (màu mới đều dùng token đã có).
- Crawl: **840 trang, bảy vai trò, 0 lỗi 500**. Bốn 404 đã biết: Create thiếu roomTypeId và ba mã demo UI Kit.
- Luồng chưa đăng nhập: Home chọn 07–09/12/2026 → tìm → chi tiết → đặt → **FJ261018-005**, QR → tra cứu bằng mã/SĐT thử ra đúng đơn. Khách 0987000001: 14–16/12/2026 → cùng luồng → **FJ261018-006**, QR và tra cứu đúng. Hai đơn chỉ giữ phòng theo nghiệp vụ hiện có; không thanh toán, không xác nhận thay chủ.

Inspector nguyên bản, không sửa công cụ, chạy đủ 375/768/1280:

| Route | FAIL | WARN | Số phần tử chuyển động 375 / 768 / 1280 |
|---|---:|---:|---|
| `/` | 0 | 2 | 51 / 49 / 41 |
| `/Homestays` | 0 | 2 | 17 / 19 / 19 |
| `/Homestays/Details/6` | 0 | 3 | 10 / 10 / 10 |
| `/Bookings/Create?roomTypeId=14` | 0 | 2 | 2 / 3 / 3 |
| `/Bookings/Lookup` | 0 | 1 | 1 / 1 / 1 |
| `/Partner` | 0 | 2 | 4 / 4 / 4 |

Dùng đúng các ngoại lệ 03y-D đã ghi ở decisions.md (shadow/hairline/outlined-box/glass/palette-cream/mixed-font-headline; eyebrow/icon-cards của Home; focal-point/few-pictures/still-page theo trang tác vụ). **Home không miễn mức chuyển động**, đạt 41–51 phần tử. WARN còn lại: size-count của hệ UI, no-footage vì prompt yêu cầu ảnh tự chạy và không thêm video stock, side-scroll ngăn kéo lọc, first-action form dài, still-page WARN của Details với 10 phần tử (đạt ngưỡng 8, ít hơn mức khuyến nghị 16). Không miễn lỗi chữ bị che, tràn ngang hoặc headline-lines.

Vòng nhìn/sửa theo design-taste: (1) sửa caption thẻ phía sau bị che trong vòng 3D, chỉ vẽ nhãn phía trước nhưng giữ tên truy cập mọi link; (2) sửa backdrop chặn lightbox; (3) sửa nền nút dừng auth/đối tác để tránh chữ trắng trên nền sáng và tăng màu chữ đoạn đối tác trên ảnh nền. Không draw.mjs, không làm lại thiết kế. Chấm nội bộ sau sửa: Design 8, Usability 8, Creativity 7, Content 8 → **7.8/10**; đây là thực thi hướng Lúa đã chốt, không thiết kế nhận diện mới.

Rà [Web Interface Guidelines](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md): nút có aria-label, link điều hướng thật, focus nhìn thấy, điều khiển bàn phím, nội dung tĩnh đầy đủ, ảnh có kích thước, không tự đọc nội dung thay đổi bằng aria-live. Giữ sentence case tiếng Việt theo quyết định cũ.

Không đổi palette/font. Các cặp phát sinh khi đặt điều khiển/chữ trên ảnh được tính theo nền ảnh trắng xấu nhất ở cả hai theme:

| Cặp | Tương phản tối thiểu sáng / tối |
|---|---|
| Chữ paper trên photo-glass (nút auth) | 8.49:1 / 8.49:1 |
| Chữ footer-ink trên photo-glass (nút chồng ảnh) | 7.01:1 / 7.01:1 |
| Chữ footer-ink trên ảnh đối tác opacity .15 + nền sidebar | 7.40:1 / 7.40:1 |

### CLS, frame và bằng chứng

Đo qua playwright-cli ở 1280×800, sau khi build/test/inspector nặng đã kết thúc. PerformanceObserver layout-shift buffered và long-animation-frame, rAF lấy mẫu thực tế:

- Đứng yên 15s: **CLS=0**, 900 frame; lớn nhất **33.4ms**, **0 frame >50ms**, không có long-animation-frame.
- Cuộn hết trang: **CLS=0**, lớn nhất **16.8ms**, **0 frame >50ms**, không có long-animation-frame; tối đa **2** khối chạy; root đã khuất dừng, khối còn giao với viewport được phép chạy.
- Video `home-autoplay.webm` ghi phép đo; `home-autoplay-tour.webm` thêm dừng 6s ở băng chuyền/cam kết/bộ bài/đối tác để thấy tự chạy. Trace `.playwright-cli/traces/trace-1792342042164.trace`, network và resources đi cùng. Đây là số đo trên máy kiểm thử này, không suy rộng thành cam kết cho mọi thiết bị.
- Toàn bộ ảnh/video/log/JSON không commit: `/private/tmp/fuji-03y2/` và `.playwright-cli/`. Inspector `inspect-final-{home,search,detail,booking,lookup,partner}/report.json`; logs build-1/build-2-verified/build-final, test-1/test-2/test-3, interactions, visibility-contract, performance, booking-flow-final, fallbacks-final, shots-final, uikit-final. test-1 log có phần output từ lượt sandbox chạy trùng; lượt ngoài sandbox thoát 0 và có summary 1597 pass.

### Commit và phạm vi

1. `bfe3589` — `refactor(ui): bỏ chuyển động 3D theo chuột và theo cuộn`.
2. `67cb2ea` — `feat(ui): 3D tự chạy cho hero, khu vực, ba bước, đối tác và gallery`.
3. Commit ảnh AI kèm cập nhật credits/decisions/DESIGN và báo cáo này; có các sửa QA nhỏ nêu trên. Cả ba commit kết thúc bằng Co-Authored-By đúng yêu cầu.

### Cần người dùng quyết định

Không có quyết định nghiệp vụ mới cần chốt. Không đề xuất/thêm thư viện. Điểm kiểm thử còn giới hạn: tab ẩn thật và thao tác trên điện thoại vật lý chưa được xác minh ngoài mô phỏng; có thể kiểm thêm trên thiết bị khi nghiệm thu, không đổi hành vi hoặc nghiệp vụ để né giới hạn công cụ.

## 03y2 – sửa sau review (19/10/2026)

Bắt đầu từ `6eeed0e`, giữ nhánh `feature/foundation`. Sửa đúng bốn lỗi thị giác được giao, dùng ảnh đã có; không thay nghiệp vụ, controller, Services, Data, Entities, migration, font, palette hay thư viện. Không bắt đầu bước 04.

### Lỗi, nguyên nhân gốc và kết quả

Các đường dẫn mã dưới đây tính từ `HomestayBooking.Web/`. Tất cả bốn lỗi đều đã sửa, không có lỗi được giữ lại.

| Lỗi → nguyên nhân gốc | Đã sửa / không sửa | File đã đổi | Ảnh chụp lại 1280 sáng / tối |
|---|---|---|---|
| Hero tối: scrim dọc 64–92% phủ toàn ảnh; gợi ý ngày neo theo ô ngày nên chạm mép pill | **Đã sửa.** Scrim ngang tập trung vùng chữ trái, scrim dưới bảo vệ thanh tìm; vùng Tháp Rùa/bầu trời bên phải không có scrim ngang. Tablet phủ rộng hơn vì đoạn chữ chiếm nhiều chiều ngang; mobile 72→67%, nhẹ hơn lớp cũ ở vùng chữ. Đẩy gợi ý xuống khỏi mép pill, không thêm hộp/bóng chữ. Tách hero thành partial để UI Kit dùng đúng component thật | `Views/Shared/_HomeHero.cshtml`, `Views/Home/Index.cshtml`, `wwwroot/css/layout-public.css`, `wwwroot/css/components.css`, `Views/Dev/UiKit.cshtml` | `/private/tmp/fuji-03y2-fix/hero-light-1280.png`, `hero-dark-1280.png` |
| Khu vực nhỏ/trống: khung 13×18rem, vòng quay góc lớn khiến thẻ bên gần cạnh nhìn; stage 28rem và lề section quá rộng | **Đã sửa.** Cung tròn 3D nông, năm thẻ nhìn rõ, mờ dần theo chiều sâu. 1280: thẻ trước **336×448px (21rem, 3:4)**, section **795px**. 768: thẻ 288×384px, section 782px, thấy 3–5 thẻ; 375: thẻ **270px =72vw**, coverflow ±35°, section 779px. Caption chỉ ở thẻ trước; link thật/Tab đưa thẻ ra trước vẫn giữ | `wwwroot/js/motion/carousel-3d.js`, `wwwroot/css/motion.css`, `wwwroot/css/components.css`, `wwwroot/css/layout-public.css`, `Views/Dev/UiKit.cshtml` | `/private/tmp/fuji-03y2-fix/area-light-1280.png`, `area-dark-1280.png` |
| Bộ bài phẳng: mỗi lớp chỉ lệch 16px, opacity .35 làm mất lớp; thẻ bay bị hạ z-index trước khi được nhấc lên | **Đã sửa.** Ba lớp lệch 52px/lớp, Z giảm 70px/lớp, scale giảm, xoay −2.5°/+2.5°, hai sắc nền từ token. Thẻ trước cao tối thiểu 16rem, nhóm danh sách/điều khiển căn theo thẻ trước. Xào hai chặng trong cùng nhịp 1.2s: nhấc/lật, rồi đổi lớp/hạ xuống đáy. Chỉ giữ chữ thẻ trước (và thẻ đang bay); fallback vẫn đủ ba nội dung. Thu khoảng cách tới khối kế; mobile xếp dọc, không tràn | `wwwroot/js/motion/card-deck.js`, `wwwroot/css/motion.css`, `wwwroot/css/layout-public.css`, `Views/Dev/UiKit.cshtml` | `/private/tmp/fuji-03y2-fix/deck-light-1280.png`, `deck-dark-1280.png`; `deck-shuffle.png` ghi giữa động tác |
| Đối tác: ba ảnh cùng cỡ gần trùng nhau, keyframe cuối kéo cả ba về cùng transform; CTA là flex item riêng nên rớt xuống xa đoạn chữ | **Đã sửa.** Ảnh rộng 72% vùng ảnh, lệch x/y/Z riêng, xoay −6°/6°/0°; keyframe giữ độ xòe ở cả hai đầu và lệch pha như cũ. Nút dừng nằm sát góc dưới phải vùng ảnh, chữ trắng trên photo-glass. CTA nằm trong nhóm chữ, cách đoạn văn 1.5rem; grid hai cột căn giữa, mobile một cột | `wwwroot/css/motion.css`, `wwwroot/css/components.css`, `wwwroot/css/layout-public.css`, `Views/Home/Index.cshtml`, `Views/Dev/UiKit.cshtml` | `/private/tmp/fuji-03y2-fix/partner-light-1280.png`, `partner-dark-1280.png` |

Các sửa hình học dùng chính class của `_AreaCarousel`, `_CardDeck`, `_PhotoStack`, áp dụng cả Home và UI Kit; không thêm nhánh CSS vá theo route. UI Kit thêm hero dùng chung, mẫu carousel năm thẻ và ba ảnh có sẵn khác nhau để nhìn được độ xòe.

### Tương phản hero theo pixel ảnh thực

Đo trên Chromium qua playwright-cli, viewport cao 800px, ảnh WebP nén được trình duyệt chọn qua `currentSrc`. Cho máy quay chạy rồi pause/seek CSS animation tại **0/6/12/18/24 giây** (bao gồm hai đầu scale/rotate), giữ nguyên scrim và bố cục. Dùng DOM Range lấy từng dòng văn bản; tạm ẩn màu chữ khi chụp để lấy pixel nền thực nằm dưới chữ. Tính luminance sRGB/WCAG trên **mọi pixel trong từng hình chữ nhật dòng chữ**, lấy tỷ lệ nhỏ nhất so với màu chữ computed. Đây là cách đo bảo thủ, gồm cả khoảng trống quanh glyph; không suy luận từ nền trắng giả định và không dựa vào text-shadow.

| Rộng / theme | Chữ trắng tiêu đề (min hai dòng) | Cụm cam “Hà Nội.” | Câu phụ | Gợi ý tính theo đêm |
|---|---:|---:|---:|---:|
| 375 sáng | 7.46:1 | **4.66:1** | 9.91:1 | 5.25:1 |
| 375 tối | 7.46:1 | **4.66:1** | 9.91:1 | 8.40:1 |
| 768 sáng | 8.87:1 | 5.60:1 | 8.91:1 | 13.03:1 |
| 768 tối | 8.87:1 | 5.60:1 | 8.91:1 | 13.03:1 |
| 1280 sáng | 10.71:1 | 5.39:1 | 10.58:1 | 12.75:1 |
| 1280 tối | 10.71:1 | 5.39:1 | 10.58:1 | 12.75:1 |

Các số là min của cả năm thời điểm. Mobile gợi ý nằm trong surface của form, dùng muted theo theme; desktop/tablet gợi ý nằm trên ảnh, dùng paper. Đo thêm ảnh tĩnh không máy quay (reduce): cụm cam đạt **4.75 / 5.56 / 5.41:1** tại 375/768/1280, chữ trắng đạt ít nhất 8.43:1. Không đổi token màu hoặc tạo/nén lại ảnh. Dữ liệu, tọa độ/RGB pixel xấu nhất, script và ảnh nền đo ở `/private/tmp/fuji-03y2-fix/contrast.json`, `contrast-static.json`, `contrast.js`, `contrast.py`, `pixels-*.png`.

### Kiểm tra và bằng chứng

- `dotnet build HomestayBooking.sln -c Release -p:UseSharedCompilation=false`: 0 lỗi, 0 warning. `dotnet test HomestayBooking.sln -c Release --no-build`: **1597 đạt, 1 bỏ qua, 0 lỗi**; skip là `StaffAvailabilityAndDatedSearchStayWithinTargets` theo cấu hình sẵn có. Log `build-final.log`, `test-final.log` trong thư mục artifact.
- Trang chủ: 375/768/1280 × sáng/tối, mỗi khối đã được để tự chạy hơn một nhịp 4s trước chụp; không lỗi JS, không ảnh hỏng, không tràn ngang. 6 ảnh toàn trang `home-{light,dark}-{375,768,1280}.png` và 24 ảnh khối `{hero,area,deck,partner}-{light,dark}-{375,768,1280}.png`, tất cả ở `/private/tmp/fuji-03y2-fix/`. `shots-final.log`, `deck-proof.log` ghi kích thước/trạng thái.
- Reduce và không JS: **12 cấu hình**, mỗi cấu hình chụp đủ bốn khối (48 ảnh). Đủ **7 khu vực / 3 bước / 3 ảnh**, **0 animation chạy, 0 transform không gian, 0 tràn ngang**. Ảnh `{reduce,nojs}-{hero,area,deck,partner}-{light,dark}-{375,768,1280}.png`; kết quả `fallbacks.log`. Với no-JS tối, harness đặt data-bs-theme vào HTML trả về vì script đọc localStorage không chạy; không sửa app để phục vụ phép thử.
- Bật reduce ngay giữa động tác xào: enhanced=false, shuffling=0, transform không gian=0; `reduce-mid-shuffle.log` xác nhận lớp tạm được dọn.
- UI Kit đăng nhập QL001: 24 ảnh `uikit-{hero,area,deck,photos}-{light,dark}-{375,768,1280}.png`; **0 lỗi JS / 0 tràn ngang**, mẫu băng chuyền đủ năm thẻ. Log `uikit.log`.
- Inspector **nguyên bản**, `/` tại 375/768/1280: **0 FAIL, 2 WARN**. Giữ đúng các ngoại lệ 03y-D đã chốt; không miễn covered-text, overflow, headline-lines hay text-shadow. Hai WARN giữ nguyên phạm vi: số cỡ chữ của hệ UI và no-footage (yêu cầu dùng ảnh hiện có). Bằng chứng `inspect-verified/report.json`, `inspect-verified.log`.
- `node --check` hai module JS và `git diff --check` sạch; Views không có `style="`. Không thêm màu literal ngoài tokens, thư viện, chuyển động theo chuột/cuộn, hay animation thuộc tính ngoài transform/opacity. Rà các file thay đổi theo [Web Interface Guidelines](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md): link/nút thật, label/alt/focus/bàn phím, nút dừng đủ vùng bấm 44px; không có lỗi mức cao mới trong bốn khối.

- Điều khiển đã thử: Tạm dừng/Tiếp tục, hover/focus dừng trên carousel/deck/photo stack; Tab qua đủ bảy link khu vực đều ra trước với opacity=1 và aria-current; mũi tên và chọn bước thứ ba đúng. `interactions-final.log`, `deck-proof.log`, `deck-shuffle.png` là bằng chứng. Khi giảm khoảng cách section, có vị trí hai khối lân cận vẫn giao viewport nên bộ điều khiển chủ động giữ bộ bài theo giới hạn hai khối; đã giữ luật này, cuộn bộ bài vào vùng chạy và xác nhận tự chuyển sang bước 2 ở cả sáu cấu hình trước chụp.

### CLS và frame khi đứng yên

Đo bản Release trên cổng 5012, Chromium 1280×800, sau khi test/build/inspector và các lượt chụp khác kết thúc. Mỗi khối × mỗi theme đứng yên **15 giây** sau khi ổn định; PerformanceObserver layout-shift (buffered, bỏ hadRecentInput), long-animation-frame, cùng rAF đo khoảng cách frame. Bộ bài được đặt trong vùng thật sự được bộ điều khiển cấp chạy.

| Khối | Frame sáng / tối | CLS sáng / tối | Frame lớn nhất sáng / tối | Frame >50ms sáng / tối |
|---|---:|---:|---:|---:|
| Hero | 900 / 901 | 0 / 0 | 16.8 / 16.8ms | 0 / 0 |
| Khu vực | 900 / 900 | 0 / 0 | 16.8 / 16.8ms | 0 / 0 |
| Bộ bài | 900 / 900 | 0 / 0 | 16.8 / 16.8ms | 0 / 0 |
| Đối tác | 900 / 900 | 0 / 0 | 16.8 / 16.8ms | 0 / 0 |

Tổng **7201 frame**, không có long-animation-frame; mỗi mẫu có 1–2 khối đang chạy. Đây là số đo trên máy kiểm thử, không suy rộng thành bảo đảm cho mọi thiết bị. Script `performance.js`, dữ liệu `performance.json`/`performance.log`, trace `performance-trace.zip` đều tại `/private/tmp/fuji-03y2-fix/`, không commit artifact.

### Cần người dùng quyết định

Không có quyết định nghiệp vụ mới cần chốt; không có lỗi nào trong bốn lỗi bị bỏ qua. Giữ giới hạn tối đa hai khối tự chạy và các ngoại lệ inspector đã chốt. Các thao tác chạm và kích thước điện thoại dùng Chromium emulation, chưa kiểm trên điện thoại vật lý.

App kiểm thử riêng 5012 được dừng bằng đúng PID khi hoàn tất; không tắt app 5011, không dừng/xóa container `sql-homestay`. Một commit `fix(ui): sửa lỗi review bước 03y2`, không đổi nhánh, không push, không bắt đầu bước 04.
