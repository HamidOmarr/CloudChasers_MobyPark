#!/bin/bash
set -e

# Wait until Postgres is ready
until pg_isready -U "$POSTGRES_USER"; do
  sleep 1
done

# Drop and recreate database to reset it
psql -U "$POSTGRES_USER" -d postgres -c "DROP DATABASE IF EXISTS $POSTGRES_DB;"
psql -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE $POSTGRES_DB;"

# Create TimescaleDB extension
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;"

# Restore from dump
echo "Restoring database from dump..."
pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --clean --exit-on-error /app/mobypark.dump

echo "Database restore complete."