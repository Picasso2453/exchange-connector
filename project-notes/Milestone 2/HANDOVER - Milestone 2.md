PM Handover — xws / exchange-connector

Milestones 1 + 2 closed (v0.1.0, v0.2.0)

Current status

Milestone 1 complete and frozen: tag v0.1.0 (HL-only raw JSONL pump).

Milestone 2 complete and frozen: completed on branch milestone-2, tag v0.2.0 (adds MEXC proof + mux + envelope default).

What shipped (customer-visible)
Milestone 1 (v0.1.0)

Hyperliquid adapter:

hl subscribe trades --symbol <native> → streams raw WS frames as JSONL to stdout.

hl symbols [--filter] → symbol discovery via /info, outputs raw JSONL.

hl subscribe positions → private positions/account stream gated by env var, outputs raw JSONL.

IO contract: stdout data only (JSONL), stderr logs/errors only.

Reliability: exponential backoff + resubscribe; fail after 3 reconnect failures with non-zero exit.

Determinism: --max-messages, --timeout-seconds; Ctrl+C exits 0.

Milestone 2 (v0.2.0)

MEXC Spot trades adapter:

Protobuf decode implemented; Futures endpoint scaffold exists (no futures runtime yet).

Mux command:

subscribe trades --sub ... runs multiple sources concurrently and emits envelope JSONL on stdout.

Unified output:

Default output = envelope JSONL (for mux paths).

Legacy HL raw frames preserved via --format raw.

Best-effort mux policy (important for ops):

If one source can’t connect, error goes to stderr, others continue.

Exit 0 if stop condition met and at least one envelope emitted; exit 1 if zero envelopes emitted.

Offline validation:

Offline protobuf decode test added; CI can validate without live MEXC connectivity.

Commands (PM-friendly smoke tests)
Build + test
dotnet build -c Release
dotnet test -c Release


(Validation listed in M2 closure.)

Hyperliquid (legacy raw, M1-compatible)
dotnet run --project src/xws -- hl subscribe trades --symbol SOL --format raw --max-messages 10 --timeout-seconds 30


MEXC Spot trades (may be region-blocked)
dotnet run --project src/xws -- mexc spot subscribe trades --symbol BTCUSDT --max-messages 10 --timeout-seconds 30


Mux: HL + MEXC concurrently (envelope default)
dotnet run --project src/xws -- subscribe trades \
  --sub hl=SOL \
  --sub mexc.spot=BTCUSDT \
  --max-messages 50 --timeout-seconds 30


Smoke scripts (network-restricted friendly)

Run with XWS_SMOKE_ALLOW_NETFAIL=1 (alias XWS_MEXC_ALLOW_FAIL=1) on restricted networks.

Environment variables
Hyperliquid (M1)

XWS_HL_NETWORK (optional, default mainnet; mainnet|testnet)

XWS_HL_USER (required for private positions stream)

XWS_HL_WS_URL (optional)

XWS_HL_HTTP_URL (optional)

MEXC (M2)

MEXC WS may be blocked regionally; treat as expected and rely on best-effort mux + offline tests.

Repo map (where things live)

(From DEV handover, unchanged through M2 unless otherwise noted.)

CLI entry: src/xws/Program.cs

WS runner + backoff: src/xws/Core/WebSocket/WebSocketRunner.cs

Output writer(s): src/xws/Core/Output/*

Subscription registry: src/xws/Core/Subscriptions/*

HL config/helpers: src/xws/Exchanges/Hyperliquid/*

MEXC adapter: src/xws/Exchanges/Mexc/* (added M2)

Env reader: src/xws/Core/Env/EnvReader.cs

CI workflow: .github/workflows/ci.yml

Docs/decisions: README.md, DECISIONS.md, PROJECT.md

Key decisions (locked)
Milestone 1 (2026-01-29)

Raw-first pump; stdout JSONL only, stderr logs only; env-var-only secrets; exchange-native symbols; reconnect cap=3 + resubscribe.

Milestone 2 (v0.2.0)

Envelope JSONL default for mux/unified consumption; preserve M1 raw via --format raw.

Best-effort mux under partial connectivity; offline protobuf decode test is the “proof” when MEXC WS is blocked.

Known limits / risks (going into M3)

Regional blocking: MEXC WS may be unreachable from some networks; VPN may be required; mux continues best-effort.

No normalized cross-exchange schema yet (still raw-first, now wrapped).

MEXC Futures is scaffolded only (not implemented).

“Where we left it” checklist for next PM

Confirm tags exist on main/remote: v0.1.0, v0.2.0.

Run:

dotnet build -c Release

dotnet test -c Release

Run smoke scripts:

On restricted networks: set XWS_SMOKE_ALLOW_NETFAIL=1

Verify IO contract still holds: stdout=data JSONL, stderr=logs/errors.

Decide M3 direction (pick one and spec-lock):

Add MEXC Futures trades (using existing scaffold)

Add more streams (e.g., order book / ticker) under the envelope

Add packaging/distribution (artifacts + GitHub releases)

If you want, paste your new Milestone 2 - Closure.txt contents and I’ll reformat the handover to match your preferred “DEV handover” style exactly (same headings/ordering), but the above is already grounded in the uploaded artifacts.