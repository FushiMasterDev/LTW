# Tailscale Funnel — lệnh để chủ máy tự chạy

Đối chiếu tài liệu chính thức ngày **20/10/2026**, CLI **1.52 trở lên**, changelog có **v1.104.1 (07/10/2026)**. Đây là kiểm tài liệu, chưa cài/chạy Tailscale trên máy này. Xem [CLI Funnel](https://tailscale.com/docs/reference/tailscale-cli/funnel) và [changelog](https://tailscale.com/changelog#2026-10-07-client).

**Giấy phép:** Personal có giá 0đ cho phi thương mại. Web nhận đặt phòng kinh doanh không được mặc nhiên tính là Personal hợp lệ chỉ vì chạy trên Mac cá nhân hoặc đăng nhập Gmail. Cần Tailscale xác nhận quyền dùng miễn phí cho trường hợp này trước khi public; không đăng ký gói trả phí trong quy trình này. [Điều kiện gói](https://tailscale.com/pricing).

1. Tải bản macOS **Standalone** từ [Tailscale](https://tailscale.com/download/mac), mở app, cho phép system extension theo hướng dẫn và đăng nhập Google/GitHub/Microsoft. Không cài đồng thời nhiều bản Tailscale. Bật mở app khi đăng nhập. Trong Settings → CLI integration → Show me how → Install Now để dùng `tailscale` trong Terminal; chủ máy tự nhập quyền quản trị khi được hỏi. [Các bản macOS](https://tailscale.com/docs/concepts/macos-variants), [CLI macOS](https://tailscale.com/docs/reference/tailscale-cli?tab=macos).
2. Vào [admin console](https://console.tailscale.com/), DNS → bật **MagicDNS**, **HTTPS certificates**. Chứng chỉ làm tên máy/tailnet xuất hiện trong sổ Certificate Transparency; chọn tên không chứa thông tin riêng tư. [HTTPS](https://tailscale.com/docs/how-to/set-up-https-certificates).
3. Access controls → Funnel → Add Funnel to policy. Mặc định cho `autogroup:member`; để chỉ Mac này được bật Funnel, dùng tag riêng `tag:fuji-server`. Gộp mẫu dưới vào `tagOwners`/`nodeAttrs` hiện có, không thay cả policy; bỏ quyền Funnel mặc định rộng nếu đã có. Sau khi lưu, vào Machines → đúng máy Mac → Edit tags → gắn **duy nhất máy này** tag `tag:fuji-server`:

   ```json
   {
     "tagOwners": { "tag:fuji-server": ["autogroup:admin"] },
     "nodeAttrs": [
       { "target": ["tag:fuji-server"], "attr": ["funnel"] }
     ]
   }
   ```

   Lưu/validate policy bằng giao diện admin, giữ các grants/ACL đang dùng. Tag chuyển máy sang danh tính thiết bị thay cho danh tính người dùng; kiểm lại quyền truy cập tailnet và key expiry của máy sau khi gắn. `nodeAttrs.target` chọn tag/user/group, không lấy IP làm mẫu. [Quyền Funnel](https://tailscale.com/docs/features/tailscale-funnel#funnel-node-attribute), [cú pháp nodeAttrs](https://tailscale.com/docs/reference/syntax/policy-file#target).
4. Kiểm app cổng 8080 trước. Serve chỉ chia sẻ trong tailnet; Funnel công khai cho mọi người. **Lệnh Funnel dưới đây bắt đầu đưa app ra Internet**, chỉ chủ máy chạy khi đã chuẩn bị xong:

   ```bash
   tailscale version
   curl --fail http://127.0.0.1:8080/healthz
   tailscale serve --bg 8080
   tailscale serve status
   # Sau khi đã sẵn sàng public:
   tailscale funnel --bg 8080
   tailscale funnel status
   ```

   Cổng public mặc định **443**, backend **8080** chỉ ở loopback. Lệnh cuối cho URL `https://TEN-MAY.TAILNET.ts.net`; chép vào `Reminders__PublicBaseUrl` và hostname vào `AllowedHosts` trong env, rồi restart app như runbook. Lệnh Serve chạy sau Funnel sẽ đổi cổng về riêng tư. Không dùng cú pháp cũ `funnel 443 on`.
5. Xem trạng thái và tắt đúng mapping này:

   ```bash
   tailscale status
   tailscale funnel status
   tailscale serve status
   tailscale funnel --bg 8080 off
   tailscale funnel status
   # Nếu trước đó còn mapping Serve riêng, tắt nó:
   tailscale serve --bg 8080 off
   ```

   `tailscale funnel reset` xóa toàn bộ cấu hình Funnel: chỉ dùng nếu chắc không có dịch vụ khác. `--bg` giữ cấu hình qua restart Tailscale/reboot; Tailscale phải chạy, đăng nhập còn hiệu lực, Mac phải thức và app/SQL phải khỏe. Sau reboot tự kiểm trạng thái và thử từ 4G. [Tự chạy lại và tắt](https://tailscale.com/docs/reference/tailscale-cli/funnel#effects-of-rebooting-and-restarting).

Funnel chỉ có TLS trên cổng public **443/8443/10000**, dùng tên dưới `ts.net` của tailnet; không gắn tên miền tùy ý. URL giữ ổn định khi giữ tên máy và tailnet, đổi tên/xóa máy có thể đổi URL. Có giới hạn băng thông không cấu hình được; tài liệu không công bố số Mbps hay hạn mức kết nối đồng thời, nên không hứa tải không giới hạn và không đoán số. Chưa đo tải qua Funnel thật. [Giới hạn chính thức](https://tailscale.com/docs/features/tailscale-funnel#requirements-and-limitations).
