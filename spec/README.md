# spec/

Machine-readable source of truth for API coverage (spec sections 5 and 7):

- `spec-manifest.json` — provenance of the OpenAPI snapshot (source URL, retrieval time, SHA-256,
  generator version, overrides version, scope revision).
- `endpoint-manifest.json` — the operation registry: one entry per discovered HTTP operation, its
  scope decision, implementation/contract/live-validation status.
- `openapi-snapshot.json` — the unmodified downloaded snapshot.
- `overrides/` — documented, sourced corrections where the snapshot and the reference docs
  disagree.

## Current snapshot

Retrieved 2026-09-06T18:22:51Z from `https://api.x.com/2/openapi.json`, SHA-256
`68f24f5f2332bd60127a86e46f5f34d4b45fc1ad4ff19a91b1d24c3ccda7f995` — 156 paths / 190 operations.
See `docs/product-scope.md` for the family breakdown and open E0 items, and
`endpoint-manifest.json` for the per-operation registry.

This is a **first-pass inventory**: scope decisions are mechanical (in-scope unless it matches a
hard exclusion from the spec section 3.3), auth/scopes are read directly from the snapshot, but
`methodName`, request/response types, and the pagination/streaming/upload `flags` are not yet
manually confirmed. `docsReference` is filled for 163/190 operations from a docs.x.com
cross-check — see `overrides/e0-docs-cross-check.md` for the full discrepancy list (0 operations
documented-with-a-contract are missing from this snapshot; Broadcasts and Bots are fully
undocumented on docs.x.com despite being present here).

## Licensing of `openapi-snapshot.json`

The document's own `info.license` block declares it is governed by the **X Developer Agreement
and Policy** (`https://developer.x.com/en/developer-terms/agreement-and-policy.html`), not MIT.
**Do not relicense this file, or treat it as covered by this repository's root `LICENSE` (MIT).**
The MIT license in this repository applies to original SDK code only.

Precedent for vendoring the raw snapshot alongside generator/SDK code: X's own official generator
repository (`github.com/xdevplatform/xdk`) is MIT-licensed for its generator code but separately
vendors the spec document at `specs/openapi.json` without applying that MIT license to it — same
pattern used here.
