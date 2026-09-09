# Releasing

**Status: not yet applicable.** This SDK has never been published - there is no NuGet account,
package ownership, or release workflow configured yet (see `.github/workflows/ci.yml`'s own
`TODO (stage E8/E9)` note). This document records the target process (spec sections 22-24) so
it's ready to follow once those prerequisites exist; nothing below should be read as "already
automated."

## What exists today

- `.github/workflows/ci.yml` runs on every PR and push to `main`: restore (`--locked-mode`),
  the generation guard-check, a format check, Release build, the full test suite, and `dotnet pack`.
  It does not publish anywhere.
- `eng/build.sh` is the single documented local entry point for the same
  restore → build → test → pack sequence (spec 22.2's "one documented entry point"), runnable
  outside CI for a local sanity check or a manual package build (see `docs/getting-started.md`'s
  "reference the package" section for using its output locally).

## What's still needed before a release is possible (spec 22-23)

1. **A release-candidate workflow** (`eng/`-driven, spec 22.2): checkout a specific
   commit/tag → restore → generation check → build → test → pack → package-consumer smoke test →
   save artifacts, on Linux and Windows (plus a macOS install/basic-use check), producing two
   `.nupkg`s and matching `.snupkg`s, their SHA-256 hashes, a `release-manifest.json` (version,
   commit, tag, API snapshot, SDK/generator versions, CI/report links), the coverage matrix with
   live-validation dates, and release notes with known limitations.
2. **A pre-publish consumer check** (spec 22.3): a throwaway project outside this solution,
   installing the exact candidate version from a local package folder (never a `ProjectReference`
   to this repo's source), exercising read/error/pagination/cancellation and the DI package's
   registration through a test `HttpMessageHandler`.
3. **NuGet ownership and publishing access** (spec 23.1): a real NuGet.org account with publish
   rights to the `XApiSharp.Net`/`XApiSharp.Net.Extensions.DependencyInjection` package IDs,
   preferably via [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
   (GitHub Actions OIDC, `NuGet/login@v1`, pinned to this exact owner/repo/workflow) rather than a
   long-lived API key stored in the repo. This is an external dependency - not something this
   codebase can configure on its own.

## The release sequence, once the above exists (spec 23.2)

1. Confirm the stable-release criteria (spec section 27) are met.
2. Finalize release notes, version, and API snapshot.
3. Create the release tag on the verified commit; confirm the version isn't already taken.
4. Pull the verified artifacts from the release-candidate job and re-check their hashes.
5. Authorize against NuGet via the chosen mechanism (OIDC preferred).
6. Push the main package, then the DI package, then symbols.
7. Poll (bounded, not indefinite) until the version is indexed and installable.
8. Run the post-publish consumer check (below).
9. Publish a GitHub Release with notes, packages, the manifest, and NuGet links.
10. Update the README with links to the now-stable version.

`dotnet nuget push` targets `https://api.nuget.org/v3/index.json`; the workflow supplies the
package path and API key (from OIDC login or CI secrets) - never logged. A successful push is not
the same as a completed publish: indexing and install availability are checked separately, and
`--skip-duplicate` is never used to paper over a version conflict.

## Post-publish verification (spec 23.3)

In a clean environment with a separate package cache: create a new `net10.0` console project,
install the exact published version of both packages from NuGet.org, verify restore/compile/run of
a standalone consumer scenario, and check the package page, README, dependencies, license,
SourceLink, and symbols. Compare package contents/metadata against the release manifest (not a
whole-ZIP hash comparison - NuGet's repository signature means the useful content, not the
container, is what has to match). Only after this does the release become
`published-and-verified`.

## Failed releases (spec 24)

A published NuGet package version can never be overwritten with different content. A defect gets a
new version, not a patched-in-place old one; the bad version can be unlisted and marked deprecated,
but deletion is not a rollback mechanism
([NuGet's deletion policy](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)).
If the main package publishes but the DI package fails: record the partial release, diagnose
whether the cause was external before simply retrying the unchanged remaining artifact, cut a new
consistent version if the already-published package's content needs to change, and never move an
existing release tag to a different commit. Any regression gets a reproducing test, a fix, release
notes, and a full repeat of the packaging path - the coverage matrix is never edited to hide a
problem.
