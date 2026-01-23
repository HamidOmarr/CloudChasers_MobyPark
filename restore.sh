#!/bin/sh
set -eu

DB_HOST=${DB_HOST}
DB_USER=${POSTGRES_USER}
DB_PASSWORD=${POSTGRES_PASSWORD}
DB_NAME=${POSTGRES_DB}

echo "Waiting for database to be healthy..."
until pg_isready -h "$DB_HOST" -U "$DB_USER"; do
  sleep 2
done

echo "Terminating existing connections..."
export PGPASSWORD="$DB_PASSWORD"
psql -h "$DB_HOST" -U "$DB_USER" -d "postgres" -c "
  SELECT pid, pg_terminate_backend(pid)
  FROM pg_stat_activity
  WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid();" || true

echo "Dropping database if exists..."
PGPASSWORD="$DB_PASSWORD" dropdb -h "$DB_HOST" -U "$DB_USER" --if-exists "$DB_NAME"

echo "Creating database..."
PGPASSWORD="$DB_PASSWORD" createdb -h "$DB_HOST" -U "$DB_USER" "$DB_NAME"

echo "Starting restore..."
PGPASSWORD="$DB_PASSWORD" pg_restore \
  -h "$DB_HOST" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -j 4 \
  -v /mobypark.dump

echo "Restore complete!"
