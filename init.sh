#!/bin/bash
set -e

DB_NAME="mobypark"
DB_USER="MobyParkAdmin"
DB_PASS="MobyParkCloudChasers"
DUMP_FILE="/docker-entrypoint-initdb.d/mobypark.dump"

export PGPASSWORD=$DB_PASS

# Drop DB if exists
psql -U $DB_USER -d postgres -c "DROP DATABASE IF EXISTS $DB_NAME;"

# Create fresh DB
psql -U $DB_USER -d postgres -c "CREATE DATABASE $DB_NAME;"

# Restore dump
pg_restore -U $DB_USER -d $DB_NAME -v $DUMP_FILE
