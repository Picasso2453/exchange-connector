# Prompts

## Milestone 1 (Complete)
- Slice 1: CLI scaffold + IO contract (stdout JSONL, stderr logs)
- Slice 2: Hyperliquid symbols discovery
- Slice 3: Hyperliquid subscribe trades with reconnect/backoff and retry cap
- Slice 4: Hyperliquid private positions gated by env vars
- Slice 5: Baseline docs + CI smoke notes

## Milestone 4 (Complete)
- Slice 1: Fixture replay harness + envelope JSONL assertions
- Slice 2: MEXC futures trades via mux (market key: mexc.fut)
- Slice 3: MEXC futures L2 via mux
- Slice 4: Documentation updates + tag v0.4.0

## Milestone 5 (Complete)
- Slice 1: Dotenv loader in CLI (optional load if present)
- Slice 2: Dotenv flags --dotenv and --no-dotenv
- Slice 3: Dotenv precedence rules + tests
- Slice 4: README updates + tag v0.5.0

## Milestone 6 (Complete)
- Slice 1: Execution split (Xws.Exec library + xws.exec.cli)
- Slice 2: Paper execution flows + deterministic tests
- Slice 3: Mainnet safety gates and idempotency guard
- Slice 4: Hyperliquid place/cancel/cancel-all (offline-safe tests)
- Slice 5: Documentation updates + tag v0.6.0

## Milestone 8 (Complete)
- Slice 1: OKX adapter structure (Core + config)
- Slice 2: OKX WS trades subscription (mux source)
- Slice 3: OKX WS L2 subscription (mux source)
- Slice 4: OKX mux registry integration (`okx.spot`, `okx.fut`)
- Slice 5: OKX WS funding subscription (mux source)
- Slice 6: OKX WS liquidations subscription (mux source)
- Slice 7: OKX WS mark price subscription (mux source)
- Slice 8: OKX offline fixtures + replay tests
- Slice 9: OKX execution adapter structure (client + REST interface)
- Slice 10: OKX place order (paper mode)
- Slice 11: OKX cancel order + cancel-all (paper mode)
- Slice 12: OKX amend order (paper mode)
- Slice 13: OKX query orders (paper mode snapshot)
- Slice 14: OKX query positions (paper mode snapshot)
- Slice 15: OKX added to xws.exec.cli via --exchange flag
- Slice 16: Bybit adapter structure (Core + config)
- Slice 17: Bybit WS trades subscription (mux source)
- Slice 18: Bybit WS L2 subscription (mux source)
- Slice 19: Bybit mux registry integration (`bybit.spot`, `bybit.fut`)
- Slice 20: Bybit WS funding subscription (mux source)
- Slice 21: Bybit WS liquidations subscription (mux source)
- Slice 22: Bybit WS mark price subscription (mux source)
- Slice 23: Bybit offline fixtures + replay tests
- Slice 24: Bybit execution adapter structure (client + REST interface)
- Slice 25: Bybit place order (paper mode)
- Slice 26: Bybit cancel order + cancel-all (paper mode)
- Slice 27: Bybit amend order (paper mode)
- Slice 28: Bybit query orders (paper mode snapshot)
- Slice 29: Bybit query positions (paper mode snapshot)
- Slice 30: Bybit added to xws.exec.cli via --exchange flag
- Slice 31: Hyperliquid funding rates WS subscription
- Slice 32: Hyperliquid liquidations WS subscription
- Slice 33: Hyperliquid mark price WS subscription
- Slice 34: Hyperliquid user fills WS subscription
- Slice 35: MEXC futures funding rates WS subscription
- Slice 36: MEXC futures mark price WS subscription
- Slice 37: Mux support for funding/liquidations/markprice/fills
- Slice 38: Offline fixtures for HL/MEXC expanded data types
- Slice 39: Exec CLI query orders command
- Slice 40: Exec CLI query positions command
- Slice 41: Exec CLI amend command
- Slice 42: Exec CLI place updated with --exchange flag for all exchanges
- Slice 43: scripts/demo-paper.sh created
- Slice 44: scripts/demo-paper.ps1 created
- Slice 45: README Quick Start: Paper Demo section added
- Slice 46: README examples updated for OKX/Bybit and new data types
- Slice 47: Windows test suite executed (Release)
- Slice 48: Linux test suite executed (Release)
- Slice 49: Acceptance demo workflow executed (HL + OKX + Bybit)
- Slice 50: README examples validated (core samples)
- Slice 51: PROJECT.md updated for M8 scope and exchanges
- Slice 52: PROMPTS.md updated for M8 slices
- Slice 53: DECISIONS.md updated with M8 architecture decisions
- Slice 54: MILESTONES.md updated with M8 completion and burst evaluations
- Slice 55: Release prep (CHECKLIST, clean root, version bump, tag v0.8.0)
