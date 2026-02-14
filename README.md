# xws

## Overview

xws is a raw, scriptable WebSocket pump that emits JSONL to stdout. It targets
engineers and quants who want exchange-native data with minimal processing and
deterministic CLI behavior.

## Architecture / Modules

- `Xws.Core`: receiver runtime (adapters, mux runner, JSONL formatting).
- `xws`: receiver CLI (market data only).
- `Xws.Exec`: execution library (no console IO).
- `xws.exec.cli`: thin execution CLI (Windows-safe name to avoid case collisions).

## Goals

- Emit JSONL streams suitable for piping into other tools.
- Preserve raw exchange payloads (no normalization by default).
- Support multi-exchange muxing in a single run.
- Provide deterministic stop conditions (`--max-messages`, `--timeout-seconds`).

## Non-Goals

- Normalized cross-exchange schemas.
- Trading actions inside the receiver (`xws`) or `Xws.Core`.
- Local persistence or stateful order management.

## Execution

`Xws.Exec` is a library-first execution module (separate from the receiver).
`xws.exec.cli` is a thin wrapper that emits JSONL to stdout.

Modes:

- `paper` (default, deterministic, offline)
- `testnet`
- `mainnet`

Safety gates (mainnet only):

- requires `--arm-live`
- requires `XWS_EXEC_ARM=1`
- requires `--client-order-id`

Examples (paper mode):

```
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol HYPE --side buy --type market --size 1
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol HYPE --side buy --type limit --size 1 --price 1.23
dotnet run --project src/xws.exec.cli -- amend --mode paper --exchange hl --order-id 000001 --price 1.25
dotnet run --project src/xws.exec.cli -- query orders --mode paper --exchange hl --status open
dotnet run --project src/xws.exec.cli -- query positions --mode paper --exchange hl
dotnet run --project src/xws.exec.cli -- cancel --mode paper --exchange hl --order-id 000001
dotnet run --project src/xws.exec.cli -- cancel-all --mode paper --exchange hl
```

Note: `xws.exec.cli` persists paper state to `artifacts/paper/state.json` so sequential commands can share state. Delete the file to reset the paper session.

## Quick Start: Paper Demo

Run the scripted demo:

```
./scripts/demo-paper.sh
```

PowerShell (Windows):

```
.\scripts\demo-paper.ps1
```

Manual demo flow (Hyperliquid):

```
dotnet run --project src/xws -- subscribe trades --sub hl=SOL --max-messages 5 --timeout-seconds 20
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol SOL --side buy --type limit --size 1 --price 100 --client-order-id demo-hl-001
dotnet run --project src/xws.exec.cli -- query orders --mode paper --exchange hl --status open
dotnet run --project src/xws.exec.cli -- amend --mode paper --exchange hl --order-id 000001 --price 101
dotnet run --project src/xws.exec.cli -- cancel --mode paper --exchange hl --order-id 000001
```

OKX paper order example (symbol format: `BTC-USDT-SWAP`):

```
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange okx --symbol BTC-USDT-SWAP --side buy --type limit --size 0.01 --price 50000 --client-order-id demo-okx-001
```

Bybit paper order example (symbol format: `BTCUSDT`):

```
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange bybit --symbol BTCUSDT --side buy --type limit --size 0.01 --price 50000 --client-order-id demo-bybit-001
```

Expected output (JSONL):

```
{"status":1,"orderId":"000001","clientOrderId":"demo-hl-001","mode":0}
```

## Windows Examples (Hyperliquid)

Get bid/ask levels (L2) and size:

```
dotnet run --project src/xws -- hl subscribe l2 --symbol SOL --max-messages 10 --timeout-seconds 30
```

Get OHLCV candles:

```
dotnet run --project src/xws -- hl subscribe candle --symbol SOL --interval 1m --max-messages 10 --timeout-seconds 30
```

Send a market buy:

```
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol SOL --side buy --type market --size 1 --client-order-id buy-001
```

Close a trade (reduce-only market sell):

```
dotnet run --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol SOL --side sell --type market --size 1 --reduce-only --client-order-id close-001
```

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

### hl subscribe l2

Stream L2 orderbook updates (bid/ask levels with size).

Examples (PowerShell):

```
dotnet run --project src/xws -- hl subscribe l2 --symbol SOL --max-messages 10 --timeout-seconds 30
```

### hl subscribe candle

Stream candle (OHLCV) updates.

Examples (PowerShell):

```
dotnet run --project src/xws -- hl subscribe candle --symbol SOL --interval 1m --max-messages 10 --timeout-seconds 30
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

Note: MEXC connectivity varies by region. If no envelopes are emitted, the command exits `1` (see Troubleshooting).

PowerShell (Windows):

```
dotnet run --project src/xws -- subscribe trades `
  --sub hl=SOL `
  --sub mexc.spot=BTCUSDT,ETHUSDT `
  --max-messages 50 --timeout-seconds 30

# MEXC futures trades (market key: mexc.fut, symbol format: BTC_USDT)
dotnet run --project src/xws -- subscribe trades `
  --sub mexc.fut=BTC_USDT `
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

PowerShell (Windows):

```
# MEXC futures L2 (market key: mexc.fut, symbol format: BTC_USDT)
dotnet run --project src/xws -- subscribe l2 `
  --sub mexc.fut=BTC_USDT `
  --max-messages 5 --timeout-seconds 15
```

### mux subscribe funding / liquidations / markprice / fills

Examples:

```
dotnet run --project src/xws -- subscribe funding \
  --sub hl=SOL \
  --sub mexc.fut=BTC_USDT \
  --sub okx.fut=BTC-USDT-SWAP \
  --max-messages 5 --timeout-seconds 20

# HL liquidations requires XWS_HL_USER
dotnet run --project src/xws -- subscribe liquidations \
  --sub hl=SOL \
  --sub okx.fut=BTC-USDT-SWAP \
  --max-messages 5 --timeout-seconds 20

dotnet run --project src/xws -- subscribe markprice \
  --sub hl=SOL \
  --sub mexc.fut=BTC_USDT \
  --sub bybit.fut=BTCUSDT \
  --max-messages 5 --timeout-seconds 20

# HL fills requires XWS_HL_USER
dotnet run --project src/xws -- subscribe fills \
  --sub hl=SOL \
  --max-messages 5 --timeout-seconds 20
```

PowerShell (Windows):

```
dotnet run --project src/xws -- subscribe funding `
  --sub hl=SOL `
  --sub mexc.fut=BTC_USDT `
  --sub okx.fut=BTC-USDT-SWAP `
  --max-messages 5 --timeout-seconds 20

# HL liquidations requires XWS_HL_USER
dotnet run --project src/xws -- subscribe liquidations `
  --sub hl=SOL `
  --sub okx.fut=BTC-USDT-SWAP `
  --max-messages 5 --timeout-seconds 20

dotnet run --project src/xws -- subscribe markprice `
  --sub hl=SOL `
  --sub mexc.fut=BTC_USDT `
  --sub bybit.fut=BTCUSDT `
  --max-messages 5 --timeout-seconds 20

# HL fills requires XWS_HL_USER
dotnet run --project src/xws -- subscribe fills `
  --sub hl=SOL `
  --max-messages 5 --timeout-seconds 20
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
- XWS_HL_USER (required for private positions and fills subscriptions)
- XWS_HL_WS_URL (optional override)
- XWS_HL_HTTP_URL (optional override)
- XWS_MEXC_SPOT_WS_URL (optional override)
- XWS_MEXC_FUT_WS_URL (optional override for futures WS)
- XWS_OKX_WS_URL (optional override for OKX public WS)
- XWS_BYBIT_SPOT_WS_URL (optional override for Bybit spot WS)
- XWS_BYBIT_FUT_WS_URL (optional override for Bybit futures WS)
- XWS_EXEC_ARM (required for mainnet execution; must be exactly `1`)

Execution credentials:

- `xws.exec.cli` does not define env var names for Hyperliquid credentials yet.
  Hosts can pass credentials directly to `Xws.Exec` via `ExecutionConfig`.

## Connectivity Note (MEXC)

In some regions, MEXC WebSocket access may be blocked. If MEXC cannot connect,
the mux continues with other sources and exits 0 once a stop condition is met
and at least one envelope line was emitted.

## Troubleshooting

- MEXC region blocking: Some regions block MEXC WS. Expect connection failures; use `dev emit` for offline validation or run mux with at least one non-MEXC source.
- Dotenv load errors: `--dotenv <path>` requires the file to exist. Use `--no-dotenv` to disable loading or ensure the file is present. Default `.env` is optional.
- Exit codes: `0` success, `1` user/input/config error, `2` system/runtime error (unexpected failure).
- Common CLI errors:
  - `--timeout-seconds requires --max-messages`: set both to enforce deterministic stop.
  - `missing required env var: XWS_HL_USER`: required for `hl subscribe positions`, `subscribe liquidations`, and `subscribe fills`.
  - `mux only supports --format envelope`: mux does not support raw output.
  - `mux completed without output` or `mux timeout reached`: no envelopes were emitted; exit code will be `1`.

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
