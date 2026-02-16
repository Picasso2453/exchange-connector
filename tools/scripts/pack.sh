#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
artifacts="$root/artifacts"
nuget="$artifacts/nuget"
mkdir -p "$artifacts"
mkdir -p "$nuget"

echo "Building Release..."
dotnet build -c Release

if [ -f "$root/src/Xws.Core/Xws.Core.csproj" ]; then
  echo "Packing Xws.Core..."
  dotnet pack "$root/src/Xws.Core/Xws.Core.csproj" -c Release -o "$nuget"
else
  echo "Xws.Core not found; skipping pack."
fi
