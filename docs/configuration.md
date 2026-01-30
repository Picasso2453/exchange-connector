# Configuration

## .env loading (CLI only)

- Default: loads `./.env` if it exists (no error if missing).
- `--dotenv <path>`: load explicit file; missing file exits non-zero and logs to stderr.
- `--no-dotenv`: disables dotenv loading entirely.
- Precedence: process env vars win; dotenv fills missing values only.

## Env vars

Hyperliquid:
- XWS_HL_NETWORK (optional, default: mainnet)
- XWS_HL_USER (required for positions)
- XWS_HL_WS_URL (optional override)
- XWS_HL_HTTP_URL (optional override)

MEXC:
- XWS_MEXC_SPOT_WS_URL (optional override)
- XWS_MEXC_FUT_WS_URL (optional override)
