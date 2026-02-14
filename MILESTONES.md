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
