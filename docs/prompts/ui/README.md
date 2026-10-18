# Prompt từng bước – xây lại giao diện

Luật chung, kiến trúc component và ràng buộc nằm ở `docs/prompts/ui-rebuild.md`. Các prompt dưới đây đều yêu cầu đọc file đó, nên **không cần dán file tổng**.

| Thứ tự | File | Nội dung | Sửa code? |
|---|---|---|---|
| 1 | `00-kiem-ke.md` | Kiểm kê view, route, màn còn thiếu | Không |
| 2 | `../b8-backend-ui-gaps.md` | **Backend B8:** lấp gap cho giao diện (thông báo, sửa homestay, hợp đồng…) | Backend |
| 2b | `01-nen.md` | CSS mới, 3 layout, điều hướng, Tag Helper nền, UI Kit | Có |
| 3 | `02-component.md` | Thư viện component đầy đủ | Có |
| 3b | `02x-ra-skill.md` | Rà lại component theo skill thiết kế (bù cho 01–02 chạy khi Codex chưa có skill) | Sửa lỗi review 02 + chỉnh phần nhìn |
| 4 | `03-trang-khach.md` (Claude làm: mở phiên bằng `claude-session.md`) | Trang khách + đăng ký đối tác + viết đánh giá | Có |
| 4b | `03y-phong-cach-moi.md` | Đổi nhận diện theo bản mẫu Lovable "Lúa" (xanh rừng + cam đất, Fraunces/Manrope) và thêm chuyển động 3D tự chạy, không theo chuột (chốt 18/10) | Có |
| 5 | `04-noi-bo-cu.md` | Chuyển các màn Admin/Staff/Owner/Accounting đang có | Có |
| 6 | `05a-chu-homestay.md` | Màn mới cho chủ homestay | Có |
| 7 | `05b-nhan-vien.md` | Màn mới cho nhân viên | Có |
| 8 | `05c-ke-toan.md` | Màn mới cho kế toán | Có |
| 9 | `05d-quan-ly.md` | Màn mới cho quản lý/quản trị + báo cáo | Có |
| 10 | `06-qa.md` | Kiểm tra toàn bộ, báo cáo QA | Chỉ sửa lỗi |
| — | `sua-loi.md` | Mẫu giao danh sách lỗi sau review | Có |

## Cách chạy mỗi bước

1. Mở **phiên Codex mới**, dán nội dung một file.
2. Codex làm xong thì commit và dừng.
3. Tự chạy `dotnet run --project HomestayBooking.Web`, xem http://localhost:5011 (từ bước 2: đăng nhập `QL001` / `Fuji@2026`, mở `/Dev/UiKit`).
4. Quay lại Claude Code, gõ `review bước <tên file>`.
5. Có lỗi thì dán `sua-loi.md` kèm danh sách lỗi cho Codex. Đạt thì sang bước kế tiếp.

Nên chạy B8 **ngay sau bước 00, trước `01`**. Không chạy song song hai phiên Codex trên cùng thư mục: B8 sửa `HomestayCard` và `BookingsController`, đúng phần bước 01 cũng đang build. `05a`–`05d` độc lập với nhau, làm theo thứ tự nào cũng được, nhưng phải sau `04`.
