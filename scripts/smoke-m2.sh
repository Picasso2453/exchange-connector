#!/usr/bin/env bash
set -euo pipefail

raw_out="$(mktemp)"
mux_out="$(mktemp)"
allow_mexc_fail="${XWS_MEXC_ALLOW_FAIL:-}"

cleanup() {
  rm -f "$raw_out" "$mux_out"
}
trap cleanup EXIT

dotnet run --project src/xws -- hl subscribe trades --symbol SOL --format raw --max-messages 3 --timeout-seconds 30 > "$raw_out"
if [ ! -s "$raw_out" ]; then
  echo "hl raw produced no output" >&2
  exit 1
fi

dotnet run --project src/xws -- subscribe trades --sub hl=SOL --max-messages 3 --timeout-seconds 30 > "$mux_out"
if [ ! -s "$mux_out" ]; then
  echo "mux produced no output" >&2
  exit 1
fi

if ! dotnet run --project src/xws -- mexc spot subscribe trades --symbol BTCUSDT --max-messages 2 --timeout-seconds 5 > "$mux_out"; then
  if [ "$allow_mexc_fail" != "1" ]; then
    echo "mexc spot failed" >&2
    exit 1
  fi
fi

if ! dotnet run --project src/xws -- subscribe trades --sub hl=SOL --sub mexc.spot=BTCUSDT --max-messages 3 --timeout-seconds 30 > "$mux_out"; then
  if [ "$allow_mexc_fail" != "1" ]; then
    echo "mux hl+mexc failed" >&2
    exit 1
  fi
fi
if [ ! -s "$mux_out" ]; then
  echo "mux hl+mexc produced no output" >&2
  exit 1
fi

echo "smoke ok"
