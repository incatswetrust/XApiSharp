#!/usr/bin/env bash
# Single documented entry point for the release-candidate sequence (spec section 22.2):
# restore -> generation guard-check -> endpoint coverage gate -> build -> test (with coverage) ->
# branch coverage gate -> pack -> hash artifacts -> release manifest -> package consumer test.
#
# Checkout of the exact commit/tag being released is the CALLER's responsibility (the release
# workflow does this before invoking this script; for a local dry run, just check out the ref
# yourself first) - this script never touches git itself, and always operates on whatever the
# working directory currently contains. It verifies that matches the version you're asserting
# (Directory.Build.props' <Version>) before doing anything else, so a forgotten version bump or a
# release run against the wrong commit fails immediately instead of silently packing the wrong
# thing (spec 22.2: "does not publish an arbitrary state of the working directory").
#
# Exits non-zero on any failure (set -e) - there is no step here that's allowed to fail silently.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

EXPECTED_VERSION="${1:?Usage: eng/release.sh <expected-version> [commit] [tag] [ci-run-url]}"
COMMIT="${2:-$(git rev-parse HEAD)}"
TAG="${3:-}"
CI_RUN_URL="${4:-}"

CONFIGURATION="${CONFIGURATION:-Release}"
ARTIFACTS_DIR="${ARTIFACTS_DIR:-./artifacts}"

ACTUAL_VERSION=$(grep -o '<Version>[^<]*</Version>' Directory.Build.props | sed -E 's/<\/?Version>//g')
if [[ "$ACTUAL_VERSION" != "$EXPECTED_VERSION" ]]; then
  echo "Version mismatch: Directory.Build.props has '$ACTUAL_VERSION', expected '$EXPECTED_VERSION'." >&2
  echo "Either the version wasn't bumped, or this is being run against the wrong commit." >&2
  exit 1
fi

rm -rf "$ARTIFACTS_DIR"
mkdir -p "$ARTIFACTS_DIR/packages"

echo "== restore (locked mode) =="
dotnet restore --locked-mode

echo "== tool restore =="
dotnet tool restore

echo "== generation guard-check (GEN-05/GEN-08/GEN-09) =="
dotnet run --project tools/XApiSharp.CodeGen -- guard-check

echo "== endpoint coverage-matrix gate =="
dotnet run --project tools/XApiSharp.CodeGen -- coverage-check

echo "== build ($CONFIGURATION) =="
dotnet build --configuration "$CONFIGURATION" --no-restore -p:ContinuousIntegrationBuild=true

echo "== test with coverage =="
dotnet test tests/XApiSharp.UnitTests/XApiSharp.UnitTests.csproj --configuration "$CONFIGURATION" --no-build --collect:"XPlat Code Coverage" --results-directory "$ARTIFACTS_DIR/coverage/unit"
dotnet test tests/XApiSharp.ContractTests/XApiSharp.ContractTests.csproj --configuration "$CONFIGURATION" --no-build --collect:"XPlat Code Coverage" --results-directory "$ARTIFACTS_DIR/coverage/contract"
dotnet test tests/XApiSharp.IntegrationTests/XApiSharp.IntegrationTests.csproj --configuration "$CONFIGURATION" --no-build
dotnet test tests/XApiSharp.SoakTests/XApiSharp.SoakTests.csproj --configuration "$CONFIGURATION" --no-build

echo "== branch coverage gate (spec 19.5, 80% hand-written core) =="
dotnet run --project tools/XApiSharp.CodeGen -- branch-coverage-check --unit "$ARTIFACTS_DIR/coverage/unit" --contract "$ARTIFACTS_DIR/coverage/contract" \
  | tee "$ARTIFACTS_DIR/branch-coverage.txt"
BRANCH_COVERAGE_PERCENT=$(grep -oE '^Total \(core\): [0-9.]+' "$ARTIFACTS_DIR/branch-coverage.txt" | grep -oE '[0-9.]+$')

echo "== pack (both packages, same version, ContinuousIntegrationBuild) =="
dotnet pack src/XApiSharp/XApiSharp.csproj --configuration "$CONFIGURATION" --no-build --output "$ARTIFACTS_DIR/packages" -p:ContinuousIntegrationBuild=true
dotnet pack src/XApiSharp.Extensions.DependencyInjection/XApiSharp.Extensions.DependencyInjection.csproj --configuration "$CONFIGURATION" --no-build --output "$ARTIFACTS_DIR/packages" -p:ContinuousIntegrationBuild=true

echo "== hash artifacts (SHA-256) =="
dotnet run --project tools/XApiSharp.CodeGen -- hash-artifacts --dir "$ARTIFACTS_DIR/packages" --output "$ARTIFACTS_DIR/SHA256SUMS.txt"

echo "== release manifest =="
dotnet run --project tools/XApiSharp.CodeGen -- release-manifest \
  --version "$EXPECTED_VERSION" \
  --commit "$COMMIT" \
  --tag "$TAG" \
  --ci-run-url "$CI_RUN_URL" \
  --branch-coverage-percent "$BRANCH_COVERAGE_PERCENT" \
  --output "$ARTIFACTS_DIR/release-manifest.json" \
  --repo-root .

echo "== package consumer test (spec 22.3 - packed output, not source) =="
"$(dirname "${BASH_SOURCE[0]}")/package-consumer-test.sh" "$ARTIFACTS_DIR/packages" "$EXPECTED_VERSION"

echo ""
echo "Release candidate $EXPECTED_VERSION ready. Artifacts in $ARTIFACTS_DIR:"
find "$ARTIFACTS_DIR/packages" -maxdepth 1 -type f -name "*.nupkg" -o -name "*.snupkg" 2>/dev/null
