#!/usr/bin/env bash
set -euo pipefail

# AITS production setup script. Run from the repository root on the VPS.

if [ ! -f docker-compose.prod.yml ]; then
  echo "Error: run this script from the repository root (docker-compose.prod.yml not found)."
  exit 1
fi

if [ ! -f .env ]; then
  echo "Error: .env not found. Copy deploy/.env.example to .env and fill in the values."
  exit 1
fi

echo "==> Installing Docker (if needed)..."
if ! command -v docker >/dev/null 2>&1; then
  curl -fsSL https://get.docker.com | sh
fi

echo "==> Building and starting containers..."
docker compose -f docker-compose.prod.yml up -d --build

echo "==> Done. Check status with: docker compose -f docker-compose.prod.yml ps"
echo "==> Follow logs with:       docker compose -f docker-compose.prod.yml logs -f api"
