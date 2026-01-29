# Milestone 1 Handover (v0.1.0)

## Status

Milestone 1 is closed and tagged as `v0.1.0`.

## What Shipped

- .NET 8 CLI (`xws`) with Hyperliquid adapter.
- Raw JSONL output to stdout; logs/errors only to stderr.
- Public trades stream: `hl subscribe trades --symbol <coin>`.
- Private positions/account stream: `hl subscribe positions` (env-gated).
- Symbols discovery: `hl symbols` via `/info` (meta + spotMeta).
- Reconnect with exponential backoff and retry cap = 3; resubscribe on reconnect.
- Ctrl+C clean shutdown (exit code 0).
- Deterministic validation flags: `--max-messages`, `--timeout-seconds`.
- CI on Windows + Ubuntu (GitHub Actions).
- Usage + publish docs updated in README.

## How To Run (Quick)

```
dotnet build -c Release
dotnet run --project src/xws -- --help
dotnet run --project src/xws -- hl symbols
dotnet run --project src/xws -- hl subscribe trades --symbol SOL --max-messages 50 --timeout-seconds 30
```

Private positions (env required):

```
$env:XWS_HL_USER="0xYourAddressHere"
dotnet run --project src/xws -- hl subscribe positions --max-messages 10 --timeout-seconds 30
```

## Env Vars

- XWS_HL_NETWORK (optional, default: mainnet; values: mainnet|testnet)
- XWS_HL_USER (required for private positions subscription)
- XWS_HL_WS_URL (optional override)
- XWS_HL_HTTP_URL (optional override)

## Repo Map

- CLI entry: `src/xws/Program.cs`
- WS runner + backoff: `src/xws/Core/WebSocket/WebSocketRunner.cs`
- JSONL writer: `src/xws/Core/Output/JsonlWriter.cs`
- Subscriptions registry: `src/xws/Core/Subscriptions/*`
- HL config + HTTP/WS helpers: `src/xws/Exchanges/Hyperliquid/*`
- Env reader: `src/xws/Core/Env/EnvReader.cs`
- CI workflow: `.github/workflows/ci.yml`
- Docs/decisions: `README.md`, `DECISIONS.md`

## Known Limits / Non-Goals (M1)

- No normalized schema or cross-exchange mapping.
- No persistence, no TUI, no trading actions.
- Private stream uses user address only (no API keys).

## Validation Checklist (last run)

- `dotnet build -c Release`
- `dotnet run --project src/xws -- --help`
- `dotnet publish src/xws -c Release -r win-x64 --self-contained false -o out/win-x64`
- `.\out\win-x64\xws.exe --help`

## Next Milestone (Suggested)

- Multi-exchange adapter layer and normalization.
- More HL subscriptions + symbol filtering improvements.
- CI extensions (packaging artifacts, release notes).
