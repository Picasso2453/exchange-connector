#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
artifacts="$root/artifacts"
publish="$artifacts/publish"
win="$publish/win-x64"
linux="$publish/linux-x64"

mkdir -p "$publish"

echo "Publishing xws (win-x64)..."
dotnet publish "$root/src/xws/xws.csproj" -c Release -r win-x64 --self-contained false -o "$win"

echo "Publishing xws (linux-x64)..."
dotnet publish "$root/src/xws/xws.csproj" -c Release -r linux-x64 --self-contained false -o "$linux"
