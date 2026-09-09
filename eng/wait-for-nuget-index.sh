#!/usr/bin/env bash
# Spec section 23.2: "poll (bounded, not indefinite) until the version is indexed and
# installable" - a successful `dotnet nuget push` is not the same as a completed publish;
# validation/indexing happens asynchronously afterward.
set -euo pipefail

VERSION="${1:?Usage: eng/wait-for-nuget-index.sh <version>}"
# NuGet's own docs: "usually under 15 minutes ... if it hasn't published within an hour, contact
# support." The 1.0.0-beta.1 publish run measured this directly: indexing took longer than the
# original 900s (15 min) default, causing a false-failure after the push itself had already
# succeeded (see release notes for that version). 2700s (45 min) gives real headroom under NuGet's
# own "up to an hour is not abnormal" ceiling while staying bounded, not indefinite (spec 22.2).
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-2700}"
POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-15}"

LOWER_VERSION=$(echo "$VERSION" | tr '[:upper:]' '[:lower:]')
PACKAGE_IDS=("xapisharp.net" "xapisharp.net.extensions.dependencyinjection")

for id in "${PACKAGE_IDS[@]}"; do
  URL="https://api.nuget.org/v3-flatcontainer/$id/$LOWER_VERSION/$id.nuspec"
  echo "Waiting for $id $VERSION to be indexed ($URL) ..."
  ELAPSED=0
  until curl -sf -o /dev/null "$URL"; do
    if [[ "$ELAPSED" -ge "$TIMEOUT_SECONDS" ]]; then
      echo "Timed out after ${TIMEOUT_SECONDS}s waiting for $id $VERSION to be indexed - check https://status.nuget.org/ and the package page directly." >&2
      exit 1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
    ELAPSED=$((ELAPSED + POLL_INTERVAL_SECONDS))
  done
  echo "$id $VERSION is indexed."
done
