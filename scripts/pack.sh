#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
artifacts="$root/artifacts"
mkdir -p "$artifacts"

echo "Building Release..."
dotnet build -c Release

if [ -f "$root/src/Xws.Core/Xws.Core.csproj" ]; then
  echo "Packing Xws.Core..."
  dotnet pack "$root/src/Xws.Core/Xws.Core.csproj" -c Release -o "$artifacts"
else
  echo "Xws.Core not found; skipping pack."
fi

echo "Publishing xws (linux-x64)..."
dotnet publish "$root/src/xws/xws.csproj" -c Release -r linux-x64 --self-contained false -o "$artifacts/linux-x64"
