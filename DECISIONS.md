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
