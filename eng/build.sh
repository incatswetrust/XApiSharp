#!/usr/bin/env bash
# Plain local dev loop: restore -> build -> test -> pack. For an actual release candidate
# (version check, coverage gates, artifact hashing, release manifest, package-consumer test), use
# eng/release.sh instead - see docs/releasing.md.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

CONFIGURATION="${CONFIGURATION:-Release}"

echo "== restore =="
dotnet restore --locked-mode

echo "== build ($CONFIGURATION) =="
dotnet build --configuration "$CONFIGURATION" --no-restore

echo "== test =="
dotnet test --configuration "$CONFIGURATION" --no-build

echo "== pack =="
dotnet pack --configuration "$CONFIGURATION" --no-build --output ./artifacts
