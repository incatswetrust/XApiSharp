# Releasing

**Status: pipeline complete, first publish is 1.0.0-beta.1.** Every step from build through
publish is implemented and runs green end to end (see "What exists today"). Nothing has been
published yet - the first real publish will be `1.0.0-beta.1`, not `1.0.0`: spec section 27's
final acceptance criteria require "the mandatory minimum of live checks" before something can
honestly be called stable, and live validation is still `blocked-by-budget` (no paid X API
credentials). `1.0.0` stable is a later bump once that's resolved, not part of this first publish.

## What exists today

- **`.github/workflows/ci.yml`** runs on every PR and push to `main`: restore (`--locked-mode`),
  the generation guard-check, the endpoint coverage-matrix gate, a format check, Release build,
  the full test suite with coverage collection, the branch-coverage gate (spec 19.5, ≥80% on
  hand-written core), and `dotnet pack` (which also runs `EnablePackageValidation`'s shape checks).
  It does not publish anywhere.
- **`eng/release.sh <version> [commit] [tag] [ci-run-url]`** - the single documented
  release-candidate entry point (spec 22.2): verifies the given version matches
  `Directory.Build.props`, then restore → generation guard-check → endpoint coverage gate → build
  → test-with-coverage → branch-coverage gate → pack (both packages, same version,
  `ContinuousIntegrationBuild=true`) → SHA-256 hash the artifacts → write `release-manifest.json`
  → run the pre-publish consumer check. Exits non-zero on any failure; never publishes anything
  further. Safe to run locally for a dry run against your own working tree.
- **`.github/workflows/release.yml`** (`workflow_dispatch`, inputs `version` + `ref`): checks out
  the exact given ref, runs `eng/release.sh` on Linux and Windows, uploads the packages and
  reports (hashes, manifest, coverage output) as build artifacts, then runs the macOS
  install/basic-use smoke check (`eng/package-consumer-test.sh`) against the already-built
  packages. Produces verified artifacts for `publish.yml` to pick up - it does not push to
  NuGet.org itself.
- **`.github/workflows/publish.yml`** (`workflow_dispatch`, inputs `version` + `release_run_id`) -
  the actual publish step (spec 23.2). Never rebuilds: downloads the exact packages/reports
  artifacts from the given `release.yml` run, re-verifies their hashes and the release-manifest's
  version against what was requested (`verify-artifacts`, below), authenticates via NuGet Trusted
  Publishing OIDC (`NuGet/login@v1`, `id-token: write` scoped to this job only), pushes both
  packages (symbols auto-detected from the sibling `.snupkg`), waits (bounded) for feed indexing,
  runs the post-publish consumer check against the real published feed, then creates a GitHub
  Release with the packages/manifest/hashes attached. External actions in this file are pinned to
  a commit SHA, not a floating tag (spec 23.1) - this is the one workflow that can push a package
  under this project's identity.
- **`eng/wait-for-nuget-index.sh <version>`** - polls the NuGet v3 flat-container endpoint for
  both package IDs at the given version, bounded by `TIMEOUT_SECONDS` (default 900s).
- **`eng/post-publish-verify.sh <version>`** - spec 23.3: a fresh, isolated `NUGET_PACKAGES` cache
  (never a locally-primed one), restoring/building/running `tests/XApiSharp.PackageTests` against
  the real `https://api.nuget.org/v3/index.json` feed. Automates the restore/compile/run part of
  spec 23.3; the page/README/license/SourceLink/symbols checks it lists are a human look at the
  actual published nuget.org page, printed as a reminder rather than faked.
- **`eng/package-consumer-test.sh <packages-dir> <version>`** - spec 22.3's *pre*-publish consumer
  check: generates a temporary, isolated `NuGet.config` (nuget.org for everything, an explicit
  package-source-mapping entry pointing only the two packages under test at the local pack
  output - this repo's own `NuGet.config` maps every package to nuget.org only, so a plain
  `--source` flag would otherwise be silently ignored), then restores/builds/tests
  `tests/XApiSharp.PackageTests` against exactly that local, not-yet-published version.
- **`tests/XApiSharp.PackageTests/`** - the consumer-scenario project itself, shared by both the
  pre- and post-publish checks. Deliberately **not** listed in `XApiSharp.slnx` and never given a
  `ProjectReference` to `src/` - both would silently make it exercise source instead of the packed
  output. References the two packages via `VersionOverride` (supplied at restore/build time via
  `-p:PackageUnderTestVersion=X.Y.Z`, never committed as a fixed version). Covers read, a
  typed-exception error response, pagination, and cancellation through a fake
  `HttpMessageHandler`, plus a separate DI-registration check (`AddXApiSharpAppOnly`/
  `AddXApiSharpMultiUser` resolving correctly from a real `ServiceProvider`).
- **`tools/XApiSharp.CodeGen`** gained five CI/release-gate commands across E8/E9: `coverage-check`
  (endpoint coverage-matrix gate), `branch-coverage-check` (merges two Cobertura reports and
  enforces the 80% core-file floor), `hash-artifacts`/`release-manifest` (portable dotnet
  implementations, not `sha256sum`/`shasum`/`certutil`, so the same commands work unmodified on
  all three OSes), and `verify-artifacts` (re-checks a downloaded artifact set's hashes and
  version before publish - spec 23.2 step 4).
- **`eng/build.sh`** remains the plain restore → build → test → pack sequence for everyday local
  use (no version check, no hashing, no manifest, no consumer test) - `eng/release.sh` is the
  superset for an actual release candidate.

## Prerequisites already in place

- A NuGet Trusted Publishing policy exists on nuget.org: Repository Owner `incatswetrust`,
  Repository `XApiSharp`, Workflow File `publish.yml`, scoped to the `XApiSharp.*` glob pattern.
- **Still needed before running `publish.yml` for real:** a `NUGET_USER` repository secret (the
  nuget.org account/profile name that owns the Trusted Publishing policy, never an email address)
  and a GitHub Actions environment named `release` (the workflow targets `environment: release` -
  configuring required reviewers on it gives a manual "actually publish" gate even though
  everything else is automated).

## The actual release sequence (spec 23.2)

1. Confirm the beta-release criteria are met (not full spec 27 stable criteria yet - see the
   Status note above). Merge the target commit to `main`.
2. Run **`release.yml`** (`workflow_dispatch`) with the version to release (currently
   `1.0.0-beta.1`) and the exact commit/tag as `ref`. Wait for it to go green on both OSes; note
   its run ID.
3. Inspect that run's uploaded artifacts if you want to eyeball anything before publishing -
   `publish.yml` will re-verify hashes itself regardless.
4. Run **`publish.yml`** (`workflow_dispatch`) with the same version and that run's ID as
   `release_run_id`. It downloads those exact artifacts (never rebuilds), verifies them, pushes
   both packages via Trusted Publishing OIDC, waits for indexing, runs the post-publish consumer
   check, and creates the GitHub Release.
5. Manually finish the checks `eng/post-publish-verify.sh` prints as a reminder (package page,
   README rendering, license, SourceLink, symbols) and update `README.md`'s Status section to
   point at the new package - this step stays manual rather than having CI push back to `main`.

`dotnet nuget push` targets `https://api.nuget.org/v3/index.json`; the short-lived API key from
`NuGet/login@v1` is never logged. A successful push is not the same as a completed publish -
indexing and install availability are checked separately by `eng/wait-for-nuget-index.sh`, and
`--skip-duplicate` is never used to paper over a version conflict.

## Post-publish verification (spec 23.3)

Automated by `eng/post-publish-verify.sh` (restore/compile/run against the real feed with a fresh
package cache); the page/README/dependencies/license/SourceLink/symbols checks it lists at the end
are a human look at the actual nuget.org page - compare what you see there against
`release-manifest.json` (not a whole-ZIP hash comparison; NuGet's repository signature means the
useful content, not the raw container, is what has to match). Only after both the automated and
manual checks pass does a release become `published-and-verified`.

## Failed releases (spec 24)

A published NuGet package version can never be overwritten with different content. A defect gets a
new version, not a patched-in-place old one; the bad version can be unlisted and marked deprecated,
but deletion is not a rollback mechanism
([NuGet's deletion policy](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)).
If the main package publishes but the DI package fails: record the partial release, diagnose
whether the cause was external before simply retrying the unchanged remaining artifact (rerun just
that push step against the same verified `release_run_id` artifacts), cut a new consistent version
if the already-published package's content needs to change, and never move an existing release tag
to a different commit. Any regression gets a reproducing test, a fix, release notes, and a full
repeat of the packaging path - the coverage matrix is never edited to hide a problem.
