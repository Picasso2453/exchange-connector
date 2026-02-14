$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")

$stateDir = Join-Path $rootDir "artifacts\\paper"
$stateFile = Join-Path $stateDir "state.json"
$tradesFile = Join-Path $stateDir "trades.jsonl"

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
Remove-Item -Path $stateFile -ErrorAction SilentlyContinue

$tradesProcess = Start-Process dotnet -NoNewWindow -PassThru `
    -ArgumentList @("run","--project","$rootDir\\src\\xws","--","hl","subscribe","trades","--symbol","SOL","--max-messages","2","--timeout-seconds","30") `
    -RedirectStandardOutput $tradesFile `
    -RedirectStandardError (Join-Path $stateDir "trades.err")

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- place `
    --mode paper --exchange hl --symbol SOL `
    --side buy --type limit --size 1 --price 100 --client-order-id demo-hl-001

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- query orders `
    --mode paper --exchange hl --status open

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- amend `
    --mode paper --exchange hl --order-id 000001 --price 101

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- cancel `
    --mode paper --exchange hl --order-id 000001

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- place `
    --mode paper --exchange hl --symbol SOL `
    --side sell --type market --size 1 --client-order-id demo-hl-002

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- query positions `
    --mode paper --exchange hl

dotnet run --project "$rootDir\\src\\xws.exec.cli" -- cancel-all `
    --mode paper --exchange hl

Wait-Process -Id $tradesProcess.Id -ErrorAction SilentlyContinue

Write-Host "demo complete: $tradesFile"
