# Checklist

## Milestones

## Milestone 2 (Draft)

- [ ] Bootstrap complete
- [ ] Envelope + mux scaffolding
- [ ] MEXC spot proof
- [ ] Docs updated

## Milestone 4 (Draft)

- [x] Bootstrap complete
- [x] Fixture replay harness
- [x] Envelope JSONL assertions
- [x] MEXC futures trades (mux + fixtures)
- [x] MEXC futures L2 (mux + fixtures)
- [x] Docs updated
- [x] Tag v0.4.0 pushed

## Milestone 5 (Draft)

- [x] Bootstrap complete
- [x] Dotenv loader + flags
- [x] Dotenv tests
- [x] README updated
- [x] Version bump + tag v0.5.0

## Milestone 6 (Draft)

- [x] Exec library project exists
- [x] Paper deterministic execution + tests
- [x] Exec CLI JSONL stdout contract
- [x] Mainnet arming + idempotency guards
- [x] Hyperliquid live place/cancel/cancel-all (offline-safe tests)
- [x] Docs updated
- [x] Version bump + tag v0.6.0

## Milestone 7 (In Progress)

- [x] PROJECT.md finalized (no Draft/TBD)
- [x] PROMPTS.md completed (M1/M4/M5/M6 slices)
- [x] AGENTS.md governance instructions completed
- [x] README.md examples validated on Windows (documented + fixes)
- [x] README.md examples validated on Linux
- [x] README.md troubleshooting section added
- [x] Windows tests pass (Release)
- [x] Linux tests pass (Release)
- [x] pack.ps1 / publish.ps1 validated on Windows
- [x] pack.sh / publish.sh validated on Linux
- [x] Exit codes consistent (0 success, 1 user, 2 system)
- [x] CLI error messages reviewed and improved
- [x] --help outputs reviewed
- [x] Version bumped to v0.7.0
- [ ] Tag v0.7.0 pushed
- [ ] CI green on Windows + Linux

## Milestone 8 (Complete)

- [x] Slice 1: OKX adapter structure (Core + config)
- [x] Slice 2: OKX WS trades subscription (mux source)
- [x] Slice 3: OKX WS L2 subscription (mux source)
- [x] Slice 4: OKX mux registry integration (`okx.spot`, `okx.fut`)
- [x] Slice 5: OKX WS funding subscription (mux source)
- [x] Slice 6: OKX WS liquidations subscription (mux source)
- [x] Slice 7: OKX WS mark price subscription (mux source)
- [x] Slice 8: OKX offline fixtures + replay tests
- [x] Slice 9: OKX execution adapter structure (client + REST interface)
- [x] Slice 10: OKX place order (paper mode)
- [x] Slice 11: OKX cancel order + cancel-all (paper mode)
- [x] Slice 12: OKX amend order (paper mode)
- [x] Slice 13: OKX query orders (paper mode snapshot)
- [x] Slice 14: OKX query positions (paper mode snapshot)
- [x] Slice 15: OKX added to xws.exec.cli via --exchange flag
- [x] Slice 16: Bybit adapter structure (Core + config)
- [x] Slice 17: Bybit WS trades subscription (mux source)
- [x] Slice 18: Bybit WS L2 subscription (mux source)
- [x] Slice 19: Bybit mux registry integration (`bybit.spot`, `bybit.fut`)
- [x] Slice 20: Bybit WS funding subscription (mux source)
- [x] Slice 21: Bybit WS liquidations subscription (mux source)
- [x] Slice 22: Bybit WS mark price subscription (mux source)
- [x] Slice 23: Bybit offline fixtures + replay tests
- [x] Slice 24: Bybit execution adapter structure (client + REST interface)
- [x] Slice 25: Bybit place order (paper mode)
- [x] Slice 26: Bybit cancel order + cancel-all (paper mode)
- [x] Slice 27: Bybit amend order (paper mode)
- [x] Slice 28: Bybit query orders (paper mode snapshot)
- [x] Slice 29: Bybit query positions (paper mode snapshot)
- [x] Slice 30: Bybit added to xws.exec.cli via --exchange flag
- [x] Slice 31: Hyperliquid funding rates WS subscription
- [x] Slice 32: Hyperliquid liquidations WS subscription
- [x] Slice 33: Hyperliquid mark price WS subscription
- [x] Slice 34: Hyperliquid user fills WS subscription
- [x] Slice 35: MEXC futures funding rates WS subscription
- [x] Slice 36: MEXC futures mark price WS subscription
- [x] Slice 37: Mux support for funding/liquidations/markprice/fills
- [x] Slice 38: Offline fixtures for HL/MEXC expanded data types
- [x] Slice 39: Exec CLI query orders command
- [x] Slice 40: Exec CLI query positions command
- [x] Slice 41: Exec CLI amend command
- [x] Slice 42: Exec CLI place updated with --exchange flag for all exchanges
- [x] Slice 43: scripts/demo-paper.sh created
- [x] Slice 44: scripts/demo-paper.ps1 created
- [x] Slice 45: README Quick Start: Paper Demo section added
- [x] Slice 46: README examples updated for OKX/Bybit and new data types
- [x] Slice 47: Windows test suite executed (Release)
- [x] Slice 48: Linux test suite executed (Release)
- [x] Slice 49: Acceptance demo workflow executed (HL + OKX + Bybit)
- [x] Slice 50: README examples validated (core samples)
- [x] Slice 51: PROJECT.md updated for M8 scope and exchanges
- [x] Slice 52: PROMPTS.md updated for M8 slices
- [x] Slice 53: DECISIONS.md updated with M8 architecture decisions
- [x] Slice 54: MILESTONES.md updated with M8 completion and burst evaluations
- [x] Slice 55: Release prep (CHECKLIST, clean root, version bump, tag v0.8.0)

## Milestone 9 (In Progress)

- [x] Slice 1: Create Xws.Core folder structure (Exchanges/{HL,OKX,Bybit,MEXC}/{WebSocket,Rest}, Shared/)
- [x] Slice 2: Define core interfaces (IExchangeAdapter, IWebSocketClient, IMessageParser)
- [x] Slice 3: Move shared utilities to Xws.Core/Shared (Logger)
- [x] Slice 4: Run tests after folder restructure
- [x] Slice 5: Refactor Hyperliquid adapter (WebSocket client/subscriptions/parser)
- [x] Slice 6: Extract Hyperliquid REST client
- [x] Slice 7: Refactor Hyperliquid execution adapter (Exec/Exchanges/HL)
- [x] Slice 8: Run HL-specific tests
- [x] Slice 9: Refactor OKX adapter (WebSocket client/subscriptions/parser)
- [x] Slice 10: Refactor OKX execution adapter (Exec/Exchanges/OKX)
- [x] Slice 11: Extract OKX shared logic (auth/config)
- [x] Slice 12: Run OKX-specific tests
- [x] Slice 13: Refactor Bybit adapter (WebSocket client/subscriptions/parser)
- [x] Slice 14: Refactor Bybit execution adapter (Exec/Exchanges/Bybit)
- [x] Slice 15: Refactor MEXC adapter (WebSocket client/parser)
- [x] Slice 16: Run Bybit/MEXC-specific tests
- [x] Slice 17: Refactor xws CLI Program.cs (Commands/)
- [x] Slice 18: Refactor xws.exec.cli Program.cs (Commands/ + Execution/)
- [x] Slice 19: Consolidate paper state management (versioning + recovery)
- [x] Slice 20: Run full CLI test suite after refactor
- [x] Slice 21: Measure v0.8.0 baseline performance
- [x] Slice 22: Profile WebSocket parsing allocations
- [x] Slice 23: Optimize WebSocket parsing (buffer reuse, allocations)
- [x] Slice 24: Benchmark optimized parsing, document results
- [x] Slice 25: Optimize REST clients (pooling/keep-alive/timeouts)
- [x] Slice 26: Optimize paper execution state lookups
- [x] Slice 27: Profile memory usage (long-running stream)
- [x] Slice 28: Fix memory leaks, dispose on reconnect
- [x] Slice 29: Implement rate limiter abstraction
- [x] Slice 30: Add HL rate limiting (20/sec default)
- [x] Slice 31: Add OKX/Bybit rate limiting (10/sec default)
- [x] Slice 32: Test rate limiting (20 rapid orders)
- [x] Slice 33: Improve reconnect logic (stale detection)
- [x] Slice 34: Add connection health monitoring (ping/pong)
- [x] Slice 35: Handle edge cases (partial/corrupt frames)
- [x] Slice 36: Test connection stability (simulate interruption)
- [x] Slice 37: Standardize error handling patterns
- [x] Slice 38: Improve error messages (context + guidance)
- [x] Slice 39: Harden paper state persistence (versioning + recovery)
- [x] Slice 40: Test state corruption recovery
- [x] Slice 41: Add unit tests for refactored modules
- [x] Slice 42: Add integration tests (mux + multi-exchange exec)
- [x] Slice 43: Add load tests (1000+ msg/sec, rapid orders)
- [x] Slice 44: Measure test coverage (XPlat Code Coverage)
- [x] Slice 45: Add edge case tests (malformed JSON, 429)
- [x] Slice 46: Add state corruption tests
- [x] Slice 47: Testnet validation HL
- [x] Slice 48: Testnet validation OKX or Bybit
- [x] Slice 49: Create ARCHITECTURE.md
- [x] Slice 50: Create/expand OPERATIONS.md
- [x] Slice 51: Add performance section to OPERATIONS.md
- [x] Slice 52: Document testnet setup in OPERATIONS.md
- [x] Slice 53: Update PROJECT.md for M9 scope
- [x] Slice 54: Update PROMPTS.md for M9 slices
- [x] Slice 55: Update DECISIONS.md for M9 architecture
- [x] Slice 56: Update MILESTONES.md for M9 completion
- [x] Slice 57: Release prep (CHECKLIST, clean root, version bump, tag v0.9.0)
