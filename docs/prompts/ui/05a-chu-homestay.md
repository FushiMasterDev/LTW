# Bước 5a – Màn mới cho chủ homestay

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/ui/inventory.md` (mục màn còn thiếu), `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui`, `ui-ux-pro-max` (UX form và lịch trên điện thoại), `full-output-enforcement`.

## Luật controller (áp dụng cho mọi bước 5)

- Controller mới đặt trong area tương ứng, mỏng: `[Authorize]` đúng policy (`AppPolicies`/`AppRoles`) → gọi service → map ra ViewModel → View.
- POST dùng mẫu PRG + `TempData` cho `_Alerts`, có `[ValidateAntiForgeryToken]`.
- **Đọc method public của service trước khi nối.** Không sửa `Services/`, `Data/`, `Models/Entities/`. Thiếu dữ liệu thì ghi vào `backend-gaps.md` rồi bỏ qua màn đó.
- Kiểm tra quyền sở hữu theo đúng cách service/`HomestayAccessService` đang làm: chủ chỉ thấy homestay của mình.
- Thêm mục vào sidebar (`SidebarLink`) cho màn mới.
- Mỗi controller mới có test cho đường thành công + đường bị chặn quyền, theo style test hiện có.

## Các màn (area `Owner`, ưu tiên điện thoại)

1. **Homestay của tôi (FR-13):** danh sách từ `HomestayContentService.OwnerHomestaysAsync`, có trạng thái duyệt (`<status-badge>`). Trang sửa đổ dữ liệu từ `HomestayContentService.EditorAsync` (B8), chia `_Tabs`:
   - Thông tin, mô tả: `HomestayContentService`.
   - Phòng, loại phòng, giá 3 mức ngày thường/cuối tuần/lễ: `RoomService`.
   - Ảnh: `HomestayImageService`, `_UploadZone`, chọn ảnh bìa, xóa ảnh có xác nhận.
   - Tiện nghi.
   - Chính sách hủy: `HomestayPolicyService`.
   - Giấy tờ: `HomestayDocumentService`.
   - Theo quyết định 16/10: giá/phòng/ảnh/mô tả sửa là áp dụng ngay. Riêng địa chỉ và giấy tờ pháp lý đưa homestay về chờ duyệt. Form phải báo trước điều này ở hai tab đó, và sau khi lưu hiện "Đang chờ Quản lý duyệt".
2. **Lịch phòng (FR-14):** `_RoomCalendar` + `RoomBlockService.CalendarAsync/CreateAsync/UnlockAsync`. **Khóa ngày cho khách từ kênh khác tối đa 3 thao tác:** chọn phòng → chọn khoảng ngày → xác nhận. Mở khóa tương tự.
3. **Lịch nhận–trả (FR-16):** `OwnerScheduleService.ReadAsync(actor)` (overload B8). Danh sách theo ngày (Hôm nay / Ngày mai / 7 ngày tới), mỗi dòng có tên khách, SĐT (theo `BookingContactVisibility`), phòng, số đêm.
4. **Đối soát (FR-16):** `SettlementService.ListAsync/DetailsAsync` phía chủ. Danh sách kỳ + trạng thái; chi tiết gồm các đơn, hoa hồng, khoản bù, số được nhận (`<money>`).
5. **Đánh giá (FR-06):** danh sách đánh giá homestay của mình, trả lời bằng `ReviewService.ReplyAsync`.
6. **Thông báo (FR-15):** trang danh sách đầy đủ của `NotificationBell` dùng `NotificationService.ListAsync/MarkReadAsync/MarkAllReadAsync` (B8). Trang này đặt ở chỗ dùng chung cho mọi vai trò nội bộ (ví dụ `/Notifications`), không chỉ riêng Owner.
7. **Dashboard Owner:** bổ sung lối tắt tới các màn trên, đơn chờ xác nhận, khách đến hôm nay.

## Kết quả bàn giao

- Commit theo màn: `feat(owner): ...`.
- Ảnh `playwright-cli` mỗi màn ở **375** và 1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập `0912000001` / `Fuji@2026` trên viewport 375: sửa giá một phòng, tải 1 ảnh, khóa rồi mở 2 đêm, xem đối soát, trả lời 1 đánh giá.
- Thử mở URL homestay của chủ khác: phải bị chặn.

## Báo cáo rồi dừng

Báo cáo màn đã làm, service đã nối, gap đã ghi, số thao tác khóa ngày đo được.
