#!/usr/bin/env bash
set -euo pipefail

provider=${AUTHORIZATION_PROVIDER:-$(sed -n 's/^AUTHORIZATION_PROVIDER=//p' .env)}
provider=${provider:-openfga}

case "$provider" in
openfga)
  docker compose up -d
  echo "Waiting for OpenFGA to be healthy..."
  for _health_attempt in $(seq 1 30); do
    if curl -sf http://localhost:8080/healthz >/dev/null; then
      echo "OpenFGA is up."
      break
    fi
    sleep 1
  done
  ;;
verifiedpermissions)
  docker compose up -d postgres flyway
  echo "Using Amazon Verified Permissions; bootstrap it with ./db/avp/bootstrap.sh first."
  ;;
*)
  echo "Unknown AUTHORIZATION_PROVIDER '$provider'." >&2
  exit 1
  ;;
esac

dotnet run
