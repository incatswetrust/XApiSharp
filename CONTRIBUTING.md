# Contributing

## Build

```bash
dotnet restore --locked-mode
dotnet build
dotnet test
```

Requires the .NET SDK version pinned in `global.json`.

## Adding an endpoint

Endpoints are tracked in `spec/endpoint-manifest.json` (once populated — see `docs/product-scope.md`).
Each in-scope operation needs:

1. A typed method in the appropriate generated/manual layer (see `docs/adr/0002-code-generation.md`).
2. A contract test in `tests/XApiSharp.ContractTests`.
3. An updated manifest entry (implementation status, test links).

## Code generation

`Generated/` directories are produced by `tools/XApiSharp.CodeGen` from the pinned local snapshot
in `spec/`. Do not hand-edit generated files — put manual code in partial classes, templates, or
overrides instead. Regeneration must be deterministic (no diff on repeated runs with the same
input).

## Tests

- Unit and contract tests run on every PR and must not hit the real X API.
- Integration tests (`tests/XApiSharp.IntegrationTests`) are explicitly opt-in and require a
  dedicated test X application; see that project's README.
