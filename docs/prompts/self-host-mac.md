# Chạy Fuji Homestay thật trên máy Mac của chủ dự án

> Chạy trong **phiên Codex mới**, trên `main` (HEAD `86d574f` hoặc mới hơn). Người dùng chốt ngày 20/10/2026: **dùng chính máy Mac này làm máy chủ** (macOS 15, SQL Server 2022 đang chạy trong container Docker `sql-homestay`). Mục tiêu: chuẩn bị đầy đủ để app chạy ổn định ở chế độ **Production** trên máy này, mở ra Internet bằng **Cloudflare Tunnel** (có HTTPS, không cần mở cổng router), và diễn tập trọn vẹn trên máy.

Đọc trước:
- `README.md`, `database/README.md` (mục B7 "Quản trị viên đầu tiên", mục 5–7);
- `docs/qa/prelaunch-report.md` (mục 3 và 12);
- `HomestayBooking.Web/Program.cs`, `appsettings.json`;
- `Controllers/Api/BankWebhookController.cs`.

## Luật

- **Không tự mở app ra Internet.** Không chạy `cloudflared tunnel login`, không tạo tunnel thật, không chạy quick tunnel công khai, không mở cổng router. Mọi bước cần tài khoản Cloudflare, tên miền, khóa SePay hay tài khoản ngân hàng thật thì **viết lệnh sẵn trong runbook để người dùng tự chạy**.
- **Không cài phần mềm hệ thống** (Homebrew, cloudflared…). Ghi lệnh cài vào runbook.
- **Không đổi cài đặt hệ thống của máy** (`pmset`, tường lửa, tài khoản người dùng). Ghi lệnh khuyến nghị vào runbook.
- **Không ghi bí mật vào repo:** mật khẩu `sa`, mật khẩu quản trị, khóa SePay, số tài khoản. Bí mật nằm trong file môi trường ngoài repo, quyền `600`.
  - Lấy mật khẩu `sa` bằng `docker exec sql-homestay printenv MSSQL_SA_PASSWORD` đưa thẳng vào biến shell; **không in ra log/báo cáo**.
- **Không đụng** `HomestayBooking_v5` và các database đang có. Không dừng/xóa container `sql-homestay`. Không tắt app cổng 5011. Dừng tiến trình của mình bằng đúng PID; không dùng `pkill -f`/`killall` với mẫu ngắn.
- **Commit:** nhiều commit nhỏ trên nhánh mới `deploy/self-host-mac` tách từ `main`. **Không thêm dòng `Co-Authored-By`, không viết tên công cụ AI vào nội dung commit.** Không push, không merge.

## 1. Sửa code cho chạy thật sau tunnel

1. **Forwarded headers:** dùng `UseForwardedHeaders` (X-Forwarded-For, X-Forwarded-Proto), chỉ tin proxy loopback (cloudflared chạy trên cùng máy). Đặt trước `UseHsts`/`UseHttpsRedirection`, để app biết request gốc là HTTPS:
   - cookie `Secure` vẫn đúng;
   - không chuyển hướng HTTPS vòng lặp;
   - link trong thông báo/QR dùng đúng `https://tên-miền`.

   Bật bằng cấu hình (ví dụ `ReverseProxy:Enabled`), mặc định tắt để Development không đổi.
2. **Data Protection keys:** lưu khóa ra thư mục cố định cấu hình được (ví dụ `DataProtection:KeysPath`, mặc định `App_Data/keys`), đặt `SetApplicationName`. Nếu không làm, khởi động lại app sẽ đăng xuất mọi người và làm hỏng token form.
3. **Health check:** endpoint `GET /healthz` trả 200 khi app sống và kết nối được database (dùng `AddHealthChecks` + kiểm `DbContext`, không lộ chi tiết lỗi), dùng cho giám sát và script khởi động. Không yêu cầu đăng nhập, không bị header bảo mật làm hỏng.
4. **`Reminders:PublicBaseUrl`** và mọi URL tuyệt đối khác (QR, thông báo, lời mời đối tác) phải lấy từ cấu hình. Rà và ghi rõ các khóa cần đặt.
5. **Test cho cả 4 mục**, theo style test tích hợp hiện có. Build không cảnh báo mới, toàn bộ test pass.

## 2. Bộ triển khai trong repo (thư mục `deploy/mac/`)

- **`production.env.example`:** đủ mọi biến môi trường Production, giá trị giả và chú thích tiếng Việt, gồm:
  - `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://127.0.0.1:8080` (chỉ nghe localhost, tunnel nối vào);
  - `ConnectionStrings__Default` tới database Production mới `HomestayBooking`;
  - `Bootstrap__Administrator__*`;
  - `BankAccount__*` (gồm `WebhookApiKey`);
  - `Reminders__PublicBaseUrl`;
  - `DataProtection__KeysPath`, `ReverseProxy__Enabled`.

  Đọc code để không sót khóa nào bắt buộc.
- **`setup-database.sh`:** tạo database `HomestayBooking` nếu chưa có (có rồi thì dừng, không xóa), chạy `deploy.sql` + `verify.sql` trong container. Có tham số tên database để diễn tập.
- **`publish.sh`:** `dotnet publish -c Release` ra thư mục ngoài repo, ví dụ `~/FujiHomestay/app`. Giữ `App_Data` (bill, giấy tờ, khóa) và `wwwroot/uploads` khi cập nhật bản mới.
- **`start.sh` / `stop.sh` / `status.sh`:**
  - nạp file env (quyền phải là `600`, nếu không thì từ chối chạy);
  - chạy app nền, ghi PID và log vào thư mục cố định;
  - `status.sh` gọi `/healthz`.
- **LaunchAgent `com.fuji.homestay.plist`:** tự chạy app khi đăng nhập, tự khởi động lại khi app chết. Kèm lệnh `launchctl` cài/gỡ trong runbook (người dùng tự chạy).
- **`cloudflared/config.yml.example`:** tunnel trỏ `https://<tên-miền>` → `http://127.0.0.1:8080`. Kèm LaunchAgent mẫu cho `cloudflared` nếu cần.
- **`backup.sh`:**
  - `BACKUP DATABASE` trong container ra volume, chép file `.bak` ra `~/FujiHomestay/backups`, giữ 14 bản gần nhất;
  - nén `App_Data` + `wwwroot/uploads`;
  - LaunchAgent chạy hằng ngày.

  **`restore.sh`** khôi phục vào một database **tên khác** để kiểm tra, không ghi đè database đang chạy.
- **`update.sh`:** sao lưu → publish bản mới → dừng → thay → chạy → `/healthz`; lỗi thì quay lại bản cũ.

Script dùng `bash`, `set -euo pipefail`, chạy được nhiều lần (idempotent), không in bí mật.

## 3. Runbook `docs/deploy/self-host-mac.md` (tiếng Việt, cho người không chuyên)

1. **Yêu cầu và giới hạn của máy nhà:**
   - Mac phải luôn bật, cắm sạc, không ngủ: lệnh `pmset`/`caffeinate` khuyến nghị; hiện máy đặt ngủ sau 1 phút.
   - Docker Desktop tự chạy khi đăng nhập.
   - Mạng nhà (tốc độ upload, mất điện/mất mạng thì web ngừng).
   - Nên có UPS.
   - Một máy là một điểm hỏng duy nhất.
2. **Chuẩn bị Cloudflare:** tài khoản miễn phí, tên miền (mua mới hoặc chuyển DNS về Cloudflare); `brew install cloudflared`; `cloudflared tunnel login`, `create`, `route dns`, chạy dạng dịch vụ. Ghi rõ quick tunnel (`trycloudflare.com`) chỉ để thử, vì URL đổi mỗi lần chạy nên không dùng cho webhook SePay.
3. **Database Production:** chạy `setup-database.sh`.
4. **Bí mật:** chép `production.env.example` → `~/FujiHomestay/production.env`, `chmod 600`, điền:
   - chuỗi kết nối;
   - quản trị viên đầu tiên (sau lần chạy đầu thì **xóa dòng mật khẩu** theo README B7);
   - ngân hàng, `WebhookApiKey`;
   - `PublicBaseUrl` là tên miền.
5. **Publish, chạy, cài LaunchAgent; kiểm `/healthz`.**
6. **SePay:** đăng ký, liên kết tài khoản ngân hàng thật, đặt webhook `https://<tên-miền>/api/bank-webhook/sepay` với header `Authorization: Apikey <khóa>`; thử một giao dịch nhỏ thật.
7. **Kiểm tra sau khi lên mạng (checklist):**
   - HTTPS khóa xanh, chuyển hướng HTTP → HTTPS;
   - đăng nhập quản trị, đổi mật khẩu, tạo nhân viên/chủ thật;
   - `/swagger` và `/Dev/*` trả 404;
   - đặt một đơn thật và chuyển một khoản nhỏ thật để thử khớp tiền;
   - thử trên điện thoại 4G (ngoài mạng nhà).
8. **Vận hành:** xem log, khởi động lại, cập nhật bản mới (`update.sh`), sao lưu/khôi phục, đổi mật khẩu `sa`, gia hạn tên miền. Gặp sự cố: tunnel rớt, Docker không chạy, ổ đầy.
9. **Bảo mật máy:** bật FileVault, tường lửa macOS, mật khẩu đăng nhập máy, không chia sẻ tài khoản; cổng 1433 của SQL chỉ nghe localhost (kiểm và hướng dẫn nếu đang mở ra mạng LAN).

## 4. Diễn tập trên máy (không ra Internet)

1. `setup-database.sh HomestayBooking_ProdRehearsal`.
2. File env diễn tập **ngoài repo** (`/private/tmp/fuji-selfhost/rehearsal.env`, quyền `600`):
   - mật khẩu quản trị ngẫu nhiên, ghi vào file `600` cạnh đó, không in ra;
   - `WebhookApiKey` ngẫu nhiên.
3. `publish.sh` vào `/private/tmp/fuji-selfhost/app`, `start.sh` ở **cổng 8080 localhost**. Không cài LaunchAgent thật; nếu muốn kiểm plist thì chỉ `plutil -lint`.
4. Giả lập tunnel: gửi request kèm `X-Forwarded-Proto: https`, `X-Forwarded-For`, `Host: fuji.example` từ loopback. Kiểm:
   - cookie `Secure`;
   - không vòng chuyển hướng;
   - URL tuyệt đối dùng `https://fuji.example`;
   - header bảo mật có mặt;
   - `/swagger`, `/Dev/UiKit`, `/Dev/SimulateBank` là 404;
   - trang lỗi thân thiện.
5. **Quản trị viên đầu tiên:** đăng nhập, đổi mật khẩu, tạo 1 nhân viên + 1 chủ (qua `/Partner`) + duyệt + hợp đồng + homestay lên trang tìm kiếm; khách đặt 1 đơn.
6. **Webhook như SePay thật:** POST JSON mẫu đúng định dạng SePay với khóa đúng → đơn tự khớp, sang Deposited; khóa sai → 401; gửi trùng → không cộng hai lần.
7. **Khởi động lại app:** vẫn đăng nhập được (khóa Data Protection còn), form cũ không lỗi token, file upload còn.
8. `backup.sh` → `restore.sh` sang `HomestayBooking_ProdRehearsal_Restore` → `data_consistency.sql` 35/35 trên cả hai.
9. `update.sh` với một bản build mới: `/healthz` xanh sau cập nhật.
10. Dừng app diễn tập. **Giữ** database diễn tập và thư mục `/private/tmp/fuji-selfhost` để người dùng xem; ghi tên vào báo cáo.

## 5. Báo cáo rồi dừng

Viết `docs/deploy/self-host-rehearsal-report.md`:
- code đã sửa và test;
- danh sách file trong `deploy/mac/`;
- kết quả từng bước diễn tập (Đạt/Lỗi + bằng chứng);
- **danh sách việc người dùng phải tự làm**, theo thứ tự, mỗi việc kèm lệnh: mua tên miền, Cloudflare, SePay, điền bí mật, cài LaunchAgent, chỉnh `pmset`;
- rủi ro của việc chạy trên máy nhà;
- mục "Cần người dùng quyết định".

Build không cảnh báo, toàn bộ test pass. Không push, không merge.
