# DECISIONS.md

Append-only log of architectural decisions.

---

## D001 — Transport/Translator separation
**Date**: 2026-02-17
**Context**: Need clean exchange extensibility without coupling transport machinery to protocol details.
**Decision**: Separate Transport (connect/send/receive/reconnect) from Translator (unified ↔ exchange message mapping).
**Consequence**: Each exchange only needs to implement IWsTranslator + IRestTranslator. Transport code is fully reusable.

## D002 — stdout = JSONL events only
**Date**: 2026-02-17
**Context**: CLI needs to be scriptable and pipeable.
**Decision**: stdout emits only JSONL events (one JSON object per line). All logs, diagnostics, and status go to stderr.
**Consequence**: Users can pipe stdout to jq, files, or other tools without filtering noise.

## D003 — Single WS connection per exchange instance
**Date**: 2026-02-17
**Context**: Design for robustness up to ~10 exchanges / 100 symbols.
**Decision**: One WebSocket connection per exchange adapter instance. Multiple subscriptions multiplexed on same connection.
**Consequence**: Simpler connection management; exchange-imposed connection limits respected naturally.

## D004 — JSON serialization: camelCase + enums as strings
**Date**: 2026-02-17
**Context**: Need stable, readable JSONL output.
**Decision**: Use System.Text.Json with camelCase property naming and JsonStringEnumConverter for all unified contract types.
**Consequence**: Output is human-readable and stable across versions.

## D005 — Microsoft.Extensions.Logging
**Date**: 2026-02-17
**Context**: Need structured logging to stderr.
**Decision**: Use Microsoft.Extensions.Logging.Abstractions in Core, Console provider in CLI.
**Consequence**: Standard .NET logging; easy to swap providers.

## D006 — Per-subscription ordering via bounded Channel<T>
**Date**: 2026-02-17
**Context**: Need per-subscription ordered delivery while supporting concurrent exchange streams.
**Decision**: Use `Channel.CreateBounded<UnifiedWsEvent>` with DropOldest backpressure policy. Capacity 10,000 by default.
**Consequence**: No deadlocks under burst; oldest events dropped if consumer falls behind. Log warning on drop.

## D007 — Exponential backoff with jitter for WS reconnect
**Date**: 2026-02-17
**Context**: Need resilient reconnect without hammering exchange during outages.
**Decision**: Initial 500ms, 2x multiplier, max 30s, 25% jitter. Reset on successful receive.
**Consequence**: Fast reconnect for transient failures; polite backoff for sustained outages.

## D008 — Token bucket rate limiter
**Date**: 2026-02-17
**Context**: Need to respect exchange rate limits for both WS sends and REST calls.
**Decision**: Simple token bucket (configurable max tokens + refill interval). Default: 10 tokens/second.
**Consequence**: Conservative defaults prevent rate limit violations. Exchanges can tune via config.

## D009 — HL channels: Trades + L2Book + Candles (public), OrderUpdates + UserFills (private)
**Date**: 2026-02-17
**Context**: Need to choose which HL WS channels to support for M1.
**Decision**: Support 5 channels matching HL's cleanest API surface. Trades and L2Book for market data, Candles for OHLCV, OrderUpdates and UserFills for private.
**Consequence**: Covers primary use cases. HL-specific channels like userEvents deferred to future milestones.

## D010 — Error strategy: ConnectorException with status + body
**Date**: 2026-02-17
**Context**: Need consistent error propagation for REST failures.
**Decision**: Throw `ConnectorException` with StatusCode and ResponseBody for non-2xx REST responses. Return typed results for success.
**Consequence**: Callers can catch ConnectorException and inspect status/body for error handling.
