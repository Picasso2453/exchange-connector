# CURRENT_STATE.md

## What works now
- Solution builds cleanly (0 errors, 0 warnings)
- 41 tests passing (unit + soak)
- CLI streams live JSONL events from Hyperliquid
- Supported channels: trades, l2Book, candles (public), orderUpdates, userFills (private)
- WebSocket transport with exponential backoff reconnect
- REST transport with rate limiting and auth hooks
- Token bucket rate limiter
- Per-subscription ordering via bounded Channel<T>
- Exchange registry for multi-exchange support
- HL REST translator (candles, open orders, positions)
- NoAuth + HyperliquidAuth providers

## Architecture
```
Connector.Core/
├── Abstractions/     # IExchangeAdapter, IWsTranslator, IRestTranslator, etc.
├── Contracts/        # Unified WS/REST request/response/event types
├── Exchanges/
│   └── Hyperliquid/  # HL adapter, config, translators, auth
├── Managers/         # WebSocketManager, RestManager
├── Transport/        # WsTransport, RestTransport, TokenBucketRateLimiter
└── ConnectorConfig.cs
```

## Test coverage
- 17 contract serialization roundtrip tests
- 3 manager lifecycle tests
- 4 transport tests (rate limiter, models)
- 14 HL WS translator tests (golden JSON fixtures)
- 3 soak tests (fake transport, multi-channel, high-throughput)

## What's next
- M2: Bybit adapter (same unified contracts)
- M3: MEXC adapter
- M4: Hardening, plugin docs, observability

## Known issues
- Decimal parsing requires InvariantCulture on non-English locales (fixed)
- HL auth provider REST signing is stub-only (needs Ethereum signing library for exchange API)
