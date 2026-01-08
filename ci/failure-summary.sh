#!/usr/bin/env bash
set -e

reason=""
fix=""

if [[ "$GITHUB_STEP_FORMAT_OUTCOME" == "failure" ]]; then
  reason="Code formatting rules are violated."
  fix="Run \`dotnet format\` and commit the changes."
elif [[ "$GITHUB_STEP_BUILD_OUTCOME" == "failure" ]]; then
  reason="Build errors or warnings treated as errors."
  fix="Resolve compiler warnings/errors locally."
elif [[ "$GITHUB_STEP_TEST_OUTCOME" == "failure" ]]; then
  reason="One or more tests failed."
  fix="Run \`dotnet test\` and fix failing tests."
fi

if [[ -n "$reason" ]]; then
  echo "### CI Failure" >> "$GITHUB_STEP_SUMMARY"
  echo "" >> "$GITHUB_STEP_SUMMARY"
  echo "**Reason:** $reason" >> "$GITHUB_STEP_SUMMARY"
  echo "**Fix:** $fix" >> "$GITHUB_STEP_SUMMARY"
fi
