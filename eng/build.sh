#!/usr/bin/env bash
# Single documented entry point for restore -> build -> test -> pack (spec section 22.2).
# Release-candidate specific steps (generation check, package-consumer test, artifact
# hashing) are added here as those stages (E1/E8) land.
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
