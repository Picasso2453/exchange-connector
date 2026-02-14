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
