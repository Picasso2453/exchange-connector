# Decisions

## Pending

## Milestone 1 (2026-01-29)

- Output contract: stdout is JSONL only; stderr is logs/errors.
- Secrets: env vars only; no secrets written to disk.
- Symbols: exchange-native only; discovery via `hl symbols`.
- Reconnect: exponential backoff with retry cap = 3; resubscribe on reconnect.
