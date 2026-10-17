# Bước 0 – Kiểm kê giao diện (không sửa code)

Đọc `docs/prompts/ui-rebuild.md` (luật chung), `DESIGN.md`, `QUY_TRINH_BTL.md` (bảng FR-01…FR-27 và mục 7.x). Bước này **chỉ đọc và viết tài liệu**: không sửa file nào ngoài `docs/ui/`.

## Việc cần làm

1. Đọc toàn bộ 67 file `.cshtml` trong `HomestayBooking.Web/Views/**` và `HomestayBooking.Web/Areas/*/Views/**`, cùng controller tương ứng.
2. Tạo `docs/ui/inventory.md`, gồm:
   - **Bảng route:** URL, area/controller/action, GET/POST, role/policy (`[Authorize]`, `AppPolicies`), `@model`, partial dùng, tên trường form, hook `data-*`, FR liên quan.
   - **Mẫu UI lặp lại:** mẫu nào xuất hiện ở ≥ 2 view (bảng, thẻ số liệu, form lọc, badge trạng thái, khối tiền…). Ghi view nào dùng, đề xuất tên component trong danh mục của `ui-rebuild.md`.
   - **CSS/JS:** class nào trong `wwwroot/css/*.css` còn được dùng và ở đâu, class nào chết. Hành vi nào trong `site.js` đang được view nào gọi.
   - **Màn còn thiếu:** đối chiếu FR-01…FR-27. Với mỗi màn: vai trò, service + method public đã có (đọc file trong `Services/`), policy nên dùng, thuộc bước nào (`03`, `05a`, `05b`, `05c`, `05d`).
   - **Test phụ thuộc view:** test nào trong `HomestayBooking.Tests` render hoặc kiểm tra partial, ViewModel, controller trả View.
3. Tạo `docs/ui/backend-gaps.md`: màn nào cần dữ liệu mà service chưa có. Backend đã đủ FR-01…FR-27 sau B7; Zalo/SMS thật là phần rút gọn đã chấp thuận. Nếu không có gap thì ghi "không có".
4. Tạo `docs/ui/decisions.md` với tiêu đề và một dòng mẫu, để các bước sau ghi quyết định vào.

## Kết quả bàn giao

- `docs/ui/inventory.md`, `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`.
- Commit: `docs(ui): kiểm kê giao diện hiện có và màn còn thiếu`.

## Kiểm tra trước khi báo xong

- `git diff --stat HEAD~1` chỉ có file trong `docs/ui/`.
- Số route trong inventory khớp với số action public trong các controller MVC (không tính `Controllers/Api`).

## Báo cáo rồi dừng

Báo cáo: số view, số route, top 10 mẫu lặp lại, danh sách màn còn thiếu theo bước. **Không bắt đầu bước 1.**
