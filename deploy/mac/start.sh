#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
if app_running; then health; exit 0; fi
rm -f "$FUJI_RUN/maintenance"
agent="gui/$(id -u)/com.fuji.homestay"
# LaunchAgent (KeepAlive) đang nạp thì để launchd chạy run.sh; tự chạy nohup sẽ khiến bản của launchd lặp lỗi "App đã chạy"
# và app mất giám sát. -k bỏ lượt run.sh đang chờ cờ bảo trì để khởi động ngay.
if launchctl print "$agent" >/dev/null 2>&1; then
  launchctl kickstart -k "$agent"
else
  nohup "$SCRIPT_DIR/run.sh" </dev/null >> "$FUJI_LOG/launcher.log" 2>&1 &
fi
if wait_health; then echo 'App đã chạy, /healthz xanh.'; else "$SCRIPT_DIR/stop.sh"; fail 'Khởi động thất bại; xem logs/app.log và launcher.log.'; fi
