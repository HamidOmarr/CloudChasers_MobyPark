#!/usr/bin/env bash

# This script summarizes the failures from the CI logs.

set -env

echo "==================== CI FAILURE SUMMARY ====================" >> ${GITHUB_STEP_SUMMARY}
echo "" >> ${GITHUB_STEP_SUMMARY}

reason=""
fix=""

if [[ "${GITHUB_STEP_FORMAT_OUTCOME}" == "failure" ]]; then
  reason="Code formatting rules are violated."
  fix="Run \`dotnet format\` and commit the changes."
elif [[ "${GITHUB_STEP_BUILD_OUTCOME}" == "failure" ]]; then
  reason="Build errors or warnings treated as errors."
  fix="Resolve compiler warnings/errors locally."
elif [[ "${GITHUB_STEP_TEST_OUTCOME}" == "failure" ]]; then
  reason="One or more tests failed."
  fix="Run \`dotnet test\` and fix failing tests."
fi

# Only write if we have a reason
if [[ -n "$reason" ]]; then
  echo "**Reason:** $reason" >> "$GITHUB_STEP_SUMMARY"
  echo "**Fix:** $fix" >> "$GITHUB_STEP_SUMMARY"
fi
