# Prompt: tìm và sửa lỗi treo của test giao diện WinForms

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh **`feature/desktop-ui-tests`** (đã push, chưa merge). Đọc trước:

- `HomestayBooking.Desktop.Tests/UiHarness.cs`: chạy form thật trên luồng STA riêng cho mỗi test, `Ui.Until` bơm `Application.DoEvents`, `DialogWatcher` tự bấm Yes/OK ở MessageBox, `Snapshot` chụp form bằng `DrawToBitmap`, có canh giờ 2 phút và chụp `dotnet-stack` khi một bước kẹt quá 25 giây.
- `HomestayBooking.Desktop.Tests/DesktopUiTests.cs`: 13 test.
- `.github/workflows/desktop-ui.yml`: chạy trên `windows-latest`, khởi động `HomestayBooking.DesktopHost` (SQLite seed) rồi `dotnet test` trong **cùng một bước** (runner Windows tắt tiến trình con khi bước kết thúc).
- Các form liên quan: `HomestayBooking.Desktop/View/FrmCreateBooking*.cs`, `FrmCatalogManagement*.cs`; helper `HomestayBooking.Desktop/Models/DesktopApi.cs` (`ToTable`, `ShowColumns`, `AllPagesAsync`).

## Hiện trạng

Run gần nhất (GitHub Actions run `40280149495`): **11/13 pass**. Hai test luôn hỏng:

1. `ConsultantCreatesBookingFromAvailability`: thân test chạy xong (đơn đã tạo, bước cuối "kết quả đặt hộ" đạt), nhưng luồng test kẹt trong `finally` của `Ui.Run` → `Snapshot` → `Control.DrawToBitmap` → `SendMessage` (WM_PRINT) và không trả về. Sau 2 phút watchdog báo `TimeoutException`; luồng STA đó vẫn sống.
2. `ManagerAddsUpdatesAndDeletesHoliday` (chạy sau): POST thêm ngày lễ tới server và trả 201, danh sách tải lại xong (log host: mọi request < 300 ms), nhưng luồng giao diện kẹt ở `FrmCatalogManagement.SelectTabAsync` → `DataGridView.OnDataSourceChanged` → `LayoutScrollBars` → `SetWindowPos`.

Các run trước đó, chỗ kẹt nhảy ngẫu nhiên giữa test duyệt bill, đặt hộ và ngày lễ; một lần `DoEvents` mất tới 118 giây. Đã thử: cài `WindowsFormsSynchronizationContext` cho luồng test; chỉ chụp và dọn form có `!InvokeRequired`. Vẫn kẹt.

Bằng chứng (đọc được từ máy này):
- Run 9: `/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/f738d737-380b-4aa0-a855-ea40e6dfdd40/scratchpad/run9/desktop-ui/` (ảnh `screenshots/*.png`, stack `screenshots/*-stack.txt`, danh sách cửa sổ `*.txt`, `host.log`, TRX).
- Run 8: thư mục `.../scratchpad/run8/desktop-ui/` cùng cấu trúc.
- Log đầy đủ: `gh run view 40280149495 --log` (nếu có mạng).

Giả thuyết đang có, **chưa chứng minh**: lỗi nằm ở harness (`DrawToBitmap` trên control Guna2 làm kẹt luồng STA đầu tiên; luồng kẹt đó còn giữ cửa sổ, khiến `SetWindowPos` ở test sau chờ chéo luồng), không phải lỗi của app (app thật không gọi `DrawToBitmap`). Hãy kiểm chứng hoặc bác bỏ bằng cách đọc stack đầy đủ, không đoán.

## Việc cần làm

1. Đọc kỹ các file stack (mọi luồng, không chỉ luồng test) và ảnh, xác định **nguyên nhân gốc** của từng chỗ kẹt: luồng nào chờ luồng nào, chờ message gì.
2. Nếu là lỗi harness: sửa harness để 13 test chạy độc lập, không có cách nào một test kẹt kéo test sau kẹt theo. Gợi ý cần cân nhắc: bỏ `DrawToBitmap` (thay bằng `Graphics.CopyFromScreen` theo `RectangleToScreen` của form, hoặc chỉ chụp khi test hỏng); đóng form bằng `Close()` rồi chờ message loop xử lý trước `Dispose()`; mỗi test chạy trong tiến trình hoặc AppDomain/ALC riêng nếu thật sự cần. Chọn cách đơn giản nhất mà đúng.
3. Nếu là lỗi của app (form, Guna2, binding `DataTable`, `ShowColumns`…): sửa trong `HomestayBooking.Desktop`, giải thích vì sao người dùng thật cũng gặp.
4. Không làm yếu test để cho qua: giữ nguyên các bước bấm nút và kiểm tra hiện có. Được thêm test nếu cần chứng minh bản sửa.

## Giới hạn

- Máy này là macOS: build được (`dotnet build HomestayBooking.sln`, có `EnableWindowsTargeting`) nhưng **không chạy được** test WinForms. Chỉ GitHub Actions trên Windows chạy được; bạn không push được nên hãy để thay đổi trong working tree, người dùng sẽ commit, push và chạy CI.
- Không commit, không push, không merge. Không sửa `HomestayBooking.Web` trừ khi chứng minh được lỗi nằm ở đó. Không đụng `docs/bao-cao/`, `docs/api/chay-desktop-host-windows.md`, `HomestayBooking.DesktopHost/README.md` (thay đổi riêng của người dùng). Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file.
- Build phải sạch: `dotnet build HomestayBooking.sln`. Test web không được trượt thêm: `dotnet test HomestayBooking.Tests --filter "FullyQualifiedName~Desktop"`.

## Báo cáo

Ghi `docs/api/desktop-ui-tests-report.md`: nguyên nhân gốc của từng chỗ kẹt kèm trích stack làm bằng chứng, file đã sửa và lý do, cách kiểm tra lại trên CI (lệnh `gh` cần chạy), điểm còn chưa chắc.
