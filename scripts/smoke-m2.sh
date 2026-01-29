#!/usr/bin/env bash
set -euo pipefail

raw_out="$(mktemp)"
mux_out="$(mktemp)"

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

echo "smoke ok"
