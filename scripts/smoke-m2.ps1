$ErrorActionPreference = "Stop"

$rawOut = Join-Path $env:TEMP ("xws_raw_" + [guid]::NewGuid().ToString() + ".txt")
$muxOut = Join-Path $env:TEMP ("xws_mux_" + [guid]::NewGuid().ToString() + ".txt")
$allowMexcFail = $env:XWS_MEXC_ALLOW_FAIL -eq "1"

try {
    dotnet run --project src/xws -- hl subscribe trades --symbol SOL --format raw --max-messages 3 --timeout-seconds 30 > $rawOut
    if ($LASTEXITCODE -ne 0) { throw "hl raw exited with $LASTEXITCODE" }
    if ((Get-Item $rawOut).Length -le 0) { throw "hl raw produced no output" }

    dotnet run --project src/xws -- subscribe trades --sub hl=SOL --max-messages 3 --timeout-seconds 30 > $muxOut
    if ($LASTEXITCODE -ne 0) { throw "mux hl exited with $LASTEXITCODE" }
    if ((Get-Item $muxOut).Length -le 0) { throw "mux produced no output" }

    dotnet run --project src/xws -- mexc spot subscribe trades --symbol BTCUSDT --max-messages 2 --timeout-seconds 5 > $muxOut
    if ($LASTEXITCODE -ne 0 -and -not $allowMexcFail) { throw "mexc spot exited with $LASTEXITCODE" }

    dotnet run --project src/xws -- subscribe trades --sub hl=SOL --sub mexc.spot=BTCUSDT --max-messages 3 --timeout-seconds 30 > $muxOut
    if ($LASTEXITCODE -ne 0 -and -not $allowMexcFail) { throw "mux hl+mexc exited with $LASTEXITCODE" }
    if ((Get-Item $muxOut).Length -le 0) { throw "mux hl+mexc produced no output" }
}
finally {
    if (Test-Path $rawOut) { Remove-Item $rawOut -Force }
    if (Test-Path $muxOut) { Remove-Item $muxOut -Force }
}

Write-Host "smoke ok"
