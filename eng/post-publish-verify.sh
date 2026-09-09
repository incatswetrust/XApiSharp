#!/usr/bin/env bash
# Spec section 23.3: post-publish verification against the REAL NuGet.org feed - a fresh, separate
# package cache (never a locally-primed one), installing the exact published version, then
# restore/compile/run of the same consumer scenario tests/XApiSharp.PackageTests uses pre-publish.
#
# This automates the restore/compile/run part of spec 23.3. The remaining checks it lists -
# the nuget.org package page rendering, README, declared dependencies, license, SourceLink, and
# symbols - are a human look at the actual published page, not something worth faking an automated
# check for; this script prints a reminder rather than pretending to verify them.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

VERSION="${1:?Usage: eng/post-publish-verify.sh <version>}"
PROJECT="tests/XApiSharp.PackageTests/XApiSharp.PackageTests.csproj"

NUGET_PACKAGES="$(mktemp -d -t xapisharp-post-publish-cache-XXXXXX)"
export NUGET_PACKAGES
trap 'rm -rf "$NUGET_PACKAGES"' EXIT

echo "== restoring against the real NuGet.org feed (fresh, isolated cache: $NUGET_PACKAGES) =="
dotnet restore "$PROJECT" --source https://api.nuget.org/v3/index.json -p:PackageUnderTestVersion="$VERSION"

echo "== building =="
dotnet build "$PROJECT" --configuration Release --no-restore -p:PackageUnderTestVersion="$VERSION"

echo "== running consumer scenario + DI registration tests against the published package =="
dotnet test "$PROJECT" --configuration Release --no-build

cat <<EOF

Automated restore/compile/run checks passed for $VERSION.

Still confirm by hand (spec 23.3, not automated here):
  - https://www.nuget.org/packages/XApiSharp.Net/$VERSION
  - https://www.nuget.org/packages/XApiSharp.Net.Extensions.DependencyInjection/$VERSION
  README rendering, declared dependencies, license, SourceLink, and symbols for both packages.
EOF
