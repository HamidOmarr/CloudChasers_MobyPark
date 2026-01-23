#!/usr/bin/env bash
set -euo pipefail

DB="$1"
USER="$2"
SCHEMA="$3"
OUTFILE="$4"

if [[ -z "$DB" || -z "$USER" || -z "$SCHEMA" || -z "$OUTFILE" ]]; then
  echo "Usage: $0 <database> <user> <schema> <output.json>"
  exit 1
fi

PSQL=(psql -X -U "$USER" -d "$DB" -v ON_ERROR_STOP=1 -qAt)

# Start JSON object with Metadata
echo "{" > "$OUTFILE"
echo "  \"metadata\": {" >> "$OUTFILE"
echo "    \"generated_at\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\"," >> "$OUTFILE"
echo "    \"database\": \"$DB\"," >> "$OUTFILE"
echo "    \"schema\": \"$SCHEMA\"" >> "$OUTFILE"
echo "  }," >> "$OUTFILE"
echo "  \"tables\": {" >> "$OUTFILE"

first_table=true

# ---------- Tables Loop ----------
while IFS= read -r t; do
  [[ "$first_table" == true ]] || echo "," >> "$OUTFILE"
  first_table=false

  echo "\"$t\": {" >> "$OUTFILE"

  # Get Row Count
  row_count="$("${PSQL[@]}" -c "SELECT COUNT(*) FROM \"$SCHEMA\".\"$t\";")"
  echo "  \"row_count\": $row_count," >> "$OUTFILE"
  echo "  \"columns\": {" >> "$OUTFILE"

  first_col=true

  while IFS='|' read -r pos name type nullable; do
    [[ "$first_col" == true ]] || echo "," >> "$OUTFILE"
    first_col=false

    # Basic Counts
    null_count="$("${PSQL[@]}" -c \
      "SELECT COUNT(*) FROM \"$SCHEMA\".\"$t\" WHERE \"$name\" IS NULL;")"

    distinct_count="$("${PSQL[@]}" -c \
      "SELECT COUNT(DISTINCT \"$name\") FROM \"$SCHEMA\".\"$t\";")"

    echo "    \"$name\": {" >> "$OUTFILE"
    echo "      \"ordinal_position\": $pos," >> "$OUTFILE"
    echo "      \"data_type\": \"$type\"," >> "$OUTFILE"
    echo "      \"nullable\": \"$nullable\"," >> "$OUTFILE"
    echo "      \"null_count\": $null_count," >> "$OUTFILE"
    echo "      \"distinct_count\": $distinct_count," >> "$OUTFILE"

    # 1. Constraints (PK / FK / UNIQUE)
    # UPDATED: Now checks for UNIQUE constraints as well
    constraint_info="$("${PSQL[@]}" -c "
      SELECT json_build_object(
        'is_primary_key', BOOL_OR(tc.constraint_type = 'PRIMARY KEY'),
        'is_unique',      BOOL_OR(tc.constraint_type = 'UNIQUE'),
        'is_foreign_key', BOOL_OR(tc.constraint_type = 'FOREIGN KEY'),
        'foreign_table',  MAX(ccu.table_name),
        'foreign_column', MAX(ccu.column_name)
      )
      FROM information_schema.key_column_usage kcu
      JOIN information_schema.table_constraints tc 
        ON kcu.constraint_name = tc.constraint_name 
        AND kcu.table_schema = tc.table_schema
      LEFT JOIN information_schema.constraint_column_usage ccu 
        ON tc.constraint_name = ccu.constraint_name 
        AND tc.table_schema = ccu.table_schema
      WHERE kcu.table_schema = '$SCHEMA'
        AND kcu.table_name = '$t'
        AND kcu.column_name = '$name'
      GROUP BY kcu.column_name;
    ")"

    if [[ -n "$constraint_info" && "$constraint_info" != "null" ]]; then
      echo "      \"constraints\": $constraint_info," >> "$OUTFILE"
    fi

    # 2. ENUM Values (New!)
    # Only runs if type is USER-DEFINED
    if [[ "$type" == "USER-DEFINED" ]]; then
       enum_values="$("${PSQL[@]}" -c "
         SELECT json_agg(e.enumlabel)
         FROM pg_type t
         JOIN pg_enum e ON t.oid = e.enumtypid
         JOIN pg_catalog.pg_namespace n ON n.oid = t.typnamespace
         WHERE t.typname = (
            SELECT udt_name 
            FROM information_schema.columns 
            WHERE table_schema = '$SCHEMA' AND table_name = '$t' AND column_name = '$name'
         );
       ")"
       
       if [[ -n "$enum_values" && "$enum_values" != "null" ]]; then
         echo "      \"enum_values\": $enum_values," >> "$OUTFILE"
       fi
    fi

    # 3. Numeric Stats + Top 5
    if [[ "$type" =~ ^(smallint|integer|bigint|numeric|real|double\ precision)$ ]]; then
      role="measure"
      if (( row_count > 0 )) && awk "BEGIN {exit !($distinct_count/$row_count >= 0.7)}"; then
        role="identifier"
      fi

      if [[ "$role" == "identifier" ]]; then
        stats="$("${PSQL[@]}" -c "
          SELECT json_build_object(
            'role', 'identifier',
            'min', MIN(\"$name\"),
            'max', MAX(\"$name\")
          )
          FROM \"$SCHEMA\".\"$t\";
        ")"
      else
        stats="$("${PSQL[@]}" -c "
          SELECT json_build_object(
            'role', 'measure',
            'non_null_count', COUNT(\"$name\"),
            'min', MIN(\"$name\"),
            'max', MAX(\"$name\"),
            'avg', AVG(\"$name\"),
            'stddev', STDDEV(\"$name\"),
            'p25', percentile_cont(0.25) WITHIN GROUP (ORDER BY \"$name\"),
            'p50', percentile_cont(0.50) WITHIN GROUP (ORDER BY \"$name\"),
            'p75', percentile_cont(0.75) WITHIN GROUP (ORDER BY \"$name\")
          )
          FROM \"$SCHEMA\".\"$t\";
        ")"

        if awk "BEGIN {exit !($distinct_count/$row_count < 0.7)}"; then
          top_values_numeric="$("${PSQL[@]}" -c "
            SELECT COALESCE(
              json_agg(json_build_object('value', val, 'count', cnt)),
              '[]'::json
            )
            FROM (
              SELECT \"$name\" AS val, COUNT(*) AS cnt
              FROM \"$SCHEMA\".\"$t\"
              GROUP BY \"$name\"
              ORDER BY cnt DESC
              LIMIT 5
            ) s;
          ")"
          echo "      \"top_values\": $top_values_numeric," >> "$OUTFILE"
        fi
      fi
      echo "      \"numeric_stats\": $stats," >> "$OUTFILE"
    fi

    # 4. Text Stats
    if [[ "$type" =~ ^(text|character\ varying|character)$ ]]; then
      text_stats="$("${PSQL[@]}" -c "
        SELECT json_build_object(
          'min_length', MIN(LENGTH(\"$name\")),
          'max_length', MAX(LENGTH(\"$name\")),
          'avg_length', ROUND(AVG(LENGTH(\"$name\"))::numeric, 2)
        )
        FROM \"$SCHEMA\".\"$t\";
      ")"
      echo "      \"text_stats\": $text_stats," >> "$OUTFILE"

      top_values="$("${PSQL[@]}" -c "
        SELECT COALESCE(
          json_agg(json_build_object('value', val, 'count', cnt)),
          '[]'::json
        )
        FROM (
          SELECT \"$name\" AS val, COUNT(*) AS cnt
          FROM \"$SCHEMA\".\"$t\"
          WHERE \"$name\" IS NOT NULL
          GROUP BY \"$name\"
          ORDER BY cnt DESC
          LIMIT 5
        ) s;
      ")"
      echo "      \"top_values\": $top_values," >> "$OUTFILE"
    fi

    # 5. Date Stats
    if [[ "$type" =~ ^(date|timestamp|timestamp\ with\ time\ zone)$ ]]; then
      date_stats="$("${PSQL[@]}" -c "
        SELECT json_build_object(
          'min_value', MIN(\"$name\"),
          'max_value', MAX(\"$name\")
        )
        FROM \"$SCHEMA\".\"$t\";
      ")"
      echo "      \"date_stats\": $date_stats," >> "$OUTFILE"
    fi

    # 6. Boolean Stats
    if [[ "$type" == "boolean" ]]; then
      bool_stats="$("${PSQL[@]}" -c "
        SELECT json_build_object(
          'true_count', COUNT(*) FILTER (WHERE \"$name\" = true),
          'false_count', COUNT(*) FILTER (WHERE \"$name\" = false)
        )
        FROM \"$SCHEMA\".\"$t\";
      ")"
      echo "      \"boolean_stats\": $bool_stats," >> "$OUTFILE"
    fi

    # 7. pg_stats
    pg_stats="$("${PSQL[@]}" -c "
      SELECT json_build_object(
        'null_percentage', null_frac * 100,
        'n_distinct_estimate', n_distinct
      )
      FROM pg_stats
      WHERE schemaname = '$SCHEMA'
        AND tablename  = '$t'
        AND attname    = '$name';
    ")"

    if [[ -n "$pg_stats" ]]; then
      echo "      \"pg_stats\": $pg_stats," >> "$OUTFILE"
    fi

    sed -i '$ s/,$//' "$OUTFILE"
    echo "    }" >> "$OUTFILE"

  done < <("${PSQL[@]}" <<SQL
    SELECT ordinal_position, column_name, data_type, is_nullable
    FROM information_schema.columns
    WHERE table_schema = '$SCHEMA'
      AND table_name   = '$t'
    ORDER BY ordinal_position;
SQL
)

  echo "  }" >> "$OUTFILE"
  echo "}" >> "$OUTFILE"

done < <("${PSQL[@]}" <<SQL
  SELECT table_name
  FROM information_schema.tables
  WHERE table_schema = '$SCHEMA'
    AND table_type   = 'BASE TABLE'
  ORDER BY table_name;
SQL
)

# Close Tables object and Metadata object
echo "  }" >> "$OUTFILE" 
echo "}" >> "$OUTFILE"