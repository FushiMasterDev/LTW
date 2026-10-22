# Chạy Fuji Homestay trên máy Mac của chủ dự án

Máy Mac này là máy chủ Production. App nghe **127.0.0.1:8080**, SQL chạy trong container `sql-homestay`; **Tailscale Funnel** cung cấp HTTPS qua `https://TEN-MAY.TAILNET.ts.net`, không mua tên miền, không mở cổng router, không dùng SePay. Mọi lệnh cài đặt, public và đổi cấu hình máy dưới đây **do chủ máy tự chạy**.

**Điều kiện 0đ cần giải quyết trước khi kinh doanh:** Tailscale Personal miễn phí chỉ dành cho phi thương mại theo [bảng giá hiện hành](https://tailscale.com/pricing) đối chiếu 20/10/2026. Web homestay nhận tiền là trường hợp thương mại: chưa thể cam kết toàn bộ phương án 0đ hợp lệ nếu chưa có xác nhận phù hợp từ Tailscale. Các bước Funnel được chuẩn bị đầy đủ để chủ máy thực hiện khi quyền sử dụng đã rõ; không mua gói nào tự động.

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

## 2. Tailscale Funnel là đường chính (chủ máy tự thực hiện)

1. Cài Tailscale macOS **Standalone** miễn phí từ trang chính thức, đăng nhập bằng Google/GitHub/Microsoft; bật mở khi đăng nhập, cài CLI integration trong Settings. Không cần cài Tailscale trên điện thoại của khách.
2. Vào admin console bật MagicDNS và HTTPS certificates; Access controls cho phép thuộc tính `funnel` chỉ cho máy Mac này. Thực hiện từng bước và chép mẫu policy tại [deploy/mac/tailscale.md](../../deploy/mac/tailscale.md).
3. Chuẩn bị DB/env/app theo mục 3–5 trước khi public. Có thể dùng `tailscale serve --bg 8080` để xem trong tailnet trước; khi sẵn sàng, **chính chủ máy** chạy:

   ```bash
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   tailscale serve --bg 8080
   tailscale funnel --bg 8080
   tailscale funnel status
   ```

4. Chép URL HTTPS do lệnh status in ra vào env. Ví dụ `Reminders__PublicBaseUrl='https://fuji-mac.example.ts.net'`; `AllowedHosts='fuji-mac.example.ts.net;127.0.0.1;localhost'`. Đây chỉ là ví dụ, phải thay bằng URL thật. Giữ `ReverseProxy__Enabled='true'`, `ReverseProxy__AssumeHttps='true'`, `BankAccount__WebhookApiKey=''`, rồi restart:

   ```bash
   nano "$HOME/FujiHomestay/production.env"
   ./deploy/mac/stop.sh
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   # Nếu dùng LaunchAgent: stop tạo cờ bảo trì; tự bỏ cờ và kickstart thay start.sh:
   # rm -f "$HOME/FujiHomestay/run/maintenance"
   # launchctl kickstart -k "gui/$(id -u)/com.fuji.homestay"
   ```

5. Tắt Wi-Fi điện thoại để dùng **4G**, mở URL HTTPS, đăng nhập, đặt đơn thử, chuyển khoản thật số nhỏ theo QR, tải ảnh bill, kế toán đối chiếu rồi duyệt và chủ xác nhận. Không coi diễn tập loopback là đã kiểm 4G/chứng chỉ/tiền thật.
6. `--bg` giữ cấu hình Funnel qua reboot. Tailscale mở khi đăng nhập; app dùng LaunchAgent ở mục 5; Docker cũng phải mở. Sau reboot đăng nhập macOS/FileVault, kiểm `tailscale funnel status` và `/healthz`. Tắt bằng `tailscale funnel --bg 8080 off`; xem thêm cách tắt Serve trong tài liệu kèm.

**Giới hạn Funnel:** public chỉ cổng 443/8443/10000 (8080 là backend); tên dưới tailnet `ts.net`, không gắn tên miền tùy ý. Địa chỉ ổn định nếu giữ tên máy/tailnet. Băng thông bị giới hạn, tài liệu không đưa số Mbps/kết nối đồng thời; không cam kết tải vô hạn. Mac phải bật và không ngủ. Cú pháp đã đối chiếu tài liệu CLI từ 1.52 và changelog v1.104.1 ngày 20/10/2026; chưa cài/chạy Tailscale thật. [Nguồn giới hạn](https://tailscale.com/docs/features/tailscale-funnel#requirements-and-limitations), [CLI](https://tailscale.com/docs/reference/tailscale-cli/funnel).

### Cloudflare: phương án khi đã có tên miền

Giữ các file `deploy/mac/cloudflared/` để dùng tùy chọn về sau; không thuộc đường triển khai 0đ này. Nếu đã có domain và tự chọn Cloudflare, tạo named tunnel bằng hướng dẫn [Cloudflare](https://developers.cloudflare.com/tunnel/features/locally-managed-tunnels/create-local-tunnel/), điền UUID/credentials/hostname trong config mẫu, trỏ về `http://127.0.0.1:8080`, bật HTTPS ở edge. Chỉ cài LaunchAgent cloudflared nếu thực sự chọn phương án đó. Không chạy đồng thời hai đường public khi chưa chủ động quản lý hostname/cookie/URL; quick tunnel không phải địa chỉ vận hành cố định.

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

Điền `DOTNET_BIN` từ `command -v dotnet`, `ConnectionStrings__Default` đúng DB Production, `DATABASE_NAME=HomestayBooking`, quản trị đầu tiên, ngân hàng thật (mã ngân hàng, số tài khoản nhận tiền, tên chủ tài khoản hiển thị trên QR); **để trống khóa webhook** vì không dùng SePay. Giữ `ASPNETCORE_URLS=http://127.0.0.1:8080`.

Các URL cần cấu hình:

| Khóa | Giá trị và tác dụng |
|---|---|
| `Reminders__PublicBaseUrl` | `https://TEN-MAY.TAILNET.ts.net`, tối đa 50 ký tự (chọn tên máy ngắn nếu URL dài hơn), không userinfo/query/fragment; dùng cho tin nhắc khách **và lời mời kích hoạt đối tác** |
| `BankAccount__QrImageBaseUrl` | `https://img.vietqr.io/image`, dịch vụ tạo QR bên ngoài; không thay bằng hostname app |
| `BankAccount__FallbackQrImageUrl` | Mặc định trống ở Production; chỉ điền đường dẫn/URL ảnh QR đã kiểm đúng tài khoản thật (ví dụ `/uploads/verified-bank-qr.png`). Không dùng ảnh QR mẫu trong repo |
| `BankAccount__BankCode`, `AccountNumber`, `AccountName` | Tài khoản nhận cọc, quyết định nội dung QR và khớp tiền; không dùng số ví dụ |
| `BankAccount__WebhookApiKey` | **Trống**; webhook trả 503, không ghi dữ liệu |
| `DesktopApi__SigningKey` | Khóa bí mật ngẫu nhiên tối thiểu 32 byte UTF-8 cho JWT HS256; để trống/sai cấu hình thì chỉ desktop login trả 503, web vẫn khởi động và log cảnh báo |
| `DesktopApi__Issuer`, `DesktopApi__Audience` | Mặc định `FujiHomestay`, `FujiDesktop`; phải nhất quán khi cấp/kiểm token |
| `DesktopApi__TokenMinutes` | Mặc định 60, từ 1–1440 phút. Hết hạn thì đăng nhập lại |
| `DesktopApi__LoginPermitLimit`, `DesktopApi__LoginWindowSeconds` | Mặc định 10 lần/60 giây/IP, chỉ POST desktop login; không có global limiter |
| `AllowedHosts` | hostname thật + `127.0.0.1;localhost` để health check nội bộ hoạt động |
| `ReverseProxy__Enabled` | `true`; chỉ tin X-Forwarded-For/Proto từ proxy loopback, không tin X-Forwarded-Host; app bỏ redirect/HSTS, proxy lo TLS |
| `ReverseProxy__AssumeHttps` | `true`; coi socket loopback là HTTPS trước forwarded headers, hỗ trợ Funnel thiếu X-Forwarded-Proto và antiforgery Secure |
| `DataProtection__KeysPath` | thư mục tuyệt đối `$FUJI_HOME/data/App_Data/keys`, giữ nguyên qua mọi release |

Để bật API nhân viên WinForms, sinh khóa bằng `openssl rand -base64 48`, rồi điền trực tiếp vào file bí mật `production.env` ngoài repo (quyền 600). Không đưa khóa vào tài liệu, lệnh git hay log. Khóa dùng nguyên chuỗi UTF-8, không giải mã base64. Thay khóa và khởi động lại sẽ thu hồi toàn bộ token cũ; khóa tài khoản/đổi security stamp có hiệu lực ngay trên request Bearer tiếp theo. Desktop gửi HTTPS với `Authorization: Bearer <accessToken>`, không dùng cookie hoặc antiforgery. Không cấu hình CORS. Development/Testing khi thiếu khóa sinh khóa ngẫu nhiên mỗi lần khởi động; Production không sinh khóa để bật desktop tự động. Swagger vẫn chỉ Development.

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

`/healthz` công khai trả `Healthy`/200 khi app kết nối DB được, hoặc `Unhealthy`/503 không lộ lỗi. Health nội bộ bỏ chuyển hướng HTTPS. Khách truy cập HTTPS của Funnel; app không redirect HTTP backend và không tự phát HSTS khi bật proxy, các header bảo mật khác vẫn có. Không đăng nhập bằng HTTP loopback vì cookie Production bắt buộc Secure.

Bản publish ở `~/FujiHomestay/app`; `App_Data` và `wwwroot/uploads` là symlink tới `~/FujiHomestay/data/…`. Không xóa thư mục `data`. File khóa chưa mã hóa trên ổ: bảo vệ bằng quyền thư mục, FileVault và bản backup riêng tư. Khóa bị mất sẽ vô hiệu cookie/token cũ. Log ở `logs/app.log`, PID ở `run/app.pid`.

Để LaunchAgent độc lập với checkout repo, sao chép script đã kiểm và thay chỗ giữ trong plist:

```bash
mkdir -p "$HOME/FujiHomestay/scripts" "$HOME/FujiHomestay/logs" "$HOME/Library/LaunchAgents"
cp deploy/mac/*.sh "$HOME/FujiHomestay/scripts/"
# Script chạy không cần repo khi chỉ start/stop/status/backup/restore.
python3 - <<'PY'
import pathlib,plistlib,shutil
home=pathlib.Path.home(); base=home/'FujiHomestay'; repo=pathlib.Path.cwd()
for rel in ['com.fuji.homestay.plist','com.fuji.homestay.backup.plist']:
    raw=(repo/'deploy/mac'/rel).read_text().replace('/Users/REPLACE_ME/FujiHomestay',str(base))
    obj=plistlib.loads(raw.encode())
    dest=home/'Library/LaunchAgents'/pathlib.Path(rel).name
    dest.write_bytes(plistlib.dumps(obj))
PY
plutil -lint "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
# Dừng bản chạy tay trước, bỏ cờ bảo trì rồi bật LaunchAgent:
./deploy/mac/stop.sh
rm -f "$HOME/FujiHomestay/run/maintenance"
launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
launchctl bootstrap "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
launchctl print "gui/$(id -u)/com.fuji.homestay"
```

Funnel tự phục hồi nhờ cấu hình `--bg` của Tailscale, không cần LaunchAgent cloudflared. LaunchAgent app chạy `run.sh` foreground, KeepAlive giám sát đúng PID dotnet. `stop.sh` tạo cờ bảo trì để app không được tự bật lại; `start.sh` xóa cờ đó. Khi LaunchAgent đã nạp, `start.sh` (kể cả bên trong `update.sh`) nhờ launchd khởi động bằng `launchctl kickstart -k` thay vì tự chạy `run.sh`, nên app luôn được launchd giám sát sau mỗi lần cập nhật. Không xóa `start.lock` khi còn lượt khởi động; nếu máy tắt đột ngột, kiểm PID/cổng trước khi xóa lock cũ.

Gỡ tự chạy (không xóa dữ liệu):

```bash
tailscale funnel --bg 8080 off
launchctl bootout "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.backup.plist"
./deploy/mac/stop.sh
launchctl bootout "gui/$(id -u)" "$HOME/Library/LaunchAgents/com.fuji.homestay.plist"
```

## 6. Thanh toán (0đ): VietQR → tải bill → kế toán duyệt → chủ xác nhận

Không đăng ký/liên kết SePay; giữ `BankAccount__WebhookApiKey=''`. POST JSON hợp lệ tới `/api/bank-webhook/sepay` trả **503 Bank webhook is disabled** trước khi xử lý tiền. Payload sai định dạng có thể bị model validation trả 400; không có giao dịch nào được nhận khi khóa trống.

Khách mở trang chi tiết đơn → quét QR VietQR có sẵn (kiểm đúng người nhận, số tiền và nội dung có **mã đơn**) → chuyển khoản trong app ngân hàng → tải ảnh bill ngay trên trang đơn. Bill chuyển đơn sang `PaymentSubmitted`, tiếp tục giữ phòng. Kế toán vào **Kế toán → Bill chờ duyệt** (`/Accounting/Receipts`), đối chiếu **tiền thực nhận** trong sao kê/app ngân hàng rồi duyệt → đơn `Deposited`; chủ vào đơn và xác nhận → `Confirmed`.

Hằng ngày, kế toán mở hàng chờ nhiều lần trong giờ làm việc, kiểm đơn cũ trước, đối chiếu mã đơn trong nội dung, số tiền, tài khoản nhận và tránh duyệt hai bill cho cùng giao dịch. Xem ảnh bill chỉ để tham khảo; duyệt khi đã thấy tiền thực nhận. Không khớp thì từ chối kèm lý do cụ thể (sai số tiền, thiếu mã đơn, chưa thấy giao dịch…), liên hệ khách khi cần. Theo dõi cảnh báo bill chậm và bàn giao ngoài giờ. Quản lý cũng được duyệt; tài khoản Administrator không thay thế nhân viên kế toán/quản lý ở bước duyệt bill.

Duyệt tay chậm hơn tự khớp. Khách chuyển tiền nhưng quên tải bill có thể hết hạn giữ phòng (mặc định 15 phút); tiền không tự làm đơn sống lại. Cần hỗ trợ/đối chiếu và hoàn tiền nếu phòng đã hết, không hứa chắc còn phòng. Từ chối cho khách một hạn nộp lại; quá số lần cấu hình (mặc định 2) đơn hết hạn. Chủ cần xác nhận trong hạn sau khi nhận cọc. Thử tiền thật số nhỏ trước phục vụ khách; ảnh bill giả trong diễn tập không chứng minh đã nhận tiền.

## 7. Checklist sau khi đưa lên mạng

```bash
curl -I https://TEN-MAY.TAILNET.ts.net/
curl --fail https://TEN-MAY.TAILNET.ts.net/healthz
curl -o /dev/null -w '%{http_code}\n' https://TEN-MAY.TAILNET.ts.net/swagger
curl -o /dev/null -w '%{http_code}\n' https://TEN-MAY.TAILNET.ts.net/Dev/UiKit
curl -o /dev/null -w '%{http_code}\n' https://TEN-MAY.TAILNET.ts.net/Dev/SimulateBank
```

- HTTPS có chứng chỉ hợp lệ, không lặp chuyển hướng. Funnel chỉ phục vụ TLS ở public; không yêu cầu HTTP cổng 80 chuyển hướng. HTTP loopback chỉ để health/proxy, không dùng đăng nhập trực tiếp.
- Đăng nhập quản trị, đổi mật khẩu, xóa mật khẩu bootstrap; tạo nhân viên/chủ thật.
- Swagger và Dev trả 404; trang lỗi thân thiện không lộ stack/chuỗi kết nối.
- Tự đăng ký qua `/Partner`, nộp giấy tờ, duyệt/hợp đồng/nội dung, tìm được homestay.
- Đặt đơn và thanh toán nhỏ thật; tải bill, kế toán đối chiếu/duyệt, chủ xác nhận, không duyệt trùng.
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
| Funnel rớt/không truy cập được | `tailscale status`, `tailscale funnel status`, kiểm MagicDNS/HTTPS/quyền funnel, đăng nhập/expiry Tailscale; `curl http://127.0.0.1:8080/healthz`; kiểm Wi-Fi/router/DNS |
| Docker chưa chạy | Mở Docker Desktop bằng giao diện, chờ `docker ps`; không tạo/xóa container để chữa vội |
| App không khỏe | Xem `app.log`, env quyền 600, schema đã deploy, SQL đang chạy; không in env |
| 8080 bị chiếm | `lsof -nP -iTCP:8080 -sTCP:LISTEN`; script từ chối chiếm cổng, chỉ dừng đúng PID của mình |
| Ổ đầy | `df -h`, xem kích thước backups/release/log; chép backup ra nơi khác trước khi dọn; không xóa data/keys |
| Mất admin | Theo B7 không bootstrap thêm khi đã có Administrator; phục hồi tài khoản bằng quy trình có chủ đích, không seed Development |

Đổi mật khẩu `sa`: bảo trì có backup, đổi bằng SSMS/Azure Data Studio hoặc phiên `sqlcmd` tương tác không ghi mật khẩu vào lịch sử; cập nhật `ConnectionStrings__Default`. **Biến `MSSQL_SA_PASSWORD` của container không tự đổi theo ALTER LOGIN**; các script backup đang đọc biến đó, nên phải lập kế hoạch cập nhật cấu hình container bằng Docker Desktop/Compose (giữ volume, localhost binding) hoặc chuyển sang tài khoản vận hành riêng. Không dán mật khẩu vào lệnh `-Q`/`-P` hay repo. Đừng đổi một phía rồi bỏ quên phía còn lại. Bật MFA cho tài khoản đăng nhập Tailscale; theo dõi key expiry trong Machines và tái xác thực trước hạn.

## 9. Bảo vệ máy và SQL

Bật FileVault trong System Settings → Privacy & Security, bật tường lửa macOS, mật khẩu đăng nhập mạnh, không chia sẻ tài khoản vận hành. Các thiết lập này do chủ máy thao tác; không mở cổng router 8080/1433.

```bash
fdesetup status
/usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
docker port sql-homestay 1433
```

**Kiểm chỉ đọc ngày 20/10/2026:** SQL đang nghe `0.0.0.0:1433`, `[::]:1433` (LAN), volume `sql-homestay-data`, SQL Server 2022 **Developer**. Không public SQL và không mở cổng router 1433. Developer chỉ dùng phát triển/thử nghiệm, không hợp lệ cho Production thương mại. Đổi image edition bằng `MSSQL_PID=Express`, vẫn dùng image SQL Server **2022** cùng version; Express miễn phí, tối đa **10 GB dữ liệu mỗi database**. Không chỉ đổi env trong container cũ rồi cho rằng đã đổi edition. [Edition Microsoft](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022?view=sql-server-ver16), [container Production](https://learn.microsoft.com/en-us/sql/linux/containers/deploy?view=sql-server-linux-ver16).

### Bảo trì SQL không mất dữ liệu (chủ máy tự chạy)

Docker không sửa binding cổng tại chỗ. **Không dùng `docker rm -v`, không xóa volume cũ, không hạ edition trực tiếp trên bộ dữ liệu duy nhất.** Chọn cửa sổ bảo trì vì container dùng chung với app 5011. Nếu Mac Apple Silicon đang giả lập image amd64, đây vẫn là giới hạn hỗ trợ nền tảng của SQL container; không coi diễn tập này là chứng nhận hỗ trợ Production trên ARM. [Nền tảng SQL Linux](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment?view=sql-server-ver16).

1. Ghi danh sách **mọi** database, dung lượng, mount/image và edition; kiểm tính năng đặc thù, login/SID/quyền, Agent job, linked server, certificate/credential. Express không có SQL Agent. DB lớn hơn 10 GB hoặc tính năng không tương thích phải có kế hoạch riêng. Tự dừng toàn bộ app ghi SQL (cả app 5011); sao lưu App_Data/uploads, cấu hình và khóa ngoài repo. Việc này chưa được phiên chuẩn bị thực hiện.
2. Chạy script trong terminal tương tác:

   ```bash
   cd /Users/phamngocphu/Downloads/LTW
   ./deploy/mac/sql-harden.sh --apply
   ```

   Script yêu cầu xác nhận `DA_DUNG_APP_VA_SAO_LUU`, backup COPY_ONLY/CHECKSUM + VERIFYONLY **master/model/msdb và mọi DB người dùng** (tempdb tạo lại), chép ra `~/FujiHomestay/sql-harden-THOI_GIAN`, ghi manifest SHA-256. Chép thư mục đó sang ổ khác trước khi nhập `RESTORE_EXPRESS`. Backup có dữ liệu nhạy cảm, giữ quyền 600/700 và mã hóa bản ngoài máy.
3. Script giữ nguồn chạy, dựng container **sql-homestay-express**, volume **mới**, cùng image ID SQL 2022, env `MSSQL_PID=Express`, tạm bind `127.0.0.1:1434:1433`. Restore mọi DB người dùng với đường dẫn MOVE riêng, CHECKDB và kiểm edition. Không restore master/msdb khác edition; script từ chối login riêng/Agent job đang bật để tránh mất cấu hình âm thầm. Những cấu hình cấp server khác cần tự kiểm/xuất trước khi xác nhận.
4. Khi script chờ, mở terminal khác, dùng app kiểm thử riêng với connection port 1434 và kiểm dữ liệu, quyền, đăng nhập, đặt/duyệt đơn trên bản Express; đối chiếu tổng hàng/số tiền với nguồn, chạy `data_consistency.sql`. Không cho khách ghi vào cả hai bản. **Nếu chưa đạt: không nhập xác nhận cuối**, nguồn vẫn nguyên, đích/backup được giữ để xử lý.
5. Chỉ khi đã kiểm và dừng app thử, nhập `CHUYEN_SANG_EXPRESS`: script tắt tự restart của container nguồn để không giành lại cổng sau reboot, giữ container cũ dưới tên `sql-homestay-before-express-THOI_GIAN`, không xóa volume nguồn; tạo container đích tên `sql-homestay` gắn volume Express mới với **`-p 127.0.0.1:1433:1433`**. Dùng cùng mật khẩu sa ngoài repo, không in vào lệnh/log. Sau đó kiểm:

   ```bash
   docker port sql-homestay 1433
   lsof -nP -iTCP:1433 -sTCP:LISTEN
   # Truy vấn chỉ đọc, mật khẩu chỉ trong biến:
   SQLCMDPASSWORD="$(docker exec sql-homestay printenv MSSQL_SA_PASSWORD)"
   export SQLCMDPASSWORD
   docker exec -e SQLCMDPASSWORD sql-homestay /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "SELECT SERVERPROPERTY('Edition') AS Edition; SELECT name,state_desc FROM sys.databases;"
   unset SQLCMDPASSWORD
   ./deploy/mac/start.sh
   ./deploy/mac/status.sh
   ./deploy/mac/backup.sh
   ```

   Phải thấy Express và duy nhất `127.0.0.1:1433` trong `docker port` (Docker Desktop có thể hiển thị listener tổng quát trong lsof; port mapping là đối chiếu chính). Tự khởi động/kiểm cả app 5011 theo quy trình của app đó. Giữ container/volume cũ và backup đến khi xác minh hoàn tất.
6. Nếu lỗi trước cutover: nguồn chưa đổi, giữ app bảo trì rồi xử lý đích. Nếu lỗi sau cutover **trước khi nhận ghi mới**, script in tên rollback chính xác: dừng đích, đổi tên đích sang tên giữ lỗi, đổi container cũ lại `sql-homestay`, start container cũ (restart policy cũ có trong `source-config.txt`, chỉ khôi phục nó khi đã chủ động chọn rollback). Nếu lệnh tạo đích thất bại và tên `sql-homestay` chưa tồn tại, bỏ qua hai lệnh đầu sau khi kiểm `docker ps -a`. Nếu đã có giao dịch mới thì phải đồng bộ/khôi phục có chủ đích; quay lại snapshot cũ sẽ mất ghi mới. Bản cũ còn binding LAN/Developer, nên rollback chỉ để phục hồi và tiếp tục bảo trì.

Script **chưa được chạy** trên Docker thật trong phiên chuẩn bị; chỉ kiểm cú pháp và rà soát. Thất bại không tự xóa DB/volume. Mỗi lần chạy dùng tên/thư mục mới; nếu còn container thử thì dừng để người vận hành xử lý, không tự ghi đè.

## 10. Chi phí và giấy phép

| Thành phần | Phí phần mềm/dịch vụ dự kiến | Điều kiện |
|---|---:|---|
| macOS trên Mac đang có | 0đ thêm | Dùng Mac hợp lệ đã sở hữu |
| Docker Desktop | 0đ | Cá nhân; doanh nghiệp nhỏ dưới 250 nhân viên **và** doanh thu năm dưới 10 triệu USD; tổ chức chính phủ/doanh nghiệp vượt ngưỡng cần gói trả phí theo [Docker](https://docs.docker.com/subscription-billing/desktop-license/) |
| SQL Server **2022 Express** | 0đ | Production được phép, 10 GB mỗi DB; Developer đang chạy chỉ dành phát triển/thử nghiệm |
| .NET | 0đ | Mã nguồn mở, không có phí runtime; [Microsoft](https://dotnet.microsoft.com/en-us/platform/free) |
| Tailscale Personal + Funnel | 0đ **cho phi thương mại** | [Điều kiện hiện hành](https://tailscale.com/pricing); chưa bảo đảm quyền miễn phí cho kinh doanh homestay, không coi trial là miễn phí lâu dài |
| QR VietQR + tải bill/duyệt tay | 0đ phí tích hợp trong app | Dùng URL ảnh QR có sẵn, không mua API; chuyển tiền/phí tài khoản theo ngân hàng, thời gian kế toán không được tự động hóa |
| Tên miền / SePay | 0đ | Không mua / không dùng |

Đây là ngân sách phần mềm với máy/mạng sẵn có; điện, Internet, UPS/ổ backup và phí ngân hàng (nếu có) là chi phí vận hành thực, không tuyên bố bằng 0. **Mục tiêu mọi dịch vụ 0đ cho Production thương mại còn vướng điều kiện Tailscale**, cần quyền sử dụng phù hợp hoặc một quyết định đường public khác sau này; phiên này không đổi sang dịch vụ trả phí hay Cloudflare làm chính.
