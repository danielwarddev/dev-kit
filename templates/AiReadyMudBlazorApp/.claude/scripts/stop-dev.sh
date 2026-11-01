#!/usr/bin/env bash
set -euo pipefail

app_exe="AiReadyMudBlazorApp.exe"

pid=$(tasklist //FO CSV //NH 2>/dev/null | grep -i "$app_exe" | cut -d'"' -f4 | head -1) || true
if [ -n "$pid" ]; then
    echo "Killing $app_exe (PID $pid)..."
    taskkill //PID "$pid" //F
    sleep 1
else
    echo "No running $app_exe found."
fi
