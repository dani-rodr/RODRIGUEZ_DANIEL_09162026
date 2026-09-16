#!/usr/bin/env bash
set -euo pipefail

cd "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

if ! command -v docker >/dev/null 2>&1; then
    printf '%s\n' "Docker is required. Install Docker Desktop and try again."
    exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
    printf '%s\n' "Docker Compose is required. Update Docker Desktop and try again."
    exit 1
fi

if [ ! -f .env ]; then
    cp .env.example .env
    printf '%s\n' "Created .env from .env.example."
fi

docker compose up --build --detach --wait

api_port=5274
while IFS='=' read -r key value; do
    value=${value%$'\r'}
    if [ "$key" = "API_PORT" ] && [ -n "$value" ]; then
        api_port=$value
    fi
done < .env

printf '%s\n' "API is running at http://localhost:${api_port}"
printf '%s\n' "Swagger: http://localhost:${api_port}/swagger"
printf '%s\n' "Default API key: local-development-key"
printf '%s\n' "Use the API_KEY value from .env if you changed it."
