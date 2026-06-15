#!/usr/bin/env bash
# PostToolUse hook — triggered after file writes by the agents.
# Formats the C# code produced in output/src/ with `dotnet format`.
# Silent and non-blocking: if dotnet or the solution are absent, it does not fail.

set -uo pipefail

SRC_DIR="output/src"

# Do nothing if there is no source folder yet
[ -d "$SRC_DIR" ] || exit 0

# Do nothing if dotnet is not installed
command -v dotnet >/dev/null 2>&1 || exit 0

# Find a solution or project to format
TARGET=$(find "$SRC_DIR" -maxdepth 2 -name "*.sln" | head -n1)
if [ -z "$TARGET" ]; then
  TARGET=$(find "$SRC_DIR" -maxdepth 3 -name "*.csproj" | head -n1)
fi
[ -n "$TARGET" ] || exit 0

# Format (whitespace + style). Timeout so it never blocks the pipeline.
timeout 90 dotnet format "$TARGET" --verbosity quiet >/dev/null 2>&1 || true

echo "[hook] dotnet format applied on $TARGET" >&2
exit 0
