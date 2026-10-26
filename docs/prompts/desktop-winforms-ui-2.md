# Prompt: Làm lại giao diện WinForms kéo thả được (đợt 2/3): 16 form nhân viên

Chạy trên **máy Windows**, cùng nhánh **`feature/desktop-ui-rebuild`**, nối tiếp đợt 1 đã được review.

## 0. Trước khi làm

1. Cập nhật nhánh, lấy các sửa sau review đợt 1:
   ```powershell
   cd D:\LTW
   git switch feature/desktop-ui-rebuild
   git pull
   dotnet build HomestayBooking.sln
   ```
2. Đọc lại `docs/prompts/desktop-winforms-ui-1.md`. **Mục 0 (chuẩn bị máy, chạy DesktopHost tại máy này) và mục 2 (quy ước Designer) giữ nguyên cho đợt này.** Đọc báo cáo `docs/desktop-ui/dot-1-report.md` và làm theo các quyết định đã chốt ở đợt 1: Form hay UserControl, lớp `ApiClient`, phiên chung, hàm dịch enum.
3. Đọc `docs/api/winforms-giao-dien.md` **mục 3.2 và mục 4**. Mục 4 là đặc tả đợt này: mỗi form có thành phần, API, lỗi đã thấy và việc cần làm.

## 1. Mục tiêu

Làm lại toàn bộ form nhân viên theo quy ước Designer của đợt 1, sửa hết các lỗi liệt kê trong mục 4, **giữ nguyên chức năng và API đang gọi**. Người dùng mở được mọi form trong Visual Studio Designer và sửa bằng kéo thả.

## 2. Danh sách form

| Form | Việc chính (chi tiết ở mục 4 của đặc tả) |
|---|---|
| `FrmEmployeeHome` | Khung Tư vấn dựng lại theo kiểu khung chủ ở đợt 1. Xóa `RolePlaceholderForm` |
| `FrmAccountantDashboard` | Khung Kế toán. Đổi menu "Kiểm kê & Cọc bổ sung" thành "Giao dịch & Cọc bổ sung" |
| `FrmAdminDashboard` | Khung Quản lý/Quản trị. Nút "Làm mới" chạy với mọi màn. Thêm menu "Giao dịch ngân hàng" mở `FrmBankAndAdditionalDeposit` **ẩn tab Cọc bổ sung** (API cọc bổ sung chỉ cho Kế toán) |
| `FrmBookingManagement` | Ô "phòng mới" bỏ kiểu gõ id; chỉ hiện khi trạng thái đích là `Confirmed`, chọn từ danh sách phòng nếu có API trả danh sách đó, không có thì ẩn và ghi vào báo cáo. Trạng thái đích lọc theo trạng thái hiện tại. Thêm bộ lọc homestay, ngày nhận phòng từ/đến, người phụ trách, "chưa giao". 5 bảng chi tiết có tiêu đề tiếng Việt |
| `FrmCreateBooking` | Kiểm ô bắt buộc (SĐT, họ tên, loại phòng) trước khi gửi. Tắt "Sau" ở trang cuối. Giờ giữ chỗ theo giờ Việt Nam |
| `FrmCustomerManagement` | Tự tải khi mở. Ô CCCD/thẻ SV chỉ đọc. Thống nhất thông báo |
| `FrmReceiptReview` | Nút "Mở PDF" (`Process.Start` với `UseShellExecute = true`). Tiêu đề cột, bộ lọc tiếng Việt. Giữ chế độ chỉ xem cho Quản trị |
| `FrmBankAndAdditionalDeposit` | Tách ô nhập theo từng thao tác (khớp tay, bỏ qua, chuyển trả) hoặc mỗi thao tác một hộp thoại nhỏ. Xem ảnh bill cọc bổ sung qua `fileUrl`, dùng chung khung xem ảnh với `FrmReceiptReview` |
| `FrmCatalogLookup` | Tiêu đề cột và nhóm khu vực tiếng Việt |
| `FrmCatalogManagement` | Nút loại đang chọn đổi màu. Khóa ô ngày khi sửa ngày lễ. Trạng thái không hiện mã kỹ thuật |
| `FrmAnalyticsReports` | Tiêu đề cột không còn dấu gạch dưới. Thêm thẻ số còn thiếu (tỷ lệ hủy/từ chối/hết hạn, cọc giữ lại, hoa hồng, tiền bù, cờ "số liệu tạm tính", thời gian xác nhận trung bình). Thêm lọc homestay |
| `FrmApprovalAndContract` | Chưa có API desktop. Bỏ lệnh gọi giả `management/unsupported`. Dựng lại giao diện, các nút để `Enabled = false`, kèm dòng giải thích "Chức năng này hiện làm trên web" và địa chỉ trang web tương ứng |
| `FrmEmployeeAccountAdmin` | Như trên: không gọi API giả, nút tắt, có dòng giải thích |
| `FrmRefundAndCompensation`, `FrmSettlementManagement` | **Không làm ở đợt này** (đợt 3 làm lại và nối API) |

Form nào đặc tả và code hiện tại khác nhau về chức năng thì theo code hiện tại (nó đang chạy đúng API), rồi ghi vào báo cáo.

## 3. Dọn khung chung

- Mọi form gọi API qua lớp `ApiClient` của đợt 1. **Xóa** `Models/DesktopApi.cs` (phần gọi HTTP), `Controllers/AccountantController.cs`, `Controllers/AdminController.cs`. DTO trong `Models/Accountant`, `Models/Admin` giữ lại nếu còn dùng, gom về một chỗ nếu hợp lý.
- Xóa mọi handler rỗng do Designer sinh (`FrmLogin_Load`, `guna2HtmlLabel1_Click`, `btnTransactionPrev_Click`…).
- Interface `IAccountantRefreshable` đổi tên cho đúng nghĩa (ví dụ `IRefreshable`), dùng cho mọi màn.
- Không còn `Application.Restart()` nào trong code.

## 4. Ngoài phạm vi

- Không sửa `HomestayBooking.Web`, `HomestayBooking.DesktopHost`, `HomestayBooking.Tests`. Lỗi API ghi vào báo cáo.
- Không đổi luồng form chủ của đợt 1, trừ sửa lỗi dùng chung.

## 5. Test

1. Test tĩnh quy ước Designer của đợt 1: **mở rộng ra mọi `*.Designer.cs`** (trừ hai form của đợt 3), không còn danh sách ngoại lệ.
2. Test giao diện hiện có (`DesktopUiTests`) phải còn pass. Test nào tìm control theo tên cũ thì sửa tên theo form mới, **không bỏ bớt kiểm tra**.
3. Thêm test cho các sửa ở mục 2:
   - đổi trạng thái đơn chỉ hiện trạng thái đích hợp lệ;
   - đặt hộ thiếu SĐT bị chặn trước khi gọi API;
   - khách hàng tự tải khi mở;
   - Quản lý mở được Giao dịch ngân hàng và không thấy tab Cọc bổ sung;
   - `FrmApprovalAndContract`, `FrmEmployeeAccountAdmin` không gọi API nào.
4. **Ảnh chụp** mỗi form ở 1366×768 (100%, 125%) và 1920×1080 (100%), lưu `docs/desktop-ui/anh-chup/dot-2/`, mỗi ảnh ≤ 300KB.
5. Mở từng form trong Visual Studio Designer nếu có VS, ghi kết quả.

## 6. Tài liệu và bàn giao

- `docs/api/winforms-giao-dien.md`: cập nhật mục 3.2 (lỗi chung nào đã sửa) và mục 4 (trạng thái, lỗi còn lại của từng form).
- Báo cáo `docs/desktop-ui/dot-2-report.md`: file thêm/sửa/xóa, mỗi lỗi ở mục 4 đặc tả đã sửa hay chưa (bảng), kết quả test, danh sách ảnh, kết quả mở Designer, lỗi API phát hiện.
- Commit theo từng form hoặc từng nhóm form trên `feature/desktop-ui-rebuild`, message tiếng Việt kiểu `feat(desktop): ...` / `fix(desktop): ...`. **Không push, không merge, không tạo PR.**
- Không đụng `docs/bao-cao/`, `deploy/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file hay commit.
