# Fix Log

## Blocking
- None.

## Non-Blocking
- Linux restore warnings during `dotnet test` (ResponseEnded when downloading `System.Runtime.4.3.0` and `Grpc.Tools.2.60.0`), but tests completed successfully.
- PowerShell pipeline stop behavior: `dotnet run --project src/xws -- hl symbols | Select-Object -First 1` returns exit `-1` because `Select-Object -First` terminates the pipeline early. xws exits 0; this is expected PowerShell behavior.
- OKX mux L2 example timed out during Windows README validation (`dotnet run --project src/xws -- subscribe l2 --sub okx.fut=BTC-USDT-SWAP --max-messages 2 --timeout-seconds 20`); no envelopes emitted before timeout.
