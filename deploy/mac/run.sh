#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
# LaunchAgent chạy foreground để KeepAlive giám sát đúng PID dotnet.
[[ ! -f "$FUJI_RUN/maintenance" ]] || { sleep 10; exit 0; }
mkdir "$FUJI_RUN/start.lock" 2>/dev/null || fail 'Có lượt khởi động khác đang chạy.'
trap 'rmdir "$FUJI_RUN/start.lock" 2>/dev/null || true' EXIT
if app_running; then fail 'App đã chạy.'; fi
[[ -f "$FUJI_APP/HomestayBooking.Web.dll" ]] || fail 'Chưa publish app.'
# Không chiếm cổng của tiến trình không thuộc phiên này.
if lsof -nP -iTCP:8080 -sTCP:LISTEN >/dev/null 2>&1; then fail 'Cổng 8080 đang được dùng.'; fi
echo "$$" > "$FUJI_RUN/app.pid"
cd "$FUJI_APP"
rmdir "$FUJI_RUN/start.lock"
trap - EXIT
exec "$DOTNET_BIN" "$FUJI_APP/HomestayBooking.Web.dll" >> "$FUJI_LOG/app.log" 2>&1
