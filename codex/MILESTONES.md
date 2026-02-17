# MILESTONES.md

## Milestone 1 — Hyperliquid core (MVP)
**Status**: Complete

Core framework + unified contracts + WS/REST managers + HL adapter; CLI streams JSONL.

### Acceptance Contract
- [x] All TESTS.md checks pass (41 tests, 0 failures)
- [x] CLI streams HL public data (live tested: BTC trades, SOL L2)
- [x] WS reconnect/backoff works (exponential, 500ms-30s, jitter)
- [x] Private WS channels wired (orderUpdates, userFills)
- [x] REST translator wired (candles, openOrders, positions)
- [x] Docs accurate; root clean; no debug artifacts

### Bursts
- [x] B1: Bootstrap + Contracts (S1-S3)
- [x] B2: Transport (WS/REST plumbing + ordering) (S4-S6)
- [x] B3: HL adapter v1 + public market data (S7-S9)
- [x] B4: Private WS + REST (S10-S12)
- [x] B5: Hardening + docs + tests + acceptance (S13-S16)

### Key Files
- `src/Connector.Core/Contracts/` — Unified WS/REST contracts
- `src/Connector.Core/Abstractions/` — Interfaces (adapter, translator, transport)
- `src/Connector.Core/Transport/` — WS + REST transports, rate limiter
- `src/Connector.Core/Managers/` — WebSocketManager, RestManager
- `src/Connector.Core/Exchanges/Hyperliquid/` — HL adapter, translator, config, auth
- `src/Connector.Cli/Program.cs` — CLI daemon
- `tests/Connector.Tests/` — 41 tests (contracts, translator, soak)

## Milestone 2 — Bybit adapter
**Status**: Planned

## Milestone 3 — MEXC adapter
**Status**: Planned

## Milestone 4 — Hardening + Extension UX
**Status**: Planned
