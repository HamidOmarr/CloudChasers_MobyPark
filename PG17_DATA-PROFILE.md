## PostgreSQL 17 Data Profiling Script Documentation

This is a data profiling Bash script, created to analyse the PostgreSQL 17 database of the Cloud Chasers implementation of the MobyPark backend application, but should be suitable for standard PostgreSQL 17 setups.
The output is a machine readable JSON file.

## Note
This script is created using Generative AI and explicit prompt engineering. The base script was created using ChatGPT, whilst the refinement was done using Google Gemini.

## How to run
This how-to assumes the PostgreSQL 17 database is running inside a Docker container.
1. Download the `pg17_profile_json.sh` file.
2. Open a terminal.
3. Navigate to the folder in which the `pg17_profile_json.sh` file is installed within that terminal using `cd /path/to/folder`.
4. Use the command `docker cp pg17_profile_json.sh <YOUR_CONTAINER_NAME>:/pg17_profile_json.sh` to copy the script to the container. (NOTE: Replace YOUR_CONTAINER_NAME by your container name)
5. Use the command `docker exec -i <YOUR_CONTAINER_NAME> bash /pg17_profile_json.sh <YOUR_DB_NAME> <YOUR_USER_NAME> <YOUR_SCHEMA_NAME> /tmp/profiling.json` to run the script within the container. (NOTE: Replace YOUR_CONTAINER_NAME by your container name, YOUR_DB_NAME by the database name, YOUR_USER_NAME by the database owner, and YOUR_SCHEMA_NAME by the schema name.)
6. Use the command `docker cp EXAMPLE_CONTAINER:/tmp/profiling.json ./profiling.json` to get the generated JSON file onto the local machine.

## Example
```
"Transactions": {
  "row_count": 5787789,
  "columns": {
    "Id": {
      "ordinal_position": 1,
      "data_type": "uuid",
      "nullable": "NO",
      "null_count": 0,
      "distinct_count": 5787789,
      "constraints": {"is_primary_key" : true, "is_unique" : false, "is_foreign_key" : false, "foreign_table" : "Transactions", "foreign_column" : "Id"},
      "pg_stats": {"null_percentage" : 0, "n_distinct_estimate" : -1}
    },
    "Amount": {
      "ordinal_position": 2,
      "data_type": "numeric",
      "nullable": "NO",
      "null_count": 0,
      "distinct_count": 62212,
      "top_values": [{"value" : 14, "count" : 4272}, {"value" : 6.67, "count" : 3983}, {"value" : 3.6, "count" : 3922}, {"value" : 19.43, "count" : 3920}, {"value" : 10.08, "count" : 3795}],
      "numeric_stats": {"role" : "measure", "non_null_count" : 5787789, "min" : 1.3, "max" : 940.71, "avg" : 51.9358325588579680, "stddev" : 85.7289099160084785, "p25" : 10.79, "p50" : 20.79, "p75" : 36.29},
      "pg_stats": {"null_percentage" : 0, "n_distinct_estimate" : 11797}
    },
    "Method": {
      "ordinal_position": 3,
      "data_type": "text",
      "nullable": "NO",
      "null_count": 0,
      "distinct_count": 6,
      "text_stats": {"min_length" : 5, "max_length" : 10, "avg_length" : 8.00},
      "top_values": [{"value" : "applepay", "count" : 965651}, {"value" : "bancontact", "count" : 965592}, {"value" : "creditcard", "count" : 964974}, {"value" : "googlepay", "count" : 964307}, {"value" : "ideal", "count" : 964147}],
      "pg_stats": {"null_percentage" : 0, "n_distinct_estimate" : 6}
    },
    "Issuer": {
      "ordinal_position": 4,
      "data_type": "text",
      "nullable": "NO",
      "null_count": 0,
      "distinct_count": 5787780,
      "text_stats": {"min_length" : 8, "max_length" : 8, "avg_length" : 8.00},
      "top_values": [{"value" : "GW6TA3LX", "count" : 2}, {"value" : "B0MXVJWX", "count" : 2}, {"value" : "PKJNNTXA", "count" : 2}, {"value" : "4S95B4CL", "count" : 2}, {"value" : "9XYVO1TA", "count" : 2}],
      "pg_stats": {"null_percentage" : 0, "n_distinct_estimate" : -1}
    },
    "Bank": {
      "ordinal_position": 5,
      "data_type": "text",
      "nullable": "NO",
      "null_count": 0,
      "distinct_count": 11,
      "text_stats": {"min_length" : 3, "max_length" : 12, "avg_length" : 6.36},
      "top_values": [{"value" : "Volksbank", "count" : 527383}, {"value" : "Bunq", "count" : 526699}, {"value" : "ASN", "count" : 526546}, {"value" : "Knab", "count" : 526503}, {"value" : "Triodos", "count" : 526240}],
      "pg_stats": {"null_percentage" : 0, "n_distinct_estimate" : 11}
    }
  }
}
```