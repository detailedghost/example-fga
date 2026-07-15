#!/usr/bin/env bash
set -euo pipefail

docker compose up -d

echo "Waiting for OpenFGA to be healthy..."
for _ in $(seq 1 30); do
  if curl -sf http://localhost:8080/healthz >/dev/null; then
    echo "OpenFGA is up."
    break
  fi
  sleep 1
done

dotnet run
