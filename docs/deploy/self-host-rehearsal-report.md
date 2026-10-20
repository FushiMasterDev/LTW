# Báo cáo chuẩn bị tự host và diễn tập Production trên Mac

Ngày 20/10/2026. Nhánh `deploy/self-host-mac`, tách từ `main` tại `a276f3f` (sau `86d574f`). Phạm vi theo [prompt](../prompts/self-host-mac.md). Hướng dẫn thao tác thật: [runbook](self-host-mac.md). Các commit code/bộ triển khai/QR lần lượt là `ccc3d8e`, `143a143`, `0b76266`; tài liệu được lưu trong commit riêng tiếp theo.

**Cập nhật quyết định:** mục 1–7 dưới đây lưu lịch sử lượt diễn tập đầu. Kế hoạch mua domain/Cloudflare/SePay trong các mục đó đã được thay bằng **mục 8 — Phương án miễn phí (Tailscale Funnel)** theo [prompt tiếp theo](../prompts/self-host-mac-free.md). Khi triển khai hãy theo mục 8 và runbook hiện hành, không làm checklist cũ.

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

## 8. Phương án miễn phí (Tailscale Funnel)

Ngày đối chiếu và diễn tập: **20/10/2026**, cùng nhánh `deploy/self-host-mac`. Không mở web ra Internet, không cài Tailscale hay phần mềm hệ thống, không nạp LaunchAgent, không đổi thiết lập máy. Không chạy `sql-harden.sh`, không dừng/đổi/xóa container SQL hay app 5011, không xóa database nào. Không push/merge. Code và bộ triển khai theo [prompt phương án miễn phí](../prompts/self-host-mac-free.md).

### Code và kiểm thử

- `Program.cs`: khi `ReverseProxy:Enabled=true`, app bỏ HSTS/HTTPS redirection, để proxy kết thúc TLS. `ReverseProxy:AssumeHttps` mặc định true khi bật proxy: xét **socket peer loopback trước** Forwarded Headers, hỗ trợ IPv4, IPv6 và IPv4-mapped, không tin máy ngoài loopback. Kết nối từ Tailscale thiếu `X-Forwarded-Proto` vẫn có scheme HTTPS để antiforgery Secure hoạt động. Mặc định proxy vẫn tắt; cookie Production giữ Secure cho Identity/antiforgery/session/TempData.
- `BankWebhookController`: khóa null/trống/toàn khoảng trắng trả **503 ProblemDetails: Bank webhook is disabled**, trước xác thực/xử lý giao dịch. Khóa đã cấu hình vẫn dùng xác thực cũ. JSON không hợp lệ có thể trả 400 do model validation; không nhận dữ liệu khi khóa trống.
- Rà URL tuyệt đối: `PublicUrlService` và tin nhắc dùng `Reminders:PublicBaseUrl`; URL QR dùng `BankAccount:QrImageBaseUrl` và thông tin ngân hàng cấu hình, QR dự phòng có khóa riêng. Điều hướng/thông báo nội bộ là URL tương đối; không tìm thấy code tạo URL công khai từ `Request.Host`/`Request.Scheme`. Giữ giới hạn base URL 50 ký tự để tin nhắc còn trong 160 ký tự; nếu hostname dài, chọn tên máy ngắn trước public.
- Thêm **9 ca test**, tổng **1.781**: thiếu Proto với proxy bật/tắt và 3 dạng loopback/ngoài loopback; đăng nhập qua HTTP backend, cookie Secure, token form, truy cập Admin; 503 qua pipeline không ghi giao dịch; null/rỗng/khoảng trắng không gọi matching service. Hai ca trang QR được tăng assertion: khóa webhook trống vẫn có QR và form tải bill. Các test ReceiptReviewService có sẵn kiểm kế toán duyệt/từ chối/giữ phòng/quá hạn/phân quyền.
- Kiểm đối chứng code cũ bằng 8 ca mới chọn lọc: **6 fail đúng lỗi cần sửa, 2 pass** (`regression-red.log`, exit 1). Sau đó phục hồi nguyên code mới, build và chạy lại toàn bộ. Lượt xác nhận trước đối chứng: `dotnet build HomestayBooking.sln -c Release --no-restore` **0 cảnh báo / 0 lỗi**; test **1.781 pass / 0 fail / 0 skip**, gồm benchmark SQL, thời gian **3 phút 53 giây** (`build.log`, `test-complete.log`, `free-complete.trx`). Kết quả lượt cuối sau đối chứng ghi ở đoạn chốt kiểm chứng bên dưới.
- SQL test dùng **DB mới** `HomestayBooking_SelfHostTest_Free_20261020`, đã deploy/verify; hai test SQL rollback dữ liệu. Không chạy trên ConstraintTest/v5 hay DB làm việc có sẵn.

Bổ sung bộ triển khai: env chuyển sang hostname `ts.net`, `AssumeHttps=true`, khóa webhook trống; [tailscale.md](../../deploy/mac/tailscale.md) có cài đặt/quyền admin/lệnh bật-tắt-status và phiên bản đối chiếu; [sql-harden.sh](../../deploy/mac/sql-harden.sh) có 3 lần xác nhận, backup toàn bộ DB (trừ tempdb), restore vào **volume Express mới**, kiểm trước cutover, giữ nguồn để quay lui. Cloudflare được giữ là tùy chọn khi có tên miền. Bash syntax và cả 3 plist đã kiểm; script SQL chỉ được kiểm cú pháp/rà soát, chưa diễn tập đổi edition/container thật.

### Kết quả diễn tập nội bộ

App publish thật ở `/private/tmp/fuji-selfhost-free/app`, env quyền **600**, thư mục gốc **700**. Dùng lại **chỉ** `HomestayBooking_ProdRehearsal` theo quyền trong prompt; file dữ liệu được chép sang thư mục thử riêng, giữ nguyên thư mục lượt trước. HTTP tới **127.0.0.1:8080**, `Host: fuji-mac.example.ts.net`, chỉ `X-Forwarded-For`, **không** `X-Forwarded-Proto`. Harness chuyển cookie giữa các request để mô phỏng trình duyệt đi qua HTTPS của Funnel; đây không phải thử TLS/4G thực tế.

| Kiểm | Kết quả và bằng chứng |
|---|---|
| Không redirect loop | `/`, `/healthz`, `/Account/EmployeeLogin` trả 200, không Location; Dev/Swagger GET 404. App không phát HSTS ở proxy mode; nosniff/DENY/CSP còn. `funnel-manual-flow.json` |
| Đăng nhập/form | Admin, kế toán mới KTFREE001, chủ đăng nhập qua form thật và token antiforgery; cookie Secure được kiểm trong test pipeline và antiforgery cookie trên app publish. Tạo tài khoản kế toán qua form quản trị, mật khẩu tạm chỉ lưu file 600 ngoài repo |
| URL cấu hình | Lời mời qua controller thật với **Host: localhost** vẫn bắt đầu `https://fuji-mac.example.ts.net/Partner/Activate`, TempData Secure; không lưu token/HTML lời mời vào evidence. `funnel-absolute-url.json`. Fixture hồ sơ EmployeeAssisted được thêm bằng SQL chỉ trong DB diễn tập; không xem đó là thử quy trình tạo hồ sơ qua UI |
| Webhook tắt | Payload JSON hợp lệ + header API key giả → **503**, thông báo disabled; BankTransactions giữ **1** dòng của lượt trước, không thêm giao dịch. `funnel-manual-flow.json` |
| Không SePay, qua form HTTP thật | Khách tạo **FJ261020-003**, QR VietQR đúng số cọc thử **150.000đ**, form tải bill có mặt → tải PNG thử `PaymentSubmitted` → KTFREE001 thấy trong `/Accounting/Receipts`, duyệt thành `Deposited` → chủ xác nhận `Confirmed`. Payment `ManualReceipt/Approved`, reviewer employee **3**; không có webhook/tiền thật. `rehearse.log`, `funnel-manual-flow.json`, `rehearsal-inventory.txt` |
| Dữ liệu và dừng app | `database/tests/data_consistency.sql` **35/35 OK**, không sửa dữ liệu; app thử dừng bằng script đúng PID. Không còn listener 8080. App **5011 vẫn PID 31028**, SQL container vẫn chạy. `data-consistency.log`, `listeners-final.txt` |
| SQL chỉ đọc | `docker port sql-homestay`: vẫn `0.0.0.0:1433`, `[::]:1433`; lsof `*:1433`, Docker PID 32952. Edition **Developer Edition (64-bit)**, version **16.0.4230.2**, image `mcr.microsoft.com/mssql/server:2022-latest`, volume `sql-homestay-data`. `docker-port.txt`, `sql-readonly.txt`, `source-mount.txt` |

Các lỗi harness không tính đạt: lệnh đầu đã kết thúc launcher nên tiến trình nền bị môi trường exec thu hồi, HTTP connection refused; sau đó chạy start và harness trong cùng phiên lệnh, rồi stop rõ ràng. Fixture lời mời ban đầu thiếu `SET QUOTED_IDENTIFIER ON` khi insert vào bảng có filtered index, transaction rollback; sửa SET option rồi kiểm đạt (`rehearse-url-final.log`). Lượt targeted đầu bị chạy trùng/chung log; đã dừng đúng PID test trùng của phiên, không dùng log lẫn đó làm bằng chứng cuối. Sandbox hạn chế `ps`/`lsof` và certificate store: dùng lượt kiểm với quyền phù hợp, không sửa cấu hình máy.

Dữ liệu giữ lại: DB rehearsal có thêm 1 kế toán, 1 khách/đơn đã Confirmed và 1 hồ sơ mời thử (WaitingOwnerActivation); toàn bộ đơn cũ còn. Giữ `/private/tmp/fuji-selfhost-free` gồm env/credentials thử, binary, data/bill/key, harness và `evidence/`; không commit chúng. `/private/tmp` có thể bị dọn, không dùng làm nơi lưu lâu dài.

### Việc chủ máy tự làm — theo thứ tự

1. **Xử lý điều kiện Tailscale trước public thương mại.** Personal 0đ chỉ dành phi thương mại theo [bảng giá chính thức](https://tailscale.com/pricing). Không thể xác nhận web kinh doanh homestay được dùng Personal chỉ dựa trên máy/tài khoản cá nhân. Tự đối chiếu/xin xác nhận quyền sử dụng miễn phí; không lấy trial làm phương án lâu dài và không tự mua gói trả phí:

   ```bash
   open https://tailscale.com/pricing
   ```

2. **Chọn cửa sổ bảo trì SQL, backup ngoài máy và tự dừng mọi app ghi SQL, kể cả 5011.** Theo runbook mục 9; script sẽ backup mọi DB, VERIFYONLY, yêu cầu chép backup sang ổ khác, dựng bản Express trên volume mới ở 1434 để bạn kiểm trước khi cutover 1433. Không xóa container/volume cũ:

   ```bash
   docker port sql-homestay 1433
   ./deploy/mac/sql-harden.sh --apply
   # Sau khi tự kiểm, xác nhận cutover trong script:
   docker port sql-homestay 1433  # chỉ 127.0.0.1:1433
   ```

   Kiểm `SELECT SERVERPROPERTY('Edition')`, CHECKDB, mọi DB/app/quyền theo lệnh runbook; Express phải đúng **SQL Server 2022**, 10 GB/DB. Không tiếp tục nếu restore/tính năng/edition không đạt. Bản nguồn vẫn Developer/LAN nên rollback chỉ để khôi phục phục vụ bảo trì.
3. **Dựng DB thật và điền env ngoài repo**, không dùng dữ liệu thử. BankAccount là tài khoản nhận tiền thật/đúng tên chủ, khóa webhook trống; điền base URL/AllowedHosts theo Tailscale sau khi lấy URL. Giữ proxy/AssumeHttps true:

   ```bash
   cd /Users/phamngocphu/Downloads/LTW
   ./deploy/mac/setup-database.sh HomestayBooking
   mkdir -p "$HOME/FujiHomestay"
   chmod 700 "$HOME/FujiHomestay"
   cp deploy/mac/production.env.example "$HOME/FujiHomestay/production.env"
   chmod 600 "$HOME/FujiHomestay/production.env"
   nano "$HOME/FujiHomestay/production.env"
   ./deploy/mac/publish.sh
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   ```

   Sau bootstrap, đổi mật khẩu và xóa dòng mật khẩu bootstrap khỏi env; tạo người kế toán thật qua UI.
4. **Cài/đăng nhập Tailscale Standalone**, bật mở khi đăng nhập/CLI integration; admin console DNS bật MagicDNS/HTTPS certificates và policy `nodeAttrs` cho Funnel đúng máy. [Hướng dẫn từng bước + ACL](../../deploy/mac/tailscale.md):

   ```bash
   open https://tailscale.com/download/mac
   open https://console.tailscale.com/
   tailscale version
   tailscale serve --bg 8080
   tailscale serve status
   ```

5. **Chỉ khi sẵn sàng và có quyền sử dụng phù hợp mới bật public**. Lấy URL, ghi env và restart app:

   ```bash
   tailscale funnel --bg 8080
   tailscale funnel status
   nano "$HOME/FujiHomestay/production.env"
   ./deploy/mac/stop.sh
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   # Thay bằng hostname thật vừa lấy:
   curl --fail https://TEN-MAY.TAILNET.ts.net/healthz
   ```

   Điền `Reminders__PublicBaseUrl='https://TEN-MAY.TAILNET.ts.net'`, `AllowedHosts='TEN-MAY.TAILNET.ts.net;127.0.0.1;localhost'`; giữ `BankAccount__WebhookApiKey=''`. Không cài/nạp cloudflared trong đường chính này.
6. **Tự cài LaunchAgent app/backup theo khối lệnh mục 5 runbook**, dừng bản chạy tay trước, thay đường dẫn plist, rồi:

   ```bash
   ./deploy/mac/stop.sh
   rm -f "$HOME/FujiHomestay/run/maintenance"
   launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
   launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
   tailscale funnel status
   ```

   `--bg` tự giữ cấu hình Funnel; Tailscale/Docker mở khi đăng nhập, Mac phải thức. Chủ máy tự chỉnh sleep/FileVault/tường lửa theo runbook nếu chọn. Không có thay đổi hệ thống tự động từ phiên chuẩn bị.
7. **Thử bằng điện thoại 4G**, tắt Wi-Fi, mở web HTTPS, đặt đơn, chuyển tiền thật số nhỏ đúng QR/mã đơn, tải bill, kế toán đối chiếu sao kê rồi duyệt, chủ xác nhận. Kế toán xem hàng chờ nhiều lần mỗi ngày, đối chiếu số tiền/nội dung chứa mã đơn/người nhận/giao dịch duy nhất; từ chối phải ghi lý do. Không chỉ tin ảnh bill. Sau reboot kiểm lại health và Funnel; định kỳ backup/restore ra DB tên khác:

   ```bash
   ./deploy/mac/backup.sh
   ./deploy/mac/restore.sh "$HOME/FujiHomestay/backups/TEN_BAN_SAO.bak" HomestayBooking_Restore_FreeCheck
   tailscale funnel status
   # Khi muốn dừng public:
   tailscale funnel --bg 8080 off
   ```

### Chi phí, giấy phép và rủi ro

| Thành phần | Chi phí thêm dự kiến | Điều kiện |
|---|---:|---|
| macOS trên Mac đang có | 0đ | Máy/giấy phép đã sở hữu |
| Docker Desktop cá nhân/doanh nghiệp nhỏ | 0đ | Dưới 250 nhân viên **và** doanh thu dưới 10 triệu USD/năm; điều kiện [Docker](https://docs.docker.com/subscription-billing/desktop-license/) |
| SQL Server 2022 Express | 0đ | Production hợp lệ, 10 GB/DB; Developer không dùng Production thương mại, [Microsoft](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022?view=sql-server-ver16) |
| .NET | 0đ | Runtime miễn phí kể cả thương mại, [Microsoft](https://dotnet.microsoft.com/en-us/platform/free) |
| Tailscale Personal/Funnel | 0đ **phi thương mại** | Chưa xác nhận điều kiện cho web kinh doanh; [Tailscale](https://tailscale.com/pricing) |
| VietQR có sẵn + duyệt bill thủ công | 0đ phí tích hợp | Không đăng ký API/gói SePay; phí tài khoản/chuyển tiền tùy ngân hàng, cần người đối chiếu |
| Tên miền, SePay | 0đ | Không mua, không dùng |

**Không thể khẳng định mọi dịch vụ Production thương mại 0đ theo điều kiện hiện hành của Tailscale Personal.** Không tự đổi dịch vụ/gói trả phí để giải quyết; báo rõ giới hạn này cho chủ máy. Điện/mạng, phần cứng, ổ backup, UPS, nhân công và phí ngân hàng không được tính là miễn phí.

Funnel chỉ TLS trên 443/8443/10000, tên dưới `ts.net` của tailnet, không đổi thành tên miền tùy ý; giữ tên máy/tailnet để URL ổn định. Có giới hạn băng thông không cấu hình được; tài liệu không công bố số Mbps/hạn mức kết nối đồng thời, không đoán số. Chưa kiểm tải qua Funnel thật. [Giới hạn chính thức](https://tailscale.com/docs/features/tailscale-funnel#requirements-and-limitations). Cú pháp đối chiếu CLI từ 1.52, changelog v1.104.1 (07/10/2026); chưa kiểm trên binary Tailscale cài tại máy.

Rủi ro vận hành còn nguyên: máy ngủ/đóng nắp/mất điện/mạng/Docker/Tailscale ngừng thì web ngừng; cần đăng nhập sau reboot/FileVault, theo dõi key expiry. SQL hiện vẫn LAN/Developer cho đến khi chủ máy tự bảo trì; chuyển edition/port thật chưa diễn tập. Mac chạy SQL amd64 qua giả lập (nếu có) không thuộc cấu hình SQL container được Microsoft hỗ trợ. Duyệt tay chậm hơn tự khớp; khách đã chuyển nhưng quên tải bill có thể hết hạn giữ phòng, cần đối chiếu và hỗ trợ/hoàn tiền khi cần. Không có SMS/Zalo thật, không dùng MessageLog như chứng cứ giao tin. Chưa thử ngân hàng/4G/TLS public hay reboot; backup cùng Mac vẫn cần một bản ngoài máy.

### Chốt kiểm chứng lượt phương án miễn phí

Sau khi phục hồi code mới từ kiểm đối chứng: `dotnet build HomestayBooking.sln -c Release --no-restore` thoát **0**, **0 cảnh báo / 0 lỗi** (`build-final.log`). `FUJI_SQL_TEST_DATABASE=HomestayBooking_SelfHostTest_Free_20261020 RUN_PERF_TESTS=1 dotnet test HomestayBooking.sln -c Release --no-build` thoát **0**, **1.781 pass / 0 fail / 0 skip**, thời gian runner **3 phút 20 giây** (`test-final.log`, `free-final.trx`). Bash `/bin/bash -n` toàn bộ script, `plutil -lint` cả 3 plist, kiểm link tài liệu nội bộ và `git diff --check` đều đạt. Code ở commit **`3f620aa`**, bộ triển khai ở **`2c6bae8`**; báo cáo/runbook được commit riêng tiếp theo. Toàn bộ bằng chứng lượt này nằm tại **`/private/tmp/fuji-selfhost-free/evidence/`**.

Giới hạn cần chủ máy xử lý trước kinh doanh: quyền Tailscale Personal cho mục đích thương mại chưa được xác nhận; SQL hiện vẫn Developer và bind LAN. Đây là kết quả chuẩn bị/diễn tập nội bộ, chưa công bố web, chưa chuyển SQL thật và chưa kiểm thanh toán thật.
