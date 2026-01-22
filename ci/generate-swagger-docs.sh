#!/bin/bash
set -e

CI_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

WORK_DIR="$CI_DIR/../MobyPark/OpenAPI"

echo "Changing working directory to $WORK_DIR"
cd "$WORK_DIR"

echo "Bootstrapping pip..."
curl -sS https://bootstrap.pypa.io/get-pip.py -o get-pip.py

# Install pip to local user directory
python3 get-pip.py --user

echo "Installing dependencies..."
python3 -m pip install pyyaml --user --quiet --disable-pip-version-check

# Clean up installer
rm get-pip.py

echo "Fetching Swagger JSON from API..."
curl -k -f \
  --retry 15 \
  --retry-delay 5 \
  --retry-connrefused \
  "https://localhost:8501/swagger/v1/swagger.json" \
  -o swagger.json

echo "Generating Swagger documentation..."
python generate_swagger_docs.py

echo "Documentation generated successfully in $WORK_DIR/src"