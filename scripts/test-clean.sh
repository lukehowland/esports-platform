#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

restore_stack() {
  local status=$?
  echo
  echo "Restoring clean demo stack..."
  docker compose down --remove-orphans
  docker compose up --build -d
  exit "$status"
}

trap restore_stack EXIT

echo "Resetting stack before integration tests..."
docker compose down --remove-orphans

echo
echo "Running integration tests against a freshly seeded stack..."
docker compose --profile test up --build --abort-on-container-exit --exit-code-from tests tests
