#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
app_running || fail 'App không chạy.'
echo "PID: $APP_PID"
health
echo
