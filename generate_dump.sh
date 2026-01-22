#!/bin/sh
set -eu

# Load environment variables from .env in the repo
export $(grep -v '^#' .env | xargs)

# Make sure DB container is running
docker compose up -d mobypark-db

echo "Waiting for database to initialize..."
until docker compose exec -u postgres mobypark-db pg_isready -U "$POSTGRES_USER"; do
  echo "Database is starting up... waiting 2 seconds"
  sleep 2
done

echo "Database is ready. Starting backup..."

# Dump DB as Postgres user
#docker compose exec -u postgres mobypark-db \
#  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /var/lib/postgresql/data/mobypark.dump
#
## Copy dump to server home directory
#CONTAINER=$(docker compose ps -q mobypark-db)
#docker cp "$CONTAINER":/var/lib/postgresql/data/mobypark.dump ~/CloudChasers_MobyPark/mobypark.dump

echo "Database dump available at ~/CloudChasers_MobyPark/mobypark.dump"
