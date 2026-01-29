$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $root "artifacts"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "Building Release..."
dotnet build -c Release

if (Test-Path (Join-Path $root "src\\Xws.Core\\Xws.Core.csproj")) {
    Write-Host "Packing Xws.Core..."
    dotnet pack (Join-Path $root "src\\Xws.Core\\Xws.Core.csproj") -c Release -o $artifacts
} else {
    Write-Host "Xws.Core not found; skipping pack."
}

Write-Host "Publishing xws (win-x64)..."
dotnet publish (Join-Path $root "src\\xws\\xws.csproj") -c Release -r win-x64 --self-contained false -o (Join-Path $artifacts "win-x64")
