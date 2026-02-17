# PROJECT.md

## Overview

**Connector** is a .NET 8 library + CLI for exchange WebSocket/REST connectivity. It provides:

- Unified typed contracts for market data (WS) and execution (REST)
- Transport layer with reconnect/backoff, rate limiting, auth hooks
- Translator/Adapter pattern for adding exchanges
- Long-lived daemon that emits JSONL events to stdout, logs to stderr
- Per-subscription ordering guarantees

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│ CLI (thin)   │────▶│ Managers      │────▶│ Transports       │
│ stdout=JSONL │     │ WS / REST     │     │ WS / REST        │
│ stderr=logs  │     └──────┬───────┘     └─────────┬───────┘
└─────────────┘            │                        │
                    ┌──────▼───────┐         ┌──────▼───────┐
                    │ Translators   │         │ Exchange WS   │
                    │ (per exchange)│         │ / HTTP APIs   │
                    └──────────────┘         └──────────────┘
```

### Layers

- **Contracts**: Unified enums, request/response/event models for WS and REST
- **Abstractions**: Interfaces for adapters, translators, transports, auth, rate limiting
- **Transports**: WebSocket (reconnect, backoff) and REST (HttpClient) implementations
- **Managers**: WebSocketManager (subscribe, fanout, ordering) and RestManager (execute)
- **Exchanges**: Per-exchange adapter + translator implementations
- **CLI**: Thin orchestration (parse args → create managers → stream events)

### Key Decisions

- Single WS connection per exchange instance
- Per-subscription ordering via Channel<T>
- stdout = JSONL events only; stderr = structured logs
- No external services required for tests (mock/fake transports)

## Supported Exchanges

- [x] Hyperliquid (Milestone 1) — trades, l2Book, candles, orderUpdates, userFills
- [ ] Bybit (Milestone 2)
- [ ] MEXC (Milestone 3)

## Unified Contracts

### WS Events (8 types)
- TradesEvent, OrderBookL1Event, OrderBookL2Event, CandleEvent
- UserOrderEvent, FillEvent, PositionEvent, BalanceEvent

### REST Operations (7 types)
- PlaceOrder, CancelOrder, GetBalances, GetPositions, GetOpenOrders, GetFills, GetCandles

### Serialization
- camelCase property names, enums as strings
- Optional RawPayload escape hatch (--raw flag)

## CLI Usage

```bash
# Stream BTC trades from Hyperliquid
connector --exchange hl --symbols BTC --channels trades --no-auth

# Stream multiple symbols/channels
connector --exchange hl --symbols BTC,ETH,SOL --channels trades,l2,candles --no-auth

# Include raw exchange payloads
connector --exchange hl --symbols BTC --channels trades --no-auth --raw
```

## Test Suite
41 tests across 5 test files:
- ContractSerializationTests (17) — roundtrip for all contract types
- ManagerLifecycleTests (3) — start/stop/subscribe lifecycle
- TransportTests (4) — rate limiter and model validation
- HyperliquidWsTranslatorTests (14) — golden JSON fixtures
- SoakTests (3) — pipeline stress tests with fake transport
