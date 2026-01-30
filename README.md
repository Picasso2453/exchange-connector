# xws

## Overview

xws is a raw, scriptable WebSocket pump that emits JSONL to stdout. It targets
engineers and quants who want exchange-native data with minimal processing and
deterministic CLI behavior.

## Goals

- Emit JSONL streams suitable for piping into other tools.
- Preserve raw exchange payloads (no normalization by default).
- Support multi-exchange muxing in a single run.
- Provide deterministic stop conditions (`--max-messages`, `--timeout-seconds`).

## Non-Goals

- Normalized cross-exchange schemas.
- Trading actions (place/cancel), signing, or key management.
- Local persistence or stateful order management.

## Commands

### hl symbols

List exchange-native symbols/instruments as raw JSONL (one JSON object per line).

Examples:

```
dotnet run --project src/xws -- hl symbols
dotnet run --project src/xws -- hl symbols --filter BTC
```

### hl subscribe trades

Stream trades for a native coin symbol as raw JSONL on stdout.

Examples:

```
dotnet run --project src/xws -- hl subscribe trades --symbol SOL
dotnet run --project src/xws -- hl subscribe trades --symbol SOL --max-messages 50 --timeout-seconds 30
dotnet run --project src/xws -- hl subscribe trades --symbol SOL --format raw --max-messages 10 --timeout-seconds 30
```

### hl subscribe positions

Stream private positions/account feed as raw JSONL on stdout. Requires env vars.

Examples:

```
$env:XWS_HL_USER="0xYourAddressHere"
dotnet run --project src/xws -- hl subscribe positions --max-messages 10 --timeout-seconds 30
```

### mux subscribe trades (envelope default)

Run multiple exchanges in one command and emit envelope JSONL.

Examples:

```
dotnet run --project src/xws -- subscribe trades \
  --sub hl=SOL \
  --sub mexc.spot=BTCUSDT,ETHUSDT \
  --max-messages 50 --timeout-seconds 30

# MEXC futures trades (market key: mexc.fut, symbol format: BTC_USDT)
dotnet run --project src/xws -- subscribe trades \
  --sub mexc.fut=BTC_USDT \
  --max-messages 10 --timeout-seconds 15
```

### mux subscribe l2 (envelope default)

Run L2 orderbook streams via mux and emit envelope JSONL.

Examples:

```
# MEXC futures L2 (market key: mexc.fut, symbol format: BTC_USDT)
dotnet run --project src/xws -- subscribe l2 \
  --sub mexc.fut=BTC_USDT \
  --max-messages 5 --timeout-seconds 15
```

## IO Contract

- stdout: WebSocket messages only, one JSON object per line (JSONL)
- stderr: logs/status/errors only

### Envelope format

Mux output defaults to an envelope JSONL format. Legacy HL commands support
`--format raw` to preserve the M1 raw frame contract.

## Library usage (Xws.Core)

Xws.Core is the reusable runtime: adapters, mux runner, and JSONL formatting.
You can reference it via project reference (today) or NuGet (once published).

Project reference:

```
dotnet add <your-project> reference src/Xws.Core/Xws.Core.csproj
```

## CLI usage

stdout is data only (JSONL). stderr is logs/errors. For offline validation, use
the deterministic dev emitter:

```
dotnet run --project src/xws -- dev emit --count 5 --timeout-seconds 5
```

### .env loading (CLI only)

- Default: loads `./.env` if it exists (no error if missing).
- `--dotenv <path>`: load explicit file; missing file exits non-zero and logs to stderr.
- `--no-dotenv`: disables dotenv loading entirely.
- Precedence: process env vars win; dotenv fills missing values only.
- Output contract: dotenv notices go to stderr; stdout remains JSONL-only.

## Env Vars

- XWS_HL_NETWORK (optional, default: mainnet; values: mainnet|testnet)
- XWS_HL_USER (required for private positions subscription)
- XWS_HL_WS_URL (optional override)
- XWS_HL_HTTP_URL (optional override)
- XWS_MEXC_SPOT_WS_URL (optional override)
- XWS_MEXC_FUT_WS_URL (optional override; scaffold only)

## Connectivity Note (MEXC)

In some regions, MEXC WebSocket access may be blocked. If MEXC cannot connect,
the mux continues with other sources and exits 0 once a stop condition is met
and at least one envelope line was emitted.

## Configuration

## Build

```
dotnet restore
dotnet build -c Release
```

## Run

```
dotnet run --project src/xws -- --help
```

## Test

```
dotnet test -c Release
```

## Release

### Publish (Windows)

```
dotnet publish src/xws -c Release -r win-x64 --self-contained false -o out/win-x64
.\out\win-x64\xws.exe --help
```

### Publish (Linux)

```
dotnet publish src/xws -c Release -r linux-x64 --self-contained false -o out/linux-x64
./out/linux-x64/xws --help
```

## Compatibility

Validated by CI on Windows and Ubuntu. The CLI should also run on Linux without changes.

## Artifacts

Local packaging outputs go to `artifacts/` (ignored by git).

Pack Xws.Core to NuGet:

```
scripts/pack.ps1
# or
./scripts/pack.sh
```

Note: on Windows, the `.sh` scripts require WSL. Use the `.ps1` scripts if WSL
is not installed.

Outputs:

- `artifacts/nuget/` for `Xws.Core.*.nupkg`

Publish CLI:

```
scripts/publish.ps1
# or
./scripts/publish.sh
```

Outputs:

- `artifacts/publish/win-x64`
- `artifacts/publish/linux-x64`

## CI (Offline-Safe)

CI runs build/test on Windows + Linux without hitting live endpoints. A manual,
allow-fail smoke job exists for live checks.

## Milestone 1

- Raw JSONL streaming CLI with Hyperliquid adapter.
- Reconnect/backoff with retry cap = 3 and resubscribe.
- Discovery via `hl symbols`.
