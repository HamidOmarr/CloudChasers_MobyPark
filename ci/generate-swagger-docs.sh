#!/bin/bash
set -e

CI_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# Location of the .csproj
PROJECT_DIR="$CI_DIR/../MobyPark"
# Location where the python script resides
WORK_DIR="$CI_DIR/../MobyPark/OpenAPI"

# We use the absolute path to ensure .NET finds it regardless of the working dir
CERT_PATH="$(cd "$CI_DIR/../certs" && pwd)/selfsigned.pfx"

echo "Changing working directory to $WORK_DIR"
cd "$WORK_DIR"

# Install Python Dependencies
echo "Bootstrapping pip..."
curl -sS https://bootstrap.pypa.io/get-pip.py -o get-pip.py
python3 get-pip.py --user
rm get-pip.py

echo "Installing PyYAML..."
python3 -m pip install pyyaml --user --quiet --disable-pip-version-check

# Start the API in Background
echo "Starting API in background for documentation generation..."

# Verify the password exists (passed from YAML)
if [ -z "$CERT_PASSWORD" ]; then
  echo "Error: CERT_PASSWORD environment variable is missing."
  exit 1
fi

# Configure .NET to use the specific certificate and password
export Https__CertificatePath="$CERT_PATH"
export Https__CertificatePassword="$CERT_PASSWORD"
export ASPNETCORE_URLS="https://localhost:8578"

# Start the dotnet app in background
dotnet run --project "$PROJECT_DIR/MobyPark.csproj" > /dev/null 2>&1 &
API_PID=$!

echo "Waiting for API to start (PID: $API_PID)..."

# Loop to wait for the API to become available
for i in {1..30}; do
    if curl -k -s "https://localhost:8578/swagger/v1/swagger.json" > /dev/null; then
        echo "API is up!"
        break
    fi
    if ! kill -0 $API_PID 2>/dev/null; then
        echo "API process died unexpectedly."
        exit 1
    fi
    echo "Waiting..."
    sleep 2
done

# Fetch and Generate Documentation
echo "Fetching Swagger JSON from API..."
curl -k -s "https://localhost:8578/swagger/v1/swagger.json" -o swagger.json

echo "Generating Swagger documentation..."
python3 generate_swagger_docs.py

# Cleanup
echo "Stopping background API..."
kill $API_PID

echo "Documentation generated successfully in $WORK_DIR/src"