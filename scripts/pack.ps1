$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $root "artifacts"
$nuget = Join-Path $artifacts "nuget"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
New-Item -ItemType Directory -Force -Path $nuget | Out-Null

Write-Host "Building Release..."
dotnet build -c Release

if (Test-Path (Join-Path $root "src\\Xws.Core\\Xws.Core.csproj")) {
    Write-Host "Packing Xws.Core..."
    dotnet pack (Join-Path $root "src\\Xws.Core\\Xws.Core.csproj") -c Release -o $nuget
} else {
    Write-Host "Xws.Core not found; skipping pack."
}
