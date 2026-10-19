# Bước 5c – Màn mới cho kế toán

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/prompts/ui/05a-chu-homestay.md` (**mục "Hiện trạng và luật chung" và mục "Luật controller", áp dụng nguyên văn**), `docs/ui/inventory.md`, `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui` (chỉ mục cấm, thông số component, icon), `ui-animation`, `full-output-enforcement`, `web-design-guidelines`.

## Các màn (area `Accounting`, policy `Accounting`, gom vào `_Tabs` của khu kế toán)

1. **Đối soát chủ homestay (FR-18):** `SettlementService`.
   - Danh sách kỳ: lọc kỳ/homestay/trạng thái. Nút "Lập bảng đối soát gần nhất" gọi `GeneratePeriodAsync(period, actor)` với kỳ gần nhất đã hết thời gian chờ (`SettlementCalculator.PeriodOf`). Lập cho một homestay gọi `GenerateAsync(homestayId, period, actor)`. **Không gọi `GenerateRecentAsync`**: hàm này dành cho job, không kiểm quyền.
   - Chi tiết kỳ: bảng đơn (tiền phòng, cọc, hoa hồng, giảm giá, khoản bù, số phải trả chủ), tổng cuối bảng, `_Timeline` trạng thái.
   - Thao tác theo trạng thái: Gửi chủ (`SendAsync`) → Xác nhận (`ConfirmAsync`) → Ghi nhận đã chuyển tiền (`RecordPaidAsync`, form số tiền/ngày/tham chiếu). Mở lại (`ReopenAsync`) dùng hộp xác nhận `danger`.
   - Chỉ hiện nút hợp lệ với trạng thái hiện tại. Công thức tiền lấy từ service, view không tự tính.
2. **Khoản bù trùng phòng (FR-10/FR-18):** `CompensationService.PendingAsync/RecordPaidAsync/RejectAsync`. Danh sách khoản bù chờ chi. Ghi nhận đã chi kèm mã tham chiếu ngân hàng. Từ chối kèm lý do, có hộp xác nhận.
3. **Rà lại màn bước 4** (`Transactions`, `Receipts`, `Refunds`, `AdditionalDeposits`): bổ sung lối tắt từ giao dịch/đơn sang kỳ đối soát liên quan nếu ViewModel đã có dữ liệu.
4. **Dashboard Accounting:** giao dịch chưa khớp, bill chờ duyệt, hoàn tiền chờ chi, kỳ đối soát chờ gửi/chờ chuyển tiền (`<money>` tổng).

## Kết quả bàn giao

- Commit: `feat(accounting): màn đối soát và khoản bù`.
- Ảnh `playwright-cli` ở 1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập `KT001`: lập kỳ đối soát, gửi, xác nhận, ghi nhận đã chuyển tiền, mở lại một kỳ. Kiểm tra tổng trên màn khớp số service trả về.
- Bảng tiền: số căn phải, tabular-nums, VND.

## Báo cáo rồi dừng

Báo cáo màn đã làm, service đã nối, gap đã ghi.
