# Decisions

## Pending

## Milestone 7 (2026-02-14)

- Assumption: PM v2.2 protocol document is not present in repo; applied baseline governance rules in AGENTS.md.
- Assumption: PROMPTS.md slice definitions for M1/M4/M5/M6 inferred from CHECKLIST.md and DECISIONS.md because original slice history was unavailable.

## Milestone 1 (2026-01-29)

- Output contract: stdout is JSONL only; stderr is logs/errors.
- Secrets: env vars only; no secrets written to disk.
- Symbols: exchange-native only; discovery via `hl symbols`.
- Reconnect: exponential backoff with retry cap = 3; resubscribe on reconnect.

## Milestone 2 (Draft)

- Envelope default: mux output is envelope JSONL; legacy HL supports `--format raw`.
- Mux surface: `subscribe trades --sub <exchange>[.<market>]=<sym1>,<sym2>`.
- Best-effort mux: continue if one source fails; exit 0 if stop condition met and at least one envelope emitted, else exit 1.
- MEXC spot uses protobuf decoding (official proto files); futures endpoint scaffold only in M2.
- Network reachability: MEXC WS may be region-blocked; treated as supported runtime condition.

## Milestone 3 (2026-01-30)

- Packaging split: Xws.Core is a reusable library; xws CLI depends on it.
- Output policy: single `Channel<string>` with one stdout writer in CLI; Core never writes to console.
- Offline CI: build/test must succeed without live exchange endpoints; smoke is optional and allow-fail.

## Milestone 4 (2026-01-30)

- MEXC futures supported via mux using market key `mexc.fut`.
- Mux supports `subscribe l2` for orderbook streams (envelope JSONL).

## Milestone 5 (2026-01-30)

- CLI loads .env by default only if present; `--no-dotenv` disables.
- `--dotenv <path>` requires the file to exist or exits non-zero.
- Process env vars take precedence; dotenv fills missing values only.
- Xws.Core remains pure (no env/dotenv loading).

## Milestone 6 (2026-01-30)

- Execution split: `Xws.Exec` library + `xws.exec.cli` thin wrapper.
- Modes: `paper` default; `testnet` and `mainnet` supported in library.
- Mainnet arming rule: requires `--arm-live` and `XWS_EXEC_ARM=1` (fail-closed).
- Idempotency rule: `clientOrderId` required for mainnet.
- Windows-safe CLI name: `xws.exec.cli` to avoid case-insensitive collisions.

## Milestone 8 (2026-02-14)

- OKX WS public endpoint defaulted to `wss://ws.okx.com:8443/ws/v5/public` with override via `XWS_OKX_WS_URL`.
- OKX WS channel assumptions for receiver: `trades` for trades and `books5` for L2 (logged due to partial doc access).
- OKX WS channel assumptions for expanded data: `funding-rate`, `liquidation-orders`, `mark-price` (logged due to partial doc access).
- Paper mode fill price: market orders use deterministic default price `100` when no price is provided (logged for demo consistency).
- Bybit WS endpoints assumed: `wss://stream.bybit.com/v5/public/spot` and `wss://stream.bybit.com/v5/public/linear`, override via `XWS_BYBIT_SPOT_WS_URL` / `XWS_BYBIT_FUT_WS_URL`.
- Bybit WS channel assumptions: `publicTrade.<symbol>` for trades and `orderbook.50.<symbol>` for L2 (logged due to partial doc access).
- Bybit WS channel assumptions for expanded data: `fundingRate.<symbol>`, `liquidation.<symbol>`, `markPrice.<symbol>` (logged due to partial doc access).
- Hyperliquid WS channel assumptions for expanded data: `activeAssetCtx` for funding/mark price, `userEvents` for liquidations, `userFills` for fills (logged due to partial doc access).
- MEXC futures WS channel assumptions for expanded data: `sub.fundingRate`/`push.fundingRate` and `sub.markPrice`/`push.markPrice` (logged due to partial doc access).
- Paper execution CLI persists state to `artifacts/paper/state.json` to enable multi-command lifecycle demos (library default remains in-memory).

## Milestone 9 (2026-02-14)

- Assumption: baseline performance measurements recorded after refactor/early optimizations because pre-refactor (v0.8.0) timings were no longer available; document methodology accordingly in OPERATIONS.md.
- Assumption: OKX/Bybit ping payload uses plain `ping`/`pong` text frames; added periodic ping and pong ignore in WS clients.
- Assumption: `dotnet-counters` not available in environment; memory profiling performed via short-run process sampling instead of counters.
- Assumption: testnet validation skipped due to missing credentials; documented setup steps in OPERATIONS.md.
