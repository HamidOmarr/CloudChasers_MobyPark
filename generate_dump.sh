#!/bin/sh
set -eu

# Load environment variables from .env in the repo
export $(grep -v '^#' .env | xargs)

# Make sure DB container is running
docker compose up -d mobypark-db

# Dump DB as Postgres user
docker compose exec -u postgres mobypark-db \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /var/lib/postgresql/data/mobypark.dump

# Copy dump to server home directory
CONTAINER=$(docker compose ps -q mobypark-db)
docker cp "$CONTAINER":/var/lib/postgresql/data/mobypark.dump ~/CloudChasers_MobyPark/mobypark.dump

echo "Database dump available at ~/CloudChasers_MobyPark/mobypark.dump"
