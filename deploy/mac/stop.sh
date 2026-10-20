#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
# Ngăn LaunchAgent khởi động lại trong khi bảo trì. start.sh xóa cờ này.
touch "$FUJI_RUN/maintenance"
if app_running; then
  kill -TERM "$APP_PID"
  for n in {1..30}; do if ! kill -0 "$APP_PID" 2>/dev/null; then break; fi; sleep 1; done
  if app_running; then fail 'App chưa dừng sau 30 giây; giữ PID để kiểm tra, không ép kill.'; fi
fi
rm -f "$FUJI_RUN/app.pid"
echo 'App đã dừng.'
