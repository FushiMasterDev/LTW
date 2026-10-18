# Bước 3y2 – Đổi sang 3D tự chạy và ảnh trang trí tạo bằng AI

> Chạy trong **phiên Codex mới**, sau khi bước 03y đã commit (`470e14e` → `269d413`), trước bước 04.
>
> Bước 03y đã làm theo bản prompt đầu, trong đó 3D đi theo con trỏ chuột và theo tiến độ cuộn. Sau khi xem, người dùng chốt (18/10/2026):
> 1. **Không muốn** 3D theo chuột: không nghiêng thẻ, không vệt sáng, không lệch lớp theo con trỏ.
> 2. Muốn **ảnh tự chuyển động trong không gian 3D**, theo nhịp thời gian.
> 3. **Ảnh trang trí do ChatGPT tự tạo.**
>
> Mục 4 và mục 5 của `docs/prompts/ui/03y-phong-cach-moi.md` đã được viết lại theo ý này. Bước này chỉ làm phần chênh giữa code hiện tại và hai mục đó. **Không làm lại** token, font, component, layout của 03y.

Đọc:
- `docs/prompts/ui/03y-phong-cach-moi.md`, nhất là **mục 4** (chuyển động, bảng 16 dòng và luật tự chạy) và **mục 5** (ảnh). Mục 6–8 của file đó vẫn áp dụng: không được đổi gì, cách chạy app, cách kiểm tra.
- `docs/ui/03y-phong-cach-report.md`, `docs/ui/decisions.md` (các dòng 03y), `DESIGN.md` (mục Chuyển động).
- `wwwroot/js/motion/*.js`, `motion.css`.

Skill: `ui-animation`, `design-taste-frontend` (house style, `references/motion.md`, `inspect.mjs`, vòng chụp → sửa; không `draw.mjs`), `full-output-enforcement`, `web-design-guidelines`.

## 1. Gỡ: chuyển động phụ thuộc chuột hoặc tiến độ cuộn

Hiện-khi-cuộn **một lần** (`reveal.js`) được giữ. Mọi chuyển động 3D còn lại không được bám theo vị trí chuột hay vị trí cuộn.

| Hiện có | Việc cần làm |
|---|---|
| `wwwroot/js/motion/tilt.js`, `data-tilt` ở `Home/Index` (`.home-hero__scene`) và `_AreaCard`, class `.is-tilting`, lớp vệt sáng (sheen) của thẻ khu vực, nghiêng 4° của `_HomestayCard` | Xóa hẳn: file, thẻ `<script>` trong `_Layout`, thuộc tính, CSS |
| Hero 3 lớp `translateZ` nghiêng theo con trỏ | Xóa phần nghiêng. Giữ thứ tự hiện lần lượt của chữ (mục 4, dòng 2) |
| `parallax.js`: hero dịch theo cuộn, khối đối tác `rotateY` theo tiến độ cuộn | Xóa cả hai. Thay bằng "máy quay" tự chạy (dòng 1) và chồng ảnh trôi (dòng 5). Phần bật/tắt hero khi khuất màn hình hoặc ẩn tab chuyển sang bộ điều khiển chung ở mục 2 |
| Bộ thẻ "3 bước" tách ra theo `animation-timeline: view()` | Thay bằng bộ bài tự xào (dòng 4) |
| Icon cam kết xoay khi hover | Thay bằng tự xoay lệch nhịp (dòng 6) |
| Hover thẻ homestay: nâng 4px + bóng + ảnh phóng 1.06 | **Giữ** (đây là phản hồi hover của bản mẫu, không phải 3D theo chuột) |

## 2. Thêm: 3D tự chạy

Làm đúng các dòng 1, 3, 4, 5, 6, 7, 8 (phần chuyển ảnh), 9 trong bảng mục 4 của `03y-phong-cach-moi.md`:

1. **Hero:** "máy quay" 3D chậm. Mở rộng Ken Burns hiện có (1.05 → 1.12, 20s) thành phóng 1.05 → 1.15, kèm trôi `rotateX`/`rotateY` khoảng 2°, vòng `--fx-loop-slow` (khoảng 24s).
2. **Khu vực:** băng chuyền 3D tự quay. Trên điện thoại là dải coverflow tự trượt.
3. **3 bước:** bộ bài tự xào, có danh sách bước bên cạnh để đánh dấu bước đang ở trên.
4. **Đối tác:** chồng 3 ảnh homestay thật lấy từ `Model.Featured`, trôi lơ lửng lệch pha.
5. **Cam kết:** icon tự xoay 3D lệch nhịp.
6. **Chi tiết homestay:** gallery tự lật khối lập phương. Lightbox chuyển ảnh bằng lật lập phương, không tự chạy.
7. **`_AuthAside`:** "máy quay" như hero.

**Bộ điều khiển chung** `wwwroot/js/motion/autoplay.js`:
- Đăng ký mọi khối tự chạy.
- Dừng khi khối khuất màn hình (dùng IntersectionObserver chung của `FxMotion`), khi tab ẩn, khi rê chuột vào, hoặc khi focus bên trong.
- Mỗi khối một nút Tạm dừng/Tiếp tục.
- Tối đa 2 khối chạy cùng lúc trong một màn hình.

Nút `data-motion-toggle` hiện có ở hero chuyển sang dùng bộ này.

**Thứ tự làm:**
- Mẫu mới (băng chuyền, bộ bài, gallery lập phương, chồng ảnh trôi) vào thư viện component và `/Dev/UiKit` trước, rồi mới gắn vào trang.
- Mỗi việc một file trong `wwwroot/js/motion/`: `carousel-3d.js`, `card-deck.js`, `cube-gallery.js`, `autoplay.js`.

Áp đủ "Luật cho phần tự chạy" và "Luật chung" của mục 4:
- nút dừng, phím mũi tên, `aria-current`, link thật đi qua được bằng Tab;
- reduce và khi không có JS thì hiện thành lưới/danh sách phẳng;
- không `style="` trong markup;
- chỉ animate `transform`/`opacity`.

## 3. Ảnh trang trí tạo bằng AI

Theo mục 5 của `03y-phong-cach-moi.md`. Tóm tắt:
- **Ảnh cần tạo:** `hero-hanoi` (thêm bản điện thoại), `auth-veranda`, `partner-host`, ảnh dự phòng cho từng nhóm khu vực, `status-lost`. Ảnh khu vực dự phòng theo đúng các giá trị của enum `AreaGroup`: `InnerCity`, `Suburb`, `OtherProvince`.
- **Phong cách chung** ghi ở đầu mọi lệnh tạo ảnh.
- **Lưu:** `wwwroot/images/ai/`, ảnh hero ≤ 300 KB, ảnh khác ≤ 200 KB.
- **Dọn ảnh cũ:** xóa `hero-hoan-kiem*` và `auth-veranda.webp` khi không còn dùng; cập nhật `credits.md` (ghi nguyên văn lệnh tạo ảnh) và dòng credit trên hero.
- **Không** dùng ảnh AI cho homestay, phòng, gallery, thẻ homestay. Các ảnh đó vẫn lấy từ dữ liệu.
- **Phiên không có công cụ tạo ảnh:** viết `docs/ui/image-prompts.md` đủ lệnh và tên file, giữ ảnh cũ, ghi rõ trong báo cáo. Không tự tìm ảnh khác thay thế.

## 4. Commit và kiểm tra

Ba commit:
1. `refactor(ui): bỏ chuyển động 3D theo chuột và theo cuộn`
2. `feat(ui): 3D tự chạy cho hero, khu vực, ba bước, đối tác và gallery`
3. `feat(ui): ảnh trang trí tạo bằng AI`

Mỗi commit đi qua vòng: build → test → chụp 375/768/1280 × sáng/tối → sửa.

Kiểm tra theo mục 8 của `03y-phong-cach-moi.md`, thêm các điểm sau:
- `grep` không còn `tilt`, `pointermove`, `data-hero-parallax`, `animation-timeline` trong `Views/` và `wwwroot/js/motion/`.
- Video `playwright-cli`: trang chủ đứng yên khoảng 15 giây rồi cuộn hết trang. Các khối 3D phải tự chạy khi đứng yên, và dừng khi đã cuộn qua.
- Thử: nút Tạm dừng, dừng khi rê chuột/focus, phím mũi tên, Tab qua thẻ khu vực đưa thẻ đó ra trước.
- Reduce và khi không có JS: băng chuyền, bộ bài, gallery thành lưới/danh sách, đủ nội dung.
- Trang chủ đứng yên ở 1280px: không có frame > 50ms (ghi trace).

## 5. Báo cáo rồi dừng

Thêm mục "03y2" vào cuối `docs/ui/03y-phong-cach-report.md`:
- đã gỡ gì;
- danh sách chuyển động tự chạy: chỗ, nhịp, cách dừng, cách tắt khi reduce;
- ảnh AI: tên file, lệnh tạo, dùng ở đâu, dung lượng (hoặc đang chờ người dùng tạo);
- kết quả `inspect.mjs`, CLS, frame dài.

Cập nhật mục Chuyển động của `DESIGN.md` và thêm dòng vào `decisions.md`.

**Không bắt đầu bước 04.**
