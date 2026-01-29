# xws

## Overview

## Goals

## Non-Goals

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
```

### hl subscribe positions

Stream private positions/account feed as raw JSONL on stdout. Requires env vars.

Examples:

```
$env:XWS_HL_USER="0xYourAddressHere"
dotnet run --project src/xws -- hl subscribe positions --max-messages 10 --timeout-seconds 30
```

## IO Contract

- stdout: WebSocket messages only, one JSON object per line (JSONL)
- stderr: logs/status/errors only

## Env Vars

- XWS_HL_NETWORK (optional, default: mainnet; values: mainnet|testnet)
- XWS_HL_USER (required for private positions subscription)
- XWS_HL_WS_URL (optional override)
- XWS_HL_HTTP_URL (optional override)

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
