# Prompt: Làm lại giao diện WinForms kéo thả được (đợt 1/3): khung chung, đăng nhập, chủ homestay

Chạy trên **máy Windows**. Repo `https://github.com/FushiMasterDev/LTW`, nhánh **`feature/desktop-ui-rebuild`**.

Làm lại giao diện chia 3 đợt:

| Đợt | Nội dung |
|---|---|
| **1 (prompt này)** | Khung chung, `FrmLogin`, toàn bộ form chủ homestay |
| 2 | Làm lại 16 form nhân viên đang có theo cùng quy ước |
| 3 | Nối form Hoàn tiền/Khoản bù và Đối soát khi API kế toán xong |

Mục 2 (quy ước Designer) áp dụng cho cả 3 đợt.

## 0. Chuẩn bị máy

1. Cài **.NET 10 SDK x64 ≥ 10.0.400** (theo `global.json`) và Git. Cài thêm **Visual Studio 2022 17.14+ hoặc Visual Studio 2026** có workload ".NET desktop development" nếu muốn tự mở Designer kiểm.
2. Lấy code:
   ```powershell
   git clone https://github.com/FushiMasterDev/LTW.git D:\LTW
   cd D:\LTW
   git switch feature/desktop-ui-rebuild
   dotnet build HomestayBooking.sln
   ```
3. Chạy **DesktopHost ngay trên máy Windows này** để test, không dùng host trên Mac. Host trên Mac là dữ liệu demo dùng chung, test sẽ ghi dữ liệu vào đó.
   ```powershell
   dotnet run --project HomestayBooking.DesktopHost
   # http://localhost:5080, Swagger: http://localhost:5080/swagger
   ```
   Muốn làm sạch dữ liệu thì dừng host rồi chạy lại với `-- --reset`. Hướng dẫn đầy đủ: `docs/api/chay-desktop-host-windows.md`.
4. Chạy app: `dotnet run --project HomestayBooking.Desktop`. Chạy test giao diện: theo các bước trong `.github/workflows/desktop-ui.yml` (bật host, đặt `FUJI_API_URL=http://127.0.0.1:5080`, `dotnet test HomestayBooking.Desktop.Tests`).

Tài khoản demo, mật khẩu chung `Fuji@2026`:

| Vai trò | Đăng nhập |
|---|---|
| Nhân viên | `NV001`, `KT001`, `QL001`, `AD001` |
| Chủ homestay | SĐT `0912000001`, `0912000002` (mỗi chủ 7 homestay) |

## 1. Đọc trước

Theo thứ tự:

1. `docs/api/winforms-giao-dien.md`: danh sách mọi form, API từng form gọi, lỗi đã thấy. **Mục 3 (khung chung) và mục 5 (form chủ) là đặc tả của đợt này.**
2. `docs/api/desktop-api-theo-vai-tro.md`: API theo vai trò, mục 7 là toàn bộ API chủ homestay, có JSON mẫu.
3. `HomestayBooking.Desktop/`: `Program.cs`, `Properties/Settings.settings`, `Models/*`, `Controllers/*`, `View/FrmLogin.*`, `View/FrmEmployeeHome.*`, `View/FrmBookingManagement.*` (form mẫu đang chạy).
4. `HomestayBooking.Web/DesktopApi/DesktopOwnerDtos.cs` và `HomestayBooking.Web/Controllers/Api/Desktop/Owner/*`: JSON chính xác của API chủ.
5. `HomestayBooking.Desktop.Tests/*` và `.github/workflows/desktop-ui.yml`: cách test mở form thật.

Swagger `http://localhost:5080/swagger` là nguồn sự thật về request/response. Khác tài liệu thì theo Swagger và ghi vào báo cáo.

## 2. Quy ước Designer (cả 3 đợt)

Mục tiêu chính: **mọi form mở được trong Visual Studio Designer và sửa bằng kéo thả** mà không làm hỏng code.

### 2.1 File

- Mỗi form có đúng bộ ba `FrmX.cs` (logic), `FrmX.Designer.cs` (chỉ `InitializeComponent` và khai báo field), `FrmX.resx` (nếu có ảnh/tài nguyên). Lớp đầu tiên trong `FrmX.cs` phải là chính form đó. Không đặt hai form trong một file (ví dụ `RolePlaceholderForm` hiện nằm trong `FrmEmployeeHome.Designer.cs`).
- Màn nhúng vào khung (`TopLevel = false`) được phép đổi từ `Form` sang `UserControl` nếu thấy hợp hơn, nhưng phải giữ thống nhất trong cả đợt. Editor nhiều tab thì mỗi tab là một `UserControl` riêng (`UcXxx`) có Designer riêng, không dồn mọi control vào một file Designer vài nghìn dòng.
- Form có constructor không tham số (Designer và test cần). Dữ liệu phiên lấy từ `AppSession`, không bắt buộc truyền qua constructor.

### 2.2 `InitializeComponent`

Viết **đúng định dạng Visual Studio tự sinh cho .NET 8 trở lên**:

- Có `components = new System.ComponentModel.Container()` khi có component.
- Tạo control, `SuspendLayout()` các container, mỗi control một khối có comment `// tenControl`, **mỗi dòng một thuộc tính**, rồi `Controls.Add(...)`, `ResumeLayout(false)`, `PerformLayout()`.
- `DataGridView` có `((System.ComponentModel.ISupportInitialize)dgv).BeginInit()/EndInit()`.
- Field khai báo ở cuối file.
- **Cấm** trong `InitializeComponent`: lambda, vòng lặp, `if`, biến cục bộ dùng chung (kiểu `var muted = ...`), gọi hàm tự viết, LINQ, chuỗi lệnh nối bằng `;` trên một dòng. Màu viết thẳng `Color.FromArgb(r, g, b)`. Item ComboBox cố định dùng `Items.AddRange(new object[] { ... })`.
- Sự kiện gắn bằng tên hàm: `btnLogin.Click += btnLogin_Click;`, hàm nằm trong `FrmX.cs`. Không gắn sự kiện bằng lambda trong constructor (Designer không thấy). Riêng sự kiện cần `async` thì viết `private async void btnLogin_Click(object? sender, EventArgs e)` có `try/catch` hiện lỗi.
- Không gọi API, không đọc `Settings`, không đọc `AppSession` trong `InitializeComponent` hoặc constructor. Tải dữ liệu ở `Load`/`Shown`.

### 2.3 Bố cục

- Không đặt vị trí bằng số cứng cho cả màn hình. Dùng `TableLayoutPanel`/`FlowLayoutPanel`, `Dock`, `Anchor`, `Padding`, `Margin`. Bảng dữ liệu `Dock = Fill` trong ô của nó.
- `AutoScaleMode = Font`, thống nhất `AutoScaleDimensions` giữa các form. Kiểm ở **1366×768 tỉ lệ 100% và 125%**, **1920×1080 tỉ lệ 100% và 150%**: không chữ bị cắt, không control đè nhau, không thanh cuộn ngang ở khung chính.
- Khung chính kích thước tối thiểu 1280×720. Mở ra ở giữa màn hình, cho phép phóng to.
- Bảng màu giữ như hiện tại: xanh rêu `30,58,47` (menu, nút chính), cam `200,90,50` (mục đang chọn, nút nhấn mạnh), xám chữ phụ `104,120,124`, viền `220,227,231`, nền nội dung trắng/kem. Font `Segoe UI`.
- Mọi bảng: chỉ hiện cột cần đọc, **tiêu đề tiếng Việt**, enum hiện chữ tiếng Việt, tiền `N0` + " đ", ngày `dd/MM/yyyy`, giờ theo **Asia/Ho_Chi_Minh** (không theo múi giờ máy).

### 2.4 Hành vi chung

- Mỗi thao tác gọi API khóa nút đó (và ô nhập liên quan) cho tới khi xong, con trỏ chờ. Không gửi hai lần.
- Màn danh sách tự tải trang 1 khi mở. Nút "Trước"/"Sau" tắt ở trang đầu/cuối.
- Lỗi API hiện câu `detail` của ProblemDetails trong MessageBox có tiêu đề "Fuji Homestay" và icon cảnh báo. Thành công hiện thông báo ngắn ở thanh trạng thái hoặc MessageBox icon thông tin, thống nhất một kiểu.
- Kiểm ô bắt buộc trước khi gọi API, báo ngay cạnh ô hoặc bằng MessageBox, rồi focus vào ô sai.
- Tên control theo tiền tố đang dùng: `btn`, `txt`, `cbo`, `dgv`, `lbl`, `pnl`, `tab`, `dtp`, `num`, `chk`, `pic`, `uc`.

## 3. Phạm vi đợt 1

### 3.1 Khung chung (đặc tả: `winforms-giao-dien.md` mục 3.2)

1. **Một lớp gọi API duy nhất** (ví dụ `Models/ApiClient.cs`):
   - GET/POST/PUT/DELETE JSON.
   - `multipart/form-data` (ảnh, giấy tờ của chủ).
   - Tải file riêng tư bằng token (bill, giấy tờ).
   - Gom mọi trang của danh sách.
   - Đọc ProblemDetails.

   Giữ chữ ký công khai của `DesktopApi`, `AccountantController`, `AdminController` nhưng cho chúng gọi qua lớp mới, để 16 form nhân viên vẫn chạy như cũ. Đợt 2 sẽ bỏ hẳn các lớp này.
2. **Phiên chung** cho nhân viên và chủ (`Kind` = Employee/Owner, token, hạn, tên hiển thị, mã NV hoặc SĐT). `EmployeeSession` cũ đổi theo hoặc bọc lại, form nhân viên không được hỏng.
3. **Hết phiên (401):** bỏ `Application.Restart()`. Đóng khung đang mở và quay về `FrmLogin`, giữ địa chỉ máy chủ và lựa chọn đăng nhập. Hiện thông báo "Phiên đăng nhập đã hết hạn" một lần, kể cả khi nhiều request cùng nhận 401.
4. **Hàm dùng chung:** dịch enum sang tiếng Việt (trạng thái đơn, bill, đối soát, khóa phòng, loại ngày, loại giấy tờ, kênh…), định dạng tiền/ngày/giờ Việt Nam, đặt tiêu đề cột bảng.

### 3.2 `FrmLogin` (đặc tả: mục 4.1)

- Lựa chọn **Nhân viên / Chủ homestay**. Nhãn và placeholder đổi theo ("Mã nhân viên" / "Số điện thoại"). Gọi `/auth/login` với `employeeCode` hoặc `/owner/auth/login` với `phoneNumber`.
- Nhớ lựa chọn cuối và mã/SĐT (nếu tick "Ghi nhớ") trong `Settings` (thêm setting mới, giữ `ApiBaseUrl`, `RememberEmployeeCode`).
- Chủ đăng nhập xong mở `FrmOwnerHome`. Nhân viên giữ nguyên logic mở khung theo `position`.
- Sửa layout nút "Quên mật khẩu". Ô địa chỉ máy chủ giữ như hiện tại.

### 3.3 Form chủ homestay (đặc tả: mục 5.1 đến 5.8)

Làm đủ 8 màn theo đúng đặc tả, gọi đúng API trong `desktop-api-theo-vai-tro.md` mục 7:

| Form | Ghi chú thêm |
|---|---|
| `FrmOwnerHome` | Khung menu trái giống khung nhân viên, có Làm mới và Đăng xuất |
| `FrmOwnerDashboard` | Bấm thẻ "đơn chờ xác nhận" mở màn Đơn đã lọc sẵn |
| `FrmOwnerSchedule` | |
| `FrmOwnerBookings` | Nút theo trạng thái đơn như bảng ở mục 5.4. Xác nhận có ô chọn phòng trống cùng loại lấy từ chi tiết đơn. Từ chối hỏi lại trước khi gửi |
| `FrmOwnerCalendar` | Lưới tháng tự vẽ bằng `TableLayoutPanel`; 409 khi khóa trùng đơn thì hiện danh sách ngày trùng |
| `FrmOwnerHomestays` | Danh sách bên trái, editor bên phải gồm 8 tab, mỗi tab là một `UserControl` |
| `FrmOwnerReviews` | |
| `FrmOwnerSettlements` | Nút chỉ bật khi bảng ở trạng thái `Sent` |

Lưu ý dữ liệu:

- DesktopHost **không phục vụ ảnh** `/images/...`, `/uploads/...`. Ảnh tải lỗi thì hiện khung xám có chữ "Không tải được ảnh", không MessageBox.
- Upload: chặn ở client trước khi gửi (đuôi file, ≤ 5MB), vẫn hiện lỗi API nếu máy chủ từ chối.
- Bật/tắt bán loại phòng trả về danh sách đơn sắp tới cần liên hệ: phải hiện danh sách đó cho chủ.
- Sửa vị trí có thể đưa homestay về chờ duyệt: hỏi xác nhận trước.

### 3.4 Ngoài phạm vi đợt 1

- Không sửa giao diện 16 form nhân viên, ngoài việc tối thiểu để chạy với khung chung mới.
- Không sửa `HomestayBooking.Web`, `HomestayBooking.DesktopHost`, `HomestayBooking.Tests`. Thấy lỗi API thì ghi vào báo cáo, không tự sửa phía máy chủ.

## 4. Test

1. **Test tĩnh cho quy ước Designer** (thêm vào `HomestayBooking.Desktop.Tests`, chạy được cả khi không có host): đọc mọi `View/**/*.Designer.cs` và fail nếu có lambda `=>`, `for`/`foreach`/`if`, hoặc một dòng có hơn một câu lệnh kết thúc bằng `;` (bỏ qua chuỗi). Ban đầu chỉ áp cho file của đợt 1 (danh sách cho phép rõ ràng), đợt 2 mở rộng ra toàn bộ.
2. **Test giao diện** theo kiểu `DesktopUiTests` hiện có:
   - Đăng nhập chủ đúng mở `FrmOwnerHome`; sai mật khẩu hiện câu của máy chủ.
   - Mở từng màn chủ, bảng có dữ liệu, không MessageBox lỗi.
   - Xác nhận một đơn chờ.
   - Khóa rồi mở khóa một khoảng ngày trống.
   - Trả lời một đánh giá (nếu seed có).
   - Hết phiên giả lập (token hỏng) quay về `FrmLogin`, không restart app.
3. Toàn bộ test giao diện đang có của nhân viên phải còn pass.
4. **Ảnh chụp:** mỗi màn đợt 1 chụp ở 1366×768 (100%, 125%) và 1920×1080 (100%), lưu `docs/desktop-ui/anh-chup/dot-1/<ten-form>-<kich-thuoc>.png`, nén PNG, mỗi ảnh ≤ 300KB.
5. Mở thử từng form mới trong Visual Studio Designer (nếu có VS). Ghi form nào mở được, form nào báo lỗi Designer và lý do.

## 5. Tài liệu và bàn giao

- `docs/api/winforms-giao-dien.md`: cập nhật mục 3 và mục 5 theo cái đã làm (tên form, trạng thái "Chạy").
- Báo cáo `docs/desktop-ui/dot-1-report.md`:
  - file thêm/sửa;
  - quyết định Form hay UserControl;
  - kết quả test (số ca, ca trượt);
  - danh sách ảnh;
  - kết quả mở Designer;
  - lỗi API phát hiện;
  - việc còn lại cho đợt 2.
- **Commit** theo từng bước trên nhánh `feature/desktop-ui-rebuild` (khung chung → đăng nhập → từng màn chủ), message tiếng Việt kiểu `feat(desktop): ...`. **Không push, không merge, không tạo PR**; người dùng tự push để Claude review.
- Không đụng `docs/bao-cao/`, `deploy/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file hay commit.
