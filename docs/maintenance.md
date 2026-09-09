# Maintenance after 1.0

Automatic, scheduled checking for X API changes is explicitly deferred by prior project decision
(spec section 25) - it is not a release gate and must not be silently added by whoever picks up
this work later. Until it exists as its own separate task, updates are manual:

1. Pull a new official OpenAPI snapshot.
2. Diff it against `spec/openapi-snapshot.json`: operations, schemas, scopes, required-ness of
   parameters, response shapes, and deprecation notices.
3. Classify each change by whether it affects the wire contract, the public C# API, both, or
   neither.
4. Update `spec/overrides/*`, hand-modeled types, endpoint methods, and the independent test
   fixtures accordingly.
5. Re-run generation, the full test suite, and package validation; update `spec/endpoint-manifest.json`
   and `docs/coverage.md`.
6. Cut a SemVer release with release notes that actually say what changed (see
   `docs/releasing.md`).

A snapshot that can't be fetched isn't evidence that nothing changed - don't treat source
unavailability as "no update needed." Some changes (e.g. a pricing/tier change) can require a docs
fix even when the JSON schemas themselves are untouched.

Future automated watching of X's changelog/OpenAPI source is a candidate for its own separate task
- if built, it should produce a report or a PR for a human to review, never publish a new SDK
version on its own on every upstream change.

## Defect fixing

A regression found post-release follows the same shape as any other fix in this repo: a
reproducing test first, then the fix, then release notes, then the full release path in
`docs/releasing.md` - never a silent edit to the coverage matrix to make a gap disappear.

## Where the ground truth lives

- `spec/openapi-snapshot.json` + `spec/endpoint-manifest.json` - what operations exist and their
  current implementation/contract-test/live-validation status.
- `spec/overrides/generation-guard.json` - which schemas are intentionally hand-modeled instead of
  generated, and why (see `docs/adr/0002-code-generation.md`).
- `docs/coverage.md` - the full coverage matrix (implementation/contract/live-validation/branch
  coverage), regenerated as part of step 5 above.
- `x-api-dotnet-sdk-spec.md` - the original technical decomposition this SDK was built against;
  still the reference for "why does it work this way" on anything not covered by an ADR.
