# Điều tra treo test WinForms

Ngày: 2026-10-26. Nhánh: `feature/desktop-ui-tests`. Mốc mã nguồn trước sửa: `d20bb8f`.

## Kết luận và giới hạn bằng chứng

Harness có hai lỗi xác định được: chụp form bằng `DrawToBitmap` chặn STA trong xử lý native của `WM_PRINT`, và không duy trì vòng `Application.Run`, khiến synchronization context bị gỡ giữa các bước. Stack ngày lễ chứng minh code bind UI thực tế chạy trên ThreadPool, không phải STA.

Không đủ dữ liệu để xác định chính xác HWND đích và toàn bộ chuỗi chờ native của hai chỗ kẹt. `dotnet-stack` chỉ lưu managed frames, che native frames bằng `[Native Frames]`/`?!?`, không lưu tham số message. Không thể khẳng định “Guna2 deadlock” hoặc “STA đặt hộ giữ cửa sổ làm STA ngày lễ chờ” từ những file này. Bản sửa loại bỏ thao tác gây kẹt đã biết, sửa môi trường message loop và cô lập từng ca bằng tiến trình; chưa được chạy WinForms trên Windows sau sửa.

Không thay đổi mã ứng dụng Desktop/Web: `Program.Main` của ứng dụng thật đã gọi `Application.Run(new FrmLogin())`; ứng dụng không gọi `DrawToBitmap`. Các hàm API được đọc không dùng `ConfigureAwait(false)` để cố ý đưa continuation UI sang ThreadPool.

## Nguồn đã đọc

Đọc đủ mọi thread trong cả năm file stack, đối chiếu TRX, danh sách cửa sổ, `host.log` và ảnh lỗi của run 8/9. Thư mục gốc trên máy điều tra:

```text
/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/f738d737-380b-4aa0-a855-ea40e6dfdd40/scratchpad/
```

| Run | File dưới `desktop-ui/screenshots/` | Số thread |
| --- | --- | ---: |
| 8 | `ConsultantCreatesBookingFromAvailability-man-hinh-stack.txt` | 16 |
| 8 | `ManagerAddsUpdatesAndDeletesHoliday-KET-thêm ngày lễ-man-hinh-stack.txt` | 17 |
| 9 | `ConsultantCreatesBookingFromAvailability-man-hinh-stack.txt` | 16 |
| 9 | `ManagerAddsUpdatesAndDeletesHoliday-KET-thêm ngày lễ-man-hinh-stack.txt` | 17 |
| 9 | `ManagerAddsUpdatesAndDeletesHoliday-man-hinh-stack.txt` | 16 |

Lệnh lấy log trực tiếp `gh run view 40280149495 --log` không truy cập được `api.github.com` trong môi trường này. Kết luận dưới đây dựa trên artifact cục bộ, không dựa trên log trực tiếp chưa đọc được.

## 1. Đặt hộ: thân test xong, thao tác chụp chặn trong native code

Run 9, thread STA `0x1630`, stack đọc từ điểm gọi vào trong:

```text
Ui.<Run>b__0
  Ui.Snapshot
    Control.DrawToBitmap
      PInvokeCore.SendMessage
        NativeWindow.Callback
          Control.WndProc / NativeWindow.DefWndProc
            ?!?
              NativeWindow.Callback / Control.WndProc / Control.DefWndProc
                ?!?
                  NativeWindow.Callback / Control.WndProc / Control.DefWndProc
                    [Native Frames]
```

Run 8 có cấu trúc tương tự trên STA `0x20E0`, với `CallWindowProc` lồng nhau. Không có `SubmitAsync`, HTTP hay thao tác của thân test ở trên đường gọi này. TRX ghi timeout ở `Ui.Run`, bước cuối “kết quả đặt hộ”. Ảnh form `ConsultantCreatesBookingFromAvailability-FrmCreateBooking-758140.png` của run 9 hiện mã đơn `FJ261026-009`, trạng thái `AwaitingPayment`; host ghi POST booking 201 trong 116.0525 ms, GET chi tiết 200 trong 8.0506 ms.

Thread chạy xUnit `0x19F0` ở thời điểm dump đang ở `StreamReader.ReadToEnd -> Ui.ScreenSnapshot -> Ui.Run`: nó chờ pipe stdout của `dotnet-stack`, không phải chờ API. `WaitForExit(30000)` trong harness cũ nằm **sau** `ReadToEnd()` không giới hạn, nên không thật sự giới hạn thời gian thu stack. Đây là lý do cần sửa cả đường chẩn đoán, ngoài timeout STA hai phút.

[Mã nguồn `Control.DrawToBitmap` của WinForms .NET 10](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Control.cs#L4580) cho biết lệnh gửi là `WM_PRINT` (`0x0317`) với các cờ vẽ cả child, client và nonclient. Vì vậy xác định được message **ngoài cùng** và thao tác harness gây chặn. Chưa xác định được message/HWND ở các frame native sâu hơn; stack cũng không chỉ ra một method Guna2 đang giữ lock.

Các thread khác ở dump đặt hộ run 9:

| Thread | Trạng thái/đường gọi |
| --- | --- |
| `0x794` | testhost `Task.Wait` chờ test execution |
| `0x19B4` | adapter xUnit `WaitOne` chờ lượt test |
| `0x13B0`, `0x1088` | ThreadPool WaitThread / IOCompletionPoller |
| `0x1ED4` | socket điều khiển VSTest `Socket.Poll`, không phải request DesktopHost |
| `0x14A8`, `0x2070` | GateThread / TimerQueue |
| `0x1CF4`, `0x22CC` | TestRequestHandler / xUnit MessageBus chờ việc |
| `0x1F38` | SystemEvents window thread, native frames bị ẩn |
| `0x14E0`, `0x18D4` | worker ThreadPool chờ semaphore |
| `0x2064`, `0x1F2C` | watchdog / DialogWatcher đang Sleep |

Không có thread managed nào trong dump này chứng minh nó là bên nhận message mà STA đang chờ. Một child HWND thuộc thread không bơm message là khả năng cần native dump để kiểm chứng, không phải kết luận từ artifact.

## 2. Ngày lễ: continuation bind bảng chạy trên ThreadPool; STA cũng chụp trong finally

Run 9, cả dump 25 giây và dump hai phút đều có thread `0x14E0`:

```text
ThreadPool.WorkerThreadStart
  ThreadPoolWorkQueue.Dispatch
    ThreadPoolBoundHandleOverlapped.CompletionCallback
      HttpConnection.SendAsync / HttpClient.SendAsync
        AdminController.SendAsync
          DesktopApi.AllPagesAsync
            FrmCatalogManagement.SelectTabAsync.MoveNext
              DataGridView.OnDataSourceChanged
                RefreshColumnsAndRows / ComputeLayout / AdjustFillingColumns
                  LayoutScrollBars
                    Control.SetVisibleCore
                      PInvoke.SetWindowPos
                        [Native Frames]
```

Đây là **ThreadPool từ HTTP completion**. Không có `Control.InvokeMarshaledCallbacks`, `WindowsFormsSynchronizationContext` dispatch hay `Ui.Until` bên dưới continuation. Nó đang đổi `dgvItems.DataSource` ngoài UI thread. Run 8 lặp lại cùng đường gọi trên ThreadPool `0x484`.

STA ngày lễ run 9 là `0x13AC`, đã ở:

```text
Ui.<Run>b__0 -> Ui.Snapshot -> Control.DrawToBitmap
  -> PInvokeCore.SendMessage -> Form.WndProc / Control.DefWndProc -> [Native Frames]
```

Nghĩa là lúc chụp stack sau 25 giây, STA **đã rời thân test vào finally**; không còn kẹt trong `Ui.Until` chờ thêm ngày lễ. Run 8, STA `0x1750` cũng ở Snapshot; TRX cuối cùng giữ được lỗi ban đầu `Hết 20000 ms ... thêm ngày lễ`. Run 9 cuối cùng báo timeout tổng vì chính cleanup tiếp tục chặn.

Host run 9 đã trả POST holidays 201 trong 17.6572 ms, rồi ba GET tải lại lần lượt 2.9151 / 1.5799 / 1.4590 ms. Trong stack, `AllPagesAsync` đang hoàn tất kết quả, không lặp vô hạn hay chờ server. `ToTable`/`ShowColumns` không nằm trên đường bind danh mục này: form bind trực tiếp danh sách DTO.

Chuỗi chờ quan sát được là test runner `0x19F0` chờ STA ngày lễ qua `Thread.Join`; STA chặn trong native `WM_PRINT`, đồng thời ThreadPool `0x14E0` chặn trong native `SetWindowPos`. Bên nhận và sự phụ thuộc hai chiều bên trong User32 chưa thể khôi phục từ managed stack. Theo [tài liệu `SetWindowPos`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos), lời gọi có thể liên quan việc xử lý trên thread sở hữu HWND và các message bố trí cửa sổ; không thể từ tên hàm suy ra chắc chắn nó đang đợi `WM_WINDOWPOSCHANGING`, `WM_SYNCPAINT` hay một message cụ thể khác.

Các thread nền VSTest/ThreadPool/SystemEvents trong hai dump ngày lễ giữ các vai trò như bảng trên. Các thread mới là STA `0x13AC`, DialogWatcher `0x5AC`, watchdog `0xAD8` đang đọc stdout của dotnet-stack ở dump 25 giây; các worker rảnh `0x88C`, `0x18D4`/`0x1B00`. Ở dump hai phút, chính runner `0x19F0` chuyển sang đọc stdout để thu stack.

STA đặt hộ `0x1630` không còn trong cả hai dump ngày lễ run 9; `0x20E0` cũng không còn trong dump ngày lễ run 8. Danh sách cửa sổ lúc đó chỉ ghi form “Quản lý Danh mục”, không còn form đặt hộ. Vì vậy giả thuyết STA đặt hộ vẫn sống và trực tiếp chặn test ngày lễ **không được các dump này hỗ trợ**. Không loại trừ mọi tác động trước thời điểm dump, nhưng không dùng giả thuyết đó làm nguyên nhân đã chứng minh.

## 3. Vì sao cài synchronization context một lần không đủ

Harness cũ cài `WindowsFormsSynchronizationContext`, gọi thân test trực tiếp, rồi dùng các vòng `Application.DoEvents()` rời rạc. Không có `Application.Run` bao quanh chúng.

Đối chiếu mã nguồn WinForms .NET 10:

1. [`Application.DoEvents`](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Application.cs#L875) chạy một message loop riêng.
2. [`ThreadContext.RunMessageLoopInner`](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Application.ThreadContext.cs#L738) giảm bộ đếm khi loop kết thúc; nếu về 0, gọi `WindowsFormsSynchronizationContext.Uninstall(false)`.
3. [`Uninstall`](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/WindowsFormsSynchronizationContext.cs#L143) phục hồi context trước đó hoặc context cơ bản. Click gọi trực tiếp sau đó không còn được bảo đảm bắt UI context tại `await`.

Đây là cơ chế mã nguồn phù hợp với bằng chứng ThreadPool của cả hai run. Một `Application.Run` tồn tại suốt thân test giữ loop ngoài cùng; `DoEvents` và `ShowDialog` chỉ còn là loop lồng nhau. Test hồi quy sẽ kiểm chứng cơ chế này trên runtime Windows thực tế của CI.

Ảnh toàn màn hình lỗi ở cả hai run bị cửa sổ terminal của runner che phần lớn form. Chúng xác nhận form còn hiện và không thấy MessageBox; không chứng minh được toàn bộ nội dung bảng. Ảnh form đặt hộ cuối cùng có kết quả đơn; việc ảnh tồn tại cũng cho thấy `DrawToBitmap` có thể trả về rất muộn, không nhất thiết deadlock vĩnh viễn.

## Thay đổi

| File | Thay đổi và lý do |
| --- | --- |
| `HomestayBooking.Desktop.Tests/UiHarness.cs` | Chạy body sau khi vào `Application.Run(ApplicationContext)` trên STA; giữ cách click/Until/Settle hiện có. Kiểm tra STA và context ở các bước. Chụp pixel bằng `RectangleToScreen` + `Graphics.CopyFromScreen`, không gọi `DrawToBitmap`/`WM_PRINT`. Gọi `Close`, trả về message loop bằng `BeginInvoke`, rồi `Dispose` phần còn lại. Ghi lỗi thân test trước cleanup để không mất lỗi gốc. |
| `HomestayBooking.Desktop.Tests/UiHarness.cs` | Watchdog theo dõi cả click/chụp/đóng/dọn; dừng và join khi xong. Thu stdout/stderr dotnet-stack bất đồng bộ, giới hạn đợi 10 giây, kill collector khi quá hạn. Liệt kê cả HWND con, PID/TID và parent. Đọc text bằng `SendMessageTimeout(WM_GETTEXT)` 200 ms để watcher/chẩn đoán không chờ UI vô hạn. |
| `HomestayBooking.Desktop.Tests/UiWorker.cs` | Mỗi ca xUnit mở một worker process cùng assembly, entry point `[STAThread]`, gọi lại đúng method và tham số. Bật kiểm tra truy cập control chéo luồng và cho lỗi message loop nổi lên. Parent drain cả hai pipe, giới hạn ca hai phút, kill process tree khi timeout, đợi process thoát trước ca mới. Nếu không thu hồi được worker thì chặn mở ca mới. |
| `HomestayBooking.Desktop.Tests/HomestayBooking.Desktop.Tests.csproj` | `OutputType=Exe` và entry point worker; vẫn dùng VSTest/xUnit để discover và báo kết quả. Không thêm package. |
| `HomestayBooking.Desktop.Tests/DesktopUiTests.cs` | Chuyển hai tham số theory đăng nhập sang worker. Hai cast `string?` chỉ sửa annotation nullable để hết cảnh báo; so sánh và assertion giữ nguyên. |
| `HomestayBooking.Desktop.Tests/UiHarnessTests.cs` | Hai ca hồi quy: giữ context/STA qua DoEvents, ShowDialog và await bind DataGridView; cố ý kẹt STA có cửa sổ, kiểm tra worker bị thu hồi và worker mới có trạng thái sạch. |

Tiến trình riêng cần thiết để giới hạn lỗi native: một STA chặn trong User32 không thể hủy an toàn bằng cancellation token; ALC không hủy thread/cửa sổ native. Chỉ đổi cách chụp không bảo đảm cleanup hoặc handler khác sẽ không kẹt. Worker dùng lại assembly test để không cần thêm project hay runner package.

Giữ nguyên workflow: DesktopHost và `dotnet test` vẫn trong cùng một bước. Cô lập worker nằm trong harness, nên chạy `dotnet test` trên máy Windows khác cũng có cùng bảo đảm, không phụ thuộc chia filter trong CI. Cơ sở dữ liệu host vẫn dùng chung trong lượt chạy và các ca chạy tuần tự; cô lập ở đây là tiến trình/UI/static state, không phải reset dữ liệu server cho từng ca.

## Kiểm chứng tại máy macOS

Build solution bản sửa cuối cùng **exit 0, 0 Warning(s), 0 Error(s)**. Do sandbox không truy cập được NuGet và build server mặc định không hoạt động ổn định, lệnh chạy dùng package cache có sẵn, CLI home tạm và tắt build server:

```bash
env DOTNET_CLI_HOME=/private/tmp/desktop-ui-dotnet \
  NUGET_PACKAGES=/Users/phamngocphu/.nuget/packages \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
  DOTNET_EnableDiagnostics=0 \
  dotnet build HomestayBooking.sln --disable-build-servers -m:1 \
    -p:NuGetAudit=false --ignore-failed-sources
```

Log cuối: `/private/tmp/desktop-ui-verified-build.log`. Lần build có biên dịch lại Desktop trước đó còn hai cảnh báo CS0649 có sẵn ở `FrmBookingManagement.Designer.cs` và `FrmCustomerManagement.Designer.cs` (field `components` chưa được gán); không sửa các form chỉ để xóa cảnh báo. Lệnh build có restore mặc định trước khi dùng cache không thành công vì DNS của `api.nuget.org` bị chặn, không phải lỗi biên dịch.

Đã chạy cùng filter được yêu cầu, với các cờ tránh restore/build server:

```bash
dotnet test HomestayBooking.Tests --filter "FullyQualifiedName~Desktop" \
  --no-restore --disable-build-servers -m:1
```

VSTest build các project xong rồi **Test Run Aborted** trước khi chạy test, do `SocketException (13): Permission denied` tại `SocketServer.Start -> TcpListener.Start -> Socket.Bind`. Log: `/private/tmp/desktop-ui-web-tests-final.log`. Vì vậy **chưa xác nhận được suite web không có regression** bằng lệnh chuẩn trong môi trường này.

Thử runner engine xUnit trong `/private/tmp/desktop-web-inprocess/`, không thêm runner vào repo: discover được 77 ca theo tên đầy đủ chứa `Desktop`, nhưng hai ca Kestrel thật cũng bị chặn bind socket, và phương án tạm không hoàn tất được một lượt chạy đáng tin cậy. Đã dừng thử nghiệm; không dùng nó làm bằng chứng suite pass hoặc quy lỗi mới cho ứng dụng.

`git diff --check` exit 0. Đối chiếu tự động `DesktopUiTests.cs` với HEAD xác nhận toàn bộ thao tác/assertion của 13 ca gốc giống hệt sau khi chuẩn hóa đúng ba thay đổi cho phép: truyền tham số theory và hai annotation nullable. Không chạy WinForms trên macOS. Không commit/push/merge; các file tài liệu riêng của người dùng không được chỉnh sửa.

## Kiểm tra lại trên Windows CI

Sau khi commit/push thay đổi vào nhánh, workflow path filter tự kích hoạt, hoặc chạy thủ công:

```bash
gh workflow run desktop-ui.yml --ref feature/desktop-ui-tests
gh run list --workflow desktop-ui.yml --branch feature/desktop-ui-tests --limit 5
```

Lấy ID mới từ danh sách, thay `RUN_ID` ở các lệnh sau:

```bash
gh run watch RUN_ID --exit-status
gh run view RUN_ID --log > /private/tmp/desktop-ui-ci.log
gh run download RUN_ID --name desktop-ui --dir /private/tmp/desktop-ui-ci
```

Yêu cầu chấp nhận: **15/15 pass** (13 ca nghiệp vụ gốc + 2 ca harness). Bốn dòng theory đăng nhập phải gọi đúng dashboard. Đặt hộ phải có `AwaitingPayment`; ngày lễ phải hoàn tất thêm/sửa/xóa. Log ghi một PID worker cho mỗi ca, thread bước UI là STA, không có lỗi truy cập control chéo luồng. Ca hồi quy timeout cố ý tạo artifact `BlockWithWindow-TIMEOUT-*`; artifact đó là bình thường nếu test hồi quy pass. Các ca nghiệp vụ không được có artifact `TIMEOUT`/`KET`.

Có thể chạy trước riêng hai ca hồi quy trên Windows, không cần DesktopHost:

```powershell
dotnet test HomestayBooking.Desktop.Tests -c Release --filter "FullyQualifiedName~UiHarnessTests" --logger "console;verbosity=normal"
```

Chạy lại kiểm tra web ở môi trường cho phép VSTest mở socket:

```bash
dotnet build HomestayBooking.sln
dotnet test HomestayBooking.Tests --filter "FullyQualifiedName~Desktop"
```

Để xác nhận ổn định, chạy workflow lại vài lượt với host/SQLite seed mới cho mỗi lượt. Không dùng retry riêng ca nghiệp vụ trong cùng database để che lỗi hoặc tạo lại đơn/ngày lễ trùng dữ liệu.

## Phần còn chưa chắc

- Chưa chạy được 15 ca WinForms trên macOS; build không chứng minh hành vi Windows đã hết treo.
- Không xác định được HWND nhận `WM_PRINT` sâu bên dưới, thread nhận message đó, hay message cụ thể khiến `SetWindowPos` chờ trong artifact cũ. Nếu CI còn lỗi, dùng PID/TID/HWND mới và thu **native full dump khi còn kẹt**, trước khi parent kill. Ví dụ khi tái hiện trên Windows có Sysinternals ProcDump: `procdump64 -accepteula -ma <worker-pid> worker-hang.dmp`, mở bằng WinDbg, xem toàn bộ native stack và wait chain. Managed stack một mình không đủ kết luận chuỗi chờ User32.
- `CopyFromScreen` ghi cả cửa sổ đang che form. Nó tránh lệnh in control, không tạo ảnh render độc lập với desktop; cần xem ảnh cùng log/TRX, không dùng ảnh bị che làm bằng chứng dữ liệu chưa tải.
- Nếu có lỗi cross-thread thật bên trong ứng dụng hoặc thư viện khi chạy với message loop đúng, kiểm tra nghiêm ngặt sẽ làm worker thất bại và giữ stack. Không tắt kiểm tra đó để làm CI xanh.
