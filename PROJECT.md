# xws — Exchange WebSocket CLI (Milestone 1)

## One-sentence intent
A **.NET 8 C# CLI** that connects to an exchange WebSocket (starting with **Hyperliquid**) and streams **raw JSON messages** to **stdout as JSONL**, with **robust reconnect** and **env-var gated private streams**.

---

## Target user + primary use case
Engineers/quants who want a **scriptable, pipe-friendly WebSocket pump** to feed other tools (`jq`, Python, log processors) without schema normalization yet.

---

## What Milestone 1 includes (IN)
### CLI + I/O contract
- **stdout:** WebSocket messages only, **one JSON object per line (JSONL)**
- **stderr:** logs, status, errors only

### Hyperliquid commands
- `xws hl symbols [--filter <text>]`
  - Lists valid instruments/symbols as **raw JSONL** (discovery; no symbol mapping).
- `xws hl subscribe trades --symbol <native_symbol> [--max-messages N] [--timeout-seconds T]`
  - Connects to HL WS, subscribes, streams WS frames as JSONL.
- `xws hl subscribe positions [--max-messages N] [--timeout-seconds T]`
  - Private proof: subscribes to user/account positions stream when env vars present.

### Reliability + lifecycle
- **Reconnect with exponential backoff**
- **Auto re-subscribe** after reconnect (idempotent subscription registry)
- **Retry cap:** after **3 failed reconnect attempts**, exits **non-zero** with a clear stderr message
- **Ctrl+C:** clean shutdown, exits **0**

### Platform target
- **Windows primary**
- Designed to be **Linux-portable** (CI checks Ubuntu build)

---

## What Milestone 1 does NOT include (OUT)
- No normalized schema / cross-exchange translation layer
- No trading actions (placing/canceling orders), no signing flows
- No local order state, no portfolio tracking beyond streaming
- No persistence (DB/files)
- No TUI/fancy UI
- No symbol translation (no `BTC/USDT` mapping); **exchange-native symbols only**

---

## Tech stack
- **Language/runtime:** C# / .NET 8
- **CLI parsing:** `System.CommandLine` (minimal deps)
- **WS client:** `ClientWebSocket` (built-in)
- **HTTP:** `HttpClient` (for `/info` discovery)

---

## Environment variables (Hyperliquid)
Secrets/identity are **env-var only**; nothing is written to disk.

- `XWS_HL_NETWORK` (optional) — `mainnet` | `testnet` (default: `mainnet`)
- `XWS_HL_USER` (required for private) — user address (used for positions/account subscription)
- `XWS_HL_WS_URL` (optional) — override WS endpoint
- `XWS_HL_HTTP_URL` (optional) — override HTTP endpoint

---

## How to run (dev)
From repo root:

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/xws -- --help

---

## Milestone 2 (Draft)
- Add MEXC spot trades adapter (protobuf parsing) with futures scaffolding.
- Introduce mux command to run HL + MEXC concurrently with envelope JSONL output.
- Preserve legacy raw output via `--format raw` on existing HL commands.

## Milestone 2 (Complete)
- MEXC spot trades adapter with protobuf decoding and offline test.
- Mux command runs HL + MEXC concurrently with envelope JSONL default.
- Best-effort mux behavior documented for partial network access.
