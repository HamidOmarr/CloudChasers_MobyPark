#!/usr/bin/env bash
set -euo pipefail

base_sha="${BASE_SHA:-}"
head_sha="${HEAD_SHA:-${GITHUB_SHA:-}}"

if [[ -z "$head_sha" ]]; then
  echo "HEAD_SHA is required" >&2
  exit 2
fi

repo_root=$(pwd)
artifacts_root="$repo_root/artifacts"
mkdir -p "$artifacts_root"

append() {
  if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    echo "$1" >> "$GITHUB_STEP_SUMMARY"
  else
    echo "$1"
  fi
}

run_suite() {
  local workdir="$1"
  local outdir="$2"
  local nobuild="$3"

  pushd "$workdir" >/dev/null
  mkdir -p "$outdir"

  if [[ "$nobuild" == "1" ]]; then
    bash "$repo_root/ci/test-and-coverage.sh" --out "$outdir" --no-build
  else
    bash "$repo_root/ci/test-and-coverage.sh" --out "$outdir"
  fi

  popd >/dev/null
}

if [[ -z "$base_sha" ]]; then
  base_sha=$(git rev-parse "${head_sha}^" 2>/dev/null || true)
fi

base_dir=""
if [[ -n "$base_sha" ]]; then
  git fetch --no-tags --prune --depth=0 origin "+${base_sha}:${base_sha}" >/dev/null 2>&1 || true
  if git cat-file -e "${base_sha}^{commit}" 2>/dev/null; then
    base_dir=$(mktemp -d)
    git -c core.hooksPath=/dev/null worktree add --detach "$base_dir" "$base_sha" >/dev/null
  fi
fi

head_out="$artifacts_root/head"
run_suite "$repo_root" "$head_out" 1 || true

retry_needed=0
if [[ -f "$head_out/coverage.cobertura.xml" ]]; then
  lines_valid=$(grep -m1 '<coverage' "$head_out/coverage.cobertura.xml" | sed -n 's/.*lines-valid="\([0-9][0-9]*\)".*/\1/p')
  if [[ "${lines_valid:-0}" == "0" ]]; then
    retry_needed=1
  fi
fi

if [[ "$retry_needed" -eq 1 ]]; then
  run_suite "$repo_root" "$head_out" 0 || true
fi

if [[ -f "$head_out/test.log" ]]; then
  cp -f "$head_out/test.log" "$repo_root/test.log"
fi

base_out="$artifacts_root/base"
base_ran=0
if [[ -n "$base_dir" ]]; then
  run_suite "$base_dir" "$base_out" 0 || true
  base_ran=1
fi

source "$head_out/summary.env"
head_total="$TOTAL"; head_passed="$PASSED"; head_failed="$FAILED"; head_skipped="$SKIPPED"; head_cov="$COVERAGE_PERCENT"; head_exit="$EXIT_CODE"

base_total=""; base_passed=""; base_failed=""; base_skipped=""; base_cov=""
if [[ "$base_ran" -eq 1 && -f "$base_out/summary.env" ]]; then
  source "$base_out/summary.env"
  base_total="$TOTAL"; base_passed="$PASSED"; base_failed="$FAILED"; base_skipped="$SKIPPED"; base_cov="$COVERAGE_PERCENT"
fi

append "### Test results & coverage"
append "- Tests: Total: $head_total | Passed: $head_passed | Failed: $head_failed | Skipped: $head_skipped"

if [[ -n "$head_cov" ]]; then
  append "- Coverage: ${head_cov}%"
else
  append "- Coverage: (not available)"
fi

if [[ "$base_ran" -eq 1 && -n "$base_cov" && -n "$head_cov" ]]; then
  delta=$(awk -v h="$head_cov" -v b="$base_cov" 'BEGIN { printf "%.2f", (h-b) }')
  if awk -v d="$delta" 'BEGIN { exit !(d < 0) }'; then
    append "- Coverage delta: ${delta}% (decreased)"
  else
    append "- Coverage delta: ${delta}% (increased/unchanged)"
  fi
else
  append "- Coverage delta: (not available)"
fi

if [[ -n "$base_dir" ]]; then
  git -c core.hooksPath=/dev/null worktree remove --force "$base_dir" >/dev/null 2>&1 || true
fi

exit "$head_exit"
