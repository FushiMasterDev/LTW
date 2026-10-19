# Bước 5b – Màn mới cho nhân viên

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/prompts/ui/05a-chu-homestay.md` (**mục "Hiện trạng và luật chung" và mục "Luật controller", áp dụng nguyên văn**), `docs/ui/inventory.md`, `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui` (chỉ mục cấm, thông số component, icon), `ui-animation`, `full-output-enforcement`, `web-design-guidelines`.

## Các màn (area `Staff`, policy `Employee`; thao tác cần quyền quản lý dùng đúng policy service yêu cầu)

1. **Quản lý đơn (FR-09):** `StaffBookingService.SearchAsync/DetailsAsync/AssignAsync/ChangeStatusAsync`.
   - Danh sách: `_FilterBar` (mã, tên, SĐT, trạng thái, homestay, ngày, người phụ trách), `_DataTable`.
   - Chi tiết: `_BookingStepper`, `_DescriptionList`, `_PriceBreakdown`, `_Timeline`, giao đơn (danh sách người nhận từ `StaffBookingService.AssigneesAsync`, B8; Tư vấn thấy nút "Nhận đơn"), đổi trạng thái có xác nhận.
2. **Đặt hộ khách (FR-07):** `StaffBookingService.CreateForCustomerAsync`.
   - Nhập SĐT → tìm khách (`CustomerService.SearchAsync`), chưa có thì tạo mới qua luồng service.
   - Chọn kênh Phone/Zalo/Messenger/Other → chọn homestay/phòng/ngày (`_DateRangePicker`, `_PriceBreakdown`) → tạo đơn.
3. **Phòng trống toàn hệ thống (FR-08):** `AvailabilityService.StaffAvailabilityAsync`. Lọc khu vực/ngày/số khách, hiện dạng bảng homestay × loại phòng × số phòng trống theo đêm. Không dùng `_RoomCalendar`: Tư vấn không có quyền đọc lịch tháng (quyết định 17/10). Có nút "Làm mới" và thời điểm cập nhật. Không cache phía client.
4. **Khách hàng (FR-12):** `CustomerService.SearchAsync/DetailsAsync/UpdateAsync`. Danh sách, chi tiết kèm lịch sử đơn, form sửa.
5. **Trùng phòng (FR-10):** `OverbookingService.ReportAsync/AlternativesAsync/MoveRoomAsync/ReplaceAsync/RefundAsync`.
   - Từ chi tiết đơn: báo trùng → xem phương án thay thế (thẻ so sánh giá/khu vực/chênh lệch) → chọn chuyển phòng / thay homestay / hoàn tiền.
   - Mỗi bước có xác nhận và `_PriceBreakdown` phần chênh lệch.
6. **Duyệt nội dung homestay (FR-27):** không làm ở bước này. Theo quyết định 17/10, chỉ Quản lý duyệt, nên màn này chuyển sang `05d-quan-ly.md`. Dashboard Staff không hiện hàng chờ duyệt nội dung.
7. **Thẩm định hồ sơ đối tác (FR-26):**
   - Danh sách, chi tiết: `OwnerRegistrationQueryService`.
   - Thao tác: `OwnerRegistrationService.TakeAsync/RequestRevisionAsync/ApproveAsync/RejectAsync/InviteAsync/ResendInviteAsync`.
   - Lọc theo trạng thái, cảnh báo hồ sơ quá SLA, xem giấy tờ, ghi chú yêu cầu bổ sung. Thao tác chỉ dành cho quản lý thì chỉ hiện với người có quyền.
8. **Cảnh báo đơn chưa xác nhận > 30 phút (FR-11):** hiện trên Dashboard Staff (danh sách đơn quá hạn xác nhận) và qua `NotificationBell` (thông báo do `BookingTimeoutJob` tạo, đọc bằng `NotificationService` B8).
9. **Dashboard Staff:** đơn chờ xử lý, yêu cầu đổi/hủy (`BookingChangeRequestService.PendingCountAsync`, B8), cảnh báo quá hạn, hồ sơ đối tác chờ thẩm định, tỷ lệ chốt của tôi (nếu `ReportService` có sẵn số này).

## Kết quả bàn giao

- Commit theo màn: `feat(staff): ...`.
- Ảnh `playwright-cli` mỗi màn ở 1280 × sáng/tối, thêm 768 cho danh sách đơn.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập `NV001`: tìm đơn theo SĐT, nhận đơn, đặt hộ cho SĐT mới, xem phòng trống. Thử mở URL duyệt nội dung homestay: phải bị chặn.
- Đăng nhập `QL001`: giao đơn cho `NV002`, thẩm định 1 hồ sơ (bản seed `0912000006` đang PendingReview; hồ sơ thử #1002 của SĐT `0977123456` tạo ở bước 03 cũng đang PendingReview), xử lý 1 ca trùng phòng.

## Báo cáo rồi dừng

Báo cáo màn đã làm, service đã nối, gap đã ghi.
