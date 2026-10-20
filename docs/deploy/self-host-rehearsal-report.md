# Báo cáo chuẩn bị tự host và diễn tập Production trên Mac

Ngày 20/10/2026. Nhánh `deploy/self-host-mac`, tách từ `main` tại `a276f3f` (sau `86d574f`). Phạm vi theo [prompt](../prompts/self-host-mac.md). Hướng dẫn thao tác thật: [runbook](self-host-mac.md). Các commit code/bộ triển khai/QR lần lượt là `ccc3d8e`, `143a143`, `0b76266`; tài liệu được lưu trong commit riêng tiếp theo.

**Diễn tập chỉ trên loopback, không công bố web ra Internet.** Không chạy Cloudflare login/create/route/quick tunnel, không cài phần mềm hay LaunchAgent thật, không đổi `pmset`, tường lửa, người dùng máy hoặc port Docker. Không push/merge. App 5011 và container `sql-homestay` được giữ chạy.

## 1. Code và kiểm thử

- `Program.cs`: bật Forwarded Headers bằng `ReverseProxy:Enabled`, mặc định false; chỉ nhận X-Forwarded-For/Proto từ `127.0.0.1`, `::1` và dạng IPv4-mapped tương ứng, giới hạn một proxy. Middleware đứng trước HSTS/HTTPS redirect. Không nhận X-Forwarded-Host.
- Lưu Data Protection vào `DataProtection:KeysPath` (mặc định `App_Data/keys`), application name cố định `FujiHomestay`. Trong triển khai đặt đường dẫn tuyệt đối ngoài release/repo. Cookie Identity, antiforgery, session và TempData có Secure ở Production; TempData có thể chứa lời mời/mật khẩu tạm đã mã hóa.
- `/healthz`: AddHealthChecks + `DatabaseHealthCheck` dùng `ApplicationDbContext.Database.CanConnectAsync`; 200/Healthy hoặc 503/Unhealthy, không trả exception/chuỗi kết nối. Công khai, dùng HTTP loopback để script giám sát, giữ header bảo mật.
- Lời mời đối tác dùng `PublicUrlService` lấy `Reminders:PublicBaseUrl`, không lấy Host từ request. Lời nhắc khách đã lấy cùng cấu hình. URL ảnh QR bên ngoài lấy `BankAccount:QrImageBaseUrl` + ba trường tài khoản. Ảnh dự phòng lấy `BankAccount:FallbackQrImageUrl` (mặc định trống ở Production); loại việc rơi về ảnh QR tài khoản mẫu, hiển thị mã ngân hàng từ cấu hình. Development giữ QR mẫu trong appsettings riêng. URL điều hướng/thông báo trong app là tương đối. Runbook có bảng toàn bộ khóa URL cần đặt.
- Ngoài Development, loại `DevController` khỏi MVC để khách chưa đăng nhập cũng nhận GET 404; trước sửa khách bị chuyển tới Login.
- `SelfHostTests`: 12 ca cho proxy bật/tắt, IPv4/IPv6, từ chối proxy ngoài loopback, Secure/HSTS, GET Dev/Swagger 404, health 503 kín lỗi, lời mời và QR theo cấu hình qua pipeline thật, khóa và token cũ qua host mới, hai nhánh QR dự phòng từ trang thanh toán thật trong host tích hợp. JavaScript mất ảnh được chạy kiểm với/không có fallback (`qr-fallback-js.json`).
- Fixture lưu khóa test ngoài repo. Hai lớp SQL test nhận `FUJI_SQL_TEST_DATABASE` với whitelist tên DB thử; lượt này dùng **DB mới** `HomestayBooking_SelfHostTest_20261020`, không chạy test ghi dữ liệu vào ConstraintTest có sẵn.

Kết quả cuối: `dotnet build HomestayBooking.sln -c Release` **0 cảnh báo / 0 lỗi**; `FUJI_SQL_TEST_DATABASE=HomestayBooking_SelfHostTest_20261020 RUN_PERF_TESTS=1 dotnet test HomestayBooking.sln -c Release --no-build` **1772 pass / 0 fail / 0 skip**, gồm benchmark SQL. Lượt toàn bộ cuối có thời gian test **20 phút 41 giây** theo runner; lượt trước bổ sung 2 ca QR là 1770 pass trong 4 phút 2 giây. Bằng chứng cuối: `build-complete.log`, `test-complete.log`, `complete.trx`.

Bằng chứng ở `/private/tmp/fuji-selfhost/evidence/`. Giữ cả lượt đỏ: cookie TempData chưa có Secure (`tempdata-red.log`); sau sửa có assertion trên cookie chứa lời mời. Các lượt đầu cũng phát hiện giả định sai của test về HSTS cho localhost, base URL mặc định HTTP của test client và POST route không có trong TestServer (405); đã chỉnh test về yêu cầu GET của checklist, đối chiếu thêm GET/POST trên app publish thực tế. Không tính các lượt lỗi đó là pass. Sandbox không cho `ps` và host test có hạn chế certificate store macOS; lượt xác nhận dùng quyền phù hợp, không thay cấu hình certificate của máy.

## 2. Bộ triển khai

15 file trong `deploy/mac/`:

| File | Mục đích |
|---|---|
| `production.env.example` | Đủ biến môi trường, chú thích Việt, giá trị giả; bí mật thật ngoài repo |
| `common.sh` | Nạp env đúng chủ sở hữu/quyền 600, guard DB/path/PID, SQL auth không in mật khẩu |
| `setup-database.sh` | Chỉ tạo DB mới, từ chối DB có sẵn/v5, deploy + verify |
| `publish.sh` | Release ra ngoài repo, App_Data/uploads dùng dữ liệu bền ngoài release |
| `run.sh` | Chạy foreground cho LaunchAgent, khóa chống khởi động trùng |
| `start.sh` | Chạy nền, PID/log và chờ health |
| `stop.sh` | SIGTERM đúng PID đã đối chiếu command, cờ bảo trì, không dùng pkill/killall |
| `status.sh` | PID + GET health |
| `backup.sh` | COPY_ONLY/CHECKSUM/VERIFYONLY, .bak + tar + SHA-256, giữ 14 bộ |
| `restore.sh` | Chỉ restore DB khác có `_Restore`, không REPLACE, logical MOVE riêng, CHECKDB |
| `update.sh` | Backup → publish stage → dừng → đổi release → chạy → health; lỗi quay lui binary |
| `com.fuji.homestay.plist` | App tự chạy khi đăng nhập, KeepAlive |
| `com.fuji.homestay.backup.plist` | Backup hằng ngày 03:15 |
| `cloudflared/config.yml.example` | Named tunnel hostname → HTTP 127.0.0.1:8080, fallback 404 |
| `cloudflared/com.fuji.cloudflared.plist` | Mẫu LaunchAgent riêng cho tunnel |

Đã kiểm cú pháp Bash và `plutil -lint` cả ba plist, không nạp plist vào launchd. Bản script được sao ra `/private/tmp/fuji-selfhost/scripts` và kiểm `status.sh` thành công, tương tự vị trí chạy thật ngoài checkout.

## 3. Kết quả diễn tập

HTTP được gửi tới **127.0.0.1:8080** với `Host: fuji.example`, `X-Forwarded-Proto: https`, X-Forwarded-For. Harness giữ cookie Secure khi mô phỏng đường đi tunnel nội bộ; đây không phải thử chứng chỉ HTTPS/Cloudflare thật hoặc kiểm trình duyệt/điện thoại 4G.

| Bước | Kết quả | Bằng chứng thực tế |
|---|---|---|
| 1. DB mới | **Đạt** | `HomestayBooking_ProdRehearsal`: deploy/verify schema 45 bảng, 9 migration; `setup-database.log`. Lỗi dọn thư mục SQL tạm ở lượt đầu đã sửa bằng chỉ chép 3 file cần thiết; lượt dựng DB SQL test bằng script mới thoát 0 |
| 2. Bí mật | **Đạt** | `/private/tmp/fuji-selfhost/rehearsal.env` quyền 600; mật khẩu quản trị và webhook sinh ngẫu nhiên trong file 600, không đưa vào repo/log. Đã xóa dòng mật khẩu bootstrap sau lần đầu |
| 3. Publish/start | **Đạt** | Release tại `/private/tmp/fuji-selfhost/app`; chỉ nghe 127.0.0.1:8080; `publish-routes.log`, `update-final.log`, health 200 |
| 4. Tunnel giả lập | **Đạt nội bộ** | Không có redirect loop; HTTP trả 307 tới `https://fuji.example/`, request XFP=https trả 200; Secure cookie, HSTS/nosniff/DENY/CSP…; GET Swagger/Dev 404, lỗi 404 thân thiện không stack. `initial.json`, `update-final-smoke.json`, `qr-final-http.json`; QR dự phòng tài khoản mẫu không còn xuất hiện, mã ngân hàng theo cấu hình; lời mời HTTPS theo cấu hình được kiểm trong test tích hợp |
| 5. Quản trị/đối tác/đơn | **Đạt qua form HTTP thật** | QTSELF001 đăng nhập, đổi mật khẩu, tạo NVSELF001. Chủ tự đăng ký `/Partner`, 3 giấy tờ hợp lệ; NVSELF001 nhận thẩm định, quản trị duyệt; hợp đồng `SELFHOST-20261020`; homestay #1 Active, phòng SELF101, xuất hiện tìm kiếm; khách tạo `FJ261020-001`. `initial.json`, `partner-registered.json`, `partner-complete.json`, `booking.json`, `data-inventory-final.txt` |
| 6. Webhook SePay | **Đạt giả lập** | Payload SePay JSON; khóa sai 401; khóa đúng 200/Matched; đơn Deposited, cọc 150.000đ; gửi lại duplicate=true, chỉ 1 Payment và 1 BankTransaction. `webhook.json`; không chuyển tiền ngân hàng thật |
| 7. Restart | **Đạt** | Cookie quản trị trước restart mở Admin được; token form tạo trước restart đổi mật khẩu thành công, không 400; 1 key XML giữ nguyên; 3 giấy tờ và ảnh homestay còn, tải giấy tờ 200. `before-restart.json`, `restart.json` |
| 8. Backup/restore | **Đạt** | Restore sang `HomestayBooking_ProdRehearsal_Restore`, CHECKDB không lỗi; `data_consistency.sql` **35/35 trên cả hai**. SHA-256 manifest đúng; giải nén riêng, hash cả 5 file (3 giấy tờ, ảnh, key) khớp. `backup.log`, `restore.log`, `*-consistency.log`, `restore-files.json` |
| 9. Update | **Đạt cả hai nhánh** | Update build mới thành công, health xanh, cookie cũ dùng được, ảnh upload được phục vụ HTTP 200; `update-final.log`, `update-qr-final.log`, `update-final-smoke.json`, `qr-final-http.json`. Cố ý bỏ runtimeconfig **chỉ bản chờ**: update trả 1, tự phục hồi bản cũ, health xanh (`update-rollback.log`, `update-rollback-exit.txt`) |
| 10. Dừng/giữ dữ liệu | **Đạt** | `stop.sh` chạy hai lần đều an toàn; status trả 1 vì app đã dừng, không còn listener 8080/PID file, có cờ bảo trì. App 5011 vẫn nghe, PID 31028. Không xóa DB/thư mục diễn tập; `stopped.json`, `port5011-final.txt` |

Gọi trực tiếp `/Home/Error` chỉ kiểm trang hiển thị (200), không tính là gây lỗi server thật. Test `ProductionErrorHandlingTests` kiểm exception POST thật trong pipeline trả 500 thân thiện và không lộ sentinel/stack.

Kiểm thêm: env sai quyền bị từ chối; setup DB đã tồn tại và tên v5 bị từ chối; restore vào DB nguồn bị từ chối (`script-guards.json`, `restore-guard.json`). Đã tạo thêm đủ backup thật để kiểm giữ đúng **14 .bak + 14 archive + 14 manifest ở host, 14 .bak ở volume SQL** (`retention.json`). Bộ dùng cho restore được giữ riêng tại `restore-source/` để không bị chính sách giữ 14 bản dọn mất.

## 4. Dữ liệu giữ lại

- `HomestayBooking_ProdRehearsal`: dữ liệu Production diễn tập, 2 nhân viên (1 quản trị, 1 tư vấn), 1 chủ tự đăng ký, 1 homestay/phòng/hợp đồng, 1 khách, 2 đơn (FJ261020-001 đã nhận cọc; FJ261020-002 kiểm QR cuối rồi hủy khi chưa trả tiền), 1 giao dịch và 1 payment.
- `HomestayBooking_ProdRehearsal_Restore`: bản khôi phục kiểm tra ở snapshot 1 đơn trước lần kiểm QR cuối, MDF/LDF riêng, không ghi đè nguồn.
- `HomestayBooking_SelfHostTest_20261020`: DB mới cho test SQL; dữ liệu nghiệp vụ test rollback, chỉ giữ schema.
- `/private/tmp/fuji-selfhost`: app, data, env, mật khẩu thử dạng file 600, log/PID, script/harness HTTP, backup, restore-source, restore-files-check, release cũ/bản cố ý hỏng và bằng chứng. Thư mục gốc quyền 700. **/private/tmp có thể bị hệ điều hành dọn**; dữ liệu vận hành thật phải ở `~/FujiHomestay`, không dùng thư mục này lâu dài.
- Không dùng dữ liệu/số tài khoản giả/mật khẩu diễn tập để chạy cho khách thật. Không đưa thư mục diễn tập hoặc các file credentials vào Git.

## 5. Việc chủ máy tự làm, theo thứ tự

Chi tiết từng thao tác nằm trong [runbook](self-host-mac.md), bao gồm cách điền plist; các lệnh sau chưa được thực hiện bởi phiên chuẩn bị.

1. **Chọn/mua tên miền, tài khoản Cloudflare, chuyển nameserver.** Không có lệnh mua thay cho xác nhận thanh toán của chủ máy. Mở trang quản lý, thêm domain bạn sở hữu:

   ```bash
   open https://dash.cloudflare.com/
   ```

2. **Chốt edition SQL cho Production và cửa sổ bảo trì port 1433.** Máy đang dùng **Developer Edition**, chỉ được cấp phép cho phát triển/thử nghiệm theo [Microsoft](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022?view=sql-server-ver17). Không coi việc chạy môi trường ASPNETCORE Production là đã có giấy phép SQL Production. SQL hiện công bố `0.0.0.0:1433` và `[::]:1433`; cần kế hoạch giữ volume, backup/restore đã xác minh và đổi binding thành `127.0.0.1:1433:1433`, có tính đến app 5011 dùng chung container. Kiểm trước/sau:

   ```bash
   docker port sql-homestay 1433
   ```

3. **Dựng DB thật và điền bí mật ngoài repo.** Không dùng DB diễn tập cho khách thật:

   ```bash
   cd /Users/phamngocphu/Downloads/LTW
   ./deploy/mac/setup-database.sh HomestayBooking
   mkdir -p "$HOME/FujiHomestay"
   chmod 700 "$HOME/FujiHomestay"
   cp deploy/mac/production.env.example "$HOME/FujiHomestay/production.env"
   chmod 600 "$HOME/FujiHomestay/production.env"
   command -v dotnet
   nano "$HOME/FujiHomestay/production.env"
   ```

   Điền SQL, quản trị, ngân hàng thật, khóa webhook, hostname, AllowedHosts, DOTNET_BIN. Sau bootstrap/đổi mật khẩu: `nano "$HOME/FujiHomestay/production.env"` để **xóa dòng Password bootstrap**.

4. **Publish và kiểm app nội bộ:**

   ```bash
   ./deploy/mac/publish.sh
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   ```

5. **Cài cloudflared, tạo named tunnel/DNS và điền config.** Các lệnh này cần tài khoản thật và sẽ chuẩn bị khả năng public; chỉ chủ máy chạy:

   ```bash
   brew install cloudflared
   cloudflared tunnel login
   cloudflared tunnel create fuji-homestay
   cloudflared tunnel route dns fuji-homestay TEN_MIEN_THAT
   mkdir -p "$HOME/FujiHomestay/cloudflared"
   cp deploy/mac/cloudflared/config.yml.example "$HOME/FujiHomestay/cloudflared/config.yml"
   nano "$HOME/FujiHomestay/cloudflared/config.yml"
   chmod 600 "$HOME/FujiHomestay/cloudflared/config.yml"
   cloudflared tunnel --config "$HOME/FujiHomestay/cloudflared/config.yml" ingress validate
   ```

   Bật Always Use HTTPS ở Cloudflare. Không dùng quick tunnel cho webhook.

6. **Cài LaunchAgent app/backup/tunnel.** Chạy khối sao chép script + thay đường dẫn plist bằng Python trong runbook, rồi:

   ```bash
   ./deploy/mac/stop.sh
   rm -f "$HOME/FujiHomestay/run/maintenance"
   launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
   launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
   launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.cloudflared.plist"
   ```

   Dòng tunnel bắt đầu công bố website. Không cài thêm `cloudflared service install` nếu đã dùng mẫu LaunchAgent này.

7. **Đăng ký/liên kết SePay và thử khoản tiền nhỏ thật.** Trong trang SePay tạo webhook `https://TEN_MIEN_THAT/api/bank-webhook/sepay`, xác thực `Authorization: Apikey <khóa trong env>`, tài khoản nhận trùng QR:

   ```bash
   open https://my.sepay.vn/
   curl --fail https://TEN_MIEN_THAT/healthz
   ```

   Tạo đơn thật, chuyển đúng AmountDue/nội dung mã đơn, đối chiếu Deposited, kiểm gửi trùng. Cấu hình webhook bằng giao diện, không dán khóa thật vào lịch sử lệnh.

8. **Giữ máy thức và bảo vệ máy.** Chọn phương án cắm nguồn, không đóng nắp, bật Docker Desktop khi đăng nhập; tự bật FileVault/tường lửa. Ví dụ lệnh do chủ máy quyết định:

   ```bash
   pmset -g custom
   sudo pmset -c sleep 0
   # Hoặc phiên giữ thức tạm: caffeinate -s
   fdesetup status
   /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
   ```

9. **Kiểm 4G, chứng chỉ thật, phân quyền/tài khoản thật và sao lưu ngoài máy.** Theo checklist runbook, kiểm HTTP→HTTPS, GET Swagger/Dev 404, đăng nhập, đặt/khớp tiền, upload. Chạy thử backup/restore định kỳ, theo dõi dung lượng/log và hạn tên miền:

   ```bash
   ./deploy/mac/backup.sh
   ./deploy/mac/restore.sh "$HOME/FujiHomestay/backups/TEN_BAN_SAO.bak" HomestayBooking_Restore_Check
   ./deploy/mac/update.sh
   ```

## 6. Rủi ro và giới hạn còn lại

- Mac ngủ sau 1 phút hiện chưa đổi; đóng nắp, mất điện/mạng, Docker dừng đều ngắt web. UPS cho cả router và máy sẽ giảm gián đoạn, không tạo dự phòng đầy đủ.
- SQL đang mở LAN và đang dùng Developer Edition: cần xử lý có chủ đích trước phục vụ khách thật; không tự dừng container đang dùng chung.
- LaunchAgent cần đăng nhập; FileVault có thể yêu cầu chủ máy mở khóa sau reboot. Chưa diễn tập reboot thật hay nạp launchd thật để giữ nguyên cấu hình máy.
- Chưa có domain/tunnel/SePay/tài khoản ngân hàng thật, chưa kiểm chứng chỉ bên ngoài hoặc 4G. Bằng chứng thanh toán chỉ là payload giả lập đi qua controller/service thật.
- Backup SQL và file là hai thời điểm gần nhau; chọn giờ ít tải hoặc dừng app khi cần snapshot nhất quán chặt. 14 bản trên cùng Mac không chống hỏng/mất máy; phải có bản ngoài máy có mã hóa.
- Khóa Data Protection trên ổ chưa mã hóa ở mức ứng dụng; quyền thư mục/FileVault/bản sao lưu riêng tư là cần thiết. Mất khóa làm mất khả năng đọc cookie/token cũ.
- Release cũ và log chưa tự xoay/dọn; cần theo dõi ổ đầy. Rollback chỉ binary; không tự đảo migration hoặc dữ liệu đã ghi.
- SMS/Zalo vẫn giả lập, không coi MessageLog Sent là khách nhận được tin. CSP mới có các directive nền; nonce cho script-src/style-src đầy đủ vẫn là công việc riêng đã ghi trong QA.

## 7. Cần người dùng quyết định

1. Hostname chính thức và dùng tên miền mới hay tên miền hiện có.
2. Edition/giấy phép SQL hợp lệ cho Production; thời điểm đổi port binding/credential mà không ảnh hưởng dữ liệu và app 5011.
3. Tài khoản ngân hàng nhận tiền và gói/tài khoản SePay, người giữ khóa và kiểm giao dịch thật.
4. Khi nào cho phép chạy named tunnel/LaunchAgent thật; người đăng nhập sau reboot/FileVault, chính sách máy không ngủ, UPS.
5. Nơi sao lưu ngoài máy, người nhận cảnh báo health/dung lượng và lịch diễn tập restore định kỳ.
