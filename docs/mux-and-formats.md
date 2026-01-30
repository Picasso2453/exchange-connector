# Mux & Formats

## Envelope JSONL (default for mux)

Mux output is one JSON object per line with an envelope wrapper. Stdout is data only; stderr is logs/errors.

## Raw format

Legacy Hyperliquid commands support `--format raw` to preserve raw frames:

```
dotnet run --project src/xws -- hl subscribe trades --symbol SOL --format raw
```

## Stop conditions

- `--max-messages <N>`: exit 0 after N lines
- `--timeout-seconds <T>`: exit non-zero if N not reached within T seconds
