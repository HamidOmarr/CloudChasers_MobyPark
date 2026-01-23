#!/bin/sh
set -eu

# Set up variables
HOST_BACKUP_DIR="$HOME/CloudChasers_MobyPark/backups"
TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
BACKUP_FILENAME="mobypark_${TIMESTAMP}.dump"

mkdir -p "$HOST_BACKUP_DIR"

# Load environment variables from .env file
export $(grep -v '^#' .env | xargs)

echo "Starting backup process for $TIMESTAMP..."

docker compose exec -u postgres mobypark-db \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /tmp/temp_backup.dump

CONTAINER=$(docker compose ps -q mobypark-db)
docker cp "$CONTAINER":/tmp/temp_backup.dump "$HOST_BACKUP_DIR/$BACKUP_FILENAME"

docker compose exec -u postgres mobypark-db rm /tmp/temp_backup.dump

echo "Backup saved to: $HOST_BACKUP_DIR/$BACKUP_FILENAME"

echo "Cleaning up backups older than 7 days..."
find "$HOST_BACKUP_DIR" -type f -name "mobypark_*.dump" -mtime +7 -delete

echo "Backup process complete."