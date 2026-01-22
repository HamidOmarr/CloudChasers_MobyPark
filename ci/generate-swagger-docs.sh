#!/bin/bash
set -e

CI_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

WORK_DIR="$CI_DIR/../MobyPark/OpenAPI"

echo "Changing working directory to $WORK_DIR"
cd "$WORK_DIR"

if [ ! -d "venv" ]; then
    echo "Creating virtual environment..."
    python3 -m venv venv
fi

source venv/bin/activate

echo "Installing dependencies..."
pip install pyyaml --quiet --disable-pip-version-check

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