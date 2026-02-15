# Milestones

## Milestone 7 - Hardening & QA Pass (Complete, 2026-02-14)

### Burst 1 - Documentation Audit & Completion
- Scope: PROJECT.md, PROMPTS.md, AGENTS.md, DECISIONS.md assumptions.
- Outcome: Documentation baseline finalized for M1-M6; governance rules established.
- Uncertainty eliminated: current documentation accuracy and governance expectations.

### Burst 2 - README Validation & Troubleshooting
- Scope: README examples (Windows), troubleshooting section, mux exit behavior.
- Outcome: Windows examples executed; mux exit code behavior corrected; troubleshooting added.
- Uncertainty eliminated: Windows execution paths and failure modes for CLI examples.

### Burst 3 - Cross-Platform Testing & Validation
- Scope: Windows + Linux tests, pack.ps1/sh, publish.ps1/sh.
- Outcome: Tests pass on Windows + Linux; packaging/publish scripts validated on both platforms.
- Uncertainty eliminated: cross-platform build/test/release script integrity.

### Burst 4 - Error Handling & UX Polish
- Scope: error messages, exit code consistency, help output review.
- Outcome: exit codes standardized; error messages clarified; help outputs reviewed.
- Uncertainty eliminated: CLI failure semantics and actionable error surfaces.

### Burst 5 - Bug Resolution & Release Prep
- Scope: version bump, checklist updates, decisions updates.
- Outcome: version bumped to 0.7.0; checklists updated; blocking items remain.
- Uncertainty eliminated: release version alignment and documentation tracking.

### Blocking Items
- None.

## Milestone 8 - Paper Demo Readiness (Complete, 2026-02-14)

### Burst 1 - OKX Foundation (Receiver)
- Scope: OKX WS config + trades/L2 mux sources + mux registry integration.
- Outcome: OKX receiver scaffold in Xws.Core with trades/L2 mux support.
- Uncertainty eliminated: OKX WS connectivity wiring and mux integration feasibility.

### Burst 2 - OKX Expanded Market Data
- Scope: OKX funding/liquidations/mark WS wiring + offline fixtures/tests.
- Outcome: OKX expanded market data scaffolding and fixture validation in tests.
- Uncertainty eliminated: envelope expectations for OKX expanded streams.

### Burst 3 - OKX Execution Module
- Scope: OKX execution client stub with paper mode place/cancel/cancel-all/amend.
- Outcome: OKX execution adapter added with paper-mode lifecycle support.
- Uncertainty eliminated: paper-mode execution wiring for OKX command set.

### Burst 4 - OKX Query & State Management
- Scope: paper-mode query orders/positions + CLI exchange flag for OKX.
- Outcome: xws.exec.cli now supports --exchange okx for existing commands.
- Uncertainty eliminated: execution command routing for OKX in paper mode.

### Burst 5 - Bybit Foundation (Receiver)
- Scope: Bybit WS config + trades/L2 mux sources + mux registry integration.
- Outcome: Bybit receiver scaffold in Xws.Core with trades/L2 mux support.
- Uncertainty eliminated: Bybit WS connectivity wiring and mux integration feasibility.

### Burst 6 - Bybit Expanded Market Data
- Scope: Bybit funding/liquidations/mark WS wiring + offline fixtures/tests.
- Outcome: Bybit expanded market data scaffolding and fixture validation in tests.
- Uncertainty eliminated: envelope expectations for Bybit expanded streams.

### Burst 7 - Bybit Execution Module
- Scope: Bybit execution client stub with paper mode place/cancel/cancel-all/amend.
- Outcome: Bybit execution adapter added with paper-mode lifecycle support.
- Uncertainty eliminated: paper-mode execution wiring for Bybit command set.

### Burst 8 - Bybit Query & State Management
- Scope: Bybit paper-mode query orders/positions + CLI exchange flag.
- Outcome: Bybit query operations use paper-state; CLI accepts --exchange bybit.
- Uncertainty eliminated: query surface parity for Bybit paper workflows.

### Burst 9 - Hyperliquid Expanded Market Data
- Scope: Hyperliquid funding/liquidations/mark price/user fills subscriptions.
- Outcome: Hyperliquid WS subscription builders and mux sources added for new data types.
- Uncertainty eliminated: subscription scaffolding for HL expanded market data.

### Burst 10 - MEXC Expansion & Mux Integration
- Scope: MEXC funding/mark price futures streams + mux commands for new data types + fixtures.
- Outcome: Mux supports funding/liquidations/markprice/fills; MEXC futures funding/mark price sources added with fixtures.
- Uncertainty eliminated: cross-exchange mux coverage for expanded data types.

### Burst 11 - Execution Commands Expansion
- Scope: amend/query orders/query positions commands; exchange flag extended to all exchanges.
- Outcome: xws.exec.cli supports full lifecycle commands across hl/okx/bybit/mexc in paper mode.
- Uncertainty eliminated: CLI surface area for full paper lifecycle and query workflows.

### Burst 12 - Paper Demo & Documentation
- Scope: demo scripts + README quick start + expanded examples.
- Outcome: paper demo scripts added and README updated for new exchanges and streams.
- Uncertainty eliminated: demo reproducibility and documentation coverage for paper workflows.

### Burst 13 - Testing & Validation
- Scope: Windows + Linux test runs, demo workflow, README sample validation.
- Outcome: tests pass on both platforms; demo workflow executed; core README examples verified.
- Uncertainty eliminated: cross-platform test status and demo operability.

### Burst 14 - Documentation & Release Prep
- Scope: PROJECT/PROMPTS/DECISIONS/MILESTONES/CHECKLIST updates, version bump, release prep.
- Outcome: documentation aligned with M8 delivery; version bumped to 0.8.0.
- Uncertainty eliminated: release metadata alignment with delivered scope.

## Milestone 9 - Optimization & Hardening (Complete, 2026-02-15)

### Burst 1 - Core Foundations
- Scope: new folder structure, shared interfaces, shared logging utility.
- Outcome: Xws.Core scaffolding for refactor; Logger moved to Shared/Logging.
- Uncertainty eliminated: feasibility of Shared namespace split without regressions.

### Burst 2 - Hyperliquid Refactor
- Scope: HL WS subscription builder/parser/client split, HL REST client extraction, HL execution client refactor.
- Outcome: HL adapter moved into structured modules; execution client reorganized under Exec/Exchanges/HL.
- Uncertainty eliminated: Hyperliquid refactor compatibility with existing runtime and tests.

### Burst 3 - OKX Refactor
- Scope: OKX WS split (client/subscription/parser), OKX execution adapter refactor, shared auth/config.
- Outcome: OKX modules reorganized under Exchanges/OKX with shared helpers.
- Uncertainty eliminated: OKX refactor compatibility with mux and execution surfaces.

### Burst 4 - Bybit & MEXC Refactor
- Scope: Bybit WS split (client/subscription/parser), Bybit execution adapter refactor, MEXC WS client/parser extraction.
- Outcome: Bybit modules reorganized under Exchanges/Bybit; MEXC futures sources unified under shared WS client.
- Uncertainty eliminated: Bybit/MEXC refactor compatibility with mux sources and tests.

### Burst 5 - CLI & Paper State Refactor
- Scope: xws CLI command extraction, xws.exec.cli command extraction, paper state versioning and recovery.
- Outcome: CLI programs simplified; paper state stored with version metadata and corruption handling.
- Uncertainty eliminated: CLI refactor safety and paper state persistence robustness.

### Burst 6 - Baseline & WebSocket Optimization
- Scope: baseline timing capture, allocation review for WS clients, buffer reuse and text decoding improvements.
- Outcome: WS clients use pooled buffers and avoid extra allocations; baseline timing captured for paper place command.
- Uncertainty eliminated: feasibility of reducing WS parsing allocations without changing output.

### Burst 7 - Execution & Memory Optimization
- Scope: paper state lookup optimization, memory sampling during long-running emit, pooled buffers in spot protobuf paths.
- Outcome: paper client uses clientOrderId index; memory samples captured during extended emit run.
- Uncertainty eliminated: paper execution lookup scalability and absence of immediate memory growth during emit workload.

### Burst 8 - Rate Limiting
- Scope: token-bucket limiter, HL/OKX/Bybit rate limiter wrappers, rapid-order smoke test.
- Outcome: rate limiter scaffolding in exec layer with env var overrides; rapid orders completed without errors.
- Uncertainty eliminated: ability to throttle REST calls without breaking paper workflows.

### Burst 9 - Connection Stability
- Scope: stale connection detection in runner, ping/pong loops for OKX/Bybit, frame handler error isolation, HL mux L2 wiring.
- Outcome: reconnects triggered on stale sockets; OKX/Bybit WS clients now emit keepalive pings; corrupt frame errors no longer kill sockets; HL mux L2 included for regression coverage.
- Uncertainty eliminated: resilience of WS ingestion under stale or malformed frames.

### Burst 10 - Error Handling & State Persistence
- Scope: standardized CLI error output, paper state recovery tests.
- Outcome: exec CLI uses shared error helper for consistent exit handling; corrupt state recovery covered by tests.
- Uncertainty eliminated: error message consistency and paper state recovery behavior.

### Burst 11 - Testing (Unit/Integration/Load)
- Scope: unit tests for rate limiter/paper state, mux integration test, load test loop, coverage collection.
- Outcome: expanded test suite and coverage artifacts captured for core paths.
- Uncertainty eliminated: basic regression coverage for refactored modules and mux behavior.

### Burst 12 - Testing (Edge/Testnet)
- Scope: edge case tests for malformed JSON, WebSocket stale simulation, testnet setup documentation.
- Outcome: edge cases covered in tests; testnet prerequisites documented in OPERATIONS.md.
- Uncertainty eliminated: expected behavior under malformed frames and clarity on testnet setup requirements.

### Completion Summary
- Version bumped to 0.9.0.
- ARCHITECTURE.md and OPERATIONS.md added.
- Tests, coverage, and regression commands executed (paper mode; testnet setup documented).
