# Fix Log

## Blocking
- None.

## Non-Blocking
- Linux restore warnings during `dotnet test` (ResponseEnded when downloading `System.Runtime.4.3.0` and `Grpc.Tools.2.60.0`), but tests completed successfully.
- PowerShell pipeline stop behavior: `dotnet run --project src/xws -- hl symbols | Select-Object -First 1` returns exit `-1` because `Select-Object -First` terminates the pipeline early. xws exits 0; this is expected PowerShell behavior.
