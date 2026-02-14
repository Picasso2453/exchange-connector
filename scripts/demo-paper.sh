#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

STATE_DIR="${ROOT_DIR}/artifacts/paper"
STATE_FILE="${STATE_DIR}/state.json"
TRADES_FILE="${STATE_DIR}/trades.jsonl"

mkdir -p "${STATE_DIR}"
rm -f "${STATE_FILE}"

dotnet run --project "${ROOT_DIR}/src/xws" -- hl subscribe trades \
  --symbol SOL \
  --max-messages 2 --timeout-seconds 30 > "${TRADES_FILE}" &
TRADES_PID=$!

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- place \
  --mode paper --exchange hl --symbol SOL \
  --side buy --type limit --size 1 --price 100 --client-order-id demo-hl-001

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- query orders \
  --mode paper --exchange hl --status open

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- amend \
  --mode paper --exchange hl --order-id 000001 --price 101

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- cancel \
  --mode paper --exchange hl --order-id 000001

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- place \
  --mode paper --exchange hl --symbol SOL \
  --side sell --type market --size 1 --client-order-id demo-hl-002

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- query positions \
  --mode paper --exchange hl

dotnet run --project "${ROOT_DIR}/src/xws.exec.cli" -- cancel-all \
  --mode paper --exchange hl

wait "${TRADES_PID}"

echo "demo complete: ${TRADES_FILE}"
