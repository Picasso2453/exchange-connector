# Operations Guide

## Deployment Checklist
- Install .NET 8 SDK/runtime.
- Configure required environment variables.
- Validate CLI help output: `dotnet run --project src/xws -- --help`.
- Run offline tests: `dotnet test -c Release`.
- For execution (paper), ensure `artifacts/paper/` is writable.

## Environment Variables
- `XWS_HL_USER`: required for Hyperliquid private subscriptions (positions/fills).
- `XWS_EXEC_ARM`: set to `1` to arm mainnet execution.
- `XWS_HL_RATE_LIMIT`, `XWS_OKX_RATE_LIMIT`, `XWS_BYBIT_RATE_LIMIT`, `XWS_MEXC_RATE_LIMIT`: per-second REST limits (exec layer).
- `XWS_OKX_WS_URL`, `XWS_BYBIT_SPOT_WS_URL`, `XWS_BYBIT_FUT_WS_URL`: override WS endpoints.
- `.env`: optional; loaded by `xws` unless `--no-dotenv` is supplied.

## Performance
### Baseline Measurements (Local, 2026-02-15)
- `xws.exec.cli place` (paper, cold start): ~0.83s total runtime (Release, `--no-build`).
- Memory sample during extended `dev emit` run: ~80 MB working set after initial ramp.

### How To Measure
- Execution latency:
  - `Measure-Command { dotnet run -c Release --no-build --project src/xws.exec.cli -- place --mode paper --exchange hl --symbol SOL --side buy --type market --size 1 --client-order-id perf-001 }`
- WebSocket throughput:
  - Use fixture-based decoder loops (see tests in `tests/xws.tests/LoadTests.cs`) or live streams if allowed.
- Memory:
  - Sample process working set during a long run (e.g., `dev emit --count 5000000`) via `Get-Process`.

## Monitoring
- Logs are emitted to stderr; data is JSONL on stdout.
- Exit codes: `0` success, `1` user/config errors, `2` system errors.
- Watch for:
  - `connection appears stale` (WebSocket reconnects)
  - `rate limit: throttling request` (REST limiter engaged)
  - `paper state file corrupt` (auto-reset with `.corrupt` copy)

## Troubleshooting
- **No output on subscriptions:** verify symbol format and exchange availability; check for region blocking (MEXC).
- **Timeouts on mux:** ensure `--max-messages` and `--timeout-seconds` are paired.
- **Paper state issues:** corrupt `state.json` will be renamed to `state.json.corrupt.<timestamp>` and reset.
- **Rate limit warnings:** reduce request rate or raise limits via env vars (use conservative values).

## Resource Requirements
- CPU: 1-2 cores for a single exchange stream; more for multi-exchange mux.
- Memory: ~100 MB baseline for CLI + WebSocket streams; higher under heavy load.
- Network: stable low-latency connection recommended for live streams.

## Testnet Setup
### Hyperliquid
- Requires wallet address and API credentials.
- Set `XWS_HL_USER` and configure `ExecutionConfig` for testnet.

### OKX
- Requires OKX testnet API key/secret/passphrase.
- Use native OKX symbol formats (e.g., `BTC-USDT-SWAP`).

### Bybit
- Requires Bybit testnet API key/secret.
- Use Bybit symbol formats (e.g., `BTCUSDT`).

If credentials are unavailable, document setup and run paper mode validation only.
