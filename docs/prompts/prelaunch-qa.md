# Kiểm thử trước khi ra mắt (pre-launch) — Fuji Homestay

> Chạy trong **phiên Codex mới**, trên nhánh `feature/foundation` (HEAD `e54def1` hoặc mới hơn). Mục tiêu: dùng thử toàn bộ hệ thống **như người dùng thật trước ngày ra mắt**, tìm lỗi, sửa các lỗi rõ ràng, và kết luận **có ra mắt được không**.

Đọc trước:
- `README.md`;
- `QUY_TRINH_BTL.md`: bảng FR, mục 5.4 Bảo mật, mục 7 Quy tắc nghiệp vụ, mục 8 API, mục 13 Kế hoạch kiểm thử;
- `database/README.md`;
- `docs/ui/qa-report.md`, `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`.

Skill: `playwright-cli`, `verification-before-completion`, `web-design-guidelines`, `full-output-enforcement`.

## Luật

- **Không làm bẩn database đang dùng.** Không ghi vào `HomestayBooking_v5` và các database có sẵn khác.
  - Tự dựng database riêng mới tinh từ script (mục 1).
  - Chạy app riêng ở cổng **5014** (Development) và **5015** (Production), trỏ vào database mới bằng biến môi trường `ConnectionStrings__Default`.
  - Lấy mật khẩu `sa` từ biến môi trường trong container (`docker exec sql-homestay printenv MSSQL_SA_PASSWORD`) đưa thẳng vào biến shell. **Không in mật khẩu ra log, báo cáo hay commit.**
- **Không tắt** app cổng 5011, không dừng/xóa container `sql-homestay`. Dừng app của mình bằng đúng PID. **Không dùng `pkill -f`/`killall` với mẫu ngắn.**
- **Được sửa lỗi rõ ràng**, mỗi lỗi sửa ở nguyên nhân gốc và kèm test hồi quy. "Lỗi rõ ràng" gồm:
  - lỗi 500/exception;
  - luồng nghiệp vụ hỏng, sai số tiền/trạng thái so với quy tắc đã ghi trong `QUY_TRINH_BTL.md`;
  - lỗ hổng bảo mật;
  - lỗi hiển thị làm không dùng được (tràn, che nút, mất chữ);
  - chuỗi chưa dịch.

  Được sửa cả `Services/` nếu đó là chỗ lỗi.
- **Không tự chốt nghiệp vụ mới**, không đổi schema/migration, không thêm thư viện. Việc cần người dùng quyết định thì ghi vào báo cáo, mục "Cần người dùng quyết định".
- **Commit** mỗi lỗi một commit `fix(...): ...`. **Không thêm dòng `Co-Authored-By` hay tên AI nào** vào commit. **Không push, không merge PR.**
- Ảnh/video/log để ở `/private/tmp/fuji-prelaunch/` hoặc `.playwright-cli/`, không commit.

## 1. Dựng từ đầu như máy chủ mới

1. Tạo database `HomestayBooking_PreLaunch` (nếu đã có thì dừng và báo, không xóa).
2. Chạy `database/deploy.sql` rồi `verify.sql` theo README (sqlcmd trong container). Mọi dòng phải OK.
3. `dotnet build -c Release`, `dotnet test HomestayBooking.sln -c Release --no-build`. Ghi số pass/skip/fail.
4. `dotnet publish HomestayBooking.Web -c Release -o /private/tmp/fuji-prelaunch/publish`. Chạy bản publish (không chạy từ source) ở 2 chế độ:
   - **Development, cổng 5014:** app tự seed dữ liệu mẫu. Đây là môi trường cho mục 2–6.
   - **Production, cổng 5015:** trên **một database thứ hai** `HomestayBooking_PreLaunchProd` dựng như bước 1–2. Kiểm:
     - chỉ seed danh mục nền, không có tài khoản/đơn demo;
     - `/swagger`, `/Dev/UiKit`, `/Dev/SimulateBank` không truy cập được;
     - lỗi trả trang lỗi thân thiện, không lộ stack trace;
     - có HSTS/chuyển HTTPS theo cấu hình;
     - cookie đăng nhập/antiforgery/session có `HttpOnly`/`Secure`/`SameSite` đúng mục 5.4;
     - header bảo mật cơ bản; ghi rõ cái nào chưa có.

     Cách cấp tài khoản quản trị đầu tiên ở Production đọc ở `database/README.md` (B7). Làm theo đúng quy trình đó nếu có; không có thì ghi vào báo cáo.

## 2. Luồng nghiệp vụ trọn vẹn (Development, cổng 5014)

Đi bằng `playwright-cli` như người thật: bấm nút, điền form, không gọi thẳng service. Mật khẩu seed `Fuji@2026`. Mỗi bước ghi kết quả và ảnh.

1. **Khách chưa đăng nhập, điện thoại 375px:**
   - tìm theo khu vực/ngày/giá → chi tiết → đặt → thấy QR cọc;
   - `/Dev/SimulateBank` chuyển đúng số tiền và nội dung → đơn tự khớp, sang Deposited;
   - tra cứu bằng mã + SĐT → thấy đúng trạng thái.
2. **Chủ `0912000001`:**
   - nhận thông báo, xác nhận đơn đó → khách thấy địa chỉ/hướng dẫn nhận phòng (BR-13: trước xác nhận không thấy);
   - ghi nhận khách nhận phòng → hoàn thành;
   - khách viết đánh giá → chủ trả lời → đánh giá hiện ở trang homestay.
3. **Khách `0987000001`:** đặt có voucher (số giảm đúng), đơn của tôi (3 tab), đổi ngày (báo giá, phí BR-04), hủy đơn đã cọc ở các mốc 7 ngày / 3–6 ngày / dưới 3 ngày, số hoàn đúng chính sách.
4. **Kế toán `KT001`:**
   - giao dịch chuyển thiếu/chuyển muộn/không rõ đơn → xử lý tay;
   - bill khách tải lên → duyệt và từ chối;
   - hoàn tiền;
   - đối soát: lập kỳ → gửi → chủ xác nhận hoặc kế toán xác nhận thay (có lý do) → ghi chuyển tiền (sai số tiền bị chặn) → mở lại;
   - khoản bù.
5. **Nhân viên `NV001`/`NV002`:** đặt hộ khách mới qua điện thoại, nhận/giao đơn, phòng trống toàn hệ thống, xử lý yêu cầu đổi/hủy, xử lý trùng phòng (đổi phòng/đổi homestay/hoàn tiền).
6. **Quản lý `QL001`:**
   - duyệt đổi/hủy;
   - đối tác mới end-to-end: đăng ký `/Partner` → tải giấy tờ → gửi → thẩm định (yêu cầu bổ sung → duyệt) → mời kích hoạt → chủ kích hoạt → tạo hợp đồng → duyệt nội dung → homestay lên trang tìm kiếm;
   - báo cáo 5 tab với khoảng ngày có dữ liệu, đổi sáng/tối.
7. **Quản trị `AD001`:** tạo nhân viên → đăng nhập bằng mật khẩu tạm → đổi mật khẩu; khóa/mở khóa; đặt lại mật khẩu; danh mục thêm/sửa/xóa (xóa mục đang dùng bị chặn).
8. **Lịch phòng của chủ:** khóa/mở ngày bằng AJAX; khóa trùng đơn bị chặn; khách không đặt được đêm đã khóa.

Sau cùng chạy `database/tests/data_consistency.sql` trên `HomestayBooking_PreLaunch`: 35/35 OK.

## 3. Tình huống khó và đồng thời

- **TC04 – hai người cùng đặt phòng cuối cùng một lúc:** bắn song song (script gửi nhiều POST đồng thời, mỗi request có token riêng) → chỉ đúng một đơn giữ được đêm, các đơn còn lại báo hết phòng, không có đêm bị giữ hai lần.
- **Hết hạn giữ phòng và cảnh báo 30 phút:** chạy thêm một bản Development với cấu hình ngắn (ví dụ `Booking__HoldMinutes=1`, `Booking__ConfirmMinutes=2` qua biến môi trường), để job nền thật chạy → đơn chưa cọc tự Expired và nhả đêm; đơn đã cọc quá hạn xác nhận sinh cảnh báo cho nhân viên.
- **Bấm hai lần / lùi trang / mở hai tab:** gửi form hai lần, F5 sau POST, xác nhận cùng đơn ở hai tab → không tạo trùng, có thông báo rõ.
- **Biên dữ liệu:**
  - ngày quá khứ, trả trước nhận (TC02), quá số đêm tối đa;
  - số khách vượt sức chứa;
  - SĐT sai (TC03), SĐT có khoảng trắng/+84;
  - chuỗi rất dài, emoji, dấu tiếng Việt;
  - trường trống;
  - số tiền âm/0 ở form kế toán.
- **Lọc + phân trang (TC09):** bộ lọc giữ nguyên khi chuyển trang; trang ngoài phạm vi.

## 4. Bảo mật

- **Ma trận quyền (TC05):** mỗi vai trò mở URL của vai trò khác (Admin/Staff/Owner/Accounting/Dev) → bị chặn.
  - Chủ A mở đơn, phòng, homestay, đối soát của chủ B → 404/403.
  - Khách mở đơn của khách khác bằng mã → không thấy.
  - Tra cứu đúng mã sai SĐT (TC06) → không lộ.
  - Thử cả qua `/api/...`.
- **CSRF:** mọi POST/DELETE (MVC và API) thiếu token → bị từ chối.
- **XSS (TC08):** chèn `<script>`, `onerror=`, `javascript:` vào tên, ghi chú, nhận xét, mô tả (TinyMCE), nội dung chuyển khoản → không chạy, hiển thị an toàn.
- **Upload (TC07):** `.exe` đổi đuôi `.jpg`, file > 5 MB, ảnh hỏng, tên file có `../` → bị từ chối. File hợp lệ đổi tên GUID.
- **Đăng nhập:** khóa sau 5 lần sai (mục 5.4), chính sách mật khẩu, đăng xuất xóa phiên, `returnUrl` ngoài site không chuyển hướng ra ngoài (open redirect).
- **Lộ thông tin:** trang lỗi, header, API trả lỗi không lộ stack/SQL; không có bí mật trong repo (`git grep` mật khẩu/khóa API).
- **Webhook SePay:** sai/thiếu khóa API → 401; gửi trùng một giao dịch → không cộng tiền hai lần.

## 5. Trải nghiệm trên thiết bị và trình duyệt

- `playwright-cli` với Chromium, **WebKit (Safari)** và Firefox (nếu cài được; không cài được thì ghi rõ).
- Mô phỏng iPhone SE (375), iPhone 14 (390), Android (412), iPad (768/1024 ngang).
- Luồng đặt phòng và lịch phòng của chủ trên điện thoại: vùng bấm ≥ 44px, không cuộn ngang, bàn phím không che nút.
- Tiếng Anh (TC10) cho luồng khách và một màn mỗi vai trò nội bộ: không còn chữ giao diện tiếng Việt, định dạng tiền/ngày đúng.
- Sáng/tối, `prefers-reduced-motion`, tắt JavaScript: trang khách vẫn đọc và đặt được.
- Bàn phím: Tab qua header, form đặt phòng, bộ chọn ngày, modal, lịch phòng; focus nhìn thấy.

## 6. Hiệu năng

- Đo thời gian tải các trang chính (trang chủ, tìm kiếm có lọc, chi tiết, đặt phòng, dashboard từng vai trò, báo cáo) bằng Performance API: TTFB, DOMContentLoaded, load; liệt kê trang chậm nhất.
- NFR-02: phòng trống toàn hệ thống ≤ 5 giây. Chạy `RUN_PERF_TESTS=1` theo README trên database `HomestayBooking_ConstraintTest` (cần rỗng; nếu không rỗng thì không xóa dữ liệu, ghi lại).
- Đếm request, dung lượng trang chủ (ảnh, JS, CSS); ảnh có kích thước, lazy-load hợp lý.

## 7. Kết luận

Viết `docs/qa/prelaunch-report.md`:
- môi trường và phiên bản đã thử;
- bảng kết quả từng mục 1–6 (Đạt/Lỗi/Không kiểm được + bằng chứng);
- bảng test case TC01–TC12 của `QUY_TRINH_BTL.md` mục 13.2 với đầu ra thực tế;
- lỗi đã sửa (mức, nguyên nhân, commit, test);
- lỗi còn lại (mức Cao/Trung bình/Thấp, cách tái hiện);
- mục "Cần người dùng quyết định";
- dữ liệu thử đã tạo;
- **kết luận Go / No-go cho ra mắt**, kèm điều kiện.

Cuối cùng: build không cảnh báo, toàn bộ test pass. Dừng mọi app và trình duyệt của mình. **Giữ lại** hai database `HomestayBooking_PreLaunch*` để người dùng xem, ghi tên vào báo cáo. Rồi dừng.
