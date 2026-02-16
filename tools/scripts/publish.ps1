$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $root "artifacts"
$publish = Join-Path $artifacts "publish"
$win = Join-Path $publish "win-x64"
$linux = Join-Path $publish "linux-x64"

New-Item -ItemType Directory -Force -Path $publish | Out-Null

Write-Host "Publishing xws (win-x64)..."
dotnet publish (Join-Path $root "src\\xws\\xws.csproj") -c Release -r win-x64 --self-contained false -o $win

Write-Host "Publishing xws (linux-x64)..."
dotnet publish (Join-Path $root "src\\xws\\xws.csproj") -c Release -r linux-x64 --self-contained false -o $linux
