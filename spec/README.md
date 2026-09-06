# spec/

Machine-readable source of truth for API coverage (spec sections 5 and 7):

- `spec-manifest.json` — provenance of the OpenAPI snapshot (source URL, retrieval time, SHA-256,
  generator version, overrides version, scope revision).
- `endpoint-manifest.json` — the operation registry: one entry per discovered HTTP operation, its
  scope decision, implementation/contract/live-validation status.
- `openapi-snapshot.json` — the unmodified downloaded snapshot (added once retrieved).
- `overrides/` — documented, sourced corrections where the snapshot and the reference docs
  disagree.

Nothing has been retrieved yet — this is stage E0 work. Do not fabricate a SHA-256 or operation
count before the snapshot is actually downloaded and hashed.
