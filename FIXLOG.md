# Fix Log

## Blocking
- None.

## Non-Blocking
- Linux restore warnings during `dotnet test` (ResponseEnded when downloading `System.Runtime.4.3.0` and `Grpc.Tools.2.60.0`), but tests completed successfully.
- PowerShell pipeline stop behavior: `dotnet run --project src/xws -- hl symbols | Select-Object -First 1` returns exit `-1` because `Select-Object -First` terminates the pipeline early. xws exits 0; this is expected PowerShell behavior.
- OKX mux L2 example timed out during Windows README validation (`dotnet run --project src/xws -- subscribe l2 --sub okx.fut=BTC-USDT-SWAP --max-messages 2 --timeout-seconds 20`); no envelopes emitted before timeout.

## 2026-02-16
- Status: Blocking
- Issue: `Xws.Exchanges` build fails after moving adapters.
- Root cause: exchange adapters depend on `xws.Core.*` types; `Xws.Exchanges` now depends on `Xws.Core`, but `Xws.Core` still references `xws.Exchanges.*`, creating a circular dependency and missing namespace errors.
- Resolution: Pending (requires dependency direction decision or shared types relocation).
- Verification command: `dotnet build src\Xws.Exchanges\Xws.Exchanges.csproj`
- Result: Failed (CS0234/CS0246).

## 2026-02-16
- Status: Blocking
- Issue: Unable to add wiki as submodule.
- Root cause: GitHub wiki repo not found at `https://github.com/Picasso2453/exchange-connector.wiki.git`.
- Resolution: Pending (need correct wiki repo URL or wiki enabled).
- Verification command: `git submodule add https://github.com/Picasso2453/exchange-connector.wiki.git wiki`
- Result: Failed (repository not found).

## 2026-02-16 - RESOLVED: MSBuild Circular Dependency
- Status: **RESOLVED** ✅
- Issue: MSBuild error MSB4006 - circular dependency between `Xws.Core` and `Xws.Exchanges`
- Root cause: Bidirectional project references not allowed in .NET MSBuild
- Resolution: **Option A** - Created `Xws.Abstractions` project
- Implementation summary:
  - Created `Xws.Abstractions` with 4 core interfaces: `IExchangeAdapter`, `IWebSocketClient`, `IMessageParser<T>`, `IJsonlWriter`
  - Established clean dependency graph: `Xws.Abstractions` ← `Xws.Core` ← `Xws.Exchanges`
  - Moved `XwsRunner` from `Xws.Core` to `xws` CLI (architectural improvement)
  - Updated all namespace references throughout solution
  - Fixed protobuf configuration for MEXC protos
- Verification: `dotnet build` succeeds, 79/81 tests pass (2 pre-existing logger redaction test failures)
- Result: Clean architecture with modular separation of concerns
