#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
if app_running; then health; exit 0; fi
rm -f "$FUJI_RUN/maintenance"
nohup "$SCRIPT_DIR/run.sh" </dev/null >> "$FUJI_LOG/launcher.log" 2>&1 &
if wait_health; then echo 'App đã chạy, /healthz xanh.'; else "$SCRIPT_DIR/stop.sh"; fail 'Khởi động thất bại; xem logs/app.log và launcher.log.'; fi
