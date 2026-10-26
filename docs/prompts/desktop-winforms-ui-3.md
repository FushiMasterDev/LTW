# Prompt: Làm lại giao diện WinForms kéo thả được (đợt 3/3): Hoàn tiền, Khoản bù, Đối soát

Chạy trên **máy Windows**, cùng nhánh **`feature/desktop-ui-rebuild`**, nối tiếp đợt 2 đã được review.

## 0. Điều kiện trước khi làm

Đợt này cần API kế toán mới (đối soát, hoàn tiền, khoản bù), làm ở nhánh `feature/desktop-api-settlements` theo `docs/prompts/desktop-api-settlements.md`. **API đó phải đã merge vào `main`.**

```powershell
cd D:\LTW
git switch feature/desktop-ui-rebuild
git pull
git fetch origin
git merge origin/main
dotnet build HomestayBooking.sln
dotnet run --project HomestayBooking.DesktopHost -- --reset
```

Mở `http://localhost:5080/swagger`, kiểm có các nhóm `/api/desktop/refunds`, `/api/desktop/compensations`, `/api/desktop/settlements`, `/api/desktop/accounting/counts`.

- **Thiếu thì dừng lại**, không tự viết API. Ghi vào báo cáo rồi kết thúc.
- `git merge` bị xung đột thì dừng lại, báo các file xung đột, không tự chọn bên nào.

Đọc lại:

- `docs/prompts/desktop-winforms-ui-1.md` mục 0 và mục 2: quy ước Designer giữ nguyên.
- Hai báo cáo `docs/desktop-ui/dot-1-report.md`, `dot-2-report.md`: dùng đúng các quyết định đã chốt.
- `docs/api/desktop-api-settlements-report.md` và `docs/api/desktop-api-theo-vai-tro.md` mục Kế toán: endpoint thật, JSON, quyền.
- `docs/api/winforms-giao-dien.md` mục 4.9, 4.10.

**Swagger là nguồn sự thật.** Tên endpoint trong file này là tên đề xuất; khác Swagger thì theo Swagger.

## 1. Quyền cần bám

| Nhóm | Kế toán | Quản lý, Quản trị |
|---|---|---|
| Hoàn tiền | Xem, ghi nhận đã hoàn | Xem; ghi nhận theo kết quả API (service tự chặn người không được xử lý, hiện câu lỗi) |
| Khoản bù | Xem, ghi nhận đã chi, từ chối, xem danh sách "có thể từ chối" | Chỉ xem danh sách chờ chi |
| Đối soát | Toàn bộ thao tác | Chỉ xem danh sách và chi tiết |

Giao diện ẩn hoặc tắt nút theo vai trò, nhưng vẫn phải xử lý đúng 403 nếu API từ chối.

## 2. `FrmRefundAndCompensation`: Hoàn tiền và khoản bù

Dựng lại từ đầu theo quy ước Designer. Bố cục: tab trên, mỗi tab là bảng + khung chi tiết/thao tác.

### Tab "Hoàn tiền cọc"

- Lọc **Chờ hoàn** / **Đã hoàn** (`done`), ô tìm (mã đơn, tên, SĐT), bảng phân trang. Chờ hoàn: cũ nhất trước; đã hoàn: mới nhất trước.
- Cột: mã đơn, khách, SĐT, homestay, trạng thái đơn, số tiền hoàn, ngày hủy/từ chối; đã hoàn thì thêm mã giao dịch và người ghi nhận.
- Thao tác (chỉ khi Chờ hoàn): ô **mã giao dịch chuyển hoàn** (bắt buộc), nút "Ghi nhận đã hoàn". Hỏi xác nhận, hiện số tiền trong câu hỏi.

### Tab "Khoản bù"

- Lọc **Chờ chi** / **Có thể từ chối** (lọc thứ hai chỉ hiện cho Kế toán).
- Bảng: mã đơn, homestay, số tiền bù, ngày tạo, trạng thái đơn.
- Thao tác Kế toán:
  - "Ghi nhận đã chi" cần mã giao dịch (bắt buộc).
  - "Từ chối" cần lý do 1–500 ký tự, kiểm độ dài ở client.

## 3. `FrmSettlementManagement`: Đối soát chủ homestay

Dựng lại từ đầu theo quy ước Designer.

- **Bộ lọc:** năm, tháng, kỳ (đủ cả ba hoặc để trống cả ba), homestay, trạng thái (Nháp / Đã gửi / Đã xác nhận / Đã thanh toán), bảng phân trang.
  - Mở màn thì đặt sẵn năm/tháng/kỳ theo `GET /settlements/latest-period` nhưng **chưa lọc**. Danh sách đầu tiên là toàn bộ.
- **Lập bảng:**
  - "Lập kỳ gần nhất" gọi `generate-recent`.
  - "Lập theo kỳ đã chọn" gọi `generate`, có thể chọn một homestay.
  - Sau khi lập, hiện số bảng có dữ liệu. Có **mã đơn chưa chốt** thì hiện danh sách đó trong hộp thoại (không chỉ một dòng chữ).
- **Chi tiết bảng đang chọn:**
  - Thông tin kỳ, các tổng tiền (tiền phòng, giữ lại, hoa hồng, thực nhận của chủ).
  - Bảng từng đơn, bảng khoản bù, dòng thời gian lịch sử (bước và ghi chú).
- **Nút theo trạng thái** (chỉ Kế toán; bật/tắt theo trạng thái hiện tại):

| Trạng thái | Nút |
|---|---|
| Nháp | Gửi cho chủ |
| Đã gửi | Xác nhận thay chủ (bắt buộc lý do), Mở lại bản nháp (bắt buộc lý do) |
| Đã xác nhận | Ghi nhận đã thanh toán, Mở lại bản nháp |
| Đã thanh toán | Không có |

Đối chiếu bảng này với luật của `SettlementService` qua thử thật; sai thì theo service và ghi vào báo cáo.

- **Ghi nhận đã thanh toán** mở hộp thoại nhỏ:
  - mã giao dịch;
  - **số tiền thực chuyển** (`NumericUpDown`, mặc định số thực nhận của chủ);
  - **thời điểm chuyển** (ngày + giờ theo giờ Việt Nam).

  Client đổi thời điểm sang **UTC có hậu tố `Z`** trước khi gửi `paidAt`.

## 4. Tổng quan kế toán

- `FrmAccountantDashboard`: menu hiện **số việc đang chờ** cạnh từng mục (bill, giao dịch, cọc bổ sung, hoàn tiền, khoản bù, đối soát) từ `GET /accounting/counts`. Cập nhật khi bấm "Làm mới" và sau mỗi thao tác ghi.
- `FrmAdminDashboard`: thêm menu "Hoàn tiền & Khoản bù", "Đối soát" ở chế độ chỉ xem cho Quản lý, Quản trị.

## 5. Ngoài phạm vi

- Không sửa `HomestayBooking.Web`, `HomestayBooking.DesktopHost`, `HomestayBooking.Tests`. Lỗi API ghi vào báo cáo.
- Không đổi các form đợt 1, 2, trừ sửa lỗi dùng chung và menu ở mục 4.

## 6. Test

1. Test tĩnh quy ước Designer phủ **mọi** `*.Designer.cs`, không còn ngoại lệ.
2. Toàn bộ `DesktopUiTests` (đợt 1, 2 và cũ) pass.
3. Thêm test giao diện (host đã `--reset`):
   - Kế toán ghi nhận hoàn tiền một đơn chờ hoàn, đơn chuyển sang danh sách Đã hoàn.
   - Thiếu mã giao dịch bị chặn ở client.
   - Vòng đời đối soát: lập kỳ → gửi → (đăng nhập chủ ở `FrmOwnerSettlements` xác nhận, hoặc kế toán xác nhận thay) → ghi nhận đã thanh toán. Trạng thái và nút đổi đúng ở mỗi bước.
   - `paidAt` gửi lên là UTC: kiểm bằng chi tiết bảng sau khi lưu.
   - Quản lý mở hai màn ở chế độ chỉ xem, không thấy nút ghi.
   - Số việc chờ trên menu giảm sau khi xử lý.
4. **Ảnh chụp** hai form và hai khung dashboard ở 1366×768 (100%, 125%) và 1920×1080 (100%), lưu `docs/desktop-ui/anh-chup/dot-3/`, mỗi ảnh ≤ 300KB.
5. Mở toàn bộ form của app trong Visual Studio Designer nếu có VS, ghi bảng kết quả cuối cùng cho **mọi form**.

## 7. Tài liệu và bàn giao

- `docs/api/winforms-giao-dien.md`:
  - mục 4.9 và 4.10 chuyển sang trạng thái "Chạy";
  - mục 1 cập nhật số form chạy;
  - mục 2.3 bỏ các dòng đã có API;
  - mục 7 đánh dấu thứ tự làm đã xong.
- Báo cáo `docs/desktop-ui/dot-3-report.md`: như các đợt trước, thêm **bảng tổng kết cả 3 đợt** (mọi form, trạng thái, mở được Designer chưa, lỗi còn lại).
- Commit từng bước trên `feature/desktop-ui-rebuild`, message tiếng Việt. **Không push, không merge, không tạo PR.**
- Không đụng `docs/bao-cao/`, `deploy/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file hay commit.
