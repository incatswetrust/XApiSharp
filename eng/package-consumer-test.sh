#!/usr/bin/env bash
# Spec section 22.3: pre-publish consumer check - a project outside XApiSharp.slnx, installing the
# exact version under test from the local pack output (never a ProjectReference to src/), then
# compiling and running read/error/pagination/cancellation plus a separate DI-registration check.
#
# Usage: eng/package-consumer-test.sh <packages-dir> <version>
# Normally invoked by eng/release.sh with that run's own artifacts/packages and version - call it
# directly only for a local dry run against an already-packed output.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

PACKAGES_DIR="${1:?Usage: eng/package-consumer-test.sh <packages-dir> <version>}"
VERSION="${2:?Usage: eng/package-consumer-test.sh <packages-dir> <version>}"
PROJECT="tests/XApiSharp.PackageTests/XApiSharp.PackageTests.csproj"

# On Windows, Git Bash's plain `pwd` returns a POSIX-style path (e.g. /d/a/XApiSharp/...) that
# .NET's own path resolution does not understand the same way (it was observed to misparse it as
# C:\d\a\XApiSharp\...) when embedded as a nuget.config source value - `pwd -W` gives the
# Windows-style equivalent (D:/a/XApiSharp/...) instead. Real POSIX systems don't have `-W`.
if [[ "${OS:-}" == "Windows_NT" ]]; then
  ABS_PACKAGES_DIR="$(cd "$PACKAGES_DIR" && pwd -W)"
else
  ABS_PACKAGES_DIR="$(cd "$PACKAGES_DIR" && pwd)"
fi

# The repo's own NuGet.config maps every package pattern to nuget.org only (dependency-confusion
# hardening) - a plain `--source` doesn't participate in that mapping and is silently ignored, so
# a temporary, isolated config is generated here instead: nuget.org for everything, plus an
# explicit mapping for exactly the two packages under test to the local pack output. Never reused
# outside this script, never committed.
TEMP_CONFIG="$(mktemp -t xapisharp-package-test-nuget-XXXXXX).config"
trap 'rm -f "$TEMP_CONFIG"' EXIT

cat > "$TEMP_CONFIG" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-under-test" value="$ABS_PACKAGES_DIR" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-under-test">
      <package pattern="XApiSharp.Net" />
      <package pattern="XApiSharp.Net.Extensions.DependencyInjection" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

echo "== restoring XApiSharp.PackageTests against packed output $VERSION from $ABS_PACKAGES_DIR =="
dotnet restore "$PROJECT" --configfile "$TEMP_CONFIG" -p:PackageUnderTestVersion="$VERSION"

echo "== building (no ProjectReference to src/ - this compiles against the packed assembly only) =="
dotnet build "$PROJECT" --configuration Release --no-restore -p:PackageUnderTestVersion="$VERSION"

echo "== running consumer scenario + DI registration tests =="
dotnet test "$PROJECT" --configuration Release --no-build
