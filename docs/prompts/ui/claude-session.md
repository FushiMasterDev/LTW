# Mở phiên Claude Code mới cho một bước giao diện

Dán khối dưới vào phiên Claude Code mới mở ở thư mục `/Users/phamngocphu/Downloads/LTW`. Đổi `03-trang-khach.md` thành file của bước cần làm.

---

```text
Mình làm tiếp giai đoạn giao diện của HomestayBooking (Fuji Homestay), nhánh feature/foundation.
Phiên này bạn tự làm bước: docs/prompts/ui/03-trang-khach.md.

1. Đọc trước, theo thứ tự:
   - docs/prompts/ui/README.md: thứ tự các bước và quy trình.
   - docs/prompts/ui-rebuild.md: luật chung, kiến trúc component.
   - DESIGN.md: tokens.css là nguồn chuẩn; Airbnb chỉ tham khảo bố cục.
   - docs/ui/decisions.md: các quyết định đã chốt. Đừng làm ngược.
   - docs/ui/02-component-report.md và docs/ui/02x-component-report.md: component đã có.
   - File prompt của bước này. Làm đúng phạm vi trong đó, kể cả mục "Việc còn treo".
   Sau đó chạy git log --oneline -10 và git status để biết điểm bắt đầu.

2. Skill:
   - Nạp design-taste-frontend, full-output-enforcement, web-design-guidelines bằng Skill tool.
   - Đọc thẳng .agents/skills/ui-animation/SKILL.md.
   - Chỉ dùng house style, scripts/inspect.mjs và vòng chụp → sửa của design-taste-frontend.
   - Không chạy draw.mjs. Không đổi màu, font, bo góc. Chỉ dùng thời lượng chuyển động có trong motion.css và --fx-ease.

3. Luật bắt buộc:
   - Không sửa Services/Data/Entities/migration. Controller mới chỉ được mỏng, gọi service có sẵn.
   - Giữ @model, tên trường form, route, antiforgery, hook data-* mà test tích hợp đọc
     (bảng "Ràng buộc test" trong 02-component.md, docs/ui/inventory.md mục 5–6).
   - Không style="", không mã hex ngoài tokens.css. Chữ hiển thị qua L[...] và có bản dịch trong SharedResource.en.resx.
   - Cần mẫu UI mới thì thêm vào thư viện component + /Dev/UiKit trước, rồi mới dùng ở trang.
   - Ghi quyết định mới vào docs/ui/decisions.md.

4. Chạy và kiểm:
   - Build và test: dotnet build -c Release -p:UseSharedCompilation=false; dotnet test HomestayBooking.sln -c Release --no-build.
     Mốc hiện tại: 1589 pass + 1 skip có từ trước.
   - Cổng 5011 có thể là app người dùng đang chạy: không tắt. Chạy bản của bạn bằng:
     cd HomestayBooking.Web && ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5012
     Tắt app khi xong.
   - Tài khoản seed, mật khẩu Fuji@2026:
     - Khách: 0987000001. Chủ homestay: 0912000001. Tư vấn: NV001. Kế toán: KT001. Quản lý: QL001. Quản trị: AD001.
     - Nhân viên đăng nhập ở /Account/EmployeeLogin.
     - /Dev/UiKit cần QL001.
   - Tìm trang lỗi theo vai trò: python3 -I tools/ui-crawl.py http://localhost:5012
     Lỗi đã biết: /Bookings/Create thiếu roomTypeId trả 404.
   - Chụp màn hình: playwright-cli (open, fill, click, resize, goto, screenshot, eval).
     Lưu ảnh vào scratchpad hoặc .playwright-cli/, không commit.
     Ảnh full-page có thể hiện "Chưa có ảnh" do ảnh lazy chưa tải: cuộn tới rồi chụp lại trước khi kết luận.

5. Cách làm:
   - Làm từng nhóm commit ghi trong prompt của bước.
   - Mỗi nhóm: sửa → build → test → chụp 375/1280 sáng/tối → sửa → commit.
   - Commit message kết thúc bằng dòng Co-Authored-By theo hướng dẫn hệ thống.
   - Báo tiến độ ngắn sau mỗi nhóm. Nếu sắp hết ngữ cảnh thì dừng ở cuối một nhóm đã commit.
   - Gặp quyết định nghiệp vụ không có trong prompt hay decisions.md thì hỏi mình, đừng tự chốt.
   - Xong thì viết docs/ui/03-trang-khach-report.md theo mục "Báo cáo rồi dừng" của prompt, tóm tắt cho mình bằng tiếng Việt và dừng.
     Không bắt đầu bước kế.
```
