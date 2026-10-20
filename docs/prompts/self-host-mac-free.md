# Chạy trên máy Mac: phương án miễn phí (Tailscale Funnel, không tên miền, không SePay)

> Chạy **sau** `docs/prompts/self-host-mac.md` (nhánh `deploy/self-host-mac` đã có code + `deploy/mac/` + runbook + báo cáo diễn tập), trong phiên Codex mới, trên **cùng nhánh** đó.
>
> Ngày 20/10/2026 người dùng chốt: **chỉ dùng thứ miễn phí**.
> - Không mua tên miền, **không dùng Cloudflare Tunnel làm đường chính**.
> - Mở ra Internet bằng **Tailscale Funnel**: miễn phí, HTTPS sẵn, địa chỉ cố định `https://<tên-máy>.<tailnet>.ts.net`.
> - **Không dùng SePay.** Thanh toán: khách chuyển khoản theo QR VietQR, tải ảnh bill, kế toán đối chiếu sao kê ngân hàng và duyệt tay (luồng đã có). Webhook SePay để tắt: khóa trống.

Đọc trước:
- `docs/prompts/self-host-mac.md`;
- `docs/deploy/self-host-mac.md`, `docs/deploy/self-host-rehearsal-report.md`;
- `deploy/mac/`;
- `HomestayBooking.Web/Program.cs`;
- `Controllers/Api/BankWebhookController.cs`.

Luật giữ nguyên như prompt trước:
- không tự mở app ra Internet, không cài phần mềm hệ thống, không đổi cài đặt máy;
- không ghi bí mật vào repo;
- không đụng database đang có, container `sql-homestay`, app cổng 5011;
- không `pkill -f`/`killall` với mẫu ngắn;
- commit nhỏ, **không `Co-Authored-By`, không tên công cụ AI trong commit**, không push/merge.

## 1. Code

1. **Proxy không gửi `X-Forwarded-Proto`.** Tailscale Funnel/serve chuyển HTTPS về `http://127.0.0.1:8080`, có thể chỉ gửi `X-Forwarded-For`. Bảo đảm khi `ReverseProxy:Enabled=true`:
   - **không có vòng chuyển hướng HTTPS:** khi có proxy thì không dùng `UseHttpsRedirection`/HSTS ở app, để proxy lo HTTPS; hoặc coi request từ loopback là HTTPS qua cấu hình `ReverseProxy:AssumeHttps`;
   - cookie đăng nhập/antiforgery vẫn `Secure` và dùng được qua HTTPS của Funnel;
   - mọi URL tuyệt đối (QR, thông báo, lời mời, link trong tin nhắn) lấy từ `Reminders:PublicBaseUrl` hoặc khóa chung tương tự, không suy từ request.

   Thêm test cho trường hợp không có `X-Forwarded-Proto`.
2. **Chạy không SePay:**
   - `BankAccount:WebhookApiKey` trống thì webhook trả 404 hoặc 503 rõ ràng (không 500, không nhận dữ liệu);
   - trang chi tiết đơn của khách vẫn có QR chuyển khoản + ô tải bill;
   - kế toán duyệt bill như cũ.

   Kiểm và thêm test nếu chưa có.
3. Build không cảnh báo mới, toàn bộ test pass.

## 2. Bộ triển khai

- `deploy/mac/production.env.example`:
  - `ReverseProxy__Enabled=true` (+ `AssumeHttps` nếu dùng);
  - `Reminders__PublicBaseUrl=https://<tên-máy>.<tailnet>.ts.net`;
  - `BankAccount__*`: số tài khoản nhận tiền và tên chủ tài khoản (hiện trên QR VietQR); `WebhookApiKey` **để trống** (không dùng SePay).
- `deploy/mac/tailscale.md` hoặc script `funnel-on.sh` / `funnel-off.sh` / `funnel-status.sh` (chỉ in hoặc chạy lệnh khi người dùng tự gọi):
  - `tailscale serve --bg 8080` + `tailscale funnel --bg 8080` (hoặc cú pháp hiện hành của Tailscale; ghi phiên bản đã đối chiếu), tắt, xem trạng thái;
  - bật Funnel trong trang quản trị Tailscale (HTTPS certificates + thuộc tính funnel trong ACL).
- **Giữ** các file Cloudflare nhưng chuyển thành "phương án khi có tên miền", không phải đường chính.

## 3. Runbook `docs/deploy/self-host-mac.md`

Viết lại phần mở ra Internet theo **Tailscale Funnel làm đường chính**, từng bước cho người không chuyên:
1. Cài Tailscale cho macOS (bản miễn phí), đăng nhập (Google/GitHub/Microsoft).
2. Trong admin console: bật MagicDNS, bật HTTPS certificates, cho phép Funnel cho máy này.
3. Chạy app (`start.sh` / LaunchAgent), bật Funnel ở cổng 8080, lấy địa chỉ `https://….ts.net`.
4. Điền địa chỉ đó vào `Reminders__PublicBaseUrl`, khởi động lại app.
5. Thử từ điện thoại 4G: mở web, đặt đơn, chuyển khoản thật số nhỏ theo QR, tải bill, kế toán duyệt.
6. Cho Funnel tự bật lại khi khởi động máy (`--bg` giữ cấu hình; Tailscale mở khi đăng nhập).

Thêm các mục sau:
- **Giới hạn của Funnel bản miễn phí:** chỉ cổng 443/8443/10000; tên miền phụ `ts.net` không đổi được thành tên tùy ý; có giới hạn băng thông/kết nối (ghi theo tài liệu Tailscale hiện hành, không đoán số); máy phải bật.
- **Thanh toán (0đ):** khách chuyển khoản bằng QR VietQR (có sẵn) → tải ảnh bill trên trang đơn → kế toán đối chiếu sao kê/app ngân hàng và duyệt trong "Bill chờ duyệt" → chủ xác nhận. Ghi rõ việc kế toán cần làm hằng ngày (xem hàng chờ, đối chiếu nội dung chuyển khoản có mã đơn và số tiền, duyệt hoặc từ chối kèm lý do) và rủi ro (chậm hơn tự khớp; khách quên tải bill thì đơn hết hạn giữ phòng).
- **Bảng chi phí:** mọi thứ 0đ (macOS, Docker Desktop cá nhân, SQL Server Express, .NET, Tailscale cá nhân, VietQR; không SePay). Ghi rõ điều kiện giấy phép:
  - Docker Desktop miễn phí cho cá nhân/doanh nghiệp nhỏ theo điều khoản Docker;
  - SQL Server **Developer** không được dùng cho môi trường production thương mại; muốn hợp lệ thì dùng **SQL Server Express** (miễn phí, giới hạn 10 GB mỗi database). Hướng dẫn đổi image sang `MSSQL_PID=Express`, cách kiểm edition đang chạy.

- **SQL Server đang mở cổng 1433 ra mạng LAN** (lượt 1 phát hiện).
  - Hướng dẫn chuyển container sang chỉ nghe localhost (`-p 127.0.0.1:1433:1433`), đồng thời đổi sang `MSSQL_PID=Express`, theo cách **không mất dữ liệu**: sao lưu mọi database → tạo container mới gắn cùng volume dữ liệu (hoặc khôi phục từ bản sao lưu) → kiểm `SELECT SERVERPROPERTY('Edition')` → kiểm app.
  - Viết thành script `deploy/mac/sql-harden.sh` có bước xác nhận, nhưng **không tự chạy**: không dừng, xóa hay tạo lại container `sql-homestay`. Người dùng tự chạy theo runbook.
  - Kiểm hiện trạng bằng lệnh chỉ đọc (`docker port sql-homestay`, `lsof`) và ghi vào báo cáo.

## 4. Diễn tập lại trên máy (không ra Internet)

Giả lập Funnel: request tới `127.0.0.1:8080` chỉ kèm `X-Forwarded-For` (không có `X-Forwarded-Proto`), `Host: fuji-mac.example.ts.net`. Kiểm:
- không vòng chuyển hướng;
- đăng nhập và gửi form chạy được;
- URL tuyệt đối dùng `PublicBaseUrl`;
- webhook trả đúng mã khi không có khóa SePay;
- luồng không SePay: đặt đơn → QR → tải bill → kế toán duyệt → chủ xác nhận.

Dùng lại database diễn tập của lượt trước, hoặc tạo `HomestayBooking_ProdRehearsal2`. Không xóa database nào.

## 5. Báo cáo

Thêm mục "Phương án miễn phí (Tailscale Funnel)" vào `docs/deploy/self-host-rehearsal-report.md`:
- code đã sửa, test, kết quả diễn tập;
- **danh sách việc người dùng tự làm, theo thứ tự, kèm lệnh**;
- bảng chi phí và giấy phép;
- rủi ro.

Rồi dừng.
