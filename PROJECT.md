# xws - Exchange WebSocket CLI

## One-sentence intent
A .NET 8 C# CLI that connects to exchange WebSockets (starting with Hyperliquid) and streams raw JSON messages to stdout as JSONL, with robust reconnect and env-var gated private streams.

---

## Target user + primary use case
Engineers/quants who want a scriptable, pipe-friendly WebSocket pump to feed other tools (jq, Python, log processors) without schema normalization yet.

---

## Project scope (stable contract)
### CLI + I/O contract
- stdout: WebSocket messages only, one JSON object per line (JSONL)
- stderr: logs, status, errors only

### Reliability + lifecycle
- Reconnect with exponential backoff
- Auto re-subscribe after reconnect (idempotent subscription registry)
- Retry cap: after 3 failed reconnect attempts, exits non-zero with a clear stderr message
- Ctrl+C: clean shutdown, exits 0

### Platform target
- Windows primary
- Linux supported (CI validates on Ubuntu)

---

## Tech stack
- Language/runtime: C# / .NET 8
- CLI parsing: System.CommandLine (minimal deps)
- WS client: ClientWebSocket (built-in)
- HTTP: HttpClient (for /info discovery)

---

## Environment variables (Hyperliquid)
Secrets/identity are env-var only; nothing is written to disk.

- XWS_HL_NETWORK (optional) - mainnet | testnet (default: mainnet)
- XWS_HL_USER (required for private) - user address (positions/fills/liquidations subscription)
- XWS_HL_WS_URL (optional) - override WS endpoint
- XWS_HL_HTTP_URL (optional) - override HTTP endpoint

## Environment variables (Other exchanges)
- XWS_MEXC_SPOT_WS_URL (optional) - override MEXC spot WS
- XWS_MEXC_FUT_WS_URL (optional) - override MEXC futures WS
- XWS_OKX_WS_URL (optional) - override OKX public WS
- XWS_BYBIT_SPOT_WS_URL (optional) - override Bybit spot WS
- XWS_BYBIT_FUT_WS_URL (optional) - override Bybit futures WS

---

## How to run (dev)
From repo root:

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/xws -- --help
```

---

## Milestones (M1-M8 complete)

## Milestone 1 - Hyperliquid receiver baseline
- HL symbols discovery command (raw JSONL)
- HL subscribe trades + positions (private gated by env vars)
- Reconnect/backoff with resubscribe and retry cap
- JSONL stdout contract with logs to stderr

## Milestone 2 - MEXC spot + mux envelope
- MEXC spot trades adapter (protobuf decoding) with offline tests
- Mux command to run HL + MEXC concurrently
- Envelope JSONL default for mux; legacy HL supports --format raw
- Best-effort mux behavior for partial connectivity

## Milestone 3 - Core/CLI split + offline CI
- Xws.Core library split from CLI
- Single stdout writer in CLI; Core never writes to console
- Offline CI build/test without live exchange endpoints

## Milestone 4 - MEXC futures + L2 via mux
- MEXC futures trades supported via mux (market key: mexc.fut)
- MEXC futures L2 orderbook stream via mux
- Envelope JSONL assertions and fixture replay harness

## Milestone 5 - dotenv loading and flags
- CLI loads .env if present (no error if missing)
- --dotenv <path> requires file to exist (non-zero if missing)
- --no-dotenv disables dotenv loading
- Process env vars take precedence; dotenv fills missing values only

## Milestone 6 - Execution module
- Execution split: Xws.Exec library + xws.exec.cli wrapper
- Modes: paper default; testnet and mainnet supported in library
- Mainnet arming rule: requires --arm-live and XWS_EXEC_ARM=1
- Idempotency guard: clientOrderId required for mainnet

## Milestone 7 - Hardening & QA pass
- Documentation completed (PROJECT/README/PROMPTS/AGENTS/CHECKLIST)
- Cross-platform validation on Windows + Linux
- Error handling and exit code consistency verified
- Version bumped to v0.7.0

## Milestone 8 - Paper demo readiness
- Added OKX + Bybit receiver adapters (trades, L2, funding, liquidations, mark price)
- Expanded Hyperliquid + MEXC market data (funding, liquidations, mark price, fills)
- Execution CLI expanded: amend + query orders/positions + multi-exchange flag
- Paper demo scripts + Quick Start documentation

## Milestone 9 - Optimization & Hardening
- Refactored exchange adapters into structured modules (WebSocket/Rest/Shared)
- CLI command wiring extracted to command modules (xws + xws.exec.cli)
- WebSocket parsing allocation reductions (pooled buffers, decoding optimizations)
- Rate limiting scaffolding in exec layer with env var overrides
- Connection health monitoring (stale detection, ping/pong on OKX/Bybit)
- Added ARCHITECTURE.md and OPERATIONS.md
