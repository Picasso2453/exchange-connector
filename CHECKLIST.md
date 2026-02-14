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
- [ ] Slice 55: Release prep (CHECKLIST, clean root, version bump, tag v0.8.0)
