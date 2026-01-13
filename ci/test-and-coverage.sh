#!/usr/bin/env bash
set -euo pipefail

out_dir=""
no_build=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --out)
      out_dir="$2"; shift 2 ;;
    --no-build)
      no_build=1; shift ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ -z "$out_dir" ]]; then
  echo "--out is required" >&2
  exit 2
fi

mkdir -p "$out_dir"
results_dir="$out_dir/TestResults"
mkdir -p "$results_dir"

log_file="$out_dir/test.log"
trx_name="test_results.trx"

target=""
if [[ -f "MobyPark.sln" ]]; then
  target="MobyPark.sln"
fi

if [[ -n "$target" ]]; then
  dotnet restore "$target" >/dev/null
else
  dotnet restore >/dev/null
fi

args=(
  test
  $target
  --verbosity minimal
  --no-restore
  --logger "trx;LogFileName=$trx_name"
  --results-directory "$results_dir"
  --collect:"XPlat Code Coverage"
)

if [[ "$no_build" -eq 1 ]]; then
  args+=(--no-build)
fi

args+=(-- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura)

set +e
dotnet "${args[@]}" 2>&1 | tee "$log_file"
exit_code=${PIPESTATUS[0]}
set -e

trx_file=$(find "$results_dir" -maxdepth 3 -type f -name "$trx_name" -print -quit || true)
if [[ -n "$trx_file" ]]; then
  cp -f "$trx_file" "$out_dir/test_results.trx"
fi

coverage_file=$(find "$results_dir" -maxdepth 5 -type f -name 'coverage.cobertura.xml' -print -quit || true)
if [[ -z "$coverage_file" ]]; then
  coverage_file=$(find "$results_dir" -maxdepth 5 -type f -name '*.cobertura.xml' -print -quit || true)
fi

if [[ -n "$coverage_file" ]]; then
  cp -f "$coverage_file" "$out_dir/coverage.cobertura.xml"
  coverage_file="$out_dir/coverage.cobertura.xml"
fi

total="0"; passed="0"; failed="0"; skipped="0"

summary_line=$(grep -E "Total tests: [0-9]+" "$log_file" | tail -n 1 || true)
if [[ -n "$summary_line" ]]; then
  total=$(echo "$summary_line" | sed -n 's/.*Total tests: \([0-9][0-9]*\).*/\1/p')
  passed=$(echo "$summary_line" | sed -n 's/.*Passed: \([0-9][0-9]*\).*/\1/p')
  failed=$(echo "$summary_line" | sed -n 's/.*Failed: \([0-9][0-9]*\).*/\1/p')
  skipped=$(echo "$summary_line" | sed -n 's/.*Skipped: \([0-9][0-9]*\).*/\1/p')
fi

if [[ "$total" == "0" ]]; then
  summary_line=$(grep -E "(Passed!|Failed!).*Failed:.*Passed:.*Skipped:.*Total:" "$log_file" | tail -n 1 || true)
  if [[ -n "$summary_line" ]]; then
    total=$(echo "$summary_line" | sed -n 's/.*Total:[^0-9]*\([0-9][0-9]*\).*/\1/p')
    passed=$(echo "$summary_line" | sed -n 's/.*Passed:[^0-9]*\([0-9][0-9]*\).*/\1/p')
    failed=$(echo "$summary_line" | sed -n 's/.*Failed:[^0-9]*\([0-9][0-9]*\).*/\1/p')
    skipped=$(echo "$summary_line" | sed -n 's/.*Skipped:[^0-9]*\([0-9][0-9]*\).*/\1/p')
  fi
fi

total=${total:-0}; passed=${passed:-0}; failed=${failed:-0}; skipped=${skipped:-0}

coverage_percent=""
if [[ -n "$coverage_file" && -f "$coverage_file" ]]; then
  line_rate=$(grep -m1 -E '<coverage[^>]*line-rate="[0-9]+(\.[0-9]+)?"' "$coverage_file" | sed -n 's/.*line-rate="\([0-9][0-9]*\(\.[0-9][0-9]*\)\?\)".*/\1/p')
  if [[ -n "$line_rate" ]]; then
    coverage_percent=$(awk -v r="$line_rate" 'BEGIN { printf "%.2f", (r*100.0) }')
  fi
fi

cat > "$out_dir/summary.env" <<EOF
TOTAL=$total
PASSED=$passed
FAILED=$failed
SKIPPED=$skipped
COVERAGE_PERCENT=$coverage_percent
EXIT_CODE=$exit_code
EOF

exit "$exit_code"
