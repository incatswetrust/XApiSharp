# API Coverage

## Snapshot

See `docs/product-scope.md` and `spec/spec-manifest.json` for provenance
(source URL, retrieval time, SHA-256, snapshot version).

- Denominator `N` (in-scope operations at this snapshot): **190**
- Source of truth: `spec/endpoint-manifest.json`

## Metrics (per ТЗ section 5.3)

| Metric | Numerator / Denominator | % |
| --- | --- | --- |
| Implementation coverage | 1 / 190 | 0.5% |
| Contract coverage | 1 / 190 | 0.5% |
| Live validation coverage | 0 / 190 | 0% |

Stage E2 (vertical slice) landed the first typed operation: `GET /2/users/{id}`
(`Users.GetByIdAsync`), with contract tests covering method/path, ID escaping, auth header,
success-body deserialization, unknown-field preservation, and the 401/404/429/204 response
shapes. Bulk implementation of the remaining 189 operations happens in E4/E5.

## By family

| Family | Operations | Implemented | Contract-tested | Live-validated |
| --- | --- | --- | --- | --- |
| Users | 36 | 1 | 1 | 0 |
| Stream | 18 | 0 | 0 | 0 |
| Chat | 16 | 0 | 0 | 0 |
| Posts | 14 | 0 | 0 | 0 |
| Broadcasts | 13 | 0 | 0 | 0 |
| Media | 11 | 0 | 0 | 0 |
| Direct Messages | 9 | 0 | 0 | 0 |
| Lists | 9 | 0 | 0 | 0 |
| Webhooks | 8 | 0 | 0 | 0 |
| Bookmarks | 6 | 0 | 0 | 0 |
| Bots | 6 | 0 | 0 | 0 |
| Compliance | 6 | 0 | 0 | 0 |
| Spaces | 6 | 0 | 0 | 0 |
| Account Activity | 5 | 0 | 0 | 0 |
| Activity | 5 | 0 | 0 | 0 |
| Community Notes | 5 | 0 | 0 | 0 |
| Connections | 4 | 0 | 0 | 0 |
| Account | 2 | 0 | 0 | 0 |
| Articles | 2 | 0 | 0 | 0 |
| Communities | 2 | 0 | 0 | 0 |
| News | 2 | 0 | 0 | 0 |
| Trends | 2 | 0 | 0 | 0 |
| Usage | 2 | 0 | 0 | 0 |
| General | 1 | 0 | 0 | 0 |

Regenerate this table from `spec/endpoint-manifest.json` (`implementation`/`contractTests`/
`liveValidation.status` fields) rather than hand-editing counts as work progresses.

## Live validation status legend

`not-run` / `blocked-by-access` / `blocked-by-budget` / `failed` / `passed` — see section 5.3.
All 190 operations are currently `not-run` (no X credentials configured yet, no implementation
exists to validate).
