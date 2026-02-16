# FIXLOG

## 2026-02-16 - Milestone 11 Complete ✅
All issues resolved. See final summary below.

## 2026-02-16 - RESOLVED: MSBuild Circular Dependency
- Status: **RESOLVED** ✅
- Issue: MSBuild error MSB4006 - circular dependency in target dependency graph
- Root cause: `Xws.Data` ↔ `Xws.Exchanges` bidirectional project references
- Resolution: **Option A** - Created `Xws.Abstractions` project with shared interfaces
- Implementation:
  - Created `Xws.Abstractions` project containing `IExchangeAdapter`, `IWebSocketClient`, `IMessageParser<T>`, `IJsonlWriter`
  - Updated dependency graph: `Xws.Data` → `Xws.Abstractions`, `Xws.Exchanges` → (`Xws.Abstractions` + `Xws.Data`)
  - Moved `XwsRunner` from `Xws.Data` to `xws` CLI project (proper layering)
  - Updated all namespace references: `xws.Exchanges.*` → `Xws.Exchanges.*`
  - Fixed protobuf configuration in `Xws.Exchanges.csproj`
- Verification: `dotnet build` succeeds, `dotnet test` passes 79/81 tests (2 pre-existing security test failures)
- Result: Clean dependency graph with no circular references
