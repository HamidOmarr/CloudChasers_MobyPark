#!/bin/sh
set -eu

# Load environment variables from .env
export $(grep -v '^#' .env | xargs)

# Ensure DB container is running
docker compose up -d mobypark-db

# Generate dump inside container
docker compose exec -u postgres mobypark-db \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /var/lib/postgresql/data/mobypark.dump

# Copy dump to host server
CONTAINER=$(docker compose ps -q mobypark-db)
docker cp "$CONTAINER":/var/lib/postgresql/data/mobypark.dump ./mobypark.dump

echo "Database dump generated at ./mobypark.dump"
