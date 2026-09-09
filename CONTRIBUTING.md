# Contributing

## Build

```bash
dotnet restore --locked-mode
dotnet build
dotnet test
```

Requires the .NET SDK version pinned in `global.json`.

## Adding an endpoint

All 190 in-scope operations are already implemented and contract-tested (see `docs/coverage.md`)
- this section applies if X adds a new operation to the API, or scope is deliberately widened.
Endpoints are tracked in `spec/endpoint-manifest.json`. Each in-scope operation needs:

1. A typed method in the appropriate generated/manual layer (see `docs/adr/0002-code-generation.md`).
2. A contract test in `tests/XApiSharp.ContractTests`.
3. An updated manifest entry (implementation status, test links) and a refreshed `docs/coverage.md`.

See `docs/maintenance.md` for the full manual-update process used when X's contract changes.

## Code generation

`Generated/` directories are produced by `tools/XApiSharp.CodeGen` from the pinned local snapshot
in `spec/`. Do not hand-edit generated files — put manual code in partial classes, templates, or
overrides instead. Regeneration must be deterministic (no diff on repeated runs with the same
input).

## Tests

- Unit and contract tests run on every PR and must not hit the real X API.
- Integration tests (`tests/XApiSharp.IntegrationTests`) are explicitly opt-in and require a
  dedicated test X application; see that project's README.
- `tests/XApiSharp.PackageTests` verifies the *packed* output, not source - it's deliberately not
  in `XApiSharp.slnx` and never gets a `ProjectReference`. Run it via
  `eng/package-consumer-test.sh <packages-dir> <version>`, not directly.

## Releasing

`eng/release.sh <version>` runs the full release-candidate pipeline (build, test, coverage gates,
pack, hash, manifest, consumer test) locally for a dry run. See `docs/releasing.md` for the full
picture, including what's still blocked on external NuGet setup.
