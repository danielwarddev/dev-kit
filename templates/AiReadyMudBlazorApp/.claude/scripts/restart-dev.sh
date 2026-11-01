#!/usr/bin/env bash
set -euo pipefail

app_exe="AiReadyMudBlazorApp.exe"

# Kill any running app process
pid=$(tasklist //FO CSV //NH 2>/dev/null | grep -i "$app_exe" | cut -d'"' -f4 | head -1) || true
if [ -n "$pid" ]; then
    echo "Killing $app_exe (PID $pid)..."
    taskkill //PID "$pid" //F
    sleep 1
fi

# Build
echo "Building..."
cd C:/repos/AiReadyMudBlazorApp
dotnet build AiReadyMudblazorApp.slnx

# Start the dev server in the background
echo "Starting dev server..."
mkdir -p .claude/app-logs
dotnet run --project AiReadyMudBlazorApp --launch-profile http > .claude/app-logs/server.log 2>&1 &
echo "Dev server started (PID $!). Logs: .claude/app-logs/server.log"
