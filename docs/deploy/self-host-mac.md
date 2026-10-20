# Chạy Fuji Homestay trên máy Mac của chủ dự án

Máy Mac này là máy chủ Production. App nghe **127.0.0.1:8080**, SQL chạy trong container `sql-homestay`; Cloudflare Tunnel nối từ máy ra Cloudflare và cung cấp HTTPS cho khách. Không cần mở cổng router. Các lệnh Cloudflare, cài phần mềm, LaunchAgent và thay đổi hệ thống bên dưới **do chủ máy tự chạy** khi đã sẵn sàng đưa web lên mạng.

## 1. Chuẩn bị máy và hiểu giới hạn

Cắm nguồn, để máy luôn bật, không đóng nắp. Hiện `pmset -g custom` cho thấy `sleep 1` khi cắm nguồn: máy có thể ngủ sau một phút. Người dùng có thể chọn:

```bash
pmset -g custom                     # xem, không thay đổi
sudo pmset -c sleep 0               # tắt ngủ khi cắm điện; vẫn cho màn hình tắt
# Hoặc giữ máy thức tạm thời trong terminal này:
caffeinate -s
```

Không coi `caffeinate` là bảo đảm khi đóng nắp. Sau khi khởi động lại phải đăng nhập người dùng vận hành: LaunchAgent chạy sau đăng nhập, không phải trước màn FileVault. Trong Docker Desktop → Settings → General bật khởi động khi đăng nhập. Docker có thể sẵn sàng chậm hơn app; LaunchAgent sẽ thử lại app khi startup SQL thất bại.

Upload của mạng nhà quyết định tốc độ khách tải ảnh. Mất điện, mất Internet, Docker ngừng, hết ổ đĩa hoặc Mac ngủ đều làm web ngừng. Nên có UPS cho Mac/router và bản sao lưu ở ổ/máy khác. Một máy là một điểm hỏng duy nhất; đây không phải hệ thống dự phòng cao. Kiểm lại giấy phép SQL Server trước khai thác thương mại: SQL Server Developer chỉ cho phát triển/thử nghiệm; cần edition/giấy phép phù hợp tải thực tế. Xem [quyền sử dụng Developer theo Microsoft](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022?view=sql-server-ver17).

## 2. Tên miền và Cloudflare (tự thực hiện)

Mua tên miền ở nhà đăng ký bạn chọn hoặc dùng tên miền đang sở hữu; chuyển nameserver theo Cloudflare. Tạo tài khoản Cloudflare miễn phí và thêm domain; tên miền có phí theo nhà đăng ký. Thay `fuji.example` bằng hostname thật ở **mọi** vị trí. Lệnh không thể thay thao tác mua/đăng ký tài khoản.

```bash
# Nếu đã có Homebrew; nếu chưa có, cài từ https://brew.sh bằng hướng dẫn chính thức.
brew install cloudflared
cloudflared tunnel login
cloudflared tunnel create fuji-homestay
cloudflared tunnel route dns fuji-homestay fuji.example
mkdir -p "$HOME/FujiHomestay/cloudflared"
cp deploy/mac/cloudflared/config.yml.example "$HOME/FujiHomestay/cloudflared/config.yml"
nano "$HOME/FujiHomestay/cloudflared/config.yml"
# Điền UUID từ lệnh create, đường dẫn credentials JSON và hostname.
chmod 600 "$HOME/FujiHomestay/cloudflared/config.yml"
cloudflared tunnel --config "$HOME/FujiHomestay/cloudflared/config.yml" ingress validate
# Chỉ chạy sau khi app và bí mật đã chuẩn bị xong:
cloudflared tunnel --config "$HOME/FujiHomestay/cloudflared/config.yml" run
```

Trong Cloudflare SSL/TLS → Edge Certificates bật **Always Use HTTPS**, rồi thử HTTP phải chuyển sang HTTPS. Không bật cache cho HTML có cookie, trang tài khoản, đặt phòng hoặc API; giữ quy tắc cache mặc định cho nội dung động. Không đặt Cloudflare Access/challenge chặn endpoint SePay. Cloudflare bảo vệ HTTPS phía khách; kết nối tunnel → app là HTTP chỉ trong loopback.

Quick tunnel `trycloudflare.com` chỉ dành cho thử nhanh, URL đổi mỗi lần nên **không dùng cho webhook SePay**. Phiên chuẩn bị không chạy quick tunnel hay tunnel thật.

Tài liệu chính thức: [tạo tunnel](https://developers.cloudflare.com/tunnel/features/locally-managed-tunnels/create-local-tunnel/), [cấu hình ingress](https://developers.cloudflare.com/tunnel/features/locally-managed-tunnels/configuration-file/), [dịch vụ macOS](https://developers.cloudflare.com/tunnel/features/locally-managed-tunnels/as-a-service/macos/).

## 3. Dựng database Production mới

Đứng tại repo, nhánh triển khai đã kiểm:

```bash
cd /Users/phamngocphu/Downloads/LTW
docker ps --filter name=sql-homestay
./deploy/mac/setup-database.sh HomestayBooking
```

Script tự lấy mật khẩu `sa` từ container vào bộ nhớ, không in mật khẩu; tạo DB mới rồi chạy `deploy.sql` và `verify.sql`. Nếu DB có sẵn, script **dừng**; không tự xóa, không chạy app vào DB đó để thử lại. Nếu tạo dở do lỗi, giữ DB và xem log để xử lý có chủ đích. Không dùng `HomestayBooking_v5` hay DB có dữ liệu khác cho diễn tập. Không bật `Database__AutoMigrate` khi vận hành.

## 4. File bí mật ngoài repo

```bash
mkdir -p "$HOME/FujiHomestay"
chmod 700 "$HOME/FujiHomestay"
cp deploy/mac/production.env.example "$HOME/FujiHomestay/production.env"
chmod 600 "$HOME/FujiHomestay/production.env"
command -v dotnet
nano "$HOME/FujiHomestay/production.env"
```

Đây là file shell, bao giá trị bằng dấu nháy đơn. Nếu mật khẩu có dấu nháy đơn, dùng cú pháp shell đúng hoặc tạo mật khẩu mới không chứa ký tự đó; không chạy `cat`/`set -x`/`env` để kiểm tra bí mật. Script từ chối env không thuộc bạn hoặc khác quyền 600. Chỉ dùng file tự quản lý vì `source` sẽ thực thi nội dung file.

Điền `DOTNET_BIN` từ `command -v dotnet`, `ConnectionStrings__Default` đúng DB Production, `DATABASE_NAME=HomestayBooking`, quản trị đầu tiên, ngân hàng thật và khóa webhook mạnh (tạo bằng `openssl rand -hex 32`). Giữ `ASPNETCORE_URLS=http://127.0.0.1:8080`.

Các URL cần cấu hình:

| Khóa | Giá trị và tác dụng |
|---|---|
| `Reminders__PublicBaseUrl` | `https://hostname-thật`, tối đa 50 ký tự, không userinfo/query/fragment; dùng cho tin nhắc khách **và lời mời kích hoạt đối tác** |
| `BankAccount__QrImageBaseUrl` | `https://img.vietqr.io/image`, dịch vụ tạo QR bên ngoài; không thay bằng hostname app |
| `BankAccount__FallbackQrImageUrl` | Mặc định trống ở Production; chỉ điền đường dẫn/URL ảnh QR đã kiểm đúng tài khoản thật (ví dụ `/uploads/verified-bank-qr.png`). Không dùng ảnh QR mẫu trong repo |
| `BankAccount__BankCode`, `AccountNumber`, `AccountName` | Tài khoản nhận cọc, quyết định nội dung QR và khớp tiền; không dùng số ví dụ |
| `BankAccount__WebhookApiKey` | Khớp đúng khóa xác thực API Key ở SePay |
| `AllowedHosts` | hostname thật + `127.0.0.1;localhost` để health check nội bộ hoạt động |
| `ReverseProxy__Enabled` | `true`; chỉ tin X-Forwarded-For/Proto từ proxy loopback, không tin X-Forwarded-Host |
| `DataProtection__KeysPath` | thư mục tuyệt đối `$FUJI_HOME/data/App_Data/keys`, giữ nguyên qua mọi release |

Nếu dùng QR dự phòng, tự xuất QR từ ngân hàng thật, kiểm số tài khoản bằng ứng dụng ngân hàng rồi chép **ngoài repo**:

```bash
cp /DUONG_DAN/QR_DA_KIEM.png "$HOME/FujiHomestay/data/uploads/verified-bank-qr.png"
# Trong production.env: BankAccount__FallbackQrImageUrl='/uploads/verified-bank-qr.png'
```

Nếu chưa có ảnh đã kiểm, giữ biến này trống; không dùng `/images/qr-mb.png` của môi trường demo.

Các link điều hướng, thông báo trong app và trang xem đơn dùng đường dẫn tương đối; trình duyệt giữ hostname HTTPS. URL ảnh QR là dịch vụ VietQR; ảnh QR dự phòng cũng lấy cấu hình riêng. Production không dùng ảnh tài khoản demo; khi ảnh lỗi, trang hướng dẫn chuyển khoản bằng thông tin đã cấu hình. Development giữ ảnh dự phòng mẫu trong cấu hình riêng. Google Fonts, Bootstrap Icons CDN và map người chủ cung cấp là tài nguyên bên ngoài, không có khóa máy chủ bổ sung. Tin SMS/Zalo hiện là giả lập `LoggingMessageSender`, chưa giao tin thật (README B4/B7).

File mẫu còn đủ Booking/Settlements/Reminders/Voucher; không cần đổi thông số nghiệp vụ mặc định. `Bootstrap__Administrator__Email` tùy chọn. Ngay sau lần đầu tạo quản trị, đăng nhập và đổi mật khẩu, **xóa dòng `Bootstrap__Administrator__Password` khỏi env**, như [B7](../../database/README.md). Không chỉ để lại bản cũ trong file backup của trình soạn thảo. Chỉ cấp nhân viên/chủ thật qua giao diện; Production không seed tài khoản mẫu.

## 5. Publish, chạy và tự khởi động khi đăng nhập

Cần .NET SDK theo `global.json`, Docker, Python 3 và các công cụ macOS `curl`, `tar`, `lsof`, `shasum`. Không cần sqlcmd trên host.

```bash
./deploy/mac/publish.sh
./deploy/mac/start.sh
./deploy/mac/status.sh
curl --fail http://127.0.0.1:8080/healthz
# Trình duyệt dùng hostname HTTPS sau khi tunnel chạy.
```

`/healthz` công khai trả `Healthy`/200 khi app kết nối DB được, hoặc `Unhealthy`/503 không lộ lỗi. Health nội bộ bỏ chuyển hướng HTTPS. Các route khác đi qua HTTPS và header bảo mật. Không đăng nhập bằng HTTP loopback vì cookie Production bắt buộc Secure.

Bản publish ở `~/FujiHomestay/app`; `App_Data` và `wwwroot/uploads` là symlink tới `~/FujiHomestay/data/…`. Không xóa thư mục `data`. File khóa chưa mã hóa trên ổ: bảo vệ bằng quyền thư mục, FileVault và bản backup riêng tư. Khóa bị mất sẽ vô hiệu cookie/token cũ. Log ở `logs/app.log`, PID ở `run/app.pid`.

Để LaunchAgent độc lập với checkout repo, sao chép script đã kiểm và thay chỗ giữ trong plist:

```bash
mkdir -p "$HOME/FujiHomestay/scripts" "$HOME/FujiHomestay/logs" "$HOME/Library/LaunchAgents"
cp deploy/mac/*.sh "$HOME/FujiHomestay/scripts/"
# Script chạy không cần repo khi chỉ start/stop/status/backup/restore.
python3 - <<'PY'
import pathlib,plistlib,shutil
home=pathlib.Path.home(); base=home/'FujiHomestay'; repo=pathlib.Path.cwd()
for rel in ['com.fuji.homestay.plist','com.fuji.homestay.backup.plist','cloudflared/com.fuji.cloudflared.plist']:
    raw=(repo/'deploy/mac'/rel).read_text().replace('/Users/REPLACE_ME/FujiHomestay',str(base))
    obj=plistlib.loads(raw.encode())
    if obj['Label']=='com.fuji.cloudflared':
        obj['ProgramArguments'][0]=shutil.which('cloudflared') or '/opt/homebrew/bin/cloudflared'
    dest=home/'Library/LaunchAgents'/pathlib.Path(rel).name
    dest.write_bytes(plistlib.dumps(obj))
PY
plutil -lint "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
# Dừng bản chạy tay trước, bỏ cờ bảo trì rồi bật LaunchAgent:
./deploy/mac/stop.sh
rm -f "$HOME/FujiHomestay/run/maintenance"
launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
# Chỉ khi đã chọn đưa lên Internet:
launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.cloudflared.plist"
launchctl print "gui/$(id -u)/com.fuji.homestay"
```

Chỉ cài **một** dịch vụ cloudflared: mẫu ở trên hoặc `cloudflared service install` theo tài liệu Cloudflare, không cả hai. LaunchAgent app chạy `run.sh` foreground, KeepAlive giám sát đúng PID dotnet. `stop.sh` tạo cờ bảo trì để app không được tự bật lại; `start.sh` xóa cờ đó. Không xóa `start.lock` khi còn lượt khởi động; nếu máy tắt đột ngột, kiểm PID/cổng trước khi xóa lock cũ.

Gỡ tự chạy (không xóa dữ liệu):

```bash
launchctl bootout "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.cloudflared.plist"
launchctl bootout "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
./deploy/mac/stop.sh
launchctl bootout "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
```

## 6. SePay và thử tiền thật (chủ tài khoản thực hiện)

Đăng ký SePay, liên kết tài khoản ngân hàng thật trùng tài khoản QR. Trong mục Webhooks tạo webhook nhận tiền vào:

- URL: `https://hostname-thật/api/bank-webhook/sepay`.
- Xác thực **API Key**, header `Authorization: Apikey <khóa trong env>`.
- Payload JSON chuẩn SePay, `transactionDate` dạng `yyyy-MM-dd HH:mm:ss` giờ Việt Nam.

Đặt một đơn, chuyển khoản nhỏ **đúng AmountDue và nội dung chứa mã đơn**. Kiểm SePay nhận HTTP 200 + `success:true`, giao dịch khớp, đơn sang `Deposited`; chủ xác nhận tiếp theo quy trình. Gửi trùng phải không tăng tiền. Khóa sai phải 401. Không dùng dữ liệu giả lập như bằng chứng chuyển tiền thật. Xem [tạo webhook](https://developer.sepay.vn/en/sepay-webhooks/tao-webhook) và [xác thực SePay](https://developer.sepay.vn/en/sepay-webhooks/xac-thuc).

## 7. Checklist sau khi đưa lên mạng

```bash
curl -I http://hostname-thật/
curl -I https://hostname-thật/
curl --fail https://hostname-thật/healthz
curl -o /dev/null -w '%{http_code}\n' https://hostname-thật/swagger
curl -o /dev/null -w '%{http_code}\n' https://hostname-thật/Dev/UiKit
curl -o /dev/null -w '%{http_code}\n' https://hostname-thật/Dev/SimulateBank
```

- HTTPS có chứng chỉ hợp lệ, HTTP chuyển sang HTTPS, không lặp chuyển hướng.
- Đăng nhập quản trị, đổi mật khẩu, xóa mật khẩu bootstrap; tạo nhân viên/chủ thật.
- Swagger và Dev trả 404; trang lỗi thân thiện không lộ stack/chuỗi kết nối.
- Tự đăng ký qua `/Partner`, nộp giấy tờ, duyệt/hợp đồng/nội dung, tìm được homestay.
- Đặt đơn và thanh toán nhỏ thật; xác nhận khớp đúng, không nhân đôi.
- Dùng điện thoại **4G ngoài Wi-Fi nhà** kiểm các bước trên. Tắt Wi-Fi để chắc chắn đang ở mạng ngoài.

## 8. Vận hành và xử lý sự cố

```bash
tail -n 100 "$HOME/FujiHomestay/logs/app.log"
./deploy/mac/status.sh
./deploy/mac/stop.sh
./deploy/mac/start.sh
./deploy/mac/backup.sh
# Khôi phục kiểm tra vào DB MỚI, không ghi đè DB chạy:
./deploy/mac/restore.sh "$HOME/FujiHomestay/backups/TEN_BAN_SAO.bak" HomestayBooking_Restore_20261020
```

Backup dùng COPY_ONLY/CHECKSUM, VERIFYONLY; bản `.bak` nằm trong volume SQL và được chép ra `backups`. Cùng timestamp có `.files.tar.gz` (App_Data + uploads) và manifest SHA-256. Giữ 14 bộ hoàn chỉnh gần nhất **theo database**, ở host và volume SQL. Mẫu LaunchAgent chạy 03:15 mỗi ngày; máy phải thức và Docker đang chạy. Nếu bỏ lỡ do máy tắt, chạy tay. Sao chép bộ backup ra ổ ngoài/vị trí riêng có mã hóa; backup chứa khóa và giấy tờ nhạy cảm. Sao lưu khi đang có ghi có thể lệch thời điểm giữa SQL và file; để phục hồi nhất quán cao nhất, chọn giờ ít khách hoặc dừng app ngắn trước backup.

Kiểm bộ file mà không chép đè:

```bash
cd "$HOME/FujiHomestay/backups"
shasum -a 256 -c TEN_BAN_SAO.sha256
mkdir -p "$HOME/FujiHomestay/restore-files-check"
tar -xzf TEN_BAN_SAO.files.tar.gz -C "$HOME/FujiHomestay/restore-files-check"
# Chạy data_consistency.sql chỉ đọc trên DB vừa restore (dùng sqlcmd trong container như database/README).
```

Cập nhật từ repo sau khi kiểm code và build/test:

```bash
cd /Users/phamngocphu/Downloads/LTW
./deploy/mac/update.sh
# Sau khi thành công, cập nhật bản script vận hành đã kiểm nếu script có thay đổi:
cp deploy/mac/*.sh "$HOME/FujiHomestay/scripts/"
```

Update khóa chống chạy song song, backup → publish thư mục chờ → dừng → đổi release → chạy → health. Lỗi app mới sẽ giữ bản lỗi và phục hồi binary trước; dữ liệu dùng chung còn nguyên. Không tự migrate, không tự restore DB khi rollback binary. Trước khi nhận thay đổi schema phải có kế hoạch deploy/rollback riêng. Thư mục release cũ không tự xóa: kiểm rồi dọn định kỳ để tránh đầy ổ. Đặt cảnh báo dung lượng/log và giám sát `/healthz` từ bên ngoài sau khi public; chưa có log rotation tự động, nên lưu trữ/xoay `logs/app.log` khi bảo trì.

| Sự cố | Kiểm tra / xử lý |
|---|---|
| Tunnel rớt, Cloudflare 502/1033 | `launchctl print gui/$(id -u)/com.fuji.cloudflared`, xem log cloudflared, `curl http://127.0.0.1:8080/healthz`; kiểm Wi-Fi/router/DNS |
| Docker chưa chạy | Mở Docker Desktop bằng giao diện, chờ `docker ps`; không tạo/xóa container để chữa vội |
| App không khỏe | Xem `app.log`, env quyền 600, schema đã deploy, SQL đang chạy; không in env |
| 8080 bị chiếm | `lsof -nP -iTCP:8080 -sTCP:LISTEN`; script từ chối chiếm cổng, chỉ dừng đúng PID của mình |
| Ổ đầy | `df -h`, xem kích thước backups/release/log; chép backup ra nơi khác trước khi dọn; không xóa data/keys |
| Mất admin | Theo B7 không bootstrap thêm khi đã có Administrator; phục hồi tài khoản bằng quy trình có chủ đích, không seed Development |

Đổi mật khẩu `sa`: bảo trì có backup, đổi bằng SSMS/Azure Data Studio hoặc phiên `sqlcmd` tương tác không ghi mật khẩu vào lịch sử; cập nhật `ConnectionStrings__Default`. **Biến `MSSQL_SA_PASSWORD` của container không tự đổi theo ALTER LOGIN**; các script backup đang đọc biến đó, nên phải lập kế hoạch cập nhật cấu hình container bằng Docker Desktop/Compose (giữ volume, localhost binding) hoặc chuyển sang tài khoản vận hành riêng. Không dán mật khẩu vào lệnh `-Q`/`-P` hay repo. Đừng đổi một phía rồi bỏ quên phía còn lại. Theo dõi hạn tên miền, thanh toán và email gia hạn, bật MFA Cloudflare/nhà đăng ký/SePay.

## 9. Bảo vệ máy và SQL

Bật FileVault trong System Settings → Privacy & Security, bật tường lửa macOS, mật khẩu đăng nhập mạnh, không chia sẻ tài khoản vận hành. Các thiết lập này do chủ máy thao tác; không mở cổng router 8080/1433.

```bash
fdesetup status
/usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
docker port sql-homestay 1433
```

**Máy diễn tập hiện có `0.0.0.0:1433` và `[::]:1433`**, tức SQL được công bố trên LAN. Cần sửa trước public: trong một cửa sổ bảo trì riêng, backup/xác minh restore, lưu cấu hình Docker hiện có và volume, đổi publish port thành `127.0.0.1:1433:1433` rồi tái tạo container dùng **đúng volume cũ**. Docker không sửa port binding container đang chạy tại chỗ. Không thực hiện lệnh xóa/dừng container đang chạy trong phiên này. Với Compose do bạn quản lý, trường cần sửa là:

```yaml
ports:
  - "127.0.0.1:1433:1433"
```

Sau bảo trì `docker port sql-homestay 1433` chỉ được in `127.0.0.1:1433`; kiểm ứng dụng và backup lại. Việc đổi SQL edition/credential/container là bước riêng cần người dùng quyết định để tránh ảnh hưởng app cổng 5011 đang dùng chung SQL.
