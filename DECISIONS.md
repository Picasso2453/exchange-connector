# Decisions

## Pending

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
